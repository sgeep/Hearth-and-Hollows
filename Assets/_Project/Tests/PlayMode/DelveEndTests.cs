using System.Collections;
using System.Linq;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Screens;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Step 4 on the test floor: the low-Essence heartbeat, death with the Lockbox choice, extraction
    /// up the rope, and the result screen, each ending in a fresh floor.
    /// </summary>
    public class DelveEndTests : LookTestFixture
    {
        const string TestFloorScene = "Dungeon_TestFloor";

        EssenceHealth Essence => Player.GetComponent<EssenceHealth>();
        Satchel Satchel => Player.GetComponent<SatchelCarrier>().Satchel;

        [TearDown]
        public void ClearPause() => MenuPause.Clear();

        IEnumerator LoadFloor()
        {
            yield return Load(TestFloorScene);
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
                enemy.GetComponent<Character>().CharacterBrain.BrainActive = false;
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        IEnumerator Tap(params Key[] keys)
        {
            Hold(keys);
            yield return Frames(2);
            ReleaseKeys();
            yield return Frames(2);
        }

        /// <summary>Three different stacks of the bat's wing: Standard ×2, Fine ×3, Seared ×1.</summary>
        IngredientItem[] FillSome()
        {
            IngredientDefinition wing = Object.FindObjectsByType<EnemyIdentity>().First(e => e.Definition.id == "bat").Definition.harvest[0].ingredient;
            var items = new[] { new IngredientItem(wing, Quality.Standard), new IngredientItem(wing, Quality.Fine), new IngredientItem(wing, Quality.Standard, PrepState.Seared) };
            Satchel.Clear();
            Satchel.Add(items[0], 2);
            Satchel.Add(items[1], 3);
            Satchel.Add(items[2], 1);
            return items;
        }

        void DrainTo(float normalized)
        {
            EssenceMeter meter = Essence.Essence;
            while (meter.Normalized > normalized) meter.TakeDamage(0.5f);
        }

        IEnumerator Die()
        {
            Essence.Damage(Essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
        }

        IEnumerator ExpectFreshFloor(DelveResultScreen result)
        {
            Character before = Player;
            yield return Tap(Key.Enter);
            yield return WaitUntil(() => LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0 && LevelManager.Instance.Players[0] != before, 10f, "a fresh floor");
            yield return Frames(3);
            Assert.That(MenuPause.IsPaused, Is.False, "not paused any more");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(InputMaps.Find(InputMaps.Dungeon, DungeonActions.Move).enabled, "gameplay input is back");
            Assert.That(LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>().Satchel.IsEmpty, "a new delve starts with an empty satchel");
        }

        [UnityTest]
        public IEnumerator LowEssence_HeartbeatStartsBelowTheThreshold_SpeedsUp_AndStops()
        {
            yield return LoadFloor();
            var warning = Player.GetComponent<LowEssenceWarning>();
            var bar = Object.FindAnyObjectByType<EssenceBar>();
            Assert.That(warning, Is.Not.Null);
            yield return new WaitForSeconds(0.3f);
            Assert.That(warning.IsWarning, Is.False, "full Essence: no heartbeat");

            DrainTo(0.2f);
            yield return new WaitForSeconds(1.5f);
            Assert.That(warning.IsWarning, "below the low threshold: heartbeat");
            Assert.That(warning.Beats, Is.GreaterThanOrEqualTo(2), "it repeats");
            Assert.That(Haptics.Sets, Is.GreaterThan(0), "with the Heartbeat.Warning haptic");
            Assert.That(bar.IsLow, "the Essence bar shows the low state");
            float slower = warning.Interval;

            DrainTo(0.05f);
            yield return null;
            Assert.That(warning.Interval, Is.LessThan(slower), "faster as Essence falls");

            Essence.Restore(Essence.MaximumHealth);
            yield return new WaitForSeconds(0.2f);
            int beats = warning.Beats;
            yield return new WaitForSeconds(1.2f);
            Assert.That(warning.IsWarning, Is.False, "stops once Essence is back up");
            Assert.That(warning.Beats, Is.EqualTo(beats));
            Assert.That(bar.IsLow, Is.False);
        }

        [UnityTest]
        public IEnumerator Death_OpensTheLockbox_AndOnlyTheChosenStackGoesHome()
        {
            yield return LoadFloor();
            IngredientItem[] items = FillSome();
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);

            yield return Die();
            Assert.That(death.IsOpen, Is.False, "the death animation plays first");
            yield return WaitUntil(() => death.IsOpen, 3f, "the death screen");
            yield return Frames(2);
            Assert.That(MenuPause.IsPaused);
            Assert.That(InputMaps.Find(InputMaps.Dungeon, DungeonActions.Move).enabled, Is.False, "UI input only");
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(death.Slots[0].gameObject), "the first part is selected");
            Assert.That(death.Slots.Count(s => s.gameObject.activeSelf), Is.EqualTo(6));

            yield return Tap(Key.RightArrow);
            yield return Tap(Key.Enter);
            Assert.That(death.Kept, Is.EqualTo(1), "Submit on a slot puts it in the Lockbox");
            Assert.That(death.Slots[1].IsMarked, "marked by its outline");
            Assert.That(death.Slots.Count(s => s.IsMarked), Is.EqualTo(1));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(death.Confirm.gameObject), "then on to Return to the surface");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            string shown = string.Join(" | ", death.GetComponentsInChildren<SuperTextMesh>().Select(t => t.text));
            Assert.That(shown, Does.Contain("your essence fades").And.Contain("lockbox: fine bat wing ×3"), shown);

            yield return Tap(Key.Enter);
            Assert.That(death.IsOpen, Is.False);
            yield return WaitUntil(() => result.IsOpen, 1f, "the result screen");
            Assert.That(result.Report.Outcome, Is.EqualTo(DelveOutcome.Died));
            Assert.That(result.Report.Haul.Count, Is.EqualTo(1), "only the Lockbox stack");
            Assert.That(result.Report.Haul[0].Item, Is.EqualTo(items[1]));
            Assert.That(result.Report.Haul[0].Count, Is.EqualTo(3), "the whole stack");
            Assert.That(result.Report.PartsLost, Is.EqualTo(3), "the rest is lost");
            Assert.That(MenuPause.IsPaused, "still paused under the result");
            shown = string.Join(" | ", result.GetComponentsInChildren<SuperTextMesh>().Select(t => t.text));
            Assert.That(shown, Does.Contain("delve again").And.Contain("lost: 3"), shown);

            yield return ExpectFreshFloor(result);
        }

        [UnityTest]
        public IEnumerator Death_KeepNothing_LosesTheWholeHaul()
        {
            yield return LoadFloor();
            FillSome();
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            yield return Die();
            yield return WaitUntil(() => death.IsOpen, 3f, "the death screen");
            yield return Frames(2);
            yield return Tap(Key.DownArrow);
            yield return Tap(Key.LeftArrow);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(death.KeepNothingButton.gameObject));
            yield return Tap(Key.Enter);
            yield return WaitUntil(() => result.IsOpen, 1f, "the result screen");
            Assert.That(result.Report.Haul, Is.Empty);
            Assert.That(result.Report.PartsLost, Is.EqualTo(6));
            yield return ExpectFreshFloor(result);
        }

        [UnityTest]
        public IEnumerator Death_WithAnEmptySatchel_StillExplains_ThenReturns()
        {
            yield return LoadFloor();
            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            yield return Die();
            yield return WaitUntil(() => death.IsOpen, 3f, "the death screen");
            yield return Frames(2);
            Assert.That(death.Slots.All(s => !s.gameObject.activeSelf), "no slots to choose from");
            Assert.That(death.KeepNothingButton.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(death.Confirm.gameObject));
            yield return Tap(Key.Enter);
            yield return WaitUntil(() => result.IsOpen, 1f, "the result screen");
            Assert.That(result.Report.Haul, Is.Empty);
            Assert.That(result.Report.PartsLost, Is.Zero);
            yield return ExpectFreshFloor(result);
        }

        /// <summary>
        /// The step 4 playtest freeze: pushed in and out of the rope's trigger by enemies, the exit hint was
        /// toggled inside physics callbacks, and Super Text Mesh logged an error (rebuilding text there is
        /// forbidden) every physics step. Any error fails this test.
        /// </summary>
        [UnityTest]
        public IEnumerator BeingPushedInAndOutOfTheRope_LogsNothing_AndTheHintFollows()
        {
            yield return LoadFloor();
            var exit = Object.FindAnyObjectByType<DelveExit>();
            var hint = Object.FindAnyObjectByType<ExitHintView>(FindObjectsInactive.Include);
            Vector2 at = exit.transform.position;
            for (int i = 0; i < 40; i++)
            {
                Teleport(Player, i % 2 == 0 ? at : at + new Vector2(2f, 0f));
                yield return new WaitForFixedUpdate();
            }
            Teleport(Player, at);
            yield return WaitUntil(() => hint.IsShown, 1f, "the hint at the rope");
            Assert.That(hint.gameObject.activeInHierarchy, "the hint view is never switched off, only faded");
            Teleport(Player, at + new Vector2(3f, 0f));
            yield return WaitUntil(() => !hint.IsShown, 1f, "the hint to go when stepping away");
        }

        [UnityTest]
        public IEnumerator Extraction_UpTheRope_TakesTheWholeSatchelHome()
        {
            yield return LoadFloor();
            IngredientItem[] items = FillSome();
            var exit = Object.FindAnyObjectByType<DelveExit>();
            var hint = Object.FindAnyObjectByType<ExitHintView>(FindObjectsInactive.Include);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            Assert.That(exit, Is.Not.Null, "the floor has a rope out");

            Teleport(Player, exit.transform.position);
            yield return WaitUntil(() => exit.PlayerAtExit, 1f, "the player at the rope");
            yield return null;
            Assert.That(hint.IsShown, "the climb hint shows");

            yield return Tap(Key.E);
            Assert.That(exit.IsClimbing, "Interact climbs");
            Assert.That(hint.IsShown, Is.False);
            Assert.That(Essence.DrainPaused, "no Essence drain on the way out");
            float before = Essence.CurrentHealth;
            Essence.Damage(10f, Player.gameObject, 0f, 0f, Vector3.zero);
            Assert.That(Essence.CurrentHealth, Is.EqualTo(before), "and nothing can hurt the climb");

            yield return WaitUntil(() => result.IsOpen, 2f, "the result screen");
            Assert.That(result.Report.Outcome, Is.EqualTo(DelveOutcome.Extracted));
            Assert.That(result.Report.Haul.Select(s => (s.Item, s.Count)), Is.EquivalentTo(new[] { (items[0], 2), (items[1], 3), (items[2], 1) }), "the whole satchel came home");
            Assert.That(result.Report.PartsLost, Is.Zero);
            Assert.That(result.Slots.Count(s => s.gameObject.activeSelf), Is.EqualTo(3), "shown as slots");
            yield return ExpectFreshFloor(result);
        }
    }
}
