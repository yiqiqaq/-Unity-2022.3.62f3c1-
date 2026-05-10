using UnityEngine;
using UnityEngine.UI;
using Logic;
using Core;

namespace Presentation.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        private GameObject _pausePanel;
        private Text _txtSaveFeedback;
        private float _feedbackTimer;

        public void Build(Transform root)
        {
            // 暂停按钮（右上角）
            var btnPause = UIBuilder.CreateTextButton(root, "btnPause", "||",
                fontSize: 36, anchoredPos: new Vector2(-60, -30), sizeDelta: new Vector2(60, 60));
            var pauseRt = btnPause.GetComponent<RectTransform>();
            pauseRt.anchorMin = new Vector2(1, 1);
            pauseRt.anchorMax = new Vector2(1, 1);
            pauseRt.pivot = new Vector2(1, 1);
            pauseRt.anchoredPosition = new Vector2(-20, -20);
            btnPause.onClick.AddListener(TogglePause);

            // 暂停面板
            _pausePanel = new GameObject("PausePanel");
            _pausePanel.transform.SetParent(root, false);
            var panelRt = _pausePanel.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            _pausePanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);

            // 中央内容区
            var content = new GameObject("PauseContent");
            content.transform.SetParent(_pausePanel.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(400, 420);

            // 标题
            UIBuilder.CreateText(content.transform, "txtPauseTitle", "游 戏 暂 停", 40,
                TextAnchor.MiddleCenter, Color.white, new Vector2(0, 155));

            // 保存反馈文字
            _txtSaveFeedback = UIBuilder.CreateText(content.transform, "txtSaveFeedback", "", 24,
                TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.4f), new Vector2(0, 75));

            // 四个按钮
            var btnResume = UIBuilder.CreateTextButton(content.transform, "btnResume", "返回游戏", 28,
                new Vector2(0, 10), new Vector2(280, 55));
            btnResume.onClick.AddListener(Unpause);

            var btnSave = UIBuilder.CreateTextButton(content.transform, "btnSave", "保存进度", 28,
                new Vector2(0, -55), new Vector2(280, 55));
            btnSave.onClick.AddListener(OnSaveClick);

            var btnReturn = UIBuilder.CreateTextButton(content.transform, "btnReturn", "返回开始页", 28,
                new Vector2(0, -120), new Vector2(280, 55));
            btnReturn.onClick.AddListener(OnReturnClick);

            var btnExit = UIBuilder.CreateTextButton(content.transform, "btnExit", "退出游戏", 28,
                new Vector2(0, -185), new Vector2(280, 55));
            btnExit.onClick.AddListener(OnExitClick);

            _pausePanel.SetActive(false);
        }

        private void TogglePause()
        {
            if (_pausePanel.activeSelf)
                Unpause();
            else
                Pause();
        }

        private void Pause()
        {
            _pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void Unpause()
        {
            _pausePanel.SetActive(false);
            Time.timeScale = 1f;
            _txtSaveFeedback.text = "";
        }

        private void OnSaveClick()
        {
            GameManager.Instance.AutoSaveActiveAccount();
            _txtSaveFeedback.text = "进度已保存";
            _feedbackTimer = 2f;
        }

        private void OnReturnClick()
        {
            Time.timeScale = 1f;
            _pausePanel.SetActive(false);
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToStartup));
        }

        private void OnExitClick()
        {
            Time.timeScale = 1f;
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.QuitGame));
        }

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.unscaledDeltaTime;
                if (_feedbackTimer <= 0f)
                    _txtSaveFeedback.text = "";
            }
        }
    }
}
