using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Run;
using Hearthdelve.Shared.Save;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4f Checkpoint C through <see cref="GameFlow"/> (saves in a temp folder): furnishing discoveries from a kill and a
    /// room, kept on extraction and lost on death; the Larder Troll's tusks, from the first clear and from an old save, and
    /// their homecoming; the Brackenford market; the revised menu through Prep and service with Pip's ledger; the Butcher
    /// Block by hand and by Gunta; Gunta at a station; and the staff's looks and beats.
    /// </summary>
    public class CheckpointCTests : LookTestFixture
    {
        string m_SaveDir;
        readonly List<string> m_Patterns = new();

        void Record(string id) => m_Patterns.Add(id);

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
            m_Patterns.Clear();
            Hearthdelve.Shared.Haptics.HapticService.PatternPlayed += Record;
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            Hearthdelve.Shared.Haptics.HapticService.PatternPlayed -= Record;
            RoomRunner.StartInArenaOverride = false;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        // ---------- Helpers (as DayLoopTests) ----------

        IEnumerator BootToMenu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            Haptics = new RecordingHapticOutput();
            Hearthdelve.Shared.Haptics.HapticService.Instance.Output = Haptics;
            yield return null;
        }

        static IEnumerator InTavern(TavernPhase phase, string what) =>
            WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, what);

        IEnumerator InDungeon()
        {
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return null;
            FreezeEnemies();
        }

        IEnumerator NewGameToTheDelve()
        {
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return InDungeon();
        }

        IEnumerator ExtractAndGoHome(Action<DelveResultScreen> look = null)
        {
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "extracted");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            look?.Invoke(result);
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night, home again");
        }

        IEnumerator SleepIntoDaytime()
        {
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the daytime");
            yield return null;
        }

        static IEnumerator OpenForTheEvening()
        {
            Object.FindAnyObjectByType<MorningScreen>().DescendButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Prep, "the evening's prep");
            yield return null;
        }

        SaveData SavedGame() => SaveSystem.FromJson(File.ReadAllText(Path.Combine(m_SaveDir, SaveStore.FileName)));

        static CurioPool Pool => RoomRunner.Active.Settings.tuning.curios;

        /// <summary>Walks the delver onto every curio on the floor and waits for the run to count them.</summary>
        IEnumerator PickUpCurios(int expected)
        {
            yield return new WaitForSeconds(0.6f); // a pickup can't be taken the moment it lands
            foreach (CurioPickup pickup in Object.FindObjectsByType<CurioPickup>())
            {
                Player.transform.position = pickup.transform.position;
                if (Player.TryGetComponent(out Rigidbody2D body)) body.position = pickup.transform.position;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
            }
            yield return WaitUntil(() => DelveRunController.Active.Loot.Curios.Count == expected, 3f, $"{expected} curios carried");
        }

        /// <summary>A kill by an enemy the pool knows, with drops made certain for the test.</summary>
        static void KillADropper(Vector2 at)
        {
            string enemy = Pool.entries.First(e => e.droppedBy != null && e.droppedBy.Length > 0).droppedBy[0];
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.id = enemy;
            EventBus<EnemyKilled>.Publish(new EnemyKilled(definition, default, at));
            Object.Destroy(definition);
        }

        static string[] ShownText(Component root) =>
            root.GetComponentsInChildren<SuperTextMesh>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToArray();

        // ---------- Discoveries ----------

        [UnityTest]
        public IEnumerator Curios_FromAKillAndARoom_AreCarriedApartFromTheSatchel_AndComeHomeNew()
        {
            yield return NewGameToTheDelve();
            CurioPool pool = Pool;
            float chance = pool.enemyDropChance;
            var home = new List<string>();
            void Brought(CurioBroughtHome e) => home.Add(e.FurnitureId);
            EventBus<CurioBroughtHome>.Subscribe(Brought);
            try
            {
                pool.enemyDropChance = 1f;
                var found = new List<string>();
                void Found(CurioFound e) => found.Add(e.FurnitureId);
                EventBus<CurioFound>.Subscribe(Found);
                KillADropper((Vector2)Player.transform.position + new Vector2(2f, 0f));
                yield return null;
                Assert.That(Object.FindObjectsByType<CurioPickup>(), Has.Length.EqualTo(1), "a kill dropped one");
                RoomRunner.Active.GrantReward(RoomReward.Curio());
                yield return null;
                Assert.That(Object.FindObjectsByType<CurioPickup>(), Has.Length.EqualTo(2), "and the room's cache");
                yield return PickUpCurios(2);
                Assert.That(found, Has.Count.EqualTo(2), "each pickup announced (toast, HUD)");
                Assert.That(m_Patterns, Has.Member(Hearthdelve.Core.Haptics.HapticIds.DiscoveryFound), "the discovery's named haptic");
                Assert.That(Player.GetComponent<SatchelCarrier>().Satchel.Slots.All(s => s.IsEmpty), "no satchel slot used");
                Assert.That(DelveRunController.Active.Loot.CuriosDropped, Is.EqualTo(1));
                EventBus<CurioFound>.Unsubscribe(Found);
            }
            finally
            {
                pool.enemyDropChance = chance;
            }
            List<string> carried = DelveRunController.Active.Loot.Curios.ToList();
            string shown = null;
            yield return ExtractAndGoHome(result => shown = result.CuriosText);
            EventBus<CurioBroughtHome>.Unsubscribe(Brought);
            Assert.That(shown, Does.StartWith("found for Tally Ho!: "));
            Assert.That(home, Is.EquivalentTo(carried));
            foreach (string id in carried.Distinct())
            {
                Assert.That(Flow.State.Furniture.OwnedCount(id), Is.EqualTo(carried.Count(c => c == id)), id);
                Assert.That(Flow.State.Furniture.IsNew(id), $"{id} is new in storage");
            }
            Assert.That(SavedGame().furniture.newPieces, Is.SupersetOf(carried.Distinct()), "saved, marked new");
            Assert.That(Flow.State.Today.CuriosKept, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Dying_LosesEveryCurio_AndTheDeathScreenSaysSo()
        {
            yield return NewGameToTheDelve();
            RoomRunner.Active.GrantReward(RoomReward.Curio());
            yield return PickUpCurios(1);
            string piece = DelveRunController.Active.Loot.Curios[0];
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            var essence = Player.GetComponent<EssenceHealth>();
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            yield return null;
            Assert.That(death.CuriosLostText, Does.StartWith("lost with you: "));
            death.KeepNothingButton.onClick.Invoke();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            Assert.That(result.CuriosText, Does.StartWith("found, then lost: "));
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night");
            Assert.That(Flow.State.Furniture.OwnedCount(piece) - (Flow.Database.startingFurniture.areas.SelectMany(a => a.pieces).Count(p => p.definition == piece)),
                Is.Zero, "nothing found came home");
            Assert.That(Flow.State.Today.CuriosLost, Is.EqualTo(1));
        }

        // ---------- The trophy ----------

        [UnityTest]
        public IEnumerator TheTrollsTusks_AreGrantedWithItsFall_AndHungAtTheirHomecoming()
        {
            RoomRunner.StartInArenaOverride = true;
            yield return NewGameToTheDelve();
            Player.GetComponent<EssenceHealth>().GodMode = true;
            var encounter = Object.FindAnyObjectByType<Hearthdelve.Dungeon.Bosses.BossEncounter>();
            yield return WaitUntil(() => encounter.State == Hearthdelve.Dungeon.Bosses.BossEncounterState.Fighting, 8f, "the troll");
            var health = encounter.GetComponent<Hearthdelve.Dungeon.Bosses.BossHealth>();
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            health.FinishOff(Player.gameObject, finisher: false);
            yield return WaitUntil(() => DelveRunController.Active.Loot.BossesDefeated.Count == 1, 3f, "the defeat recorded");
            string trophyLine = null;
            yield return ExtractAndGoHome(result => trophyLine = result.TrophyText);
            Assert.That(trophyLine, Is.EqualTo("a trophy: Larder Troll tusks"));
            const string tusks = "trophy_larder_troll";
            Assert.That(Flow.State.Furniture.OwnedCount(tusks), Is.EqualTo(1));
            Assert.That(Flow.State.Furniture.PendingHomecoming, Is.EqualTo(tusks));
            Assert.That(SavedGame().furniture.homecoming, Is.EqualTo(tusks), "the homecoming survives a reload");

            // The homecoming: Decorate opens with them on the cursor, to hang on the wall.
            string displayed = null;
            void Displayed(TrophyDisplayed e) => displayed = e.FurnitureId;
            EventBus<TrophyDisplayed>.Subscribe(Displayed);
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            night.DecorateButton.onClick.Invoke();
            yield return null;
            DecorateMode mode = DecorateMode.Instance;
            Assert.That(mode.HomecomingPiece, Is.EqualTo(tusks));
            Assert.That(mode.Carried?.definition, Is.EqualTo(tusks), "already in hand");
            Assert.That(Flow.State.Furniture.PendingHomecoming, Is.Null, "offered once");
            Assert.That(mode.CarriedCheck.IsValid, "it opens where they can hang");
            Vector2Int? wall = null;
            for (int x = 4; x < 24 && wall == null; x++)
            for (int y = 13; y <= 15 && wall == null; y++)
            {
                mode.SetCursor(new Vector2Int(x, y));
                if (mode.CarriedCheck.IsValid) wall = new Vector2Int(x, y);
            }
            Assert.That(wall, Is.Not.Null, "somewhere on the back wall");
            mode.Place();
            Assert.That(displayed, Is.EqualTo(tusks), "TrophyDisplayed, for 4g's reactions");
            Assert.That(mode.HomecomingPiece, Is.Null);
            mode.Leave();
            yield return null;
            EventBus<TrophyDisplayed>.Unsubscribe(Displayed);
            Assert.That(SavedGame().furniture.areas.Single(a => a.id == "tavern").pieces.Any(p => p.def == tusks), "hung, and saved");
            Assert.That(SavedGame().furniture.homecoming, Is.Null.Or.Empty);
        }

        [UnityTest]
        public IEnumerator ASaveThatBeatTheTrollBeforeTrophies_GetsTheTusksOnce_AndTheButcherBlock()
        {
            yield return NewGameToTheDelve();
            yield return ExtractAndGoHome();
            // Rewrite the save as a version 6 save from 4e/Checkpoint B: two clears, no tusks, no Butcher Block.
            string path = Path.Combine(m_SaveDir, SaveStore.FileName);
            SaveData data = SavedGame();
            data.version = 6;
            data.bosses = new List<BossClearData> { new() { id = "larder_troll", clears = 2 } };
            data.furniture.owned.RemoveAll(o => o.id == "butcher_block");
            foreach (AreaSaveData area in data.furniture.areas) area.pieces.RemoveAll(p => p.def == "butcher_block");
            data.furniture.newPieces = null;
            data.furniture.homecoming = null;
            File.WriteAllText(path, SaveSystem.ToJson(data).Replace("\"version\": 7", "\"version\": 6"));

            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Night, "the night, continued");
            Assert.That(Flow.State.Furniture.OwnedCount("trophy_larder_troll"), Is.EqualTo(1), "earned before trophies existed");
            Assert.That(Flow.State.Furniture.PendingHomecoming, Is.EqualTo("trophy_larder_troll"), "as if just earned");
            Assert.That(Flow.State.Furniture.OwnedCount("butcher_block"), Is.EqualTo(1));
            Assert.That(Flow.State.Furniture.Layout("tavern").Any(p => p.definition == "butcher_block"), Is.False, "in storage: the room is as it was");
            Assert.That(Flow.State.Furniture.IsNew("butcher_block"));
            Assert.That(SavedGame().version, Is.EqualTo(SaveSystem.CurrentVersion), "saved in the new version at once");

            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Night, "the night, continued again");
            Assert.That(Flow.State.Furniture.OwnedCount("trophy_larder_troll"), Is.EqualTo(1), "once");
            Assert.That(Flow.State.Furniture.OwnedCount("butcher_block"), Is.EqualTo(1), "once");
        }

        // ---------- The market, the menu, the ledger ----------

        [UnityTest]
        public IEnumerator TheMarket_FillsTheStoreroom_AndAnEverydayDishGoesThroughPrepAndService_IntoPipsLedger()
        {
            yield return NewGameToTheDelve();
            yield return ExtractAndGoHome();
            yield return SleepIntoDaytime();
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            Assert.That(daytime.MarketButton.gameObject.activeInHierarchy, "the market is a daytime errand");
            Flow.DebugAddGold(20);
            int purchases = 0;
            void Bought(MarketPurchase e) => purchases++;
            EventBus<MarketPurchase>.Subscribe(Bought);
            daytime.MarketButton.onClick.Invoke();
            yield return null;
            MarketPanel market = daytime.Market;
            Assert.That(market.IsOpen);
            Assert.That(daytime.IsShown, Is.False, "the daytime panel steps aside");
            SupplySource source = Flow.Database.market;
            int Row(string id) => source.offers.FindIndex(o => o.ingredient.id == id);
            int gold = Flow.State.Gold;
            foreach (string id in new[] { "eggs", "bread" }) market.Rows[Row(id)].buy.onClick.Invoke();
            EventBus<MarketPurchase>.Unsubscribe(Bought);
            Assert.That(purchases, Is.EqualTo(2));
            int spent = source.offers[Row("eggs")].price + source.offers[Row("bread")].price;
            Assert.That(Flow.State.Gold, Is.EqualTo(gold - spent));
            IngredientStack eggs = Flow.State.Storeroom.Stacks.Single(s => s.Item.Definition.id == "eggs");
            Assert.That((eggs.Item.Quality, eggs.Freshness), Is.EqualTo((Quality.Standard, 1f)), "Standard and fresh, at once");
            Assert.That(SavedGame().storeroom.Any(s => s.ingredient == "bread"), "saved after buying");
            market.Done.onClick.Invoke();
            yield return null;
            Assert.That(daytime.IsShown);

            // The evening: eggs on toast is on the menu's first page (it can be made), served, and in Pip's ledger.
            yield return OpenForTheEvening();
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            RecipeDefinition toast = Director.Content.recipes.First(r => r.id == "eggs_on_toast");
            Assert.That(prep.Recipes, Has.Member(toast), "cookable dishes come first");
            Director.SetMenu(new[] { toast });
            Director.AssignStaff(StaffStation.None);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            CustomerAgent customer = Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());
            yield return WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood, 30f, "an order");
            Ticket ticket = Director.Session.Tickets.Single();
            Assert.That(ticket.Recipe, Is.SameAs(toast));
            Director.Session.StartCooking(ticket, this);
            Director.Session.FinishCooking(ticket, 1f);
            Director.Session.StartDelivery(ticket, this);
            Director.Session.Deliver(ticket, customer.Logic, 1f);
            Time.timeScale = 6f;
            yield return WaitUntil(() => Director.Session.Ledger.Gold > 0, 40f, "them to pay");
            Time.timeScale = 1f;
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            var results = Object.FindAnyObjectByType<EveningResultsScreen>();
            results.DoneButton.onClick.Invoke(); // the first press finishes the reveal
            yield return null;
            Assert.That(ShownText(results), Has.Some.EqualTo("from Pip's ledger"));
        }

        // ---------- The Butcher Block, Gunta, Pip ----------

        IEnumerator TavernAtPrep()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            yield return null;
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Prep));
        }

        static IngredientDefinition Part(string id) => Director.Content.recipes.SelectMany(r => r.slots).Select(sl => sl.ingredient)
            .FirstOrDefault(d => d != null && d.id == id);

        [UnityTest]
        public IEnumerator TheButcherBlock_ByHand_TurnsAPartIntoCuts_ThatKeepItsQualityAndFreshness()
        {
            yield return TavernAtPrep();
            IngredientDefinition leg = Part("spider_leg");
            Director.Storeroom.Take(i => i.Definition == leg, 99);
            Director.Storeroom.Add(new IngredientStack(new IngredientItem(leg, Quality.Fine), 2, 0.7f));
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            yield return null;
            Assert.That(KeeperWork.Instance.ButcherBlock, Is.Not.Null, "the starting tavern has a Butcher Block");
            prep.ButcherButton.onClick.Invoke();
            yield return null;
            ButcherPanel panel = prep.Butcher;
            Assert.That(panel.IsOpen);
            Assert.That(prep.IsShown, Is.False, "the prep list steps aside");
            // The debug storeroom may hold other legs; this test's are the Fine ones.
            int Leg() => panel.Parts.ToList().FindIndex(st => st.Item.Definition == leg && st.Item.Quality == Quality.Fine);
            Assert.That(Leg(), Is.GreaterThanOrEqualTo(0), "the spider legs are on the list");
            Assert.That(panel.Parts.All(st => st.Item.Definition.Butcherable), "only parts that break down");
            int partsBefore = 0;
            void Cut(PartButchered e) => partsBefore += e.Cuts;
            EventBus<PartButchered>.Subscribe(Cut);

            // (The debug stock's Fine legs merge with these: the stack's freshness is the count-weighted average.)
            float partFreshness = panel.Parts[Leg()].Freshness;
            int fineLegs = panel.Parts[Leg()].Count;
            Assert.That(panel.CutYourself(Leg()));
            yield return null;
            Assert.That(KeeperWork.Instance.ActiveCook, Is.InstanceOf<ButcherMinigame>(), "the cut opens at the block");
            Assert.That(Object.FindAnyObjectByType<ButcherBlockView>().IsWorking, "the block's knife works");
            Assert.That(Vector2.Distance(Player.transform.position, KeeperWork.Instance.ButcherBlock.UsePoint), Is.LessThan(0.1f), "the keeper at the block");
            // A steady hand plays the cut to the end (between frames, so the block finishes it as it would a player's).
            var game = (ButcherMinigame)KeeperWork.Instance.ActiveCook;
            var hand = new ButcherAutoPlayer(game, 1f, new Hearthdelve.Core.Random.SeededRandom(3));
            for (int i = 0; i < 60 * 20 && !game.IsComplete; i++) game.Tick(1f / 60f, hand.NextInput(1f / 60f));
            Assert.That(game.IsComplete);
            int expected = ButcherRules.Yield(leg.butchering, game.Evaluate());
            yield return null;
            yield return null;
            yield return null;
            Assert.That(panel.IsOpen, "back to the list");
            IngredientStack cuts = Director.Storeroom.Stacks.Single(s => s.Item.Definition == leg.butchering.cut && s.Item.Quality == Quality.Fine);
            Assert.That(cuts.Count, Is.EqualTo(expected), "the yield from the cut's score");
            Assert.That(expected, Is.GreaterThanOrEqualTo(2), "a steady hand gets more than the minimum");
            Assert.That(cuts.Item.Quality, Is.EqualTo(Quality.Fine));
            Assert.That(cuts.Freshness, Is.EqualTo(partFreshness).Within(1e-4f), "the part's freshness");
            Assert.That(Director.Storeroom.CountMatching(i => i.Definition == leg && i.Quality == Quality.Fine), Is.EqualTo(fineLegs - 1), "one part used");
            Assert.That(panel.MessageText, Does.StartWith($"+{expected} "));
            Assert.That(m_Patterns, Has.Member(Hearthdelve.Core.Haptics.HapticIds.TapLight), "each stroke's knife-in");
            Assert.That(m_Patterns, Has.Member(Hearthdelve.Core.Haptics.HapticIds.CutClean), "clean strokes");
            Assert.That(m_Patterns, Has.Member(Hearthdelve.Core.Haptics.HapticIds.PulseSuccess), "the finished cut's moment");

            // Gunta takes the other: she walks to the block and cuts it at her skill.
            string by = null;
            void ByWhom(PartButchered e) => by = e.By;
            EventBus<PartButchered>.Subscribe(ByWhom);
            Assert.That(panel.LetStaff(Leg()));
            Assert.That(Director.Cook.HasTask);
            Time.timeScale = 4f;
            yield return WaitUntil(() => !Director.Cook.HasTask, 15f, "Gunta to finish at the block");
            Time.timeScale = 1f;
            EventBus<PartButchered>.Unsubscribe(ByWhom);
            EventBus<PartButchered>.Unsubscribe(Cut);
            Assert.That(by, Is.EqualTo(StaffIds.Gunta));
            Assert.That(Director.Storeroom.CountMatching(i => i.Definition == leg && i.Quality == Quality.Fine), Is.EqualTo(fineLegs - 2));
            Assert.That(Director.Storeroom.CountMatching(i => i.Definition == leg.butchering.cut && i.Quality == Quality.Fine), Is.GreaterThan(expected), "her cuts too");
            panel.Close();
            yield return null;
            Assert.That(prep.IsShown, "and Prep is back");
        }

        [UnityTest]
        public IEnumerator Gunta_TakesTheStationSheIsGiven_AndCooksItsOrders_WhilePipServes()
        {
            yield return TavernAtPrep();
            Assert.That(Director.CookMember?.id, Is.EqualTo(StaffIds.Gunta), "Gunta is here from the first evening");
            Assert.That(Director.CookAssignment, Is.EqualTo(StaffStation.None), "off duty until she's given a station");
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            prep.CookButton.onClick.Invoke();
            Assert.That(Director.CookAssignment, Is.EqualTo(StaffStation.Grill), "the job button: the Grill first");
            Assert.That(Director.StaffAssignment, Is.EqualTo(StaffStation.Serving), "Pip still carries plates");
            Director.AssignStaff(StaffStation.Grill);
            Assert.That(Director.CookAssignment, Is.EqualTo(StaffStation.None), "a station is never shared");
            Director.AssignStaff(StaffStation.Serving);
            Director.AssignCook(StaffStation.Grill);

            RecipeDefinition grilled = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            foreach (var need in grilled.slots.Where(sl => sl.ingredient != null))
                Director.Storeroom.Add(new IngredientStack(new IngredientItem(need.ingredient, Quality.Standard), 4, 1f));
            var work = new List<StaffWorkDone>();
            void Done(StaffWorkDone e) => work.Add(e);
            EventBus<StaffWorkDone>.Subscribe(Done);
            Director.SetMenu(new[] { grilled });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            CustomerAgent customer = Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());
            Time.timeScale = 4f;
            yield return WaitUntil(() => work.Any(w => w.StaffId == StaffIds.Gunta), 60f, "Gunta to grill the order");
            yield return WaitUntil(() => work.Any(w => w.StaffId == StaffIds.Pip), 60f, "Pip to carry it");
            Time.timeScale = 1f;
            EventBus<StaffWorkDone>.Unsubscribe(Done);
            StaffWorkDone gunta = work.First(w => w.StaffId == StaffIds.Gunta);
            Assert.That(gunta.RecipeId, Is.EqualTo("grilled_spider_leg"));
            Assert.That(gunta.Quality, Is.LessThanOrEqualTo(Director.CookMember.qualityCap + 1e-4f), "capped like all staff work");
        }

        [UnityTest]
        public IEnumerator PipAndGunta_HaveTheirOwnLooks_AndEmotes()
        {
            yield return TavernAtPrep();
            StaffAgent pip = Director.StaffAgents.Single(a => a.Member != null && a.Member.id == StaffIds.Pip);
            StaffAgent gunta = Director.StaffAgents.Single(a => a.Member != null && a.Member.id == StaffIds.Gunta);
            // The cook (id gunta) is Boog, drawn from the Goblin Sapper's sheets.
            foreach (var (agent, who) in new[] { (pip, "Pip"), (gunta, "GoblinSapper") })
            {
                SpriteRenderer body = agent.GetComponentsInChildren<SpriteRenderer>().FirstOrDefault(r => r.sprite != null && r.sprite.texture != null && r.sprite.texture.name.StartsWith(who));
                Assert.That(body, Is.Not.Null, $"{who} drawn from their own sheet");
                Assert.That(agent.Emote, Is.Not.Null, $"{who} can emote");
                Assert.That(agent.Faces, Is.Not.Null);
                agent.Show(agent.Faces.happy, 1f);
                Assert.That(agent.Emote.Showing, Is.SameAs(agent.Faces.happy), $"{who}'s face shows");
            }
            Assert.That(Vector2.Distance(pip.transform.position, gunta.transform.position), Is.GreaterThan(0.5f), "not standing on each other");
        }

        [UnityTest]
        public IEnumerator Patrons_NoticeTheTusks_OnceTheyHang()
        {
            yield return TavernAtPrep();
            AreaFurniture tavern = AreaFurniture.All.Single(a => a.Area.Id == PropertyArea.TavernId);
            Assert.That(tavern.CurrentLayout().Any(p => p.definition == "trophy_larder_troll"), Is.False);
            DecorateMode mode = DecorateMode.Instance;
            mode.Enter(tavern);
            tavern.State.Receive(tavern.Definition("trophy_larder_troll"));
            Assert.That(mode.TakeFromStorage("trophy_larder_troll"));
            for (int x = 4; x < 24 && !mode.CarriedCheck.IsValid; x++)
            for (int y = 13; y <= 15 && !mode.CarriedCheck.IsValid; y++)
                mode.SetCursor(new Vector2Int(x, y));
            mode.Place();
            mode.Leave();
            yield return null;
            Assert.That(tavern.CurrentLayout().Any(p => p.definition == "trophy_larder_troll"));

            RecipeDefinition dish = Director.Content.recipes.First(r => Hearthdelve.Tavern.Service.PrepRules.Makeable(r, Director.Storeroom) > 0);
            Director.SetMenu(new[] { dish });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            var noticed = false;
            for (int i = 0; i < 6 && !noticed; i++)
            {
                CustomerAgent customer = Director.SpawnCustomer(Director.Content.customers[i % Director.Content.customers.Count]);
                typeof(CustomerAgent).GetField("m_NoticeChance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(customer, 1f);
                Time.timeScale = 4f;
                yield return WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood || customer.NoticedTrophy, 30f, "a patron to sit");
                Time.timeScale = 1f;
                noticed = customer.NoticedTrophy;
            }
            Assert.That(noticed, "a patron looks up at the tusks (wordless)");
        }
    }
}
