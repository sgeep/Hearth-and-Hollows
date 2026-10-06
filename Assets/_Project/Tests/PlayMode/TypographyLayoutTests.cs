using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using Hearthdelve.UI.Typography;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// The type pass in the running tavern, with real text: the results panel at its fullest (eight lines and a note) on whole
    /// 12-pixel lines with nothing overlapping or leaving the panel; the HUD's 2× numbers up to four digits on one line; the
    /// restored "spider-leg steaks" clearing its price.
    /// </summary>
    public class TypographyLayoutTests : LookTestFixture
    {
        static TavernDirector Director => TavernDirector.Instance;

        [TearDown]
        public void Restore() => MenuPause.Clear();

        IEnumerator TavernAtPrep()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            yield return null;
        }

        static void OpenOnTheFirstDish()
        {
            Director.SetMenu(new[] { Director.Content.recipes.First(r => Hearthdelve.Tavern.Service.PrepRules.Makeable(r, Director.Storeroom) > 0) });
            Director.OpenService();
            Director.ArrivalsPaused = true;
        }

        static int Lines(SuperTextMesh text)
        {
            text.Rebuild();
            return text.lineHeights.Count - 1;
        }

        /// <summary>A text's drawn extent in its canvas's pixels.</summary>
        static Rect Ink(SuperTextMesh text, Transform canvas)
        {
            text.Rebuild();
            Vector3 a = canvas.InverseTransformPoint(text.finalTopLeftTextBounds), b = canvas.InverseTransformPoint(text.finalBottomRightTextBounds);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static Rect Box(RectTransform rect, Transform canvas)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 a = canvas.InverseTransformPoint(corners[0]), b = canvas.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        /// <summary>Eight lines (the most there can be) and the closing-early note: every line one line, none overlapping, all inside.</summary>
        [UnityTest]
        public IEnumerator TheResults_AtTheirFullest_FitOnWholeLines()
        {
            yield return TavernAtPrep();
            OpenOnTheFirstDish();
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "results");
            var results = Object.FindAnyObjectByType<EveningResultsScreen>();
            results.DoneButton.onClick.Invoke(); // finish the reveal
            yield return null;
            Transform panel = results.DoneButton.transform.parent;
            Transform canvas = panel.GetComponentInParent<Canvas>().rootCanvas.transform;
            var texts = panel.GetComponentsInChildren<LocalizedSuperText>(true);
            string[] labels = { "dishes served", "gold earned", "tips", "Renown", "special requests", "walkouts", "left, sold out", "dropped" };
            for (int i = 0; i < 8; i++)
            {
                LocalizedSuperText label = texts.First(t => t.name == $"Label{i + 1}"), line = texts.First(t => t.name == $"Line{i + 1}");
                label.gameObject.SetActive(true);
                line.gameObject.SetActive(true);
                label.GetComponent<SuperTextMesh>().text = labels[i];
                line.GetComponent<SuperTextMesh>().text = i == 4 ? "12 of 12" : "1234";
            }
            panel.Find("TakingsRow").gameObject.SetActive(true);
            LocalizedSuperText note = texts.First(t => t.name == "Note");
            note.gameObject.SetActive(true);
            note.Set(TavernLocKeys.ResultsClosedEarly);
            panel.Find("TakingsRow/Ledger").gameObject.SetActive(false);
            yield return null;

            Rect panelBox = Box((RectTransform)panel, canvas);
            var shown = panel.GetComponentsInChildren<SuperTextMesh>().Where(t => t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text)).ToList();
            var problems = new List<string>();
            var inks = new List<(string name, Rect ink)>();
            foreach (SuperTextMesh text in shown)
            {
                if (Lines(text) > 1) problems.Add($"{text.name}: \"{text.text}\" wraps");
                Rect ink = Ink(text, canvas);
                if (!panelBox.Contains(ink.min) || !panelBox.Contains(ink.max)) problems.Add($"{text.name}: \"{text.text}\" leaves the panel ({ink} in {panelBox})");
                foreach (var (other, otherInk) in inks)
                    if (ink.Overlaps(otherInk)) problems.Add($"{text.name} \"{text.text}\" overlaps {other}");
                inks.Add(($"{text.name} \"{text.text}\"", ink));
            }
            // The lines step a whole text line apart.
            for (int i = 1; i < 8; i++)
            {
                float a = Box((RectTransform)texts.First(t => t.name == $"Line{i}").transform, canvas).yMax;
                float b = Box((RectTransform)texts.First(t => t.name == $"Line{i + 1}").transform, canvas).yMax;
                if (!Mathf.Approximately(a - b, SilverMetrics.LinePixels)) problems.Add($"Line{i}→{i + 1}: {a - b} px apart");
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        /// <summary>The tavern HUD's 2× numbers: four digits, or a sign and three, stay on one line inside the margin.</summary>
        [UnityTest]
        public IEnumerator TheHudsNumbers_FitTheMargin_AtTwiceTheSize()
        {
            yield return TavernAtPrep();
            OpenOnTheFirstDish();
            yield return null;
            var hud = Object.FindAnyObjectByType<TavernHud>();
            Transform content = hud.transform.Find("Content");
            Transform canvas = hud.GetComponentInParent<Canvas>().rootCanvas.transform;
            var problems = new List<string>();
            foreach (string name in new[] { "Gold", "Tips", "Renown" })
            {
                SuperTextMesh value = content.Find(name).GetComponent<SuperTextMesh>();
                Assert.That(value.GetComponent<StyledText>().Style, Is.EqualTo(TextStyle.Heading), name);
                foreach (string sample in new[] { "1000", "+150", "-15" })
                {
                    value.text = sample;
                    Rect ink = Ink(value, canvas), box = Box((RectTransform)value.transform, canvas);
                    if (Lines(value) > 1) problems.Add($"{name}: \"{sample}\" wraps");
                    if (ink.xMax > box.xMax + 0.01f) problems.Add($"{name}: \"{sample}\" runs {ink.xMax - box.xMax} px past its box");
                }
            }
            Director.EndServiceNow();
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        /// <summary>The Checkpoint D shortening is undone: the card says "spider-leg steaks" and still clears its price by three pixels.</summary>
        [UnityTest]
        public IEnumerator SpiderLegSteaks_HasItsFullName_AndClearsItsPrice()
        {
            yield return TavernAtPrep();
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            DishCard found = null;
            for (int page = 0; page < prep.PageCount && found == null; page++)
            {
                yield return null;
                foreach (DishCard card in prep.Cards.Where(c => c.name.isActiveAndEnabled))
                    if (card.name.GetComponent<SuperTextMesh>().text == "spider-leg steaks") found = card;
                if (found == null) prep.NextPage();
            }
            Assert.That(found, Is.Not.Null, "a card named \"spider-leg steaks\"");
            DishCard c2 = found;
            Transform canvas = prep.GetComponentInParent<Canvas>().rootCanvas.transform;
            float gap = Ink(c2.detail.GetComponent<SuperTextMesh>(), canvas).xMin - Ink(c2.name.GetComponent<SuperTextMesh>(), canvas).xMax;
            Assert.That(gap, Is.GreaterThanOrEqualTo(3f), $"\"spider-leg steaks\" to \"{c2.detail.GetComponent<SuperTextMesh>().text}\"");
            Assert.That(Lines(c2.name.GetComponent<SuperTextMesh>()), Is.EqualTo(1));
        }
    }
}
