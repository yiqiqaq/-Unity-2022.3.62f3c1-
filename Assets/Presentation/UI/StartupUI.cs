using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Core;

namespace Presentation.UI
{
    /// <summary>
    /// 启动页 —— 淮畔科创行 · 标题画面
    /// 三个功能选项：选择存档、了解本次科考、告别淮畔
    /// </summary>
    public class StartupUI : MonoBehaviour
    {
        [Header("标题区")]
        public Text txtTitle;

        [Header("底部功能键 (纯文字)")]
        public Button btnSelectSave;
        public Button btnIntro;
        public Button btnQuit;

        [Header("退出确认弹窗")]
        public GameObject quitPopupPanel;
        public Text txtQuitPrompt;
        public Button btnConfirmQuit;
        public Button btnCancelQuit;

        private void Start()
        {
            Debug.Log("[StartupUI] Start() 执行");

            // 诊断: 检查 EventSystem 是否存在
            var es = FindObjectOfType<EventSystem>();
            Debug.Log($"[StartupUI] EventSystem 存在: {es != null}, 名称: {(es != null ? es.name : "NULL")}");
            if (es != null)
            {
                var input = es.GetComponent<StandaloneInputModule>();
                Debug.Log($"[StartupUI] StandandaloneInputModule 存在: {input != null}, enabled: {(input != null ? input.enabled.ToString() : "N/A")}");
            }

            // 标题
            if (txtTitle != null)
                txtTitle.text = "淮畔科创行";

            // 功能键
            if (btnSelectSave != null)
            {
                btnSelectSave.GetComponentInChildren<Text>().text = "选择存档";
                btnSelectSave.onClick.AddListener(OnSelectSaveClick);
                Debug.Log("[StartupUI] btnSelectSave 已注册点击监听");
            }
            else
            {
                Debug.LogError("[StartupUI] btnSelectSave 为 NULL，无法注册监听!");
            }

            if (btnIntro != null)
            {
                btnIntro.GetComponentInChildren<Text>().text = "了解本次科考";
                btnIntro.onClick.AddListener(OnIntroClick);
                Debug.Log("[StartupUI] btnIntro 已注册点击监听");
            }
            else
            {
                Debug.LogError("[StartupUI] btnIntro 为 NULL，无法注册监听!");
            }

            if (btnQuit != null)
            {
                btnQuit.GetComponentInChildren<Text>().text = "告别淮畔";
                btnQuit.onClick.AddListener(OnQuitClick);
            }

            // 退出弹窗
            if (quitPopupPanel != null)
                quitPopupPanel.SetActive(false);

            if (txtQuitPrompt != null)
                txtQuitPrompt.text = "此去山高水长，后会有期？";

            if (btnConfirmQuit != null)
                btnConfirmQuit.onClick.AddListener(OnConfirmQuit);

            if (btnCancelQuit != null)
                btnCancelQuit.onClick.AddListener(OnCancelQuit);

            // 诊断: 检查 TransitionManager 是否可用
            Debug.Log($"[StartupUI] TransitionManager.Instance: {(Logic.TransitionManager.Instance != null ? "OK" : "NULL!")}");
        }

        private void Update()
        {
            // 诊断: 检测鼠标点击是否被 EventSystem 捕获
            if (Input.GetMouseButtonDown(0))
            {
                var es = FindObjectOfType<EventSystem>();
                var ped = new PointerEventData(es);
                ped.position = Input.mousePosition;
                var results = new System.Collections.Generic.List<RaycastResult>();
                if (es != null)
                    es.RaycastAll(ped, results);
                Debug.Log($"[StartupUI] 鼠标点击位置: {Input.mousePosition}, Raycast 命中 {results.Count} 个对象");
                foreach (var r in results)
                {
                    Debug.Log($"  -> 命中: {r.gameObject.name} (layer={r.gameObject.layer}, distance={r.distance})");
                }
            }
        }

        private void OnSelectSaveClick()
        {
            Debug.Log("[StartupUI] >>> OnSelectSaveClick 被调用!");
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToLogin));
            Debug.Log("[StartupUI] EventBus.Trigger(GoToLogin) 已执行");
        }

        private void OnIntroClick()
        {
            Debug.Log("[StartupUI] >>> OnIntroClick 被调用!");
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.GoToIntro));
            Debug.Log("[StartupUI] EventBus.Trigger(GoToIntro) 已执行");
        }

        private void OnQuitClick()
        {
            if (quitPopupPanel != null)
                quitPopupPanel.SetActive(true);
        }

        private void OnCancelQuit()
        {
            if (quitPopupPanel != null)
                quitPopupPanel.SetActive(false);
        }

        private void OnConfirmQuit()
        {
            EventBus.Trigger(new IntentEvent(IntentEvent.IntentType.QuitGame));
        }
    }
}
