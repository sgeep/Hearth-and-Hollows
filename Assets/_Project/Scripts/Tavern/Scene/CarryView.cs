using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The plate a character is carrying (4c decision 2): the dish's icon above their head, and a small
    /// spill meter under it that fills, yellow to red, with each bump. Presentation only.
    /// </summary>
    public sealed class CarryView : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] SpriteRenderer m_Icon;
        [SerializeField] GameObject m_Meter;
        [SerializeField, Tooltip("Scaled in whole art pixels (8 = full) from its left end.")]
        Transform m_SpillAnchor;
        [SerializeField] SpriteRenderer m_SpillFill;
        [SerializeField] Color m_Low = new(1f, 0.85f, 0.3f);
        [SerializeField] Color m_High = new(0.95f, 0.25f, 0.2f);

        public bool IsShowing => m_Root != null && m_Root.activeSelf;
        public Sprite Icon => m_Icon != null ? m_Icon.sprite : null;
        public float Spill { get; private set; }

        public void Configure(GameObject root, SpriteRenderer icon, GameObject meter, Transform spillAnchor, SpriteRenderer spillFill)
        {
            m_Root = root;
            m_Icon = icon;
            m_Meter = meter;
            m_SpillAnchor = spillAnchor;
            m_SpillFill = spillFill;
        }

        void Awake() => Hide();

        public void Show(Sprite dish)
        {
            if (m_Root == null) return;
            m_Root.SetActive(true);
            if (m_Icon != null) m_Icon.sprite = dish;
            SetSpill(0f);
        }

        /// <summary>0 (none) to 1 (dropped). The meter only shows once something has spilled.</summary>
        public void SetSpill(float spill)
        {
            Spill = Mathf.Clamp01(spill);
            if (m_Meter != null) m_Meter.SetActive(Spill > 0f);
            if (m_SpillAnchor != null) m_SpillAnchor.localScale = new Vector3(Mathf.Ceil(Spill * 8f), 1f, 1f);
            if (m_SpillFill != null) m_SpillFill.color = Color.Lerp(m_Low, m_High, Spill);
        }

        public void Hide()
        {
            if (m_Root != null) m_Root.SetActive(false);
        }
    }
}
