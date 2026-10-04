using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Run;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Essence
{
    /// <summary>
    /// The player's Essence, as TDE's <see cref="Health"/>: the only health pool (CLAUDE.md).
    /// An <see cref="EssenceMeter"/> holds the value; it drains over time in the dungeon and
    /// drops when TDE applies damage. At zero the character dies through TDE's normal death
    /// path, and <see cref="PlayerDefeated"/> is published.
    /// </summary>
    [AddComponentMenu("Hearthdelve/Dungeon/Essence Health")]
    public class EssenceHealth : Health
    {
        [Header("Essence")]
        [SerializeField] EssenceConfig m_Config;
        [SerializeField, Tooltip("Debug, for playtesting: no drain and no damage. Tick it while playing.")]
        bool m_GodMode;

        EssenceMeter m_Meter;
        float m_RunMaxBonus;
        bool m_Dying;
        bool m_ApplyingDamage;

        public EssenceMeter Essence => m_Meter;
        public EssenceConfig Config => m_Config;

        /// <summary>Debug: no drain and no damage.</summary>
        public bool GodMode
        {
            get => m_GodMode;
            set => m_GodMode = value;
        }

        /// <summary>Stops the time drain (menus, safe rooms). Damage still applies.</summary>
        public bool DrainPaused
        {
            get => m_Meter != null && m_Meter.Paused;
            set
            {
                if (m_Meter != null) m_Meter.Paused = value;
            }
        }

        public void Configure(EssenceConfig config) => m_Config = config;

        /// <summary>Builds a fresh meter (start of a delve, or a revive) and fills Essence.</summary>
        public override void InitializeCurrentHealth()
        {
            BuildMeter();
            MaximumHealth = m_Meter.Max;
            InitialHealth = m_Meter.Max;
            m_Dying = false;
            base.SetHealth(m_Meter.Current);
            PublishChanged(false);
        }

        void BuildMeter()
        {
            if (m_Meter != null)
            {
                m_Meter.Changed -= PublishChanged;
                m_Meter.Depleted -= OnDepleted;
            }

            EssenceSettings settings = m_Config != null ? m_Config.essence : EssenceSettings.Default;
            // Upgrades (max Essence) and breakfast (max Essence or slower drain) from the day loop.
            DelveLoadout loadout = GameFlow.Instance != null ? GameFlow.Instance.Loadout : DelveLoadout.None;
            m_Meter = new EssenceMeter(settings, new EssenceModifiers { MaxBonus = loadout.MaxEssenceBonus, DrainMultiplier = loadout.DrainMultiplier });
            m_Meter.Changed += PublishChanged;
            m_Meter.Depleted += OnDepleted;
            m_RunMaxBonus = 0f;
            ApplyRunModifiers(DelveRunController.CurrentModifiers, publish: false);
        }

        /// <summary>The run's powers (4d step 4): slower drain, lighter hits, and more max Essence (filled as it's added).</summary>
        public void ApplyRunModifiers(RunModifiers modifiers, bool publish = true)
        {
            if (m_Meter == null) return;
            m_Meter.RunDrainMultiplier = modifiers.DrainMultiplier;
            m_Meter.HitCostMultiplier = modifiers.HitCostMultiplier;
            float added = modifiers.MaxEssenceBonus - m_RunMaxBonus;
            if (added > 0f)
            {
                m_RunMaxBonus = modifiers.MaxEssenceBonus;
                m_Meter.RaiseMax(added);
                MaximumHealth = m_Meter.Max;
                base.SetHealth(m_Meter.Current);
            }
            if (publish) PublishChanged(false);
        }

        protected virtual void Update()
        {
            if (m_Meter == null || m_Dying || GodMode || m_Meter.IsDepleted) return;
            m_Meter.Tick(Time.deltaTime);
            if (CurrentHealth != m_Meter.Current) base.SetHealth(m_Meter.Current);
        }

        /// <summary>TDE sets health directly when it applies damage or healing; route it through the meter.</summary>
        public override void SetHealth(float newValue)
        {
            if (m_Meter == null)
            {
                base.SetHealth(newValue);
                return;
            }

            float delta = newValue - m_Meter.Current;
            if (delta < 0f)
            {
                if (!GodMode) m_Meter.TakeDamage(-delta);
            }
            else if (delta > 0f)
            {
                m_Meter.Restore(delta);
            }
            base.SetHealth(m_Meter.Current);
        }

        public override void Damage(float damage, GameObject instigator, float flickerDuration, float invincibilityDuration,
            Vector3 damageDirection, List<TypedDamage> typedDamages = null)
        {
            float invulnerability = m_Config != null ? Mathf.Max(invincibilityDuration, m_Config.postHitInvulnerability) : invincibilityDuration;
            // TDE raises its damage event and then kills at zero; keep that order.
            m_ApplyingDamage = true;
            try { base.Damage(damage, instigator, flickerDuration, invulnerability, damageDirection, typedDamages); }
            finally { m_ApplyingDamage = false; }
        }

        /// <summary>Field cooking and debug: restores Essence, up to the maximum.</summary>
        public void Restore(float amount) => ReceiveHealth(amount, gameObject);

        public override void Kill()
        {
            if (m_Dying || ImmuneToDamage) return;
            m_Dying = true;
            base.Kill();
            EventBus<PlayerDefeated>.Publish(new PlayerDefeated(DefeatReason.EssenceDepleted));
        }

        void OnDepleted()
        {
            // Drained to zero by time (damage reaches Kill through TDE's own path).
            if (!m_Dying && !m_ApplyingDamage)
            {
                CurrentHealth = 0f;
                Kill();
            }
        }

        void PublishChanged(bool fromDamage) =>
            EventBus<EssenceChanged>.Publish(new EssenceChanged(m_Meter.Current, m_Meter.Max, m_Meter.IsLow, fromDamage));
    }
}
