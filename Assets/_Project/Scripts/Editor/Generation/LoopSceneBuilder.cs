using System.IO;
using Hearthdelve.Shared.Game;
using Hearthdelve.UI.DebugTools;
using Hearthdelve.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the Boot scene (the persistent <see cref="GameFlow"/> and day-loop debug keys) and
    /// the MainMenu scene, and puts Boot first in the build list. Existing scenes are only
    /// replaced with approval (CLAUDE.md).
    /// </summary>
    public static class LoopSceneBuilder
    {
        public const string BootScene = EditorPaths.Scenes + "/Boot.unity";
        public const string MainMenuScene = EditorPaths.Scenes + "/MainMenu.unity";

        public static void Build(GameDatabase database, InputActionAsset actions, bool overwriteApproved)
        {
            EditorPaths.Ensure(EditorPaths.Scenes);
            if (CanWrite(BootScene, overwriteApproved))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var game = new GameObject("Game");
                game.AddComponent<GameFlow>().Configure(database);
                game.AddComponent<GameFlowDebugOverlay>();
                EditorSceneManager.SaveScene(scene, BootScene);
            }
            if (CanWrite(MainMenuScene, overwriteApproved))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                SceneKit.CreatePixelCamera(new Vector3(0f, 0f, -10f), new Color(0.12f, 0.08f, 0.06f), withCinemachineBrain: false);
                var ui = new GameObject("UI");
                SceneKit.AddDocument<MainMenuScreen>(ui.transform, "Main Menu", SceneKit.PanelSettings(), "MainMenu.uxml", 0);
                SceneKit.CreateEventSystem(actions);
                EditorSceneManager.SaveScene(scene, MainMenuScene);
            }
            // Boot must load first: it owns the game services and opens the main menu.
            SceneKit.SetBuildOrder(BootScene, MainMenuScene, TavernSceneBuilder.ScenePath, EditorPaths.GreyboxScene);
        }

        static bool CanWrite(string path, bool overwriteApproved)
        {
            if (!File.Exists(path) || overwriteApproved) return true;
            Debug.Log($"[Hearthdelve] {Path.GetFileName(path)} exists and overwrite wasn't approved; leaving it untouched.");
            return false;
        }
    }
}
