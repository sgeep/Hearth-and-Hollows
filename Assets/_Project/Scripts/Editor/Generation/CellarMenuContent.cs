using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4f Checkpoint C (plan §18, D17, D19): the surface staples and the Brackenford market, butchered cuts, the mushrooms as
    /// Cellars forage, and the Biome 1 menu. Surface food is dependable and everyday; food from the Hollows is unusual and
    /// valuable. Recipes ask for ingredients or categories, never a source. Ingredients and the market are refreshed every
    /// run (art, names, sources); each recipe's slots, value and delve meal are set once for this pass
    /// (<see cref="RecipeDefinition.menuVersion"/>), so later tuning is kept.
    /// </summary>
    public static class CellarMenuContent
    {
        public const int MenuVersion = 2;
        public const string MarketPath = EditorPaths.Data + "/Tavern/Supply_BrackenfordMarket.asset";
        const string k_Recipes = EditorPaths.Data + "/Recipes";
        const string k_TavernContent = EditorPaths.Data + "/Tavern/TavernContent.asset";
        const string k_Cooking = MinifantasySheets.CraftingAndProfessions;

        public static void Build()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset");
            var tavern = AssetDatabase.LoadAssetAtPath<TavernContent>(k_TavernContent);

            // ---------- Ingredients ----------
            IngredientDefinition Existing(string asset) => AssetDatabase.LoadAssetAtPath<IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_{asset}.asset");
            IngredientDefinition spiderLeg = Existing("SpiderLeg"), batWing = Existing("BatWing"), venomSac = Existing("VenomSac");
            IngredientDefinition slimeGel = Existing("SlimeGel"), slimeCore = Existing("SlimeCore"), shroomCap = Existing("ShroomCap"), sporeSac = Existing("SporeSac");

            IngredientDefinition onion = Staple("Onion", "onion", IngredientCategory.Plant, FlavorTags.Savory | FlavorTags.Sweet, 2, Icon(k_Cooking, "PrepIngredients", "Onion"));
            IngredientDefinition herbs = Staple("Herbs", "herbs", IngredientCategory.Plant | IngredientCategory.Spice, FlavorTags.Earthy, 2, Icon(k_Cooking, "PrepIngredients", "Herbs"));
            IngredientDefinition bread = Staple("Bread", "bread", IngredientCategory.Grain, FlavorTags.Savory, 3, Icon(k_Cooking, "PrepIngredients", "Bread"));
            IngredientDefinition eggs = Staple("Eggs", "eggs", IngredientCategory.Egg, FlavorTags.Savory | FlavorTags.Umami, 3, Icon(MinifantasySheets.FarmIcons, "AnimalProducts", "BrownEgg"));
            IngredientDefinition malt = Staple("Malt", "malt", IngredientCategory.Grain, FlavorTags.Sweet | FlavorTags.Bitter, 2, Icon(MinifantasySheets.FarmIcons, "Crops", "Wheat"));

            IngredientDefinition legCuts = Cut("SpiderLegCuts", "spider_leg_cuts", spiderLeg, 7, Icon(k_Cooking, "PrepIngredients", "Steak"));
            IngredientDefinition wingCuts = Cut("BatWingCuts", "bat_wing_cuts", batWing, 4, Icon(k_Cooking, "PrepIngredients", "Slices"));
            Butcher(spiderLeg, legCuts);
            Butcher(batWing, wingCuts);

            // The mushrooms finally get icons (4b left them blank) and come from the Cellars' forage caches.
            SetIcon(shroomCap, Icon(k_Cooking, "PrepIngredients", "Mushroom"));
            SetIcon(sporeSac, Icon(k_Cooking, "PotionHerbs", "SporePowder"));
            foreach (IngredientDefinition hollows in new[] { spiderLeg, batWing, venomSac, slimeGel, slimeCore, shroomCap, sporeSac })
                if (hollows != null && hollows.source != IngredientSource.Hollows)
                {
                    hollows.source = IngredientSource.Hollows;
                    EditorUtility.SetDirty(hollows);
                }

            var all = new[] { onion, herbs, bread, eggs, malt, legCuts, wingCuts };
            if (database != null)
            {
                foreach (IngredientDefinition d in all)
                    if (d != null && !database.ingredients.Contains(d)) database.ingredients.Add(d);
                database.market = BuildMarket(onion, herbs, bread, eggs, malt);
                EditorUtility.SetDirty(database);
            }
            if (tavern != null)
            {
                foreach (IngredientDefinition d in new[] { onion, herbs, bread, eggs, malt, shroomCap, sporeSac })
                    if (d != null && !tavern.debugStockIngredients.Contains(d)) tavern.debugStockIngredients.Add(d);
                EditorUtility.SetDirty(tavern);
            }

            // ---------- The menu ----------
            RecipeSlot One(IngredientDefinition d, int count = 1, bool optional = false) => new() { match = SlotMatch.Ingredient, ingredient = d, count = count, optional = optional };
            RecipeSlot Any(IngredientCategory categories, int count = 1) => new() { match = SlotMatch.Category, categories = categories, count = count };
            MealBuffSettings Essence(float amount) => new() { kind = MealBuffKind.MaxEssence, amount = amount };
            MealBuffSettings Drain(float amount) => new() { kind = MealBuffKind.SlowerDrain, amount = amount };

            var menu = new List<RecipeDefinition>
            {
                // Everyday (surface only): cheap, dependable, modestly profitable.
                Recipe("BrackenfordAle", "brackenford_ale", CookStation.Tap, 6, FlavorTags.Bitter | FlavorTags.Sweet, Drain(0.1f),
                    Icon(MinifantasySheets.MiscellanyIcons, "Miscellany", "FullBeer"), One(malt)),
                Recipe("OnionBroth", "onion_broth", CookStation.StewPot, 6, FlavorTags.Savory | FlavorTags.Sweet, default,
                    Icon(k_Cooking, "DishIcons", "OnionBroth"), One(onion, 2), One(herbs, 1, true)),
                Recipe("EggsOnToast", "eggs_on_toast", CookStation.Grill, 7, FlavorTags.Savory | FlavorTags.Umami, Essence(10f),
                    Icon(k_Cooking, "DishIcons", "FriedEgg"), One(eggs), One(bread)),
                // Better: one thing from below.
                Recipe("Gelbrew", "gelbrew", CookStation.Tap, 10, FlavorTags.Sour | FlavorTags.Arcane, Drain(0.2f), null, One(slimeGel), One(malt)),
                Recipe("GrilledSpiderLeg", "grilled_spider_leg", CookStation.Grill, 13, FlavorTags.Savory, Essence(20f), null, One(spiderLeg), One(herbs, 1, true)),
                Recipe("CrispyBatWings", "crispy_bat_wings", CookStation.Grill, 12, FlavorTags.Savory | FlavorTags.Spicy, Essence(15f),
                    Icon(k_Cooking, "DishIcons", "WingsPlate"), One(batWing, 2), One(herbs, 1, true)),
                Recipe("ShroomSkewer", "shroom_skewer", CookStation.Grill, 10, FlavorTags.Earthy | FlavorTags.Umami, Essence(15f), null,
                    One(shroomCap, 2), One(sporeSac, 1, true)),
                Recipe("CellarStew", "cellar_stew", CookStation.StewPot, 10, FlavorTags.Savory | FlavorTags.Earthy, default, null,
                    One(spiderLeg), One(onion), One(shroomCap, 1, true)),
                Recipe("OffalPottage", "offal_pottage", CookStation.StewPot, 9, FlavorTags.Umami | FlavorTags.Bitter, default, null,
                    One(venomSac), One(onion), One(bread, 1, true)),
                Recipe("CellarKebab", "cellar_kebab", CookStation.Grill, 16, FlavorTags.Savory | FlavorTags.Earthy, Essence(25f), null,
                    Any(IngredientCategory.Meat | IngredientCategory.Offal), One(shroomCap), One(bread)),
                // Signature: valuable Hollows parts, or the Butcher Block's cuts.
                Recipe("CoreTonic", "core_tonic", CookStation.Tap, 22, FlavorTags.Arcane | FlavorTags.Sweet, Drain(0.3f), null, One(slimeCore), One(slimeGel)),
                Recipe("SpiderLegSteaks", "spider_leg_steaks", CookStation.Grill, 26, FlavorTags.Savory | FlavorTags.Earthy, Essence(30f),
                    Icon(k_Cooking, "DishIcons", "SteakPlate"), One(legCuts, 2), One(herbs)),
                Recipe("BatWingPlatter", "bat_wing_platter", CookStation.Grill, 28, FlavorTags.Savory | FlavorTags.Spicy | FlavorTags.Earthy, Essence(30f),
                    Icon(k_Cooking, "DishIcons", "MeatPlatter"), One(wingCuts, 3), One(bread), One(sporeSac)),
            };
            if (tavern != null)
            {
                foreach (RecipeDefinition r in menu)
                    if (r != null && !tavern.recipes.Contains(r)) tavern.recipes.Add(r);
                // Menu order: everyday, better, signature (the prep screen lists them in this order).
                tavern.recipes = tavern.recipes.Where(r => r != null).OrderBy(r => menu.IndexOf(r) < 0 ? int.MaxValue : menu.IndexOf(r)).ToList();
                EditorUtility.SetDirty(tavern);
            }

            AddForage(shroomCap, sporeSac);
            AssetDatabase.SaveAssets();
        }

        static Sprite Icon(string pack, string file, string sprite) => MinifantasyImporter.Sprite(pack, file, sprite);

        static void SetIcon(IngredientDefinition d, Sprite icon)
        {
            if (d == null || icon == null || d.icon == icon) return;
            d.icon = icon;
            EditorUtility.SetDirty(d);
        }

        /// <summary>A surface staple: bought at the market, Standard quality, everyday value.</summary>
        static IngredientDefinition Staple(string asset, string id, IngredientCategory category, FlavorTags flavors, int value, Sprite icon)
        {
            IngredientDefinition d = LookTestContent.LoadOrCreate<IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_{asset}.asset", created =>
            {
                created.rarity = Rarity.Common;
                created.baseValue = value;
                created.flavors = flavors;
            });
            d.id = id;
            d.displayName = new UnityEngine.Localization.LocalizedString(Hearthdelve.UI.Localization.Loc.ContentTable, $"ingredient.{id}");
            d.category = category;
            d.source = IngredientSource.Market;
            SetIcon(d, icon);
            EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>A part's cuts (D17): their own ingredient, the part's category and flavour, from the Hollows.</summary>
        static IngredientDefinition Cut(string asset, string id, IngredientDefinition part, int value, Sprite icon)
        {
            IngredientDefinition d = LookTestContent.LoadOrCreate<IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_{asset}.asset", created =>
            {
                created.baseValue = value;
                created.rarity = part != null ? part.rarity : Rarity.Common;
            });
            d.id = id;
            d.displayName = new UnityEngine.Localization.LocalizedString(Hearthdelve.UI.Localization.Loc.ContentTable, $"ingredient.{id}");
            d.category = part != null ? part.category : IngredientCategory.Meat;
            d.flavors = part != null ? part.flavors : default;
            d.source = IngredientSource.Hollows;
            SetIcon(d, icon);
            EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>The part can go on the Butcher Block, into these cuts (1 ragged, 2 decent, 3 clean; once, tuning kept).</summary>
        static void Butcher(IngredientDefinition part, IngredientDefinition cuts)
        {
            if (part == null || cuts == null) return;
            part.butchering ??= new ButcheringSettings();
            if (part.butchering.cut == null)
            {
                part.butchering.cut = cuts;
                part.butchering.minCuts = 1;
                part.butchering.maxCuts = 3;
                part.butchering.thresholds = new[] { 0.5f, 0.85f };
            }
            EditorUtility.SetDirty(part);
        }

        static SupplySource BuildMarket(params IngredientDefinition[] staples)
        {
            SupplySource market = LookTestContent.LoadOrCreate<SupplySource>(MarketPath, created =>
            {
                created.offers = new List<SupplyOffer>
                {
                    new() { ingredient = staples[0], price = 2 },
                    new() { ingredient = staples[1], price = 2 },
                    new() { ingredient = staples[2], price = 3 },
                    new() { ingredient = staples[3], price = 3 },
                    new() { ingredient = staples[4], price = 2 },
                };
            });
            market.id = "brackenford_market";
            market.displayName = new UnityEngine.Localization.LocalizedString(Hearthdelve.UI.Localization.Loc.ContentTable, "supply.brackenford_market");
            market.source = IngredientSource.Market;
            market.quality = Quality.Standard;
            EditorUtility.SetDirty(market);
            return market;
        }

        /// <summary>A recipe (created if new); its slots, value and delve meal set once for this menu pass.</summary>
        static RecipeDefinition Recipe(string asset, string id, CookStation station, int value, FlavorTags flavors, MealBuffSettings meal, Sprite icon,
            params RecipeSlot[] slots)
        {
            RecipeDefinition r = LookTestContent.LoadOrCreate<RecipeDefinition>($"{k_Recipes}/Recipe_{asset}.asset");
            r.id = id;
            r.displayName = new UnityEngine.Localization.LocalizedString(Hearthdelve.UI.Localization.Loc.ContentTable, $"recipe.{id}");
            if (icon != null) r.icon = icon;
            if (r.menuVersion < MenuVersion)
            {
                r.station = station;
                r.slots = slots.Where(s => s.match == SlotMatch.Category || s.ingredient != null).ToList();
                r.baseValue = value;
                r.flavors = flavors;
                r.mealBuff = meal;
                r.menuVersion = MenuVersion;
            }
            EditorUtility.SetDirty(r);
            return r;
        }

        /// <summary>The mushrooms as Cellars forage (until the Mushroom People): ingredient room rewards, added once.</summary>
        static void AddForage(IngredientDefinition shroomCap, IngredientDefinition sporeSac)
        {
            var settings = AssetDatabase.LoadAssetAtPath<RunSettings>(DungeonRunBuilder.SettingsPath);
            if (settings == null) return;
            var options = new List<IngredientRewardOption>(settings.tuning.ingredientRewards ?? new IngredientRewardOption[0]);
            bool changed = false;
            foreach (var (d, weight, floor) in new[] { (shroomCap, 0.8f, 1), (sporeSac, 0.5f, 2) })
            {
                if (d == null || options.Any(o => o != null && o.ingredient == d)) continue;
                options.Add(new IngredientRewardOption { ingredient = d, weight = weight, fromFloor = floor });
                changed = true;
            }
            if (!changed) return;
            settings.tuning.ingredientRewards = options.ToArray();
            EditorUtility.SetDirty(settings);
        }
    }
}
