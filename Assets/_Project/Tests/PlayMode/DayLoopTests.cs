using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Save;
using Hearthdelve.Tavern.Customers;
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
    /// The whole day through <see cref="GameFlow"/>, from the Boot scene, with saves in a temp folder (4c step 5;
    /// reordered in 4d step 5): Boot → New Game → the first delve → night → sleep → daytime (the delve meal) → evening (serve, Results, close up) → the
    /// generated delve (a power, run Gold, a haul, extract) → Night (bank, buy, save) → Sleep → day 2 → boot again →
    /// Continue; and the other ways a day goes (dying with the Lockbox, staying shut, quitting mid-delve).
    /// </summary>
    public class DayLoopTests : LookTestFixture
    {
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        // ---------- Helpers ----------

        IEnumerator BootToMenu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            Haptics = new RecordingHapticOutput();
            HapticService.Instance.Output = Haptics;
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

        static IngredientDefinition Leg => Flow.Database.Ingredient("spider_leg");
        static RecipeDefinition GrilledLeg => Director.Content.recipes.First(r => r != null && r.id == "grilled_spider_leg");
        static int Legs(Storeroom storeroom) => storeroom.CountMatching(i => i.Definition == Leg);
        static Satchel PlayerSatchel => LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>().Satchel;

        SaveData SavedGame() => SaveSystem.FromJson(File.ReadAllText(Path.Combine(m_SaveDir, SaveStore.FileName)));

        /// <summary>Extracts and goes home: the delve result, then the tavern at Night.</summary>
        IEnumerator ExtractAndGoHome()
        {
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "extracted");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night, home again");
        }

        /// <summary>Daytime is done: open the tavern for the evening's prep.</summary>
        static IEnumerator OpenForTheEvening()
        {
            Object.FindAnyObjectByType<MorningScreen>().DescendButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Prep, "the evening's prep");
        }

        /// <summary>Keep the tavern shut tonight: straight on to the night's delve.</summary>
        IEnumerator StayShutAndDelve()
        {
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            yield return null;
            prep.CloseButton.onClick.Invoke();
            yield return InDungeon();
        }

        /// <summary>A new game: it starts with a delve into the Hollows (the 4d playtest).</summary>
        IEnumerator NewGameToTheDelve()
        {
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return InDungeon();
        }

        /// <summary>Home from a delve with nothing, then sleep into the next daytime.</summary>
        IEnumerator HomeAndSleep(string what)
        {
            yield return ExtractAndGoHome();
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, what);
        }

        /// <summary>One customer, one dish, cooked and served through the session; returns once they've paid.</summary>
        IEnumerator ServeOne(RecipeDefinition dish)
        {
            Director.SetMenu(new[] { dish });
            Director.AssignStaff(StaffStation.None);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            CustomerAgent customer = Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());
            yield return WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood, 30f, "an order");
            Ticket ticket = Director.Session.Tickets.Single();
            Director.Session.StartCooking(ticket, this);
            Director.Session.FinishCooking(ticket, 1f);
            Director.Session.StartDelivery(ticket, this);
            Director.Session.Deliver(ticket, customer.Logic, 1f);
            Time.timeScale = 6f;
            yield return WaitUntil(() => Director.Session.Ledger.Gold > 0, 40f, "them to pay");
            Time.timeScale = 1f;
        }

        static IEnumerator CloseResults()
        {
            var results = Object.FindAnyObjectByType<EveningResultsScreen>();
            // The first press finishes the reveal, the next ends the evening.
            for (int i = 0; i < 4 && Director.Phase == TavernPhase.Results; i++)
            {
                results.DoneButton.onClick.Invoke();
                yield return null;
            }
        }

        static string[] ShownText(Component root) =>
            root.GetComponentsInChildren<SuperTextMesh>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToArray();

        // ---------- The day ----------

        [UnityTest]
        public IEnumerator AWholeDay_InTheNewOrder_ThenSleep_ThenContinue_RestoresEverything()
        {
            yield return BootToMenu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(menu.ContinueButton.gameObject.activeSelf, Is.False, "no save, no Continue");
            menu.NewGameButton.onClick.Invoke();
            Assert.That(menu.IsConfirming, Is.False, "nothing to replace, so no question");

            // Day 1 starts with a delve into the Hollows: the storeroom is empty, so the first job is something to cook.
            var transition = Object.FindAnyObjectByType<TransitionScreen>();
            yield return WaitUntil(() => transition.IsCovering, 3f, "the way down");
            Assert.That(transition.CaptionKey, Is.EqualTo(LoopLocKeys.TransitionFirstDelve), "straight into the Hollows");
            yield return InDungeon();
            Assert.That((Flow.State.Day, Flow.State.Phase), Is.EqualTo((1, DayPhase.Delve)));
            Assert.That(File.Exists(Path.Combine(m_SaveDir, SaveStore.FileName)), "a new game is saved at once");
            Assert.That(SavedGame().phase, Is.EqualTo(nameof(DayPhase.Delve)));
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Standard), 2);
            yield return ExtractAndGoHome();
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(2), "the first haul");
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();

            // Daytime, day 2: tonight's delve meal from a spider leg in the storeroom.
            yield return InTavern(TavernPhase.Daytime, "the first daytime");
            Assert.That(Flow.State.Day, Is.EqualTo(2));
            Assert.That(Director.InDayLoop);
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            yield return null;
            Assert.That(daytime.IsShown);
            Assert.That(ShownText(daytime), Has.Some.EqualTo("daytime · day 2").And.Some.EqualTo("open for the evening"));
            Assert.That(GameObject.Find("Controls"), Is.Null, "no walking controls over the daytime panel");
            int card = daytime.Options.ToList().IndexOf(GrilledLeg);
            Assert.That(card, Is.GreaterThanOrEqualTo(0), "grilled spider leg makes a delve meal");
            daytime.Cook(card);
            yield return null;
            Assert.That(KeeperWork.Instance.DelveMeal, Is.SameAs(GrilledLeg), "cooked at the grill's panel");
            Assert.That(daytime.IsShown, Is.False, "the daytime panel steps aside while it cooks");
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(1), "its ingredients are taken");
            KeeperWork.Instance.FinishCook(1f);
            yield return null;
            Assert.That(Flow.State.Meal.Kind, Is.EqualTo(MealBuffKind.MaxEssence), "eaten: the buff waits for tonight's delve");
            float bonus = Flow.Loadout.MaxEssenceBonus;
            Assert.That(bonus, Is.GreaterThan(0f));
            Assert.That(daytime.IsShown);
            Assert.That(ShownText(daytime), Has.Some.StartsWith("tonight's delve: Essence +"));

            // The evening: the meal is still waiting; one dish served, Results, and close up to head below.
            yield return OpenForTheEvening();
            Assert.That(Flow.State.Phase, Is.EqualTo(DayPhase.Evening));
            Assert.That(Flow.State.Meal.IsActive, "the meal lasts until the delve");
            Assert.That(Object.FindAnyObjectByType<PrepScreen>().IsShown);
            yield return ServeOne(GrilledLeg);
            Assert.That(GameObject.Find("Controls"), Is.Not.Null, "the controls line comes with service");
            ServiceLedger ledger = Director.Session.Ledger;
            int takings = ledger.Gold + ledger.Tips;
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            yield return CloseResults();
            yield return WaitUntil(() => transition.IsCovering, 3f, "the way down");
            Assert.That(transition.CaptionKey, Is.EqualTo(LoopLocKeys.TransitionDelve), "the tavern closes; down to the Cellars");

            // The night's delve, in the generated run: banked takings and a pre-delve save, the meal's Essence, a power,
            // run Gold and a haul, and home.
            yield return InDungeon();
            Assert.That(Object.FindAnyObjectByType<Hearthdelve.Dungeon.Rooms.RoomRunner>(), Is.Not.Null, "the generated Cellars run, not the test floor");
            Assert.That(Flow.State.Gold, Is.EqualTo(takings), "the takings are banked");
            SaveData beforeDelve = SavedGame();
            Assert.That(beforeDelve.phase, Is.EqualTo(nameof(DayPhase.Delve)), "saved before the run");
            Assert.That(beforeDelve.gold, Is.EqualTo(takings));
            var essence = Player.GetComponent<EssenceHealth>();
            Assert.That(essence.Essence.Max, Is.EqualTo(essence.Config.essence.baseMax + bonus).Within(0.01f), "the delve meal raised max Essence");
            DelveRunController run = DelveRunController.Active;
            Assert.That(run.Powers.Taken, Is.Empty, "a delve starts with no powers");
            Assert.That(run.TakePower(Hearthdelve.Dungeon.Rooms.RoomRunner.Active.Settings.tuning.powers[0]));
            run.Loot.AddGold(25);
            int dayOneCapacity = PlayerSatchel.Capacity;
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Fine), 3);
            yield return ExtractAndGoHome();

            // Night: the haul and the run's Gold home once, saved; the meal used up; an upgrade bought (and saved).
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(3), "the haul came home, once (the first night's two were eaten and served)");
            Assert.That(Flow.State.Gold, Is.EqualTo(takings + 25), "the run's Gold is banked");
            Assert.That(Flow.State.Meal.IsActive, Is.False, "the meal is used up");
            SaveData atNight = SavedGame();
            Assert.That(atNight.phase, Is.EqualTo(nameof(DayPhase.Night)), "saved as soon as the delve was over");
            Assert.That(atNight.gold, Is.EqualTo(takings + 25));
            Assert.That(atNight.storeroom.Where(st => st.ingredient == "spider_leg").Sum(st => st.count), Is.EqualTo(3));
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            Assert.That(night.IsShown);
            Assert.That(night.SavedNoteShown, "says it saved");
            Assert.That(ShownText(night), Has.Some.EqualTo("banked today").And.Some.EqualTo($"{takings + 25} Gold"), "the takings and the run's Gold");
            Assert.That(ShownText(night), Has.Some.EqualTo("made it out"));
            Flow.DebugAddGold(300);
            int satchelRow = night.Definitions.ToList().FindIndex(u => u.kind == UpgradeKind.SatchelSlots);
            TavernUpgradeDefinition satchelUpgrade = night.Definitions[satchelRow];
            int gold = Flow.State.Gold;
            night.Buy(satchelRow);
            Assert.That(Flow.State.UpgradeLevel(satchelUpgrade.id), Is.EqualTo(1));
            Assert.That(UiFeedback.Last, Is.EqualTo(UiMoment.Buy), "a purchase has its moment");
            yield return null;
            Assert.That(night.IsGlowing(satchelRow), "and the bought row lights up");
            Assert.That(Flow.State.Gold, Is.EqualTo(gold - satchelUpgrade.levels[0].cost));
            Assert.That(SavedGame().upgrades.Single(u => u.id == satchelUpgrade.id).level, Is.EqualTo(1), "saved after buying");

            // Sleep: overnight freshness, day 3's daytime, saved.
            int legsLeft = Legs(Flow.State.Storeroom);
            float freshness = Flow.State.Storeroom.Stacks.First(st => st.Item.Definition == Leg).Freshness;
            night.SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the third daytime");
            Assert.That(Flow.State.Day, Is.EqualTo(3));
            float overnight = Flow.State.Storeroom.Stacks.First(st => st.Item.Definition == Leg).Freshness;
            Assert.That(overnight, Is.LessThan(freshness), "the storeroom lost a little freshness overnight");
            SaveData daySave = SavedGame();
            Assert.That(daySave.day, Is.EqualTo(3));
            Assert.That(daySave.phase, Is.EqualTo(nameof(DayPhase.Daytime)));
            int goldBefore = Flow.State.Gold;
            int renownBefore = Flow.State.Renown;

            // Quit and come back: Continue restores it all, and the upgrade reaches the next delve; the powers don't.
            yield return BootToMenu();
            menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(menu.ContinueButton.gameObject.activeSelf);
            Assert.That(ShownText(menu), Has.Some.EqualTo("day 3, daytime"));
            menu.ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the daytime, continued");
            Assert.That(Flow.State.Day, Is.EqualTo(3));
            Assert.That(Flow.State.Gold, Is.EqualTo(goldBefore));
            Assert.That(Flow.State.Renown, Is.EqualTo(renownBefore));
            Assert.That(Flow.State.UpgradeLevel(satchelUpgrade.id), Is.EqualTo(1));
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(legsLeft), "not duplicated, not lost");
            Assert.That(Flow.State.Storeroom.Stacks.First(st => st.Item.Definition == Leg).Freshness, Is.EqualTo(overnight).Within(1e-4f));
            yield return OpenForTheEvening();
            yield return StayShutAndDelve();
            Assert.That(PlayerSatchel.Capacity, Is.EqualTo(dayOneCapacity + 1), "the satchel upgrade applies to the delve");
            Assert.That(DelveRunController.Active.Powers.Taken, Is.Empty, "last night's power is gone");
        }

        /// <summary>
        /// A save made before the delve (phase Delve) resumes with the night's delve from the top: nothing from the
        /// abandoned run (its satchel, its Gold, its powers) comes back, and nothing is banked twice.
        /// </summary>
        [UnityTest]
        public IEnumerator QuittingMidDelve_ContinuesWithTheNightsDelveFromTheTop()
        {
            yield return NewGameToTheDelve();
            DelveRunController.Active.Loot.AddGold(40);
            DelveRunController.Active.TakePower(Hearthdelve.Dungeon.Rooms.RoomRunner.Active.Settings.tuning.powers[0]);
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Standard), 3);
            Assert.That(SavedGame().phase, Is.EqualTo(nameof(DayPhase.Delve)));

            yield return BootToMenu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(ShownText(menu), Has.Some.EqualTo("day 1, the night's delve"));
            menu.ContinueButton.onClick.Invoke();
            yield return InDungeon();
            Assert.That(Flow.State.Phase, Is.EqualTo(DayPhase.Delve));
            Assert.That(PlayerSatchel.Slots.All(sl => sl.IsEmpty), "the abandoned run's haul is gone");
            Assert.That(DelveRunController.Active.Loot.Gold, Is.Zero);
            Assert.That(DelveRunController.Active.Powers.Taken, Is.Empty);
            Assert.That(Flow.State.Gold, Is.Zero, "nothing banked");
            yield return ExtractAndGoHome();
            Assert.That(Flow.State.Day, Is.EqualTo(1));
            // The evening was played before the reload: the night tells the delve, not an evening it can't know
            // (found in the 4d web smoke test: it showed "served 0").
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            string[] shown = ShownText(night);
            Assert.That(shown, Has.Some.EqualTo("made it out"));
            Assert.That(shown, Has.None.EqualTo("served").And.None.EqualTo("kept shut").And.None.EqualTo("banked today"));
        }

        /// <summary>
        /// 4c sign-off: the Essence and seating upgrades bought at Night reach the next day's scenes (the pure rules are
        /// EditMode-tested; this checks the dungeon and tavern actually read them). Every seat opens, and the order rail
        /// has a row for each.
        /// </summary>
        /// <summary>
        /// 4e: defeating the Larder Troll in a real delve is recorded once in the save (its first clear announced for 4f's
        /// trophy and story), even across a reload, and its Gold comes home with the haul.
        /// </summary>
        [UnityTest]
        public IEnumerator DefeatingTheTroll_IsRecordedOnce_InTheSave()
        {
            Hearthdelve.Dungeon.Rooms.RoomRunner.StartInArenaOverride = true;
            int firsts = 0;
            void Count(BossFirstCleared e) => firsts += e.BossId == "larder_troll" ? 1 : 0;
            Hearthdelve.Core.Events.EventBus<BossFirstCleared>.Subscribe(Count);
            try
            {
                yield return NewGameToTheDelve();
                Player.GetComponent<EssenceHealth>().GodMode = true;
                var encounter = Object.FindAnyObjectByType<Hearthdelve.Dungeon.Bosses.BossEncounter>();
                yield return WaitUntil(() => encounter.State == Hearthdelve.Dungeon.Bosses.BossEncounterState.Fighting, 8f, "the troll");
                var health = encounter.GetComponent<Hearthdelve.Dungeon.Bosses.BossHealth>();
                health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
                health.FinishOff(Player.gameObject, finisher: false);
                yield return WaitUntil(() => DelveRunController.Active.Loot.BossesDefeated.Count == 1, 3f, "the defeat recorded");
                DelveRunController.Active.Loot.AddGold(encounter.Boss.gold);
                yield return ExtractAndGoHome();
                Assert.That(Flow.State.TimesDefeated("larder_troll"), Is.EqualTo(1));
                Assert.That(firsts, Is.EqualTo(1), "its first clear, announced once");
                Assert.That(SavedGame().bosses.Single().id, Is.EqualTo("larder_troll"));
                Assert.That(Flow.State.Gold, Is.EqualTo(encounter.Boss.gold), "its Gold banked");

                yield return BootToMenu();
                Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
                yield return InTavern(TavernPhase.Night, "the night, continued");
                Assert.That(Flow.State.TimesDefeated("larder_troll"), Is.EqualTo(1), "kept, and not counted twice");
                Assert.That(firsts, Is.EqualTo(1));
            }
            finally
            {
                Hearthdelve.Dungeon.Rooms.RoomRunner.StartInArenaOverride = false;
                Hearthdelve.Core.Events.EventBus<BossFirstCleared>.Unsubscribe(Count);
            }
        }

        [UnityTest]
        public IEnumerator EssenceAndSeatingUpgrades_ReachTheNextDaysScenes()
        {
            yield return NewGameToTheDelve();
            float baseMax = Player.GetComponent<EssenceHealth>().Essence.Max;
            yield return ExtractAndGoHome();

            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            Flow.DebugAddGold(1000);
            int essenceRow = night.Definitions.ToList().FindIndex(u => u.kind == UpgradeKind.MaxEssence);
            int seatsRow = night.Definitions.ToList().FindIndex(u => u.kind == UpgradeKind.Seats);
            night.Buy(essenceRow);
            for (int i = 0; i < night.Definitions[seatsRow].levels.Count; i++) night.Buy(seatsRow);
            Assert.That(Flow.State.UpgradeLevel(night.Definitions[seatsRow].id), Is.EqualTo(night.Definitions[seatsRow].levels.Count), "seating maxed");
            night.SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the next daytime");

            float bonus = Flow.Loadout.MaxEssenceBonus;
            Assert.That(bonus, Is.GreaterThan(0f), "no delve meal today: the bonus is the upgrade's");
            yield return OpenForTheEvening();
            Assert.That(Director.ActiveSeats, Is.EqualTo(Director.Layout.Seats.Count), "every seat in the room is open");
            Assert.That(Director.Layout.Seats.Count(st => st.IsActive), Is.EqualTo(Director.ActiveSeats));
            Transform rail = Object.FindObjectsByType<Canvas>().First(c => c.name == "UI").transform.Find("TavernHud/Content");
            for (int i = 1; i <= Director.ActiveSeats; i++)
                Assert.That(rail.Find($"Order{i}"), Is.Not.Null, $"a rail row for order {i}");
            yield return StayShutAndDelve();
            Assert.That(Player.GetComponent<EssenceHealth>().Essence.Max, Is.EqualTo(baseMax + bonus).Within(0.01f), "the Essence upgrade applies to the delve");
        }

        /// <summary>
        /// The day loop's delve is the generated run (4d step 5), not the test floor: no look-test overlay, so no key can
        /// send a real day into a look-test scene (the 4c web lesson); and the tavern is never loaded beside it.
        /// </summary>
        [UnityTest]
        public IEnumerator TheDelve_InTheDayLoop_IsTheGeneratedRun_WithNoLookTestKeys()
        {
            yield return NewGameToTheDelve();
            Assert.That(Flow.LoadedScene, Is.EqualTo("Dungeon"));
            Assert.That(Hearthdelve.Dungeon.Rooms.RoomRunner.Active, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<Hearthdelve.UI.Debugging.LookTestOverlay>(), Is.Null, "no F2-F4 look-test keys");
            Assert.That(SceneManager.GetSceneByName(GameScenes.Tavern).isLoaded, Is.False, "the tavern and the dungeon are never loaded together");
        }

        /// <summary>
        /// A save made at Night keeps the purse, Renown and the haul, but not the day's story: a resumed Night shows only
        /// what the save knows, and nothing from the delve is applied twice.
        /// </summary>
        [UnityTest]
        public IEnumerator ContinuingAtNight_ShowsOnlyWhatTheSaveKnows_AndAppliesNothingTwice()
        {
            yield return NewGameToTheDelve();
            DelveRunController.Active.Loot.AddGold(20);
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Standard), 2);
            yield return ExtractAndGoHome();
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            Assert.That(ShownText(night), Has.Some.EqualTo("made it out"), "played through: the delve is told");
            Assert.That(ShownText(night), Has.None.EqualTo("kept shut").And.None.EqualTo("served"), "the first night had no evening");

            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Night, "the night, continued");
            Assert.That(Flow.State.Gold, Is.EqualTo(20), "the run's Gold, banked once");
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(2), "the haul, once");
            night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            string[] shown = ShownText(night);
            Assert.That(shown, Has.None.EqualTo("skipped").And.None.EqualTo("delve").And.None.EqualTo("banked today"), "no made-up day");
            Assert.That(shown, Has.Some.EqualTo("purse").And.Some.EqualTo("Renown"), "the purse and Renown are known");
        }

        [UnityTest]
        public IEnumerator Dying_TheLockboxStackComesHome_TheRunsGoldIsLost_AndNightFollows()
        {
            yield return NewGameToTheDelve();
            Flow.DebugAddGold(60);
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Standard), 2);
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Fine), 3);
            DelveRunController.Active.Loot.AddGold(30);

            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            var essence = Player.GetComponent<EssenceHealth>();
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            death.Mark(1);
            death.Confirm.onClick.Invoke();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            yield return null;
            Assert.That(ShownText(result), Has.Some.EqualTo("30 Gold left in the dark"));
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the night");

            Assert.That(Flow.State.Today.Delve, Is.EqualTo(DelveOutcome.Died));
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(3), "only the Lockbox stack");
            Assert.That(Flow.State.Storeroom.Stacks.Where(st => !st.IsEmpty).All(st => st.Item.Quality == Quality.Fine), "the stack that was kept");
            Assert.That(Flow.State.Today.PartsLost, Is.EqualTo(2));
            Assert.That(Flow.State.Gold, Is.EqualTo(60), "the run's Gold is lost; the banked Gold is safe");
            Assert.That(SavedGame().gold, Is.EqualTo(60));
            Assert.That(MenuPause.IsPaused, Is.False, "not paused in the tavern");
        }

        [UnityTest]
        public IEnumerator NothingToCook_StayShut_GoesStraightToTheDelve_ThenNight_AndSleeps()
        {
            yield return NewGameToTheDelve();
            yield return HomeAndSleep("the second daytime");
            yield return OpenForTheEvening();

            var prep = Object.FindAnyObjectByType<PrepScreen>();
            yield return null;
            Assert.That(prep.OpenButton.interactable, Is.False, "an empty storeroom can't open");
            Assert.That(ShownText(prep), Has.Some.EqualTo("nothing in the storeroom makes a dish tonight.").And.Some.EqualTo("stay shut tonight"));
            Assert.That(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject, Is.SameAs(prep.CloseButton.gameObject),
                "starts on staying shut (found in the web smoke test: it started on a disabled card)");
            prep.CloseButton.onClick.Invoke();
            yield return InDungeon();
            Assert.That(SavedGame().phase, Is.EqualTo(nameof(DayPhase.Delve)));
            yield return ExtractAndGoHome();
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            Assert.That(ShownText(night), Has.Some.EqualTo("kept shut"));
            night.SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Daytime, "the next daytime");
            Assert.That(Flow.State.Day, Is.EqualTo(3));
        }

        /// <summary>
        /// While a transition swaps scenes there's a moment with no content scene at all. Something must still render
        /// (and draw the black cover over it), or the screen shows whatever was there last (the step 5 playtest: a
        /// different screen flickered behind "Evening · Day 1").
        /// </summary>
        [UnityTest]
        public IEnumerator EveryFrameOfATransition_HasACameraDrawingTheCover()
        {
            yield return BootToMenu();
            int framesWithoutCamera = 0, frames = 0;
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            var transition = Object.FindAnyObjectByType<TransitionScreen>();
            do
            {
                yield return null;
                frames++;
                if (transition.IsCovering && Camera.allCamerasCount == 0) framesWithoutCamera++;
            } while (Flow.IsLoading || transition.IsCovering);
            Assert.That(frames, Is.GreaterThan(5), "a transition happened");
            Assert.That(framesWithoutCamera, Is.Zero, "a camera renders through the scene swap");
        }

        [UnityTest]
        public IEnumerator NewGame_OverASave_AsksOnce_AndBackKeepsIt()
        {
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return InDungeon();
            Flow.DebugAddGold(50);
            Flow.Save();

            yield return BootToMenu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.NewGameButton.onClick.Invoke();
            Assert.That(menu.IsConfirming, "asks before replacing the save");
            menu.ConfirmNo.onClick.Invoke();
            Assert.That(menu.IsConfirming, Is.False);
            Assert.That(SavedGame().gold, Is.EqualTo(50), "Back keeps it");
            menu.NewGameButton.onClick.Invoke();
            menu.ConfirmYes.onClick.Invoke();
            yield return InDungeon();
            Assert.That(SavedGame().gold, Is.Zero, "started over");
        }
    }
}
