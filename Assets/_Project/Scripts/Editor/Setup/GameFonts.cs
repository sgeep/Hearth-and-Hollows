using System.Collections.Generic;
using System.Linq;
using Hearthdelve.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The game's text font: <b>Silver</b> by Poppy Works (chosen 2026-10-03, replacing m5x7), a pixel font on a
    /// 1900-unit em whose pixels are 100 units, so it is pixel-exact at size 19 (one font pixel per game pixel at
    /// 320×180) and at 38. Capitals and ascenders are 9 pixels, descenders 2. Imported as hinted raster at 19 and
    /// rasterised by Super Text Mesh at 19, point filtered, on a 12-pixel line. Any other size drops or doubles pixel
    /// rows, so text is only ever <see cref="Body"/> (1×) or <see cref="Large"/> (2×).
    /// </summary>
    public static class GameFonts
    {
        public const string FontPath = "Assets/_Project/Fonts/silver/Silver.ttf";
        public const string FontName = "Silver";
        /// <summary>The font's native pixel size (its em in pixels).</summary>
        public const int Native = 19;
        public const float Body = Native;
        public const float Large = Native * 2;
        /// <summary>A line in game pixels: 9-pixel capitals, 2-pixel descenders and a pixel between lines.</summary>
        public const int LinePixels = 12;
        /// <summary>Super Text Mesh's line height is the size times this.</summary>
        public const float LineSpacing = (float)LinePixels / Native;
        /// <summary>
        /// Super Text Mesh drops the first line by the full size (19), not the spaced line: lifting every glyph by 9/19 of
        /// the size puts the first baseline 10 pixels under the box's top (capitals a pixel below it, descenders to its
        /// line's bottom), like the lines after it.
        /// </summary>
        public const float BaseLift = (float)(Native - 10) / Native;

        public static Font Load() => AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        /// <summary>Imports the font as a pixel font: hinted raster at its native size, its own data only (no OS fallback names).</summary>
        public static void ConfigureImporter()
        {
            if (AssetImporter.GetAtPath(FontPath) is not TrueTypeFontImporter importer) return;
            bool changed = importer.fontSize != Native || importer.fontRenderingMode != FontRenderingMode.HintedRaster
                || importer.fontTextureCase != FontTextureCase.Dynamic || !importer.includeFontData
                || importer.fontNames == null || importer.fontNames.Length != 1 || importer.fontNames[0] != FontName
                || (importer.fontReferences != null && importer.fontReferences.Length > 0);
            if (!changed) return;
            importer.fontSize = Native;
            importer.fontRenderingMode = FontRenderingMode.HintedRaster;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.includeFontData = true;
            importer.fontNames = new[] { FontName };
            importer.fontReferences = new Font[0];
            importer.SaveAndReimport();
        }

        /// <summary>Text built with a requested size of this or more is large (2×); smaller is body text.</summary>
        public const float LargeRequest = 10f;

        /// <summary>Existing text drawn large when retrofitted: the game's name on the main menu and the transition caption.</summary>
        static bool IsLarge(SuperTextMesh text) =>
            text.name == "Caption" || (text.name == "Title" && text.transform.parent != null && text.transform.parent.name == "Menu");

        /// <summary>Sets a text up to draw the font crisply, at body (1×) or large (2×) size.</summary>
        public static void Apply(SuperTextMesh text, bool large)
        {
            Font font = Load();
            if (font == null || text == null) return;
            text.font = font;
            text.size = large ? Large : Body;
            text.quality = Native;
            text.autoQuality = false;
            text.filterMode = FilterMode.Point;
            text.lineSpacing = LineSpacing;
            text.relativeBaseOffset = true;
            text.baseOffset = new Vector3(0f, BaseLift, 0f);
            EditorUtility.SetDirty(text);
        }

        /// <summary>
        /// The open scene's text and canvases, in place: every Super Text Mesh gets the font, and every screen-space canvas
        /// scales by whole pixels. For text built before the font (screens that are rebuilt get it as they're built).
        /// </summary>
        public static void ApplyToOpenScene()
        {
            foreach (SuperTextMesh text in Object.FindObjectsByType<SuperTextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(text, IsLarge(text));
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace) PixelScale(canvas);
        }

        /// <summary>The canvas scales by the same whole number as the pixel-perfect camera (never fractionally).</summary>
        public static void PixelScale(Canvas canvas)
        {
            if (canvas.GetComponent<CanvasScaler>() == null) return;
            if (canvas.GetComponent<PixelCanvasScaler>() == null) canvas.gameObject.AddComponent<PixelCanvasScaler>();
            EditorUtility.SetDirty(canvas.gameObject);
        }
    }
}
