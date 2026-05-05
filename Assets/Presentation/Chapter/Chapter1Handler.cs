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
    public class Chapter1Handler : MonoBehaviour
    {
        [SerializeField] private Text txtChapterTitle;
        [SerializeField] private Text txtProgress;

        private DialogueUI _dialogue;
        private StoryChapterConfig _config;
        private List<DialogueNode> _currentList;
        private int _currentIndex;
        private int _state; // 0=prelude,1=guide,2=waitForMiniGame,3=postGame,4=knowledge,5=done

        private void Start()
        {
            Debug.Log("[CH1Handler] Start BEGIN");
            _config = StoryScenarioLibrary.BuildChapter1();
            _dialogue = FindObjectOfType<DialogueUI>();
            Debug.Log($"[CH1Handler] _dialogue found: {_dialogue != null}, _config: {_config != null}");

            if (txtChapterTitle != null)
                txtChapterTitle.text = _config.ChapterName;
            UpdateProgress("科考进行中");

            EventBus.Subscribe<MiniGameCompleteEvent>(OnMiniGameComplete);

            StartDialogueSequence(_config.PreludeDialogues, state: 0);
            Debug.Log("[CH1Handler] Start COMPLETE");
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
            Debug.Log($"[CH1Handler] ShowCurrentNode: idx={_currentIndex}, list.Count={_currentList?.Count}, state={_state}");
            if (_currentIndex >= _currentList.Count)
            {
                OnSequenceEnd();
                return;
            }

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

        private void OnAdvance()
        {
            _currentIndex++;
            ShowCurrentNode();
        }

        private void OnBranchChoice(int choiceIndex)
        {
            _dialogue.HideAllUI();
            var choice = _currentList[_currentIndex].Choices[choiceIndex];

            // 先推进到下一个节点（跳过当前选择节点）
            _currentIndex++;

            // 然后显示分支反应对话
            if (choice.ReactionDialogues != null && choice.ReactionDialogues.Count > 0)
            {
                _currentList = choice.ReactionDialogues;
                _currentIndex = 0;
                ShowCurrentNode();
            }
            else
            {
                ShowCurrentNode();
            }
        }

        private void OnSequenceEnd()
        {
            _dialogue.HideAllUI();

            switch (_state)
            {
                case 0: // Prelude 结束 → Guide
                    StartDialogueSequence(_config.MiniGameGuideDialogues, state: 1);
                    break;
                case 1: // Guide 结束 → 显示"开始挑战"按钮
                    ShowStartChallengeButton();
                    break;
                case 3: // PostGame 结束 → Knowledge
                    ShowKnowledgeCards();
                    break;
                case 4: // Knowledge 结束 → Transition
                    ShowTransitionDialogue();
                    break;
                case 5: // Transition 结束 → 加载第二章
                    CompleteChapter();
                    break;
            }
        }

        private void ShowStartChallengeButton()
        {
            _state = 2;
            if (txtProgress != null) txtProgress.text = "等待开始挑战";

            Transform parent = (_dialogue.choicePanel != null)
                ? _dialogue.choicePanel.transform.parent
                : _dialogue.transform;
            var challengeBtn = UIBuilder.CreateTextButton(parent,
                "btnStartChallenge", "开始挑战",
                fontSize: 30, anchoredPos: Vector2.zero, sizeDelta: new Vector2(280, 65));

            challengeBtn.onClick.AddListener(() =>
            {
                Destroy(challengeBtn.gameObject);
                StartMiniGame();
            });

            // 居中显示
            var rt = challengeBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
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

            // 延迟一帧执行，等待 MiniGame 场景卸载完成
            StartCoroutine(PostMiniGameRoutine());
        }

        private IEnumerator PostMiniGameRoutine()
        {
            yield return null; // 等待 MiniGame 场景卸载 + GC

            GameManager.Instance.SetState(GameState.ChapterPlaying);
            SaveCurrentProgress(step: 100);

            UpdateProgress("挑战完成");
            StartDialogueSequence(_config.PostMiniGameDialogues, state: 3);
        }

        private void ShowKnowledgeCards()
        {
            _state = 4;
            UpdateProgress("知识卡片解锁");

            // 解锁知识卡片
            var data = GameManager.Instance.CurrentAccountData;
            if (data != null)
            {
                StoryProgressService.UnlockKnowledgeCards(data, _config.UnlockedCards);
                GameManager.Instance.AutoSaveActiveAccount();
            }

            // 显示卡片内容
            string cardText = "【知识卡片解锁】\n";
            foreach (var card in _config.UnlockedCards)
            {
                cardText += "\n  · " + card;
            }

            _dialogue.ShowLine(cardText);
            _dialogue.OnAdvance = () =>
            {
                _state = 5;
                ShowCurrentNode2();
            };
        }

        private void ShowCurrentNode2()
        {
            // 知识卡片之后进入过渡对话
            ShowTransitionDialogue();
        }

        private void ShowTransitionDialogue()
        {
            _state = 5;
            if (string.IsNullOrEmpty(_config.TransitionDialogue))
            {
                CompleteChapter();
                return;
            }

            _dialogue.ShowLine(_config.TransitionDialogue);
            _dialogue.OnAdvance = CompleteChapter;
        }

        private void CompleteChapter()
        {
            // 标记章节完成
            var data = GameManager.Instance.CurrentAccountData;
            if (data != null)
            {
                StoryProgressService.MarkChapterCompleted(data, ChapterIds.HuaiheEco, 100);
                StoryProgressService.UnlockKnowledgeCards(data, _config.UnlockedCards);
                GameManager.Instance.AutoSaveActiveAccount();
            }

            // 通过 EventBus 过渡到第二章（TransitionManager 负责叠加场景切换）
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToChapter, 2));
        }

        private void SaveCurrentProgress(int step, bool completed = false)
        {
            var data = GameManager.Instance.CurrentAccountData;
            if (data == null) return;

            var progress = data.ChapterProgresses.Find(p => p.ChapterId == ChapterIds.HuaiheEco);
            if (progress == null)
            {
                progress = new ChapterProgress { ChapterId = ChapterIds.HuaiheEco };
                data.ChapterProgresses.Add(progress);
            }
            progress.CurrentStep = step;
            progress.IsCompleted = completed;

            if (completed) GameManager.Instance.AutoSaveActiveAccount();
        }

        private void UpdateProgress(string text)
        {
            if (txtProgress != null) txtProgress.text = text;
        }
    }
}
