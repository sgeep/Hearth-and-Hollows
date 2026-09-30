using Hearthdelve.Tavern.Service;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Shows the stew pot's state on the pot itself: the stew inside (tinted by recipe), a simmer
    /// progress bar, and one pip per helping left. Reads <see cref="ServiceSession.Pot"/>.
    /// </summary>
    public sealed class StewPotView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Contents;
        [SerializeField] SpriteRenderer m_BarBack;
        [SerializeField] SpriteRenderer m_BarFill;
        [SerializeField] SpriteRenderer[] m_Helpings = new SpriteRenderer[0];
        [SerializeField] Color m_ChoppingTint = new(0.8f, 0.8f, 0.8f, 0.6f);

        Vector3 m_FillScale;

        public void Configure(SpriteRenderer contents, SpriteRenderer barBack, SpriteRenderer barFill, SpriteRenderer[] helpings)
        {
            m_Contents = contents;
            m_BarBack = barBack;
            m_BarFill = barFill;
            m_Helpings = helpings;
        }

        void Awake() => m_FillScale = m_BarFill != null ? m_BarFill.transform.localScale : Vector3.one;

        void Update()
        {
            var director = TavernDirector.Instance;
            var pot = director != null && director.Session != null ? director.Session.Pot : null;
            var state = pot?.State ?? PotState.Empty;

            if (m_Contents != null)
            {
                m_Contents.enabled = state != PotState.Empty;
                if (pot?.Recipe != null)
                    m_Contents.color = state == PotState.Chopping ? m_ChoppingTint * pot.Recipe.placeholderColor : pot.Recipe.placeholderColor;
            }

            bool simmering = state == PotState.Simmering;
            if (m_BarBack != null) m_BarBack.enabled = simmering;
            if (m_BarFill != null)
            {
                m_BarFill.enabled = simmering;
                if (simmering) m_BarFill.transform.localScale = new Vector3(m_FillScale.x * pot.SimmerProgress, m_FillScale.y, 1f);
            }

            int helpings = state is PotState.Ready or PotState.Simmering ? pot.Helpings : 0;
            for (int i = 0; i < m_Helpings.Length; i++)
            {
                if (m_Helpings[i] == null) continue;
                m_Helpings[i].enabled = i < helpings;
                // Promised-but-not-ready helpings show dimmed while it simmers.
                m_Helpings[i].color = state == PotState.Ready ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
        }
    }
}
