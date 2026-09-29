using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class DishScoringTests : KitchenFixture
    {
        static readonly DishScoringSettings S = DishScoringSettings.Default;

        [Test]
        public void PerfectFineFreshDish_ScoresOne_AndValueIsRecipeValue()
        {
            var used = new[] { Stack(Haunch, 1, Quality.Fine, fresh: 1f) };
            float q = DishScoring.DishQuality(used, 1f, S);
            Assert.That(q, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(DishScoring.DishValue(12, q), Is.EqualTo(12f).Within(1e-4f));
        }

        [Test]
        public void Formula_IsValueTimesQualityTimesFreshnessTimesMinigame()
        {
            // Premium (1.25) × freshness 0.5 → lerp(0.5, 1, 0.5) = 0.75 × minigame 0.8 = 0.75
            var used = new[] { Stack(Haunch, 1, Quality.Premium, fresh: 0.5f) };
            float q = DishScoring.DishQuality(used, 0.8f, S);
            Assert.That(q, Is.EqualTo(1.25f * 0.75f * 0.8f).Within(1e-5f));
            Assert.That(DishScoring.DishValue(20, q), Is.EqualTo(20f * 0.75f).Within(1e-4f));
        }

        [Test]
        public void MultiIngredient_UsesCountWeightedAverages()
        {
            var used = new[] { Stack(Cap, 2, Quality.Poor, 1f), Stack(Haunch, 1, Quality.Premium, 1f) };
            var (quality, freshness) = DishScoring.IngredientMultipliers(used, S);
            Assert.That(quality, Is.EqualTo((2 * 0.6f + 1.25f) / 3f).Within(1e-5f));
            Assert.That(freshness, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ZeroMinigame_GivesWorthlessDish()
        {
            var used = new[] { Stack(Haunch, 1, Quality.Premium) };
            Assert.That(DishScoring.DishQuality(used, 0f, S), Is.EqualTo(0f));
        }

        [Test]
        public void StaleIngredients_AreWorthLessButNotNothing()
        {
            Assert.That(DishScoring.FreshnessMultiplier(0f, S), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(DishScoring.FreshnessMultiplier(1f, S), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ServingScore_ScalesMinigame_WithFloor()
        {
            Assert.That(DishScoring.MinigameScore(1f, 1f, S), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(DishScoring.MinigameScore(1f, 0f, S), Is.EqualTo(S.servingAtZero).Within(1e-5f));
            Assert.That(DishScoring.MinigameScore(0.5f, 1f, S), Is.EqualTo(0.5f).Within(1e-5f));
        }
    }

    public class ServiceEconomyTests
    {
        static readonly ServiceEconomySettings S = ServiceEconomySettings.Default;

        [Test]
        public void Satisfaction_IsWeightedAndClamped()
        {
            Assert.That(ServiceEconomy.Satisfaction(1f, 1f, 0f, S), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(ServiceEconomy.Satisfaction(0f, 0f, 1f, S), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(ServiceEconomy.Satisfaction(1.3f, 1f, 0f, S), Is.EqualTo(1f).Within(1e-5f), "premium dishes cap at full satisfaction");
        }

        [Test]
        public void LongerWait_LowersSatisfaction()
        {
            Assert.That(ServiceEconomy.Satisfaction(0.8f, 0.5f, 0.9f, S), Is.LessThan(ServiceEconomy.Satisfaction(0.8f, 0.5f, 0.1f, S)));
        }

        [Test]
        public void Tip_NoneBelowThreshold_ScalesWithGenerosity()
        {
            Assert.That(ServiceEconomy.Tip(20f, 0.4f, 1f, S), Is.EqualTo(0));
            Assert.That(ServiceEconomy.Tip(20f, 1f, 1f, S), Is.EqualTo(10));
            Assert.That(ServiceEconomy.Tip(20f, 1f, 0.5f, S), Is.EqualTo(5));
        }

        [Test]
        public void Payment_IsRoundedDishValue()
        {
            Assert.That(ServiceEconomy.Payment(11.6f), Is.EqualTo(12));
            Assert.That(ServiceEconomy.Payment(-3f), Is.EqualTo(0));
        }

        [Test]
        public void Renown_PositiveWhenHappy_NegativeWhenNot()
        {
            Assert.That(ServiceEconomy.Renown(1f, S), Is.EqualTo(2));
            Assert.That(ServiceEconomy.Renown(0.5f, S), Is.EqualTo(0));
            Assert.That(ServiceEconomy.Renown(0f, S), Is.EqualTo(-2));
        }

        [Test]
        public void SoldOutLeave_CostsLessRenownThanWalkout()
        {
            Assert.That(S.soldOutRenown, Is.LessThan(0));
            Assert.That(S.soldOutRenown, Is.GreaterThan(S.walkoutRenown));
        }
    }
}
