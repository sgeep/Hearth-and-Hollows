using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The death screen and the Lockbox (GDD §4.4): Essence ran out, so the haul is lost except one
    /// satchel slot the player chooses to keep, its whole stack. Choosing a slot marks it (an outline,
    /// not just a colour) and moves to "Return to the surface", which confirms; "Keep nothing" gives
    /// everything up. With an empty satchel it only explains and returns. Gameplay pauses it and
    /// applies the choice; this only asks.
    /// </summary>
    public sealed class DeathScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Panel;
        [SerializeField] LocalizedSuperText m_Subtitle;
        [SerializeField] LocalizedSuperText m_Chosen;
        [SerializeField] SatchelSlotView[] m_Slots = Array.Empty<SatchelSlotView>();
        [SerializeField] Button m_Confirm;
        [SerializeField] Button m_KeepNothing;
        float m_ConfirmX = float.NaN;
        [SerializeField, Tooltip("Furnishings found on the delve (4f Checkpoint C): lost with it, never in the Lockbox.")]
        LocalizedSuperText m_CuriosLost;

        public string CuriosLostText => m_CuriosLost != null && m_CuriosLost.gameObject.activeSelf ? m_CuriosLost.GetComponent<SuperTextMesh>()?.text : null;

        public void ConfigureCurios(LocalizedSuperText curiosLost) => m_CuriosLost = curiosLost;

        Action<int> m_OnChosen;
        Satchel m_Satchel;
        int m_Keep = KeepNothing;

        const int KeepNothing = -1;

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;
        public int Kept => m_Keep;
        public SatchelSlotView[] Slots => m_Slots;
        public Button Confirm => m_Confirm;
        public Button KeepNothingButton => m_KeepNothing;

        public void Configure(GameObject panel, LocalizedSuperText subtitle, LocalizedSuperText chosen, SatchelSlotView[] slots, Button confirm, Button keepNothing)
        {
            m_Panel = panel;
            m_Subtitle = subtitle;
            m_Chosen = chosen;
            m_Slots = slots;
            m_Confirm = confirm;
            m_KeepNothing = keepNothing;
        }

        void Awake()
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                int index = i;
                m_Slots[i].GetComponent<Button>().onClick.AddListener(() => Mark(index));
            }
            if (m_Confirm != null) m_Confirm.onClick.AddListener(() => Answer(m_Keep));
            if (m_KeepNothing != null) m_KeepNothing.onClick.AddListener(() => Answer(KeepNothing));
            if (m_Panel != null) m_Panel.SetActive(false);
        }

        void OnEnable() => EventBus<DeathScreenRequested>.Subscribe(Open);
        void OnDisable() => EventBus<DeathScreenRequested>.Unsubscribe(Open);

        void Open(DeathScreenRequested request)
        {
            m_Satchel = request.Satchel;
            m_OnChosen = request.OnChosen;
            m_Keep = KeepNothing;
            bool empty = m_Satchel.IsEmpty;
            if (m_Subtitle != null) m_Subtitle.Set(empty ? LocKeys.DeathSubtitleEmpty : LocKeys.DeathSubtitle);
            GameObject first = null;
            for (int i = 0; i < m_Slots.Length; i++)
            {
                bool shown = !empty && i < m_Satchel.Capacity;
                m_Slots[i].gameObject.SetActive(shown);
                if (!shown) continue;
                IngredientStack stack = m_Satchel.Slots[i];
                m_Slots[i].Show(stack);
                m_Slots[i].SetMarked(false);
                m_Slots[i].GetComponent<Button>().interactable = !stack.IsEmpty;
                if (first == null && !stack.IsEmpty) first = m_Slots[i].gameObject;
            }
            if (m_KeepNothing != null) m_KeepNothing.gameObject.SetActive(!empty);
            // 4i-C: with nothing to keep, the one button left stands in the middle (not in its place beside "keep nothing").
            if (m_Confirm != null)
            {
                var rect = (RectTransform)m_Confirm.transform;
                if (float.IsNaN(m_ConfirmX)) m_ConfirmX = rect.anchoredPosition.x;
                rect.anchoredPosition = new Vector2(empty ? 0f : m_ConfirmX, rect.anchoredPosition.y);
            }
            if (m_Chosen != null) m_Chosen.gameObject.SetActive(!empty);
            if (m_CuriosLost != null)
            {
                var curios = request.Curios;
                bool quest = request.QuestObjects != null && request.QuestObjects.Count > 0;
                bool any = quest || (curios != null && curios.Count > 0);
                m_CuriosLost.gameObject.SetActive(any);
                // A quest object can't go in the Lockbox (4g Checkpoint B): said first, by name.
                if (quest) m_CuriosLost.Set(LocKeys.DeathCuriosLost, LocKeys.QuestObjectName(request.QuestObjects[0]));
                else if (any) m_CuriosLost.Set(LocKeys.DeathCuriosLost, LocKeys.FurnitureList(curios));
            }
            ShowChoice();
            m_Panel.SetActive(true);
            Select(first != null ? first : m_Confirm != null ? m_Confirm.gameObject : null);
        }

        /// <summary>Puts slot <paramref name="index"/> in the Lockbox (empty slots can't be chosen).</summary>
        public void Mark(int index)
        {
            if (!IsOpen || index < 0 || index >= m_Slots.Length || m_Slots[index].Stack.IsEmpty) return;
            m_Keep = index;
            for (int i = 0; i < m_Slots.Length; i++) m_Slots[i].SetMarked(i == index);
            ShowChoice();
            if (m_Confirm != null) Select(m_Confirm.gameObject);
        }

        void ShowChoice()
        {
            if (m_Chosen == null) return;
            if (m_Keep < 0) m_Chosen.Set(LocKeys.DeathSelectedNone);
            else m_Chosen.Set(LocKeys.DeathSelected, Loc.ItemName(m_Slots[m_Keep].Stack.Item), m_Slots[m_Keep].Stack.Count);
        }

        static void Select(GameObject target)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }

        void Answer(int slot)
        {
            if (!IsOpen) return;
            m_Panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Action<int> answer = m_OnChosen;
            m_OnChosen = null;
            answer?.Invoke(slot);
        }
    }
}
