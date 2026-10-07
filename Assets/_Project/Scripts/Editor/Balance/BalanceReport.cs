using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.Tavern.Service;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The whole-loop balance check (4f Checkpoint D): representative evenings estimated from the real content (recipes,
    /// market prices, scoring and economy settings) with <see cref="EveningEstimate"/>, compared side by side. Not a
    /// simulation of a whole game; a quick way to see whether the economy tells the right story after a tuning change.
    /// Hearthdelve → Balance → Evening Report (or <see cref="RunBatch"/>) writes <c>BatchLogs/balance.md</c>.
    /// </summary>
    public static class BalanceReport
    {
        const string k_Output = "BatchLogs/balance.md";

        /// <summary>One kind of evening: the menu, the stock (parts from below free, market goods bought), how it's played.</summary>
        public sealed class Scenario
        {
            public string Name;
            public string[] Menu;
            /// <summary>(ingredient id, count, quality, freshness, bought at the market?)</summary>
            public (string id, int count, Quality quality, float freshness, bool bought)[] Stock;
            public PlayStyle Play;
            /// <summary>Special requests met (the evening's expected one or two).</summary>
            public int Requests;
        }

        public sealed class Row
        {
            public Scenario Scenario;
            public EveningResult Result;
            /// <summary>Gold spent on the market goods this evening used (what's left keeps for tomorrow).</summary>
            public int CostUsed;
            public int ProfitUsed => Result.Takings - CostUsed;
        }

        static GameDatabase Database => AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset");
        static TavernContent Content => AssetDatabase.LoadAssetAtPath<TavernContent>(EditorPaths.Data + "/Tavern/TavernContent.asset");

        static (string, int, Quality, float, bool) Below(string id, int count, Quality q = Quality.Standard, float fresh = 0.85f) => (id, count, q, fresh, false);
        static (string, int, Quality, float, bool) Market(string id, int count) => (id, count, Quality.Standard, 1f, true);

        /// <summary>The evenings compared (a market-only night, ordinary and strong delves, a signature night, a recovery night).</summary>
        public static List<Scenario> Scenarios() => new()
        {
            new Scenario
            {
                Name = "market only, weak", Menu = new[] { "brackenford_ale", "onion_broth", "eggs_on_toast" }, Play = PlayStyle.Weak,
                Stock = new[] { Market("malt", 4), Market("onion", 4), Market("herbs", 2), Market("eggs", 4), Market("bread", 4) },
            },
            new Scenario
            {
                Name = "market only, competent", Menu = new[] { "brackenford_ale", "onion_broth", "eggs_on_toast" }, Play = PlayStyle.Competent, Requests = 1,
                Stock = new[] { Market("malt", 4), Market("onion", 4), Market("herbs", 2), Market("eggs", 4), Market("bread", 4) },
            },
            new Scenario
            {
                Name = "market only, strong", Menu = new[] { "brackenford_ale", "onion_broth", "eggs_on_toast" }, Play = PlayStyle.Strong, Requests = 1,
                Stock = new[] { Market("malt", 4), Market("onion", 4), Market("herbs", 2), Market("eggs", 4), Market("bread", 4) },
            },
            new Scenario
            {
                Name = "recovery after a death (a Lockbox stack of legs)", Menu = new[] { "grilled_spider_leg", "brackenford_ale", "eggs_on_toast" },
                Play = PlayStyle.Competent, Requests = 1,
                Stock = new[] { Below("spider_leg", 3), Market("herbs", 3), Market("malt", 4), Market("eggs", 4), Market("bread", 4) },
            },
            new Scenario
            {
                Name = "ordinary Cellars delve", Menu = new[] { "grilled_spider_leg", "crispy_bat_wings", "gelbrew" }, Play = PlayStyle.Competent, Requests = 1,
                Stock = new[] { Below("spider_leg", 4), Below("bat_wing", 6), Below("slime_gel", 3), Market("herbs", 4), Market("malt", 3) },
            },
            new Scenario
            {
                Name = "ordinary delve, Gunta on the grill", Menu = new[] { "grilled_spider_leg", "crispy_bat_wings", "gelbrew" }, Requests = 1,
                // Gunta's measured grill score; the keeper pours and serves as usual.
                Play = new PlayStyle { cookScore = GuntaGrill(), servingScore = 0.8f, chopScore = 0.7f, waitFraction = 0.35f, flavorMatch = 0.55f },
                Stock = new[] { Below("spider_leg", 4), Below("bat_wing", 6), Below("slime_gel", 3), Market("herbs", 4), Market("malt", 3) },
            },
            new Scenario
            {
                Name = "strong Cellars delve", Menu = new[] { "cellar_kebab", "crispy_bat_wings", "gelbrew" }, Play = PlayStyle.Strong, Requests = 2,
                Stock = new[]
                {
                    Below("spider_leg", 3, Quality.Fine, 0.9f), Below("venom_sac", 2, Quality.Fine, 0.9f), Below("bat_wing", 6, Quality.Fine, 0.9f),
                    Below("slime_gel", 3, Quality.Fine, 0.9f), Below("shroom_cap", 5, Quality.Standard, 0.9f), Market("bread", 5), Market("herbs", 3), Market("malt", 3),
                },
            },
            new Scenario
            {
                Name = "signature night (butchered cuts, a core)", Menu = new[] { "spider_leg_steaks", "bat_wing_platter", "core_tonic" }, Play = PlayStyle.Strong, Requests = 2,
                Stock = new[]
                {
                    Below("spider_leg_cuts", 8, Quality.Fine, 0.9f), Below("bat_wing_cuts", 9, Quality.Fine, 0.9f), Below("spore_sac", 3, Quality.Fine, 0.9f),
                    Below("slime_core", 2, Quality.Fine, 0.9f), Below("slime_gel", 2, Quality.Fine, 0.9f), Market("herbs", 4), Market("bread", 3),
                },
            },
        };

        static float GuntaGrill()
        {
            StaffDefinition gunta = Content.staff.FirstOrDefault(s => s != null && s.id == StaffIds.Boog);
            return gunta != null ? StaffScore(gunta, CookStation.Grill) : 0.75f;
        }

        public static int MarketPrice(string ingredientId)
        {
            SupplySource market = Database != null ? Database.market : null;
            SupplyOffer offer = market != null ? market.offers.FirstOrDefault(o => o.ingredient != null && o.ingredient.id == ingredientId) : null;
            return offer != null ? Mathf.CeilToInt(offer.price / (float)Mathf.Max(1, offer.bundle)) : 0;
        }

        /// <summary>
        /// The garden's best week (4h Checkpoint B), in market gold: every bed growing its most valuable crop back to back for
        /// <paramref name="days"/> days, every harvest sold at the market's price. Staples, not riches: it must stay well under
        /// what a delve night earns.
        /// </summary>
        public static int GardenWeekValue(int days = 7)
        {
            GameDatabase db = Database;
            if (db == null) return 0;
            int best = 0;
            foreach (Hearthdelve.Shared.Garden.CropDefinition crop in db.crops.Where(c => c != null && c.produce != null))
                best = Mathf.Max(best, days / crop.growthDays * crop.yield * MarketPrice(crop.produce.id));
            return best * db.GardenBeds.Count;
        }

        /// <summary>Runs one scenario on the current content.</summary>
        public static Row Run(Scenario s)
        {
            GameDatabase db = Database;
            TavernContent content = Content;
            var stock = new Storeroom();
            int cost = 0;
            foreach (var (id, count, quality, fresh, bought) in s.Stock)
            {
                IngredientDefinition d = db.Ingredient(id);
                if (d == null) throw new System.InvalidOperationException($"No ingredient '{id}'.");
                stock.Add(new IngredientStack(new IngredientItem(d, quality), count, fresh));
                if (bought) cost += MarketPrice(id) * count;
            }
            var before = stock.Clone();
            List<RecipeDefinition> menu = s.Menu.Select(id => content.recipes.First(r => r != null && r.id == id)).ToList();
            float generosity = content.customers.Count > 0 ? content.customers.Average(c => c.traits.generosity) : 1f;
            int covers = EveningEstimate.ExpectedCovers(content.service.service);
            EveningResult result = EveningEstimate.Run(menu, stock, covers, s.Play, content.economy.dishScoring, content.economy.service,
                content.stew != null ? content.stew.pot : StewPotSettings.Default, generosity, cost, s.Requests, content.service.requests);
            int used = 0;
            foreach (var (id, _, _, _, bought) in s.Stock)
                if (bought) used += MarketPrice(id) * (before.CountMatching(i => i.Definition.id == id) - stock.CountMatching(i => i.Definition.id == id));
            return new Row { Scenario = s, Result = result, CostUsed = used };
        }

        /// <summary>
        /// What one part is worth as dishes: grilled whole, or broken down at the Butcher Block at a given score and sold as
        /// the cut dish (a share of it: the cut dish needs more than one cut).
        /// </summary>
        public static (float whole, float butchered) ButcherValue(string partId, string wholeDish, string cutDish, float cutScore)
        {
            GameDatabase db = Database;
            TavernContent content = Content;
            IngredientDefinition part = db.Ingredient(partId);
            RecipeDefinition whole = content.recipes.First(r => r.id == wholeDish), cut = content.recipes.First(r => r.id == cutDish);
            int partsPerWhole = whole.slots.Where(sl => !sl.optional && sl.ingredient == part).Sum(sl => sl.count);
            int cutsPerDish = cut.slots.Where(sl => !sl.optional && sl.ingredient == part.butchering.cut).Sum(sl => sl.count);
            int cuts = ButcherRules.Yield(part.butchering, cutScore);
            return (whole.baseValue / (float)Mathf.Max(1, partsPerWhole), cut.baseValue * cuts / (float)Mathf.Max(1, cutsPerDish));
        }

        /// <summary>
        /// A staff member's average score at a station: their skill through the station's real auto-player, capped like all
        /// staff work. The keeper's own score is whatever they play.
        /// </summary>
        public static float StaffScore(StaffDefinition staff, CookStation station, int samples = 40)
        {
            TavernContent content = Content;
            var factory = new MinigameFactory(content.grill.grill, content.tap.tap, content.serving.serving,
                content.stew != null ? content.stew.chop : ChopSettings.Default, content.butcher != null ? content.butcher.butcher : ButcherSettings.Default);
            float total = 0f;
            for (int i = 0; i < samples; i++)
            {
                var random = new Hearthdelve.Core.Random.SeededRandom(1000 + i);
                Hearthdelve.Core.Minigames.IMinigame game = station == CookStation.StewPot ? factory.CreateChop(3, random) : factory.CreateCook(station);
                float score = Hearthdelve.Core.Minigames.MinigameRunner.RunToCompletion(game, MinigameFactory.CreateAutoPlayer(game, staff.skill, random));
                total += Mathf.Min(score, staff.qualityCap);
            }
            return total / samples;
        }

        [MenuItem("Hearthdelve/Balance/Evening Report")]
        public static void Write()
        {
            var sb = new StringBuilder();
            TavernContent content = Content;
            int covers = EveningEstimate.ExpectedCovers(content.service.service);
            sb.AppendLine("# Evening balance report");
            sb.AppendLine();
            sb.AppendLine($"Estimated with `EveningEstimate` from the current content: {covers} patrons a night, average generosity "
                          + $"{content.customers.Average(c => c.traits.generosity):0.00}. Parts from below cost nothing here (their price is the delve).");
            sb.AppendLine();
            sb.AppendLine("| Evening | Covers | Paid | Tips (incl. requests) | Takings | Market goods used | Profit | Renown |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|");
            foreach (Scenario s in Scenarios())
            {
                Row r = Run(s);
                EveningResult e = r.Result;
                sb.AppendLine($"| {s.Name} | {e.Covers} | {e.Gold} | {e.Tips + e.RequestBonus} | {e.Takings} | {r.CostUsed} | {r.ProfitUsed} | {e.Renown:+0;-0;0} |");
            }
            sb.AppendLine();
            sb.AppendLine("## The garden (4h Checkpoint B)");
            sb.AppendLine();
            sb.AppendLine($"Its best week, every bed harvested back to back and the produce priced at the market: **{GardenWeekValue()} gold** "
                          + $"({Database.GardenBeds.Count} beds). Staples, never the way to get rich: compare one ordinary delve night above.");
            sb.AppendLine();
            sb.AppendLine("## A part at the Butcher Block (gold of dishes per part)");
            sb.AppendLine();
            sb.AppendLine("| Part | Whole dish | Weak cut (0.3) | Fair cut (0.6) | Clean cut (0.9) |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var (part, whole, cut) in new[] { ("spider_leg", "grilled_spider_leg", "spider_leg_steaks"), ("bat_wing", "crispy_bat_wings", "bat_wing_platter") })
            {
                var weak = ButcherValue(part, whole, cut, 0.3f);
                var fair = ButcherValue(part, whole, cut, 0.6f);
                var clean = ButcherValue(part, whole, cut, 0.9f);
                sb.AppendLine($"| {part} | {weak.whole:0.0} | {weak.butchered:0.0} | {fair.butchered:0.0} | {clean.butchered:0.0} |");
            }
            sb.AppendLine();
            sb.AppendLine("## Staff at the stations (average score; the keeper's best is 1)");
            sb.AppendLine();
            sb.AppendLine("| Staff | Skill | Cap | Grill | Tap | Stew (chop) |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (StaffDefinition staff in content.staff.Where(s => s != null))
                sb.AppendLine($"| {staff.id} | {staff.skill:0.00} | {staff.qualityCap:0.00} | {StaffScore(staff, CookStation.Grill):0.00} | "
                              + $"{StaffScore(staff, CookStation.Tap):0.00} | {StaffScore(staff, CookStation.StewPot):0.00} |");
            sb.AppendLine();
            sb.AppendLine("## Furniture prices by catalog tier");
            sb.AppendLine();
            sb.AppendLine("| Tier (Renown) | Priced pieces | Cheapest | Median | Dearest |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            GameDatabase db = Database;
            CatalogSettings catalog = db.catalog;
            foreach (var g in db.furniture.Where(f => f != null && f.ForSale).GroupBy(f => f.catalogTier).OrderBy(g => g.Key))
            {
                var prices = g.Select(f => f.price).OrderBy(p => p).ToList();
                int[] thresholds = catalog != null ? catalog.Thresholds() : new int[0];
                int renown = g.Key < thresholds.Length ? thresholds[g.Key] : -1;
                sb.AppendLine($"| {g.Key} ({renown}) | {prices.Count} | {prices[0]} | {prices[prices.Count / 2]} | {prices[^1]} |");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(k_Output));
            File.WriteAllText(k_Output, sb.ToString());
            Debug.Log("[Hearthdelve] Balance report:\n" + sb);
        }

        public static void RunBatch() => Write();
    }
}
