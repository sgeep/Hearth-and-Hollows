using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.UI.Debugging;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using MoreMountains.Feedbacks;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The 4d delve scene (<c>Dungeon</c>): one scene into which rooms are loaded one at a time (GDD §10.2). It holds
    /// what lasts across rooms (the player, managers, harvest, the delve controller, the navigation grid, the camera
    /// kept inside the room, the HUD, the fade between rooms and a seed line); <see cref="RoomRunner"/> generates the run
    /// from <see cref="RunSettings"/> and loads its rooms. <c>Dungeon_TestFloor</c> stays the standalone combat test bed.
    /// </summary>
    public static class DungeonRunBuilder
    {
        [MenuItem("Hearthdelve/Generate/4d Dungeon (Rooms)", priority = 4)]
        public static void GenerateMenu() => Generate(rebuildSceneApproved: false);

        /// <summary>Batch entry point: <c>-executeMethod Hearthdelve.Editor.DungeonRunBuilder.RunBatch [-rebuildScene]</c>.</summary>
        public static void RunBatch()
        {
            try
            {
                Generate(Environment.GetCommandLineArgs().Contains("-rebuildScene"));
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public const string SettingsPath = EditorPaths.Data + "/Dungeon/RunSettings.asset";

        /// <summary>
        /// Rebuilds the rooms and the run settings (keeping their tuning). The scene is created if it doesn't exist, rebuilt
        /// only with approval, and otherwise brought up to date in place.
        /// </summary>
        public static void Generate(bool rebuildSceneApproved)
        {
            LookTestBuilder.Content content = LookTestBuilder.BuildContent();
            // Rebuilding the shared prefabs recreates the tavern player without what the tavern adds to it.
            TavernBuilder.AddCarryViewToPlayer();
            BuildSounds();
            Dictionary<string, RoomDefinition> rooms = RoomContent.Build(content);
            RunSettings settings = BuildSettings(content, rooms);
            bool exists = System.IO.File.Exists(EditorPaths.DungeonScene);
            if (!exists || rebuildSceneApproved && LookTestBuilder.MayWrite(EditorPaths.DungeonScene, true)) BuildScene(content, settings);
            else UpdateRun(settings);
            AddToBuild(EditorPaths.DungeonScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] 4d dungeon generated.");
        }

        /// <summary>The run's rooms and enemies are refreshed every time; its tuning is kept (it's for editing).</summary>
        static RunSettings BuildSettings(LookTestBuilder.Content content, Dictionary<string, RoomDefinition> rooms)
        {
            RunSettings settings = LookTestContent.LoadOrCreate<RunSettings>(SettingsPath);
            settings.rooms = RoomContent.Ids.Select(id => rooms[id]).ToArray();
            settings.slime = content.Slime;
            settings.bat = content.Bat;
            settings.spider = content.Spider;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>
        /// Brings the existing scene up to the generated run in place (4d step 2): the room runner gets the run settings,
        /// the exit signs and the fall's feedback, and the canvas gets the seed line.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/4d Update Dungeon Run", priority = 25)]
        public static void UpdateRunMenu() => UpdateRun(AssetDatabase.LoadAssetAtPath<RunSettings>(SettingsPath));

        public static void UpdateRunBatch()
        {
            try
            {
                UpdateRunMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void UpdateRun(RunSettings settings)
        {
            Scene scene = EditorSceneManager.OpenScene(EditorPaths.DungeonScene, OpenSceneMode.Single);
            var runner = UnityEngine.Object.FindAnyObjectByType<RoomRunner>(FindObjectsInactive.Include);
            ConfigureRunner(runner, settings);
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "UI");
            BuildDebugLabel(canvas);
            BuildRoomFade(canvas);
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Dungeon run updated.");
        }

        /// <summary>Points the runner at the scene's pieces and the run settings; its feedbacks are rebuilt.</summary>
        static void ConfigureRunner(RoomRunner runner, RunSettings settings)
        {
            Transform rooms = runner.transform;
            foreach (string name in new[] { "Feedback_Seal", "Feedback_Clear", "Feedback_Fall" })
            {
                Transform old = rooms.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            MMF_Player seal = LookTestContent.Feedback(rooms, "Feedback_Seal", null, 0.1f, LookTestContent.Sfx("PH_GateSlam"), LookTestContent.Pattern(HapticIds.BumpSoft));
            MMF_Player clear = LookTestContent.Feedback(rooms, "Feedback_Clear", null, 0f, LookTestContent.Sfx("PH_GateRise"), LookTestContent.Pattern(HapticIds.PulseSuccess));
            MMF_Player fall = LookTestContent.Feedback(rooms, "Feedback_Fall", null, 0f, LookTestContent.Sfx("PH_Whoosh"), LookTestContent.Pattern(HapticIds.TapFirm));
            GameObject follow = GameObject.Find("Follow Camera");
            Transform roomRoot = rooms.Find("Room");
            if (roomRoot == null)
            {
                roomRoot = new GameObject("Room").transform;
                roomRoot.SetParent(rooms, false);
            }
            runner.Configure(settings, roomRoot, UnityEngine.Object.FindAnyObjectByType<NavGrid>(), follow.GetComponent<CinemachineCamera>(),
                follow.GetComponent<RoomCameraBounds>(), seal, clear, fall,
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "ArrowUp"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "ArrowDown"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Swords"));
            EditorUtility.SetDirty(runner);
        }

        /// <summary>The seed line (debug): bottom right, where the HUD has nothing, quiet, under the room fade.</summary>
        static void BuildDebugLabel(Canvas canvas)
        {
            Transform old = canvas.transform.Find("DelveDebug");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var corner = new Vector2(1f, 0f);
            LocalizedSuperText text = LookTestBuilder.Text(canvas.transform, "DelveDebug", LocKeys.DelveDebug, 6f, new Color(0.6f, 0.62f, 0.7f), TextAnchor.LowerRight,
                corner, corner, corner, new Vector2(-4f, 3f), new Vector2(200f, GameFonts.LinePixels));
            text.gameObject.AddComponent<DelveDebugLabel>();
        }

        /// <summary>The scene's UI, in place: the HUD and screens are rebuilt, the room fade added, and text gets the game font.</summary>
        [MenuItem("Hearthdelve/Generate/4d Update Dungeon UI", priority = 24)]
        public static void UpdateDungeonUI()
        {
            MinifantasyImporter.ImportAll();
            LocalizationBuilder.Build();
            Scene scene = EditorSceneManager.OpenScene(EditorPaths.DungeonScene, OpenSceneMode.Single);
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "UI");
            DungeonUI.RebuildScreens(canvas);
            BuildRoomFade(canvas);
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Dungeon UI updated.");
        }

        public static void UpdateDungeonUIBatch()
        {
            try
            {
                UpdateDungeonUI();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>The gates: an iron slam as they drop, a rattle as they rise.</summary>
        static void BuildSounds()
        {
            static float Sin(float t, float hz) => Mathf.Sin(t * 2f * Mathf.PI * hz);
            LookTestContent.WriteWav("PH_GateSlam", 0.35f, (t, n) =>
                (LookTestContent.Noise(n) * 0.5f + Sin(t, 70f - 40f * t) * 0.9f) * Mathf.Exp(-t * 14f) * 0.8f);
            LookTestContent.WriteWav("PH_GateRise", 0.5f, (t, n) =>
                (LookTestContent.Noise(n) * 0.35f + Sin(t, 900f) * 0.15f) * (0.6f + 0.4f * Sin(t, 22f)) * Mathf.Sin(t / 0.5f * Mathf.PI) * 0.6f);
            foreach (string name in new[] { "PH_GateSlam", "PH_GateRise", "PH_Whoosh" })
                AssetDatabase.ImportAsset($"{EditorPaths.Audio}/{name}.wav");
        }

        static void BuildScene(LookTestBuilder.Content content, RunSettings settings)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // The player spawns here and is moved to the first room's arrival point.
            GameObject managers = LookTestBuilder.Managers(HearthdelveInputManager.GameplayMap.Dungeon, content.Library, content.Player, new Vector2(20f, 3f));
            HarvestSystem harvest = managers.AddComponent<HarvestSystem>();
            harvest.Configure(content.HarvestRules, content.Cleaver, content.Pickup.GetComponent<IngredientPickup>(), content.Freshness);
            harvest.Scatter = 1.1f;
            TestFloorBuilder.HarvestFeedbacks(harvest);
            var nav = managers.AddComponent<NavGrid>();
            nav.Configure(new RectInt(0, 0, RoomLayout.MinWidth, RoomLayout.MinHeight), LayerMask.GetMask(Layers.Obstacles));
            managers.AddComponent<DelveRunController>();

            LookTestBuilder.Cameras(new Color(0.05f, 0.05f, 0.07f));
            var follow = GameObject.Find("Follow Camera");
            // Kept inside the room, with no easing at its edge, like the follow itself (CLAUDE.md, Camera and scrolling).
            follow.AddComponent<RoomCameraBounds>();
            LookTestBuilder.Light("Global Light 2D", Vector3.zero, Light2D.LightType.Global);

            var rooms = new GameObject("Rooms");
            new GameObject("Room").transform.SetParent(rooms.transform, false);
            ConfigureRunner(rooms.AddComponent<RoomRunner>(), settings);

            Canvas canvas = LookTestBuilder.Canvas(content.Actions, out _);
            DungeonUI.RebuildScreens(canvas);
            BuildDebugLabel(canvas);
            BuildRoomFade(canvas);

            LookTestBuilder.ApplyLighting(dungeon: true);
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, EditorPaths.DungeonScene);
        }

        /// <summary>The black cover between rooms, over everything else on the canvas.</summary>
        static void BuildRoomFade(Canvas canvas)
        {
            Transform old = canvas.transform.Find("RoomFade");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            RectTransform rect = DungeonUI.FullScreen(canvas, "RoomFade");
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            rect.gameObject.AddComponent<CanvasGroup>();
            rect.gameObject.AddComponent<RoomFadeView>();
            rect.SetAsLastSibling();
        }

        /// <summary>Adds the scene to the build list if it isn't there, keeping the existing order (Boot stays first).</summary>
        static void AddToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
