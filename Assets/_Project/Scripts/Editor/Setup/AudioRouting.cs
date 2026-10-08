using System;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-B: every sound the generators made before the mixer existed, routed to the mixer's Effects group, in place: each prefab
    /// under <c>Prefabs</c> and the game's scenes (the 4a look scenes stay untouched as baselines). It only fills a sound's empty
    /// output; anything already routed is left alone, so running it again changes nothing. The generators route new sounds
    /// themselves (<see cref="LookTestContent"/>, <see cref="TavernFeedbackContent"/>).
    /// </summary>
    public static class AudioRouting
    {
        static readonly string[] k_Scenes =
        {
            BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene, EditorPaths.TestFloorScene,
        };

        [MenuItem("Hearthdelve/Generate/Route Sounds to the Mixer", priority = 32)]
        public static void RouteAllMenu() => Debug.Log($"[Hearthdelve] Routed {RouteAll()} sound(s) to the mixer.");

        public static void RouteAllBatch()
        {
            try
            {
                RouteAllMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static int RouteAll()
        {
            AudioMixerGroup effects = AudioMixerBuilder.Effects;
            if (effects == null) throw new InvalidOperationException("The mixer has no Effects group.");
            int routed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = Route(root, effects);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    routed += n;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            foreach (string scenePath in k_Scenes.Where(System.IO.File.Exists))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int n = scene.GetRootGameObjects().Sum(g => Route(g, effects));
                if (n > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                routed += n;
            }
            AssetDatabase.SaveAssets();
            return routed;
        }

        /// <summary>The unrouted sounds under <paramref name="root"/> sent to <paramref name="effects"/>; how many.</summary>
        static int Route(GameObject root, AudioMixerGroup effects)
        {
            int n = 0;
            foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
            {
                if (player.FeedbacksList == null) continue;
                bool changed = false;
                foreach (MMF_Feedback feedback in player.FeedbacksList)
                    if (feedback is MMF_Sound sound && sound.SfxAudioMixerGroup == null)
                    {
                        sound.SfxAudioMixerGroup = effects;
                        changed = true;
                        n++;
                    }
                if (changed) EditorUtility.SetDirty(player);
            }
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
                if (source.outputAudioMixerGroup == null)
                {
                    source.outputAudioMixerGroup = effects;
                    EditorUtility.SetDirty(source);
                    n++;
                }
            return n;
        }

        /// <summary>Tests: every sound under the prefabs and the game's scenes that isn't routed (by path), without changing anything.</summary>
        public static List<string> Unrouted()
        {
            var found = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) Collect(prefab, path, found);
            }
            return found;
        }

        public static void Collect(GameObject root, string where, List<string> found)
        {
            foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
                if (player.FeedbacksList != null)
                    foreach (MMF_Feedback f in player.FeedbacksList)
                        if (f is MMF_Sound s && s.Sfx != null && s.SfxAudioMixerGroup == null) found.Add($"{where}: {player.name}/{f.Label}");
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
                if (source.outputAudioMixerGroup == null) found.Add($"{where}: {source.name} (AudioSource)");
        }
    }
}
