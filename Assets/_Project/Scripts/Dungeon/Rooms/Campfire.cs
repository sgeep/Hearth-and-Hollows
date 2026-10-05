using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Run;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// The campfire before the boss (4e playtest): lit in the room that leads to the arena once it's clear. Standing by it
    /// gives back Essence, a share of the player's maximum (<see cref="CampfireSettings"/>), over a few seconds; stepping
    /// away pauses it, and once it's given everything it burns low. Resting commits nothing: the rope is still a door away.
    /// </summary>
    public sealed class Campfire : MonoBehaviour
    {
        [SerializeField] Light2D m_Light;
        [SerializeField] SpriteRenderer m_Fire;
        [SerializeField, Tooltip("Solid ring of stones; enabled once the player isn't standing on it.")]
        Collider2D m_Body;
        [SerializeField, Tooltip("First warmth: the crackle, a soft pulse.")]
        MMF_Player m_WarmFeedback;
        [SerializeField, Tooltip("Burnt low: everything given.")]
        MMF_Player m_SpentFeedback;

        CampfireSettings m_Settings = new();
        float m_Pool = -1f;
        float m_FullIntensity;
        bool m_Warmed;

        /// <summary>Essence it still has to give, once someone has warmed by it (-1 before).</summary>
        public float Remaining => m_Pool;
        public bool IsSpent => m_Pool == 0f;

        public void Configure(Light2D light, SpriteRenderer fire, Collider2D body, MMF_Player warm, MMF_Player spent)
        {
            m_Light = light;
            m_Fire = fire;
            m_Body = body;
            m_WarmFeedback = warm;
            m_SpentFeedback = spent;
        }

        public void Setup(CampfireSettings settings) => m_Settings = settings ?? new CampfireSettings();

        void Awake()
        {
            if (m_Light != null) m_FullIntensity = m_Light.intensity;
            if (m_Body != null) m_Body.enabled = false;
        }

        void Update()
        {
            if (m_Light != null && !IsSpent)
                m_Light.intensity = m_FullIntensity * (0.9f + 0.1f * Mathf.Sin(Time.time * 9f) * Mathf.Sin(Time.time * 4.3f));

            Character player = LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0
                ? LevelManager.Instance.Players[0] : null;
            if (player == null) return;
            Vector2 feet = player.transform.position;
            float distance = Vector2.Distance(feet, transform.position);
            // The stones turn solid once nobody is standing in them (it can appear under the player's feet).
            if (m_Body != null && !m_Body.enabled && distance > 1f) m_Body.enabled = true;

            if (IsSpent || distance > m_Settings.radius) return;
            DelveRunController run = DelveRunController.Active;
            if (run != null && run.IsEnding) return;
            if (!player.TryGetComponent(out EssenceHealth essence) || essence.CurrentHealth <= 0f) return;
            if (m_Pool < 0f) m_Pool = essence.MaximumHealth * m_Settings.restoreFraction;

            float missing = essence.MaximumHealth - essence.CurrentHealth;
            float amount = CampfireRules.Warm(m_Pool, essence.MaximumHealth * m_Settings.restoreFraction, m_Settings.restoreSeconds, Time.deltaTime, missing);
            if (amount <= 0f) return;
            if (!m_Warmed)
            {
                m_Warmed = true;
                m_WarmFeedback?.PlayFeedbacks(transform.position);
            }
            essence.Restore(amount);
            m_Pool = Mathf.Max(0f, m_Pool - amount);
            if (m_Pool <= 0.01f) BurnLow();
        }

        void BurnLow()
        {
            m_Pool = 0f;
            if (m_Light != null) m_Light.intensity = m_FullIntensity * 0.35f;
            if (m_Fire != null) m_Fire.color = new Color(0.55f, 0.45f, 0.4f);
            m_SpentFeedback?.PlayFeedbacks(transform.position);
        }
    }

    /// <summary>The campfire's tuning, in <see cref="RunTuning"/>.</summary>
    [System.Serializable]
    public sealed class CampfireSettings
    {
        [Tooltip("Lit in the room before the boss's arena once it's clear.")]
        public bool enabled = true;
        [Range(0f, 1f), Tooltip("Essence it gives back in all, as a share of the player's maximum.")]
        public float restoreFraction = 0.5f;
        [Min(0.1f), Tooltip("Seconds of standing by it to receive all of it.")]
        public float restoreSeconds = 2.5f;
        [Min(0.5f), Tooltip("How close counts as by the fire, in tiles.")]
        public float radius = 1.7f;
    }

    /// <summary>Pure rule for the campfire's warmth.</summary>
    public static class CampfireRules
    {
        /// <summary>
        /// Essence given this frame: its whole gift (<paramref name="total"/>) spread over <paramref name="seconds"/>, never more
        /// than it has left (<paramref name="pool"/>) or than the player is missing (so a nearly full delver doesn't waste it).
        /// </summary>
        public static float Warm(float pool, float total, float seconds, float deltaTime, float missing)
        {
            if (pool <= 0f || missing <= 0f || deltaTime <= 0f) return 0f;
            float rate = total / Mathf.Max(0.01f, seconds);
            return Mathf.Min(pool, missing, rate * deltaTime);
        }
    }
}
