using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Screens;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Step 5: the dungeon HUD on the test floor. Essence (fill, low state, damage flash), the satchel
    /// (live contents, quality dots, freshness), the harvest feed, and a layout that never overlaps.
    /// </summary>
    public class HudTests : LookTestFixture
    {
        const string TestFloorScene = "Dungeon_TestFloor";

        EssenceHealth Essence => Player.GetComponent<EssenceHealth>();
        Satchel Satchel => Player.GetComponent<SatchelCarrier>().Satchel;

        IEnumerator LoadFloor()
        {
            yield return Load(TestFloorScene);
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
                enemy.GetComponent<Character>().CharacterBrain.BrainActive = false;
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
        }

        static IngredientDefinition Wing() =>
            Object.FindObjectsByType<EnemyIdentity>().First(e => e.Definition.id == "bat").Definition.harvest[0].ingredient;

        [UnityTest]
        public IEnumerator EssenceBar_FollowsEssence_TurnsRedAndPulsesWhenLow_AndFlashesOnAHit()
        {
            yield return LoadFloor();
            var bar = Object.FindAnyObjectByType<EssenceBar>();
            Image fill = bar.transform.Find("Fill").GetComponent<Image>();
            Assert.That(bar.transform.parent.name, Is.EqualTo("Hud"), "the HUD's bar, not the look test's placeholder");

            Essence.Damage(30f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            Assert.That(bar.Shown, Is.EqualTo(Essence.Essence.Normalized).Within(0.01f));
            Assert.That(fill.fillAmount, Is.EqualTo(bar.Shown).Within(0.01f));
            Assert.That(bar.IsFlashing, "a hit flashes the bar");
            Assert.That(fill.sprite.name, Does.Contain("Blue"));

            while (Essence.Essence.Normalized > 0.2f) Essence.Essence.TakeDamage(0.5f);
            yield return new WaitForSeconds(0.3f);
            Assert.That(bar.IsLow);
            Assert.That(fill.sprite.name, Does.Contain("Red"), "low: the red fill, not just a tint");
            Color a = fill.color;
            yield return new WaitForSecondsRealtime(0.17f);
            Assert.That(fill.color, Is.Not.EqualTo(a), "and it pulses");
        }

        [UnityTest]
        public IEnumerator SatchelHud_ShowsTheSatchelLive_WithQualityDotsAndFreshness()
        {
            yield return LoadFloor();
            var hud = Object.FindAnyObjectByType<SatchelHud>();
            Assert.That(hud.Satchel, Is.SameAs(Satchel), "bound to the delve's satchel");
            Assert.That(hud.Slots.Count(s => s.gameObject.activeSelf), Is.EqualTo(Satchel.Capacity));
            Assert.That(hud.Slots.All(s => s.Stack.IsEmpty));

            IngredientDefinition wing = Wing();
            Satchel.Add(new IngredientItem(wing, Quality.Fine), 2, 1f);
            yield return null;
            SatchelSlotView slot = hud.Slots[0];
            Assert.That(slot.Stack.Count, Is.EqualTo(2), "updates as soon as a part goes in");
            Assert.That(slot.transform.Find("Icon").GetComponent<Image>().sprite, Is.SameAs(wing.icon));
            Assert.That(slot.GetComponentsInChildren<Image>().Count(i => i.name.StartsWith("Pip") && i.enabled), Is.EqualTo(3), "three dots for Fine");

            Image freshness = slot.transform.Find("Freshness/Fill").GetComponent<Image>();
            float before = freshness.fillAmount;
            yield return new WaitForSeconds(2f);
            Assert.That(freshness.fillAmount, Is.LessThan(before), "the freshness bar shortens as the part spoils");
        }

        [UnityTest]
        public IEnumerator HarvestFeed_TellsWhatAKillProduced_ThenFades()
        {
            yield return LoadFloor();
            var feed = Object.FindAnyObjectByType<HarvestFeed>();
            EnemyIdentity bat = Object.FindObjectsByType<EnemyIdentity>().First(e => e.Definition.id == "bat");
            bat.GetComponent<EnemyPerch>()?.Detach();
            Teleport(bat, (Vector2)Player.transform.position + new Vector2(6f, 0f));
            yield return new WaitForFixedUpdate();
            Health health = bat.GetComponent<Health>();
            health.Damage(health.CurrentHealth, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;

            HarvestFeedLine top = feed.Lines[0];
            Assert.That(top.group.alpha, Is.EqualTo(1f));
            Assert.That(top.text.GetComponent<SuperTextMesh>().text, Does.Contain("clean kill!").And.Contain("bat wing ×"), "the cleaver cuts meat cleanly");
            Assert.That(top.icon.sprite, Is.SameAs(Wing().icon));
            yield return new WaitForSeconds(3.2f);
            Assert.That(feed.Lines.All(l => l.group.alpha == 0f), "faded");
        }

        /// <summary>Nothing on the HUD covers anything else at 320×180: bar, its icon, satchel, feed, prompts, the debug label.</summary>
        [UnityTest]
        public IEnumerator HudAndPrompts_NeverOverlap()
        {
            yield return LoadFloor();
            Canvas canvas = Object.FindObjectsByType<Canvas>().First(c => c.name == "UI");
            var parts = new Dictionary<string, RectTransform>
            {
                ["essence bar"] = Object.FindAnyObjectByType<EssenceBar>().GetComponent<RectTransform>(),
                ["essence icon"] = (RectTransform)canvas.transform.Find("Hud/EssenceIcon"),
                ["satchel"] = Object.FindAnyObjectByType<SatchelHud>().GetComponent<RectTransform>(),
                ["harvest feed"] = Object.FindAnyObjectByType<HarvestFeed>().GetComponent<RectTransform>(),
                ["prompts"] = (RectTransform)canvas.transform.Find("SwapPrompt/Hint"),
                ["debug label"] = (RectTransform)canvas.transform.Find("Resolution"),
            };
            foreach (var (name, rect) in parts) Assert.That(rect, Is.Not.Null, name);
            Assert.That(((RectTransform)canvas.transform.Find("ExitHint/Hint")).anchoredPosition, Is.EqualTo(parts["prompts"].anchoredPosition), "both prompts use the same spot");

            Rect Bounds(RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            }
            var names = parts.Keys.ToArray();
            for (int i = 0; i < names.Length; i++)
            for (int j = i + 1; j < names.Length; j++)
                Assert.That(Bounds(parts[names[i]]).Overlaps(Bounds(parts[names[j]])), Is.False, $"{names[i]} overlaps {names[j]}");
            Rect screen = Bounds((RectTransform)canvas.transform);
            foreach (var (name, rect) in parts)
            {
                Rect r = Bounds(rect);
                Assert.That(screen.Contains(r.min) && screen.Contains(r.max), $"{name} is on screen");
            }
        }
    }
}
