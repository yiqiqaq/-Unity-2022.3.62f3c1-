using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Logic;
using Core;
using Presentation.UI;

namespace Presentation.Chapter
{
    public class SummaryHandler : MonoBehaviour
    {
        private DialogueUI _dialogue;
        private List<DialogueNode> _mainList;
        private List<DialogueNode> _currentList;
        private int _currentIndex;
        private bool _inBranch;
        private Canvas _summaryCanvas;

        public void SetCanvas(Canvas canvas) { _summaryCanvas = canvas; }

        private void Start()
        {
            _dialogue = FindObjectOfType<DialogueUI>();
            _mainList = StoryScenarioLibrary.SummaryDialogues;
            _inBranch = false;
            StartDialogueSequence(_mainList);
        }

        private void StartDialogueSequence(List<DialogueNode> list)
        {
            _currentList = list;
            _currentIndex = 0;
            ShowCurrentNode();
        }

        private void ShowCurrentNode()
        {
            if (_currentIndex >= _currentList.Count)
            {
                // 分支对话结束 → 恢复主列表，继续后续节点
                if (_inBranch)
                {
                    _inBranch = false;
                    _currentList = _mainList;
                    // _currentIndex 已在 OnBranchChoice 中 +1，指向选择节点之后
                    ShowCurrentNode();
                    return;
                }
                // 主列表全部播完 → 返回启动页
                OnDialogueEnd();
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

            // 先推进主列表索引（跳过选择节点）
            _currentIndex++;

            // 进入分支对话
            if (choice.ReactionDialogues != null && choice.ReactionDialogues.Count > 0)
            {
                _inBranch = true;
                _currentList = choice.ReactionDialogues;
                _currentIndex = 0;
            }

            ShowCurrentNode();
        }

        private void OnDialogueEnd()
        {
            _dialogue.HideAllUI();
            Debug.Log("[SummaryHandler] Dialogue ended, triggering GoToStartup");
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToStartup));
        }
    }
}
