using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Kariaston's ground tiles (the Crossroads, 2026-10-10): rule tiles that autotile exactly as the mockup's script does
    /// (Tools/village/mockup/kar.py, <c>Map.autotile</c>), so the relayout reproduces the chosen image and paths painted by hand
    /// later get their edges too. Each autotile is a 3×3 block (corners and edges) with its inner corners in the two rows below:
    /// NW, NE, NW+SE, then SW, SE, NE+SW. The script's order decides: a missing north wins over a missing south, a missing west
    /// over a missing east; with all four sides present, the missing diagonals pick an inner corner, and anything else is the centre.
    /// </summary>
    public static class KariastonGround
    {
        public const string Dirt = "Kariaston_Dirt", Cobble = "Kariaston_Cobble", Water = "Kariaston_Water", PatchA = "Kariaston_GrassPatchA", PatchB = "Kariaston_GrassPatchB";

        /// <summary>One autotile: the sheet, the 3×3 block's top-left cell, and whether it has the two-corner pieces.</summary>
        public readonly struct Autotile
        {
            public readonly string pack, file;
            public readonly int column, row;
            public readonly bool pairs;
            /// <summary>Animation: a second frame's block this many columns to the right (0: still).</summary>
            public readonly int frameOffset;

            public Autotile(string pack, string file, int column, int row, bool pairs, int frameOffset = 0)
            {
                this.pack = pack;
                this.file = file;
                this.column = column;
                this.row = row;
                this.pairs = pairs;
                this.frameOffset = frameOffset;
            }
        }

        public static readonly Dictionary<string, Autotile> Autotiles = new()
        {
            [Dirt] = new Autotile(MinifantasySheets.MedievalCity, KariastonSheets.CityTiles, 14, 138, pairs: true),
            [Cobble] = new Autotile(MinifantasySheets.MedievalCity, KariastonSheets.CityTiles, 22, 138, pairs: false),
            [Water] = new Autotile(KariastonSheets.PlainsPack, KariastonSheets.Tiles, 25, 3, pairs: false, frameOffset: 4),
            [PatchA] = new Autotile(KariastonSheets.PlainsPack, KariastonSheets.MoreGrass, 1, 3, pairs: true),
            [PatchB] = new Autotile(KariastonSheets.PlainsPack, KariastonSheets.MoreGrass, 5, 3, pairs: true),
        };

        /// <summary>Seconds per frame of the pond's ripple (as before the Crossroads).</summary>
        const float k_WaterFrameSeconds = 0.4f;

        // ------------------------------------------------------------------ the script's rule (pure; the tests hold the tiles to it)

        /// <summary>
        /// The block cell (column, row offsets from the 3×3's top-left) the script draws for a cell with these neighbours present.
        /// Exactly <c>Map.autotile</c> in kar.py; an autotile without two-corner pieces falls back to the centre for them.
        /// </summary>
        public static Vector2Int Part(bool n, bool s, bool e, bool w, bool nw, bool ne, bool sw, bool se, bool pairs = true)
        {
            int r = !n ? 0 : !s ? 2 : 1, c = !w ? 0 : !e ? 2 : 1;
            if (r != 1 || c != 1) return new Vector2Int(c, r);
            bool mNW = !nw, mNE = !ne, mSW = !sw, mSE = !se;
            int missing = (mNW ? 1 : 0) + (mNE ? 1 : 0) + (mSW ? 1 : 0) + (mSE ? 1 : 0);
            if (missing == 0) return new Vector2Int(1, 1);
            if (missing == 1) return mNW ? new Vector2Int(0, 3) : mNE ? new Vector2Int(1, 3) : mSW ? new Vector2Int(0, 4) : new Vector2Int(1, 4);
            if (missing == 2 && pairs && mNW && mSE) return new Vector2Int(2, 3);
            if (missing == 2 && pairs && mNE && mSW) return new Vector2Int(2, 4);
            return new Vector2Int(1, 1);
        }

        // ------------------------------------------------------------------ the tiles

        static readonly Vector3Int k_N = new(0, 1, 0), k_S = new(0, -1, 0), k_E = new(1, 0, 0), k_W = new(-1, 0, 0);
        static readonly Vector3Int k_NW = new(-1, 1, 0), k_NE = new(1, 1, 0), k_SW = new(-1, -1, 0), k_SE = new(1, -1, 0);
        const int This = RuleTile.TilingRuleOutput.Neighbor.This, NotThis = RuleTile.TilingRuleOutput.Neighbor.NotThis;

        /// <summary>The rules in the script's order (first match wins): which neighbours must be present or missing, and the part.</summary>
        static IEnumerable<(Vector3Int[] at, int[] want, Vector2Int part)> Rules(bool pairs)
        {
            yield return (new[] { k_N, k_W }, new[] { NotThis, NotThis }, new Vector2Int(0, 0));
            yield return (new[] { k_N, k_E }, new[] { NotThis, NotThis }, new Vector2Int(2, 0));
            yield return (new[] { k_N }, new[] { NotThis }, new Vector2Int(1, 0));
            yield return (new[] { k_S, k_W }, new[] { NotThis, NotThis }, new Vector2Int(0, 2));
            yield return (new[] { k_S, k_E }, new[] { NotThis, NotThis }, new Vector2Int(2, 2));
            yield return (new[] { k_S }, new[] { NotThis }, new Vector2Int(1, 2));
            yield return (new[] { k_W }, new[] { NotThis }, new Vector2Int(0, 1));
            yield return (new[] { k_E }, new[] { NotThis }, new Vector2Int(2, 1));
            // All four sides present: exactly one diagonal missing, then (with the pieces) exactly two opposite ones.
            Vector3Int[] diagonals = { k_NW, k_NE, k_SW, k_SE };
            Vector2Int[] single = { new(0, 3), new(1, 3), new(0, 4), new(1, 4) };
            for (int i = 0; i < 4; i++)
            {
                var want = new int[4];
                for (int j = 0; j < 4; j++) want[j] = j == i ? NotThis : This;
                yield return (diagonals, want, single[i]);
            }
            if (!pairs) yield break;
            yield return (diagonals, new[] { NotThis, This, This, NotThis }, new Vector2Int(2, 3));
            yield return (diagonals, new[] { This, NotThis, NotThis, This }, new Vector2Int(2, 4));
        }

        static Sprite Cell(Autotile a, int column, int row)
        {
            Sprite sprite = a.file == KariastonSheets.Tiles ? PlainsCell(column, row) : MinifantasyImporter.Sprite(a.pack, a.file, $"Cell_{column}_{row}");
            if (sprite == null) throw new InvalidOperationException($"Kariaston: no ground cell {column},{row} in {a.pack}/{a.file}.");
            return sprite;
        }

        /// <summary>Forgotten Plains' lake cells keep their 4h names (Water_TL … for frame one, Water2_TL … for frame two).</summary>
        static Sprite PlainsCell(int column, int row)
        {
            string[,] parts = { { "TL", "L", "BL", "InNW", "InSW" }, { "T", "C", "B", "InNE", "InSE" }, { "TR", "R", "BR", null, null } };
            bool second = column >= 29;
            int c = column - (second ? 29 : 25), r = row - 3;
            string part = parts[c, r];
            return part == null ? null : MinifantasyImporter.Sprite(KariastonSheets.PlainsPack, KariastonSheets.Tiles, $"{(second ? "Water2" : "Water")}_{part}");
        }

        /// <summary>Makes (or refreshes) one rule tile under Art/Tiles.</summary>
        public static RuleTile Tile(string name)
        {
            Autotile a = Autotiles[name];
            bool solid = name == Water;
            return LookTestContent.CreateOrUpdate<RuleTile>($"{EditorPaths.Tiles}/{name}.asset", tile =>
            {
                Sprite[] Frames(Vector2Int part) => a.frameOffset == 0
                    ? new[] { Cell(a, a.column + part.x, a.row + part.y) }
                    : new[] { Cell(a, a.column + part.x, a.row + part.y), Cell(a, a.column + a.frameOffset + part.x, a.row + part.y) };
                var centre = new Vector2Int(1, 1);
                tile.m_DefaultSprite = Frames(centre)[0];
                tile.m_DefaultColliderType = solid ? UnityEngine.Tilemaps.Tile.ColliderType.Grid : UnityEngine.Tilemaps.Tile.ColliderType.None;
                tile.m_TilingRules = new List<RuleTile.TilingRule>();
                int id = 0;
                foreach (var (at, want, part) in Rules(a.pairs))
                    tile.m_TilingRules.Add(Rule(id++, at, want, Frames(part), solid));
                // The centre last, as its own rule, so an animated centre ripples too.
                tile.m_TilingRules.Add(Rule(id, Array.Empty<Vector3Int>(), Array.Empty<int>(), Frames(centre), solid));
            });
        }

        static RuleTile.TilingRule Rule(int id, Vector3Int[] at, int[] want, Sprite[] frames, bool solid) => new()
        {
            m_Id = id,
            m_NeighborPositions = new List<Vector3Int>(at),
            m_Neighbors = new List<int>(want),
            m_Sprites = frames,
            m_Output = frames.Length > 1 ? RuleTile.TilingRuleOutput.OutputSprite.Animation : RuleTile.TilingRuleOutput.OutputSprite.Single,
            m_MinAnimationSpeed = 1f / k_WaterFrameSeconds,
            m_MaxAnimationSpeed = 1f / k_WaterFrameSeconds,
            m_ColliderType = solid ? UnityEngine.Tilemaps.Tile.ColliderType.Grid : UnityEngine.Tilemaps.Tile.ColliderType.None,
            m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
        };

        /// <summary>A plain meadow tile (one Forgotten Plains cell, no collider): the flat grass, a sparse tuft, the script's denser ones.</summary>
        public static Tile Grass(string cell) => LookTestContent.CreateOrUpdate<Tile>($"{EditorPaths.Tiles}/Kariaston_{cell}.asset", tile =>
        {
            tile.sprite = MinifantasyImporter.Sprite(KariastonSheets.PlainsPack, KariastonSheets.Tiles, cell)
                          ?? throw new InvalidOperationException($"Kariaston: no grass cell {cell}.");
            tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
        });

        /// <summary>The name of the meadow cell the script picked (column, row), or the flat grass for (-1, -1).</summary>
        public static string GrassCell(int column, int row) => column < 0 ? "GrassFlat" : row == 1 ? $"Grass{column - 1}" : $"Sparse_{column}_{row}";
    }
}
