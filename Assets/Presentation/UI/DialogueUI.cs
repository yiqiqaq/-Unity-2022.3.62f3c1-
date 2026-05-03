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
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("对话展示")]
        public Text txtDialogue;
        public Button btnAdvance;
        public Button btnSkip;

        [Header("分支选择")]
        public GameObject choicePanel;
        public Transform choiceContainer;

        [SerializeField] private float typingSpeed = 0.05f;

        public Action OnAdvance;
        public Action<int> OnChoiceSelected;

        private bool _isTyping;
        private Coroutine _typingCoroutine;

        private void Start()
        {
            if (btnAdvance != null)
            {
                btnAdvance.onClick.AddListener(() => OnAdvance?.Invoke());
                btnAdvance.gameObject.SetActive(false);
            }
            if (btnSkip != null)
                btnSkip.onClick.AddListener(SkipTyping);
            if (choicePanel != null)
                choicePanel.SetActive(false);

            EventBus.Subscribe<MiniGameStartEvent>(OnMiniGameStart);
            EventBus.Subscribe<MiniGameFailedEvent>(OnMiniGameFailed);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameStartEvent>(OnMiniGameStart);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnMiniGameFailed);
        }

        public void ShowLine(string text)
        {
            HideAllUI();
            if (_typingCoroutine != null)
                StopCoroutine(_typingCoroutine);
            _typingCoroutine = StartCoroutine(TypeText(text));
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
            _isTyping = false;
            if (txtDialogue != null) txtDialogue.text = "";
            if (btnAdvance != null) btnAdvance.gameObject.SetActive(false);
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        private IEnumerator TypeText(string text)
        {
            _isTyping = true;
            if (txtDialogue != null) txtDialogue.text = "";
            if (btnSkip != null) btnSkip.gameObject.SetActive(true);

            foreach (char c in text)
            {
                if (txtDialogue != null) txtDialogue.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            _isTyping = false;
            OnTypingFinished();
        }

        private void OnTypingFinished()
        {
            _isTyping = false;
            if (btnSkip != null) btnSkip.gameObject.SetActive(false);
            if (btnAdvance != null) btnAdvance.gameObject.SetActive(true);
        }

        private void SkipTyping()
        {
            if (!_isTyping) return;
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _isTyping = false;
            OnTypingFinished();
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

        private void OnMiniGameStart(MiniGameStartEvent evt) => HideAllUI();
        private void OnMiniGameFailed(MiniGameFailedEvent evt) { }
    }
}
