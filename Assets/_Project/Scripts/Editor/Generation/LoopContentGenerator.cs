using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Phase 3 data: the three upgrades, breakfast buffs on the Grill/Tap recipes, and the
    /// GameDatabase. Like earlier phases, assets are only created, never overwritten; recipe
    /// buffs are filled in only where a recipe has none yet, so tuning is kept.
    /// </summary>
    public static class LoopContentGenerator
    {
        public const string UpgradesFolder = EditorPaths.Data + "/Upgrades";
        public const string DatabasePath = EditorPaths.Data + "/GameDatabase.asset";

        public static GameDatabase Generate(ContentGenerator.Content phase1, TavernContent tavern)
        {
            EditorPaths.Ensure(UpgradesFolder);

            // Starting costs aim at GDD §7.3: a decent delve plus service (~100–150 gold) buys about one level.
            var upgrades = new List<TavernUpgradeDefinition>
            {
                Upgrade("satchel_slots", "Bigger Satchel", UpgradeKind.SatchelSlots, (100, 1), (180, 1), (280, 1)),
                Upgrade("max_essence", "Deeper Reserves", UpgradeKind.MaxEssence, (80, 20), (150, 20), (240, 20)),
                Upgrade("tavern_seats", "Extra Seating", UpgradeKind.Seats, (120, 1), (220, 1)),
            };

            // Hearty grill food adds Essence; tap drinks slow its drain. Stews aren't breakfast.
            var buffs = new Dictionary<string, (MealBuffKind kind, float amount)>
            {
                ["grilled_haunch"] = (MealBuffKind.MaxEssence, 20f),
                ["shroom_skewer"] = (MealBuffKind.MaxEssence, 15f),
                ["cellar_kebab"] = (MealBuffKind.MaxEssence, 25f),
                ["gelbrew"] = (MealBuffKind.SlowerDrain, 0.2f),
                ["core_tonic"] = (MealBuffKind.SlowerDrain, 0.3f),
            };
            foreach (var r in tavern.recipes)
            {
                if (r == null || r.mealBuff.kind != MealBuffKind.None || !buffs.TryGetValue(r.id, out var b)) continue;
                r.mealBuff = new MealBuffSettings { kind = b.kind, amount = b.amount };
                EditorUtility.SetDirty(r);
            }

            var db = ContentGenerator.LoadOrCreate<GameDatabase>(DatabasePath, d =>
            {
                d.newGameGold = 0;
                d.allowDebugFill = false;
            });
            db.ingredients = phase1.Ingredients.Values.OrderBy(i => i.id).ToList();
            db.upgrades = upgrades;
            db.freshness = phase1.Freshness;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            return db;
        }

        static TavernUpgradeDefinition Upgrade(string id, string english, UpgradeKind kind, params (int cost, float amount)[] levels)
        {
            string name = string.Concat(id.Split('_').Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
            var u = ContentGenerator.LoadOrCreate<TavernUpgradeDefinition>($"{UpgradesFolder}/Upgrade_{name}.asset", x =>
            {
                x.id = id;
                x.kind = kind;
                x.levels = levels.Select(l => new UpgradeLevel { cost = l.cost, amount = l.amount }).ToList();
            });
            u.displayName = LocalizationBuilder.ContentString($"upgrade.{id}", english);
            EditorUtility.SetDirty(u);
            return u;
        }
    }
}
