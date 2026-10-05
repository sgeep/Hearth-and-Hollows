using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// An enemy that can be harvested with the Harvest Finisher (4e step 3): while it's low and the player's last hit on it
    /// is fresh (<see cref="FinisherRules"/>), or a boss is down after lethal damage, a drumstick over its head says so.
    /// <see cref="Finish"/> kills it as a finisher: the harvest treats it as one (Premium parts, no overkill).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class FinisherTarget : MonoBehaviour
    {
        [SerializeField] HarvestRulesConfig m_Rules;
        [SerializeField, Tooltip("The drumstick over its head while it can be finished.")]
        GameObject m_Prompt;

        Health m_Health;
        HitReaction m_Reaction;
        BossHealth m_Boss;

        /// <summary>The killing blow was the finisher (the harvest reads this).</summary>
        public bool FinishingBlow { get; private set; }
        public bool IsEligible { get; private set; }
        public bool IsBoss => m_Boss != null;

        FinisherSettings Settings => m_Rules != null ? m_Rules.finisher : FinisherSettings.Default;

        public void Configure(HarvestRulesConfig rules, GameObject prompt)
        {
            m_Rules = rules;
            m_Prompt = prompt;
        }

        void Awake()
        {
            m_Health = GetComponent<Health>();
            m_Reaction = GetComponent<HitReaction>();
            m_Boss = m_Health as BossHealth;
            if (m_Prompt != null) m_Prompt.SetActive(false);
        }

        void Update()
        {
            bool eligible;
            if (m_Boss != null) eligible = m_Boss.IsDowned;
            else
            {
                float since = m_Reaction != null ? Time.time - m_Reaction.LastHitTime : float.MaxValue;
                eligible = FinisherRules.Eligible(m_Health.CurrentHealth, m_Health.MaximumHealth, since, Settings);
            }
            IsEligible = eligible && !FinishingBlow;
            if (m_Prompt != null && m_Prompt.activeSelf != IsEligible) m_Prompt.SetActive(IsEligible);
        }

        /// <summary>The finishing blow, from <paramref name="by"/>. False if it can't be finished now.</summary>
        public bool Finish(GameObject by)
        {
            if (!IsEligible || m_Health.CurrentHealth <= 0f) return false;
            FinishingBlow = true;
            IsEligible = false;
            if (m_Prompt != null) m_Prompt.SetActive(false);
            if (m_Boss != null) m_Boss.FinishOff(by, finisher: true);
            // Exactly its remaining health: no overkill to spoil the parts.
            else m_Health.Damage(m_Health.CurrentHealth, by, 0f, 0f, Vector3.zero);
            return true;
        }
    }
}
