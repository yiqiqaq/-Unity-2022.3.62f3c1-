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

            // ===== 背景图 =====
            UIBuilder.CreateBackground(root, "BgImage", "StartupBackground");

            // ===== 标题区 =====
            var txtTitle = UIBuilder.CreateText(root, "txtTitle", "淮畔科创行",
                fontSize: 68, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 180));
            var titleShadow = txtTitle.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.9f);
            titleShadow.effectDistance = new Vector2(3, -3);

            // ===== 核心功能键（视觉中心偏下） =====
            var btnSelectSave = UIBuilder.CreateTraditionalButton(root, "btnSelectSave", "选择存档",
                fontSize: 28,
                anchoredPos: new Vector2(-160, -180),
                sizeDelta: new Vector2(220, 58));

            var btnIntro = UIBuilder.CreateTraditionalButton(root, "btnIntro", "了解本次科考",
                fontSize: 28,
                anchoredPos: new Vector2(100, -180),
                sizeDelta: new Vector2(260, 58));

            // ===== 次要操作键（右下角） =====
            var btnQuit = UIBuilder.CreateTraditionalButton(root, "btnQuit", "告别淮畔",
                fontSize: 22,
                anchoredPos: new Vector2(720, -480),
                sizeDelta: new Vector2(160, 48));

            // ===== 退出弹窗 =====
            var overlay = UIBuilder.CreateOverlay(root, "QuitOverlay");
            overlay.SetActive(false);

            var popupGo = new GameObject("QuitPopup");
            popupGo.transform.SetParent(overlay.transform, false);
            var popupRt = popupGo.AddComponent<RectTransform>();
            popupRt.anchoredPosition = Vector2.zero;
            popupRt.sizeDelta = new Vector2(520, 280);
            var popupBg = popupGo.AddComponent<Image>();
            popupBg.sprite = UIBuilder.GenerateTraditionalButtonSprite();
            popupBg.type = Image.Type.Sliced;
            popupBg.color = new Color(0.8f, 0.72f, 0.6f, 0.97f);

            var txtQuitPrompt = UIBuilder.CreateText(popupGo.transform, "txtQuitPrompt",
                "此去山高水长，后会有期？",
                fontSize: 30, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 40));
            txtQuitPrompt.color = new Color(0.92f, 0.78f, 0.42f);
            var promptShadow = txtQuitPrompt.gameObject.AddComponent<Shadow>();
            promptShadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.7f);
            promptShadow.effectDistance = new Vector2(2, -2);

            var btnConfirmQuit = UIBuilder.CreateTraditionalButton(popupGo.transform, "btnConfirmQuit", "归去",
                fontSize: 24,
                anchoredPos: new Vector2(-80, -50),
                sizeDelta: new Vector2(160, 48));

            var btnCancelQuit = UIBuilder.CreateTraditionalButton(popupGo.transform, "btnCancelQuit", "继续科考",
                fontSize: 24,
                anchoredPos: new Vector2(80, -50),
                sizeDelta: new Vector2(160, 48));

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
