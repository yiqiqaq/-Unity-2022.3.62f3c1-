using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Bootstrap
{
    /// <summary>
    /// 运行时 UI 构建工具，简化 Canvas / Text / Button / InputField 的程序化创建
    /// </summary>
    public static class UIBuilder
    {
        private static Font _defaultFont;

        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                    _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _defaultFont;
            }
        }

        /// <summary>销毁同名旧 Canvas（防止重复 UI）</summary>
        public static void DestroyExistingCanvas(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                Object.Destroy(existing);
        }

        /// <summary>创建 Screen Space - Overlay Canvas + EventSystem</summary>
        public static Canvas CreateCanvas(string name = "Canvas")
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        /// <summary>创建纯文字 Text</summary>
        public static Text CreateText(Transform parent, string name, string content,
            int fontSize = 28, TextAnchor anchor = TextAnchor.MiddleCenter,
            Color? color = null, Vector2? anchoredPos = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
            if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color ?? Color.white;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>创建无边框纯文字按钮</summary>
        public static Button CreateTextButton(Transform parent, string name, string label,
            int fontSize = 28, Vector2? anchoredPos = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
            if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;
            else rt.sizeDelta = new Vector2(300, 50);

            var btn = go.AddComponent<Button>();

            // 按钮背景 (透明)
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.01f); // 近透明但可点击
            btn.targetGraphic = bg;

            // 高亮变色
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.15f);
            colors.pressedColor = new Color(1, 1, 1, 0.08f);
            btn.colors = colors;

            // 文字子对象
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            return btn;
        }

        /// <summary>创建极细下划线 InputField</summary>
        public static InputField CreateInputField(Transform parent, string name,
            string placeholder = "", Vector2? anchoredPos = null, float width = 400, float height = 50)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
            rt.sizeDelta = new Vector2(width, height);

            // 底部极细线
            var lineGo = new GameObject("Underline");
            lineGo.transform.SetParent(go.transform, false);
            var lineRt = lineGo.AddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 0);
            lineRt.anchorMax = new Vector2(1, 0);
            lineRt.sizeDelta = new Vector2(0, 2);
            lineRt.anchoredPosition = Vector2.zero;
            var lineImg = lineGo.AddComponent<Image>();
            lineImg.color = new Color(1, 1, 1, 0.6f);
            lineImg.raycastTarget = false;

            // 输入文字
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 2);
            textRt.offsetMax = new Vector2(-10, 0);
            var inputText = textGo.AddComponent<Text>();
            inputText.font = DefaultFont;
            inputText.fontSize = 30;
            inputText.color = Color.white;
            inputText.supportRichText = false;

            // 占位文字
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(go.transform, false);
            var phRt = placeholderGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(10, 2);
            phRt.offsetMax = new Vector2(-10, 0);
            var phText = placeholderGo.AddComponent<Text>();
            phText.font = DefaultFont;
            phText.fontSize = 30;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(1, 1, 1, 0.35f);
            phText.text = placeholder;

            var inputField = go.AddComponent<InputField>();
            inputField.textComponent = inputText;
            inputField.placeholder = phText;
            inputField.characterLimit = 12;

            return inputField;
        }

        /// <summary>创建全屏半透明遮罩面板 (用于弹窗背景)</summary>
        public static GameObject CreateOverlay(Transform parent, string name, float alpha = 0.6f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = new Color(0, 0, 0, alpha);
            return go;
        }
    }
}
