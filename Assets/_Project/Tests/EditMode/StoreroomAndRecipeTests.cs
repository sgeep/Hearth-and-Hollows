using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>Creates ingredient and recipe assets in memory for tests.</summary>
    public abstract class KitchenFixture
    {
        protected IngredientDefinition Haunch, Liver, Cap, Spore, Gel, Core;
        readonly List<Object> m_Created = new();

        [SetUp]
        public void CreateKitchen()
        {
            Haunch = Ingredient("rat_haunch", IngredientCategory.Meat, FlavorTags.Savory);
            Liver = Ingredient("rat_liver", IngredientCategory.Offal, FlavorTags.Bitter);
            Cap = Ingredient("shroom_cap", IngredientCategory.Fungus, FlavorTags.Earthy | FlavorTags.Umami);
            Spore = Ingredient("spore_sac", IngredientCategory.Spice, FlavorTags.Spicy);
            Gel = Ingredient("slime_gel", IngredientCategory.Liquid, FlavorTags.Sweet);
            Core = Ingredient("slime_core", IngredientCategory.Magical, FlavorTags.Arcane);
        }

        [TearDown]
        public void DestroyKitchen()
        {
            foreach (var o in m_Created) Object.DestroyImmediate(o);
            m_Created.Clear();
        }

        protected IngredientDefinition Ingredient(string id, IngredientCategory category, FlavorTags flavors)
        {
            var d = ScriptableObject.CreateInstance<IngredientDefinition>();
            d.id = id;
            d.category = category;
            d.flavors = flavors;
            m_Created.Add(d);
            return d;
        }

        protected RecipeDefinition Recipe(string id, int value, FlavorTags flavors, params RecipeSlot[] slots)
        {
            var r = ScriptableObject.CreateInstance<RecipeDefinition>();
            r.id = id;
            r.baseValue = value;
            r.flavors = flavors;
            r.slots = new List<RecipeSlot>(slots);
            m_Created.Add(r);
            return r;
        }

        protected static RecipeSlot Needs(IngredientDefinition d, int count = 1, bool optional = false) =>
            new() { match = SlotMatch.Ingredient, ingredient = d, count = count, optional = optional };

        protected static RecipeSlot NeedsAny(IngredientCategory categories, int count = 1) =>
            new() { match = SlotMatch.Category, categories = categories, count = count };

        protected static IngredientStack Stack(IngredientDefinition d, int count, Quality q = Quality.Standard, float fresh = 1f, PrepState prep = PrepState.Raw) =>
            new(new IngredientItem(d, q, prep), count, fresh);
    }

    public class StoreroomTests : KitchenFixture
    {
        [Test]
        public void IdenticalItems_MergeWithCountWeightedFreshness()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 2, fresh: 1.0f));
            s.Add(Stack(Haunch, 2, fresh: 0.5f));
            Assert.That(s.Stacks, Has.Count.EqualTo(1));
            Assert.That(s.Stacks[0].Count, Is.EqualTo(4));
            Assert.That(s.Stacks[0].Freshness, Is.EqualTo(0.75f).Within(1e-5f));
        }

        [Test]
        public void DifferentQuality_StaysSeparate()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1, Quality.Fine));
            s.Add(Stack(Haunch, 1, Quality.Poor));
            Assert.That(s.Stacks, Has.Count.EqualTo(2));
        }

        [Test]
        public void Take_UsesLeastFreshFirst()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1, Quality.Fine, fresh: 0.9f));
            s.Add(Stack(Haunch, 1, Quality.Premium, fresh: 0.3f));
            var taken = s.Take(i => i.Definition == Haunch, 1);
            Assert.That(taken[0].Item.Quality, Is.EqualTo(Quality.Premium), "the staler stack goes first");
            Assert.That(taken[0].Freshness, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void Take_OnFreshnessTie_UsesLowerQualityFirst()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1, Quality.Premium, fresh: 0.6f));
            s.Add(Stack(Haunch, 1, Quality.Poor, fresh: 0.6f));
            var taken = s.Take(i => i.Definition == Haunch, 1);
            Assert.That(taken[0].Item.Quality, Is.EqualTo(Quality.Poor));
        }

        [Test]
        public void Take_SpansStacks_AndRemovesEmptied()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1, Quality.Poor, fresh: 0.2f));
            s.Add(Stack(Haunch, 3, Quality.Fine, fresh: 0.8f));
            var taken = s.Take(i => i.Definition == Haunch, 2);
            Assert.That(taken, Has.Count.EqualTo(2));
            Assert.That(s.Stacks, Has.Count.EqualTo(1));
            Assert.That(s.Stacks[0].Count, Is.EqualTo(2));
        }

        [Test]
        public void Take_NotEnough_ReturnsNull_AndTakesNothing()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1));
            Assert.That(s.Take(i => i.Definition == Haunch, 2), Is.Null);
            Assert.That(s.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var s = new Storeroom();
            s.Add(Stack(Haunch, 2));
            var c = s.Clone();
            c.Take(_ => true, 2);
            Assert.That(s.TotalCount, Is.EqualTo(2));
        }
    }

    public class RecipeMatcherTests : KitchenFixture
    {
        [Test]
        public void SpecificIngredient_Cooks_AndConsumes()
        {
            var r = Recipe("grilled_haunch", 12, FlavorTags.Savory, Needs(Haunch));
            var s = new Storeroom();
            s.Add(Stack(Haunch, 2));
            var cooked = RecipeMatcher.TryTake(r, s);
            Assert.That(cooked, Is.Not.Null);
            Assert.That(cooked.TotalCount, Is.EqualTo(1));
            Assert.That(s.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void CategorySlot_AcceptsAnyMatchingCategory()
        {
            var kebab = Recipe("kebab", 15, FlavorTags.Savory, NeedsAny(IngredientCategory.Meat | IngredientCategory.Offal), Needs(Cap));
            var s = new Storeroom();
            s.Add(Stack(Liver, 1));
            s.Add(Stack(Cap, 1));
            Assert.That(RecipeMatcher.TryTake(kebab, s), Is.Not.Null, "offal satisfies Meat|Offal");
        }

        [Test]
        public void MissingRequired_TakesNothing()
        {
            var kebab = Recipe("kebab", 15, FlavorTags.Savory, Needs(Haunch), Needs(Cap));
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1));
            Assert.That(RecipeMatcher.TryTake(kebab, s), Is.Null);
            Assert.That(s.TotalCount, Is.EqualTo(1), "all-or-nothing");
        }

        [Test]
        public void OptionalSlot_UsedWhenInStock_AddsItsFlavors()
        {
            var skewer = Recipe("skewer", 10, FlavorTags.Earthy, Needs(Cap, 2), Needs(Spore, optional: true));
            var s = new Storeroom();
            s.Add(Stack(Cap, 2));
            s.Add(Stack(Spore, 1));
            var cooked = RecipeMatcher.TryTake(skewer, s);
            Assert.That(cooked.TotalCount, Is.EqualTo(3));
            Assert.That(cooked.Flavors & FlavorTags.Spicy, Is.EqualTo(FlavorTags.Spicy));
        }

        [Test]
        public void OptionalSlot_Missing_StillCooks()
        {
            var skewer = Recipe("skewer", 10, FlavorTags.Earthy, Needs(Cap, 2), Needs(Spore, optional: true));
            var s = new Storeroom();
            s.Add(Stack(Cap, 2));
            var cooked = RecipeMatcher.TryTake(skewer, s);
            Assert.That(cooked, Is.Not.Null);
            Assert.That(cooked.Flavors & FlavorTags.Spicy, Is.EqualTo(FlavorTags.None));
        }

        [Test]
        public void InedibleParts_AreNeverUsed()
        {
            var r = Recipe("grilled_haunch", 12, FlavorTags.Savory, Needs(Haunch));
            var s = new Storeroom();
            s.Add(Stack(Haunch, 3, prep: PrepState.Inedible));
            Assert.That(RecipeMatcher.CanCook(r, s), Is.False);
            Assert.That(RecipeMatcher.ServingsAvailable(r, s), Is.EqualTo(0));
        }

        [Test]
        public void ServingsAvailable_CountsWithoutConsuming()
        {
            var gelbrew = Recipe("gelbrew", 8, FlavorTags.Sweet, Needs(Gel, 2));
            var s = new Storeroom();
            s.Add(Stack(Gel, 5));
            Assert.That(RecipeMatcher.ServingsAvailable(gelbrew, s), Is.EqualTo(2));
            Assert.That(s.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void Cooking_UsesLeastFreshStockFirst()
        {
            var r = Recipe("grilled_haunch", 12, FlavorTags.Savory, Needs(Haunch));
            var s = new Storeroom();
            s.Add(Stack(Haunch, 1, Quality.Fine, fresh: 1f));
            s.Add(Stack(Haunch, 1, Quality.Standard, fresh: 0.4f));
            var cooked = RecipeMatcher.TryTake(r, s);
            Assert.That(cooked.Used[0].Freshness, Is.EqualTo(0.4f).Within(1e-5f));
        }
    }
}
