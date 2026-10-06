using System.Collections.Generic;
using Hearthdelve.Shared.Customization;
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
        [Tooltip("Every furniture piece saves and layouts may name (4f).")]
        public List<FurnitureDefinition> furniture = new();
        [Tooltip("What a new game's property starts with; also granted to saves from before furniture.")]
        public FurnitureStartingLayout startingFurniture;
        [Tooltip("Floor and wall finishes (4f, D5).")]
        public List<FinishDefinition> finishes = new();
        [Tooltip("Palette ramps and presets for recolouring (D11).")]
        public PaletteLibrary palettes;
        [Tooltip("Each boss's first-clear trophy (4f, D10): a unique furnishing, never lost.")]
        public List<BossTrophy> bossTrophies = new();
        [Tooltip("The Brackenford market (4f Checkpoint C, D19): what the daytime market list sells.")]
        public Hearthdelve.Shared.Inventory.SupplySource market;
        [Tooltip("The catalogue's Renown tiers (D14).")]
        public CatalogSettings catalog;

        [Tooltip("Quest objects the Hollows can hold (4g Checkpoint B).")]
        public List<Quests.QuestObjectDefinition> questObjects = new();
        [Tooltip("The keeper's bodies and colourways (4g Checkpoint B): the creator and the keeper in play read them.")]
        public Characters.KeeperLooks keeperLooks;

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

        Dictionary<string, FurnitureDefinition> m_FurnitureById;

        public FurnitureDefinition Furniture(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (m_FurnitureById == null || m_FurnitureById.Count != furniture.Count)
            {
                m_FurnitureById = new Dictionary<string, FurnitureDefinition>();
                foreach (var f in furniture)
                    if (f != null && !string.IsNullOrEmpty(f.id)) m_FurnitureById[f.id] = f;
            }
            return m_FurnitureById.TryGetValue(id, out var found) ? found : null;
        }

        public Quests.QuestObjectDefinition QuestObject(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var q in questObjects) if (q != null && q.id == id) return q;
            return null;
        }

        public FinishDefinition Finish(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var f in finishes) if (f != null && f.id == id) return f;
            return null;
        }

        /// <summary>The Renown each catalogue tier needs.</summary>
        public int[] CatalogThresholds() => catalog != null ? catalog.Thresholds() : new[] { 0, 25, 60, 100 };

        public UpgradeEffects Effects(GameState state) => Upgrades.Effects(upgrades, state.UpgradeLevel);

        public DelveLoadout Loadout(GameState state) => DelveLoadout.From(Effects(state), state.Meal);
    }
}
