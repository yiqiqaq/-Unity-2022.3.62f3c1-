using System;
using System.Collections.Generic;

namespace Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_events.TryGetValue(type, out var existing))
                _events[type] = Delegate.Combine(existing, handler);
            else
                _events[type] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_events.TryGetValue(type, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                    _events.Remove(type);
                else
                    _events[type] = result;
            }
        }

        public static void Trigger<T>(T eventData)
        {
            if (_events.TryGetValue(typeof(T), out var existing))
                (existing as Action<T>)?.Invoke(eventData);
        }

        public static void Clear()
        {
            _events.Clear();
        }
    }
}
