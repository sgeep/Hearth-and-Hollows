using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Applies <see cref="PlayerMoveConfig"/> to the TDE abilities, so feel is tuned in the
    /// config asset rather than on the prefab. In the editor, changes made in Play mode
    /// apply immediately.
    /// </summary>
    public sealed class PlayerTuning : MonoBehaviour
    {
        [SerializeField] PlayerMoveConfig m_Config;

        CharacterMovement m_Movement;
        CharacterDash2D m_Dash;

        float m_DodgeCooldownMultiplier = 1f;

        public PlayerMoveConfig Config => m_Config;

        /// <summary>A delve's run powers (4d step 4): 1 = the configured cooldown.</summary>
        public float DodgeCooldownMultiplier
        {
            get => m_DodgeCooldownMultiplier;
            set
            {
                m_DodgeCooldownMultiplier = Mathf.Max(0f, value);
                Apply();
            }
        }

        public void Configure(PlayerMoveConfig config) => m_Config = config;

        void Awake()
        {
            m_Movement = GetComponent<CharacterMovement>();
            m_Dash = GetComponent<CharacterDash2D>();
            Apply();
        }

#if UNITY_EDITOR
        void Update() => Apply();
#endif

        void Apply()
        {
            if (m_Config == null) return;
            if (m_Movement != null)
            {
                m_Movement.WalkSpeed = m_Config.walkSpeed;
                m_Movement.Acceleration = m_Config.acceleration;
                m_Movement.Deceleration = m_Config.deceleration;
            }
            if (m_Dash != null)
            {
                m_Dash.DashDistance = m_Config.dodgeDistance;
                m_Dash.DashDuration = m_Config.dodgeDuration;
                m_Dash.InvincibleWhileDashing = m_Config.dodgeInvulnerable;
                if (m_Dash.Cooldown != null)
                {
                    m_Dash.Cooldown.Unlimited = false;
                    // TDE lets a new dash cut the refill short by default: spamming the button skipped the cooldown.
                    m_Dash.Cooldown.CanInterruptRefill = false;
                    m_Dash.Cooldown.ConsumptionDuration = 0f;
                    m_Dash.Cooldown.PauseOnEmptyDuration = 0f;
                    m_Dash.Cooldown.RefillDuration = m_Config.dodgeCooldown * m_DodgeCooldownMultiplier;
                }
            }
        }
    }
}
