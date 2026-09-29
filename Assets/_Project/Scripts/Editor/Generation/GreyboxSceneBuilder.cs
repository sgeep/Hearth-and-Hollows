using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon.Cameras;
using Hearthdelve.Dungeon.DebugTools;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Screens;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the CombatGreybox test level: a tilemap course that exercises every Phase 1
    /// move (wall-jump shaft, one-way platform tower), the three Cellars enemies, a training
    /// dummy, pixel-perfect Cinemachine camera with impulse shake, HUD and menus.
    /// </summary>
    public static class GreyboxSceneBuilder
    {
        const int k_LevelWidth = 100;
        const int k_LevelTop = 24;
        const int k_FloorBottom = -4;

        /// <param name="overwriteApproved">Must be true to replace an existing scene (CLAUDE.md: never overwrite a scene without asking).</param>
        public static void Build(ContentGenerator.Content content, PrefabGenerator.Prefabs prefabs, InputActionAsset actions, bool overwriteApproved)
        {
            if (System.IO.File.Exists(EditorPaths.GreyboxScene) && !overwriteApproved)
            {
                Debug.Log("[Hearthdelve] CombatGreybox exists and overwrite wasn't approved; leaving it untouched.");
                return;
            }
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorPaths.Ensure(EditorPaths.Tiles);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var (solidTile, oneWayTile) = CreateTiles();
            var (solid, oneWay) = CreateTilemaps();
            PaintLevel(solid, solidTile, oneWay, oneWayTile);
            var bounds = CreateCameraBounds();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player);
            player.transform.position = Cell(5, 0);

            Spawn(prefabs.Dummy, 13, 0);
            Spawn(prefabs.Rat, 40, 0);
            Spawn(prefabs.Slime, 54, 0);
            Spawn(prefabs.Rat, 68, 0);
            Spawn(prefabs.Rat, 75, 0);
            Spawn(prefabs.Shroom, 86, 3);
            Spawn(prefabs.Slime, 94, 3);
            Spawn(prefabs.Shroom, 92, 6);

            CreateCamera(player.transform, bounds);
            CreateSystems(content, prefabs, player.GetComponent<PlayerController>());
            CreateLighting();
            CreateUI(actions);

            EditorSceneManager.SaveScene(scene, EditorPaths.GreyboxScene);
            SceneKit.AddToBuild(EditorPaths.GreyboxScene);
        }

        static Vector3 Cell(int x, int y) => new(x + 0.5f, y, 0f);

        static void Spawn(GameObject prefab, int x, int y)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = Cell(x, y);
        }

        static (Tile solid, Tile oneWay) CreateTiles()
        {
            var solid = ContentGenerator.LoadOrCreate<Tile>($"{EditorPaths.Tiles}/Tile_Solid.asset", t => t.colliderType = Tile.ColliderType.Grid);
            solid.sprite = PlaceholderArtGenerator.Load(PlaceholderArtGenerator.TileSolid);
            // Sprite collider: only the plank at the top of the cell is solid.
            var oneWay = ContentGenerator.LoadOrCreate<Tile>($"{EditorPaths.Tiles}/Tile_OneWay.asset", t => t.colliderType = Tile.ColliderType.Sprite);
            oneWay.sprite = PlaceholderArtGenerator.Load(PlaceholderArtGenerator.TileOneWay);
            EditorUtility.SetDirty(solid);
            EditorUtility.SetDirty(oneWay);
            return (solid, oneWay);
        }

        static (Tilemap solid, Tilemap oneWay) CreateTilemaps()
        {
            var grid = new GameObject("Level Grid", typeof(Grid));

            var solidGo = new GameObject("Solid", typeof(Tilemap), typeof(TilemapRenderer)) { layer = LayerMask.NameToLayer(Layers.Ground) };
            solidGo.transform.SetParent(grid.transform, false);
            solidGo.GetComponent<TilemapRenderer>().sortingLayerName = SortingLayers.Level;
            // Plain per-tile colliders. A CompositeCollider2D here generated no shapes at all for
            // the level shell (one ring-shaped outline), leaving no floor. KinematicMover2D insets
            // its casts, so seams between tiles don't snag.
            solidGo.AddComponent<TilemapCollider2D>();

            var oneWayGo = new GameObject("OneWay", typeof(Tilemap), typeof(TilemapRenderer)) { layer = LayerMask.NameToLayer(Layers.OneWayPlatform) };
            oneWayGo.transform.SetParent(grid.transform, false);
            var oneWayRenderer = oneWayGo.GetComponent<TilemapRenderer>();
            oneWayRenderer.sortingLayerName = SortingLayers.Level;
            oneWayRenderer.sortingOrder = 1;
            var oneWayCollider = oneWayGo.AddComponent<TilemapCollider2D>();
            oneWayCollider.usedByEffector = true;
            var effector = oneWayGo.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;

            return (solidGo.GetComponent<Tilemap>(), oneWayGo.GetComponent<Tilemap>());
        }

        /// <summary>Tile coords: floor surface at y = 0 (ground tiles occupy y â¤ -1).</summary>
        static void PaintLevel(Tilemap solid, Tile s, Tilemap oneWay, Tile o)
        {
            void Box(int x0, int y0, int x1, int y1)
            {
                for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    solid.SetTile(new Vector3Int(x, y, 0), s);
            }
            void Plat(int x0, int x1, int y)
            {
                for (int x = x0; x <= x1; x++) oneWay.SetTile(new Vector3Int(x, y, 0), o);
            }

            // Shell.
            Box(0, k_FloorBottom, k_LevelWidth, -1);
            Box(0, k_FloorBottom, 1, k_LevelTop);
            Box(k_LevelWidth - 1, k_FloorBottom, k_LevelWidth, k_LevelTop);
            Box(0, k_LevelTop - 1, k_LevelWidth, k_LevelTop);

            // 1. Start: training dummy and a one-tile step.
            Box(17, 0, 19, 0);

            // 2. Wall-jump shaft: 3-wide chimney with walk-through gaps at the bottom.
            //    Every step below is ≤ 3 tiles because a full jump reaches 3.2.
            Box(23, 3, 24, 15);
            Box(28, 3, 29, 17);
            Box(30, 17, 36, 17);                 // upper ledge off the top of the shaft (surface y = 18)

            // 3. One-way platform tower (drop through with Down + Jump). Surfaces at 3, 6, 9, 12, 15.
            Plat(32, 36, 2);
            Plat(38, 42, 5);
            Plat(33, 37, 8);
            Plat(39, 44, 11);
            Plat(37, 41, 14);                    // hop left from here onto the upper ledge

            // 4. Arena: cover pillar and two overhead platforms.
            Box(59, 0, 60, 1);
            Plat(51, 55, 2);
            Plat(64, 68, 2);

            // 5. Shroom perch: a step, a raised block (surface 3), and a higher platform (surface 6).
            Plat(76, 78, 1);
            Box(80, 0, 97, 2);
            Plat(89, 95, 5);
        }

        static PolygonCollider2D CreateCameraBounds()
        {
            var go = new GameObject("Camera Bounds") { layer = 2 }; // Ignore Raycast
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.isTrigger = true;
            float l = 0f, r = k_LevelWidth + 1, b = k_FloorBottom, t = k_LevelTop + 1;
            poly.points = new[] { new Vector2(l, b), new Vector2(r, b), new Vector2(r, t), new Vector2(l, t) };
            return poly;
        }

        static void CreateCamera(Transform player, Collider2D bounds)
        {
            float orthoSize = SceneKit.OrthoSize;
            var camGo = SceneKit.CreatePixelCamera(new Vector3(player.position.x, player.position.y, -10f), new Color(0.08f, 0.09f, 0.12f), withCinemachineBrain: true).gameObject;

            var vcamGo = new GameObject("CM Follow Camera");
            vcamGo.transform.position = camGo.transform.position;
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Target.TrackingTarget = player;
            vcam.Lens.OrthographicSize = orthoSize;

            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.35f, 0.3f, 0f);
            composer.Lookahead = new LookaheadSettings { Enabled = true, Time = 0.15f, Smoothing = 5f, IgnoreY = true };
            composer.Composition.ScreenPosition = new Vector2(0f, -0.1f);
            composer.Composition.DeadZone.Enabled = true;
            composer.Composition.DeadZone.Size = new Vector2(0.08f, 0.12f);

            vcamGo.AddComponent<CinemachineConfiner2D>().BoundingShape2D = bounds;
            vcamGo.AddComponent<CinemachinePixelPerfect>();

            var listener = vcamGo.AddComponent<CinemachineImpulseListener>();
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.Use2DDistance = true;
            listener.UseCameraSpace = true;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction { AmplitudeGain = 1f, FrequencyGain = 1f, Duration = 1f };
        }

        static void CreateSystems(ContentGenerator.Content content, PrefabGenerator.Prefabs prefabs, PlayerController player)
        {
            var systems = new GameObject("Systems");
            systems.AddComponent<HitStopDriver>();
            systems.AddComponent<HarvestSystem>().Configure(content.HarvestRules, prefabs.Pickup);
            systems.AddComponent<DelveRunController>().Configure(content.Delve);
            systems.AddComponent<DungeonDebugOverlay>().Configure(player);

            var shakeGo = new GameObject("Screen Shake");
            shakeGo.transform.SetParent(systems.transform, false);
            var source = shakeGo.AddComponent<CinemachineImpulseSource>();
            source.ImpulseDefinition.ImpulseChannel = 1;
            source.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            source.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            source.ImpulseDefinition.ImpulseDuration = 0.15f;
            source.DefaultVelocity = new Vector3(0f, -1f, 0f);
            shakeGo.AddComponent<ScreenShaker>();
        }

        static void CreateLighting() => SceneKit.CreateGlobalLight();

        static void CreateUI(InputActionAsset actions)
        {
            var panel = SceneKit.PanelSettings();
            var ui = new GameObject("UI");
            SceneKit.AddDocument<DungeonHud>(ui.transform, "HUD", panel, "DungeonHud.uxml", 0);
            SceneKit.AddDocument<SwapPrompt>(ui.transform, "Swap Prompt", panel, "SwapPrompt.uxml", 5);
            SceneKit.AddDocument<DeathScreen>(ui.transform, "Death Screen", panel, "DeathScreen.uxml", 10);
            SceneKit.CreateEventSystem(actions);
        }
    }
}
