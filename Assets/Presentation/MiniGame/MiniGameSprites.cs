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
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    // 海蓝渐变：顶部浅海蓝 → 底部深海蓝
                    Color c = Color.Lerp(
                        new Color(0.08f, 0.28f, 0.52f, 1f),  // 浅海蓝（水面附近）
                        new Color(0.04f, 0.12f, 0.30f, 1f),  // 深海蓝（水底）
                        t);

                    // 阳光折射波纹（Caustic 效果）
                    float caustic1 = Mathf.Sin(u * 18f + t * 12f + 1.5f) * Mathf.Cos(t * 10f - u * 8f + 0.7f);
                    float caustic2 = Mathf.Sin(u * 14f - t * 16f + 3.0f) * Mathf.Cos(t * 12f + u * 10f - 1.2f);
                    float caustic = (caustic1 + caustic2) * 0.5f;
                    // 阳光从水面透入，越浅越亮
                    float depthFade = Mathf.Pow(1f - t, 1.5f);
                    float causticBright = Mathf.Max(0f, caustic) * 0.15f * depthFade;
                    c.r += causticBright * 0.6f;
                    c.g += causticBright * 0.9f;
                    c.b += causticBright * 1.0f;

                    // 水面附近微亮
                    float surfaceGlow = Mathf.Pow(Mathf.Max(0f, 1f - t * 3f), 2f) * 0.06f;
                    c.r += surfaceGlow * 0.4f;
                    c.g += surfaceGlow * 0.7f;
                    c.b += surfaceGlow;

                    px[y * width + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  2. 水面光线
        // ================================================================
        public static Sprite CreateLightRay(int width = 60, int height = 400)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            for (int y = 0; y < height; y++)
            {
                float t = 1f - (float)y / height;
                float alpha = t * t * 0.25f; // 顶部更亮
                float spread = 1f + (float)y / height * 0.8f;
                for (int x = 0; x < width; x++)
                {
                    float cx = width * 0.5f;
                    float dx = Mathf.Abs(x - cx) / (width * 0.5f * spread);
                    if (dx <= 1f)
                    {
                        float falloff = 1f - dx * dx;
                        float a = alpha * falloff;
                        // 温暖阳光色：偏金黄的白色
                        SetPixelSafe(px, width, height, x, y,
                            new Color(0.85f, 0.95f, 1f, a));
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
        //  4. 鱼（写实风格，银白色鲤鱼/鲫鱼）
        // ================================================================
        public static Sprite CreateFish(int width = 80, int height = 50)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            float cx = width * 0.45f, cy = height * 0.5f;

            // 尾巴（半透明扇形）
            Color tailCol = new Color(0.55f, 0.65f, 0.75f, 0.8f);
            FillTriangle(px, width, height,
                new Vector2(cx - 20, cy),
                new Vector2(cx - 40, cy - 16),
                new Vector2(cx - 40, cy + 16), tailCol);
            // 尾巴内层
            FillTriangle(px, width, height,
                new Vector2(cx - 20, cy),
                new Vector2(cx - 36, cy - 10),
                new Vector2(cx - 36, cy + 10), new Color(0.65f, 0.72f, 0.8f, 0.6f));

            // 身体（银白渐变椭圆）
            Color bodyCol = new Color(0.72f, 0.78f, 0.82f, 1f); // 银灰色
            FillEllipse(px, width, height, cx, cy, 24, 14, bodyCol);
            // 身体上部深色（背部）
            Color backCol = new Color(0.45f, 0.52f, 0.58f, 0.6f);
            FillEllipse(px, width, height, cx, cy + 5, 20, 7, backCol);

            // 肚皮（浅白色）
            Color bellyCol = new Color(0.92f, 0.94f, 0.95f, 1f);
            FillEllipse(px, width, height, cx + 2, cy - 3, 18, 7, bellyCol);

            // 鳞片纹理（交错排列的小高光点）
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    float lx = cx - 12 + col * 5.5f + (row % 2) * 2.5f;
                    float ly = cy - 4 + row * 3.5f;
                    float dx = (lx - cx) / 20f;
                    float dy = (ly - cy) / 11f;
                    if (dx * dx + dy * dy < 0.7f)
                        SetPixelSafe(px, width, height, (int)lx, (int)ly,
                            new Color(0.8f, 0.85f, 0.9f, 0.3f));
                }
            }

            // 背鳍（半透明灰蓝）
            Color finCol = new Color(0.5f, 0.58f, 0.68f, 0.75f);
            FillTriangle(px, width, height,
                new Vector2(cx - 8, cy + 12),
                new Vector2(cx + 8, cy + 12),
                new Vector2(cx + 2, cy + 26), finCol);
            // 背鳍鳍条
            for (int i = 0; i < 4; i++)
            {
                float fx = cx - 5 + i * 4;
                SetPixelSafe(px, width, height, (int)fx, (int)(cy + 14 + i),
                    new Color(0.4f, 0.48f, 0.58f, 0.4f));
            }

            // 眼睛（真实鱼眼：金色虹膜+黑色瞳孔）
            float eyeX = cx + 12, eyeY = cy + 1;
            FillCircle(px, width, height, eyeX, eyeY, 5, Color.white);     // 巩膜
            FillCircle(px, width, height, eyeX + 1, eyeY, 3.5f, new Color(0.85f, 0.7f, 0.2f, 1f)); // 虹膜
            FillCircle(px, width, height, eyeX + 1.5f, eyeY, 1.8f, new Color(0.05f, 0.05f, 0.05f, 1f)); // 瞳孔
            FillCircle(px, width, height, eyeX + 3f, eyeY + 1.5f, 1f, Color.white); // 高光

            // 嘴部
            FillEllipse(px, width, height, cx + 22, cy - 1, 3, 2,
                new Color(0.6f, 0.4f, 0.35f, 1f));

            // 胸鳍
            FillTriangle(px, width, height,
                new Vector2(cx + 4, cy - 10),
                new Vector2(cx + 12, cy - 10),
                new Vector2(cx + 8, cy - 20), finCol);

            // 侧线
            for (int x = (int)(cx - 16); x < (int)(cx + 18); x++)
            {
                float t2 = (float)(x - (cx - 16)) / 34f;
                float ly = cy + Mathf.Sin(t2 * 0.5f) * 1.5f;
                SetPixelSafe(px, width, height, x, (int)ly,
                    new Color(0.55f, 0.6f, 0.65f, 0.35f));
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  5. 芦苇（写实风格）
        // ================================================================
        public static Sprite CreateReed(int width = 24, int height = 130)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            int cx = width / 2;

            // 茎（底部粗，顶部细，自然弯曲）
            for (int y = 0; y < height - 25; y++)
            {
                float t = (float)y / height;
                float sway = Mathf.Sin(y * 0.04f + 0.5f) * 3f;
                int thickness = (int)Mathf.Lerp(3f, 1.5f, t);
                // 颜色渐变：底部深绿 → 顶部黄绿
                Color stemCol = Color.Lerp(
                    new Color(0.28f, 0.42f, 0.18f, 1f),
                    new Color(0.45f, 0.58f, 0.25f, 1f), t);
                for (int dx = -thickness; dx <= thickness; dx++)
                    SetPixelSafe(px, width, height, cx + (int)sway + dx, y, stemCol);
            }

            // 穗（芦花 — 棕色蓬松椭圆）
            float headBaseY = height - 18;
            Color headCol = new Color(0.55f, 0.38f, 0.15f, 1f);
            FillEllipse(px, width, height, cx, headBaseY, 7, 16, headCol);
            // 穗内层（浅棕色）
            FillEllipse(px, width, height, cx, headBaseY - 2, 5, 12, new Color(0.65f, 0.48f, 0.22f, 0.8f));

            // 穗的小花穗（放射状）
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 0.8f - 2.8f;
                float gx = cx + Mathf.Sin(angle) * (4f + i * 0.5f);
                float gy = headBaseY - 6 + Mathf.Cos(angle) * (6f + i * 0.8f);
                Color grainCol = new Color(0.5f + i * 0.02f, 0.35f, 0.12f, 0.7f);
                FillCircle(px, width, height, gx, gy, 1.5f, grainCol);
            }

            // 叶片（底部弯曲的长叶）
            Color leafCol = new Color(0.32f, 0.5f, 0.2f, 0.7f);
            for (int leaf = 0; leaf < 2; leaf++)
            {
                float dir = leaf == 0 ? 1f : -1f;
                float leafBaseY = height * 0.3f + leaf * 15;
                for (int y = 0; y < 30; y++)
                {
                    float lt = (float)y / 30f;
                    float lx = cx + dir * lt * 12f;
                    float ly = leafBaseY - y * 0.5f;
                    SetPixelSafe(px, width, height, (int)lx, (int)ly, leafCol);
                    SetPixelSafe(px, width, height, (int)lx, (int)ly + 1,
                        new Color(leafCol.r, leafCol.g, leafCol.b, 0.4f));
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  6. 水草（写实风格，多条自然弯曲叶片）
        // ================================================================
        public static Sprite CreateGrass(int width = 44, int height = 90)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            // 自然绿色调
            Color[] grassCols = {
                new Color(0.18f, 0.55f, 0.22f, 1f),  // 深绿
                new Color(0.22f, 0.62f, 0.28f, 1f),  // 中绿
                new Color(0.15f, 0.48f, 0.18f, 1f),  // 暗绿
                new Color(0.28f, 0.68f, 0.32f, 1f),  // 亮绿
                new Color(0.12f, 0.42f, 0.15f, 1f),  // 墨绿
            };

            // 画 5 条水草叶片（更自然的分布）
            for (int blade = 0; blade < 5; blade++)
            {
                float baseX = width * 0.15f + blade * (width * 0.18f);
                float phase = blade * 1.1f + 0.3f;
                Color col = grassCols[blade];

                for (int y = 0; y < height; y++)
                {
                    float t = (float)y / height;
                    // 叶片变窄的自然曲线
                    float sway = Mathf.Sin(t * 2.8f + phase) * 9f * t
                               + Mathf.Sin(t * 5.0f + phase * 1.7f) * 2f * t;
                    float x = baseX + sway;
                    // 叶片宽度：底部宽，顶部尖
                    float thickness = 3.0f * (1f - t * t);
                    for (int dx = -(int)thickness; dx <= (int)thickness; dx++)
                    {
                        float dist = Mathf.Abs(dx) / Mathf.Max(1f, thickness);
                        // 叶片中脉颜色稍深
                        Color lc = dist < 0.3f
                            ? new Color(col.r * 0.85f, col.g * 0.85f, col.b * 0.85f, 1f)
                            : col;
                        SetPixelSafe(px, width, height, (int)x + dx, y, lc);
                    }
                }

                // 叶片边缘的半透明效果
                for (int y = 0; y < height; y++)
                {
                    float t = (float)y / height;
                    float sway = Mathf.Sin(t * 2.8f + phase) * 9f * t;
                    float x = baseX + sway;
                    float thickness = 3.0f * (1f - t * t);
                    int edgeX1 = (int)x - (int)thickness - 1;
                    int edgeX2 = (int)x + (int)thickness + 1;
                    SetPixelSafe(px, width, height, edgeX1, y,
                        new Color(col.r, col.g, col.b, 0.3f));
                    SetPixelSafe(px, width, height, edgeX2, y,
                        new Color(col.r, col.g, col.b, 0.3f));
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  7. 塑料瓶（写实风格）
        // ================================================================
        public static Sprite CreateBottle(int width = 36, int height = 56)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            float cx = width * 0.5f;

            // 瓶身（半透明塑料感）
            Color bodyCol = new Color(0.75f, 0.82f, 0.85f, 0.8f);
            FillEllipse(px, width, height, cx, height * 0.45f, 12, 20, bodyCol);
            // 瓶身内部液体
            FillEllipse(px, width, height, cx, height * 0.42f, 10, 16, new Color(0.6f, 0.7f, 0.3f, 0.4f));

            // 瓶颈
            Color neckCol = new Color(0.7f, 0.78f, 0.82f, 0.8f);
            FillRect(px, width, height, (int)cx - 4, height - 20, 8, 15, neckCol);

            // 瓶盖（蓝色）
            Color capCol = new Color(0.2f, 0.4f, 0.75f, 1f);
            FillRect(px, width, height, (int)cx - 5, height - 8, 10, 8, capCol);

            // 高光（塑料反光）
            Color shineCol = new Color(0.95f, 0.97f, 1f, 0.45f);
            FillEllipse(px, width, height, cx - 5, height * 0.4f, 2, 14, shineCol);
            FillEllipse(px, width, height, cx + 4, height * 0.5f, 1.5f, 10, new Color(0.9f, 0.95f, 1f, 0.25f));

            // 标签（彩色标签纸）
            Color labelCol = new Color(0.95f, 0.95f, 0.9f, 0.85f);
            FillRect(px, width, height, (int)cx - 9, (int)(height * 0.33f), 18, 12, labelCol);
            // 标签上的色条
            FillRect(px, width, height, (int)cx - 9, (int)(height * 0.33f), 18, 3, new Color(0.2f, 0.5f, 0.8f, 0.7f));

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  8. 塑料袋（写实风格，水下漂浮变形）
        // ================================================================
        public static Sprite CreateBag(int width = 44, int height = 40)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            // 袋身（半透明白色，水下发青）
            Color bagCol = new Color(0.88f, 0.92f, 0.95f, 0.65f);
            // 不规则椭圆（模拟水下飘浮变形）
            FillEllipse(px, width, height, width * 0.48f, height * 0.52f,
                width * 0.44f, height * 0.38f, bagCol);
            FillEllipse(px, width, height, width * 0.55f, height * 0.45f,
                width * 0.35f, height * 0.32f, new Color(0.9f, 0.94f, 0.96f, 0.45f));

            // 皱褶（更自然的曲线）
            Color wrinkleCol = new Color(0.75f, 0.8f, 0.85f, 0.35f);
            for (int i = 0; i < 4; i++)
            {
                float wy = height * 0.25f + i * height * 0.14f;
                for (int x = (int)(width * 0.15f); x < width * 0.85f; x++)
                {
                    float wave = Mathf.Sin(x * 0.25f + i * 1.5f) * 3f
                               + Mathf.Sin(x * 0.5f + i * 0.7f) * 1.5f;
                    SetPixelSafe(px, width, height, x, (int)(wy + wave), wrinkleCol);
                    SetPixelSafe(px, width, height, x, (int)(wy + wave) + 1,
                        new Color(wrinkleCol.r, wrinkleCol.g, wrinkleCol.b, 0.15f));
                }
            }

            // 边缘高光（水下折射）
            Color edgeCol = new Color(0.95f, 0.98f, 1f, 0.25f);
            FillEllipse(px, width, height, width * 0.5f, height * 0.5f,
                width * 0.40f, height * 0.35f, edgeCol);

            // 水下气泡附着
            FillCircle(px, width, height, width * 0.3f, height * 0.3f, 2,
                new Color(0.85f, 0.95f, 1f, 0.35f));
            FillCircle(px, width, height, width * 0.7f, height * 0.6f, 1.5f,
                new Color(0.85f, 0.95f, 1f, 0.3f));

            tex.SetPixels(px);
            tex.Apply();
            return TexToSprite(tex);
        }

        // ================================================================
        //  9. 油污块（写实风格，虹彩反射）
        // ================================================================
        public static Sprite CreateOilSlick(int width = 50, int height = 32)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] px = new Color[width * height];
            FillClear(px);

            // 主体深色（不规则形状：叠加多个椭圆）
            Color oilCol = new Color(0.12f, 0.1f, 0.08f, 0.88f);
            FillEllipse(px, width, height, width * 0.5f, height * 0.5f,
                width * 0.42f, height * 0.38f, oilCol);
            FillEllipse(px, width, height, width * 0.32f, height * 0.48f,
                width * 0.22f, height * 0.28f, oilCol);
            FillEllipse(px, width, height, width * 0.68f, height * 0.52f,
                width * 0.20f, height * 0.24f, oilCol);
            // 延伸的薄膜
            FillEllipse(px, width, height, width * 0.45f, height * 0.42f,
                width * 0.38f, height * 0.32f, new Color(0.15f, 0.12f, 0.1f, 0.45f));

            // 虹彩反射（多色斑点模拟油膜光干涉）
            Color shine1 = new Color(0.25f, 0.45f, 0.55f, 0.4f);
            FillEllipse(px, width, height, width * 0.35f, height * 0.38f, 7, 4, shine1);
            Color shine2 = new Color(0.4f, 0.25f, 0.5f, 0.35f);
            FillEllipse(px, width, height, width * 0.6f, height * 0.55f, 6, 3, shine2);
            Color shine3 = new Color(0.2f, 0.5f, 0.35f, 0.3f);
            FillEllipse(px, width, height, width * 0.5f, height * 0.45f, 8, 3, shine3);
            Color shine4 = new Color(0.55f, 0.35f, 0.2f, 0.25f);
            FillEllipse(px, width, height, width * 0.4f, height * 0.55f, 5, 2.5f, shine4);

            // 表面小气泡
            FillCircle(px, width, height, width * 0.28f, height * 0.35f, 1.5f,
                new Color(0.6f, 0.65f, 0.7f, 0.3f));
            FillCircle(px, width, height, width * 0.72f, height * 0.4f, 1f,
                new Color(0.6f, 0.65f, 0.7f, 0.25f));

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
