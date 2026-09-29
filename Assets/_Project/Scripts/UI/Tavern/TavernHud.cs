using Hearthdelve.Core.Input;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// Service HUD: clock, gold/tips/renown, tonight's menu with sold-out marks, the order rail,
    /// the station hint, and the spill meter while carrying.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TavernHud : MonoBehaviour
    {
        VisualElement m_Hud, m_Menu, m_Tickets, m_SpillBar, m_SpillFill;
        Label m_Clock, m_Gold, m_Tips, m_Renown, m_LastOrders, m_MenuHeader, m_OrdersHeader, m_Hint, m_SpillLabel;
        TavernDirector m_Director;
        ServiceSession m_Session;
        bool m_ListsDirty = true;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Hud = root.Q("hud");
            m_Clock = root.Q<Label>("clock");
            m_Gold = root.Q<Label>("gold");
            m_Tips = root.Q<Label>("tips");
            m_Renown = root.Q<Label>("renown");
            m_LastOrders = root.Q<Label>("last-orders");
            m_MenuHeader = root.Q<Label>("menu-header");
            m_Menu = root.Q("menu-list");
            m_OrdersHeader = root.Q<Label>("orders-header");
            m_Tickets = root.Q("ticket-list");
            m_Hint = root.Q<Label>("hint");
            m_SpillBar = root.Q("spill-bar");
            m_SpillFill = root.Q("spill-fill");
            m_SpillLabel = root.Q<Label>("spill-label");
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director != null) m_Director.PhaseChanged += OnPhaseChanged;
            OnPhaseChanged();
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= OnPhaseChanged;
            Unhook();
        }

        void OnPhaseChanged()
        {
            bool visible = m_Director != null && m_Director.Phase == TavernPhase.Service;
            m_Hud.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || m_Director.Session == m_Session) return;
            Unhook();
            m_Session = m_Director.Session;
            m_Session.SoldOutChanged += MarkDirty;
            m_Session.TicketChanged += OnTicketChanged;
            m_MenuHeader.text = Loc.UI(TavernLocKeys.HudMenu);
            m_OrdersHeader.text = Loc.UI(TavernLocKeys.HudOrders);
            m_LastOrders.text = Loc.UI(TavernLocKeys.HudLastOrders);
            m_SpillLabel.text = Loc.UI(TavernLocKeys.Spill);
            MarkDirty();
        }

        void Unhook()
        {
            if (m_Session == null) return;
            m_Session.SoldOutChanged -= MarkDirty;
            m_Session.TicketChanged -= OnTicketChanged;
            m_Session = null;
        }

        void MarkDirty() => m_ListsDirty = true;
        void OnTicketChanged(Ticket _) => m_ListsDirty = true;

        void Update()
        {
            if (m_Session == null || m_Director.Phase != TavernPhase.Service) return;
            var ledger = m_Session.Ledger;
            m_Clock.text = Loc.UI(TavernLocKeys.HudTime, TavernUI.Clock(m_Session.Remaining));
            m_Gold.text = Loc.UI(TavernLocKeys.HudGold, ledger.Gold);
            m_Tips.text = Loc.UI(TavernLocKeys.HudTips, ledger.Tips);
            m_Renown.text = Loc.UI(TavernLocKeys.HudRenown, ledger.Renown);
            m_LastOrders.style.display = m_Session.IsLastOrders ? DisplayStyle.Flex : DisplayStyle.None;

            if (m_ListsDirty) RebuildLists();
            UpdateHint();
            UpdateSpill();
        }

        void RebuildLists()
        {
            m_ListsDirty = false;
            m_Menu.Clear();
            foreach (var recipe in m_Session.Menu)
            {
                var row = new VisualElement();
                row.AddToClassList("hd-menu-row");
                bool soldOut = m_Session.IsSoldOut(recipe);
                row.EnableInClassList("hd-menu-row--sold-out", soldOut);
                var swatch = new VisualElement();
                swatch.AddToClassList("hd-recipe__swatch");
                swatch.style.backgroundColor = recipe.placeholderColor;
                row.Add(swatch);
                row.Add(new Label(TavernUI.RecipeName(recipe)));
                if (soldOut)
                {
                    var tag = new Label(Loc.UI(TavernLocKeys.HudSoldOut));
                    tag.AddToClassList("hd-tag--sold-out");
                    row.Add(tag);
                }
                m_Menu.Add(row);
            }

            m_Tickets.Clear();
            foreach (var t in m_Session.Tickets)
            {
                if (t.State is TicketState.Served or TicketState.Cancelled) continue;
                var row = new VisualElement();
                row.AddToClassList("hd-ticket");
                row.AddToClassList($"hd-ticket--{t.State.ToString().ToLowerInvariant()}");
                var swatch = new VisualElement();
                swatch.AddToClassList("hd-recipe__swatch");
                swatch.style.backgroundColor = t.Recipe.placeholderColor;
                row.Add(swatch);
                row.Add(new Label(TavernUI.RecipeName(t.Recipe)));
                var state = new Label(Loc.UI(t.State switch
                {
                    TicketState.Cooking => TavernLocKeys.TicketCooking,
                    TicketState.Ready => TavernLocKeys.TicketReady,
                    TicketState.Delivering => TavernLocKeys.TicketDelivering,
                    _ => TavernLocKeys.TicketQueued,
                }));
                state.AddToClassList("hd-ticket__state");
                row.Add(state);
                m_Tickets.Add(row);
            }
        }

        void UpdateHint()
        {
            var player = m_Director.Player;
            string text = string.Empty;
            if (player.ActiveCook != null)
                text = Loc.UI(TavernLocKeys.HintStepAway, TavernUI.Binding(InputMaps.Minigame, MinigameActions.Cancel));
            else if (player.Carrying == null)
            {
                string interact = TavernUI.Binding(InputMaps.Tavern, TavernActions.Interact);
                text = player.Hint switch
                {
                    PlayerHint.Cook => Loc.UI(TavernLocKeys.HintCook, interact, TavernUI.RecipeName(player.HintRecipe)),
                    PlayerHint.PickUp => Loc.UI(TavernLocKeys.HintPickUp, interact, TavernUI.RecipeName(player.HintRecipe)),
                    PlayerHint.Staffed => Loc.UI(TavernLocKeys.HintStaffed,
                        m_Director.StaffMember != null ? Loc.Get(m_Director.StaffMember.displayName) : string.Empty),
                    _ => string.Empty,
                };
            }
            m_Hint.text = text;
            m_Hint.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void UpdateSpill()
        {
            var carrying = m_Director.Player.Carrying;
            m_SpillBar.parent.style.display = carrying != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (carrying != null) m_SpillFill.style.width = Length.Percent(Mathf.Clamp01(carrying.Spill) * 100f);
        }
    }
}
