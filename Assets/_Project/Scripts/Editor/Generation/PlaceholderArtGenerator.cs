using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Generates clearly named placeholder sprites (PH_*) until real pixel art exists.
    /// Character sprites are white-ish so SpriteRenderer tint sets their colour, and each has a
    /// dark "eye" pixel so facing is readable. Replace a PH_ file with real art of the same name
    /// and prefabs pick it up automatically.
    /// </summary>
    public static class PlaceholderArtGenerator
    {
        public const string Player = "PH_Char_Player";
        public const string Rat = "PH_Char_Rat";
        public const string Slime = "PH_Char_Slime";
        public const string Shroom = "PH_Char_Shroom";
        public const string Dummy = "PH_Char_Dummy";
        public const string TileSolid = "PH_Tile_Solid";
        public const string TileOneWay = "PH_Tile_OneWay";
        public const string Pickup = "PH_Pickup";
        public const string Pip = "PH_Pip";
        public const string Exclaim = "PH_Exclaim";
        public const string ExclaimHeavy = "PH_ExclaimHeavy";
        public const string CleanKill = "PH_CleanKill";
        public const string Marker = "PH_Marker";
        public const string Spore = "PH_Spore";
        public const string Slash = "PH_Slash";

        static readonly Color32 Clear = new(0, 0, 0, 0);
        static readonly Color32 White = new(235, 235, 235, 255);
        static readonly Color32 Outline = new(40, 36, 44, 255);
        static readonly Color32 Eye = new(20, 18, 24, 255);

        [MenuItem("Hearthdelve/Generate/Placeholder Art", priority = 100)]
        public static void Generate()
        {
            EditorPaths.Ensure(EditorPaths.Placeholders);

            Write(Player, 20, 46, (x, y, w, h) => Outlined(x, y, w, h, (px, py) => true, eye: (13, 38)));
            Write(Rat, 34, 18, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => py < 13 || (px > 22 && px < 28), eye: (27, 10)));
            Write(Slime, 28, 20, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => py < 8 || Ellipse(px, py - 8, w / 2f, 12f, w / 2f), eye: (19, 11)));
            Write(Shroom, 22, 34, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => (py >= 20 && Ellipse(px, py - 20, w / 2f, 14f, w / 2f)) || (py < 20 && px >= 6 && px < 16), eye: (13, 15)));
            Write(Dummy, 20, 44, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => (px >= 7 && px < 13) || (py >= 28 && py < 34), eye: null));

            Write(TileSolid, 32, 32, (x, y, w, h) =>
            {
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                bool mortar = y == 15 || (x == 15 && y > 15) || (x == 7 && y < 15) || (x == 23 && y < 15);
                return edge ? new Color32(48, 44, 56, 255) : mortar ? new Color32(70, 66, 80, 255) : new Color32(92, 88, 104, 255);
            });
            Write(TileOneWay, 32, 32, (x, y, w, h) =>
            {
                if (y < h - 6) return Clear;
                bool edge = y == h - 6 || y == h - 1 || x == 0 || x == w - 1;
                return edge ? new Color32(86, 58, 34, 255) : new Color32(150, 104, 62, 255);
            });

            Write(Pickup, 12, 12, (x, y, w, h) => Outlined(x, y, w, h, (px, py) => !IsCorner(px, py, w, h), eye: null));
            Write(Pip, 4, 4, (x, y, w, h) => White);
            Write(Exclaim, 8, 16, (x, y, w, h) =>
            {
                bool bar = x >= 2 && x <= 5 && y >= 5;
                bool dot = x >= 2 && x <= 5 && y <= 2;
                bool inner = (x >= 3 && x <= 4) && ((y >= 6 && y <= 14) || y == 1);
                if (inner) return new Color32(255, 214, 64, 255);
                return bar || dot ? Outline : Clear;
            });
            // Double "!!" in red-orange: the uninterruptible (super-armor) wind-up.
            Write(ExclaimHeavy, 14, 16, (x, y, w, h) =>
            {
                int lx = x < 7 ? x : x - 7;
                bool bar = lx >= 1 && lx <= 4 && y >= 5;
                bool dot = lx >= 1 && lx <= 4 && y <= 2;
                bool inner = lx >= 2 && lx <= 3 && ((y >= 6 && y <= 14) || y == 1);
                if (inner) return new Color32(255, 110, 70, 255);
                return bar || dot ? Outline : Clear;
            });
            // Check mark in green: "a light hit here is a Clean Kill".
            Write(CleanKill, 12, 10, (x, y, w, h) =>
            {
                bool Mark(int px, int py) => (px >= 1 && px <= 4 && py == 6 - px) || (px >= 4 && px <= 10 && py == px - 3);
                bool Thick(int px, int py) => Mark(px, py) || Mark(px, py - 1);
                if (Thick(x, y)) return new Color32(120, 235, 120, 255);
                if (Thick(x - 1, y) || Thick(x + 1, y) || Thick(x, y - 1) || Thick(x, y + 1)) return Outline;
                return Clear;
            });
            Write(Marker, 40, 6, (x, y, w, h) =>
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                float d = dx * dx + dy * dy;
                return d <= 1f && d >= 0.45f ? new Color32(255, 90, 70, 220) : Clear;
            });
            Write(Spore, 10, 10, (x, y, w, h) =>
            {
                float dx = x + 0.5f - w / 2f, dy = y + 0.5f - h / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 5f) return Clear;
                return d > 4f ? new Color32(60, 40, 70, 255) : new Color32(170, 120, 210, 255);
            });
            Write(Slash, 32, 32, (x, y, w, h) =>
            {
                float dx = x + 0.5f, dy = y + 0.5f - h / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < 20f || d > 31f) return Clear;
                byte a = (byte)(d > 28f ? 230 : 120);
                return new Color32(255, 255, 255, a);
            });

            AssetDatabase.Refresh();
        }

        public static Sprite Load(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{EditorPaths.Placeholders}/{name}.png");

        /// <summary>Only writes files that don't exist, so real art dropped in under the same name is kept.</summary>
        static void Write(string name, int width, int height, Func<int, int, int, int, Color32> pixel)
        {
            string path = $"{EditorPaths.Placeholders}/{name}.png";
            if (File.Exists(path)) return;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                pixels[y * width + x] = pixel(x, y, width, height);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>Fills a shape in white with a 1 px dark outline and an optional eye.</summary>
        static Color32 Outlined(int x, int y, int w, int h, Func<int, int, bool> inside, (int x, int y)? eye)
        {
            bool In(int px, int py) => px >= 0 && py >= 0 && px < w && py < h && inside(px, py);
            if (!In(x, y)) return Clear;
            if (eye.HasValue && (x == eye.Value.x || x == eye.Value.x + 1) && (y == eye.Value.y || y == eye.Value.y + 1)) return Eye;
            bool edge = !In(x - 1, y) || !In(x + 1, y) || !In(x, y - 1) || !In(x, y + 1);
            return edge ? Outline : White;
        }

        static bool Ellipse(int x, int y, float cx, float ry, float rx)
        {
            float dx = (x + 0.5f - cx) / rx, dy = y / ry;
            return dx * dx + dy * dy <= 1f;
        }

        static bool IsCorner(int x, int y, int w, int h) =>
            (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
    }
}
