using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Story;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Kariaston, the village (4h Checkpoint A; docs/PLAN_4H.md §6–7). Its own scene, loaded beside the tavern in the daytime (H1),
    /// at a world offset so the two never overlap. <b>The blockout is generated once</b> (H13): the ground, paths, the square,
    /// the buildings, trees and props are created only when the scene doesn't exist (or a rebuild is approved, like every
    /// scene builder), and are hand-owned Unity content from then on. <see cref="UpdateGameplay"/> maintains only the named
    /// gameplay objects under <c>Kariaston/Gameplay</c> (the area, Tally Ho!'s outside door, the market stall, the memorial's
    /// look, the edges), and never touches tiles or the dressing.
    /// </summary>
    public static class KariastonBuilder
    {
        public const string ScenePath = EditorPaths.Scenes + "/Kariaston.unity";
        /// <summary>Where the village sits in the world: well clear of the tavern (0–28) and the guest room (50–68).</summary>
        public static readonly Vector2 Origin = new(200f, 0f);
        public const int Width = 72, Height = 48;

        // Landmarks, in village cells (y up). Tally Ho! faces south over the village: its porch steps at (36, 30.75).
        public static readonly Vector2 TallyHo = new(36f, 33f);
        public static readonly Vector2 TallyHoDoor = new(36f, 30.9f);
        public static readonly Vector2 MaximoHouse = new(13f, 14f);
        public static readonly Vector2 GrimCottage = new(51f, 12.5f);
        public static readonly Vector2 KalorenTower = new(62f, 12f);
        public static readonly Vector2 BartWagon = new(24f, 15f);
        public static readonly Vector2 Memorial = new(35.5f, 19f);
        public static readonly Vector2 Well = new(31.5f, 14.5f);
        public static readonly Vector2 Market = new(40f, 12.6f);
        public static readonly RectInt Square = new(29, 11, 14, 12);
        public static readonly RectInt[] GardenBeds = { new(15, 37, 3, 2), new(19, 37, 3, 2), new(15, 33, 3, 2), new(19, 33, 3, 2) };
        public static readonly RectInt[] Plots = { new(48, 30, 8, 7), new(58, 30, 8, 7), new(50, 1, 8, 5) };

        const string k_Root = "Kariaston";
        const string k_Gameplay = "Gameplay";
        static readonly Color k_Daylight = new(1f, 0.98f, 0.94f);

        [MenuItem("Hearthdelve/Generate/Kariaston (4h)", priority = 5)]
        public static void GenerateMenu() => Ensure(rebuildApproved: false);

        /// <summary>Batch: <c>-executeMethod Hearthdelve.Editor.KariastonBuilder.EnsureBatch [-rebuildScene]</c>.</summary>
        public static void EnsureBatch()
        {
            try
            {
                Ensure(Environment.GetCommandLineArgs().Contains("-rebuildScene"));
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Creates the village once (or again, only when approved), then maintains its gameplay objects.</summary>
        public static void Ensure(bool rebuildApproved)
        {
            MinifantasyImporter.Import(KariastonSheets.Sheets());
            if (LookTestBuilder.MayWrite(ScenePath, rebuildApproved)) Build();
            UpdateGameplay();
        }

        /// <summary>Batch (run without -nographics): the whole village from above, one art pixel per pixel, to BatchLogs/kariaston.png.</summary>
        public static void CaptureBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var go = new GameObject("Capture Camera");
                var camera = go.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Height / 2f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                go.AddComponent<UniversalAdditionalCameraData>();
                go.transform.position = new Vector3(Origin.x + Width / 2f, Origin.y + Height / 2f, -10f);
                int w = Width * MinifantasySheets.PixelsPerUnit, h = Height * MinifantasySheets.PixelsPerUnit;
                var target = new RenderTexture(w, h, 24) { filterMode = FilterMode.Point };
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var image = new Texture2D(w, h, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                image.Apply();
                Directory.CreateDirectory("BatchLogs");
                File.WriteAllBytes("BatchLogs/kariaston.png", image.EncodeToPNG());
                RenderTexture.active = null;
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // ------------------------------------------------------------------ the blockout (generated once)

        static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(k_Root).transform;
            root.position = Origin;
            PaintGround(root);
            Transform dressing = Child(root, "Dressing");
            PlaceBuildings(Child(dressing, "Buildings"));
            PlaceGarden(root, Child(dressing, "Garden"));
            PlacePlots(Child(dressing, "Plots"));
            PlaceTrees(Child(dressing, "Trees"));
            PlaceProps(Child(dressing, "Props"));
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Hearthdelve] {ScenePath} generated (the blockout is hand-owned from here).");
        }

        static Transform Child(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return found;
            var go = new GameObject(name).transform;
            go.SetParent(parent, false);
            return go;
        }

        // ---- ground: grass everywhere, dirt paths and the stone square, autotiled from Forgotten Plains' edges

        static void PaintGround(Transform root)
        {
            var grid = new GameObject("Ground").AddComponent<Grid>();
            grid.transform.SetParent(root, false);
            Tilemap grass = LookTestBuilder.Layer(grid, "Grass", SortingLayers.Floor, 0, solid: false);
            Tilemap paths = LookTestBuilder.Layer(grid, "Paths", SortingLayers.Floor, 1, solid: false);
            Tilemap stone = LookTestBuilder.Layer(grid, "Square", SortingLayers.Floor, 2, solid: false);

            var random = new System.Random(4);
            string[] meadow = { "Grass0", "Grass0", "Grass0", "Grass1", "Grass2", "Grass3", "Grass4", "Grass5", "Grass6", "Grass7" };
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                grass.SetTile(new Vector3Int(x, y, 0), GroundTile(meadow[random.Next(meadow.Length)]));

            var dirt = new HashSet<Vector2Int>();
            void Rect(int x, int y, int w, int h)
            {
                for (int i = x; i < x + w; i++)
                for (int j = y; j < y + h; j++)
                    dirt.Add(new Vector2Int(i, j));
            }
            Rect(2, 7, Width - 3, 3);            // the main road, west to the closed road east
            Rect(35, 9, 3, 3);                    // the road up into the square
            Rect(35, 23, 3, 8);                   // the square up to Tally Ho!'s porch
            Rect(20, 28, 18, 3);                  // Tally Ho!'s porch west to the garden
            Rect(22, 30, 3, 5);                   // and up into the garden's open east side
            Rect(12, 10, 3, 3);                   // up to Maximo's door
            Rect(50, 10, 3, 3);                   // up to Grim and Ogrin's door
            Rect(61, 10, 3, 3);                   // up to Kaloren's tower
            Rect(43, 28, 4, 3);                   // east from the porch toward the empty plots
            Paint(paths, dirt, "Dirt");

            var square = new HashSet<Vector2Int>();
            for (int x = Square.xMin; x < Square.xMax; x++)
            for (int y = Square.yMin; y < Square.yMax; y++)
                square.Add(new Vector2Int(x, y));
            Paint(stone, square, "Stone");
        }

        /// <summary>Autotiles a region (at least two cells wide everywhere) with the edge, corner and inner-corner pieces.</summary>
        static void Paint(Tilemap map, HashSet<Vector2Int> cells, string kind)
        {
            foreach (Vector2Int c in cells)
            {
                bool n = cells.Contains(c + Vector2Int.up), s = cells.Contains(c + Vector2Int.down);
                bool e = cells.Contains(c + Vector2Int.right), w = cells.Contains(c + Vector2Int.left);
                string part =
                    !n && !w ? "TL" : !n && !e ? "TR" : !s && !w ? "BL" : !s && !e ? "BR" :
                    !n ? "T" : !s ? "B" : !w ? "L" : !e ? "R" :
                    !cells.Contains(c + new Vector2Int(-1, 1)) ? "InNW" : !cells.Contains(c + new Vector2Int(1, 1)) ? "InNE" :
                    !cells.Contains(c + new Vector2Int(-1, -1)) ? "InSW" : !cells.Contains(c + new Vector2Int(1, -1)) ? "InSE" : "C";
                map.SetTile(new Vector3Int(c.x, c.y, 0), GroundTile($"{kind}_{part}"));
            }
        }

        static Tile GroundTile(string name) => SheetTile(KariastonSheets.PlainsPack, KariastonSheets.Tiles, name, $"Kariaston_{name}");

        static Tile SheetTile(string pack, string file, string name, string asset)
        {
            Sprite sprite = MinifantasyImporter.Sprite(pack, file, name);
            return LookTestContent.CreateOrUpdate<Tile>($"{EditorPaths.Tiles}/{asset}.asset", tile =>
            {
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
            });
        }

        // ---- buildings and landmarks

        static Sprite Art(string pack, string file, string name) => MinifantasyImporter.Sprite(pack, file, name);

        static Sprite Single(string pack, string file) => MinifantasyImporter.Sprites(pack, file).Values.First();

        static SpriteRenderer Put(Transform parent, string name, Sprite sprite, Vector2 at)
        {
            if (sprite == null) throw new InvalidOperationException($"Kariaston: no sprite for {name}.");
            return LookTestContent.AddSprite(parent, name, sprite, SortingLayers.YSorted, 0, at);
        }

        /// <summary>A solid footprint, in village cells, under <paramref name="parent"/> (relative to the village's origin).</summary>
        static void Block(Transform parent, string name, Rect cells) => LookTestBuilder.Solid(parent, name, cells.center - (Vector2)parent.position + Origin, cells.size);

        static void PlaceBuildings(Transform parent)
        {
            // Tally Ho! (the outside of it; inside is the Tavern scene): sorted at its wings' wall foot, the porch reaching lower.
            Transform tally = Put(parent, "Tally Ho!", Art(KariastonSheets.TownsIIPack, KariastonSheets.Buildings, "ThatchedHall"), TallyHo).transform;
            Block(tally, "Body", new Rect(TallyHo.x - 7.2f, TallyHo.y, 14.4f, 9f));
            Block(tally, "Porch", new Rect(TallyHo.x - 1.4f, TallyHo.y - 1.8f, 2.8f, 1.8f));
            Put(parent, "Tally Ho! Board", Art(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "TankardBoard"), new Vector2(40.6f, 29.9f));

            Transform maximo = Put(parent, "Maximo's House", Art(KariastonSheets.TownsIIPack, KariastonSheets.Buildings, "BlueRoofHall"), MaximoHouse).transform;
            Block(maximo, "Body", new Rect(MaximoHouse.x - 6.6f, MaximoHouse.y, 13.2f, 8f));
            Block(maximo, "Porch", new Rect(MaximoHouse.x - 1.4f, MaximoHouse.y - 1.7f, 2.8f, 1.7f));

            Transform grim = Put(parent, "Grim and Ogrin's Cottage", Art(KariastonSheets.TownsIIPack, KariastonSheets.Buildings, "BrownCottage"), GrimCottage).transform;
            Block(grim, "Body", new Rect(GrimCottage.x - 3.4f, GrimCottage.y + 0.2f, 6.8f, 6f));

            Transform tower = Put(parent, "Kaloren's Tower", Single(KariastonSheets.WizardTowerPack, KariastonSheets.Tower), KalorenTower).transform;
            Block(tower, "Base", new Rect(KalorenTower.x - 1.5f, KalorenTower.y + 0.3f, 3f, 2.2f));

            Transform wagon = Put(parent, "Bart's Wagon", Art(KariastonSheets.WagonsPack, KariastonSheets.Wagons, "PaintedWagon"), BartWagon).transform;
            Block(wagon, "Wheels", new Rect(BartWagon.x - 1.6f, BartWagon.y + 0.1f, 3.2f, 1.6f));

            // The well, and Karias's memorial (a pedestal with an old bronze figure; its look is a gameplay object).
            Transform well = Put(parent, "Well", Art(KariastonSheets.WellPack, KariastonSheets.Well, "Well"), Well).transform;
            Block(well, "Base", new Rect(Well.x - 1.2f, Well.y + 0.1f, 2.4f, 1.6f));
            var memorial = new GameObject("Karias Memorial");
            memorial.transform.SetParent(parent, false);
            memorial.transform.localPosition = Memorial;
            memorial.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            Put(memorial.transform, "Pedestal", Art(KariastonSheets.MonumentsPack, KariastonSheets.Monuments, "Pedestal"), Vector2.zero);
            SpriteRenderer figure = Put(memorial.transform, "Figure", Art(KariastonSheets.MonumentsPack, KariastonSheets.Monuments, "Figure"), new Vector2(0f, 2.5f));
            figure.sortingOrder = 1;
            SpriteRenderer plaque = Put(memorial.transform, "Plaque", Art(KariastonSheets.MonumentsPack, KariastonSheets.Monuments, "Plaque"), new Vector2(0f, -0.4f));
            plaque.sortingOrder = 2;
            Block(memorial.transform, "Base", new Rect(Memorial.x - 1f, Memorial.y, 2f, 1.4f));
        }

        static void PlaceGarden(Transform root, Transform parent)
        {
            // Four inert beds on Tally Ho!'s grounds (Checkpoint B gives them crops): tilled soil, 3×2 each, edged with grass.
            Grid grid = root.Find("Ground").GetComponent<Grid>();
            Tilemap beds = LookTestBuilder.Layer(grid, "Garden Beds", SortingLayers.Floor, 3, solid: false);
            foreach (RectInt bed in GardenBeds)
            {
                var cells = new HashSet<Vector2Int>();
                for (int x = bed.xMin; x < bed.xMax; x++)
                for (int y = bed.yMin; y < bed.yMax; y++)
                    cells.Add(new Vector2Int(x, y));
                Paint(beds, cells, "Dirt");
            }
            Sprite fence = Art(KariastonSheets.FarmPack, KariastonSheets.FarmTiles, "FenceRun");
            for (float x = 14.5f; x < 23.5f; x += 3f)
            {
                Put(parent, "Fence North", fence, new Vector2(x + 1.5f, 40.2f));
                Put(parent, "Fence South", fence, new Vector2(x + 1.5f, 31.6f));
            }
            Put(parent, "Scarecrow", Art(KariastonSheets.FarmPack, KariastonSheets.FarmProps, "Scarecrow"), new Vector2(23.8f, 36.2f));
            Put(parent, "Haystack", Art(KariastonSheets.FarmPack, KariastonSheets.FarmProps, "Haystack"), new Vector2(12.2f, 38.5f));
            Put(parent, "Bales", Art(KariastonSheets.FarmPack, KariastonSheets.FarmProps, "Bales"), new Vector2(12.4f, 34.6f));
            Put(parent, "Bucket", Art(KariastonSheets.FarmPack, KariastonSheets.FarmProps, "Bucket"), new Vector2(18.5f, 36.3f));
        }

        static void PlacePlots(Transform parent)
        {
            // Three empty residential plots (GDD Decided 23): fenced, with a sign. Nothing lives there yet.
            Sprite fence = Art(KariastonSheets.FarmPack, KariastonSheets.FarmTiles, "FenceRun");
            Sprite sign = Art(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "PostSign");
            for (int i = 0; i < Plots.Length; i++)
            {
                RectInt plot = Plots[i];
                Transform p = Child(parent, $"Plot {i + 1}");
                for (float x = plot.xMin; x + 3f <= plot.xMax + 0.01f; x += 3f)
                {
                    Put(p, "Fence North", fence, new Vector2(x + 1.5f, plot.yMax - 0.4f));
                    Put(p, "Fence South", fence, new Vector2(x + 1.5f, plot.yMin + 0.2f));
                }
                Put(p, "Sign", sign, new Vector2(plot.xMin + 0.8f, plot.yMin + 0.4f));
            }
            // The road east is closed for now (the expansion edge).
            Put(parent, "Road East Sign", sign, new Vector2(68.6f, 10.6f));
            Put(parent, "Road East Fence", fence, new Vector2(69.6f, 9.8f));
            Put(parent, "Road East Fence Low", fence, new Vector2(69.6f, 7f));
        }

        static void PlaceTrees(Transform parent)
        {
            var random = new System.Random(11);
            Sprite[] trees =
            {
                Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "TreeLarge"), Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "TreeMedium"),
                Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "TreeSmall"),
            };
            Sprite[] bushes =
            {
                Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "Bush0"), Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "Bush1"),
                Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "Bush2"), Art(KariastonSheets.FoliagePack, KariastonSheets.Foliage, "Bush3"),
            };
            void Tree(Vector2 at, int size)
            {
                SpriteRenderer t = Put(parent, "Tree", trees[size], at);
                Block(t.transform, "Trunk", new Rect(at.x - 0.6f, at.y - 0.1f, 1.2f, 0.8f));
            }
            // The edges: a wood behind the village (north), and down both sides.
            for (float x = 3f; x < Width - 2f; x += 9f + (float)random.NextDouble() * 3f) Tree(new Vector2(x, 45.2f), random.Next(2));
            for (float y = 13f; y < 44f; y += 7f + (float)random.NextDouble() * 3f)
            {
                Tree(new Vector2(1.8f, y), random.Next(1, 3));
                Tree(new Vector2(Width - 2f, y), random.Next(1, 3));
            }
            // A few in the village: one on the green, by the plots, behind the tower and the cottage.
            Tree(new Vector2(6f, 33f), 1);
            Tree(new Vector2(46f, 24f), 2);
            Tree(new Vector2(8f, 28f), 1);
            Tree(new Vector2(57f, 23f), 2);
            Tree(new Vector2(29f, 3f), 1);
            Tree(new Vector2(44f, 3f), 2);
            // Bushes along the south edge and here and there.
            for (float x = 2f; x < Width - 1f; x += 3.5f + (float)random.NextDouble() * 2f)
                Put(parent, "Bush", bushes[random.Next(bushes.Length)], new Vector2(x, 0.4f));
            foreach (Vector2 at in new[] { new Vector2(5f, 22f), new Vector2(26f, 32f), new Vector2(45f, 36f), new Vector2(66f, 26f), new Vector2(20f, 4f) })
                Put(parent, "Bush", bushes[random.Next(bushes.Length)], at);
        }

        static void PlaceProps(Transform parent)
        {
            Sprite lamp = Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "LampPost");
            foreach (Vector2 at in new[] { new Vector2(28.5f, 22.6f), new Vector2(43.5f, 22.6f), new Vector2(28.5f, 11.4f), new Vector2(43.5f, 11.4f), new Vector2(34.2f, 30.4f) })
            {
                SpriteRenderer l = Put(parent, "Lamp Post", lamp, at);
                Block(l.transform, "Post", new Rect(at.x - 0.25f, at.y, 0.5f, 0.4f));
            }
            Sprite bench = Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "Bench");
            foreach (Vector2 at in new[] { new Vector2(31.5f, 21.2f), new Vector2(40.5f, 21.2f), new Vector2(26.5f, 13.2f), new Vector2(22f, 24.2f) })
            {
                SpriteRenderer b = Put(parent, "Bench", bench, at);
                Block(b.transform, "Seat", new Rect(at.x - 1f, at.y, 2f, 0.6f));
            }
            Put(parent, "Flower Box", Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "FlowerBox"), new Vector2(32f, 32.2f));
            Put(parent, "Flower Box", Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "FlowerBoxRed"), new Vector2(40f, 32.2f));
            Put(parent, "Flower Box", Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "FlowerBox"), new Vector2(16f, 13.2f));
            Sprite barrel = Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "Barrel");
            foreach (Vector2 at in new[] { new Vector2(44.2f, 31.2f), new Vector2(45.6f, 31.4f), new Vector2(44.8f, 32.6f), new Vector2(43.4f, 14.6f) })
            {
                SpriteRenderer b = Put(parent, "Barrel", barrel, at);
                Block(b.transform, "Body", new Rect(at.x - 0.7f, at.y, 1.4f, 0.8f));
            }
            Put(parent, "Crate", Art(MinifantasySheets.MedievalCity, KariastonSheets.CityProps, "Crate"), new Vector2(42.4f, 13.4f));
            Put(parent, "Trough", Art(KariastonSheets.FarmPack, KariastonSheets.FarmProps, "Trough"), new Vector2(55.5f, 11.4f));
        }

        // ------------------------------------------------------------------ gameplay objects (maintained, by name)

        /// <summary>
        /// The village's gameplay objects, rebuilt by name under <c>Kariaston/Gameplay</c>: nothing else in the scene is touched (H13).
        /// The area (camera bounds and its own lit daylight), Tally Ho!'s outside door, the market stall, the memorial's look and the
        /// village's edges.
        /// </summary>
        public static void UpdateGameplay()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject rootGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == k_Root);
            if (rootGo == null) throw new InvalidOperationException($"{ScenePath} has no {k_Root} root.");
            Transform root = rootGo.transform;
            for (Transform old = root.Find(k_Gameplay); old != null; old = root.Find(k_Gameplay)) Object.DestroyImmediate(old.gameObject);
            Transform gameplay = Child(root, k_Gameplay);

            // The area: the camera follows the keeper inside the village; its daylight is lit only while the keeper is here.
            Light2D daylight = LookTestBuilder.Light("Daylight", Vector3.zero, Light2D.LightType.Global);
            daylight.transform.SetParent(gameplay, false);
            daylight.color = k_Daylight;
            daylight.intensity = 1f;
            daylight.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            daylight.gameObject.AddComponent<SurfaceDaylight>().Configure(daylight);
            // Authored dark: the village loads beside the lit tavern, and only one global light may be lit (SurfaceArea lights it).
            daylight.enabled = false;
            var areaGo = new GameObject("Area");
            areaGo.transform.SetParent(gameplay, false);
            SurfaceArea area = areaGo.AddComponent<SurfaceArea>();
            area.Configure(SurfaceArea.KariastonId, indoors: false, isDefault: false, SurfaceCameraMode.Follow, Vector2.zero,
                new Rect(Origin, new Vector2(Width, Height)), daylight);

            // Tally Ho!'s front door, outside: onto its porch steps, back in. Arrive a step south of it.
            var door = new GameObject("Tally Ho! Door");
            door.transform.SetParent(gameplay, false);
            door.transform.localPosition = TallyHoDoor;
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.1f, 0.5f);
            door.AddComponent<SurfaceDoor>().Configure(SurfaceDoor.FrontOutside, SurfaceDoor.FrontInside, area, new Vector2(0f, -1.3f));

            BuildMarket(gameplay);
            BuildMemorialLook(gameplay);
            BuildEdges(gameplay);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hearthdelve] {ScenePath}: gameplay objects updated (the blockout untouched).");
        }

        static void BuildMarket(Transform gameplay)
        {
            var stall = new GameObject("Market Stall");
            stall.transform.SetParent(gameplay, false);
            stall.transform.localPosition = Market;
            SpriteRenderer open = Put(stall.transform, "Open", Art(KariastonSheets.MerchantPack, KariastonSheets.CartOpen, "Cart"), Vector2.zero);
            SpriteRenderer closed = Put(stall.transform, "Closed", Art(KariastonSheets.MerchantPack, KariastonSheets.CartClosed, "Cart"), Vector2.zero);
            closed.gameObject.SetActive(false);
            Block(stall.transform, "Body", new Rect(Market.x - 2.6f, Market.y + 0.4f, 5.2f, 2.2f));
            var use = new GameObject("Use");
            use.transform.SetParent(stall.transform, false);
            var interactable = use.AddComponent<TavernInteractable>();
            interactable.Configure(TavernInteractableKind.MarketStall, SurfaceLocKeys.Market, new Vector2(0f, -0.3f), 1.4f, null);
            stall.AddComponent<MarketStall>().Configure(interactable, open.gameObject, closed.gameObject, SurfaceLocKeys.MarketClosed);
        }

        static void BuildMemorialLook(Transform gameplay)
        {
            var look = new GameObject("Memorial Look");
            look.transform.SetParent(gameplay, false);
            look.transform.localPosition = Memorial;
            var interactable = look.AddComponent<TavernInteractable>();
            interactable.Configure(TavernInteractableKind.Inspect, SurfaceLocKeys.LookMemorial, new Vector2(0f, -0.9f), 1.1f, null);
            look.AddComponent<DaytimeFixture>().Configure(SurfaceConversations.Memorial);
        }

        /// <summary>The village's edges: nobody walks off the map.</summary>
        static void BuildEdges(Transform gameplay)
        {
            Transform edges = Child(gameplay, "Edges");
            LookTestBuilder.Solid(edges, "West", new Vector2(0.5f, Height / 2f), new Vector2(1f, Height));
            LookTestBuilder.Solid(edges, "East", new Vector2(Width - 0.5f, Height / 2f), new Vector2(1f, Height));
            LookTestBuilder.Solid(edges, "South", new Vector2(Width / 2f, 0.25f), new Vector2(Width, 0.5f));
            LookTestBuilder.Solid(edges, "North", new Vector2(Width / 2f, Height - 2.5f), new Vector2(Width, 5f));
        }
    }
}
