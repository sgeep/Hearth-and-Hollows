using System.Collections.Generic;
using System.Linq;
using Hearthdelve.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The game's text font (locked 2026-10-03): <b>m5x7</b> by Daniel Linssen, a pixel font drawn on a 16-unit em.
    /// Imported as hinted raster at 16 and rasterised by Super Text Mesh at 16 (one font pixel per texel), point
    /// filtered so its pixels stay square: at size 16 one font pixel is one game pixel at 320×180 (measured in
    /// captures). Any other size drops or doubles pixel rows, so text is only ever <see cref="Body"/> (1×) or
    /// <see cref="Large"/> (2×).
    /// </summary>
    public static class GameFonts
    {
        public const string FontPath = "Assets/_Project/Fonts/m5x7/m5x7.ttf";
        /// <summary>The font's native pixel size.</summary>
        public const int Native = 16;
        public const float Body = Native;
        public const float Large = Native * 2;
        /// <summary>
        /// A line is 10 game pixels (7-pixel capitals, 2-pixel descenders, a pixel between lines); Super Text Mesh's
        /// line height is the size times this. The font's own metrics would give 16 and sit text low in its box.
        /// </summary>
        public const float LineSpacing = 0.625f;
        /// <summary>
        /// Super Text Mesh drops the first line by the full size (16), not the spaced line (10): lifting every glyph by
        /// 6/16 of the size puts the first baseline 10 pixels under the box's top, like the lines after it (measured).
        /// </summary>
        public const float BaseLift = 6f / Native;

        public static Font Load() => AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        /// <summary>Imports m5x7 as a pixel font: hinted raster at its native size, its own data only (no OS fallback names).</summary>
        public static void ConfigureImporter()
        {
            if (AssetImporter.GetAtPath(FontPath) is not TrueTypeFontImporter importer) return;
            bool changed = importer.fontSize != Native || importer.fontRenderingMode != FontRenderingMode.HintedRaster
                || importer.fontTextureCase != FontTextureCase.Dynamic || !importer.includeFontData
                || importer.fontNames == null || importer.fontNames.Length != 1 || importer.fontNames[0] != "m5x7"
                || (importer.fontReferences != null && importer.fontReferences.Length > 0);
            if (!changed) return;
            importer.fontSize = Native;
            importer.fontRenderingMode = FontRenderingMode.HintedRaster;
            importer.fontTextureCase = FontTextureCase.Dynamic;
            importer.includeFontData = true;
            importer.fontNames = new[] { "m5x7" };
            importer.fontReferences = new Font[0];
            importer.SaveAndReimport();
        }

        /// <summary>Text built with a requested size of this or more is large (2×); smaller is body text.</summary>
        public const float LargeRequest = 10f;

        /// <summary>Existing text drawn large when retrofitted: the game's name on the main menu and the transition caption.</summary>
        static bool IsLarge(SuperTextMesh text) =>
            text.name == "Caption" || (text.name == "Title" && text.transform.parent != null && text.transform.parent.name == "Menu");

        /// <summary>Sets a text up to draw m5x7 crisply, at body (1×) or large (2×) size.</summary>
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
        /// The open scene's text and canvases, in place: every Super Text Mesh gets m5x7, and every screen-space canvas
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
