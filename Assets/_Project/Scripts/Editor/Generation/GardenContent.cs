using System;
using System.Linq;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Surface;
using Hearthdelve.UI.Localization;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The garden's data (4h Checkpoint B): the starter crops (herbs, onions, barley), the garden's beds and switches, and
    /// Vigor's tuning, all in the game database. Crops' looks are refreshed from the Farm art; their numbers are set once and
    /// then tuned on the assets (a rerun never undoes a tuning).
    /// </summary>
    public static class GardenContent
    {
        public const string Folder = EditorPaths.Data + "/Garden";
        const string k_DatabasePath = EditorPaths.Data + "/GameDatabase.asset";

        /// <summary>The starter crops: id, name, ingredient, art, days, yield (B playtest values).</summary>
        static readonly (string id, string nameKey, string ingredient, string art, int days, int yield)[] k_Crops =
        {
            ("herbs", GardenLocKeys.CropHerbs, "herbs", "Spinach", 2, 3),
            ("onions", GardenLocKeys.CropOnions, "onion", "Onion", 3, 3),
            ("barley", GardenLocKeys.CropBarley, "malt", "Wheat", 4, 2),
        };

        public static void Build()
        {
            EditorPaths.Ensure(Folder);
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(k_DatabasePath);
            if (database == null) throw new InvalidOperationException($"No game database at {k_DatabasePath}.");

            foreach (var (id, nameKey, ingredient, art, days, yield) in k_Crops)
            {
                IngredientDefinition produce = database.Ingredient(ingredient) ?? throw new InvalidOperationException($"No ingredient '{ingredient}' for the {id} crop.");
                string path = $"{Folder}/Crop_{id}.asset";
                bool fresh = AssetDatabase.LoadAssetAtPath<CropDefinition>(path) == null;
                CropDefinition crop = LookTestContent.CreateOrUpdate<CropDefinition>(path, c =>
                {
                    c.id = id;
                    c.nameKey = nameKey;
                    c.produce = produce;
                    if (fresh)
                    {
                        c.growthDays = days;
                        c.yield = yield;
                        c.fineTendedShare = 0.5f;
                    }
                    string file = KariastonSheets.Crops.First(k => k.name == art).file;
                    c.seeds = Stage(file, art, "Seeds");
                    c.sprout = Stage(file, art, "Grow1");
                    c.growing = Stage(file, art, "Grow2");
                    c.ready = Stage(file, art, "Grow3");
                    c.icon = Stage(file, art, "Icon");
                });
                if (!database.crops.Contains(crop)) database.crops.Add(crop);
            }
            database.crops.RemoveAll(c => c == null);

            database.garden = LookTestContent.CreateOrUpdate<GardenConfig>($"{Folder}/GardenConfig.asset", g =>
            {
                if (g.bedIds == null || g.bedIds.Count == 0) g.bedIds = new() { GardenConfig.Bed1, GardenConfig.Bed2, GardenConfig.Bed3, GardenConfig.Bed4 };
            });
            database.vigor = LookTestContent.CreateOrUpdate<VigorConfig>(EditorPaths.Config + "/VigorConfig.asset", v =>
            {
                if (v.settings.maxVigor <= 0) v.settings = VigorSettings.Default;
            });
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        static Sprite Stage(string file, string art, string stage) =>
            MinifantasyImporter.Sprite(KariastonSheets.FarmPack, file, $"{art}_{stage}") ?? throw new InvalidOperationException($"No {art} {stage} sprite.");
    }
}
