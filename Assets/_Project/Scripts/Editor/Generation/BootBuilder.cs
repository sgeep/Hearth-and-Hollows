using System;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Typography;
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
            // The transition caption is a heading (2×, the type pass): its box takes a 2× line.
            foreach (SuperTextMesh text in Object.FindObjectsByType<SuperTextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (text.name == "Caption" && text.transform is RectTransform rect && rect.sizeDelta.y < 24f) rect.sizeDelta = new Vector2(rect.sizeDelta.x, 24f);
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
            LocalizedSuperText caption = TavernScreens.Label(cover, "Caption", LoopLocKeys.MorningTitle, TextStyle.Heading, k_Light, TextAnchor.MiddleCenter, centre, Vector2.zero, new Vector2(300f, 24f));
            go.AddComponent<TransitionScreen>().Configure(group, caption);

            EditorSceneManager.SaveScene(scene, BootScene);
            Debug.Log($"[Hearthdelve] Created {BootScene}.");
        }

        public const string BackdropPath = EditorPaths.Art + "/Menu/MenuBackdrop.png";

        /// <summary>
        /// The menu's still (4i-A, D1): Tally Ho! and Kariaston as the game draws them, rendered once at 320×180 from the village (owned
        /// Minifantasy art; the keeper and the HUD out of frame) with a little warmth and a vignette. A sprite, point filtered, uncompressed.
        /// </summary>
        static Sprite Backdrop()
        {
            var importer = AssetImporter.GetAtPath(BackdropPath) as TextureImporter;
            if (importer == null) return null;
            if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point ||
                importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(BackdropPath);
        }

        /// <summary>
        /// The menu itself (4i-A): the still of Tally Ho! and Kariaston behind the game's name, a panel with Continue, New Game, the
        /// controls and (on desktop) Quit, the start-over question, a line for a save that can't be loaded, the version in the corner,
        /// and the controls reference.
        /// </summary>
        static void BuildMenu(Canvas canvas)
        {
            UiFeedbackContent.Ensure(canvas);
            RectTransform root = DungeonUI.FullScreen(canvas, "Menu");
            var centre = new Vector2(0.5f, 0.5f);
            Sprite backdrop = Backdrop();
            if (backdrop != null)
            {
                RectTransform still = TavernScreens.Rect(root, "Backdrop", centre, centre, Vector2.zero, new Vector2(LookTestBuilder.ReferenceWidth, LookTestBuilder.ReferenceHeight));
                DungeonUI.AddImage(still, backdrop, Color.white);
            }
            RectTransform titleBand = TavernScreens.Rect(root, "TitleBand", centre, centre, new Vector2(0f, 62f), new Vector2(LookTestBuilder.ReferenceWidth, 40f),
                new Color(0.05f, 0.03f, 0.03f, 0.45f));
            TavernScreens.Label(titleBand, "Title", LoopLocKeys.MenuTitle, TextStyle.Display, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter, centre, Vector2.zero, new Vector2(300f, 36f));

            // A save that can't be loaded: said plainly, under the title (hidden otherwise). 4i-B (the owner's 4i-A note): a full-width
            // band, nearly opaque, so the words read over any part of the still.
            RectTransform messageStrip = TavernScreens.Rect(root, "Message", centre, centre, new Vector2(0f, 32f), new Vector2(LookTestBuilder.ReferenceWidth, 28f));
            var messageBack = messageStrip.gameObject.AddComponent<Image>();
            messageBack.sprite = DungeonUI.Pixel();
            messageBack.color = new Color(0.05f, 0.03f, 0.03f, 0.9f);
            messageBack.raycastTarget = false;
            LocalizedSuperText message = LookTestBuilder.Text(messageStrip, "Text", MenuLocKeys.SaveUnreadable, TextStyle.Body, DungeonUI.k_Light, TextAnchor.MiddleCenter,
                centre, centre, centre, Vector2.zero, new Vector2(284f, 24f));

            RectTransform panel = DungeonUI.Panel(root, new Vector2(150f, 106f), new Vector2(0f, -36f));

            // Stacked and centred, so the panel looks right with or without Continue and Quit.
            RectTransform choices = TavernScreens.Rect(panel, "Choices", centre, centre, Vector2.zero, new Vector2(150f, 106f));
            var stack = choices.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.childAlignment = TextAnchor.MiddleCenter;
            stack.spacing = 1f;
            stack.childControlWidth = stack.childControlHeight = false;
            stack.childForceExpandWidth = stack.childForceExpandHeight = false;
            Button continueButton = TavernScreens.SmallButton(choices, "Continue", LoopLocKeys.MenuContinue, centre, Vector2.zero, 110f, out _);
            LocalizedSuperText detail = TavernScreens.Label(choices, "ContinueDetail", LoopLocKeys.MenuContinueFrom, 6f, DungeonUI.k_Label, TextAnchor.MiddleCenter, centre, Vector2.zero, new Vector2(140f, 12f));
            var gap = TavernScreens.Rect(choices, "Gap", centre, centre, Vector2.zero, new Vector2(10f, 1f));
            Button newGame = TavernScreens.SmallButton(choices, "NewGame", LoopLocKeys.MenuNewGame, centre, Vector2.zero, 110f, out _);
            Button options = TavernScreens.SmallButton(choices, "Options", MenuLocKeys.Options, centre, Vector2.zero, 110f, out _);
            Button controls = TavernScreens.SmallButton(choices, "Controls", MenuLocKeys.Controls, centre, Vector2.zero, 110f, out _);
            Button quit = TavernScreens.SmallButton(choices, "Quit", MenuLocKeys.Quit, centre, Vector2.zero, 110f, out _);

            RectTransform confirm = TavernScreens.Rect(root, "Confirm", centre, centre, new Vector2(0f, -40f), new Vector2(200f, 64f));
            DungeonUI.AddImage(confirm, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            TavernScreens.Label(confirm, "Question", LoopLocKeys.MenuConfirm, 6f, DungeonUI.k_Ink, TextAnchor.MiddleCenter, centre, new Vector2(0f, 11f), new Vector2(186f, 24f));
            Button yes = TavernScreens.SmallButton(confirm, "Yes", LoopLocKeys.MenuConfirmYes, centre, new Vector2(-45f, -16f), 80f, out _);
            Button no = TavernScreens.SmallButton(confirm, "No", LoopLocKeys.MenuConfirmNo, centre, new Vector2(45f, -16f), 80f, out _);
            confirm.gameObject.SetActive(false);

            // The version, small, in the corner.
            var corner = new Vector2(1f, 0f);
            LocalizedSuperText version = LookTestBuilder.Text(root, "Version", MenuLocKeys.Version, TextStyle.Secondary, DungeonUI.k_Light, TextAnchor.LowerRight,
                corner, corner, corner, new Vector2(-4f, 2f), new Vector2(90f, 12f));

            ControlsPage controlsPage = FirstImpressionsUI.BuildControlsPage(root);
            OptionsScreen optionsScreen = FirstImpressionsUI.BuildOptions(root, controlsPage);

            var menu = root.gameObject.AddComponent<MainMenuScreen>();
            menu.Configure(choices.gameObject, continueButton, detail, newGame, confirm.gameObject, yes, no);
            menu.ConfigureFirstImpressions(controls, quit, controlsPage, message, version, titleBand.gameObject, panel.gameObject);
            menu.ConfigureOptions(options, optionsScreen);
            // 4g Checkpoint B: New Game makes the keeper first.
            menu.ConfigureCreator(KeeperCreatorUI.Build(root));
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
