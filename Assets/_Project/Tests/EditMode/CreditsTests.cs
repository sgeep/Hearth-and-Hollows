using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Game;
using Hearthdelve.UI.Localization;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>4i-C: the credits against docs/CREDITS.md, the Credits table, and every change of place on one timing.</summary>
    public class CreditsTests
    {
        static string CreditsDoc => File.ReadAllText("docs/CREDITS.md");
        static string AllCredits => string.Join("\n", CreditsLocKeys.English.Select(e => e.english));

        /// <summary>Who the credits must name: every licence-required credit and every courtesy credit in docs/CREDITS.md.</summary>
        static readonly string[] k_Required =
        {
            "Krishna Palacio", "Minifantasy", "Portrait Generator", "Pixel_Pincher",
            "Kenney", "OwlishMedia", "Leohpaz",
            "HeatleyBros", "Quirkii", "Continue", "Coastal Market", "Otherworld",
            "Silver", "Poppy Works", "CC BY 4.0", "Itou Hiro", "leedheo", "ぶち", "adapted",
            "More Mountains", "TopDown Engine", "Nice Vibrations", "Super Text Mesh", "Kai Clavier", "Pixel Crushers", "Unity",
        };

        [Test]
        public void EveryCreditTheDocumentRequires_IsOnTheCreditsScreen()
        {
            foreach (string name in k_Required)
            {
                Assert.That(CreditsDoc, Does.Contain(name.Replace("Pixel_Pincher", "Pixel_Pincher")), $"docs/CREDITS.md names {name}");
                Assert.That(AllCredits, Does.Contain(name), $"the credits screen names {name}");
            }
        }

        [Test]
        public void HeatleyBros_HasAWorkingLink_TheSameInTheGameAndTheDocument()
        {
            Assert.That(CreditsLocKeys.HeatleyBrosUrl, Does.StartWith("https://"));
            Assert.That(CreditsDoc, Does.Contain(CreditsLocKeys.HeatleyBrosUrl), "docs/CREDITS.md records the link the game opens");
            Assert.That(CreditsLocKeys.Lines.Count(l => l.kind == CreditsLocKeys.Kind.Link), Is.EqualTo(1));
        }

        [Test]
        public void TheCreditsTable_HoldsEveryLine_InEnglish()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Loc.CreditsTable);
            Assert.That(collection, Is.Not.Null, "the Credits table exists");
            var table = collection.StringTables.First(t => t.LocaleIdentifier.Code == "en");
            foreach ((string key, string english) in CreditsLocKeys.English)
            {
                StringTableEntry entry = table.GetEntry(key);
                Assert.That(entry, Is.Not.Null, key);
                Assert.That(entry.Value, Is.EqualTo(english), key);
            }
        }

        [Test]
        public void EveryChangeOfPlace_UsesTheOneTiming()
        {
            var wrong = new List<string>();
            foreach (string path in new[] { EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene })
            {
                Scene scene = ProjectScan.Open(path);
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (var (type, fields) in PresentationUpdates.Fades)
                foreach (Component c in root.GetComponentsInChildren(type, true))
                {
                    var so = new UnityEditor.SerializedObject(c);
                    foreach (string field in fields)
                    {
                        float value = so.FindProperty(field).floatValue;
                        if (!Mathf.Approximately(value, PlaceFade.Seconds)) wrong.Add($"{scene.name}: {c.name} {type.Name}.{field} = {value}");
                    }
                }
            }
            Assert.That(wrong, Is.Empty, string.Join("\n", wrong));
        }
    }
}
