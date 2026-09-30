using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    public enum StationKind
    {
        Grill,
        Tap,
        /// <summary>Where finished dishes wait to be carried out.</summary>
        Pass,
        /// <summary>Batch stews: chop the ingredients, then it simmers on its own.</summary>
        StewPot,
    }

    /// <summary>An interactable spot on the tavern floor.</summary>
    public sealed class Station : MonoBehaviour
    {
        [SerializeField] StationKind m_Kind;
        [SerializeField] SpriteRenderer m_Highlight;

        public StationKind Kind => m_Kind;
        public float X => transform.position.x;

        public void Configure(StationKind kind, SpriteRenderer highlight)
        {
            m_Kind = kind;
            m_Highlight = highlight;
        }

        public void SetHighlighted(bool on)
        {
            if (m_Highlight != null) m_Highlight.enabled = on;
        }
    }
}
