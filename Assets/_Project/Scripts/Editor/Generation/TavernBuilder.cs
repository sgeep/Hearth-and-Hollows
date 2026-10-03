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
        static readonly Vector2 k_StewPot = new(24.75f, 12f);     // bottom-centre of the cauldron
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
        /// Applies builder changes to the existing tavern scene in place, without rebuilding it:
        /// the camera's resting point.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Tavern", priority = 3)]
        public static void UpdateTavern()
        {
            var scene = EditorSceneManager.OpenScene(EditorPaths.TavernScene, OpenSceneMode.Single);
            var tavernCamera = GameObject.Find("Tavern Camera");
            if (tavernCamera == null) throw new InvalidOperationException("The tavern scene has no Tavern Camera.");
            tavernCamera.transform.position = new Vector3(CameraCentre.x, CameraCentre.y, -10f);
            Camera.main.transform.position = tavernCamera.transform.position;
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
            if (LookTestBuilder.MayWrite(EditorPaths.TavernScene, rebuildSceneApproved)) BuildScene(content);
            ProjectConfigurator.SetBuildOrder(EditorPaths.TestFloorScene, EditorPaths.TavernScene, EditorPaths.LookTestDungeonScene, EditorPaths.LookTestTavernScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] 4c tavern generated.");
        }

        static void BuildScene(LookTestBuilder.Content content)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject managers = LookTestBuilder.Managers(HearthdelveInputManager.GameplayMap.Tavern, content.Library, content.TavernPlayer, Spawn);
            managers.AddComponent<NavGrid>().Configure(new RectInt(0, 0, Width, Height), LayerMask.GetMask(Layers.Obstacles));
            LookTestBuilder.Cameras(new Color(0.07f, 0.05f, 0.05f));
            HoldCamera();

            BuildRoom();
            BuildFurniture();
            BuildLights();

            Canvas canvas = LookTestBuilder.Canvas(content.Actions, out _);
            BuildHint(canvas);
            LocalizedSuperText controls = LookTestBuilder.Text(canvas.transform, "Controls", TavernLocKeys.TavernControls, 6f, new Color(0.95f, 0.92f, 0.85f),
                TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(310f, 10f));
            controls.gameObject.AddComponent<Hearthdelve.UI.Debugging.FadeOutAfter>();

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
            Footprint(bar, 4f, 55f, 14f);
            Footprint(bar, 0f, 5f, 22f);
            Station(bar, TavernInteractableKind.Tap, TavernLocKeys.StationTap, new Vector2(1.1f, -0.55f), 1.0f, new Rect(0.1f, 1f, 1.8f, 1.75f));

            // The kitchen: the oven behind, the range in front (the Grill). Its working loop plays while cooking (step 3).
            SpriteRenderer kitchen = Piece(furniture, Single(MinifantasySheets.CraftingAndProfessions, "Kitchen"), "Kitchen", k_Kitchen);
            Piece(kitchen.transform, Single(MinifantasySheets.CraftingAndProfessions, "KitchenShadow"), "Shadow", Vector2.zero, SortingLayers.Floor, 5);
            Footprint(kitchen, 12f, 16f, 8f, 3f);
            Footprint(kitchen, 1f, 11f, 18f, 10f);
            Station(kitchen, TavernInteractableKind.Grill, TavernLocKeys.StationGrill, new Vector2(2.5f, -0.2f), 1.0f, new Rect(1.5f, 0.375f, 2f, 1f));

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
            FullFootprint(cauldron);
            Station(cauldron, TavernInteractableKind.StewPot, TavernLocKeys.StationStewPot, new Vector2(0f, -0.6f), 1.0f, new Rect(-0.75f, 0f, 1.5f, 1.4f));

            // The pass: finished plates wait here; usable from either side.
            SpriteRenderer pass = Piece(furniture, Tavern("props", "LongTableH"), "Pass", k_Pass + new Vector2(1.75f, 0f));
            FullFootprint(pass);
            Station(pass, TavernInteractableKind.Pass, TavernLocKeys.StationPass, new Vector2(0f, 0.5f), 1.5f, new Rect(-1.75f, 0f, 3.5f, 1f));

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

        /// <summary>
        /// Makes a piece usable: where the player stands (relative to its pivot) and how near, plus its
        /// highlight: gold corner brackets around <paramref name="frame"/> (pivot-relative) and a bobbing marker above.
        /// </summary>
        static void Station(SpriteRenderer piece, TavernInteractableKind kind, string nameKey, Vector2 useOffset, float reach, Rect frame)
        {
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            var highlight = new GameObject("Highlight").transform;
            highlight.SetParent(piece.transform, false);

            SpriteRenderer brackets = LookTestContent.AddSprite(highlight, "Brackets", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", "Brackets"),
                SortingLayers.Above, 0, frame.center);
            brackets.drawMode = SpriteDrawMode.Sliced;
            brackets.size = frame.size + Vector2.one * 0.25f;
            brackets.color = k_Highlight;
            SpriteRenderer marker = LookTestContent.AddSprite(highlight, "Marker", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", "Marker"),
                SortingLayers.Above, 1, new Vector2(frame.center.x, frame.yMax + 0.25f));
            marker.color = k_Highlight;
            marker.gameObject.AddComponent<SpriteBob>();
            if (unlit != null)
            {
                brackets.sharedMaterial = unlit;
                marker.sharedMaterial = unlit;
            }

            piece.gameObject.AddComponent<TavernInteractable>().Configure(kind, nameKey, useOffset, reach, highlight.gameObject);
            highlight.gameObject.SetActive(false);
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
            RectTransform root = DungeonUI.FullScreen(canvas, "TavernHint");
            GameObject hint = DungeonUI.Hint(root, TavernLocKeys.HintUse, 14f, out LocalizedSuperText text);
            root.gameObject.AddComponent<TavernHintView>().Configure(hint, text);
        }
    }
}
