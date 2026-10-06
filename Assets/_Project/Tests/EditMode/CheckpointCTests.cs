using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Run;
using Hearthdelve.Shared.Save;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4f Checkpoint C (pure rules): furnishing discoveries and their duplicate rules, keeping and losing them, the boss
    /// trophy (and old saves that beat the boss before it existed), the market, the Butcher Block's yield, and the v7 save.
    /// </summary>
    public class CurioRulesTests
    {
        readonly List<Object> m_Made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_Made) Object.DestroyImmediate(o);
            m_Made.Clear();
        }

        FurnitureDefinition Piece(string id, bool unique = false)
        {
            var d = ScriptableObject.CreateInstance<FurnitureDefinition>();
            d.id = id;
            d.unique = unique;
            m_Made.Add(d);
            return d;
        }

        static CurioEntry Entry(FurnitureDefinition piece, float weight = 1f, bool rooms = true, params string[] droppedBy) =>
            new() { piece = piece, weight = weight, inRoomCaches = rooms, droppedBy = droppedBy };

        static int None(string _) => 0;

        [Test]
        public void TheRoll_IsSeeded_SoTheSameSeedFindsTheSameThings()
        {
            var entries = new List<CurioEntry> { Entry(Piece("a")), Entry(Piece("b")), Entry(Piece("c")) };
            string[] Run(int seed)
            {
                var random = new SeededRandom(seed);
                return Enumerable.Range(0, 20).Select(_ => CurioRules.Roll(entries, CurioRules.RoomOrigin, None, null, random, 3f).id).ToArray();
            }
            Assert.That(Run(7), Is.EqualTo(Run(7)));
            Assert.That(Run(7), Is.Not.EqualTo(Run(8)), "different seeds, different finds");
        }

        [Test]
        public void PiecesNotOwnedYet_AreWeightedUp_ByTheFirstCopyWeight()
        {
            FurnitureDefinition owned = Piece("owned"), fresh = Piece("fresh");
            var entries = new List<CurioEntry> { Entry(owned), Entry(fresh) };
            int Owned(string id) => id == "owned" ? 2 : 0;
            Assert.That(CurioRules.Weight(entries[0], CurioRules.RoomOrigin, Owned, null, 3f), Is.EqualTo(1f));
            Assert.That(CurioRules.Weight(entries[1], CurioRules.RoomOrigin, Owned, null, 3f), Is.EqualTo(3f));
            var random = new SeededRandom(11);
            int freshCount = Enumerable.Range(0, 4000).Count(_ => CurioRules.Roll(entries, CurioRules.RoomOrigin, Owned, null, random, 3f) == fresh);
            Assert.That(freshCount / 4000f, Is.EqualTo(0.75f).Within(0.03f), "3 to 1");
        }

        [Test]
        public void UsefulPieces_CanRepeat_ButOwnedUniquesAreNeverRolled()
        {
            FurnitureDefinition candles = Piece("skull_candle"), heap = Piece("junk_heap", unique: true);
            var entries = new List<CurioEntry> { Entry(candles), Entry(heap) };
            int Owned(string id) => 1;
            var random = new SeededRandom(3);
            var rolled = Enumerable.Range(0, 200).Select(_ => CurioRules.Roll(entries, CurioRules.RoomOrigin, Owned, null, random, 3f)).ToList();
            Assert.That(rolled, Has.All.EqualTo(candles), "a second skull candle, never a second junk heap");
        }

        [Test]
        public void AUniqueFoundEarlierThisDelve_IsNotFoundTwice()
        {
            FurnitureDefinition heap = Piece("junk_heap", unique: true);
            var entries = new List<CurioEntry> { Entry(heap) };
            Assert.That(CurioRules.Roll(entries, CurioRules.RoomOrigin, None, new[] { "junk_heap" }, new SeededRandom(1), 3f), Is.Null);
        }

        [Test]
        public void AnExhaustedPool_RollsNothing_AndTheRewardFallsBackToRunGold()
        {
            var entries = new List<CurioEntry> { Entry(Piece("u1", true)), Entry(Piece("u2", true)) };
            Assert.That(CurioRules.Roll(entries, CurioRules.RoomOrigin, _ => 1, null, new SeededRandom(1), 3f), Is.Null);
            var pool = ScriptableObject.CreateInstance<CurioPool>();
            m_Made.Add(pool);
            var random = new SeededRandom(5);
            for (int i = 0; i < 50; i++) Assert.That(CurioRules.FallbackGold(pool, random), Is.InRange(pool.fallbackGoldMin, pool.fallbackGoldMax));
        }

        [Test]
        public void EnemiesDrop_OnlyWhatTheyCarry_RoomsOnlyTheirCaches_AndTheDelveCapComesFirst()
        {
            FurnitureDefinition web = Piece("cobweb"), jars = Piece("slime_jars");
            var entries = new List<CurioEntry> { Entry(web, rooms: false, droppedBy: "spider"), Entry(jars, rooms: true) };
            var random = new SeededRandom(9);
            Assert.That(CurioRules.Roll(entries, "spider", None, null, random, 3f), Is.EqualTo(web));
            Assert.That(CurioRules.Roll(entries, "bat", None, null, random, 3f), Is.Null, "bats carry nothing");
            Assert.That(CurioRules.Roll(entries, CurioRules.RoomOrigin, None, null, random, 3f), Is.EqualTo(jars));
            var pool = ScriptableObject.CreateInstance<CurioPool>();
            m_Made.Add(pool);
            Assert.That(CurioRules.EnemyDrops(pool, 0, 0f));
            Assert.That(CurioRules.EnemyDrops(pool, pool.maxEnemyDropsPerDelve, 0f), Is.False, "the cap");
            Assert.That(CurioRules.EnemyDrops(pool, 0, pool.enemyDropChance), Is.False);
        }

        [Test]
        public void ExtractingKeepsTheCurios_MarkedNewInStorage()
        {
            FurnitureDefinition candle = Piece("skull_candle");
            var state = new GameState(1, DayPhase.Delve);
            var loot = new RunLoot();
            loot.AddCurio("skull_candle");
            loot.AddCurio("skull_candle", fromEnemy: true);
            DayRules.CompleteDelve(state, DelveReport.Extraction(new Satchel(6, 3), 0, null, loot.Curios), id => id == "skull_candle" ? candle : null);
            Assert.That(state.Furniture.OwnedCount("skull_candle"), Is.EqualTo(2));
            Assert.That(state.Furniture.InStorage("skull_candle"), Is.EqualTo(2), "in storage, not placed");
            Assert.That(state.Furniture.IsNew("skull_candle"));
            Assert.That(state.Today.CuriosKept, Is.EqualTo(2));
            Assert.That(loot.CuriosDropped, Is.EqualTo(1));
        }

        [Test]
        public void DyingLosesEveryCurio_TheLockboxNeverHoldsOne()
        {
            FurnitureDefinition candle = Piece("skull_candle");
            var state = new GameState(1, DayPhase.Delve);
            var satchel = new Satchel(6, 3);
            DeathPenaltyResult death = DeathPenalty.Resolve(satchel, 0, 0);
            DayRules.CompleteDelve(state, DelveReport.Death(death, 0, null, new[] { "skull_candle" }), _ => candle);
            Assert.That(state.Furniture.OwnedCount("skull_candle"), Is.Zero);
            Assert.That(state.Today.CuriosLost, Is.EqualTo(1));
            Assert.That(satchel.Slots.All(s => s.IsEmpty), "curios never took a satchel slot");
        }
    }

    public class TrophyTests
    {
        readonly List<Object> m_Made = new();
        FurnitureDefinition m_Tusks;
        BossTrophy[] m_Trophies;

        [SetUp]
        public void SetUp()
        {
            m_Tusks = ScriptableObject.CreateInstance<FurnitureDefinition>();
            m_Tusks.id = "trophy_larder_troll";
            m_Tusks.unique = true;
            m_Tusks.sources = FurnitureSource.Boss;
            m_Made.Add(m_Tusks);
            m_Trophies = new[] { new BossTrophy { bossId = "larder_troll", trophy = m_Tusks } };
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_Made) Object.DestroyImmediate(o);
            m_Made.Clear();
        }

        static GameState Delve() => new(1, DayPhase.Delve);

        [Test]
        public void TheTrophy_IsGrantedWithTheVictory_EvenIfTheDelveEndsInDeath_AndAwaitsItsHomecoming()
        {
            GameState state = Delve();
            DayRules.CompleteDelve(state, DelveReport.Death(DeathPenalty.Resolve(new Satchel(6, 3), DeathPenalty.KeepNothing, 0), 0, new[] { "larder_troll" }),
                null, m_Trophies);
            Assert.That(state.Furniture.OwnedCount("trophy_larder_troll"), Is.EqualTo(1));
            Assert.That(state.Furniture.PendingHomecoming, Is.EqualTo("trophy_larder_troll"));
        }

        [Test]
        public void TheTrophy_IsNeverLost_NorGrantedTwice()
        {
            GameState state = Delve();
            DayRules.CompleteDelve(state, DelveReport.Extraction(new Satchel(6, 3), 0, new[] { "larder_troll" }), null, m_Trophies);
            state.Furniture.ClearNew("trophy_larder_troll");
            state.Furniture.PendingHomecoming = null;
            DayRules.Sleep(state, FreshnessSettings.Default);
            DayRules.StartEvening(state);
            DayRules.SkipService(state);
            // A later death, then a second victory.
            DayRules.CompleteDelve(state, DelveReport.Death(DeathPenalty.Resolve(new Satchel(6, 3), DeathPenalty.KeepNothing, 0), 0, new[] { "larder_troll" }),
                null, m_Trophies);
            Assert.That(state.Furniture.OwnedCount("trophy_larder_troll"), Is.EqualTo(1));
            Assert.That(state.Furniture.PendingHomecoming, Is.Null, "one homecoming");
            Assert.That(TrophyRules.Unearned(state, m_Trophies, "larder_troll"), Is.Null);
        }

        [Test]
        public void ASaveThatBeatTheBossBeforeTrophies_GetsItExactlyOnce_JustAsTheNormalPathWould()
        {
            // The normal path.
            GameState played = Delve();
            DayRules.CompleteDelve(played, DelveReport.Extraction(new Satchel(6, 3), 0, new[] { "larder_troll" }), null, m_Trophies);

            // A version 6 save: the Larder Troll cleared twice, no trophy.
            const string v6 = "{\"version\":6,\"day\":4,\"phase\":\"Night\",\"gold\":50,\"bosses\":[{\"id\":\"larder_troll\",\"clears\":2}]," +
                              "\"furniture\":{\"initialized\":true,\"nextUid\":1,\"owned\":[],\"areas\":[]}}";
            SaveData data = SaveSystem.FromJson(v6);
            Assert.That(data.version, Is.EqualTo(SaveSystem.CurrentVersion));
            GameState loaded = SaveSystem.Restore(data, _ => null, _ => true, trophies: m_Trophies);
            Assert.That(loaded.Furniture.OwnedCount("trophy_larder_troll"), Is.EqualTo(1));
            Assert.That(loaded.Furniture.PendingHomecoming, Is.EqualTo(played.Furniture.PendingHomecoming));
            Assert.That(loaded.Furniture.IsNew("trophy_larder_troll"), Is.EqualTo(played.Furniture.IsNew("trophy_larder_troll")));

            // Saved and loaded again: still one.
            GameState again = SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(loaded))), _ => null, _ => true, trophies: m_Trophies);
            Assert.That(again.Furniture.OwnedCount("trophy_larder_troll"), Is.EqualTo(1));
            Assert.That(again.Furniture.PendingHomecoming, Is.EqualTo("trophy_larder_troll"), "still waiting to be hung");
        }

        [Test]
        public void ASaveThatNeverBeatTheBoss_GetsNoTrophy()
        {
            const string v6 = "{\"version\":6,\"day\":2,\"phase\":\"Night\",\"furniture\":{\"initialized\":true,\"nextUid\":1,\"owned\":[],\"areas\":[]}}";
            GameState loaded = SaveSystem.Restore(SaveSystem.FromJson(v6), _ => null, _ => true, trophies: m_Trophies);
            Assert.That(loaded.Furniture.OwnedCount("trophy_larder_troll"), Is.Zero);
            Assert.That(loaded.Furniture.PendingHomecoming, Is.Null);
        }
    }

    public class MarketAndButcherTests
    {
        readonly List<Object> m_Made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_Made) Object.DestroyImmediate(o);
            m_Made.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            m_Made.Add(o);
            return o;
        }

        IngredientDefinition Ingredient(string id, IngredientSource source = IngredientSource.Hollows)
        {
            var d = Make<IngredientDefinition>();
            d.id = id;
            d.source = source;
            return d;
        }

        (IngredientDefinition leg, IngredientDefinition cuts) Leg()
        {
            IngredientDefinition cuts = Ingredient("spider_leg_cuts");
            IngredientDefinition leg = Ingredient("spider_leg");
            leg.butchering = new ButcheringSettings { cut = cuts, minCuts = 1, maxCuts = 3, thresholds = new[] { 0.5f, 0.85f } };
            return (leg, cuts);
        }

        [Test]
        public void BuyingAtTheMarket_CostsGold_AndDeliversStandardFreshStock_StraightToTheStoreroom()
        {
            IngredientDefinition onion = Ingredient("onion", IngredientSource.Market);
            var market = Make<SupplySource>();
            var offer = new SupplyOffer { ingredient = onion, price = 2, bundle = 2 };
            market.offers.Add(offer);
            var state = new GameState();
            state.AddGold(5);
            Assert.That(DayRules.Buy(state, market, offer));
            Assert.That(state.Gold, Is.EqualTo(3));
            IngredientStack stack = state.Storeroom.Stacks.Single();
            Assert.That((stack.Item.Definition, stack.Item.Quality, stack.Count), Is.EqualTo((onion, Quality.Standard, 2)));
            Assert.That(stack.Freshness, Is.EqualTo(1f));
            Assert.That(DayRules.Buy(state, market, offer));
            Assert.That(DayRules.Buy(state, market, offer), Is.False, "1 gold left");
            Assert.That(state.Gold, Is.EqualTo(1));
            Assert.That(state.Storeroom.TotalCount, Is.EqualTo(4));
        }

        [Test]
        public void TheMarket_IsADaytimeErrand()
        {
            var market = Make<SupplySource>();
            var offer = new SupplyOffer { ingredient = Ingredient("bread", IngredientSource.Market), price = 1, bundle = 1 };
            var state = new GameState(1, DayPhase.Evening);
            state.AddGold(10);
            Assert.Throws<System.InvalidOperationException>(() => DayRules.Buy(state, market, offer));
        }

        [Test]
        public void MarketStock_AgesLikeAnyOther()
        {
            var market = Make<SupplySource>();
            var offer = new SupplyOffer { ingredient = Ingredient("eggs", IngredientSource.Market), price = 1, bundle = 1 };
            var state = new GameState();
            state.AddGold(10);
            DayRules.Buy(state, market, offer);
            DayRules.StartEvening(state);
            DayRules.SkipService(state);
            DayRules.CompleteDelve(state, DelveReport.Empty);
            DayRules.Sleep(state, FreshnessSettings.Default);
            Assert.That(state.Storeroom.Stacks.Single().Freshness, Is.LessThan(1f));
        }

        [TestCase(0f, 1)]
        [TestCase(0.49f, 1)]
        [TestCase(0.5f, 2)]
        [TestCase(0.84f, 2)]
        [TestCase(0.85f, 3)]
        [TestCase(1f, 3)]
        public void TheCut_SetsTheYield(float score, int cuts)
        {
            Assert.That(ButcherRules.Yield(Leg().leg.butchering, score), Is.EqualTo(cuts));
        }

        [Test]
        public void Cuts_InheritThePartsQualityAndFreshness_AndTakeExactlyThatPart()
        {
            var (leg, cuts) = Leg();
            var storeroom = new Storeroom();
            storeroom.Add(new IngredientStack(new IngredientItem(leg, Quality.Fine), 1, 0.6f));
            storeroom.Add(new IngredientStack(new IngredientItem(leg, Quality.Standard), 2, 0.9f));
            IngredientStack result = ButcherRules.Butcher(storeroom, new IngredientItem(leg, Quality.Fine), 0.9f);
            Assert.That((result.Item.Definition, result.Item.Quality, result.Count), Is.EqualTo((cuts, Quality.Fine, 3)));
            Assert.That(result.Freshness, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(storeroom.CountMatching(i => i.Definition == leg && i.Quality == Quality.Fine), Is.Zero);
            Assert.That(storeroom.CountMatching(i => i.Definition == leg && i.Quality == Quality.Standard), Is.EqualTo(2), "the others untouched");
            Assert.That(storeroom.CountMatching(i => i.Definition == cuts), Is.EqualTo(3));
        }

        [Test]
        public void APartThatIsntThere_OrDoesntBreakDown_GivesNothing_AndTakesNothing()
        {
            var (leg, _) = Leg();
            IngredientDefinition gel = Ingredient("slime_gel");
            var storeroom = new Storeroom();
            storeroom.Add(new IngredientStack(new IngredientItem(gel, Quality.Standard), 1, 1f));
            Assert.That(ButcherRules.Butcher(storeroom, new IngredientItem(leg, Quality.Standard), 1f).IsEmpty);
            Assert.That(ButcherRules.Butcher(storeroom, new IngredientItem(gel, Quality.Standard), 1f).IsEmpty);
            Assert.That(storeroom.TotalCount, Is.EqualTo(1));
        }

        [Test]
        public void TheButcherMinigame_ScoresAStraightCleanHand_AboveAShakyOne()
        {
            float Play(float skill, int seed)
            {
                var game = new ButcherMinigame(ButcherSettings.Default, 3, new SeededRandom(seed));
                return MinigameRunner.RunToCompletion(game, new ButcherAutoPlayer(game, skill, new SeededRandom(seed + 1)));
            }
            float steady = Enumerable.Range(1, 10).Average(s => Play(1f, s));
            float shaky = Enumerable.Range(1, 10).Average(s => Play(0.1f, s));
            Assert.That(steady, Is.GreaterThan(0.85f), "a perfect hand earns every cut");
            Assert.That(shaky, Is.LessThan(steady - 0.2f));
            var timed = new ButcherMinigame(ButcherSettings.Default, 3, new SeededRandom(1));
            MinigameRunner.RunToCompletion(timed, new ButcherAutoPlayer(timed, 1f, new SeededRandom(2)));
            Assert.That(timed.Elapsed, Is.InRange(3f, ButcherSettings.Default.timeLimit), "a 5–10 second job");
        }

        [Test]
        public void Gunta_ButchersSteadily_ButHerCapKeepsTheBestCutsForTheKeeper()
        {
            var tavern = AssetDatabase.LoadAssetAtPath<TavernContent>("Assets/_Project/Data/Tavern/TavernContent.asset");
            StaffDefinition gunta = tavern.staff.Single(s => s.id == StaffIds.Boog);
            Assert.That(tavern.staff.Any(s => s.id == StaffIds.Orik));
            var random = new SeededRandom(4);
            var scores = Enumerable.Range(0, 20).Select(_ =>
            {
                var game = new ButcherMinigame(ButcherSettings.Default, 3, random);
                return Mathf.Min(MinigameRunner.RunToCompletion(game, new ButcherAutoPlayer(game, gunta.skill, random)), gunta.qualityCap);
            }).ToList();
            Assert.That(scores, Has.All.LessThanOrEqualTo(gunta.qualityCap));
            Assert.That(scores.Average(), Is.GreaterThan(0.5f), "most of her parts give two cuts or more");
            Assert.That(gunta.qualityCap, Is.LessThan(0.85f + 1e-4f).And.LessThan(1f));
            Assert.That(tavern.butcher, Is.Not.Null, "the Butcher Block's tuning asset");
        }
    }

    /// <summary>The revised Cellars menu, the staples and the forage, as content (the assets the updater builds).</summary>
    public class CellarMenuTests
    {
        static GameDatabase Database => AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
        static TavernContent Tavern => AssetDatabase.LoadAssetAtPath<TavernContent>("Assets/_Project/Data/Tavern/TavernContent.asset");

        static readonly string[] k_Staples = { "onion", "herbs", "bread", "eggs", "malt" };

        static Storeroom Stock(IEnumerable<IngredientDefinition> ingredients, int each = 4)
        {
            var storeroom = new Storeroom();
            foreach (IngredientDefinition d in ingredients) storeroom.Add(new IngredientStack(new IngredientItem(d, Quality.Standard), each, 1f));
            return storeroom;
        }

        [Test]
        public void TheMarket_SellsTheFiveStaples_AsSurfaceFood()
        {
            SupplySource market = Database.market;
            Assert.That(market, Is.Not.Null);
            Assert.That(market.offers.Select(o => o.ingredient.id), Is.EquivalentTo(k_Staples));
            Assert.That(market.offers.Select(o => o.ingredient.source), Has.All.EqualTo(IngredientSource.Market));
            Assert.That(market.offers.Select(o => o.price), Has.All.GreaterThan(0));
            Assert.That(market.quality, Is.EqualTo(Quality.Standard));
            foreach (string grain in new[] { "bread", "malt" })
                Assert.That(Database.Ingredient(grain).category.HasFlag(IngredientCategory.Grain), grain);
            foreach (IngredientDefinition d in Database.ingredients.Where(d => !k_Staples.Contains(d.id)))
                Assert.That(d.source, Is.EqualTo(IngredientSource.Hollows), d.id);
        }

        [Test]
        public void TheMenu_HasThirteenDishes_InThreeTiers_WithEverydayDishesFromTheMarketAlone()
        {
            List<RecipeDefinition> recipes = Tavern.recipes.Where(r => r != null).ToList();
            Assert.That(recipes.Select(r => r.id), Is.EqualTo(new[]
            {
                "brackenford_ale", "onion_broth", "eggs_on_toast",
                "gelbrew", "grilled_spider_leg", "crispy_bat_wings", "shroom_skewer", "cellar_stew", "offal_pottage", "cellar_kebab",
                "core_tonic", "spider_leg_steaks", "bat_wing_platter",
            }));
            Storeroom market = Stock(Database.market.offers.Select(o => o.ingredient));
            foreach (RecipeDefinition r in recipes.Take(3)) Assert.That(PrepRules.Makeable(r, market), Is.GreaterThan(0), $"{r.id} from the market");
            foreach (RecipeDefinition r in recipes.Skip(3)) Assert.That(PrepRules.Makeable(r, market), Is.Zero, $"{r.id} needs something from below");
            Storeroom everything = Stock(Database.ingredients);
            foreach (RecipeDefinition r in recipes) Assert.That(PrepRules.Makeable(r, everything), Is.GreaterThan(0), r.id);
            Assert.That(recipes.Select(r => r.icon), Has.None.Null);
        }

        [Test]
        public void TheSignatureCutDishes_NeedTheButcherBlock()
        {
            Storeroom wholeParts = Stock(Database.ingredients.Where(d => !d.id.EndsWith("_cuts")));
            foreach (string id in new[] { "spider_leg_steaks", "bat_wing_platter" })
                Assert.That(PrepRules.Makeable(Tavern.recipes.Single(r => r.id == id), wholeParts), Is.Zero, id);
            foreach (string part in new[] { "spider_leg", "bat_wing" })
                Assert.That(Database.Ingredient(part).Butcherable, part);
        }

        [Test]
        public void TheMushrooms_AreCellarsForage()
        {
            var settings = AssetDatabase.LoadAssetAtPath<RunSettings>("Assets/_Project/Data/Dungeon/RunSettings.asset");
            IngredientRewardOption cap = settings.tuning.ingredientRewards.Single(o => o.ingredient.id == "shroom_cap");
            IngredientRewardOption spores = settings.tuning.ingredientRewards.Single(o => o.ingredient.id == "spore_sac");
            Assert.That(cap.fromFloor, Is.EqualTo(1));
            Assert.That(spores.fromFloor, Is.EqualTo(2));
            Assert.That(cap.ingredient.icon, Is.Not.Null);
            Assert.That(spores.ingredient.icon, Is.Not.Null);
            Assert.That(settings.tuning.curios, Is.Not.Null, "and the Cellars' furnishing pool");
        }
    }

    public class SaveVersion7Tests
    {
        readonly List<Object> m_Made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_Made) Object.DestroyImmediate(o);
            m_Made.Clear();
        }

        FurnitureStartingLayout Start()
        {
            var start = ScriptableObject.CreateInstance<FurnitureStartingLayout>();
            m_Made.Add(start);
            start.areas.Add(new AreaLayoutData
            {
                area = "tavern",
                pieces =
                {
                    new PlacedFurniture { uid = 1, definition = "barrel", cell = new Vector2Int(2, 2) },
                    new PlacedFurniture { uid = 2, definition = "butcher_block", cell = new Vector2Int(22, 3) },
                },
            });
            return start;
        }

        [Test]
        public void NewPiecesAndTheHomecoming_SurviveTheSave()
        {
            var piece = ScriptableObject.CreateInstance<FurnitureDefinition>();
            piece.id = "skull_candle";
            m_Made.Add(piece);
            var state = new GameState();
            state.Furniture.GrantStarter(Start());
            state.Furniture.Receive(piece);
            state.Furniture.PendingHomecoming = "skull_candle";
            string json = SaveSystem.ToJson(SaveSystem.Capture(state));
            GameState back = SaveSystem.Restore(SaveSystem.FromJson(json), _ => null, _ => true, startingFurniture: Start());
            Assert.That(back.Furniture.IsNew("skull_candle"));
            Assert.That(back.Furniture.PendingHomecoming, Is.EqualTo("skull_candle"));
            Assert.That(back.Furniture.OwnedCount("butcher_block"), Is.EqualTo(1), "a version 7 save isn't granted it again");
        }

        [Test]
        public void AVersion6Tavern_GetsTheButcherBlockOnce_InStorage_AndASoldStarterStaysSold()
        {
            // A version 6 save whose keeper sold the barrel; no Butcher Block existed.
            const string v6 = "{\"version\":6,\"day\":3,\"phase\":\"Night\",\"furniture\":{\"initialized\":true,\"nextUid\":5,\"owned\":[]," +
                              "\"areas\":[{\"id\":\"tavern\",\"pieces\":[]}]}}";
            GameState loaded = SaveSystem.Restore(SaveSystem.FromJson(v6), _ => null, _ => true, startingFurniture: Start());
            Assert.That(loaded.Furniture.OwnedCount("butcher_block"), Is.EqualTo(1));
            Assert.That(loaded.Furniture.InStorage("butcher_block"), Is.EqualTo(1), "in storage: the layout is as it was");
            Assert.That(loaded.Furniture.IsNew("butcher_block"));
            Assert.That(loaded.Furniture.Layout("tavern"), Is.Empty);
            Assert.That(loaded.Furniture.OwnedCount("barrel"), Is.Zero, "sold stays sold");

            GameState again = SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(loaded))), _ => null, _ => true, startingFurniture: Start());
            Assert.That(again.Furniture.OwnedCount("butcher_block"), Is.EqualTo(1), "once");
        }
    }
}
