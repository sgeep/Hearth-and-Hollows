using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Generates everything the Phase 3 day loop needs, on top of Phases 1 and 2: upgrades,
    /// breakfast buffs, the GameDatabase, the exit in CombatGreybox, seat props in TavernGreybox,
    /// and the Boot and MainMenu scenes. Menu: Hearthdelve/Generate/Phase 3 Loop (All).
    /// Batch: -executeMethod Hearthdelve.Editor.Phase3Generator.RunBatch (add -rebuildScene only
    /// with the user's approval: it overwrites the existing scenes).
    /// </summary>
    public static class Phase3Generator
    {
        [MenuItem("Hearthdelve/Generate/Phase 3 Loop (All)", priority = 92)]
        static void RunMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            bool anyExists = File.Exists(EditorPaths.GreyboxScene) || File.Exists(TavernSceneBuilder.ScenePath) ||
                             File.Exists(LoopSceneBuilder.BootScene) || File.Exists(LoopSceneBuilder.MainMenuScene);
            bool overwrite = anyExists && EditorUtility.DisplayDialog("Hearthdelve",
                "Rebuild the existing scenes (CombatGreybox, TavernGreybox, Boot, MainMenu)? Manual edits to them will be lost.", "Rebuild", "Keep scenes");
            Run(overwrite);
            EditorSceneManager.OpenScene(LoopSceneBuilder.BootScene);
        }

        public static void Run(bool overwriteScenesApproved)
        {
            Phase1Generator.Run(overwriteScenesApproved);
            Phase2Generator.Run(overwriteScenesApproved);
            var phase1 = ContentGenerator.Generate();
            var tavern = AssetDatabase.LoadAssetAtPath<Tavern.Scene.TavernContent>($"{TavernContentGenerator.TavernData}/TavernContent.asset");
            var database = LoopContentGenerator.Generate(phase1, tavern);
            LocalizationBuilder.Build();
            var actions = InputActionsBuilder.Build(force: false);
            LoopSceneBuilder.Build(database, actions, overwriteScenesApproved);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hearthdelve] Phase 3 generation complete.");
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
