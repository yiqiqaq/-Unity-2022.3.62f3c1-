using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Presentation.Chapter;
using Presentation.MiniGame;
using Core;

namespace Bootstrap
{
    public class Chapter2SceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log("[Chapter2BS] Awake START");
            BaseSceneBootstrap.EnsureGameSystems();

            EnsureCameraAndLight();

            UIBuilder.DestroyExistingCanvas("Chapter2Canvas");
            var canvas = UIBuilder.CreateCanvas("Chapter2Canvas");
            var root = canvas.transform;

            // 第二章对话背景板
            UIBuilder.CreateBackground(root, "Chapter2Bg", "Chapter2DialogueBg");

            // 顶部 HUD（章节标题，单行）
            var txtChapterTitle = UIBuilder.CreateText(root, "txtChapterTitle", "",
                fontSize: 36, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 310));

            // 对话文本区域
            var dialogueArea = new GameObject("DialogueArea");
            dialogueArea.transform.SetParent(root, false);
            var daRt = dialogueArea.AddComponent<RectTransform>();
            daRt.anchorMin = new Vector2(0.03f, 0.22f);
            daRt.anchorMax = new Vector2(0.97f, 0.72f);
            daRt.offsetMin = Vector2.zero;
            daRt.offsetMax = Vector2.zero;

            var txtDialogue = UIBuilder.CreateText(dialogueArea.transform, "txtDialogue", "",
                fontSize: 27, anchor: TextAnchor.UpperLeft,
                anchoredPos: Vector2.zero, sizeDelta: Vector2.zero);
            var tdRt = txtDialogue.GetComponent<RectTransform>();
            tdRt.anchorMin = Vector2.zero;
            tdRt.anchorMax = Vector2.one;
            tdRt.offsetMin = new Vector2(20, 10);
            tdRt.offsetMax = new Vector2(-20, -10);
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
            choicePanel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.06f, 0.15f); // 高透明度木色底

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

            // 跳过按钮
            var btnSkip = UIBuilder.CreateTextButton(root, "btnSkip", ">>>",
                fontSize: 20, anchoredPos: new Vector2(420, -280), sizeDelta: new Vector2(120, 44));

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
            dialogueUI.btnSkip = btnSkip;
            dialogueUI.choicePanel = choicePanel;
            dialogueUI.choiceContainer = choiceContainer.transform;

            var bfBtn = btnFullScreen.AddComponent<Button>();
            bfBtn.targetGraphic = bfImg;
            bfBtn.onClick.AddListener(() => dialogueUI.OnScreenClicked(Input.mousePosition));

            // 小游戏加载器
            gameObject.AddComponent<MiniGameLoader>();

            // 暂停菜单
            gameObject.AddComponent<PauseMenuUI>().Build(root);

            // Chapter2Handler
            var handler = gameObject.AddComponent<Chapter2Handler>();
            handler.SetCanvas(canvas);
            SetPrivateField(handler, "txtChapterTitle", txtChapterTitle);

            Debug.Log("[Chapter2BS] Awake COMPLETE");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(target, value);
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
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
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
