using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Animation;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>
    /// The Larder Troll's appetite (4e step 2): when parts lie on the floor it walks to the nearest and eats it
    /// (<see cref="FeedingSettings.eatSeconds"/>), healing a share of its health. The player can take the part first
    /// (it's an ordinary part for the satchel), or hit the troll hard while it eats: enough damage
    /// (<see cref="FeedingSettings.spoilDamage"/>) spoils the meal: the part is ruined and nothing is healed. Its TDE
    /// brain drives the walking and the eating through thin AI actions; the rules are <see cref="FeedingRules"/>.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class ScrapEater : MonoBehaviour
    {
        [SerializeField] BossDefinition m_Boss;
        [SerializeField, Tooltip("A meal finished: the gulp, a green flash.")]
        MMF_Player m_GulpFeedback;
        [SerializeField, Tooltip("A meal spoiled by the player's hits: the squelch.")]
        MMF_Player m_SpoilFeedback;
        [SerializeField, Min(0.1f), Tooltip("It eats a part from this close, in tiles.")]
        float m_EatReach = 0.9f;

        Health m_Health;
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;
        IngredientPickup m_Meal;
        float m_EatLeft;
        float m_HealthAtStart;
        float m_ReadyAt;
        float m_Best;
        float m_StuckFor;
        // Parts it gave up on (out of its reach): never chased again.
        readonly System.Collections.Generic.HashSet<IngredientPickup> m_Unreachable = new();

        public bool IsEating { get; private set; }
        /// <summary>The part it's going for or eating, if any.</summary>
        public IngredientPickup Food { get; private set; }
        public int Eaten { get; private set; }
        public int Spoiled { get; private set; }
        public int GaveUp { get; private set; }
        /// <summary>The walk to its part: the closest it has been, and how long since it got closer (for tests and tuning).</summary>
        public Vector2 Approaching => new(m_Best, m_StuckFor);

        FeedingSettings Settings => m_Boss != null ? m_Boss.feeding : null;

        public void Configure(BossDefinition boss, MMF_Player gulp, MMF_Player spoil)
        {
            m_Boss = boss;
            m_GulpFeedback = gulp;
            m_SpoilFeedback = spoil;
        }

        void Awake()
        {
            m_Health = GetComponent<Health>();
            m_Movement = GetComponent<CharacterMovement>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
        }

        /// <summary>Whether it wants a part now: hungry (off cooldown), alive, and a part lies on the floor. Picks the nearest.</summary>
        public bool WantsFood()
        {
            FeedingSettings s = Settings;
            if (s == null || !s.enabled || m_Health.CurrentHealth <= 0f || Time.time < m_ReadyAt) return false;
            if (Food != null && Food.Count > 0 && Food.IsCollectable) return true;
            Food = Nearest();
            m_Best = Food != null ? Vector2.Distance(transform.position, Food.transform.position) : 0f;
            m_StuckFor = 0f;
            return Food != null;
        }

        public bool FoodGone => Food == null || Food.Count <= 0;
        public bool InReach => Food != null && Vector2.Distance(transform.position, Food.transform.position) <= m_EatReach;

        IngredientPickup Nearest()
        {
            IngredientPickup best = null;
            float bestDistance = float.MaxValue;
            foreach (IngredientPickup pickup in FindObjectsByType<IngredientPickup>(FindObjectsSortMode.None))
            {
                if (pickup.Count <= 0 || !pickup.IsCollectable || m_Unreachable.Contains(pickup)) continue;
                float d = Vector2.Distance(transform.position, pickup.transform.position);
                if (d < bestDistance)
                {
                    best = pickup;
                    bestDistance = d;
                }
            }
            return best;
        }

        /// <summary>
        /// Called each frame it walks to its part (the Feed state). If it stops getting closer it gives up on that part, so it
        /// never grinds against a pillar: back to the fight, and it won't try that part again.
        /// </summary>
        public void Approach(float deltaTime)
        {
            FeedingSettings s = Settings;
            if (FoodGone || s == null || IsEating) return;
            float distance = Vector2.Distance(transform.position, Food.transform.position);
            if (!FeedingRules.Stalled(ref m_Best, ref m_StuckFor, distance, deltaTime, s.progressStep, s.giveUpSeconds)) return;
            GaveUp++;
            m_Unreachable.Add(Food);
            Food = null;
            m_ReadyAt = Time.time + s.eatCooldown;
        }

        /// <summary>Starts eating the part in reach (the AI's Eat state).</summary>
        public void BeginEating()
        {
            if (FoodGone || Settings == null) return;
            IsEating = true;
            m_Meal = Food;
            m_EatLeft = Settings.eatSeconds;
            m_HealthAtStart = m_Health.CurrentHealth;
            m_Movement?.SetMovement(Vector2.zero);
            m_Animator?.Hold(CharacterAnim.Eat);
        }

        /// <summary>Stops eating without finishing (the state was left: the part was taken, or the boss died).</summary>
        public void StopEating()
        {
            if (!IsEating) return;
            IsEating = false;
            m_Animator?.Release();
        }

        void Update()
        {
            if (!IsEating) return;
            FeedingSettings s = Settings;
            if (m_Health.CurrentHealth <= 0f || m_Meal == null || m_Meal.Count <= 0)
            {
                // The player took it, or the troll fell.
                StopEating();
                return;
            }
            if (FeedingRules.Spoiled(m_HealthAtStart, m_Health.CurrentHealth, s.spoilDamage))
            {
                // Hit hard enough to spoil it: the part is ruined, nothing healed.
                Spoiled++;
                m_SpoilFeedback?.PlayFeedbacks(m_Meal.transform.position);
                Destroy(m_Meal.gameObject);
                Finish();
                return;
            }
            m_EatLeft -= Time.deltaTime;
            if (m_EatLeft > 0f) return;
            Eaten++;
            float heal = FeedingRules.Heal(m_Health.CurrentHealth, m_Health.MaximumHealth, s.healFraction);
            if (heal > 0f) m_Health.ReceiveHealth(heal, gameObject);
            m_GulpFeedback?.PlayFeedbacks(transform.position);
            Destroy(m_Meal.gameObject);
            Finish();
        }

        void Finish()
        {
            m_Meal = null;
            Food = null;
            m_ReadyAt = Time.time + (Settings != null ? Settings.eatCooldown : 0f);
            StopEating();
        }
    }
}
