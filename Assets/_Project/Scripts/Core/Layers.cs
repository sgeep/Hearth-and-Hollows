using UnityEngine;

namespace Hearthdelve.Core
{
    /// <summary>
    /// Physics layer names/indices (configured by Hearthdelve/Setup/Configure Project). The TDE
    /// layers keep TDE's own indices, because its <c>LayerManager</c> hard-codes them; ours go
    /// in the slots TDE leaves free.
    /// </summary>
    public static class Layers
    {
        // TDE's layers.
        public const string Obstacles = "Obstacles";
        public const string Ground = "Ground";
        public const string Player = "Player";
        public const string Enemies = "Enemies";
        public const string NoCollisions = "NoCollisions";
        public const string Projectile = "Projectile";

        // Ours.
        public const string Pickup = "Pickup";

        /// <summary>Index each layer is assigned to in the TagManager.</summary>
        public static readonly (string name, int index)[] All =
        {
            (Pickup, 6),
            (Obstacles, 8),
            (Ground, 9),
            (Player, 10),
            (Enemies, 13),
            (NoCollisions, 14),
            (Projectile, 18),
        };

        public static LayerMask Mask(params string[] names) => LayerMask.GetMask(names);
    }

    /// <summary>
    /// 2D sorting layers, back to front (TDE's names). Everything that should sort by Y
    /// against characters (walls with height, props, pickups, characters) shares
    /// <see cref="YSorted"/>; the custom sort axis (0, 1, 0) orders sprites inside it.
    /// </summary>
    public static class SortingLayers
    {
        public const string Background = "Background";
        public const string Floor = "Ground";
        public const string YSorted = "Characters";
        public const string Above = "Above";

        public static readonly string[] Ordered = { Background, Floor, YSorted, Above };
    }
}
