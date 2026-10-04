using System;
using UnityEngine;

namespace Hearthdelve.Core.Input
{
    /// <summary>Tuning for <see cref="StickReleaseFilter"/>.</summary>
    [Serializable]
    public struct StickReleaseSettings
    {
        [Range(0f, 1f), Tooltip("A push at least this far is a real push; it passes at once, whichever way it points.")]
        public float strongPush;
        [Min(0f), Tooltip("Seconds after the last real push during which a weaker push the other way is treated as the stick springing back.")]
        public float springBackWindow;

        public static StickReleaseSettings Default => new() { strongPush = 0.5f, springBackWindow = 0.15f };
    }

    /// <summary>
    /// Removes a released stick's spring-back from movement input. Let go of a pushed stick and it snaps back past
    /// centre for a frame or two, the other way; read as movement, that turns the character round as it stops (the
    /// 4c step 5 playtest: a flick down-left could end facing up-right). A weak push pointing away from a real push
    /// made moments ago reads as rest; a real push in any direction passes at once, so turning round is delayed by a
    /// frame or two at most, and a gentle push the other way passes once the window is over. Keyboards never trip
    /// it (their pushes are full strength). Pure logic.
    /// </summary>
    public sealed class StickReleaseFilter
    {
        readonly StickReleaseSettings m_Settings;
        Vector2 m_LastStrong;
        float m_LastStrongTime = float.NegativeInfinity;

        public StickReleaseFilter(StickReleaseSettings settings) => m_Settings = settings;

        /// <summary>The movement to use for <paramref name="raw"/> stick input read at <paramref name="time"/> (seconds).</summary>
        public Vector2 Filter(Vector2 raw, float time)
        {
            float magnitude = raw.magnitude;
            if (magnitude >= m_Settings.strongPush && magnitude > 0f)
            {
                m_LastStrong = raw / magnitude;
                m_LastStrongTime = time;
                return raw;
            }
            bool springingBack = magnitude > 0f && time - m_LastStrongTime <= m_Settings.springBackWindow && Vector2.Dot(raw / magnitude, m_LastStrong) < 0f;
            return springingBack ? Vector2.zero : raw;
        }
    }
}
