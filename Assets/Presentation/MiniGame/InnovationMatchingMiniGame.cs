using UnityEngine;

namespace Presentation.MiniGame
{
    public class InnovationMatchingMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 90f;
        [SerializeField] private int requiredMatchCount = 8;
        [SerializeField] private float wrongMatchTimePenaltySeconds = 6f;
        [SerializeField] private int wrongMatchProgressPenalty = 1;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _currentMatched;
        private int _errorCount;

        protected override void Start()
        {
            base.Start();
            ResetChallenge();
        }

        private void Update()
        {
            if (!IsRoundRunning)
                return;

            if (_currentMatched >= requiredMatchCount)
            {
                FinishGame(scoreOnPass);
                return;
            }

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
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
                FinishGame(scoreOnPass);
            }
        }

        public void RegisterWrongMatch()
        {
            _errorCount++;
            _remainingTime -= wrongMatchTimePenaltySeconds;
            if (wrongMatchProgressPenalty > 0 && _currentMatched > 0)
            {
                _currentMatched = Mathf.Max(0, _currentMatched - wrongMatchProgressPenalty);
            }

            if (_remainingTime <= 0f)
            {
                FailGame();
            }
        }

        public void RetryChallenge()
        {
            SafeRetryReset();
            ResetChallenge();
        }

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _currentMatched = 0;
            _errorCount = 0;
        }

        // 公开属性
        public float RemainingTime => _remainingTime;
        public int   CurrentMatched => _currentMatched;
        public int   ErrorCount => _errorCount;
        public float DurationSeconds => durationSeconds;
    }
}
