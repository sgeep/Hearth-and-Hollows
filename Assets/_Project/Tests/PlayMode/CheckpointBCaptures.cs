using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Not a check: renders 4f Checkpoint B for review at 320×180 into <c>BatchLogs/checkpointB/</c>: three taverns furnished
    /// differently from the catalogue (a dwarven hall, a castle banquet, a haunted den), the guest room as it starts and
    /// furnished, and the catalogue and colour panels. Layouts go through the real placement rules: a piece that won't stand
    /// where it's asked is skipped and listed. Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class CheckpointBCaptures : LookTestFixture
    {
        const string k_Out = "BatchLogs/checkpointB";
        static DecorateMode Mode => DecorateMode.Instance;

        /// <summary>A piece to place: definition, cell, turns, colourway, palette, and the index (in this list) of the piece it stands on.</summary>
        internal readonly struct P
        {
            public readonly string Id;
            public readonly int X, Y, Turns, Host;
            public readonly string Variant, Palette;

            public P(string id, int x, int y, int turns = 0, string variant = "", string palette = "", int host = -1)
            {
                Id = id;
                X = x;
                Y = y;
                Turns = turns;
                Variant = variant;
                Palette = palette;
                Host = host;
            }
        }

        static readonly string[] k_Keep = { "tavern_bar", "kitchen_range", "stew_pot", "pass_table", "bottle_shelves", "wall_sign", "wall_fireplace", "low_shelf", "shelf_glasses" };

        /// <summary>
        /// Lays out an area: the stations and back-wall pieces kept (for the tavern), everything else replaced by
        /// <paramref name="pieces"/> (each owned for the occasion), and the finishes. Returns what wouldn't stand.
        /// </summary>
        internal static List<string> Furnish(AreaFurniture area, string floor, string wall, bool keepStations, params P[] pieces)
        {
            FurnitureState state = area.State;
            var placed = new List<PlacedFurniture>();
            if (keepStations) placed.AddRange(area.CurrentLayout().Where(p => k_Keep.Contains(p.definition)).Select(p => p.Clone()));
            var layout = new FurnitureLayout(area.Shape(), area.Definition, placed);
            var uids = new Dictionary<int, int>();
            var skipped = new List<string>();
            for (int i = 0; i < pieces.Length; i++)
            {
                P p = pieces[i];
                var piece = new PlacedFurniture
                {
                    uid = state.TakeUid(), definition = p.Id, cell = new Vector2Int(p.X, p.Y), turns = p.Turns, variant = p.Variant, palette = p.Palette,
                };
                if (p.Host >= 0)
                {
                    if (!uids.TryGetValue(p.Host, out int host))
                    {
                        skipped.Add($"{p.Id} on #{p.Host}: no host");
                        continue;
                    }
                    piece.host = host;
                    piece.anchor = 0;
                    while (layout.Check(piece).Problem == PlacementProblem.Overlaps && piece.anchor < 4) piece.anchor++;
                }
                PlacementCheck check = layout.Check(piece);
                if (!check.IsValid)
                {
                    skipped.Add($"{p.Id} at ({p.X},{p.Y}) t{p.Turns}: {check.Problem}");
                    continue;
                }
                layout.Add(piece);
                uids[i] = piece.uid;
            }
            foreach (PlacedFurniture p in layout.Pieces)
                if (state.InStorage(p.definition) <= 0 && state.OwnedCount(p.definition) <= layout.Pieces.Count(q => q.definition == p.definition))
                    state.AddOwnedCopies(p.definition, 1);
            state.SetFinish(area.Area.Id, FinishKind.Floor, floor);
            state.SetFinish(area.Area.Id, FinishKind.Wall, wall);
            area.Commit(layout.Pieces);
            return skipped;
        }

        /// <summary>A dwarven hall: slate and stone tables in rows, banners, lanterns, kegs and a great tun.</summary>
        internal static readonly P[] DwarvenHall =
        {
            new("dwarven_rug", 3, 5, variant: "red"), new("dwarven_rug", 15, 5, variant: "teal"),
            new("dwarven_table_great", 3, 5, variant: "slate"),
            new("dwarven_chair", 3, 4, 2, "slate"), new("dwarven_chair", 4, 4, 2, "slate"), new("dwarven_chair", 5, 4, 2, "slate"),
            new("dwarven_chair", 3, 8, 0, "slate"), new("dwarven_chair", 4, 8, 0, "slate"), new("dwarven_chair", 5, 8, 0, "slate"),
            new("dwarven_chair", 2, 6, 1, "slate"), new("dwarven_chair", 6, 6, 3, "slate"),
            new("dwarven_table_great", 15, 5, variant: "stone"),
            new("dwarven_chair", 15, 4, 2, "stone"), new("dwarven_chair", 16, 4, 2, "stone"), new("dwarven_chair", 17, 4, 2, "stone"),
            new("dwarven_chair", 15, 8, 0, "stone"), new("dwarven_chair", 16, 8, 0, "stone"), new("dwarven_chair", 17, 8, 0, "stone"),
            new("dwarven_chair", 14, 6, 1, "stone"), new("dwarven_chair", 18, 6, 3, "stone"),
            new("dwarven_table_long", 9, 6, 0, "wood"), new("dwarven_chair", 9, 5, 2, "wood"), new("dwarven_chair", 10, 5, 2, "wood"),
            new("dwarven_chair", 11, 5, 2, "wood"),
            new("dwarven_tankards", 0, 0, 0, host: 11), new("dwarven_tankards", 0, 0, 2, host: 20), new("candle", 0, 0, host: 2),
            new("dwarven_keg", 25, 3), new("dwarven_keg", 26, 3), new("dwarven_keg", 26, 4), new("dwarven_barrel_stack", 23, 3),
            new("dwarven_great_tun", 16, 10), new("dwarven_chest", 10, 9, variant: "slate"),
            new("dwarven_lantern", 1, 2, variant: "amber"), new("dwarven_lantern", 21, 2, variant: "teal"), new("dwarven_lantern", 1, 9, variant: "red"),
            new("dwarven_banner", 8, 14, variant: "red"), new("dwarven_banner", 13, 14, variant: "amber"), new("dwarven_banner", 18, 14, variant: "teal"),
            new("dwarven_banner", 22, 14, variant: "red"),
        };

        /// <summary>A castle banquet: gilded tables and chairs on rugs, banners, trophies, torches and a high-backed chair.</summary>
        internal static readonly P[] CastleBanquet =
        {
            new("castle_rug", 3, 4), new("castle_rug", 9, 4, variant: "silver"), new("castle_rug", 15, 4),
            new("castle_runner", 12, 7),
            new("castle_table", 4, 5), new("castle_chair", 4, 4, 2), new("castle_chair", 5, 4, 2), new("castle_chair", 4, 7, 0), new("castle_chair", 5, 7, 0),
            new("castle_table", 10, 5, 1, "silver"), new("castle_chair", 10, 4, 2, "silver"), new("castle_chair", 11, 4, 2, "silver"),
            new("castle_chair", 10, 7, 0, "silver"), new("castle_chair", 11, 7, 0, "silver"),
            new("castle_table", 16, 5), new("castle_chair", 16, 4, 2), new("castle_chair", 17, 4, 2), new("castle_chair", 16, 7, 0), new("castle_chair", 17, 7, 0),
            new("candle", 0, 0, host: 4), new("candle", 0, 0, host: 9), new("candle", 0, 0, host: 14),
            new("castle_candlestick", 7, 3), new("castle_candlestick", 14, 3, variant: "silver"),
            new("castle_throne", 13, 10), new("castle_torch", 1, 2), new("castle_torch", 25, 2), new("castle_torch", 8, 9, variant: "iron"),
            new("castle_banner", 8, 14), new("castle_banner", 13, 14, variant: "gilded"), new("castle_banner", 17, 14, variant: "chequered"),
            new("mounted_antlers", 9, 14), new("mounted_bear", 18, 14), new("castle_portrait", 20, 14, variant: "couple"),
            new("castle_arms", 23, 15, variant: "swords"), new("knight_statue", 26, 6, 3, "marble"), new("knight_statue", 1, 6, 1, "marble"),
            new("royal_bed", 25, 9),
        };

        /// <summary>A haunted den: clawed armchairs, a faded sofa, cloth tables with jack-o'-lanterns, grimoires, portraits and webs.</summary>
        internal static readonly P[] HauntedDen =
        {
            new("haunted_carpet", 3, 4), new("haunted_carpet", 14, 4, variant: "square"),
            new("cloth_table", 4, 5, variant: "candle"), new("haunted_chair", 3, 5, 1), new("haunted_chair", 6, 5, 3),
            new("haunted_armchair", 4, 7, 0, "torn"), new("haunted_armchair", 4, 3, 2),
            new("cloth_table", 15, 5, variant: "set"), new("haunted_chair", 14, 5, 1), new("haunted_chair", 17, 5, 3), new("haunted_armchair", 15, 7, 0),
            new("haunted_sofa", 8, 3), new("cloth_table", 9, 5, variant: "flowers"), new("haunted_chair", 8, 6, 1), new("haunted_chair", 11, 6, 3),
            new("jack_o_lantern", 0, 0, host: 2), new("jack_o_lantern", 0, 0, variant: "grin", host: 7), new("jack_o_lantern", 0, 0, variant: "fangs", host: 12),
            new("gothic_bookcase", 13, 13), new("gothic_bookcase", 15, 13, variant: "skulls"), new("mimic_chest", 24, 3, variant: "purple"),
            new("candle_rack", 1, 2), new("plant_moss", 25, 9, variant: "purple"),
            new("haunted_portrait", 8, 14), new("haunted_portrait", 20, 14, variant: "shade"), new("haunted_portrait", 22, 14, variant: "pumpkin"),
            new("haunted_mirror", 13, 15), new("cobweb", 25, 15, variant: "right"), new("cobweb_great", 17, 14),
        };

        /// <summary>The guest room furnished: a bed, a wardrobe, a washstand, a desk corner and a rug.</summary>
        internal static readonly P[] GuestRoomFurnished =
        {
            new("rug_village", 5, 5, variant: "navy"), new("bed_double", 2, 7, variant: "linen"), new("nightstand", 4, 8, variant: "blue"),
            new("candle", 0, 0, host: 2), new("wardrobe", 9, 8, variant: "carved"), new("washstand", 12, 8), new("standing_mirror", 13, 8),
            new("chest", 1, 3, variant: "blue"), new("plant_lilies", 16, 3, variant: "blue"), new("armchair_leather", 12, 4, 0),
            new("tavern_table_small", 12, 3), new("book_stack", 0, 0, host: 10), new("picture", 3, 10, variant: "sea"), new("picture", 7, 11, variant: "flowers"),
            new("elven_bunting", 10, 11, variant: "blue"), new("bed_single", 15, 6, 0, "brown"), new("dresser", 6, 8, variant: "blue"),
            new("plant_roses", 1, 8, variant: "green"), new("standing_mirror", 16, 8),
        };

        [UnityTest]
        public IEnumerator CaptureCheckpointB()
        {
            Directory.CreateDirectory(k_Out);
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            DecorateMode.Sandbox = true;
            AreaFurniture tavern = AreaFurniture.Tavern, guest = AreaFurniture.Find(PropertyArea.GuestRoomId);
            var report = new List<string>();

            yield return RoomShot("tavern_starting.png");

            // The starting tavern restyled through palette ramps alone (D11): walnut and teal, rosewood and amber.
            List<PlacedFurniture> restyled = tavern.CurrentLayout().Select(p => p.Clone()).ToList();
            foreach (PlacedFurniture p in restyled)
            {
                if (p.definition == "tavern_chair") p.palette = p.cell.x < 12 ? "cushion=teal;wood=walnut" : "cushion=amber;wood=rosewood";
                if (p.definition.StartsWith("table_round")) p.palette = p.cell.x < 12 ? "wood=walnut" : "wood=rosewood";
            }
            tavern.State.SetFinish(tavern.Area.Id, FinishKind.Wall, "wall_panelling_walnut");
            tavern.Commit(restyled);
            yield return RoomShot("tavern_recoloured.png");
            tavern.State.SetFinish(tavern.Area.Id, FinishKind.Wall, "wall_panelling");
            foreach (var (name, floor, wall, pieces) in new[]
                     {
                         ("tavern_dwarven", "floor_chequer", "wall_panelling_walnut", DwarvenHall),
                         ("tavern_castle", "floor_blue_tiles", "wall_cream", CastleBanquet),
                         ("tavern_haunted", "floor_plum", "wall_panelling_ash", HauntedDen),
                     })
            {
                foreach (string s in Furnish(tavern, floor, wall, true, pieces)) report.Add($"{name}: {s}");
                report.Add($"{name}: {tavern.Seats.Count} seats; layout: {string.Join(", ", tavern.Report.Issues.Select(i => (i.Blocking ? "blocking " : "") + i.Kind))}");
                yield return RoomShot($"{name}.png");
            }

            TavernView.Show(guest.Area);
            yield return RoomShot("guest_starting.png");

            // Every ramp on the tavern's own chair and table (the prototype group): woods across the back, cushions in front.
            PaletteLibrary library = guest.Palettes;
            var swatch = new List<P>();
            List<PaletteRamp> woods = library.For("wood"), cushions = library.For("cushion");
            for (int i = 0; i < woods.Count; i++)
            {
                swatch.Add(new P("table_round_a", 1 + i * 2, 7, palette: $"wood={woods[i].id}"));
                swatch.Add(new P("tavern_chair", 1 + i * 2, 9, 0, palette: $"wood={woods[i].id}"));
            }
            for (int i = 0; i < cushions.Count; i++)
                swatch.Add(new P("tavern_chair", 1 + i * 2, 4, 0, palette: $"cushion={cushions[i].id}"));
            for (int i = 0; i < 6; i++) swatch.Add(new P("plant_roses", 1 + i * 3, 2, variant: new[] { "terracotta", "brown", "blue", "purple", "sage", "green" }[i]));
            foreach (string s in Furnish(guest, "floor_teal", "wall_cream", false, swatch.ToArray())) report.Add($"ramps: {s}");
            yield return RoomShot("recolour_ramps.png");
            foreach (string s in Furnish(guest, "floor_parquet", "wall_green", false, GuestRoomFurnished)) report.Add($"guest: {s}");
            yield return RoomShot("guest_furnished.png");
            TavernView.Show(tavern.Area);

            // The catalogue and the colour panel.
            Mode.Enter();
            yield return null;
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            screen.OpenStorage();
            yield return Shot("catalogue_storage.png");
            screen.Catalogue.Tab(2);
            yield return Shot("catalogue_tables.png");
            DecorateMode.Sandbox = false;
            screen.Catalogue.Tab(-1);
            screen.Catalogue.Select(6);
            yield return Shot("catalogue_seating_locked.png");
            screen.Catalogue.Close();
            Mode.SetCursor(new Vector2Int(4, 4));
            screen.OpenStyle();
            yield return Shot("colours.png");
            screen.Style.Close();
            // A tavern chair: wood and cushion ramps, and the schemes.
            Mode.Leave();
            List<PlacedFurniture> plain = tavern.CurrentLayout().Select(p => p.Clone()).ToList();
            plain.RemoveAll(p => !k_Keep.Contains(p.definition));
            plain.Add(new PlacedFurniture { uid = tavern.State.TakeUid(), definition = "tavern_chair", cell = new Vector2Int(5, 6), palette = "cushion=teal;wood=walnut" });
            tavern.State.AddOwnedCopies("tavern_chair", 1);
            tavern.Commit(plain);
            Mode.Enter();
            yield return null;
            Mode.SetCursor(new Vector2Int(5, 6));
            screen.OpenStyle();
            yield return Shot("colours_chair.png");
            screen.Style.Close();
            Mode.Leave();
            File.WriteAllLines($"{k_Out}/report.txt", report);
            Debug.Log("[Hearthdelve] Checkpoint B captures:\n" + string.Join("\n", report));
        }

        static IEnumerator Shot(string name)
        {
            yield return null;
            yield return null;
            TavernEveningCaptures.Capture($"{k_Out}/{name}");
        }

        /// <summary>The room alone (no UI, no characters), then everything shown again.</summary>
        static IEnumerator RoomShot(string name)
        {
            yield return null;
            yield return null;
            var hidden = new List<Behaviour>();
            foreach (Canvas c in Object.FindObjectsByType<Canvas>())
                if (c.enabled) hidden.Add(c);
            foreach (Renderer r in Object.FindObjectsByType<MoreMountains.TopDownEngine.Character>().SelectMany(c => c.GetComponentsInChildren<Renderer>()))
                if (r.enabled) r.enabled = false;
            foreach (Behaviour b in hidden) b.enabled = false;
            TavernEveningCaptures.Capture($"{k_Out}/{name}");
            foreach (Behaviour b in hidden) b.enabled = true;
        }
    }
}
