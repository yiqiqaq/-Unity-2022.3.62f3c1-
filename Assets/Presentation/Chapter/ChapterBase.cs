using UnityEngine;
using Logic;
using Core;
using Presentation.MiniGame;

namespace Presentation.Chapter
{
    public abstract class ChapterBase : MonoBehaviour
    {
        [SerializeField] protected int chapterId;

        protected virtual void OnEnable()
        {
            EventBus.Subscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
        }

        protected virtual void OnDisable()
        {
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
        }

        protected virtual void Start()
        {
            // 只向GameManager询问"我该干什么"
            var data = GameManager.Instance.CurrentAccountData;
            if (data != null)
            {
                var progress = data.ChapterProgresses.Find(p => p.ChapterId == chapterId);
                if (progress != null)
                {
                    OnRestoreProgress(progress);
                }
            }
        }

        protected abstract void OnRestoreProgress(ChapterProgress progress);

        protected void SaveCurrentProgress(int step, bool completed = false)
        {
            var data = GameManager.Instance.CurrentAccountData;
            if (data == null) return;

            var progress = data.ChapterProgresses.Find(p => p.ChapterId == chapterId);
            if (progress == null)
            {
                progress = new ChapterProgress { ChapterId = chapterId };
                data.ChapterProgresses.Add(progress);
            }
            progress.CurrentStep = step;
            progress.IsCompleted = completed;

            if (completed)
            {
                GameManager.Instance.AutoSaveActiveAccount();
            }
        }

        protected void RequestMiniGame(string miniGameId)
        {
            GameManager.Instance.SetState(GameState.MiniGamePlaying);
            EventBus.Trigger(new MiniGameStartEvent { MiniGameId = miniGameId });
        }

        private void OnMiniGameComplete(MiniGameCompleteEvent evt)
        {
            GameManager.Instance.SetState(GameState.ChapterPlaying);
            OnMiniGameFinished(evt.MiniGameId, evt.Score);
        }

        protected abstract void OnMiniGameFinished(string miniGameId, int score);
    }
}
