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
    /// floor rows repeat exactly); furniture comes from the add-on's prop sheet, the Grill is the
    /// Crafting And Professions II kitchen, the Stew Pot the Dungeon cauldron over a Dwarven Kingdom
    /// floor fire. Every solid piece blocks movement with a footprint that ends at the bottom of its
    /// art (its sort point). The walkable grid is baked from those colliders at scene load and can be
    /// invalidated when furniture moves (<see cref="NavGrid"/>). The camera holds still on the room.
    /// <c>LookTest_Tavern</c> is not touched. The scene is created when missing and never overwritten
    /// without approval (a dialog in the editor; <c>-rebuildScene</c> in batch mode).
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

        // Positions (room tiles, origin at the room's bottom-left corner).
        static readonly Vector2 k_Bar = new(1.5f, 11f);           // bottom-left of the L-shaped bar
        static readonly Vector2 k_Kitchen = new(19f, 11.625f);    // bottom-left of the 32×32 kitchen frame
        static readonly Vector2 k_StewPot = new(24.75f, 11.5f);   // bottom-centre of the cauldron (a tile clear of the wall, to walk behind)
        static readonly Vector2 k_Pass = new(19.75f, 8.5f);       // bottom-left of the pass (a long table)
        static readonly Vector2 k_Hearth = new(15.5f, 14f);       // bottom-centre of the wall fireplace
        /// <summary>Table groups (bottom-centre of each round table): a chair on each side, facing it.</summary>
        public static readonly Vector2[] Tables = { new(4.5f, 7f), new(9.5f, 7f), new(18f, 4f), new(23f, 4f) };
        const float k_ChairOffset = 1.25f;

        static readonly Color k_Ambient = new(1f, 0.86f, 0.68f);
        static readonly Color k_Warm = new(1f, 0.66f, 0.36f);
        static readonly Color k_Highlight = new(1f, 0.82f, 0.3f);
        const string k_UnlitSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>
        /// Applies builder changes to the existing tavern scene in place, without rebuilding it: the
        /// camera's resting point; the kitchen and stew pot blocking back to the wall, the whole kitchen
        /// being the Grill, and highlights drawn from corner sprites (step 1 playtest); the service
        /// (customers, seats, the queue, Pip; step 2), rebuilt each time; the Grill and Stew Pot usable
        /// from behind, with the pot moved off the wall (step 2 playtest).
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Tavern", priority = 3)]
        public static void UpdateTavern()
        {
            ProjectConfigurator.ConfigureAll();
            MinifantasyImporter.ImportAll();
            LocalizationBuilder.Build();
            TavernStationContent.AssignDishIcons();
            NpcContent.Built npcs = NpcContent.Build();
            AddCarryViewToPlayer();
            var scene = EditorSceneManager.OpenScene(EditorPaths.TavernScene, OpenSceneMode.Single);
            var tavernCamera = GameObject.Find("Tavern Camera");
            if (tavernCamera == null) throw new InvalidOperationException("The tavern scene has no Tavern Camera.");
            tavernCamera.transform.position = new Vector3(CameraCentre.x, CameraCentre.y, -10f);
            Camera.main.transform.position = tavernCamera.transform.position;
            SetUpBar(GameObject.Find("Furniture/Bar").GetComponent<SpriteRenderer>());
            SetUpPass(GameObject.Find("Pass").GetComponent<SpriteRenderer>());
            SetUpKitchen(GameObject.Find("Kitchen").GetComponent<SpriteRenderer>());
            // Step 2 playtest: the stew pot moves half a tile off the wall, so it can be walked behind.
            GameObject.Find("StewPot").transform.position = k_StewPot;
            GameObject.Find("Stew Fire").transform.position = k_StewPot + new Vector2(0f, 0.25f);
            SetUpStewPot(GameObject.Find("Cauldron").GetComponent<SpriteRenderer>());
            AddService(npcs);
            AddStations();
            TavernFeedbackContent.Build(GameObject.Find("Service").transform);
            Canvas ui = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).First(c => c.name == "UI");
            TavernStationContent.BuildStationPanel(ui);
            BuildHint(ui);
            TavernScreens.Rebuild(ui);
            AddMood();
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
            BuildFurniture();
            BuildLights();
            AddService(npcs);
            AddStations();
            TavernFeedbackContent.Build(GameObject.Find("Service").transform);

            Canvas canvas = LookTestBuilder.Canvas(content.Actions, out _);
            BuildHint(canvas);
            TavernStationContent.BuildStationPanel(canvas);
            LocalizedSuperText controls = LookTestBuilder.Text(canvas.transform, "Controls", TavernLocKeys.TavernControls, 6f, new Color(0.95f, 0.92f, 0.85f),
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

        // ------------------------------------------------------------------ furniture

        /// <summary>The sprite of a sheet imported whole (<see cref="SliceMode.Single"/>).</summary>
        static Sprite Single(string pack, string file) => MinifantasyImporter.Sprites(pack, file).Values.First();

        static Sprite Tavern(string layer, string name) => MinifantasyImporter.Sprite(MinifantasySheets.TavernIndoor, $"TavernIndoor_{layer}", name);

        static SpriteRenderer Piece(Transform parent, Sprite sprite, string name, Vector2 position, string sortingLayer = SortingLayers.YSorted, int order = 0) =>
            LookTestContent.AddSprite(parent, name, sprite, sortingLayer, order, position);

        /// <summary>A footprint that starts at the bottom of the art (its sort point), in art pixels from its bottom-left corner.</summary>
        static void Footprint(SpriteRenderer piece, float x, float width, float height, float y = 0f)
        {
            piece.gameObject.layer = LayerMask.NameToLayer(Layers.Obstacles);
            Bounds art = piece.sprite.bounds;
            var box = piece.gameObject.AddComponent<BoxCollider2D>();
            const float ppu = MinifantasySheets.PixelsPerUnit;
            box.size = new Vector2(width / ppu, height / ppu);
            box.offset = new Vector2(art.min.x + (x + width / 2f) / ppu, art.min.y + (y + height / 2f) / ppu);
        }

        /// <summary>The whole art blocks (tables, chairs, barrels).</summary>
        static void FullFootprint(SpriteRenderer piece)
        {
            Vector2 size = piece.sprite.bounds.size * MinifantasySheets.PixelsPerUnit;
            Footprint(piece, 0f, size.x, size.y);
        }

        static void BuildFurniture()
        {
            var furniture = new GameObject("Furniture").transform;

            // Back wall: shelves of bottles behind the bar, a sign, a fireplace, a low shelf by the kitchen.
            var wallDecor = new GameObject("Wall Decor").transform;
            Piece(wallDecor, Tavern("props", "Shelves"), "Shelves", new Vector2(k_Bar.x + 1.25f, 14.25f), SortingLayers.Floor, 3);
            Piece(wallDecor, Tavern("props2", "ShelfGoods"), "ShelfGoods", new Vector2(k_Bar.x + 1.25f, 14.125f), SortingLayers.Floor, 4);
            Piece(wallDecor, Tavern("props", "Sign"), "Sign", new Vector2(11f, 15.25f), SortingLayers.Floor, 3);
            Piece(wallDecor, Tavern("props", "ShelfLow"), "ShelfLow", new Vector2(24.5f, 14.75f), SortingLayers.Floor, 3);
            Piece(wallDecor, Tavern("props2", "Glasses"), "Glasses", new Vector2(24.75f, 15.5f), SortingLayers.Floor, 4);
            Sprite[] hearthFrames = MinifantasyImporter.Row(MinifantasySheets.DwarvenKingdom, "WallFireplace", 0, 8);
            SpriteRenderer hearth = Piece(wallDecor, hearthFrames[0], "Fireplace", k_Hearth, SortingLayers.Floor, 3);
            hearth.gameObject.AddComponent<SpriteLoop>().Configure(hearthFrames, 0.12f);

            // The bar (premade L-shape with its stools drawn in) and its taps: the Tap station.
            SpriteRenderer bar = Piece(furniture, Tavern("props", "Bar"), "Bar", k_Bar);
            bar.gameObject.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            Piece(bar.transform, Tavern("props2", "BarTop"), "Taps", new Vector2(0.25f, 1.125f), SortingLayers.YSorted, 1);
            SetUpBar(bar);

            // The kitchen: the oven behind, the range in front (the Grill). Its working loop plays while cooking (step 3).
            SpriteRenderer kitchen = Piece(furniture, Single(MinifantasySheets.CraftingAndProfessions, "Kitchen"), "Kitchen", k_Kitchen);
            Piece(kitchen.transform, Single(MinifantasySheets.CraftingAndProfessions, "KitchenShadow"), "Shadow", Vector2.zero, SortingLayers.Floor, 5);
            SetUpKitchen(kitchen);

            // The stew pot: the Dungeon cauldron over a small floor fire, sorted as one.
            var stewRoot = new GameObject("StewPot").transform;
            stewRoot.SetParent(furniture, false);
            stewRoot.localPosition = k_StewPot;
            stewRoot.gameObject.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            Sprite[] fireFrames = MinifantasyImporter.Row(MinifantasySheets.DwarvenKingdom, "FloorFireplace", 0, 8);
            SpriteRenderer fire = Piece(stewRoot, fireFrames[0], "Fire", Vector2.zero, SortingLayers.YSorted, 0);
            fire.transform.localPosition = new Vector2(0f, -0.125f);
            fire.gameObject.AddComponent<SpriteLoop>().Configure(fireFrames, 0.12f);
            SpriteRenderer cauldron = Piece(stewRoot, MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Props", "Cauldron"), "Cauldron", Vector2.zero, SortingLayers.YSorted, 1);
            cauldron.transform.localPosition = Vector2.zero;
            SetUpStewPot(cauldron);

            // The pass: finished plates wait here; usable from either side.
            SpriteRenderer pass = Piece(furniture, Tavern("props", "LongTableH"), "Pass", k_Pass + new Vector2(1.75f, 0f));
            SetUpPass(pass);

            // Dining: a round table with a chair each side, facing it. Seats are marked for customers (step 2).
            var seats = new GameObject("Seats").transform;
            for (int i = 0; i < Tables.Length; i++)
            {
                Vector2 t = Tables[i];
                SpriteRenderer table = Piece(furniture, Tavern("props", i % 2 == 0 ? "TableRoundA" : "TableRoundB"), $"Table{i + 1}", t);
                FullFootprint(table);
                foreach (int side in new[] { -1, 1 })
                {
                    Vector2 at = t + new Vector2(side * k_ChairOffset, 0.25f);
                    SpriteRenderer chair = Piece(furniture, Tavern("props", side < 0 ? "ChairFacingE" : "ChairFacingW"), $"Chair{i + 1}{(side < 0 ? "W" : "E")}", at);
                    FullFootprint(chair);
                    var seat = new GameObject($"Seat{i * 2 + (side < 0 ? 1 : 2)}").transform;
                    seat.SetParent(seats, false);
                    seat.localPosition = at;
                }
            }

            // Decor: barrels by the east wall and a few stools near the bar.
            foreach (Vector2 at in new[] { new Vector2(26.25f, 8f), new Vector2(26.25f, 9f), new Vector2(25.25f, 8f) })
                FullFootprint(Piece(furniture, MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Props", "Barrel"), "Barrel", at));
            FullFootprint(Piece(furniture, Tavern("props", "StoolRedA"), "Stool", new Vector2(11.5f, 11.5f)));
        }

        /// <summary>The bar, used at its taps from the customer side.</summary>
        static void SetUpBar(SpriteRenderer bar)
        {
            ClearStation(bar);
            Footprint(bar, 4f, 55f, 14f);
            Footprint(bar, 0f, 5f, 22f);
            Station(bar, TavernInteractableKind.Tap, TavernLocKeys.StationTap, new Vector2(1.1f, -0.55f), 1.0f, new Rect(0.1f, 1f, 1.8f, 1.75f));
        }

        /// <summary>The pass, usable from either side of its table.</summary>
        static void SetUpPass(SpriteRenderer pass)
        {
            ClearStation(pass);
            FullFootprint(pass);
            Station(pass, TavernInteractableKind.Pass, TavernLocKeys.StationPass, new Vector2(0f, 0.5f), 1.5f, new Rect(-1.75f, 0f, 3.5f, 1f));
        }

        /// <summary>
        /// The whole kitchen is the Grill: the range in front and the oven beside it against the wall. There's
        /// a tile of floor behind the range (in round its east end), and the Grill can be used from there too,
        /// like a cook behind the stove (step 2 playtest: for immersion).
        /// The kitchen is one sprite, sorted at the bottom of its frame. Both footprints start 3 px up, where the
        /// range's art does, on a tile edge (so the row in front stays open for pathfinding): every character's
        /// collider is at least 0.4 tiles tall, so anyone stopped in front has their feet below the sort point and
        /// draws in front. The oven's footprint used to start at its own art, 10 px up, which let the keeper stand
        /// inside the sort point and drew the kitchen over them (4c step 4 playtest).
        /// </summary>
        static void SetUpKitchen(SpriteRenderer kitchen)
        {
            ClearStation(kitchen);
            Footprint(kitchen, 12f, 16f, 8f, 3f);
            Footprint(kitchen, 1f, 11f, 25f, 3f);
            Station(kitchen, TavernInteractableKind.Grill, TavernLocKeys.StationGrill, new Vector2(1.8125f, -0.2f), 1.5f, new Rect(0.125f, 0.375f, 3.375f, 3.125f),
                new[] { new Vector2(2.5f, 1.875f) });
        }

        /// <summary>The cauldron, a tile clear of the wall: used from the front or from behind (step 2 playtest).</summary>
        static void SetUpStewPot(SpriteRenderer cauldron)
        {
            ClearStation(cauldron);
            FullFootprint(cauldron);
            Station(cauldron, TavernInteractableKind.StewPot, TavernLocKeys.StationStewPot, new Vector2(0f, -0.6f), 1.0f, new Rect(-0.75f, 0f, 1.5f, 1.4f),
                new[] { new Vector2(0f, 1.95f) });
        }

        /// <summary>Removes a piece's footprints, interaction and highlight, so it can be set up again.</summary>
        static void ClearStation(Component piece)
        {
            foreach (BoxCollider2D box in piece.GetComponents<BoxCollider2D>()) Object.DestroyImmediate(box);
            Object.DestroyImmediate(piece.GetComponent<TavernInteractable>());
            Transform highlight = piece.transform.Find("Highlight");
            if (highlight != null) Object.DestroyImmediate(highlight.gameObject);
        }

        /// <summary>
        /// Makes a piece usable: where the player stands (relative to its pivot) and how near, plus its
        /// highlight: gold corners around <paramref name="frame"/> (pivot-relative) and a bobbing marker above.
        /// </summary>
        static void Station(Component piece, TavernInteractableKind kind, string nameKey, Vector2 useOffset, float reach, Rect frame,
            Vector2[] alsoFrom = null)
        {
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            var highlight = new GameObject("Highlight").transform;
            highlight.SetParent(piece.transform, false);

            // Four corner sprites rather than one 9-sliced frame: a sliced SpriteRenderer dropped the
            // frame's top row and right column of pixels (step 1 playtest).
            Rect outer = new(frame.xMin - 0.125f, frame.yMin - 0.125f, frame.width + 0.25f, frame.height + 0.25f);
            var corners = new List<SpriteRenderer>();
            foreach (var (name, at) in new[] { ("CornerTL", new Vector2(outer.xMin, outer.yMax)), ("CornerTR", new Vector2(outer.xMax, outer.yMax)),
                         ("CornerBL", new Vector2(outer.xMin, outer.yMin)), ("CornerBR", new Vector2(outer.xMax, outer.yMin)) })
            {
                SpriteRenderer corner = LookTestContent.AddSprite(highlight, name, MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", name),
                    SortingLayers.Above, 0, at);
                corner.color = k_Highlight;
                corners.Add(corner);
            }
            SpriteRenderer marker = LookTestContent.AddSprite(highlight, "Marker", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", "Marker"),
                SortingLayers.Above, 1, new Vector2(frame.center.x, frame.yMax + 0.25f));
            marker.color = k_Highlight;
            marker.gameObject.AddComponent<SpriteBob>();
            if (unlit != null)
                foreach (SpriteRenderer r in corners.Append(marker)) r.sharedMaterial = unlit;

            piece.gameObject.AddComponent<TavernInteractable>().Configure(kind, nameKey, useOffset, reach, highlight.gameObject, alsoFrom);
            highlight.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ service (step 2)

        const string k_ContentPath = EditorPaths.Data + "/Tavern/TavernContent.asset";
        public const int QueueSpots = 6;

        /// <summary>
        /// The evening: the door, the queue (inside the door, running west along the front wall), the seats
        /// (each chair, with a spot below it to step onto it from, facing its table), staff posts, the
        /// director that runs the service, Pip, and the debug keys. Replaces any it had.
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

            Transform seatRoot = GameObject.Find("Seats").transform;
            var seats = new List<TavernSeat>();
            for (int t = 0; t < Tables.Length; t++)
            foreach (int side in new[] { -1, 1 })
            {
                Transform marker = seatRoot.Find($"Seat{t * 2 + (side < 0 ? 1 : 2)}");
                TavernSeat seat = marker.GetComponent<TavernSeat>() ?? marker.gameObject.AddComponent<TavernSeat>();
                // The west chair faces east (towards the table), the east chair west; both face the room.
                seat.Configure(new Vector2(0f, -0.9f), side < 0 ? Hearthdelve.Core.Movement.Facing4.FrontRight : Hearthdelve.Core.Movement.Facing4.FrontLeft,
                    new[] { GameObject.Find($"Table{t + 1}"), GameObject.Find($"Chair{t + 1}W"), GameObject.Find($"Chair{t + 1}E") });
                seats.Add(seat);
            }

            Vector2 UseOf(string piece) => GameObject.Find(piece).GetComponent<TavernInteractable>().UsePoint;
            var layout = root.gameObject.AddComponent<TavernLayout>();
            layout.Configure(door, queue, seats.ToArray(),
                Point("Post Serving", new Vector2(21.5f, 7.6f)), Point("Post Grill", UseOf("Kitchen")), Point("Post Tap", UseOf("Furniture/Bar")),
                Point("Post StewPot", UseOf("Cauldron")), Point("Post Rest", new Vector2(25.5f, 5.5f)));

            var pip = (GameObject)PrefabUtility.InstantiatePrefab(npcs.Pip);
            pip.transform.position = new Vector3(25.5f, 5.5f, 0f);

            var director = root.gameObject.AddComponent<TavernDirector>();
            director.Configure(AssetDatabase.LoadAssetAtPath<TavernContent>(k_ContentPath), layout, npcs.Customer.GetComponent<CustomerAgent>(), pip.GetComponent<StaffAgent>());
            root.gameObject.AddComponent<TavernDebugKeys>();

            // Pip from an earlier run of this updater.
            foreach (StaffAgent extra in Object.FindObjectsByType<StaffAgent>(FindObjectsInactive.Include))
                if (extra.gameObject != pip) Object.DestroyImmediate(extra.gameObject);
        }

        // ------------------------------------------------------------------ stations and serving (step 3)

        /// <summary>The keeper carries plates over their head (4c decision 2): added to the tavern player prefab in place.</summary>
        static void AddCarryViewToPlayer()
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

        /// <summary>
        /// Seats become serving targets (gold corners round the chair and its sitter while carrying a plate);
        /// plates waiting on the pass, the stew pot's simmer bar and helpings, the kitchen at work; and the
        /// keeper's work, which connects the stations, the pass and the seats to the service. Replaces any it had.
        /// </summary>
        static void AddStations()
        {
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            var layout = Object.FindAnyObjectByType<TavernLayout>();
            float reach = AssetDatabase.LoadAssetAtPath<TavernContent>(k_ContentPath).serving.serving.arriveDistance + 0.2f;
            var seats = new List<TavernInteractable>();
            foreach (TavernSeat seat in layout.Seats)
            {
                ClearStation(seat);
                Station(seat, TavernInteractableKind.Seat, null, Vector2.zero, reach, new Rect(-0.5f, -0.1f, 1f, 1.85f));
                seats.Add(seat.GetComponent<TavernInteractable>());
            }

            // The pass: up to four plates along its top, sorted with the table.
            SpriteRenderer pass = GameObject.Find("Pass").GetComponent<SpriteRenderer>();
            if (pass.GetComponent<SortingGroup>() == null) pass.gameObject.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            Transform oldPlates = pass.transform.Find("Plates");
            if (oldPlates != null) Object.DestroyImmediate(oldPlates.gameObject);
            var plates = new GameObject("Plates").transform;
            plates.SetParent(pass.transform, false);
            var slots = new SpriteRenderer[4];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = LookTestContent.AddSprite(plates, $"Plate{i + 1}", null, SortingLayers.YSorted, 1, new Vector3(-1.2f + i * 0.8f, 0.55f, 0f));
            Replace<PassView>(pass.gameObject).Configure(slots);

            // The stew pot: a bar while it simmers, and a pip per helping.
            Transform stewRoot = GameObject.Find("StewPot").transform;
            Transform oldStatus = stewRoot.Find("Status");
            if (oldStatus != null) Object.DestroyImmediate(oldStatus.gameObject);
            var status = new GameObject("Status").transform;
            status.SetParent(stewRoot, false);
            status.localPosition = new Vector3(0f, 2.1f, 0f);
            var overlay = new List<SpriteRenderer>();
            // Not "Bar": that's the bar's name, and the builder finds furniture by name.
            var bar = new GameObject("Progress").transform;
            bar.SetParent(status, false);
            SpriteRenderer barBack = LookTestContent.AddSprite(bar, "Back", DungeonUI.Pixel(), SortingLayers.Above, 0, Vector3.zero);
            barBack.transform.localScale = new Vector3(12f, 3f, 1f);
            barBack.color = new Color(0.08f, 0.06f, 0.06f);
            var barAnchor = new GameObject("Anchor").transform;
            barAnchor.SetParent(bar, false);
            barAnchor.localPosition = new Vector3(-0.625f, 0f, 0f);
            SpriteRenderer barFill = LookTestContent.AddSprite(barAnchor, "Fill", DungeonUI.Pixel(), SortingLayers.Above, 1, new Vector3(0.0625f, 0f, 0f));
            barFill.color = new Color(1f, 0.66f, 0.3f);
            overlay.Add(barBack);
            overlay.Add(barFill);
            var pips = new SpriteRenderer[5];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = LookTestContent.AddSprite(status, $"Pip{i + 1}", DungeonUI.Pixel(), SortingLayers.Above, 1, new Vector3((i - 2) * 0.375f, -0.375f, 0f));
                pips[i].transform.localScale = new Vector3(2f, 2f, 1f);
                overlay.Add(pips[i]);
            }
            if (unlit != null) foreach (SpriteRenderer r in overlay) r.sharedMaterial = unlit;
            Replace<StewPotView>(stewRoot.gameObject).Configure(bar.gameObject, barAnchor, pips);

            // The kitchen works (fire, pans, smoke) while anyone cooks at the Grill.
            SpriteRenderer kitchen = GameObject.Find("Kitchen").GetComponent<SpriteRenderer>();
            Replace<KitchenView>(kitchen.gameObject).Configure(kitchen, Single(MinifantasySheets.CraftingAndProfessions, "Kitchen"),
                MinifantasyImporter.Row(MinifantasySheets.CraftingAndProfessions, "KitchenWorking", 0, 8));

            GameObject service = GameObject.Find("Service");
            Replace<KeeperWork>(service).Configure(GameObject.Find("Kitchen").GetComponent<TavernInteractable>(), GameObject.Find("Furniture/Bar").GetComponent<TavernInteractable>(),
                GameObject.Find("Cauldron").GetComponent<TavernInteractable>(), GameObject.Find("Pass").GetComponent<TavernInteractable>(), seats.ToArray());
        }

        static T Replace<T>(GameObject target) where T : Component
        {
            T old = target.GetComponent<T>();
            if (old != null) Object.DestroyImmediate(old);
            return target.AddComponent<T>();
        }

        // ------------------------------------------------------------------ lights

        /// <summary>Warm candlelight (GDD §8.1): a warm ambient light, and local light from the hearth, the kitchen fire, the stew fire and over the bar.</summary>
        static void BuildLights()
        {
            var lights = new GameObject("Lights").transform;
            Light(lights, "Ambient", Vector2.zero, Light2D.LightType.Global, k_Ambient, 0.72f, 0f);
            Light(lights, "Hearth Glow", k_Hearth + new Vector2(0f, 0.5f), Light2D.LightType.Point, k_Warm, 0.9f, 7f);
            Light(lights, "Kitchen Fire", k_Kitchen + new Vector2(1f, 1.5f), Light2D.LightType.Point, k_Warm, 0.7f, 4.5f);
            Light(lights, "Stew Fire", k_StewPot + new Vector2(0f, 0.25f), Light2D.LightType.Point, k_Warm, 0.6f, 3f);
            Light(lights, "Bar Lamp", k_Bar + new Vector2(3.5f, 2f), Light2D.LightType.Point, new Color(1f, 0.8f, 0.55f), 0.6f, 6f);
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
