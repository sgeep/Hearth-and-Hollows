using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Run;
using Hearthdelve.Shared.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests
{
    public class DayCycleTests
    {
        [Test]
        public void StartsOnDayOneMorning_AndRunsThePhasesInOrder()
        {
            var c = new DayCycle();
            Assert.That((c.Day, c.Phase), Is.EqualTo((1, DayPhase.Morning)));
            c.AdvanceTo(DayPhase.Delve);
            c.AdvanceTo(DayPhase.Evening);
            c.AdvanceTo(DayPhase.Night);
            Assert.That(c.Day, Is.EqualTo(1));
            c.AdvanceTo(DayPhase.Morning);
            Assert.That((c.Day, c.Phase), Is.EqualTo((2, DayPhase.Morning)), "sleeping starts a new day");
        }

        [Test]
        public void SkippingAPhase_Throws_AndChangesNothing()
        {
            var c = new DayCycle();
            Assert.Throws<InvalidOperationException>(() => c.AdvanceTo(DayPhase.Evening));
            Assert.Throws<InvalidOperationException>(() => c.AdvanceTo(DayPhase.Morning));
            Assert.That((c.Day, c.Phase), Is.EqualTo((1, DayPhase.Morning)));
        }

        [Test]
        public void Changed_ReportsPreviousAndCurrent()
        {
            var c = new DayCycle(3, DayPhase.Night);
            (DayPhase from, DayPhase to)? seen = null;
            c.Changed += (from, to) => seen = (from, to);
            c.Advance();
            Assert.That(seen, Is.EqualTo((DayPhase.Night, DayPhase.Morning)));
            Assert.That(c.Day, Is.EqualTo(4));
        }
    }

    public class DayRulesTests : KitchenFixture
    {
        readonly List<Object> m_Owned = new();

        [TearDown]
        public void DestroyOwned()
        {
            foreach (var o in m_Owned) Object.DestroyImmediate(o);
            m_Owned.Clear();
        }

        TavernUpgradeDefinition Upgrade(string id, UpgradeKind kind, params (int cost, float amount)[] levels)
        {
            var u = ScriptableObject.CreateInstance<TavernUpgradeDefinition>();
            u.id = id;
            u.kind = kind;
            u.levels = levels.Select(l => new UpgradeLevel { cost = l.cost, amount = l.amount }).ToList();
            m_Owned.Add(u);
            return u;
        }

        static GameState At(DayPhase phase)
        {
            var s = new GameState();
            while (s.Phase != phase) s.Cycle.Advance();
            return s;
        }

        Satchel Haul()
        {
            var satchel = new Satchel(6, 3);
            satchel.Add(new IngredientItem(Haunch, Quality.Fine), 3, 0.9f);
            satchel.Add(new IngredientItem(Gel, Quality.Standard, PrepState.Chilled), 2, 0.8f);
            return satchel;
        }

        // ---------- Delve → storeroom ----------

        [Test]
        public void Extraction_MovesTheWholeSatchelIntoTheStoreroom_WithItsFreshness()
        {
            var state = At(DayPhase.Delve);
            var satchel = Haul();
            DayRules.CompleteDelve(state, DelveReport.Extraction(satchel));

            Assert.That(state.Phase, Is.EqualTo(DayPhase.Evening));
            Assert.That(state.Storeroom.TotalCount, Is.EqualTo(5));
            var haunch = state.Storeroom.Stacks.Single(s => s.Item.Definition == Haunch);
            Assert.That(haunch.Freshness, Is.EqualTo(0.9f).Within(1e-5f));
            Assert.That(state.Today.Delve, Is.EqualTo(DelveOutcome.Extracted));
            Assert.That(state.Today.PartsBroughtBack, Is.EqualTo(5));
        }

        [Test]
        public void Death_OnlyTheLockboxStackReachesTheStoreroom_AndTheDayGoesOn()
        {
            var state = At(DayPhase.Delve);
            state.Storeroom.Add(Stack(Cap, 1));
            var satchel = Haul();
            var result = DeathPenalty.Resolve(satchel, keepSlotIndex: 1, runCurrency: 0);
            DayRules.CompleteDelve(state, DelveReport.Death(result));

            Assert.That(state.Phase, Is.EqualTo(DayPhase.Evening), "a bad delve means a lean night, not a lost day");
            Assert.That(state.Storeroom.CountMatching(i => i.Definition == Gel), Is.EqualTo(2), "the kept stack");
            Assert.That(state.Storeroom.CountMatching(i => i.Definition == Haunch), Is.Zero);
            Assert.That(state.Storeroom.CountMatching(i => i.Definition == Cap), Is.EqualTo(1), "stock already home is untouched");
            Assert.That(state.Today.Delve, Is.EqualTo(DelveOutcome.Died));
            Assert.That(state.Today.PartsLost, Is.EqualTo(3));
        }

        [Test]
        public void Death_KeepingNothing_StillEndsInTheEvening()
        {
            var state = At(DayPhase.Delve);
            var result = DeathPenalty.Resolve(Haul(), DeathPenalty.KeepNothing, 0);
            DayRules.CompleteDelve(state, DelveReport.Death(result));
            Assert.That(state.Storeroom.TotalCount, Is.Zero);
            Assert.That(state.Phase, Is.EqualTo(DayPhase.Evening));
        }

        // ---------- Service, night ----------

        [Test]
        public void Service_BanksPaymentsAndTips_ChangesRenown_AndMovesToNight()
        {
            var state = At(DayPhase.Evening);
            DayRules.CompleteService(state, new ServiceReport(dishesServed: 6, gold: 70, tips: 15, renownChange: 4, walkouts: 1));
            Assert.That(state.Gold, Is.EqualTo(85));
            Assert.That(state.Renown, Is.EqualTo(4));
            Assert.That(state.Phase, Is.EqualTo(DayPhase.Night));
            Assert.That(state.Today.DishesServed, Is.EqualTo(6));
            Assert.That(state.Today.Earned, Is.EqualTo(85));
        }

        [Test]
        public void Rules_RefuseTheWrongPhase()
        {
            var state = new GameState();
            Assert.Throws<InvalidOperationException>(() => DayRules.CompleteService(state, default));
            Assert.Throws<InvalidOperationException>(() => DayRules.CompleteDelve(state, DelveReport.Empty));
            Assert.Throws<InvalidOperationException>(() => DayRules.Sleep(state, FreshnessSettings.Default));
            Assert.That(state.Phase, Is.EqualTo(DayPhase.Morning));
        }

        [Test]
        public void ClosingWithoutService_GoesStraightToNight()
        {
            var state = At(DayPhase.Evening);
            DayRules.SkipService(state);
            Assert.That(state.Phase, Is.EqualTo(DayPhase.Night));
            Assert.That(state.Gold, Is.Zero);
        }

        [Test]
        public void Sleep_AgesTheStoreroom_StartsANewDay_AndClearsTheSummary()
        {
            var state = At(DayPhase.Night);
            state.Storeroom.Add(Stack(Haunch, 2, fresh: 0.9f));
            state.Storeroom.Add(Stack(Gel, 1, fresh: 0.9f, prep: PrepState.Chilled));
            state.Today.DishesServed = 5;
            var f = FreshnessSettings.Default;
            DayRules.Sleep(state, f);

            Assert.That((state.Day, state.Phase), Is.EqualTo((2, DayPhase.Morning)));
            Assert.That(state.Storeroom.Stacks.Single(s => s.Item.Definition == Haunch).Freshness, Is.EqualTo(0.9f - f.overnightLoss).Within(1e-5f));
            Assert.That(state.Storeroom.Stacks.Single(s => s.Item.Definition == Gel).Freshness,
                Is.EqualTo(0.9f - f.overnightLoss * f.chilledMultiplier).Within(1e-5f), "Chilled keeps better");
            Assert.That(state.Today.DishesServed, Is.Zero);
        }

        // ---------- Freshness ----------

        [Test]
        public void SatchelDecay_RawLosesTheRate_ChilledLessAndNeverBelowZero()
        {
            var f = FreshnessSettings.Default;
            var satchel = Haul();
            satchel.Decay(f, f.dungeonLossPerMinute); // one minute in the dungeon
            Assert.That(satchel.Slots[0].Freshness, Is.EqualTo(0.9f - f.dungeonLossPerMinute).Within(1e-5f));
            Assert.That(satchel.Slots[1].Freshness, Is.EqualTo(0.8f - f.dungeonLossPerMinute * f.chilledMultiplier).Within(1e-5f));
            satchel.Decay(f, 5f);
            Assert.That(satchel.Slots.Where(s => !s.IsEmpty).All(s => s.Freshness == 0f), "spoiled parts stay, at zero");
            Assert.That(satchel.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public void StoreroomDecay_RaisesChanged()
        {
            var store = new Storeroom();
            store.Add(Stack(Haunch, 1));
            int changes = 0;
            store.Changed += () => changes++;
            store.Decay(FreshnessSettings.Default, 0.1f);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(store.Stacks[0].Freshness, Is.EqualTo(0.9f).Within(1e-5f));
        }

        // ---------- Upgrades ----------

        [Test]
        public void Upgrade_CostsRiseByLevel_AndItMaxesOut()
        {
            var u = Upgrade("satchel_slots", UpgradeKind.SatchelSlots, (100, 1), (180, 1));
            Assert.That(Upgrades.NextCost(u, 0), Is.EqualTo(100));
            Assert.That(Upgrades.NextCost(u, 1), Is.EqualTo(180));
            Assert.That(Upgrades.NextCost(u, 2), Is.EqualTo(-1));
            Assert.That(Upgrades.IsMaxed(u, 2));
            Assert.That(Upgrades.CanBuy(u, 0, 99), Is.False);
            Assert.That(Upgrades.CanBuy(u, 0, 100));
        }

        [Test]
        public void BuyingAtNight_SpendsGold_RaisesTheLevel_AndRefusesWhatYouCantAfford()
        {
            var u = Upgrade("satchel_slots", UpgradeKind.SatchelSlots, (100, 1), (180, 1));
            var state = At(DayPhase.Night);
            state.AddGold(150);
            Assert.That(DayRules.BuyUpgrade(state, u));
            Assert.That(state.Gold, Is.EqualTo(50));
            Assert.That(state.UpgradeLevel("satchel_slots"), Is.EqualTo(1));
            Assert.That(DayRules.BuyUpgrade(state, u), Is.False, "180 > 50");
            Assert.That(state.UpgradeLevel("satchel_slots"), Is.EqualTo(1));
            state.AddGold(1000);
            DayRules.BuyUpgrade(state, u);
            Assert.That(DayRules.BuyUpgrade(state, u), Is.False, "maxed");
            Assert.That(state.UpgradeLevel("satchel_slots"), Is.EqualTo(2));
        }

        [Test]
        public void BuyingOutsideNight_Throws()
        {
            var u = Upgrade("x", UpgradeKind.Seats, (1, 1));
            var state = new GameState();
            state.AddGold(10);
            Assert.Throws<InvalidOperationException>(() => DayRules.BuyUpgrade(state, u));
        }

        [Test]
        public void Effects_AddUpOwnedLevelsOfEachKind()
        {
            var satchel = Upgrade("satchel_slots", UpgradeKind.SatchelSlots, (1, 1), (1, 1), (1, 1));
            var essence = Upgrade("max_essence", UpgradeKind.MaxEssence, (1, 20), (1, 25));
            var seats = Upgrade("tavern_seats", UpgradeKind.Seats, (1, 1), (1, 1));
            var levels = new Dictionary<string, int> { ["satchel_slots"] = 2, ["max_essence"] = 2, ["tavern_seats"] = 0 };
            var e = Upgrades.Effects(new[] { satchel, essence, seats }, id => levels.TryGetValue(id, out int l) ? l : 0);
            Assert.That(e.SatchelSlots, Is.EqualTo(2));
            Assert.That(e.MaxEssence, Is.EqualTo(45f));
            Assert.That(e.Seats, Is.Zero);
        }

        // ---------- Breakfast ----------

        RecipeDefinition Breakfast(MealBuffKind kind, float amount)
        {
            var r = Recipe("grilled_haunch", 12, FlavorTags.Savory, Needs(Haunch));
            r.mealBuff = new MealBuffSettings { kind = kind, amount = amount };
            return r;
        }

        [Test]
        public void MealBuff_ScalesWithDishQuality_UpToACap()
        {
            var r = Breakfast(MealBuffKind.MaxEssence, 20f);
            Assert.That(MealBuff.FromDish(r, 1f).Amount, Is.EqualTo(20f));
            Assert.That(MealBuff.FromDish(r, 0.5f).Amount, Is.EqualTo(10f));
            Assert.That(MealBuff.FromDish(r, 3f).Amount, Is.EqualTo(20f * MealBuff.MaxQualityScale));
            Assert.That(MealBuff.FromDish(Breakfast(MealBuffKind.None, 20f), 1f).IsActive, Is.False, "no buff: not a breakfast");
        }

        [Test]
        public void Loadout_CombinesUpgradesAndBreakfast()
        {
            var up = new UpgradeEffects { SatchelSlots = 1, MaxEssence = 20f };
            var hearty = DelveLoadout.From(up, MealBuff.FromDish(Breakfast(MealBuffKind.MaxEssence, 20f), 1f));
            Assert.That(hearty.ExtraSatchelSlots, Is.EqualTo(1));
            Assert.That(hearty.MaxEssenceBonus, Is.EqualTo(40f));
            Assert.That(hearty.DrainMultiplier, Is.EqualTo(1f));

            var drink = DelveLoadout.From(up, MealBuff.FromDish(Breakfast(MealBuffKind.SlowerDrain, 0.2f), 1f));
            Assert.That(drink.MaxEssenceBonus, Is.EqualTo(20f));
            Assert.That(drink.DrainMultiplier, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(DelveLoadout.From(up, new MealBuff(MealBuffKind.SlowerDrain, 5f, "x")).DrainMultiplier, Is.EqualTo(DelveLoadout.MinDrainMultiplier));
        }

        [Test]
        public void OneBreakfastPerMorning_UsedUpByTheDelve()
        {
            var state = new GameState();
            var meal = MealBuff.FromDish(Breakfast(MealBuffKind.SlowerDrain, 0.2f), 1f);
            Assert.That(DayRules.EatMeal(state, meal));
            Assert.That(DayRules.EatMeal(state, meal), Is.False, "one per morning");
            DayRules.StartDelve(state);
            Assert.That(state.Meal.IsActive, "lasts through the delve");
            DayRules.CompleteDelve(state, DelveReport.Empty);
            Assert.That(state.Meal.IsActive, Is.False, "used up");
        }
    }

    public class SaveSystemTests : KitchenFixture
    {
        IngredientDefinition Lookup(string id) => new[] { Haunch, Gel, Cap }.FirstOrDefault(d => d.id == id);

        static bool KnownUpgrade(string id) => id is "satchel_slots" or "tavern_seats";

        [Test]
        public void RoundTrip_KeepsEverythingThatPersists()
        {
            var state = new GameState(4, DayPhase.Evening);
            DayRules.CompleteService(state, new ServiceReport(3, 200, 37, 5, 0)); // → Night, 237 gold, renown 5
            state.Storeroom.Add(Stack(Haunch, 3, Quality.Fine, 0.75f));
            state.Storeroom.Add(Stack(Gel, 2, Quality.Poor, 0.5f, PrepState.Chilled));
            var satchel = ScriptableObject.CreateInstance<TavernUpgradeDefinition>();
            satchel.id = "satchel_slots";
            satchel.levels = new List<UpgradeLevel> { new() { cost = 100, amount = 1 }, new() { cost = 150, amount = 1 } };
            DayRules.BuyUpgrade(state, satchel); // 137 gold left
            Object.DestroyImmediate(satchel);

            string json = SaveSystem.ToJson(SaveSystem.Capture(state));
            var loaded = SaveSystem.Restore(SaveSystem.FromJson(json), Lookup, KnownUpgrade);

            Assert.That((loaded.Day, loaded.Phase), Is.EqualTo((4, DayPhase.Night)));
            Assert.That(loaded.Gold, Is.EqualTo(137));
            Assert.That(loaded.Renown, Is.EqualTo(5));
            Assert.That(loaded.UpgradeLevel("satchel_slots"), Is.EqualTo(1));
            Assert.That(loaded.Storeroom.TotalCount, Is.EqualTo(5));
            var gel = loaded.Storeroom.Stacks.Single(s => s.Item.Definition == Gel);
            Assert.That(gel.Item.Quality, Is.EqualTo(Quality.Poor));
            Assert.That(gel.Item.Prep, Is.EqualTo(PrepState.Chilled));
            Assert.That(gel.Freshness, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(json, Does.Contain("\"version\": 2"));
            Assert.That(json, Does.Contain("\"rat_haunch\""), "content is saved by id");
        }

        /// <summary>Saving and loading again and again (each night, each purchase, each Continue) never duplicates or loses the haul.</summary>
        [Test]
        public void RepeatedRoundTrips_NeitherDuplicateNorLoseTheHaul()
        {
            var state = new GameState();
            DayRules.StartDelve(state);
            var satchel = new Satchel(6, 3);
            satchel.Add(new IngredientItem(Haunch, Quality.Standard), 3);
            satchel.Add(new IngredientItem(Gel, Quality.Fine), 2);
            DayRules.CompleteDelve(state, DelveReport.Extraction(satchel));
            DayRules.CompleteService(state, new ServiceReport(1, 12, 3, 1, 0));

            GameState loaded = state;
            for (int i = 0; i < 3; i++)
                loaded = SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(loaded))), Lookup, KnownUpgrade);

            Assert.That(loaded.Storeroom.TotalCount, Is.EqualTo(5));
            Assert.That(loaded.Storeroom.Stacks.Count(s => !s.IsEmpty), Is.EqualTo(2), "stacks stay merged as they were");
            Assert.That(loaded.Gold, Is.EqualTo(15));
            Assert.That((loaded.Day, loaded.Phase), Is.EqualTo((1, DayPhase.Night)));
        }

        [Test]
        public void RoundTrip_KeepsAnUneatenBreakfastBuff()
        {
            var state = new GameState();
            DayRules.EatMeal(state, new MealBuff(MealBuffKind.SlowerDrain, 0.25f, "gelbrew"));
            var loaded = SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(state))), Lookup, KnownUpgrade);
            Assert.That(loaded.Meal.Kind, Is.EqualTo(MealBuffKind.SlowerDrain));
            Assert.That(loaded.Meal.Amount, Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(loaded.Meal.RecipeId, Is.EqualTo("gelbrew"));
        }

        const string k_Version1Save = @"{
            ""version"": 1,
            ""day"": 7,
            ""gold"": 320,
            ""storeroom"": [
                { ""ingredient"": ""rat_haunch"", ""quality"": 2, ""count"": 4 },
                { ""ingredient"": ""shroom_cap"", ""quality"": 1, ""count"": 2 }
            ]
        }";

        [Test]
        public void Version1Save_IsMigrated()
        {
            var data = SaveSystem.FromJson(k_Version1Save);
            Assert.That(data.version, Is.EqualTo(SaveSystem.CurrentVersion));
            var state = SaveSystem.Restore(data, Lookup, KnownUpgrade);
            Assert.That((state.Day, state.Phase), Is.EqualTo((7, DayPhase.Morning)), "v1 had no phase: resume in the morning");
            Assert.That(state.Gold, Is.EqualTo(320));
            Assert.That(state.Renown, Is.Zero);
            Assert.That(state.UpgradeLevels, Is.Empty);
            var haunch = state.Storeroom.Stacks.Single(s => s.Item.Definition == Haunch);
            Assert.That(haunch.Count, Is.EqualTo(4));
            Assert.That(haunch.Item.Quality, Is.EqualTo(Quality.Fine));
            Assert.That(haunch.Item.Prep, Is.EqualTo(PrepState.Raw));
            Assert.That(haunch.Freshness, Is.EqualTo(1f), "v1 had no freshness: full");
        }

        [Test]
        public void UnknownContent_IsDropped_WithWarnings_NotAFailedLoad()
        {
            var data = new SaveData { version = 2, day = 2, phase = "Morning" };
            data.storeroom.Add(new StackData { ingredient = "rat_haunch", quality = "Standard", prep = "Raw", count = 1, freshness = 1f });
            data.storeroom.Add(new StackData { ingredient = "removed_part", quality = "Standard", prep = "Raw", count = 5, freshness = 1f });
            data.upgrades.Add(new UpgradeData { id = "removed_upgrade", level = 2 });
            var warnings = new List<string>();
            var state = SaveSystem.Restore(data, Lookup, KnownUpgrade, warnings);
            Assert.That(state.Storeroom.TotalCount, Is.EqualTo(1));
            Assert.That(state.UpgradeLevels, Is.Empty);
            Assert.That(warnings, Has.Count.EqualTo(2));
        }

        [Test]
        public void NewerOrBrokenSaves_AreRejectedClearly()
        {
            Assert.Throws<NotSupportedException>(() => SaveSystem.FromJson("{\"version\": 99}"));
            Assert.Throws<FormatException>(() => SaveSystem.FromJson("{\"day\": 3}"));
            Assert.Throws<FormatException>(() => SaveSystem.FromJson("   "));
        }

        [Test]
        public void SaveStore_WritesReadsAndDeletesTheSlot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "hearthdelve_save_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveStore(dir);
                Assert.That(store.Exists, Is.False);
                store.Write("{\"version\": 2}");
                store.Write("{\"version\": 2, \"day\": 3}"); // overwrite
                Assert.That(store.Exists);
                Assert.That(store.Read(), Does.Contain("\"day\": 3"));
                Assert.That(File.Exists(store.FilePath + ".tmp"), Is.False);
                store.Delete();
                Assert.That(store.Exists, Is.False);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }
}
