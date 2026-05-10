using System.Collections;
using UnityEngine;
using Core;

namespace Presentation.MiniGame
{
    /// <summary>
    /// 小游戏加载器 —— 订阅 MiniGameStartEvent，根据 MiniGameId 实例化对应场景。
    /// 在 Chapter1SceneBootstrap 中注册。
    /// </summary>
    public class MiniGameLoader : MonoBehaviour
    {
        private GameObject _activeMiniGame;

        private void Awake()
        {
            EventBus.Subscribe<MiniGameStartEvent>(OnMiniGameStart);
            EventBus.Subscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
            EventBus.Subscribe<MiniGameFailedEvent>(OnMiniGameFailed);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MiniGameStartEvent>(OnMiniGameStart);
            EventBus.Unsubscribe<MiniGameCompleteEvent>(OnMiniGameComplete);
            EventBus.Unsubscribe<MiniGameFailedEvent>(OnMiniGameFailed);
        }

        private void OnMiniGameStart(MiniGameStartEvent evt)
        {
            // 防止重复创建
            if (_activeMiniGame != null)
            {
                Destroy(_activeMiniGame);
                _activeMiniGame = null;
            }

            switch (evt.MiniGameId)
            {
                case "water_purification":
                    _activeMiniGame = new GameObject("WaterPurificationScene");
                    _activeMiniGame.AddComponent<WaterPurificationScene>();
                    break;

                case "green_factory_sorting":
                    _activeMiniGame = new GameObject("GreenFactorySortingScene");
                    _activeMiniGame.AddComponent<GreenFactorySortingScene>();
                    break;

                case "innovation_matching":
                    _activeMiniGame = new GameObject("InnovationMatchingScene");
                    _activeMiniGame.AddComponent<InnovationMatchingScene>();
                    break;

                default:
                    Debug.LogWarning($"[MiniGameLoader] 未知的 MiniGameId: {evt.MiniGameId}");
                    break;
            }
        }

        private void OnMiniGameComplete(MiniGameCompleteEvent evt)
        {
            // 延迟到帧末销毁 — 确保所有事件处理器（包括小游戏自身的 OnComplete）
            // 都执行完毕后再清理，避免访问已销毁对象。
            if (_activeMiniGame != null)
                StartCoroutine(DestroyAfterFrame());
        }

        private IEnumerator DestroyAfterFrame()
        {
            yield return null; // 等一帧，让 Chapter1Handler 的 PostMiniGameRoutine 先 re-enable Canvas
            if (_activeMiniGame != null)
            {
                Destroy(_activeMiniGame);
                _activeMiniGame = null;
            }
        }

        private void OnMiniGameFailed(MiniGameFailedEvent evt)
        {
            // 失败时不销毁，由场景内的重试按钮处理
        }
    }
}
