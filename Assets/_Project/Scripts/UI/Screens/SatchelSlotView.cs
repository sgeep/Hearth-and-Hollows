using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// One satchel slot: the part's icon, its count, its quality as pips (one to four, so it never
    /// depends on colour alone) and its freshness as a bar whose length is the freshness. Selecting it
    /// (keyboard, controller, or the mouse hovering) shows the selected frame with its marker tab.
    /// Used by the swap prompt, and later by the HUD and the death screen.
    /// </summary>
    public sealed class SatchelSlotView : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        static readonly Color[] k_QualityColors =
        {
            new(0.62f, 0.55f, 0.48f), // Poor
            new(0.92f, 0.9f, 0.85f),  // Standard
            new(0.45f, 0.85f, 1f),    // Fine
            new(1f, 0.8f, 0.3f),      // Premium
        };
        static readonly Color k_Stale = new(0.85f, 0.3f, 0.2f);
        static readonly Color k_Fresh = new(0.45f, 0.9f, 0.4f);

        [SerializeField] Image m_Frame;
        [SerializeField] Sprite m_FrameNormal;
        [SerializeField] Sprite m_FrameSelected;
        [SerializeField] Image m_Icon;
        [SerializeField] LocalizedSuperText m_Count;
        [SerializeField] Image[] m_Pips = new Image[4];
        [SerializeField] Image m_FreshnessBack;
        [SerializeField] Image m_FreshnessFill;

        public IngredientStack Stack { get; private set; }
        public bool IsSelected { get; private set; }

        /// <summary>Raised when this slot becomes the selected one.</summary>
        public event System.Action<SatchelSlotView> Selected;

        public void Configure(Image frame, Sprite normal, Sprite selected, Image icon, LocalizedSuperText count, Image[] pips, Image freshnessBack, Image freshnessFill)
        {
            m_Frame = frame;
            m_FrameNormal = normal;
            m_FrameSelected = selected;
            m_Icon = icon;
            m_Count = count;
            m_Pips = pips;
            m_FreshnessBack = freshnessBack;
            m_FreshnessFill = freshnessFill;
        }

        public void Show(IngredientStack stack)
        {
            Stack = stack;
            bool full = !stack.IsEmpty && stack.Item.IsValid;
            if (m_Icon != null)
            {
                m_Icon.enabled = full && stack.Item.Definition.icon != null;
                if (m_Icon.enabled) m_Icon.sprite = stack.Item.Definition.icon;
            }
            if (m_Count != null)
            {
                m_Count.gameObject.SetActive(full && stack.Count > 1);
                if (full && stack.Count > 1) m_Count.Set(LocKeys.SlotCount, stack.Count);
            }
            int pips = full ? PipsFor(stack.Item.Quality) : 0;
            for (int i = 0; i < m_Pips.Length; i++)
            {
                if (m_Pips[i] == null) continue;
                m_Pips[i].enabled = i < pips;
                m_Pips[i].color = full ? k_QualityColors[(int)stack.Item.Quality] : Color.clear;
            }
            if (m_FreshnessBack != null) m_FreshnessBack.enabled = full;
            if (m_FreshnessFill != null)
            {
                m_FreshnessFill.enabled = full;
                m_FreshnessFill.fillAmount = full ? Mathf.Clamp01(stack.Freshness) : 0f;
                m_FreshnessFill.color = Color.Lerp(k_Stale, k_Fresh, stack.Freshness);
            }
        }

        /// <summary>Quality as a count of pips: Poor 1, Standard 2, Fine 3, Premium 4.</summary>
        public static int PipsFor(Quality quality) => (int)quality + 1;

        public void OnSelect(BaseEventData eventData)
        {
            IsSelected = true;
            if (m_Frame != null && m_FrameSelected != null) m_Frame.sprite = m_FrameSelected;
            Selected?.Invoke(this);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            IsSelected = false;
            if (m_Frame != null && m_FrameNormal != null) m_Frame.sprite = m_FrameNormal;
        }

        // The mouse picks a slot by hovering, the same as moving the selection with keys or a stick.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
