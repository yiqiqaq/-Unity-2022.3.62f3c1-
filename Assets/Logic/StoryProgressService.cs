using System;
using System.Collections.Generic;
using Core;

namespace Logic
{
    public static class StoryProgressService
    {
        private static readonly int[] MainChapterIds =
        {
            ChapterIds.HuaiheEco,
            ChapterIds.WanbeiManufacturing,
            ChapterIds.YangtzeDelta
        };

        public static void MarkPrologueCompleted(AccountData data)
        {
            UpsertChapterProgress(data, ChapterIds.Prologue, step: 1, completed: true);
        }

        public static void MarkChapterCompleted(AccountData data, int chapterId, int finalStep)
        {
            UpsertChapterProgress(data, chapterId, finalStep, completed: true);
        }

        public static void UnlockKnowledgeCards(AccountData data, IEnumerable<string> cards)
        {
            if (data == null || cards == null)
                return;

            foreach (var card in cards)
            {
                if (string.IsNullOrWhiteSpace(card))
                    continue;
                if (!data.UnlockedKnowledgeCards.Contains(card))
                    data.UnlockedKnowledgeCards.Add(card);
            }
        }

        public static void GenerateFinalReport(AccountData data)
        {
            if (data == null)
                return;

            var completed = 0;
            for (int i = 0; i < MainChapterIds.Length; i++)
            {
                var progress = data.ChapterProgresses.Find(p => p.ChapterId == MainChapterIds[i]);
                if (progress != null && progress.IsCompleted)
                    completed++;
            }

            data.FinalReport.IsGenerated = true;
            data.FinalReport.CompletedChapterCount = completed;
            data.FinalReport.TotalChapterCount = MainChapterIds.Length;
            data.FinalReport.Rating = ResolveRating(completed, MainChapterIds.Length);
            data.FinalReport.GenerateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static string ResolveRating(int completed, int total)
        {
            if (total <= 0)
                return "C";

            var ratio = (float)completed / total;
            if (ratio >= 1f)
                return "S";
            if (ratio >= 0.66f)
                return "A";
            if (ratio >= 0.33f)
                return "B";
            return "C";
        }

        private static void UpsertChapterProgress(AccountData data, int chapterId, int step, bool completed)
        {
            if (data == null)
                return;

            var progress = data.ChapterProgresses.Find(p => p.ChapterId == chapterId);
            if (progress == null)
            {
                progress = new ChapterProgress { ChapterId = chapterId };
                data.ChapterProgresses.Add(progress);
            }

            progress.CurrentStep = step;
            progress.IsCompleted = completed;
        }
    }
}
