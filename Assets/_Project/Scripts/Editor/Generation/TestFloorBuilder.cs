using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.UI.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the 4b dungeon test floor: three hand-made Cellar rooms drawn from the text map
    /// below, for migrating and playtesting the dungeon systems. Rooms and run structure come
    /// from the room graph in 4d; this floor is fixed on purpose. The 4a look scenes are left
    /// alone. The scene is created when missing and never overwritten without approval (a
    /// dialog in the editor; <c>-rebuildScene</c> in batch mode).
    /// </summary>
    public static class TestFloorBuilder
    {
        /// <summary>
        /// One character per tile, top row first. Walls are auto-tiled from the Dungeon tileset's
        /// wall sample (docs/ASSET_MAP.md): a north wall is two rows (top face, then brick face).
        /// </summary>
        /// <remarks>
        /// <c>#</c> wall, <c>t</c> wall with a torch, <c>.</c> floor, <c>o</c> pillar (two rows),
        /// <c>c</c> crate, <c>b</c> barrel, <c>B</c> open barrel, <c>T</c> table (two tiles),
        /// <c>u</c> cauldron, <c>s</c> statue, <c>P</c> player spawn, <c>S</c> green slime, <c>V</c> bat (hanging asleep: must be
        /// directly under a wall), <c>X</c> giant spider, <c>R</c> the rope out (extraction),
        /// <c>1</c>–<c>6</c> navigation test points (editor only).
        /// </remarks>
        internal static readonly string[] Map =
        {
            "############################################",
            "####t#######t###t#######t#########t#####t###",
            "#..b.c........s....#.................V...s.#",
            "#..B...............#...o......o............#",
            "#.6................#...o......o............#",
            "#..................#.......................#",
            "#.....P..................................S.#",
            "#..........................................#",
            "#..................#...o......o............#",
            "#....T.............#...o......o.........5..#",
            "#..................#.......................#",
            "#.................u#.......................#",
            "########..##########################..######",
            "####t###..####t####################t..##t###",
            "#.....V....................................#",
            "#..............2........................R..#",
            "#...........#########........c.............#",
            "#...........#########........b........X....#",
            "#...........#.......#........c.............#",
            "#...........#...1...#.....3..b...4.........#",
            "#...........#.......#........c.........S...#",
            "#...........#.......#........b.............#",
            "#............................c.............#",
            "#..........................................#",
            "############################################",
            "############################################",
        };

        public static int Width => Map[0].Length;
        public static int Height => Map.Length;

        [MenuItem("Hearthdelve/Generate/4b Test Floor", priority = 1)]
        public static void GenerateMenu() => Generate(rebuildSceneApproved: false);

        /// <summary>Batch entry point: <c>-executeMethod Hearthdelve.Editor.TestFloorBuilder.RunBatch [-rebuildScene]</c>.</summary>
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
            if (LookTestBuilder.MayWrite(EditorPaths.TestFloorScene, rebuildSceneApproved)) BuildScene(content);
            ProjectConfigurator.SetBuildOrder(EditorPaths.TestFloorScene, EditorPaths.LookTestDungeonScene, EditorPaths.LookTestTavernScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] 4b test floor generated.");
        }

        /// <summary>Map character at a world tile (x right, y up); outside the map is wall.</summary>
        static char At(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? '#' : Map[Height - 1 - y][x];

        static bool IsWall(char c) => c is '#' or 't';
        static bool IsSolid(int x, int y) => IsWall(At(x, y)) || At(x, y) == 'o';
        static bool IsOpen(int x, int y) => !IsSolid(x, y);

        /// <summary>Feet position for something standing in a tile.</summary>
        static Vector2 Feet(int x, int y) => new(x + 0.5f, y + 0.3f);

        /// <summary>
        /// Where an enemy marker puts the enemy. A bat hangs at the top of its tile, so its sleep pose
        /// is drawn on the brick face of the wall above it.
        /// </summary>
        static Vector2 EnemyFeet(char marker, int x, int y) => marker == 'V' ? new Vector2(x + 0.5f, y + 0.55f) : Feet(x, y);

        /// <summary>Warns about bat markers with no wall directly above them (they would hover instead of hanging).</summary>
        static void ValidatePerches()
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (At(x, y) == 'V' && !IsWall(At(x, y + 1)))
                    Debug.LogWarning($"[Hearthdelve] Test floor: the bat at tile ({x}, {y}) has no wall above it to hang from.");
        }

        static void BuildScene(LookTestBuilder.Content content)
        {
            ValidatePerches();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Vector2 spawn = Vector2.zero;
            var enemyMarkers = new List<(char kind, Vector2 feet)>();
            var testPoints = new List<(char id, Vector2 feet)>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char c = At(x, y);
                if (c == 'P') spawn = Feet(x, y);
                else if (EnemyFor(content, c) != null) enemyMarkers.Add((c, EnemyFeet(c, x, y)));
                else if (c is >= '1' and <= '9') testPoints.Add((c, Feet(x, y)));
            }

            GameObject managers = LookTestBuilder.Managers(HearthdelveInputManager.GameplayMap.Dungeon, content.Library, content.Player, spawn);
            HarvestSystem newHarvest = managers.AddComponent<HarvestSystem>();
            newHarvest.Configure(content.HarvestRules, content.Cleaver, content.Pickup.GetComponent<IngredientPickup>(), content.Freshness);
            HarvestFeedbacks(newHarvest);
            managers.AddComponent<NavGrid>().Configure(new RectInt(0, 0, Width, Height), LayerMask.GetMask(Layers.Obstacles));
            managers.AddComponent<Hearthdelve.Dungeon.Run.DelveRunController>();
            PixelPerfectCamera camera = LookTestBuilder.Cameras(new Color(0.05f, 0.05f, 0.07f));
            LookTestBuilder.Light("Global Light 2D", Vector3.zero, Light2D.LightType.Global);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            Tilemap floor = LookTestBuilder.Layer(grid, "Floor", SortingLayers.Floor, 0, false);
            Tilemap walls = LookTestBuilder.Layer(grid, "Walls", SortingLayers.Floor, 1, true);
            PaintFloor(floor);
            PaintWalls(walls);
            walls.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
            walls.GetComponent<CompositeCollider2D>().GenerateGeometry();

            var props = new GameObject("Props").transform;
            Sprite[] torchFrames = MinifantasyImporter.Row(MinifantasySheets.Dungeon, "Torch", 0, 8);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var foot = new Vector2(x + 0.5f, y);
                switch (At(x, y))
                {
                    case 'c': LookTestBuilder.Prop(props, "Crate", foot); break;
                    case 'b': LookTestBuilder.Prop(props, "Barrel", foot); break;
                    case 'B': LookTestBuilder.Prop(props, "BarrelOpen", foot); break;
                    case 'u': LookTestBuilder.Prop(props, "Cauldron", foot); break;
                    case 's': LookTestBuilder.Prop(props, "Statue", foot); break;
                    case 'T': LookTestBuilder.Prop(props, "Table", foot + new Vector2(0.5f, 0f)); break;
                    case 'R':
                        var rope = (GameObject)PrefabUtility.InstantiatePrefab(content.RopeExit, props);
                        rope.transform.position = Feet(x, y);
                        break;
                    case 't':
                        // On the brick face, like the look test's torches; lit from just above.
                        SpriteRenderer torch = LookTestContent.AddSprite(props, "Torch", torchFrames[0], SortingLayers.Floor, 2, new Vector3(x + 0.5f, y - 1.4f, 0f));
                        torch.gameObject.AddComponent<SpriteLoop>().Configure(torchFrames, 0.2f);
                        LookTestBuilder.Light("Torch Light", new Vector3(x + 0.5f, y + 0.5f, 0f), Light2D.LightType.Point);
                        break;
                }
            }

            var enemies = new GameObject("Enemies").transform;
            foreach (var (kind, position) in enemyMarkers)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(EnemyFor(content, kind), enemies);
                instance.transform.position = position;
            }

            // Fixed spots for the navigation PlayMode tests. EditorOnly: stripped from player builds.
            var points = new GameObject("NavTestPoints") { tag = "EditorOnly" }.transform;
            foreach (var (id, feet) in testPoints)
            {
                var point = new GameObject($"Point{id}") { tag = "EditorOnly" };
                point.transform.SetParent(points, false);
                point.transform.position = feet;
            }

            Canvas canvas = LookTestBuilder.Canvas(content.Actions, out CanvasScaler scaler);
            DungeonUI.RebuildScreens(canvas);
            LookTestBuilder.Overlay(canvas, scaler, camera, LocKeys.TestFloorHint, Path.GetFileNameWithoutExtension(EditorPaths.LookTestTavernScene));

            LookTestBuilder.ApplyLighting(dungeon: true);
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, EditorPaths.TestFloorScene);
        }

        static GameObject EnemyFor(LookTestBuilder.Content content, char marker) => marker switch
        {
            'S' => content.Slime,
            'V' => content.Bat,
            'X' => content.Spider,
            _ => null,
        };

        /// <summary>
        /// Brings an existing test floor up to 4b step 2 in place, without rebuilding it: adds the time
        /// manager (hit-stop), and an enemy at every map marker that has none of that kind yet.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/Update Test Floor Enemies", priority = 21)]
        public static void UpdateTestFloor()
        {
            LookTestBuilder.Content content = LookTestBuilder.BuildContent();
            if (!System.IO.File.Exists(EditorPaths.TestFloorScene))
            {
                BuildScene(content);
                return;
            }
            var scene = EditorSceneManager.OpenScene(EditorPaths.TestFloorScene, OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<MoreMountains.Feedbacks.MMTimeManager>() == null)
                new GameObject("TimeManager").AddComponent<MoreMountains.Feedbacks.MMTimeManager>();
            Transform enemies = GameObject.Find("Enemies")?.transform ?? new GameObject("Enemies").transform;
            ValidatePerches();
            // Step 3: parts spoil on the floor, and the satchel-full hint and swap prompt.
            HarvestSystem harvest = Object.FindAnyObjectByType<HarvestSystem>();
            if (harvest != null)
            {
                harvest.Configure(content.HarvestRules, content.Cleaver, content.Pickup.GetComponent<IngredientPickup>(), content.Freshness);
                // Drops pop out in front of the body (step 3 fix): the distance that keeps them out of reach of a kill at your feet.
                harvest.Scatter = 1.1f;
                // Step 6: the kill moments' feedbacks.
                HarvestFeedbacks(harvest);
                EditorUtility.SetDirty(harvest);
            }
            Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include).FirstOrDefault(c => c.name == "UI");
            // Generated UI: rebuilt each time, so layout changes reach the existing scene.
            if (canvas != null) DungeonUI.RebuildScreens(canvas);
            // Step 4: how the delve ends, and the rope out.
            if (Object.FindAnyObjectByType<Hearthdelve.Dungeon.Run.DelveRunController>() == null && harvest != null)
                harvest.gameObject.AddComponent<Hearthdelve.Dungeon.Run.DelveRunController>();
            if (Object.FindAnyObjectByType<Hearthdelve.Dungeon.Run.DelveExit>() == null)
            {
                Transform props = GameObject.Find("Props")?.transform;
                for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (At(x, y) != 'R') continue;
                    var rope = (GameObject)PrefabUtility.InstantiatePrefab(content.RopeExit, props);
                    rope.transform.position = Feet(x, y);
                }
            }
            var placed = new System.Collections.Generic.HashSet<Transform>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                char marker = At(x, y);
                GameObject prefab = EnemyFor(content, marker);
                if (prefab == null) continue;
                Vector2 feet = EnemyFeet(marker, x, y);
                Transform match = null;
                foreach (Transform child in enemies)
                    if (!placed.Contains(child) && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab && Vector2.Distance(child.position, feet) < 0.6f)
                        match = child;
                if (match == null && marker == 'V')
                {
                    // A bat from before perches existed: move it onto this perch.
                    foreach (Transform child in enemies)
                        if (!placed.Contains(child) && PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject) == prefab && !OnMarker(child.position, 'V'))
                            match = child;
                }
                if (match == null) match = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, enemies)).transform;
                match.position = feet;
                placed.Add(match);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Test floor enemies updated.");
        }

        static bool OnMarker(Vector2 position, char marker)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (At(x, y) == marker && Vector2.Distance(EnemyFeet(marker, x, y), position) < 0.6f)
                    return true;
            return false;
        }

        /// <summary>Batch entry point for <see cref="UpdateTestFloor"/>.</summary>
        public static void UpdateTestFloorBatch()
        {
            try
            {
                UpdateTestFloor();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void PaintFloor(Tilemap floor)
        {
            Tile[] tiles =
            {
                LookTestContent.DungeonTile(13, 2, false), LookTestContent.DungeonTile(14, 2, false),
                LookTestContent.DungeonTile(15, 2, false), LookTestContent.DungeonTile(16, 2, false), LookTestContent.DungeonTile(18, 2, false),
            };
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                // Under everything open, and under pillars and wall bricks so no gap shows at their edges.
                if (IsSolid(x, y) && IsSolid(x, y - 1) && IsSolid(x, y + 1)) continue;
                int roll = Mathf.Abs(x * 7349 + y * 9151) % 17;
                floor.SetTile(new Vector3Int(x, y, 0), tiles[roll < 12 ? roll % 2 : 2 + roll % 3]);
            }
        }

        static void PaintWalls(Tilemap walls)
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (At(x, y) == 'o')
                {
                    // A pillar is two tiles: its top face over its brick base.
                    bool top = At(x, y - 1) == 'o';
                    walls.SetTile(new Vector3Int(x, y, 0), LookTestContent.DungeonTile(1, top ? 2 : 3, true));
                }
                else if (IsWall(At(x, y)))
                {
                    Vector2Int cell = WallCell(x, y);
                    walls.SetTile(new Vector3Int(x, y, 0), LookTestContent.DungeonTile(cell.x, cell.y, true));
                }
            }
        }

        enum Face { None, Top, Bricks, BottomTop, BottomBricks }

        /// <summary>How a wall tile reads from the floor directly above or below it.</summary>
        static Face FaceOf(int x, int y)
        {
            if (!IsWall(At(x, y))) return Face.None;
            if (IsOpen(x, y - 1)) return Face.Bricks;
            if (IsWall(At(x, y - 1)) && IsOpen(x, y - 2)) return Face.Top;
            if (IsOpen(x, y + 1)) return Face.BottomTop;
            if (IsWall(At(x, y + 1)) && IsOpen(x, y + 2)) return Face.BottomBricks;
            return Face.None;
        }

        /// <summary>
        /// Picks a tile from the Dungeon tileset's wall sample (columns 4–10, rows 5–12): plain
        /// faces in columns 5–6 (alternating), west ends in column 4, east ends in column 10,
        /// and pieces with walls on both sides in column 7.
        /// </summary>
        static Vector2Int WallCell(int x, int y)
        {
            int alternate = 5 + Mathf.Abs(x) % 2;
            Face face = FaceOf(x, y);
            bool openLeft = IsOpen(x - 1, y), openRight = IsOpen(x + 1, y);

            if (face != Face.None && !openLeft && !openRight) return new Vector2Int(alternate, Row(face, false));
            if (face == Face.None)
            {
                if (openLeft && openRight) return new Vector2Int(7, 7);
                if (openRight) return new Vector2Int(4, 7);
                if (openLeft) return new Vector2Int(10, 7);

                // A corner or a junction: take the face of the wall run beside it.
                Face right = FaceOf(x + 1, y), left = FaceOf(x - 1, y);
                bool junction = IsWall(At(x, y + 1)) && IsWall(At(x, y - 1));
                if (right != Face.None && left != Face.None) return new Vector2Int(7, Row(right, junction));
                if (right != Face.None) return new Vector2Int(4, Row(right, junction));
                if (left != Face.None) return new Vector2Int(10, Row(left, junction));
                return new Vector2Int(alternate, 5);
            }
            // A face at the end of a run that is open at the side.
            return new Vector2Int(openRight && !openLeft ? 4 : openLeft && !openRight ? 10 : 7, Row(face, false));
        }

        static int Row(Face face, bool junction) => face switch
        {
            Face.Top => junction ? 8 : 5,
            Face.Bricks => junction ? 9 : 6,
            Face.BottomTop => 11,
            Face.BottomBricks => 12,
            _ => 5,
        };

        /// <summary>A clean kill rings (Kill.Clean); an overkill or destroyed part thuds (Bump.Soft). Rebuilt each run.</summary>
        static void HarvestFeedbacks(HarvestSystem harvest)
        {
            foreach (string name in new[] { "Feedback_CleanKill", "Feedback_Overkill" })
            {
                Transform old = harvest.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            harvest.ConfigureFeedback(
                LookTestContent.Feedback(harvest.transform, "Feedback_CleanKill", null, 0f, LookTestContent.Sfx("PH_KillClean"), LookTestContent.Pattern(HapticIds.KillClean)),
                LookTestContent.Feedback(harvest.transform, "Feedback_Overkill", null, 0f, LookTestContent.Sfx("PH_Thud"), LookTestContent.Pattern(HapticIds.BumpSoft)));
        }
    }
}
