using UnityEngine;

namespace Presentation.UI
{
    /// <summary>
    /// 程序化构建的星光爆裂粒子效果。
    /// 点击屏幕时在世界坐标位置触发一次 burst 粒子发射。
    /// 主粒子 + 拖尾粒子双层效果
    /// </summary>
    public class StarBurstEffect : MonoBehaviour
    {
        private ParticleSystem _psMain;
        private ParticleSystem _psTrail;
        private Material _matStar;
        private Material _matTrail;

        private void Awake()
        {
            CreateParticleSystem();
        }

        /// <summary>在屏幕坐标位置触发一次星光爆裂</summary>
        public void Play(Vector3 screenPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 worldPos = cam.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, 10f));
            transform.position = worldPos;
            _psMain.Play(true);
            _psTrail.Play(true);
        }

        /// <summary>清理动态创建的材质</summary>
        private void OnDestroy()
        {
            if (_matStar != null) Destroy(_matStar);
            if (_matTrail != null) Destroy(_matTrail);
        }

        // ================================================================
        //  程序化纹理生成
        // ================================================================

        /// <summary>生成四角星纹理（匹配 HTML 版星形粒子）</summary>
        private static Texture2D CreateStarTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float outerR = size * 0.45f;
            float innerR = size * 0.12f;
            int points = 4;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                    if (angle < 0) angle += Mathf.PI * 2;

                    float sector = Mathf.PI / points;
                    float a = angle % (sector * 2);
                    float t = a / sector;
                    if (t > 1f) t = 2f - t;
                    float r = Mathf.Lerp(outerR, innerR, t);

                    if (dist <= r)
                    {
                        float nd = dist / outerR;
                        Color c;
                        if (nd < 0.3f)
                        {
                            float lt = nd / 0.3f;
                            c = Color.Lerp(
                                new Color(0.98f, 0.99f, 1f, 1f),
                                new Color(0.86f, 0.94f, 1f, 0.9f), lt);
                        }
                        else if (nd < 0.7f)
                        {
                            float lt = (nd - 0.3f) / 0.4f;
                            c = Color.Lerp(
                                new Color(0.86f, 0.94f, 1f, 0.9f),
                                new Color(0.68f, 0.84f, 1f, 0.4f), lt);
                        }
                        else
                        {
                            float lt = (nd - 0.7f) / 0.3f;
                            c = Color.Lerp(
                                new Color(0.68f, 0.84f, 1f, 0.4f),
                                new Color(0.5f, 0.72f, 1f, 0f), lt);
                        }

                        if (nd < 0.25f)
                        {
                            float glowA = (1f - nd / 0.25f) * 0.95f;
                            c = Color.Lerp(c, new Color(0.98f, 0.99f, 1f, 1f), glowA);
                        }

                        pixels[y * size + x] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>生成小圆点纹理（用于拖尾粒子）</summary>
        private static Texture2D CreateDotTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f;
            float r = size * 0.5f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cx)) / r;
                    if (dist > 1f) { pixels[y * size + x] = Color.clear; continue; }
                    float a = 1f - dist * dist;
                    pixels[y * size + x] = new Color(0.72f, 0.87f, 1f, a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ================================================================
        //  粒子系统构建
        // ================================================================

        private void CreateParticleSystem()
        {
            Texture2D starTex = CreateStarTexture(64);
            Texture2D dotTex = CreateDotTexture(16);

            // ===== 主粒子系统 =====
            var mainGo = new GameObject("MainParticles");
            mainGo.transform.SetParent(transform, false);
            mainGo.layer = 11;
            _psMain = mainGo.AddComponent<ParticleSystem>();
            _psMain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _psMain.main;
            main.duration = 0.3f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.067f, 0.167f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.92f, 0.97f, 1f),
                new Color(0.76f, 0.89f, 1f));
            main.gravityModifier = 0.2f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;

            var emission = _psMain.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 12, 18, 1, 0.01f)
            });

            var shape = _psMain.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            shape.randomDirectionAmount = 1f;

            var vel = _psMain.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            vel.y = new ParticleSystem.MinMaxCurve(-1f, 1f);
            vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);

            // Size over Lifetime: sin(t*π) → 0→1→0
            var sol = _psMain.sizeOverLifetime;
            sol.enabled = true;
            sol.separateAxes = false;
            int n = 20;
            var sizeKeys = new Keyframe[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                sizeKeys[i] = new Keyframe(t, Mathf.Sin(t * Mathf.PI));
            }
            sol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(sizeKeys));

            // Color over Lifetime: 颜色渐变 + Alpha 0→1→0（渐显后渐消）
            var col = _psMain.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.98f, 0.92f), 0f),
                    new GradientColorKey(new Color(1f, 0.96f, 0.85f), 0.5f),
                    new GradientColorKey(new Color(1f, 0.92f, 0.8f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            // Rotation over Lifetime
            var rot = _psMain.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

            // Stretched Billboard 渲染
            var mr = _psMain.GetComponent<ParticleSystemRenderer>();
            mr.renderMode = ParticleSystemRenderMode.Stretch;
            mr.lengthScale = 2f;
            mr.velocityScale = 0.1f;
            mr.sortingOrder = 2;
            _matStar = new Material(Shader.Find("Particles/Standard Unlit"));
            _matStar.SetColor("_Color", Color.white);
            _matStar.mainTexture = starTex;
            mr.material = _matStar;

            // ===== 拖尾粒子系统 =====
            var trailGo = new GameObject("TrailParticles");
            trailGo.transform.SetParent(transform, false);
            trailGo.layer = 11;
            _psTrail = trailGo.AddComponent<ParticleSystem>();
            _psTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var trailMain = _psTrail.main;
            trailMain.duration = 0.3f;
            trailMain.loop = false;
            trailMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            trailMain.startSpeed = 0f;
            trailMain.startSize = new ParticleSystem.MinMaxCurve(0.017f, 0.04f);
            trailMain.startColor = new Color(0.8f, 0.92f, 1f, 0.35f);
            trailMain.maxParticles = 80;
            trailMain.simulationSpace = ParticleSystemSimulationSpace.World;
            trailMain.playOnAwake = false;

            var trailEmission = _psTrail.emission;
            trailEmission.rateOverTime = 25f;

            var trailShape = _psTrail.shape;
            trailShape.shapeType = ParticleSystemShapeType.Sphere;
            trailShape.radius = 0.05f;

            var trailSol = _psTrail.sizeOverLifetime;
            trailSol.enabled = true;
            trailSol.separateAxes = false;
            var trailSizeKeys = new[]
            {
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.3f)
            };
            trailSol.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(trailSizeKeys));

            var trailCol = _psTrail.colorOverLifetime;
            trailCol.enabled = true;
            var trailGrad = new Gradient();
            trailGrad.SetKeys(
                new[] { new GradientColorKey(new Color(0.8f, 0.92f, 1f), 0f) },
                new[]
                {
                    new GradientAlphaKey(0.35f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            trailCol.color = new ParticleSystem.MinMaxGradient(trailGrad);

            var trailMr = _psTrail.GetComponent<ParticleSystemRenderer>();
            trailMr.renderMode = ParticleSystemRenderMode.Billboard;
            trailMr.sortingOrder = 1;
            _matTrail = new Material(Shader.Find("Particles/Standard Unlit"));
            _matTrail.SetColor("_Color", Color.white);
            trailMr.material = _matTrail;
            _matTrail.mainTexture = dotTex;
        }
    }
}
