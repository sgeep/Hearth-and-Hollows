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

        public static string Furniture(string id) => $"furniture.{id}";

        public static readonly (string key, string english)[] English =
        {
            (Title, "decorating the Sunken Flagon"),
            (StatusReady, "ready for service"),
            (StatusProblem, "1 problem"),
            (StatusProblems, "{0} problems"),
            (StatusWarnings, "ready, with {0} to look at"),
            (Hover, "{0}"),
            (HoverMore, "{0} · {1} more here"),
            (Carrying, "carrying: {0}"),
            (CantGo, "{0} can't go there: {1}"),
            (Empty, "an empty spot"),
            (Controls, "{0} pick up · {1} turn · {2} flip · {3} put away · {4} undo · {5} storage · {6} check · {7} done"),
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
