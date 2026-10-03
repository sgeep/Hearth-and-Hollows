using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>"Press E to climb back to the tavern", while the player stands at the way out.</summary>
    public sealed class ExitHintView : MonoBehaviour
    {
        [SerializeField] GameObject m_Hint;
        [SerializeField] LocalizedSuperText m_Text;

        public bool IsShown => HintVisibility.IsShown(m_Hint);

        public void Configure(GameObject hint, LocalizedSuperText text)
        {
            m_Hint = hint;
            m_Text = text;
        }

        void Awake() => HintVisibility.Init(m_Hint);

        void OnEnable() => EventBus<DelveExitHint>.Subscribe(OnHint);
        void OnDisable() => EventBus<DelveExitHint>.Unsubscribe(OnHint);

        void OnHint(DelveExitHint hint)
        {
            if (m_Hint == null) return;
            HintVisibility.Set(m_Hint, hint.Visible);
            if (hint.Visible) m_Text?.Set(LoopLocKeys.HudExit, InputHints.Interact());
        }
    }
}
