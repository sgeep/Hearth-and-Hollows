using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using Hearthdelve.Village;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4h Checkpoint B, "A day's work", in the game: Vigor's pips, the garden's beds (plant, tend, grow over days, harvest into
    /// the storeroom and the cooking), everything that must still work at 0 Vigor, and a mid-day save and Continue.
    /// </summary>
    public class SurfaceCheckpointBTests : LookTestFixture
    {
        string m_SaveDir;

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static Vigor Vigor => Flow.State.Vigor;
        static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        static bool OnFootNow => InputMaps.Find(InputMaps.Tavern, TavernActions.Interact) is { enabled: true };
        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        static GardenBed Bed(string id) => GardenBed.Find(id);
        static SurfaceClockView Hud => Find<SurfaceClockView>();
        static GardenPanel Panel => Find<GardenPanel>();

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
            SurfaceTime.SettingsOverride = null;
            SurfacePause.Clear();
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        /// <summary>Until the scene is in view and settled (not loading, uncovered for several frames running).</summary>
        static IEnumerator Revealed()
        {
            int settled = 0;
            float started = Time.realtimeSinceStartup;
            while (settled < 5)
            {
                bool clear = !Flow.IsLoading && (Flow.Transition == null || !Flow.Transition.IsCovering);
                settled = clear ? settled + 1 : 0;
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(8f), "the scene revealed");
                yield return null;
            }
        }

        static IEnumerator Daytime()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            yield return Revealed();
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the first delve");
            yield return Revealed();
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Revealed();
            yield return Sleep();
        }

        static IEnumerator Sleep()
        {
            Flow.Sleep();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
            yield return Frames(2);
        }

        /// <summary>The rest of the day kept quiet: the evening shut, no delve, the night, sleep.</summary>
        static IEnumerator NextDay()
        {
            Flow.StartEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "the evening");
            yield return Revealed();
            Flow.SkipService();
            Flow.CompleteDelve(DelveReport.Empty);
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "the night");
            yield return Revealed();
            yield return Sleep();
        }

        /// <summary>Plants <paramref name="bed"/> through the bed and the panel, choosing <paramref name="crop"/>'s button.</summary>
        static IEnumerator Plant(string bed, string crop)
        {
            Bed(bed).Interactable.Use();
            yield return Frames(2);
            Assert.That(Panel.IsOpen, $"{bed} asks what to plant");
            int index = Panel.Offered.ToList().FindIndex(c => c.id == crop);
            Panel.CropButtons[index].onClick.Invoke();
            yield return Frames(2);
        }

        // ---------- Vigor's pips, planting and tending ----------

        [UnityTest]
        public IEnumerator ThePips_ShowTheDay_AndPlantingAndTendingSpendThem_OncePerBedADay()
        {
            yield return Daytime();
            Assert.That(Hud.IsShown);
            Assert.That((Hud.ShownPips, Hud.ShownVigor), Is.EqualTo((6, 6)), "six pips, all full");
            Assert.That(Bed(GardenConfig.Bed1).ShownStage, Is.EqualTo(BedStage.Empty));

            // The planting choice: the crops with their days, the first selected, up and down between them.
            Bed(GardenConfig.Bed1).Interactable.Use();
            yield return Frames(2);
            Assert.That(Panel.IsOpen);
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Held), "choosing takes no time");
            Assert.That(Panel.Offered.Select(c => c.id), Is.EqualTo(new[] { "herbs", "onions", "barley" }));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(Panel.CropButtons[0].gameObject));
            Assert.That(Panel.CropButtons[0].navigation.selectOnDown, Is.SameAs(Panel.CropButtons[1]));
            Assert.That(Panel.Cancel.navigation.selectOnDown, Is.SameAs(Panel.CropButtons[0]), "it wraps");
            Panel.Close();
            yield return Frames(2);
            Assert.That((Vigor.Current, Bed(GardenConfig.Bed1).ShownStage, OnFootNow), Is.EqualTo((6, BedStage.Empty, true)), "not now: nothing spent");

            VigorSpent? spent = null;
            void Heard(VigorSpent e) => spent = e;
            EventBus<VigorSpent>.Subscribe(Heard);
            yield return Plant(GardenConfig.Bed1, "onions");
            EventBus<VigorSpent>.Unsubscribe(Heard);
            Assert.That(Flow.State.Garden.Bed(GardenConfig.Bed1).Crop, Is.EqualTo("onions"));
            Assert.That((Vigor.Current, Hud.ShownVigor), Is.EqualTo((4, 4)), "two pips");
            Assert.That(spent, Is.EqualTo(new VigorSpent(VigorActivity.PlantBed, 2, 4)), "the fact the pips (and their tick) answer");
            Assert.That(Bed(GardenConfig.Bed1).ShownStage, Is.EqualTo(BedStage.Seeds));
            Assert.That(Bed(GardenConfig.Bed1).IsActing, "the seeding icon plays over the bed");
            Assert.That(OnFootNow, "back on foot");

            // Tending: one pip, the soil darkens, once a day.
            Assert.That(Bed(GardenConfig.Bed1).Interactable.Hint.NameKey, Is.EqualTo(GardenText.Tend));
            Bed(GardenConfig.Bed1).Interactable.Use();
            yield return Frames(2);
            Assert.That((Vigor.Current, Bed(GardenConfig.Bed1).ShowsTended), Is.EqualTo((3, true)));
            Assert.That(Bed(GardenConfig.Bed1).Interactable.Hint.Kind, Is.EqualTo(TavernHintKind.Growing), "tended: it says when it'll be ready");
            Assert.That(Bed(GardenConfig.Bed1).Interactable.Hint.Count, Is.EqualTo(3));
            Bed(GardenConfig.Bed1).Interactable.Use();
            yield return Frames(2);
            Assert.That((Vigor.Current, Flow.State.Garden.Bed(GardenConfig.Bed1).TendedDays), Is.EqualTo((3, 1)), "a second try spends nothing");
        }

        // ---------- at 0 Vigor ----------

        [UnityTest]
        public IEnumerator AtZeroVigor_OnlyStrenuousWorkIsRefused_AndTheRestOfTheDayWorks()
        {
            yield return Daytime();
            yield return Plant(GardenConfig.Bed1, "herbs");
            yield return Plant(GardenConfig.Bed2, "onions");
            yield return Plant(GardenConfig.Bed3, "barley");
            Assert.That((Vigor.Current, Hud.ShownVigor), Is.EqualTo((0, 0)), "three beds: the day's Vigor");
            Assert.That(Bed(GardenConfig.Bed4).Interactable.Hint, Is.EqualTo(new TavernHint(TavernHintKind.Note, GardenText.TooTiredToPlant)));
            Bed(GardenConfig.Bed4).Interactable.Use();
            yield return Frames(2);
            Assert.That(Panel.IsOpen, Is.False, "no choice offered");
            Assert.That(Flow.State.Garden.Bed(GardenConfig.Bed4).IsEmpty);
            Bed(GardenConfig.Bed1).Interactable.Use();
            yield return Frames(2);
            Assert.That(Flow.State.Garden.Bed(GardenConfig.Bed1).TendedDays, Is.EqualTo(0), "no tending either");
            Assert.That(Bed(GardenConfig.Bed1).Interactable.Hint.Kind, Is.EqualTo(TavernHintKind.Growing), "it just says when it'll be ready");

            // Walking, the clock, the doors and the stairs.
            Assert.That(SurfaceTime.Instance.Still, Is.EqualTo(SurfaceStill.Running), "the day goes on");
            Vector2 before = Keeper.position;
            Hold(Key.S);
            yield return new WaitForSecondsRealtime(0.6f);
            ReleaseKeys();
            yield return Frames(2);
            Assert.That(Vector2.Distance(before, Keeper.position), Is.GreaterThan(0.5f), "walking");
            SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            yield return Frames(2);
            Assert.That(SurfaceArea.Current.Id, Is.EqualTo(SurfaceArea.TavernId), "in through the door");
            Object.FindObjectsByType<AreaPassage>(FindObjectsSortMode.None).First(p => p.From.Id == PropertyArea.TavernId).Pass(Keeper);
            yield return Frames(2);
            Assert.That(PropertyArea.Current.Id, Is.EqualTo(PropertyArea.GuestRoomId), "up the stairs");

            // Talking, decorating, the market.
            Assert.That(StoryServices.Conversations.Play(SurfaceConversations.PhiPortrait));
            yield return Frames(2);
            Assert.That(StoryServices.Conversations.IsTalking);
            DialogueManager.StopAllConversations();
            yield return Frames(3);
            DecorateMode.Instance.Enter();
            yield return Frames(2);
            Assert.That(DecorateMode.Instance.IsActive, "decorating");
            DecorateMode.Instance.Leave();
            yield return WaitUntil(() => OnFootNow && SurfaceTime.Instance.Still == SurfaceStill.Running, 3f, "back from decorating");
            DaytimeActions.OpenMarket();
            yield return Frames(2);
            Assert.That(Find<MarketPanel>().IsOpen, "the market");
            Find<MarketPanel>().Done.onClick.Invoke();
            yield return Frames(2);

            // The evening and the delve: Prep from the board, Essence untouched.
            DaytimeActions.BeginEvening();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "Prep");
            yield return Revealed();
            Assert.That(Hud.IsShown, Is.False, "no Vigor in the evening");
            Flow.CompleteService(new ServiceReport());
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            yield return Revealed();
            EssenceHealth essence = Object.FindAnyObjectByType<EssenceHealth>();
            Assert.That(essence.Essence.Max, Is.EqualTo(essence.Config.essence.baseMax).Within(0.01f), "Essence is the Hollows' own");
            Assert.That(Object.FindObjectsByType<SurfaceClockView>(FindObjectsSortMode.None).Any(v => v.IsShown), Is.False, "no Vigor beside Essence");
        }

        // ---------- growing, harvesting, cooking ----------

        [UnityTest]
        public IEnumerator Crops_GrowOverDays_SleepRefills_AndAHarvestGoesIntoTonightsFood()
        {
            yield return Daytime();
            yield return Plant(GardenConfig.Bed1, "herbs");
            Bed(GardenConfig.Bed1).Interactable.Use();
            yield return Frames(2);
            Assert.That(Vigor.Current, Is.EqualTo(3));
            yield return NextDay();
            Assert.That((Vigor.Current, Hud.ShownVigor), Is.EqualTo((6, 6)), "a night's sleep");
            Assert.That(Bed(GardenConfig.Bed1).ShownStage, Is.EqualTo(BedStage.Growing), "a night's growth shows");
            Assert.That(Bed(GardenConfig.Bed1).Interactable.Hint.NameKey, Is.EqualTo(GardenText.Tend));
            yield return NextDay();
            Assert.That(Bed(GardenConfig.Bed1).ShownStage, Is.EqualTo(BedStage.Ready));
            Assert.That(Bed(GardenConfig.Bed1).Interactable.Hint.NameKey, Is.EqualTo(GardenText.Harvest));

            int herbs = Flow.State.Storeroom.CountMatching(i => i.Definition.id == "herbs");
            Bed(GardenConfig.Bed1).Interactable.Use();
            yield return Frames(2);
            Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == "herbs" && i.Quality == Quality.Fine), Is.EqualTo(3),
                "tended on one of its two days: Fine");
            Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == "herbs"), Is.EqualTo(herbs + 3));
            Assert.That((Vigor.Current, Bed(GardenConfig.Bed1).ShownStage), Is.EqualTo((6, BedStage.Empty)), "free, and the bed is empty again");
            Assert.That(Hud.NoteShown, "a line says what went into the storeroom");
            Assert.That(UiFeedback.Last, Is.EqualTo(UiMoment.Harvest));

            // Tonight's food: the delve meal at the Grill takes the homegrown herbs.
            RecipeDefinition grilled = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            Flow.State.Storeroom.Add(new IngredientStack(new IngredientItem(Flow.Database.Ingredient("spider_leg"), Quality.Standard), 1));
            Assert.That(Director.CanCookDelveMeal(grilled), "spider leg and the garden's herbs");
            Assert.That(Director.CookDelveMeal(grilled));
            yield return Frames(2);
            KeeperWork.Instance.FinishCook(1f);
            yield return Frames(2);
            Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == "herbs"), Is.EqualTo(herbs + 2), "a herb went into it");
        }

        // ---------- mid-day save and Continue ----------

        [UnityTest]
        public IEnumerator AMidDaySave_Continues_AtItsMinute_WithItsVigorAndGarden()
        {
            yield return Daytime();
            yield return Plant(GardenConfig.Bed2, "barley");
            Bed(GardenConfig.Bed2).Interactable.Use();
            yield return Frames(2);
            Flow.State.Surface.Restore(13 * 60 + 20);
            Flow.Save();
            Flow.QuitToMenu();
            yield return WaitUntil(() => !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return Revealed();
            Assert.That(Flow.Continue());
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston), 30f, "the daytime");
            yield return Revealed();
            Assert.That(Flow.State.Surface.WholeMinute, Is.InRange(13 * 60 + 20, 13 * 60 + 40), "the afternoon it was saved in");
            Assert.That(Vigor.Current, Is.EqualTo(3));
            BedState bed = Flow.State.Garden.Bed(GardenConfig.Bed2);
            Assert.That((bed.Crop, bed.TendedDays, bed.LastTendedDay), Is.EqualTo(("barley", 1, Flow.State.Day)));
            yield return Frames(2);
            Assert.That((Bed(GardenConfig.Bed2).ShownStage, Bed(GardenConfig.Bed2).ShowsTended), Is.EqualTo((BedStage.Seeds, true)));
            Assert.That(OnFootNow);
        }

        [UnityTest]
        public IEnumerator EachGardenAction_AndTheEvening_SaveTheDay()
        {
            yield return Daytime();
            yield return Plant(GardenConfig.Bed3, "herbs");
            Assert.That(SavedGarden().beds.First(b => b.id == GardenConfig.Bed3).crop, Is.EqualTo("herbs"), "saved after planting");
            Bed(GardenConfig.Bed3).Interactable.Use();
            yield return Frames(2);
            Assert.That(SavedGarden().beds.First(b => b.id == GardenConfig.Bed3).tendedDays, Is.EqualTo(1), "saved after tending");
            Flow.State.Surface.Restore(15 * 60);
            DaytimeActions.BeginEvening();
            Assert.That(Flow.PeekSave().surface.minute, Is.InRange(15 * 60, 15 * 60 + 10), "the day as it was left for the evening");
            Assert.That(Flow.PeekSave().phase, Is.EqualTo(nameof(DayPhase.Daytime)));
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Prep, 30f, "Prep");
        }

        static Hearthdelve.Shared.Save.GardenSaveData SavedGarden() => Flow.PeekSave().garden;

        // ---------- Checkpoint A's carry-overs ----------

        [UnityTest]
        public IEnumerator TheMarketCart_LetsNobodyStandWhollyHiddenBehindIt_AndTheDecorateReminderSitsClearOfTheRoom()
        {
            yield return Daytime();
            MarketStall stall = DaytimeActions.Stall;
            Collider2D body = stall.GetComponentsInChildren<Collider2D>().First(c => !c.isTrigger);
            Assert.That(body.bounds.max.y - stall.transform.position.y, Is.GreaterThan(4.5f), "the body reaches up behind the canopy");
            RectTransform reminder = (RectTransform)GameObject.Find("SurfaceClock").transform.Find("Decorate");
            Assert.That((reminder.anchorMin, reminder.anchorMax), Is.EqualTo((Vector2.zero, Vector2.zero)), "the bottom-left corner");
        }
    }
}
