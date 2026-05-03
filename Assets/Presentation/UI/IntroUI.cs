using UnityEngine;
using UnityEngine.UI;
using Logic;
using Core;
using System.Collections;

namespace Presentation.UI
{
    /// <summary>
    /// 游戏介绍页 —— 逐字浮现的科考背景科普
    /// </summary>
    public class IntroUI : MonoBehaviour
    {
        [Header("大文本引言")]
        public Text txtLoreText;
        [SerializeField] private float textTypingSpeed = 0.05f;

        [Header("操作按键")]
        public Button btnBack;
        public Button btnSkip;
        public Button btnEnterGame;

        private const string LORE_CONTENT =
            "淮水之畔，科技涌动。\n\n" +
            "你是一名科考观察员，受命加入淮畔科创调研团。\n" +
            "此行将横跨三大主题——\n\n" +
            "淮河湿地生态保护区\n" +
            "皖北绿色智造工厂\n" +
            "长三角科创协同展厅\n\n" +
            "生态研究员、产业工程师、区域协同专员\n" +
            "将为你逐一讲解，并在每站设置挑战。\n\n" +
            "完成调研，解锁知识卡片，生成你的专属科考报告。";

        private bool isTyping = false;
        private Coroutine typingRoutine;

        private void Start()
        {
            if (btnBack != null)
                btnBack.onClick.AddListener(OnBackClick);

            if (btnSkip != null)
                btnSkip.onClick.AddListener(OnSkipClick);

            if (btnEnterGame != null)
            {
                var label = btnEnterGame.GetComponentInChildren<Text>();
                if (label != null) label.text = "正式开启科考";
                btnEnterGame.onClick.AddListener(OnEnterMainGameClick);
                btnEnterGame.gameObject.SetActive(false);
            }

            typingRoutine = StartCoroutine(ShowLoreTextRoutine(LORE_CONTENT));
        }

        private IEnumerator ShowLoreTextRoutine(string content)
        {
            isTyping = true;
            if (txtLoreText != null)
                txtLoreText.text = "";

            foreach (char c in content)
            {
                if (txtLoreText != null)
                    txtLoreText.text += c;
                yield return new WaitForSeconds(textTypingSpeed);
            }

            FinishPreview();
        }

        private void OnBackClick()
        {
            if (isTyping && typingRoutine != null)
                StopCoroutine(typingRoutine);
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToStartup));
        }

        private void OnSkipClick()
        {
            if (isTyping)
            {
                if (typingRoutine != null)
                    StopCoroutine(typingRoutine);

                if (txtLoreText != null)
                    txtLoreText.text = LORE_CONTENT;

                FinishPreview();
            }
        }

        private void OnEnterMainGameClick()
        {
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToMainGame));
        }

        private void FinishPreview()
        {
            isTyping = false;
            if (btnEnterGame != null)
                btnEnterGame.gameObject.SetActive(true);
            if (btnSkip != null)
                btnSkip.gameObject.SetActive(false);
        }
    }
}
