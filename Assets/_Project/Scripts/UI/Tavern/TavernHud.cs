using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>One order on the rail: the dish, where it's at, and its customer's patience.</summary>
    [Serializable]
    public sealed class RailRow
    {
        public GameObject root;
        public Image icon;
        public LocalizedSuperText state;
        public RectTransform patience;
        public Image patienceFill;
        [NonSerialized] public string shownKey;
    }

    /// <summary>
    /// The service HUD, in the screen's side margins so the room stays the focus: on the left a clock bar
    /// that runs down (with "Last orders!" near the end), tonight's takings and Renown, and the menu (a
    /// sold-out dish dims and is struck through); on the right the order rail, each order's dish icon, a
    /// one-word state and a thin bar for its customer's patience. Shown only during service. Reads the
    /// session; never changes it.
    /// </summary>
    public sealed class TavernHud : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] RectTransform m_ClockFill;
        [SerializeField] GameObject m_LastOrders;
        [SerializeField] LocalizedSuperText m_Gold;
        [SerializeField] LocalizedSuperText m_Tips;
        [SerializeField] LocalizedSuperText m_Renown;
        [SerializeField] Image[] m_Menu = Array.Empty<Image>();
        [SerializeField] GameObject[] m_MenuSoldOut = Array.Empty<GameObject>();
        [SerializeField] RailRow[] m_Rows = Array.Empty<RailRow>();
        [SerializeField] Color m_PatienceFull = new(0.45f, 0.9f, 0.35f);
        [SerializeField] Color m_PatienceEmpty = new(0.95f, 0.3f, 0.25f);
        [SerializeField, Tooltip("The takings flash this colour when a customer pays.")] Color m_GoldFlash = new(1f, 0.85f, 0.35f);
        [SerializeField, Min(0.01f)] float m_GoldFlashSeconds = 0.5f;

        float m_GoldFlashLeft;
        Color m_GoldColour;
        SuperTextMesh m_GoldText;

        readonly List<Ticket> m_Open = new();
        int m_ShownGold = int.MinValue, m_ShownTips = int.MinValue, m_ShownRenown = int.MinValue;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<RailRow> Rows => m_Rows;
        public int RowsShown { get; private set; }
        public float Clock { get; private set; }

        public void Configure(GameObject root, RectTransform clockFill, GameObject lastOrders, LocalizedSuperText gold, LocalizedSuperText tips, LocalizedSuperText renown,
            Image[] menu, GameObject[] menuSoldOut, RailRow[] rows)
        {
            m_Root = root;
            m_ClockFill = clockFill;
            m_LastOrders = lastOrders;
            m_Gold = gold;
            m_Tips = tips;
            m_Renown = renown;
            m_Menu = menu;
            m_MenuSoldOut = menuSoldOut;
            m_Rows = rows;
        }

        void LateUpdate()
        {
            TavernDirector director = TavernDirector.Instance;
            bool serving = director != null && director.Phase == TavernPhase.Service && director.Session != null;
            if (m_Root.activeSelf != serving)
            {
                m_Root.SetActive(serving);
                if (serving) m_ShownGold = m_ShownTips = m_ShownRenown = int.MinValue;
            }
            if (!serving) return;
            ServiceSession session = director.Session;

            float length = director.Content.service.service.lengthSeconds;
            Clock = length > 0f ? Mathf.Clamp01(session.Remaining / length) : 0f;
            m_ClockFill.anchorMax = new Vector2(Clock, 1f);
            m_LastOrders.SetActive(session.IsLastOrders && !session.IsOver);

            ServiceLedger ledger = session.Ledger;
            if (ledger.Gold != m_ShownGold)
            {
                // Paid: the takings flash (the coin sound is the room's; no vibration for routine payments).
                if (m_ShownGold != int.MinValue && ledger.Gold > m_ShownGold) m_GoldFlashLeft = m_GoldFlashSeconds;
                m_Gold.Set(TavernLocKeys.HudGold, m_ShownGold = ledger.Gold);
            }
            FlashGold();
            if (ledger.Tips != m_ShownTips) m_Tips.Set(TavernLocKeys.HudTips, m_ShownTips = ledger.Tips);
            if (ledger.Renown != m_ShownRenown) m_Renown.Set(TavernLocKeys.HudRenown, Signed(m_ShownRenown = ledger.Renown));

            for (int i = 0; i < m_Menu.Length; i++)
            {
                RecipeDefinition dish = i < session.Menu.Count ? session.Menu[i] : null;
                m_Menu[i].gameObject.SetActive(dish != null);
                if (dish == null) continue;
                m_Menu[i].sprite = dish.icon;
                bool soldOut = session.IsSoldOut(dish);
                m_Menu[i].color = soldOut ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                m_MenuSoldOut[i].SetActive(soldOut);
            }

            m_Open.Clear();
            foreach (Ticket t in session.Tickets)
                if (t.State is TicketState.Queued or TicketState.Cooking or TicketState.Ready or TicketState.Delivering) m_Open.Add(t);
            RowsShown = Mathf.Min(m_Open.Count, m_Rows.Length);
            for (int i = 0; i < m_Rows.Length; i++)
            {
                RailRow row = m_Rows[i];
                bool has = i < RowsShown;
                if (row.root.activeSelf != has) row.root.SetActive(has);
                if (!has) continue;
                Ticket ticket = m_Open[i];
                row.icon.sprite = ticket.Recipe.icon;
                string key = StateKey(ticket);
                if (key != row.shownKey) row.state.Set(row.shownKey = key);
                // A spare has no one waiting: no patience bar.
                float patience = ticket.Customer != null ? ticket.Customer.Patience : 0f;
                row.patience.gameObject.SetActive(ticket.Customer != null);
                row.patience.anchorMax = new Vector2(patience, 1f);
                row.patienceFill.color = Color.Lerp(m_PatienceEmpty, m_PatienceFull, patience);
            }
        }

        /// <summary>The one word an order's state shows.</summary>
        public static string StateKey(Ticket ticket)
        {
            if (ticket.IsSpare && ticket.State is TicketState.Ready or TicketState.Delivering) return TavernLocKeys.TicketSpare;
            return ticket.State switch
            {
                TicketState.Queued => ticket.Recipe.station == CookStation.StewPot ? TavernLocKeys.TicketStewWaiting : TavernLocKeys.TicketQueued,
                TicketState.Cooking => TavernLocKeys.TicketCooking,
                TicketState.Ready => TavernLocKeys.TicketReady,
                _ => TavernLocKeys.TicketDelivering,
            };
        }

        /// <summary>The takings are flashing from a payment (tests).</summary>
        public bool GoldFlashing => m_GoldFlashLeft > 0f;

        void FlashGold()
        {
            if (m_GoldText == null)
            {
                m_GoldText = m_Gold.GetComponent<SuperTextMesh>();
                if (m_GoldText == null) return;
                m_GoldColour = m_GoldText.color;
            }
            if (m_GoldFlashLeft <= 0f) return;
            m_GoldFlashLeft = Mathf.Max(0f, m_GoldFlashLeft - Time.unscaledDeltaTime);
            m_GoldText.color = Color.Lerp(m_GoldColour, m_GoldFlash, m_GoldFlashLeft / m_GoldFlashSeconds);
            m_GoldText.Rebuild();
        }

        static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
    }
}
