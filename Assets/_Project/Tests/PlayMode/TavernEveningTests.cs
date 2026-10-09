using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4c step 4: one complete evening in the <c>Tavern</c> scene. Prep (the storeroom, choosing dishes, Orik's job,
    /// opening or closing for the night), the service HUD (orders, clock, takings, the menu), service timing and an
    /// early sell-out, and Results (the evening's lines, then another evening).
    /// </summary>
    public class TavernEveningTests : LookTestFixture
    {
        const string Scene = "Tavern";
        TavernDirector Director => TavernDirector.Instance;
        static PrepScreen Prep => Object.FindAnyObjectByType<PrepScreen>();
        static TavernHud Hud => Object.FindAnyObjectByType<TavernHud>();
        static EveningResultsScreen Results => Object.FindAnyObjectByType<EveningResultsScreen>();

        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        IEnumerator LoadPrep()
        {
            yield return Load(Scene);
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            yield return null;
        }

        /// <summary>The card showing a dish, turning the menu's pages to it (six cards a page since 4f Checkpoint C).</summary>
        int CardOf(string recipeId)
        {
            for (int page = 0; page < Prep.PageCount; page++)
            {
                int at = Prep.Recipes.ToList().FindIndex(r => r.id == recipeId);
                if (at >= 0) return at;
                Prep.NextPage();
            }
            return -1;
        }

        [UnityTest]
        public IEnumerator TheEvening_StartsAtPrep_WithTheStoreroomAndADishCardEach()
        {
            yield return LoadPrep();
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Prep));
            Assert.That(Prep.IsShown, "the prep screen is up");
            Assert.That(Hud.IsShown, Is.False, "no service HUD yet");
            Assert.That(Results.IsShown, Is.False);
            Assert.That(InputMaps.Find(InputMaps.Tavern, TavernActions.Move).enabled, Is.False, "the keeper waits while you plan");

            // 4f Checkpoint C: thirteen dishes, six cards a page, dishes the storeroom can make first.
            int dishes = Director.Content.recipes.Count;
            Assert.That(Prep.Recipes.Count, Is.EqualTo(Mathf.Min(dishes, 6)), "a card per dish on this page");
            Assert.That(Prep.PageCount, Is.EqualTo((dishes + 5) / 6));
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int page = 0; page < Prep.PageCount; page++)
            {
                foreach (var r in Prep.Recipes) seen.Add(r.id);
                Prep.NextPage();
            }
            Assert.That(seen.Count, Is.EqualTo(dishes), "every dish is on some page");
            Assert.That(Prep.Page, Is.Zero, "and round again");
            DishCard stew = Prep.Cards[CardOf("cellar_stew")];
            Assert.That(stew.steps.GetComponent<SuperTextMesh>().text, Is.EqualTo("chop, simmer"), "the stew's two steps");
            DishCard kebab = Prep.Cards[CardOf("cellar_kebab")];
            Assert.That(kebab.steps.GetComponent<SuperTextMesh>().text, Is.EqualTo("grill"));
            Assert.That(kebab.detail.GetComponent<SuperTextMesh>().text, Is.EqualTo($"{Director.Content.recipes.First(r => r.id == "cellar_kebab").baseValue} gold"), "the price");
            Assert.That(kebab.amount.GetComponent<SuperTextMesh>().text, Does.EndWith(" to serve"), "how many");
            Assert.That(Prep.OpenButton.interactable, Is.False, "nothing chosen yet: the doors stay shut");
        }

        [UnityTest]
        public IEnumerator ChoosingDishes_UpToThree_AndPip_ThenOpeningTheDoors()
        {
            yield return LoadPrep();
            var makeable = Prep.Recipes.Select((r, i) => (r, i)).Where(x => PrepRules.Makeable(x.r, Director.Storeroom) > 0).Select(x => x.i).ToList();
            Assume.That(makeable.Count, Is.GreaterThanOrEqualTo(4), "the debug storeroom makes several dishes");
            foreach (int card in makeable.Take(4)) Prep.Cards[card].button.onClick.Invoke();
            yield return null;
            Assert.That(Director.Menu.Count, Is.EqualTo(3), "no more than three");
            Assert.That(Prep.Cards[makeable[0]].selected.activeSelf, "a chosen dish is marked");
            Assert.That(Prep.Cards[makeable[3]].selected.activeSelf, Is.False, "the fourth didn't fit");
            Prep.Cards[makeable[0]].button.onClick.Invoke();
            yield return null;
            Assert.That(Director.Menu.Count, Is.EqualTo(2), "tapping a chosen dish takes it off");

            StaffStation before = Director.StaffAssignment;
            Prep.StaffButton.onClick.Invoke();
            Assert.That(Director.StaffAssignment, Is.Not.EqualTo(before), "Orik's job changes");
            while (Director.StaffAssignment != StaffStation.Grill) Prep.StaffButton.onClick.Invoke();

            Assert.That(Prep.OpenButton.interactable);
            Prep.OpenButton.onClick.Invoke();
            yield return null;
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Service));
            Assert.That(Prep.IsShown, Is.False);
            Assert.That(Hud.IsShown, "the HUD during service");
            Assert.That(InputMaps.Find(InputMaps.Tavern, TavernActions.Move).enabled, "walking again");
            Assert.That(Director.Staff.Assignment, Is.EqualTo(StaffStation.Grill), "Orik works the job chosen at prep");
        }

        [UnityTest]
        public IEnumerator TheHud_ShowsOrdersOnTheRail_TheClockRunningDown_AndTheTakings()
        {
            yield return LoadPrep();
            RecipeDefinition kebab = Director.Content.recipes.First(r => r.id == "cellar_kebab");
            Director.SetMenu(new[] { kebab });
            Director.AssignStaff(StaffStation.None);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            yield return null;
            float startClock = Hud.Clock;
            Assert.That(Hud.RowsShown, Is.Zero, "no orders yet");

            CustomerAgent customer = Director.SpawnCustomer();
            yield return Fast(WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood, 20f, "an order"));
            yield return null;
            Assert.That(Hud.RowsShown, Is.EqualTo(1), "the order is on the rail");
            RailRow row = Hud.Rows[0];
            Assert.That(row.icon.sprite, Is.SameAs(kebab.icon));
            Assert.That(row.state.GetComponent<SuperTextMesh>().text, Is.EqualTo("waiting"));
            Assert.That(row.patience.gameObject.activeSelf, "with the customer's patience");
            Assert.That(Hud.Clock, Is.LessThan(startClock), "the clock runs down");

            // Cook it, serve it, let them eat and pay: the takings go up.
            Ticket ticket = Director.Session.Tickets.Single();
            Director.Session.StartCooking(ticket, this);
            yield return null;
            Assert.That(row.state.GetComponent<SuperTextMesh>().text, Is.EqualTo("cooking"));
            Director.Session.FinishCooking(ticket, 1f);
            Director.Session.StartDelivery(ticket, this);
            Director.Session.Deliver(ticket, customer.Logic, 1f);
            Time.timeScale = 6f;
            yield return WaitUntil(() => Director.Session.Ledger.Gold > 0, 30f, "them to pay");
            Time.timeScale = 1f;
            yield return null;
            Assert.That(Hud.RowsShown, Is.Zero, "served orders leave the rail");
            Assert.That(Object.FindObjectsByType<SuperTextMesh>().Any(t => t.name == "Gold" && t.text == $"{Director.Session.Ledger.Gold}"), "the takings show");
        }

        [UnityTest]
        public IEnumerator SellingOut_ClosesEarly_AndResultsTellTheEvening_ThenAnotherEveningBegins()
        {
            yield return LoadPrep();
            RecipeDefinition kebab = Director.Content.recipes.First(r => r.id == "cellar_kebab");
            // Leave stock for exactly one kebab.
            while (PrepRules.Makeable(kebab, Director.Storeroom) > 1) RecipeMatcher.TryTake(kebab, Director.Storeroom);
            Director.SetMenu(new[] { kebab });
            Director.AssignStaff(StaffStation.None);
            Director.OpenService();
            Director.ArrivalsPaused = true;

            CustomerAgent customer = Director.SpawnCustomer();
            yield return Fast(WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood, 20f, "the one order"));
            Assert.That(Director.Session.AllSoldOut, "that was the last kebab");
            Assert.That(Director.Session.CanAdmitCustomer, Is.False, "the door closes to new customers");
            yield return null;
            Assert.That(Hud.IsShown);
            Ticket ticket = Director.Session.Tickets.Single();
            Director.Session.StartCooking(ticket, this);
            Director.Session.FinishCooking(ticket, 1f);
            Director.Session.StartDelivery(ticket, this);
            Director.Session.Deliver(ticket, customer.Logic, 1f);
            Time.timeScale = 6f;
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 30f, "service to end once they've paid");
            Time.timeScale = 1f;
            yield return null;
            Assert.That(Director.Session.ClosedEarly, "closed early: everything sold");
            Assert.That(Results.IsShown);
            Assert.That(Hud.IsShown, Is.False, "the HUD goes with the service");
            Assert.That(InputMaps.Find(InputMaps.Tavern, TavernActions.Move).enabled, Is.False);

            yield return WaitUntil(() => !Results.IsRevealing, 10f, "the lines to be revealed");
            var texts = Object.FindObjectsByType<SuperTextMesh>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();
            Assert.That(texts, Has.Some.EqualTo("everything sold out, so we closed early."));
            Assert.That(texts, Has.Some.EqualTo("dishes served").And.Some.EqualTo("1"), "a label and its amount");
            Assert.That(texts, Has.Some.EqualTo("takings"));
            Assert.That(texts, Has.None.StartWith("walkouts"), "no trouble line when there was none");

            Results.DoneButton.onClick.Invoke();
            yield return WaitUntil(() => Director != null && Director.Phase == TavernPhase.Prep && Prep != null && Prep.IsShown, 5f, "another evening's prep");
        }

        [UnityTest]
        public IEnumerator ClosingForTheNight_WithoutOpening_SaysSo()
        {
            yield return LoadPrep();
            Director.Storeroom.Clear();
            Director.SetMenu(new List<RecipeDefinition>());
            yield return null;
            Assert.That(Prep.OpenButton.interactable, Is.False, "nothing to cook");
            Prep.CloseButton.onClick.Invoke();
            yield return null;
            Assert.That(Director.Phase, Is.EqualTo(TavernPhase.Results));
            Assert.That(Director.Report.StayedShut);
            yield return WaitUntil(() => !Results.IsRevealing, 5f, "the screen to settle");
            var texts = Object.FindObjectsByType<SuperTextMesh>().Where(t => t.gameObject.activeInHierarchy).Select(t => t.text).ToList();
            Assert.That(texts, Has.Some.EqualTo("we kept the doors shut tonight."));
            Assert.That(texts, Has.None.StartWith("takings"));
        }

        /// <summary>The HUD keeps to the side margins and nothing on it overlaps.</summary>
        [UnityTest]
        public IEnumerator TheHud_KeepsToTheMargins_WithoutOverlaps()
        {
            yield return LoadPrep();
            Director.OpenDebugEvening();
            yield return null;
            Canvas canvas = Object.FindObjectsByType<Canvas>().First(c => c.name == "UI");
            Transform content = canvas.transform.Find("TavernHud/Content");
            Rect Bounds(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            }
            string[] parts = { "Clock", "LastOrders", "Gold", "Tips", "Renown", "Menu1", "Orders", "Order1", "Order8" };
            var rects = parts.ToDictionary(p => p, p => Bounds((RectTransform)content.Find(p)));
            for (int i = 0; i < parts.Length; i++)
            for (int j = i + 1; j < parts.Length; j++)
                Assert.That(rects[parts[i]].Overlaps(rects[parts[j]]), Is.False, $"{parts[i]} overlaps {parts[j]}");
            Rect screen = Bounds((RectTransform)canvas.transform);
            foreach (var (name, r) in rects) Assert.That(screen.Contains(r.min) && screen.Contains(r.max), $"{name} is on screen");
            // At 16:9 the margins are 48 px; the HUD columns are 40 px wide plus a 4 px gutter.
            Assert.That(((RectTransform)content.Find("Order1")).sizeDelta.x + 4f, Is.LessThanOrEqualTo(48f));
        }
    }
}
