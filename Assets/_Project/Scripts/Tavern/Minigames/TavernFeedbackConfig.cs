using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>The tavern's feedback tuning, as an asset (CLAUDE.md: tune in the editor).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Tavern Feedback Config", fileName = "TavernFeedbackConfig")]
    public sealed class TavernFeedbackConfig : ScriptableObject
    {
        public TavernFeedbackSettings feedback = TavernFeedbackSettings.Default;
    }
}
