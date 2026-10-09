using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Game;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Tavern;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4c step 3 in the <c>Tavern</c> scene: cooking at the Grill, Tap and Stew Pot (panels and the
    /// Minigame map), plates on the pass, carrying and serving in 2D (bumps, spills, drops, the wrong
    /// customer, putting back), and Orik doing their job.
    /// </summary>
    public class TavernServiceTests : LookTestFixture
    {
        const string Scene = "Tavern";
        TavernDirector Director => TavernDirector.Instance;
        KeeperWork Keeper => KeeperWork.Instance;

        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        /// <summary>Opens an evening with just this dish on the menu (debug-filled storeroom), Orik off duty unless asked.</summary>
        IEnumerator Open(string recipeId, StaffStation pip = StaffStation.None)
        {
            yield return Load(Scene);
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            Director.ArrivalsPaused = true;
            RecipeDefinition recipe = Director.Content.recipes.First(r => r.id == recipeId);
            Director.SetMenu(new[] { recipe });
            Director.AssignStaff(pip);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            Assert.That(Director.IsServing && Director.Menu.Single() == recipe, $"open with {recipeId} on the menu");
        }

        /// <summary>Lets a customer in and waits until they've ordered.</summary>
        IEnumerator Order(System.Action<CustomerAgent> got, bool patient = false)
        {
            CustomerProfile profile = null;
            if (patient)
            {
                // For tests that speed time up: patience that outlasts it.
                profile = Object.Instantiate(Director.Content.customers[0]);
                profile.traits.orderPatience = 1000f;
            }
            CustomerAgent customer = Director.SpawnCustomer(profile);
            yield return WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood, 20f, "the customer to sit and order");
            got(customer);
        }

        IEnumerator Interact()
        {
            Hold(Key.E);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
        }

        IEnumerator UseAt(TavernInteractable target)
        {
            // The pass's use point is the middle of its table: stand in front of it.
            Teleport(Player, target.Kind == TavernInteractableKind.Pass ? target.UsePoint + Vector2.down : target.UsePoint);
            var interactor = Player.GetComponent<TavernInteractor>();
            yield return WaitUntil(() => interactor.Target == target, 2f, $"{target.Kind} to be the target");
            yield return Interact();
        }

        static TavernInteractable Station(TavernInteractableKind kind) => Object.FindObjectsByType<TavernInteractable>().First(s => s.Kind == kind);
        Ticket TicketFor(CustomerAgent customer) => Director.Session.Tickets.First(t => t.Customer == customer.Logic);

        [UnityTest]
        public IEnumerator TheGrill_CooksAnOrder_InItsPanel_AndThePlateGoesOnThePass()
        {
            yield return Open("cellar_kebab");
            CustomerAgent customer = null;
            yield return Order(c => customer = c);
            Ticket ticket = TicketFor(customer);
            Assert.That(ticket.State, Is.EqualTo(TicketState.Queued));

            yield return UseAt(Station(TavernInteractableKind.Grill));
            Assert.That(Keeper.ActiveCook, Is.InstanceOf<GrillMinigame>(), "the Grill's minigame opens");
            Assert.That(ticket.State, Is.EqualTo(TicketState.Cooking));
            yield return null;
            Assert.That(Object.FindAnyObjectByType<StationPanel>().Showing, Is.SameAs(Keeper.ActiveCook), "the panel draws it");
            Assert.That(InputMaps.Find(InputMaps.Minigame, MinigameActions.Action).enabled, "minigame input is on");
            Assert.That(InputMaps.Find(InputMaps.Tavern, TavernActions.Move).enabled, Is.False, "walking is off while cooking");
            Assert.That(Object.FindAnyObjectByType<KitchenView>().IsWorking, "the kitchen is at work");

            Keeper.FinishCook(1f);
            yield return null;
            Assert.That(ticket.State, Is.EqualTo(TicketState.Ready), "the plate is on the pass");
            Assert.That(Object.FindAnyObjectByType<PassView>().Showing, Is.EqualTo(1), "and shows there");
            Assert.That(InputMaps.Find(InputMaps.Tavern, TavernActions.Move).enabled, "walking again");
            Assert.That(Object.FindAnyObjectByType<StationPanel>().Showing, Is.Null, "the panel closes");
        }

        [UnityTest]
        public IEnumerator SteppingAway_PutsTheOrderBack()
        {
            yield return Open("core_tonic");
            CustomerAgent customer = null;
            yield return Order(c => customer = c);
            yield return UseAt(Station(TavernInteractableKind.Tap));
            Assert.That(Keeper.ActiveCook, Is.InstanceOf<TapMinigame>());
            Hold(Key.Escape);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
            Assert.That(Keeper.ActiveCook, Is.Null);
            Assert.That(TicketFor(customer).State, Is.EqualTo(TicketState.Queued), "the order waits to be cooked again");
        }

        [UnityTest]
        public IEnumerator Serving_CarryThePlate_ServeTheRightCustomer_AndItScoresTheWalk()
        {
            yield return Open("cellar_kebab");
            CustomerAgent customer = null;
            yield return Order(c => customer = c);
            yield return UseAt(Station(TavernInteractableKind.Grill));
            Keeper.FinishCook(1f);
            yield return null;

            yield return UseAt(Station(TavernInteractableKind.Pass));
            Assert.That(Keeper.Carrying, Is.Not.Null, "picked up the plate");
            var carry = Player.GetComponent<CarryView>();
            Assert.That(carry.IsShowing && carry.Icon == customer.Logic.Order.icon, "the dish shows over the keeper's head");
            Assert.That(Player.FindAbility<CharacterMovement>().MovementSpeed, Is.EqualTo(Keeper.Carrying.Settings.carrySpeed), "slower while carrying");

            TavernInteractable seat = Object.FindObjectsByType<TavernSeat>().First(s => s == Director.Layout.Seat(customer.Logic.Seat)).GetComponent<TavernInteractable>();
            Teleport(Player, Director.Layout.Seat(customer.Logic.Seat).ApproachPoint);
            var interactor = Player.GetComponent<TavernInteractor>();
            yield return WaitUntil(() => interactor.Target == seat, 2f, "the customer's seat to be the target");
            Assert.That(seat.Hint.Kind, Is.EqualTo(TavernHintKind.Serve));
            yield return Interact();
            Assert.That(customer.Logic.State, Is.EqualTo(CustomerState.Eating), "served");
            Assert.That(Director.Session.Tickets.Single().State, Is.EqualTo(TicketState.Served));
            Assert.That(customer.Logic.DishQuality, Is.GreaterThan(0f));
            Assert.That(carry.IsShowing, Is.False, "empty-handed again");
            Assert.That(Player.FindAbility<CharacterMovement>().MovementSpeed, Is.GreaterThan(Keeper.Carrying?.Settings.carrySpeed ?? 4f), "back to walking speed");
        }

        [UnityTest]
        public IEnumerator ThePass_TakesACarriedPlateBack()
        {
            yield return Open("cellar_kebab");
            CustomerAgent customer = null;
            yield return Order(c => customer = c);
            yield return UseAt(Station(TavernInteractableKind.Grill));
            Keeper.FinishCook(1f);
            yield return UseAt(Station(TavernInteractableKind.Pass));
            Assert.That(Keeper.Carrying, Is.Not.Null);
            yield return UseAt(Station(TavernInteractableKind.Pass));
            Assert.That(Keeper.Carrying, Is.Null, "put back on the pass");
            Assert.That(TicketFor(customer).State, Is.EqualTo(TicketState.Ready));
        }

        [UnityTest]
        public IEnumerator BumpsFromWalkingCustomers_SpillThePlate_AndAFullMeterDropsIt()
        {
            yield return Open("cellar_kebab");
            CustomerAgent customer = null;
            yield return Order(c => customer = c);
            yield return UseAt(Station(TavernInteractableKind.Grill));
            Keeper.FinishCook(1f);
            yield return UseAt(Station(TavernInteractableKind.Pass));
            ServingMinigame plate = Keeper.Carrying;

            // Stand in a walking customer's way, over and over (letting another in once one sits down).
            CustomerAgent walker = Director.SpawnCustomer();
            float until = Time.time + 10f;
            while (Time.time < until && Keeper.Carrying != null)
            {
                if (walker == null || walker.IsSeated) walker = Director.SpawnCustomer();
                if (walker != null && walker.IsWalking) Teleport(Player, walker.transform.position);
                yield return new WaitForFixedUpdate();
            }
            Assert.That(plate.Spill, Is.GreaterThan(0f), "bumps spilled the plate");
            Assert.That(plate.Dropped, "and enough of them dropped it");
            Assert.That(Keeper.Carrying, Is.Null);
            Assert.That(Director.Session.Ledger.DroppedDishes, Is.EqualTo(1));
            Assert.That(TicketFor(customer).State, Is.EqualTo(TicketState.Queued), "the order goes back to the kitchen");
        }

        [UnityTest]
        public IEnumerator TheStewPot_ChopThenSimmer_ThenHelpingsGoToThePass()
        {
            yield return Open("cellar_stew");
            CustomerAgent customer = null;
            yield return Order(c => customer = c, patient: true);
            TavernInteractable pot = Station(TavernInteractableKind.StewPot);
            Assert.That(pot.Hint.Kind, Is.EqualTo(TavernHintKind.StartStew));
            yield return UseAt(pot);
            Assert.That(Keeper.ActiveCook, Is.InstanceOf<ChopMinigame>(), "chopping opens");
            Keeper.FinishCook(1f);
            var view = Object.FindAnyObjectByType<StewPotView>();
            yield return null;
            Assert.That(Director.Session.Pot.State, Is.EqualTo(PotState.Simmering));
            Assert.That(view.PipsShown, Is.EqualTo(5), "clean chopping: five helpings");
            Time.timeScale = 8f;
            yield return WaitUntil(() => Director.Session.Tickets.Any(t => t.Customer == customer.Logic && t.State == TicketState.Ready), 40f, "a helping to reach the pass (25 s of simmering, sped up)");
            Time.timeScale = 1f;
            yield return null;
            Assert.That(view.PipsShown, Is.EqualTo(4), "one ladled");
        }

        /// <summary>
        /// The cap is on Orik's own work, his serve: the dish's quality also carries the keeper's cooking and the ingredients, so
        /// a perfect cook served by Orik at his cap (a serving factor of 0.94) can come out above 0.85, and that's right. Until
        /// 4i-C this test compared the dish's quality with the cap, which failed whenever Orik served well (0.861 on one run): the
        /// rule held, the assertion measured the wrong thing. Now it checks his serve against the cap, and that the dish he
        /// carries is worse than the keeper's own perfect serve of it, so the player's best always beats his.
        /// </summary>
        [UnityTest]
        public IEnumerator Pip_OnServing_CarriesPlatesToWhoeverOrderedThem()
        {
            var work = new List<StaffWorkDone>();
            EventBus<StaffWorkDone>.Subscribe(work.Add);
            try
            {
                yield return Open("cellar_kebab", StaffStation.Serving);
                CustomerAgent customer = null;
                yield return Order(c => customer = c);
                yield return UseAt(Station(TavernInteractableKind.Grill));
                Keeper.FinishCook(1f);
                Teleport(Player, new Vector2(3f, 3f));
                StaffAgent pip = Director.Staff;
                yield return WaitUntil(() => pip.Carrying != null, 6f, "Orik to pick up the plate");
                Assert.That(pip.GetComponent<CarryView>().IsShowing, "the plate shows over Orik's head");
                Ticket ticket = TicketFor(customer);
                yield return WaitUntil(() => customer.Logic.State == CustomerState.Eating, 15f, "Orik to serve it");
                StaffWorkDone serve = work.Single(w => w.StaffId == pip.Member.id);
                Assert.That(serve.Quality, Is.LessThanOrEqualTo(pip.Member.qualityCap + 1e-4f), "Orik's serve is capped");
                DishScoringSettings scoring = Director.Session.Scoring;
                float keepersBest = DishScoring.DishQuality(ticket.Reserved.Used, DishScoring.MinigameScore(ticket.CookScore, 1f, scoring), scoring);
                float orik = DishScoring.DishQuality(ticket.Reserved.Used, DishScoring.MinigameScore(ticket.CookScore, serve.Quality, scoring), scoring);
                Assert.That(customer.Logic.DishQuality, Is.EqualTo(orik).Within(1e-3f), "the dish is the keeper's cooking with Orik's capped serve");
                Assert.That(customer.Logic.DishQuality, Is.LessThan(keepersBest), "the keeper's own perfect serve beats Orik's");
            }
            finally
            {
                EventBus<StaffWorkDone>.Unsubscribe(work.Add);
            }
        }

        [UnityTest]
        public IEnumerator Pip_AtTheGrill_CooksOrders_AndTheKeeperCantUseIt()
        {
            yield return Open("cellar_kebab", StaffStation.Grill);
            CustomerAgent customer = null;
            yield return Order(c => customer = c);
            TavernInteractable grill = Station(TavernInteractableKind.Grill);
            Assert.That(grill.Hint.Kind, Is.EqualTo(TavernHintKind.Staffed), "Orik is working here");
            yield return UseAt(grill);
            Assert.That(Keeper.ActiveCook, Is.Null, "the keeper can't take over");
            yield return WaitUntil(() => TicketFor(customer).State == TicketState.Ready, 30f, "Orik to cook it");
        }
    }
}
