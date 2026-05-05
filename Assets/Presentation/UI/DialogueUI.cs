using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Core;
using Presentation.MiniGame;

namespace Presentation.UI
{
    /// <summary>
    /// 可复用对话系统 UI —— 逐句展示、分支选择、跳过
    /// 使用 _typingTicket 版本号防止旧协程写入残留文字
    /// 点击屏幕任意位置继续对话，跳过按钮显示为 >>>
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("对话展示")]
        public Text txtDialogue;
        public Button btnSkip;

        [Header("分支选择")]
        public GameObject choicePanel;
        public Transform choiceContainer;

        [SerializeField] private float typingSpeed = 0.05f;

        public Action OnAdvance;
        public Action<int> OnChoiceSelected;

        private bool _isTyping;
        private int _typingTicket;
        private string _fullText;

        private StarBurstEffect _starBurst;
        private Camera _overlayCam;
        private bool _suspended; // 小游戏期间暂停响应点击

        private void Start()
        {
            Debug.Log("[DialogueUI] Start — creating star burst + overlay camera");
            // 创建星光爆裂粒子效果
            _starBurst = new GameObject("StarBurstEffect")
                .AddComponent<StarBurstEffect>();

            // 创建粒子专用覆盖相机（depth > 主相机，仅渲染粒子层）
            SetupOverlayCamera();

            if (btnSkip != null)
                btnSkip.onClick.AddListener(SkipTyping);
            if (choicePanel != null)
                choicePanel.SetActive(false);

            EventBus.Subscribe<MiniGameStartEvent>(OnMiniGameStart);
            EventBus.Subscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
            EventBus.Subscribe<MiniGameFailedEvent>(OnMiniGameFailed);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameStartEvent>(OnMiniGameStart);
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnMiniGameFailed);
            if (_overlayCam != null) Destroy(_overlayCam.gameObject);
        }

        public void ShowLine(string text)
        {
            Debug.Log($"[DialogueUI] ShowLine: {text.Substring(0, Mathf.Min(30, text.Length))}...");
            _fullText = text;
            HideAllUI();
            _isTyping = true;
            StartCoroutine(TypeText(text, _typingTicket));
        }

        public void ShowChoices(List<DialogueChoice> choices)
        {
            HideAllUI();
            if (choicePanel != null)
                choicePanel.SetActive(true);
            BuildChoiceButtons(choices);
        }

        public void HideAllUI()
        {
            _typingTicket++;
            _isTyping = false;
            if (txtDialogue != null) txtDialogue.text = "";
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        private IEnumerator TypeText(string text, int ticket)
        {
            yield return null;
            if (ticket != _typingTicket) yield break;

            if (txtDialogue != null) txtDialogue.text = "";
            if (btnSkip != null) btnSkip.gameObject.SetActive(true);

            foreach (char c in text)
            {
                if (ticket != _typingTicket) yield break;
                if (txtDialogue != null) txtDialogue.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            if (ticket != _typingTicket) yield break;
            _isTyping = false;
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
        }

        private void SkipTyping()
        {
            if (!_isTyping) return;
            _typingTicket++;
            _isTyping = false;
            if (txtDialogue != null) txtDialogue.text = _fullText;
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
        }

        private void Update()
        {
            // 覆盖相机跟随主相机
            if (_overlayCam != null && Camera.main != null)
            {
                _overlayCam.transform.position = Camera.main.transform.position;
                _overlayCam.transform.rotation = Camera.main.transform.rotation;
            }
        }

        /// <summary>全屏按钮回调 — 推进对话并触发粒子特效</summary>
        public void OnScreenClicked(Vector3 screenPosition)
        {
            if (_suspended) return;
            OnAdvance?.Invoke();
            if (_starBurst != null)
                _starBurst.Play(screenPosition);
        }

        private void SetupOverlayCamera()
        {
            var camGo = new GameObject("ParticleOverlayCamera");
            camGo.transform.SetParent(transform);
            _overlayCam = camGo.AddComponent<Camera>();
            _overlayCam.depth = 2;
            _overlayCam.clearFlags = CameraClearFlags.Depth;
            _overlayCam.cullingMask = 1 << 11; // 仅渲染第 11 层（Particles）
            _overlayCam.fieldOfView = 60f;
            _overlayCam.nearClipPlane = 0.3f;
            _overlayCam.farClipPlane = 1000f;

            // 同步主相机位置
            if (Camera.main != null)
            {
                _overlayCam.transform.position = Camera.main.transform.position;
                _overlayCam.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private void BuildChoiceButtons(List<DialogueChoice> choices)
        {
            if (choiceContainer == null) return;
            for (int i = choiceContainer.childCount - 1; i >= 0; i--)
                Destroy(choiceContainer.GetChild(i).gameObject);

            for (int i = 0; i < choices.Count; i++)
            {
                int idx = i;
                var btn = UIBuilder.CreateTextButton(choiceContainer, $"Choice_{i}",
                    choices[i].OptionText, fontSize: 26,
                    anchoredPos: Vector2.zero, sizeDelta: new Vector2(400, 55));
                btn.onClick.AddListener(() => OnChoiceSelected?.Invoke(idx));

                var layout = btn.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 55;
                layout.flexibleWidth = 1;
            }
        }

        private void OnMiniGameStart(MiniGameStartEvent evt)
        {
            _suspended = true;
            HideAllUI();
            if (_starBurst != null) _starBurst.gameObject.SetActive(false);
            if (_overlayCam != null) _overlayCam.gameObject.SetActive(false);
        }

        private void OnMiniGameComplete(MiniGameCompleteEvent evt)
        {
            _suspended = false;
            if (_starBurst != null) _starBurst.gameObject.SetActive(true);
            if (_overlayCam != null) _overlayCam.gameObject.SetActive(true);
        }

        private void OnMiniGameFailed(MiniGameFailedEvent evt)
        {
            _suspended = false;
            if (_starBurst != null) _starBurst.gameObject.SetActive(true);
            if (_overlayCam != null) _overlayCam.gameObject.SetActive(true);
        }
    }
}
