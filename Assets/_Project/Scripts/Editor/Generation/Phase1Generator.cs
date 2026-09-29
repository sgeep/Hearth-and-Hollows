using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// One entry point that sets up the project and generates everything Phase 1 needs.
    /// From the menu: Hearthdelve/Generate/Phase 1 (All). From the command line:
    /// Unity -batchmode -projectPath . -executeMethod Hearthdelve.Editor.Phase1Generator.RunBatch -quit
    /// </summary>
    public static class Phase1Generator
    {
        [MenuItem("Hearthdelve/Generate/Phase 1 (All)", priority = 90)]
        static void RunMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool rebuildScene = !File.Exists(EditorPaths.GreyboxScene) ||
                EditorUtility.DisplayDialog("Hearthdelve", "Rebuild the CombatGreybox scene? Manual edits to it will be lost.", "Rebuild", "Keep scene");
            Run(rebuildScene);
            EditorSceneManager.OpenScene(EditorPaths.GreyboxScene);
        }

        [MenuItem("Hearthdelve/Generate/Rebuild Greybox Scene (overwrites)", priority = 120)]
        static void RebuildSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(EditorPaths.GreyboxScene) &&
                !EditorUtility.DisplayDialog("Hearthdelve", "Overwrite the CombatGreybox scene? Manual edits to it will be lost.", "Overwrite", "Cancel"))
                return;
            Run(rebuildScene: true);
            EditorSceneManager.OpenScene(EditorPaths.GreyboxScene);
        }

        public static void Run(bool rebuildScene)
        {
            ProjectConfigurator.ConfigureAll();
            var actions = InputActionsBuilder.Build(force: false);
            PlaceholderArtGenerator.Generate();
            var content = ContentGenerator.Generate();
            LocalizationBuilder.Build();
            var prefabs = PrefabGenerator.Generate(content);
            GreyboxSceneBuilder.Build(content, prefabs, actions, overwriteApproved: rebuildScene);
            ProjectConfigurator.RemoveTemplateAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hearthdelve] Phase 1 generation complete.");
        }

        /// <summary>
        /// Batch-mode entry. Creates the scene only if it doesn't exist; pass -rebuildScene to
        /// overwrite it, and only with the user's approval (CLAUDE.md).
        /// </summary>
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
