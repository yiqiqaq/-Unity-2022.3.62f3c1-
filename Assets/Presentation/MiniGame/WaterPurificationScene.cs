using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core;

namespace Presentation.MiniGame
{
    public class WaterPurificationScene : MonoBehaviour
    {
        private const float WATER_SURFACE_Y = 4.2f;
        private const float SEABED_Y = -4.5f;
        private const float BIN_WORLD_X = 5.5f;
        private const float BIN_WORLD_Y = 0f;
        private const float SPAWN_INTERVAL = 3f;
        private const int INITIAL_POLLUTANTS = 6;
        private const int INITIAL_AQUATIC = 4;
        private const int TARGET_POLLUTANT_SPAWNS = 15;
        private const int MAX_ON_SCREEN = 10;

        private WaterPurificationMiniGame _game;
        private Camera _cam;

        private Transform _entityLayer;
        private Rect _binWorldRect;

        private Canvas _hudCanvas;
        private Text _txtTimer;
        private Text _txtProgress;
        private Text _txtMisTouch;

        private GameObject _resultPanel;
        private Text _txtResultTitle;
        private Text _txtResultDetail;

        private struct EntityData
        {
            public int Id;
            public bool IsPollutant;
            public GameObject Go;
            public Vector3 OriginalPos;
        }
        private List<EntityData> _entities = new List<EntityData>();
        private int _nextEntityId;
        private int _pollutantSpawnCount;
        private int _aquaticSpawnCount;
        private float _spawnTimer;
        private bool _initialized;

        private void Awake()
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
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = new Color(0.04f, 0.12f, 0.30f, 1f);
            }

            UIBuilder.EnsureEventSystem();

            var logicGo = new GameObject("GameLogic");
            logicGo.transform.SetParent(transform, false);
            _game = logicGo.AddComponent<WaterPurificationMiniGame>();
            var idField = typeof(MiniGameBase).GetField("miniGameId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField.SetValue(_game, "water_purification");

            EventBus.Subscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Subscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void Start()
        {
            BuildScene();
            _initialized = true;
            Debug.Log("[水净化] 场景初始化完成，游戏开始");
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_game.RunState != MiniGameRunState.Running) return;

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                bool canSpawnPollutant = _pollutantSpawnCount < TARGET_POLLUTANT_SPAWNS;
                bool screenNotFull = _entities.Count < MAX_ON_SCREEN;

                if (canSpawnPollutant && screenNotFull)
                {
                    SpawnEntity(true);
                    if (Random.Range(0, 3) == 0 && screenNotFull)
                        SpawnEntity(false);
                }
                _spawnTimer = SPAWN_INTERVAL;
            }

            UpdateHUD();
        }

        // ================================================================
        //  场景构建
        // ================================================================

        private void BuildScene()
        {
            BuildBackground();
            BuildDecorations();
            BuildBin();
            BuildEntities();
            BuildHUD();
            BuildResultPanel();
            _spawnTimer = SPAWN_INTERVAL;
        }

        private void BuildBackground()
        {
            var bgGo = new GameObject("WaterBg");
            bgGo.transform.SetParent(transform, false);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = MiniGameSprites.CreateWaterBg(1280, 720);
            bgSr.sortingOrder = -10;
            bgGo.transform.localScale = new Vector3(0.01f, 0.01f, 1f);

            var seabedGo = new GameObject("Seabed");
            seabedGo.transform.SetParent(transform, false);
            var sbSr = seabedGo.AddComponent<SpriteRenderer>();
            sbSr.sprite = MiniGameSprites.CreateSeabed(1280, 80);
            sbSr.sortingOrder = -5;
            seabedGo.transform.localPosition = new Vector3(0, SEABED_Y + 0.2f, 0);
            seabedGo.transform.localScale = new Vector3(0.01f, 0.01f, 1f);
        }

        private void BuildDecorations()
        {
            var decorLayer = new GameObject("Decorations").transform;
            decorLayer.SetParent(transform, false);

            var waterFx = gameObject.AddComponent<WaterEffect>();
            waterFx.Setup(WATER_SURFACE_Y, SEABED_Y);

            Sprite grassSprite = MiniGameSprites.CreateGrass(40, 80);
            Sprite reedSprite = MiniGameSprites.CreateReed(20, 120);

            for (int i = 0; i < 8; i++)
            {
                float x = -5.5f + i * 1.4f + Random.Range(-0.3f, 0.3f);
                bool isReed = Random.Range(0, 3) == 0;

                var go = new GameObject(isReed ? $"Reed_{i}" : $"Grass_{i}");
                go.transform.SetParent(decorLayer, false);
                go.transform.localPosition = new Vector3(x, SEABED_Y + 0.3f, 0);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = isReed ? reedSprite : grassSprite;
                sr.sortingOrder = 2;

                var anim = go.AddComponent<AquaticAnimator>();
                anim.Type = isReed ? AquaticAnimator.AnimType.Reed : AquaticAnimator.AnimType.Grass;
                anim.Speed = Random.Range(0.6f, 1.2f);
            }
        }

        private void BuildBin()
        {
            var binGo = new GameObject("RecycleBin");
            binGo.transform.SetParent(transform, false);
            binGo.transform.localPosition = new Vector3(BIN_WORLD_X, BIN_WORLD_Y, 0);

            var binSr = binGo.AddComponent<SpriteRenderer>();
            binSr.sprite = MiniGameSprites.CreateBin(80, 120);
            binSr.sortingOrder = 50;

            // 用距离检测替代 Rect，更可靠
            // 记录回收箱中心位置和判定半径
            _binWorldRect = new Rect(0, 0, 0, 0); // 不再使用
        }

        private void BuildEntities()
        {
            _entityLayer = new GameObject("Entities").transform;
            _entityLayer.SetParent(transform, false);

            for (int i = 0; i < INITIAL_POLLUTANTS; i++) SpawnEntity(true);
            for (int i = 0; i < INITIAL_AQUATIC; i++) SpawnEntity(false);

            Debug.Log($"[水净化] 初始实体: 污染物 {_pollutantSpawnCount} + 水生 {_aquaticSpawnCount} = 屏上 {_entities.Count}");
        }

        // ================================================================
        //  实体生成
        // ================================================================

        private void SpawnEntity(bool isPollutant)
        {
            int id = _nextEntityId++;

            if (isPollutant)
            {
                _pollutantSpawnCount++;
                _game.RegisterPollutantSpawned();
            }
            else
            {
                _aquaticSpawnCount++;
            }

            Sprite sprite;
            string name;
            float scale;

            if (isPollutant)
            {
                int type = Random.Range(0, 3);
                switch (type)
                {
                    case 0:
                        sprite = MiniGameSprites.CreateBottle(36, 56);
                        name = "Bottle";
                        scale = 0.8f;
                        break;
                    case 1:
                        sprite = MiniGameSprites.CreateBag(44, 36);
                        name = "Bag";
                        scale = 0.8f;
                        break;
                    default:
                        sprite = MiniGameSprites.CreateOilSlick(44, 28);
                        name = "OilSlick";
                        scale = 0.8f;
                        break;
                }
            }
            else
            {
                int type = Random.Range(0, 3);
                switch (type)
                {
                    case 0:
                        sprite = MiniGameSprites.CreateFish(80, 50);
                        name = "Fish";
                        scale = 0.7f;
                        break;
                    case 1:
                        sprite = MiniGameSprites.CreateReed(20, 120);
                        name = "Reed";
                        scale = 0.6f;
                        break;
                    default:
                        sprite = MiniGameSprites.CreateGrass(40, 80);
                        name = "Grass";
                        scale = 0.6f;
                        break;
                }
            }

            var go = new GameObject(name);
            go.transform.SetParent(_entityLayer, false);

            float x = Random.Range(-5.5f, 4.5f);
            float y = Random.Range(SEABED_Y + 1f, WATER_SURFACE_Y - 0.5f);
            var spawnPos = new Vector3(x, y, 0);
            go.transform.localPosition = spawnPos;
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20 + id;

            if (name == "Fish")
            {
                var anim = go.AddComponent<AquaticAnimator>();
                anim.Type = AquaticAnimator.AnimType.Fish;
                anim.Speed = Random.Range(0.2f, 0.45f);
                anim.SwimRange = Random.Range(30f, 55f);
            }

            var col = go.AddComponent<BoxCollider2D>();
            var bounds = sr.sprite.bounds;
            col.size = new Vector2(bounds.size.x, bounds.size.y);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var drag = go.AddComponent<DragHandler>();
            drag.EntityId = id;
            drag.BinCenter = new Vector2(BIN_WORLD_X, BIN_WORLD_Y);
            drag.BinRadius = 1.5f; // 距离回收箱中心 1.5 世界单位内算投入
            drag.OnDragEnded = OnEntityDropped;

            _entities.Add(new EntityData { Id = id, IsPollutant = isPollutant, Go = go, OriginalPos = spawnPos });
        }

        // ================================================================
        //  拖拽回调
        // ================================================================

        private void OnEntityDropped(int entityId, bool inBin, Vector2 dropPos)
        {
            Debug.Log($"[水净化] OnEntityDropped: entityId={entityId} inBin={inBin} dropPos={dropPos} 屏上实体数={_entities.Count}");

            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Id != entityId) continue;
                var data = _entities[i];

                if (inBin)
                {
                    Debug.Log($"[水净化] 入箱! isPollutant={data.IsPollutant} 名={data.Go.name}");
                    _game.OnEntityDropped(data.IsPollutant);
                    Debug.Log($"[水净化] 计数更新: 清理={_game.CleanedPollutants} / 总污染物={_game.TotalPollutants} 误触={_game.MisTouchCount}");
                    StartCoroutine(ShrinkAndDestroy(data.Go));
                    _entities.RemoveAt(i);
                }
                else
                {
                    Debug.Log($"[水净化] 未入箱，弹回原位");
                    StartCoroutine(SpringBack(data.Go, data.OriginalPos));
                }
                return;
            }

            Debug.LogWarning($"[水净化] entityId={entityId} 在实体列表中未找到!");
        }

        private IEnumerator ShrinkAndDestroy(GameObject go)
        {
            if (go == null) yield break;
            float elapsed = 0f;
            Vector3 origScale = go.transform.localScale;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = 1f - elapsed / 0.25f;
                if (go != null)
                    go.transform.localScale = origScale * t;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private IEnumerator SpringBack(GameObject go, Vector3 targetLocalPos)
        {
            if (go == null) yield break;
            Vector3 startPos = go.transform.localPosition;
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                if (go != null)
                    go.transform.localPosition = Vector3.Lerp(startPos, targetLocalPos, t);
                yield return null;
            }
            if (go != null)
                go.transform.localPosition = targetLocalPos;
        }

        // ================================================================
        //  HUD
        // ================================================================

        private void BuildHUD()
        {
            // 独立 Canvas（不挂在场景 GO 下），与 UIBuilder.CreateCanvas 模式一致
            _hudCanvas = UIBuilder.CreateCanvas("MiniGameHUD");
            _hudCanvas.sortingOrder = 100;
            var canvasGo = _hudCanvas.gameObject;

            // 顶栏背景
            var topBarGo = new GameObject("TopBar");
            topBarGo.transform.SetParent(canvasGo.transform, false);
            var topBarRt = topBarGo.AddComponent<RectTransform>();
            topBarRt.anchorMin = new Vector2(0, 1);
            topBarRt.anchorMax = new Vector2(1, 1);
            topBarRt.pivot = new Vector2(0.5f, 1);
            topBarRt.anchoredPosition = Vector2.zero;
            topBarRt.sizeDelta = new Vector2(0, 60);
            var topBarImg = topBarGo.AddComponent<Image>();
            topBarImg.color = new Color(0.05f, 0.05f, 0.15f, 0.85f);

            _txtTimer = MakeText(canvasGo.transform, "txtTimer",
                "01:30", 32, new Vector2(-600, -30), Color.white);
            _txtProgress = MakeText(canvasGo.transform, "txtProgress",
                "已清理: 0 / 0 (0%)", 24, new Vector2(-200, -30), new Color(0.8f, 0.95f, 1f));
            _txtMisTouch = MakeText(canvasGo.transform, "txtMisTouch",
                "误触: 0 / 2", 24, new Vector2(200, -30), new Color(1f, 0.9f, 0.7f));
            MakeText(canvasGo.transform, "txtGoal",
                "目标: 80% 清理率 | 误触 ≤ 2", 18,
                new Vector2(550, -30), new Color(0.7f, 0.7f, 0.8f, 0.8f));

            // 底部提示栏
            var tipGo = new GameObject("TipBar");
            tipGo.transform.SetParent(canvasGo.transform, false);
            var tipRt = tipGo.AddComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0, 0);
            tipRt.anchorMax = new Vector2(1, 0);
            tipRt.pivot = new Vector2(0.5f, 0);
            tipRt.anchoredPosition = Vector2.zero;
            tipRt.sizeDelta = new Vector2(0, 40);
            var tipImg = tipGo.AddComponent<Image>();
            tipImg.color = new Color(0.05f, 0.05f, 0.15f, 0.75f);

            var tipText = tipGo.AddComponent<Text>();
            tipText.text = "拖动污染物(瓶/袋/油污)到右侧回收箱 | 请勿触碰水生生物(鱼/芦苇/水草)";
            tipText.font = UIBuilder.DefaultFont;
            tipText.fontSize = 18;
            tipText.alignment = TextAnchor.MiddleCenter;
            tipText.color = new Color(0.8f, 0.8f, 0.9f, 0.9f);
            tipText.raycastTarget = false;

            Debug.Log("[水净化] HUD 构建完成");
        }

        private Text MakeText(Transform parent, string name, string content,
            int fontSize, Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(400, 40);

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = UIBuilder.DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.raycastTarget = false;
            return txt;
        }

        private void UpdateHUD()
        {
            int sec = Mathf.CeilToInt(_game.RemainingTime);
            if (sec < 0) sec = 0;
            string timeStr = $"{sec / 60:D2}:{sec % 60:D2}";
            _txtTimer.text = timeStr;
            _txtTimer.color = sec > 20 ? Color.white :
                (sec > 10 ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.3f, 0.3f));

            float ratio = _game.TotalPollutants > 0 ?
                (float)_game.CleanedPollutants / _game.TotalPollutants * 100f : 0f;
            _txtProgress.text = $"已清理: {_game.CleanedPollutants} / {_game.TotalPollutants} ({ratio:F0}%)";

            _txtMisTouch.text = $"误触: {_game.MisTouchCount} / 2";
            _txtMisTouch.color = _game.MisTouchCount <= 2 ?
                new Color(1f, 0.9f, 0.7f) : new Color(1f, 0.3f, 0.3f);
        }

        // ================================================================
        //  结算面板
        // ================================================================

        private void BuildResultPanel()
        {
            _resultPanel = new GameObject("ResultPanel");
            _resultPanel.transform.SetParent(_hudCanvas.transform, false);
            var rpRt = _resultPanel.AddComponent<RectTransform>();
            rpRt.anchorMin = Vector2.zero;
            rpRt.anchorMax = Vector2.one;
            rpRt.offsetMin = Vector2.zero;
            rpRt.offsetMax = Vector2.zero;
            var rpImg = _resultPanel.AddComponent<Image>();
            rpImg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);

            var dialogGo = new GameObject("Dialog");
            dialogGo.transform.SetParent(_resultPanel.transform, false);
            var dRt = dialogGo.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = Vector2.zero;
            dRt.sizeDelta = new Vector2(600, 350);
            var dImg = dialogGo.AddComponent<Image>();
            dImg.color = new Color(0.08f, 0.08f, 0.2f, 0.95f);

            _txtResultTitle = CreateResultText(dialogGo.transform, "Title",
                "", 40, new Vector2(0, 100), Color.white);
            _txtResultDetail = CreateResultText(dialogGo.transform, "Detail",
                "", 24, new Vector2(0, 20), Color.white);
            CreateResultText(dialogGo.transform, "Hint",
                "点击任意位置重新挑战", 20, new Vector2(0, -110),
                new Color(0.6f, 0.6f, 0.7f));

            _resultPanel.SetActive(false);
        }

        private Text CreateResultText(Transform parent, string name, string content,
            int fontSize, Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(500, 50);

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = UIBuilder.DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.raycastTarget = false;
            return txt;
        }

        private void ShowResult(bool passed)
        {
            _resultPanel.SetActive(true);

            if (passed)
            {
                _txtResultTitle.text = "挑战成功！";
                _txtResultTitle.color = new Color(0.3f, 1f, 0.5f);
                float ratio = _game.TotalPollutants > 0 ?
                    (float)_game.CleanedPollutants / _game.TotalPollutants * 100f : 0f;
                _txtResultDetail.text = $"清理率: {ratio:F0}%  |  误触: {_game.MisTouchCount} / 2\n淮河生态治理，你贡献了一份力量！";
                _txtResultDetail.color = new Color(0.7f, 0.9f, 0.7f);
            }
            else
            {
                _txtResultTitle.text = "挑战失败";
                _txtResultTitle.color = new Color(1f, 0.3f, 0.3f);
                if (_game.MisTouchCount > 2)
                    _txtResultDetail.text = "原因: 误触水生生物超过 2 次";
                else
                {
                    float ratio = _game.TotalPollutants > 0 ?
                        (float)_game.CleanedPollutants / _game.TotalPollutants * 100f : 0f;
                    _txtResultDetail.text = $"原因: 清理率 {ratio:F0}% 未达 80%";
                }
                _txtResultDetail.color = new Color(1f, 0.8f, 0.8f);
            }

            var btn = _resultPanel.AddComponent<Button>();
            var bg = _resultPanel.GetComponent<Image>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(OnRetryClicked);
        }

        private void OnRetryClicked()
        {
            foreach (var e in _entities)
                if (e.Go != null) Destroy(e.Go);
            _entities.Clear();

            _resultPanel.SetActive(false);
            _game.RetryChallenge();

            _nextEntityId = 0;
            _pollutantSpawnCount = 0;
            _aquaticSpawnCount = 0;
            _spawnTimer = SPAWN_INTERVAL;
            for (int i = 0; i < INITIAL_POLLUTANTS; i++) SpawnEntity(true);
            for (int i = 0; i < INITIAL_AQUATIC; i++) SpawnEntity(false);
        }

        private void OnComplete(MiniGameCompleteEvent evt)
        {
            if (evt.MiniGameId != "water_purification") return;
            _initialized = false;
            Debug.Log("[水净化] 通关成功！");
        }

        private void OnFailed(MiniGameFailedEvent evt)
        {
            if (evt.MiniGameId != "water_purification") return;
            _initialized = false;
            ShowResult(false);
            Debug.Log("[水净化] 挑战失败");
        }
    }
}
