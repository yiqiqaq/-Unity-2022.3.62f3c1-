using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Core
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

        // ===== 古风按钮纹理缓存 =====
        private static Sprite _cachedBtnSprite;
        private static Sprite _cachedSlotSprite;

        /// <summary>程序化生成竹简/木牌风格按钮纹理</summary>
        public static Sprite GenerateTraditionalButtonSprite()
        {
            if (_cachedBtnSprite != null) return _cachedBtnSprite;

            int w = 256, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            Color woodBase = new Color(0.38f, 0.24f, 0.13f);
            Color woodDark = new Color(0.22f, 0.14f, 0.07f);
            Color woodLight = new Color(0.50f, 0.34f, 0.18f);
            Color gold = new Color(0.75f, 0.58f, 0.30f, 0.6f);

            // 填充木纹底色 + 微噪波
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.08f, y * 0.3f) * 0.08f;
                    Color c = Color.Lerp(woodBase, woodLight, noise);
                    // 横向木纹条纹
                    float grain = Mathf.Sin(y * 0.5f + Mathf.Sin(x * 0.02f) * 2f) * 0.03f;
                    c += new Color(grain, grain * 0.8f, grain * 0.3f);
                    pixels[y * w + x] = c;
                }
            }

            // 上下边框深色线 (3px)
            for (int x = 0; x < w; x++)
            {
                for (int b = 0; b < 3; b++)
                {
                    pixels[b * w + x] = woodDark;
                    pixels[(h - 1 - b) * w + x] = woodDark;
                }
            }
            // 左右边框深色线 (2px)
            for (int y = 0; y < h; y++)
            {
                for (int b = 0; b < 2; b++)
                {
                    pixels[y * w + b] = woodDark;
                    pixels[y * w + (w - 1 - b)] = woodDark;
                }
            }

            // 回纹角饰 — 四角绘制 L 形纹路
            DrawCornerPattern(pixels, w, h, 6, 16, gold, false, false);
            DrawCornerPattern(pixels, w, h, 6, 16, gold, true, false);
            DrawCornerPattern(pixels, w, h, 6, 16, gold, false, true);
            DrawCornerPattern(pixels, w, h, 6, 16, gold, true, true);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            _cachedBtnSprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f);
            return _cachedBtnSprite;
        }

        /// <summary>程序化生成木纹边框卡槽纹理</summary>
        private static Sprite GenerateSlotSprite()
        {
            if (_cachedSlotSprite != null) return _cachedSlotSprite;

            int w = 256, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            Color fill = new Color(0.20f, 0.14f, 0.08f, 0.45f);
            Color border = new Color(0.50f, 0.36f, 0.18f, 0.8f);
            Color gold = new Color(0.75f, 0.58f, 0.30f, 0.5f);

            // 半透明木纹底
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = fill;

            // 边框 (4px)
            for (int x = 0; x < w; x++)
            {
                for (int b = 0; b < 4; b++)
                {
                    pixels[b * w + x] = border;
                    pixels[(h - 1 - b) * w + x] = border;
                }
            }
            for (int y = 0; y < h; y++)
            {
                for (int b = 0; b < 4; b++)
                {
                    pixels[y * w + b] = border;
                    pixels[y * w + (w - 1 - b)] = border;
                }
            }

            // 回纹角饰
            DrawCornerPattern(pixels, w, h, 8, 20, gold, false, false);
            DrawCornerPattern(pixels, w, h, 8, 20, gold, true, false);
            DrawCornerPattern(pixels, w, h, 8, 20, gold, false, true);
            DrawCornerPattern(pixels, w, h, 8, 20, gold, true, true);

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            _cachedSlotSprite = Sprite.Create(tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f);
            return _cachedSlotSprite;
        }

        /// <summary>绘制回纹角饰 (L形 + 内折)</summary>
        private static void DrawCornerPattern(Color[] pixels, int w, int h,
            int margin, int armLen, Color color, bool flipX, bool flipY)
        {
            int startX = flipX ? w - 1 - margin : margin;
            int startY = flipY ? h - 1 - margin : margin;
            int dirX = flipX ? -1 : 1;
            int dirY = flipY ? -1 : 1;

            // 横臂
            for (int i = 0; i < armLen; i++)
            {
                int x = startX + i * dirX;
                if (x < 0 || x >= w) continue;
                SetPixelSafe(pixels, w, h, x, startY, color);
                SetPixelSafe(pixels, w, h, x, startY + dirY, color);
            }
            // 竖臂
            for (int i = 0; i < armLen; i++)
            {
                int y = startY + i * dirY;
                if (y < 0 || y >= h) continue;
                SetPixelSafe(pixels, w, h, startX, y, color);
                SetPixelSafe(pixels, w, h, startX + dirX, y, color);
            }
            // 内折小横线
            int foldLen = armLen / 3;
            int foldY = startY + dirY * (armLen - 1);
            for (int i = 0; i < foldLen; i++)
            {
                int x = startX + (armLen / 2 + i) * dirX;
                SetPixelSafe(pixels, w, h, x, foldY, color);
            }
        }

        private static void SetPixelSafe(Color[] pixels, int w, int h, int x, int y, Color c)
        {
            if (x >= 0 && x < w && y >= 0 && y < h)
                pixels[y * w + x] = c;
        }

        /// <summary>创建古风竹简/木牌按钮 (程序化纹理 + 金色文字 + 阴影)</summary>
        public static Button CreateTraditionalButton(Transform parent, string name, string label,
            int fontSize = 26, Vector2? anchoredPos = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
            if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;
            else rt.sizeDelta = new Vector2(200, 55);

            var btn = go.AddComponent<Button>();
            var bg = go.AddComponent<Image>();
            bg.sprite = GenerateTraditionalButtonSprite();
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            btn.targetGraphic = bg;

            // 古风色调：hover 提亮，press 变暗如印章
            var colors = btn.colors;
            colors.normalColor = new Color(0.9f, 0.85f, 0.75f);
            colors.highlightedColor = new Color(1.05f, 1.0f, 0.88f);
            colors.pressedColor = new Color(0.6f, 0.5f, 0.38f);
            colors.disabledColor = new Color(0.5f, 0.45f, 0.4f);
            btn.colors = colors;

            // 文字子对象
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(10, 4);
            txtRt.offsetMax = new Vector2(-10, -4);

            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.92f, 0.78f, 0.42f);
            txt.raycastTarget = false;

            // 阴影增加立体感
            var shadow = txtGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);

            return btn;
        }

        /// <summary>创建古风存档卡槽按钮 (木纹边框 + 半透明底)</summary>
        public static Button CreateTraditionalSlotButton(Transform parent, string name,
            Vector2? anchoredPos = null, Vector2? sizeDelta = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
            if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;
            else rt.sizeDelta = new Vector2(600, 150);

            var btn = go.AddComponent<Button>();
            var bg = go.AddComponent<Image>();
            bg.sprite = GenerateSlotSprite();
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            btn.targetGraphic = bg;

            var colors = btn.colors;
            colors.normalColor = new Color(0.95f, 0.9f, 0.82f);
            colors.highlightedColor = new Color(1.1f, 1.05f, 0.95f);
            colors.pressedColor = new Color(0.75f, 0.68f, 0.55f);
            btn.colors = colors;

            return btn;
        }

        /// <summary>创建全屏背景图片 (Resources 文件夹下加载)</summary>
        public static GameObject CreateBackground(Transform parent, string name, string resourcePath)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.white;
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.preserveAspect = false;
            }
            return go;
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
