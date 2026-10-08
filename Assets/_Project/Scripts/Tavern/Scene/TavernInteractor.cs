using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The player's side of tavern interactions: picks the nearest usable thing in reach
    /// (<see cref="InteractionRules"/>), highlights it, keeps the hint up to date, and uses it when
    /// Interact (Tavern map) is pressed. Does nothing while the Tavern map is off (a panel or menu is open).
    /// </summary>
    public sealed class TavernInteractor : MonoBehaviour
    {
        readonly List<InteractionCandidate> m_Candidates = new();
        readonly List<TavernInteractable> m_Targets = new();
        TavernInteractable m_Target;
        bool m_HintShown;
        TavernHint m_ShownHint;

        /// <summary>What Interact would use now, if anything.</summary>
        public TavernInteractable Target => m_Target;

        void Update()
        {
            var interact = InputMaps.Find(InputMaps.Tavern, TavernActions.Interact);
            bool active = interact != null && interact.enabled;
            SetTarget(active ? Choose() : null);
            if (active && m_Target != null && interact.WasPressedThisFrame()) m_Target.Use();
        }

        TavernInteractable Choose()
        {
            m_Candidates.Clear();
            m_Targets.Clear();
            int current = -1;
            // One candidate per spot a thing can be used from (the stove from the front or from behind).
            foreach (TavernInteractable t in TavernInteractable.All)
            foreach (Vector2 point in t.UsePoints)
            {
                if (t == m_Target && current < 0) current = m_Targets.Count;
                m_Targets.Add(t);
                m_Candidates.Add(new InteractionCandidate(point, t.Reach, t.IsAvailable));
            }
            int pick = InteractionRules.Pick(transform.position, m_Candidates, current);
            return pick >= 0 ? m_Targets[pick] : null;
        }

        void SetTarget(TavernInteractable target)
        {
            if (target != m_Target)
            {
                if (m_Target != null) m_Target.SetHighlighted(false);
                m_Target = target;
                if (m_Target != null) m_Target.SetHighlighted(true);
                PublishHint();
            }
            // The same target can say something new (a plate reached the pass): publish when the words change.
            else if (m_Target != null && (!m_HintShown || !m_Target.Hint.Equals(m_ShownHint))) PublishHint();
        }

        void PublishHint()
        {
            m_HintShown = m_Target != null;
            m_ShownHint = m_Target != null ? m_Target.Hint : default;
            EventBus<TavernInteractHint>.Publish(new TavernInteractHint(m_HintShown, m_ShownHint, m_Target != null ? m_Target.Kind : default));
        }

        void OnDisable()
        {
            if (m_Target != null) m_Target.SetHighlighted(false);
            m_Target = null;
            if (m_HintShown) EventBus<TavernInteractHint>.Publish(new TavernInteractHint(false, default));
            m_HintShown = false;
        }
    }
}
