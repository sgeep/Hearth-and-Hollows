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
            foreach (TavernInteractable t in TavernInteractable.All)
            {
                if (t == m_Target) current = m_Targets.Count;
                m_Targets.Add(t);
                m_Candidates.Add(new InteractionCandidate(t.UsePoint, t.Reach, t.IsAvailable));
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
            else if (!m_HintShown && m_Target != null) PublishHint();
        }

        void PublishHint()
        {
            m_HintShown = m_Target != null;
            EventBus<TavernInteractHint>.Publish(new TavernInteractHint(m_HintShown, m_Target != null ? m_Target.NameKey : null));
        }

        void OnDisable()
        {
            if (m_Target != null) m_Target.SetHighlighted(false);
            m_Target = null;
            if (m_HintShown) EventBus<TavernInteractHint>.Publish(new TavernInteractHint(false, null));
            m_HintShown = false;
        }
    }
}
