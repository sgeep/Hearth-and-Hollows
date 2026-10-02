using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Copies the images listed in <see cref="MinifantasySheets"/> from the raw packs
    /// (outside the repo) into Assets/ThirdParty/Minifantasy/&lt;Pack&gt;/. The raw packs are only
    /// read. Slicing and import settings are applied by <see cref="MinifantasyImportPostprocessor"/>.
    /// </summary>
    public static class MinifantasyImporter
    {
        [MenuItem("Hearthdelve/Art/Import Minifantasy", priority = 100)]
        public static void ImportAll()
        {
            int copied = 0, missing = 0;
            foreach (Sheet sheet in MinifantasySheets.All)
            {
                string source = Path.Combine(EditorPaths.MinifantasySource, sheet.Source);
                if (!File.Exists(source))
                {
                    // On a machine without the raw packs, the copies already in the repo are used.
                    if (!File.Exists(sheet.AssetPath))
                    {
                        Debug.LogError($"[Hearthdelve] Minifantasy source not found: {source}");
                        missing++;
                    }
                    continue;
                }

                EditorPaths.Ensure(Path.GetDirectoryName(sheet.AssetPath)?.Replace('\\', '/'));
                if (!File.Exists(sheet.AssetPath) || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(sheet.AssetPath)))
                {
                    File.Copy(source, sheet.AssetPath, overwrite: true);
                    copied++;
                }
                AssetDatabase.ImportAsset(sheet.AssetPath, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Hearthdelve] Minifantasy import: {MinifantasySheets.All.Count} sheets, {copied} copied, {missing} missing.");
        }

        /// <summary>All sprites of an imported sheet, by name.</summary>
        public static Dictionary<string, Sprite> Sprites(string pack, string file)
        {
            string path = $"{EditorPaths.Minifantasy}/{pack}/{file}.png";
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name, s => s);
        }

        /// <summary>One sprite cut by name (<see cref="SliceMode.Rects"/>) from a sheet.</summary>
        public static Sprite Sprite(string pack, string file, string name)
        {
            Sprites(pack, file).TryGetValue($"{file}_{name}", out Sprite sprite);
            if (sprite == null) Debug.LogError($"[Hearthdelve] Sprite '{file}_{name}' not found in {pack}/{file}.png.");
            return sprite;
        }

        /// <summary>One cell of a grid sheet; row 0 is the top row.</summary>
        public static Sprite Cell(string pack, string file, int column, int row) => Sprite(pack, file, $"{column}_{row}");

        /// <summary>A row of a grid sheet, left to right, as animation frames.</summary>
        public static Sprite[] Row(string pack, string file, int row, int count)
        {
            Dictionary<string, Sprite> sprites = Sprites(pack, file);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) sprites.TryGetValue($"{file}_{i}_{row}", out frames[i]);
            if (frames.Any(f => f == null)) Debug.LogError($"[Hearthdelve] Row {row} of {pack}/{file}.png has fewer than {count} frames.");
            return frames;
        }
    }
}
