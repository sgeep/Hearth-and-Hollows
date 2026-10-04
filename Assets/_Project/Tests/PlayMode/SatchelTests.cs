using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Screens;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Step 3 on the test floor: harvest drops into the satchel, freshness falling while carried and
    /// on the floor, and the satchel-full flow (hint, Interact, the swap prompt with keyboard and mouse,
    /// "Leave it", the dropped stack not snapping straight back).
    /// </summary>
    public class SatchelTests : LookTestFixture
    {
        const string TestFloorScene = "Dungeon_TestFloor";
        static readonly Vector2 k_Arena = new(20.5f, 2.3f);

        EssenceHealth Essence => Player.GetComponent<EssenceHealth>();
        Satchel Satchel => Player.GetComponent<SatchelCarrier>().Satchel;
        SatchelCarrier Carrier => Player.GetComponent<SatchelCarrier>();

        IEnumerator LoadFloor()
        {
            yield return Load(TestFloorScene);
            Essence.GodMode = true;
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
                enemy.GetComponent<Character>().CharacterBrain.BrainActive = false;
            Teleport(Player, k_Arena);
            yield return new WaitForFixedUpdate();
        }

        static EnemyIdentity Enemy(string id) => Object.FindObjectsByType<EnemyIdentity>().First(e => e.Definition.id == id);
        static IngredientDefinition Part(string enemyId, int index = 0) => Enemy(enemyId).Definition.harvest[index].ingredient;

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        /// <summary>Taps a key: down for two frames, then up (frames, not seconds: time may be paused).</summary>
        IEnumerator Tap(params Key[] keys)
        {
            Hold(keys);
            yield return Frames(2);
            ReleaseKeys();
            yield return Frames(2);
        }

        /// <summary>Six different stacks (one ingredient, each quality and prep), each a full slot.</summary>
        void FillSatchel(IngredientDefinition definition)
        {
            Satchel.Clear();
            var variants = new[]
            {
                (Quality.Poor, PrepState.Raw), (Quality.Standard, PrepState.Raw), (Quality.Fine, PrepState.Raw),
                (Quality.Premium, PrepState.Raw), (Quality.Standard, PrepState.Seared), (Quality.Fine, PrepState.Seared),
            };
            foreach (var (quality, prep) in variants)
                Assert.That(Satchel.Add(new IngredientItem(definition, quality, prep), Satchel.MaxStack, 0.9f), Is.Zero);
            Assert.That(Satchel.SpaceFor(new IngredientItem(definition, Quality.Fine, PrepState.Chilled)), Is.Zero, "full");
        }

        [UnityTest]
        public IEnumerator KillingABat_DropsBatWings_ThatGoIntoTheSatchel_AndSpoilWhileCarried()
        {
            yield return LoadFloor();
            EnemyIdentity bat = Enemy("bat");
            Object.FindAnyObjectByType<HarvestSystem>().Scatter = 0f;
            Teleport(bat, k_Arena + new Vector2(3f, 0f));
            yield return new WaitForFixedUpdate();
            // Exactly lethal: overkill would ruin the parts (the harvest rules, GDD §4.3).
            Health health = bat.GetComponent<Health>();
            health.Damage(health.CurrentHealth, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => Object.FindObjectsByType<IngredientPickup>().Any(p => p.Count > 0), 1f, "the harvest to drop");

            IngredientPickup drop = Object.FindObjectsByType<IngredientPickup>().First(p => p.Count > 0);
            Assert.That(drop.Item.Definition.id, Is.EqualTo("bat_wing"), "the bat drops its own part");
            Assert.That(drop.GetComponentInChildren<SpriteRenderer>().sprite, Is.SameAs(drop.Item.Definition.icon), "shown with its icon");
            int count = drop.Count;

            Teleport(Player, drop.transform.position);
            yield return WaitUntil(() => !Satchel.IsEmpty, 1f, "the wing to go into the satchel");
            Assert.That(Satchel.Slots[0].Item.Definition.id, Is.EqualTo("bat_wing"));
            Assert.That(Satchel.Slots[0].Count, Is.EqualTo(count));

            float before = Satchel.Slots[0].Freshness;
            float started = Time.time;
            yield return new WaitForSeconds(2f);
            float perMinute = (before - Satchel.Slots[0].Freshness) / ((Time.time - started) / 60f);
            Assert.That(perMinute, Is.EqualTo(FreshnessSettings.Default.dungeonLossPerMinute).Within(0.02f), "loses freshness at the delve's rate while carried");
        }

        /// <summary>
        /// The bug found in step 3's playtest: a bat or spider killed right next to the player dropped its
        /// parts within reach, and they went straight into the satchel, so nothing seemed to drop. Now a
        /// harvest pops out in front of the body and can't be picked up until it has landed.
        /// </summary>
        [UnityTest]
        public IEnumerator AKillAtYourFeet_PopsItsDropsOutWhereYouSeeThem_BeforeTheyCanBePickedUp()
        {
            yield return LoadFloor();
            EnemyIdentity bat = Enemy("bat");
            bat.GetComponent<EnemyPerch>()?.Detach();
            // Open floor in room C, clear of walls and props in every direction.
            var open = new Vector2(35.5f, 6.3f);
            Teleport(bat, open + new Vector2(0f, 0.6f));
            Teleport(Player, open);
            yield return new WaitForFixedUpdate();
            Vector2 died = bat.transform.position;
            Health health = bat.GetComponent<Health>();
            health.Damage(health.CurrentHealth, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;

            IngredientPickup[] drops = Object.FindObjectsByType<IngredientPickup>().Where(p => p.Count > 0).ToArray();
            Assert.That(drops, Is.Not.Empty, "the bat dropped its wings");
            Assert.That(drops.All(d => !d.IsCollectable), "in the air");
            Assert.That(drops.All(d => d.GetComponentInChildren<SpriteRenderer>().sharedMaterial.shader.name.Contains("Unlit")), "drawn unlit, so they read on a dark floor");
            float until = Time.time + 0.25f;
            while (Time.time < until)
            {
                Assert.That(Satchel.IsEmpty, "nothing is picked up while it is in the air");
                yield return null;
            }
            yield return WaitUntil(() => drops.All(d => d == null || d.IsCollectable), 1f, "the drops to land");
            foreach (IngredientPickup drop in drops.Where(d => d != null))
            {
                Vector2 landed = drop.transform.position;
                Assert.That(Vector2.Distance(landed, died), Is.InRange(0.5f, 1.2f), "hopped clear of the body");
                Assert.That(landed.y, Is.LessThanOrEqualTo(died.y + 0.01f), "in front of it, where a corpse can't hide it");
            }
        }

        [UnityTest]
        public IEnumerator PartsOnTheFloor_SpoilToo()
        {
            yield return LoadFloor();
            IngredientPickup pickup = HarvestSystem.Instance.Drop(new IngredientStack(new IngredientItem(Part("giant_spider"), Quality.Standard), 1, 1f), k_Arena + new Vector2(5f, 3f), null);
            yield return new WaitForSeconds(2f);
            Assert.That(pickup.Freshness, Is.LessThan(1f));
            Assert.That(pickup.Freshness, Is.GreaterThan(0.99f));
        }

        [UnityTest]
        public IEnumerator FullSatchel_ShowsTheHint_InteractOpensTheSwapPrompt_AndAKeyboardSwap_DropsTheOldStackAtYourFeet()
        {
            var hints = new List<SatchelFullHint>();
            EventBus<SatchelFullHint>.Subscribe(hints.Add);
            yield return LoadFloor();
            var screen = Object.FindAnyObjectByType<SwapPromptScreen>(FindObjectsInactive.Include);
            Assert.That(screen, Is.Not.Null, "the floor has the swap prompt");
            FillSatchel(Part("bat"));
            IngredientItem incoming = new(Part("giant_spider"), Quality.Fine);
            IngredientPickup pickup = HarvestSystem.Instance.Drop(new IngredientStack(incoming, 1, 1f), k_Arena + new Vector2(2f, 0f), null);
            IngredientStack slotOne = Satchel.Slots[1];

            Teleport(Player, pickup.transform.position);
            yield return WaitUntil(() => Carrier.Blocked == pickup, 1f, "the full satchel to block the pickup");
            yield return null;
            Assert.That(hints.Last().Visible, "the satchel-full hint shows");
            Assert.That(pickup.Count, Is.EqualTo(1), "the part stays on the floor");

            yield return Tap(Key.E);
            yield return WaitUntil(() => screen.IsOpen, 1f, "Interact to open the swap prompt");
            yield return Frames(3);
            Assert.That(Time.timeScale, Is.Zero, "gameplay is paused");
            Assert.That(MenuPause.IsPaused);
            Assert.That(InputMaps.Find(InputMaps.Dungeon, DungeonActions.Move).enabled, Is.False, "no gameplay input");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(screen.Slots[0].gameObject), "the first slot is selected");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            string shown = string.Join(" | ", screen.GetComponentsInChildren<SuperTextMesh>().Select(t => t.text));
            Assert.That(shown, Does.Contain("satchel full"), shown);
            Assert.That(shown, Does.Contain("fine spider leg"), "the subtitle names the part found: " + shown);
            Assert.That(shown, Does.Contain("leave it"), shown);
            Assert.That(shown, Does.Contain("bat wing"), "the detail line names the selected slot's part: " + shown);

            yield return Tap(Key.RightArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(screen.Slots[1].gameObject), "right moves along the slots");
            yield return Tap(Key.Enter);
            yield return Frames(2);

            Assert.That(screen.IsOpen, Is.False);
            Assert.That(MenuPause.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "gameplay resumes");
            Assert.That(InputMaps.Find(InputMaps.Dungeon, DungeonActions.Move).enabled, "gameplay input is back");
            Assert.That(Satchel.Slots[1].Item, Is.EqualTo(incoming), "the new part took slot 2");
            Assert.That(Satchel.Slots[1].Count, Is.EqualTo(1));

            IngredientPickup dropped = Object.FindObjectsByType<IngredientPickup>().FirstOrDefault(p => p.Count > 0 && p.Item.Equals(slotOne.Item));
            Assert.That(dropped, Is.Not.Null, "the old stack fell to the floor");
            Assert.That(dropped.Count, Is.EqualTo(slotOne.Count), "all of it");
            Assert.That(Vector2.Distance(dropped.transform.position, Player.transform.position), Is.LessThan(1f), "at the player's feet");
            yield return new WaitForSeconds(0.5f);
            Assert.That(dropped.Count, Is.EqualTo(slotOne.Count), "it doesn't snap straight back while the player stands on it");

            // Step off and back on: it's offered again (the satchel is still full, so the hint returns).
            Teleport(Player, k_Arena + new Vector2(-4f, 0f));
            yield return new WaitForSeconds(0.3f);
            Teleport(Player, dropped.transform.position);
            yield return WaitUntil(() => Carrier.Blocked == dropped, 1f, "the dropped stack to be offered again after stepping off");
        }

        [UnityTest]
        public IEnumerator LeaveIt_OrCancel_LeavesTheSatchelAndThePartAsTheyWere()
        {
            yield return LoadFloor();
            var screen = Object.FindAnyObjectByType<SwapPromptScreen>(FindObjectsInactive.Include);
            FillSatchel(Part("bat"));
            // Freshness keeps falling while the test runs; what matters is what is in each slot.
            var before = Satchel.Slots.Select(s => (s.Item, s.Count)).ToArray();
            IngredientPickup pickup = HarvestSystem.Instance.Drop(new IngredientStack(new IngredientItem(Part("giant_spider"), Quality.Fine), 1, 1f), k_Arena + new Vector2(2f, 0f), null);
            Teleport(Player, pickup.transform.position);
            yield return WaitUntil(() => Carrier.Blocked == pickup, 1f, "the full satchel to block the pickup");

            yield return Tap(Key.E);
            yield return WaitUntil(() => screen.IsOpen, 1f, "the swap prompt");
            yield return Tap(Key.Escape);
            Assert.That(screen.IsOpen, Is.False, "Cancel closes it");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Satchel.Slots.Select(s => (s.Item, s.Count)), Is.EqualTo(before), "nothing swapped");
            Assert.That(pickup.Count, Is.EqualTo(1), "the part is still on the floor");

            yield return Tap(Key.E);
            yield return WaitUntil(() => screen.IsOpen, 1f, "the swap prompt again");
            yield return Frames(2);
            yield return Tap(Key.DownArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(screen.LeaveIt.gameObject), "down goes to Leave it");
            yield return Tap(Key.Enter);
            Assert.That(screen.IsOpen, Is.False);
            Assert.That(Satchel.Slots.Select(s => (s.Item, s.Count)), Is.EqualTo(before));
            Assert.That(pickup.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TheMouse_PicksASlotByHoveringAndClicking()
        {
            yield return LoadFloor();
            var screen = Object.FindAnyObjectByType<SwapPromptScreen>(FindObjectsInactive.Include);
            FillSatchel(Part("bat"));
            IngredientItem incoming = new(Part("giant_spider"), Quality.Fine);
            IngredientPickup pickup = HarvestSystem.Instance.Drop(new IngredientStack(incoming, 1, 1f), k_Arena + new Vector2(2f, 0f), null);
            Teleport(Player, pickup.transform.position);
            yield return WaitUntil(() => Carrier.Blocked == pickup, 1f, "the full satchel to block the pickup");
            yield return Tap(Key.E);
            yield return WaitUntil(() => screen.IsOpen, 1f, "the swap prompt");
            yield return Frames(2);

            Vector2 slot = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)screen.Slots[3].transform).TransformPoint(new Vector3(0f, 8f, 0f)));
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = slot });
            yield return Frames(3);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(screen.Slots[3].gameObject), "hovering selects the slot");
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = slot }.WithButton(MouseButton.Left));
            yield return Frames(2);
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = slot });
            yield return Frames(2);
            Assert.That(screen.IsOpen, Is.False, "clicking chooses it");
            Assert.That(Satchel.Slots[3].Item, Is.EqualTo(incoming));
        }
    }
}
