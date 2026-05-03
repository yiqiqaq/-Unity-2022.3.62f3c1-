using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Presentation.Chapter;
using Core;

namespace Bootstrap
{
    /// <summary>
    /// 挂在 Chapter1Scene 的空 GameObject 上。
    /// 运行时自动构建第一章全部 UI 并连线。
    /// </summary>
    public class Chapter1SceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            BaseSceneBootstrap.EnsureGameSystems();

            UIBuilder.DestroyExistingCanvas("Chapter1Canvas");
            var canvas = UIBuilder.CreateCanvas("Chapter1Canvas");
            var root = canvas.transform;

            // ===== 顶部 HUD =====
            var txtChapterTitle = UIBuilder.CreateText(root, "txtChapterTitle", "",
                fontSize: 36, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 320));

            var txtProgress = UIBuilder.CreateText(root, "txtProgress", "",
                fontSize: 18, anchor: TextAnchor.MiddleCenter,
                color: new Color(1, 1, 1, 0.5f),
                anchoredPos: new Vector2(0, 285));

            // ===== 对话文本区域 =====
            var dialogueArea = new GameObject("DialogueArea");
            dialogueArea.transform.SetParent(root, false);
            var daRt = dialogueArea.AddComponent<RectTransform>();
            daRt.anchorMin = new Vector2(0.10f, 0.25f);
            daRt.anchorMax = new Vector2(0.90f, 0.75f);
            daRt.offsetMin = Vector2.zero;
            daRt.offsetMax = Vector2.zero;

            var txtDialogue = UIBuilder.CreateText(dialogueArea.transform, "txtDialogue", "",
                fontSize: 28, anchor: TextAnchor.UpperLeft,
                anchoredPos: Vector2.zero, sizeDelta: Vector2.zero);
            var tdRt = txtDialogue.GetComponent<RectTransform>();
            tdRt.anchorMin = Vector2.zero;
            tdRt.anchorMax = Vector2.one;
            tdRt.offsetMin = Vector2.zero;
            tdRt.offsetMax = Vector2.zero;

            // ===== 底部操作栏 =====
            var btnAdvance = UIBuilder.CreateTextButton(root, "btnAdvance", "继续",
                fontSize: 26,
                anchoredPos: new Vector2(0, -280),
                sizeDelta: new Vector2(200, 55));

            var btnSkip = UIBuilder.CreateTextButton(root, "btnSkip", "跳过",
                fontSize: 20,
                anchoredPos: new Vector2(420, -280),
                sizeDelta: new Vector2(120, 44));

            // ===== 分支选择面板 =====
            var choicePanel = new GameObject("ChoicePanel");
            choicePanel.transform.SetParent(root, false);
            var cpRt = choicePanel.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.25f, 0.20f);
            cpRt.anchorMax = new Vector2(0.75f, 0.70f);
            cpRt.offsetMin = Vector2.zero;
            cpRt.offsetMax = Vector2.zero;

            var choiceBg = choicePanel.AddComponent<Image>();
            choiceBg.color = new Color(0, 0, 0, 0.55f);

            var choiceContainer = new GameObject("ChoiceContainer");
            choiceContainer.transform.SetParent(choicePanel.transform, false);
            var ccRt = choiceContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = Vector2.zero;
            ccRt.anchorMax = Vector2.one;
            ccRt.offsetMin = new Vector2(30, 20);
            ccRt.offsetMax = new Vector2(-30, -20);

            var ccVLG = choiceContainer.AddComponent<VerticalLayoutGroup>();
            ccVLG.spacing = 12;
            ccVLG.childForceExpandWidth = true;
            ccVLG.childForceExpandHeight = false;
            ccVLG.childAlignment = TextAnchor.MiddleCenter;
            ccVLG.padding = new RectOffset(20, 20, 20, 20);

            var ccFitter = choiceContainer.AddComponent<ContentSizeFitter>();
            ccFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            choicePanel.SetActive(false);

            // ===== 连线 DialogueUI =====
            var dialogueUI = gameObject.AddComponent<DialogueUI>();
            dialogueUI.txtDialogue = txtDialogue;
            dialogueUI.btnAdvance = btnAdvance;
            dialogueUI.btnSkip = btnSkip;
            dialogueUI.choicePanel = choicePanel;
            dialogueUI.choiceContainer = choiceContainer.transform;

            // ===== 连线 Chapter1Handler =====
            var handler = gameObject.AddComponent<Chapter1Handler>();
            // 通过反射设置 private SerializeField
            SetPrivateField(handler, "txtChapterTitle", txtChapterTitle);
            SetPrivateField(handler, "txtProgress", txtProgress);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(target, value);
        }
    }
}
