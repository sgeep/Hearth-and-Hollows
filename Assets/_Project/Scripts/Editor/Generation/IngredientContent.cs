using System.Collections.Generic;
using System.IO;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4b ingredients (step 3). The prototype's rat parts become the giant spider's (Rat Haunch → Spider
    /// Leg, Rat Liver → Venom Sac: same categories, so recipes still make sense), the bat gets its own
    /// part (Bat Wing), and the enemies' names and harvests follow. Assets are renamed in Unity (GUIDs and
    /// every reference kept), and their Content-table keys are renamed in place, so nothing named after
    /// a rat is left behind. Each change happens once; later runs leave tuning alone.
    /// </summary>
    public static class IngredientContent
    {
        const string k_English = "en";

        public static void Build()
        {
            IngredientDefinition spiderLeg = Ingredient("Ingredient_RatHaunch", "Ingredient_SpiderLeg", "spider_leg",
                "ingredient.rat_haunch", "ingredient.spider_leg", "spider leg", 13, 1, null);
            IngredientDefinition venomSac = Ingredient("Ingredient_RatLiver", "Ingredient_VenomSac", "venom_sac",
                "ingredient.rat_liver", "ingredient.venom_sac", "venom sac", 16, 1, null);
            // No bat parts on the loot sheet: the vampire's cape stands in for the wing (docs/ASSET_MAP.md).
            IngredientDefinition batWing = Ingredient(null, "Ingredient_BatWing", "bat_wing",
                null, "ingredient.bat_wing", "bat wing", 14, 9, d =>
                {
                    d.category = IngredientCategory.Meat;
                    d.flavors = spiderLeg != null ? spiderLeg.flavors : default;
                    d.baseValue = 5;
                    d.placeholderColor = new Color(0.4f, 0.25f, 0.35f);
                });

            Recipe("Recipe_GrilledHaunch", "Recipe_GrilledSpiderLeg", "grilled_spider_leg", "recipe.grilled_haunch", "recipe.grilled_spider_leg", "grilled spider leg");
            EnemyName(DungeonContent.BatDefinitionPath, "enemy.giant_rat", "enemy.bat", "bat");
            EnemyName(DungeonContent.SpiderDefinitionPath, "enemy.cellar_shroom", "enemy.giant_spider", "giant spider");

            Harvest(DungeonContent.BatDefinitionPath, new HarvestPart { ingredient = batWing, baseQuality = Quality.Standard, dropChance = 1f, minCount = 1, maxCount = 2 });
            Harvest(DungeonContent.SpiderDefinitionPath,
                new HarvestPart { ingredient = spiderLeg, baseQuality = Quality.Standard, dropChance = 1f, minCount = 1, maxCount = 2 },
                new HarvestPart { ingredient = venomSac, baseQuality = Quality.Fine, dropChance = 0.6f, minCount = 1, maxCount = 1 });

            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset");
            if (database != null && batWing != null && !database.ingredients.Contains(batWing))
            {
                database.ingredients.Add(batWing);
                EditorUtility.SetDirty(database);
            }
            var tavern = AssetDatabase.LoadAssetAtPath<TavernContent>(EditorPaths.Data + "/Tavern/TavernContent.asset");
            if (tavern != null && batWing != null && !tavern.debugStockIngredients.Contains(batWing))
            {
                tavern.debugStockIngredients.Add(batWing);
                EditorUtility.SetDirty(tavern);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Renames (or creates) an ingredient, gives it its id, name, Content key and loot icon.</summary>
        static IngredientDefinition Ingredient(string oldName, string newName, string id, string oldKey, string newKey, string english,
            int iconColumn, int iconRow, System.Action<IngredientDefinition> create)
        {
            IngredientDefinition definition = RenameOrCreate<IngredientDefinition>(EditorPaths.Ingredients, oldName, newName, create);
            if (definition == null) return null;
            definition.id = id;
            definition.displayName = Key(oldKey, newKey, english);
            Sprite icon = MinifantasyImporter.Cell(MinifantasySheets.LootIcons, "LootIcons", iconColumn, iconRow);
            if (icon != null) definition.icon = icon;
            EditorUtility.SetDirty(definition);
            return definition;
        }

        static void Recipe(string oldName, string newName, string id, string oldKey, string newKey, string english)
        {
            RecipeDefinition recipe = RenameOrCreate<RecipeDefinition>(EditorPaths.Data + "/Recipes", oldName, newName, null, createIfMissing: false);
            if (recipe == null) return;
            recipe.id = id;
            recipe.displayName = Key(oldKey, newKey, english);
            EditorUtility.SetDirty(recipe);
        }

        static void EnemyName(string path, string oldKey, string newKey, string english)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (enemy == null) return;
            enemy.displayName = Key(oldKey, newKey, english);
            EditorUtility.SetDirty(enemy);
        }

        /// <summary>The bat's and spider's own parts, replacing the prototype's rat and shroom drops (once: schema 2).</summary>
        static void Harvest(string path, params HarvestPart[] parts)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (enemy == null || enemy.schema >= 2) return;
            enemy.harvest = new List<HarvestPart>(parts);
            enemy.schema = 2;
            EditorUtility.SetDirty(enemy);
        }

        static T RenameOrCreate<T>(string folder, string oldName, string newName, System.Action<T> create, bool createIfMissing = true) where T : ScriptableObject
        {
            string path = $"{folder}/{newName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            string old = oldName != null ? $"{folder}/{oldName}.asset" : null;
            if (old != null && File.Exists(old))
            {
                string error = AssetDatabase.RenameAsset(old, newName);
                if (!string.IsNullOrEmpty(error)) Debug.LogError($"[Hearthdelve] Could not rename {old}: {error}");
                AssetDatabase.SaveAssets();
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            if (!createIfMissing) return null;
            return LookTestContent.LoadOrCreate(path, create);
        }

        /// <summary>
        /// A Content-table entry: the old key renamed in place (keeping its id), or a new one, with its
        /// English text.
        /// </summary>
        static LocalizedString Key(string oldKey, string newKey, string english)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(Loc.ContentTable);
            if (collection == null) return new LocalizedString(Loc.ContentTable, newKey);
            SharedTableData shared = collection.SharedData;
            if (oldKey != null && shared.Contains(oldKey) && !shared.Contains(newKey)) shared.RenameKey(oldKey, newKey);
            var table = collection.GetTable(k_English) as StringTable;
            if (table != null)
            {
                StringTableEntry entry = table.GetEntry(newKey);
                if (entry == null) table.AddEntry(newKey, english);
                else entry.Value = english;
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(shared);
            EditorUtility.SetDirty(collection);
            return new LocalizedString(Loc.ContentTable, newKey);
        }
    }
}
