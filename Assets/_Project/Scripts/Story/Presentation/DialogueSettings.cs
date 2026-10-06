using UnityEngine;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>
    /// How the dialogue box reads (4g Checkpoint B), in one place: the typewriter's speed and the continue mark's bob. The box reads it
    /// when each line starts, so a change in the editor applies to the next line.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Story/Dialogue Settings", fileName = "DialogueSettings")]
    public sealed class DialogueSettings : ScriptableObject
    {
        [Min(1f), Tooltip("How fast a line is revealed (characters a second). Confirm while it's revealing shows it whole.")]
        public float charactersPerSecond = 45f;
        [Min(0.05f), Tooltip("The ▼ bobs a pixel this often (seconds).")]
        public float continueBob = 0.35f;
    }
}
