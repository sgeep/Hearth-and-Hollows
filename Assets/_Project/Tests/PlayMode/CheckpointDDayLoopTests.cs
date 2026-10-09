using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Save;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
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
    /// 4f Checkpoint D's integration run through <see cref="GameFlow"/> (saves in a temp folder): three days from a new game
    /// through every part of 4f (a curio and a haul home, an upgrade and a rearranged room at night, the market, the Butcher
    /// Block, Boog and Orik, a special request, a death that loses a curio, sleep), then Continue twice with nothing granted
    /// again.
    /// </summary>
    public class CheckpointDDayLoopTests : BootFixture
    {
        IEnumerator BootToMenu()
        {
            yield return Boot();
            yield return null;
        }

        static IEnumerator InTavern(TavernPhase phase, string what)
        {
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == phase && Flow.LoadedScene == GameScenes.Tavern, 30f, what);
            yield return null;
        }

        IEnumerator InDungeon()
        {
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Player = LevelManager.Instance.Players[0];
            yield return null;
            FreezeEnemies();
        }

        static Satchel PlayerSatchel => LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>().Satchel;
        static IngredientDefinition Ingredient(string id) => Flow.Database.Ingredient(id);

        IEnumerator PickUpCurios(int expected)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            foreach (CurioPickup pickup in Object.FindObjectsByType<CurioPickup>())
            {
                Player.transform.position = pickup.transform.position;
                if (Player.TryGetComponent(out Rigidbody2D body)) body.position = pickup.transform.position;
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                yield return null;
            }
            yield return WaitUntil(() => DelveRunController.Active.Loot.Curios.Count == expected, 3f, $"{expected} curio carried");
        }

        SaveData SavedGame() => SaveSystem.FromJson(File.ReadAllText(Path.Combine(SaveDir, SaveStore.FileName)));

        static Dictionary<string, int> Owned() => Flow.State.Furniture.Owned.ToDictionary(kv => kv.Key, kv => kv.Value);

        [UnityTest, Category("Slow")]
        public IEnumerator ThreeDays_FromANewGame_ThroughEveryPartOf4f_ThenContinueTwice()
        {
            // ---------- Day 1: the first night's delve, a curio and a haul home ----------
            yield return BootToMenu();
            GameFlow.Instance.QuickNewGame();
            yield return InDungeon();
            RoomRunner.Active.GrantReward(RoomReward.Curio());
            yield return PickUpCurios(1);
            string found = DelveRunController.Active.Loot.Curios[0];
            int ownedBefore = Flow.State.Furniture.OwnedCount(found);
            PlayerSatchel.Add(new IngredientItem(Ingredient("spider_leg"), Quality.Fine), 3);
            PlayerSatchel.Add(new IngredientItem(Ingredient("bat_wing"), Quality.Standard), 2);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(DelveRunController.Active.Extract());
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the first night");
            Assert.That(Flow.State.Furniture.OwnedCount(found), Is.EqualTo(ownedBefore + 1), "the curio came home");
            Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == "spider_leg"), Is.EqualTo(3));

            // Night: an upgrade, and the room rearranged (a barrel moved).
            var night = Object.FindAnyObjectByType<NightScreen>();
            Flow.DebugAddGold(200);
            int satchelRow = night.Definitions.ToList().FindIndex(u => u.kind == UpgradeKind.SatchelSlots);
            night.Buy(satchelRow);
            night.DecorateButton.onClick.Invoke();
            yield return null;
            DecorateMode mode = DecorateMode.Instance;
            mode.SetCursor(new Vector2Int(26, 9));
            mode.PickUp();
            mode.SetCursor(new Vector2Int(12, 10));
            mode.Place();
            mode.Leave();
            yield return null;
            Assert.That(SavedGame().furniture.areas.Single(a => a.id == "tavern").pieces.Any(p => p.def == "cellar_barrel" && p.x == 12 && p.y == 10));
            night.SleepButton.onClick.Invoke();

            // ---------- Day 2: the market, the Butcher Block, Boog and Orik, a special request ----------
            yield return InTavern(TavernPhase.Daytime, "the second day");
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            DaytimeActions.OpenMarket();
            yield return null;
            SupplySource market = Flow.Database.market;
            int Row(string id) => market.offers.FindIndex(o => o.ingredient.id == id);
            foreach (string staple in new[] { "herbs", "herbs", "eggs", "bread" }) daytime.Market.Rows[Row(staple)].buy.onClick.Invoke();
            daytime.Market.Done.onClick.Invoke();
            Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == "herbs"), Is.EqualTo(2));
            DaytimeActions.BeginEvening();
            yield return InTavern(TavernPhase.Prep, "the evening's prep");

            // A leg broken down by hand: a clean cut.
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            prep.ButcherButton.onClick.Invoke();
            yield return null;
            ButcherPanel panel = prep.Butcher;
            int leg = panel.Parts.ToList().FindIndex(s => s.Item.Definition.id == "spider_leg");
            Assert.That(panel.CutYourself(leg));
            var cut = (ButcherMinigame)KeeperWork.Instance.ActiveCook;
            var hand = new ButcherAutoPlayer(cut, 1f, new Hearthdelve.Core.Random.SeededRandom(9));
            for (int i = 0; i < 60 * 20 && !cut.IsComplete; i++) cut.Tick(1f / 60f, hand.NextInput(1f / 60f));
            yield return WaitUntil(() => panel.IsOpen, 5f, "back to the list");
            Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == "spider_leg_cuts"), Is.GreaterThanOrEqualTo(2));
            panel.Close();
            yield return null;

            // Boog on the grill, Orik on the plates; one special request, met.
            Director.AssignStaff(StaffStation.Serving);
            Director.AssignCook(StaffStation.Grill);
            RecipeDefinition grilled = Director.Content.recipes.First(r => r.id == "grilled_spider_leg");
            Director.SetMenu(new[] { grilled });
            Director.OpenService();
            Director.ArrivalsPaused = true;
            Director.Session.ConfigureRequests(new CustomerRequestSettings { chance = 1f, maxPerEvening = 1, bonusFraction = 0.5f, minBonusGold = 2, bonusRenown = 1 });
            bool met = false;
            void Met(CustomerRequestCompleted e) => met = true;
            EventBus<CustomerRequestCompleted>.Subscribe(Met);
            Director.SpawnCustomer(Director.Content.customers.OrderByDescending(c => c.traits.orderPatience).First());
            Time.timeScale = 4f;
            yield return WaitUntil(() => met, 60f, "the request met by Boog and Orik");
            Time.timeScale = 1f;
            EventBus<CustomerRequestCompleted>.Unsubscribe(Met);
            int takings = Director.Session.Ledger.Gold + Director.Session.Ledger.Tips;
            int gold = Flow.State.Gold;
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            var results = Object.FindAnyObjectByType<EveningResultsScreen>();
            for (int i = 0; i < 4 && Director != null && Director.Phase == TavernPhase.Results; i++)
            {
                results.DoneButton.onClick.Invoke();
                yield return null;
            }

            // ---------- The second delve: a curio found, then a death ----------
            yield return InDungeon();
            Assert.That(Flow.State.Gold, Is.EqualTo(gold + takings), "the takings, the request's thanks among them, banked");
            RoomRunner.Active.GrantReward(RoomReward.Curio());
            yield return PickUpCurios(1);
            string lost = DelveRunController.Active.Loot.Curios[0];
            int lostBefore = Flow.State.Furniture.OwnedCount(lost);
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            var essence = Player.GetComponent<EssenceHealth>();
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 5f, "the death screen");
            death.KeepNothingButton.onClick.Invoke();
            yield return WaitUntil(() => result.IsOpen, 5f, "the delve result");
            result.Proceed();
            yield return InTavern(TavernPhase.Night, "the second night");
            Assert.That(Flow.State.Furniture.OwnedCount(lost), Is.EqualTo(lostBefore), "the curio was lost with the delve");
            Object.FindAnyObjectByType<NightScreen>().SleepButton.onClick.Invoke();

            // ---------- Day 3, then Continue twice: everything kept, nothing granted again ----------
            yield return InTavern(TavernPhase.Daytime, "the third day");
            Assert.That(Flow.State.Day, Is.EqualTo(3));
            Dictionary<string, int> owned = Owned();
            int goldNow = Flow.State.Gold, renown = Flow.State.Renown, stock = Flow.State.Storeroom.TotalCount;
            Assert.That(owned["butcher_block"], Is.EqualTo(1));
            Assert.That(owned.ContainsKey("trophy_larder_troll"), Is.False, "no trophy without the troll");
            for (int run = 0; run < 2; run++)
            {
                yield return BootToMenu();
                Object.FindAnyObjectByType<MainMenuScreen>().ContinueButton.onClick.Invoke();
                yield return InTavern(TavernPhase.Daytime, "the third day, continued");
                Assert.That(Owned(), Is.EquivalentTo(owned), "the same furniture");
                Assert.That((Flow.State.Gold, Flow.State.Renown, Flow.State.Storeroom.TotalCount), Is.EqualTo((goldNow, renown, stock)));
                Assert.That(Flow.State.Furniture.Layout("tavern").Any(p => p.definition == "cellar_barrel" && p.cell == new Vector2Int(12, 10)), "the rearranged room");
                Assert.That(Flow.State.Furniture.PendingHomecoming, Is.Null);
                Assert.That(SavedGame().version, Is.EqualTo(SaveSystem.CurrentVersion));
            }
        }
    }
}
