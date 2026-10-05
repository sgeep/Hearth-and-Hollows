using System.Collections.Generic;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>
    /// A boss's health (4e step 3): the hit that would kill it brings it **down** instead, at 1 health, for
    /// <see cref="FinisherSettings.bossDownedSeconds"/>: an optional moment for the Harvest Finisher (a better harvest and a
    /// bigger finish). Down, it can't be hurt; when the moment passes it falls as it would have. Health it never had to
    /// lose can't be skipped: only lethal damage brings it down. Extends TDE's <see cref="Health"/>.
    /// </summary>
    public class BossHealth : Health
    {
        [Header("Brought down")]
        [SerializeField] HarvestRulesConfig m_Rules;
        [SerializeField, Tooltip("Brought down (not killed) by lethal damage, for the finisher.")]
        bool m_Downable = true;

        float m_DownedUntil;
        GameObject m_LastInstigator;

        public bool IsDowned { get; private set; }
        /// <summary>It fell to the finisher (vs. the moment passing).</summary>
        public bool WasFinished { get; private set; }

        public void ConfigureDowned(HarvestRulesConfig rules) => m_Rules = rules;

        public override void Damage(float damage, GameObject instigator, float flickerDuration, float invincibilityDuration,
            Vector3 damageDirection, List<TypedDamage> typedDamages = null)
        {
            if (IsDowned) return;
            if (m_Downable && !Invulnerable && !ImmuneToDamage && CurrentHealth > 0f && damage >= CurrentHealth)
            {
                m_LastInstigator = instigator;
                float toOne = CurrentHealth - 1f;
                if (toOne > 0f) base.Damage(toOne, instigator, flickerDuration, invincibilityDuration, damageDirection, typedDamages);
                BringDown();
                return;
            }
            base.Damage(damage, instigator, flickerDuration, invincibilityDuration, damageDirection, typedDamages);
        }

        void BringDown()
        {
            IsDowned = true;
            float seconds = m_Rules != null ? m_Rules.finisher.bossDownedSeconds : FinisherSettings.Default.bossDownedSeconds;
            m_DownedUntil = Time.time + seconds;
            foreach (EnemyAttack attack in GetComponents<EnemyAttack>()) attack.Interrupt();
            GetComponent<ScrapEater>()?.StopEating();
            Character character = GetComponent<Character>();
            AIBrain brain = character != null ? character.CharacterBrain : GetComponent<AIBrain>();
            if (brain != null) brain.BrainActive = false;
            GetComponent<CharacterMovement>()?.SetMovement(Vector2.zero);
            CharacterSpriteAnimator animator = GetComponentInChildren<CharacterSpriteAnimator>();
            animator?.Hold(CharacterAnim.Hurt);
        }

        protected virtual void Update()
        {
            if (IsDowned && Time.time >= m_DownedUntil) FinishOff(m_LastInstigator, finisher: false);
        }

        /// <summary>Ends it: by the finisher, or because the moment passed (the normal death).</summary>
        public void FinishOff(GameObject by, bool finisher)
        {
            if (!IsDowned) return;
            IsDowned = false;
            m_Downable = false;
            WasFinished = finisher;
            GetComponentInChildren<CharacterSpriteAnimator>()?.Release();
            base.Damage(CurrentHealth, by != null ? by : gameObject, 0f, 0f, Vector3.zero);
        }
    }
}
