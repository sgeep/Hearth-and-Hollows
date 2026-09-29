using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Enforces pixel-art import settings (CLAUDE.md) for every texture under Assets/_Project/Art:
    /// sprite, 32 PPU, point filtering, no compression, no mipmaps. Characters (files named
    /// *_Char_*) pivot at the feet; everything else pivots at the centre.
    /// </summary>
    public sealed class PixelArtImportPostprocessor : AssetPostprocessor
    {
        public const int PixelsPerUnit = 32;
        const string k_ArtRoot = EditorPaths.Root + "/Art/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(k_ArtRoot)) return;
            var importer = (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = assetPath.Contains("_Char_") ? (int)SpriteAlignment.BottomCenter : (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = true;
            importer.SetTextureSettings(settings);
        }
    }
}
