using UnityEngine;
using Core;

namespace Logic
{
    public enum GameState
    {
        AccountSelect,
        ChapterPlaying,
        MiniGamePlaying,
        SceneTransitioning
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.AccountSelect;
        public int ActiveSlotIndex { get; private set; } = -1;
        public AccountData CurrentAccountData { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 30;
            QualitySettings.SetQualityLevel(0, true);
        }

        private void Start()
        {
            CheckNetworkAndInitialize();
        }

        private void CheckNetworkAndInitialize()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ShowNetworkWarning();
            }
            else
            {
                InitializeServices();
            }
        }

        private void ShowNetworkWarning()
        {
            Debug.LogWarning("[GameManager] 无网络连接。请提醒用户连接网络才可以进行游戏。");
            // TODO: 此处可调用UI层显示弹窗（例如: UIManager.ShowAlert("请连接网络", RetryAction)）
        }

        private void InitializeServices()
        {
            // 初始化天气服务等需联网的组件
            if (WeatherService.Instance == null)
            {
                GameObject weatherObj = new GameObject("WeatherService");
                weatherObj.transform.SetParent(this.transform);
                weatherObj.AddComponent<WeatherService>();
            }

            WeatherService.Instance.FetchWeather();
        }

        public bool CanTransition(GameState requestedState)
        {
            if (CurrentState == GameState.SceneTransitioning)
                return false;
            return true;
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
        }

        public void SetActiveAccount(int slotIndex, AccountData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[GameManager] SetActiveAccount ignored: data is null.");
                return;
            }

            if (CurrentAccountData != null && ActiveSlotIndex >= 0 && ActiveSlotIndex != slotIndex)
            {
                SaveManager.Save(ActiveSlotIndex, CurrentAccountData);
            }

            ActiveSlotIndex = slotIndex;
            CurrentAccountData = data;
            SetState(GameState.ChapterPlaying);
        }

        public void ClearActiveAccount(bool saveBeforeClear = false)
        {
            if (saveBeforeClear && CurrentAccountData != null && ActiveSlotIndex >= 0)
            {
                SaveManager.Save(ActiveSlotIndex, CurrentAccountData);
            }
            ActiveSlotIndex = -1;
            CurrentAccountData = null;
            SetState(GameState.AccountSelect);
        }

        public void AutoSaveActiveAccount()
        {
            if (CurrentAccountData == null || ActiveSlotIndex < 0)
                return;
            SaveManager.Save(ActiveSlotIndex, CurrentAccountData);
        }

        private void OnApplicationQuit()
        {
            AutoSaveActiveAccount();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }
    }
}
