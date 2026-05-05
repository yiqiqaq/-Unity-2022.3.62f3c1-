using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Logic;
using Core;
using Presentation.MiniGame;
using Presentation.UI;

namespace Presentation.Chapter
{
    public class Chapter3Handler : MonoBehaviour
    {
        [SerializeField] private Text txtChapterTitle;
        [SerializeField] private Text txtProgress;

        private DialogueUI _dialogue;
        private StoryChapterConfig _config;
        private List<DialogueNode> _currentList;
        private int _currentIndex;
        private int _state;

        private void Start()
        {
            _config = StoryScenarioLibrary.BuildChapter3();
            _dialogue = FindObjectOfType<DialogueUI>();

            if (txtChapterTitle != null)
                txtChapterTitle.text = _config.ChapterName;
            UpdateProgress("科考进行中");

            EventBus.Subscribe<MiniGameCompleteEvent>(OnMiniGameComplete);

            StartDialogueSequence(_config.PreludeDialogues, state: 0);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
        }

        private void StartDialogueSequence(List<DialogueNode> list, int state)
        {
            _currentList = list;
            _currentIndex = 0;
            _state = state;
            ShowCurrentNode();
        }

        private void ShowCurrentNode()
        {
            if (_currentIndex >= _currentList.Count) { OnSequenceEnd(); return; }
            var node = _currentList[_currentIndex];
            if (node.Choices != null && node.Choices.Count > 0)
            {
                _dialogue.ShowChoices(node.Choices);
                _dialogue.OnChoiceSelected = OnBranchChoice;
            }
            else
            {
                _dialogue.ShowLine(node.Text);
                _dialogue.OnAdvance = OnAdvance;
            }
        }

        private void OnAdvance() { _currentIndex++; ShowCurrentNode(); }

        private void OnBranchChoice(int choiceIndex)
        {
            _dialogue.HideAllUI();
            var choice = _currentList[_currentIndex].Choices[choiceIndex];
            _currentIndex++;
            if (choice.ReactionDialogues != null && choice.ReactionDialogues.Count > 0)
            {
                _currentList = choice.ReactionDialogues;
                _currentIndex = 0;
            }
            ShowCurrentNode();
        }

        private void OnSequenceEnd()
        {
            _dialogue.HideAllUI();
            switch (_state)
            {
                case 0: StartDialogueSequence(_config.MiniGameGuideDialogues, 1); break;
                case 1: ShowStartChallengeButton(); break;
                case 3: ShowKnowledgeCards(); break;
                case 4: ShowTransitionDialogue(); break;
                case 5: CompleteChapter(); break;
            }
        }

        private void ShowStartChallengeButton()
        {
            _state = 2;
            UpdateProgress("等待开始挑战");
            var btn = UIBuilder.CreateTextButton(_dialogue.choicePanel.transform.parent,
                "btnStartChallenge", "开始挑战", 30, Vector2.zero, new Vector2(280, 65));
            btn.onClick.AddListener(() => { Destroy(btn.gameObject); StartMiniGame(); });
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private void StartMiniGame()
        {
            GameManager.Instance.SetState(GameState.MiniGamePlaying);
            EventBus.Trigger(new MiniGameStartEvent { MiniGameId = _config.MiniGame.MiniGameId });
        }

        private void OnMiniGameComplete(MiniGameCompleteEvent evt)
        {
            if (evt.MiniGameId != _config.MiniGame.MiniGameId) return;
            StartCoroutine(PostMiniGameRoutine());
        }

        private IEnumerator PostMiniGameRoutine()
        {
            yield return null;
            GameManager.Instance.SetState(GameState.ChapterPlaying);
            SaveCurrentProgress(100);
            UpdateProgress("挑战完成");
            StartDialogueSequence(_config.PostMiniGameDialogues, 3);
        }

        private void ShowKnowledgeCards()
        {
            _state = 4;
            UpdateProgress("知识卡片解锁");
            var data = GameManager.Instance.CurrentAccountData;
            if (data != null)
            {
                StoryProgressService.UnlockKnowledgeCards(data, _config.UnlockedCards);
                GameManager.Instance.AutoSaveActiveAccount();
            }
            string cardText = "【知识卡片解锁】\n";
            foreach (var card in _config.UnlockedCards) cardText += "\n  · " + card;
            _dialogue.ShowLine(cardText);
            _dialogue.OnAdvance = () => { _state = 5; ShowTransitionDialogue(); };
        }

        private void ShowTransitionDialogue()
        {
            _state = 5;
            // 第三章无过渡对话，直接进入结局
            CompleteChapter();
        }

        private void CompleteChapter()
        {
            var data = GameManager.Instance.CurrentAccountData;
            if (data != null)
            {
                StoryProgressService.MarkChapterCompleted(data, ChapterIds.YangtzeDelta, 100);
                StoryProgressService.UnlockKnowledgeCards(data, _config.UnlockedCards);
                GameManager.Instance.AutoSaveActiveAccount();
            }
            // 最终章 → 结局（暂无 EndingScene，回到 Startup）
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToStartup));
        }

        private void SaveCurrentProgress(int step, bool completed = false)
        {
            var data = GameManager.Instance.CurrentAccountData;
            if (data == null) return;
            var progress = data.ChapterProgresses.Find(p => p.ChapterId == ChapterIds.YangtzeDelta);
            if (progress == null) { progress = new ChapterProgress { ChapterId = ChapterIds.YangtzeDelta }; data.ChapterProgresses.Add(progress); }
            progress.CurrentStep = step;
            progress.IsCompleted = completed;
            if (completed) GameManager.Instance.AutoSaveActiveAccount();
        }

        private void UpdateProgress(string text) { if (txtProgress != null) txtProgress.text = text; }
    }
}
