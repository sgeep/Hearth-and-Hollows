using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Tavern.Scene;
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
    /// No two texts on a screen overlap, and none leaves its panel, in every state the screens are shown in, with real words and
    /// stress values (a four-figure purse, Renown both none and 100, every catalog page and piece, every Prep page). Added
    /// after the catalog's page name ran into a three-digit purse (the type pass review).
    /// </summary>
    public class TextOverlapTests : LookTestFixture
    {
        static TavernDirector Director => TavernDirector.Instance;
        static GameFlow Flow => GameFlow.Instance;
        string m_SaveDir;

        [SetUp]
        public void Prepare()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveOverlap_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void Restore()
        {
            GameFlow.SaveDirectoryOverride = null;
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static Rect Ink(SuperTextMesh text, Transform canvas)
        {
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

        /// <summary>Every visible text under <paramref name="panel"/>: overlapping another (by more than touching), or outside the panel.</summary>
        static int s_States, s_Texts;

        static void Check(Transform panel, string state, List<string> problems)
        {
            s_States++;
            Transform canvas = panel.GetComponentInParent<Canvas>().rootCanvas.transform;
            Rect bounds = Box((RectTransform)panel, canvas);
            bool fullScreen = bounds.width >= 319f;
            var inks = new List<(string name, Rect ink)>();
            foreach (SuperTextMesh text in panel.GetComponentsInChildren<SuperTextMesh>())
            {
                if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text)) continue;
                text.Rebuild();
                Rect ink = Ink(text, canvas);
                if (ink.width <= 0f || ink.height <= 0f) continue;
                s_Texts++;
                string name = $"{text.name} \"{text.text.Replace("\n", " ")}\"";
                if (!fullScreen && (ink.xMin < bounds.xMin - 0.01f || ink.xMax > bounds.xMax + 0.01f || ink.yMin < bounds.yMin - 0.01f || ink.yMax > bounds.yMax + 0.01f))
                    problems.Add($"{state}: {name} leaves its panel");
                foreach (var (other, otherInk) in inks)
                {
                    float w = Mathf.Min(ink.xMax, otherInk.xMax) - Mathf.Max(ink.xMin, otherInk.xMin);
                    float h = Mathf.Min(ink.yMax, otherInk.yMax) - Mathf.Max(ink.yMin, otherInk.yMin);
                    if (w > 0.5f && h > 0.5f) problems.Add($"{state}: {name} overlaps {other}");
                }
                inks.Add((name, ink));
            }
        }

        static void Report(List<string> problems) => Assert.That(problems.Distinct().ToList(), Is.Empty, string.Join("\n", problems.Distinct()));

        IEnumerator TavernAtPrep()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 5f, "the string tables");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrepAndTheButcherBlock_NoTextOverlaps()
        {
            yield return TavernAtPrep();
            var problems = new List<string>();
            if (Director.CanDebugFill) Director.FillStoreroom();
            yield return null;
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            Transform panel = prep.ButcherButton.transform.parent;
            for (int page = 0; page < prep.PageCount; page++)
            {
                yield return null;
                Check(panel, $"Prep page {page + 1}", problems);
                prep.NextPage();
            }
            prep.ButcherButton.onClick.Invoke();
            yield return null;
            Check(prep.Butcher.transform.Find("Butcher") ?? prep.Butcher.transform, "the Butcher Block", problems);
            prep.Butcher.Close();
            Report(problems);
        }

        [UnityTest]
        public IEnumerator DecorateTheCatalogAndTheCheck_NoTextOverlaps()
        {
            yield return TavernAtPrep();
            var problems = new List<string>();
            DecorateMode mode = DecorateMode.Instance;
            mode.Enter(AreaFurniture.Tavern);
            yield return null;
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            Transform content = screen.transform.Find("Content");
            Check(content, "Decorate", problems);

            mode.Game.AddGold(9999);
            // The check finds what it's for: the purse dragged back over the page's name is reported.
            screen.OpenStorage();
            yield return null;
            {
                var purse = (RectTransform)screen.Catalogue.transform.Find("Purse");
                Vector2 was = purse.anchoredPosition;
                Vector2 wasMin = purse.anchorMin, wasMax = purse.anchorMax, wasPivot = purse.pivot;
                purse.anchorMin = purse.anchorMax = purse.pivot = new Vector2(0f, 1f);
                purse.anchoredPosition = new Vector2(8f, -7f);
                yield return null;
                var planted = new List<string>();
                Check(screen.Catalogue.transform, "planted", planted);
                Assert.That(planted, Has.Some.Contains("overlaps"), "the check reports a purse over the page's name");
                purse.anchorMin = wasMin;
                purse.anchorMax = wasMax;
                purse.pivot = wasPivot;
                purse.anchoredPosition = was;
                screen.Catalogue.Close();
                yield return null;
            }
            s_States = s_Texts = 0;
            foreach (int renown in new[] { 0, 100 })
            {
                mode.Game.AddRenown(renown - mode.Game.Renown);
                screen.OpenStorage();
                DecorateCatalogue catalogue = screen.Catalogue;
                var seen = new HashSet<string>();
                for (int tab = 0; tab < 20; tab++)
                {
                    yield return null;
                    string title = catalogue.transform.Find("Title").GetComponent<SuperTextMesh>().text;
                    if (!seen.Add(title)) break;
                    for (int i = 0; i < Mathf.Max(1, catalogue.Count); i++)
                    {
                        if (catalogue.Count > 0) catalogue.Select(i);
                        yield return null;
                        Check(catalogue.transform, $"catalog ({title}, row {i + 1}, Renown {renown})", problems);
                    }
                    catalogue.Tab(1);
                }
                catalogue.Close();
                yield return null;
            }

            Assert.That(s_States, Is.GreaterThan(40), "every catalog page and piece, twice");
            Debug.Log($"[Overlap] the catalog: {s_States} states, {s_Texts} texts");
            screen.OpenCheck();
            yield return null;
            Check(content.Find("Check"), "the layout check", problems);
            mode.Leave();
            Report(problems);
        }

        /// <summary>Through the day loop with a four-figure purse: the delve's result, Night, Daytime and the market.</summary>
        [UnityTest]
        public IEnumerator TheDayLoopsScreens_NoTextOverlaps()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            var problems = new List<string>();
            Check(Object.FindAnyObjectByType<MainMenuScreen>().transform, "the main menu", problems);
            GameFlow.Instance.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon && LevelManager.HasInstance &&
                                         LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0, 30f, "the delve");
            Satchel satchel = LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>().Satchel;
            satchel.Add(new IngredientItem(Flow.Database.Ingredient("spider_leg"), Quality.Fine), 3);
            satchel.Add(new IngredientItem(Flow.Database.Ingredient("bat_wing"), Quality.Standard), 2);
            var result = Object.FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
            DelveRunController.Active.Extract();
            yield return WaitUntil(() => result.IsOpen, 5f, "the result");
            yield return new WaitForSecondsRealtime(0.5f);
            Check(result.transform.Find("Panel"), "the delve's result", problems);
            result.Proceed();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Night, 30f, "night");
            Flow.DebugAddGold(9900);
            yield return null;
            var night = Object.FindAnyObjectByType<NightScreen>();
            night.gameObject.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
            yield return null;
            Check(night.SleepButton.transform.parent, "Night", problems);
            night.SleepButton.onClick.Invoke();
            yield return WaitUntil(() => !Flow.IsLoading && Director != null && Director.Phase == TavernPhase.Daytime, 30f, "daytime");
            yield return null;
            var daytime = Object.FindAnyObjectByType<MorningScreen>();
            Check(daytime.MarketButton.transform.parent, "Daytime", problems);
            daytime.MarketButton.onClick.Invoke();
            yield return null;
            Check(daytime.Market.Rows[0].root.transform.parent, "the market", problems);
            Report(problems);
        }
    }
}
