using System.Linq;
using Hearthdelve.Core.Minigames;
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
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class ChopMinigameTests
    {
        const float Dt = 1f / 60f;
        static readonly ChopSettings S = ChopSettings.Default;

        static MinigameInput PointAt(float x, bool press = false) =>
            new() { PointerActive = true, Pointer = x, ActionPressed = press, ActionHeld = press };

        /// <summary>Waits out the pause between ingredients.</summary>
        static void SkipPause(ChopMinigame g)
        {
            while (g.IsPausing) g.Tick(Dt, default);
        }

        [Test]
        public void Lines_StayWithinCountAndMargins()
        {
            var g = new ChopMinigame(S, 6, new SeededRandom(3));
            for (int i = 0; i < g.ItemCount; i++)
            {
                var lines = g.LinesOf(i);
                Assert.That(lines.Length, Is.InRange(S.minLines, S.maxLines));
                Assert.That(lines.All(x => x >= S.edgeMargin && x <= 1f - S.edgeMargin));
                for (int l = 1; l < lines.Length; l++) Assert.That(lines[l], Is.GreaterThan(lines[l - 1]), "left to right");
            }
        }

        [Test]
        public void CuttingExactlyOnEveryLine_ScoresOne()
        {
            var g = new ChopMinigame(S, 2, new SeededRandom(1));
            g.Begin();
            for (int item = 0; item < g.ItemCount; item++)
            {
                SkipPause(g);
                foreach (float x in g.LinesOf(item)) g.Tick(Dt, PointAt(x, press: true));
            }
            Assert.That(g.IsComplete);
            Assert.That(g.Evaluate(), Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void CutScore_FallsOffWithDistance()
        {
            Assert.That(ChopMinigame.ScoreCut(0f, S), Is.EqualTo(1f));
            Assert.That(ChopMinigame.ScoreCut(S.perfectDistance, S), Is.EqualTo(1f));
            Assert.That(ChopMinigame.ScoreCut(S.perfectDistance + S.falloff * 0.5f, S), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(ChopMinigame.ScoreCut(S.perfectDistance + S.falloff, S), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void EachCut_TakesTheNearestUncutLine()
        {
            var g = new ChopMinigame(S, 1, new SeededRandom(2));
            g.Begin();
            float first = g.LinesOf(0)[0];
            g.Tick(Dt, PointAt(first, press: true));
            g.Tick(Dt, PointAt(first, press: true)); // same spot again: counts for the next line, badly
            Assert.That(g.IsCut(0, 0) && g.IsCut(0, 1));
            Assert.That(g.CutScore(0, 0), Is.EqualTo(1f));
            Assert.That(g.CutScore(0, 1), Is.LessThan(1f));
        }

        [Test]
        public void RunningOutOfTime_LeavesUncutLinesAtZero()
        {
            var g = new ChopMinigame(S, 1, new SeededRandom(4));
            g.Begin();
            int lines = g.LinesOf(0).Length;
            g.Tick(Dt, PointAt(g.LinesOf(0)[0], press: true));
            while (!g.IsComplete) g.Tick(Dt, default);
            Assert.That(g.Elapsed, Is.EqualTo(S.itemTimeLimit).Within(0.05f));
            Assert.That(g.Evaluate(), Is.EqualTo(1f / lines).Within(1e-4f));
        }

        [Test]
        public void Stick_SlidesTheKnife_MouseSetsItDirectly()
        {
            var g = new ChopMinigame(S, 1, new SeededRandom(5));
            g.Begin();
            Assert.That(g.Knife, Is.EqualTo(0.5f));
            for (int i = 0; i < 12; i++) g.Tick(Dt, new MinigameInput { Aim = Vector2.right }); // 0.2 s
            Assert.That(g.Knife, Is.EqualTo(0.5f + S.knifeSpeed * 0.2f).Within(0.01f));
            g.Tick(Dt, PointAt(0.1f));
            Assert.That(g.Knife, Is.EqualTo(0.1f));
        }

        [Test]
        public void DefaultTuning_TwoIngredients_SteadyPlayer_TakesFiveToTenSeconds()
        {
            // A steady player: one accurate cut every 0.8 s.
            var g = new ChopMinigame(S, 2, new SeededRandom(6));
            g.Begin();
            while (!g.IsComplete)
            {
                SkipPause(g);
                if (g.IsComplete) break;
                int item = g.Item;
                int next = Enumerable.Range(0, g.LinesOf(item).Length).First(i => !g.IsCut(item, i));
                float x = g.LinesOf(item)[next];
                for (float t = 0f; t < 0.8f && !g.IsComplete; t += Dt) g.Tick(Dt, PointAt(x));
                g.Tick(Dt, PointAt(x, press: true));
            }
            Assert.That(g.Elapsed, Is.InRange(5f, 10f));
            Assert.That(g.Evaluate(), Is.EqualTo(1f).Within(1e-4f));
        }

        static float Auto(float skill, int seed)
        {
            var g = new ChopMinigame(S, 2, new SeededRandom(seed));
            return MinigameRunner.RunToCompletion(g, new ChopAutoPlayer(g, skill, new SeededRandom(seed + 100)));
        }

        [Test]
        public void AutoPlayer_LowerSkill_ScoresLower_StaffStillUsable()
        {
            float expert = 0f, staff = 0f, novice = 0f;
            for (int seed = 1; seed <= 30; seed++)
            {
                expert += Auto(1f, seed) / 30f;
                staff += Auto(0.6f, seed) / 30f;
                novice += Auto(0.1f, seed) / 30f;
            }
            Assert.That(expert, Is.GreaterThan(0.95f));
            Assert.That(staff, Is.LessThan(expert));
            Assert.That(novice, Is.LessThan(staff));
            Assert.That(staff, Is.InRange(0.4f, 0.97f));
        }

        [Test]
        public void Factory_HasAChopAutoPlayer()
        {
            var f = new MinigameFactory(GrillSettings.Default, TapSettings.Default, ServingSettings.Default, S);
            var chop = f.CreateChop(2, new SeededRandom(1));
            Assert.That(chop.ItemCount, Is.EqualTo(2));
            Assert.That(MinigameFactory.CreateAutoPlayer(chop, 0.5f, new SeededRandom(1)), Is.InstanceOf<ChopAutoPlayer>());
        }
    }

    public class StewPotTests : KitchenFixture
    {
        const float Dt = 1f / 60f;
        RecipeDefinition m_Stew, m_Pottage, m_Grilled;

        [SetUp]
        public void CreateRecipes()
        {
            m_Stew = Recipe("cellar_stew", 9, FlavorTags.Savory, Needs(Haunch), Needs(Cap));
            m_Stew.station = CookStation.StewPot;
            m_Pottage = Recipe("offal_pottage", 8, FlavorTags.Earthy, Needs(Liver), Needs(Cap));
            m_Pottage.station = CookStation.StewPot;
            m_Grilled = Recipe("grilled_haunch", 12, FlavorTags.Savory, Needs(Haunch));
        }

        static CustomerTraits Quick() => new()
        {
            walkSpeed = 3f, seatPatience = 10f, orderPatience = 60f, orderDelay = 0.1f, eatSeconds = 1f, generosity = 1f,
        };

        static ServiceSession Session(Storeroom stock, params RecipeDefinition[] menu) =>
            new(ServiceSettings.Default, DishScoringSettings.Default, ServiceEconomySettings.Default, stock, menu, 6, new SeededRandom(7));

        static void Run(ServiceSession s, float seconds)
        {
            for (float t = 0; t < seconds; t += Dt) s.Tick(Dt);
        }

        static CustomerLogic Seat(ServiceSession s, CustomerTraits traits)
        {
            var c = new CustomerLogic(traits);
            s.AdmitCustomer(c);
            c.ArrivedAtSeat();
            Run(s, 0.2f);
            return c;
        }

        Storeroom StockFor(int stewBatches)
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, stewBatches));
            stock.Add(Stack(Cap, stewBatches));
            return stock;
        }

        /// <summary>Put a batch on and finish chopping it with <paramref name="chopScore"/>.</summary>
        void Cook(ServiceSession s, RecipeDefinition recipe, float chopScore)
        {
            Assert.That(s.StartBatch(recipe, this));
            s.FinishChopping(this, chopScore);
        }

        [Test]
        public void HelpingsFromChopAccuracy()
        {
            var p = StewPotSettings.Default;
            Assert.That(StewPot.HelpingsFor(0f, p), Is.EqualTo(3));
            Assert.That(StewPot.HelpingsFor(0.5f, p), Is.EqualTo(4));
            Assert.That(StewPot.HelpingsFor(1f, p), Is.EqualTo(5));
        }

        [Test]
        public void Batch_TakesOneSetOfIngredients_Chops_Simmers_ThenIsReady()
        {
            var stock = StockFor(2);
            var s = Session(stock, m_Stew);
            Assert.That(s.NextBatch(), Is.SameAs(m_Stew));
            Assert.That(s.StartBatch(m_Stew, this));
            Assert.That(stock.TotalCount, Is.EqualTo(2), "one haunch and one cap went in");
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Chopping));
            Assert.That(s.Pot.ChopItems.Select(i => i.Definition), Is.EqualTo(new[] { Haunch, Cap }));
            Assert.That(s.NextBatch(), Is.Null, "the pot is busy");

            s.FinishChopping(this, 1f);
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Simmering));
            Assert.That(s.Pot.Helpings, Is.EqualTo(5));
            Run(s, StewPotSettings.Default.simmerSeconds - 1f);
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Simmering));
            Run(s, 1.1f);
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Ready));
        }

        [Test]
        public void AbandonedChop_ReturnsTheIngredients()
        {
            var stock = StockFor(1);
            var s = Session(stock, m_Stew);
            s.StartBatch(m_Stew, this);
            s.AbandonBatch("someone else");
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Chopping), "only the cook can abandon");
            s.AbandonBatch(this);
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Empty));
            Assert.That(stock.TotalCount, Is.EqualTo(2));
        }

        [Test]
        public void StewOrder_WaitsForThePot_ThenIsLadledOntoThePass_AndPays()
        {
            var s = Session(StockFor(1), m_Stew);
            var c = Seat(s, Quick());
            var t = s.Tickets.Single();
            Assert.That(t.Recipe, Is.SameAs(m_Stew));
            Assert.That(t.State, Is.EqualTo(TicketState.Queued));
            Assert.That(s.WaitingForStew(m_Stew), Is.EqualTo(1));

            Cook(s, m_Stew, 0f); // 3 helpings
            Assert.That(t.State, Is.EqualTo(TicketState.Queued), "still simmering");
            Run(s, StewPotSettings.Default.simmerSeconds + 0.1f);
            Assert.That(t.State, Is.EqualTo(TicketState.Ready), "ladled automatically");
            Assert.That(s.Pot.Helpings, Is.EqualTo(2));
            Assert.That(s.NextToServe(), Is.SameAs(t));

            s.StartDelivery(t, this);
            Assert.That(s.Deliver(t, c, 1f));
            Run(s, 1.5f);
            Assert.That(c.Departure, Is.EqualTo(Departure.Paid));
            Assert.That(s.Ledger.Gold, Is.GreaterThan(0));
        }

        [Test]
        public void OrderWhilePotIsReady_IsLadledAtOnce_AndTheLastHelpingEmptiesThePot()
        {
            var s = Session(StockFor(1), m_Stew);
            Cook(s, m_Stew, 0f); // 3 helpings, nobody waiting yet
            Run(s, StewPotSettings.Default.simmerSeconds + 0.1f);
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Ready));
            for (int i = 0; i < 3; i++)
            {
                Seat(s, Quick());
                Assert.That(s.Tickets.Last().State, Is.EqualTo(TicketState.Ready));
            }
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Empty));
        }

        [Test]
        public void Stew_SoldOutExactly_AtThreeWaitingOrders_FromOneBatch()
        {
            var s = Session(StockFor(1), m_Stew);
            for (int i = 0; i < 2; i++) Seat(s, Quick());
            Assert.That(s.IsSoldOut(m_Stew), Is.False);
            Seat(s, Quick());
            Assert.That(s.IsSoldOut(m_Stew), "3 waiting = the fewest helpings one batch makes");
            var fourth = Seat(s, Quick());
            Assert.That(fourth.Departure, Is.EqualTo(Departure.SoldOut));

            // A great chop makes more than promised; the extra helpings wait in the pot.
            Cook(s, m_Stew, 1f);
            Run(s, StewPotSettings.Default.simmerSeconds + 0.1f);
            Assert.That(s.Tickets.Count(t => t.State == TicketState.Ready), Is.EqualTo(3));
            Assert.That(s.Pot.Helpings, Is.EqualTo(2));
        }

        [Test]
        public void NextBatch_PrefersTheStewWithTheMostWaitingOrders()
        {
            var stock = StockFor(1);
            stock.Add(Stack(Liver, 1));
            stock.Add(Stack(Cap, 1));
            var s = Session(stock, m_Stew, m_Pottage);
            Assert.That(s.NextBatch(), Is.SameAs(m_Stew), "nothing waiting: the first stew on the menu");
            for (int i = 0; i < 3; i++) Seat(s, Quick());
            int stew = s.WaitingForStew(m_Stew), pottage = s.WaitingForStew(m_Pottage);
            Assert.That(stew + pottage, Is.EqualTo(3));
            Assert.That(s.NextBatch(), Is.SameAs(pottage > stew ? m_Pottage : m_Stew));
        }

        [Test]
        public void DroppedHelping_GoesBackInLineForTheNextOne()
        {
            var s = Session(StockFor(1), m_Stew);
            var c = Seat(s, Quick());
            Cook(s, m_Stew, 0f);
            Run(s, StewPotSettings.Default.simmerSeconds + 0.1f);
            var t = s.Tickets.Single();
            s.StartDelivery(t, this);
            s.Dropped(t);
            Assert.That(t.State, Is.EqualTo(TicketState.Ready), "re-ladled from the pot straight away");
            Assert.That(s.Pot.Helpings, Is.EqualTo(1));
            Assert.That(c.State, Is.EqualTo(CustomerState.WaitingForFood));
        }

        [Test]
        public void WalkoutBeforeLadling_CancelsTheOrder_TheBatchStays()
        {
            var s = Session(StockFor(1), m_Stew);
            var impatient = Quick();
            impatient.orderPatience = 3f;
            Seat(s, impatient);
            Cook(s, m_Stew, 0f);
            Run(s, 4f);
            Assert.That(s.Tickets, Is.Empty);
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Simmering));
            Assert.That(s.Pot.Helpings, Is.EqualTo(3));
        }

        [Test]
        public void StaffPotCook_PutsABatchOn_AndChopsIt_WithoutPlayerInput()
        {
            var s = Session(StockFor(1), m_Stew);
            var factory = new MinigameFactory(GrillSettings.Default, TapSettings.Default, ServingSettings.Default, ChopSettings.Default);
            var staff = new StaffPotCook(s, factory, skill: 0.6f, qualityCap: 0.85f, restBetweenJobs: 0.5f, new SeededRandom(3));
            for (float t = 0; t < 30f && s.Pot.State is PotState.Empty or PotState.Chopping; t += Dt)
            {
                staff.Tick(Dt);
                s.Tick(Dt);
            }
            Assert.That(s.Pot.State, Is.EqualTo(PotState.Simmering));
            Assert.That(s.Pot.Helpings, Is.InRange(3, StewPot.HelpingsFor(0.85f, StewPotSettings.Default)));
        }
    }
}
