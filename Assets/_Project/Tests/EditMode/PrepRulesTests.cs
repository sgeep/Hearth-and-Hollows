using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Service;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    /// <summary>The evening's prep (4c step 4): steps, how many can be made, the menu, opening, and the results lines.</summary>
    public class PrepRulesTests : KitchenFixture
    {
        [Test]
        public void ADish_ListsItsSteps_TheStewTwo()
        {
            RecipeDefinition kebab = Recipe("kebab", 10, FlavorTags.None, Needs(Haunch));
            RecipeDefinition tonic = Recipe("tonic", 8, FlavorTags.None, Needs(Gel));
            tonic.station = CookStation.Tap;
            RecipeDefinition stew = Recipe("stew", 6, FlavorTags.None, Needs(Haunch));
            stew.station = CookStation.StewPot;
            Assert.That(PrepRules.Steps(kebab), Is.EqualTo(new[] { PrepStep.Grill }));
            Assert.That(PrepRules.Steps(tonic), Is.EqualTo(new[] { PrepStep.Tap }));
            Assert.That(PrepRules.Steps(stew), Is.EqualTo(new[] { PrepStep.Chop, PrepStep.Simmer }));
            Assert.That(PrepRules.Steps(null), Is.Empty);
        }

        [Test]
        public void Makeable_CountsWholeServings_AndTheDoorsOpen_OnlyIfAMenuDishCanBeMade()
        {
            RecipeDefinition kebab = Recipe("kebab", 10, FlavorTags.None, Needs(Haunch, 2));
            RecipeDefinition tonic = Recipe("tonic", 8, FlavorTags.None, Needs(Gel));
            var storeroom = new Storeroom();
            storeroom.Add(Stack(Haunch, 5));
            Assert.That(PrepRules.Makeable(kebab, storeroom), Is.EqualTo(2));
            Assert.That(PrepRules.Makeable(tonic, storeroom), Is.Zero);

            Assert.That(PrepRules.CanOpen(new List<RecipeDefinition>(), storeroom), Is.False, "an empty menu");
            Assert.That(PrepRules.CanOpen(new List<RecipeDefinition> { tonic }, storeroom), Is.False, "nothing on it can be made");
            Assert.That(PrepRules.CanOpen(new List<RecipeDefinition> { tonic, kebab }, storeroom), "one dish is enough");
            Assert.That(PrepRules.AnythingCookable(new[] { tonic }, storeroom), Is.False);
            Assert.That(PrepRules.AnythingCookable(new[] { tonic, kebab }, storeroom));
        }

        [Test]
        public void Toggle_AddsAndRemoves_UpToTheMenuSize()
        {
            RecipeDefinition a = Recipe("a", 1, FlavorTags.None), b = Recipe("b", 1, FlavorTags.None), c = Recipe("c", 1, FlavorTags.None), d = Recipe("d", 1, FlavorTags.None);
            var menu = new List<RecipeDefinition>();
            Assert.That(PrepRules.Toggle(menu, a, 3), Is.True);
            Assert.That(PrepRules.Toggle(menu, b, 3), Is.True);
            Assert.That(PrepRules.Toggle(menu, c, 3), Is.True);
            Assert.That(PrepRules.Toggle(menu, d, 3), Is.False, "a full menu takes no more");
            Assert.That(menu, Is.EqualTo(new[] { a, b, c }));
            Assert.That(PrepRules.Toggle(menu, b, 3), Is.False, "tapping a chosen dish takes it off");
            Assert.That(PrepRules.Toggle(menu, d, 3), Is.True, "making room for another");
            Assert.That(menu, Is.EqualTo(new[] { a, c, d }));
        }

        [Test]
        public void TheReport_LeadsWithTheGood_AndOnlyMentionsTroubleThatHappened()
        {
            var calm = new ServiceLedger { DishesServed = 4, Gold = 40, Tips = 6, Renown = 3 };
            var report = new EveningReport(calm, closedEarly: false, stayedShut: false);
            Assert.That(report.Lines.ConvertAll(l => l.line), Is.EqualTo(new[] { EveningLine.Served, EveningLine.Gold, EveningLine.Tips, EveningLine.Renown }));
            Assert.That(report.Takings, Is.EqualTo(46), "payments plus tips");

            var rough = new ServiceLedger { DishesServed = 1, Gold = 10, Walkouts = 2, DroppedDishes = 1 };
            var lines = new EveningReport(rough, closedEarly: true, stayedShut: false).Lines.ConvertAll(l => l.line);
            Assert.That(lines, Is.EqualTo(new[] { EveningLine.Served, EveningLine.Gold, EveningLine.Tips, EveningLine.Renown, EveningLine.Walkouts, EveningLine.Dropped }));

            var shut = new EveningReport(null, closedEarly: false, stayedShut: true);
            Assert.That(shut.StayedShut);
            Assert.That(shut.Lines, Is.Empty);
            Assert.That(shut.Takings, Is.Zero);
        }
    }
}
