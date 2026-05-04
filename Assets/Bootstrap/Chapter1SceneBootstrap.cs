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

            EnsureCameraAndLight();

            UIBuilder.DestroyExistingCanvas("Chapter1Canvas");
            var canvas = UIBuilder.CreateCanvas("Chapter1Canvas");
            var root = canvas.transform;

            // ===== 顶部 HUD =====
            var txtChapterTitle = UIBuilder.CreateText(root, "txtChapterTitle", "",
                fontSize: 36, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 310));

            var txtProgress = UIBuilder.CreateText(root, "txtProgress", "",
                fontSize: 18, anchor: TextAnchor.MiddleCenter,
                color: new Color(1, 1, 1, 0.5f),
                anchoredPos: new Vector2(0, 275));

            // ===== 对话文本区域 =====
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
            txtDialogue.horizontalOverflow = HorizontalWrapMode.Wrap;
            txtDialogue.verticalOverflow = VerticalWrapMode.Overflow;

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

            // ===== 跳过按钮（>>> 样式）=====
            var btnSkip = UIBuilder.CreateTextButton(root, "btnSkip", ">>>",
                fontSize: 20,
                anchoredPos: new Vector2(420, -280),
                sizeDelta: new Vector2(120, 44));

            // ===== 全屏点击区域（代替原"继续"按钮）=====
            var btnFullScreen = new GameObject("btnFullScreen");
            btnFullScreen.transform.SetParent(root, false);
            var bfRt = btnFullScreen.AddComponent<RectTransform>();
            bfRt.anchorMin = Vector2.zero;
            bfRt.anchorMax = Vector2.one;
            bfRt.offsetMin = Vector2.zero;
            bfRt.offsetMax = Vector2.zero;

            var bfImg = btnFullScreen.AddComponent<Image>();
            bfImg.color = new Color(0, 0, 0, 0.01f); // 近透明但可点击
            bfImg.raycastTarget = true;

            // 全屏按钮置于最底层 —— choicePanel 是其兄弟且先创建，
            // 因此 choicePanel 的子按钮优先接收点击事件
            bfRt.SetAsLastSibling();

            // ===== 连线 DialogueUI =====
            var dialogueUI = gameObject.AddComponent<DialogueUI>();
            dialogueUI.txtDialogue = txtDialogue;
            dialogueUI.btnSkip = btnSkip;
            dialogueUI.choicePanel = choicePanel;
            dialogueUI.choiceContainer = choiceContainer.transform;

            // 全屏按钮 → 推进对话 + 粒子特效
            var bfBtn = btnFullScreen.AddComponent<Button>();
            bfBtn.targetGraphic = bfImg;
            bfBtn.onClick.AddListener(() =>
                dialogueUI.OnScreenClicked(Input.mousePosition));

            // ===== 连线 Chapter1Handler =====
            var handler = gameObject.AddComponent<Chapter1Handler>();
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
        /// 确保场景有 Camera 和 Directional Light，
        /// 主相机排除 Particles 层（第 11 层由覆盖相机渲染）
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

            // 主相机不渲染第 11 层（Particles），由覆盖相机负责
            if (Camera.main != null)
                Camera.main.cullingMask &= ~(1 << 11);

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
