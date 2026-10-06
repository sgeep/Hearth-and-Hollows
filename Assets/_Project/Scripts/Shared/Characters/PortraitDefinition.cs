using UnityEngine;

namespace Hearthdelve.Shared.Characters
{
    /// <summary>
    /// A dialogue portrait from the Minifantasy Portrait Generator (32×32, shown at 2×): the face at rest, the same face blinking,
    /// and its talking mouths. Made by <c>Tools/portraits/compose.py</c> from a recipe of the generator's own layers
    /// (docs/ASSET_MAP.md). Referenced from <see cref="CharacterDefinition"/>, read by the dialogue presenter.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Portrait", fileName = "Portrait_")]
    public sealed class PortraitDefinition : ScriptableObject
    {
        public Sprite still;
        [Tooltip("The same face with its eyes shut, shown briefly every few seconds.")]
        public Sprite blink;
        [Tooltip("Mouths while the line is being spoken, cycled in order.")]
        public Sprite[] talking = System.Array.Empty<Sprite>();
        [Min(0.02f), Tooltip("Seconds per talking frame.")]
        public float talkFrameSeconds = 0.12f;
        [Tooltip("Seconds between blinks: a random time in this range.")]
        public Vector2 blinkEvery = new(2.5f, 5f);
        [Min(0.02f)] public float blinkSeconds = 0.12f;

        /// <summary>The frame to show: talking cycles the mouths; otherwise the still face, or the blink while it lasts.</summary>
        public Sprite Frame(bool talking, float talkTime, bool blinking)
        {
            if (talking && this.talking != null && this.talking.Length > 0)
                return this.talking[Mathf.FloorToInt(Mathf.Max(0f, talkTime) / talkFrameSeconds) % this.talking.Length];
            return blinking && blink != null ? blink : still;
        }
    }
}
