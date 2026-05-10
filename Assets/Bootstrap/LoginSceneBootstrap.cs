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

            // ===== 背景图 =====
            UIBuilder.CreateBackground(root, "BgImage", "StartupBackground");

            // ===== 页面标题 =====
            var txtPageTitle = UIBuilder.CreateText(root, "txtPageTitle", "选择科考档案",
                fontSize: 44, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 260));
            txtPageTitle.color = new Color(0.92f, 0.78f, 0.42f);
            var titleShadow = txtPageTitle.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.9f);
            titleShadow.effectDistance = new Vector2(3, -3);

            // ===== 返回键（次要操作，左上角） =====
            var btnBack = UIBuilder.CreateTraditionalButton(root, "btnBack", "返回",
                fontSize: 20,
                anchoredPos: new Vector2(-780, 460),
                sizeDelta: new Vector2(110, 42));

            // ===== 3 个古风卡槽 =====
            var slotContainers = new GameObject[3];
            var slotTexts = new Text[3];
            var slotButtons = new Button[3];
            var slotDeleteButtons = new Button[3];

            float startY = 100f;
            float spacing = 180f;

            for (int i = 0; i < 3; i++)
            {
                float y = startY - i * spacing;

                // 古风木纹卡槽
                var slotBtn = UIBuilder.CreateTraditionalSlotButton(root, $"Slot_{i + 1}",
                    anchoredPos: new Vector2(0, y),
                    sizeDelta: new Vector2(620, 155));
                var container = slotBtn.gameObject;

                // 卡槽文字（金色）
                var txt = UIBuilder.CreateText(container.transform, "SlotText", "",
                    fontSize: 24, anchor: TextAnchor.MiddleLeft,
                    anchoredPos: new Vector2(25, 0),
                    sizeDelta: new Vector2(480, 140));
                txt.color = new Color(0.92f, 0.82f, 0.55f);
                var slotShadow = txt.gameObject.AddComponent<Shadow>();
                slotShadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.6f);
                slotShadow.effectDistance = new Vector2(1, -1);

                // 删除按钮（卡槽右侧）
                var delBtn = UIBuilder.CreateTraditionalButton(container.transform, "btnDelete", "删除",
                    fontSize: 18,
                    anchoredPos: Vector2.zero,
                    sizeDelta: new Vector2(80, 36));
                var delRt = delBtn.GetComponent<RectTransform>();
                delRt.anchorMin = new Vector2(1, 0.5f);
                delRt.anchorMax = new Vector2(1, 0.5f);
                delRt.pivot = new Vector2(1, 0.5f);
                delRt.anchoredPosition = new Vector2(-15, 0);

                slotContainers[i] = container;
                slotTexts[i] = txt;
                slotButtons[i] = slotBtn;
                slotDeleteButtons[i] = delBtn;
            }

            // ===== 删除确认弹窗 =====
            var deletePopup = UIBuilder.CreateOverlay(root, "DeletePopup", 0.7f);
            var popupContent = new GameObject("PopupContent");
            popupContent.transform.SetParent(deletePopup.transform, false);
            var pcRt = popupContent.AddComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0.5f, 0.5f);
            pcRt.anchorMax = new Vector2(0.5f, 0.5f);
            pcRt.sizeDelta = new Vector2(440, 230);
            var pcBg = popupContent.AddComponent<Image>();
            pcBg.sprite = UIBuilder.GenerateTraditionalButtonSprite();
            pcBg.type = Image.Type.Sliced;
            pcBg.color = new Color(0.8f, 0.72f, 0.6f, 0.97f);

            var txtDeletePrompt = UIBuilder.CreateText(popupContent.transform, "txtDeletePrompt",
                "确定删除该存档？此操作不可撤销。", 26,
                TextAnchor.MiddleCenter, new Color(0.92f, 0.78f, 0.42f), new Vector2(0, 40));
            var delPromptShadow = txtDeletePrompt.gameObject.AddComponent<Shadow>();
            delPromptShadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.7f);
            delPromptShadow.effectDistance = new Vector2(2, -2);

            var btnConfirmDelete = UIBuilder.CreateTraditionalButton(popupContent.transform,
                "btnConfirmDelete", "确认删除", 24,
                new Vector2(-80, -40), new Vector2(140, 48));

            var btnCancelDelete = UIBuilder.CreateTraditionalButton(popupContent.transform,
                "btnCancelDelete", "取消", 24,
                new Vector2(80, -40), new Vector2(140, 48));

            deletePopup.SetActive(false);

            // ===== 连线 LoginUI =====
            var ui = gameObject.AddComponent<LoginUI>();
            ui.txtPageTitle = txtPageTitle;
            ui.btnBack = btnBack;
            ui.slotContainers = slotContainers;
            ui.slotTexts = slotTexts;
            ui.slotButtons = slotButtons;
            ui.slotDeleteButtons = slotDeleteButtons;
            ui.deletePopup = deletePopup;
            ui.txtDeletePrompt = txtDeletePrompt;
            ui.btnConfirmDelete = btnConfirmDelete;
            ui.btnCancelDelete = btnCancelDelete;
        }
    }
}
