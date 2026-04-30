using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Core
{
    public static class FileUtils
    {
        private static readonly Regex SlotFileRegex = new Regex(@"^Slot_(\d+)\.json$", RegexOptions.Compiled);

        public static string GetSaveDirectory()
        {
            string dir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetSlotFilePath(int slotIndex)
        {
            return Path.Combine(GetSaveDirectory(), $"Slot_{slotIndex}.json");
        }

        public static void SaveToJson<T>(int slotIndex, T data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSlotFilePath(slotIndex), json);
        }

        public static T LoadFromJson<T>(int slotIndex) where T : class
        {
            string path = GetSlotFilePath(slotIndex);
            if (!File.Exists(path))
                return null;
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<T>(json);
        }

        public static bool SlotExists(int slotIndex)
        {
            return File.Exists(GetSlotFilePath(slotIndex));
        }

        public static void DeleteSlot(int slotIndex)
        {
            string path = GetSlotFilePath(slotIndex);
            if (File.Exists(path))
                File.Delete(path);
        }

        public static List<int> GetAllSlotIndices()
        {
            var result = new List<int>();
            var saveDir = GetSaveDirectory();
            var files = Directory.GetFiles(saveDir, "Slot_*.json", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                var fileName = Path.GetFileName(files[i]);
                if (TryParseSlotIndex(fileName, out var slotIndex))
                    result.Add(slotIndex);
            }
            result.Sort();
            return result;
        }

        private static bool TryParseSlotIndex(string fileName, out int slotIndex)
        {
            slotIndex = -1;
            var match = SlotFileRegex.Match(fileName);
            if (!match.Success)
                return false;
            return int.TryParse(match.Groups[1].Value, out slotIndex);
        }
    }
}
