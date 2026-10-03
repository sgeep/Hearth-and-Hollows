using System;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.UI.Debugging;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.World;
using MoreMountains.TopDownEngine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the 4a look test: one dungeon room and one tavern corner with real Minifantasy
    /// art at 320×180 and 8 PPU. Scenes are created when missing and never overwritten
    /// without approval (a dialog in the editor; <c>-rebuildScene</c> in batch mode).
    /// </summary>
    public static class LookTestBuilder
    {
        public const int ReferenceWidth = 320;
        public const int ReferenceHeight = 180;

        [MenuItem("Hearthdelve/Generate/4a Look Test (All)", priority = 0)]
        public static void GenerateMenu() => Generate(rebuildScenesApproved: false);

        /// <summary>Batch entry point: <c>-executeMethod Hearthdelve.Editor.LookTestBuilder.RunBatch [-rebuildScene]</c>.</summary>
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

        /// <summary>Everything the generated scenes use: settings, art, data and prefabs.</summary>
        internal sealed class Content
        {
            public InputActionAsset Actions;
            public HapticLibrary Library;
            public GameObject Player, TavernPlayer, Slime, Pickup, Cook;
            public HarvestRulesConfig HarvestRules;
            public WeaponDefinition Cleaver;
        }

        public static void Generate(bool rebuildScenesApproved)
        {
            Content content = BuildContent();
            if (MayWrite(EditorPaths.LookTestDungeonScene, rebuildScenesApproved))
                BuildDungeon(content.Actions, content.Library, content.Player, content.Slime, content.Pickup, content.HarvestRules, content.Cleaver);
            if (MayWrite(EditorPaths.LookTestTavernScene, rebuildScenesApproved))
                BuildTavern(content.Actions, content.Library, content.TavernPlayer, content.Cook);

            ProjectConfigurator.SetBuildOrder(EditorPaths.LookTestDungeonScene, EditorPaths.LookTestTavernScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] 4a look test generated.");
        }

        /// <summary>Configures the project and (re)builds the shared data and prefabs. Never touches scenes.</summary>
        internal static Content BuildContent()
        {
            ProjectConfigurator.ConfigureAll();
            InputActionAsset actions = InputActionsBuilder.Build(force: false);
            LocalizationBuilder.Build();
            MinifantasyImporter.ImportAll();
            LookTestContent.AssignIngredientIcons();

            HapticLibrary library = LookTestContent.BuildHaptics();
            LookTestContent.BuildPlaceholderAudio();
            LookTestContent.BuildAnimationSets(out var human, out var humanShadow, out var slime, out var slimeShadow, out var cook);

            var moveConfig = LookTestContent.LoadOrCreate<PlayerMoveConfig>(LookTestContent.MoveConfigPath);
            var essenceConfig = LookTestContent.Load<EssenceConfig>($"{EditorPaths.Config}/EssenceConfig.asset");
            var delveConfig = LookTestContent.Load<DelveConfig>($"{EditorPaths.Config}/DelveConfig.asset");
            var harvestRules = LookTestContent.Load<HarvestRulesConfig>($"{EditorPaths.Config}/HarvestRulesConfig.asset");
            var cleaverDefinition = LookTestContent.Load<WeaponDefinition>($"{EditorPaths.Weapons}/Weapon_ButchersCleaver.asset");
            var slimeDefinition = LookTestContent.Load<EnemyDefinition>($"{EditorPaths.Enemies}/Enemy_GreenSlime.asset");

            GameObject cleaver = LookTestContent.BuildCleaver(cleaverDefinition);
            GameObject player = LookTestContent.BuildPlayer(true, human, humanShadow, moveConfig, essenceConfig, delveConfig, cleaver);
            GameObject tavernPlayer = LookTestContent.BuildPlayer(false, human, humanShadow, moveConfig, null, null, null);
            GameObject slimePrefab = LookTestContent.BuildSlime(slimeDefinition, slime, slimeShadow);
            GameObject pickup = LookTestContent.BuildPickup();
            GameObject cookPrefab = LookTestContent.BuildCook(cook);
            AssetDatabase.SaveAssets();
            return new Content
            {
                Actions = actions, Library = library, Player = player, TavernPlayer = tavernPlayer, Slime = slimePrefab,
                Pickup = pickup, Cook = cookPrefab, HarvestRules = harvestRules, Cleaver = cleaverDefinition,
            };
        }

        /// <summary>Scenes are never overwritten without asking (CLAUDE.md).</summary>
        internal static bool MayWrite(string scenePath, bool approved)
        {
            if (!File.Exists(scenePath)) return true;
            if (Application.isBatchMode)
            {
                if (!approved) Debug.Log($"[Hearthdelve] Kept the existing scene {scenePath} (pass -rebuildScene to rebuild it).");
                return approved;
            }
            return EditorUtility.DisplayDialog("Rebuild scene?",
                $"{scenePath} already exists. Rebuilding replaces it, including any changes made by hand.", "Rebuild", "Keep");
        }

        // ------------------------------------------------------------------ shared scene pieces

        internal static GameObject Managers(HearthdelveInputManager.GameplayMap map, HapticLibrary library, GameObject playerPrefab, Vector2 spawn)
        {
            // TDE's GameManager is a persistent singleton: it keeps its whole GameObject alive across
            // scene loads and destroys later copies, so it must not share an object with scene managers.
            var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            // -1 = platform default: the web then runs on requestAnimationFrame and desktop on vsync.
            gameManager.TargetFrameRate = -1;
            var managers = new GameObject("Managers");
            managers.AddComponent<HearthdelveInputManager>().Map = map;
            managers.AddComponent<TdeEventBridge>();
            managers.AddComponent<LocalizationBoot>();
            managers.AddComponent<HapticService>().Library = library;

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = spawn;
            var checkpoint = spawnPoint.AddComponent<CheckPoint>();

            var level = managers.AddComponent<LevelManager>();
            level.PlayerPrefabs = new[] { playerPrefab.GetComponent<Character>() };
            level.InitialSpawnPoint = checkpoint;
            level.UseLevelBounds = false;
            level.IntroFadeDuration = 0f;
            level.SpawnDelay = 0f;
            return managers;
        }

        internal static PixelPerfectCamera Cameras(Color background)
        {
            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = ReferenceHeight / 2f / MinifantasySheets.PixelsPerUnit;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<UniversalAdditionalCameraData>();
            var pixelPerfect = cameraGo.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = MinifantasySheets.PixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceWidth;
            pixelPerfect.refResolutionY = ReferenceHeight;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            ConfigureFollow(cameraGo.AddComponent<CinemachineBrain>(), null);

            var follow = new GameObject("Follow Camera");
            follow.transform.position = new Vector3(0f, 0f, -10f);
            var virtualCamera = follow.AddComponent<CinemachineCamera>();
            virtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            virtualCamera.Lens.OrthographicSize = camera.orthographicSize;
            ConfigureFollow(null, follow.AddComponent<CinemachinePositionComposer>());
            follow.AddComponent<CinemachinePixelPerfect>();
            follow.AddComponent<CinemachineImpulseListener>();
            follow.AddComponent<PlayerCameraTarget>();

            var shake = new GameObject("Screen Shake");
            var source = shake.AddComponent<CinemachineImpulseSource>();
            source.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            source.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            source.ImpulseDefinition.ImpulseDuration = 0.15f;
            source.DefaultVelocity = new Vector3(1f, 1f, 0f);
            shake.AddComponent<ScreenShakeListener>();
            return pixelPerfect;
        }

        // ------------------------------------------------------------------ lighting

        // The approved visual baseline: URP 2D lit sprites (GDD §8.1). The look test only needs
        // representative lighting: an ambient (global) light per environment plus a warm local
        // light. Kept deliberately simple; this is not a lighting system.
        static readonly Color k_DungeonAmbient = new(0.52f, 0.6f, 0.86f);
        const float k_DungeonAmbientIntensity = 0.62f;
        static readonly Color k_TorchColor = new(1f, 0.56f, 0.26f);
        const float k_TorchIntensity = 1.25f;
        const float k_TorchRadius = 6.5f;

        static readonly Color k_TavernAmbient = new(1f, 0.86f, 0.68f);
        const float k_TavernAmbientIntensity = 0.78f;
        static readonly Color k_HearthColor = new(1f, 0.68f, 0.38f);
        const float k_HearthIntensity = 0.9f;
        const float k_HearthRadius = 7f;

        /// <summary>
        /// Camera follow for the pixel-perfect pipeline. The Pixel Perfect Camera snaps the view to
        /// the art-pixel grid; Cinemachine only positions it. Two rules keep the world from
        /// shaking by a pixel as the player moves:
        /// - The brain updates in LateUpdate: the player is an interpolated rigidbody whose
        ///   transform changes every rendered frame, so SmartUpdate or FixedUpdate would let the
        ///   camera lag or stall on frames without a physics step.
        /// - The follow has no damping: a damped camera trails the player by a fractional amount
        ///   that changes frame to frame, so the player and the camera round to different pixels on
        ///   alternate frames and, with the eye on the player, the whole room appears to shake.
        /// </summary>
        static void ConfigureFollow(CinemachineBrain brain, CinemachinePositionComposer composer)
        {
            if (brain != null)
            {
                brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
                brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
                EditorUtility.SetDirty(brain);
            }
            if (composer != null)
            {
                composer.Damping = Vector3.zero;
                EditorUtility.SetDirty(composer);
            }
        }

        /// <summary>
        /// Applies the camera follow rules to the existing look test in place (no rebuild): the player
        /// prefabs get <see cref="PixelSnappedPresentation"/>, and the scenes' brain and follow are set.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Look Test Camera", priority = 23)]
        public static void UpdateLookTestCamera()
        {
            foreach (string path in new[] { LookTestContent.PlayerPrefab, LookTestContent.TavernPlayerPrefab })
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (contents.GetComponent<PixelSnappedPresentation>() == null) contents.AddComponent<PixelSnappedPresentation>();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            foreach (string path in new[] { EditorPaths.LookTestDungeonScene, EditorPaths.LookTestTavernScene })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (CinemachineBrain brain in Object.FindObjectsByType<CinemachineBrain>(FindObjectsInactive.Include)) ConfigureFollow(brain, null);
                foreach (CinemachinePositionComposer composer in Object.FindObjectsByType<CinemachinePositionComposer>(FindObjectsInactive.Include)) ConfigureFollow(null, composer);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Hearthdelve] Look-test camera follow updated.");
        }

        /// <summary>Batch entry point for <see cref="UpdateLookTestCamera"/>.</summary>
        public static void UpdateLookTestCameraBatch()
        {
            try
            {
                UpdateLookTestCamera();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        internal static Light2D Light(string name, Vector3 position, Light2D.LightType type)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var light = go.AddComponent<Light2D>();
            light.lightType = type;
            return light;
        }

        static void Configure(Light2D light, Color color, float intensity, float radius = 0f)
        {
            light.color = color;
            light.intensity = intensity;
            if (light.lightType == Light2D.LightType.Point)
            {
                light.pointLightInnerRadius = radius * 0.2f;
                light.pointLightOuterRadius = radius;
                light.falloffIntensity = 0.6f;
            }
            // Light every sorting layer: floor, Y-sorted characters and props, and anything above.
            light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            EditorUtility.SetDirty(light);
        }

        /// <summary>Applies the look-test lighting to the open scene: ambient by light type, local lights by name.</summary>
        internal static void ApplyLighting(bool dungeon)
        {
            foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            {
                if (light.lightType == Light2D.LightType.Global)
                    Configure(light, dungeon ? k_DungeonAmbient : k_TavernAmbient, dungeon ? k_DungeonAmbientIntensity : k_TavernAmbientIntensity);
                else if (dungeon && light.name == "Torch Light")
                    Configure(light, k_TorchColor, k_TorchIntensity, k_TorchRadius);
                else if (!dungeon && light.name == "Hearth Glow")
                    Configure(light, k_HearthColor, k_HearthIntensity, k_HearthRadius);
            }
        }

        static void UseLitMaterial(Renderer renderer)
        {
            Material lit = LookTestContent.LitSpriteMaterial;
            if (lit == null || renderer.sharedMaterial == lit) return;
            renderer.sharedMaterial = lit;
            EditorUtility.SetDirty(renderer);
        }

        static void UseLitMaterials(GameObject root)
        {
            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) UseLitMaterial(sprite);
            foreach (TilemapRenderer tiles in root.GetComponentsInChildren<TilemapRenderer>(true)) UseLitMaterial(tiles);
        }

        /// <summary>
        /// Moves the existing look test to the approved lit baseline in place, without rebuilding
        /// scenes or prefabs: every sprite and tilemap gets URP's lit sprite material, and the lights
        /// get their look-test settings.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Look Test Lighting", priority = 22)]
        public static void UpdateLookTestLighting()
        {
            if (LookTestContent.LitSpriteMaterial == null)
                throw new InvalidOperationException("URP has no default 2D material; is the URP 2D asset the default render pipeline?");

            foreach (string path in new[] { LookTestContent.PlayerPrefab, LookTestContent.TavernPlayerPrefab, LookTestContent.SlimePrefab, LookTestContent.PickupPrefab, LookTestContent.CookPrefab })
            {
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    UseLitMaterials(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            foreach (var (path, dungeon) in new[] { (EditorPaths.LookTestDungeonScene, true), (EditorPaths.LookTestTavernScene, false) })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                // Prefab instances take the material from their prefab; only the scene's own renderers change here.
                foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                    if ((renderer is SpriteRenderer || renderer is TilemapRenderer) && !PrefabUtility.IsPartOfPrefabInstance(renderer))
                        UseLitMaterial(renderer);
                ApplyLighting(dungeon);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] Look test moved to lit sprites and lighting updated.");
        }

        /// <summary>Batch entry point for <see cref="UpdateLookTestLighting"/>.</summary>
        public static void UpdateLookTestLightingBatch()
        {
            try
            {
                UpdateLookTestLighting();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static Material TextMaterial()
        {
            const string path = EditorPaths.Art + "/UI/STM_Ultra.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            // Super Text Mesh needs its Ultra shader under URP (CLAUDE.md).
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Clavian/SuperTextMesh/Shaders/URP/STM URP Ultra.shader");
            if (shader == null) return null;
            EditorPaths.Ensure(EditorPaths.Art + "/UI");
            material = new Material(shader) { name = "STM_Ultra" };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static LocalizedSuperText Text(Transform parent, string name, string key, float size, Color color, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 sizeDelta)
        {
            var prefab = Resources.Load<GameObject>("STMPrefabs/Super Text");
            GameObject go = Object.Instantiate(prefab, parent);
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<SuperTextMesh>();
            text._text = string.Empty; // text comes from the string table at runtime
            text.size = size;
            text.color = color;
            text.anchor = anchor;
            text.alignment = anchor is TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter
                ? SuperTextMesh.Alignment.Center
                : SuperTextMesh.Alignment.Left;
            text.autoWrap = rect.rect.width;
            Material material = TextMaterial();
            if (material != null) text.textMaterial = material;

            var localized = go.AddComponent<LocalizedSuperText>();
            localized.Configure(key);
            return localized;
        }

        internal static RectTransform UIRect(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        internal static Canvas Canvas(InputActionAsset actions, out CanvasScaler scaler)
        {
            var go = new GameObject("UI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            // One UI sprite pixel = one canvas unit = one game pixel at the reference resolution.
            scaler.referencePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
            go.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var module = eventSystem.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = actions;
            return canvas;
        }

        internal static void Overlay(Canvas canvas, CanvasScaler scaler, PixelPerfectCamera camera, string hintKey, string otherScene)
        {
            Color light = new(0.95f, 0.92f, 0.85f);
            LocalizedSuperText label = Text(canvas.transform, "Resolution", LocKeys.LookTestResolution, 7f, light, TextAnchor.UpperRight,
                Vector2.one, Vector2.one, Vector2.one, new Vector2(-4f, -3f), new Vector2(150f, 10f));
            Text(canvas.transform, "Hint", hintKey, 6f, light, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(310f, 10f));
            canvas.gameObject.AddComponent<LookTestOverlay>().Configure(camera, scaler, label, otherScene);
        }

        static void Solid(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name) { layer = LayerMask.NameToLayer(Layers.Obstacles) };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        // ------------------------------------------------------------------ dungeon room

        const int k_HalfWidth = 11;   // interior: x from -11 to 10
        const int k_Bottom = -6;      // interior: y from -6 to 4
        const int k_Top = 4;

        static void BuildDungeon(InputActionAsset actions, HapticLibrary library, GameObject player, GameObject slime, GameObject pickup,
            HarvestRulesConfig harvestRules, WeaponDefinition weapon)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject managers = Managers(HearthdelveInputManager.GameplayMap.Dungeon, library, player, new Vector2(-7f, -2f));
            managers.AddComponent<HarvestSystem>().Configure(harvestRules, weapon, pickup.GetComponent<IngredientPickup>());
            PixelPerfectCamera camera = Cameras(new Color(0.05f, 0.05f, 0.07f));

            // Cool, dim room light with warm torches (GDD §8.1).
            Light("Global Light 2D", Vector3.zero, Light2D.LightType.Global);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            Tilemap floor = Layer(grid, "Floor", SortingLayers.Floor, 0, false);
            Tilemap walls = Layer(grid, "Walls", SortingLayers.Floor, 1, true);

            // Dungeon tileset cells (column, row from the top-left); see docs/ASSET_MAP.md.
            Tile[] floorTiles =
            {
                LookTestContent.DungeonTile(13, 2, false), LookTestContent.DungeonTile(14, 2, false),
                LookTestContent.DungeonTile(15, 2, false), LookTestContent.DungeonTile(16, 2, false), LookTestContent.DungeonTile(18, 2, false),
            };
            Tile[] topEdge = { LookTestContent.DungeonTile(5, 5, true), LookTestContent.DungeonTile(6, 5, true) };
            Tile[] bricks = { LookTestContent.DungeonTile(5, 6, true), LookTestContent.DungeonTile(6, 6, true) };
            Tile[] bottomEdge = { LookTestContent.DungeonTile(5, 11, true), LookTestContent.DungeonTile(6, 11, true) };
            Tile[] bottomBricks = { LookTestContent.DungeonTile(5, 12, true), LookTestContent.DungeonTile(6, 12, true) };
            int left = -k_HalfWidth - 1, right = k_HalfWidth;

            for (int x = -k_HalfWidth; x < k_HalfWidth; x++)
            {
                for (int y = k_Bottom; y <= k_Top + 1; y++)
                {
                    // Mostly plain flagstones, with the odd cracked one.
                    int roll = Mathf.Abs(x * 7349 + y * 9151) % 17;
                    floor.SetTile(new Vector3Int(x, y, 0), floorTiles[roll < 12 ? roll % 2 : 2 + roll % 3]);
                }
                int alt = Mathf.Abs(x) % 2;
                walls.SetTile(new Vector3Int(x, k_Top + 2, 0), topEdge[alt]);
                walls.SetTile(new Vector3Int(x, k_Top + 1, 0), bricks[alt]);
                walls.SetTile(new Vector3Int(x, k_Bottom - 1, 0), bottomEdge[alt]);
                walls.SetTile(new Vector3Int(x, k_Bottom - 2, 0), bottomBricks[alt]);
            }
            walls.SetTile(new Vector3Int(left, k_Top + 2, 0), LookTestContent.DungeonTile(4, 5, true));
            walls.SetTile(new Vector3Int(right, k_Top + 2, 0), LookTestContent.DungeonTile(10, 5, true));
            walls.SetTile(new Vector3Int(left, k_Top + 1, 0), LookTestContent.DungeonTile(4, 6, true));
            walls.SetTile(new Vector3Int(right, k_Top + 1, 0), LookTestContent.DungeonTile(10, 6, true));
            for (int y = k_Bottom; y <= k_Top; y++)
            {
                walls.SetTile(new Vector3Int(left, y, 0), LookTestContent.DungeonTile(4, 7, true));
                walls.SetTile(new Vector3Int(right, y, 0), LookTestContent.DungeonTile(10, 7, true));
            }
            walls.SetTile(new Vector3Int(left, k_Bottom - 1, 0), LookTestContent.DungeonTile(4, 11, true));
            walls.SetTile(new Vector3Int(right, k_Bottom - 1, 0), LookTestContent.DungeonTile(10, 11, true));
            walls.SetTile(new Vector3Int(left, k_Bottom - 2, 0), LookTestContent.DungeonTile(4, 12, true));
            walls.SetTile(new Vector3Int(right, k_Bottom - 2, 0), LookTestContent.DungeonTile(10, 12, true));

            // Bake the wall outline now; a composite saved without geometry has no collision.
            walls.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            walls.GetComponent<CompositeCollider2D>().GenerateGeometry();

            // Props sort by Y against the characters and block movement.
            var props = new GameObject("Props").transform;
            Prop(props, "Crate", new Vector2(-9.5f, 3f));
            Prop(props, "Barrel", new Vector2(-10.5f, 3f));
            Prop(props, "BarrelOpen", new Vector2(-10.5f, 1.8f));
            Prop(props, "Table", new Vector2(6f, -3f));
            Prop(props, "Cauldron", new Vector2(8f, 2.5f));
            Prop(props, "Statue", new Vector2(-4.5f, 4f));
            Prop(props, "Statue", new Vector2(3.5f, 4f));

            Sprite[] torchFrames = MinifantasyImporter.Row(MinifantasySheets.Dungeon, "Torch", 0, 8);
            foreach (float x in new[] { -6.5f, 5.5f })
            {
                SpriteRenderer torch = LookTestContent.AddSprite(props, "Torch", torchFrames[0], SortingLayers.Floor, 2, new Vector3(x, k_Top - 0.4f, 0f));
                torch.gameObject.AddComponent<SpriteLoop>().Configure(torchFrames, 0.2f);
                Light("Torch Light", new Vector3(x, k_Top + 1.5f, 0f), Light2D.LightType.Point);
            }

            var enemies = new GameObject("Enemies").transform;
            foreach (Vector2 position in new[] { new Vector2(4f, 1f), new Vector2(7f, -4f) })
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(slime, enemies);
                instance.transform.position = position;
            }

            Canvas canvas = Canvas(actions, out CanvasScaler scaler);
            AddEssenceBar(canvas);
            Overlay(canvas, scaler, camera, LocKeys.LookTestHintDungeon, Path.GetFileNameWithoutExtension(EditorPaths.LookTestTavernScene));

            ApplyLighting(dungeon: true);
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, EditorPaths.LookTestDungeonScene);
        }

        /// <summary>The placeholder Essence bar and its label, top left.</summary>
        internal static void AddEssenceBar(Canvas canvas)
        {
            RectTransform bar = UIRect(canvas.transform, "PH_EssenceBar", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(64f, 5f));
            var back = bar.gameObject.AddComponent<Image>();
            back.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
            RectTransform fillRect = UIRect(bar, "Fill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(62f, 3f));
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.color = new Color(0.45f, 0.85f, 0.95f);
            bar.gameObject.AddComponent<EssenceBar>().Configure(fill);
            Text(canvas.transform, "EssenceLabel", LocKeys.HudEssence, 6f, new Color(0.95f, 0.92f, 0.85f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -10f), new Vector2(80f, 10f));
        }

        internal static Tilemap Layer(Grid grid, string name, string sortingLayer, int order, bool solid)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            // Cell (x, y) covers world x..x+1, y..y+1, so tile anchors stay at the cell centre.
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            if (LookTestContent.LitSpriteMaterial != null) renderer.sharedMaterial = LookTestContent.LitSpriteMaterial;
            if (solid)
            {
                go.layer = LayerMask.NameToLayer(Layers.Obstacles);
                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Static;
                var composite = go.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
                composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
                var collider = go.AddComponent<TilemapCollider2D>();
                collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
            return tilemap;
        }

        internal static void Prop(Transform parent, string name, Vector2 position)
        {
            Sprite sprite = MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Props", name);
            SpriteRenderer renderer = LookTestContent.AddSprite(parent, name, sprite, SortingLayers.YSorted, 0, position);
            renderer.gameObject.layer = LayerMask.NameToLayer(Layers.Obstacles);
            if (sprite == null) return;
            // Only the base blocks movement, so characters can stand "behind" the top of a prop.
            Vector2 size = sprite.bounds.size;
            var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(size.x * 0.9f, Mathf.Min(size.y, 0.6f));
            collider.offset = new Vector2(0f, collider.size.y * 0.5f);
        }

        // ------------------------------------------------------------------ tavern corner

        /// <summary>Sheet pixel (from the top-left of the 320×104 Tavern Indoor sheet) to room-centred world units.</summary>
        static Vector2 TavernPoint(float x, float y) =>
            new(x / MinifantasySheets.PixelsPerUnit - 33.5f, (104f - y) / MinifantasySheets.PixelsPerUnit - 6.5f);

        static SpriteRenderer TavernSprite(Transform parent, string layer, string name, string sortingLayer, int order)
        {
            Sheet sheet = MinifantasySheets.All.First(s => s.File == $"TavernIndoor_{layer}");
            SheetRect rect = sheet.Rects.First(r => r.Name == name);
            Sprite sprite = MinifantasyImporter.Sprite(MinifantasySheets.TavernIndoor, sheet.File, name);
            // Sprites pivot at their bottom-left, so each lands exactly where the premade room has it.
            Vector2 position = TavernPoint(rect.Rect.x, rect.Rect.y + rect.Rect.height);
            return LookTestContent.AddSprite(parent, name, sprite, sortingLayer, order, position);
        }

        static void TavernSolid(Transform parent, string name, float x0, float y0, float x1, float y1)
        {
            Vector2 a = TavernPoint(x0, y1), b = TavernPoint(x1, y0);
            Solid(parent, name, (a + b) * 0.5f, b - a);
        }

        /// <summary>
        /// What blocks movement under each furniture piece, in sheet pixels (x, y from the top-left,
        /// width, height). Every footprint of a piece ends at the bottom of its art, which is also
        /// its Y-sort point: a character stopped in front of a piece is always drawn in front of
        /// it, and one that got behind it is always drawn behind. The bar's footprint covers its
        /// stools, and a table set's covers its side chairs.
        /// </summary>
        static readonly (string piece, RectInt[] footprints)[] k_FurnitureFootprints =
        {
            ("Bar", new[] { new RectInt(246, 38, 55, 14), new RectInt(242, 30, 5, 22) }),
            ("StoolA", new[] { new RectInt(235, 33, 5, 6) }),
            ("StoolB", new[] { new RectInt(235, 40, 5, 6) }),
            ("TableSetA", new[] { new RectInt(234, 64, 20, 16) }),
            ("TableSetB", new[] { new RectInt(282, 64, 20, 16) }),
        };

        /// <summary>Gives each furniture piece its own collision, aligned with its sort point. Replaces any it had.</summary>
        static void AddFurnitureFootprints(Transform furniture)
        {
            int obstacles = LayerMask.NameToLayer(Layers.Obstacles);
            foreach (var (name, footprints) in k_FurnitureFootprints)
            {
                Transform piece = furniture.Find(name);
                if (piece == null)
                {
                    Debug.LogError($"[Hearthdelve] Furniture '{name}' not found.");
                    continue;
                }
                foreach (BoxCollider2D old in piece.GetComponents<BoxCollider2D>()) Object.DestroyImmediate(old);
                piece.gameObject.layer = obstacles;
                foreach (RectInt r in footprints)
                {
                    Vector2 bottomLeft = TavernPoint(r.x, r.yMax), topRight = TavernPoint(r.xMax, r.y);
                    var box = piece.gameObject.AddComponent<BoxCollider2D>();
                    box.size = topRight - bottomLeft;
                    box.offset = (bottomLeft + topRight) * 0.5f - (Vector2)piece.position;
                }
            }
        }

        /// <summary>
        /// Applies the furniture footprints to the existing tavern scene in place, without
        /// rebuilding it: the old shared collision boxes go, each piece gets its own.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Tavern Furniture Collision", priority = 20)]
        public static void UpdateTavernFurnitureCollision()
        {
            var scene = EditorSceneManager.OpenScene(EditorPaths.LookTestTavernScene, OpenSceneMode.Single);
            foreach (string old in new[] { "Bar Counter", "Bar Return", "Table A", "Table B" })
            {
                GameObject box = GameObject.Find($"Collision/{old}");
                if (box != null) Object.DestroyImmediate(box);
            }
            GameObject furniture = GameObject.Find("Furniture");
            if (furniture == null) throw new System.InvalidOperationException("The tavern scene has no Furniture object.");
            AddFurnitureFootprints(furniture.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Tavern furniture collision updated.");
        }

        /// <summary>Batch entry point for <see cref="UpdateTavernFurnitureCollision"/>.</summary>
        public static void UpdateTavernFurnitureCollisionBatch()
        {
            try
            {
                UpdateTavernFurnitureCollision();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void BuildTavern(InputActionAsset actions, HapticLibrary library, GameObject player, GameObject cook)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Managers(HearthdelveInputManager.GameplayMap.Tavern, library, player, TavernPoint(268f, 88f));
            PixelPerfectCamera camera = Cameras(new Color(0.07f, 0.05f, 0.05f));

            // Warm candlelight (GDD §8.1).
            Light("Global Light 2D", Vector3.zero, Light2D.LightType.Global);
            Light("Hearth Glow", TavernPoint(268f, 40f), Light2D.LightType.Point);

            var room = new GameObject("Room").transform;
            TavernSprite(room, "base_building", "Room", SortingLayers.Background, 0);
            TavernSprite(room, "floor2", "Floor", SortingLayers.Floor, 0);
            TavernSprite(room, "wall", "Wall", SortingLayers.Floor, 1);
            TavernSprite(room, "shadows", "Shadows", SortingLayers.Floor, 2);
            TavernSprite(room, "props", "Shelves", SortingLayers.Floor, 3);
            TavernSprite(room, "props2", "ShelfGoods", SortingLayers.Floor, 4);
            TavernSprite(room, "props", "Sign", SortingLayers.Floor, 3);

            // Furniture sorts by Y against the characters. The bar and what stands on it sort as one.
            var furniture = new GameObject("Furniture").transform;
            SpriteRenderer bar = TavernSprite(furniture, "props", "Bar", SortingLayers.YSorted, 0);
            bar.gameObject.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            SpriteRenderer barTop = TavernSprite(bar.transform, "props2", "BarTop", SortingLayers.YSorted, 1);
            barTop.transform.position = TavernPoint(244f, 43f);
            TavernSprite(furniture, "props", "StoolA", SortingLayers.YSorted, 0);
            TavernSprite(furniture, "props", "StoolB", SortingLayers.YSorted, 0);
            TavernSprite(furniture, "props", "TableSetA", SortingLayers.YSorted, 0);
            TavernSprite(furniture, "props", "TableSetB", SortingLayers.YSorted, 0);

            var solids = new GameObject("Collision").transform;
            TavernSolid(solids, "Wall North", 220f, 0f, 316f, 33f);
            TavernSolid(solids, "Wall South", 220f, 96f, 316f, 104f);
            TavernSolid(solids, "Wall West", 220f, 0f, 228f, 104f);
            TavernSolid(solids, "Wall East", 308f, 0f, 316f, 104f);
            AddFurnitureFootprints(furniture);

            var cookInstance = (GameObject)PrefabUtility.InstantiatePrefab(cook);
            cookInstance.transform.position = TavernPoint(276f, 37f);

            Canvas canvas = Canvas(actions, out CanvasScaler scaler);
            RectTransform bubbleRoot = UIRect(canvas.transform, "SpeechBubble", Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            RectTransform visual = UIRect(bubbleRoot, "Visual", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(96f, 13f));
            var body = visual.gameObject.AddComponent<Image>();
            body.sprite = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Bubble", "Body");
            body.type = Image.Type.Sliced;
            RectTransform tail = UIRect(visual, "Tail", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(4f, 3f));
            tail.gameObject.AddComponent<Image>().sprite = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Bubble", "Tail");
            Text(visual, "Text", LocKeys.LookTestGreeting, 6f, new Color(0.25f, 0.16f, 0.1f), TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -2f));
            bubbleRoot.gameObject.AddComponent<SpeechBubble>().Configure(cookInstance.transform, visual.gameObject, new Vector2(0f, 1.4f), 5f);
            Overlay(canvas, scaler, camera, LocKeys.LookTestHintTavern, Path.GetFileNameWithoutExtension(EditorPaths.LookTestDungeonScene));

            ApplyLighting(dungeon: false);
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, EditorPaths.LookTestTavernScene);
        }
    }
}
