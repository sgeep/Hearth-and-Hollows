using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
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

        IRandom m_Random = new SeededRandom();

        public float Scatter
        {
            get => m_Scatter;
            set => m_Scatter = Mathf.Max(0f, value);
        }

        public void Configure(HarvestRulesConfig rules, WeaponDefinition weapon, IngredientPickup pickupPrefab)
        {
            m_Rules = rules;
            m_Weapon = weapon;
            m_PickupPrefab = pickupPrefab;
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
                IngredientPickup pickup = Instantiate(m_PickupPrefab, e.Position + offset, Quaternion.identity);
                pickup.Initialize(drop.Item, drop.Count);
            }
        }
    }
}
