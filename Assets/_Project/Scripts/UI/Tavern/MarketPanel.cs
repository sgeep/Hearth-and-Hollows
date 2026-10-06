using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>One line of the market list: the staple, what's in the storeroom, and its buy button.</summary>
    [Serializable]
    public sealed class MarketRow
    {
        public GameObject root;
        public Image icon;
        public LocalizedSuperText name;
        public LocalizedSuperText stock;
        public Button buy;
        public LocalizedSuperText buyLabel;
    }

    /// <summary>
    /// The Brackenford market (4f Checkpoint C, D19): a temporary list on the daytime panel until the village has a shop.
    /// Surface staples, bought with gold, straight into the storeroom at Standard quality and full freshness. One button
    /// per staple, one bundle per press; the purse and the storeroom count update as you buy, and the game saves.
    /// </summary>
    public sealed class MarketPanel : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Purse;
        [SerializeField] MarketRow[] m_Rows = Array.Empty<MarketRow>();
        [SerializeField] Button m_Done;
        [SerializeField] LocalizedSuperText m_Message;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<MarketRow> Rows => m_Rows;
        public Button Done => m_Done;

        static GameFlow Flow => GameFlow.Instance;
        static SupplySource Market => Flow != null && Flow.Database != null ? Flow.Database.market : null;

        public void Configure(GameObject root, LocalizedSuperText purse, MarketRow[] rows, Button done, LocalizedSuperText message)
        {
            m_Root = root;
            m_Purse = purse;
            m_Rows = rows;
            m_Done = done;
            m_Message = message;
        }

        void Awake()
        {
            for (int i = 0; i < m_Rows.Length; i++)
            {
                int index = i;
                m_Rows[i].buy.onClick.AddListener(() => Buy(index));
            }
            if (m_Done != null) m_Done.onClick.AddListener(Close);
            if (m_Root != null) m_Root.SetActive(false);
        }

        /// <summary>Shown only in the day loop, where there's a purse and a market.</summary>
        public static bool Available => Market != null && Flow.InGame && Flow.State != null && Flow.State.Phase == DayPhase.Daytime;

        public void Open()
        {
            if (!Available || m_Root == null) return;
            m_Root.SetActive(true);
            m_Message?.gameObject.SetActive(false);
            Fill();
            Select(m_Rows.Length > 0 && m_Rows[0].root.activeSelf ? m_Rows[0].buy.gameObject : m_Done != null ? m_Done.gameObject : null);
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>Raised when the list closes (the daytime panel takes the selection back).</summary>
        public event Action Closed;

        void Update()
        {
            if (!IsOpen) return;
            if (!Available)
            {
                Close();
                return;
            }
            // Escape / B closes it, like every other panel.
            bool back = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                        || UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back) Close();
        }

        /// <summary>Buys one bundle of row <paramref name="index"/>'s staple.</summary>
        public bool Buy(int index)
        {
            SupplySource market = Market;
            if (!Available || index < 0 || index >= m_Rows.Length || index >= market.offers.Count) return false;
            SupplyOffer offer = market.offers[index];
            bool bought = Flow.BuyFromMarket(offer);
            if (m_Message != null)
            {
                m_Message.gameObject.SetActive(!bought);
                if (!bought) m_Message.Set(LoopLocKeys.MarketShort);
            }
            Fill();
            return bought;
        }

        void Fill()
        {
            SupplySource market = Market;
            if (market == null) return;
            m_Purse?.Set(LoopLocKeys.MarketPurse, Flow.State.Gold);
            Storeroom storeroom = Flow.State.Storeroom;
            for (int i = 0; i < m_Rows.Length; i++)
            {
                MarketRow row = m_Rows[i];
                bool has = i < market.offers.Count && market.offers[i]?.ingredient != null;
                row.root.SetActive(has);
                if (!has) continue;
                SupplyOffer offer = market.offers[i];
                IngredientDefinition d = offer.ingredient;
                row.icon.sprite = d.icon;
                row.icon.enabled = d.icon != null;
                row.name.Set(TavernLocKeys.Plain, Loc.Get(d.displayName));
                row.stock.Set(LoopLocKeys.MarketHave, storeroom.CountMatching(item => item.Definition == d));
                row.buyLabel.Set(offer.bundle > 1 ? LoopLocKeys.MarketBuyBundle : LoopLocKeys.MarketBuy, offer.price, offer.bundle);
                row.buy.interactable = true;
            }
        }

        static void Select(GameObject target)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(null);
            if (target != null) EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
