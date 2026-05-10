using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Core;

namespace Bootstrap
{
    /// <summary>
    /// 挂在 IntroScene 的空 GameObject 上。
    /// 运行时自动构建游戏介绍页全部 UI 并连线。
    /// </summary>
    public class IntroSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            BaseSceneBootstrap.EnsureGameSystems();

            UIBuilder.DestroyExistingCanvas("IntroCanvas");
            var canvas = UIBuilder.CreateCanvas("IntroCanvas");
            var root = canvas.transform;

            // ===== 返回键 =====
            var btnBack = UIBuilder.CreateTextButton(root, "btnBack", "返回",
                fontSize: 20,
                anchoredPos: new Vector2(-460, 280),
                sizeDelta: new Vector2(100, 40));

            // ===== 大段文本区域 =====
            var loreGo = new GameObject("LoreArea");
            loreGo.transform.SetParent(root, false);
            var loreRt = loreGo.AddComponent<RectTransform>();
            loreRt.anchorMin = new Vector2(0.15f, 0.15f);
            loreRt.anchorMax = new Vector2(0.85f, 0.85f);
            loreRt.offsetMin = Vector2.zero;
            loreRt.offsetMax = Vector2.zero;

            var txtLoreText = UIBuilder.CreateText(loreGo.transform, "txtLoreText", "",
                fontSize: 28, anchor: TextAnchor.UpperLeft,
                anchoredPos: Vector2.zero,
                sizeDelta: Vector2.zero);
            // 填满父级
            var loreTxtRt = txtLoreText.GetComponent<RectTransform>();
            loreTxtRt.anchorMin = Vector2.zero;
            loreTxtRt.anchorMax = Vector2.one;
            loreTxtRt.offsetMin = Vector2.zero;
            loreTxtRt.offsetMax = Vector2.zero;

            // 允许文字自动换行
            var loreLayout = loreGo.AddComponent<VerticalLayoutGroup>();
            loreLayout.childForceExpandWidth = true;
            loreLayout.childForceExpandHeight = true;
            loreLayout.padding = new RectOffset(20, 20, 20, 20);

            // ===== 跳过键 (右下角) =====
            var btnSkip = UIBuilder.CreateTextButton(root, "btnSkip", "跳过",
                fontSize: 24,
                anchoredPos: new Vector2(380, -260),
                sizeDelta: new Vector2(140, 44));

            // ===== 进入游戏键 (右下角，默认隐藏) =====
            var btnEnterGame = UIBuilder.CreateTextButton(root, "btnEnterGame", "正式开启科考",
                fontSize: 26,
                anchoredPos: new Vector2(380, -260),
                sizeDelta: new Vector2(200, 50));
            btnEnterGame.gameObject.SetActive(false);

            // ===== 连线 IntroUI =====
            var ui = gameObject.AddComponent<IntroUI>();
            ui.txtLoreText = txtLoreText;
            ui.btnBack = btnBack;
            ui.btnSkip = btnSkip;
            ui.btnEnterGame = btnEnterGame;
        }
    }
}
