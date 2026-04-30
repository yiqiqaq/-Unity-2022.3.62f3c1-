using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Logic
{
    public static class SaveManager
    {
        public static void Save(int slotIndex, AccountData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveManager] Save skipped: account data is null.");
                return;
            }
            data.SlotIndex = slotIndex;
            data.LastPlayTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            FileUtils.SaveToJson(slotIndex, data);
        }

        public static AccountData Load(int slotIndex)
        {
            var data = FileUtils.LoadFromJson<AccountData>(slotIndex);
            if (data != null)
                data.SlotIndex = slotIndex;
            return data;
        }

        public static void Delete(int slotIndex)
        {
            FileUtils.DeleteSlot(slotIndex);
        }

        public static bool Exists(int slotIndex)
        {
            return FileUtils.SlotExists(slotIndex);
        }

        public static List<AccountData> LoadAllAccounts()
        {
            var slotIndices = FileUtils.GetAllSlotIndices();
            var accounts = new List<AccountData>(slotIndices.Count);
            for (int i = 0; i < slotIndices.Count; i++)
            {
                var data = Load(slotIndices[i]);
                if (data != null)
                    accounts.Add(data);
            }
            return accounts.OrderBy(a => a.SlotIndex).ToList();
        }
    }
}
