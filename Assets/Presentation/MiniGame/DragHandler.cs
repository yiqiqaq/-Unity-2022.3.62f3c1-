using System;
using UnityEngine;

namespace Presentation.MiniGame
{
    public class DragHandler : MonoBehaviour
    {
        public int EntityId { get; set; }

        // 用距离判定替代 Rect，更可靠
        public Vector2 BinCenter { get; set; }
        public float BinRadius { get; set; } = 1.5f;

        public Action<int, bool, Vector2> OnDragEnded;

        private Camera _cam;
        private Collider2D _col;
        private SpriteRenderer _sr;

        private bool _isDragging;
        private Vector3 _dragOffset;
        private int _originalOrder;

        public bool IsDragging => _isDragging;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null) return;
            }

            if (Input.GetMouseButtonDown(0))
                TryBeginDrag();

            if (_isDragging)
            {
                if (Input.GetMouseButton(0))
                    ContinueDrag();
                else
                    EndDrag();
            }
        }

        private void TryBeginDrag()
        {
            if (_col == null) return;

            Vector2 mouseWorld = ScreenToWorld(Input.mousePosition);
            if (!_col.OverlapPoint(mouseWorld)) return;

            _isDragging = true;
            _dragOffset = (Vector3)mouseWorld - transform.position;

            if (_sr != null)
            {
                _originalOrder = _sr.sortingOrder;
                _sr.sortingOrder = 100;
            }
        }

        private void ContinueDrag()
        {
            Vector2 mouseWorld = ScreenToWorld(Input.mousePosition);
            transform.position = (Vector3)mouseWorld - _dragOffset;
        }

        private void EndDrag()
        {
            _isDragging = false;

            if (_sr != null)
                _sr.sortingOrder = _originalOrder;

            Vector2 pos = transform.position;
            float dist = Vector2.Distance(pos, BinCenter);
            bool inBin = dist <= BinRadius;

            Debug.Log($"[DragHandler] EndDrag: id={EntityId} ({name}) pos={pos} binCenter={BinCenter} dist={dist:F2} radius={BinRadius} inBin={inBin}");

            OnDragEnded?.Invoke(EntityId, inBin, pos);
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (_cam == null) return Vector2.zero;
            Vector3 sp = new Vector3(screenPos.x, screenPos.y,
                Mathf.Abs(_cam.transform.position.z));
            return _cam.ScreenToWorldPoint(sp);
        }

        private void OnDestroy()
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (_sr != null)
                    _sr.sortingOrder = _originalOrder;
            }
        }
    }
}
