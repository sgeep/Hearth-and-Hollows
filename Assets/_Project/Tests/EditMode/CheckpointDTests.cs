using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Random;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4f Checkpoint D: special requests in a service session (fairness, frequency, seeding, met, missed, the results line,
    /// a clean next evening), Renown that adds up over an evening, and the balance pass's story on the real content.
    /// </summary>
    public class CustomerRequestTests : KitchenFixture
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

        static CustomerTraits Quick(float patience = 30f) => new()
        {
            walkSpeed = 3f, seatPatience = 10f, orderPatience = patience, orderDelay = 0.1f, eatSeconds = 1f, generosity = 1f,
        };

        static CustomerRequestSettings Always(int max = 8, int before = 0) => new()
        {
            chance = 1f, maxPerEvening = max, ordersBeforeFirst = before, bonusFraction = 0.5f, minBonusGold = 2, bonusRenown = 1,
        };

        ServiceSession Session(Storeroom stock, CustomerRequestSettings? requests, int seed = 7, params RecipeDefinition[] menu)
        {
            var s = new ServiceSession(ServiceSettings.Default, DishScoringSettings.Default, ServiceEconomySettings.Default,
                stock, menu.Length > 0 ? menu : new[] { m_Grilled, m_Gelbrew }, 6, new SeededRandom(seed));
            if (requests.HasValue) s.ConfigureRequests(requests.Value);
            return s;
        }

        static void Run(ServiceSession s, float seconds)
        {
            for (float t = 0; t < seconds; t += Dt) s.Tick(Dt);
        }

        static CustomerLogic Seat(ServiceSession s, float patience = 30f)
        {
            var c = new CustomerLogic(Quick(patience));
            s.AdmitCustomer(c);
            c.ArrivedAtSeat();
            Run(s, 0.2f);
            return c;
        }

        static Ticket Serve(ServiceSession s, CustomerLogic c, float score = 1f)
        {
            Ticket t = s.Tickets.First(x => x.Customer == c);
            s.StartCooking(t, "cook");
            s.FinishCooking(t, score);
            s.StartDelivery(t, "carrier");
            Assert.That(s.Deliver(t, c, score));
            return t;
        }

        Storeroom Plenty()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 10));
            stock.Add(Stack(Gel, 20));
            return stock;
        }

        [Test]
        public void Requests_AreOff_UnlessTheEveningTurnsThemOn()
        {
            ServiceSession s = Session(Plenty(), null);
            for (int i = 0; i < 6; i++) Seat(s);
            Assert.That(s.Ledger.RequestsIssued, Is.Zero);
            Assert.That(s.Customers.Any(c => c.IsRequest), Is.False);
        }

        [Test]
        public void ARequest_IsOnlyEverForADishOnTheMenu_ThatCanBeMadeRightNow()
        {
            // Grilled haunch is on the menu but there's no haunch: every request is for gelbrew.
            var stock = new Storeroom();
            stock.Add(Stack(Gel, 20));
            ServiceSession s = Session(stock, Always());
            var asked = new List<RecipeDefinition>();
            s.RequestIssued += (c, dish) => asked.Add(dish);
            for (int i = 0; i < 6; i++) Seat(s);
            Assert.That(asked, Is.Not.Empty);
            Assert.That(asked, Has.All.EqualTo(m_Gelbrew));
            foreach (CustomerLogic c in s.Customers.Where(c => c.IsRequest))
                Assert.That(s.Tickets.Single(t => t.Customer == c).Reserved, Is.Not.Null, "its ingredients already set aside");
        }

        [Test]
        public void Requests_StopAtTheEveningsCap_AndWaitForTheFirstOrders()
        {
            ServiceSession s = Session(Plenty(), Always(max: 2, before: 1));
            var seated = new List<CustomerLogic>();
            for (int i = 0; i < 6; i++) seated.Add(Seat(s));
            Assert.That(seated[0].IsRequest, Is.False, "the first order settles the evening in");
            Assert.That(seated.Count(c => c.IsRequest), Is.EqualTo(2));
            Assert.That(s.Ledger.RequestsIssued, Is.EqualTo(2));
        }

        [Test]
        public void TheDefaultTuning_GivesAQuietEveningNoMoreThanTwo()
        {
            CustomerRequestSettings d = CustomerRequestSettings.Default;
            Assert.That(d.maxPerEvening, Is.EqualTo(2));
            Assert.That(d.chance, Is.InRange(0.1f, 0.3f));
            var counts = new List<int>();
            for (int seed = 1; seed <= 200; seed++)
            {
                ServiceSession s = Session(Plenty(), d, seed);
                for (int i = 0; i < 11; i++) Seat(s);
                counts.Add(s.Ledger.RequestsIssued);
            }
            Assert.That(counts.Max(), Is.LessThanOrEqualTo(2));
            Assert.That(counts.Average(), Is.InRange(0.8f, 1.9f), "about one or two a night");
            Assert.That(counts.Count(c => c == 0), Is.GreaterThan(0), "some nights have none");
        }

        [Test]
        public void WhoAsks_IsSeeded()
        {
            var settings = Always();
            settings.chance = 0.4f;
            bool[] Asks(int seed)
            {
                ServiceSession s = Session(Plenty(), settings, seed);
                return Enumerable.Range(0, 8).Select(_ => Seat(s).IsRequest).ToArray();
            }
            Assert.That(Asks(5), Is.EqualTo(Asks(5)));
            Assert.That(Enumerable.Range(1, 10).Select(Asks).Select(a => string.Join(",", a)).Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void ARequestMet_PaysAThankYou_AndALittleRenown()
        {
            ServiceSession s = Session(Plenty(), Always(max: 1));
            (RecipeDefinition dish, float quality, int gold, int renown)? met = null;
            s.RequestCompleted += (c, dish, q, g, r) => met = (dish, q, g, r);
            CustomerLogic c = Seat(s);
            Assert.That(c.IsRequest);
            Ticket t = Serve(s, c, 1f);
            Run(s, 2f);
            Assert.That(c.RequestOutcome, Is.EqualTo(RequestOutcome.Completed));
            int bonus = CustomerRequestRules.BonusGold(t.DishValue, Always());
            Assert.That(met, Is.Not.Null);
            Assert.That(met.Value.dish, Is.SameAs(c.Order));
            Assert.That((met.Value.gold, met.Value.renown), Is.EqualTo((bonus, 1)));
            Assert.That(s.Ledger.RequestsCompleted, Is.EqualTo(1));
            Assert.That(s.Ledger.RequestGold, Is.EqualTo(bonus));
            Assert.That(s.Ledger.Tips, Is.GreaterThanOrEqualTo(bonus), "the thanks is in the tips");
            Assert.That(s.Ledger.RequestRenown, Is.EqualTo(1));
            Assert.That(bonus, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ARequestMissed_ByWalkingOut_OrAtClosingTime_GivesNothingExtra()
        {
            ServiceSession s = Session(Plenty(), Always(max: 2));
            var missed = new List<RequestOutcome>();
            s.RequestFailed += (c, dish, outcome) => missed.Add(outcome);
            CustomerLogic impatient = Seat(s, patience: 1f);
            Assert.That(impatient.IsRequest);
            Run(s, 3f);
            Assert.That(impatient.RequestOutcome, Is.EqualTo(RequestOutcome.WalkedOut));
            CustomerLogic late = Seat(s);
            Assert.That(late.IsRequest);
            s.End();
            Assert.That(late.RequestOutcome, Is.EqualTo(RequestOutcome.ClosingTime));
            Assert.That(missed, Is.EqualTo(new[] { RequestOutcome.WalkedOut, RequestOutcome.ClosingTime }));
            Assert.That((s.Ledger.RequestsFailed, s.Ledger.RequestsCompleted, s.Ledger.RequestGold), Is.EqualTo((2, 0, 0)));
        }

        [Test]
        public void ARequestMissed_WhenItsLastPlateIsDropped_IsSoldOut()
        {
            var stock = new Storeroom();
            stock.Add(Stack(Haunch, 1));
            ServiceSession s = Session(stock, Always(max: 1), 7, m_Grilled);
            CustomerLogic c = Seat(s);
            Assert.That(c.IsRequest);
            Ticket t = s.Tickets.Single();
            s.StartCooking(t, "cook");
            s.FinishCooking(t, 1f);
            s.StartDelivery(t, "carrier");
            s.Dropped(t);
            Run(s, 0.5f);
            Assert.That(c.RequestOutcome, Is.EqualTo(RequestOutcome.SoldOut));
            Assert.That(s.Ledger.RequestsFailed, Is.EqualTo(1));
        }

        [Test]
        public void TheResults_ShowRequestsMetOfMade_OnlyWhenThereWereAny()
        {
            ServiceSession s = Session(Plenty(), Always(max: 2));
            CustomerLogic first = Seat(s), second = Seat(s);
            Serve(s, first);
            Run(s, 2f);
            s.End();
            var report = new EveningReport(s.Ledger, false, false);
            Assert.That(report.Lines.Single(l => l.line == EveningLine.Requests).value, Is.EqualTo(1));
            Assert.That(report.RequestsIssued, Is.EqualTo(2));
            var quiet = new EveningReport(Session(Plenty(), null).Ledger, false, false);
            Assert.That(quiet.Lines.Any(l => l.line == EveningLine.Requests), Is.False);
        }

        [Test]
        public void EachEvening_StartsWithNoRequests()
        {
            ServiceSession tonight = Session(Plenty(), Always(max: 2));
            Seat(tonight);
            Seat(tonight);
            tonight.End();
            ServiceSession tomorrow = Session(Plenty(), Always(max: 2));
            Assert.That((tomorrow.Ledger.RequestsIssued, tomorrow.Ledger.RequestsCompleted, tomorrow.Ledger.RequestsFailed), Is.EqualTo((0, 0, 0)));
            Assert.That(tomorrow.Customers, Is.Empty);
            Assert.That(Seat(tomorrow).IsRequest, "a fresh cap");
        }
    }

    public class RenownAndBalanceTests
    {
        [Test]
        public void Renown_AddsUpOverTheEvening_InsteadOfRoundingEachPatronToNothing()
        {
            ServiceEconomySettings e = ServiceEconomySettings.Default;
            float decent = e.renownNeutral + 0.1f;
            Assert.That(ServiceEconomy.Renown(decent, e), Is.Zero, "one decent patron rounds to nothing");
            Assert.That(ServiceEconomy.RenownExact(decent, e) * 10f, Is.EqualTo(4f).Within(0.01f), "ten of them add up to 4");
        }

        static BalanceReport.Row Run(string name) => BalanceReport.Run(BalanceReport.Scenarios().Single(s => s.Name.StartsWith(name)));

        [Test]
        public void AMarketOnlyNight_IsASafetyNet_NotTheWayToGetRich()
        {
            BalanceReport.Row weak = Run("market only, weak"), competent = Run("market only, competent"), strong = Run("market only, strong");
            BalanceReport.Row delve = Run("ordinary Cellars delve"), strongDelve = Run("strong Cellars delve");
            Assert.That(weak.ProfitUsed, Is.GreaterThanOrEqualTo(0), "even a weak market night doesn't lose money");
            Assert.That(competent.ProfitUsed, Is.GreaterThan(0));
            Assert.That(delve.ProfitUsed, Is.GreaterThan(competent.ProfitUsed * 2), "a delve night at the same skill earns far more");
            Assert.That(strongDelve.ProfitUsed, Is.GreaterThan(strong.ProfitUsed * 2.5f));
            Assert.That(Run("recovery after a death").ProfitUsed, Is.GreaterThan(competent.ProfitUsed), "a Lockbox stack helps the next night");
        }

        [Test]
        public void SignatureDishes_AreTheBestNight_ButConstrainedByTheirCuts()
        {
            BalanceReport.Row signature = Run("signature night"), strong = Run("strong Cellars delve");
            Assert.That(signature.ProfitUsed, Is.GreaterThan(strong.ProfitUsed));
            Assert.That(signature.ProfitUsed, Is.LessThan(strong.ProfitUsed * 2f), "valuable, not runaway");
            Assert.That(signature.Result.Covers, Is.LessThan(EveningEstimate.ExpectedCovers(ServiceSettings.Default)), "the cuts run out before the evening does");
        }

        [Test]
        public void TheButcherBlock_RewardsACleanCut_AndAWeakOneCostsLittle()
        {
            foreach (var (part, whole, cut) in new[] { ("spider_leg", "grilled_spider_leg", "spider_leg_steaks"), ("bat_wing", "crispy_bat_wings", "bat_wing_platter") })
            {
                var weak = BalanceReport.ButcherValue(part, whole, cut, 0.3f);
                var clean = BalanceReport.ButcherValue(part, whole, cut, 0.9f);
                Assert.That(weak.butchered, Is.GreaterThanOrEqualTo(weak.whole * 0.8f), $"{part}: a weak cut is close to cooking it whole");
                Assert.That(clean.butchered, Is.InRange(clean.whole * 2f, clean.whole * 3.5f), $"{part}: a clean cut is well worth it, not absurd");
            }
        }

        [Test]
        public void Gunta_TradesQualityForConvenience()
        {
            StaffDefinition gunta = UnityEditor.AssetDatabase.LoadAssetAtPath<Hearthdelve.Tavern.Scene.TavernContent>("Assets/_Project/Data/Tavern/TavernContent.asset")
                .staff.Single(s => s.id == StaffIds.Boog);
            float grill = BalanceReport.StaffScore(gunta, CookStation.Grill);
            Assert.That(grill, Is.LessThan(PlayStyle.Competent.cookScore), "below a competent keeper");
            Assert.That(grill, Is.GreaterThan(0.6f), "but never bad food");
            Assert.That(Run("ordinary delve, Gunta").ProfitUsed, Is.InRange(Run("ordinary Cellars delve").ProfitUsed * 0.6f, Run("ordinary Cellars delve").ProfitUsed));
        }

        [Test]
        public void Renown_OpensTheCatalog_OverDaysOfPlay()
        {
            int competent = Run("ordinary Cellars delve").Result.Renown, strong = Run("strong Cellars delve").Result.Renown;
            Assert.That(competent, Is.GreaterThan(2), "a competent night earns some Renown");
            Assert.That(strong, Is.InRange(competent * 2, 25), "a strong one clearly more, but no tier in a single night");
            Assert.That(100 / strong, Is.GreaterThanOrEqualTo(5), "the last tier takes about a week of strong nights");
        }
    }
}
