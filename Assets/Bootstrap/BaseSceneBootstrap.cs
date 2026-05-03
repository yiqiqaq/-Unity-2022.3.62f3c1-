using UnityEngine;
using UnityEngine.UI;
using Logic;

namespace Bootstrap
{
    /// <summary>
    /// 挂在 BaseScene 的空 GameObject 上即可。
    /// 运行时自动创建 GameManager / AccountManager / TransitionManager + 黑幕 Canvas，
    /// 并加载 StartupScene 作为首个叠加场景。
    /// </summary>
    public class BaseSceneBootstrap : MonoBehaviour
    {
        [Tooltip("首个加载的 UI 场景名")]
        [SerializeField] private string firstOverlayScene = "StartupScene";

        private void Awake()
        {
            Debug.Log("[BaseSceneBootstrap] Awake - creating GameSystems");
            var root = new GameObject("[GameSystems]");
            DontDestroyOnLoad(root);

            CreateGameManager(root.transform);
            CreateAccountManager(root.transform);
            CreateTransitionManager(root.transform);
        }

        private void Start()
        {
            Debug.Log($"[BaseSceneBootstrap] Start - loading firstOverlayScene: {firstOverlayScene}");
            Debug.Log($"[BaseSceneBootstrap] TransitionManager.Instance: {(TransitionManager.Instance != null ? "OK" : "NULL!")}");
            if (!string.IsNullOrEmpty(firstOverlayScene))
                TransitionManager.Instance.LoadScene(firstOverlayScene);
        }

        private void CreateGameManager(Transform parent)
        {
            if (GameManager.Instance != null) return;
            var go = new GameObject("[GameManager]");
            go.transform.SetParent(parent, false);
            go.AddComponent<GameManager>();
        }

        private void CreateAccountManager(Transform parent)
        {
            if (AccountManager.Instance != null) return;
            var go = new GameObject("[AccountManager]");
            go.transform.SetParent(parent, false);
            go.AddComponent<AccountManager>();
        }

        private void CreateTransitionManager(Transform parent)
        {
            if (TransitionManager.Instance != null) return;

            // 全屏黑幕 Canvas
            var canvasGo = new GameObject("[FadeCanvas]");
            canvasGo.transform.SetParent(parent, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();

            // 全屏黑色 Image
            var bgGo = new GameObject("FadeBG");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = Color.black;
            bgImage.raycastTarget = false;
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // CanvasGroup 控制透明度
            var canvasGroup = canvasGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            // TransitionManager
            var tmGo = new GameObject("[TransitionManager]");
            tmGo.transform.SetParent(parent, false);
            var tm = tmGo.AddComponent<TransitionManager>();

            // 注入 fadeCanvasGroup (private SerializeField)
            var field = typeof(TransitionManager).GetField("fadeCanvasGroup",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(tm, canvasGroup);

            // 在反射注入后显式初始化黑幕 Canvas 状态
            tm.InitFadeCanvas();
        }
    }
}
