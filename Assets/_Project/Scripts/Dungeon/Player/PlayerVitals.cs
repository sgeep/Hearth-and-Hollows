using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// The player's Essence (their only health pool). Drains in scaled time — so hit-stop and
    /// menus pause it — and on hits. At zero, publishes <see cref="PlayerDefeated"/>.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerVitals : MonoBehaviour, IDamageable
    {
        [SerializeField] EssenceConfig m_Config;

        PlayerController m_Controller;
        readonly Countdown m_Invulnerable = new();
        readonly Countdown m_Stun = new();

        public EssenceMeter Essence { get; private set; }
        public Team Team => Team.Player;
        public bool IsDefeated => Essence != null && Essence.IsDepleted;
        public bool IsStunned => m_Stun.IsRunning;
        public bool IsInvulnerable => m_Invulnerable.IsRunning || (m_Controller.Motor?.IsInvulnerable ?? false);
        public bool GodMode { get; set; }

        public void Configure(EssenceConfig config) => m_Config = config;

        void Awake()
        {
            m_Controller = GetComponent<PlayerController>();
            if (m_Config == null)
            {
                Debug.LogError("PlayerVitals needs an EssenceConfig.", this);
                enabled = false;
                return;
            }
            // Upgrades (max Essence) and breakfast (max Essence or slower drain) from the day loop.
            var loadout = GameFlow.Instance != null ? GameFlow.Instance.Loadout : DelveLoadout.None;
            Essence = new EssenceMeter(m_Config.essence, new EssenceModifiers { MaxBonus = loadout.MaxEssenceBonus, DrainMultiplier = loadout.DrainMultiplier });
            Essence.Changed += OnEssenceChanged;
            Essence.Depleted += OnDepleted;
        }

        void Start() => OnEssenceChanged(false);

        void Update()
        {
            if (!GodMode) Essence.Tick(Time.deltaTime);
        }

        void FixedUpdate()
        {
            m_Invulnerable.Tick(Time.fixedDeltaTime);
            m_Stun.Tick(Time.fixedDeltaTime);
        }

        public bool ReceiveHit(in DamageInfo hit)
        {
            if (!enabled || IsDefeated || IsInvulnerable) return false;

            if (!GodMode) Essence.TakeDamage(hit.Amount);
            if (IsDefeated) return true;

            m_Invulnerable.Start(m_Config.postHitInvulnerability);
            m_Stun.Start(m_Config.hitStunTime);

            float away = Mathf.Abs(hit.Knockback.x) > 0.01f
                ? Mathf.Sign(hit.Knockback.x)
                : Mathf.Sign(transform.position.x - hit.HitPoint.x);
            if (away == 0f) away = -m_Controller.Facing;
            m_Controller.Motor.ApplyKnockback(new Vector2(away * m_Config.hitKnockback.x, m_Config.hitKnockback.y), m_Config.hitStunTime);
            m_Controller.Combo.Cancel();

            EventBus<HitStopRequested>.Publish(new HitStopRequested(m_Config.hitStop));
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(m_Config.hitScreenShake));
            return true;
        }

        /// <summary>Debug/field-cooking hook.</summary>
        public void Restore(float amount) => Essence.Restore(amount);

        void OnEssenceChanged(bool fromDamage) =>
            EventBus<EssenceChanged>.Publish(new EssenceChanged(Essence.Current, Essence.Max, Essence.IsLow, fromDamage));

        void OnDepleted() => EventBus<PlayerDefeated>.Publish(new PlayerDefeated(DefeatReason.EssenceDepleted));
    }
}
