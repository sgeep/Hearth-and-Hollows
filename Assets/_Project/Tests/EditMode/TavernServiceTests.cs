using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class PreferenceTests
    {
        static CustomerTraits Dwarf => new()
        {
            liked = FlavorTags.Savory | FlavorTags.Earthy,
            disliked = FlavorTags.Sweet,
            hasFavoriteStation = true,
            favoriteStation = CookStation.Tap,
            favoriteStationBonus = 0.2f,
        };

        [Test]
        public void NeutralDish_IsHalf()
        {
            Assert.That(Preferences.FlavorMatch(FlavorTags.Umami, CookStation.Grill, Dwarf, ServiceEconomySettings.Default), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void LikedFlavors_RaiseMatch_DislikedLowerIt()
        {
            Assert.That(Preferences.FlavorMatch(FlavorTags.Savory | FlavorTags.Earthy, CookStation.Grill, Dwarf, ServiceEconomySettings.Default), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(Preferences.FlavorMatch(FlavorTags.Sweet, CookStation.Grill, Dwarf, ServiceEconomySettings.Default), Is.EqualTo(0.2f).Within(1e-5f));
        }

        [Test]
        public void FavoriteStation_AddsBonus()
        {
            Assert.That(Preferences.FlavorMatch(FlavorTags.Umami, CookStation.Tap, Dwarf, ServiceEconomySettings.Default), Is.EqualTo(0.7f).Within(1e-5f));
        }
    }

    public class ServiceSessionTests : KitchenFixture
    {
        const float Dt = 1f / 60f;
        RecipeDefinition m_Grilled, m_Gelbrew;

        [SetUp]
        public void CreateRecipes()
        {
            m_Grilled = Recipe("grilled_haunch", 12, FlavorTags.Savory, Needs(Haunch));
            m_Gelbrew = Recipe("gelbrew", 8, FlavorTags.Sweet, Needs(Gel, 2));
            m_Gelbrew.station = CookStation.Tap;
        }

        static CustomerTraits Quick(FlavorTags liked = FlavorTags.None) => new()
        {
            walkSpeed = 3f, seatPatience = 10f, orderPatience = 30f, orderDelay = 0.1f, eatSeconds = 1f,
            generosity = 1f, liked = liked,
        };

        ServiceSession Session(Storeroom stock, int seats = 4, params RecipeDefinition[] menu) =>
            new(ServiceSettings.Default, DishScoringSettings.Default, ServiceEconomySettings.Default,
                stock, menu.Length > 0 ? menu : new[] { m_Grilled, m_Gelbrew }, seats, new SeededRandom(7));

        static void Run(ServiceSession s, float seconds)
        {
            for (float t = 0; t < seconds; t += Dt) s.Tick(Dt);
        }

        /// <summary>Admit a customer and walk them to their seat so they order.</summary>
        static CustomerLogic Seat(ServiceSession s, CustomerTraits traits)
        {
            var c = new CustomerLogic(traits);
            s.AdmitCustomer(c);
            c.ArrivedAtSeat();
            Run(s, 0.2f);
            return c;
        }

        /// <summary>Cook and carry out the customer's own ticket.</summary>
        static Ticket ServeTheirOrder(ServiceSession s, CustomerLogic c, float score = 1f)
        {
            var t = s.Tickets.First(x => x.Customer == c);
            s.StartCooking(t, "cook");
            s.FinishCooking(t, score);
            s.StartDelivery(t, "carrier");
            Assert.That(s.Deliver(t, c, score));
            return t;
        }

        // ---------- Menu ----------

        [Test]
        public void Menu_MustHaveOneToThreeDishes()
        {
            var stock = new Storeroom();
            Assert.Throws<ArgumentException>(() => new ServiceSession(ServiceSettings.Default, DishScoringSettings.Default,
                ServiceEconomySettings.Default, stock, Array.Empty<RecipeDefinition>(), 4, new SeededRandom(1)));
            var r = new[] { m_Grilled, m_Gelbrew, Recipe("a", 1, FlavorTags.None, Needs(Cap)), Recipe("b", 1, FlavorTags.None, Needs(Core)) };
            Assert.Throws<ArgumentException>(() => Session(stock, 4, r));
        }

        // ---------- Ordering & sold out ----------

        [Test]
        public void Ordering_ReservesIngredients_AndCreatesTicket()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 4, m_Grilled);
            var c = Seat(s, Quick());
            Assert.That(c.State, Is.EqualTo(CustomerState.WaitingForFood));
            Assert.That(s.Tickets, Has.Count.EqualTo(1));
            Assert.That(stock.TotalCount, Is.EqualTo(1), "one haunch reserved");
        }

        /// <summary>
        /// 4i-C playtest: Orik waits by the pass while a plate is coming (ordered, cooking or ready) and tidies only when none is,
        /// and a table someone has sat at again isn't one to tidy.
        /// </summary>
        [Test]
        public void PlatesComing_AndSeatTaken_KeepTheServerByThePass()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 4, m_Grilled);
            Assert.That(s.PlatesComing, Is.False, "an empty room: free to tidy");
            var c = Seat(s, Quick());
            Assert.That(s.PlatesComing, "ordered: a plate is coming");
            Assert.That(s.SeatTaken(c.Seat), "their seat is taken");
            var t = s.Tickets.First(x => x.Customer == c);
            s.StartCooking(t, "cook");
            Assert.That(s.PlatesComing, "cooking");
            s.FinishCooking(t, 1f);
            Assert.That(s.PlatesComing, "on the pass");
            s.StartDelivery(t, "keeper");
            Assert.That(s.PlatesComing, Is.False, "the keeper has it: nothing left coming");
            Assert.That(s.Deliver(t, c, 1f));
            Run(s, 3f);
            Assert.That(s.SeatTaken(c.Seat), Is.False, "eaten, paid and gone: the table is free to tidy");
        }

        [Test]
        public void LastServingReserved_MarksDishSoldOut_AndRaisesEvent()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            stock.Add(Stack(Gel, 4));
            var s = Session(stock, 4, m_Grilled, m_Gelbrew);
            int changes = 0;
            s.SoldOutChanged += () => changes++;
            Seat(s, Quick(liked: FlavorTags.Savory | FlavorTags.Umami));
            while (!s.IsSoldOut(m_Grilled) && s.Customers.Count < 8) Seat(s, Quick(liked: FlavorTags.Savory));
            Assert.That(s.IsSoldOut(m_Grilled));
            Assert.That(changes, Is.GreaterThanOrEqualTo(1));
            Assert.That(s.AvailableDishes(), Has.No.Member(m_Grilled));
        }

        [Test]
        public void WhenOneDishIsSoldOut_NewCustomersOrderSomethingElse()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Gel, 6)); // no haunch at all: Grilled Haunch starts sold out
            var s = Session(stock, 4, m_Grilled, m_Gelbrew);
            Assert.That(s.IsSoldOut(m_Grilled));
            var c = Seat(s, Quick(liked: FlavorTags.Savory)); // would prefer the haunch
            Assert.That(c.Order, Is.SameAs(m_Gelbrew));
        }

        [Test]
        public void WhenEverythingIsSoldOut_CustomerLeaves_WithSmallerPenaltyThanWalkout()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            Seat(s, Quick()); // takes the last serving; their order keeps service open
            var c = Seat(s, Quick());
            Assert.That(c.State, Is.EqualTo(CustomerState.Leaving));
            Assert.That(c.Departure, Is.EqualTo(Departure.SoldOut));
            Assert.That(s.Ledger.SoldOutLeaves, Is.EqualTo(1));
            Assert.That(s.Ledger.Renown, Is.EqualTo(ServiceEconomySettings.Default.soldOutRenown));
            Assert.That(s.Ledger.Renown, Is.GreaterThan(ServiceEconomySettings.Default.walkoutRenown));
            Assert.That(s.Ledger.Walkouts, Is.EqualTo(0));
        }

        [Test]
        public void Walkout_BeforeCooking_ReturnsReservedIngredients_ButDishStaysSoldOut()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            var c = Seat(s, Quick());
            Assert.That(s.IsSoldOut(m_Grilled));
            Run(s, 31f); // order patience 30 s
            Assert.That(c.Departure, Is.EqualTo(Departure.WalkedOut));
            Assert.That(s.Ledger.Walkouts, Is.EqualTo(1));
            Assert.That(s.Ledger.Renown, Is.EqualTo(ServiceEconomySettings.Default.walkoutRenown));
            Assert.That(stock.TotalCount, Is.EqualTo(1), "uncooked ingredients go back");
            Assert.That(s.IsSoldOut(m_Grilled), "sold out is final for the night");
            Assert.That(s.AvailableDishes(), Is.Empty);
        }

        [Test]
        public void Walkout_AfterCooking_LeavesASpareDish()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            Seat(s, Quick());
            var t = s.NextToCook(CookStation.Grill);
            s.StartCooking(t, this);
            s.FinishCooking(t, 1f);
            Run(s, 31f);
            Assert.That(stock.TotalCount, Is.EqualTo(0));
            Assert.That(t.State, Is.EqualTo(TicketState.Ready));
            Assert.That(t.IsSpare);
            Assert.That(s.Tickets, Has.Member(t));
        }

        // ---------- Full loop ----------

        [Test]
        public void FullLoop_OrderCookServeEatPay_FillsLedger()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1, Quality.Fine, fresh: 1f));
            var s = Session(stock, 4, m_Grilled);
            var c = Seat(s, Quick(liked: FlavorTags.Savory));

            var t = s.NextToCook(CookStation.Grill);
            Assert.That(s.StartCooking(t, this));
            s.FinishCooking(t, 1f);
            Assert.That(s.NextToServe(), Is.SameAs(t));
            Assert.That(s.StartDelivery(t, this));
            Assert.That(s.Deliver(t, c, 1f));
            Assert.That(c.State, Is.EqualTo(CustomerState.Eating));

            Run(s, 1.5f);
            Assert.That(c.Departure, Is.EqualTo(Departure.Paid));
            Assert.That(s.Ledger.DishesServed, Is.EqualTo(1));
            Assert.That(s.Ledger.Gold, Is.EqualTo(12), "perfect Fine fresh dish pays full recipe value");
            Assert.That(s.Ledger.Tips, Is.GreaterThan(0));
            Assert.That(s.Ledger.Renown, Is.GreaterThan(0));
            Assert.That(s.Tickets, Is.Empty);
        }

        [Test]
        public void WorseDish_PaysLess()
        {
            int Pay(Quality q, float fresh, float cook)
            {
                var stock = new Storeroom();
                stock.Add(Stack(Haunch, 1, q, fresh));
                var s = Session(stock, 4, m_Grilled);
                var c = Seat(s, Quick());
                var t = s.NextToCook(CookStation.Grill);
                s.StartCooking(t, this);
                s.FinishCooking(t, cook);
                s.StartDelivery(t, this);
                s.Deliver(t, c, 1f);
                Run(s, 1.5f);
                return s.Ledger.Gold;
            }
            Assert.That(Pay(Quality.Poor, 0.3f, 0.6f), Is.LessThan(Pay(Quality.Fine, 1f, 1f)));
        }

        [Test]
        public void ClaimedTicket_CannotBeTakenTwice()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            Seat(s, Quick());
            var t = s.NextToCook(CookStation.Grill);
            Assert.That(s.StartCooking(t, "player"), Is.True);
            Assert.That(s.StartCooking(t, "staff"), Is.False);
            Assert.That(s.NextToCook(CookStation.Grill), Is.Null);
        }

        [Test]
        public void DroppedPlate_RequeuesIfStockAllows_ElseCustomerLeavesSoldOut()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 4, m_Grilled);
            var c = Seat(s, Quick());
            var t = s.NextToCook(CookStation.Grill);
            s.StartCooking(t, this);
            s.FinishCooking(t, 1f);
            s.StartDelivery(t, this);
            s.Dropped(t);
            Assert.That(t.State, Is.EqualTo(TicketState.Queued), "re-made from the second haunch");
            Assert.That(stock.TotalCount, Is.EqualTo(0));

            s.StartCooking(t, this);
            s.FinishCooking(t, 1f);
            s.StartDelivery(t, this);
            s.Dropped(t);
            Assert.That(c.Departure, Is.EqualTo(Departure.SoldOut));
            Assert.That(s.Ledger.DroppedDishes, Is.EqualTo(2));
        }

        // ---------- Delivering by hand & spare dishes ----------

        [Test]
        public void Deliver_ToAnotherCustomerWithTheSameOrder_PassesTheirOrderToTheFirst()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 4, m_Grilled);
            var a = Seat(s, Quick());
            var b = Seat(s, Quick());
            var forA = s.Tickets.First(t => t.Customer == a);
            var forB = s.Tickets.First(t => t.Customer == b);
            s.StartCooking(forA, this);
            s.FinishCooking(forA, 1f);
            s.StartDelivery(forA, this);

            Assert.That(s.Deliver(forA, b, 1f), Is.True);
            Assert.That(b.State, Is.EqualTo(CustomerState.Eating));
            Assert.That(forA.Customer, Is.SameAs(b));
            Assert.That(a.State, Is.EqualTo(CustomerState.WaitingForFood), "A still gets a dish");
            Assert.That(forB.Customer, Is.SameAs(a), "B's order now belongs to A");
            Assert.That(forB.State, Is.EqualTo(TicketState.Queued));
        }

        [Test]
        public void Deliver_ToSomeoneWhoOrderedADifferentDish_IsRefused()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            stock.Add(Stack(Gel, 2));
            var s = Session(stock, 4, m_Grilled, m_Gelbrew);
            Seat(s, Quick());
            Seat(s, Quick()); // the first took one dish's only serving, so these two ordered different dishes
            var grilled = s.Tickets.First(t => t.Recipe == m_Grilled);
            var gelbrew = s.Tickets.First(t => t.Recipe == m_Gelbrew);
            s.StartCooking(grilled, this);
            s.FinishCooking(grilled, 1f);
            s.StartDelivery(grilled, this);

            Assert.That(s.CanDeliver(grilled, gelbrew.Customer), Is.False);
            Assert.That(s.Deliver(grilled, gelbrew.Customer, 1f), Is.False);
            Assert.That(grilled.State, Is.EqualTo(TicketState.Delivering), "still in hand");
            Assert.That(s.Deliver(grilled, grilled.Customer, 1f), Is.True);
        }

        [Test]
        public void PutBack_ReturnsThePlateToThePass()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            Seat(s, Quick());
            var t = s.NextToCook(CookStation.Grill);
            s.StartCooking(t, this);
            s.FinishCooking(t, 1f);
            s.StartDelivery(t, this);
            s.PutBack(t);
            Assert.That(t.State, Is.EqualTo(TicketState.Ready));
            Assert.That(s.NextToServe(), Is.SameAs(t));
        }

        [Test]
        public void Spare_GoesToTheNextCustomerWhoOrdersIt_WithoutUsingMoreStock()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 4, m_Grilled);
            Seat(s, Quick());
            var t = s.NextToCook(CookStation.Grill);
            s.StartCooking(t, this);
            s.FinishCooking(t, 1f);
            Run(s, 31f); // they walk out; the dish is left on the pass
            Assert.That(t.IsSpare);
            Assert.That(stock.TotalCount, Is.EqualTo(1));

            var next = Seat(s, Quick());
            Assert.That(next.Order, Is.SameAs(m_Grilled));
            Assert.That(t.Customer, Is.SameAs(next));
            Assert.That(stock.TotalCount, Is.EqualTo(1), "the spare was used, not the storeroom");
            Assert.That(s.Tickets, Has.Count.EqualTo(1));
        }

        [Test]
        public void Spare_HandDeliveredToSomeoneWaiting_CancelsTheirOrder_AndReturnsItsStock()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 4, m_Grilled);
            var impatient = Quick();
            impatient.orderPatience = 5f;
            var a = Seat(s, impatient);
            var b = Seat(s, Quick());
            var forA = s.Tickets.First(t => t.Customer == a);
            var forB = s.Tickets.First(t => t.Customer == b);
            s.StartCooking(forA, this);
            s.FinishCooking(forA, 1f);
            Run(s, 6f); // A walks out
            Assert.That(forA.IsSpare);
            Assert.That(stock.TotalCount, Is.EqualTo(0));

            Assert.That(s.NextToServe(includeSpares: false), Is.Null, "staff don't carry spares");
            Assert.That(s.NextToServe(), Is.SameAs(forA));
            s.StartDelivery(forA, this);
            Assert.That(s.Deliver(forA, b, 1f), Is.True);
            Assert.That(b.State, Is.EqualTo(CustomerState.Eating));
            Assert.That(forB.State, Is.EqualTo(TicketState.Cancelled));
            Assert.That(stock.TotalCount, Is.EqualTo(1), "B's reserved haunch goes back");
        }

        [Test]
        public void CarriedPlate_WhoseCustomerLeaves_StaysInHand_AsASpare()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            Seat(s, Quick());
            var t = s.NextToCook(CookStation.Grill);
            s.StartCooking(t, this);
            s.FinishCooking(t, 1f);
            s.StartDelivery(t, this);
            Run(s, 31f);
            Assert.That(t.State, Is.EqualTo(TicketState.Delivering));
            Assert.That(t.IsSpare);
            s.Dropped(t);
            Assert.That(t.State, Is.EqualTo(TicketState.Cancelled), "a dropped spare is simply gone");
            Assert.That(s.Ledger.DroppedDishes, Is.EqualTo(1));
        }

        // ---------- Selling out closes early ----------

        [Test]
        public void AllSoldOut_ClosesTheDoor_ThenEndsServiceOnceTheLastDinerPays()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = Session(stock, 4, m_Grilled);
            bool ended = false;
            s.Ended += () => ended = true;
            var c = Seat(s, Quick());
            Assert.That(s.AllSoldOut);
            Assert.That(s.CanAdmitCustomer, Is.False, "no one new comes in");
            Run(s, 2f);
            Assert.That(s.IsOver, Is.False, "the open order can still be cooked and served");

            ServeTheirOrder(s, c);
            Run(s, 0.5f);
            Assert.That(s.IsOver, Is.False, "still eating");
            Run(s, 1f);
            Assert.That(c.Departure, Is.EqualTo(Departure.Paid));
            Assert.That(ended);
            Assert.That(s.ClosedEarly);
            Assert.That(s.Ledger.DishesServed, Is.EqualTo(1));
        }

        [Test]
        public void NotAllSoldOut_KeepsServiceOpen()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            stock.Add(Stack(Gel, 2));
            var s = Session(stock, 4, m_Grilled, m_Gelbrew);
            Seat(s, Quick());
            Assert.That(s.AllSoldOut, Is.False);
            Assert.That(s.CanAdmitCustomer);
            Run(s, 40f); // the order walks out; the other dish is still on
            Assert.That(s.IsOver, Is.False);
        }

        [Test]
        public void ClosingTime_WhileEating_TheDinerStillPays()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 3));
            var settings = ServiceSettings.Default;
            settings.lengthSeconds = 20f;
            var s = new ServiceSession(settings, DishScoringSettings.Default, ServiceEconomySettings.Default,
                stock, new[] { m_Grilled }, 4, new SeededRandom(1));
            var slowEater = Quick();
            slowEater.eatSeconds = 100f;
            var c = Seat(s, slowEater);
            ServeTheirOrder(s, c);
            Run(s, 21f);
            Assert.That(s.IsOver);
            Assert.That(s.ClosedEarly, Is.False);
            Assert.That(c.Departure, Is.EqualTo(Departure.Paid));
            Assert.That(s.Ledger.DishesServed, Is.EqualTo(1));
            Assert.That(s.Ledger.Gold, Is.GreaterThan(0));
        }

        // ---------- Seating & clock ----------

        [Test]
        public void NoFreeSeat_Queues_ThenSitsWhenSomeoneLeaves()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 1, m_Grilled);
            var first = new CustomerLogic(Quick());
            var second = new CustomerLogic(Quick());
            Assert.That(s.AdmitCustomer(first), Is.EqualTo(0));
            Assert.That(s.AdmitCustomer(second), Is.EqualTo(-1));
            Assert.That(second.State, Is.EqualTo(CustomerState.Queueing));

            first.ArrivedAtSeat();
            Run(s, 0.2f);
            ServeTheirOrder(s, first);
            Run(s, 1.5f); // first eats, pays and leaves
            Assert.That(second.State, Is.EqualTo(CustomerState.WalkingToSeat));
            Assert.That(second.Seat, Is.EqualTo(0));
        }

        [Test]
        public void QueuePatience_RunsOut_CountsAsWalkout()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 2));
            var s = Session(stock, 1, m_Grilled);
            s.AdmitCustomer(new CustomerLogic(Quick()));
            var waiting = new CustomerLogic(Quick());
            s.AdmitCustomer(waiting);
            Run(s, 11f);
            Assert.That(waiting.Departure, Is.EqualTo(Departure.WalkedOut));
        }

        [Test]
        public void Clock_EndsService_SendsEveryoneHome_WithoutPenalty()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var settings = ServiceSettings.Default;
            settings.lengthSeconds = 20f;
            var s = new ServiceSession(settings, DishScoringSettings.Default, ServiceEconomySettings.Default,
                stock, new[] { m_Grilled }, 4, new SeededRandom(1));
            bool ended = false;
            s.Ended += () => ended = true;
            var c = Seat(s, Quick());
            Run(s, 21f);
            Assert.That(ended);
            Assert.That(s.IsOver);
            Assert.That(c.Departure, Is.EqualTo(Departure.ClosingTime));
            Assert.That(s.Ledger.Renown, Is.EqualTo(0));
            Assert.That(stock.TotalCount, Is.EqualTo(1), "reserved but uncooked stock returns at closing");
        }

        [Test]
        public void LastOrders_StopsAdmissions()
        {
            var settings = ServiceSettings.Default;
            settings.lengthSeconds = 60f;
            settings.lastOrdersSeconds = 10f;
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            var s = new ServiceSession(settings, DishScoringSettings.Default, ServiceEconomySettings.Default,
                stock, new[] { m_Grilled }, 4, new SeededRandom(1));
            Assert.That(s.CanAdmitCustomer);
            Run(s, 51f);
            Assert.That(s.IsLastOrders);
            Assert.That(s.CanAdmitCustomer, Is.False);
        }

        // ---------- Staff ----------

        [Test]
        public void StaffCook_AutoResolvesTickets_ThroughIMinigame_AtReducedQuality()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 3));
            var s = Session(stock, 4, m_Grilled);
            var factory = new MinigameFactory(GrillSettings.Default, TapSettings.Default, ServingSettings.Default);
            var staff = new StaffCook(s, factory, CookStation.Grill, skill: 0.6f, qualityCap: 0.85f, restBetweenJobs: 0.5f, new SeededRandom(3));
            Seat(s, Quick());
            Seat(s, Quick());

            for (float t = 0; t < 30f && s.Tickets.Any(x => x.State != TicketState.Ready); t += Dt)
            {
                staff.Tick(Dt);
                s.Tick(Dt);
            }

            Assert.That(s.Tickets, Has.Count.EqualTo(2));
            Assert.That(s.Tickets.All(x => x.State == TicketState.Ready), "staff cooked both without any player input");
            Assert.That(s.Tickets.All(x => x.CookScore > 0f && x.CookScore <= 0.85f));
        }

        // ---------- Arrivals & debug fill ----------

        [Test]
        public void ArrivalSchedule_OnlySpawnsWhenAdmitting()
        {
            var settings = ServiceSettings.Default;
            var schedule = new ArrivalSchedule(settings, new List<CustomerProfile>(), new SeededRandom(1), firstArrival: 0.5f);
            Assert.That(schedule.Tick(1f, canAdmit: false), Is.Null);
        }

        [Test]
        public void DebugFill_GivesMixedQualityAndFreshness()
        {
            var stock = new Storeroom();
            DebugStockFiller.Fill(stock, new[] { Haunch, Gel, Cap }, new SeededRandom(5));
            Assert.That(stock.TotalCount, Is.GreaterThan(0));
            Assert.That(stock.Stacks.Select(x => x.Item.Quality).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(stock.Stacks.All(x => x.Freshness >= DebugStockFiller.MinFreshness && x.Freshness <= 1f));
            Assert.That(stock.Stacks.Select(x => x.Freshness).Distinct().Count(), Is.GreaterThan(1));
        }
    }

    public class CustomerLogicTests
    {
        static CustomerTraits Traits => new() { seatPatience = 5f, orderPatience = 10f, orderDelay = 1f, eatSeconds = 2f, generosity = 1f };

        [Test]
        public void Lifecycle_SeatOrderWaitEatPay()
        {
            var c = new CustomerLogic(Traits);
            RecipeDefinition requested = null;
            c.OrderRequested += x => requested = ScriptableObjectStub();
            c.ArrivedInside(0);
            Assert.That(c.State, Is.EqualTo(CustomerState.WalkingToSeat));
            c.ArrivedAtSeat();
            c.Tick(1.1f);
            Assert.That(requested, Is.Not.Null, "asks for an order after reading the menu");
            c.PlaceOrder(requested);
            Assert.That(c.State, Is.EqualTo(CustomerState.WaitingForFood));
            c.Tick(4f);
            c.Serve(0.9f);
            Assert.That(c.WaitFraction, Is.EqualTo(0.4f).Within(1e-4f));
            c.Tick(2.1f);
            Assert.That(c.Departure, Is.EqualTo(Departure.Paid));
            UnityEngine.Object.DestroyImmediate(requested);
        }

        [Test]
        public void Patience_Drains_ThenWalksOut()
        {
            var c = new CustomerLogic(Traits);
            c.ArrivedInside(0);
            c.ArrivedAtSeat();
            c.Tick(1.1f);
            var r = ScriptableObjectStub();
            c.PlaceOrder(r);
            c.Tick(5f);
            Assert.That(c.Patience, Is.EqualTo(0.5f).Within(1e-3f));
            c.Tick(5.1f);
            Assert.That(c.Departure, Is.EqualTo(Departure.WalkedOut));
            UnityEngine.Object.DestroyImmediate(r);
        }

        [Test]
        public void Departed_FiresOnce()
        {
            var c = new CustomerLogic(Traits);
            int n = 0;
            c.Departed += _ => n++;
            c.ArrivedInside(-1);
            c.Tick(6f);
            c.CloseService();
            Assert.That(n, Is.EqualTo(1));
        }

        static RecipeDefinition ScriptableObjectStub() => UnityEngine.ScriptableObject.CreateInstance<RecipeDefinition>();
    }
}
