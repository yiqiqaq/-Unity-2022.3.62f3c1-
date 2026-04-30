using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class AccountData
    {
        public int SlotIndex;
        public string AccountName;
        public string CreateTime;
        public string LastPlayTime;
        public List<ChapterProgress> ChapterProgresses = new List<ChapterProgress>();
        public List<MiniGameResult> MiniGameResults = new List<MiniGameResult>();
        public List<string> UnlockedKnowledgeCards = new List<string>();
        public FinalReportData FinalReport = new FinalReportData();
    }

    [Serializable]
    public class ChapterProgress
    {
        public int ChapterId;
        public bool IsCompleted;
        public int CurrentStep;
    }

    [Serializable]
    public class MiniGameResult
    {
        public string MiniGameId;
        public bool IsCompleted;
        public int Score;
    }

    [Serializable]
    public class FinalReportData
    {
        public bool IsGenerated;
        public string Rating;
        public int CompletedChapterCount;
        public int TotalChapterCount;
        public string GenerateTime;
    }

    public static class ChapterIds
    {
        public const int Prologue = 0;
        public const int HuaiheEco = 1;
        public const int WanbeiManufacturing = 2;
        public const int YangtzeDelta = 3;
    }

    public static class AccountDataFactory
    {
        public static AccountData CreateNew(int slotIndex, string accountName)
        {
            var data = new AccountData
            {
                SlotIndex = slotIndex,
                AccountName = accountName,
                CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            data.ChapterProgresses.Add(new ChapterProgress { ChapterId = ChapterIds.Prologue, IsCompleted = false, CurrentStep = 0 });
            data.ChapterProgresses.Add(new ChapterProgress { ChapterId = ChapterIds.HuaiheEco, IsCompleted = false, CurrentStep = 0 });
            data.ChapterProgresses.Add(new ChapterProgress { ChapterId = ChapterIds.WanbeiManufacturing, IsCompleted = false, CurrentStep = 0 });
            data.ChapterProgresses.Add(new ChapterProgress { ChapterId = ChapterIds.YangtzeDelta, IsCompleted = false, CurrentStep = 0 });
            return data;
        }
    }
}
