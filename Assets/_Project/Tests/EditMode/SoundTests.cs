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
        /// Round 2 (2026-10-09) filled the troll's roar, the grill's sizzle, the fall's whoosh, the charge ticks and the splash; the
        /// 4i-C playtest filled the campfire. Decorate Mode's placeholders are kept on purpose and aren't gaps.
        static readonly string[] k_Left =
        {
            "PH_Brush", "PH_Bump", "PH_Burn", "PH_Heartbeat", "PH_SpillWarn",
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
            Assert.That(SoundBank.For("Feedback_Stairs", null).Key, Is.EqualTo("Stairs"), "the stairs keep the planks");
            Assert.That(SoundBank.For("Feedback_Climb", "PH_Climb").Key, Is.EqualTo("Rope"), "round 2: the rope has its own climb");
            Assert.That(SoundBank.For("Feedback_Vigor", "PH_UiTick").Key, Is.EqualTo("Soil"), "planting is heard as soil, not the interface tick");
            Assert.That(SoundBank.For("Feedback_Tick", "PH_UiTick").Key, Is.EqualTo("UiTick"));
            Assert.That(SoundBank.For("Feedback_Fall", "PH_Whoosh").Key, Is.EqualTo("Fall"), "round 2: the fall into a hole (Leohpaz, trimmed)");
            Assert.That(SoundBank.For("Feedback_Area", "PH_Whoosh"), Is.Null, "Decorate Mode's area change keeps its placeholder");
            // The 4i-C playtest's picks.
            // The kills (the owner's picks, 2026-10-09): one file each, alone, with a small pitch spread, levelled as impacts.
            Assert.That(SoundBank.For("Feedback_CleanKill", "PH_KillClean").Sources.Select(Path.GetFileName), Is.EqualTo(new[] { "CleanKill.wav" }), "the trimmed flesh");
            Assert.That(SoundBank.For("Feedback_Overkill", "PH_Thud").Sources.Select(Path.GetFileName), Is.EqualTo(new[] { "impactPunch_heavy_000.ogg" }));
            Assert.That(SoundBank.Layers.ContainsKey("Feedback_CleanKill") || SoundBank.Layers.ContainsKey("Feedback_Overkill"), Is.False, "no layered tone");
            foreach (string kill in new[] { "CleanKill", "Overkill" })
            {
                SoundBank.Family family = SoundBank.Get(kill);
                Assert.That(family.Target, Is.EqualTo(SoundBank.Kind.Impact), kill);
                Assert.That(family.MaxPitch - family.MinPitch, Is.InRange(0.04f, 0.12f), $"{kill}: a small pitch spread");
            }
            Assert.That(SoundBank.Get("EnemyDeath"), Is.Null, "the old shared death sound is gone");
            Assert.That(SoundBank.Sources.Any(s => s.Contains("Enemy_death")), Is.False);
            Assert.That(SoundBank.Get("Dodge").Sources.Select(Path.GetFileName), Is.EqualTo(new[] { "65_Dash_evade_01.wav" }));
            Assert.That(SoundBank.For("Feedback_PowerUp", "PH_PowerUp").Sources.Select(Path.GetFileName), Is.EqualTo(new[] { "maximize_006.ogg" }), "the upgrade pickup");
            Assert.That(SoundBank.Get("SatchelFull").Sources.Select(Path.GetFileName), Is.EqualTo(new[] { "Window_Close_2.wav" }));
            Assert.That(SoundBank.For("Feedback_Warm", "PH_Campfire").Key, Is.EqualTo("Campfire"));
            Assert.That(SoundBank.Get("Footsteps.Village").Sources.Count(s => s.Contains("Step_grass_")), Is.EqualTo(3), "outdoors: dirt and grass mixed");
        }

        /// <summary>Decorate Mode plays its placeholders (the owner's call after the 4i-C playtest), every moment as it was built.</summary>
        [Test]
        public void DecorateMode_KeepsItsPlaceholders()
        {
            Scene scene = ProjectScan.Open(EditorPaths.TavernScene);
            var decorate = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Hearthdelve.Tavern.Scene.DecorateFeedback>(true)).Single();
            int checkedMoments = 0;
            foreach (MMF_Player player in decorate.GetComponentsInChildren<MMF_Player>(true))
            {
                if (!SoundBank.DecoratePlaceholders.TryGetValue(player.name, out string placeholder)) continue;
                foreach (MMF_Sound sound in player.FeedbacksList.OfType<MMF_Sound>())
                {
                    Assert.That(sound.Sfx != null ? sound.Sfx.name : null, Is.EqualTo(placeholder), player.name);
                    Assert.That(sound.RandomSfx == null || sound.RandomSfx.Length == 0, Is.True, player.name);
                    checkedMoments++;
                }
            }
            Assert.That(checkedMoments, Is.EqualTo(SoundBank.DecoratePlaceholders.Count), "every moment of Decorate Mode");
        }

        /// <summary>
        /// No sound anywhere points at a file that's gone (4i-C: the keeper's footsteps still named round 1's removed clips, so
        /// walking was silent while every other check passed). Reads the prefabs, scenes and assets as text: a missing clip has no
        /// object left to inspect.
        /// </summary>
        [Test]
        public void NoSound_PointsAtAMissingFile()
        {
            var missing = new List<string>();
            var reference = new System.Text.RegularExpressions.Regex("fileID: 8300000, guid: ([0-9a-f]{32})");
            foreach (string file in Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories)
                         .Where(f => f.EndsWith(".prefab") || f.EndsWith(".unity") || f.EndsWith(".asset")))
            foreach (System.Text.RegularExpressions.Match m in reference.Matches(File.ReadAllText(file)))
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))) missing.Add($"{file}: {m.Groups[1].Value}");
            Assert.That(missing, Is.Empty);
        }

        /// <summary>
        /// From Retro Dialogue, only the four synth blips (the owner's hard rule) and the one file the owner picked themselves for
        /// a full satchel (the 4i-C playtest).
        /// </summary>
        [Test]
        public void FromRetroDialogue_OnlyTheBlips_AndTheOwnersSatchelPick()
        {
            string pack = Path.Combine(SoundBank.Leohpaz, "Leohpaz_RetroDialogue_SFX");
            var used = SoundBank.Families.SelectMany(f => f.Sources.Select(s => (f.Key, s))).Where(x => x.s.StartsWith(pack)).ToList();
            foreach (var (key, source) in used)
                Assert.That(key is "Blip" or "Blip.Low" || (key == "SatchelFull" && source.EndsWith(Path.Combine("Window", "Window_Close_2.wav"))), Is.True, $"{key}: {source}");
        }

        static IEnumerable<GameObject> PrefabRoots() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }).Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)));

        [Test]
        public void OnlyThePlaceholdersNoApprovedSoundCovers_AreStillPlaying()
        {
            var left = new SortedSet<string>(SoundSwap.PlaceholdersLeft(PrefabRoots()));
            foreach (string path in new[] { BootBuilder.BootScene, EditorPaths.TavernScene, EditorPaths.DungeonScene })
            {
                Scene scene = ProjectScan.Open(path);
                left.UnionWith(SoundSwap.PlaceholdersLeft(scene.GetRootGameObjects()));
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
                Footsteps steps = prefab.GetComponentInChildren<Footsteps>(true);
                Assert.That(steps, Is.Not.Null, path);
                // The steps are the bank's, every clip present (4i-C: they pointed at removed files and were silent).
                Assert.That(steps.VillageClips, Is.EqualTo(SoundBank.Clips(SoundBank.Get("Footsteps.Village"))), path);
                Assert.That(steps.TavernClips, Is.EqualTo(SoundBank.Clips(SoundBank.Get("Footsteps.Tavern"))), path);
                Assert.That(steps.StoneClips, Is.EqualTo(SoundBank.Clips(SoundBank.Get("Footsteps.Hollows"))), path);
                Assert.That(steps.VillageClips.Concat(steps.TavernClips).Concat(steps.StoneClips), Has.None.Null, path);
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
            Scene tavern = ProjectScan.Open(EditorPaths.TavernScene);
            PassageSounds passages = tavern.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PassageSounds>(true)).Single();
            Assert.That(passages.Door, Is.Not.Null);
            Assert.That(passages.Stairs, Is.Not.Null);
            Scene boot = ProjectScan.Open(BootBuilder.BootScene);
            Assert.That(boot.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DialogueBlips>(true)).Count(), Is.EqualTo(1));
            foreach (UiFeedback ui in boot.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<UiFeedback>(true)))
                Assert.That(ui.WaterPlayer, Is.Not.Null, "tending a bed is heard");
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
