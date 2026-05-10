using UnityEngine;

namespace Presentation.MiniGame
{
    /// <summary>
    /// 水面动效 —— 水面波纹动画 + 底部上浮气泡粒子。
    /// 纯代码构建，无需 Animator Controller。
    /// </summary>
    public class WaterEffect : MonoBehaviour
    {
        private ParticleSystem _bubbles;
        private SpriteRenderer[] _waveLines;
        private float[] _waveOffsets;

        /// <summary>构建水面效果</summary>
        public void Setup(float waterSurfaceY, float seabedY)
        {
            CreateWaveLines(waterSurfaceY);
            CreateBubbles(seabedY);
            CreateLightRays(waterSurfaceY);
        }

        private void Update()
        {
            // 波纹水平漂移
            if (_waveLines != null)
            {
                for (int i = 0; i < _waveLines.Length; i++)
                {
                    if (_waveLines[i] == null) continue;
                    var pos = _waveLines[i].transform.localPosition;
                    pos.x = _waveOffsets[i] + Mathf.Sin(Time.time * 0.3f + i * 1.5f) * 40f;
                    _waveLines[i].transform.localPosition = pos;

                    // 透明度波动
                    var c = _waveLines[i].color;
                    c.a = 0.15f + Mathf.Sin(Time.time * 0.4f + i * 0.9f) * 0.08f;
                    _waveLines[i].color = c;
                }
            }
        }

        private void CreateWaveLines(float surfaceY)
        {
            _waveLines = new SpriteRenderer[5];
            _waveOffsets = new float[5];
            Sprite waveSprite = MiniGameSprites.CreateWaveLine(200, 30);

            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject($"Wave_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(-300 + i * 180, surfaceY - i * 8, 0);
                go.transform.localScale = new Vector3(8f, 1.2f, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = waveSprite;
                sr.sortingOrder = 10 + i;
                sr.color = new Color(0.7f, 0.9f, 1f, 0.2f + i * 0.03f);

                _waveLines[i] = sr;
                _waveOffsets[i] = go.transform.localPosition.x;
            }
        }

        private void CreateBubbles(float seabedY)
        {
            var go = new GameObject("Bubbles");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, seabedY, 0);

            _bubbles = go.AddComponent<ParticleSystem>();
            _bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _bubbles.main;
            main.duration = Mathf.Infinity;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startColor = new Color(0.8f, 0.95f, 1f, 0.4f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;
            main.gravityModifier = -0.02f; // 轻微上浮

            var emission = _bubbles.emission;
            emission.rateOverTime = 5f;

            var shape = _bubbles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(12f, 0.1f, 0f);

            // 大小随生命周期变化
            var sol = _bubbles.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.5f, 0.8f),
                new Keyframe(1f, 1.2f)));

            // 透明度随生命周期变化
            var col = _bubbles.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.8f, 0.95f, 1f), 0f) },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.4f, 0.1f),
                    new GradientAlphaKey(0.3f, 0.8f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // 速度随生命周期变慢
            var vol = _bubbles.velocityOverLifetime;
            vol.enabled = true;
            vol.y = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.3f)));

            // 使用程序化气泡纹理
            Sprite bubbleSprite = MiniGameSprites.CreateBubble(12);
            var mr = _bubbles.GetComponent<ParticleSystemRenderer>();
            mr.renderMode = ParticleSystemRenderMode.Billboard;
            mr.sortingOrder = 8;
            mr.material = new Material(Shader.Find("Particles/Standard Unlit"));
            mr.material.mainTexture = bubbleSprite.texture;
        }

        private void CreateLightRays(float surfaceY)
        {
            Sprite raySprite = MiniGameSprites.CreateLightRay(60, 400);

            for (int i = 0; i < 5; i++)
            {
                var go = new GameObject($"LightRay_{i}");
                go.transform.SetParent(transform, false);
                float x = -500 + i * 280 + Random.Range(-40, 40);
                go.transform.localPosition = new Vector3(x, surfaceY - 5, 1);
                float scaleX = 4f + Random.Range(0f, 3f);
                float scaleY = 5f + Random.Range(0f, 2f);
                go.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = raySprite;
                sr.sortingOrder = 1;
                sr.color = new Color(0.85f, 0.95f, 1f, 0.06f + Random.Range(0f, 0.06f));

                // 每条光线缓慢摆动
                var anim = go.AddComponent<AquaticAnimator>();
                anim.Type = AquaticAnimator.AnimType.Reed;
                anim.Speed = Random.Range(0.3f, 0.6f);
            }
        }

        private void OnDestroy()
        {
            // 清理由 CreateBubble 创建的材质
        }
    }
}
