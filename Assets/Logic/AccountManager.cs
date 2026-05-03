using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core;

namespace Logic
{
    public class AccountManager : MonoBehaviour
    {
        public const int SlotCount = 3;
        public const string BaseSceneName = "BaseScene";

        public static AccountManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool HasSlotData(int slotIndex)
        {
            return FileUtils.SlotExists(slotIndex);
        }

        public AccountData GetSlotData(int slotIndex)
        {
            return SaveManager.Load(slotIndex);
        }

        public List<AccountData> GetAllAccounts()
        {
            return SaveManager.LoadAllAccounts();
        }

        private int pendingCreateSlotIndex = -1;

        public void SetActiveSlot(int slotIndex)
        {
            pendingCreateSlotIndex = slotIndex;
        }

        public int GetActiveSlot()
        {
            return pendingCreateSlotIndex;
        }

        public void CreateNewAccount(int slotIndex, string accountName)
        {
            PrepareForAccountActivation();
            var data = AccountDataFactory.CreateNew(slotIndex, accountName);
            SaveManager.Save(slotIndex, data);
            GameManager.Instance.SetActiveAccount(slotIndex, data);
        }

        public void LoadAccount(int slotIndex)
        {
            PrepareForAccountActivation();
            var data = SaveManager.Load(slotIndex);
            if (data != null)
            {
                data.LastPlayTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                GameManager.Instance.SetActiveAccount(slotIndex, data);
            }
        }

        public void SwitchAccount()
        {
            if (!GameManager.Instance.CanTransition(GameState.AccountSelect))
                return;
            StartCoroutine(SwitchAccountRoutine());
        }

        private IEnumerator SwitchAccountRoutine()
        {
            GameManager.Instance.SetState(GameState.SceneTransitioning);

            // Step 1: 保存当前数据
            if (GameManager.Instance.CurrentAccountData != null)
            {
                SaveManager.Save(
                    GameManager.Instance.ActiveSlotIndex,
                    GameManager.Instance.CurrentAccountData
                );
            }

            // Step 2: 卸载所有非基座场景
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != BaseSceneName && scene.isLoaded)
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            // Step 3: 清空数据引用
            GameManager.Instance.ClearActiveAccount();

            // Step 4: 卸载未使用资源
            yield return Resources.UnloadUnusedAssets();

            // Step 5: 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            // Step 6: 激活账户选择UI（通过事件通知表现层）
            EventBus.Trigger(new AccountSelectShowEvent());
        }

        private static void PrepareForAccountActivation()
        {
            GameManager.Instance.ClearActiveAccount(saveBeforeClear: true);
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }
    }

    public struct AccountSelectShowEvent { }
}
