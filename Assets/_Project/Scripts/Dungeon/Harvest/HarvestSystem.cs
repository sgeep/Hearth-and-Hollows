using System.Linq;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// Turns an enemy's death (bridged from TDE as <see cref="CharacterDied"/>) into harvest
    /// drops: the pure <see cref="HarvestRules"/> decide what the kill produced, and a pickup
    /// is spawned for each part that survived.
    /// </summary>
    public sealed class HarvestSystem : MonoBehaviour
    {
        [SerializeField] HarvestRulesConfig m_Rules;
        [SerializeField] WeaponDefinition m_Weapon;
        [SerializeField] IngredientPickup m_PickupPrefab;
        [SerializeField, Min(0f), Tooltip("How far drops pop out from where the enemy died, in tiles (the furthest; the nearest is 60% of it).")]
        float m_Scatter = 1.1f;
        [SerializeField, Min(0f), Tooltip("How long a drop's hop takes, in seconds. It can't be picked up until it lands.")]
        float m_PopTime = 0.35f;
        [SerializeField, Tooltip("How fast parts spoil on the floor (the delve's freshness settings).")]
        FreshnessConfig m_Freshness;
        [SerializeField, Tooltip("A clean kill: sound and the Kill.Clean haptic.")]
        MMF_Player m_CleanKillFeedback;
        [SerializeField, Tooltip("An overkill, or a part destroyed: a muted thud.")]
        MMF_Player m_OverkillFeedback;

        IRandom m_Random = new SeededRandom();

        public float Scatter
        {
            get => m_Scatter;
            set => m_Scatter = Mathf.Max(0f, value);
        }

        /// <summary>The kill moments' feedbacks: a clean kill, and an overkill or destroyed part.</summary>
        public void ConfigureFeedback(MMF_Player cleanKill, MMF_Player overkill)
        {
            m_CleanKillFeedback = cleanKill;
            m_OverkillFeedback = overkill;
        }

        /// <summary>The scene's harvest system, if it has one.</summary>
        public static HarvestSystem Instance { get; private set; }

        public void Configure(HarvestRulesConfig rules, WeaponDefinition weapon, IngredientPickup pickupPrefab, FreshnessConfig freshness = null)
        {
            m_Rules = rules;
            m_Weapon = weapon;
            m_PickupPrefab = pickupPrefab;
            m_Freshness = freshness;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Puts a stack on the floor at <paramref name="position"/> (a part swapped out of the satchel).
        /// <paramref name="droppedBy"/> won't pick it straight back up until they step off it.
        /// </summary>
        public IngredientPickup Drop(IngredientStack stack, Vector2 position, SatchelCarrier droppedBy)
        {
            if (stack.IsEmpty || m_PickupPrefab == null) return null;
            IngredientPickup pickup = Spawn(stack.Item, stack.Count, stack.Freshness, position);
            if (droppedBy != null) pickup.IgnoreUntilLeft(droppedBy);
            return pickup;
        }

        /// <summary>
        /// Where drop <paramref name="index"/> of <paramref name="count"/> lands: spread across the half
        /// circle in front of the body (towards the camera), so a large corpse can't hide it, and away from
        /// walls and props. Against a wall it tries the sides and behind at full distance before landing
        /// closer; with no room at all it lands where the enemy died.
        /// </summary>
        Vector2 LandingSpot(Vector2 origin, int index, int count)
        {
            if (m_Scatter <= 0f) return origin;
            int obstacles = LayerMask.GetMask(Hearthdelve.Core.Layers.Obstacles);
            float spread = count <= 1 ? 0.5f : index / (float)(count - 1);
            float angle = Mathf.Lerp(200f, 340f, spread) + (m_Random.Value() - 0.5f) * 30f;
            float reach = Mathf.Lerp(0.6f, 1f, m_Random.Value()) * m_Scatter;
            for (float distance = reach; distance > 0.2f; distance -= 0.25f)
            {
                // In front first, then turning towards the sides, then behind.
                foreach (float turn in new[] { 0f, 50f, -50f, 100f, -100f, 180f })
                {
                    float a = (angle + turn) * Mathf.Deg2Rad;
                    Vector2 spot = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * distance;
                    if (Physics2D.OverlapCircle(spot, 0.3f, obstacles) == null && Physics2D.Linecast(origin, spot, obstacles).collider == null) return spot;
                }
            }
            return origin;
        }

        IngredientPickup Spawn(IngredientItem item, int count, float freshness, Vector2 position)
        {
            IngredientPickup pickup = Instantiate(m_PickupPrefab, position, Quaternion.identity);
            pickup.Initialize(item, count, freshness);
            pickup.SetFreshnessRules(m_Freshness != null ? m_Freshness.freshness : FreshnessSettings.Default);
            return pickup;
        }

        /// <summary>Tests pass a seeded random for repeatable drops.</summary>
        public void SetRandom(IRandom random) => m_Random = random;

        void OnEnable() => EventBus<CharacterDied>.Subscribe(OnCharacterDied);
        void OnDisable() => EventBus<CharacterDied>.Unsubscribe(OnCharacterDied);

        void OnCharacterDied(CharacterDied e)
        {
            if (e.IsPlayer || e.Target == null || !e.Target.TryGetComponent(out EnemyIdentity identity)) return;
            EnemyDefinition definition = identity.Definition;
            if (definition == null) return;

            // The weapon that landed the killing hit; the scene's default weapon if none is known.
            WeaponDefinition weapon = e.Target.TryGetComponent(out HitReaction reaction) && reaction.LastHit is { Weapon: { } hitWeapon }
                ? hitWeapon
                : m_Weapon;
            var kill = new KillContext
            {
                Element = weapon != null ? weapon.element : default,
                CleanKillCategories = weapon != null ? weapon.cleanKillCategories : default,
                Overkill = e.Overkill,
                MaxHealth = e.MaxHealth,
                IsFinisher = false,
            };
            EventBus<EnemyKilled>.Publish(new EnemyKilled(definition, kill, e.Position));

            HarvestRuleSettings settings = m_Rules != null ? m_Rules.rules : HarvestRuleSettings.Default;
            List<HarvestDrop> drops = HarvestRules.Resolve(definition.harvest, kill, settings, m_Random);
            // The kill's own moment: a ruined harvest thuds; a clean one rings. A plain kill has the hit's feedback.
            if (drops.Any(d => (d.Flags & (HarvestFlags.Overkill | HarvestFlags.Destroyed)) != 0)) m_OverkillFeedback?.PlayFeedbacks(e.Position);
            else if (drops.Any(d => (d.Flags & HarvestFlags.CleanKill) != 0)) m_CleanKillFeedback?.PlayFeedbacks(e.Position);

            int landed = drops.Count(d => !d.Destroyed && d.Count > 0);
            int index = 0;
            foreach (HarvestDrop drop in drops)
            {
                EventBus<HarvestFeedback>.Publish(new HarvestFeedback(drop.Item, drop.Count, drop.Flags));
                if (drop.Destroyed || drop.Count <= 0 || m_PickupPrefab == null) continue;
                IngredientPickup pickup = Spawn(drop.Item, drop.Count, Freshness.Max, e.Position);
                pickup.PopTo(LandingSpot(e.Position, index++, landed), m_PopTime);
            }
        }
    }
}
