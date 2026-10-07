namespace Hearthdelve.UI.Localization
{
    /// <summary>UI table keys for the surface day (4h Checkpoint A): the clock, the places in Tally Ho!, the market stall, Prep.</summary>
    public static class SurfaceLocKeys
    {
        /// <summary>"8:00 am": the hour, the minutes (two digits), and the morning or afternoon word.</summary>
        public const string Clock = "surface.clock";
        public const string Am = "surface.am";
        public const string Pm = "surface.pm";

        public const string Storeroom = "surface.storeroom";
        public const string DelveMeal = "surface.delve_meal";
        public const string MenuBoard = "surface.menu_board";
        public const string Plans = "surface.plans";
        public const string LookPortrait = "surface.look.portrait";
        public const string LookMemorial = "surface.look.memorial";
        public const string Market = "surface.market";
        public const string MarketClosed = "surface.market_closed";

        public const string PanelStoreroom = "surface.panel.storeroom";
        public const string PanelMeal = "surface.panel.meal";
        public const string PanelBack = "surface.panel.back";

        /// <summary>"Tab: decorate", under the clock while indoors.</summary>
        public const string DecorateHint = "surface.decorate_hint";
        public const string PrepQuestion = "surface.prep.question";
        public const string PrepYes = "surface.prep.yes";
        public const string PrepNo = "surface.prep.no";

        public static readonly (string key, string english)[] English =
        {
            (Clock, "{0}:{1} {2}"),
            (Am, "am"),
            (Pm, "pm"),
            (Storeroom, "check the storeroom"),
            (DelveMeal, "cook a delve meal"),
            (MenuBoard, "begin evening prep"),
            (Plans, "decorate"),
            (LookPortrait, "look at the portrait"),
            (LookMemorial, "read the memorial"),
            ("surface.look.hatch", "look at the hatch"),
            (Market, "browse the market"),
            (MarketClosed, "the market's packed up till morning"),
            (PanelStoreroom, "storeroom"),
            (PanelMeal, "tonight's delve meal"),
            (PanelBack, "back"),
            (DecorateHint, "{0}: decorate"),
            (PrepQuestion, "begin evening prep? the rest of the day goes by."),
            (PrepYes, "begin prep"),
            (PrepNo, "not yet"),
        };
    }
}
