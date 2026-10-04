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
    /// 4c step 5: the whole day through <see cref="GameFlow"/>, from the Boot scene, with saves in a temp folder.
    /// Boot → New Game → Morning (breakfast) → delve → extract → Evening → serve → Results → Night (bank, buy,
    /// save) → Sleep → Day 2 → boot again → Continue; and the other ways a day goes (dying with the Lockbox,
    /// closing with nothing to cook).
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

        IEnumerator ExtractAndGoHome()
        {
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract(), "extracted");
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            result.Proceed();
            yield return InTavern(TavernPhase.Prep, "the evening's prep");
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
        public IEnumerator AWholeDay_ThenSleep_ThenContinue_RestoresEverything()
        {
            yield return BootToMenu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(menu.ContinueButton.gameObject.activeSelf, Is.False, "no save, no Continue");
            menu.NewGameButton.onClick.Invoke();
            Assert.That(menu.IsConfirming, Is.False, "nothing to replace, so no question");

            // Morning, day 1: breakfast from a spider leg in the storeroom.
            yield return InTavern(TavernPhase.Morning, "the first morning");
            Assert.That(Flow.State.Day, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(m_SaveDir, SaveStore.FileName)), "a new game is saved at once");
            Assert.That(Director.InDayLoop);
            var morning = Object.FindAnyObjectByType<MorningScreen>();
            yield return null;
            Assert.That(morning.IsShown);
            Assert.That(GameObject.Find("Controls"), Is.Null, "no walking controls over the morning panel");
            Flow.State.Storeroom.Add(new IngredientStack(new IngredientItem(Leg, Quality.Standard), 1, 1f));
            int card = morning.Options.ToList().IndexOf(GrilledLeg);
            Assert.That(card, Is.GreaterThanOrEqualTo(0), "grilled spider leg makes a breakfast");
            morning.Cook(card);
            yield return null;
            Assert.That(KeeperWork.Instance.Breakfast, Is.SameAs(GrilledLeg), "cooked at the grill's panel");
            Assert.That(morning.IsShown, Is.False, "the morning panel steps aside while it cooks");
            Assert.That(Legs(Flow.State.Storeroom), Is.Zero, "the ingredients are taken");
            KeeperWork.Instance.FinishCook(1f);
            yield return null;
            Assert.That(Flow.State.Meal.Kind, Is.EqualTo(MealBuffKind.MaxEssence), "eaten: the buff waits for the delve");
            float bonus = Flow.Loadout.MaxEssenceBonus;
            Assert.That(bonus, Is.GreaterThan(0f));
            Assert.That(morning.IsShown);
            Assert.That(ShownText(morning), Has.Some.StartsWith("today's delve: essence +"));

            // The delve: the breakfast's Essence, a haul, and home.
            morning.DescendButton.onClick.Invoke();
            yield return InDungeon();
            var essence = Player.GetComponent<EssenceHealth>();
            Assert.That(essence.Essence.Max, Is.EqualTo(essence.Config.essence.baseMax + bonus).Within(0.01f), "breakfast raised max Essence");
            int dayOneCapacity = PlayerSatchel.Capacity;
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Standard), 3);
            yield return ExtractAndGoHome();

            // Evening: the haul is in the storeroom, the breakfast is used up.
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(3), "the haul came home, once");
            Assert.That(Flow.State.Meal.IsActive, Is.False);
            Assert.That(Object.FindAnyObjectByType<PrepScreen>().IsShown);
            yield return ServeOne(GrilledLeg);
            Assert.That(GameObject.Find("Controls"), Is.Not.Null, "the controls line comes with service");
            ServiceLedger ledger = Director.Session.Ledger;
            int takings = ledger.Gold + ledger.Tips;
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            yield return CloseResults();

            // Night: banked, saved, an upgrade bought (and saved again).
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Night));
            Assert.That(Flow.State.Gold, Is.EqualTo(takings), "the takings are banked");
            SaveData atNight = SavedGame();
            Assert.That(atNight.phase, Is.EqualTo(nameof(DayPhase.Night)), "autosaved as night fell");
            Assert.That(atNight.gold, Is.EqualTo(takings));
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            Assert.That(night.IsShown);
            Assert.That(night.SavedNoteShown, "says it saved");
            Assert.That(ShownText(night), Has.Some.EqualTo($"banked tonight: {takings} gold"));
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

            // Sleep: overnight freshness, day 2, saved.
            int legsLeft = Legs(Flow.State.Storeroom);
            Assert.That(legsLeft, Is.EqualTo(2), "one served, two left");
            float freshness = Flow.State.Storeroom.Stacks.First(s => s.Item.Definition == Leg).Freshness;
            night.SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Morning, "the second morning");
            Assert.That(Flow.State.Day, Is.EqualTo(2));
            float overnight = Flow.State.Storeroom.Stacks.First(s => s.Item.Definition == Leg).Freshness;
            Assert.That(overnight, Is.LessThan(freshness), "the storeroom lost a little freshness overnight");
            SaveData morningSave = SavedGame();
            Assert.That(morningSave.day, Is.EqualTo(2));
            Assert.That(morningSave.phase, Is.EqualTo(nameof(DayPhase.Morning)));
            int goldBefore = Flow.State.Gold;
            int renownBefore = Flow.State.Renown;

            // Quit and come back: Continue restores it all, and the upgrade still works.
            yield return BootToMenu();
            menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(menu.ContinueButton.gameObject.activeSelf);
            Assert.That(ShownText(menu), Has.Some.EqualTo("day 2, morning"));
            menu.ContinueButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Morning, "the morning, continued");
            Assert.That(Flow.State.Day, Is.EqualTo(2));
            Assert.That(Flow.State.Gold, Is.EqualTo(goldBefore));
            Assert.That(Flow.State.Renown, Is.EqualTo(renownBefore));
            Assert.That(Flow.State.UpgradeLevel(satchelUpgrade.id), Is.EqualTo(1));
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(legsLeft), "not duplicated, not lost");
            Assert.That(Flow.State.Storeroom.Stacks.First(s => s.Item.Definition == Leg).Freshness, Is.EqualTo(overnight).Within(1e-4f));
            Object.FindAnyObjectByType<MorningScreen>().DescendButton.onClick.Invoke();
            yield return InDungeon();
            Assert.That(PlayerSatchel.Capacity, Is.EqualTo(dayOneCapacity + 1), "the satchel upgrade applies to the delve");
        }

        [UnityTest]
        public IEnumerator Dying_TheLockboxStack_ComesHome_AndTheEveningGoesOn()
        {
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Morning, "the first morning");
            Object.FindAnyObjectByType<MorningScreen>().DescendButton.onClick.Invoke();
            yield return InDungeon();
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Standard), 2);
            PlayerSatchel.Add(new IngredientItem(Leg, Quality.Fine), 3);

            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            var essence = Player.GetComponent<EssenceHealth>();
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            death.Mark(1);
            death.Confirm.onClick.Invoke();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            result.Proceed();
            yield return InTavern(TavernPhase.Prep, "the evening");

            Assert.That(Flow.State.Today.Delve, Is.EqualTo(DelveOutcome.Died));
            Assert.That(Legs(Flow.State.Storeroom), Is.EqualTo(3), "only the Lockbox stack");
            Assert.That(Flow.State.Storeroom.Stacks.Where(s => !s.IsEmpty).All(s => s.Item.Quality == Quality.Fine), "the stack that was kept");
            Assert.That(Flow.State.Today.PartsLost, Is.EqualTo(2));
            Assert.That(MenuPause.IsPaused, Is.False, "not paused in the tavern");
        }

        [UnityTest]
        public IEnumerator NothingToCook_CloseForTheNight_GoesStraightToNight_AndSleeps()
        {
            yield return BootToMenu();
            Object.FindAnyObjectByType<MainMenuScreen>().NewGameButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Morning, "the first morning");
            Object.FindAnyObjectByType<MorningScreen>().DescendButton.onClick.Invoke();
            yield return InDungeon();
            yield return ExtractAndGoHome();

            var prep = Object.FindAnyObjectByType<PrepScreen>();
            yield return null;
            Assert.That(prep.OpenButton.interactable, Is.False, "an empty storeroom can't open");
            Assert.That(ShownText(prep), Has.Some.EqualTo("nothing in the storeroom makes a dish tonight."));
            prep.CloseButton.onClick.Invoke();
            yield return null;
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Night), "no results to click through");
            Assert.That(SavedGame().phase, Is.EqualTo(nameof(DayPhase.Night)));
            var night = Object.FindAnyObjectByType<NightScreen>();
            yield return null;
            Assert.That(ShownText(night), Has.Some.EqualTo("the doors stayed shut tonight."));
            night.SleepButton.onClick.Invoke();
            yield return InTavern(TavernPhase.Morning, "the next morning");
            Assert.That(Flow.State.Day, Is.EqualTo(2));
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
            yield return InTavern(TavernPhase.Morning, "the first morning");
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
            yield return InTavern(TavernPhase.Morning, "a new first morning");
            Assert.That(SavedGame().gold, Is.Zero, "started over");
        }
    }
}
