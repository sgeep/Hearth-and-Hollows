using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The guest room (4f step 6; plan §8): a second area of the property, proving one decorating system for every room.
    /// A one-screen room (18×13 cells; 16×8 of floor) cut from the Shop Indoor add-on's cream-walled premade room (a different shell from
    /// the tavern's), placed beside the tavern in the Tavern scene, off camera. Decorate Mode switches to it (there's no
    /// staircase in the tavern yet); its door leads back down with a fade. It has its own layout, finishes and
    /// validation profile (only its doorway is checked), and the same catalogue, storage and save as the tavern. No
    /// guests, prices or ratings. Built in place in the existing scene (D23): every run replaces what it made and nothing
    /// else, so running it again changes nothing.
    /// </summary>
    public static class GuestRoomBuilder
    {
        public static readonly Vector2 Origin = new(50f, 0f);
        public const int Width = 18, Height = 13, DoorColumn = 9;
        /// <summary>Walkable floor rows: from the front wall's top to the back wall's foot.</summary>
        public const int FloorBottom = 2, FloorTop = 10;

        static int SourceColumn(int column, bool doorRow) =>
            column == 0 ? 0 : column == Width - 1 ? 16 : doorRow && column == DoorColumn ? MinifantasySheets.ShopDoorColumn : 2;

        static int SourceRow(int row) => row <= 2 ? row : row == Height - 2 ? 13 : row == Height - 1 ? 14 : 5;

        static Tile ShopTile(string layer, int column, int row) =>
            LookTestContent.CreateOrUpdate<Tile>($"{EditorPaths.Tiles}/Shop_{layer}_R_{column}_{row}.asset", tile =>
            {
                tile.sprite = MinifantasyImporter.Sprite(MinifantasySheets.ShopIndoor, $"ShopIndoor_{layer}", $"R_{column}_{row}");
                tile.colliderType = Tile.ColliderType.None;
            });

        /// <summary>The tavern's finishes component (over the room the 4c builder made) and its view and name.</summary>
        public static AreaFinishes TavernFinishes(Transform tavernArea)
        {
            Grid room = Object.FindObjectsByType<Grid>(FindObjectsInactive.Include).FirstOrDefault(g => g.transform.parent == null && g.name == "Room");
            AreaFinishes finishes = tavernArea.GetComponent<AreaFinishes>() ?? tavernArea.gameObject.AddComponent<AreaFinishes>();
            if (room != null)
                finishes.Configure(room.transform.Find("Floor").GetComponent<Tilemap>(), new RectInt(0, (int)TavernBuilder.FloorBottom, TavernBuilder.Width,
                    (int)(TavernBuilder.FloorTop - TavernBuilder.FloorBottom)), room.transform.Find("Wall").GetComponent<Tilemap>(),
                    new RectInt(0, (int)TavernBuilder.FloorTop, TavernBuilder.Width, TavernBuilder.Height - (int)TavernBuilder.FloorTop));
            return finishes;
        }

        /// <summary>Where you arrive in the tavern coming down: in front of the back wall, between the bar and the sign.</summary>
        public static readonly Vector2 TavernArrival = new(12.5f, 11.2f);

        public static void Build(PropertyArea tavern, FurnitureContent.Built furniture, TavernContent content)
        {
            Transform areas = tavern.transform.parent;
            Transform root = areas.Find("Guest Room");
            if (root == null)
            {
                root = new GameObject("Guest Room").transform;
                root.SetParent(areas, false);
            }
            root.position = Origin;
            foreach (string made in new[] { "Room", "Walls", "Door", "Placed Furniture" })
                for (Transform old = root.Find(made); old != null; old = root.Find(made))
                    Object.DestroyImmediate(old.gameObject);

            // The shell: the premade room stretched by cell, as the tavern's is.
            var grid = new GameObject("Room").AddComponent<Grid>();
            grid.transform.SetParent(root, false);
            Tilemap floor = LookTestBuilder.Layer(grid, "Floor", SortingLayers.Floor, 0, solid: false);
            Tilemap shell = LookTestBuilder.Layer(grid, "Shell", SortingLayers.Floor, 1, solid: false);
            Tilemap wall = LookTestBuilder.Layer(grid, "Wall", SortingLayers.Floor, 2, solid: false);
            for (int row = 0; row < Height; row++)
            for (int column = 0; column < Width; column++)
            {
                var cell = new Vector3Int(column, Height - 1 - row, 0);
                bool backWall = row <= 2, frontWall = row >= Height - 2, side = column == 0 || column == Width - 1;
                int sc = SourceColumn(column, frontWall), sr = SourceRow(row);
                if (backWall) wall.SetTile(cell, ShopTile("wall", sc, sr));
                if (!backWall && !frontWall) floor.SetTile(cell, ShopTile("floor", sc, 5));
                if (backWall || frontWall || side) shell.SetTile(cell, ShopTile("basebuilding", sc, sr));
            }

            var solids = new GameObject("Walls").transform;
            solids.SetParent(root, false);
            LookTestBuilder.Solid(solids, "West", new Vector2(0.25f, Height / 2f), new Vector2(0.5f, Height));
            LookTestBuilder.Solid(solids, "East", new Vector2(Width - 0.25f, Height / 2f), new Vector2(0.5f, Height));
            LookTestBuilder.Solid(solids, "North", new Vector2(Width / 2f, (FloorTop + Height) / 2f), new Vector2(Width, Height - FloorTop));
            LookTestBuilder.Solid(solids, "South", new Vector2(Width / 2f, FloorBottom / 2f), new Vector2(Width, FloorBottom));

            PropertyArea area = root.GetComponent<PropertyArea>() ?? root.gameObject.AddComponent<PropertyArea>();
            area.Configure(PropertyArea.GuestRoomId, AreaKind.GuestRoom, Origin, new RectInt(0, 0, Width, Height), new RectInt(1, FloorBottom, Width - 2, FloorTop - FloorBottom),
                new RectInt(1, FloorTop, Width - 2, Height - FloorTop), new[] { new Vector2Int(DoorColumn, FloorBottom), new Vector2Int(DoorColumn, FloorBottom + 1) });
            area.SetView(Origin + new Vector2(Width / 2f, Height / 2f), Origin + new Vector2(DoorColumn + 0.5f, FloorBottom + 1.4f), DecorateLocKeys.AreaGuestRoom);
            AreaFinishes finishes = root.GetComponent<AreaFinishes>() ?? root.gameObject.AddComponent<AreaFinishes>();
            finishes.Configure(floor, new RectInt(0, FloorBottom, Width, FloorTop - FloorBottom), wall, new RectInt(0, FloorTop, Width, Height - FloorTop));
            AreaFurniture builder = root.GetComponent<AreaFurniture>() ?? root.gameObject.AddComponent<AreaFurniture>();
            builder.Configure(area, furniture.Database, furniture.Presentation, content, finishes);

            // Down: the doorway (a step onto it fades back down to the tavern).
            var door = new GameObject("Door");
            door.transform.SetParent(root, false);
            door.transform.localPosition = new Vector2(DoorColumn + 0.5f, FloorBottom + 0.25f);
            var doorTrigger = door.AddComponent<BoxCollider2D>();
            doorTrigger.isTrigger = true;
            doorTrigger.size = new Vector2(0.9f, 0.5f);
            door.AddComponent<AreaPassage>().Configure(area, tavern);

            RemoveStairs(tavern);
        }

        /// <summary>
        /// No staircase in the tavern (removed at the owner's request after the Checkpoint B playtest): until the Inn gets a
        /// real way up, the guest room is reached from Decorate Mode. Clears the stairs an earlier update put in the scene.
        /// </summary>
        static void RemoveStairs(PropertyArea tavern)
        {
            Transform root = tavern.transform;
            for (Transform old = root.Find("Stairs"); old != null; old = root.Find("Stairs"))
                Object.DestroyImmediate(old.gameObject);
            tavern.SetFixtures();
        }

        /// <summary>The black cover for the fade between areas, over everything on the tavern's canvas.</summary>
        public static void BuildFade(Canvas canvas)
        {
            Transform old = canvas.transform.Find("AreaFade");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform cover = DungeonUI.FullScreen(canvas, "AreaFade");
            DungeonUI.AddImage(cover, DungeonUI.Pixel(), Color.black);
            cover.gameObject.AddComponent<CanvasGroup>();
            cover.gameObject.AddComponent<AreaFadeView>();
            cover.GetComponent<Image>().raycastTarget = false;
            cover.SetAsLastSibling();
        }
    }
}
