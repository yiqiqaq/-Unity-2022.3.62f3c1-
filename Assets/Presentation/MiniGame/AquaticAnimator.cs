using UnityEngine;

namespace Presentation.MiniGame
{
    /// <summary>
    /// 水生生物动画 —— 纯代码驱动鱼游动 / 芦苇摇摆 / 水草摆动。
    /// 无 Animator Controller，挂载到对应 GameObject 上即可。
    /// </summary>
    public class AquaticAnimator : MonoBehaviour
    {
        public enum AnimType { Fish, Reed, Grass }

        public AnimType Type;
        public float Speed = 0.6f;     // 动画速度倍率
        public float SwimRange = 50f;  // 鱼水平游动范围

        private Vector3 _startPos;
        private float _phase;
        private SpriteRenderer _sr;
        private float _direction = 1f;
        private float _nextTurnTime;

        private void Start()
        {
            _startPos = transform.localPosition;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _sr = GetComponent<SpriteRenderer>();
            _nextTurnTime = Time.time + Random.Range(2f, 5f);
        }

        private void Update()
        {
            switch (Type)
            {
                case AnimType.Fish: AnimateFish(); break;
                case AnimType.Reed: AnimateReed(); break;
                case AnimType.Grass: AnimateGrass(); break;
            }
        }

        private void AnimateFish()
        {
            float t = Time.time * Speed;

            // 偶尔掉头
            if (Time.time > _nextTurnTime)
            {
                _direction *= -1f;
                _nextTurnTime = Time.time + Random.Range(4f, 8f);
            }

            // 水平缓游
            float x = _startPos.x + Mathf.Sin(t * 0.25f + _phase) * SwimRange * _direction;
            // 上下微幅摆动
            float y = _startPos.y + Mathf.Sin(t * 0.6f + _phase) * 5f;

            transform.localPosition = new Vector3(x, y, _startPos.z);

            // 翻转朝向
            if (_sr != null)
                _sr.flipX = _direction < 0;
        }

        private void AnimateReed()
        {
            float t = Time.time * Speed;
            // 底部固定，顶部左右摇摆（旋转 Z 轴）
            float angle = Mathf.Sin(t * 0.8f + _phase) * 5f;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void AnimateGrass()
        {
            float t = Time.time * Speed;
            // S 形摆动
            float angle = Mathf.Sin(t * 1.0f + _phase) * 8f
                        + Mathf.Sin(t * 1.7f + _phase * 2f) * 3f;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
