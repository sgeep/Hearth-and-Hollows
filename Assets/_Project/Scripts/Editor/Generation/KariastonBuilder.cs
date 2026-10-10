using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Animation;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Navigation;
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
    /// scene builder), and are hand-owned Unity content from then on. Since 2026-10-10 the blockout is the Crossroads, read from
    /// the mockup's export (<see cref="LayoutPath"/>); the existing scene was relaid out once (<see cref="RelayoutBatch"/>). <see cref="UpdateGameplay"/> maintains only the named
    /// gameplay objects under <c>Kariaston/Gameplay</c> (the area, Tally Ho!'s outside door, the market stall, the memorial's
    /// look, the edges), and never touches tiles or the dressing.
    /// </summary>
    public static class KariastonBuilder
    {
        public const string ScenePath = EditorPaths.Scenes + "/Kariaston.unity";
        /// <summary>Where the village sits in the world: well clear of the tavern (0–28) and the guest room (50–68).</summary>
        public static readonly Vector2 Origin = new(200f, 0f);
        /// <summary>The Crossroads (2026-10-10): 64×42 tiles, as the mockup.</summary>
        public const int Width = 64, Height = 42;

        // Landmarks, in village cells (y up), where the Crossroads puts them (their art's pixels, the game's pivots).
        // Tally Ho! faces south onto the square: its porch steps' foot at (32, 26.125).
        public static readonly Vector2 TallyHo = new(32f, 28.375f);
        public static readonly Vector2 TallyHoDoor = new(32f, 26.275f);
        public static readonly Vector2 Memorial = new(32f, 19.5f);
        public static readonly Vector2 Market = new(37.5f, 14.75f);
        public static readonly Vector2 Cottage = new(47f, 6.75f);
        /// <summary>The garden's four beds (garden_1..4, reading order): the cells their plants stand on.</summary>
        public static readonly RectInt[] GardenBeds = { new(9, 35, 3, 2), new(13, 35, 3, 2), new(9, 31, 3, 2), new(13, 31, 3, 2) };
        /// <summary>Each bed's soil: its plants' cells and one row above them (the crops' art stands tall into it).</summary>
        public static RectInt GardenSoil(int bed) => new(GardenBeds[bed].x, GardenBeds[bed].y, GardenBeds[bed].width, GardenBeds[bed].height + 1);

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

        // ------------------------------------------------------------------ the blockout (generated once: the Crossroads)

        /// <summary>The Crossroads' layout, exported once from the mockup (Tools/village/mockup/export_crossroads.py).</summary>
        public const string LayoutPath = "Tools/village/crossroads_layout.json";
        const string k_RelayoutFlag = "-relayoutApproved";

        static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(k_Root).transform;
            root.position = Origin;
            LayOut(root);
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Hearthdelve] {ScenePath} generated (the blockout is hand-owned from here).");
        }

        /// <summary>
        /// Batch, once, with the owner's go-ahead (2026-10-10): <c>-executeMethod Hearthdelve.Editor.KariastonBuilder.RelayoutBatch
        /// -relayoutApproved</c> replaces the hand-owned village's ground and dressing with the Crossroads in the existing scene (its
        /// asset, settings and root are kept), then maintains the gameplay objects as usual. It refuses to run without the flag, and
        /// does nothing once the village is the Crossroads (its ground has a Cobble map), so the result is hand-owned again.
        /// </summary>
        public static void RelayoutBatch()
        {
            try
            {
                if (!Environment.GetCommandLineArgs().Contains(k_RelayoutFlag))
                    throw new InvalidOperationException($"The relayout replaces Kariaston's hand-owned ground and dressing: pass {k_RelayoutFlag} with the owner's go-ahead.");
                MinifantasyImporter.Import(KariastonSheets.Sheets());
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Transform root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == k_Root)?.transform
                                 ?? throw new InvalidOperationException($"{ScenePath} has no {k_Root} root.");
                if (root.Find("Ground/Cobble") != null)
                {
                    Debug.Log("[Hearthdelve] Kariaston is already the Crossroads: nothing replaced.");
                }
                else
                {
                    foreach (string part in new[] { "Ground", "Dressing" })
                        for (Transform old = root.Find(part); old != null; old = root.Find(part)) Object.DestroyImmediate(old.gameObject);
                    LayOut(root);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("[Hearthdelve] Kariaston relaid out as the Crossroads.");
                }
                UpdateGameplay();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // ---- the export (art pixels and tiles, origin top-left, y down; flat int lists for JsonUtility)

        [Serializable] sealed class Layout
        {
            public int width, height;
            public int[] grass, beds;
            public Ground[] ground;
            public Placed[] sprites;
        }

        [Serializable] sealed class Ground
        {
            public string kind;
            public int[] cells, expected;
        }

        [Serializable] sealed class Placed
        {
            public string name, group, sheet;
            public int[] rect, shadow;
            public int left, top;
            public bool flip;
        }

        static Layout ReadLayout()
        {
            if (!File.Exists(LayoutPath)) throw new InvalidOperationException($"Kariaston: no layout at {LayoutPath} (run Tools/village/mockup/export_crossroads.py).");
            Layout layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            if (layout.width != Width || layout.height != Height) throw new InvalidOperationException($"Kariaston: the layout is {layout.width}×{layout.height}, the village {Width}×{Height}.");
            return layout;
        }

        /// <summary>The export's cell (x, y down) as a village cell (y up).</summary>
        static Vector3Int CellOf(int x, int y) => new(x, Height - 1 - y, 0);

        static void LayOut(Transform root)
        {
            Layout layout = ReadLayout();
            PaintGround(root, layout);
            PlaceDressing(Child(root, "Dressing"), layout);
        }

        /// <summary>
        /// The ground: the meadow cell by cell as the script picked it, then each region in its own map with its rule tile (the roads
        /// run two cells past the edge, off camera, so the border reads as a road that carries on). Every cell the script drew is
        /// checked against the rule tile's choice: a difference stops the relayout.
        /// </summary>
        static void PaintGround(Transform root, Layout layout)
        {
            var grid = new GameObject("Ground").AddComponent<Grid>();
            grid.transform.SetParent(root, false);
            Tilemap grass = LookTestBuilder.Layer(grid, "Grass", SortingLayers.Floor, 0, solid: false);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = (y * Width + x) * 2;
                grass.SetTile(CellOf(x, y), KariastonGround.Grass(KariastonGround.GrassCell(layout.grass[i], layout.grass[i + 1])));
            }

            var maps = new (string kind, string map, int order, bool solid, string tile)[]
            {
                ("patch0", "Grass Patches", 1, false, KariastonGround.PatchA), ("patch1", "Grass Patches", 1, false, KariastonGround.PatchB),
                ("dirt", "Paths", 2, false, KariastonGround.Dirt), ("cobble", "Cobble", 3, false, KariastonGround.Cobble),
                ("water", "Pond", 4, true, KariastonGround.Water), ("soil", "Garden Beds", 5, false, KariastonGround.Dirt),
            };
            var mismatches = new List<string>();
            foreach (var (kind, mapName, order, solid, tileName) in maps)
            {
                Ground region = layout.ground.FirstOrDefault(g => g.kind == kind) ?? throw new InvalidOperationException($"Kariaston: no {kind} in the layout.");
                Tilemap map = grid.transform.Find(mapName)?.GetComponent<Tilemap>() ?? LookTestBuilder.Layer(grid, mapName, SortingLayers.Floor, order, solid);
                RuleTile tile = KariastonGround.Tile(tileName);
                for (int i = 0; i < region.cells.Length; i += 2) map.SetTile(CellOf(region.cells[i], region.cells[i + 1]), tile);
                map.RefreshAllTiles();
                KariastonGround.Autotile a = KariastonGround.Autotiles[tileName];
                for (int i = 0; i < region.expected.Length; i += 4)
                {
                    Vector3Int cell = CellOf(region.expected[i], region.expected[i + 1]);
                    int column = region.expected[i + 2], row = region.expected[i + 3];
                    Sprite drawn = map.GetSprite(cell);
                    Sprite wanted = ExpectedCell(a, column, row);
                    if (drawn != wanted) mismatches.Add($"{kind} ({region.expected[i]},{region.expected[i + 1]}): {(drawn != null ? drawn.name : "nothing")}, the script drew {column},{row}");
                }
            }
            if (mismatches.Count > 0)
                throw new InvalidOperationException($"Kariaston: {mismatches.Count} ground cells differ from the mockup:\n" + string.Join("\n", mismatches.Take(20)));
            Debug.Log("[Hearthdelve] Kariaston's ground matches the mockup cell for cell.");
        }

        static Sprite ExpectedCell(KariastonGround.Autotile a, int column, int row)
        {
            if (a.file != KariastonSheets.Tiles) return MinifantasyImporter.Sprite(a.pack, a.file, $"Cell_{column}_{row}");
            // The pond (Forgotten Plains' lake keeps its 4h part names).
            string[,] parts = { { "TL", "L", "BL", "InNW", "InSW" }, { "T", "C", "B", "InNE", "InSE" }, { "TR", "R", "BR", "", "" } };
            return MinifantasyImporter.Sprite(KariastonSheets.PlainsPack, KariastonSheets.Tiles, $"Water_{parts[column - a.column, row - a.row]}");
        }

        // ---- the dressing

        /// <summary>Where each drawing is cut: its pack and file.</summary>
        static (string pack, string file) SheetOf(string name) => name switch
        {
            "ThatchedHall" or "BlueRoofHall" or "BrownCottage" => (KariastonSheets.TownsIIPack, KariastonSheets.Buildings),
            "TowerExterior" => (KariastonSheets.WizardTowerPack, KariastonSheets.Tower),
            "PaintedWagon" => (KariastonSheets.WagonsPack, KariastonSheets.Wagons),
            "Pedestal" or "Figure" or "Plaque" => (KariastonSheets.MonumentsPack, KariastonSheets.Monuments),
            "Well" => (KariastonSheets.WellPack, KariastonSheets.Well),
            "LampPost" or "BenchLong" or "Barrel" or "BarrelSmall" or "Crate" or "Planter" or "PlanterSmall" => (MinifantasySheets.MedievalCity, KariastonSheets.CityProps),
            "Haystack" or "HayPile" or "Bales" or "Scarecrow" => (KariastonSheets.FarmPack, KariastonSheets.FarmProps),
            "TankardBoard" or "PostSign" => (KariastonSheets.TownsPack, KariastonSheets.TownsProps),
            _ when name.StartsWith("Fence_", StringComparison.Ordinal) => (KariastonSheets.FarmPack, KariastonSheets.FarmTiles),
            _ => (KariastonSheets.FoliagePack, KariastonSheets.Foliage),
        };

        /// <summary>The scene's names for the village's landmarks (other drawings keep their own).</summary>
        static readonly Dictionary<string, string> k_Landmarks = new()
        {
            ["ThatchedHall"] = "Tally Ho!", ["BlueRoofHall"] = "Maximo's House", ["BrownCottage"] = CottageName, ["TowerExterior"] = "Kaloren's Tower",
            ["PaintedWagon"] = "Bart's Wagon", ["TankardBoard"] = "Tally Ho! Board", ["Well"] = "Well",
        };

        public const string CottageName = "Grim and Ogrin's Cottage", ShadowName = "Shadow";

        public static Sprite Drawing(string name)
        {
            var (pack, file) = SheetOf(name);
            return name == "TowerExterior" ? Single(pack, file) : Art(pack, file, name);
        }

        /// <summary>A drawing's Minifantasy shadow (cut by its own extent, pivoted at the drawing's pivot), or null.</summary>
        public static Sprite ShadowOf(string name, string file = null)
        {
            foreach (KariastonSheets.ShadowCut cut in KariastonSheets.Shadows)
                if (cut.name == name && (file == null || cut.file == file))
                    return MinifantasyImporter.Sprites(KariastonSheets.ShadowPack(cut.file), cut.file).TryGetValue($"{cut.file}_{name}", out Sprite s) ? s : null;
            return null;
        }

        /// <summary>Puts a drawing's shadow under it: a child on the floor, flipped with it.</summary>
        public static void AddShadow(SpriteRenderer drawing, Sprite shadow)
        {
            if (shadow == null) return;
            SpriteRenderer s = LookTestContent.AddSprite(drawing.transform, ShadowName, shadow, SortingLayers.Floor, 10, Vector3.zero);
            s.flipX = drawing.flipX;
        }

        /// <summary>A drawing placed by its art's pixels (left, top, y down), whatever its pivot: exactly where the mockup draws it.</summary>
        static Vector2 PositionOf(Placed p, Sprite sprite)
        {
            Vector2 pivot = sprite.pivot;
            float w = sprite.rect.width, h = sprite.rect.height;
            float x = p.left + (p.flip ? w - pivot.x : pivot.x);
            float y = Height * MinifantasySheets.PixelsPerUnit - (p.top + h) + pivot.y;
            return new Vector2(x, y) / MinifantasySheets.PixelsPerUnit;
        }

        static void PlaceDressing(Transform dressing, Layout layout)
        {
            Transform memorial = null;
            Vector2 memorialAt = Vector2.zero;
            foreach (Placed p in layout.sprites.OrderBy(s => s.name == "Pedestal" ? 0 : 1))
            {
                if (p.name == "CartOpen") continue;   // the market cart is a gameplay object (Market Stall)
                Sprite sprite = Drawing(p.name) ?? throw new InvalidOperationException($"Kariaston: no drawing {p.name}.");
                if (Mathf.RoundToInt(sprite.rect.width) != p.rect[2] || Mathf.RoundToInt(sprite.rect.height) != p.rect[3])
                    throw new InvalidOperationException($"Kariaston: {p.name} is cut {sprite.rect.width}×{sprite.rect.height}, the layout says {p.rect[2]}×{p.rect[3]}.");
                Vector2 at = PositionOf(p, sprite);
                if (p.group == "Memorial")
                {
                    // Karias's memorial: one sorting group at the pedestal's foot; its shadow on the floor beside it.
                    if (memorial == null)
                    {
                        memorial = new GameObject("Karias Memorial").transform;
                        memorial.SetParent(Child(dressing, "Buildings"), false);
                        memorial.localPosition = memorialAt = at;
                        memorial.gameObject.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
                        var shadowGo = new GameObject("Karias Memorial Shadow");
                        shadowGo.transform.SetParent(Child(dressing, "Buildings"), false);
                        shadowGo.transform.localPosition = at;
                        SpriteRenderer shadow = shadowGo.AddComponent<SpriteRenderer>();
                        shadow.sprite = ShadowOf("Pedestal");
                        shadow.sortingLayerName = SortingLayers.Floor;
                        shadow.sortingOrder = 10;
                        if (LookTestContent.LitSpriteMaterial != null) shadow.sharedMaterial = LookTestContent.LitSpriteMaterial;
                    }
                    SpriteRenderer part = Put(memorial, p.name, sprite, at - memorialAt);
                    part.sortingOrder = p.name == "Pedestal" ? 0 : p.name == "Figure" ? 1 : 2;
                    continue;
                }
                string name = k_Landmarks.TryGetValue(p.name, out string landmark) ? landmark : p.name;
                SpriteRenderer r = Put(Child(dressing, p.group), name, sprite, at);
                r.flipX = p.flip;
                AddShadow(r, ShadowOf(p.name));
                if (p.name == "BrownCottage") AddLitWindow(r.transform, VillageContent.OgrinWindow);
            }
        }

        static Transform Child(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return found;
            var go = new GameObject(name).transform;
            go.SetParent(parent, false);
            return go;
        }

        public const string LitWindowName = "Lit Window";

        /// <summary>
        /// A building's lit window (2026-10-08): the derived lit glass over the building's own window, a child with the building's pivot
        /// (so it moves with the building), drawn unlit (it's a light) just in front of it, shown by <see cref="LitWindow"/> while the
        /// schedule anchor <paramref name="anchor"/> has someone behind it.
        /// </summary>
        static void AddLitWindow(Transform building, string anchor)
        {
            Sprite glass = Single(KariastonSheets.TownsIIPack, "BrownCottageWindowLit");
            SpriteRenderer lit = LookTestContent.AddSprite(building, LitWindowName, glass, SortingLayers.YSorted, 1, Vector2.zero);
            lit.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            lit.enabled = false;
            lit.gameObject.AddComponent<LitWindow>().Configure(anchor);
        }

        static Sprite Art(string pack, string file, string name) => MinifantasyImporter.Sprite(pack, file, name);

        static Sprite Single(string pack, string file) => MinifantasyImporter.Sprites(pack, file).Values.First();

        static SpriteRenderer Put(Transform parent, string name, Sprite sprite, Vector2 at)
        {
            if (sprite == null) throw new InvalidOperationException($"Kariaston: no sprite for {name}.");
            return LookTestContent.AddSprite(parent, name, sprite, SortingLayers.YSorted, 0, at);
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
            trigger.size = new Vector2(1.6f, 0.7f);
            door.AddComponent<SurfaceDoor>().Configure(SurfaceDoor.FrontOutside, SurfaceDoor.FrontInside, area, new Vector2(0f, -1.3f),
                through: Vector2.up);

            BuildMarket(gameplay);
            NavGrid grid = BuildGrid(gameplay);
            BuildMusashi(gameplay, grid);
            BuildPeople(gameplay, grid);
            BuildGarden(root, gameplay);
            BuildMemorialLook(gameplay);
            BuildEdges(gameplay);
            BuildDressingCollision(gameplay);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hearthdelve] {ScenePath}: gameplay objects updated (the blockout untouched).");
        }

        /// <summary>
        /// The drawings that block wherever they stand, by what they are (2026-10-08; the Crossroads, 2026-10-10), every footprint
        /// measured from the drawing's own pixels:
        /// <list type="bullet">
        /// <item>small props (fences, signs, the board, lamps, benches, barrels, the crate, planters, the plaque) at their base
        /// (<see cref="Measure"/>);</item>
        /// <item>tall drawings (trees, the halls, the cottage, the tower, the wagon, the well, the memorial's pedestal) over the ground
        /// where they would hide a figure standing behind them (<see cref="Hides"/>), so the keeper and the villagers are never lost
        /// behind a canopy or a roof; the halls' porch steps too (<see cref="Porch"/>).</item>
        /// </list>
        /// Bushes, flowers, the hay and the scarecrow stay walk-through. <see cref="DressingCollision"/> also makes the water solid by
        /// tile. To make another drawing solid, add it here and run the surface updater.
        /// </summary>
        static void BuildDressingCollision(Transform gameplay)
        {
            var go = new GameObject("Dressing Collision");
            go.transform.SetParent(gameplay, false);
            var solids = new List<DressingCollision.Solid>
            {
                Measure(Art(KariastonSheets.FarmPack, KariastonSheets.FarmTiles, "FenceRun"), depthPixels: 3),
                Measure(Art(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "PostSign"), depthPixels: 2),
                Measure(Art(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "TankardBoard"), depthPixels: 2),
            };
            foreach (var (name, _, _) in KariastonSheets.ThinFence) solids.Add(Measure(Drawing(name), depthPixels: 8));
            foreach (var (name, depth) in SmallProps) solids.Add(Measure(Drawing(name), depth));
            foreach (string name in TallDrawings)
            {
                Sprite sprite = Drawing(name);
                solids.AddRange(Hides(sprite));
                if (name is "ThatchedHall" or "BlueRoofHall") solids.Add(Porch(sprite));
            }
            go.AddComponent<DressingCollision>().Configure(solids);
        }

        /// <summary>Props solid at their base: the drawing and how many pixels of its foot.</summary>
        public static readonly (string name, int depth)[] SmallProps =
        {
            ("LampPost", 3), ("BenchLong", 3), ("Barrel", 3), ("BarrelSmall", 3), ("Crate", 4), ("Planter", 3), ("PlanterSmall", 3), ("Plaque", 3),
        };

        /// <summary>The drawings tall enough to hide someone behind them.</summary>
        public static readonly string[] TallDrawings =
        {
            "ThatchedHall", "BlueRoofHall", "BrownCottage", "TowerExterior", "PaintedWagon", "Well", "Pedestal", "TreeLarge", "TreeMedium", "TreeSmall",
        };

        /// <summary>A figure, for <see cref="Hides"/>: as wide and tall as the keeper's drawn body, in pixels; hidden past this share.</summary>
        const float k_FigureWidth = 6.4f, k_FigureHeight = 10f, k_HiddenShare = 0.3f;
        /// <summary>The bands <see cref="Hides"/> merges its footprint from (pixels).</summary>
        const int k_Band = 4;

        static bool[,] OpaqueMask(Sprite sprite)
        {
            var source = new Texture2D(2, 2);
            source.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
            Rect r = sprite.rect;
            int x0 = (int)r.x, y0 = (int)r.y, w = (int)r.width, h = (int)r.height;
            var mask = new bool[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                mask[x, y] = source.GetPixel(x0 + x, y0 + y).a > 0.5f;
            Object.DestroyImmediate(source);
            return mask;
        }

        /// <summary>
        /// Where a tall drawing would hide a figure: every foot position behind its sort point (above its pivot) from which more than
        /// <see cref="k_HiddenShare"/> of a keeper-sized body is under the drawing's opaque pixels, as boxes (bands of
        /// <see cref="k_Band"/> pixels, runs merged), starting at the pivot (the Y-sort rule: nobody stops behind it and is drawn over).
        /// </summary>
        internal static List<DressingCollision.Solid> Hides(Sprite sprite)
        {
            bool[,] opaque = OpaqueMask(sprite);
            int w = opaque.GetLength(0), h = opaque.GetLength(1);
            var pivot = sprite.pivot;
            int[,] sum = new int[w + 1, h + 1];   // summed area of the opaque pixels
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                sum[x + 1, y + 1] = (opaque[x, y] ? 1 : 0) + sum[x, y + 1] + sum[x + 1, y] - sum[x, y];
            int Count(int ax, int ay, int bx, int by)
            {
                ax = Mathf.Clamp(ax, 0, w); bx = Mathf.Clamp(bx, 0, w); ay = Mathf.Clamp(ay, 0, h); by = Mathf.Clamp(by, 0, h);
                return ax >= bx || ay >= by ? 0 : sum[bx, by] - sum[ax, by] - sum[bx, ay] + sum[ax, ay];
            }
            bool Hidden(int fx, int fy)
            {
                int ax = Mathf.RoundToInt(fx - k_FigureWidth / 2f), bx = Mathf.RoundToInt(fx + k_FigureWidth / 2f);
                int ay = fy, by = Mathf.RoundToInt(fy + k_FigureHeight);
                return Count(ax, ay, bx, by) > k_HiddenShare * (bx - ax) * (by - ay);
            }

            var bands = new List<(int y0, int y1, List<(int x0, int x1)> runs)>();
            for (int y0 = Mathf.CeilToInt(pivot.y); y0 < h; y0 += k_Band)
            {
                int y1 = Mathf.Min(h, y0 + k_Band);
                var runs = new List<(int, int)>();
                int start = int.MinValue;
                for (int x = -8; x <= w + 8; x++)
                {
                    bool hidden = false;
                    for (int y = y0; y < y1 && !hidden; y++) hidden = x <= w + 7 && Hidden(x, y);
                    if (hidden && start == int.MinValue) start = x;
                    if (!hidden && start != int.MinValue)
                    {
                        runs.Add((start, x));
                        start = int.MinValue;
                    }
                }
                if (runs.Count > 0) bands.Add((y0, y1, runs));
            }
            // Merge each run with the same run in the band above while its ends stay within a pixel.
            var boxes = new List<RectInt>();
            foreach (var (y0, y1, runs) in bands)
            foreach (var (x0, x1) in runs)
            {
                int i = boxes.FindIndex(b => b.yMax == y0 && Mathf.Abs(b.xMin - x0) <= 1 && Mathf.Abs(b.xMax - x1) <= 1);
                if (i >= 0) boxes[i] = new RectInt(Mathf.Min(boxes[i].xMin, x0), boxes[i].yMin, Mathf.Max(boxes[i].xMax, x1) - Mathf.Min(boxes[i].xMin, x0), y1 - boxes[i].yMin);
                else boxes.Add(new RectInt(x0, y0, x1 - x0, y1 - y0));
            }
            float ppu = sprite.pixelsPerUnit;
            int firstBand = Mathf.CeilToInt(pivot.y);
            return boxes.Select(b =>
            {
                float bottom = b.yMin == firstBand ? pivot.y : b.yMin;   // the lowest boxes reach down to the pivot exactly
                return new DressingCollision.Solid
                {
                    sprite = sprite,
                    offset = new Vector2((b.center.x - pivot.x) / ppu, ((bottom + b.yMax) / 2f - pivot.y) / ppu),
                    size = new Vector2(b.width / ppu, (b.yMax - bottom) / ppu),
                };
            }).ToList();
        }

        /// <summary>
        /// A hall's porch steps, below its pivot (the wings' wall foot): solid from half a tile above the art's bottom, so the door's
        /// trigger at the steps' foot can be stood in, as wide as the steps' pixels there.
        /// </summary>
        internal static DressingCollision.Solid Porch(Sprite sprite)
        {
            bool[,] opaque = OpaqueMask(sprite);
            int w = opaque.GetLength(0), h = opaque.GetLength(1), top = Mathf.FloorToInt(sprite.pivot.y);
            int bottom = Enumerable.Range(0, h).First(y => Enumerable.Range(0, w).Any(x => opaque[x, y])) + 4;
            int left = w, right = -1;
            for (int y = bottom; y < top; y++)
            for (int x = 0; x < w; x++)
                if (opaque[x, y])
                {
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                }
            float ppu = sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot;
            return new DressingCollision.Solid
            {
                sprite = sprite,
                offset = new Vector2(((left + right + 1) / 2f - pivot.x) / ppu, ((bottom + top) / 2f - pivot.y) / ppu),
                size = new Vector2((right - left + 1) / ppu, (top - bottom) / ppu),
            };
        }

        /// <summary>
        /// A drawing's footprint from its pixels: its lowest opaque row and <paramref name="depthPixels"/> above it, as wide as the
        /// opaque pixels in those rows (a sign's post, not its board). The bottom edge is the art's own bottom (the Y-sort rule).
        /// </summary>
        internal static DressingCollision.Solid Measure(Sprite sprite, int depthPixels)
        {
            if (sprite == null) throw new InvalidOperationException("Kariaston: a solid drawing is missing.");
            var source = new Texture2D(2, 2);
            source.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
            Rect r = sprite.rect;
            int x0 = (int)r.x, y0 = (int)r.y, w = (int)r.width, h = (int)r.height;
            bool Opaque(int x, int y) => source.GetPixel(x0 + x, y0 + y).a > 0.5f;
            int bottom = -1;
            for (int y = 0; y < h && bottom < 0; y++)
                for (int x = 0; x < w; x++)
                    if (Opaque(x, y))
                    {
                        bottom = y;
                        break;
                    }
            if (bottom < 0) throw new InvalidOperationException($"Kariaston: {sprite.name} has no opaque pixels.");
            int top = Mathf.Min(h - 1, bottom + depthPixels - 1), left = w, right = -1;
            for (int y = bottom; y <= top; y++)
                for (int x = 0; x < w; x++)
                    if (Opaque(x, y))
                    {
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                    }
            Object.DestroyImmediate(source);
            float ppu = sprite.pixelsPerUnit;
            Vector2 pivot = sprite.pivot;
            return new DressingCollision.Solid
            {
                sprite = sprite,
                offset = new Vector2(((left + right + 1) / 2f - pivot.x) / ppu, (bottom + (top - bottom + 1) / 2f - pivot.y) / ppu),
                size = new Vector2((right - left + 1) / ppu, (top - bottom + 1) / ppu),
            };
        }

        /// <summary>A solid footprint, in village cells, under <paramref name="parent"/> (relative to the village's origin).</summary>
        static void Block(Transform parent, string name, Rect cells) => LookTestBuilder.Solid(parent, name, cells.center - (Vector2)parent.position + Origin, cells.size);

        static void BuildMarket(Transform gameplay)
        {
            var stall = new GameObject("Market Stall");
            stall.transform.SetParent(gameplay, false);
            stall.transform.localPosition = Market;
            SpriteRenderer open = Put(stall.transform, "Open", Art(KariastonSheets.MerchantPack, KariastonSheets.CartOpen, "Cart"), Vector2.zero);
            SpriteRenderer closed = Put(stall.transform, "Closed", Art(KariastonSheets.MerchantPack, KariastonSheets.CartClosed, "Cart"), Vector2.zero);
            closed.gameObject.SetActive(false);
            AddShadow(open, ShadowOf("Cart", KariastonSheets.CartOpenShadow));
            AddShadow(closed, ShadowOf("Cart", KariastonSheets.CartClosedShadow));
            // Deep enough that nobody stands where the canopy (6¼ tiles of art) hides them whole (the owner's Checkpoint A playtest).
            Block(stall.transform, "Body", new Rect(Market.x - 2.6f, Market.y + 0.4f, 5.2f, 4.6f));
            var use = new GameObject("Use");
            use.transform.SetParent(stall.transform, false);
            var interactable = use.AddComponent<TavernInteractable>();
            interactable.Configure(TavernInteractableKind.MarketStall, SurfaceLocKeys.Market, new Vector2(0f, -0.3f), 1.4f, null);
            stall.AddComponent<MarketStall>().Configure(interactable, open.gameObject, closed.gameObject, SurfaceLocKeys.MarketClosed);
        }

        /// <summary>Where Musashi stands (village cells): his schedule's place, at the market cart's front-left corner.</summary>
        public static Vector2 MusashiSpot => VillageContent.KariastonAnchors.First(a => a.id == VillageContent.MarketCart).at;

        /// <summary>
        /// Musashi (2026-10-07, the owner's canon), who keeps the market cart: a standing villager in A Myriad of NPCs' layers (an
        /// elf, a black ponytail, a white shirt, dark trousers), talkable in the daytime. His spot is fixed until Checkpoint C's schedules.
        /// </summary>
        static void BuildMusashi(Transform gameplay, NavGrid grid)
        {
            const string npc = EditorPaths.Animations + "/Npc";
            SpriteAnimationSet Set(string name) => AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>($"{npc}/{name}.asset")
                ?? throw new InvalidOperationException($"Kariaston: no NPC layer '{name}' (run the NPC content).");
            var root = new GameObject("Musashi");
            root.transform.SetParent(gameplay, false);
            root.transform.localPosition = MusashiSpot;
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            // One figure, sorted by his feet like every other villager (without it his layers' orders, 1–5, drew him over the keeper
            // standing in front of him: the owner's 4i-B note).
            model.AddComponent<UnityEngine.Rendering.SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            SpriteRenderer Layer(string name, int order)
            {
                SpriteRenderer r = LookTestContent.AddSprite(model.transform, name, null, SortingLayers.YSorted, order, Vector3.zero);
                r.spriteSortPoint = SpriteSortPoint.Pivot;
                return r;
            }
            SpriteRenderer shadow = Layer("Shadow", 0);
            // Back to front, as the patrons: body, trousers, top, beard (none), hair.
            SpriteRenderer[] layers = { Layer("Body", 1), Layer("Trousers", 2), Layer("Top", 3), Layer("Beard", 4), Layer("Head", 5) };
            var look = model.AddComponent<LayeredSpriteAnimator>();
            look.Configure(layers, shadow, Set("NpcShadow"));
            look.SetAppearance(new[] { Set("Body_Elf_elfskin"), Set("Trousers_Trousers_black"), Set("Top_Shirt_white"), null, Set("Hair_PonyTail_black") });
            LookTestBuilder.Solid(root.transform, "Feet", new Vector2(0f, 0.15f), new Vector2(0.6f, 0.3f));
            var talk = new GameObject("Talk");
            talk.transform.SetParent(root.transform, false);
            var interactable = talk.AddComponent<TavernInteractable>();
            interactable.Configure(TavernInteractableKind.Person, null, new Vector2(0f, -0.8f), 1f, null, new[] { new Vector2(-0.9f, 0.1f), new Vector2(0f, 0.9f) });
            Villager villager = root.AddComponent<Villager>();
            villager.Configure(CharacterIds.Musashi, "villager.musashi", interactable);
            // 4h Checkpoint C: in the village's presence like everyone else (his schedule keeps him at the cart all day).
            villager.ConfigurePresence(SurfaceArea.KariastonId, model, look, root.transform.Find("Feet").GetComponent<Collider2D>(),
                VillageContent.Emote(root.transform, 2.1f), grid, VillageContent.Looks(CharacterIds.Musashi));
        }

        /// <summary>
        /// The village's own walking grid (4h Checkpoint C): baked from its buildings and props, held only by its villagers (it never
        /// becomes the floor's grid: the tavern's is that, loaded beside it).
        /// </summary>
        static NavGrid BuildGrid(Transform gameplay)
        {
            var go = new GameObject("Village Grid");
            go.transform.SetParent(gameplay, false);
            NavGrid grid = go.AddComponent<NavGrid>();
            grid.Configure(new RectInt((int)Origin.x, (int)Origin.y, Width, Height), LayerMask.GetMask(Layers.Obstacles), global: false);
            return grid;
        }

        /// <summary>
        /// Kariaston's people (4h Checkpoint C): the named places their schedules use, a copy of each villager who lives out here,
        /// the window glow at Ogrin's, the presence that places them all, and Kaloren's herbs changing hands.
        /// </summary>
        static void BuildPeople(Transform gameplay, NavGrid grid)
        {
            Transform people = Child(gameplay, "People");
            Transform anchors = Child(people, "Anchors");
            // Ogrin's window: a small warm glow while he's in bed behind it.
            var glow = new GameObject("Ogrin's Window Glow");
            glow.transform.SetParent(people, false);
            glow.transform.localPosition = Cottage + new Vector2(2f, 1.1f);   // the cottage's right ground-floor window
            Light2D light = glow.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.78f, 0.45f);
            light.intensity = 1.1f;
            light.pointLightOuterRadius = 1.3f;
            light.pointLightInnerRadius = 0.2f;
            glow.SetActive(false);
            foreach (var (id, at, facing, window) in VillageContent.KariastonAnchors)
                VillageContent.Anchor(anchors, id, SurfaceArea.KariastonId, at, facing, window, window ? glow : null);

            Dictionary<string, VillageContent.Figure> figures = VillageContent.Figures();
            foreach (var (id, name, start) in new[]
            {
                (CharacterIds.Maximo, "Maximo", VillageContent.MemorialSquare), (CharacterIds.Kaloren, "Kaloren", VillageContent.KalorenTower),
                (CharacterIds.Grim, "Grim", VillageContent.GrimYard), (CharacterIds.Ogrin, "Ogrin", VillageContent.OgrinYard),
                (CharacterIds.Bart, "Bart", VillageContent.BartWagon),
            })
            {
                Vector2 at = VillageContent.KariastonAnchors.First(a => a.id == start).at;
                VillageContent.BuildVillager(people, name, id, $"villager.{id}", SurfaceArea.KariastonId, figures[id], grid, at);
            }

            var presence = new GameObject("Village Presence");
            presence.transform.SetParent(people, false);
            presence.AddComponent<VillagePresence>();
            Sprite herbs = AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset")?.Ingredient("herbs")?.icon;
            presence.AddComponent<HerbVisit>().Configure(herbs, MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Heart"));
            // 4h Checkpoint D: the village talking among itself, and a light at Ogrin's window on some evenings.
            presence.AddComponent<AmbientMoments>().Configure(SurfaceArea.KariastonId, VillageContent.KariastonMoments());
            BuildWindowLight(people);
        }

        /// <summary>
        /// A small pale mote at Ogrin's window on some evenings (4h Checkpoint D, foreshadowing only): the Naughty Fairy's flight,
        /// drawn unlit and pale so it reads as a light, with a faint glow. Nothing names it.
        /// </summary>
        static void BuildWindowLight(Transform people)
        {
            var root = new GameObject("Ogrin's Window Light");
            root.transform.SetParent(people, false);
            root.transform.localPosition = Cottage + new Vector2(2f, 1.4f);
            var moteGo = new GameObject("Mote");
            moteGo.transform.SetParent(root.transform, false);
            SpriteRenderer mote = moteGo.AddComponent<SpriteRenderer>();
            mote.sortingLayerName = SortingLayers.Above;
            mote.sortingOrder = 4;
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (unlit != null) mote.sharedMaterial = unlit;
            mote.enabled = false;
            // The back-facing flight (its pale blue wings), six frames.
            Sprite[] frames = MinifantasyImporter.Row(KariastonSheets.FairyPack, "FairyFly", 2, 6);
            Light2D glow = root.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = new Color(0.7f, 0.85f, 1f);
            glow.intensity = 0f;
            glow.pointLightOuterRadius = 1.1f;
            glow.pointLightInnerRadius = 0.1f;
            glow.enabled = false;
            root.AddComponent<WindowLight>().Configure(mote, frames, glow);
        }

        /// <summary>The garden's four beds (4h Checkpoint B), on the soil painted in the blockout: ids in reading order.</summary>
        public static readonly string[] GardenBedIds = { GardenConfig.Bed1, GardenConfig.Bed2, GardenConfig.Bed3, GardenConfig.Bed4 };

        /// <summary>
        /// Each bed's gameplay (4h Checkpoint B): one plant per tile (the crop's stage), the interaction (from any side), Farm's
        /// action icon over it, and the soil it darkens when tended.
        /// </summary>
        static void BuildGarden(Transform root, Transform gameplay)
        {
            Tilemap soil = root.Find("Ground/Garden Beds")?.GetComponent<Tilemap>();
            Sprite[] Frames(string name) => Enumerable.Range(0, 4).Select(f => Art(KariastonSheets.FarmPack, KariastonSheets.FarmActions, $"{name}_{f}")).ToArray();
            Sprite[] plant = Frames("Seed"), tend = Frames("Water"), harvest = Frames("Pull");
            var garden = new GameObject("Garden").transform;
            garden.SetParent(gameplay, false);
            for (int i = 0; i < GardenBeds.Length; i++)
            {
                RectInt cells = GardenBeds[i];
                var bed = new GameObject($"Bed {GardenBedIds[i]}");
                bed.transform.SetParent(garden, false);
                bed.transform.localPosition = cells.center;
                var plants = new List<SpriteRenderer>();
                for (int x = cells.xMin; x < cells.xMax; x++)
                for (int y = cells.yMin; y < cells.yMax; y++)
                {
                    var go = new GameObject($"Plant {x - cells.xMin},{y - cells.yMin}");
                    go.transform.SetParent(bed.transform, false);
                    go.transform.localPosition = new Vector2(x + 0.5f, y + 0.15f) - cells.center;
                    SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                    r.sortingLayerName = SortingLayers.YSorted;
                    r.spriteSortPoint = SpriteSortPoint.Pivot;
                    if (LookTestContent.LitSpriteMaterial != null) r.sharedMaterial = LookTestContent.LitSpriteMaterial;
                    r.enabled = false;
                    plants.Add(r);
                }
                var actionGo = new GameObject("Action");
                actionGo.transform.SetParent(bed.transform, false);
                actionGo.transform.localPosition = new Vector2(0f, cells.height / 2f + 0.4f);
                SpriteRenderer action = actionGo.AddComponent<SpriteRenderer>();
                action.sortingLayerName = SortingLayers.Above;
                action.sortingOrder = 10;
                if (LookTestContent.LitSpriteMaterial != null) action.sharedMaterial = LookTestContent.LitSpriteMaterial;
                action.enabled = false;
                var use = new GameObject("Use");
                use.transform.SetParent(bed.transform, false);
                var interactable = use.AddComponent<TavernInteractable>();
                // Reached from any side: below the plants, beside them, and above the soil's back row.
                float w = cells.width / 2f + 0.45f, h = cells.height / 2f + 0.45f;
                interactable.Configure(TavernInteractableKind.GardenBed, GardenText.Plant, new Vector2(0f, -h), 1.2f, null,
                    new[] { new Vector2(0f, h + 1f), new Vector2(-w, 0f), new Vector2(w, 0f) });
                // The whole soil (plants' cells and the row behind them) darkens when tended.
                bed.AddComponent<GardenBed>().Configure(GardenBedIds[i], interactable, plants.ToArray(), soil, GardenSoil(i), action, plant, tend, harvest);
            }
        }

        static void BuildMemorialLook(Transform gameplay)
        {
            var look = new GameObject("Memorial Look");
            look.transform.SetParent(gameplay, false);
            look.transform.localPosition = Memorial;
            var interactable = look.AddComponent<TavernInteractable>();
            interactable.Configure(TavernInteractableKind.Inspect, SurfaceLocKeys.LookMemorial, new Vector2(0f, -1.6f), 1.1f, null);   // in front of the plaque
            look.AddComponent<DaytimeFixture>().Configure(SurfaceConversations.Memorial);
        }

        /// <summary>
        /// The village's edges: nobody walks off the map (one tile at every border; the trees' own footprints close the rest). Where
        /// the roads leave, the keeper stops at the edge and the camera never shows past it.
        /// </summary>
        static void BuildEdges(Transform gameplay)
        {
            Transform edges = Child(gameplay, "Edges");
            LookTestBuilder.Solid(edges, "West", new Vector2(0.5f, Height / 2f), new Vector2(1f, Height));
            LookTestBuilder.Solid(edges, "East", new Vector2(Width - 0.5f, Height / 2f), new Vector2(1f, Height));
            LookTestBuilder.Solid(edges, "South", new Vector2(Width / 2f, 0.5f), new Vector2(Width, 1f));
            LookTestBuilder.Solid(edges, "North", new Vector2(Width / 2f, Height - 0.5f), new Vector2(Width, 1f));
        }
    }
}
