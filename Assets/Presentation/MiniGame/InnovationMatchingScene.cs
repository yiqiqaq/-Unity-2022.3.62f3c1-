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

        private InnovationMatchingMiniGame _miniGame;
        private List<CardUI>[] _columns = { new List<CardUI>(), new List<CardUI>(), new List<CardUI>() };
        private int[] _selIdx = { -1, -1, -1 };

        private Text _txtTimer, _txtMatched, _txtErrors;
        private GameObject _resultPanel;
        private Text _txtResultTitle, _txtResultDetail;
        private Button _btnRetry;

        private static readonly Color ColCard = new Color(0.12f, 0.16f, 0.28f);
        private static readonly Color ColSelected = new Color(0.16f, 0.4f, 0.78f);
        private static readonly Color ColMatched = new Color(0.08f, 0.32f, 0.12f);
        private static readonly Color ColError = new Color(0.7f, 0.12f, 0.12f);
        private static readonly Color[] ColHeader = {
            new Color(0.4f, 0.63f, 1f),
            new Color(1f, 0.78f, 0.31f),
            new Color(0.31f, 0.86f, 0.63f)
        };

        private void Start()
        {
            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.06f, 0.08f, 0.16f);

            _miniGame = gameObject.AddComponent<InnovationMatchingMiniGame>();
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
            var canvasGo = new GameObject("MatchCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // 列标题
            string[] headers = { "城  市", "核心产业", "科创资源" };
            float[] colX = { -600f, 0f, 600f };
            for (int c = 0; c < 3; c++)
            {
                var hdr = UIBuilder.CreateText(root, $"hdr{c}", headers[c], 28,
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
                        group = group,
                        col = col,
                        matched = false
                    });
                }
            }

            // HUD
            _txtTimer = UIBuilder.CreateText(root, "txtTimer", "", 32,
                TextAnchor.MiddleLeft, Color.white, new Vector2(-800, 480));
            _txtMatched = UIBuilder.CreateText(root, "txtMatched", "", 24,
                TextAnchor.MiddleLeft, Color.white, new Vector2(-400, 480));
            _txtErrors = UIBuilder.CreateText(root, "txtErrors", "", 22,
                TextAnchor.MiddleLeft, Color.red, new Vector2(-100, 480));
            UIBuilder.CreateText(root, "txtGoal", "目标: 完成8组正确匹配", 20,
                TextAnchor.MiddleLeft, new Color(0.6f, 0.7f, 0.85f), new Vector2(300, 480));
            UIBuilder.CreateText(root, "txtTip", "依次点击三列中的对应卡片进行匹配 | 错误匹配将扣除6秒", 18,
                TextAnchor.MiddleCenter, new Color(0.7f, 0.8f, 0.85f), new Vector2(0, -500));

            // 结算面板
            BuildResultPanel(root);
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

        private void Update()
        {
            if (_miniGame == null || _miniGame.RunState != MiniGameRunState.Running) return;
            int sec = Mathf.CeilToInt(_miniGame.RemainingTime);
            _txtTimer.text = $"TIME {sec / 60}:{sec % 60:D2}";
            _txtTimer.color = sec > 20 ? Color.white : (sec > 10 ? Color.yellow : Color.red);
            _txtMatched.text = $"已匹配: {_miniGame.CurrentMatched} / 8";
        }

        private void OnCardClicked(int col, int idx)
        {
            var card = _columns[col][idx];
            if (card.matched) return;

            // 取消同列之前选中
            if (_selIdx[col] >= 0)
            {
                var prev = _columns[col][_selIdx[col]];
                if (!prev.matched) prev.bg.color = ColCard;
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
                // 正确
                for (int c = 0; c < 3; c++)
                {
                    var ci = _columns[c][_selIdx[c]];
                    ci.bg.color = ColMatched;
                    ci.matched = true;
                    ci.btn.interactable = false;
                    _columns[c][_selIdx[c]] = ci;
                }
                _miniGame.RegisterCorrectMatch();
                _selIdx[0] = _selIdx[1] = _selIdx[2] = -1;
            }
            else
            {
                // 错误：闪红
                _miniGame.RegisterWrongMatch();
                for (int c = 0; c < 3; c++)
                {
                    if (_selIdx[c] >= 0)
                        _columns[c][_selIdx[c]].bg.color = ColError;
                }
                int sel0 = _selIdx[0], sel1 = _selIdx[1], sel2 = _selIdx[2];
                _selIdx[0] = _selIdx[1] = _selIdx[2] = -1;

                // 0.5 秒后恢复
                StartCoroutine(ErrorFlash(sel0, sel1, sel2));
            }
        }

        private System.Collections.IEnumerator ErrorFlash(int i0, int i1, int i2)
        {
            yield return new WaitForSeconds(0.5f);
            int[] cols = { i0, i1, i2 };
            for (int c = 0; c < 3; c++)
            {
                if (cols[c] >= 0 && cols[c] < _columns[c].Count)
                {
                    var ci = _columns[c][cols[c]];
                    if (!ci.matched) ci.bg.color = ColCard;
                }
            }
        }

        private void OnRetry()
        {
            // 重建卡片
            foreach (var col in _columns)
            {
                foreach (var c in col)
                    if (c.btn != null) Destroy(c.btn.gameObject);
                col.Clear();
            }
            _selIdx[0] = _selIdx[1] = _selIdx[2] = -1;
            _miniGame.RetryChallenge();

            // 重新生成卡片 UI
            var canvasGo = GameObject.Find("MatchCanvas");
            if (canvasGo != null) RebuildCards(canvasGo.transform);

            _resultPanel.SetActive(false);
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
                    var btn = UIBuilder.CreateTextButton(root, $"C{c}_{i}_r", text, 22,
                        new Vector2(colX[c], y), new Vector2(280, 60));
                    int col = c, idx = i;
                    btn.onClick.AddListener(() => OnCardClicked(col, idx));
                    var bg = btn.GetComponent<Image>();
                    _columns[c].Add(new CardUI
                    {
                        btn = btn, txt = btn.GetComponentInChildren<Text>(),
                        bg = bg, group = gid, col = c, matched = false
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

        private void OnComplete(MiniGameCompleteEvent evt)
        {
            if (evt.MiniGameId != "innovation_matching") return;
            ShowResult(true);
        }

        private void OnFailed(MiniGameFailedEvent evt)
        {
            if (evt.MiniGameId != "innovation_matching") return;
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
