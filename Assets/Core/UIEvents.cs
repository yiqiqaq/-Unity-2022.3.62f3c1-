using System;

namespace Core
{
    /// <summary>
    /// UI表现层抛给Logic层的路由意图事件
    /// </summary>
    public struct IntentEvent
    {
        public enum IntentType
        {
            GoToLogin,
            GoToRegister,
            GoToIntro,
            GoToMainGame,
            GoToStartup,
            LoadAccount,
            CreateAccount,
            QuitGame
        }

        public IntentType Type;
        public int PayloadInt;
        public string PayloadString;

        public IntentEvent(IntentType type, int payloadInt = -1, string payloadStr = "")
        {
            Type = type;
            PayloadInt = payloadInt;
            PayloadString = payloadStr;
        }
    }
}