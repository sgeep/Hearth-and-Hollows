using UnityEngine;

namespace Hearthdelve.Core
{
    /// <summary>Physics layer names/indices (configured by Hearthdelve/Setup/Configure Project).</summary>
    public static class Layers
    {
        public const string Ground = "Ground";
        public const string OneWayPlatform = "OneWayPlatform";
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Pickup = "Pickup";
        public const string Projectile = "Projectile";

        /// <summary>Index each layer is assigned to in the TagManager.</summary>
        public static readonly (string name, int index)[] All =
        {
            (Ground, 6),
            (OneWayPlatform, 7),
            (Player, 8),
            (Enemy, 9),
            (Pickup, 10),
            (Projectile, 11),
        };

        public static LayerMask Mask(params string[] names) => LayerMask.GetMask(names);
    }

    /// <summary>2D sorting layers, back to front.</summary>
    public static class SortingLayers
    {
        public const string Background = "Background";
        public const string Level = "Level";
        public const string Pickups = "Pickups";
        public const string Enemies = "Enemies";
        public const string Player = "Player";
        public const string FX = "FX";
        public const string Foreground = "Foreground";

        public static readonly string[] Ordered = { Background, Level, Pickups, Enemies, Player, FX, Foreground };
    }
}
