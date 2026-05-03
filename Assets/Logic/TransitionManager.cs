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
                case IntentEvent.IntentType.GoToLogin:
                    LoadSceneAsOverlay("LoginScene");
                    break;
                case IntentEvent.IntentType.GoToRegister:
                    // AccountManager记录空槽位
                    AccountManager.Instance.SetActiveSlot(evt.PayloadInt);
                    LoadSceneAsOverlay("RegisterScene");
                    break;
                case IntentEvent.IntentType.LoadAccount:
                    // 读取现存档案后进入介绍页
                    AccountManager.Instance.LoadAccount(evt.PayloadInt);
                    GameManager.Instance.SetState(GameState.AccountSelect);
                    LoadSceneAsOverlay("IntroScene");
                    break;
                case IntentEvent.IntentType.CreateAccount:
                    // 建立新档案后进入介绍页
                    AccountManager.Instance.CreateNewAccount(evt.PayloadInt, evt.PayloadString);
                    GameManager.Instance.SetState(GameState.AccountSelect);
                    LoadSceneAsOverlay("IntroScene");
                    break;
                case IntentEvent.IntentType.GoToMainGame:
                    LoadSceneAsOverlay("Chapter1Scene"); // 根据存档进度读取对应场景
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
            Debug.Log($"[TransitionManager] LoadSceneRoutine starting: {targetSceneName}");
            SetFadeRaycasterEnabled(true);

            // 1. 黑幕缓动淡入 (Fade Out)
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.blocksRaycasts = true;
                float t = 0;
                while (t < fadeDuration)
                {
                    fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
                    t += Time.deltaTime;
                    yield return null;
                }
                fadeCanvasGroup.alpha = 1f;
            }

            // 2. 卸载当前的附加载场景 (薄场景)
            if (!string.IsNullOrEmpty(currentActiveOverlay))
            {
                var asyncUnload = SceneManager.UnloadSceneAsync(currentActiveOverlay);
                if (asyncUnload != null)
                {
                    yield return asyncUnload;
                }
            }

            // 加强内存管控，在卸载完UI后调用一次GC
            Resources.UnloadUnusedAssets();

            // 3. Additive 附加加载新场景，显式设置 allowSceneActivation = true
            Debug.Log($"[TransitionManager] Loading scene: {targetSceneName}");
            var asyncLoad = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (asyncLoad == null)
            {
                Debug.LogError($"[TransitionManager] LoadSceneAsync returned NULL for: {targetSceneName}. Check Build Settings!");
                // 恢复黑幕状态，防止全黑卡死
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = 0f;
                    fadeCanvasGroup.blocksRaycasts = false;
                }
                isTransitioning = false;
                SetFadeRaycasterEnabled(false);
                yield break;
            }
            asyncLoad.allowSceneActivation = true;
            // 等待加载完成（progress 达到 1.0 且 isDone = true）
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            currentActiveOverlay = targetSceneName;

            // 等待一帧确保新场景的 Awake() 全部执行完毕
            yield return null;

            var newScene = SceneManager.GetSceneByName(targetSceneName);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
                Debug.Log($"[TransitionManager] Scene loaded and activated: {targetSceneName}");
            }
            else
            {
                Debug.LogError($"[TransitionManager] Scene not valid after load: {targetSceneName}");
            }

            // 4. 黑幕淡出 (Fade In)
            if (fadeCanvasGroup != null)
            {
                float t = 0;
                while (t < fadeDuration)
                {
                    fadeCanvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeDuration);
                    t += Time.deltaTime;
                    yield return null;
                }
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }

            SetFadeRaycasterEnabled(false);
            isTransitioning = false;
            Debug.Log($"[TransitionManager] LoadSceneRoutine completed: {targetSceneName}");
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