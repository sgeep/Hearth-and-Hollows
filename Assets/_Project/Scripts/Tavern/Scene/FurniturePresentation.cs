using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// What placed furniture is drawn with besides its own art: the materials (lit for the room, unlit for overlays),
    /// the gold highlight corners and marker shown on a target, the pixel used for the stew pot's bar, and the seat
    /// highlight's frame. Generated with the furniture (4f step 1); one asset for every area.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Furniture Presentation", fileName = "FurniturePresentation")]
    public sealed class FurniturePresentation : ScriptableObject
    {
        [Tooltip("URP's lit sprite material (every room sprite uses it; CLAUDE.md, Lighting).")]
        public Material litMaterial;
        [Tooltip("Overlays (highlights, status bars) stay readable whatever the light.")]
        public Material overlayMaterial;

        [Header("Highlight")]
        public Sprite cornerTopLeft;
        public Sprite cornerTopRight;
        public Sprite cornerBottomLeft;
        public Sprite cornerBottomRight;
        public Sprite marker;
        public Color highlightColor = new(1f, 0.82f, 0.3f);
        [Tooltip("Around a seat and its sitter, relative to the seat.")]
        public Rect seatHighlight = new(-0.5f, -0.1f, 1f, 1.85f);

        [Header("Station overlays")]
        public Sprite pixel;
    }
}
