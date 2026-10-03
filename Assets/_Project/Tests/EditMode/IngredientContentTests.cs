using System.IO;
using System.Linq;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// The 4b re-theme: the prototype's rat parts became the giant spider's (renamed in Unity, GUIDs
    /// kept), the bat has its own part, and nothing named after a rat is left in assets or text.
    /// </summary>
    public class IngredientContentTests
    {
        const string k_Ingredients = "Assets/_Project/Data/Ingredients";
        const string k_ContentKeys = "Assets/_Project/Localization/Tables/Content Shared Data.asset";
        const string k_ContentEnglish = "Assets/_Project/Localization/Tables/Content_en.asset";

        static IngredientDefinition Ingredient(string name) => AssetDatabase.LoadAssetAtPath<IngredientDefinition>($"{k_Ingredients}/{name}.asset");

        [Test]
        public void TheRatParts_AreNowTheSpidersParts_WithTheirOldGuids()
        {
            Assert.That(Ingredient("Ingredient_RatHaunch"), Is.Null, "no RatHaunch asset left behind");
            Assert.That(Ingredient("Ingredient_RatLiver"), Is.Null, "no RatLiver asset left behind");
            Assert.That(AssetDatabase.AssetPathToGUID($"{k_Ingredients}/Ingredient_SpiderLeg.asset"), Is.EqualTo("98c7efdca280afb42a749aa85b5f9eef"), "Spider Leg kept the Rat Haunch GUID");
            Assert.That(AssetDatabase.AssetPathToGUID($"{k_Ingredients}/Ingredient_VenomSac.asset"), Is.EqualTo("1d5030355ac523f4eade52bcdc259975"), "Venom Sac kept the Rat Liver GUID");
            Assert.That(AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/_Project/Data/Recipes/Recipe_GrilledHaunch.asset"), Is.Null);

            var leg = Ingredient("Ingredient_SpiderLeg");
            var sac = Ingredient("Ingredient_VenomSac");
            var wing = Ingredient("Ingredient_BatWing");
            Assert.That(leg.id, Is.EqualTo("spider_leg"));
            Assert.That(leg.category, Is.EqualTo(IngredientCategory.Meat), "same category as the haunch it replaces");
            Assert.That(sac.id, Is.EqualTo("venom_sac"));
            Assert.That(sac.category, Is.EqualTo(IngredientCategory.Offal), "same category as the liver it replaces");
            Assert.That(wing.id, Is.EqualTo("bat_wing"));
            Assert.That(wing.category, Is.EqualTo(IngredientCategory.Meat));
        }

        [Test]
        public void TheBatAndSpider_DropTheirOwnParts_WithIcons_AllKnownToSaves()
        {
            var bat = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_Bat.asset");
            var spider = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_GiantSpider.asset");
            var slime = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_GreenSlime.asset");
            Assert.That(bat.harvest.Select(h => h.ingredient.id), Is.EquivalentTo(new[] { "bat_wing" }));
            Assert.That(spider.harvest.Select(h => h.ingredient.id), Is.EquivalentTo(new[] { "spider_leg", "venom_sac" }));

            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            foreach (EnemyDefinition enemy in new[] { slime, bat, spider })
            foreach (HarvestPart part in enemy.harvest)
            {
                Assert.That(part.ingredient, Is.Not.Null, enemy.id);
                Assert.That(part.ingredient.icon, Is.Not.Null, $"{part.ingredient.id} has an icon for the pickup and the satchel");
                Assert.That(database.ingredients, Has.Member(part.ingredient), $"{part.ingredient.id} can be saved and loaded");
                Assert.That(File.ReadAllText(AssetDatabase.GetAssetPath(part.ingredient)), Does.Contain($"m_Key: ingredient.{part.ingredient.id}"), $"{part.ingredient.id} has a name");
            }
        }

        [Test]
        public void GrilledSpiderLeg_IsTheGrilledHaunchRecipe_UsingTheSpiderLeg()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>("Assets/_Project/Data/Recipes/Recipe_GrilledSpiderLeg.asset");
            Assert.That(recipe, Is.Not.Null);
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/_Project/Data/Recipes/Recipe_GrilledSpiderLeg.asset"), Is.EqualTo("5df2507673983e3499f0e935c87ce03d"));
            Assert.That(recipe.id, Is.EqualTo("grilled_spider_leg"));
            string yaml = File.ReadAllText("Assets/_Project/Data/Recipes/Recipe_GrilledSpiderLeg.asset");
            Assert.That(yaml, Does.Contain("98c7efdca280afb42a749aa85b5f9eef"), "it still calls for the (renamed) leg");
        }

        [Test]
        public void TheContentTable_HasTheNewNames_AndNoRatsLeft()
        {
            string keys = File.ReadAllText(k_ContentKeys);
            string english = File.ReadAllText(k_ContentEnglish);
            foreach (string gone in new[] { "rat_haunch", "rat_liver", "giant_rat", "cellar_shroom", "grilled_haunch" })
                Assert.That(keys, Does.Not.Contain(gone), $"{gone} key renamed");
            Assert.That(english, Does.Not.Contain("Rat Haunch").And.Not.Contain("Rat Liver").And.Not.Contain("Giant Rat"));
            foreach (string key in new[] { "ingredient.spider_leg", "ingredient.venom_sac", "ingredient.bat_wing", "enemy.bat", "enemy.giant_spider", "recipe.grilled_spider_leg" })
                Assert.That(keys, Does.Contain($"m_Key: {key}"), key);
            foreach (string name in new[] { "Spider Leg", "Venom Sac", "Bat Wing", "Giant Spider", "Grilled Spider Leg" })
                Assert.That(english, Does.Contain($"m_Localized: {name}"), name);
        }
    }
}
