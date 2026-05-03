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

            // ===== 修复 Bug3: 确保场景有相机和光源 =====
            EnsureCameraAndLight();

            UIBuilder.DestroyExistingCanvas("Chapter1Canvas");
            var canvas = UIBuilder.CreateCanvas("Chapter1Canvas");
            var root = canvas.transform;

            // ===== 顶部 HUD =====
            // 修复 Bug2: 标题和进度文字使用锚点定位，确保水平对称居中
            var txtChapterTitle = UIBuilder.CreateText(root, "txtChapterTitle", "",
                fontSize: 36, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 310));

            var txtProgress = UIBuilder.CreateText(root, "txtProgress", "",
                fontSize: 18, anchor: TextAnchor.MiddleCenter,
                color: new Color(1, 1, 1, 0.5f),
                anchoredPos: new Vector2(0, 275));

            // ===== 对话文本区域 =====
            // 修复 Bug1: 合理设置对话区域范围，防止文字溢出重叠
            var dialogueArea = new GameObject("DialogueArea");
            dialogueArea.transform.SetParent(root, false);
            var daRt = dialogueArea.AddComponent<RectTransform>();
            daRt.anchorMin = new Vector2(0.08f, 0.22f);
            daRt.anchorMax = new Vector2(0.92f, 0.72f);
            daRt.offsetMin = Vector2.zero;
            daRt.offsetMax = Vector2.zero;

            var txtDialogue = UIBuilder.CreateText(dialogueArea.transform, "txtDialogue", "",
                fontSize: 26, anchor: TextAnchor.UpperLeft,
                anchoredPos: Vector2.zero, sizeDelta: Vector2.zero);
            var tdRt = txtDialogue.GetComponent<RectTransform>();
            tdRt.anchorMin = Vector2.zero;
            tdRt.anchorMax = Vector2.one;
            tdRt.offsetMin = new Vector2(20, 10);
            tdRt.offsetMax = new Vector2(-20, -10);
            // 修复 Bug1: 设置文字换行和溢出策略
            txtDialogue.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtDialogue.verticalOverflow = VerticalWrapMode.Overflow;

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

        /// <summary>
        /// 修复 Bug3: 确保场景有 Camera 和 Directional Light，
        /// 消除 "Display1 No cameras rendering" 提示
        /// </summary>
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
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 1000f;
                cam.depth = -1;
            }

            if (FindObjectOfType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;
            }
        }
    }
}
