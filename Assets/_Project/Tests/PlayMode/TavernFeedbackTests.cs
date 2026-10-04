using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Services;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4c step 6 in the <c>Tavern</c> scene: cooking and serving play their named moments (sound, visuals and a named
    /// pattern together), read from the minigames' own state; the grill's warning rumble starts only past the band; the
    /// player's vibration setting and unsupported controllers are respected; the menus click quietly.
    /// </summary>
    public class TavernFeedbackTests : LookTestFixture
    {
        readonly List<string> m_Patterns = new();
        static TavernDirector Director => TavernDirector.Instance;
        static KeeperWork Keeper => KeeperWork.Instance;
        static TavernFeedback Feedback => Object.FindAnyObjectByType<TavernFeedback>();

        void Record(string id) => m_Patterns.Add(id);

        [SetUp]
        public void Listen()
        {
            m_Patterns.Clear();
            HapticService.PatternPlayed += Record;
        }

        [TearDown]
        public void StopListening()
        {
            HapticService.PatternPlayed -= Record;
            GameSettings.VibrationEnabled = true;
        }

        IEnumerator OpenWith(string recipeId)
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            RecipeDefinition recipe = Director.Content.recipes.First(r => r.id == recipeId);
            Director.SetMenu(new[] { recipe });
            Director.AssignStaff(StaffStation.None);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            yield return null;
        }

        /// <summary>Opens the grill's panel with an order to cook.</summary>
        IEnumerator CookAtTheGrill()
        {
            yield return OpenWith("cellar_kebab");
            var customer = Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());
            yield return WaitUntil(() => customer.Logic.State == Hearthdelve.Tavern.Customers.CustomerState.WaitingForFood, 30f, "an order");
            TavernInteractable grill = Object.FindObjectsByType<TavernInteractable>().First(s => s.Kind == TavernInteractableKind.Grill);
            Teleport(Player, grill.UsePoint);
            var interactor = Player.GetComponent<TavernInteractor>();
            yield return WaitUntil(() => interactor.Target == grill, 2f, "the grill");
            Hold(Key.E);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
            Assert.That(Keeper.ActiveCook, Is.InstanceOf<GrillMinigame>());
        }

        [UnityTest]
        public IEnumerator AGrillFlip_InTheBand_IsPerfect_AndABurn_IsAFailure_AndTheWarningComesOnlyPastTheBand()
        {
            yield return CookAtTheGrill();
            var grill = (GrillMinigame)Keeper.ActiveCook;

            // Up to the middle of the band: no warning rumble.
            while (grill.Meter < grill.Settings.BandCenter) grill.Tick(0.05f, default);
            yield return null;
            Assert.That(Feedback.GrillWarning, Is.Zero, "cooking normally is quiet");
            grill.Tick(0.01f, new MinigameInput { ActionPressed = true });
            yield return null;
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.perfectFlip)));
            Assert.That(m_Patterns, Has.Member(HapticIds.PulseSuccess), "a perfect flip pulses");
            Assert.That(Object.FindAnyObjectByType<StationPanel>().IsFlashing, "and the panel flashes gold");

            // The second side: let it run past the band (the warning grows), then burn.
            while (grill.IsPausing) { grill.Tick(0.05f, default); }
            while (grill.Meter < grill.Settings.bandMax + 0.05f) grill.Tick(0.05f, default);
            yield return null;
            float early = Feedback.GrillWarning;
            Assert.That(early, Is.GreaterThan(0f), "past the band: a warning");
            Assert.That(Haptics.PeakHigh, Is.GreaterThan(0f), "felt through the controller");
            while (grill.Meter < 0.97f) grill.Tick(0.02f, default);
            yield return null;
            Assert.That(Feedback.GrillWarning, Is.GreaterThan(early), "stronger nearer the burn");
            m_Patterns.Clear();
            grill.Tick(0.5f, default);
            yield return null;
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.burned)));
            Assert.That(m_Patterns, Has.Member(HapticIds.BuzzFailure), "a burn buzzes");
            Assert.That(Feedback.GrillWarning, Is.Zero, "and the warning stops with the cooking");
        }

        [UnityTest]
        public IEnumerator Bumps_AreSoftOrHard_TheSpillWarns_AndOnlyTheKeepersCount()
        {
            yield return OpenWith("cellar_kebab");
            EventBus<KeeperPlate>.Publish(new KeeperPlate(PlateMoment.PickedUp, Director.Menu[0]));
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.pickUp)));

            m_Patterns.Clear();
            EventBus<ServingBumped>.Publish(new ServingBumped(0.6f, 0.2f, false, true));
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.softBump)));
            Assert.That(m_Patterns, Is.EqualTo(new[] { HapticIds.BumpSoft }));

            EventBus<ServingBumped>.Publish(new ServingBumped(1.3f, 0.5f, false, true));
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.hardBump)));

            EventBus<ServingBumped>.Publish(new ServingBumped(0.7f, 0.75f, false, true));
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.spillWarning)), "close to falling: warned");

            m_Patterns.Clear();
            EventBus<ServingBumped>.Publish(new ServingBumped(1.4f, 0.9f, false, false));
            Assert.That(m_Patterns, Is.Empty, "Pip's bumps are Pip's");

            EventBus<KeeperPlate>.Publish(new KeeperPlate(PlateMoment.Dropped, Director.Menu[0]));
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.dropped)));
            Assert.That(m_Patterns, Has.Member(HapticIds.BuzzFailure));
        }

        [UnityTest]
        public IEnumerator WithVibrationOff_OrNoController_NothingRumbles_AndNothingBreaks()
        {
            yield return CookAtTheGrill();
            var grill = (GrillMinigame)Keeper.ActiveCook;
            GameSettings.VibrationEnabled = false;
            while (grill.Meter < 0.95f) grill.Tick(0.05f, default);
            yield return new WaitForSeconds(0.1f);
            Assert.That(Feedback.GrillWarning, Is.GreaterThan(0f), "the warning still drives the sound and the needle");
            Assert.That(Haptics.PeakLow + Haptics.PeakHigh, Is.Zero, "but vibration is off");

            // A controller that can't rumble (or the web build): the feedback plays without it.
            HapticService.Instance.Output = new NullHapticOutput();
            GameSettings.VibrationEnabled = true;
            grill.Tick(0.01f, new MinigameInput { ActionPressed = true });
            yield return new WaitForSeconds(0.1f);
            Assert.That(Feedback.LastMoment, Is.EqualTo(nameof(TavernMoments.flip)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheMenus_Click_AndOnlyCommitments_AreFelt()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            yield return null;
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            m_Patterns.Clear();
            prep.Cards[0].button.onClick.Invoke();
            Assert.That(UiFeedback.Last, Is.EqualTo(UiMoment.Confirm), "choosing a dish clicks");
            Assert.That(m_Patterns, Is.Empty, "and isn't felt");
            prep.OpenButton.onClick.Invoke();
            Assert.That(UiFeedback.Last, Is.EqualTo(UiMoment.Commit), "opening the doors is a commitment");
            Assert.That(m_Patterns, Is.EqualTo(new[] { HapticIds.TapLight }), "felt as a very light tap");
        }
    }
}
