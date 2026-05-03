using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Core;

namespace Bootstrap
{
    /// <summary>
    /// 挂在 LoginScene 的空 GameObject 上。
    /// 运行时自动构建存档选择页全部 UI 并连线。
    /// </summary>
    public class LoginSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            BaseSceneBootstrap.EnsureGameSystems();

            UIBuilder.DestroyExistingCanvas("LoginCanvas");
            var canvas = UIBuilder.CreateCanvas("LoginCanvas");
            var root = canvas.transform;

            // ===== 页面标题 =====
            var txtPageTitle = UIBuilder.CreateText(root, "txtPageTitle", "选择科考档案",
                fontSize: 42, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 260));

            // ===== 返回键 =====
            var btnBack = UIBuilder.CreateTextButton(root, "btnBack", "返回",
                fontSize: 20,
                anchoredPos: new Vector2(-460, 280),
                sizeDelta: new Vector2(100, 40));

            // ===== 3 个卡槽 =====
            var slotContainers = new GameObject[3];
            var slotTexts = new Text[3];
            var slotButtons = new Button[3];

            float startY = 100f;
            float spacing = 180f;

            for (int i = 0; i < 3; i++)
            {
                float y = startY - i * spacing;

                // 卡槽容器
                var container = new GameObject($"Slot_{i + 1}");
                container.transform.SetParent(root, false);
                var cRt = container.AddComponent<RectTransform>();
                cRt.anchoredPosition = new Vector2(0, y);
                cRt.sizeDelta = new Vector2(600, 150);

                // 卡槽半透明背景
                var cBg = container.AddComponent<Image>();
                cBg.color = new Color(1, 1, 1, 0.04f);

                // 卡槽按钮 (覆盖整个区域)
                var slotBtn = container.AddComponent<Button>();
                slotBtn.targetGraphic = cBg;
                var colors = slotBtn.colors;
                colors.highlightedColor = new Color(1, 1, 1, 0.1f);
                colors.pressedColor = new Color(1, 1, 1, 0.06f);
                slotBtn.colors = colors;

                // 卡槽文字
                var txt = UIBuilder.CreateText(container.transform, "SlotText", "",
                    fontSize: 24, anchor: TextAnchor.MiddleLeft,
                    anchoredPos: new Vector2(20, 0),
                    sizeDelta: new Vector2(560, 140));

                slotContainers[i] = container;
                slotTexts[i] = txt;
                slotButtons[i] = slotBtn;
            }

            // ===== 连线 LoginUI =====
            var ui = gameObject.AddComponent<LoginUI>();
            ui.txtPageTitle = txtPageTitle;
            ui.btnBack = btnBack;
            ui.slotContainers = slotContainers;
            ui.slotTexts = slotTexts;
            ui.slotButtons = slotButtons;
        }
    }
}
