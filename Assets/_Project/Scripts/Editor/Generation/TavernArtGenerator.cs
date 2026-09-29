using UnityEngine;
using static Hearthdelve.Editor.PlaceholderArtGenerator;

namespace Hearthdelve.Editor
{
    /// <summary>Placeholder tavern sprites (PH_*). Like Phase 1, existing files are never overwritten.</summary>
    public static class TavernArtGenerator
    {
        public const string Villager = "PH_Char_Villager";
        public const string Adventurer = "PH_Char_Adventurer";
        public const string Dwarf = "PH_Char_Dwarf";
        public const string Grill = "PH_Station_Grill";
        public const string Tap = "PH_Station_Tap";
        public const string Pass = "PH_Station_Pass";
        public const string Table = "PH_Table";
        public const string Stool = "PH_Stool";
        public const string Door = "PH_Door";
        public const string Plate = "PH_Plate";
        public const string Bar = "PH_Bar";
        public const string Working = "PH_Working";
        public const string FloorTile = "PH_Tile_Floor";
        public const string WallTile = "PH_Tile_Wall";

        public static void Generate()
        {
            // Customers: white bodies (tinted per profile) with an eye so facing reads.
            Write(Villager, 18, 42, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => py < 32 || (px >= 3 && px < 15), eye: (12, 35)));
            Write(Adventurer, 20, 46, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => (px >= 2 && px < 18) || (px < 4 && py > 20 && py < 36), eye: (14, 38)));
            Write(Dwarf, 22, 32, (x, y, w, h) => Outlined(x, y, w, h,
                (px, py) => py < 26 || (px >= 4 && px < 18), eye: (15, 25)));

            Write(Grill, 48, 36, (x, y, w, h) =>
            {
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                if (y >= h - 6) return (x % 6 == 0) ? new Color32(40, 36, 40, 255) : new Color32(90, 80, 80, 255); // grate
                if (y < 10 && x > 16 && x < 32) return new Color32(230, 110, 40, 255);                            // fire
                return edge ? Outline : new Color32(70, 64, 70, 255);
            });
            Write(Tap, 30, 52, (x, y, w, h) =>
            {
                bool keg = y < 34 && Ellipse(x, y - 17, w / 2f, 17f, w / 2f);
                bool spout = y >= 34 && x >= 13 && x < 17;
                if (!keg && !spout) return Clear;
                bool hoop = keg && (y == 8 || y == 26);
                return hoop ? Outline : spout ? new Color32(190, 170, 90, 255) : new Color32(140, 92, 52, 255);
            });
            Write(Pass, 48, 30, (x, y, w, h) =>
            {
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                if (y >= h - 5) return new Color32(200, 190, 170, 255);
                return edge ? Outline : new Color32(120, 84, 52, 255);
            });
            Write(Table, 64, 26, (x, y, w, h) =>
            {
                bool top = y >= h - 5;
                bool leg = y < h - 5 && (x is >= 6 and < 11 || x is >= 53 and < 58);
                if (!top && !leg) return Clear;
                return y == h - 1 || y == h - 5 ? new Color32(80, 52, 30, 255) : new Color32(150, 100, 58, 255);
            });
            Write(Stool, 14, 14, (x, y, w, h) =>
            {
                bool seat = y >= h - 3;
                bool leg = y < h - 3 && (x is 2 or 3 or 10 or 11);
                return seat || leg ? new Color32(120, 80, 46, 255) : Clear;
            });
            Write(Door, 32, 64, (x, y, w, h) =>
            {
                bool frame = x < 3 || x >= w - 3 || y >= h - 3;
                bool plank = x % 8 == 0;
                bool knob = x == 24 && y is 30 or 31;
                if (knob) return new Color32(220, 190, 90, 255);
                return frame ? new Color32(60, 40, 26, 255) : plank ? new Color32(96, 64, 40, 255) : new Color32(120, 80, 50, 255);
            });
            Write(Plate, 14, 8, (x, y, w, h) =>
            {
                float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
                float d = dx * dx + dy * dy;
                return d > 1f ? Clear : d > 0.55f ? White : new Color32(255, 255, 255, 255);
            });
            Write(Bar, 4, 4, (x, y, w, h) => White);
            Write(Working, 14, 6, (x, y, w, h) =>
            {
                bool dot = (x % 5) < 3 && y >= 1 && y <= 3 && x < 13;
                return dot ? White : Clear;
            });
            Write(FloorTile, 32, 32, (x, y, w, h) =>
            {
                bool seam = y == h - 1 || x == 0 || (y == 15 && x % 16 != 0);
                return seam ? new Color32(70, 46, 30, 255) : new Color32(112, 76, 48, 255);
            });
            Write(WallTile, 32, 32, (x, y, w, h) =>
            {
                bool beam = x == 0 || x == 1;
                return beam ? new Color32(80, 56, 38, 255) : new Color32(150, 128, 100, 255);
            });
        }
    }
}
