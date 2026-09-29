using System;
using System.Collections;
using Hearthdelve.Core.Events;
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
        public IEnumerator EssenceDepleted_ShowsDeathScreen_KeepsChosenPart_AndRestarts()
        {
            yield return LoadScene();
            yield return null;

            var part = ScriptableObject.CreateInstance<IngredientDefinition>();
            part.id = "test_part";
            var kept = new IngredientItem(part, Quality.Fine);
            DelveRunController.Active.Satchel.Add(kept, 3);

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

            Assert.That(PersistentStash.Items, Has.Count.EqualTo(1));
            Assert.That(PersistentStash.Items[0], Is.EqualTo(kept));
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
