using UnityEngine;

namespace Presentation.MiniGame
{
    public class GreenFactorySortingMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 75f;
        [SerializeField] private int minSortedCount = 50;
        [SerializeField] private float minAccuracy = 0.85f;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _totalSorted;
        private int _correctSorted;

        protected override void Start()
        {
            base.Start();
            ResetChallenge();
        }

        private void Update()
        {
            if (!IsRoundRunning)
                return;

            if (IsPassConditionMet())
            {
                FinishGame(scoreOnPass);
                return;
            }

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
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

            if (IsPassConditionMet())
                FinishGame(scoreOnPass);
        }

        public void RetryChallenge()
        {
            SafeRetryReset();
            ResetChallenge();
        }

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _totalSorted = 0;
            _correctSorted = 0;
        }

        private bool IsPassConditionMet()
        {
            var accuracy = _totalSorted == 0 ? 0f : (float)_correctSorted / _totalSorted;
            return _totalSorted >= minSortedCount && accuracy >= minAccuracy;
        }
    }
}
