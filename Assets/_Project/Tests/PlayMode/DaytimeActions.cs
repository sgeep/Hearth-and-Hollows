using Hearthdelve.Core.Events;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using Hearthdelve.Village;
using UnityEngine;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// What the player does in the free daytime since 4h, for the tests that walk through a day: begin the evening at the menu
    /// board (and say yes), browse the market at its stall, check the storeroom shelves. The same paths the player takes, not
    /// shortcuts past them.
    /// </summary>
    public static class DaytimeActions
    {
        /// <summary>The menu board, then "begin prep".</summary>
        public static void BeginEvening()
        {
            EventBus<DaytimePlaceUsed>.Publish(new DaytimePlaceUsed(TavernInteractableKind.MenuBoard));
            Object.FindAnyObjectByType<PrepConfirm>(FindObjectsInactive.Include).Confirm();
        }

        /// <summary>The market stall in Kariaston's square (open until five).</summary>
        public static MarketStall Stall => Object.FindAnyObjectByType<MarketStall>(FindObjectsInactive.Include);

        /// <summary>Uses the market stall: the market's list opens while it trades.</summary>
        public static void OpenMarket() => Stall.Interactable.Use();

        /// <summary>The storeroom shelves: the daytime panel opens on the stock.</summary>
        public static void OpenStoreroom() => EventBus<DaytimePlaceUsed>.Publish(new DaytimePlaceUsed(TavernInteractableKind.Storeroom));
    }
}
