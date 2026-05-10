using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Logic;

namespace Presentation.MiniGame
{
    public class GreenFactorySortingScene : MonoBehaviour
    {
        private const int WINDOW_W = 1920;
        private const int WINDOW_H = 1080;
        private const int CONV_Y = 250;
        private const float CONV_SPEED = 180f;
        private const float SPAWN_INTERVAL = 1.0f;
        private const int MAX_ITEMS = 70;
        private const float GAME_DURATION = 80f;

        private enum ItemType
        {
            SolarPanel, EcoBuilding, RecyclablePlastic,
            IndustrialSlag, ToxicWaste, DefectivePart
        }

        private struct ItemData
        {
            public GameObject go;
            public ItemType type;
            public bool isGood;
        }

        private GreenFactorySortingMiniGame _miniGame;
        private Transform _itemContainer;
        private Camera _cam;
        private Text _txtTimer, _txtCount, _txtAccuracy, _txtGoal;
        private Text _txtFeedback;
        private float _feedbackTimer;
        private GameObject _resultPanel;
        private Text _txtResultTitle, _txtResultDetail;
        private Button _btnRetry;

        private float _spawnTimer;
        private int _totalSpawned;
        private int _selectedIdx = -1;
        private System.Collections.Generic.List<ItemData> _items
            = new System.Collections.Generic.List<ItemData>();

        private readonly string[] _goodLabels = { "光伏", "建材", "塑料" };
        private readonly string[] _failLabels = { "废渣", "废料", "零件" };
        private readonly Color[] _goodColors = {
            new Color(0.2f, 0.5f, 0.85f),
            new Color(0.25f, 0.7f, 0.35f),
            new Color(0.3f, 0.78f, 0.78f)
        };
        private readonly Color[] _failColors = {
            new Color(0.55f, 0.4f, 0.27f),
            new Color(0.7f, 0.2f, 0.2f),
            new Color(0.63f, 0.63f, 0.24f)
        };

        private void Start()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("MiniGameCamera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
                _cam.transform.position = new Vector3(0, 0, -10);
                _cam.orthographic = true;
                _cam.orthographicSize = 5.4f;
            }
            _cam.backgroundColor = new Color(0.18f, 0.18f, 0.22f);

            _miniGame = gameObject.AddComponent<GreenFactorySortingMiniGame>();
            var idField = typeof(MiniGameBase).GetField("miniGameId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField.SetValue(_miniGame, "green_factory_sorting");

            BuildScene();
            UIBuilder.EnsureEventSystem();

            EventBus.Subscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Subscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void BuildScene()
        {
            // 背景（缩放至铺满屏幕）
            var bg = new GameObject("Background");
            bg.transform.SetParent(transform, false);
            var bgSr = bg.AddComponent<SpriteRenderer>();
            bgSr.sprite = MiniGameSprites.CreateFactoryBg();
            bgSr.sortingOrder = -10;
            // 根据相机 orthographicSize 计算缩放，确保铺满
            float camHeight = _cam.orthographicSize * 2f;
            float camWidth = camHeight * _cam.aspect;
            var bgBounds = bgSr.sprite.bounds;
            bg.transform.localScale = new Vector3(
                camWidth / bgBounds.size.x,
                camHeight / bgBounds.size.y,
                1f);

            // 传送带
            var conveyor = new GameObject("Conveyor").transform;
            conveyor.SetParent(transform, false);
            var convSr = new GameObject("Belt").AddComponent<SpriteRenderer>();
            convSr.transform.SetParent(conveyor, false);
            convSr.sprite = MiniGameSprites.CreateConveyorBelt();
            convSr.transform.localPosition = new Vector3(0, CONV_Y * 0.01f, 0);
            convSr.sortingOrder = 0;

            // 物料容器
            _itemContainer = new GameObject("Items").transform;
            _itemContainer.SetParent(transform, false);

            // 分拣区
            CreateZone("PassZone", new Vector2(-3.5f, -3f), new Vector2(4f, 2f),
                new Color(0.1f, 0.4f, 0.15f), "合格品区");
            CreateZone("FailZone", new Vector2(3.5f, -3f), new Vector2(4f, 2f),
                new Color(0.4f, 0.25f, 0.08f), "回收处理区");

            BuildHUD();
            BuildResultPanel();
        }

        private void CreateZone(string name, Vector2 pos, Vector2 size, Color col, string label)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(transform, false);
            zone.transform.localPosition = pos;
            var sr = zone.AddComponent<SpriteRenderer>();
            sr.sprite = MiniGameSprites.CreateRectSprite((int)(size.x * 100), (int)(size.y * 100), col);
            sr.sortingOrder = -1;
            var col2d = zone.AddComponent<BoxCollider2D>();
            col2d.size = size;
            col2d.isTrigger = true;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(zone.transform, false);
            labelGo.transform.localPosition = new Vector3(0, size.y * 0.3f, 0);
            var canvas = labelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;
            var rt = labelGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);
            rt.localScale = Vector3.one * 0.01f;
            var txt = labelGo.AddComponent<Text>();
            txt.text = label;
            txt.font = UIBuilder.DefaultFont;
            txt.fontSize = 36;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        private void BuildHUD()
        {
            var canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var root = canvasGo.transform;

            // 顶栏背景
            var topBar = new GameObject("TopBar");
            topBar.transform.SetParent(root, false);
            var topRt = topBar.AddComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1);
            topRt.anchoredPosition = Vector2.zero;
            topRt.sizeDelta = new Vector2(0, 56);
            topBar.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.15f, 0.85f);

            _txtTimer = UIBuilder.CreateText(root, "txtTimer", "", 32,
                TextAnchor.MiddleLeft, Color.white, new Vector2(-750, -28));
            _txtCount = UIBuilder.CreateText(root, "txtCount", "", 24,
                TextAnchor.MiddleLeft, Color.white, new Vector2(-450, -28));
            _txtAccuracy = UIBuilder.CreateText(root, "txtAccuracy", "", 24,
                TextAnchor.MiddleLeft, Color.white, new Vector2(-150, -28));
            _txtGoal = UIBuilder.CreateText(root, "txtGoal",
                "目标: 分拣≥50% 且 准确率≥50%", 18,
                TextAnchor.MiddleLeft, new Color(0.7f, 0.7f, 0.8f), new Vector2(250, -28));

            // 分拣反馈文字（✓正确 / ✗错误）
            _txtFeedback = UIBuilder.CreateText(root, "txtFeedback", "", 36,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, -200));
            _txtFeedback.text = "";

            // 底部提示
            UIBuilder.CreateText(root, "txtTip",
                "点击物料选中 → 点击下方「合格品区」或「回收处理区」完成分拣", 16,
                TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.7f), new Vector2(0, -500));
        }

        private void BuildResultPanel()
        {
            var canvasGo = new GameObject("ResultCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            _resultPanel = new GameObject("ResultPanel");
            _resultPanel.transform.SetParent(canvasGo.transform, false);
            var rpRt = _resultPanel.AddComponent<RectTransform>();
            rpRt.anchorMin = Vector2.zero;
            rpRt.anchorMax = Vector2.one;
            rpRt.offsetMin = Vector2.zero;
            rpRt.offsetMax = Vector2.zero;
            _resultPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

            var root = _resultPanel.transform;
            _txtResultTitle = UIBuilder.CreateText(root, "txtTitle", "", 48,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, 120));
            _txtResultDetail = UIBuilder.CreateText(root, "txtDetail", "", 24,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, 30));

            _btnRetry = UIBuilder.CreateTextButton(root, "btnRetry", "重新挑战", 32,
                new Vector2(0, -80), new Vector2(300, 70));
            _btnRetry.onClick.AddListener(OnRetry);

            _resultPanel.SetActive(false);
        }

        private void Update()
        {
            if (_miniGame == null || _miniGame.RunState != MiniGameRunState.Running) return;

            // 生成物料
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0 && _totalSpawned < MAX_ITEMS)
            {
                SpawnItem();
                _spawnTimer = SPAWN_INTERVAL;
                _miniGame.SetTotalSpawned(_totalSpawned);
            }

            // 移动物料
            float dx = CONV_SPEED * Time.deltaTime;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item.go == null) { _items.RemoveAt(i); continue; }
                var p = item.go.transform.position;
                p.x += dx * 0.01f;
                if (p.x > 10f)
                {
                    Destroy(item.go);
                    _items.RemoveAt(i);
                    if (_selectedIdx == i) _selectedIdx = -1;
                    else if (_selectedIdx > i) _selectedIdx--;
                    continue;
                }
                item.go.transform.position = p;
            }

            // 点击检测
            if (Input.GetMouseButtonDown(0) && _cam != null)
            {
                Vector2 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
                HandleScreenClick(worldPos);
            }

            // 反馈文字淡出
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f)
                    _txtFeedback.text = "";
            }

            UpdateHUD();
        }

        private void SpawnItem()
        {
            bool isGood = Random.value > 0.4f;
            int idx;
            string label;
            Color col;

            if (isGood)
            {
                idx = Random.Range(0, 3);
                label = _goodLabels[idx];
                col = _goodColors[idx];
            }
            else
            {
                idx = Random.Range(0, 3);
                label = _failLabels[idx];
                col = _failColors[idx];
            }

            var go = new GameObject($"Item_{_totalSpawned}");
            go.transform.SetParent(_itemContainer, false);
            go.transform.position = new Vector3(-9f, CONV_Y * 0.01f, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MiniGameSprites.CreateItemSprite(label, col);
            sr.sortingOrder = 2;

            var col2d = go.AddComponent<BoxCollider2D>();
            col2d.size = new Vector2(0.7f, 0.5f);
            col2d.isTrigger = true;

            _items.Add(new ItemData
            {
                go = go,
                type = isGood ? (ItemType)idx : (ItemType)(idx + 3),
                isGood = isGood
            });
            _totalSpawned++;
        }

        private void UpdateHUD()
        {
            int sec = Mathf.CeilToInt(_miniGame.RemainingTime);
            if (sec < 0) sec = 0;
            _txtTimer.text = $"TIME {sec / 60}:{sec % 60:D2}";
            _txtTimer.color = sec > 15 ? Color.white : (sec > 5 ? Color.yellow : Color.red);

            _txtCount.text = $"已分拣: {_miniGame.TotalSorted} / {_totalSpawned}";
            float acc = _miniGame.AccuracyValue * 100f;
            _txtAccuracy.text = $"准确率: {acc:F0}%";
            _txtAccuracy.color = acc >= 50f ? Color.green : Color.white;
        }

        private void ShowFeedback(bool correct)
        {
            if (correct)
            {
                _txtFeedback.text = "✓ 正确 +1";
                _txtFeedback.color = new Color(0.3f, 1f, 0.4f);
            }
            else
            {
                _txtFeedback.text = "✗ 错误";
                _txtFeedback.color = new Color(1f, 0.3f, 0.3f);
            }
            _feedbackTimer = 1.2f;
        }

        public void OnZoneClicked(bool isPassZone)
        {
            if (_selectedIdx < 0 || _selectedIdx >= _items.Count) return;
            var item = _items[_selectedIdx];
            if (item.go == null) return;

            bool correct = isPassZone == item.isGood;
            _miniGame.RegisterSortResult(correct);
            ShowFeedback(correct);

            Destroy(item.go);
            _items.RemoveAt(_selectedIdx);
            _selectedIdx = -1;
        }

        private void OnRetry()
        {
            foreach (var it in _items) if (it.go != null) Destroy(it.go);
            _items.Clear();
            _totalSpawned = 0;
            _selectedIdx = -1;
            _spawnTimer = 0.5f;

            _miniGame.RetryChallenge();
            _resultPanel.SetActive(false);
        }

        private void OnComplete(MiniGameCompleteEvent evt)
        {
            if (evt.MiniGameId != "green_factory_sorting") return;
            ShowResult(true);
        }

        private void OnFailed(MiniGameFailedEvent evt)
        {
            if (evt.MiniGameId != "green_factory_sorting") return;
            ShowResult(false);
        }

        private void ShowResult(bool passed)
        {
            _resultPanel.SetActive(true);
            float acc = _miniGame.AccuracyValue * 100f;
            float sortPct = _totalSpawned > 0 ? (float)_miniGame.TotalSorted / _totalSpawned * 100f : 0f;

            if (passed)
            {
                _txtResultTitle.text = "分拣完成！";
                _txtResultTitle.color = Color.green;
                _txtResultDetail.text = $"分拣: {_miniGame.TotalSorted}/{_totalSpawned} ({sortPct:F0}%)  |  准确率: {acc:F0}%";
            }
            else
            {
                _txtResultTitle.text = "分拣失败";
                _txtResultTitle.color = Color.red;
                string reason = "";
                if (sortPct < 50f)
                    reason += $"分拣率 {sortPct:F0}% 未达 50%";
                if (acc < 50f)
                    reason += (reason.Length > 0 ? "\n" : "") + $"准确率 {acc:F0}% 未达 50%";
                _txtResultDetail.text = reason;
            }
        }

        public void HandleScreenClick(Vector2 worldPos)
        {
            // 优先检测分拣区
            var hit = Physics2D.OverlapPoint(worldPos, LayerMask.GetMask("Default"));
            if (hit != null)
            {
                if (hit.gameObject.name == "PassZone")
                {
                    OnZoneClicked(true);
                    return;
                }
                if (hit.gameObject.name == "FailZone")
                {
                    OnZoneClicked(false);
                    return;
                }
            }

            // 检测物料
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i].go == null) continue;
                var col = _items[i].go.GetComponent<Collider2D>();
                if (col != null && col.OverlapPoint(worldPos))
                {
                    // 取消之前选中
                    if (_selectedIdx >= 0 && _selectedIdx < _items.Count && _items[_selectedIdx].go != null)
                    {
                        var prevSr = _items[_selectedIdx].go.GetComponent<SpriteRenderer>();
                        if (prevSr != null) prevSr.color = Color.white;
                    }
                    _selectedIdx = i;
                    var sr = _items[i].go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = Color.cyan;
                    return;
                }
            }

            // 点空白 → 取消选中
            if (_selectedIdx >= 0 && _selectedIdx < _items.Count && _items[_selectedIdx].go != null)
            {
                var sr = _items[_selectedIdx].go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;
            }
            _selectedIdx = -1;
        }
    }
}
