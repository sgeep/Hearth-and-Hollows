using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The furniture catalogue pipeline (4f step 3; plan §10): the source table <c>Data/Furniture/Catalog/catalog.json</c>
    /// (one entry per piece: its sheet, a rect per drawn facing, colourways as offsets or rects, layer, function, tier,
    /// price…) becomes a <see cref="FurnitureDefinition"/> per piece. Only the drawings the table names enter the repo: each
    /// source sheet is copied with everything else cleared (<c>ThirdParty/Minifantasy/Furniture/Furniture_&lt;sheet&gt;.png</c>),
    /// and sliced as <c>catalog_sprites.json</c> says (written here, read by the import pipeline, so a fresh clone slices the
    /// same way). Geometry follows simple rules (footprint from the drawing, art centred on it, bodies the drawing or its
    /// bottom rows, seats at the pivot facing the way the facing is drawn), with overrides in the table. Problems are
    /// reported, not thrown. Adding the hundredth chair is a table entry. Run it again: everything updates in place.
    /// Contact sheets for review: <c>python Tools/furniture/contact.py</c>.
    /// </summary>
    public static class FurnitureCatalog
    {
        public const string Folder = EditorPaths.Data + "/Furniture/Catalog";
        public const string JsonPath = Folder + "/catalog.json";
        public const string SpritesPath = Folder + "/catalog_sprites.json";
        public const string Pack = "Furniture";
        const int k_Ppt = FurnitureGeometry.PixelsPerTile;

        // ------------------------------------------------------------------ the table (JsonUtility shapes)

        [Serializable] public sealed class CatalogJson { public SheetJson[] sheets; public VariantSetJson[] variantSets; public PieceJson[] pieces; }
        [Serializable] public sealed class SheetJson { public string key; public string source; }
        [Serializable] public sealed class VariantSetJson { public string id; public VariantJson[] variants; }
        [Serializable] public sealed class VariantJson { public string id; public string name; public string swatch; public int dx; public int dy; public string sheet; }
        [Serializable] public sealed class ChannelJson { public string kind; public string[] source; }
        [Serializable] public sealed class RectOverrideJson { public string variant; public int[] rect; }
        [Serializable] public sealed class LightJson { public int[] at; public string color; public float intensity; public float radius; }

        [Serializable]
        public sealed class FacingJson
        {
            public int turns;
            public int[] rect;
            public int[] size;
            public int[] at;
            public int[] body;
            public int depth;
            public bool anySeat;
            public int[] seats;
            public int[] approaches;
            public int[] surfaces;
            public int frames;
            public float frameSeconds;
            public LightJson light;
            public RectOverrideJson[] variantRects;
        }

        [Serializable]
        public sealed class PieceJson
        {
            public string id, name, description, category, theme, sheet, layer, function, rotation, variantSet, sources;
            public int tier, price;
            public bool flippable, unique, wallBound;
            public FacingJson[] facings;
            public VariantJson[] variants;
            public ChannelJson[] palette;
        }

        [Serializable] sealed class SpritesJson { public SheetSpritesJson[] sheets; }
        [Serializable] sealed class SheetSpritesJson { public string file; public SpriteJson[] sprites; }
        [Serializable] sealed class SpriteJson { public string name; public int x, y, w, h, px; }

        public static CatalogJson Load() => JsonUtility.FromJson<CatalogJson>(File.ReadAllText(JsonPath));

        public static string SheetFile(string key) => $"Furniture_{key}";
        public static string SheetPath(string key) => $"{EditorPaths.Minifantasy}/{Pack}/{SheetFile(key)}.png";

        // ------------------------------------------------------------------ text

        public static string NameKey(string id) => $"furniture.{id}";
        public static string DescriptionKey(string id) => $"furniture.{id}.description";
        /// <summary>Colourway names are shared by text ("gold", "teal"), so the table holds each once.</summary>
        public static string LookKey(string name) => "look." + new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

        /// <summary>The table's English (UI table): names, descriptions and colourway names.</summary>
        public static IEnumerable<(string key, string english)> English()
        {
            CatalogJson data = File.Exists(JsonPath) ? Load() : null;
            if (data?.pieces == null) yield break;
            var looks = new HashSet<string>();
            foreach (PieceJson p in data.pieces)
            {
                yield return (NameKey(p.id), p.name);
                if (!string.IsNullOrEmpty(p.description)) yield return (DescriptionKey(p.id), p.description);
                foreach (VariantJson v in Variants(p, data))
                    if (!string.IsNullOrEmpty(v.name) && looks.Add(LookKey(v.name))) yield return (LookKey(v.name), v.name);
            }
        }

        static VariantJson[] Variants(PieceJson p, CatalogJson data)
        {
            if (p.variants != null && p.variants.Length > 0) return p.variants;
            if (!string.IsNullOrEmpty(p.variantSet))
            {
                VariantSetJson set = data.variantSets?.FirstOrDefault(s => s.id == p.variantSet);
                if (set != null) return set.variants;
            }
            return new[] { new VariantJson { id = string.Empty } };
        }

        // ------------------------------------------------------------------ slicing (read by the import pipeline)

        static Dictionary<string, Sheet> s_Sheets;
        static DateTime s_SheetsStamp;

        /// <summary>The slicing of a catalogue sheet copy, from <c>catalog_sprites.json</c> (null for any other texture).</summary>
        public static Sheet FindSheet(string assetPath)
        {
            if (!assetPath.StartsWith($"{EditorPaths.Minifantasy}/{Pack}/", StringComparison.Ordinal) || !File.Exists(SpritesPath)) return null;
            DateTime stamp = File.GetLastWriteTimeUtc(SpritesPath);
            if (s_Sheets == null || stamp != s_SheetsStamp)
            {
                s_Sheets = new Dictionary<string, Sheet>();
                s_SheetsStamp = stamp;
                var data = JsonUtility.FromJson<SpritesJson>(File.ReadAllText(SpritesPath));
                foreach (SheetSpritesJson s in data.sheets ?? Array.Empty<SheetSpritesJson>())
                {
                    var sheet = new Sheet
                    {
                        Source = null, Pack = Pack, File = s.file, Mode = SliceMode.Rects, Readable = true,
                        Rects = s.sprites.Select(r => new SheetRect(r.name, r.x, r.y, r.w, r.h, new Vector2((float)r.px / r.w, 0f))).ToArray(),
                    };
                    s_Sheets[sheet.AssetPath] = sheet;
                }
            }
            return s_Sheets.TryGetValue(assetPath, out Sheet found) ? found : null;
        }

        // ------------------------------------------------------------------ build

        public sealed class Built
        {
            public readonly List<FurnitureDefinition> Definitions = new();
            public readonly List<string> Warnings = new();
            public int Drawings;
        }

        sealed class Cut
        {
            public string Sheet;
            public string Name;
            public RectInt Rect;
            public int PivotX;
        }

        [MenuItem("Hearthdelve/Generate/Furniture Catalog (4f)", priority = 5)]
        public static void BuildMenu()
        {
            Built built = Build(AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset"));
            Debug.Log($"[Hearthdelve] Furniture catalogue: {built.Definitions.Count} pieces, {built.Drawings} drawings, {built.Warnings.Count} warnings.");
        }

        public static Built Build(GameDatabase database, IEnumerable<string> reservedIds = null)
        {
            var built = new Built();
            CatalogJson data = Load();
            EditorPaths.Ensure(Folder);
            EditorPaths.Ensure($"{EditorPaths.Minifantasy}/{Pack}");

            // The source images: the raw packs when present, else the copies already in the repo (same coordinates).
            var images = new Dictionary<string, Texture2D>();
            foreach (SheetJson s in data.sheets)
            {
                string raw = Path.Combine(EditorPaths.MinifantasySource, s.source);
                string copy = SheetPath(s.key);
                string from = File.Exists(raw) ? raw : File.Exists(copy) ? copy : null;
                if (from == null)
                {
                    built.Warnings.Add($"sheet '{s.key}': neither {raw} nor {copy} exists");
                    continue;
                }
                var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                image.LoadImage(File.ReadAllBytes(from));
                images[s.key] = image;
            }

            // Every drawing: (piece, facing, variant, frame) → a rect on a sheet.
            var cuts = new Dictionary<(string piece, int facing, int variant, int frame), Cut>();
            var ids = new HashSet<string>(reservedIds ?? Array.Empty<string>());
            foreach (PieceJson p in data.pieces)
            {
                if (!ids.Add(p.id)) built.Warnings.Add($"{p.id}: duplicate id");
                if (p.facings == null || p.facings.Length == 0)
                {
                    built.Warnings.Add($"{p.id}: no facings");
                    continue;
                }
                if (p.facings.Select(f => f.turns).Distinct().Count() != p.facings.Length) built.Warnings.Add($"{p.id}: two facings share a turn");
                VariantJson[] variants = Variants(p, data);
                for (int f = 0; f < p.facings.Length; f++)
                {
                    FacingJson facing = p.facings[f];
                    foreach (RectOverrideJson o in facing.variantRects ?? Array.Empty<RectOverrideJson>())
                        if (!variants.Any(v => v.id == o.variant)) built.Warnings.Add($"{p.id}: a rect for unknown variant '{o.variant}'");
                    for (int v = 0; v < variants.Length; v++)
                    {
                        string sheet = string.IsNullOrEmpty(variants[v].sheet) ? p.sheet : variants[v].sheet;
                        if (!images.TryGetValue(sheet ?? string.Empty, out Texture2D image))
                        {
                            built.Warnings.Add($"{p.id}: unknown sheet '{sheet}'");
                            continue;
                        }
                        RectInt rect = RectFor(facing, variants[v], v);
                        int frames = Mathf.Max(1, facing.frames);
                        RectInt trim = frames > 1 ? Trim(image, rect, frames) : new RectInt(0, 0, rect.width, rect.height);
                        for (int k = 0; k < frames; k++)
                        {
                            var r = new RectInt(rect.x + k * rect.width + trim.x, rect.y + trim.y, trim.width, trim.height);
                            if (!Inside(image, r))
                            {
                                built.Warnings.Add($"{p.id}: rect {r} is outside sheet '{sheet}'");
                                continue;
                            }
                            if (Empty(image, r)) built.Warnings.Add($"{p.id}/{variants[v].id}/turn {facing.turns}: rect {r} is empty");
                            string name = frames > 1 ? $"{p.id}_{f}_{v}_{k}" : $"{p.id}_{f}_{v}";
                            cuts[(p.id, f, v, k)] = new Cut { Sheet = sheet, Name = name, Rect = r, PivotX = r.width / 2 };
                        }
                    }
                }
            }
            built.Drawings = cuts.Count;

            // The copies: each sheet with only the named drawings left, and how to slice them.
            var sprites = new SpritesJson { sheets = cuts.Values.GroupBy(c => c.Sheet).OrderBy(g => g.Key).Select(g => new SheetSpritesJson
            {
                file = SheetFile(g.Key),
                sprites = g.OrderBy(c => c.Name, StringComparer.Ordinal).Select(c => new SpriteJson
                    { name = c.Name, x = c.Rect.x, y = c.Rect.y, w = c.Rect.width, h = c.Rect.height, px = c.PivotX }).ToArray(),
            }).ToArray() };
            WriteIfChanged(SpritesPath, JsonUtility.ToJson(sprites, true));
            s_Sheets = null;
            var written = new List<string>();
            foreach (var group in cuts.Values.GroupBy(c => c.Sheet))
            {
                Texture2D image = images[group.Key];
                var copy = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
                copy.SetPixels32(new Color32[image.width * image.height]);
                foreach (Cut c in group)
                {
                    int y = image.height - c.Rect.y - c.Rect.height;
                    copy.SetPixels(c.Rect.x, y, c.Rect.width, c.Rect.height, image.GetPixels(c.Rect.x, y, c.Rect.width, c.Rect.height));
                }
                copy.Apply();
                string path = SheetPath(group.Key);
                byte[] png = copy.EncodeToPNG();
                if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png)) File.WriteAllBytes(path, png);
                written.Add(path);
                UnityEngine.Object.DestroyImmediate(copy);
            }
            AssetDatabase.Refresh();
            foreach (string path in written) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var lookup = new Dictionary<string, Sprite>();
            foreach (string path in written)
                foreach (Sprite s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    lookup[s.name] = s;
            Sprite SpriteOf(string piece, int f, int v, int k)
            {
                if (!cuts.TryGetValue((piece, f, v, k), out Cut c)) return null;
                lookup.TryGetValue($"{SheetFile(c.Sheet)}_{c.Name}", out Sprite s);
                if (s == null) built.Warnings.Add($"{piece}: sprite {c.Name} wasn't sliced");
                return s;
            }

            var keep = new HashSet<string>();
            foreach (PieceJson p in data.pieces)
            {
                if (p.facings == null || p.facings.Length == 0) continue;
                FurnitureDefinition d = Define(p, Variants(p, data), SpriteOf, built.Warnings);
                built.Definitions.Add(d);
                keep.Add(AssetDatabase.GetAssetPath(d));
            }
            // Pieces taken out of the table leave the project (saves holding them drop them with a warning).
            foreach (string guid in AssetDatabase.FindAssets("t:FurnitureDefinition", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!keep.Contains(path)) AssetDatabase.DeleteAsset(path);
            }

            if (database != null)
            {
                foreach (FurnitureDefinition d in built.Definitions)
                    if (!database.furniture.Contains(d)) database.furniture.Add(d);
                database.furniture.RemoveAll(d => d == null);
                EditorUtility.SetDirty(database);
            }
            AssetDatabase.SaveAssets();
            foreach (string w in built.Warnings) Debug.LogWarning($"[Hearthdelve] Furniture catalogue: {w}");
            foreach (Texture2D image in images.Values) UnityEngine.Object.DestroyImmediate(image);
            return built;
        }

        static RectInt RectFor(FacingJson facing, VariantJson variant, int index)
        {
            RectOverrideJson o = facing.variantRects?.FirstOrDefault(r => r.variant == variant.id);
            int[] r = o?.rect is { Length: 4 } ? o.rect : facing.rect;
            var rect = new RectInt(r[0], r[1], r[2], r[3]);
            if (o == null && index > 0) rect.position += new Vector2Int(variant.dx, variant.dy);
            return rect;
        }

        /// <summary>The smallest box (inside one frame) holding every frame's drawing, so a strip's empty margins aren't kept.</summary>
        static RectInt Trim(Texture2D image, RectInt first, int frames)
        {
            int x0 = first.width, y0 = first.height, x1 = -1, y1 = -1;
            for (int k = 0; k < frames; k++)
                for (int y = 0; y < first.height; y++)
                for (int x = 0; x < first.width; x++)
                {
                    int sx = first.x + k * first.width + x, sy = first.y + y;
                    if (sx >= image.width || sy >= image.height || image.GetPixel(sx, image.height - 1 - sy).a <= 0f) continue;
                    x0 = Mathf.Min(x0, x);
                    y0 = Mathf.Min(y0, y);
                    x1 = Mathf.Max(x1, x);
                    y1 = Mathf.Max(y1, y);
                }
            return x1 < 0 ? new RectInt(0, 0, first.width, first.height) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        static bool Inside(Texture2D image, RectInt r) => r.x >= 0 && r.y >= 0 && r.xMax <= image.width && r.yMax <= image.height && r.width > 0 && r.height > 0;

        static bool Empty(Texture2D image, RectInt r)
        {
            int y = image.height - r.y - r.height;
            return image.GetPixels(r.x, y, r.width, r.height).All(c => c.a <= 0f);
        }

        static void WriteIfChanged(string path, string text)
        {
            if (File.Exists(path) && File.ReadAllText(path) == text) return;
            File.WriteAllText(path, text);
        }

        static T Parse<T>(string text, T fallback, string piece, List<string> warnings) where T : struct
        {
            if (string.IsNullOrEmpty(text)) return fallback;
            if (Enum.TryParse(text, out T value)) return value;
            warnings.Add($"{piece}: unknown {typeof(T).Name} '{text}'");
            return fallback;
        }

        static Color Hex(string hex, Color fallback) => !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out Color c) ? c : fallback;

        static Vector2Int DirectionFor(int turns) => (((turns % 4) + 4) % 4) switch
        {
            0 => Vector2Int.down,
            1 => Vector2Int.right,
            2 => Vector2Int.up,
            _ => Vector2Int.left,
        };

        static List<Vector2> Points(int[] flat, Vector2 origin)
        {
            var points = new List<Vector2>();
            if (flat == null) return points;
            for (int i = 0; i + 1 < flat.Length; i += 2) points.Add(origin + new Vector2(flat[i], flat[i + 1]) / k_Ppt);
            return points;
        }

        static FurnitureDefinition Define(PieceJson p, VariantJson[] variants, Func<string, int, int, int, Sprite> spriteOf, List<string> warnings) =>
            LookTestContent.CreateOrUpdate<FurnitureDefinition>($"{Folder}/Furniture_{p.id}.asset", d =>
            {
                d.id = p.id;
                d.nameKey = NameKey(p.id);
                d.descriptionKey = string.IsNullOrEmpty(p.description) ? null : DescriptionKey(p.id);
                d.category = Parse(p.category, FurnitureCategory.Curios, p.id, warnings);
                d.theme = Parse(p.theme, FurnitureTheme.Village, p.id, warnings);
                d.layer = Parse(p.layer, FurnitureLayer.Standing, p.id, warnings);
                d.function = Parse(p.function, FurnitureFunction.None, p.id, warnings);
                d.station = StationKind.None;
                d.useNameKey = null;
                d.reach = 1f;
                d.flippable = p.flippable;
                d.wallBound = p.wallBound;
                d.unique = p.unique;
                d.price = Mathf.Max(0, p.price);
                d.catalogTier = Mathf.Max(0, p.tier);
                d.sources = string.IsNullOrEmpty(p.sources) ? FurnitureSource.Bought : Parse(p.sources, FurnitureSource.Bought, p.id, warnings);
                d.sellBack = 0.5f;
                bool turned = p.facings.Length > 1 || p.facings[0].turns != 0;
                d.rotation = Parse(p.rotation, turned ? RotationMode.AuthoredFacings : RotationMode.None, p.id, warnings);

                d.variants = variants.Length > 1 || !string.IsNullOrEmpty(variants[0].id)
                    ? variants.Select(v => new FurnitureVariant { id = v.id, nameKey = string.IsNullOrEmpty(v.name) ? null : LookKey(v.name), swatch = Hex(v.swatch, Color.white) }).ToList()
                    : new List<FurnitureVariant>();
                d.paletteChannels = (p.palette ?? Array.Empty<ChannelJson>())
                    .Select(c => new PaletteChannel { kind = c.kind, source = (c.source ?? Array.Empty<string>()).Select(h => (Color32)Hex(h, Color.clear)).ToArray() })
                    .ToList();

                d.facings = new List<FurnitureFacing>();
                for (int f = 0; f < p.facings.Length; f++)
                {
                    FacingJson j = p.facings[f];
                    Sprite still = spriteOf(p.id, f, 0, 0);
                    if (still == null) continue;
                    d.facings.Add(BuildFacing(p, d, j, f, variants.Length, still, spriteOf, warnings));
                }
                CheckPalette(d, warnings);
            });

        static FurnitureFacing BuildFacing(PieceJson p, FurnitureDefinition d, FacingJson j, int f, int variantCount, Sprite still,
            Func<string, int, int, int, Sprite> spriteOf, List<string> warnings)
        {
            int w = Mathf.RoundToInt(still.rect.width), h = Mathf.RoundToInt(still.rect.height), pivot = w / 2;
            bool standing = d.layer == FurnitureLayer.Standing, surface = d.layer == FurnitureLayer.Surface;
            // The body, in art pixels from the drawing's bottom-left: as given, its bottom rows, or the whole drawing.
            RectInt body = j.body is { Length: 4 } ? new RectInt(j.body[0], j.body[1], j.body[2], j.body[3])
                : j.depth > 0 ? new RectInt(0, 0, w, Mathf.Min(j.depth, h)) : new RectInt(0, 0, w, h);
            Vector2Int size = j.size is { Length: 2 } ? new Vector2Int(j.size[0], j.size[1])
                : surface ? Vector2Int.one
                : standing ? new Vector2Int(Mathf.Max(1, Mathf.CeilToInt(body.width / (float)k_Ppt)), Mathf.Max(1, Mathf.CeilToInt(body.yMax / (float)k_Ppt)))
                : new Vector2Int(Mathf.Max(1, Mathf.CeilToInt(w / (float)k_Ppt)), Mathf.Max(1, Mathf.CeilToInt(h / (float)k_Ppt)));
            Vector2 at = j.at is { Length: 2 } ? new Vector2(j.at[0], j.at[1]) / k_Ppt : Vector2.zero;
            Vector2 pivotAt = (surface ? Vector2.zero : new Vector2(size.x / 2f, 0f)) + at;
            float left = pivotAt.x - pivot / (float)k_Ppt;

            var facing = new FurnitureFacing { turns = j.turns, size = size, interactPoint = pivotAt };
            int frames = Mathf.Max(1, j.frames);
            var art = new FurnitureArt
            {
                name = "Art",
                position = pivotAt,
                sortingLayer = d.layer is FurnitureLayer.Wall or FurnitureLayer.Floor ? SortingLayers.Floor : SortingLayers.YSorted,
                order = d.layer switch { FurnitureLayer.Wall => 3, FurnitureLayer.Floor => 1, _ => 0 },
                frameSeconds = j.frameSeconds > 0f ? j.frameSeconds : 0.12f,
            };
            if (frames > 1)
            {
                art.frames = Enumerable.Range(0, frames).Select(k => spriteOf(p.id, f, 0, k)).ToArray();
                art.variantFrames = Enumerable.Range(0, variantCount)
                    .Select(v => new SpriteList { frames = v == 0 ? Array.Empty<Sprite>() : Enumerable.Range(0, frames).Select(k => spriteOf(p.id, f, v, k)).ToArray() }).ToList();
            }
            else
            {
                art.sprite = still;
                art.frames = Array.Empty<Sprite>();
                art.variantSprites = Enumerable.Range(0, variantCount).Select(v => v == 0 ? null : spriteOf(p.id, f, v, 0)).ToArray();
            }
            facing.art.Add(art);

            if (standing) facing.bodies.Add(new Rect(left + body.x / (float)k_Ppt, pivotAt.y + body.y / (float)k_Ppt, body.width / (float)k_Ppt, body.height / (float)k_Ppt));

            if (d.function == FurnitureFunction.Seat)
            {
                Vector2Int dir = j.anySeat ? Vector2Int.zero : DirectionFor(j.turns);
                List<Vector2> seats = j.seats is { Length: >= 2 } ? Points(j.seats, pivotAt) : new List<Vector2> { pivotAt };
                List<Vector2> approaches = Points(j.approaches, pivotAt);
                for (int i = 0; i < seats.Count; i++)
                    facing.seats.Add(new FurnitureSeat
                    {
                        position = seats[i],
                        approach = j.anySeat ? seats[i] : i < approaches.Count ? approaches[i] : FurnitureRules.ApproachFor(seats[i], dir),
                        facing = dir,
                    });
            }
            facing.surfaces = j.surfaces is { Length: >= 2 } ? Points(j.surfaces, pivotAt)
                : d.function == FurnitureFunction.Table ? new List<Vector2> { new(pivotAt.x, pivotAt.y + (body.y + body.height / 2f) / k_Ppt) } : new List<Vector2>();
            if (j.light?.at is { Length: 2 })
            {
                float r = j.light.radius > 0f ? j.light.radius : 3f;
                facing.lights.Add(new FurnitureLight
                {
                    position = pivotAt + new Vector2(j.light.at[0], j.light.at[1]) / k_Ppt,
                    color = Hex(j.light.color, new Color(1f, 0.7f, 0.4f)),
                    intensity = j.light.intensity > 0f ? j.light.intensity : 0.6f,
                    innerRadius = r * 0.2f,
                    outerRadius = r,
                    falloff = 0.6f,
                });
            }
            if (d.layer == FurnitureLayer.Wall && size.y > 3) warnings.Add($"{p.id}: a wall piece {size.y} tiles tall won't fit the back wall's three rows");
            if (standing && (body.width > size.x * k_Ppt || body.yMax > size.y * k_Ppt))
                warnings.Add($"{p.id}/turn {j.turns}: its body ({body.width}x{body.yMax} px) is bigger than its {size.x}x{size.y} footprint");
            return facing;
        }

        /// <summary>A channel whose colours appear nowhere in the drawings would never change anything.</summary>
        static void CheckPalette(FurnitureDefinition d, List<string> warnings)
        {
            if (d.paletteChannels.Count == 0) return;
            var drawn = new HashSet<Color32>();
            foreach (FurnitureFacing f in d.facings)
            foreach (FurnitureArt a in f.art)
            {
                Sprite s = a.SpriteFor(0);
                if (s == null || !s.texture.isReadable) continue;
                Rect r = s.rect;
                foreach (Color c in s.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height))
                    if (c.a > 0f) drawn.Add(c);
            }
            if (drawn.Count == 0) return;
            foreach (PaletteChannel c in d.paletteChannels)
                if (!c.source.Any(drawn.Contains)) warnings.Add($"{d.id}: palette channel '{c.kind}' matches no colour in its drawings");
        }
    }
}
