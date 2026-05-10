using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Logic;

namespace Presentation.MiniGame
{
    public class InnovationMatchingScene : MonoBehaviour
    {
        private struct MatchGroup
        {
            public string city, industry, resource;
        }

        private struct CardUI
        {
            public Button btn;
            public Text txt;
            public Image bg;
            public Image basePlate;
            public int group;
            public int col;
            public bool matched;
        }

        private static readonly MatchGroup[] Groups = {
            new MatchGroup { city = "合肥",   industry = "量子信息",     resource = "量子科学实验室" },
            new MatchGroup { city = "芜湖",   industry = "新能源汽车",   resource = "新能源汽车研发中心" },
            new MatchGroup { city = "蚌埠",   industry = "玻璃新材料",   resource = "浮法玻璃国家实验室" },
            new MatchGroup { city = "马鞍山", industry = "智能装备制造", resource = "智能制造研究院" },
            new MatchGroup { city = "铜陵",   industry = "铜基新材料",   resource = "铜产业技术中心" },
            new MatchGroup { city = "安庆",   industry = "化工新材料",   resource = "石化产业研究院" },
            new MatchGroup { city = "滁州",   industry = "光伏产业",     resource = "光伏技术创新中心" },
            new MatchGroup { city = "阜阳",   industry = "现代农业",     resource = "现代农业科技园" },
        };

        private const string MINI_GAME_ID = "innovation_matching";

        private InnovationMatchingMiniGame _miniGame;
        private List<CardUI>[] _columns = { new List<CardUI>(), new List<CardUI>(), new List<CardUI>() };
        private int[] _selIdx = { -1, -1, -1 };

        private Text _txtTimer;
        private GameObject _resultPanel;
        private Text _txtResultTitle, _txtResultDetail;
        private Button _btnRetry;
        private GameObject _timeoutPanel;

        private static readonly Color ColCard = new Color(0.12f, 0.16f, 0.28f);
        private static readonly Color ColSelected = new Color(0.16f, 0.4f, 0.78f);
        private static readonly Color ColMatched = new Color(0.08f, 0.32f, 0.12f);
        private static readonly Color ColError = new Color(0.7f, 0.12f, 0.12f);
        private static readonly Color ColBaseDefault = new Color(0.22f, 0.22f, 0.24f);
        private static readonly Color ColBaseCorrect = new Color(0.1f, 0.55f, 0.15f);
        private static readonly Color ColBaseWrong = new Color(0.65f, 0.08f, 0.08f);
        private static readonly Color[] ColHeader = {
            new Color(0.4f, 0.63f, 1f),
            new Color(1f, 0.78f, 0.31f),
            new Color(0.31f, 0.86f, 0.63f)
        };

        private void Start()
        {
            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.06f, 0.08f, 0.16f);

            // 关键修复：设置 miniGameId，确保事件 ID 匹配
            _miniGame = gameObject.AddComponent<InnovationMatchingMiniGame>();
            SetMiniGameId(_miniGame, MINI_GAME_ID);

            BuildScene();
            UIBuilder.EnsureEventSystem();

            EventBus.Subscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Subscribe<MiniGameFailedEvent>(OnFailed);

            Debug.Log($"[MatchScene] Start complete, miniGameId={MINI_GAME_ID}");
        }

        private static void SetMiniGameId(InnovationMatchingMiniGame mg, string id)
        {
            var field = typeof(MiniGameBase).GetField("miniGameId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(mg, id);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnComplete);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnFailed);
        }

        private void BuildScene()
        {
            var canvasGo = new GameObject("MatchCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // 列标题
            string[] headers = { "城  市", "核心产业", "科创资源" };
            float[] colX = { -600f, 0f, 600f };
            for (int c = 0; c < 3; c++)
            {
                UIBuilder.CreateText(root, $"hdr{c}", headers[c], 28,
                    TextAnchor.MiddleCenter, ColHeader[c], new Vector2(colX[c], 420));
            }

            // 卡片
            int[] order0 = Shuffle(8);
            int[] order1 = Shuffle(8);
            int[] order2 = Shuffle(8);

            for (int c = 0; c < 3; c++)
            {
                int[] order = c == 0 ? order0 : (c == 1 ? order1 : order2);
                for (int i = 0; i < 8; i++)
                {
                    int gid = order[i];
                    string text = c == 0 ? Groups[gid].city :
                                  c == 1 ? Groups[gid].industry : Groups[gid].resource;

                    float y = 340f - i * 90f;

                    // 深灰底板
                    var baseGo = new GameObject($"Base{c}_{i}");
                    baseGo.transform.SetParent(root, false);
                    var baseRt = baseGo.AddComponent<RectTransform>();
                    baseRt.anchoredPosition = new Vector2(colX[c], y);
                    baseRt.sizeDelta = new Vector2(296, 68);
                    var baseImg = baseGo.AddComponent<Image>();
                    baseImg.color = ColBaseDefault;
                    baseImg.raycastTarget = false;

                    // 卡片按钮
                    var btn = UIBuilder.CreateTextButton(root, $"C{c}_{i}", text, 22,
                        new Vector2(colX[c], y), new Vector2(280, 60));

                    int col = c, idx = i, group = gid;
                    btn.onClick.AddListener(() => OnCardClicked(col, idx));

                    var bg = btn.GetComponent<Image>();

                    _columns[c].Add(new CardUI
                    {
                        btn = btn,
                        txt = btn.GetComponentInChildren<Text>(),
                        bg = bg,
                        basePlate = baseImg,
                        group = group,
                        col = col,
                        matched = false
                    });
                }
            }

            // HUD — 仅计时器
            _txtTimer = UIBuilder.CreateText(root, "txtTimer", "", 32,
                TextAnchor.MiddleLeft, Color.white, new Vector2(-800, 480));
            UIBuilder.CreateText(root, "txtTip", "依次匹配！", 22,
                TextAnchor.MiddleCenter, new Color(0.92f, 0.78f, 0.42f), new Vector2(0, -500));

            // 结算面板
            BuildResultPanel(root);

            // 超时弹窗
            BuildTimeoutPanel(root);
        }

        private void BuildResultPanel(Transform parent)
        {
            _resultPanel = new GameObject("ResultPanel");
            _resultPanel.transform.SetParent(parent, false);
            var rt = _resultPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _resultPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

            var root = _resultPanel.transform;
            _txtResultTitle = UIBuilder.CreateText(root, "txtTitle", "", 48,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, 150));
            _txtResultDetail = UIBuilder.CreateText(root, "txtDetail", "", 28,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, 50));
            _btnRetry = UIBuilder.CreateTextButton(root, "btnRetry", "重新挑战", 32,
                new Vector2(0, -80), new Vector2(300, 70));
            _btnRetry.onClick.AddListener(OnRetry);

            _resultPanel.SetActive(false);
        }

        private void BuildTimeoutPanel(Transform parent)
        {
            _timeoutPanel = new GameObject("TimeoutPanel");
            _timeoutPanel.transform.SetParent(parent, false);
            var rt = _timeoutPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _timeoutPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);

            var root = _timeoutPanel.transform;
            UIBuilder.CreateText(root, "txtTimeout", "游戏时间到", 52,
                TextAnchor.MiddleCenter, new Color(0.92f, 0.78f, 0.42f), new Vector2(0, 80));
            UIBuilder.CreateText(root, "txtRetryHint", "请重新尝试", 36,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, 0));
            UIBuilder.CreateText(root, "txtClickHint", "点击任意处重试", 22,
                TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f), new Vector2(0, -60));

            // 全屏点击区域 → 重试
            var btnArea = new GameObject("BtnRetryArea");
            btnArea.transform.SetParent(root, false);
            var btnRt = btnArea.AddComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero; btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;
            var btnImg = btnArea.AddComponent<Image>();
            btnImg.color = new Color(0, 0, 0, 0.01f);
            var btn = btnArea.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(OnTimeoutRetry);

            _timeoutPanel.SetActive(false);
        }

        private void Update()
        {
            if (_miniGame == null) return;
            if (_miniGame.RunState != MiniGameRunState.Running) return;

            int sec = Mathf.CeilToInt(_miniGame.RemainingTime);
            _txtTimer.text = $"TIME {sec / 60}:{sec % 60:D2}";
            _txtTimer.color = sec > 20 ? Color.white : (sec > 10 ? Color.yellow : Color.red);
        }

        private void OnCardClicked(int col, int idx)
        {
            var card = _columns[col][idx];
            if (card.matched) return;
            if (_miniGame == null || _miniGame.RunState != MiniGameRunState.Running) return;

            // 取消同列之前选中
            if (_selIdx[col] >= 0)
            {
                var prev = _columns[col][_selIdx[col]];
                if (!prev.matched)
                {
                    prev.bg.color = ColCard;
                    prev.basePlate.color = ColBaseDefault;
                }
            }

            _columns[col][idx].bg.color = ColSelected;
            _selIdx[col] = idx;

            // 检查三列是否都选了
            if (_selIdx[0] < 0 || _selIdx[1] < 0 || _selIdx[2] < 0) return;

            int g0 = _columns[0][_selIdx[0]].group;
            int g1 = _columns[1][_selIdx[1]].group;
            int g2 = _columns[2][_selIdx[2]].group;

            if (g0 == g1 && g1 == g2)
            {
                // 正确 → 绿色底板 + 黄色连接线
                for (int c = 0; c < 3; c++)
                {
                    var ci = _columns[c][_selIdx[c]];
                    ci.bg.color = ColMatched;
                    ci.basePlate.color = ColBaseCorrect;
                    ci.matched = true;
                    ci.btn.interactable = false;
                    _columns[c][_selIdx[c]] = ci;
                }
                DrawConnectingLine();
                _miniGame.RegisterCorrectMatch();
                _selIdx[0] = _selIdx[1] = _selIdx[2] = -1;

                // 检测所有底板是否全部变绿
                if (AllMatched())
                {
                    _miniGame.CompleteGame(100);
                }
            }
            else
            {
                // 错误 → 深红底板 + 黄色连接线
                _miniGame.RegisterWrongMatch();
                for (int c = 0; c < 3; c++)
                {
                    if (_selIdx[c] >= 0)
                    {
                        _columns[c][_selIdx[c]].bg.color = ColError;
                        _columns[c][_selIdx[c]].basePlate.color = ColBaseWrong;
                    }
                }
                DrawConnectingLine();
                int sel0 = _selIdx[0], sel1 = _selIdx[1], sel2 = _selIdx[2];
                _selIdx[0] = _selIdx[1] = _selIdx[2] = -1;

                StartCoroutine(ErrorFlash(sel0, sel1, sel2));
            }
        }

        private IEnumerator ErrorFlash(int i0, int i1, int i2)
        {
            yield return new WaitForSeconds(0.5f);
            int[] cols = { i0, i1, i2 };
            for (int c = 0; c < 3; c++)
            {
                if (cols[c] >= 0 && cols[c] < _columns[c].Count)
                {
                    var ci = _columns[c][cols[c]];
                    if (!ci.matched)
                    {
                        ci.bg.color = ColCard;
                        ci.basePlate.color = ColBaseDefault;
                    }
                }
            }
        }

        private bool AllMatched()
        {
            for (int c = 0; c < 3; c++)
                for (int i = 0; i < _columns[c].Count; i++)
                    if (!_columns[c][i].matched)
                        return false;
            return true;
        }

        private void DrawConnectingLine()
        {
            Vector3[] positions = new Vector3[3];
            for (int c = 0; c < 3; c++)
            {
                var rt = _columns[c][_selIdx[c]].btn.GetComponent<RectTransform>();
                positions[c] = rt.position;
            }

            var lineGo = new GameObject("ConnectingLine");
            lineGo.transform.SetParent(transform, false);
            var lr = lineGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 3;
            lr.SetPositions(positions);
            lr.startWidth = 6f;
            lr.endWidth = 6f;
            lr.startColor = new Color(1f, 0.85f, 0.1f);
            lr.endColor = new Color(1f, 0.85f, 0.1f);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 100;

            StartCoroutine(AnimateLine(lr, lineGo, positions));
        }

        private IEnumerator AnimateLine(LineRenderer lr, GameObject lineGo, Vector3[] positions)
        {
            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (t < 0.5f)
                {
                    lr.positionCount = 2;
                    lr.SetPosition(0, positions[0]);
                    lr.SetPosition(1, Vector3.Lerp(positions[0], positions[1], t * 2f));
                }
                else
                {
                    lr.positionCount = 3;
                    lr.SetPosition(0, positions[0]);
                    lr.SetPosition(1, positions[1]);
                    lr.SetPosition(2, Vector3.Lerp(positions[1], positions[2], (t - 0.5f) * 2f));
                }
                yield return null;
            }

            lr.positionCount = 3;
            lr.SetPositions(positions);

            // 淡出
            float fade = 0f;
            Color startCol = new Color(1f, 0.85f, 0.1f);
            while (fade < 0.3f)
            {
                fade += Time.deltaTime;
                float a = 1f - fade / 0.3f;
                Color c = new Color(startCol.r, startCol.g, startCol.b, a);
                lr.startColor = c;
                lr.endColor = c;
                yield return null;
            }

            if (lineGo != null) Destroy(lineGo);
        }

        // ===== 超时重试（点击任意处） =====
        private void OnTimeoutRetry()
        {
            Debug.Log("[MatchScene] Timeout retry triggered");
            _timeoutPanel.SetActive(false);
            DoRetry();
        }

        // ===== 结算面板重试 =====
        private void OnRetry()
        {
            Debug.Log("[MatchScene] Result retry triggered");
            _resultPanel.SetActive(false);
            DoRetry();
        }

        // ===== 统一重试逻辑 =====
        private void DoRetry()
        {
            // 清理旧卡片和底板
            foreach (var col in _columns)
            {
                foreach (var c in col)
                {
                    if (c.btn != null) Destroy(c.btn.gameObject);
                    if (c.basePlate != null) Destroy(c.basePlate.gameObject);
                }
                col.Clear();
            }
            _selIdx[0] = _selIdx[1] = _selIdx[2] = -1;

            // 清理残留连线
            foreach (var lr in GetComponentsInChildren<LineRenderer>())
                Destroy(lr.gameObject);

            // 重置小游戏状态
            _miniGame.RetryChallenge();

            // 重建卡片
            var canvasGo = GameObject.Find("MatchCanvas");
            if (canvasGo != null) RebuildCards(canvasGo.transform);
        }

        private void RebuildCards(Transform root)
        {
            float[] colX = { -600f, 0f, 600f };
            int[] order0 = Shuffle(8);
            int[] order1 = Shuffle(8);
            int[] order2 = Shuffle(8);

            for (int c = 0; c < 3; c++)
            {
                int[] order = c == 0 ? order0 : (c == 1 ? order1 : order2);
                for (int i = 0; i < 8; i++)
                {
                    int gid = order[i];
                    string text = c == 0 ? Groups[gid].city :
                                  c == 1 ? Groups[gid].industry : Groups[gid].resource;
                    float y = 340f - i * 90f;

                    // 深灰底板
                    var baseGo = new GameObject($"Base{c}_{i}_r");
                    baseGo.transform.SetParent(root, false);
                    var baseRt = baseGo.AddComponent<RectTransform>();
                    baseRt.anchoredPosition = new Vector2(colX[c], y);
                    baseRt.sizeDelta = new Vector2(296, 68);
                    var baseImg = baseGo.AddComponent<Image>();
                    baseImg.color = ColBaseDefault;
                    baseImg.raycastTarget = false;

                    var btn = UIBuilder.CreateTextButton(root, $"C{c}_{i}_r", text, 22,
                        new Vector2(colX[c], y), new Vector2(280, 60));
                    int col = c, idx = i;
                    btn.onClick.AddListener(() => OnCardClicked(col, idx));
                    var bg = btn.GetComponent<Image>();
                    _columns[c].Add(new CardUI
                    {
                        btn = btn, txt = btn.GetComponentInChildren<Text>(),
                        bg = bg, basePlate = baseImg, group = gid, col = c, matched = false
                    });
                }
            }
        }

        private int[] Shuffle(int count)
        {
            var arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = arr[i]; arr[i] = arr[j]; arr[j] = tmp;
            }
            return arr;
        }

        // ===== 事件回调 =====
        private void OnComplete(MiniGameCompleteEvent evt)
        {
            if (evt.MiniGameId != MINI_GAME_ID) return;
            Debug.Log("[MatchScene] Game complete! Showing result.");
            ShowResult(true);
        }

        private void OnFailed(MiniGameFailedEvent evt)
        {
            if (evt.MiniGameId != MINI_GAME_ID) return;

            Debug.Log($"[MatchScene] Game failed! RemainingTime={_miniGame.RemainingTime}");

            // 时间耗尽 → 超时弹窗
            if (_miniGame != null && _miniGame.RemainingTime <= 0f)
            {
                Debug.Log("[MatchScene] Showing timeout panel");
                _timeoutPanel.SetActive(true);
                return;
            }

            ShowResult(false);
        }

        private void ShowResult(bool passed)
        {
            _resultPanel.SetActive(true);
            if (passed)
            {
                _txtResultTitle.text = "匹配完成！";
                _txtResultTitle.color = Color.green;
                _txtResultDetail.text = $"完成 {_miniGame.CurrentMatched} 组匹配 | 错误 {_miniGame.ErrorCount} 次";
            }
            else
            {
                _txtResultTitle.text = "匹配失败";
                _txtResultTitle.color = Color.red;
                _txtResultDetail.text = $"原因: 时间耗尽（完成 {_miniGame.CurrentMatched} / 8 组）";
            }
        }
    }
}
