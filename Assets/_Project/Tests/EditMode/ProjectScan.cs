using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Editor;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// The EditMode tests' one look at the game's scenes and string tables (the test review, 2026-10-09): each scene opens once in a
    /// run, on first use, and stays open (read-only) for every later test until the run ends (<see cref="ProjectScanTeardown"/>),
    /// instead of every test opening and closing its own copies. Lazy, so a test run alone works the same. Tests only read what's
    /// here; nothing is saved.
    /// </summary>
    public static class ProjectScan
    {
        /// <summary>The scenes the game ships (the look tests and the test floor are development scenes).</summary>
        public static readonly string[] GameScenes =
        {
            BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene,
        };

        static readonly Dictionary<string, Scene> s_Open = new();
        static readonly Dictionary<string, Dictionary<string, string>> s_English = new();

        /// <summary>How many times a scene was opened this run (one per scene, if the scan works).</summary>
        public static int Opens { get; private set; }

        /// <summary>
        /// The places the game never has loaded together: the Hollows and the test floor are each loaded alone, Tally Ho! and
        /// Kariaston together. Two of them open at once would each bring an enabled global light, which URP's 2D renderer reports
        /// as an error, so opening one closes the others.
        /// </summary>
        static readonly string[][] k_Apart =
        {
            new[] { EditorPaths.TavernScene, KariastonBuilder.ScenePath }, new[] { EditorPaths.DungeonScene }, new[] { EditorPaths.TestFloorScene },
        };

        /// <summary>The scene at <paramref name="path"/>, opened additively the first time it's asked for.</summary>
        public static Scene Open(string path)
        {
            if (s_Open.TryGetValue(path, out Scene scene) && scene.IsValid() && scene.isLoaded) return scene;
            string[] group = k_Apart.FirstOrDefault(g => g.Contains(path));
            if (group != null)
                foreach (string other in k_Apart.Where(g => g != group).SelectMany(g => g).ToArray())
                    if (s_Open.TryGetValue(other, out Scene open) && open.IsValid() && open.isLoaded)
                    {
                        EditorSceneManager.CloseScene(open, true);
                        s_Open.Remove(other);
                    }
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            s_Open[path] = scene;
            Opens++;
            return scene;
        }

        public static GameObject[] Roots(string path) => Open(path).GetRootGameObjects();

        /// <summary>Every component of type <typeparamref name="T"/> in the scene, inactive ones included.</summary>
        public static IEnumerable<T> All<T>(string path) where T : Component =>
            Roots(path).SelectMany(r => r.GetComponentsInChildren<T>(true));

        static GameObject[] s_Prefabs;

        /// <summary>Every prefab of the game's own (Assets/_Project/Prefabs), loaded once a run.</summary>
        public static GameObject[] Prefabs => s_Prefabs ??= UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs })
            .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).Where(g => g != null).ToArray();

        public static void CloseAll()
        {
            if (Opens > 0) Debug.Log($"[ProjectScan] {Opens} scene opens this run");
            foreach (Scene scene in s_Open.Values)
                if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
                    EditorSceneManager.CloseScene(scene, true);
            s_Open.Clear();
        }

        /// <summary>
        /// A string table's English, as the game shows it (key → text): the tables are what's drawn, so a text whose key isn't here
        /// would show its key on screen.
        /// </summary>
        public static IReadOnlyDictionary<string, string> English(string table)
        {
            if (s_English.TryGetValue(table, out Dictionary<string, string> cached)) return cached;
            var map = new Dictionary<string, string>();
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(table);
            if (collection != null && collection.GetTable("en") is StringTable english)
                foreach (StringTableEntry entry in english.Values)
                    if (!string.IsNullOrEmpty(entry.Key)) map[entry.Key] = entry.Value ?? string.Empty;
            s_English[table] = map;
            return map;
        }
    }
}

/// <summary>Closes <see cref="Hearthdelve.Tests.ProjectScan"/>'s scenes when the EditMode run ends (outside any namespace: the whole assembly).</summary>
[SetUpFixture]
public class ProjectScanTeardown
{
    [OneTimeTearDown]
    public void CloseScenes() => Hearthdelve.Tests.ProjectScan.CloseAll();
}
