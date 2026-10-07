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
    /// the tavern's), placed beside the tavern in the Tavern scene, off camera. Slim stairs in the tavern's back-right corner lead
    /// up to it, and its door leads back down; both fade. Decorate Mode switches to it too. It has its own layout, finishes and
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

        /// <summary>The tavern cells the stairs stand on: the back-right corner, against the right wall.</summary>
        public static readonly RectInt TavernStairs = new(26, 12, 1, 2);

        /// <summary>The cell in front of the stairs' foot, kept clear so the way up can't be blocked.</summary>
        public static readonly Vector2Int TavernStairsFoot = new(26, 11);

        /// <summary>The inside face of the tavern's right wall.</summary>
        const float k_RightWallFace = 27.5f;

        /// <summary>Where you arrive in the tavern coming down: just below the stairs' foot.</summary>
        public static readonly Vector2 TavernArrival = new(26.9f, 10.6f);

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

            // Down: the doorway (a step onto it fades back down to the foot of the tavern's stairs).
            var door = new GameObject("Door");
            door.transform.SetParent(root, false);
            door.transform.localPosition = new Vector2(DoorColumn + 0.5f, FloorBottom + 0.25f);
            var doorTrigger = door.AddComponent<BoxCollider2D>();
            doorTrigger.isTrigger = true;
            doorTrigger.size = new Vector2(0.9f, 0.5f);
            door.AddComponent<AreaPassage>().Configure(area, tavern);

            AddStairs(tavern, area);
        }

        /// <summary>
        /// Up: a slim wooden flight in the tavern's back-right corner, against the right wall and rising into the back wall
        /// (it replaced the wide stucco staircase between the bar and the sign after the owner's first look). Walking onto its
        /// foot fades up to the guest room.
        /// </summary>
        static void AddStairs(PropertyArea tavern, PropertyArea guest)
        {
            Transform root = tavern.transform;
            for (Transform old = root.Find("Stairs"); old != null; old = root.Find("Stairs"))
                Object.DestroyImmediate(old.gameObject);
            var stairs = new GameObject("Stairs").transform;
            stairs.SetParent(root, false);
            stairs.position = new Vector2(TavernStairs.xMin, TavernStairs.yMin);
            var art = new GameObject("Art");
            art.transform.SetParent(stairs, false);
            // Drawn against a left wall: mirrored about its left edge, which sits on the right wall's face.
            art.transform.localPosition = new Vector2(k_RightWallFace - TavernStairs.xMin, 0f);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = MinifantasyImporter.Sprite(MinifantasySheets.MedievalCity, "InteriorStairs", "WoodenFlight");
            renderer.flipX = true;
            renderer.sortingLayerName = SortingLayers.Floor;
            renderer.sortingOrder = 3;
            if (LookTestContent.LitSpriteMaterial != null) renderer.sharedMaterial = LookTestContent.LitSpriteMaterial;
            // The flight is solid (and the layout check knows it); its foot, below it, is the way up.
            const float width = 9f / 8f;
            float left = k_RightWallFace - width - TavernStairs.xMin;
            LookTestBuilder.Solid(stairs, "Flight", new Vector2(left + width / 2f, 1f), new Vector2(width, 2f));
            tavern.SetFixtures(new Rect(TavernStairs.xMin, TavernStairs.yMin, 1f, 2f));
            var foot = new GameObject("Foot");
            foot.transform.SetParent(stairs, false);
            foot.transform.localPosition = new Vector2(left + width / 2f, -0.35f);
            var trigger = foot.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            // The whole foot tile, from the stew pot's side to the wall (4h, after the owner's Checkpoint A playtest: a keeper
            // walking up the gap along the stew pot slid past a trigger that covered only its right two-thirds).
            trigger.size = new Vector2(1.34f, 0.7f);
            trigger.offset = new Vector2(-0.22f, 0f);
            foot.AddComponent<AreaPassage>().Configure(tavern, guest);
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
