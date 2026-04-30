using UnityEngine;
using Logic;
using Core;

namespace Presentation.UI
{
    public class AccountSelectUI : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<AccountSelectShowEvent>(OnShowAccountSelect);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AccountSelectShowEvent>(OnShowAccountSelect);
        }

        private void Start()
        {
            RefreshSlotDisplay();
        }

        private void OnShowAccountSelect(AccountSelectShowEvent evt)
        {
            gameObject.SetActive(true);
            RefreshSlotDisplay();
        }

        public void RefreshSlotDisplay()
        {
            var accounts = AccountManager.Instance.GetAllAccounts();
            if (accounts.Count == 0)
            {
                Debug.Log("[账户列表] 当前无本地存档。");
            }
            else
            {
                for (int i = 0; i < accounts.Count; i++)
                {
                    var data = accounts[i];
                    Debug.Log($"[存档位{data.SlotIndex}] {data.AccountName} - 最后游玩: {data.LastPlayTime}");
                }
            }

            for (int i = 1; i <= AccountManager.SlotCount; i++)
            {
                if (!AccountManager.Instance.HasSlotData(i))
                    Debug.Log($"[存档位{i}] 空");
            }
        }

        public void OnSlotClicked(int slotIndex)
        {
            if (AccountManager.Instance.HasSlotData(slotIndex))
            {
                AccountManager.Instance.LoadAccount(slotIndex);
                gameObject.SetActive(false);
            }
            else
            {
                // 新建存档 - 此处应弹出输入框，简化为默认名称
                string defaultName = $"玩家{slotIndex}";
                AccountManager.Instance.CreateNewAccount(slotIndex, defaultName);
                gameObject.SetActive(false);
            }
        }

        public void OnDeleteSlot(int slotIndex)
        {
            SaveManager.Delete(slotIndex);
            RefreshSlotDisplay();
        }
    }
}
