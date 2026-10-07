using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hearthdelve.Core.Animation;
using Hearthdelve.Editor;
using Hearthdelve.Story.Editor;
using Hearthdelve.UI;
using Hearthdelve.UI.Localization;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CharacterInfo = UnityEngine.CharacterInfo;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// The game's text (4c step 6): the game font (Silver) can draw every string in every locale (a glyph check against
    /// the font itself, replacing the old Latin-1 rule), English is written in Hearthdelve's lower-case style by its authors (not
    /// converted at runtime), and the scenes' text and canvases are set up to draw the pixel font crisply.
    /// </summary>
    public class TextStyleTests
    {
        static Font Font => GameFonts.Load();

        static IEnumerable<(string key, string text)> CodeEnglish =>
            LocKeys.English.Concat(TavernLocKeys.English).Concat(LoopLocKeys.English).Concat(DecorateLocKeys.English).Concat(StoryLocKeys.English).Concat(LocalizationBuilder.ContentEnglish)
                .Concat(FurnitureCatalog.English()).Concat(FurnitureLooks.English()).Concat(DialogueEnglish)
                // 4g Checkpoint B's strings.
                .Concat(CreatorLocKeys.English).Concat(OnboardingLocKeys.English).Concat(KeeperContent.English()).Concat(QuestObjectContent.English);

        /// <summary>4g: every line in the Dialogue System database, as a player reads it (its markup removed).</summary>
        static IEnumerable<(string key, string text)> DialogueEnglish =>
            StoryDialogue.English(AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue)).Select(e => ($"dialogue/{e.key}", StoryDialogue.Readable(e.english)));

        /// <summary>Every string in every locale's tables, as stored.</summary>
        static IEnumerable<(string where, string text)> TableStrings()
        {
            foreach (StringTableCollection collection in LocalizationEditorSettings.GetStringTableCollections())
            foreach (var table in collection.StringTables)
            foreach (var entry in table.Values)
                if (!string.IsNullOrEmpty(entry.Value))
                    yield return ($"{collection.TableCollectionName}/{table.LocaleIdentifier.Code}/{entry.Key}", entry.Value);
        }

        [Test]
        public void TheGameFont_IsSilver_ImportedAsAPixelFont_WithNoFallbackFonts()
        {
            Assert.That(Font, Is.Not.Null, GameFonts.FontPath);
            var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(GameFonts.FontPath);
            Assert.That(importer.fontRenderingMode, Is.EqualTo(FontRenderingMode.HintedRaster));
            Assert.That(importer.fontSize, Is.EqualTo(GameFonts.Native));
            Assert.That(importer.includeFontData, "the font travels with the build (no system font on the web)");
            Assert.That(importer.fontNames, Is.EqualTo(new[] { GameFonts.FontName }), "no other font names to fall back to");
            Assert.That(importer.fontReferences, Is.Empty, "no fallback font references");
            Assert.That(Directory.GetFiles("Assets/_Project/Fonts", "*.ttf", SearchOption.AllDirectories).Select(f => f.Replace(Path.DirectorySeparatorChar, '/')), Is.EqualTo(new[] { GameFonts.FontPath }),
                "one game font, one copy (m5x7 is gone)");
        }

        /// <summary>
        /// Every character in every string, in every locale, must be one the game font has: there is deliberately no
        /// fallback font (the web build has no system fonts), so a missing glyph would draw as nothing. Silver covers a
        /// great deal (Latin, Greek, Cyrillic, Japanese, Chinese, Korean, Thai and more), but the check is against the
        /// font itself, not a list.
        /// </summary>
        [Test]
        public void EveryString_InEveryLocale_IsDrawableInTheGameFont()
        {
            Assert.That(Font.HasCharacter('a') && Font.HasCharacter('ž') && Font.HasCharacter('Ж'), "the font's coverage can be read");
            var missing = new List<string>();
            foreach (var (where, text) in TableStrings().Concat(CodeEnglish.Select(e => ($"code/{e.key}", e.text))))
            foreach (char c in text)
                if (c != '\n' && !Font.HasCharacter(c))
                    missing.Add($"{where}: '{c}' (U+{(int)c:X4}) in \"{text}\"");
            Assert.That(missing, Is.Empty, string.Join("\n", missing.Distinct()));
        }

        /// <summary>Words that keep a capital in English: proper nouns, resource names and control labels (CLAUDE.md, Localization).</summary>
        static readonly HashSet<string> k_Capitalised = new()
        {
            "Hearth", "Hollows", "Orik", "Boog", "Phi", "Phi'rai", "Old", "Fortunate", "Five", "Kariaston", "Cellars", "Larder", "Troll", "Tally", "Ho",
            "Essence", "Renown", "Morale", "Cheer", "Delve", "Marks",
            "WASD", "E", "A", "B", "X", "Space", "F2", "F3", "F4",
        };

        /// <summary>
        /// Hearthdelve's English is lower case except proper nouns and control labels: every capitalised word in a source
        /// string must be one of those. (Not a snapshot of the text: it checks the rule, so new strings are held to it too.)
        /// </summary>
        /// <summary>
        /// Super Text Mesh replaces characters it takes for emoji (▶, ◀, ☺ and more) with emoji quads, which Hearth &amp; Hollows
        /// doesn't have: such a character simply vanishes (4g: the dialogue's ▶ pointer did). Checked with STM's own pattern.
        /// </summary>
        [Test]
        public void NoString_UsesACharacterSuperTextMeshTakesForAnEmoji()
        {
            var stm = (SuperTextMesh)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SuperTextMesh));
            var bad = new List<string>();
            foreach (var (where, text) in TableStrings().Concat(CodeEnglish.Select(e => ($"code/{e.key}", e.text))))
                if (stm.RemoveEmoji(text) != text) bad.Add($"{where}: \"{text}\"");
            Assert.That(bad, Is.Empty, string.Join("\n", bad));
        }

        [Test]
        public void EnglishSourceStrings_AreLowerCase_ExceptProperNounsAndControls()
        {
            var bad = new List<string>();
            foreach (var (key, text) in CodeEnglish)
            foreach (Match word in Regex.Matches(text, @"[A-Za-z][A-Za-z0-9']*"))
                // A possessive is its noun ("Orik's", "the Cellars' larder").
                if (char.IsUpper(word.Value[0]) && !k_Capitalised.Contains(Regex.Replace(word.Value, "'s?$", "")))
                    bad.Add($"{key}: \"{word.Value}\" in \"{text}\"");
            Assert.That(bad, Is.Empty, string.Join("\n", bad) + "\nIf one of these is a proper noun, add it to the list (and say so in review).");
        }

        /// <summary>Player-facing English uses American spelling (the owner's call, 2026-10-05): color, catalog, gray, harbor.</summary>
        static readonly Regex k_British = new(
            @"\b(\w*colour\w*|catalogue\w*|\w*(?<!f)(?<!y)our(ed|ing|ite|ites|able|ful)?|grey\w*|cheque\w*|chequer\w*|centre\w*|metre\w*|theatre\w*|" +
            @"travell\w*|cancell\w*|labell\w*|modell\w*|levell\w*|marvell\w*|fuelled|fuelling|panell\w*|signall\w*|quarrell\w*|jewellery|counsellor\w*|" +
            @"channelled|tunnelled|totalled|dialled|woollen|\w+ise|\w+ised|\w+ises|\w+ising|\w+isation|defence|offence|licence|cosy|cosier|mould\w*|" +
            @"plough\w*|draught\w*|fulfil|enrol|skilful|whilst|amongst)\b",
            RegexOptions.IgnoreCase);

        /// <summary>Words that match a British pattern but are American spellings too.</summary>
        static readonly HashSet<string> k_AmericanToo = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "your", "yours", "pour", "poured", "pouring", "pours", "hour", "hours", "our", "ours", "flour", "sour", "tour", "detour", "scour", "devour",
            "devoured", "contour", "troubadour", "rise", "rises", "arise", "wise", "otherwise", "likewise", "clockwise", "noise", "poise", "praise",
            "praised", "raise", "raised", "raises", "rising", "raising", "bruise", "bruised", "cruise", "promise", "promised", "premise", "expertise",
            "exercise", "advertise", "surprise", "surprised", "surprises", "compromise", "disguise", "disguised", "merchandise", "concise", "precise",
            "despise", "chastise", "supervise", "revise", "revised", "advise", "advised", "devise", "televise", "improvise", "comprise", "enterprise",
            "franchise", "treatise", "demise", "excise", "incise", "anise", "valise", "reprise", "chemise", "paradise", "porpoise", "tortoise",
            "turquoise", "mortise",
        };

        [Test]
        public void EnglishStrings_UseAmericanSpelling()
        {
            var bad = new List<string>();
            foreach (var (key, text) in CodeEnglish)
            foreach (Match word in k_British.Matches(text))
                if (!k_AmericanToo.Contains(word.Value))
                    bad.Add($"{key}: \"{word.Value}\" in \"{text}\"");
            Assert.That(bad, Is.Empty, string.Join("\n", bad) + "\nIf one of these is American too, add it to the list.");
        }

        [Test]
        public void ImportantStrings_HaveTheirAuthoredCasing()
        {
            var english = CodeEnglish.ToDictionary(e => e.key, e => e.text);
            Assert.That(english[TavernLocKeys.PrepOpen], Is.EqualTo("open the doors"));
            Assert.That(english[LoopLocKeys.PrepClose], Is.EqualTo("stay shut tonight"));
            Assert.That(english[TavernLocKeys.HudLastOrders], Is.EqualTo("last orders!"));
            Assert.That(english[TavernLocKeys.TicketReady], Is.EqualTo("ready"));
            Assert.That(english[TavernLocKeys.TicketStewWaiting], Is.EqualTo("stewing"));
            Assert.That(english[LoopLocKeys.MenuNewGame], Is.EqualTo("new game"));
            Assert.That(english["recipe.cellar_stew"], Is.EqualTo("cellar stew"));
            Assert.That(english["ingredient.spider_leg"], Is.EqualTo("spider leg"));
            Assert.That(english["staff.pip"], Is.EqualTo("Orik"), "a proper noun keeps its capital");
            Assert.That(english[LoopLocKeys.MenuTitle], Is.EqualTo("Hearth & Hollows"));
            Assert.That(english[TavernLocKeys.PrepValue], Is.EqualTo("{0} gold"), "gold is written in lower case, unlike the other resource names");
            Assert.That(english[LoopLocKeys.BuffMaxEssence], Is.EqualTo("+{0} max Essence"));
            Assert.That(english[TavernLocKeys.GrillPrompt], Does.Contain("gold band"), "gold the colour isn't the resource");
            Assert.That(english[TavernLocKeys.TavernControls], Does.Contain("WASD").And.Contain("E / A"), "control labels keep their casing");
        }

        /// <summary>The style is written into the strings, never applied in code (so proper nouns, control labels and other languages are safe).</summary>
        /// <summary>
        /// Silver is adapted (Tools/fonts/silver_plain_punctuation.py): its period, middle dot, comma, colon and semicolon
        /// were 3×3 plus signs, now single-pixel dots like its "!" and "?". Guards against re-importing the original.
        /// </summary>
        [Test]
        public void ThePunctuation_IsPlainDots_NotPlusSigns()
        {
            // The adapted glyphs' advances (the plus-sign originals were 5, 5, 5, 4 and 5 pixels); a re-imported
            // original font fails here. (Unity reports a hinted-raster glyph's width as its advance, so the advance is
            // what can be checked; the drawing itself is checked in the captures.)
            Font font = Font;
            font.RequestCharactersInTexture(".·,:;", GameFonts.Native);
            foreach (var (c, advance) in new[] { ('.', 3), ('·', 3), (',', 4), (':', 2), (';', 4) })
            {
                Assert.That(font.GetCharacterInfo(c, out CharacterInfo info, GameFonts.Native), $"'{c}' is in the font");
                Assert.That(info.advance, Is.EqualTo(advance), $"'{c}' is the adapted dot");
            }
        }

        [Test]
        public void NoCodeChangesTheCaseOfText()
        {
            var offenders = Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(f => Regex.IsMatch(File.ReadAllText(f), @"\.(ToLower|ToLowerInvariant|ToUpper|ToUpperInvariant)\s*\(|ToTitleCase"))
                .ToArray();
            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void PixelScale_IsTheLargestWholeNumberThatFits()
        {
            Assert.That(PixelScale.For(320, 180), Is.EqualTo(1));
            Assert.That(PixelScale.For(1920, 1080), Is.EqualTo(6));
            Assert.That(PixelScale.For(1280, 720), Is.EqualTo(4));
            Assert.That(PixelScale.For(1366, 768), Is.EqualTo(4), "never fractional");
            Assert.That(PixelScale.For(1920, 1200), Is.EqualTo(6), "the narrower fit wins");
            Assert.That(PixelScale.For(200, 100), Is.EqualTo(1), "at least one");
        }

        /// <summary>Every text in the day loop's scenes draws the game font at a pixel size, and every canvas scales by whole pixels.</summary>
        [Test]
        public void TheDayLoopsScenes_DrawEveryTextInTheGameFont_OnWholePixels()
        {
            string[] scenes = { EditorPaths.TavernScene, EditorPaths.TestFloorScene, BootBuilder.BootScene, BootBuilder.MainMenuScene };
            var problems = new List<string>();
            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        foreach (SuperTextMesh text in root.GetComponentsInChildren<SuperTextMesh>(true))
                        {
                            string name = $"{Path.GetFileNameWithoutExtension(path)}/{text.name}";
                            if (text.font != Font) problems.Add($"{name}: font {text.font}");
                            // A whole multiple of the native size (the type scale: 1×, 2×, 3×; TypographyTests checks each style).
                            if (text.size < GameFonts.Native || text.size % GameFonts.Native != 0f) problems.Add($"{name}: size {text.size}");
                            if (text.quality != GameFonts.Native || text.filterMode != FilterMode.Point) problems.Add($"{name}: quality {text.quality}, filter {text.filterMode}");
                        }
                        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace && canvas.GetComponent<PixelCanvasScaler>() == null)
                                problems.Add($"{Path.GetFileNameWithoutExtension(path)}/{canvas.name}: no whole-pixel scaling");
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
