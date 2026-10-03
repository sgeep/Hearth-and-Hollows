using Hearthdelve.Tavern.Service;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The stew pot's state over the cauldron: a bar filling while it simmers, and a pip per helping
    /// (dim while simmering, bright once ladling). Presentation only.
    /// </summary>
    public sealed class StewPotView : MonoBehaviour
    {
        [SerializeField] GameObject m_Bar;
        [SerializeField] Transform m_BarAnchor;
        [SerializeField] SpriteRenderer[] m_Pips = System.Array.Empty<SpriteRenderer>();
        [SerializeField] Color m_Dim = new(0.45f, 0.35f, 0.25f);
        [SerializeField] Color m_Bright = new(1f, 0.82f, 0.35f);

        public float Progress { get; private set; }
        public int PipsShown { get; private set; }

        public void Configure(GameObject bar, Transform barAnchor, SpriteRenderer[] pips)
        {
            m_Bar = bar;
            m_BarAnchor = barAnchor;
            m_Pips = pips;
        }

        void LateUpdate()
        {
            StewPot pot = TavernDirector.Instance != null && TavernDirector.Instance.Session != null ? TavernDirector.Instance.Session.Pot : null;
            bool simmering = pot != null && pot.State == PotState.Simmering;
            Progress = simmering ? pot.SimmerProgress : 0f;
            if (m_Bar != null) m_Bar.SetActive(simmering);
            if (m_BarAnchor != null) m_BarAnchor.localScale = new Vector3(Mathf.Ceil(Progress * 10f), 1f, 1f);
            int helpings = pot != null && pot.State is PotState.Simmering or PotState.Ready ? pot.Helpings : 0;
            PipsShown = Mathf.Min(helpings, m_Pips.Length);
            for (int i = 0; i < m_Pips.Length; i++)
            {
                m_Pips[i].enabled = i < PipsShown;
                m_Pips[i].color = simmering ? m_Dim : m_Bright;
            }
        }
    }
}
