using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Editor;
using Hearthdelve.UI;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Typography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// The type scale (the UI foundation pass after 4f): Silver at whole multiples only, every style present with room for its
    /// glyphs, every text in the game's scenes styled from the one asset, and every fixed English string fitting its box at its
    /// style (headings with room to spare), so layouts never depend on a squeeze.
    /// </summary>
    public class TypographyTests
    {
        static Font Font => GameFonts.Load();

        /// <summary>The game's scenes (the test floor and the look tests are development scenes; Kariaston since the test review).</summary>
        static readonly string[] k_Scenes = ProjectScan.GameScenes;

        static Dictionary<string, string> English =>
            LocKeys.English.Concat(TavernLocKeys.English).Concat(LoopLocKeys.English).Concat(DecorateLocKeys.English).Concat(StoryLocKeys.English).Concat(LocalizationBuilder.ContentEnglish)
                .GroupBy(e => e.Item1).ToDictionary(g => g.Key, g => g.First().Item2);

        /// <summary>A string's drawn width in game pixels at 1×: the advances, less the last glyph's trailing pixel. A placeholder counts as one digit
        /// (numbers and key labels; long values are checked in PlayMode, where they're real).</summary>
        static float Width(string text)
        {
            text = Regex.Replace(text, @"\{\d+[^}]*\}", "0");
            float width = 0f;
            Font.RequestCharactersInTexture(text, SilverMetrics.NativeSize);
            foreach (char c in text)
                if (Font.GetCharacterInfo(c, out CharacterInfo info, SilverMetrics.NativeSize)) width += info.advance;
            return Mathf.Max(0f, width - 1f);
        }

        static IEnumerable<(string scene, SuperTextMesh text)> SceneTexts()
        {
            foreach (string path in k_Scenes)
            foreach (SuperTextMesh text in ProjectScan.All<SuperTextMesh>(path))
                yield return (System.IO.Path.GetFileNameWithoutExtension(path), text);
        }

        static string PathOf(Transform t)
        {
            var parts = new List<string>();
            for (; t != null && t.GetComponent<Canvas>() == null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        [Test]
        public void TheTypeScale_HasEveryStyle_AtWholeMultiplesOfSilver_WithRoomForItsGlyphs()
        {
            TypeScale scale = AssetDatabase.LoadAssetAtPath<TypeScale>(GameFonts.ScalePath);
            Assert.That(scale, Is.Not.Null, GameFonts.ScalePath);
            Assert.That(scale.Problems(), Is.Empty);
            foreach (TextStyle style in Enum.GetValues(typeof(TextStyle)))
            {
                TypeScale.Entry e = scale.Get(style);
                Assert.That(e.scale, Is.GreaterThanOrEqualTo(1), $"{style}: never under Silver's native size");
                Assert.That(e.Size, Is.EqualTo(SilverMetrics.NativeSize * e.scale), $"{style}: a whole multiple of 19");
                Assert.That(e.Size % SilverMetrics.NativeSize, Is.Zero, $"{style}: pixel-clean");
            }
        }

        /// <summary>The scale as chosen: three sizes, the 1× roles on Silver's own 12-pixel line.</summary>
        [Test]
        public void TheTypeScale_IsDisplay3x_Heading2x_AndEverythingElse1x()
        {
            TypeScale scale = GameFonts.Scale();
            Assert.That(scale.Get(TextStyle.Display).scale, Is.EqualTo(3));
            Assert.That(scale.Get(TextStyle.Heading).scale, Is.EqualTo(2));
            foreach (TextStyle style in new[] { TextStyle.Body, TextStyle.Secondary, TextStyle.Prompt })
            {
                Assert.That(scale.Get(style).scale, Is.EqualTo(1), $"{style}: nothing important is drawn under Silver's native size");
                Assert.That(scale.Get(style).linePixels, Is.EqualTo(SilverMetrics.LinePixels), style.ToString());
            }
            foreach (TypeScale.Entry e in scale.Entries)
                Assert.That(e.linePixels, Is.EqualTo(SilverMetrics.LinePixels * e.scale), $"{e.style}: a pixel between lines at every size");
        }

        [Test]
        public void ABrokenScale_IsReported()
        {
            var scale = ScriptableObject.CreateInstance<TypeScale>();
            try
            {
                var entries = (List<TypeScale.Entry>)typeof(TypeScale).GetField("m_Entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(scale);
                Assert.That(scale.Problems(), Is.Empty, "the defaults are sound");
                entries.RemoveAll(e => e.style == TextStyle.Prompt);
                entries[0] = new TypeScale.Entry(entries[0].style, 0, 12);
                entries.Add(new TypeScale.Entry(TextStyle.Heading, 2, 18));
                List<string> problems = scale.Problems();
                Assert.That(problems, Has.Some.Contains("Prompt has no entry"));
                Assert.That(problems, Has.Some.Contains("under Silver's native size"));
                Assert.That(problems, Has.Some.Contains("Heading has 2 entries"));
                Assert.That(problems, Has.Some.Contains("clips"));
            }
            finally
            {
                Object.DestroyImmediate(scale);
            }
        }

        [Test]
        public void ApplyingAStyle_SetsSuperTextMesh_ToDrawItCrisply()
        {
            var go = new GameObject("TypeProbe", typeof(RectTransform));
            try
            {
                var text = go.AddComponent<SuperTextMesh>();
                foreach (TextStyle style in Enum.GetValues(typeof(TextStyle)))
                {
                    TypeScale.Entry e = GameFonts.Scale().Get(style);
                    TypeScale.Apply(text, e);
                    Assert.That(text.size, Is.EqualTo(19f * e.scale), $"{style} size");
                    Assert.That(text.quality, Is.EqualTo(SilverMetrics.NativeSize), $"{style}: rasterised at the native size");
                    Assert.That(text.autoQuality, Is.False);
                    Assert.That(text.filterMode, Is.EqualTo(FilterMode.Point), $"{style}: point filtered");
                    Assert.That(text.size * text.lineSpacing, Is.EqualTo(e.linePixels).Within(1e-4f), $"{style}: line height in game pixels");
                    Assert.That(text.relativeBaseOffset);
                    Assert.That(text.baseOffset.y * text.size, Is.EqualTo((19f - 10f) * e.scale).Within(1e-4f), $"{style}: the first baseline 10 font pixels down");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Every text in the game's scenes has a style, and its settings are that style's (font, size, line, quality and filter), from
        /// the one asset; every screen canvas scales by whole pixels.
        /// </summary>
        [Test]
        public void EveryTextInTheGame_IsStyled_FromTheTypeScale()
        {
            TypeScale scale = GameFonts.Scale();
            var problems = new List<string>();
            var counts = new Dictionary<TextStyle, int>();
            foreach (var (scene, text) in SceneTexts())
            {
                string name = $"{scene}/{PathOf(text.transform)}";
                var styled = text.GetComponent<StyledText>();
                if (styled == null)
                {
                    problems.Add($"{name}: no style");
                    continue;
                }
                counts[styled.Style] = counts.TryGetValue(styled.Style, out int n) ? n + 1 : 1;
                TypeScale.Entry e = scale.Get(styled.Style);
                if (text.font != Font) problems.Add($"{name}: font {text.font}");
                if (text.size <= 0f || !Mathf.Approximately(text.size, e.Size)) problems.Add($"{name}: size {text.size}, {styled.Style} is {e.Size}");
                if (!Mathf.Approximately(text.size * text.lineSpacing, e.linePixels)) problems.Add($"{name}: line {text.size * text.lineSpacing}, {styled.Style} is {e.linePixels}");
                if (text.quality != SilverMetrics.NativeSize || text.filterMode != FilterMode.Point) problems.Add($"{name}: quality {text.quality}, filter {text.filterMode}");
            }
            // Every screen canvas scales by whole pixels (moved here from TextStyleTests by the test review).
            foreach (string path in k_Scenes)
            foreach (Canvas canvas in ProjectScan.All<Canvas>(path))
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace && canvas.GetComponent<PixelCanvasScaler>() == null)
                    problems.Add($"{System.IO.Path.GetFileNameWithoutExtension(path)}/{canvas.name}: no whole-pixel scaling");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(counts.GetValueOrDefault(TextStyle.Display), Is.EqualTo(1), "the game's name");
            Assert.That(counts.GetValueOrDefault(TextStyle.Heading), Is.GreaterThanOrEqualTo(6), "the screens' titles");
            Assert.That(counts.GetValueOrDefault(TextStyle.Secondary), Is.GreaterThan(0));
            Assert.That(counts.GetValueOrDefault(TextStyle.Prompt), Is.GreaterThan(0));
        }

        /// <summary>
        /// Every text with fixed English words fits its box at its style: on one line where the box is one line tall, and within the
        /// box's lines where it's taller. Headings must also leave a fifth of their width for longer languages. The words are the
        /// English table's (the test review: a dictionary built from a few key lists skipped about a hundred newer strings), and a
        /// key the text's table doesn't have is a failure, since the game would draw the key.
        /// </summary>
        [Test]
        public void EveryFixedString_FitsItsBox_AtItsStyle()
        {
            TypeScale scale = GameFonts.Scale();
            var problems = new List<string>();
            int measured = 0;
            foreach (var (scene, text) in SceneTexts())
            {
                var localized = text.GetComponent<LocalizedSuperText>();
                var styled = text.GetComponent<StyledText>();
                if (localized == null || styled == null || string.IsNullOrEmpty(localized.Key)) continue;
                if (!ProjectScan.English(localized.Table).TryGetValue(localized.Key, out string words))
                {
                    problems.Add($"{scene}/{PathOf(text.transform)}: no \"{localized.Key}\" in the {localized.Table} table");
                    continue;
                }
                if (Regex.IsMatch(words, @"^\{\d+\}$")) continue;
                measured++;
                var rect = (RectTransform)text.transform;
                float boxWidth = rect.rect.width, boxHeight = rect.rect.height;
                if (boxWidth <= 0f) continue;
                TypeScale.Entry e = scale.Get(styled.Style);
                int lines = Mathf.Max(1, Mathf.FloorToInt((boxHeight + 0.5f) / e.linePixels));
                float width = 0f;
                foreach (string line in words.Split('\n')) width = Mathf.Max(width, Width(line) * e.scale);
                int needed = words.Contains('\n') ? words.Split('\n').Length : Mathf.CeilToInt(width / boxWidth);
                string name = $"{scene}/{PathOf(text.transform)} ({localized.Key}, {styled.Style})";
                if (styled.Style == TextStyle.Display)
                {
                    // The game's name: a proper noun, not translated; it only has to fit.
                    if (width > boxWidth) problems.Add($"{name}: \"{words}\" is {width} of {boxWidth} pixels");
                }
                else if (styled.Style == TextStyle.Heading)
                {
                    if (width > boxWidth * 0.8f) problems.Add($"{name}: \"{words}\" is {width} of {boxWidth} pixels (headings keep a fifth spare)");
                }
                else if (needed > lines)
                {
                    problems.Add($"{name}: \"{words}\" needs {needed} line(s) of {boxWidth}, the box has {lines}");
                }
                else if (text.name == "Label" && text.transform.parent != null && text.transform.parent.GetComponent<UnityEngine.UI.Button>() != null && width + 4f > boxWidth)
                {
                    // A button's label keeps two pixels each side: a clear pixel inside the one-pixel frame.
                    problems.Add($"{name}: \"{words}\" is {width} in a {boxWidth}-pixel button (two pixels each side)");
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(measured, Is.GreaterThan(300), "the scenes' fixed strings were measured");
        }

        /// <summary>
        /// The harvest's note fits every real harvest on one line (the 4i-C playtest: a placeholder counts as a digit above, so a
        /// crop's name never was; "3 fine onion into the storeroom" wrapped into the frame). Real counts and names, at 1×.
        /// </summary>
        [Test]
        public void TheHarvestNote_FitsEveryCropsLine_OnOneLine()
        {
            Dictionary<string, string> english = English;
            float box = SurfaceBuilder.HarvestNoteWidth - 12f;
            var problems = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:CropDefinition"))
            {
                var crop = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Garden.CropDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (crop == null || crop.produce == null) continue;
                string name = english.TryGetValue($"ingredient.{crop.produce.id}", out string n) ? n : crop.produce.id;
                foreach (string key in new[] { GardenLocKeys.Harvested, GardenLocKeys.HarvestedFine })
                {
                    string line = string.Format(GardenLocKeys.English.First(e => e.key == key).english, crop.yield + 9, name);
                    float width = Width(line);
                    if (width > box) problems.Add($"\"{line}\" is {width} of {box} pixels");
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        /// <summary>1× text beside a heading sits on the heading's baseline (Prep's count beside "evening prep").</summary>
        [Test]
        public void TheTitleRow_SharesTheHeadingsBaseline()
        {
            Transform panel = ProjectScan.All<Transform>(EditorPaths.TavernScene)
                .First(t => t.name == "Panel" && t.parent != null && t.parent.name == "Prep");
            var title = (RectTransform)panel.Find("Title");
            var tonight = (RectTransform)panel.Find("Tonight");
            float Top(RectTransform r) => panel.InverseTransformPoint(r.TransformPoint(new Vector3(0f, r.rect.yMax, 0f))).y;
            Assert.That(title.GetComponent<StyledText>().Style, Is.EqualTo(TextStyle.Heading));
            float headingBaseline = Top(title) - 2 * SilverMetrics.BaselineFromTop;
            float countBaseline = Top(tonight) - SilverMetrics.BaselineFromTop;
            Assert.That(countBaseline, Is.EqualTo(headingBaseline).Within(0.01f));
        }

        /// <summary>The restored name: "spider-leg steaks" again (its id never changed).</summary>
        [Test]
        public void SpiderLegSteaks_HasItsFullName_Again()
        {
            Assert.That(English["recipe.spider_leg_steaks"], Is.EqualTo("spider-leg steaks"));
        }
    }
}
