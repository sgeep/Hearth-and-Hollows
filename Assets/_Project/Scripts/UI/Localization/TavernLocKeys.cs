namespace Hearthdelve.UI.Localization
{
    /// <summary>UI-table keys for the tavern. English text is the source of truth (synced by the editor).</summary>
    public static class TavernLocKeys
    {
        public const string PrepTitle = "tavern.prep.title";
        public const string PrepStoreroom = "tavern.prep.storeroom";
        public const string PrepStoreroomEmpty = "tavern.prep.storeroom_empty";
        public const string PrepStockRow = "tavern.prep.stock_row";
        public const string PrepFill = "tavern.prep.fill";
        public const string PrepMenu = "tavern.prep.menu";
        public const string PrepRecipeDetails = "tavern.prep.recipe_details";
        public const string PrepStaff = "tavern.prep.staff";
        public const string PrepStaffOff = "tavern.prep.staff_off";
        public const string PrepOpen = "tavern.prep.open";

        public const string StationGrill = "station.grill";
        public const string StationTap = "station.tap";
        public const string StationServing = "station.serving";

        public const string HudTime = "tavern.hud.time";
        public const string HudGold = "tavern.hud.gold";
        public const string HudTips = "tavern.hud.tips";
        public const string HudRenown = "tavern.hud.renown";
        public const string HudLastOrders = "tavern.hud.last_orders";
        public const string HudMenu = "tavern.hud.menu";
        public const string HudSoldOut = "tavern.hud.sold_out";
        public const string HudOrders = "tavern.hud.orders";
        public const string TicketQueued = "ticket.queued";
        public const string TicketCooking = "ticket.cooking";
        public const string TicketReady = "ticket.ready";
        public const string TicketDelivering = "ticket.delivering";

        public const string HintCook = "tavern.hint.cook";
        public const string HintPickUp = "tavern.hint.pick_up";
        public const string HintStaffed = "tavern.hint.staffed";
        public const string HintStepAway = "tavern.hint.step_away";
        public const string Spill = "serving.spill";

        public const string GrillPrompt = "grill.prompt";
        public const string GrillSide = "grill.side";
        public const string TapPrompt = "tap.prompt";

        public const string ResultsTitle = "results.title";
        public const string ResultsServed = "results.served";
        public const string ResultsGold = "results.gold";
        public const string ResultsTips = "results.tips";
        public const string ResultsRenown = "results.renown";
        public const string ResultsWalkouts = "results.walkouts";
        public const string ResultsSoldOut = "results.sold_out";
        public const string ResultsDropped = "results.dropped";
        public const string ResultsAgain = "results.again";

        public static readonly (string key, string english)[] English =
        {
            (PrepTitle, "Evening Prep"),
            (PrepStoreroom, "Storeroom"),
            (PrepStoreroomEmpty, "The storeroom is empty."),
            (PrepStockRow, "{0} ×{1} — {2}% fresh"),
            (PrepFill, "Fill storeroom (debug)"),
            (PrepMenu, "Tonight's menu (up to {0})"),
            (PrepRecipeDetails, "{0} · {1} gold · {2} servings"),
            (PrepStaff, "Staff helper: {0}"),
            (PrepStaffOff, "Off duty"),
            (PrepOpen, "Open the doors"),

            (StationGrill, "Grill"),
            (StationTap, "Tap"),
            (StationServing, "Serving"),

            (HudTime, "Time left {0}"),
            (HudGold, "Gold {0}"),
            (HudTips, "Tips {0}"),
            (HudRenown, "Renown {0}"),
            (HudLastOrders, "Last orders!"),
            (HudMenu, "Menu"),
            (HudSoldOut, "Sold out"),
            (HudOrders, "Orders"),
            (TicketQueued, "Waiting"),
            (TicketCooking, "Cooking"),
            (TicketReady, "On the pass"),
            (TicketDelivering, "Serving"),

            (HintCook, "{0}: cook {1}"),
            (HintPickUp, "{0}: pick up {1}"),
            (HintStaffed, "{0} is working here"),
            (HintStepAway, "{0}: step away"),
            (Spill, "Spill"),

            (GrillPrompt, "{0}: flip while the meter is in the golden band"),
            (GrillSide, "Side {0} of {1}"),
            (TapPrompt, "Hold {0} to pour to the line · tilt with {1} to control the foam"),

            (ResultsTitle, "Service Over"),
            (ResultsServed, "Dishes served: {0}"),
            (ResultsGold, "Gold earned: {0}"),
            (ResultsTips, "Tips: {0}"),
            (ResultsRenown, "Renown: {0}"),
            (ResultsWalkouts, "Walkouts: {0}"),
            (ResultsSoldOut, "Left because we'd sold out: {0}"),
            (ResultsDropped, "Plates dropped: {0}"),
            (ResultsAgain, "Prepare another evening"),
        };
    }
}
