using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Presentation.Chapter;
using Core;

namespace Bootstrap
{
    public class SummarySceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[SummaryBS] Awake START");
            BaseSceneBootstrap.EnsureGameSystems();
            EnsureCameraAndLight();

            UIBuilder.DestroyExistingCanvas("SummaryCanvas");
            var canvas = UIBuilder.CreateCanvas("SummaryCanvas");
            var root = canvas.transform;

            // 复用第三章背景板
            UIBuilder.CreateBackground(root, "SummaryBg", "Chapter3DialogueBg");

            // 顶部标题
            var txtTitle = UIBuilder.CreateText(root, "txtTitle", "淮畔科创行 · 调研总结",
                fontSize: 38, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 380));
            txtTitle.color = new Color(0.92f, 0.78f, 0.42f);
            var shadow = txtTitle.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.9f);
            shadow.effectDistance = new Vector2(2, -2);

            // 对话文本区域
            var dialogueArea = new GameObject("DialogueArea");
            dialogueArea.transform.SetParent(root, false);
            var daRt = dialogueArea.AddComponent<RectTransform>();
            daRt.anchorMin = new Vector2(0.05f, 0.22f);
            daRt.anchorMax = new Vector2(0.95f, 0.72f);
            daRt.offsetMin = Vector2.zero;
            daRt.offsetMax = Vector2.zero;

            var txtDialogue = UIBuilder.CreateText(dialogueArea.transform, "txtDialogue", "",
                fontSize: 27, anchor: TextAnchor.UpperLeft,
                anchoredPos: Vector2.zero, sizeDelta: Vector2.zero);
            var tdRt = txtDialogue.GetComponent<RectTransform>();
            tdRt.anchorMin = Vector2.zero;
            tdRt.anchorMax = Vector2.one;
            tdRt.offsetMin = new Vector2(30, 15);
            tdRt.offsetMax = new Vector2(-30, -15);
            txtDialogue.horizontalOverflow = HorizontalWrapMode.Overflow;
            txtDialogue.verticalOverflow = VerticalWrapMode.Overflow;

            // 分支选择面板
            var choicePanel = new GameObject("ChoicePanel");
            choicePanel.transform.SetParent(root, false);
            var cpRt = choicePanel.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.25f, 0.20f);
            cpRt.anchorMax = new Vector2(0.75f, 0.70f);
            cpRt.offsetMin = Vector2.zero;
            cpRt.offsetMax = Vector2.zero;
            choicePanel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.06f, 0.15f);

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

            // 全屏点击区域
            var btnFullScreen = new GameObject("btnFullScreen");
            btnFullScreen.transform.SetParent(root, false);
            var bfRt = btnFullScreen.AddComponent<RectTransform>();
            bfRt.anchorMin = Vector2.zero;
            bfRt.anchorMax = Vector2.one;
            bfRt.offsetMin = Vector2.zero;
            bfRt.offsetMax = Vector2.zero;
            var bfImg = btnFullScreen.AddComponent<Image>();
            bfImg.color = new Color(0, 0, 0, 0.01f);
            bfImg.raycastTarget = true;
            bfRt.SetAsLastSibling();

            // DialogueUI
            var dialogueUI = gameObject.AddComponent<DialogueUI>();
            dialogueUI.txtDialogue = txtDialogue;
            dialogueUI.choicePanel = choicePanel;
            dialogueUI.choiceContainer = choiceContainer.transform;

            var bfBtn = btnFullScreen.AddComponent<Button>();
            bfBtn.targetGraphic = bfImg;
            bfBtn.onClick.AddListener(() => dialogueUI.OnScreenClicked(Input.mousePosition));

            // SummaryHandler
            var handler = gameObject.AddComponent<SummaryHandler>();
            handler.SetCanvas(canvas);

            Debug.Log("[SummaryBS] Awake COMPLETE");
        }

        private void EnsureCameraAndLight()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0, 0, -10);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.08f, 0.16f);
                cam.orthographic = false;
                cam.fieldOfView = 60;
            }
            if (Camera.main != null)
                Camera.main.cullingMask &= ~(1 << 11);
            if (FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
            }
        }
    }
}
