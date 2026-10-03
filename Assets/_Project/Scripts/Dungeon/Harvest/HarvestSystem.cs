using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Run;
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
        [SerializeField, Min(0f), Tooltip("How far drops scatter from where the enemy died, in tiles.")]
        float m_Scatter = 0.75f;
        [SerializeField, Tooltip("How fast parts spoil on the floor (the delve's freshness settings).")]
        FreshnessConfig m_Freshness;

        IRandom m_Random = new SeededRandom();

        public float Scatter
        {
            get => m_Scatter;
            set => m_Scatter = Mathf.Max(0f, value);
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
            foreach (HarvestDrop drop in drops)
            {
                EventBus<HarvestFeedback>.Publish(new HarvestFeedback(drop.Item, drop.Count, drop.Flags));
                if (drop.Destroyed || drop.Count <= 0 || m_PickupPrefab == null) continue;

                Vector2 offset = new Vector2(m_Random.Value() - 0.5f, m_Random.Value() - 0.5f) * (2f * m_Scatter);
                Spawn(drop.Item, drop.Count, Freshness.Max, e.Position + offset);
            }
        }
    }
}
