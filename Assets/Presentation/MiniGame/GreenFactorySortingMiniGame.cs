using UnityEngine;

namespace Presentation.MiniGame
{
    public class GreenFactorySortingMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 80f;
        [SerializeField] private float minSortRatio = 0.5f;
        [SerializeField] private float minAccuracy = 0.5f;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _totalSorted;
        private int _correctSorted;
        private int _totalSpawned;

        protected override void Start()
        {
            base.Start();
            ResetChallenge();
        }

        private void Update()
        {
            if (!IsRoundRunning)
                return;

            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
            {
                Debug.Log($"[分拣] 时间到! totalSorted={_totalSorted} totalSpawned={_totalSpawned} pass={IsPassConditionMet()}");
                if (IsPassConditionMet())
                    FinishGame(scoreOnPass);
                else
                    FailGame();
            }
        }

        public void RegisterSortResult(bool isCorrect)
        {
            _totalSorted++;
            if (isCorrect)
                _correctSorted++;
        }

        public void RetryChallenge()
        {
            SafeRetryReset();
            ResetChallenge();
        }

        public void SetTotalSpawned(int count) { _totalSpawned = count; }

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _totalSorted = 0;
            _correctSorted = 0;
            _totalSpawned = 0;
        }

        // 公开属性
        public float RemainingTime => _remainingTime;
        public int   TotalSorted => _totalSorted;
        public int   CorrectSorted => _correctSorted;
        public int   TotalSpawned => _totalSpawned;
        public float AccuracyValue => _totalSorted > 0 ? (float)_correctSorted / _totalSorted : 0f;
        public float DurationSeconds => durationSeconds;

        private bool IsPassConditionMet()
        {
            if (_totalSpawned <= 0) return false;
            var accuracy = _totalSorted == 0 ? 0f : (float)_correctSorted / _totalSorted;
            var sortRatio = (float)_totalSorted / _totalSpawned;
            return sortRatio >= minSortRatio && accuracy >= minAccuracy;
        }
    }
}
