using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Core.Events
{
    /// <summary>Marker for payloads carried on the <see cref="EventBus{T}"/>.</summary>
    public interface IEvent { }

    /// <summary>
    /// Lightweight typed event bus. Systems publish value-type events and never hold
    /// references across the Dungeon/Tavern boundary.
    /// </summary>
    public static class EventBus<T> where T : struct, IEvent
    {
        static readonly List<Action<T>> s_Handlers = new();
        static bool s_Registered;

        public static int HandlerCount => s_Handlers.Count;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || s_Handlers.Contains(handler)) return;
            if (!s_Registered)
            {
                EventBusRegistry.Register(Clear);
                s_Registered = true;
            }
            s_Handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler) => s_Handlers.Remove(handler);

        public static void Publish(T evt)
        {
            // Iterate a snapshot so handlers may unsubscribe (or subscribe) while handling.
            var snapshot = s_Handlers.ToArray();
            foreach (var handler in snapshot)
            {
                try { handler(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public static void Clear() => s_Handlers.Clear();
    }

    /// <summary>Tracks every bus instantiation so all can be cleared together.</summary>
    public static class EventBusRegistry
    {
        static readonly List<Action> s_Clearers = new();

        internal static void Register(Action clear) => s_Clearers.Add(clear);

        /// <summary>Removes every handler on every bus. Called on play-mode entry and by tests.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearAll()
        {
            foreach (var clear in s_Clearers) clear();
        }
    }
}
