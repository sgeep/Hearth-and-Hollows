using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Surface;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>4h Checkpoint B, "A day's work": Vigor, the garden and save version 10 (pure rules and state).</summary>
    public class SurfaceCheckpointBTests
    {
        static GameDatabase Database => AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
        static CropDefinition Herbs => Database.Crop("herbs");
        static CropDefinition Onions => Database.Crop("onions");
        static CropDefinition Barley => Database.Crop("barley");
        static readonly VigorSettings Costs = VigorSettings.Default;
        const string Bed = GardenConfig.Bed1;

        static GameState Daytime(int day = 1)
        {
            var state = new GameState(day, DayPhase.Daytime);
            state.Garden.Ensure(Database.GardenBeds);
            state.Vigor.Configure(Costs);
            return state;
        }

        /// <summary>The rest of the day and the night, then sleep: the next day's morning (garden grown, Vigor full).</summary>
        static void NextDay(GameState state)
        {
            DayRules.StartEvening(state);
            DayRules.SkipService(state);
            DayRules.CompleteDelve(state, DelveReport.Empty);
            DayRules.Sleep(state, FreshnessSettings.Default, Database.Crop, GardenSettings.Default);
        }

        // ---------- the tuning and the data ----------

        [Test]
        public void TheStarterTuning_IsTheApprovedTestValues()
        {
            Assert.That((Costs.maxVigor, Costs.Cost(VigorActivity.PlantBed), Costs.Cost(VigorActivity.TendBed), Costs.Cost(VigorActivity.HarvestBed)),
                Is.EqualTo((6, 2, 1, 0)));
            Assert.That(Database.Vigor.maxVigor, Is.EqualTo(6), "the asset as created");
            Assert.That(Database.GardenBeds, Is.EqualTo(new[] { "garden_1", "garden_2", "garden_3", "garden_4" }));
            Assert.That(Database.GardenSettings.untendedPausesGrowth, Is.False, "forgiving by default");
            Assert.That((Herbs.produce.id, Herbs.growthDays, Herbs.yield), Is.EqualTo(("herbs", 2, 3)));
            Assert.That((Onions.produce.id, Onions.growthDays, Onions.yield), Is.EqualTo(("onion", 3, 3)));
            Assert.That((Barley.produce.id, Barley.growthDays, Barley.yield), Is.EqualTo(("malt", 4, 2)), "barley gives malt for now");
            foreach (CropDefinition crop in new[] { Herbs, Onions, Barley })
                Assert.That(new[] { crop.seeds, crop.sprout, crop.growing, crop.ready, crop.icon }, Has.None.Null, $"{crop.id}'s looks");
        }

        // ---------- Vigor ----------

        [Test]
        public void Vigor_StartsFull_SpendsExactly_AndNeverGoesBelowZero()
        {
            var vigor = new Vigor();
            vigor.Configure(Costs);
            Assert.That((vigor.Max, vigor.Current, vigor.Spent), Is.EqualTo((6, 6, 0)));
            Assert.That(vigor.CanAfford(6) && !vigor.CanAfford(7));
            Assert.That(vigor.Spend(2));
            Assert.That(vigor.Current, Is.EqualTo(4));
            Assert.That(vigor.Spend(4), "the exact cost");
            Assert.That((vigor.Current, vigor.IsEmpty), Is.EqualTo((0, true)));
            Assert.That(vigor.Spend(1), Is.False, "nothing left");
            Assert.That(vigor.Current, Is.EqualTo(0), "never negative");
            Assert.That(vigor.Spend(0), "free work is always affordable");
            vigor.Refill();
            Assert.That(vigor.Current, Is.EqualTo(6));
        }

        [Test]
        public void Vigor_AnUnaffordableSpend_ChangesNothing()
        {
            var vigor = new Vigor();
            vigor.Configure(Costs);
            vigor.Spend(5);
            Assert.That(vigor.Spend(2), Is.False);
            Assert.That(vigor.Spent, Is.EqualTo(5));
        }

        [Test]
        public void Sleeping_RefillsVigor_AndNothingElseInTheDayTouchesIt()
        {
            GameState state = Daytime();
            state.AddGold(100);
            GardenRules.Plant(state.Garden, state.Vigor, Bed, Herbs, state.Day, Costs);
            int spent = state.Vigor.Spent;
            Assert.That(spent, Is.EqualTo(2));
            SupplyOffer offer = Database.market.offers[0];
            DayRules.Buy(state, Database.market, offer);
            DayRules.EatMeal(state, MealBuff.None);
            DayRules.StartEvening(state);
            DayRules.CompleteService(state, new ServiceReport());
            Assert.That(state.Vigor.Spent, Is.EqualTo(spent), "shopping, eating, Prep and service cost no Vigor");
            DayRules.CompleteDelve(state, DelveReport.Empty);
            Assert.That(state.Vigor.Spent, Is.EqualTo(spent), "nor does the delve");
            DayRules.Sleep(state, FreshnessSettings.Default, Database.Crop, GardenSettings.Default);
            Assert.That(state.Vigor.Current, Is.EqualTo(6), "a night's sleep");
        }

        // ---------- planting ----------

        [Test]
        public void AnEmptyBed_Plants_ForTwoVigor()
        {
            GameState state = Daytime(3);
            Assert.That(DayRules.PlantBed(state, Bed, Onions, Costs), Is.EqualTo(GardenResult.Done));
            BedState bed = state.Garden.Bed(Bed);
            Assert.That((bed.Crop, bed.PlantedDay, bed.Grown, bed.TendedDays), Is.EqualTo(("onions", 3, 0, 0)));
            Assert.That(state.Vigor.Current, Is.EqualTo(4));
            Assert.That(GardenRules.Stage(bed, Onions), Is.EqualTo(BedStage.Seeds));
            Assert.That(DayRules.PlantBed(state, Bed, Herbs, Costs), Is.EqualTo(GardenResult.Occupied));
            Assert.That(state.Vigor.Current, Is.EqualTo(4), "nothing spent on a refusal");
        }

        [Test]
        public void TooTiredToPlant_ChangesNothingAtAll()
        {
            GameState state = Daytime();
            state.Vigor.Spend(5);
            Assert.That(DayRules.PlantBed(state, Bed, Herbs, Costs), Is.EqualTo(GardenResult.TooTired));
            Assert.That(state.Garden.Bed(Bed).IsEmpty);
            Assert.That(state.Vigor.Current, Is.EqualTo(1));
        }

        [Test]
        public void TheGarden_WorksOnlyInTheDaytime_AndOnlyItsOwnBeds()
        {
            GameState state = Daytime();
            Assert.That(DayRules.PlantBed(state, "garden_99", Herbs, Costs), Is.EqualTo(GardenResult.UnknownBed));
            DayRules.StartEvening(state);
            Assert.That(DayRules.PlantBed(state, Bed, Herbs, Costs), Is.Not.EqualTo(GardenResult.Done));
            Assert.That(state.Garden.Bed(Bed).IsEmpty);
        }

        // ---------- tending ----------

        [Test]
        public void Tending_CostsOne_OncePerBedPerDay()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Barley, Costs);
            Assert.That(DayRules.TendBed(state, Bed, Barley, Costs), Is.EqualTo(GardenResult.Done), "the planting day counts");
            Assert.That(state.Vigor.Current, Is.EqualTo(3));
            Assert.That(DayRules.TendBed(state, Bed, Barley, Costs), Is.EqualTo(GardenResult.TendedToday));
            Assert.That((state.Vigor.Current, state.Garden.Bed(Bed).TendedDays), Is.EqualTo((3, 1)), "no second spend, no second credit");
            NextDay(state);
            Assert.That(DayRules.TendBed(state, Bed, Barley, Costs), Is.EqualTo(GardenResult.Done), "a new day");
            Assert.That(state.Garden.Bed(Bed).TendedDays, Is.EqualTo(2));
        }

        [Test]
        public void ARestingOrEmptyBed_CantBeTended()
        {
            GameState state = Daytime();
            Assert.That(DayRules.TendBed(state, Bed, Herbs, Costs), Is.EqualTo(GardenResult.NotGrowing));
            state.Vigor.Spend(6);
            DayRules.PlantBed(state, Bed, Herbs, Costs);
            Assert.That(state.Garden.Bed(Bed).IsEmpty, "too tired to plant");
        }

        // ---------- growth ----------

        [TestCase("herbs", 2)]
        [TestCase("onions", 3)]
        [TestCase("barley", 4)]
        public void ACrop_IsReady_AfterItsNightsOfGrowth_UntendedOrNot(string cropId, int days)
        {
            GameState state = Daytime();
            CropDefinition crop = Database.Crop(cropId);
            DayRules.PlantBed(state, Bed, crop, Costs);
            for (int night = 1; night <= days; night++)
            {
                Assert.That(GardenRules.IsReady(state.Garden.Bed(Bed), crop), Is.False, $"not before night {night}");
                NextDay(state);
            }
            BedState bed = state.Garden.Bed(Bed);
            Assert.That(GardenRules.IsReady(bed, crop), $"ready on day {1 + days}");
            Assert.That(GardenRules.Stage(bed, crop), Is.EqualTo(BedStage.Ready));
            Assert.That(GardenRules.HarvestQuality(bed, crop), Is.EqualTo(Quality.Standard), "untended: still grown, just not Fine");
        }

        [Test]
        public void Growth_HappensOncePerNewDay_HoweverOftenItsAsked()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Barley, Costs);
            Assert.That(GardenRules.GrowOvernight(state.Garden, 1, Database.Crop, GardenSettings.Default), Is.EqualTo(0), "not on its planting day");
            Assert.That(GardenRules.GrowOvernight(state.Garden, 2, Database.Crop, GardenSettings.Default), Is.EqualTo(1));
            Assert.That(GardenRules.GrowOvernight(state.Garden, 2, Database.Crop, GardenSettings.Default), Is.EqualTo(0), "asked again for day 2");
            Assert.That(GardenRules.GrowOvernight(state.Garden, 1, Database.Crop, GardenSettings.Default), Is.EqualTo(0), "an old day");
            Assert.That(state.Garden.Bed(Bed).Grown, Is.EqualTo(1));
        }

        [Test]
        public void Growth_SurvivesASaveAndLoad_WithoutGrowingTwice()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Onions, Costs);
            NextDay(state);
            Assert.That(state.Garden.Bed(Bed).Grown, Is.EqualTo(1));
            GameState loaded = SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(state))), Database);
            GardenRules.GrowOvernight(loaded.Garden, loaded.Day, Database.Crop, GardenSettings.Default);
            Assert.That(loaded.Garden.Bed(Bed).Grown, Is.EqualTo(1), "that night's growth is already counted");
        }

        [Test]
        public void ARipeCrop_WaitsForAsLongAsItTakes()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Herbs, Costs);
            for (int i = 0; i < 12; i++) NextDay(state);
            BedState bed = state.Garden.Bed(Bed);
            Assert.That((GardenRules.IsReady(bed, Herbs), bed.Grown, bed.Crop), Is.EqualTo((true, 2, "herbs")), "no rot, no death");
        }

        [Test]
        public void TheStricterSwitch_PausesAnUntendedCrop()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Herbs, Costs);
            var strict = new GardenSettings { untendedPausesGrowth = true };
            Assert.That(GardenRules.GrowOvernight(state.Garden, 2, Database.Crop, strict), Is.EqualTo(0), "untended on day 1");
            state.Garden.Bed(Bed).LastTendedDay = 2;
            Assert.That(GardenRules.GrowOvernight(state.Garden, 3, Database.Crop, strict), Is.EqualTo(1), "tended on day 2");
        }

        // ---------- tending and quality ----------

        [TestCase("herbs", 1)]
        [TestCase("onions", 2)]
        [TestCase("barley", 2)]
        public void TendingOnHalfItsDays_MakesTheHarvestFine(string cropId, int tends)
        {
            CropDefinition crop = Database.Crop(cropId);
            Assert.That(GardenRules.TendsForFine(crop), Is.EqualTo(tends));
            var bed = new BedState { Id = Bed, Crop = cropId, Grown = crop.growthDays, TendedDays = tends - 1 };
            Assert.That(GardenRules.HarvestQuality(bed, crop), Is.EqualTo(Quality.Standard));
            bed.TendedDays = tends;
            Assert.That(GardenRules.HarvestQuality(bed, crop), Is.EqualTo(Quality.Fine));
        }

        // ---------- harvesting ----------

        [Test]
        public void AHarvest_IsFree_GoesToTheStoreroomFresh_AndEmptiesTheBed()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Onions, Costs);
            DayRules.TendBed(state, Bed, Onions, Costs);
            NextDay(state);
            DayRules.TendBed(state, Bed, Onions, Costs);
            NextDay(state);
            NextDay(state);
            state.Vigor.Spend(6);
            Assert.That(DayRules.HarvestBed(state, Bed, Onions, Costs, out IngredientStack produce), Is.EqualTo(GardenResult.Done), "free, even at 0 Vigor");
            Assert.That((produce.Item.Definition.id, produce.Count, produce.Item.Quality, produce.Freshness), Is.EqualTo(("onion", 3, Quality.Fine, 1f)));
            Assert.That(state.Storeroom.CountMatching(i => i.Definition.id == "onion" && i.Quality == Quality.Fine), Is.EqualTo(3), "an ordinary stack");
            Assert.That(state.Garden.Bed(Bed).IsEmpty);
            Assert.That(DayRules.HarvestBed(state, Bed, Onions, Costs, out _), Is.EqualTo(GardenResult.NotReady));
        }

        [Test]
        public void AnUnripeBed_CantBeHarvested()
        {
            GameState state = Daytime();
            DayRules.PlantBed(state, Bed, Herbs, Costs);
            Assert.That(DayRules.HarvestBed(state, Bed, Herbs, Costs, out IngredientStack produce), Is.EqualTo(GardenResult.NotReady));
            Assert.That(produce.IsEmpty);
            Assert.That(state.Garden.Bed(Bed).Crop, Is.EqualTo("herbs"));
        }

        // ---------- the save (version 10) ----------

        static GameState Played()
        {
            GameState state = Daytime(4);
            state.Surface.Restore(13 * 60 + 20);
            DayRules.PlantBed(state, GardenConfig.Bed2, Barley, Costs);
            DayRules.TendBed(state, GardenConfig.Bed2, Barley, Costs);
            state.Garden.Bed(GardenConfig.Bed3).Crop = "herbs";
            state.Garden.Bed(GardenConfig.Bed3).Grown = 2;
            // The world's seed is made once by the game; here, by the save.
            SaveData data = SaveSystem.Capture(state);
            data.world.seed = 12345;
            return SaveSystem.Restore(data, Database);
        }

        static GameState RoundTrip(GameState state) => SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(state))), Database);

        [Test]
        public void Version10_RoundTrips_TheSeedTheMinuteVigorAndTheGarden()
        {
            GameState state = Played();
            GameState loaded = RoundTrip(state);
            Assert.That(SaveSystem.CurrentVersion, Is.EqualTo(10));
            Assert.That(loaded.WorldSeed, Is.EqualTo(12345));
            Assert.That(loaded.Surface.WholeMinute, Is.EqualTo(13 * 60 + 20));
            Assert.That((loaded.Vigor.Spent, loaded.Vigor.Current), Is.EqualTo((3, 3)));
            Assert.That(loaded.Garden.Initialized);
            Assert.That(loaded.Garden.Beds.Select(b => b.Id), Is.EqualTo(state.Garden.Beds.Select(b => b.Id)));
            foreach (BedState bed in state.Garden.Beds)
            {
                BedState back = loaded.Garden.Bed(bed.Id);
                Assert.That((back.Crop, back.PlantedDay, back.Grown, back.TendedDays, back.LastTendedDay, back.LastGrownDay),
                    Is.EqualTo((bed.Crop, bed.PlantedDay, bed.Grown, bed.TendedDays, bed.LastTendedDay, bed.LastGrownDay)), bed.Id);
            }
            Assert.That(SaveSystem.ToJson(SaveSystem.Capture(RoundTrip(loaded))), Is.EqualTo(SaveSystem.ToJson(SaveSystem.Capture(loaded))), "stable over repeated loads");
        }

        [Test]
        public void AnUnknownCrop_InASave_LeavesAnEmptyBed()
        {
            SaveData data = SaveSystem.Capture(Played());
            data.garden.beds.First(b => b.id == GardenConfig.Bed2).crop = "moonbeans";
            var warnings = new List<string>();
            GameState loaded = SaveSystem.Restore(data, Database, warnings);
            Assert.That(loaded.Garden.Bed(GardenConfig.Bed2).IsEmpty);
            Assert.That(warnings, Has.Some.Contains("moonbeans"));
        }

        /// <summary>A save as an older version wrote it: no world, surface or garden (version 10's), the rest as captured.</summary>
        static string AsVersion(GameState state, int version)
        {
            SaveData data = SaveSystem.Capture(state);
            data.version = version;
            string json = SaveSystem.ToJson(data);
            json = Regex.Replace(json, @",\s*""world"":[\s\S]*\}\s*$", "\n}");
            Assert.That(json, Does.Not.Contain("\"world\"").And.Not.Contain("\"garden\"").And.Not.Contain("\"vigorSpent\""), "a genuine old file");
            return json;
        }

        static GameState OldGame()
        {
            var state = new GameState(5, DayPhase.Night);
            state.AddGold(77);
            state.AddRenown(9);
            state.Story.Opening = Hearthdelve.Shared.Story.OpeningStage.Complete;
            state.Story.CreationComplete = true;
            return state;
        }

        [TestCase(9)]
        [TestCase(8)]
        [TestCase(7)]
        public void AnOlderSave_MigratesToVersion10_WithASeedAMorningFullVigorAndFourEmptyBeds(int version)
        {
            string json = AsVersion(OldGame(), version);
            SaveData migrated = SaveSystem.FromJson(json);
            Assert.That(migrated.version, Is.EqualTo(10));
            GameState state = SaveSystem.Restore(migrated, Database);
            Assert.That(state.WorldSeed, Is.Not.EqualTo(0));
            Assert.That(state.Surface.WholeMinute, Is.EqualTo(SurfaceClockSettings.Default.dayStartMinute));
            Assert.That((state.Vigor.Current, state.Vigor.Max), Is.EqualTo((6, 6)));
            Assert.That(state.Garden.Beds.Select(b => b.Id), Is.EqualTo(Database.GardenBeds));
            Assert.That(state.Garden.Beds.All(b => b.IsEmpty));
            Assert.That((state.Day, state.Phase, state.Gold, state.Renown), Is.EqualTo((5, DayPhase.Night, 77, 9)), "nothing that existed changes");
        }

        [Test]
        public void AMigratedSeed_IsTheSame_EveryTimeTheOldFileIsLoaded_AndThenKept()
        {
            string json = AsVersion(OldGame(), 9);
            int first = SaveSystem.Restore(SaveSystem.FromJson(json), Database).WorldSeed;
            int again = SaveSystem.Restore(SaveSystem.FromJson(json), Database).WorldSeed;
            Assert.That(again, Is.EqualTo(first), "the same file, the same seed");
            GameState other = OldGame();
            other.AddGold(1);
            Assert.That(SaveSystem.Restore(SaveSystem.FromJson(AsVersion(other, 9)), Database).WorldSeed, Is.Not.EqualTo(first), "another game, another seed");
            GameState saved = SaveSystem.Restore(SaveSystem.FromJson(json), Database);
            Assert.That(RoundTrip(saved).WorldSeed, Is.EqualTo(first), "kept once it's saved as version 10");
        }

        [Test]
        public void AVersion10Save_KeepsItsGarden_OnEveryLoad()
        {
            GameState state = Played();
            string json = SaveSystem.ToJson(SaveSystem.Capture(state));
            for (int i = 0; i < 3; i++)
            {
                GameState loaded = SaveSystem.Restore(SaveSystem.FromJson(json), Database);
                Assert.That(loaded.Garden.Bed(GardenConfig.Bed2).Crop, Is.EqualTo("barley"));
                Assert.That(loaded.Garden.Bed(GardenConfig.Bed3).Grown, Is.EqualTo(2));
                Assert.That(loaded.Garden.Beds.Count, Is.EqualTo(4), "no bed given twice");
                json = SaveSystem.ToJson(SaveSystem.Capture(loaded));
            }
        }

        [Test]
        public void ANewBed_InTheTuning_ArrivesEmpty_InAnExistingGarden()
        {
            SaveData data = SaveSystem.Capture(Played());
            GameState loaded = SaveSystem.Restore(data, Database.Ingredient, _ => true, null, null, null, null, Database.Crop,
                Database.GardenBeds.Concat(new[] { "garden_5" }), Costs);
            Assert.That(loaded.Garden.Bed("garden_5").IsEmpty);
            Assert.That(loaded.Garden.Bed(GardenConfig.Bed2).Crop, Is.EqualTo("barley"), "the others as they were");
        }

        // ---------- the economy ----------

        [Test]
        public void TheGardensBestWeek_IsStaples_NotAFortune()
        {
            int garden = BalanceReport.GardenWeekValue();
            BalanceReport.Row delve = BalanceReport.Run(BalanceReport.Scenarios().Single(s => s.Name.StartsWith("ordinary Cellars delve")));
            BalanceReport.Row market = BalanceReport.Run(BalanceReport.Scenarios().Single(s => s.Name.StartsWith("market only, competent")));
            Assert.That(garden, Is.GreaterThan(0));
            // The plan's rule (§13): a garden-only week (market nights, plus every staple the garden could grow) never beats a delve
            // week at the same skill; and the garden alone stays a small part of what the Hollows bring.
            Assert.That(7 * market.ProfitUsed + garden, Is.LessThan(7 * delve.ProfitUsed), "a garden week doesn't beat a delve week");
            Assert.That(garden, Is.LessThan(7 * delve.ProfitUsed / 4), "the garden's best week is under a quarter of a delve week");
        }
    }
}
