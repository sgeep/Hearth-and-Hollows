using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using UnityEngine;

namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// Everything the day loop needs to look up by id (saves store ids), plus new-game and debug
    /// settings. Content stays modular: new ingredients and upgrades are added here as data.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        public List<IngredientDefinition> ingredients = new();
        [Tooltip("Bought at Night, in display order.")]
        public List<TavernUpgradeDefinition> upgrades = new();
        public FreshnessConfig freshness;

        [Header("New game")]
        [Min(0)] public int newGameGold;

        [Header("Debug")]
        [Tooltip("Lets F4 / the prep-screen button fill the storeroom during the day loop. Off: service uses only what you bring back.")]
        public bool allowDebugFill;

        public FreshnessSettings Freshness => freshness != null ? freshness.freshness : FreshnessSettings.Default;

        public IngredientDefinition Ingredient(string id)
        {
            foreach (var i in ingredients) if (i != null && i.id == id) return i;
            return null;
        }

        public TavernUpgradeDefinition Upgrade(string id)
        {
            foreach (var u in upgrades) if (u != null && u.id == id) return u;
            return null;
        }

        public UpgradeEffects Effects(GameState state) => Upgrades.Effects(upgrades, state.UpgradeLevel);

        public DelveLoadout Loadout(GameState state) => DelveLoadout.From(Effects(state), state.Meal);
    }
}
