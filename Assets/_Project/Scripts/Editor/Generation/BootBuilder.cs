using System;
using System.IO;
using System.Linq;
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
using Object = UnityEngine.Object;

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
            if (File.Exists(BootScene)) UpdateBoot();
            else BuildBoot();
            if (File.Exists(MainMenuScene)) UpdateMainMenu();
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

        /// <summary>Adds what's missing to an existing Boot scene (in place; nothing already there is rebuilt).</summary>
        static void UpdateBoot()
        {
            var scene = EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
            if (GameObject.Find("Boot Camera") == null) AddBootCamera();
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hearthdelve] {BootScene} updated in place.");
        }

        /// <summary>The existing main menu, in place: the menu panel is rebuilt (current font, layout and buttons); the scene is kept.</summary>
        static void UpdateMainMenu()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "UI");
            Transform old = canvas.transform.Find("Menu");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            BuildMenu(canvas);
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hearthdelve] {MainMenuScene} updated in place.");
        }

        /// <summary>
        /// A camera that is always there, behind every scene's own: it draws nothing but black. While a transition
        /// swaps scenes there's a moment with no other camera, and URP draws the screen-space cover only as part of
        /// a camera's render; without this one the screen showed whatever was there last (4c step 5 playtest).
        /// </summary>
        static void AddBootCamera()
        {
            var go = new GameObject("Boot Camera");
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            camera.depth = -100f;
            camera.orthographic = true;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
        }

        static void BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AddBootCamera();
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
            go.AddComponent<Hearthdelve.UI.PixelCanvasScaler>();
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

        /// <summary>The menu itself: the game's name over a panel with Continue and New Game, and the start-over question.</summary>
        static void BuildMenu(Canvas canvas)
        {
            UiFeedbackContent.Ensure(canvas);
            RectTransform root = DungeonUI.FullScreen(canvas, "Menu");
            var centre = new Vector2(0.5f, 0.5f);
            TavernScreens.Label(root, "Title", LoopLocKeys.MenuTitle, 16f, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter, centre, new Vector2(0f, 50f), new Vector2(300f, 24f));
            RectTransform panel = DungeonUI.Panel(root, new Vector2(300f, 76f), new Vector2(0f, -18f));

            // Stacked and centred, so the panel looks right with or without Continue.
            RectTransform choices = TavernScreens.Rect(panel, "Choices", centre, centre, Vector2.zero, new Vector2(300f, 76f));
            var stack = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.spacing = 3f;
            stack.childControlWidth = stack.childControlHeight = false;
            stack.childForceExpandWidth = stack.childForceExpandHeight = false;
            Button continueButton = TavernScreens.SmallButton(choices, "Continue", LoopLocKeys.MenuContinue, centre, Vector2.zero, 110f, out _);
            LocalizedSuperText detail = TavernScreens.Label(choices, "ContinueDetail", LoopLocKeys.MenuContinueFrom, 6f, DungeonUI.k_Label, TextAnchor.MiddleCenter, centre, Vector2.zero, new Vector2(180f, 12f));
            var gap = TavernScreens.Rect(choices, "Gap", centre, centre, Vector2.zero, new Vector2(10f, 4f));
            Button newGame = TavernScreens.SmallButton(choices, "NewGame", LoopLocKeys.MenuNewGame, centre, Vector2.zero, 110f, out _);

            RectTransform confirm = TavernScreens.Rect(panel, "Confirm", centre, centre, Vector2.zero, new Vector2(300f, 76f));
            TavernScreens.Label(confirm, "Question", LoopLocKeys.MenuConfirm, 6f, DungeonUI.k_Ink, TextAnchor.MiddleCenter, centre, new Vector2(0f, 14f), new Vector2(290f, 12f));
            Button yes = TavernScreens.SmallButton(confirm, "Yes", LoopLocKeys.MenuConfirmYes, centre, new Vector2(-45f, -10f), 80f, out _);
            Button no = TavernScreens.SmallButton(confirm, "No", LoopLocKeys.MenuConfirmNo, centre, new Vector2(45f, -10f), 80f, out _);
            confirm.gameObject.SetActive(false);

            root.gameObject.AddComponent<MainMenuScreen>().Configure(choices.gameObject, continueButton, detail, newGame, confirm.gameObject, yes, no);
            UiFeedbackContent.Commit(continueButton);
            UiFeedbackContent.Commit(newGame);
            UiFeedbackContent.Commit(yes);
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
            BuildMenu(canvas);
            EditorSceneManager.SaveScene(scene, MainMenuScene);
            Debug.Log($"[Hearthdelve] Created {MainMenuScene}.");
        }
    }
}
