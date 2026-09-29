using System;
using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// End-to-end run of the generated CombatGreybox scene. Any error or exception logged
    /// while it runs fails the test (Unity Test Framework default).
    /// </summary>
    public class GreyboxSceneTests
    {
        const string k_Scene = "CombatGreybox";

        [SetUp]
        public void SetUp() => PersistentStash.Clear();

        [UnityTest]
        public IEnumerator Scene_Loads_Runs_AndLocalizesHud()
        {
            yield return LoadScene();
            yield return new WaitForSeconds(1.5f);

            Assert.That(Object.FindFirstObjectByType<PlayerController>(), Is.Not.Null);
            Assert.That(DelveRunController.Active, Is.Not.Null);

            var hudRoot = Object.FindFirstObjectByType<DungeonHud>().GetComponent<UIDocument>().rootVisualElement;
            Assert.That(hudRoot.Q<Label>("essence-label").text, Is.EqualTo("Essence"), "HUD text should come from the UI string table");
            Assert.That(hudRoot.Q("satchel-slots").childCount, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator Scene_PlayerAndEnemies_StandOnLevelGeometry()
        {
            yield return LoadScene();
            yield return new WaitForSeconds(1f);

            var player = Object.FindFirstObjectByType<PlayerController>();
            Assert.That(player.Mover.Contacts.Grounded, Is.True, "player must land on the tilemap floor, not fall through");
            Assert.That(player.Mover.Position.y, Is.EqualTo(0f).Within(0.05f));

            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                Assert.That(enemy.GetComponent<KinematicMover2D>().Contacts.Grounded, Is.True, $"{enemy.name} fell through the level");
        }

        [UnityTest]
        public IEnumerator Scene_PlayerRunsAcrossTileSeams_WithoutSnagging()
        {
            yield return LoadScene();
            yield return new WaitForSeconds(0.5f);

            var player = Object.FindFirstObjectByType<PlayerController>();
            float startX = player.Mover.Position.x;
            player.GetComponent<PlayerInputReader>().Override = new PlayerFrameInput { Move = Vector2.right };
            yield return new WaitForSeconds(0.8f);
            player.GetComponent<PlayerInputReader>().Override = null;

            Assert.That(player.Mover.Position.x - startX, Is.GreaterThan(5f), "running should cover ground freely");
            Assert.That(player.Mover.Position.y, Is.EqualTo(0f).Within(0.05f));
            Assert.That(player.Mover.Contacts.Grounded, Is.True);
        }

        [UnityTest]
        public IEnumerator CleanKillCue_ShowsOnWeakenedRat_NotOnSlime()
        {
            yield return LoadScene();
            yield return null;

            var rat = Object.FindFirstObjectByType<RatBehaviour>();
            var slime = Object.FindFirstObjectByType<SlimeBehaviour>();
            yield return null;
            Assert.That(rat.InCleanKillRange, Is.False, "full-health rat isn't finishable by a light hit");

            // Bring both to 6 HP: the Cleaver's light hit (8) would finish them with little overkill.
            Weaken(rat, 6f);
            Weaken(slime, 6f);
            yield return null;

            Assert.That(rat.InCleanKillRange, Is.True);
            Assert.That(rat.transform.Find("CleanKillIcon").GetComponent<SpriteRenderer>().enabled, Is.True);
            Assert.That(slime.InCleanKillRange, Is.False, "Cleaver earns no Clean Kill bonus on slime parts");
        }

        static void Weaken(EnemyController enemy, float leaveHealth)
        {
            var health = enemy.GetComponent<EnemyHealth>();
            health.ReceiveHit(new DamageInfo { Amount = health.Current - leaveHealth });
        }

        [UnityTest]
        public IEnumerator EssenceDepleted_ShowsDeathScreen_KeepsChosenPart_AndRestarts()
        {
            yield return LoadScene();
            yield return null;

            var part = ScriptableObject.CreateInstance<IngredientDefinition>();
            part.id = "test_part";
            var kept = new IngredientItem(part, Quality.Fine);
            Assert.That(DelveRunController.Active.Satchel.Add(kept, 3), Is.EqualTo(0));

            Action<int> choose = null;
            void Capture(DeathScreenRequested e) => choose = e.OnChosen;
            EventBus<DeathScreenRequested>.Subscribe(Capture);

            var vitals = Object.FindFirstObjectByType<PlayerVitals>();
            vitals.Essence.TakeDamage(10000f);
            yield return null;

            Assert.That(choose, Is.Not.Null, "defeat should request the death screen");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "gameplay pauses while choosing");
            var screen = Object.FindFirstObjectByType<DeathScreen>().GetComponent<UIDocument>().rootVisualElement.Q("screen");
            Assert.That(screen.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(screen.Q("slot-row").childCount, Is.EqualTo(6));

            EventBus<DeathScreenRequested>.Unsubscribe(Capture);
            choose(0); // what the Confirm button does with slot 0 selected

            // Level reloads with a fresh run.
            float timeout = Time.realtimeSinceStartup + 5f;
            while ((DelveRunController.Active == null || !DelveRunController.Active.Satchel.IsEmpty || Time.timeScale == 0f)
                   && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(PersistentStash.Stacks, Has.Count.EqualTo(1));
            Assert.That(PersistentStash.Stacks[0].Item, Is.EqualTo(kept));
            Assert.That(PersistentStash.Stacks[0].Count, Is.EqualTo(3), "the whole stack is banked");
            Assert.That(DelveRunController.Active.Satchel.IsEmpty, "the rest of the haul is gone");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Object.Destroy(part);
        }

        static IEnumerator LoadScene()
        {
            var op = SceneManager.LoadSceneAsync(k_Scene, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
        }
    }
}
