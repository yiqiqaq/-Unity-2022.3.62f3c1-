using UnityEngine;
using UnityEngine.UI;
using Logic;
using Core;
using System.Collections.Generic;

namespace Presentation.UI
{
    /// <summary>
    /// 存档选择页 —— 三位科考观察员的存档卡槽
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        [Header("导航")]
        public Button btnBack;
        public Text txtPageTitle;

        [Header("存档卡槽 (3个)")]
        public GameObject[] slotContainers;
        public Text[] slotTexts;
        public Button[] slotButtons;

        private void Start()
        {
            if (txtPageTitle != null)
                txtPageTitle.text = "选择科考档案";

            if (btnBack != null)
                btnBack.onClick.AddListener(OnBackClick);

            RefreshSlots();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < slotContainers.Length; i++)
            {
                int slotIndex = i + 1;
                bool exists = AccountManager.Instance.HasSlotData(slotIndex);

                if (exists)
                {
                    AccountData data = AccountManager.Instance.GetSlotData(slotIndex);
                    int completedCount = 0;
                    for (int c = 0; c < data.ChapterProgresses.Count; c++)
                    {
                        if (data.ChapterProgresses[c].IsCompleted)
                            completedCount++;
                    }
                    slotTexts[i].text = $"{data.AccountName}\n{data.LastPlayTime}\n科考进度: {completedCount}/3";

                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => OnLoadAccount(slotIndex));
                }
                else
                {
                    slotTexts[i].text = "空闲档案位\n成为科考观察员";

                    slotButtons[i].onClick.RemoveAllListeners();
                    slotButtons[i].onClick.AddListener(() => OnNewAccount(slotIndex));
                }
            }
        }

        private void OnBackClick()
        {
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToStartup));
        }

        private void OnLoadAccount(int slotIndex)
        {
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.LoadAccount, slotIndex));
        }

        private void OnNewAccount(int slotIndex)
        {
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToRegister, slotIndex));
        }
    }
}
