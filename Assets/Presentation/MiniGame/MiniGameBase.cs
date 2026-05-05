using System.Collections.Generic;
using UnityEngine;
using Logic;
using Core;

namespace Presentation.MiniGame
{
    public enum MiniGameRunState
    {
        Idle,
        Running,
        Passed,
        Failed
    }

    public abstract class MiniGameBase : MonoBehaviour
    {
        [SerializeField] protected string miniGameId;

        private readonly List<GameObject> _tempObjects = new List<GameObject>();
        public MiniGameRunState RunState { get; private set; } = MiniGameRunState.Idle;
        protected bool IsRoundRunning => RunState == MiniGameRunState.Running;

        protected virtual void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.MiniGamePlaying);
            StartRound();
        }

        protected void StartRound()
        {
            RunState = MiniGameRunState.Running;
        }

        protected void SafeRetryReset()
        {
            Cleanup();
            StartRound();
        }

        protected void RegisterTempObject(GameObject obj)
        {
            if (obj != null && !_tempObjects.Contains(obj))
                _tempObjects.Add(obj);
        }

        protected void FinishGame(int score)
        {
            if (!IsRoundRunning)
                return;
            RunState = MiniGameRunState.Passed;

            // 记录成绩到存档
            if (GameManager.Instance != null)
            {
                var data = GameManager.Instance.CurrentAccountData;
                if (data != null)
                {
                    var result = data.MiniGameResults.Find(r => r.MiniGameId == miniGameId);
                    if (result == null)
                    {
                        result = new MiniGameResult { MiniGameId = miniGameId };
                        data.MiniGameResults.Add(result);
                    }
                    result.IsCompleted = true;
                    result.Score = score;
                }
                GameManager.Instance.AutoSaveActiveAccount();
            }

            // 清理临时对象
            Cleanup();

            // 通知章节小游戏结束
            EventBus.Trigger(new MiniGameCompleteEvent { MiniGameId = miniGameId, Score = score });

            // 卸载自身所在场景（独立场景时才有效）
            var scene = gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        protected void FailGame()
        {
            if (!IsRoundRunning)
                return;
            RunState = MiniGameRunState.Failed;

            Cleanup();
            EventBus.Trigger(new MiniGameFailedEvent { MiniGameId = miniGameId });
        }

        private void Cleanup()
        {
            foreach (var obj in _tempObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            _tempObjects.Clear();
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }

    public struct MiniGameStartEvent
    {
        public string MiniGameId;
    }

    public struct MiniGameCompleteEvent
    {
        public string MiniGameId;
        public int Score;
    }

    public struct MiniGameFailedEvent
    {
        public string MiniGameId;
    }
}
