using System.IO;
using Hearthdelve.Core;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the TavernGreybox scene: a one-screen side view with the door on the left, three
    /// tables (six seats), and the Tap, pass and Grill on the right. The floor is a solid tilemap
    /// on the Ground layer so dungeon-style combat could be added here later (stronghold defense).
    /// </summary>
    public static class TavernSceneBuilder
    {
        public const string ScenePath = EditorPaths.Scenes + "/TavernGreybox.unity";
        public const string CustomerPrefabPath = EditorPaths.Prefabs + "/Tavern/CustomerAgent.prefab";

        static readonly float[] k_TableX = { 4.5f, 8f, 11.5f };
        const float k_StoolOffset = 0.7f;
        const float k_TapX = 14.5f, k_PassX = 16.2f, k_GrillX = 18.2f, k_DoorX = 0.8f, k_QueueFrontX = 2.6f;

        public static CustomerAgent BuildCustomerPrefab()
        {
            EditorPaths.Ensure(EditorPaths.Prefabs + "/Tavern");
            var go = new GameObject("CustomerAgent");
            var body = SceneKit.AddSprite(go.transform, "Body", Sprite(TavernArtGenerator.Villager), SortingLayers.Enemies, 0, Color.white, Vector3.zero);
            var back = SceneKit.AddSprite(go.transform, "PatienceBack", Sprite(TavernArtGenerator.Bar), SortingLayers.FX, 0, new Color(0f, 0f, 0f, 0.7f), new Vector3(0f, 1.6f, 0f));
            back.transform.localScale = new Vector3(6.5f, 1.2f, 1f);
            var fill = SceneKit.AddSprite(go.transform, "PatienceFill", Sprite(TavernArtGenerator.Bar), SortingLayers.FX, 1, Color.green, new Vector3(0f, 1.6f, 0f));
            fill.transform.localScale = new Vector3(6f, 0.7f, 1f);
            var order = SceneKit.AddSprite(go.transform, "OrderIcon", Sprite(TavernArtGenerator.Plate), SortingLayers.FX, 0, Color.white, new Vector3(0f, 1.85f, 0f));
            var upset = SceneKit.AddSprite(go.transform, "UpsetIcon", Sprite(PlaceholderArtGenerator.ExclaimHeavy), SortingLayers.FX, 0, Color.white, new Vector3(0f, 1.9f, 0f));
            foreach (var sr in new[] { back, fill, order, upset }) sr.enabled = false;

            go.AddComponent<CustomerAgent>().Configure(body, back, fill, order, upset);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CustomerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<CustomerAgent>();
        }

        /// <param name="overwriteApproved">Must be true to replace an existing scene (CLAUDE.md).</param>
        public static void Build(TavernContent content, CustomerAgent customerPrefab, InputActionAsset actions, bool overwriteApproved)
        {
            if (File.Exists(ScenePath) && !overwriteApproved)
            {
                Debug.Log("[Hearthdelve] TavernGreybox exists and overwrite wasn't approved; leaving it untouched.");
                return;
            }
            EditorPaths.Ensure(EditorPaths.Scenes);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SceneKit.CreatePixelCamera(new Vector3(10f, 4.1f, -10f), new Color(0.12f, 0.08f, 0.06f), withCinemachineBrain: false);
            SceneKit.CreateGlobalLight();
            BuildRoom();
            var layout = BuildLayout();

            var player = BuildPlayer();
            var staff = BuildStaff();

            var systems = new GameObject("Systems");
            systems.AddComponent<TavernDirector>().Configure(content, layout, customerPrefab, player, staff);
            systems.AddComponent<TavernDebugOverlay>();

            var panel = SceneKit.PanelSettings();
            var ui = new GameObject("UI");
            SceneKit.AddDocument<TavernHud>(ui.transform, "Service HUD", panel, "TavernHud.uxml", 0);
            SceneKit.AddDocument<StationMinigamePanel>(ui.transform, "Minigame Panel", panel, "TavernMinigame.uxml", 5);
            SceneKit.AddDocument<TavernPrepScreen>(ui.transform, "Prep Screen", panel, "TavernPrep.uxml", 10);
            SceneKit.AddDocument<TavernResultsScreen>(ui.transform, "Results Screen", panel, "TavernResults.uxml", 20);
            SceneKit.CreateEventSystem(actions);

            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneKit.AddToBuild(ScenePath);
        }

        static Sprite Sprite(string name) => PlaceholderArtGenerator.Load(name);

        static void BuildRoom()
        {
            var floorTile = ContentGenerator.LoadOrCreate<Tile>($"{EditorPaths.Tiles}/Tile_TavernFloor.asset", t => t.colliderType = Tile.ColliderType.Grid);
            floorTile.sprite = Sprite(TavernArtGenerator.FloorTile);
            var wallTile = ContentGenerator.LoadOrCreate<Tile>($"{EditorPaths.Tiles}/Tile_TavernWall.asset", t => t.colliderType = Tile.ColliderType.None);
            wallTile.sprite = Sprite(TavernArtGenerator.WallTile);
            EditorUtility.SetDirty(floorTile);
            EditorUtility.SetDirty(wallTile);

            var grid = new GameObject("Room Grid", typeof(Grid));
            var floorGo = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)) { layer = LayerMask.NameToLayer(Layers.Ground) };
            floorGo.transform.SetParent(grid.transform, false);
            floorGo.GetComponent<TilemapRenderer>().sortingLayerName = SortingLayers.Level;
            floorGo.AddComponent<TilemapCollider2D>(); // solid floor: keeps the tavern combat-capable
            var wallGo = new GameObject("Back Wall", typeof(Tilemap), typeof(TilemapRenderer));
            wallGo.transform.SetParent(grid.transform, false);
            wallGo.GetComponent<TilemapRenderer>().sortingLayerName = SortingLayers.Background;

            var floor = floorGo.GetComponent<Tilemap>();
            var wall = wallGo.GetComponent<Tilemap>();
            for (int x = -2; x <= 21; x++)
            {
                for (int y = -3; y <= -1; y++) floor.SetTile(new Vector3Int(x, y, 0), floorTile);
                for (int y = 0; y <= 10; y++) wall.SetTile(new Vector3Int(x, y, 0), wallTile);
            }
        }

        static TavernLayout BuildLayout()
        {
            var root = new GameObject("Layout");
            var door = Prop(root.transform, "Door", TavernArtGenerator.Door, k_DoorX, SortingLayers.Level, 2);

            var seats = new Transform[k_TableX.Length * 2];
            for (int i = 0; i < k_TableX.Length; i++)
            {
                Prop(root.transform, $"Table {i + 1}", TavernArtGenerator.Table, k_TableX[i], SortingLayers.Level, 2);
                for (int side = 0; side < 2; side++)
                {
                    float x = k_TableX[i] + (side == 0 ? -k_StoolOffset : k_StoolOffset);
                    Prop(root.transform, $"Stool {i * 2 + side + 1}", TavernArtGenerator.Stool, x, SortingLayers.Level, 1);
                    var seat = new GameObject($"Seat {i * 2 + side + 1}").transform;
                    seat.SetParent(root.transform, false);
                    seat.position = new Vector3(x, 0f, 0f);
                    seats[i * 2 + side] = seat;
                }
            }

            var queue = new GameObject("Queue Front").transform;
            queue.SetParent(root.transform, false);
            queue.position = new Vector3(k_QueueFrontX, 0f, 0f);

            var tap = StationAt(root.transform, "Tap", TavernArtGenerator.Tap, k_TapX, StationKind.Tap);
            var pass = StationAt(root.transform, "Pass", TavernArtGenerator.Pass, k_PassX, StationKind.Pass);
            var grill = StationAt(root.transform, "Grill", TavernArtGenerator.Grill, k_GrillX, StationKind.Grill);

            var doorAnchor = new GameObject("Door Anchor").transform;
            doorAnchor.SetParent(root.transform, false);
            doorAnchor.position = new Vector3(door.transform.position.x, 0f, 0f);

            var layout = root.AddComponent<TavernLayout>();
            layout.Configure(doorAnchor, seats, queue, grill, tap, pass, minX: 0.5f, maxX: 19.5f);
            return layout;
        }

        /// <summary>Places a centre-pivot sprite so it stands on the floor (y = 0).</summary>
        static SpriteRenderer Prop(Transform parent, string name, string sprite, float x, string layer, int order)
        {
            var s = Sprite(sprite);
            float halfHeight = s != null ? s.bounds.extents.y : 0.5f;
            var sr = SceneKit.AddSprite(parent, name, s, layer, order, Color.white, new Vector3(x, halfHeight, 0f));
            return sr;
        }

        static Station StationAt(Transform parent, string name, string sprite, float x, StationKind kind)
        {
            var sr = Prop(parent, name, sprite, x, SortingLayers.Level, 3);
            var highlight = SceneKit.AddSprite(sr.transform, "Highlight", Sprite(PlaceholderArtGenerator.Marker), SortingLayers.FX, -1,
                new Color(1f, 0.9f, 0.4f, 1f), new Vector3(0f, -sr.bounds.extents.y + 0.05f, 0f));
            highlight.color = new Color(1f, 0.85f, 0.3f);
            highlight.enabled = false;
            var station = sr.gameObject.AddComponent<Station>();
            station.Configure(kind, highlight);
            return station;
        }

        static TavernPlayer BuildPlayer()
        {
            var go = new GameObject("Keeper");
            go.transform.position = new Vector3(15.4f, 0f, 0f);
            var body = SceneKit.AddSprite(go.transform, "Body", Sprite(PlaceholderArtGenerator.Player), SortingLayers.Player, 0,
                new Color(0.95f, 0.82f, 0.62f), Vector3.zero);
            var plate = SceneKit.AddSprite(go.transform, "Plate", Sprite(TavernArtGenerator.Plate), SortingLayers.FX, 0, Color.white, new Vector3(0.35f, 1.1f, 0f));
            plate.enabled = false;
            var player = go.AddComponent<TavernPlayer>();
            player.Configure(body, plate);
            return player;
        }

        static StaffAgent BuildStaff()
        {
            var go = new GameObject("Staff Helper");
            var body = SceneKit.AddSprite(go.transform, "Body", Sprite(TavernArtGenerator.Villager), SortingLayers.Player, -1, Color.white, Vector3.zero);
            var plate = SceneKit.AddSprite(go.transform, "Plate", Sprite(TavernArtGenerator.Plate), SortingLayers.FX, 0, Color.white, new Vector3(0.35f, 1.05f, 0f));
            var working = SceneKit.AddSprite(go.transform, "WorkingIcon", Sprite(TavernArtGenerator.Working), SortingLayers.FX, 0, Color.white, new Vector3(0f, 1.6f, 0f));
            plate.enabled = false;
            working.enabled = false;
            var staff = go.AddComponent<StaffAgent>();
            staff.Configure(body, plate, working);
            go.SetActive(false); // shown when assigned at prep
            return staff;
        }
    }
}
