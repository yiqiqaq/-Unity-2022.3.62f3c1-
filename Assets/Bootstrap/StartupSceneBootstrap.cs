using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Core;

namespace Bootstrap
{
    /// <summary>
    /// 挂在 StartupScene 的空 GameObject 上。
    /// 运行时自动构建启动页全部 UI 并连线。
    /// </summary>
    public class StartupSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // 如果直接从 StartupScene 启动（没有经过 BaseScene），
            // 自举创建缺失的游戏系统，确保 TransitionManager 等可用。
            BaseSceneBootstrap.EnsureGameSystems();

            UIBuilder.DestroyExistingCanvas("StartupCanvas");
            var canvas = UIBuilder.CreateCanvas("StartupCanvas");
            var root = canvas.transform;

            // ===== 标题区 =====
            var txtTitle = UIBuilder.CreateText(root, "txtTitle", "淮畔科创行",
                fontSize: 64, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 160));

            // ===== 底部功能键 =====
            var btnSelectSave = UIBuilder.CreateTextButton(root, "btnSelectSave", "选择存档",
                fontSize: 22,
                anchoredPos: new Vector2(-180, -220),
                sizeDelta: new Vector2(160, 44));

            var btnIntro = UIBuilder.CreateTextButton(root, "btnIntro", "了解本次科考",
                fontSize: 22,
                anchoredPos: new Vector2(0, -220),
                sizeDelta: new Vector2(200, 44));

            var btnQuit = UIBuilder.CreateTextButton(root, "btnQuit", "告别淮畔",
                fontSize: 22,
                anchoredPos: new Vector2(180, -220),
                sizeDelta: new Vector2(160, 44));

            // ===== 退出弹窗 =====
            var overlay = UIBuilder.CreateOverlay(root, "QuitOverlay");
            overlay.SetActive(false);

            var popupGo = new GameObject("QuitPopup");
            popupGo.transform.SetParent(overlay.transform, false);
            var popupRt = popupGo.AddComponent<RectTransform>();
            popupRt.anchoredPosition = Vector2.zero;
            popupRt.sizeDelta = new Vector2(500, 260);
            var popupBg = popupGo.AddComponent<Image>();
            popupBg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            var txtQuitPrompt = UIBuilder.CreateText(popupGo.transform, "txtQuitPrompt",
                "此去山高水长，后会有期？",
                fontSize: 30, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 40));

            var btnConfirmQuit = UIBuilder.CreateTextButton(popupGo.transform, "btnConfirmQuit", "归去",
                fontSize: 24,
                anchoredPos: new Vector2(-80, -50),
                sizeDelta: new Vector2(160, 44));

            var btnCancelQuit = UIBuilder.CreateTextButton(popupGo.transform, "btnCancelQuit", "继续科考",
                fontSize: 24,
                anchoredPos: new Vector2(80, -50),
                sizeDelta: new Vector2(160, 44));

            // ===== 连线 StartupUI =====
            var ui = gameObject.AddComponent<StartupUI>();
            ui.txtTitle = txtTitle;
            ui.btnSelectSave = btnSelectSave;
            ui.btnIntro = btnIntro;
            ui.btnQuit = btnQuit;
            ui.quitPopupPanel = overlay;
            ui.txtQuitPrompt = txtQuitPrompt;
            ui.btnConfirmQuit = btnConfirmQuit;
            ui.btnCancelQuit = btnCancelQuit;
        }
    }
}
