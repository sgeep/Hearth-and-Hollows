using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.UI.Typography
{
    /// <summary>
    /// The game's type scale (the UI foundation pass after 4f): for each <see cref="TextStyle"/>, how many game pixels a font
    /// pixel is (a whole number: Silver is pixel-clean only at whole multiples of its native size on the 320×180 grid) and the
    /// line in game pixels. One asset (<c>Data/UI/TypeScale.asset</c>); the builders and updaters bake it into every text's
    /// Super Text Mesh settings, so changing a size here and running the updaters changes it everywhere.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/UI/Type Scale", fileName = "TypeScale")]
    public sealed class TypeScale : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public TextStyle style;
            [Min(1), Tooltip("Game pixels per font pixel: 1 is Silver's native size (19), 2 is 38, 3 is 57. Whole numbers only.")]
            public int scale;
            [Min(1), Tooltip("Line height in game pixels. 12 per scale step keeps a pixel between lines.")]
            public int linePixels;

            public Entry(TextStyle style, int scale, int linePixels)
            {
                this.style = style;
                this.scale = scale;
                this.linePixels = linePixels;
            }

            /// <summary>The Super Text Mesh size: the native size times the scale.</summary>
            public float Size => SilverMetrics.NativeSize * scale;
        }

        [SerializeField] List<Entry> m_Entries = new(Defaults);

        /// <summary>The scale as chosen: three sizes, every 1× role on the font's own 12-pixel line.</summary>
        public static Entry[] Defaults => new[]
        {
            new Entry(TextStyle.Body, 1, 12),
            new Entry(TextStyle.Secondary, 1, 12),
            new Entry(TextStyle.Prompt, 1, 12),
            new Entry(TextStyle.Heading, 2, 24),
            new Entry(TextStyle.Display, 3, 36),
        };

        public IReadOnlyList<Entry> Entries => m_Entries;

        /// <summary>The entry for a style (the default one if the asset lacks it).</summary>
        public Entry Get(TextStyle style)
        {
            foreach (Entry e in m_Entries)
                if (e.style == style) return e;
            foreach (Entry e in Defaults)
                if (e.style == style) return e;
            return new Entry(style, 1, SilverMetrics.LinePixels);
        }

        /// <summary>What's wrong with the scale, if anything: missing styles, sizes under native, lines too short for the glyphs.</summary>
        public List<string> Problems()
        {
            var problems = new List<string>();
            foreach (TextStyle style in Enum.GetValues(typeof(TextStyle)))
            {
                int count = 0;
                foreach (Entry e in m_Entries) if (e.style == style) count++;
                if (count == 0) problems.Add($"{style} has no entry");
                if (count > 1) problems.Add($"{style} has {count} entries");
            }
            foreach (Entry e in m_Entries)
            {
                if (e.scale < 1) problems.Add($"{e.style}: scale {e.scale} is under Silver's native size");
                // Capitals, descenders and nothing between lines is the least a line can be.
                int least = (SilverMetrics.CapHeight + SilverMetrics.Descender) * Math.Max(1, e.scale);
                if (e.linePixels < least) problems.Add($"{e.style}: a {e.linePixels}-pixel line clips {least}-pixel glyphs");
            }
            return problems;
        }

        /// <summary>
        /// Sets a text's size and line for its style, crisply: drawn at the font's native quality and point filtered, scaled by a
        /// whole number, the first baseline <see cref="SilverMetrics.BaselineFromTop"/> font pixels under the box's top.
        /// </summary>
        public static void Apply(SuperTextMesh text, Entry entry)
        {
            if (text == null) return;
            text.size = entry.Size;
            text.quality = SilverMetrics.NativeSize;
            text.autoQuality = false;
            text.filterMode = FilterMode.Point;
            // Super Text Mesh's line height is the size times its line spacing.
            text.lineSpacing = (float)entry.linePixels / entry.Size;
            // It drops the first line by the full size: lift every glyph so the first baseline is 10 font pixels down.
            text.relativeBaseOffset = true;
            text.baseOffset = new Vector3(0f, (float)(SilverMetrics.NativeSize - SilverMetrics.BaselineFromTop) / SilverMetrics.NativeSize, 0f);
        }

        public void Apply(SuperTextMesh text, TextStyle style) => Apply(text, Get(style));
    }
}
