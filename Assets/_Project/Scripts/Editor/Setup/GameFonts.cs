using System.Collections.Generic;
using System.Linq;
using Hearthdelve.UI;
using Hearthdelve.UI.Typography;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The game's text font: <b>Silver</b> by Poppy Works (chosen 2026-10-03, replacing m5x7), a pixel font on a
    /// 1900-unit em whose pixels are 100 units, so it is pixel-exact at size 19 (one font pixel per game pixel at
    /// 320×180) and at 38. Capitals and ascenders are 9 pixels, descenders 2. Imported as hinted raster at 19 and
    /// rasterised by Super Text Mesh at 19, point filtered, on a 12-pixel line. Any size that isn't a whole multiple drops or
    /// doubles pixel rows, so the type scale (<see cref="TypeScale"/>, <see cref="Scale"/>) has 1×, 2× and 3× only.
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

        /// <summary>Text built with a requested size of this or more is a heading (2×); smaller is body text (old callers).</summary>
        public const float LargeRequest = 10f;

        /// <summary>The type scale: one asset, every style's size and line (the UI foundation pass after 4f).</summary>
        public const string ScalePath = "Assets/_Project/Data/UI/TypeScale.asset";

        /// <summary>The type scale, created with the default sizes the first time.</summary>
        public static TypeScale Scale()
        {
            var scale = AssetDatabase.LoadAssetAtPath<TypeScale>(ScalePath);
            if (scale != null) return scale;
            EditorPaths.Ensure("Assets/_Project/Data/UI");
            scale = ScriptableObject.CreateInstance<TypeScale>();
            AssetDatabase.CreateAsset(scale, ScalePath);
            AssetDatabase.SaveAssets();
            return scale;
        }

        /// <summary>A text with no style yet, from before styles: the game's name is Display, the transition caption a heading.</summary>
        static TextStyle Legacy(SuperTextMesh text)
        {
            if (text.name == "Title" && text.transform.parent != null && text.transform.parent.name == "Menu") return TextStyle.Display;
            if (text.name == "Caption") return TextStyle.Heading;
            return text.size >= Large - 0.5f ? TextStyle.Heading : TextStyle.Body;
        }

        /// <summary>Sets a text up as a style: the game font, the style's size and line from the type scale, drawn crisply.</summary>
        public static void Apply(SuperTextMesh text, TextStyle style)
        {
            Font font = Load();
            if (font == null || text == null) return;
            text.font = font;
            StyledText styled = text.GetComponent<StyledText>();
            if (styled == null) styled = text.gameObject.AddComponent<StyledText>();
            styled.Style = style;
            Scale().Apply(text, style);
            EditorUtility.SetDirty(styled);
            EditorUtility.SetDirty(text);
        }

        /// <summary>Old callers: body (1×) or large (a heading, 2×).</summary>
        public static void Apply(SuperTextMesh text, bool large) => Apply(text, large ? TextStyle.Heading : TextStyle.Body);

        /// <summary>
        /// The open scene's text and canvases, in place: every Super Text Mesh gets the font and its style's size from the type
        /// scale (so a change to the scale reaches every text through the updaters), and every screen-space canvas scales by
        /// whole pixels. For text built before the font (screens that are rebuilt get it as they're built).
        /// </summary>
        public static void ApplyToOpenScene()
        {
            foreach (SuperTextMesh text in Object.FindObjectsByType<SuperTextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                StyledText styled = text.GetComponent<StyledText>();
                Apply(text, styled != null ? styled.Style : Legacy(text));
            }
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
