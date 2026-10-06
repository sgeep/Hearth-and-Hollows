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
        public const string PrepRecipeDetailsStew = "tavern.prep.recipe_details_stew";
        public const string PrepStaff = "tavern.prep.staff";
        public const string PrepStaffOff = "tavern.prep.staff_off";
        public const string PrepOpen = "tavern.prep.open";
        public const string PrepTonight = "tavern.prep.tonight";
        /// <summary>Text that's already localized (a dish name, a list of steps), shown as it is.</summary>
        public const string Plain = "tavern.plain";
        public const string PrepServings = "tavern.prep.servings";
        public const string PrepPots = "tavern.prep.pots";
        public const string PrepPot = "tavern.prep.pot";
        public const string PrepDetail = "tavern.prep.detail";
        public const string PrepNone = "tavern.prep.none";
        public const string PrepValue = "tavern.prep.value";
        public const string PrepValueStew = "tavern.prep.value_stew";
        public const string PrepThen = "tavern.prep.then";
        public const string StepChop = "tavern.step.chop";
        public const string StepSimmer = "tavern.step.simmer";
        public const string PrepStaffJob = "tavern.prep.staff_job";
        // 4f Checkpoint C: the Butcher Block at Prep, Gunta's job, the menu's pages, Pip's ledger.
        public const string StationButcherBlock = "station.butcher_block";
        public const string PrepButcher = "tavern.prep.butcher";
        public const string PrepPage = "tavern.prep.page";
        public const string ButcherTitle = "butcher.title";
        public const string ButcherIntro = "butcher.intro";
        public const string ButcherYield = "butcher.yield";
        public const string ButcherYourself = "butcher.yourself";
        public const string ButcherStaff = "butcher.staff";
        public const string ButcherNone = "butcher.none";
        public const string ButcherNoBlock = "butcher.no_block";
        public const string ButcherBusy = "butcher.busy";
        public const string ButcherWorking = "butcher.working";
        public const string ButcherResult = "butcher.result";
        public const string ButcherDone = "butcher.done";
        public const string ButcherPanelTitle = "butcher.panel_title";
        public const string ButcherPrompt = "butcher.prompt";
        public const string ResultsLedger = "results.ledger";
        public const string PrepNothingCookable = "tavern.prep.nothing_cookable";
        public const string PrepFillKey = "tavern.prep.fill_key";
        public const string ResultsStayedShut = "results.stayed_shut";
        public const string ResultsTakings = "results.takings";

        public const string StationGrill = "station.grill";
        public const string StationTap = "station.tap";
        public const string StationServing = "station.serving";
        public const string StationStewPot = "station.stew_pot";
        public const string StationPass = "station.pass";

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
        public const string TicketSpare = "ticket.spare";
        public const string TicketStewWaiting = "ticket.stew_waiting";

        public const string HintCook = "tavern.hint.cook";
        public const string HintUse = "tavern.hint.use";
        public const string TavernControls = "tavern.controls";
        public const string HintPickUp = "tavern.hint.pick_up";
        public const string HintServe = "tavern.hint.serve";
        public const string HintPutBack = "tavern.hint.put_back";
        public const string HintWrongDish = "tavern.hint.wrong_dish";
        public const string HintStartStew = "tavern.hint.start_stew";
        public const string HintSimmering = "tavern.hint.simmering";
        public const string HintStewReady = "tavern.hint.stew_ready";
        public const string HintStaffed = "tavern.hint.staffed";
        public const string HintStepAway = "tavern.hint.step_away";
        public const string Spill = "serving.spill";

        public const string GrillPrompt = "grill.prompt";
        public const string GrillSide = "grill.side";
        public const string TapPrompt = "tap.prompt";
        public const string ChopTitle = "chop.title";
        public const string ChopProgress = "chop.progress";
        public const string ChopPrompt = "chop.prompt";

        public const string ResultsTitle = "results.title";
        public const string ResultsClosedEarly = "results.closed_early";
        public const string ResultsServed = "results.served";
        public const string ResultsGold = "results.gold";
        public const string ResultsTips = "results.tips";
        public const string ResultsRenown = "results.renown";
        // 4f Checkpoint D: special requests on the evening's results.
        public const string ResultsRequests = "results.requests";
        public const string ResultsRequestsOf = "results.requests_of";
        public const string ResultsWalkouts = "results.walkouts";
        public const string ResultsSoldOut = "results.sold_out";
        public const string ResultsDropped = "results.dropped";
        public const string ResultsAgain = "results.again";

        public static readonly (string key, string english)[] English =
        {
            (PrepTitle, "evening prep"),
            (PrepStoreroom, "storeroom"),
            (PrepStoreroomEmpty, "empty"),
            (PrepStockRow, "{0} ×{1} · {2}% fresh"),
            (PrepFill, "fill storeroom (debug)"),
            (PrepMenu, "tonight's menu (up to {0})"),
            (PrepRecipeDetails, "{0} · {1} gold · {2} servings"),
            (PrepRecipeDetailsStew, "{0} · {1} gold a helping · {2} pots of {3}-{4}"),
            (PrepStaff, "staff helper: {0}"),
            (PrepStaffOff, "off duty"),
            (PrepOpen, "open the doors"),
            (PrepTonight, "menu: {0} of {1}"),
            (Plain, "{0}"),
            (PrepServings, "{0} to serve"),
            (PrepPots, "{0} pots"),
            (PrepPot, "1 pot"),
            (PrepDetail, "{0}, {1}"),
            (PrepNone, "not in stock"),
            (PrepValue, "{0} gold"),
            (PrepValueStew, "{0} gold/bowl"),
            (PrepThen, "{0}, {1}"),
            (StepChop, "chop"),
            (StepSimmer, "simmer"),
            (PrepStaffJob, "{0}: {1}"),
            (StationButcherBlock, "butcher block"),
            (PrepButcher, "butcher block"),
            (PrepPage, "dishes {0}/{1}"),
            (ButcherTitle, "the butcher block"),
            (ButcherIntro, "cut a part yourself, or hand it to {0}. a cleaner cut gives more cuts."),
            (ButcherYield, "up to {0}"),
            (ButcherYourself, "cut it"),
            (ButcherStaff, "{0}"),
            (ButcherNone, "nothing here to butcher: spider legs and bat wings break down into cuts."),
            (ButcherNoBlock, "the butcher block is in storage: put it out in decorate."),
            (ButcherBusy, "{0} is busy."),
            (ButcherWorking, "{0} is at the block..."),
            (ButcherResult, "+{0} {1}"),
            (ButcherDone, "done"),
            (ButcherPanelTitle, "butcher the {0}"),
            (ButcherPrompt, "move the knife · hold {0}: cut"),
            (ResultsLedger, "from Pip's ledger"),
            (PrepNothingCookable, "nothing in the storeroom makes a dish tonight."),
            (PrepFillKey, "F4: fill storeroom"),

            (StationGrill, "grill"),
            (StationPass, "the pass"),
            (StationTap, "tap"),
            (StationServing, "serving"),
            (StationStewPot, "stew pot"),

            (HudTime, "time left {0}"),
            (HudGold, "gold"),
            (HudTips, "tips"),
            (HudRenown, "Renown"),
            (HudLastOrders, "last orders!"),
            (HudMenu, "menu"),
            (HudSoldOut, "sold out"),
            (HudOrders, "orders"),
            (TicketQueued, "waiting"),
            (TicketCooking, "cooking"),
            (TicketReady, "ready"),
            (TicketDelivering, "serving"),
            (TicketSpare, "spare"),
            (TicketStewWaiting, "stewing"),

            (HintCook, "{0}: cook {1}"),
            (HintUse, "{0}: {1}"),
            (TavernControls, "move: WASD / stick   use: E / A"),
            (HintPickUp, "{0}: pick up {1}"),
            (HintServe, "{0}: serve {1}"),
            (HintPutBack, "{0}: put {1} back on the pass"),
            (HintWrongDish, "they ordered {0}"),
            (HintStartStew, "{0}: make {1}"),
            (HintSimmering, "{0} is simmering"),
            (HintStewReady, "{0}: {1} helpings left"),
            (HintStaffed, "{0} is working here"),
            (HintStepAway, "{0}: step away"),
            (Spill, "spill"),

            (GrillPrompt, "{0}: flip in the gold band"),
            (GrillSide, "side {0} of {1}"),
            (TapPrompt, "{0}: pour · {1}: tilt, head in the band"),
            (ChopTitle, "chop the {0}"),
            (ChopProgress, "ingredient {0} of {1}"),
            (ChopPrompt, "{0}: move the knife · {1}: chop"),

            (ResultsTitle, "service over"),
            (ResultsClosedEarly, "everything sold out, so we closed early."),
            (ResultsServed, "dishes served"),
            (ResultsGold, "gold earned"),
            (ResultsTips, "tips"),
            (ResultsRenown, "Renown"),
            (ResultsRequests, "special requests"),
            (ResultsRequestsOf, "{0} of {1}"),
            (ResultsWalkouts, "walkouts"),
            (ResultsSoldOut, "left, sold out"),
            (ResultsDropped, "plates dropped"),
            (ResultsAgain, "prepare another evening"),
            (ResultsStayedShut, "we kept the doors shut tonight."),
            (ResultsTakings, "takings"),
        };
    }
}
