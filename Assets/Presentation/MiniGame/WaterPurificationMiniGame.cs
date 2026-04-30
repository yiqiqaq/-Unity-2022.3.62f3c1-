using UnityEngine;

namespace Presentation.MiniGame
{
    public class WaterPurificationMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 90f;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _totalPollutants;
        private int _cleanedPollutants;
        private bool _misTouchedEcoObject;
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
                var pass = _totalPollutants > 0 &&
                           !_misTouchedEcoObject &&
                           ((float)_cleanedPollutants / _totalPollutants) >= 0.8f;
                if (pass)
                    FinishGame(scoreOnPass);
                else
                    FailGame();
            }
        }

        public void RegisterPollutantSpawned()
        {
            _totalPollutants++;
        }

        public void RegisterPollutantCleaned()
        {
            _cleanedPollutants++;
        }

        public void RegisterEcoObjectMisTouch()
        {
            _misTouchedEcoObject = true;
        }

        public void RetryChallenge()
        {
            ResetChallenge();
            _isRunning = true;
        }

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _totalPollutants = 0;
            _cleanedPollutants = 0;
            _misTouchedEcoObject = false;
        }
    }
}
