using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Phase 2 data: 7 Biome 1 recipes (5 single dishes, 2 stews), 3 customer types, 1 staff
    /// helper, and tavern tuning.
    /// Like Phase 1, assets are only created, never overwritten, so inspector tuning is kept.
    /// </summary>
    public static class TavernContentGenerator
    {
        public const string TavernData = EditorPaths.Data + "/Tavern";
        public const string RecipesFolder = EditorPaths.Data + "/Recipes";
        public const string CustomersFolder = EditorPaths.Data + "/Customers";
        public const string StaffFolder = EditorPaths.Data + "/Staff";

        public static TavernContent Generate(ContentGenerator.Content phase1)
        {
            foreach (var f in new[] { TavernData, RecipesFolder, CustomersFolder, StaffFolder }) EditorPaths.Ensure(f);
            var ing = phase1.Ingredients;

            var recipes = new List<RecipeDefinition>
            {
                Recipe("grilled_haunch", "Grilled Rat Haunch", CookStation.Grill, 12, FlavorTags.Savory, new Color(0.78f, 0.42f, 0.3f),
                    Slot(ing["rat_haunch"])),
                Recipe("shroom_skewer", "Shroom Skewer", CookStation.Grill, 10, FlavorTags.Earthy | FlavorTags.Umami, new Color(0.66f, 0.5f, 0.78f),
                    Slot(ing["shroom_cap"], 2), Slot(ing["spore_sac"], optional: true)),
                Recipe("cellar_kebab", "Cellar Kebab", CookStation.Grill, 16, FlavorTags.Savory | FlavorTags.Umami, new Color(0.85f, 0.58f, 0.34f),
                    new RecipeSlot { match = SlotMatch.Category, categories = IngredientCategory.Meat | IngredientCategory.Offal, count = 1 },
                    Slot(ing["shroom_cap"])),
                Recipe("gelbrew", "Gelbrew", CookStation.Tap, 8, FlavorTags.Sweet, new Color(0.55f, 0.9f, 0.45f),
                    Slot(ing["slime_gel"], 2)),
                Recipe("core_tonic", "Core Tonic", CookStation.Tap, 18, FlavorTags.Sweet | FlavorTags.Arcane, new Color(0.35f, 0.9f, 0.88f),
                    Slot(ing["slime_core"]), Slot(ing["slime_gel"])),
                // Stews: one batch of these ingredients makes several helpings; value is per helping.
                Recipe("cellar_stew", "Cellar Stew", CookStation.StewPot, 9, FlavorTags.Savory | FlavorTags.Umami, new Color(0.62f, 0.4f, 0.26f),
                    Slot(ing["rat_haunch"]), Slot(ing["shroom_cap"]), Slot(ing["spore_sac"], optional: true)),
                Recipe("offal_pottage", "Offal Pottage", CookStation.StewPot, 8, FlavorTags.Savory | FlavorTags.Earthy, new Color(0.5f, 0.3f, 0.34f),
                    Slot(ing["rat_liver"]), Slot(ing["shroom_cap"])),
            };

            var customers = new List<CustomerProfile>
            {
                Customer("villager", "Villager", 1.2f, new Color(0.86f, 0.76f, 0.6f),
                    Traits(walk: 2.2f, orderPatience: 50f, generosity: 0.8f, liked: FlavorTags.Savory | FlavorTags.Sweet, disliked: FlavorTags.Spicy)),
                Customer("adventurer", "Adventurer", 1f, new Color(0.5f, 0.7f, 0.92f),
                    Traits(walk: 3f, orderPatience: 35f, generosity: 1.4f, liked: FlavorTags.Spicy | FlavorTags.Umami | FlavorTags.Arcane, disliked: FlavorTags.Bitter)),
                // Dwarves rate drinks from the Tap higher (strong ale, GDD §6.3).
                Customer("dwarf", "Dwarf", 0.9f, new Color(0.82f, 0.52f, 0.36f),
                    Traits(walk: 2f, orderPatience: 45f, generosity: 1f, liked: FlavorTags.Savory | FlavorTags.Earthy, disliked: FlavorTags.Arcane,
                        favoriteStation: CookStation.Tap, favoriteStationBonus: 0.25f)),
            };

            var pip = ContentGenerator.LoadOrCreate<StaffDefinition>($"{StaffFolder}/Staff_Pip.asset", s =>
            {
                s.id = "pip";
                s.skill = 0.6f;
                s.qualityCap = 0.85f;
                s.restBetweenJobs = 1f;
            });
            pip.displayName = LocalizationBuilder.ContentString("staff.pip", "Pip");
            EditorUtility.SetDirty(pip);

            var content = ContentGenerator.LoadOrCreate<TavernContent>($"{TavernData}/TavernContent.asset", _ => { });
            content.recipes = recipes;
            content.customers = customers;
            content.staff = new List<StaffDefinition> { pip };
            content.debugStockIngredients = ing.Values.OrderBy(d => d.id).ToList();
            content.service = ContentGenerator.LoadOrCreate<ServiceConfig>($"{TavernData}/ServiceConfig.asset", _ => { });
            content.economy = ContentGenerator.LoadOrCreate<EconomyConfig>($"{EditorPaths.Config}/EconomyConfig.asset", _ => { });
            content.grill = ContentGenerator.LoadOrCreate<GrillConfig>($"{TavernData}/GrillConfig.asset", _ => { });
            content.tap = ContentGenerator.LoadOrCreate<TapConfig>($"{TavernData}/TapConfig.asset", _ => { });
            content.serving = ContentGenerator.LoadOrCreate<ServingConfig>($"{TavernData}/ServingConfig.asset", _ => { });
            content.stew = ContentGenerator.LoadOrCreate<StewConfig>($"{TavernData}/StewConfig.asset", _ => { });
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssets();
            return content;
        }

        static RecipeSlot Slot(IngredientDefinition ingredient, int count = 1, bool optional = false) =>
            new() { match = SlotMatch.Ingredient, ingredient = ingredient, count = count, optional = optional };

        static RecipeDefinition Recipe(string id, string english, CookStation station, int value, FlavorTags flavors, Color color, params RecipeSlot[] slots)
        {
            var r = ContentGenerator.LoadOrCreate<RecipeDefinition>($"{RecipesFolder}/Recipe_{Pascal(id)}.asset", x =>
            {
                x.id = id;
                x.station = station;
                x.baseValue = value;
                x.flavors = flavors;
                x.placeholderColor = color;
                x.slots = new List<RecipeSlot>(slots);
            });
            r.displayName = LocalizationBuilder.ContentString($"recipe.{id}", english);
            EditorUtility.SetDirty(r);
            return r;
        }

        static CustomerProfile Customer(string id, string english, float weight, Color color, CustomerTraits traits)
        {
            var c = ContentGenerator.LoadOrCreate<CustomerProfile>($"{CustomersFolder}/Customer_{Pascal(id)}.asset", x =>
            {
                x.id = id;
                x.spawnWeight = weight;
                x.placeholderColor = color;
                x.traits = traits;
            });
            c.displayName = LocalizationBuilder.ContentString($"customer.{id}", english);
            EditorUtility.SetDirty(c);
            return c;
        }

        static CustomerTraits Traits(float walk, float orderPatience, float generosity, FlavorTags liked, FlavorTags disliked,
            CookStation? favoriteStation = null, float favoriteStationBonus = 0f)
        {
            var t = CustomerTraits.Default;
            t.walkSpeed = walk;
            t.orderPatience = orderPatience;
            t.generosity = generosity;
            t.liked = liked;
            t.disliked = disliked;
            t.hasFavoriteStation = favoriteStation.HasValue;
            t.favoriteStation = favoriteStation ?? CookStation.Grill;
            t.favoriteStationBonus = favoriteStationBonus;
            return t;
        }

        static string Pascal(string id) => string.Concat(id.Split('_').Select(p => p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1) : p));
    }
}
