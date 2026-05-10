using UnityEngine;

namespace Presentation.MiniGame
{
    public class WaterPurificationMiniGame : MiniGameBase
    {
        [SerializeField] private float durationSeconds = 90f;
        [SerializeField] private float requiredCleanRatio = 0.8f;
        [SerializeField] private int maxMisTouches = 2;
        [SerializeField] private int scoreOnPass = 100;

        private float _remainingTime;
        private int _totalPollutants;
        private int _cleanedPollutants;
        private int _misTouchCount;

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
                if (IsPassConditionMet())
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

            if (IsPassConditionMet())
                FinishGame(scoreOnPass);
        }

        public void RegisterEcoObjectMisTouch()
        {
            _misTouchCount++;
            if (_misTouchCount > maxMisTouches && IsRoundRunning)
                FailGame();
        }

        public void RetryChallenge()
        {
            SafeRetryReset();
            ResetChallenge();
        }

        public void OnEntityDropped(bool isPollutant)
        {
            if (isPollutant)
                RegisterPollutantCleaned();
            else
                RegisterEcoObjectMisTouch();
        }

        public float RemainingTime => _remainingTime;
        public int TotalPollutants => _totalPollutants;
        public int CleanedPollutants => _cleanedPollutants;
        public int MisTouchCount => _misTouchCount;
        public float DurationSeconds => durationSeconds;
        public int MaxMisTouches => maxMisTouches;
        public float RequiredCleanRatio => requiredCleanRatio;

        private void ResetChallenge()
        {
            _remainingTime = durationSeconds;
            _totalPollutants = 0;
            _cleanedPollutants = 0;
            _misTouchCount = 0;
        }

        private bool IsPassConditionMet()
        {
            if (_totalPollutants <= 0)
                return false;
            if (_misTouchCount > maxMisTouches)
                return false;

            return ((float)_cleanedPollutants / _totalPollutants) >= requiredCleanRatio;
        }
    }
}
