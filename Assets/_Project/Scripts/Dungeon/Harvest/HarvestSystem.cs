using Hearthdelve.Core.Events;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// Listens for <see cref="EnemyKilled"/>, resolves drops with <see cref="HarvestRules"/>,
    /// spawns pickups, and reports each outcome for the HUD's harvest feed.
    /// </summary>
    public sealed class HarvestSystem : MonoBehaviour
    {
        [SerializeField] HarvestRulesConfig m_Rules;
        [SerializeField] IngredientPickup m_PickupPrefab;
        [SerializeField, Tooltip("0 = random each run. Fixed seeds reproduce drops for debugging.")]
        int m_Seed;

        IRandom m_Random;

        public static HarvestSystem Instance { get; private set; }

        /// <summary>Current rules (the enemy clean-kill cue uses the overkill threshold).</summary>
        public HarvestRuleSettings Rules => m_Rules != null ? m_Rules.rules : HarvestRuleSettings.Default;

        public void Configure(HarvestRulesConfig rules, IngredientPickup pickupPrefab)
        {
            m_Rules = rules;
            m_PickupPrefab = pickupPrefab;
        }

        void Awake()
        {
            Instance = this;
            m_Random = m_Seed != 0 ? new SeededRandom(m_Seed) : new SeededRandom();
        }

        void OnEnable() => EventBus<EnemyKilled>.Subscribe(OnEnemyKilled);
        void OnDisable() => EventBus<EnemyKilled>.Unsubscribe(OnEnemyKilled);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnemyKilled(EnemyKilled evt)
        {
            if (evt.Definition == null || m_Rules == null) return;
            var drops = HarvestRules.Resolve(evt.Definition.harvest, evt.Kill, m_Rules.rules, m_Random);
            foreach (var drop in drops)
            {
                EventBus<HarvestFeedback>.Publish(new HarvestFeedback(drop.Item, drop.Count, drop.Flags));
                if (drop.Count > 0)
                    Spawn(new IngredientStack(drop.Item, drop.Count), evt.Position, new Vector2((m_Random.Value() - 0.5f) * 6f, 6f + m_Random.Value() * 3f));
            }
        }

        public IngredientPickup Spawn(IngredientStack stack, Vector2 position, Vector2 velocity)
        {
            if (m_PickupPrefab == null || stack.IsEmpty || !stack.Item.IsValid) return null;
            var pickup = Instantiate(m_PickupPrefab, position, Quaternion.identity);
            pickup.Initialize(stack, velocity);
            return pickup;
        }
    }
}
