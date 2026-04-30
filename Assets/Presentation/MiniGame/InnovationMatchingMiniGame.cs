using UnityEngine;

namespace Presentation.MiniGame
{
    public class InnovationMatchingMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 90f;
        [SerializeField] private int requiredMatchCount = 8;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _currentMatched;
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
                if (_currentMatched >= requiredMatchCount)
                    FinishGame(scoreOnPass);
                else
                    FailGame();
            }
        }

        public void RegisterCorrectMatch()
        {
            _currentMatched++;
            if (_currentMatched >= requiredMatchCount)
            {
                _isRunning = false;
                FinishGame(scoreOnPass);
            }
        }

        public void RetryChallenge()
        {
            ResetChallenge();
            _isRunning = true;
        }

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _currentMatched = 0;
        }
    }
}
