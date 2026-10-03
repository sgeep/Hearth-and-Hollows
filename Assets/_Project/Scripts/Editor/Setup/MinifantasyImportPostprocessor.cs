using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The Minifantasy import pipeline (CLAUDE.md): every texture under
    /// Assets/ThirdParty/Minifantasy is a sprite at 8 PPU with point filtering, no compression
    /// and no mipmaps, sliced as its <see cref="Sheet"/> says. Sprite ids are derived from the
    /// sprite names, so re-importing never breaks references.
    /// </summary>
    public sealed class MinifantasyImportPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(EditorPaths.Minifantasy + "/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            Sheet sheet = MinifantasySheets.Find(assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = sheet == null || sheet.Mode == SliceMode.Single ? SpriteImportMode.Single : SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            if (sheet != null && sheet.Mode == SliceMode.Single)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = sheet.Pivot;
            }
            importer.SetTextureSettings(settings);

            if (sheet == null || sheet.Mode == SliceMode.Single) return;

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            List<SpriteRect> rects = Slice(sheet, width, height);
            provider.SetSpriteRects(rects.ToArray());

            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                var pairs = new List<SpriteNameFileIdPair>();
                foreach (SpriteRect rect in rects) pairs.Add(new SpriteNameFileIdPair(rect.name, rect.spriteID));
                names.SetNameFileIdPairs(pairs);
            }
            provider.Apply();
        }

        static List<SpriteRect> Slice(Sheet sheet, int width, int height)
        {
            var rects = new List<SpriteRect>();
            if (sheet.Mode == SliceMode.Grid)
            {
                int columns = width / sheet.Cell.x, rows = height / sheet.Cell.y;
                for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    // Row 0 is the top of the image; Unity's rects start at the bottom.
                    var rect = new Rect(column * sheet.Cell.x, height - (row + 1) * sheet.Cell.y, sheet.Cell.x, sheet.Cell.y);
                    rects.Add(Make($"{sheet.File}_{column}_{row}", rect, sheet.Pivot, Vector4.zero));
                }
            }
            else
            {
                foreach (SheetRect r in sheet.Rects)
                {
                    var rect = new Rect(r.Rect.x, height - r.Rect.y - r.Rect.height, r.Rect.width, r.Rect.height);
                    rects.Add(Make($"{sheet.File}_{r.Name}", rect, r.Pivot, r.Border));
                }
            }
            return rects;
        }

        static SpriteRect Make(string name, Rect rect, Vector2 pivot, Vector4 border) => new()
        {
            name = name,
            rect = rect,
            alignment = SpriteAlignment.Custom,
            pivot = pivot,
            border = border,
            spriteID = StableId(name),
        };

        /// <summary>The same sprite name always gets the same id, so references survive a re-slice.</summary>
        static GUID StableId(string name)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("hearthdelve-minifantasy:" + name));
            var hex = new StringBuilder(32);
            foreach (byte b in hash) hex.Append(b.ToString("x2"));
            return new GUID(hex.ToString());
        }
    }
}
