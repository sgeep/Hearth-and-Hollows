using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Screens;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-C: the approved sounds put to work in place (the generators do the same when they build, through the same helpers):
    /// imports the approved files (<see cref="SoundBank.Import"/>); every feedback still playing a placeholder the bank replaces
    /// gets its family; and the feedback audit's gaps are filled: the keeper's footsteps, a light rumble on the dodge, the
    /// cleaver's swing, doors and stairs, dialogue blips in each speaker's voice, watering a bed, and the tap's pour loop.
    /// Placeholders the bank doesn't replace stay (the listening list's gaps). Running it again changes nothing.
    /// </summary>
    public static class SoundSwap
    {
        static readonly string[] k_Scenes =
        {
            BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene, EditorPaths.TestFloorScene,
        };

        /// <summary>
        /// Dialogue voices (round 2, 2026-10-09): each a whole number of semitones from the blip's own note, so the pitched synth
        /// blips stay in tune (snapped from round 1's 0.78–1.25; Grim a step under Orik to keep them apart), and the deep voices in
        /// the low register. Set where the value is still the default or round 1's (an author's edit, once).
        /// </summary>
        public static readonly (string id, int semitones, bool low)[] Voices =
        {
            (CharacterIds.Orik, -3, true), (CharacterIds.Grim, -5, true), (CharacterIds.Boog, 3, false), (CharacterIds.Ogrin, 4, false),
            (CharacterIds.Maximo, -1, false), (CharacterIds.Kaloren, -4, true), (CharacterIds.Bart, -2, false), (CharacterIds.Gimp, 1, false),
        };

        /// <summary>Round 1's pitches, replaced once by <see cref="Voices"/>.</summary>
        static readonly Dictionary<string, float> k_RoundOne = new()
        {
            [CharacterIds.Orik] = 0.85f, [CharacterIds.Grim] = 0.82f, [CharacterIds.Boog] = 1.18f, [CharacterIds.Ogrin] = 1.25f,
            [CharacterIds.Maximo] = 0.95f, [CharacterIds.Kaloren] = 0.78f, [CharacterIds.Bart] = 0.9f, [CharacterIds.Gimp] = 1.05f,
        };

        public static float Pitch(int semitones) => Mathf.Pow(2f, semitones / 12f);

        [MenuItem("Hearthdelve/Generate/Swap In the Approved Sounds", priority = 35)]
        public static void ApplyMenu() => Debug.Log($"[Hearthdelve] Sound swap: {Apply()}");

        public static void ApplyBatch()
        {
            try
            {
                ApplyMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static string Apply()
        {
            int imported = SoundBank.Import();
            int swapped = 0, equipped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = Swap(root) + Relevel(root);
                    int e = 0;
                    if (path == LookTestContent.PlayerPrefab) e += EquipKeeper(root, hollows: true);
                    if (path == LookTestContent.TavernPlayerPrefab) e += EquipKeeper(root, hollows: false);
                    e += EquipSwing(root);
                    if (n + e > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    swapped += n;
                    equipped += e;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            foreach (string scenePath in k_Scenes.Where(System.IO.File.Exists))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int n = 0, e = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    n += Swap(root) + Relevel(root);
                    foreach (UiFeedback ui in root.GetComponentsInChildren<UiFeedback>(true)) e += EquipWater(ui);
                    foreach (TavernFeedback tavern in root.GetComponentsInChildren<TavernFeedback>(true)) e += EquipPassages(tavern.transform);
                    if (EquipScene != null) e += EquipScene(root);
                }
                if (n + e > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                swapped += n;
                equipped += e;
            }
            int voices = SetVoices();
            AssetDatabase.SaveAssets();
            return $"{imported} file(s) imported, {swapped} sound(s) swapped, {equipped} addition(s), {voices} voice(s) set.";
        }

        /// <summary>Every feedback under <paramref name="root"/> still on a placeholder the bank replaces, and the pour loop.</summary>
        public static int Swap(GameObject root)
        {
            int n = EquipLayers(root);
            foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
            {
                if (player.FeedbacksList == null) continue;
                bool changed = false;
                foreach (MMF_Sound sound in player.FeedbacksList.OfType<MMF_Sound>())
                {
                    if (sound.Sfx == null || !sound.Sfx.name.StartsWith("PH_")) continue;
                    if (!SoundBank.Apply(sound, SoundBank.For(player.name, sound.Sfx.name))) continue;
                    changed = true;
                    n++;
                }
                if (changed) EditorUtility.SetDirty(player);
            }
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.clip == null || !SoundBank.ByPlaceholder.TryGetValue(source.clip.name, out string key)) continue;
                AudioClip[] clips = SoundBank.Clips(SoundBank.Get(key));
                if (clips.Length == 0) continue;
                source.clip = clips[0];
                EditorUtility.SetDirty(source);
                n++;
            }
            return n;
        }

        /// <summary>Round 2: a second sound on the feedbacks <see cref="SoundBank.Layers"/> names (the troll's voice over his impacts).</summary>
        public static int EquipLayers(GameObject root)
        {
            int n = 0;
            foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
            {
                if (player.FeedbacksList == null || !SoundBank.Layers.TryGetValue(player.name, out string key)) continue;
                if (player.FeedbacksList.OfType<MMF_Sound>().Any(s => s.Label == $"Sound ({key})")) continue;
                var layer = new MMF_Sound { PlayMethod = MMF_Sound.PlayMethods.Cached, SfxAudioMixerGroup = AudioMixerBuilder.Effects };
                if (!SoundBank.Apply(layer, SoundBank.Get(key))) continue;
                player.AddFeedback(layer);
                EditorUtility.SetDirty(player);
                n++;
            }
            return n;
        }

        /// <summary>
        /// The balance pass: every approved sound under <paramref name="root"/> at its family's measured volume (and the footsteps
        /// and blips). Only what differs changes, so it runs again harmlessly.
        /// </summary>
        public static int Relevel(GameObject root)
        {
            int n = 0;
            foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
            {
                if (player.FeedbacksList == null) continue;
                foreach (MMF_Sound sound in player.FeedbacksList.OfType<MMF_Sound>())
                {
                    if (sound.Label == null || !sound.Label.StartsWith("Sound (")) continue;
                    // A feedback named in the bank follows its name (a moment given its own family later, like the rope); otherwise its label.
                    string key = sound.Label.Substring(7).TrimEnd(')');
                    if (SoundBank.ByPlayer.TryGetValue(player.name, out string named) && !SoundBank.Layers.ContainsValue(key)) key = named;
                    SoundBank.Family family = SoundBank.Get(key);
                    if (family == null) continue;
                    // The family's clips changed (an edited copy replaced a clip): put them on again.
                    AudioClip[] clips = SoundBank.Clips(family);
                    AudioClip[] current = sound.Sfx != null ? new[] { sound.Sfx } : sound.RandomSfx ?? new AudioClip[0];
                    if (!current.SequenceEqual(clips))
                    {
                        SoundBank.Apply(sound, family);
                        EditorUtility.SetDirty(player);
                        n++;
                    }
                    float volume = SoundBank.Volume(family);
                    if (Mathf.Approximately(sound.MinVolume, volume) && Mathf.Approximately(sound.MaxVolume, volume)) continue;
                    sound.MinVolume = sound.MaxVolume = volume;
                    EditorUtility.SetDirty(player);
                    n++;
                }
            }
            foreach (Footsteps steps in root.GetComponentsInChildren<Footsteps>(true))
            {
                float village = SoundBank.Volume(SoundBank.Get("Footsteps.Village")), tavern = SoundBank.Volume(SoundBank.Get("Footsteps.Tavern")),
                    stone = SoundBank.Volume(SoundBank.Get("Footsteps.Hollows"));
                if (Mathf.Approximately(steps.Volume, village) && Mathf.Approximately(steps.TavernVolume, tavern) && Mathf.Approximately(steps.StoneVolume, stone)) continue;
                steps.Volume = village;
                steps.TavernVolume = tavern;
                steps.StoneVolume = stone;
                EditorUtility.SetDirty(steps);
                n++;
            }
            if (RelevelScene != null) n += RelevelScene(root);
            return n;
        }

        /// <summary>The Story editor's part of the balance pass (the blips).</summary>
        public static Func<GameObject, int> RelevelScene;

        static MMF_Player Player(Transform parent, string name, string family, HapticPattern haptic = null)
        {
            MMF_Player player = LookTestContent.Feedback(parent, name, null, 0f, SoundBank.Clips(SoundBank.Get(family)).FirstOrDefault(), haptic);
            foreach (MMF_Sound sound in player.FeedbacksList.OfType<MMF_Sound>()) SoundBank.Apply(sound, SoundBank.Get(family));
            return player;
        }

        static AudioSource Source(GameObject go)
        {
            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null) source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = AudioMixerBuilder.Effects;
            return source;
        }

        /// <summary>
        /// The keeper: footsteps on the walk's footfalls. The dodge stays without a rumble (4b's feedback pass: it's frequent and
        /// would numb the hits); a rumble an earlier run of this pass added is taken off again.
        /// </summary>
        public static int EquipKeeper(GameObject root, bool hollows)
        {
            int n = 0;
            if (root.GetComponentInChildren<Footsteps>(true) == null)
            {
                var go = new GameObject("Footsteps");
                go.transform.SetParent(root.transform, false);
                Source(go);
                go.AddComponent<Footsteps>().Configure(root.GetComponentInChildren<CharacterSpriteAnimator>(true), hollows,
                    SoundBank.Clips(SoundBank.Get("Footsteps.Village")), SoundBank.Clips(SoundBank.Get("Footsteps.Tavern")),
                    SoundBank.Clips(SoundBank.Get("Footsteps.Hollows")));
                n++;
            }
            Transform dodge = root.transform.Find("Feedback_Dodge");
            var player = dodge != null ? dodge.GetComponent<MMF_Player>() : null;
            if (player != null && player.FeedbacksList.OfType<MMF_HapticPattern>().Any())
            {
                player.FeedbacksList.RemoveAll(f => f is MMF_HapticPattern);
                EditorUtility.SetDirty(player);
                n++;
            }
            return n;
        }

        /// <summary>Every melee weapon under <paramref name="root"/>: the swing heard as it's used, hit or miss.</summary>
        public static int EquipSwing(GameObject root)
        {
            int n = 0;
            foreach (Weapon weapon in root.GetComponentsInChildren<Weapon>(true))
            {
                if (weapon is not MeleeWeapon || weapon.WeaponUsedMMFeedback != null) continue;
                weapon.WeaponUsedMMFeedback = Player(weapon.transform, "Feedback_Swing", "Swing");
                EditorUtility.SetDirty(weapon);
                n++;
            }
            return n;
        }

        /// <summary>Tally Ho!'s doors and stairs heard as the screen covers.</summary>
        public static int EquipPassages(Transform tavernFeedback)
        {
            if (tavernFeedback.GetComponentInChildren<PassageSounds>(true) != null) return 0;
            var go = new GameObject("Passages");
            go.transform.SetParent(tavernFeedback, false);
            go.AddComponent<PassageSounds>().Configure(Player(go.transform, "Feedback_Door", "Door"), Player(go.transform, "Feedback_Stairs", "Stairs"));
            return 1;
        }

        /// <summary>
        /// Additions the Story editor assembly makes in a scene (the dialogue box's blips: this assembly can't see the box). Set by
        /// <c>Hearthdelve.Story.Editor.StorySounds</c> when the editor loads.
        /// </summary>
        public static Func<GameObject, int> EquipScene;

        /// <summary>An audio source on Effects for <paramref name="go"/> (shared with the Story editor's additions).</summary>
        public static AudioSource EffectsSource(GameObject go) => Source(go);

        /// <summary>Tending a bed: water, and a light tap.</summary>
        public static int EquipWater(UiFeedback ui)
        {
            if (ui.WaterPlayer != null) return 0;
            ui.ConfigureWater(Player(ui.transform, "Feedback_Water", "Water", LookTestContent.Pattern(HapticIds.TapLight)));
            EditorUtility.SetDirty(ui);
            return 1;
        }

        static int SetVoices()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterDefinition"))
            {
                var c = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (c == null) continue;
                foreach (var (id, semitones, low) in Voices)
                {
                    if (c.id != id) continue;
                    float pitch = Pitch(semitones);
                    bool unchanged = Mathf.Approximately(c.voicePitch, 1f) || (k_RoundOne.TryGetValue(id, out float old) && Mathf.Approximately(c.voicePitch, old));
                    if (!unchanged || (Mathf.Approximately(c.voicePitch, pitch) && c.lowVoice == low)) continue;
                    c.voicePitch = pitch;
                    c.lowVoice = low;
                    EditorUtility.SetDirty(c);
                    n++;
                }
            }
            return n;
        }

        /// <summary>Tests: placeholders still in use (the gaps), by name.</summary>
        public static SortedSet<string> PlaceholdersLeft(IEnumerable<GameObject> roots)
        {
            var left = new SortedSet<string>();
            foreach (GameObject root in roots)
            {
                foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
                    if (player.FeedbacksList != null)
                        foreach (MMF_Sound s in player.FeedbacksList.OfType<MMF_Sound>())
                            if (s.Sfx != null && s.Sfx.name.StartsWith("PH_")) left.Add(s.Sfx.name);
                foreach (AudioSource a in root.GetComponentsInChildren<AudioSource>(true))
                    if (a.clip != null && a.clip.name.StartsWith("PH_")) left.Add(a.clip.name);
            }
            return left;
        }
    }
}
