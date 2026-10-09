using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Story.Presentation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Screens;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>4i-C: the approved sounds imported (and only those), swapped in, the feedback audit's gaps filled, and recorded.</summary>
    public class SoundTests
    {
        /// <summary>The placeholders the approved sounds don't replace (the listening list's gaps and what it didn't cover).</summary>
        /// Round 2 (2026-10-09) filled the troll's roar and the grill's sizzle; the fall's whoosh, the charge tick and the splash wait
        /// for their Leohpaz packs' licences, the campfire for its file.
        static readonly string[] k_Left =
        {
            "PH_Brush", "PH_Bump", "PH_Burn", "PH_Campfire", "PH_CampfireLow", "PH_ChargeTick", "PH_Heartbeat",
            "PH_SpillWarn", "PH_Splash", "PH_Whoosh",
        };

        [Test]
        public void EveryApprovedFile_IsImported_AndNothingElseIs()
        {
            var expected = new HashSet<string>(SoundBank.Sources.Select(s => SoundBank.AssetPath(s).Replace('\\', '/')));
            foreach (string path in expected)
                Assert.That(AssetDatabase.LoadAssetAtPath<AudioClip>(path), Is.Not.Null, path);
            var imported = Directory.GetFiles(SoundBank.Folder, "*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/')).ToList();
            Assert.That(imported, Is.EquivalentTo(expected), "only the owner-approved files are in the game");
        }

        [Test]
        public void TheSounds_AreRecordedInTheCreditsAndThirdPartyDocs()
        {
            string credits = File.ReadAllText("docs/CREDITS.md"), third = File.ReadAllText("docs/THIRD_PARTY.md");
            foreach (string name in new[] { "Kenney", "OwlishMedia", "Leohpaz" })
            {
                Assert.That(credits, Does.Contain(name));
                Assert.That(third, Does.Contain(name));
            }
            Assert.That(third, Does.Contain("CC0"));
            int leohpaz = SoundBank.Sources.Count(SoundBank.IsLeohpaz);
            Assert.That(third, Does.Contain($"{SoundBank.Sources.Count() - leohpaz} files"), "the CC0 row's count is the count imported");
            Assert.That(third, Does.Contain($"{leohpaz} files"), "the Leohpaz row's count is the count imported");
        }

        [Test]
        public void TheFamilies_TheOwnerChose_AreTheOnesUsed()
        {
            // Family 15 is option B (the owner's choice): Kenney's plank impacts for the stairs, the hatch and the rope.
            Assert.That(SoundBank.Get("Stairs").Sources.All(s => s.Contains("impactPlank_medium_")), Is.True);
            Assert.That(SoundBank.For("Feedback_Climb", "PH_Climb").Key, Is.EqualTo("Stairs"));
            Assert.That(SoundBank.For("Feedback_Vigor", "PH_UiTick").Key, Is.EqualTo("Soil"), "planting is heard as soil, not the interface tick");
            Assert.That(SoundBank.For("Feedback_Tick", "PH_UiTick").Key, Is.EqualTo("UiTick"));
            Assert.That(SoundBank.For("Feedback_Fall", "PH_Whoosh"), Is.Null, "no air swoosh in the libraries: the placeholder stays");
        }

        static IEnumerable<GameObject> PrefabRoots() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }).Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)));

        [Test]
        public void OnlyThePlaceholdersNoApprovedSoundCovers_AreStillPlaying()
        {
            var left = new SortedSet<string>(SoundSwap.PlaceholdersLeft(PrefabRoots()));
            foreach (string path in new[] { BootBuilder.BootScene, EditorPaths.TavernScene, EditorPaths.DungeonScene })
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    left.UnionWith(SoundSwap.PlaceholdersLeft(scene.GetRootGameObjects()));
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Assert.That(left.Except(k_Left), Is.Empty, "a placeholder the bank replaces is still in use");
            Assert.That(left.Where(p => SoundBank.ByPlaceholder.ContainsKey(p)), Is.Empty);
        }

        [Test]
        public void TheKeeper_HasFootsteps_TheDodgeStaysUnfelt_AndEveryMeleeWeaponHasASwing()
        {
            foreach (string path in new[] { LookTestContent.PlayerPrefab, LookTestContent.TavernPlayerPrefab })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab.GetComponentInChildren<Footsteps>(true), Is.Not.Null, path);
                MMF_Player dodge = prefab.transform.Find("Feedback_Dodge").GetComponent<MMF_Player>();
                Assert.That(dodge.FeedbacksList.Any(f => f.GetType().Name.Contains("Haptic")), Is.False, $"{path}: no rumble on the dodge (4b: frequent, it would numb the hits)");
            }
            foreach (GameObject root in PrefabRoots())
            foreach (Weapon weapon in root.GetComponentsInChildren<Weapon>(true))
                if (weapon is MeleeWeapon)
                    Assert.That(weapon.WeaponUsedMMFeedback, Is.Not.Null, $"{root.name}/{weapon.name}: its swing is heard");
        }

        [Test]
        public void DoorsStairsBlipsAndWatering_AreInTheirScenes_AndTheVoicesAreSet()
        {
            Scene tavern = EditorSceneManager.OpenScene(EditorPaths.TavernScene, OpenSceneMode.Additive);
            try
            {
                PassageSounds passages = tavern.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PassageSounds>(true)).Single();
                Assert.That(passages.Door, Is.Not.Null);
                Assert.That(passages.Stairs, Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(tavern, true);
            }
            Scene boot = EditorSceneManager.OpenScene(BootBuilder.BootScene, OpenSceneMode.Additive);
            try
            {
                Assert.That(boot.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DialogueBlips>(true)).Count(), Is.EqualTo(1));
                foreach (UiFeedback ui in boot.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UiFeedback>(true)))
                    Assert.That(ui.WaterPlayer, Is.Not.Null, "tending a bed is heard");
            }
            finally
            {
                EditorSceneManager.CloseScene(boot, true);
            }
            var characters = AssetDatabase.FindAssets("t:CharacterDefinition").Select(g => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            foreach (var (id, semitones, low) in SoundSwap.Voices)
            {
                CharacterDefinition c = characters.Single(x => x.id == id);
                Assert.That(c.voicePitch, Is.EqualTo(SoundSwap.Pitch(semitones)).Within(1e-4f), id);
                Assert.That(c.lowVoice, Is.EqualTo(low), id);
            }
        }

        /// <summary>The owner's hard rule (round 2): blips only from the Sawtooth, Sine, Square and Triangular synth blips.</summary>
        [Test]
        public void TheBlips_AreOnlySawtoothSineSquareOrTriangularSynthBlips()
        {
            string folder = Path.Combine(SoundBank.Leohpaz, "Leohpaz_RetroDialogue_SFX", "RetroDialogue_SFX", "Synth_Blips");
            foreach (string key in new[] { "Blip", "Blip.Low" })
            foreach (string source in SoundBank.Get(key).Sources)
            {
                Assert.That(Path.GetDirectoryName(source), Is.EqualTo(folder), source);
                string name = Path.GetFileName(source);
                Assert.That(new[] { "Sawtooth_", "Sine_", "Square_", "Triangular_" }.Any(name.StartsWith), Is.True, $"{name}: not an allowed synth blip");
                Assert.That(name, Does.Not.StartWith("Bubble"), name);
            }
        }

        [Test]
        public void EveryVoice_IsAWholeNumberOfSemitones_AndFootstepsSitUnderTheBlips()
        {
            foreach (var (id, semitones, _) in SoundSwap.Voices)
            {
                float steps = 12f * Mathf.Log(SoundSwap.Pitch(semitones), 2f);
                Assert.That(steps, Is.EqualTo(Mathf.Round(steps)).Within(1e-3f), id);
            }
            Assert.That(SoundBank.Get("Blip").Target, Is.EqualTo(SoundBank.Kind.Quiet));
            Assert.That(SoundBank.TargetDb(SoundBank.Kind.Footsteps), Is.EqualTo(SoundBank.TargetDb(SoundBank.Kind.Quiet) - 6f).Within(1e-3f));
        }

        [Test]
        public void ABlip_SoundsEveryFewLetters_NeverTooClose()
        {
            var due = Enumerable.Range(1, 9).Where(i => BlipRules.Due(i, 3, 1f, 0.05f)).ToList();
            Assert.That(due, Is.EqualTo(new[] { 1, 4, 7 }), "the first letter, then every third");
            Assert.That(BlipRules.Due(4, 3, 0.01f, 0.05f), Is.False, "never closer than the gap (a hurried line)");
            Assert.That(BlipRules.Due(0, 3, 1f, 0.05f), Is.False);
        }
    }
}
