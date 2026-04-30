using UnityEngine;

namespace Presentation.MiniGame
{
    public class GreenFactorySortingMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 60f;
        [SerializeField] private int minSortedCount = 50;
        [SerializeField] private float minAccuracy = 0.9f;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _totalSorted;
        private int _correctSorted;
        private bool _isRunning;

        protected override void Start()
        {
            base.Start();
            ResetChallenge();
            _isRunning = true;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                _isRunning = false;
                var accuracy = _totalSorted == 0 ? 0f : (float)_correctSorted / _totalSorted;
                var pass = _totalSorted >= minSortedCount && accuracy >= minAccuracy;
                if (pass)
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
            ResetChallenge();
            _isRunning = true;
        }

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _totalSorted = 0;
            _correctSorted = 0;
        }
    }
}
