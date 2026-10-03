using System.Linq;
using Hearthdelve.Tavern.Service;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>Plates waiting on the pass: up to one icon per slot along the table top, oldest first. Presentation only.</summary>
    public sealed class PassView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer[] m_Slots = System.Array.Empty<SpriteRenderer>();

        public int Showing { get; private set; }

        public void Configure(SpriteRenderer[] slots) => m_Slots = slots;

        void LateUpdate()
        {
            ServiceSession session = TavernDirector.Instance != null ? TavernDirector.Instance.Session : null;
            var ready = session != null ? session.Tickets.Where(t => t.State == TicketState.Ready).Take(m_Slots.Length).ToList() : null;
            Showing = ready?.Count ?? 0;
            for (int i = 0; i < m_Slots.Length; i++)
            {
                bool show = ready != null && i < ready.Count;
                m_Slots[i].enabled = show;
                if (show) m_Slots[i].sprite = ready[i].Recipe.icon;
            }
        }
    }
}
