using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Core;

namespace Presentation.MiniGame
{
    /// <summary>
    /// 水质净化小游戏场景管理器 —— 纯代码构建"大鱼吃小鱼"风格水下场景。
    /// 遵循项目 programmatic UI 模式，无 Prefab、无 Scene 文件。
    /// </summary>
    public class WaterPurificationScene : MonoBehaviour
    {
        // ─── 常量 ──────────────────────────────────────────
        private const float WATER_SURFACE_Y = 4.2f;  // 水面世界坐标 Y
        private const float SEABED_Y = -4.5f;        // 水底世界坐标 Y
        private const float BIN_WORLD_X = 5.5f;      // 回收箱中心 X
        private const float BIN_WORLD_Y = 0f;        // 回收箱中心 Y
        private const float SPAWN_INTERVAL = 3f;     // 追加生成间隔
        private const int INITIAL_POLLUTANTS = 6;
        private const int INITIAL_AQUATIC = 4;
        private const int TARGET_POLLUTANTS = 15;

        // ─── 组件引用 ──────────────────────────────────────
        private WaterPurificationMiniGame _game;
        private Camera _cam;

        // ─── 场景对象 ──────────────────────────────────────
        private Transform _entityLayer;
        private Transform _decorLayer;
        private SpriteRenderer _binSr;
        private Rect _binWorldRect;

        // ─── HUD ───────────────────────────────────────────
        private Canvas _hudCanvas;
        private Text _txtTimer;
        private Text _txtProgress;
        private Text _txtMisTouch;

        // ─── 结算面板 ──────────────────────────────────────
        private GameObject _resultPanel;
        private Text _txtResultTitle;
        private Text _txtResultDetail;
        private Button _btnRetry;

        // ─── 实体管理 ──────────────────────────────────────
        private struct EntityData
        {
            public int Id;
            public bool IsPollutant;
            public GameObject Go;
        }
        private List<EntityData> _entities = new List<EntityData>();
        private int _nextEntityId;
        private int _spawnedPollutants;
        private float _spawnTimer;
        private bool _initialized;

        // ─── 清理标记 ──────────────────────────────────────
        private bool _cleaningUp;

        // ================================================================
        //  生命周期
        // ================================================================

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

            // 确保相机有 Physics2DRaycaster（拖拽需要）
            if (_cam.GetComponent<Physics2DRaycaster>() == null)
                _cam.gameObject.AddComponent<Physics2DRaycaster>();

            // 确保有 EventSystem
            UIBuilder.EnsureEventSystem();

            // 创建逻辑组件
            var logicGo = new GameObject("GameLogic");
            logicGo.transform.SetParent(transform, false);
            _game = logicGo.AddComponent<WaterPurificationMiniGame>();

            // 监听游戏结束
            EventBus.Subscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Subscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void Start()
        {
            BuildScene();
            _initialized = true;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void Update()
        {
            if (!_initialized || _cleaningUp) return;
            if (_game.RunState != MiniGameRunState.Running) return;

            // 追加生成
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f && _spawnedPollutants < TARGET_POLLUTANTS)
            {
                SpawnEntity(true);
                if (Random.Range(0, 3) == 0) SpawnEntity(false);
                _spawnTimer = SPAWN_INTERVAL;
            }

            UpdateHUD();

            // 误触超限检查（由 OnEntityDropped 触发，此处兜底）
            if (_game.MisTouchCount > 2 && _game.RunState == MiniGameRunState.Running)
            {
                // FailGame is protected, use reflection or just let the Update in base handle it
                // Actually the base Update handles time; mis-touch is checked in WaterPurificationMiniGame.Update
            }
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
            // 水面背景
            var bgGo = new GameObject("WaterBg");
            bgGo.transform.SetParent(transform, false);
            var bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = MiniGameSprites.CreateWaterBg(1280, 720);
            bgSr.sortingOrder = -10;
            bgGo.transform.localScale = new Vector3(0.01f, 0.01f, 1f); // 100px per unit

            // 海底沙地
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
            _decorLayer = new GameObject("Decorations").transform;
            _decorLayer.SetParent(transform, false);

            // 水面动效
            var waterFx = gameObject.AddComponent<WaterEffect>();
            waterFx.Setup(WATER_SURFACE_Y, SEABED_Y);

            // 底部水草和芦苇
            Sprite grassSprite = MiniGameSprites.CreateGrass(40, 80);
            Sprite reedSprite = MiniGameSprites.CreateReed(20, 120);

            for (int i = 0; i < 8; i++)
            {
                float x = -5.5f + i * 1.4f + Random.Range(-0.3f, 0.3f);
                bool isReed = Random.Range(0, 3) == 0;

                var go = new GameObject(isReed ? $"Reed_{i}" : $"Grass_{i}");
                go.transform.SetParent(_decorLayer, false);
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

            _binSr = binGo.AddComponent<SpriteRenderer>();
            _binSr.sprite = MiniGameSprites.CreateBin(80, 120);
            _binSr.sortingOrder = 50;

            // 回收箱碰撞区域（世界坐标）
            float binHalfW = 0.8f, binHalfH = 1.2f;
            _binWorldRect = new Rect(
                BIN_WORLD_X - binHalfW, BIN_WORLD_Y - binHalfH,
                binHalfW * 2, binHalfH * 2);
        }

        private void BuildEntities()
        {
            _entityLayer = new GameObject("Entities").transform;
            _entityLayer.SetParent(transform, false);

            // 初始生成
            for (int i = 0; i < INITIAL_POLLUTANTS; i++) SpawnEntity(true);
            for (int i = 0; i < INITIAL_AQUATIC; i++) SpawnEntity(false);
        }

        // ================================================================
        //  实体生成
        // ================================================================

        private void SpawnEntity(bool isPollutant)
        {
            int id = _nextEntityId++;
            _game.RegisterPollutantSpawned();

            Sprite sprite;
            string name;
            float scale = 1f;

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

            // 随机位置（回收箱左侧区域）
            float x = Random.Range(-5.5f, 4.5f);
            float y = Random.Range(SEABED_Y + 1f, WATER_SURFACE_Y - 0.5f);
            go.transform.localPosition = new Vector3(x, y, 0);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20 + id;

            // 鱼添加游泳动画
            if (name == "Fish")
            {
                var anim = go.AddComponent<AquaticAnimator>();
                anim.Type = AquaticAnimator.AnimType.Fish;
                anim.Speed = Random.Range(0.2f, 0.45f);
                anim.SwimRange = Random.Range(30f, 55f);
            }

            // 添加拖拽组件（污染物和水生生物都可拖拽）
            var drag = go.AddComponent<DragHandler>();
            drag.EntityId = id;
            drag.BinRect = _binWorldRect;
            drag.OnDragEnded = OnEntityDropped;

            // 添加碰撞体用于拖拽检测
            var col = go.AddComponent<BoxCollider2D>();
            var bounds = sr.sprite.bounds;
            col.size = new Vector2(bounds.size.x, bounds.size.y);

            // 刚体（Kinematic，不参与物理，仅用于 EventSystem 射线检测）
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            _entities.Add(new EntityData { Id = id, IsPollutant = isPollutant, Go = go });
        }

        // ================================================================
        //  拖拽回调
        // ================================================================

        private void OnEntityDropped(int entityId, bool inBin, Vector2 dropPos)
        {
            // 找到对应实体
            for (int i = 0; i < _entities.Count; i++)
            {
                if (_entities[i].Id != entityId) continue;
                var data = _entities[i];

                if (inBin)
                {
                    // 投入回收箱
                    _game.OnEntityDropped(data.IsPollutant);

                    // 播放消失效果（简单缩放消失）
                    StartCoroutine(ShrinkAndDestroy(data.Go));

                    // 从列表移除
                    _entities.RemoveAt(i);
                }
                else
                {
                    // 未投入回收箱，弹回原位（不做任何事，拖拽已移动到新位置）
                    // 实际上应该弹回，但为了简化，让它留在新位置
                }
                return;
            }
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

        // ================================================================
        //  HUD
        // ================================================================

        private void BuildHUD()
        {
            var canvasGo = new GameObject("MiniGameHUD");
            canvasGo.transform.SetParent(transform, false);
            _hudCanvas = canvasGo.AddComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // 顶部栏背景
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

            // 倒计时
            _txtTimer = CreateHUDText(canvasGo.transform, "txtTimer",
                "01:30", 28, new Vector2(-600, -30), new Color(1f, 1f, 1f, 1f));

            // 进度
            _txtProgress = CreateHUDText(canvasGo.transform, "txtProgress",
                "已清理: 0 / 0 (0%)", 22, new Vector2(-200, -30), new Color(0.8f, 0.95f, 1f, 1f));

            // 误触
            _txtMisTouch = CreateHUDText(canvasGo.transform, "txtMisTouch",
                "误触: 0 / 2", 22, new Vector2(200, -30), new Color(1f, 0.9f, 0.7f, 1f));

            // 目标提示
            CreateHUDText(canvasGo.transform, "txtGoal",
                "目标: 80% 清理率 | 误触 ≤ 2", 18,
                new Vector2(550, -30), new Color(0.7f, 0.7f, 0.8f, 0.8f));

            // 底部提示
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
            tipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tipText.fontSize = 18;
            tipText.alignment = TextAnchor.MiddleCenter;
            tipText.color = new Color(0.8f, 0.8f, 0.9f, 0.9f);
            tipText.raycastTarget = false;
        }

        private Text CreateHUDText(Transform parent, string name, string content,
            int fontSize, Vector2 anchoredPos, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(300, 40);

            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            // 遮罩
            _resultPanel = new GameObject("ResultPanel");
            _resultPanel.transform.SetParent(_hudCanvas.transform, false);
            var rpRt = _resultPanel.AddComponent<RectTransform>();
            rpRt.anchorMin = Vector2.zero;
            rpRt.anchorMax = Vector2.one;
            rpRt.offsetMin = Vector2.zero;
            rpRt.offsetMax = Vector2.zero;
            var rpImg = _resultPanel.AddComponent<Image>();
            rpImg.color = new Color(0.02f, 0.02f, 0.08f, 0.85f);

            // 对话框背景
            var dialogGo = new GameObject("Dialog");
            dialogGo.transform.SetParent(_resultPanel.transform, false);
            var dRt = dialogGo.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f);
            dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = Vector2.zero;
            dRt.sizeDelta = new Vector2(600, 350);
            var dImg = dialogGo.AddComponent<Image>();
            dImg.color = new Color(0.08f, 0.08f, 0.2f, 0.95f);

            // 标题
            _txtResultTitle = CreateResultText(dialogGo.transform, "Title",
                "", 40, new Vector2(0, 100), Color.white);

            // 详情
            _txtResultDetail = CreateResultText(dialogGo.transform, "Detail",
                "", 24, new Vector2(0, 20), Color.white);

            // 提示
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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                _txtResultTitle.text = "[OK] 挑战成功！";
                _txtResultTitle.color = new Color(0.3f, 1f, 0.5f);
                float ratio = _game.TotalPollutants > 0 ?
                    (float)_game.CleanedPollutants / _game.TotalPollutants * 100f : 0f;
                _txtResultDetail.text = $"清理率: {ratio:F0}%  |  误触: {_game.MisTouchCount} / 2\n淮河生态治理，你贡献了一份力量！";
                _txtResultDetail.color = new Color(0.7f, 0.9f, 0.7f);
            }
            else
            {
                _txtResultTitle.text = "[X] 挑战失败";
                _txtResultTitle.color = new Color(1f, 0.3f, 0.3f);
                if (_game.MisTouchCount > 2)
                    _txtResultDetail.text = $"原因: 误触水生生物超过 2 次";
                else
                {
                    float ratio = _game.TotalPollutants > 0 ?
                        (float)_game.CleanedPollutants / _game.TotalPollutants * 100f : 0f;
                    _txtResultDetail.text = $"原因: 清理率 {ratio:F0}% 未达 80%";
                }
                _txtResultDetail.color = new Color(1f, 0.8f, 0.8f);
            }

            // 添加点击重试
            var btn = _resultPanel.AddComponent<Button>();
            var bg = _resultPanel.GetComponent<Image>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(OnRetryClicked);
        }

        private void OnRetryClicked()
        {
            // 清理所有实体
            foreach (var e in _entities)
                if (e.Go != null) Destroy(e.Go);
            _entities.Clear();

            _resultPanel.SetActive(false);

            // 重置逻辑
            _game.RetryChallenge();

            // 重新生成
            _nextEntityId = 0;
            _spawnedPollutants = 0;
            _spawnTimer = SPAWN_INTERVAL;
            for (int i = 0; i < INITIAL_POLLUTANTS; i++) SpawnEntity(true);
            for (int i = 0; i < INITIAL_AQUATIC; i++) SpawnEntity(false);
        }

        // ================================================================
        //  游戏结束回调
        // ================================================================

        private void OnComplete(MiniGameCompleteEvent evt)
        {
            if (evt.MiniGameId != "water_purification") return;
            _initialized = false;
            // 成功时由 Chapter1Handler 的 PostMiniGameDialogue 处理反馈，
            // 此处不显示结果面板，由 MiniGameLoader 销毁场景
        }

        private void OnFailed(MiniGameFailedEvent evt)
        {
            if (evt.MiniGameId != "water_purification") return;
            _initialized = false;
            ShowResult(false);
        }
    }
}
