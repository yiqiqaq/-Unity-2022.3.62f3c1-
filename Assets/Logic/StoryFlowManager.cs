using System.Collections;
using Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Logic
{
    public class StoryFlowManager : MonoBehaviour
    {
        [SerializeField] private string prologueSceneName = "PrologueScene";
        [SerializeField] private string chapter1SceneName = "Chapter1Scene";
        [SerializeField] private string chapter2SceneName = "Chapter2Scene";
        [SerializeField] private string chapter3SceneName = "Chapter3Scene";
        [SerializeField] private string endingSceneName = "EndingScene";

        public void CompletePrologue()
        {
            var account = GameManager.Instance.CurrentAccountData;
            if (account == null || GameManager.Instance.ActiveSlotIndex < 0)
                return;

            StoryProgressService.MarkPrologueCompleted(account);
            GameManager.Instance.AutoSaveActiveAccount();
            StartCoroutine(TransitionToScene(chapter1SceneName, prologueSceneName));
        }

        public void CompleteChapter1()
        {
            CompleteChapter(
                ChapterIds.HuaiheEco,
                100,
                new[]
                {
                    "淮河生态治理核心措施",
                    "湿地生态功能",
                    "水污染防治基础知识"
                },
                chapter2SceneName,
                chapter1SceneName
            );
        }

        public void CompleteChapter2()
        {
            CompleteChapter(
                ChapterIds.WanbeiManufacturing,
                100,
                new[]
                {
                    "绿色制造与循环经济",
                    "清洁能源应用场景",
                    "皖北产业升级成果"
                },
                chapter3SceneName,
                chapter2SceneName
            );
        }

        public void CompleteChapter3()
        {
            CompleteChapter(
                ChapterIds.YangtzeDelta,
                100,
                new[]
                {
                    "长三角一体化发展战略",
                    "区域科创协同机制",
                    "安徽在长三角的核心定位"
                },
                endingSceneName,
                chapter3SceneName,
                generateFinalReport: true
            );
        }

        public void SwitchAccount()
        {
            AccountManager.Instance.SwitchAccount();
        }

        private void CompleteChapter(
            int chapterId,
            int finalStep,
            string[] unlockedCards,
            string nextScene,
            string currentScene,
            bool generateFinalReport = false)
        {
            var account = GameManager.Instance.CurrentAccountData;
            if (account == null || GameManager.Instance.ActiveSlotIndex < 0)
                return;

            StoryProgressService.MarkChapterCompleted(account, chapterId, finalStep);
            StoryProgressService.UnlockKnowledgeCards(account, unlockedCards);
            if (generateFinalReport)
                StoryProgressService.GenerateFinalReport(account);

            GameManager.Instance.AutoSaveActiveAccount();
            StartCoroutine(TransitionToScene(nextScene, currentScene));
        }

        private IEnumerator TransitionToScene(string nextScene, string sceneToUnload)
        {
            if (!GameManager.Instance.CanTransition(GameState.SceneTransitioning))
                yield break;

            GameManager.Instance.SetState(GameState.SceneTransitioning);

            if (!string.IsNullOrWhiteSpace(nextScene))
            {
                var loadOp = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
                if (loadOp != null)
                    yield return loadOp;
            }

            if (!string.IsNullOrWhiteSpace(sceneToUnload))
            {
                var sourceScene = SceneManager.GetSceneByName(sceneToUnload);
                if (sourceScene.IsValid() && sourceScene.isLoaded)
                {
                    var unloadOp = SceneManager.UnloadSceneAsync(sourceScene);
                    if (unloadOp != null)
                        yield return unloadOp;
                }
            }

            yield return Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            GameManager.Instance.SetState(GameState.ChapterPlaying);
        }
    }
}
