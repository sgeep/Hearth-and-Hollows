using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4f steps 4–5: the catalogue's Renown tiers (D14), the palette ramps and presets for recolouring (D11) and the
    /// area-wide floor and wall finishes (D5). Ramps are colours Minifantasy already drew (the Dwarven tables' materials,
    /// the Elven sets' woods and rugs), so a recoloured chair still looks like Minifantasy. Finishes are cells of premade
    /// rooms (Tavern Indoor, Shop Indoor) and the Castles tileset, plus the tavern's panelling recoloured through the same
    /// ramps. Safe to run again: everything is created or updated in place.
    /// </summary>
    public static class FurnitureLooks
    {
        public const string Folder = EditorPaths.Data + "/Furniture";
        public const string PalettesPath = Folder + "/PaletteLibrary.asset";
        public const string CatalogPath = Folder + "/CatalogSettings.asset";
        public const string FinishFolder = Folder + "/Finishes";

        /// <summary>The tavern's wood as drawn (Tavern Indoor props): the channel its tables, chairs, benches and stools share.</summary>
        public static readonly string[] TavernWood = { "#380a0e", "#480b0b", "#5d2415", "#742b0d", "#762505", "#953712", "#a94117", "#b34c0c", "#d56825" };
        /// <summary>The red cushions of the tavern's chairs and stools.</summary>
        public static readonly string[] TavernCushion = { "#7f092f", "#9b0e27", "#c01515", "#e6513f" };
        /// <summary>The back wall's panelling as drawn.</summary>
        public static readonly string[] Panelling = { "#2a0208", "#350601", "#431a04", "#532207", "#742b0d" };

        static readonly (string id, string kind, string name, string[] colors)[] k_Ramps =
        {
            ("walnut", "wood", "walnut", new[] { "#2b3007", "#443c09", "#593a09", "#723908", "#8a530e" }),
            ("rosewood", "wood", "rosewood", new[] { "#371d0d", "#4e2a13", "#5e2f23", "#784032", "#9a6b53" }),
            ("olive", "wood", "olive wood", new[] { "#223209", "#414518", "#635524", "#776730", "#938040" }),
            ("ash", "wood", "ash", new[] { "#231e0c", "#332d14", "#4c3b25", "#654e42", "#816c57" }),
            ("birch", "wood", "birch", new[] { "#685d4b", "#81786a", "#988f80", "#aea496", "#c3bda8" }),
            ("pine", "wood", "pine", new[] { "#564d27", "#816d41", "#b58f5c", "#cca36d" }),
            ("slate", "wood", "slate black", new[] { "#1c1c1c", "#2c2c2c", "#464646", "#656565" }),
            ("stonewood", "wood", "grey stone", new[] { "#6e6e6e", "#878787", "#9a9a9a", "#aeaeae" }),
            ("teal", "cushion", "teal", new[] { "#1b5b7a", "#1b7873", "#2c917b", "#4fae8f" }),
            ("green", "cushion", "green", new[] { "#1a6740", "#3a781b", "#5e8f2c", "#7fa83c" }),
            ("plum", "cushion", "plum", new[] { "#411c70", "#5a2177", "#7c2487", "#9a3aa0" }),
            ("amber", "cushion", "amber", new[] { "#83390a", "#985512", "#b17515", "#cf9a2a" }),
            ("rose", "cushion", "rose", new[] { "#771244", "#8c233a", "#af3f3f", "#d06060" }),
            ("gold", "cushion", "gold", new[] { "#b37c0f", "#c88c14", "#cfa617", "#e6c41c" }),
            ("navy", "cushion", "navy", new[] { "#262846", "#333851", "#46515c", "#5a6a80" }),
        };

        static readonly (string id, string name, (string kind, string ramp)[] picks)[] k_Presets =
        {
            ("dwarven_hall", "dwarven hall", new[] { ("wood", "slate"), ("cushion", "gold") }),
            ("elven_glade", "elven glade", new[] { ("wood", "olive"), ("cushion", "green") }),
            ("castle_feast", "castle feast", new[] { ("wood", "walnut"), ("cushion", "rose") }),
            ("haunted_den", "haunted den", new[] { ("wood", "ash"), ("cushion", "plum") }),
            ("harbour", "harbour", new[] { ("wood", "birch"), ("cushion", "navy") }),
            ("hearth", "hearth", new[] { ("wood", "rosewood"), ("cushion", "amber") }),
        };

        static readonly (int renown, string name, string announce)[] k_Tiers =
        {
            (0, "the village joiner", null),
            (25, "the dwarven masons", "word is spreading: the dwarven masons will take your orders."),
            (60, "the elven and castle workshops", "word is spreading: the elven carvers and the castle's suppliers will take your orders."),
            (100, "the strange and the grand", "word is spreading: dealers in strange and grand things will call on the Sunken Flagon."),
        };

        public static string RampKey(string id) => $"palette.{id}";
        public static string PresetKey(string id) => $"palette.preset.{id}";
        public static string TierKey(int tier) => $"catalog.tier.{tier}";
        public static string TierAnnounceKey(int tier) => $"catalog.tier.{tier}.announce";
        public static string FinishKey(string id) => $"finish.{id}";

        /// <summary>The finishes: (id, name, kind, theme, tier, price, starter).</summary>
        static readonly (string id, string name, FinishKind kind, FurnitureTheme theme, int tier, int price, bool starter)[] k_Finishes =
        {
            ("floor_diamonds", "green diamond tiles", FinishKind.Floor, FurnitureTheme.Tavern, 0, 0, true),
            ("floor_teal", "teal tiles", FinishKind.Floor, FurnitureTheme.Village, 0, 0, true),
            ("floor_plum", "plum tiles", FinishKind.Floor, FurnitureTheme.Village, 0, 40, false),
            ("floor_chequer", "pale chequer", FinishKind.Floor, FurnitureTheme.Village, 1, 60, false),
            ("floor_parquet", "parquet", FinishKind.Floor, FurnitureTheme.Castle, 1, 70, false),
            ("floor_blue_tiles", "blue castle tiles", FinishKind.Floor, FurnitureTheme.Castle, 2, 90, false),
            ("floor_sage_tiles", "sage castle tiles", FinishKind.Floor, FurnitureTheme.Castle, 2, 90, false),
            ("wall_panelling", "dark panelling", FinishKind.Wall, FurnitureTheme.Tavern, 0, 0, true),
            ("wall_cream", "cream plaster", FinishKind.Wall, FurnitureTheme.Village, 0, 0, true),
            ("wall_green", "green plaster", FinishKind.Wall, FurnitureTheme.Village, 0, 40, false),
            ("wall_panelling_walnut", "walnut panelling", FinishKind.Wall, FurnitureTheme.Tavern, 0, 50, false),
            ("wall_panelling_birch", "birch panelling", FinishKind.Wall, FurnitureTheme.Elven, 2, 70, false),
            ("wall_panelling_ash", "ash panelling", FinishKind.Wall, FurnitureTheme.Haunted, 3, 70, false),
        };

        /// <summary>The English of ramps, presets, tiers and finishes (UI table).</summary>
        public static IEnumerable<(string key, string english)> English()
        {
            foreach (var r in k_Ramps) yield return (RampKey(r.id), r.name);
            foreach (var p in k_Presets) yield return (PresetKey(p.id), p.name);
            for (int i = 0; i < k_Tiers.Length; i++)
            {
                yield return (TierKey(i), k_Tiers[i].name);
                if (k_Tiers[i].announce != null) yield return (TierAnnounceKey(i), k_Tiers[i].announce);
            }
            foreach (var f in k_Finishes) yield return (FinishKey(f.id), f.name);
        }

        public static IEnumerable<string> StarterFinishes => k_Finishes.Where(f => f.starter).Select(f => f.id);

        public sealed class Built
        {
            public PaletteLibrary Palettes;
            public CatalogSettings Catalog;
            public readonly List<FinishDefinition> Finishes = new();
        }

        public static Built Build(GameDatabase database)
        {
            EditorPaths.Ensure(Folder);
            EditorPaths.Ensure(FinishFolder);
            var built = new Built
            {
                Palettes = LookTestContent.CreateOrUpdate<PaletteLibrary>(PalettesPath, lib =>
                {
                    lib.ramps = k_Ramps.Select(r => new PaletteRamp { id = r.id, kind = r.kind, nameKey = RampKey(r.id), colors = r.colors.Select(Hex).ToArray() }).ToList();
                    lib.presets = k_Presets.Select(p => new PalettePreset
                    {
                        id = p.id, nameKey = PresetKey(p.id), picks = p.picks.Select(x => new PalettePick { kind = x.kind, ramp = x.ramp }).ToList(),
                    }).ToList();
                }),
                Catalog = LookTestContent.CreateOrUpdate<CatalogSettings>(CatalogPath, c =>
                {
                    c.tiers = k_Tiers.Select((t, i) => new CatalogTier { renown = t.renown, nameKey = TierKey(i), announceKey = t.announce != null ? TierAnnounceKey(i) : null }).ToList();
                }),
            };
            foreach (var f in k_Finishes) built.Finishes.Add(Finish(f.id, f.kind, f.theme, f.tier, f.price, f.starter));
            if (database != null)
            {
                database.palettes = built.Palettes;
                database.catalog = built.Catalog;
                database.finishes = built.Finishes;
                EditorUtility.SetDirty(database);
            }
            AssetDatabase.SaveAssets();
            return built;
        }

        public static Color32 Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;

        // ------------------------------------------------------------------ finishes

        static Tile CellTile(string name, Sprite sprite) =>
            LookTestContent.CreateOrUpdate<Tile>($"{EditorPaths.Tiles}/{name}.asset", tile =>
            {
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
            });

        /// <summary>A Tavern Indoor room cell (the tiles the tavern was built from, so its own finishes are exactly as built).</summary>
        static Tile TavernCell(string layer, int column, int row) =>
            CellTile($"Tavern_{layer}_{column}_{row}", MinifantasyImporter.Sprite(MinifantasySheets.TavernIndoor, $"TavernIndoor_{layer}", $"Cell_{column}_{row}"));

        static Tile ShopCell(string layer, string room, int column, int row) =>
            CellTile($"Shop_{layer}_{room}_{column}_{row}", MinifantasyImporter.Sprite(MinifantasySheets.ShopIndoor, $"ShopIndoor_{layer}", $"{room}_{column}_{row}"));

        static Tile CastleCell(string name) => CellTile($"Castle_{name}", MinifantasyImporter.Sprite(MinifantasySheets.CastlesAndStrongholds, "CastleIndoorTileset", name));

        static FinishDefinition Finish(string id, FinishKind kind, FurnitureTheme theme, int tier, int price, bool starter) =>
            LookTestContent.CreateOrUpdate<FinishDefinition>($"{FinishFolder}/Finish_{id}.asset", f =>
            {
                f.id = id;
                f.nameKey = FinishKey(id);
                f.kind = kind;
                f.theme = theme;
                f.catalogTier = tier;
                f.price = price;
                f.sources = starter ? FurnitureSource.Starter : FurnitureSource.Bought;
                f.channels = new List<PaletteChannel>();
                f.palette = null;
                f.pattern = kind == FinishKind.Wall ? new Vector2Int(1, 3) : Vector2Int.one;
                f.leftEdge = new List<TileBase>();
                f.rightEdge = new List<TileBase>();
                switch (id)
                {
                    case "floor_diamonds": Floor(f, TavernCell("floor2", 2, 5), TavernCell("floor2", 0, 5), TavernCell("floor2", 10, 5)); break;
                    case "floor_chequer": Floor(f, TavernCell("floor", 2, 5), TavernCell("floor", 0, 5), TavernCell("floor", 10, 5)); break;
                    case "floor_plum": Floor(f, ShopCell("floor", "L", 2, 5), ShopCell("floor", "L", 0, 5), ShopCell("floor", "L", 16, 5)); break;
                    case "floor_teal": Floor(f, ShopCell("floor", "R", 2, 5), ShopCell("floor", "R", 0, 5), ShopCell("floor", "R", 16, 5)); break;
                    case "floor_parquet": Floor(f, CastleCell("Parquet"), null, null); break;
                    case "floor_blue_tiles": Floor(f, CastleCell("BlueTiles"), null, null); break;
                    case "floor_sage_tiles": Floor(f, CastleCell("SageTiles"), null, null); break;
                    case "wall_cream": Wall(f, (c, r) => ShopCell("wall", "R", c, r), 16); break;
                    case "wall_green": Wall(f, (c, r) => ShopCell("wall", "L", c, r), 16); break;
                    default:
                        // The panelling, as built or recoloured through a wood ramp.
                        Wall(f, (c, r) => TavernCell("wall", c, r), 10);
                        string wood = id.StartsWith("wall_panelling_") ? id.Substring("wall_panelling_".Length) : null;
                        if (wood != null)
                        {
                            f.channels.Add(new PaletteChannel { kind = "wood", source = Panelling.Select(Hex).ToArray() });
                            f.palette = FurniturePalette.With(null, "wood", wood);
                        }
                        break;
                }
                f.swatch = (f.tiles.FirstOrDefault() as Tile)?.sprite;
            });

        static void Floor(FinishDefinition f, Tile middle, Tile left, Tile right)
        {
            f.tiles = new List<TileBase> { middle };
            if (left != null) f.leftEdge.Add(left);
            if (right != null) f.rightEdge.Add(right);
        }

        /// <summary>Three rows, bottom up: the room's wall rows 2, 1, 0 (row 0 is the top).</summary>
        static void Wall(FinishDefinition f, System.Func<int, int, Tile> cell, int lastColumn)
        {
            f.tiles = new List<TileBase> { cell(2, 2), cell(2, 1), cell(2, 0) };
            f.leftEdge.AddRange(new TileBase[] { cell(0, 2), cell(0, 1), cell(0, 0) });
            f.rightEdge.AddRange(new TileBase[] { cell(lastColumn, 2), cell(lastColumn, 1), cell(lastColumn, 0) });
        }
    }
}
