using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Core;

namespace Logic
{
    /// <summary>
    /// 全局沉浸式场景转换管理器（挂载于BaseScene的DontDestroyOnLoad节点）
    /// </summary>
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        [Header("黑场过渡设置")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.8f;

        private string currentActiveOverlay = "";
        private bool isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 由 BaseSceneBootstrap 在反射注入 fadeCanvasGroup 后显式调用。
        /// 不能放在 Awake 中，因为 Awake 在 AddComponent 时立即执行，
        /// 此时 fadeCanvasGroup 尚未通过反射注入（仍为 null）。
        /// </summary>
        public void InitFadeCanvas()
        {
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
                // 非过渡期间禁用 GraphicRaycaster，防止遮挡底层 UI
                var raycaster = fadeCanvasGroup.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                    raycaster.enabled = false;
                Debug.Log("[TransitionManager] FadeCanvas initialized: alpha=0, blocksRaycasts=false, GraphicRaycaster=disabled");
            }
            else
            {
                Debug.LogError("[TransitionManager] InitFadeCanvas called but fadeCanvasGroup is NULL!");
            }
        }

        private void OnEnable()
        {
            Debug.Log("[TransitionManager] OnEnable - 订阅 EventBus");
            EventBus.Subscribe<IntentEvent>(OnIntentReceived);
            EventBus.Subscribe<AccountSelectShowEvent>(OnAccountSelectShow);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<IntentEvent>(OnIntentReceived);
            EventBus.Unsubscribe<AccountSelectShowEvent>(OnAccountSelectShow);
        }

        private void OnAccountSelectShow(AccountSelectShowEvent evt)
        {
            LoadSceneAsOverlay("LoginScene");
        }

        private void OnIntentReceived(IntentEvent evt)
        {
            Debug.Log($"[TransitionManager] >>> OnIntentReceived: {evt.Type}, isTransitioning={isTransitioning}");
            // Logic层统一接管页面流转的信号指令
            switch (evt.Type)
            {
                case IntentEvent.IntentType.GoToStartup:
                    LoadSceneAsOverlay("StartupScene");
                    break;
                case IntentEvent.IntentType.GoToSummary:
                    LoadSceneAsOverlay("SummaryScene");
                    break;
                case IntentEvent.IntentType.GoToLogin:
                    LoadSceneAsOverlay("LoginScene");
                    break;
                case IntentEvent.IntentType.GoToRegister:
                    // AccountManager记录空槽位
                    AccountManager.Instance.SetActiveSlot(evt.PayloadInt);
                    LoadSceneAsOverlay("RegisterScene");
                    break;
                case IntentEvent.IntentType.LoadAccount:
                    if (AccountManager.Instance.LoadAccount(evt.PayloadInt))
                    {
                        GameManager.Instance.SetState(GameState.ChapterPlaying);
                        LoadSceneAsOverlay(ResolveChapterScene());
                    }
                    else
                    {
                        Debug.LogError("[TransitionManager] LoadAccount failed, staying on current scene");
                    }
                    break;
                case IntentEvent.IntentType.CreateAccount:
                    // 建立新档案后直接进入第一章
                    AccountManager.Instance.CreateNewAccount(evt.PayloadInt, evt.PayloadString);
                    GameManager.Instance.SetState(GameState.ChapterPlaying);
                    LoadSceneAsOverlay("Chapter1Scene");
                    break;
                case IntentEvent.IntentType.GoToMainGame:
                    LoadSceneAsOverlay(ResolveChapterScene());
                    break;
                case IntentEvent.IntentType.GoToChapter:
                    int chapterIndex = evt.PayloadInt;
                    switch (chapterIndex)
                    {
                        case 1: LoadSceneAsOverlay("Chapter1Scene"); break;
                        case 2: LoadSceneAsOverlay("Chapter2Scene"); break;
                        case 3: LoadSceneAsOverlay("Chapter3Scene"); break;
                        default: LoadSceneAsOverlay("Chapter1Scene"); break;
                    }
                    break;
                case IntentEvent.IntentType.QuitGame:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        /// <summary>根据存档进度确定应加载的章节场景</summary>
        private string ResolveChapterScene()
        {
            var data = GameManager.Instance.CurrentAccountData;
            if (data != null)
            {
                // 从后往前找最后一个有进度的章节
                for (int i = data.ChapterProgresses.Count - 1; i >= 0; i--)
                {
                    var p = data.ChapterProgresses[i];
                    if (p.ChapterId > ChapterIds.Prologue && (p.CurrentStep > 0 || p.IsCompleted))
                    {
                        switch (p.ChapterId)
                        {
                            case ChapterIds.HuaiheEco: return "Chapter1Scene";
                            case ChapterIds.WanbeiManufacturing: return "Chapter2Scene";
                            case ChapterIds.YangtzeDelta: return "Chapter3Scene";
                        }
                    }
                }
            }
            return "Chapter1Scene";
        }

        /// <summary>公开入口：加载指定场景（带黑幕过渡）</summary>
        public void LoadScene(string targetSceneName)
        {
            LoadSceneAsOverlay(targetSceneName);
        }

        private void LoadSceneAsOverlay(string targetSceneName)
        {
            if (isTransitioning)
            {
                Debug.LogWarning($"[TransitionManager] Transition blocked, already transitioning. Ignoring: {targetSceneName}");
                return;
            }
            Debug.Log($"[TransitionManager] LoadSceneAsOverlay: {targetSceneName}");
            StartCoroutine(LoadSceneRoutine(targetSceneName));
        }

        private IEnumerator LoadSceneRoutine(string targetSceneName)
        {
            isTransitioning = true;
            Debug.Log($"[TM] LoadSceneRoutine: {targetSceneName}");

            // 激活黑幕并 Fade-in
            if (fadeCanvasGroup != null)
            {
                var fadeGo = fadeCanvasGroup.gameObject;
                if (!fadeGo.activeSelf) fadeGo.SetActive(true);
                fadeCanvasGroup.blocksRaycasts = true;
                SetFadeRaycasterEnabled(true);

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 1f;
            }

            // 卸载旧场景
            if (!string.IsNullOrEmpty(currentActiveOverlay))
            {
                Debug.Log($"[TM] Unloading: {currentActiveOverlay}");
                yield return SceneManager.UnloadSceneAsync(currentActiveOverlay);
            }

            Resources.UnloadUnusedAssets();

            // 加载新场景
            var asyncLoad = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (asyncLoad == null)
            {
                Debug.LogError($"[TM] LoadSceneAsync NULL: {targetSceneName}");
            }
            else
            {
                asyncLoad.allowSceneActivation = true;
                yield return asyncLoad;

                currentActiveOverlay = targetSceneName;
                yield return null;

                var newScene = SceneManager.GetSceneByName(targetSceneName);
                if (newScene.IsValid())
                {
                    SceneManager.SetActiveScene(newScene);
                    Debug.Log($"[TM] Scene loaded and activated: {targetSceneName}");
#if UNITY_EDITOR
                    var rootObjects = newScene.GetRootGameObjects();
                    Debug.Log($"[TM] Scene root objects count: {rootObjects.Length}");
                    foreach (var ro in rootObjects)
                        Debug.Log($"[TM]   Root: {ro.name} active={ro.activeSelf}");
                    var canvases = UnityEngine.Object.FindObjectsOfType<UnityEngine.Canvas>();
                    Debug.Log($"[TM] Total Canvases in all scenes: {canvases.Length}");
                    foreach (var c in canvases)
                        Debug.Log($"[TM]   Canvas: {c.name} order={c.sortingOrder} active={c.gameObject.activeSelf} scene={c.gameObject.scene.name}");
                    Debug.Log($"[TM] Camera.main: {(Camera.main != null ? Camera.main.name + " scene=" + Camera.main.gameObject.scene.name : "NULL")}");
                    var es = UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
                    Debug.Log($"[TM] EventSystem: {(es != null ? es.name : "NULL")}");
#endif
                }
                else
                {
                    Debug.LogWarning($"[TM] Scene not valid after load: {targetSceneName}");
                }
            }

            // Fade-out: alpha 1→0（无论加载成功与否都执行）
            if (fadeCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                    yield return null;
                }
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
                SetFadeRaycasterEnabled(false);
                fadeCanvasGroup.gameObject.SetActive(false);
                Debug.Log("[TM] FadeCanvas deactivated");
            }

            isTransitioning = false;
            Debug.Log($"[TM] Completed: {targetSceneName}");
        }

        /// <summary>
        /// 禁用/启用黑幕 Canvas 上的 GraphicRaycaster，
        /// 防止非过渡期间遮挡底层 UI 点击。
        /// </summary>
        private void SetFadeRaycasterEnabled(bool enabled)
        {
            if (fadeCanvasGroup == null) return;
            var raycaster = fadeCanvasGroup.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = enabled;
        }
    }
}