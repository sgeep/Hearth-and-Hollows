using System.Linq;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Service;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>The kitchen at work (fire, sizzling pans, smoke) while anyone is cooking at the Grill; at rest otherwise. Presentation only.</summary>
    public sealed class KitchenView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Renderer;
        [SerializeField] Sprite m_Idle;
        [SerializeField] Sprite[] m_Working = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float m_FrameSeconds = 0.12f;

        float m_Time;

        public bool IsWorking { get; private set; }
        public Sprite Idle => m_Idle;
        public int WorkingFrameCount => m_Working.Length;

        public void Configure(SpriteRenderer renderer, Sprite idle, Sprite[] working)
        {
            m_Renderer = renderer;
            m_Idle = idle;
            m_Working = working;
        }

        void LateUpdate()
        {
            ServiceSession session = TavernDirector.Instance != null ? TavernDirector.Instance.Session : null;
            IsWorking = session != null && session.Tickets.Any(t => t.State == TicketState.Cooking && t.Recipe.station == CookStation.Grill);
            if (m_Renderer == null) return;
            if (!IsWorking || m_Working.Length == 0)
            {
                m_Renderer.sprite = m_Idle;
                m_Time = 0f;
                return;
            }
            m_Time += Time.deltaTime;
            m_Renderer.sprite = m_Working[(int)(m_Time / m_FrameSeconds) % m_Working.Length];
        }
    }
}
