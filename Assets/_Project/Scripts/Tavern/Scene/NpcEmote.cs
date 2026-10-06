using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A wordless reaction over a character's head (4f Checkpoint C, D18): a small face that pops up, bobs and fades. Staff
    /// use it for their personality beats (Gunta tasting the stew, Pip at a dropped plate). Presentation only: nothing waits
    /// for it, and the dialogue that might say more comes with 4g.
    /// </summary>
    public sealed class NpcEmote : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Icon;
        [SerializeField, Min(0.1f)] float m_DefaultSeconds = 1.4f;

        float m_Shown = float.NegativeInfinity;
        float m_Seconds;

        /// <summary>The face showing now (null when none).</summary>
        public Sprite Showing => m_Icon != null && m_Icon.enabled ? m_Icon.sprite : null;
        /// <summary>The last face shown (tests).</summary>
        public Sprite Last { get; private set; }

        public void Configure(SpriteRenderer icon) => m_Icon = icon;

        void Awake()
        {
            if (m_Icon != null) m_Icon.enabled = false;
        }

        public void Show(Sprite face, float seconds = 0f)
        {
            if (face == null || m_Icon == null) return;
            m_Icon.sprite = face;
            m_Icon.enabled = true;
            m_Shown = Time.time;
            m_Seconds = seconds > 0f ? seconds : m_DefaultSeconds;
            Last = face;
        }

        void Update()
        {
            if (m_Icon == null || !m_Icon.enabled) return;
            float age = Time.time - m_Shown;
            if (age >= m_Seconds)
            {
                m_Icon.enabled = false;
                return;
            }
            // A little hop up as it appears, then still; a fade over its last quarter.
            float hop = age < 0.15f ? Mathf.Sin(age / 0.15f * Mathf.PI) * 0.125f : 0f;
            m_Icon.transform.localPosition = new Vector3(0f, hop, 0f);
            float fade = Mathf.Clamp01((m_Seconds - age) / (m_Seconds * 0.25f));
            m_Icon.color = new Color(1f, 1f, 1f, fade);
        }
    }
}
