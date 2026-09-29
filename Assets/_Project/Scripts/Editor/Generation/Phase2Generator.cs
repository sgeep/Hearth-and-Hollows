using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Generates everything the Phase 2 tavern prototype needs. Menu: Hearthdelve/Generate/Phase 2
    /// Tavern (All). Batch: -executeMethod Hearthdelve.Editor.Phase2Generator.RunBatch
    /// (add -rebuildScene only with the user's approval: it overwrites TavernGreybox).
    /// </summary>
    public static class Phase2Generator
    {
        [MenuItem("Hearthdelve/Generate/Phase 2 Tavern (All)", priority = 91)]
        static void RunMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool overwrite = File.Exists(TavernSceneBuilder.ScenePath) &&
                EditorUtility.DisplayDialog("Hearthdelve", "TavernGreybox already exists. Overwrite it? Manual edits to it will be lost.", "Overwrite", "Keep scene");
            Run(overwrite);
            EditorSceneManager.OpenScene(TavernSceneBuilder.ScenePath);
        }

        public static void Run(bool overwriteSceneApproved)
        {
            ProjectConfigurator.ConfigureAll();
            var actions = InputActionsBuilder.Build(force: false);
            PlaceholderArtGenerator.Generate();
            TavernArtGenerator.Generate();
            var phase1 = ContentGenerator.Generate();
            var content = TavernContentGenerator.Generate(phase1);
            AssignCustomerSprites(content);
            LocalizationBuilder.Build();
            var customer = TavernSceneBuilder.BuildCustomerPrefab();
            TavernSceneBuilder.Build(content, customer, actions, overwriteSceneApproved);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hearthdelve] Phase 2 generation complete.");
        }

        /// <summary>Gives each customer type its placeholder sprite, unless real art was assigned.</summary>
        static void AssignCustomerSprites(Tavern.Scene.TavernContent content)
        {
            foreach (var c in content.customers)
            {
                if (c == null || c.sprite != null) continue;
                c.sprite = PlaceholderArtGenerator.Load(c.id switch
                {
                    "adventurer" => TavernArtGenerator.Adventurer,
                    "dwarf" => TavernArtGenerator.Dwarf,
                    _ => TavernArtGenerator.Villager,
                });
                EditorUtility.SetDirty(c);
            }
        }

        public static void RunBatch()
        {
            try
            {
                bool rebuild = Array.IndexOf(Environment.GetCommandLineArgs(), "-rebuildScene") >= 0;
                Run(rebuild);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
