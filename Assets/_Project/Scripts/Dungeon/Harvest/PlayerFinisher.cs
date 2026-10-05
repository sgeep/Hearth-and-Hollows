using System.Collections;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Animation;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// The player's Harvest Finisher (4e step 3, GDD §4.3): the Finisher action (F, left trigger) on the nearest enemy in
    /// reach that can be finished (<see cref="FinisherTarget"/>). The player commits for <see cref="FinisherSettings.lockSeconds"/>
    /// (no moving, attacking or dodging, and no i-frames), the blow lands with its own feedback (a freeze, a shake, the
    /// <c>Finisher.Harvest</c> rumble), and the kill's parts come out Premium.
    /// </summary>
    public sealed class PlayerFinisher : MonoBehaviour
    {
        [SerializeField] HarvestRulesConfig m_Rules;
        [SerializeField, Tooltip("The finishing blow: freeze, shake, sound and the Finisher.Harvest rumble together.")]
        MMF_Player m_Feedback;

        CharacterAbility[] m_Abilities;
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;
        Health m_Health;

        public bool IsFinishing { get; private set; }
        /// <summary>Finishers landed (tests).</summary>
        public int Finished { get; private set; }

        FinisherSettings Settings => m_Rules != null ? m_Rules.finisher : FinisherSettings.Default;

        public void Configure(HarvestRulesConfig rules, MMF_Player feedback)
        {
            m_Rules = rules;
            m_Feedback = feedback;
        }

        void Awake()
        {
            m_Movement = GetComponent<CharacterMovement>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
            m_Health = GetComponent<Health>();
            m_Abilities = GetComponents<CharacterAbility>();
        }

        void Update()
        {
            if (IsFinishing) return;
            InputAction finisher = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Finisher);
            if (finisher != null && finisher.enabled && finisher.WasPressedThisFrame()) TryFinish();
        }

        /// <summary>The nearest enemy in reach that can be finished, if any.</summary>
        public FinisherTarget Nearest()
        {
            FinisherTarget best = null;
            float bestDistance = Settings.reach;
            foreach (FinisherTarget target in FindObjectsByType<FinisherTarget>(FindObjectsSortMode.None))
            {
                if (!target.IsEligible) continue;
                float d = Vector2.Distance(transform.position, target.transform.position);
                // A boss is big: its reach counts from its edge, roughly.
                if (target.IsBoss) d -= 0.8f;
                if (d <= bestDistance)
                {
                    best = target;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>Starts the finisher on the nearest eligible enemy. False if there's none in reach (or the player is down).</summary>
        public bool TryFinish()
        {
            if (IsFinishing || (m_Health != null && m_Health.CurrentHealth <= 0f)) return false;
            FinisherTarget target = Nearest();
            if (target == null) return false;
            StartCoroutine(Finish(target));
            return true;
        }

        IEnumerator Finish(FinisherTarget target)
        {
            IsFinishing = true;
            foreach (CharacterAbility ability in m_Abilities) ability.PermitAbility(false);
            m_Movement?.SetMovement(Vector2.zero);
            m_Animator?.PlayOneShot(CharacterAnim.HeavyAttack);
            FinisherSettings s = Settings;
            yield return new WaitForSeconds(s.strikeAt);
            if (target != null && target.Finish(gameObject))
            {
                Finished++;
                m_Feedback?.PlayFeedbacks(target.transform.position);
            }
            yield return new WaitForSeconds(Mathf.Max(0f, s.lockSeconds - s.strikeAt));
            foreach (CharacterAbility ability in m_Abilities) ability.PermitAbility(true);
            IsFinishing = false;
        }
    }
}
