using UnityEngine;

namespace Presentation.MiniGame
{
    /// <summary>
    /// 程序化精灵工厂 —— 运行时生成"大鱼吃小鱼"风格水下场景所需的全部精灵，
    /// 零外部素材依赖。参考 StarBurstEffect.CreateStarTexture() 的像素级生成模式。
    /// </summary>
    public static class MiniGameSprites
    {
        // ================================================================
        //  辅助：Texture2D → Sprite
        // ================================================================
        private static Sprite TexToSprite(Texture2D tex, float ppu = 100f)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), ppu);
        }

        private static void FillClear(Color[] px)
        {
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        }

        private static void SetPixelSafe(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            // Alpha 混合
            Color dst = px[y * w + x];
            float a = c.a + dst.a * (1f - c.a);
            if (a < 0.001f) { px[y * w + x] = Color.clear; return; }
            px[y * w + x] = new Color(
                (c.r * c.a + dst.r * dst.a * (1f - c.a)) / a,
                (c.g * c.a + dst.g * dst.a * (1f - c.a)) / a,
                (c.b * c.a + dst.b * dst.a * (1f - c.a)) / a,
                a);
        }

        // 画填充椭圆
        private static void FillEllipse(Color[] px, int w, int h,
            float cx, float cy, float rx, float ry, Color col)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1f)
                        SetPixelSafe(px, w, h, x, y, col);
                }
        }

        // 画填充圆
        private static void FillCircle(Color[] px, int w, int h,
            float cx, float cy, float r, Color col)
        {
            FillEllipse(px, w, h, cx, cy, r, r, col);
        }

        // 填充矩形
        private static void FillRect(Color[] px, int w, int h,
            int rx, int ry, int rw, int rh, Color col)
        {
            for (int y = ry; y < ry + rh && y < h; y++)
                for (int x = rx; x < rx + rw && x < w; x++)
                    if (x >= 0 && y >= 0)
                        SetPixelSafe(px, w, h, x, y, col);
        }

        // 画三角形（重心坐标法）
        private static void FillTriangle(Color[] px, int w, int h,
            Vector2 p0, Vector2 p1, Vector2 p2, Color col)
        {
            float minX = Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x));
            float maxX = Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x));
            float minY = Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y));
            float maxY = Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y));
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(maxX));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(maxY));

            float d00 = p1.x - p0.x; float d01 = p2.x - p0.x;
            float d10 = p1.y - p0.y; float d11 = p2.y - p0.y;
            float denom = d00 * d11 - d01 * d10;
            if (Mathf.Abs(denom) < 0.001f) return;
            float invDenom = 1f / denom;

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d20 = x - p0.x; float d21 = y - p0.y;
                    float u = (d11 * d20 - d01 * d21) * invDenom;
                    float v = (d00 * d21 - d10 * d20) * invDenom;
                    float w2 = 1 - u - v;
                    if (u >= 0 && v >= 0 && w2 >= 0)
                        SetPixelSafe(px, w, h, x, y, col);
                }
        }

        // ================================================================
        //  1. 水面背景
        // ================================================================
        public static Sprite CreateWaterBg(int width = 1280, int height = 720)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                // 深蓝 → 湖蓝渐变
                Color c = Color.Lerp(
                    new Color(0.06f, 0.18f, 0.40f, 1f),  // 深水
                    new Color(0.12f, 0.42f, 0.65f, 1f),  // 浅水
                    t * 0.6f + 0.2f * Mathf.Sin(t * Mathf.PI));
                for (int x = 0; x < width; x++)
                    px[y * width + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  2. 水面光线
        // ================================================================
        public static Sprite CreateLightRay(int width = 40, int height = 300)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            for (int y = 0; y < height; y++)
            {
                float t = 1f - (float)y / height;
                float alpha = t * t * 0.18f; // 顶部亮，底部消失
                float spread = 1f + (float)y / height * 0.5f;
                for (int x = 0; x < width; x++)
                {
                    float cx = width * 0.5f;
                    float dx = Mathf.Abs(x - cx) / (width * 0.5f * spread);
                    if (dx <= 1f)
                    {
                        float a = alpha * (1f - dx * dx);
                        SetPixelSafe(px, width, height, x, y,
                            new Color(0.7f, 0.9f, 1f, a));
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  3. 气泡
        // ================================================================
        public static Sprite CreateBubble(int radius = 12)
        {
            int size = radius * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            FillClear(px);

            float cx = radius, cy = radius;
            float r = radius - 1;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    if (dist > 1f) continue;

                    // 边缘亮，内部透明
                    float edge = Mathf.Clamp01((dist - 0.7f) / 0.3f);
                    float alpha = edge * 0.6f;

                    // 高光（左上）
                    float hx = (x - cx + r * 0.3f) / r;
                    float hy = (y - cy + r * 0.3f) / r;
                    float hDist = Mathf.Sqrt(hx * hx + hy * hy);
                    float highlight = Mathf.Clamp01(1f - hDist * 2.5f) * 0.5f;

                    Color c = new Color(0.8f, 0.95f, 1f, Mathf.Max(alpha, highlight));
                    px[y * size + x] = c;
                }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  4. 卡通鱼（Q版，大鱼吃小鱼风格）
        // ================================================================
        public static Sprite CreateFish(int width = 80, int height = 50)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            float cx = width * 0.45f, cy = height * 0.5f;

            // 尾巴（三角形）
            Color tailCol = new Color(1f, 0.55f, 0.15f, 1f);
            FillTriangle(px, width, height,
                new Vector2(cx - 18, cy),
                new Vector2(cx - 38, cy - 14),
                new Vector2(cx - 38, cy + 14), tailCol);

            // 身体（椭圆）
            Color bodyCol = new Color(1f, 0.65f, 0.2f, 1f);
            FillEllipse(px, width, height, cx, cy, 22, 14, bodyCol);

            // 肚皮
            Color bellyCol = new Color(1f, 0.92f, 0.7f, 1f);
            FillEllipse(px, width, height, cx + 2, cy - 2, 16, 8, bellyCol);

            // 背鳍
            Color finCol = new Color(1f, 0.5f, 0.1f, 1f);
            FillTriangle(px, width, height,
                new Vector2(cx - 5, cy + 12),
                new Vector2(cx + 5, cy + 12),
                new Vector2(cx, cy + 24), finCol);

            // 眼睛白
            float eyeX = cx + 10, eyeY = cy + 2;
            FillCircle(px, width, height, eyeX, eyeY, 5, Color.white);
            // 眼睛黑
            FillCircle(px, width, height, eyeX + 1.5f, eyeY, 2.5f, new Color(0.1f, 0.1f, 0.1f, 1f));
            // 眼睛高光
            FillCircle(px, width, height, eyeX + 3f, eyeY + 1.5f, 1.2f, Color.white);

            // 嘴巴
            FillEllipse(px, width, height, cx + 20, cy - 2, 3, 2,
                new Color(0.85f, 0.3f, 0.2f, 1f));

            // 腹鳍
            FillTriangle(px, width, height,
                new Vector2(cx + 2, cy - 12),
                new Vector2(cx + 10, cy - 12),
                new Vector2(cx + 6, cy - 20), finCol);

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  5. 芦苇
        // ================================================================
        public static Sprite CreateReed(int width = 20, int height = 120)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            int cx = width / 2;
            Color stemCol = new Color(0.35f, 0.55f, 0.2f, 1f);

            // 茎（竖条）
            for (int y = 0; y < height - 20; y++)
            {
                float sway = Mathf.Sin(y * 0.05f) * 2f;
                for (int dx = -2; dx <= 2; dx++)
                    SetPixelSafe(px, width, height, cx + (int)sway + dx, y, stemCol);
            }

            // 穗（顶部棕色椭圆）
            Color headCol = new Color(0.6f, 0.4f, 0.15f, 1f);
            FillEllipse(px, width, height, cx, height - 12, 6, 12, headCol);

            // 穗的小颗粒
            Color grainCol = new Color(0.5f, 0.3f, 0.1f, 1f);
            for (int i = 0; i < 5; i++)
            {
                float gy = height - 6 - i * 4;
                float gx = cx + Mathf.Sin(i * 1.2f) * 3;
                FillCircle(px, width, height, gx, gy, 2, grainCol);
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  6. 水草（多条波浪形叶片）
        // ================================================================
        public static Sprite CreateGrass(int width = 40, int height = 80)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            Color[] grassCols = {
                new Color(0.2f, 0.7f, 0.3f, 1f),
                new Color(0.25f, 0.75f, 0.35f, 1f),
                new Color(0.15f, 0.65f, 0.25f, 1f),
                new Color(0.3f, 0.8f, 0.4f, 1f)
            };

            // 画 4 条水草叶片
            for (int blade = 0; blade < 4; blade++)
            {
                float baseX = width * 0.2f + blade * (width * 0.2f);
                float phase = blade * 1.3f;
                Color col = grassCols[blade];

                for (int y = 0; y < height; y++)
                {
                    float t = (float)y / height;
                    float sway = Mathf.Sin(t * 3.14f + phase) * 8f * t;
                    float x = baseX + sway;
                    float thickness = 2.5f * (1f - t * 0.5f);
                    for (int dx = -(int)thickness; dx <= (int)thickness; dx++)
                        SetPixelSafe(px, width, height, (int)x + dx, y, col);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  7. 塑料瓶
        // ================================================================
        public static Sprite CreateBottle(int width = 36, int height = 56)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            float cx = width * 0.5f;

            // 瓶身（红色）
            Color bodyCol = new Color(0.85f, 0.2f, 0.2f, 1f);
            FillEllipse(px, width, height, cx, height * 0.45f, 12, 20, bodyCol);

            // 瓶颈
            Color neckCol = new Color(0.75f, 0.18f, 0.18f, 1f);
            FillRect(px, width, height, (int)cx - 4, height - 20, 8, 15, neckCol);

            // 瓶盖
            Color capCol = new Color(0.9f, 0.9f, 0.9f, 1f);
            FillRect(px, width, height, (int)cx - 5, height - 8, 10, 8, capCol);

            // 高光
            Color shineCol = new Color(1f, 0.5f, 0.5f, 0.5f);
            FillEllipse(px, width, height, cx - 4, height * 0.4f, 3, 12, shineCol);

            // 标签
            Color labelCol = new Color(1f, 1f, 1f, 0.7f);
            FillRect(px, width, height, (int)cx - 9, (int)(height * 0.35f), 18, 10, labelCol);

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  8. 塑料袋
        // ================================================================
        public static Sprite CreateBag(int width = 44, int height = 36)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            // 袋身（半透明白色）
            Color bagCol = new Color(0.95f, 0.95f, 0.98f, 0.75f);
            FillEllipse(px, width, height, width * 0.5f, height * 0.5f,
                width * 0.45f, height * 0.4f, bagCol);

            // 皱褶线
            Color wrinkleCol = new Color(0.85f, 0.85f, 0.9f, 0.4f);
            for (int i = 0; i < 3; i++)
            {
                float wy = height * 0.3f + i * height * 0.15f;
                for (int x = (int)(width * 0.2f); x < width * 0.8f; x++)
                {
                    float wave = Mathf.Sin(x * 0.3f + i) * 2;
                    SetPixelSafe(px, width, height, x, (int)(wy + wave), wrinkleCol);
                }
            }

            // 边缘高光
            Color edgeCol = new Color(1f, 1f, 1f, 0.3f);
            FillEllipse(px, width, height, width * 0.5f, height * 0.5f,
                width * 0.42f, height * 0.37f, edgeCol);

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  9. 油污块
        // ================================================================
        public static Sprite CreateOilSlick(int width = 44, int height = 28)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            // 主体深色
            Color oilCol = new Color(0.15f, 0.12f, 0.1f, 0.85f);
            // 不规则形状：叠加多个椭圆
            FillEllipse(px, width, height, width * 0.5f, height * 0.5f,
                width * 0.42f, height * 0.35f, oilCol);
            FillEllipse(px, width, height, width * 0.35f, height * 0.45f,
                width * 0.25f, height * 0.3f, oilCol);
            FillEllipse(px, width, height, width * 0.65f, height * 0.55f,
                width * 0.22f, height * 0.25f, oilCol);

            // 光泽点（虹彩效果）
            Color shineCol = new Color(0.3f, 0.5f, 0.6f, 0.35f);
            FillEllipse(px, width, height, width * 0.4f, height * 0.4f,
                6, 3, shineCol);
            Color shineCol2 = new Color(0.4f, 0.3f, 0.5f, 0.25f);
            FillEllipse(px, width, height, width * 0.6f, height * 0.55f,
                5, 3, shineCol2);

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  10. 回收箱
        // ================================================================
        public static Sprite CreateBin(int width = 80, int height = 120)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            int margin = 6;
            Color binCol = new Color(0.2f, 0.7f, 0.3f, 1f);
            Color darkCol = new Color(0.15f, 0.55f, 0.22f, 1f);
            Color rimCol = new Color(0.25f, 0.8f, 0.35f, 1f);

            // 桶身
            FillRect(px, width, height, margin, margin * 3,
                width - margin * 2, height - margin * 4, binCol);

            // 桶身渐变条纹
            for (int y = margin * 3; y < height - margin; y += 8)
                FillRect(px, width, height, margin + 2, y,
                    width - margin * 2 - 4, 3, darkCol);

            // 桶口（稍宽的边缘）
            FillRect(px, width, height, margin - 2, height - margin * 4 - 4,
                width - margin * 2 + 4, 8, rimCol);

            // 桶底
            FillRect(px, width, height, margin + 2, margin * 2,
                width - margin * 2 - 4, 6, darkCol);

            // 回收箭头标志（白色循环箭头）
            Color arrowCol = Color.white;
            float acx = width * 0.5f, acy = height * 0.45f;
            float ar = 18;
            // 画一个简易循环箭头（弧线 + 箭头头）
            for (float angle = 0; angle < Mathf.PI * 1.5f; angle += 0.05f)
            {
                float ax = acx + Mathf.Cos(angle) * ar;
                float ay = acy + Mathf.Sin(angle) * ar;
                for (int r = -2; r <= 2; r++)
                {
                    SetPixelSafe(px, width, height, (int)ax + r, (int)ay, arrowCol);
                    SetPixelSafe(px, width, height, (int)ax, (int)ay + r, arrowCol);
                }
            }
            // 箭头头
            FillTriangle(px, width, height,
                new Vector2(acx + ar, acy - 6),
                new Vector2(acx + ar + 8, acy),
                new Vector2(acx + ar, acy + 6), arrowCol);

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  11. 水面波纹条（用于 LineRenderer 或 Sprite 拼接）
        // ================================================================
        public static Sprite CreateWaveLine(int width = 200, int height = 30)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            for (int x = 0; x < width; x++)
            {
                float t = (float)x / width;
                float wave = Mathf.Sin(t * Mathf.PI * 4) * (height * 0.3f);
                int y = (int)(height * 0.5f + wave);
                for (int dy = -1; dy <= 1; dy++)
                    SetPixelSafe(px, width, height, x, y + dy,
                        new Color(0.6f, 0.85f, 1f, 0.35f));
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  12. 沙地/水底
        // ================================================================
        public static Sprite CreateSeabed(int width = 1280, int height = 80)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];

            Color sandCol1 = new Color(0.55f, 0.45f, 0.3f, 1f);
            Color sandCol2 = new Color(0.5f, 0.4f, 0.25f, 1f);

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    // 随机微小噪点
                    float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.1f + t) * 0.15f;
                    Color c = Color.Lerp(sandCol1, sandCol2, t + noise);
                    px[y * width + x] = c;
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  13. 失败/成功图标用纹理
        // ================================================================
        public static Sprite CreateStar(int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f, cy = size * 0.5f;
            float outerR = size * 0.45f, innerR = size * 0.18f;
            Color[] px = new Color[size * size];
            FillClear(px);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                    if (angle < 0) angle += Mathf.PI * 2;
                    float sector = Mathf.PI / 5;
                    float a = angle % (sector * 2);
                    float t = a / sector;
                    if (t > 1f) t = 2f - t;
                    float r = Mathf.Lerp(outerR, innerR, t);
                    if (dist <= r)
                    {
                        float nd = dist / outerR;
                        Color c = Color.Lerp(
                            new Color(1f, 0.9f, 0.2f, 1f),
                            new Color(1f, 0.7f, 0.1f, 1f), nd);
                        px[y * size + x] = c;
                    }
                }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  14. 工厂背景
        // ================================================================
        public static Sprite CreateFactoryBg(int width = 1280, int height = 720)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                float noise = Mathf.PerlinNoise(y * 0.02f, 0.5f) * 0.05f;
                Color c = Color.Lerp(
                    new Color(0.22f, 0.22f, 0.27f),
                    new Color(0.28f, 0.27f, 0.25f), t + noise);
                for (int x = 0; x < width; x++)
                    px[y * width + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  15. 传送带
        // ================================================================
        public static Sprite CreateConveyorBelt(int width = 1280, int height = 60)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            Color beltCol = new Color(0.32f, 0.32f, 0.36f);
            Color lineCol = new Color(0.45f, 0.45f, 0.5f);
            Color edgeCol = new Color(0.5f, 0.5f, 0.55f);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (y < 2 || y >= height - 2)
                        px[y * width + x] = edgeCol;
                    else if (x % 40 < 2)
                        px[y * width + x] = lineCol;
                    else
                        px[y * width + x] = beltCol;
                }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  16. 纯色矩形精灵
        // ================================================================
        public static Sprite CreateRectSprite(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            for (int i = 0; i < px.Length; i++) px[i] = col;
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  17. 物料精灵（带中文标签的色块）
        // ================================================================
        public static Sprite CreateItemSprite(string label, Color col, int width = 70, int height = 50)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            Color edgeCol = col * 0.7f;
            edgeCol.a = 1f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool isEdge = x < 2 || x >= width - 2 || y < 2 || y >= height - 2;
                    px[y * width + x] = isEdge ? edgeCol : col;
                }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  18. 匹配卡片精灵（科技风）
        // ================================================================
        public static Sprite CreateCardSprite(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            Color edgeCol = col * 1.3f;
            edgeCol.a = 1f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool isEdge = x < 2 || x >= width - 2 || y < 2 || y >= height - 2;
                    px[y * width + x] = isEdge ? edgeCol : col;
                }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }
    }
}
