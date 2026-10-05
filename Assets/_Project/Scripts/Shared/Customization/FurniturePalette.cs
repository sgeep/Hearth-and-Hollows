using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// One recolourable part of a piece (D11): which kind of ramp it takes (wood, cushion, pot…) and the colours of that
    /// part as drawn, darkest to lightest. Only those exact colours change, so outlines and other parts keep theirs.
    /// </summary>
    [Serializable]
    public sealed class PaletteChannel
    {
        [Tooltip("The kind of ramp it takes: ramps of the same kind are offered for it.")]
        public string kind;
        [Tooltip("The part's colours as drawn (any order: they're matched by brightness).")]
        public Color32[] source = Array.Empty<Color32>();
    }

    /// <summary>A ramp of colours for one kind of part, taken from Minifantasy's own colourways so recolours look like Minifantasy art.</summary>
    [Serializable]
    public sealed class PaletteRamp
    {
        public string id;
        public string kind;
        [Tooltip("Localization key (UI table) of its name.")]
        public string nameKey;
        public Color32[] colors = Array.Empty<Color32>();

        /// <summary>The colour the swatch shows: the ramp's middle.</summary>
        public Color32 Swatch => colors.Length == 0 ? new Color32(255, 255, 255, 255) : FurniturePalette.ByBrightness(colors)[colors.Length / 2];
    }

    [Serializable]
    public sealed class PalettePick
    {
        public string kind;
        public string ramp;
    }

    /// <summary>A named scheme setting several channels at once ("dwarven slate").</summary>
    [Serializable]
    public sealed class PalettePreset
    {
        public string id;
        public string nameKey;
        public List<PalettePick> picks = new();
    }

    /// <summary>
    /// Palette choices and the colour remap (D11). A placed piece keeps its choices as text, one ramp per channel kind
    /// ("cushion=teal;wood=walnut"); the remap maps each channel's drawn colours onto the chosen ramp by brightness rank,
    /// so shading survives. Pure logic, EditMode-tested; the textures are baked by <see cref="FurnitureRecolour"/>.
    /// </summary>
    public static class FurniturePalette
    {
        /// <summary>The choices in a palette string, by channel kind.</summary>
        public static Dictionary<string, string> Parse(string palette)
        {
            var picks = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(palette)) return picks;
            foreach (string part in palette.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0 || eq == part.Length - 1) continue;
                picks[part.Substring(0, eq).Trim()] = part.Substring(eq + 1).Trim();
            }
            return picks;
        }

        /// <summary>A palette string, channels in order (so equal choices are equal strings: cache keys and saves match).</summary>
        public static string Format(IDictionary<string, string> picks)
        {
            if (picks == null || picks.Count == 0) return string.Empty;
            var kinds = new List<string>(picks.Keys);
            kinds.Sort(string.CompareOrdinal);
            var text = new StringBuilder();
            foreach (string kind in kinds)
            {
                if (string.IsNullOrEmpty(picks[kind])) continue;
                if (text.Length > 0) text.Append(';');
                text.Append(kind).Append('=').Append(picks[kind]);
            }
            return text.ToString();
        }

        /// <summary>The palette with one channel's ramp changed (null or empty: back to as drawn).</summary>
        public static string With(string palette, string kind, string ramp)
        {
            Dictionary<string, string> picks = Parse(palette);
            if (string.IsNullOrEmpty(ramp)) picks.Remove(kind);
            else picks[kind] = ramp;
            return Format(picks);
        }

        /// <summary>Only the choices a piece with these channels can use (copying colours between different pieces).</summary>
        public static string Restrict(string palette, IReadOnlyList<PaletteChannel> channels)
        {
            Dictionary<string, string> picks = Parse(palette);
            var kept = new Dictionary<string, string>();
            foreach (PaletteChannel c in channels)
                if (c != null && picks.TryGetValue(c.kind, out string ramp)) kept[c.kind] = ramp;
            return Format(kept);
        }

        /// <summary>What a cached recolour is filed under: the drawing and the choices that apply to it.</summary>
        public static string CacheKey(string sprite, string palette) => string.IsNullOrEmpty(palette) ? sprite : sprite + "|" + palette;

        public static float Brightness(Color32 c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

        /// <summary>A copy of the ramp, darkest first (ties by the colour's value, so the order never depends on input order).</summary>
        public static Color32[] ByBrightness(Color32[] ramp)
        {
            var sorted = (Color32[])ramp.Clone();
            Array.Sort(sorted, (a, b) =>
            {
                int c = Brightness(a).CompareTo(Brightness(b));
                return c != 0 ? c : Pack(a).CompareTo(Pack(b));
            });
            return sorted;
        }

        static int Pack(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        /// <summary>
        /// Each drawn colour of each channel → its colour in the chosen ramp: the i-th darkest of n goes to the colour at the
        /// same relative rank in the ramp (round(i·(m−1)/(n−1)) of m). Keyed by RGB; alpha is kept from the pixel.
        /// </summary>
        public static Dictionary<int, Color32> Mapping(IReadOnlyList<(Color32[] from, Color32[] to)> channels)
        {
            var map = new Dictionary<int, Color32>();
            if (channels == null) return map;
            foreach (var (from, to) in channels)
            {
                if (from == null || to == null || from.Length == 0 || to.Length == 0) continue;
                Color32[] a = ByBrightness(from), b = ByBrightness(to);
                for (int i = 0; i < a.Length; i++)
                {
                    int j = a.Length == 1 ? b.Length / 2 : Mathf.RoundToInt(i * (b.Length - 1) / (float)(a.Length - 1));
                    map[Pack(a[i])] = b[j];
                }
            }
            return map;
        }

        /// <summary>The pixels with every mapped colour replaced (transparent pixels and unmapped colours are kept).</summary>
        public static Color32[] Remap(Color32[] pixels, Dictionary<int, Color32> mapping)
        {
            var result = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                if (p.a != 0 && mapping.TryGetValue(Pack(p), out Color32 to)) p = new Color32(to.r, to.g, to.b, p.a);
                result[i] = p;
            }
            return result;
        }

        /// <summary>The (drawn colours, ramp) pairs for a piece's channels under a palette string.</summary>
        public static List<(Color32[] from, Color32[] to)> Channels(IReadOnlyList<PaletteChannel> channels, string palette, Func<string, PaletteRamp> ramp)
        {
            var pairs = new List<(Color32[], Color32[])>();
            if (channels == null || channels.Count == 0 || string.IsNullOrEmpty(palette)) return pairs;
            Dictionary<string, string> picks = Parse(palette);
            foreach (PaletteChannel c in channels)
            {
                if (c == null || !picks.TryGetValue(c.kind, out string id)) continue;
                PaletteRamp r = ramp(id);
                if (r != null && r.kind == c.kind) pairs.Add((c.source, r.colors));
            }
            return pairs;
        }
    }
}
