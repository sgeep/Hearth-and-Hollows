namespace Hearthdelve.UI.Localization
{
    /// <summary>UI-table keys for Decorate Mode and the furniture (4f). English text is the source of truth (synced by the editor).</summary>
    public static class DecorateLocKeys
    {
        public const string Title = "decorate.title";
        public const string StatusReady = "decorate.status.ready";
        public const string StatusProblems = "decorate.status.problems";
        public const string StatusProblem = "decorate.status.problem";
        public const string StatusWarnings = "decorate.status.warnings";
        public const string StatusRoomReady = "decorate.status.room_ready";
        public const string Hover = "decorate.hover";
        public const string HoverMore = "decorate.hover_more";
        public const string Carrying = "decorate.carrying";
        public const string CantGo = "decorate.cant_go";
        public const string Empty = "decorate.empty";
        /// <summary>The controls while browsing, with each binding filled in for the device in use.</summary>
        public const string Controls = "decorate.controls";
        /// <summary>The controls while carrying a piece.</summary>
        public const string CarryControls = "decorate.carry";
        public const string Storage = "decorate.storage";
        public const string StorageEmpty = "decorate.storage.empty";
        public const string StorageRow = "decorate.storage.row";
        public const string Check = "decorate.check";
        public const string CheckClear = "decorate.check.clear";
        public const string PutAllBack = "decorate.put_all_back";
        public const string Done = "decorate.done";
        public const string Button = "decorate.button";

        public const string ProblemOutside = "decorate.problem.outside";
        public const string ProblemWall = "decorate.problem.wall";
        public const string ProblemBackWall = "decorate.problem.back_wall";
        public const string ProblemOverlaps = "decorate.problem.overlaps";
        public const string ProblemEntrance = "decorate.problem.entrance";
        public const string ProblemSurface = "decorate.problem.surface";
        public const string ProblemTurn = "decorate.problem.turn";

        public const string IssueEntrance = "decorate.issue.entrance";
        public const string IssueStationMissing = "decorate.issue.station_missing";
        public const string IssueStationUnreachable = "decorate.issue.station_unreachable";
        public const string IssuePassMissing = "decorate.issue.pass_missing";
        public const string IssuePassUnreachable = "decorate.issue.pass_unreachable";
        public const string IssueNoSeats = "decorate.issue.no_seats";
        public const string IssueSeatsUnreachable = "decorate.issue.seats_unreachable";
        public const string IssueSeatUnreachable = "decorate.issue.seat_unreachable";
        public const string IssueStaff = "decorate.issue.staff";
        public const string IssueQueue = "decorate.issue.queue";
        public const string PrepCantOpen = "decorate.prep.cant_open";

        public const string StationGrill = "decorate.station.grill";
        public const string StationTap = "decorate.station.tap";
        public const string StationStewPot = "decorate.station.stew_pot";
        public const string StationButcherBlock = "decorate.station.butcher_block";

        // Areas (4f step 6).
        public const string AreaTavern = "decorate.area.tavern";
        public const string AreaGuestRoom = "decorate.area.guest_room";

        // The catalogue (4f step 4).
        public const string CatalogPurse = "catalog.purse";
        public const string TabStorage = "catalog.tab.storage";
        public const string TabRoom = "catalog.tab.room";
        public const string RowPrice = "catalog.row.price";
        public const string RowStored = "catalog.row.stored";
        public const string RowLocked = "catalog.row.locked";
        public const string RowNotSold = "catalog.row.not_sold";
        public const string RowInUse = "catalog.row.in_use";
        public const string RowOwned = "catalog.row.owned";
        public const string Counts = "catalog.counts";
        public const string NeedsRenown = "catalog.needs_renown";
        public const string FromTier = "catalog.from_tier";
        public const string Unique = "catalog.unique";
        public const string Looks = "catalog.looks";
        public const string SourceStarter = "catalog.source.starter";
        public const string SourceBought = "catalog.source.bought";
        public const string SourceDiscovery = "catalog.source.discovery";
        public const string SourceBoss = "catalog.source.boss";
        public const string SourceStory = "catalog.source.story";
        public const string ActionPlace = "catalog.action.place";
        public const string ActionBuyPlace = "catalog.action.buy_place";
        public const string ActionBuy = "catalog.action.buy";
        public const string ActionSell = "catalog.action.sell";
        public const string ActionUse = "catalog.action.use";
        public const string CatalogControls = "catalog.controls";
        public const string EmptyTab = "catalog.empty";
        public const string CantAfford = "catalog.cant_afford";
        public const string Locked = "catalog.locked";
        public const string AlreadyOwned = "catalog.already_owned";
        public const string Bought = "catalog.bought";
        public const string Sold = "catalog.sold";
        public const string FloorKind = "catalog.finish.floor";
        public const string WallKind = "catalog.finish.wall";
        public const string FinishRow = "catalog.finish.row";

        // The colour panel (4f step 5).
        public const string StyleTitle = "style.title";
        public const string StyleVariant = "style.row.variant";
        public const string StylePresets = "style.row.presets";
        public const string StyleAsDrawn = "style.as_drawn";
        public const string StyleCopy = "style.copy";
        public const string StyleApplyAll = "style.apply_all";
        public const string StyleControls = "style.controls";
        public const string StyleNothing = "style.nothing";
        public const string StyleChoice = "style.choice";
        public static string Channel(string kind) => $"style.channel.{kind}";

        public static string Furniture(string id) => $"furniture.{id}";
        public static string FurnitureDescription(string id) => $"furniture.{id}.description";
        public static string Category(Hearthdelve.Shared.Customization.FurnitureCategory c) => $"catalog.category.{c}";

        public static readonly (string key, string english)[] English =
        {
            (Title, "decorating {0}"),
            (StatusReady, "ready for service"),
            (StatusProblem, "1 problem"),
            (StatusProblems, "{0} problems"),
            (StatusWarnings, "ready, with {0} to look at"),
            (StatusRoomReady, "all good"),
            (Hover, "{0}"),
            (HoverMore, "{0} · {1} more here"),
            (Carrying, "carrying: {0}"),
            (CantGo, "{0} can't go there: {1}"),
            (Empty, "an empty spot"),
            (Controls, "{0} pick up · {1} turn · {2} flip · {3} put away · {4} undo · {5} catalogue · {6} colors · {7} other room · {8} check · {9} done"),
            (CarryControls, "{0} put down · {1} turn · {2} flip · {3} put away · {4} put back · hold {5}: free placement"),
            (Storage, "in storage"),
            (StorageEmpty, "nothing in storage"),
            (StorageRow, "{0} ×{1}"),
            (Check, "the layout"),
            (CheckClear, "all good: the doors can open"),
            (PutAllBack, "put it all back"),
            (Done, "done"),
            (Button, "decorate"),

            (ProblemOutside, "it's off the floor"),
            (ProblemWall, "it hangs on the back wall"),
            (ProblemBackWall, "it stands against the back wall"),
            (ProblemOverlaps, "something's already there"),
            (ProblemEntrance, "it would block the entrance"),
            (ProblemSurface, "it needs a table or a shelf"),
            (ProblemTurn, "it doesn't stand that way"),

            (IssueEntrance, "the entrance is blocked"),
            (IssueStationMissing, "there's no {0} out"),
            (IssueStationUnreachable, "the {0} can't be reached"),
            (IssuePassMissing, "there's no pass out"),
            (IssuePassUnreachable, "the pass can't be reached"),
            (IssueNoSeats, "no chair faces a table"),
            (IssueSeatUnreachable, "1 seat can't be reached"),
            (IssueSeatsUnreachable, "{0} seats can't be reached"),
            (IssueStaff, "staff can't reach their work"),
            (IssueQueue, "furniture stands where customers queue"),
            (PrepCantOpen, "the doors can't open: {0}"),

            (StationGrill, "grill"),
            (StationTap, "tap"),
            (StationStewPot, "stew pot"),
            (StationButcherBlock, "butcher block"),

            (AreaTavern, "the Sunken Flagon"),
            (AreaGuestRoom, "the guest room"),

            (CatalogPurse, "{0} gold · Renown {1}"),
            (TabStorage, "in storage"),
            (TabRoom, "walls and floors"),
            (RowPrice, "{0} gold"),
            (RowStored, "{0} stored"),
            (RowLocked, "Renown {0}"),
            (RowNotSold, "not for sale"),
            (RowInUse, "in use"),
            (RowOwned, "owned"),
            (Counts, "{1} placed · {2} stored"),
            (NeedsRenown, "opens at Renown {0}"),
            (FromTier, "{1}"),
            (Unique, "one of a kind"),
            (Looks, "{0} looks"),
            (SourceStarter, "from the start"),
            (SourceBought, "made in Brackenford"),
            (SourceDiscovery, "found in the Hollows"),
            (SourceBoss, "a trophy"),
            (SourceStory, "a gift"),
            (ActionPlace, "{0} place one"),
            (ActionBuyPlace, "{0} buy and place"),
            (ActionBuy, "{0} buy one"),
            (ActionSell, "{0} sell: {1} gold"),
            (ActionUse, "{0} use here"),
            (CatalogControls, "{0} next page · {1} close"),
            (EmptyTab, "nothing here yet"),
            (CantAfford, "not enough gold"),
            (Locked, "needs Renown {0}"),
            (AlreadyOwned, "you have one already"),
            (Bought, "bought: {0}"),
            (Sold, "sold: {0}"),
            (FloorKind, "floor"),
            (WallKind, "wall"),
            (FinishRow, "{0}, {1}"),
            ("catalog.category.Seating", "seating"),
            ("catalog.category.Tables", "tables"),
            ("catalog.category.BarAndStorage", "bar and storage"),
            ("catalog.category.Lighting", "lighting"),
            ("catalog.category.WallDecor", "on the walls"),
            ("catalog.category.FloorDecor", "rugs"),
            ("catalog.category.Plants", "plants"),
            ("catalog.category.Curios", "curios"),
            ("catalog.category.Bedroom", "bedroom"),
            ("catalog.category.Stations", "stations"),

            (StyleTitle, "colors: {0}"),
            (StyleVariant, "style"),
            (StylePresets, "schemes"),
            (StyleAsDrawn, "as drawn"),
            (StyleCopy, "use last colors"),
            (StyleApplyAll, "match every copy"),
            (StyleControls, "{0} choose · {1} use · {2} done"),
            (StyleNothing, "{0} comes in one look"),
            (StyleChoice, "{0}: {1}"),
            (Channel("wood"), "wood"),
            (Channel("cushion"), "cushion"),

            (FurnitureDescription("tavern_bar"), "the bar and its taps, where every evening starts."),
            (FurnitureDescription("kitchen_range"), "the kitchen range. it stands against the back wall, where its chimney goes."),
            (FurnitureDescription("stew_pot"), "a cauldron over a floor fire. the stew in it has opinions."),
            (FurnitureDescription("pass_table"), "the pass, where plates wait to be carried out."),
            (FurnitureDescription("table_round_a"), "a round tavern table with room for two chairs."),
            (FurnitureDescription("table_round_b"), "a round tavern table in a darker wood."),
            (FurnitureDescription("tavern_chair"), "a sturdy tavern chair with a red cushion."),
            (FurnitureDescription("cellar_barrel"), "a barrel from the cellar. full of something."),
            (FurnitureDescription("bottle_shelves"), "shelves of bottles behind the bar."),
            (FurnitureDescription("wall_sign"), "the tavern's painted sign."),
            (FurnitureDescription("low_shelf"), "a low shelf for glasses and odds and ends."),
            (FurnitureDescription("shelf_glasses"), "a row of glasses, mostly clean."),
            (FurnitureDescription("wall_fireplace"), "a small fire in the wall. warm, and nearly safe."),

            (Furniture("tavern_bar"), "bar and taps"),
            (Furniture("kitchen_range"), "kitchen range"),
            (Furniture("stew_pot"), "stew pot"),
            (Furniture("pass_table"), "pass"),
            (Furniture("table_round_a"), "round table"),
            (Furniture("table_round_b"), "round table, dark"),
            (Furniture("tavern_chair"), "tavern chair"),
            (Furniture("cellar_barrel"), "cellar barrel"),
            (Furniture("bottle_shelves"), "bottle shelves"),
            (Furniture("wall_sign"), "tavern sign"),
            (Furniture("low_shelf"), "low shelf"),
            (Furniture("shelf_glasses"), "row of glasses"),
            (Furniture("wall_fireplace"), "wall fire"),
        };
    }
}
