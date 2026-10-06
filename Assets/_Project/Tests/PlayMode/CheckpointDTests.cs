using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4f Checkpoint D in the tavern scene: special requests through a real service (met, missed, the results, a clean
    /// next evening), alongside Gunta and Pip; the trophy's clean spot; and the Prep cards' text fitting at 320×180.
    /// </summary>
    public class CheckpointDTests : LookTestFixture
    {
        static TavernDirector Director => TavernDirector.Instance;

        [TearDown]
        public void Restore()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            MenuPause.Clear();
        }

        IEnumerator TavernAtPrep()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            yield return null;
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Prep));
        }

        static float Gap(LocalizedSuperText left, LocalizedSuperText right)
        {
            var l = left.GetComponent<SuperTextMesh>();
            var r = right.GetComponent<SuperTextMesh>();
            l.Rebuild();
            r.Rebuild();
            return (r.finalTopLeftTextBounds.x - l.finalBottomRightTextBounds.x) / left.transform.lossyScale.x;
        }

        static int Lines(LocalizedSuperText label)
        {
            var text = label.GetComponent<SuperTextMesh>();
            text.Rebuild();
            return text.lineHeights.Count - 1;
        }

        static CustomerRequestSettings Always(int max = 1) => new()
        {
            chance = 1f, maxPerEvening = max, ordersBeforeFirst = 0, bonusFraction = 0.5f, minBonusGold = 2, bonusRenown = 1,
        };

        /// <summary>Opens service on one dish the storeroom can make, with requests made certain; the arrivals are paused.</summary>
        static RecipeDefinition OpenWithRequests(string dishId = null, int max = 1)
        {
            RecipeDefinition dish = dishId != null ? Director.Content.recipes.First(r => r.id == dishId)
                : Director.Content.recipes.First(r => r.station != CookStation.StewPot && PrepRules.Makeable(r, Director.Storeroom) > 0);
            foreach (RecipeSlot slot in dish.slots.Where(sl => sl.ingredient != null))
                Director.Storeroom.Add(new IngredientStack(new IngredientItem(slot.ingredient, Quality.Fine), 4, 1f));
            Director.SetMenu(new[] { dish });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            Director.Session.ConfigureRequests(Always(max));
            return dish;
        }

        static CustomerAgent Patron() => Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());

        static string[] ShownText(Component root) =>
            root.GetComponentsInChildren<SuperTextMesh>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToArray();

        IEnumerator ResultsShown()
        {
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            var results = Object.FindAnyObjectByType<EveningResultsScreen>();
            results.DoneButton.onClick.Invoke(); // the first press finishes the reveal
            yield return null;
        }

        [UnityTest]
        public IEnumerator ASpecialRequest_IsMarked_MetThroughOrdinaryService_AndOnTheResults()
        {
            yield return TavernAtPrep();
            Director.AssignStaff(StaffStation.None);
            Director.AssignCook(StaffStation.None);
            RecipeDefinition dish = OpenWithRequests();
            var issued = new List<CustomerRequestIssued>();
            var met = new List<CustomerRequestCompleted>();
            var served = new List<DishServed>();
            void Issued(CustomerRequestIssued e) => issued.Add(e);
            void Met(CustomerRequestCompleted e) => met.Add(e);
            void Served(DishServed e) => served.Add(e);
            EventBus<CustomerRequestIssued>.Subscribe(Issued);
            EventBus<CustomerRequestCompleted>.Subscribe(Met);
            EventBus<DishServed>.Subscribe(Served);
            try
            {
                CustomerAgent patron = Patron();
                yield return WaitUntil(() => patron.Logic.State == CustomerState.WaitingForFood, 30f, "an order");
                yield return null;
                Assert.That(patron.Logic.IsRequest);
                Assert.That(issued.Single().RecipeId, Is.EqualTo(dish.id));
                Assert.That(issued.Single().PatronId, Is.EqualTo(patron.Logic.Profile.id));
                Assert.That(patron.ShowsRequest, "the sparkle on their bubble");
                var hud = Object.FindAnyObjectByType<TavernHud>();
                Assert.That(hud.Rows[0].request.enabled, "and on the order rail");

                Ticket ticket = Director.Session.Tickets.Single();
                Director.Session.StartCooking(ticket, this);
                Director.Session.FinishCooking(ticket, 1f);
                Director.Session.StartDelivery(ticket, this);
                Director.Session.Deliver(ticket, patron.Logic, 1f);
                Time.timeScale = 6f;
                yield return WaitUntil(() => met.Count == 1, 40f, "them to pay");
                Time.timeScale = 1f;
                Assert.That(met[0].BonusGold, Is.GreaterThanOrEqualTo(2));
                Assert.That(met[0].BonusRenown, Is.EqualTo(1));
                Assert.That(served.Single().WasRequest);
                Assert.That(patron.ShowsRequest, Is.False);
                Assert.That(Director.Session.Ledger.RequestGold, Is.EqualTo(met[0].BonusGold));
            }
            finally
            {
                Time.timeScale = 1f;
                EventBus<CustomerRequestIssued>.Unsubscribe(Issued);
                EventBus<CustomerRequestCompleted>.Unsubscribe(Met);
                EventBus<DishServed>.Unsubscribe(Served);
            }
            var completed = new List<ServiceCompleted>();
            void Done(ServiceCompleted e) => completed.Add(e);
            EventBus<ServiceCompleted>.Subscribe(Done);
            yield return ResultsShown();
            EventBus<ServiceCompleted>.Unsubscribe(Done);
            Assert.That(completed.Single().RequestsCompleted, Is.EqualTo(1));
            string[] shown = ShownText(Object.FindAnyObjectByType<EveningResultsScreen>());
            Assert.That(shown, Has.Some.EqualTo("special requests").And.Some.EqualTo("1 of 1"));

            // The next evening starts clean.
            Director.FinishEvening();
            yield return WaitUntil(() => Director != null && Director.Phase == TavernPhase.Prep && Director.Session == null, 10f, "the next evening (the scene again)");
            yield return null;
            yield return null;
            Assert.That(Director.Session, Is.Null, "no service carried over");
            OpenWithRequests(max: 0);
            Assert.That(Director.Session.Ledger.RequestsIssued, Is.Zero);
            CustomerAgent next = Patron();
            yield return WaitUntil(() => next.Logic.State == CustomerState.WaitingForFood, 30f, "an order");
            Assert.That(next.Logic.IsRequest, Is.False);
            Assert.That(next.ShowsRequest, Is.False);
        }

        [UnityTest]
        public IEnumerator AMissedRequest_GetsAFrown_AFact_AndNothingExtra()
        {
            yield return TavernAtPrep();
            Director.AssignStaff(StaffStation.None);
            Director.AssignCook(StaffStation.None);
            OpenWithRequests();
            var missed = new List<CustomerRequestFailed>();
            void Missed(CustomerRequestFailed e) => missed.Add(e);
            EventBus<CustomerRequestFailed>.Subscribe(Missed);
            CustomerAgent patron = Patron();
            yield return WaitUntil(() => patron.Logic.State == CustomerState.WaitingForFood, 30f, "an order");
            Assert.That(patron.Logic.IsRequest);
            yield return ResultsShown();
            EventBus<CustomerRequestFailed>.Unsubscribe(Missed);
            Assert.That(missed.Single().Reason, Is.EqualTo("closing_time"));
            Assert.That(patron.Logic.RequestOutcome, Is.EqualTo(RequestOutcome.ClosingTime));
            Assert.That(Director.Session.Ledger.RequestGold, Is.Zero);
            Assert.That(ShownText(Object.FindAnyObjectByType<EveningResultsScreen>()), Has.Some.EqualTo("0 of 1"));
        }

        [UnityTest]
        public IEnumerator GuntaAndPip_MeetASpecialRequest_BetweenThem()
        {
            yield return TavernAtPrep();
            Director.AssignStaff(StaffStation.Serving);
            Director.AssignCook(StaffStation.Grill);
            OpenWithRequests("grilled_spider_leg");
            bool met = false;
            void Met(CustomerRequestCompleted e) => met = true;
            EventBus<CustomerRequestCompleted>.Subscribe(Met);
            CustomerAgent patron = Patron();
            Time.timeScale = 4f;
            yield return WaitUntil(() => met, 60f, "Gunta to grill it and Pip to carry it");
            Time.timeScale = 1f;
            EventBus<CustomerRequestCompleted>.Unsubscribe(Met);
            Assert.That(patron.Logic.RequestOutcome, Is.EqualTo(RequestOutcome.Completed));
        }

        /// <summary>The first trophy has a clean default spot on the starting back wall (the Checkpoint C playtest).</summary>
        [UnityTest]
        public IEnumerator TheTusks_FirstOfferedSpot_IsClearOfEverythingElse()
        {
            yield return TavernAtPrep();
            AreaFurniture tavern = AreaFurniture.Tavern;
            tavern.State.Receive(tavern.Definition("trophy_larder_troll"));
            tavern.State.PendingHomecoming = "trophy_larder_troll";
            DecorateMode mode = DecorateMode.Instance;
            mode.Enter(tavern);
            yield return null;
            Assert.That(mode.HomecomingPiece, Is.EqualTo("trophy_larder_troll"));
            Assert.That(mode.CarriedCheck.IsValid);
            var layout = new Hearthdelve.Shared.Customization.FurnitureLayout(tavern.Shape(), tavern.Definition, tavern.CurrentLayout());
            Hearthdelve.Shared.Customization.ResolvedFurniture tusks = layout.Resolve(mode.Carried);
            Rect art = Hearthdelve.Shared.Customization.FurnitureGeometry.ArtBounds(tusks);
            foreach (var other in tavern.CurrentLayout())
            {
                var o = layout.Resolve(other);
                if (o == null) continue;
                Assert.That(Hearthdelve.Shared.Customization.FurnitureGeometry.ArtBounds(o).Overlaps(art), Is.False, $"clear of {other.definition}");
            }
            mode.Place();
            Assert.That(mode.HomecomingPiece, Is.Null, "hung where it was offered");
            mode.Leave();
        }

        /// <summary>
        /// A job outside service never hangs (the 4f web check: Gunta pressed against the far side of the Butcher Block):
        /// if the way is blocked, she steps straight to it after a while and the cut is made.
        /// </summary>
        [UnityTest]
        public IEnumerator Gunta_StepsToTheBlock_WhenSheCannotWalkThere()
        {
            yield return TavernAtPrep();
            StaffAgent gunta = Director.Cook;
            // A spot inside the bar's solid body: no path reaches it.
            var inside = new Vector2(5f, 12f);
            bool done = false;
            Assert.That(gunta.DoTask(inside, 0.5f, () => done = true));
            Time.timeScale = 4f;
            yield return WaitUntil(() => done, 20f, "the job done anyway");
            Time.timeScale = 1f;
            Assert.That(gunta.SteppedToTask);
            Assert.That(gunta.HasTask, Is.False);
        }

        /// <summary>
        /// Staff and patrons arrive at a slow frame rate too (the 4f web check: in a background tab, at three frames a
        /// second, Pip circled the pass for ever and never picked up a plate). A goal within one frame's travel counts as reached.
        /// </summary>
        [UnityTest]
        public IEnumerator Pip_Serves_AtThreeFramesASecond()
        {
            yield return TavernAtPrep();
            Director.AssignStaff(StaffStation.Serving);
            Director.AssignCook(StaffStation.Grill);
            OpenWithRequests("grilled_spider_leg");
            int served = 0;
            void Served(DishServed e) => served++;
            EventBus<DishServed>.Subscribe(Served);
            Patron();
            Time.captureDeltaTime = 1f / 3f;
            for (int frame = 0; frame < 300 && served == 0; frame++) yield return null;
            Time.captureDeltaTime = 0f;
            EventBus<DishServed>.Unsubscribe(Served);
            Assert.That(served, Is.EqualTo(1), "the patron walked in and sat, Gunta grilled it and Pip carried it");
        }

        /// <summary>Every dish's card, on every page: one line each, and three pixels of daylight between name and price.</summary>
        [UnityTest]
        public IEnumerator EveryDishCard_NameClearsItsPrice_AndNothingWraps()
        {
            yield return TavernAtPrep();
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            var problems = new List<string>();
            for (int page = 0; page < prep.PageCount; page++)
            {
                yield return null;
                foreach (DishCard card in prep.Cards.Where(c => c.name.isActiveAndEnabled))
                {
                    string name = card.name.GetComponent<SuperTextMesh>().text;
                    foreach (var label in new[] { card.name, card.detail, card.amount, card.steps })
                        if (label.isActiveAndEnabled && Lines(label) > 1) problems.Add($"{name}: \"{label.GetComponent<SuperTextMesh>().text}\" wraps");
                    float top = Gap(card.name, card.detail), bottom = Gap(card.amount, card.steps);
                    if (top < 3f) problems.Add($"\"{name}\" runs into \"{card.detail.GetComponent<SuperTextMesh>().text}\" ({top:0.#} px)");
                    if (bottom < 3f) problems.Add($"{name}: \"{card.amount.GetComponent<SuperTextMesh>().text}\" runs into \"{card.steps.GetComponent<SuperTextMesh>().text}\" ({bottom:0.#} px)");
                }
                prep.NextPage();
            }
            Assert.That(problems, Is.Empty);
        }
    }
}
