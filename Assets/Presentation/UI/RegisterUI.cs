using UnityEngine;
using UnityEngine.UI;
using Logic;
using Core;

namespace Presentation.UI
{
    /// <summary>
    /// 注册页 —— 科考观察员登记
    /// 极简单线输入框 + 落笔确认
    /// </summary>
    public class RegisterUI : MonoBehaviour
    {
        [Header("导航")]
        public Button btnBack;
        public Text txtPageTitle;

        [Header("登记控件")]
        public Text txtPrompt;
        public InputField inputName;
        public Text txtError;
        public Button btnConfirm;

        private void Start()
        {
            if (txtPageTitle != null)
                txtPageTitle.text = "科考观察员登记";

            if (txtPrompt != null)
                txtPrompt.text = "请输入你的观察员代号";

            if (btnBack != null)
                btnBack.onClick.AddListener(OnBackClick);

            if (btnConfirm != null)
            {
                var label = btnConfirm.GetComponentInChildren<Text>();
                if (label != null) label.text = "落笔";
                btnConfirm.onClick.AddListener(OnConfirmClick);
            }

            if (txtError != null)
                txtError.gameObject.SetActive(false);
        }

        private void OnBackClick()
        {
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToLogin));
        }

        private void OnConfirmClick()
        {
            string cleanName = inputName != null ? inputName.text.Trim() : "";

            if (string.IsNullOrEmpty(cleanName))
            {
                ShowError("观察员代号不可为空");
                return;
            }

            if (cleanName.Length < 2)
            {
                ShowError("代号至少需要两个字");
                return;
            }

            int pendingSlot = AccountManager.Instance.GetActiveSlot();
            if (pendingSlot <= 0)
            {
                ShowError("未选择有效档案位");
                return;
            }

            EventBus.Trigger(new IntentEvent(
                IntentEvent.IntentType.CreateAccount,
                pendingSlot,
                cleanName
            ));
        }

        private void ShowError(string msg)
        {
            if (txtError != null)
            {
                txtError.text = msg;
                txtError.gameObject.SetActive(true);
            }
        }
    }
}
