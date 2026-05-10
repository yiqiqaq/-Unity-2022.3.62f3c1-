using UnityEngine;
using UnityEngine.UI;
using Presentation.UI;
using Core;

namespace Bootstrap
{
    /// <summary>
    /// 挂在 RegisterScene 的空 GameObject 上。
    /// 运行时自动构建科考登记页全部 UI 并连线。
    /// </summary>
    public class RegisterSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            BaseSceneBootstrap.EnsureGameSystems();

            UIBuilder.DestroyExistingCanvas("RegisterCanvas");
            var canvas = UIBuilder.CreateCanvas("RegisterCanvas");
            var root = canvas.transform;

            // ===== 页面标题 =====
            var txtPageTitle = UIBuilder.CreateText(root, "txtPageTitle", "科考观察员登记",
                fontSize: 42, anchor: TextAnchor.MiddleCenter,
                anchoredPos: new Vector2(0, 200));

            // ===== 返回键 =====
            var btnBack = UIBuilder.CreateTextButton(root, "btnBack", "返回",
                fontSize: 20,
                anchoredPos: new Vector2(-460, 280),
                sizeDelta: new Vector2(100, 40));

            // ===== 提示文字 =====
            var txtPrompt = UIBuilder.CreateText(root, "txtPrompt", "请输入你的观察员代号",
                fontSize: 26, anchor: TextAnchor.MiddleCenter,
                color: new Color(0.75f, 0.75f, 0.75f, 1f),
                anchoredPos: new Vector2(0, 80));

            // ===== 极细下划线输入框 =====
            var inputName = UIBuilder.CreateInputField(root, "inputName",
                placeholder: "两个字以上",
                anchoredPos: new Vector2(0, 10),
                width: 450, height: 56);

            // ===== 错误提示 (默认隐藏) =====
            var txtError = UIBuilder.CreateText(root, "txtError", "",
                fontSize: 22, anchor: TextAnchor.MiddleCenter,
                color: new Color(0.9f, 0.3f, 0.3f, 1f),
                anchoredPos: new Vector2(0, -50));
            txtError.gameObject.SetActive(false);

            // ===== 落笔确认键 =====
            var btnConfirm = UIBuilder.CreateTextButton(root, "btnConfirm", "落笔",
                fontSize: 28,
                anchoredPos: new Vector2(0, -120),
                sizeDelta: new Vector2(200, 56));

            // ===== 连线 RegisterUI =====
            var ui = gameObject.AddComponent<RegisterUI>();
            ui.txtPageTitle = txtPageTitle;
            ui.btnBack = btnBack;
            ui.txtPrompt = txtPrompt;
            ui.inputName = inputName;
            ui.txtError = txtError;
            ui.btnConfirm = btnConfirm;
        }
    }
}
