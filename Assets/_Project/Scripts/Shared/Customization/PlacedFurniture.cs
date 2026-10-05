using System;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// One piece standing in an area (4f plan §2): which definition, the footprint's bottom-left cell, its quarter
    /// turns (0 = facing the camera, counter-clockwise), whether it's mirrored, its nudge in art pixels (D1), and the
    /// piece it sits on (and which of its surface anchors) if it's a Surface item: it then stands where the anchor is,
    /// and moves, turns and goes to storage with its host. Plain data: layouts and saves hold these.
    /// </summary>
    [Serializable]
    public sealed class PlacedFurniture
    {
        [Tooltip("Unique within the save (Surface items name their host by it).")]
        public int uid;
        public string definition;
        public Vector2Int cell;
        [Range(0, 3)] public int turns;
        public bool flipped;
        [Tooltip("Art pixels, -4..+3 per axis: quarter-tile steps for decor, the exact 4e positions in the starting layout.")]
        public Vector2Int nudge;
        [Tooltip("uid of the piece this sits on (Surface items); -1 when on the floor or wall.")]
        public int host = -1;
        [Tooltip("Which of the host's surface anchors it sits on (Surface items).")]
        public int anchor;

        public PlacedFurniture Clone() => (PlacedFurniture)MemberwiseClone();
    }
}
