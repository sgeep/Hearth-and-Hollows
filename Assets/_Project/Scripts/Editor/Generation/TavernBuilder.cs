using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using Hearthdelve.UI.Typography;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the 4c <c>Tavern</c> scene: the Stage 1 inn (GDD §6.4), one screen of 28×17 tiles.
    /// The room is the Tavern Indoor premade room stretched cell by cell (its interior columns and
    /// floor rows repeat exactly). Since 4f step 1 the furniture is data: the scene holds the room's
    /// shell, its area (<see cref="PropertyArea"/>) and the builder of what stands in it
    /// (<see cref="AreaFurniture"/>, from <see cref="FurnitureContent"/>'s definitions and starting
    /// layout). The walkable grid is baked from the colliders and rebuilt when the layout changes
    /// (<see cref="NavGrid"/>). The camera holds still on the room. <c>LookTest_Tavern</c> is not
    /// touched. The scene is created when missing and never overwritten without approval (a dialog in
    /// the editor; <c>-rebuildScene</c> in batch mode); <see cref="UpdateTavern"/> changes it in place.
    /// </summary>
    public static class TavernBuilder
    {
        public const int Width = 28;
        public const int Height = 17;
        /// <summary>The column of the front door (the premade room's door column, 5).</summary>
        public const int DoorColumn = 13;
        /// <summary>Walkable floor: x from 0.5 to 27.5, y from 2 to 14 (the back wall's foot).</summary>
        public const float FloorBottom = 2f;
        public const float FloorTop = 14f;

        public static readonly Vector2 Spawn = new(13.5f, 5.5f);
        /// <summary>
        /// Where the camera holds still: the middle of the room, so the whole room stays on screen at any
        /// aspect from 16:9 down to 5:4 (an off-centre view lost a tile on 16:10 screens).
        /// </summary>
        public static readonly Vector2 CameraCentre = new(Width / 2f, Height / 2f);

        static readonly Color k_Ambient = new(1f, 0.86f, 0.68f);

        /// <summary>Furniture and lights the scene held before 4f made them data; the updater removes them.</summary>
        static readonly string[] k_SceneFurniture = { "Furniture", "Wall Decor", "Seats" };
        static readonly string[] k_FurnitureLights = { "Hearth Glow", "Kitchen Fire", "Stew Fire", "Bar Lamp" };

        /// <summary>
        /// Applies builder changes to the existing tavern scene in place, without rebuilding it (D23):
        /// the camera's resting point; the furniture as data (4f step 1: the old scene pieces, seats and
        /// their lights are removed, and the area with its furniture builder is added once); the service
        /// (the door, the queue, Orik, the keeper's work) and Decorate Mode, rebuilt each time; the UI. Idempotent: running
        /// it again changes nothing it made, adds nothing twice, and leaves the rest of the scene alone.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Tavern", priority = 3)]
        public static void UpdateTavern()
        {
            ProjectConfigurator.ConfigureAll();
            InputActionsBuilder.Build(force: false);
            MinifantasyImporter.ImportAll();
            // 4f Checkpoint C: the market staples, butchered cuts and the Biome 1 menu.
            CellarMenuContent.Build();
            StaffContent.Build();
            LocalizationBuilder.Build();
            LookTestContent.BuildHaptics();
            TavernStationContent.AssignDishIcons();
            NpcContent.Built npcs = NpcContent.Build();
            AddCarryViewToPlayer();
            var scene = EditorSceneManager.OpenScene(EditorPaths.TavernScene, OpenSceneMode.Single);
            var tavernCamera = GameObject.Find("Tavern Camera");
            if (tavernCamera == null) throw new InvalidOperationException("The tavern scene has no Tavern Camera.");
            tavernCamera.transform.position = new Vector3(CameraCentre.x, CameraCentre.y, -10f);
            Camera.main.transform.position = tavernCamera.transform.position;
            FurnitureContent.Built furniture = BuildFurniture();
            RemoveSceneFurniture();
            AddService(npcs);
            AddArea(furniture);
            AddDecorate();
            TavernFeedbackContent.Build(GameObject.Find("Service").transform);
            Canvas ui = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).First(c => c.name == "UI");
            TavernStationContent.BuildStationPanel(ui);
            BuildHint(ui);
            TavernScreens.Rebuild(ui);
            GuestRoomBuilder.BuildFade(ui);
            AddMood();
            AddOpeningHatch();
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Tavern updated.");
        }

        /// <summary>Batch entry point for <see cref="UpdateTavern"/>.</summary>
        public static void UpdateTavernBatch()
        {
            try
            {
                UpdateTavern();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Hearthdelve/Generate/4c Tavern", priority = 2)]
        public static void GenerateMenu() => Generate(rebuildSceneApproved: false);

        /// <summary>Batch entry point: <c>-executeMethod Hearthdelve.Editor.TavernBuilder.RunBatch [-rebuildScene]</c>.</summary>
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

        public static void Generate(bool rebuildSceneApproved)
        {
            LookTestBuilder.Content content = LookTestBuilder.BuildContent();
            TavernStationContent.AssignDishIcons();
            NpcContent.Built npcs = NpcContent.Build();
            AddCarryViewToPlayer();
            if (LookTestBuilder.MayWrite(EditorPaths.TavernScene, rebuildSceneApproved)) BuildScene(content, npcs);
            ProjectConfigurator.SetBuildOrder(EditorPaths.TestFloorScene, EditorPaths.TavernScene, EditorPaths.LookTestDungeonScene, EditorPaths.LookTestTavernScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] 4c tavern generated.");
        }

        static void BuildScene(LookTestBuilder.Content content, NpcContent.Built npcs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject managers = LookTestBuilder.Managers(HearthdelveInputManager.GameplayMap.Tavern, content.Library, content.TavernPlayer, Spawn);
            managers.AddComponent<NavGrid>().Configure(new RectInt(0, 0, Width, Height), LayerMask.GetMask(Layers.Obstacles));
            LookTestBuilder.Cameras(new Color(0.07f, 0.05f, 0.05f));
            HoldCamera();

            BuildRoom();
            BuildLights();
            AddService(npcs);
            AddArea(BuildFurniture());
            AddDecorate();
            TavernFeedbackContent.Build(GameObject.Find("Service").transform);

            Canvas canvas = LookTestBuilder.Canvas(content.Actions, out _);
            BuildHint(canvas);
            TavernStationContent.BuildStationPanel(canvas);
            LocalizedSuperText controls = LookTestBuilder.Text(canvas.transform, "Controls", TavernLocKeys.TavernControls, TextStyle.Prompt, new Color(0.95f, 0.92f, 0.85f),
                TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(310f, 10f));
            controls.gameObject.AddComponent<Hearthdelve.UI.Debugging.FadeOutAfter>();
            TavernScreens.Rebuild(canvas);
            AddMood();

            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, EditorPaths.TavernScene);
        }

        /// <summary>Decision 1 (4c): the Stage 1 room fits one screen, so the camera holds still on it.</summary>
        static void HoldCamera()
        {
            GameObject follow = GameObject.Find("Follow Camera");
            Object.DestroyImmediate(follow.GetComponent<PlayerCameraTarget>());
            Object.DestroyImmediate(follow.GetComponent<CinemachinePositionComposer>());
            follow.name = "Tavern Camera";
            follow.transform.position = new Vector3(CameraCentre.x, CameraCentre.y, -10f);
            Camera.main.transform.position = follow.transform.position;
        }

        // ------------------------------------------------------------------ room

        /// <summary>The premade cell (column, row) a room tile is cut from: edges from the edges, the rest repeats the middle.</summary>
        static int SourceColumn(int column, bool doorRow) =>
            column == 0 ? 0 : column == Width - 1 ? 10 : doorRow && column == DoorColumn ? 5 : 2;

        static int SourceRow(int row) => row <= 2 ? row : row == Height - 2 ? 9 : row == Height - 1 ? 10 : 5;

        static void BuildRoom()
        {
            var grid = new GameObject("Room").AddComponent<Grid>();
            Tilemap floor = LookTestBuilder.Layer(grid, "Floor", SortingLayers.Floor, 0, solid: false);
            Tilemap shell = LookTestBuilder.Layer(grid, "Shell", SortingLayers.Floor, 1, solid: false);
            Tilemap wall = LookTestBuilder.Layer(grid, "Wall", SortingLayers.Floor, 2, solid: false);
            for (int row = 0; row < Height; row++)
            for (int column = 0; column < Width; column++)
            {
                var cell = new Vector3Int(column, Height - 1 - row, 0);
                bool backWall = row <= 2, frontWall = row >= Height - 2, side = column == 0 || column == Width - 1;
                int sc = SourceColumn(column, frontWall), sr = SourceRow(row);
                if (backWall) wall.SetTile(cell, RoomTile("wall", sc, sr));
                if (!backWall && !frontWall) floor.SetTile(cell, RoomTile("floor2", sc, 5));
                if (backWall || frontWall || side) shell.SetTile(cell, RoomTile("base_building", sc, sr));
            }

            // Solid walls: the sides are half a tile thick, the back wall ends at its panelling's foot, the front at its top.
            var solids = new GameObject("Walls").transform;
            LookTestBuilder.Solid(solids, "West", new Vector2(0.25f, Height / 2f), new Vector2(0.5f, Height));
            LookTestBuilder.Solid(solids, "East", new Vector2(Width - 0.25f, Height / 2f), new Vector2(0.5f, Height));
            LookTestBuilder.Solid(solids, "North", new Vector2(Width / 2f, (FloorTop + Height) / 2f), new Vector2(Width, Height - FloorTop));
            LookTestBuilder.Solid(solids, "South", new Vector2(Width / 2f, FloorBottom / 2f), new Vector2(Width, FloorBottom));
        }

        static Tile RoomTile(string layer, int column, int row)
        {
            Sprite sprite = MinifantasyImporter.Sprite(MinifantasySheets.TavernIndoor, $"TavernIndoor_{layer}", $"Cell_{column}_{row}");
            return LookTestContent.CreateOrUpdate<Tile>($"{EditorPaths.Tiles}/Tavern_{layer}_{column}_{row}.asset", tile =>
            {
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
            });
        }

        // ------------------------------------------------------------------ service (step 2)

        const string k_ContentPath = EditorPaths.Data + "/Tavern/TavernContent.asset";
        public const int QueueSpots = 6;

        /// <summary>
        /// The evening: the door, the queue (inside the door, running west along the front wall), where staff
        /// rest, the director that runs the service, Orik, the keeper's work and the debug keys. Seats and staff
        /// posts come from the placed furniture at runtime (4f step 1). Replaces any it had.
        /// </summary>
        static void AddService(NpcContent.Built npcs)
        {
            GameObject old = GameObject.Find("Service");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Service").transform;

            Transform Point(string name, Vector2 at)
            {
                var point = new GameObject(name).transform;
                point.SetParent(root, false);
                point.position = at;
                return point;
            }

            Transform door = Point("Door", new Vector2(DoorColumn + 0.5f, FloorBottom + 0.4f));
            var queue = new Transform[QueueSpots];
            for (int i = 0; i < QueueSpots; i++) queue[i] = Point($"Queue{i + 1}", new Vector2(DoorColumn - 0.75f - i, FloorBottom + 0.6f));

            var layout = root.gameObject.AddComponent<TavernLayout>();
            layout.Configure(door, queue, Point("Post Rest", new Vector2(25.5f, 5.5f)));

            var pip = (GameObject)PrefabUtility.InstantiatePrefab(npcs.Pip);
            pip.transform.position = new Vector3(25.5f, 5.5f, 0f);
            // Gunta Ashbelly, the cook (4f Checkpoint C): in the kitchen from the first evening.
            var gunta = (GameObject)PrefabUtility.InstantiatePrefab(npcs.Gunta);
            gunta.transform.position = new Vector3(24.5f, 9.5f, 0f);

            var director = root.gameObject.AddComponent<TavernDirector>();
            director.Configure(AssetDatabase.LoadAssetAtPath<TavernContent>(k_ContentPath), layout, npcs.Customer.GetComponent<CustomerAgent>(), pip.GetComponent<StaffAgent>());
            director.ConfigureCook(gunta.GetComponent<StaffAgent>());
            root.gameObject.AddComponent<TavernDebugKeys>();
            // The keeper's stations, pass and seats are handed over by the area's furniture as it builds.
            root.gameObject.AddComponent<KeeperWork>();

            // Orik from an earlier run of this updater.
            foreach (StaffAgent extra in Object.FindObjectsByType<StaffAgent>(FindObjectsInactive.Include))
                if (extra.gameObject != pip && extra.gameObject != gunta) Object.DestroyImmediate(extra.gameObject);
        }

        // ------------------------------------------------------------------ stations and serving (step 3)

        /// <summary>The keeper carries plates over their head (4c decision 2): added to the tavern player prefab in place.</summary>
        /// <summary>
        /// The cellar hatch for arrival day (4g Checkpoint B): the Dungeon pack's ladder hole on the floor, used like a station. Rebuilt
        /// in place; it shows itself only while the keeper is arriving, on the open tile nearest its spot.
        /// </summary>
        static void AddOpeningHatch()
        {
            GameObject old = GameObject.Find("Opening Hatch");
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject("Opening Hatch");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            LookTestContent.AddSprite(visual.transform, "Hole", MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Holes", "Ladder"), SortingLayers.Floor, 3, Vector3.zero);
            var use = new GameObject("Use");
            use.transform.SetParent(root.transform, false);
            var interactable = use.AddComponent<TavernInteractable>();
            // The keeper stands at its lower edge, as at a station.
            interactable.Configure(TavernInteractableKind.Hatch, TavernLocKeys.Hatch, new Vector2(0f, -0.7f), 1f, null);
            root.AddComponent<OpeningHatch>().Configure(interactable, visual, new Vector2(6.5f, 5.5f));
        }

        internal static void AddCarryViewToPlayer()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(LookTestContent.TavernPlayerPrefab);
            try
            {
                TavernStationContent.AddCarryView(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, LookTestContent.TavernPlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>The area id the tavern room's furniture is saved under.</summary>
        public const string AreaId = PropertyArea.TavernId;

        /// <summary>
        /// Removes what the scene held before the furniture became data (4f step 1): the pieces, the wall decor, the seat
        /// markers and the lights that belonged to pieces. Nothing else. Nothing happens once they're gone.
        /// </summary>
        static void RemoveSceneFurniture()
        {
            foreach (GameObject root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                         .Where(t => t.parent == null && k_SceneFurniture.Contains(t.name)).Select(t => t.gameObject).ToList())
                Object.DestroyImmediate(root);
            GameObject lights = GameObject.Find("Lights");
            if (lights != null)
                foreach (string name in k_FurnitureLights)
                {
                    Transform light = lights.transform.Find(name);
                    if (light != null) Object.DestroyImmediate(light.gameObject);
                }
        }

        /// <summary>
        /// The tavern's area and its furniture builder, under <c>Areas/Tavern</c>: added once, then only reconfigured
        /// (D23). Pieces built in the editor (capture tools) are cleared, since the game builds them as the scene loads.
        /// </summary>
        /// <summary>
        /// The furniture content: the 4e pieces and the starting layout (FurnitureContent), the palettes, tiers and finishes
        /// (FurnitureLooks) and the catalogue from its table (FurnitureCatalog; 4f Checkpoint B), all in the game database.
        /// </summary>
        static FurnitureContent.Built BuildFurniture()
        {
            FurnitureContent.Built furniture = FurnitureContent.Build();
            FurnitureLooks.Build(furniture.Database);
            FurnitureCatalog.Built catalogue = FurnitureCatalog.Build(furniture.Database, furniture.Definitions.Keys);
            if (catalogue.Warnings.Count > 0) Debug.LogWarning($"[Hearthdelve] The furniture catalogue has {catalogue.Warnings.Count} warnings.");
            // 4f Checkpoint C: the troll's trophy and the Cellars' discoveries come from the catalogue.
            CurioContent.LinkTrophies(furniture.Database);
            return furniture;
        }

        static void AddArea(FurnitureContent.Built furniture)
        {
            Transform areas = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.parent == null && t.name == "Areas");
            if (areas == null) areas = new GameObject("Areas").transform;
            Transform tavern = areas.Find("Tavern");
            if (tavern == null)
            {
                tavern = new GameObject("Tavern").transform;
                tavern.SetParent(areas, false);
            }
            PropertyArea area = tavern.GetComponent<PropertyArea>() ?? tavern.gameObject.AddComponent<PropertyArea>();
            // The floor inside the half-tile side walls, from the front wall's top to the back wall's foot; the back wall's
            // band above it; the doorway and the tile inside it kept clear.
            // The stairs up to the guest room and the tile in front of them are kept clear too.
            var reserved = new List<Vector2Int> { new(DoorColumn, (int)FloorBottom), new(DoorColumn, (int)FloorBottom + 1), GuestRoomBuilder.TavernStairsFoot };
            for (int x = GuestRoomBuilder.TavernStairs.xMin; x < GuestRoomBuilder.TavernStairs.xMax; x++)
            for (int y = GuestRoomBuilder.TavernStairs.yMin; y < GuestRoomBuilder.TavernStairs.yMax; y++)
                reserved.Add(new Vector2Int(x, y));
            area.Configure(AreaId, Hearthdelve.Shared.Customization.AreaKind.Tavern, Vector2.zero, new RectInt(0, 0, Width, Height),
                new RectInt(1, (int)FloorBottom, Width - 2, (int)(FloorTop - FloorBottom)), new RectInt(1, (int)FloorTop, Width - 2, Height - (int)FloorTop),
                reserved.ToArray());
            area.SetView(CameraCentre, GuestRoomBuilder.TavernArrival, DecorateLocKeys.AreaTavern);
            AreaFinishes finishes = GuestRoomBuilder.TavernFinishes(tavern);
            AreaFurniture builder = tavern.GetComponent<AreaFurniture>() ?? tavern.gameObject.AddComponent<AreaFurniture>();
            var content = AssetDatabase.LoadAssetAtPath<TavernContent>(k_ContentPath);
            builder.Configure(area, furniture.Database, furniture.Presentation, content, finishes);
            Transform built = tavern.Find("Placed Furniture");
            while (built != null)
            {
                Object.DestroyImmediate(built.gameObject);
                built = tavern.Find("Placed Furniture");
            }
            GuestRoomBuilder.Build(area, furniture, content);
        }

        /// <summary>Decorate Mode (4f step 2) and its feedback, rebuilt each time (one object, replaced, never doubled).</summary>
        static void AddDecorate()
        {
            foreach (Transform old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t => t.parent == null && t.name == "Decorate").ToList())
                Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Decorate");
            root.AddComponent<DecorateMode>();
            TavernFeedbackContent.BuildDecorate(root.transform);
        }

        static T Replace<T>(GameObject target) where T : Component
        {
            T old = target.GetComponent<T>();
            if (old != null) Object.DestroyImmediate(old);
            return target.AddComponent<T>();
        }

        // ------------------------------------------------------------------ lights

        /// <summary>
        /// Warm candlelight (GDD §8.1): a warm ambient light. The local lights (the hearth, the kitchen fire, the stew
        /// fire, the lamp over the bar) belong to their pieces since 4f step 1, and move with them.
        /// </summary>
        static void BuildLights()
        {
            var lights = new GameObject("Lights").transform;
            Light(lights, "Ambient", Vector2.zero, Light2D.LightType.Global, k_Ambient, 0.72f, 0f);
        }

        /// <summary>The ambient light follows the day (4c step 5): morning daylight, the evening as built, night.</summary>
        static void AddMood()
        {
            GameObject lights = GameObject.Find("Lights");
            Light2D ambient = lights.transform.Find("Ambient").GetComponent<Light2D>();
            Replace<TavernMood>(lights).Configure(ambient, k_Ambient, 0.72f);
        }

        static void Light(Transform parent, string name, Vector2 position, Light2D.LightType type, Color color, float intensity, float radius)
        {
            Light2D light = LookTestBuilder.Light(name, position, type);
            light.transform.SetParent(parent, true);
            light.color = color;
            light.intensity = intensity;
            if (type == Light2D.LightType.Point)
            {
                light.pointLightInnerRadius = radius * 0.2f;
                light.pointLightOuterRadius = radius;
                light.falloffIntensity = 0.6f;
            }
            light.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
        }

        // ------------------------------------------------------------------ UI

        /// <summary>"E: Grill" at the bottom of the screen while something is in reach.</summary>
        static void BuildHint(Canvas canvas)
        {
            Transform old = canvas.transform.Find("TavernHint");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(canvas, "TavernHint");
            GameObject hint = DungeonUI.Hint(root, TavernLocKeys.HintUse, 14f, out LocalizedSuperText text);
            root.gameObject.AddComponent<TavernHintView>().Configure(hint, text);
        }
    }
}
