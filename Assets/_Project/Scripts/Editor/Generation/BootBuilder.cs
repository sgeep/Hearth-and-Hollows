using System;
using System.IO;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The day loop's scenes (4c step 5). <c>Boot</c> holds the persistent services (GDD §10.2): <see cref="GameFlow"/>
    /// with the game database, localization, haptics, the F8/F9 debug keys, and the transition overlay; it stays
    /// loaded while the main menu, tavern and dungeon are loaded and unloaded beside it. <c>MainMenu</c> offers Continue
    /// and New Game. Both are created only if they don't exist (existing scenes are never overwritten), and Boot goes
    /// first in the build list.
    /// </summary>
    public static class BootBuilder
    {
        public const string BootScene = EditorPaths.Scenes + "/Boot.unity";
        public const string MainMenuScene = EditorPaths.Scenes + "/MainMenu.unity";

        static readonly Color k_Light = DungeonUI.k_Light;

        [MenuItem("Hearthdelve/Generate/4c Boot and Main Menu", priority = 30)]
        public static void Generate()
        {
            LocalizationBuilder.Build();
            if (File.Exists(BootScene)) Debug.Log($"[Hearthdelve] {BootScene} exists; kept as it is.");
            else BuildBoot();
            if (File.Exists(MainMenuScene)) Debug.Log($"[Hearthdelve] {MainMenuScene} exists; kept as it is.");
            else BuildMainMenu();
            ProjectConfigurator.SetBuildOrder(BootScene, MainMenuScene, EditorPaths.TavernScene, EditorPaths.TestFloorScene,
                EditorPaths.LookTestDungeonScene, EditorPaths.LookTestTavernScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] Boot and main menu ready.");
        }

        public static void GenerateBatch()
        {
            try
            {
                Generate();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var services = new GameObject("GameFlow");
            services.AddComponent<GameFlow>().Configure(AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset"));
            services.AddComponent<LocalizationBoot>();
            services.AddComponent<HapticService>().Library = AssetDatabase.LoadAssetAtPath<HapticLibrary>(EditorPaths.Haptics + "/HapticLibrary.asset");
            services.AddComponent<GameFlowDebugKeys>();

            // The transition: black over everything, with the day and phase.
            var go = new GameObject("Transition");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LookTestBuilder.ReferenceWidth, LookTestBuilder.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            scaler.referencePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
            go.AddComponent<GraphicRaycaster>();
            RectTransform cover = DungeonUI.FullScreen(canvas, "Cover");
            Image black = DungeonUI.AddImage(cover, DungeonUI.Pixel(), new Color(0.03f, 0.02f, 0.03f));
            black.raycastTarget = true;
            var group = cover.gameObject.AddComponent<CanvasGroup>();
            var centre = new Vector2(0.5f, 0.5f);
            LocalizedSuperText caption = TavernScreens.Label(cover, "Caption", LoopLocKeys.MorningTitle, 10f, k_Light, TextAnchor.MiddleCenter, centre, Vector2.zero, new Vector2(300f, 14f));
            go.AddComponent<TransitionScreen>().Configure(group, caption);

            EditorSceneManager.SaveScene(scene, BootScene);
            Debug.Log($"[Hearthdelve] Created {BootScene}.");
        }

        static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.05f, 0.05f);
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();

            Canvas canvas = LookTestBuilder.Canvas(AssetDatabase.LoadAssetAtPath<InputActionAsset>(EditorPaths.InputActions), out _);
            RectTransform root = DungeonUI.FullScreen(canvas, "Menu");
            var centre = new Vector2(0.5f, 0.5f);
            TavernScreens.Label(root, "Title", LoopLocKeys.MenuTitle, 16f, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter, centre, new Vector2(0f, 48f), new Vector2(300f, 20f));
            RectTransform panel = DungeonUI.Panel(root, new Vector2(200f, 72f), new Vector2(0f, -18f));

            // Stacked and centred, so the panel looks right with or without Continue.
            RectTransform choices = TavernScreens.Rect(panel, "Choices", centre, centre, Vector2.zero, new Vector2(200f, 72f));
            var stack = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.spacing = 3f;
            stack.childControlWidth = stack.childControlHeight = false;
            stack.childForceExpandWidth = stack.childForceExpandHeight = false;
            Button continueButton = TavernScreens.SmallButton(choices, "Continue", LoopLocKeys.MenuContinue, centre, Vector2.zero, 110f, out _);
            LocalizedSuperText detail = TavernScreens.Label(choices, "ContinueDetail", LoopLocKeys.MenuContinueFrom, 6f, DungeonUI.k_Ink, TextAnchor.MiddleCenter, centre, Vector2.zero, new Vector2(180f, 8f));
            var gap = TavernScreens.Rect(choices, "Gap", centre, centre, Vector2.zero, new Vector2(10f, 4f));
            Button newGame = TavernScreens.SmallButton(choices, "NewGame", LoopLocKeys.MenuNewGame, centre, Vector2.zero, 110f, out _);

            RectTransform confirm = TavernScreens.Rect(panel, "Confirm", centre, centre, Vector2.zero, new Vector2(200f, 72f));
            TavernScreens.Label(confirm, "Question", LoopLocKeys.MenuConfirm, 6f, DungeonUI.k_Ink, TextAnchor.MiddleCenter, centre, new Vector2(0f, 14f), new Vector2(190f, 8f));
            Button yes = TavernScreens.SmallButton(confirm, "Yes", LoopLocKeys.MenuConfirmYes, centre, new Vector2(-45f, -12f), 80f, out _);
            Button no = TavernScreens.SmallButton(confirm, "No", LoopLocKeys.MenuConfirmNo, centre, new Vector2(45f, -12f), 80f, out _);
            confirm.gameObject.SetActive(false);

            root.gameObject.AddComponent<MainMenuScreen>().Configure(choices.gameObject, continueButton, detail, newGame, confirm.gameObject, yes, no);
            EditorSceneManager.SaveScene(scene, MainMenuScene);
            Debug.Log($"[Hearthdelve] Created {MainMenuScene}.");
        }
    }
}
