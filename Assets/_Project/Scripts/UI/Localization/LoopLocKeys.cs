namespace Hearthdelve.UI.Localization
{
    /// <summary>UI-table keys for the day loop (Phase 3). English text is the source of truth (synced by the editor).</summary>
    public static class LoopLocKeys
    {
        public const string MenuTitle = "menu.title";
        public const string MenuNewGame = "menu.new_game";
        public const string MenuContinue = "menu.continue";
        public const string MenuNoSave = "menu.no_save";
        public const string MenuQuit = "menu.quit";

        public const string HudDay = "hud.day";
        public const string HudExit = "hud.exit";
        public const string SlotFreshness = "slot.freshness";

        public const string MorningTitle = "morning.title";
        public const string MorningStoreroom = "morning.storeroom";
        public const string MorningBreakfast = "morning.delve meal";
        public const string MorningBreakfastRow = "morning.breakfast_row";
        public const string MorningNoBreakfast = "morning.no_breakfast";
        public const string MorningAte = "morning.ate";
        public const string MorningCooking = "morning.cooking";
        public const string MorningLoadout = "morning.loadout";
        public const string MorningDescend = "morning.descend";

        public const string BuffMaxEssence = "buff.max_essence";
        public const string BuffSlowerDrain = "buff.slower_drain";

        public const string PrepClose = "tavern.prep.close";
        public const string ResultsToNight = "results.to_night";

        public const string NightTitle = "night.title";
        public const string NightPurse = "night.purse";
        public const string NightUpgrades = "night.upgrades";
        public const string NightUpgradeLevel = "night.upgrade_level";
        public const string NightBuy = "night.buy";
        public const string NightMaxed = "night.maxed";
        public const string NightSleep = "night.sleep";
        public const string NightSaved = "night.saved";

        public const string UpgradeSatchel = "upgrade.effect_satchel";
        public const string UpgradeEssence = "upgrade.effect_essence";
        public const string UpgradeSeats = "upgrade.effect_seats";

        public const string SummaryTitle = "summary.title";
        public const string SummaryDelve = "summary.delve";
        public const string SummaryExtracted = "summary.extracted";
        public const string SummaryDied = "summary.died";
        public const string SummarySkipped = "summary.skipped";
        public const string SummaryParts = "summary.parts";
        public const string SummaryDishes = "summary.dishes";
        public const string SummaryEarned = "summary.earned";
        public const string SummaryNextUpgrade = "summary.next_upgrade";
        public const string SummaryAllBought = "summary.all_bought";
        public const string SummaryShut = "summary.shut";
        public const string SummaryEvening = "summary.evening";
        public const string SummaryPartsValue = "summary.parts_value";
        public const string SummaryDishesValue = "summary.dishes_value";
        public const string NightRenownValue = "night.renown_value";
        public const string NightBanked = "night.banked";
        public const string NightRenownToday = "night.renown_today";
        public const string NightBuyCost = "night.buy_cost";
        public const string NightNextTime = "night.next_time";
        public const string MorningBonuses = "morning.bonuses";
        public const string MorningNoBonuses = "morning.no_bonuses";
        public const string BonusSatchel = "bonus.satchel";
        public const string BonusEssence = "bonus.essence";
        public const string BonusDrain = "bonus.drain";
        public const string BonusJoin = "bonus.join";
        public const string MorningCookAt = "morning.cook_at";
        public const string TransitionDelve = "transition.delve";
        /// <summary>A delve with no evening before it this session: a new game's first, or one resumed from a save.</summary>
        public const string TransitionFirstDelve = "transition.first_delve";
        public const string TransitionEvening = "transition.evening";
        public const string MenuContinueFrom = "menu.continue_from";
        public const string PhaseMorning = "phase.morning";
        public const string PhaseEvening = "phase.evening";
        public const string PhaseNight = "phase.night";
        public const string PhaseDelve = "phase.delve";
        public const string MenuConfirm = "menu.confirm";
        public const string MenuConfirmYes = "menu.confirm_yes";
        public const string MenuConfirmNo = "menu.confirm_no";

        public static readonly (string key, string english)[] English =
        {
            (MenuTitle, "Hearth & Hollows"),
            (MenuNewGame, "new game"),
            (MenuContinue, "continue"),
            (MenuNoSave, "no saved game yet."),
            (MenuQuit, "quit"),

            (HudDay, "day {0}"),
            (HudExit, "press {0} to climb back to the tavern"),
            (SlotFreshness, "{0}% fresh"),

            (MorningTitle, "daytime · day {0}"),
            (MorningStoreroom, "storeroom"),
            (MorningBreakfast, "delve meal (one dish, for tonight's delve)"),
            (MorningBreakfastRow, "{0} · {1}"),
            (MorningNoBreakfast, "nothing in the storeroom makes a delve meal."),
            (MorningAte, "ate {0}: {1}"),
            (MorningCooking, "cooking the delve meal..."),
            (MorningLoadout, "tonight's delve bonuses: +{0} satchel slots · +{1} max Essence · Essence drain {2}%"),
            (MorningDescend, "open for the evening"),

            (BuffMaxEssence, "+{0} max Essence"),
            (BuffSlowerDrain, "Essence drain -{0}%"),

            (PrepClose, "stay shut tonight"),
            (ResultsToNight, "close up and head below"),

            (NightTitle, "night · day {0}"),
            (NightPurse, "purse"),
            (NightUpgrades, "upgrades"),
            (NightUpgradeLevel, "{0} · level {1} of {2}"),
            (NightBuy, "buy {0} · {1} Gold"),
            (NightMaxed, "fully upgraded"),
            (NightSleep, "sleep"),
            (NightSaved, "game saved."),

            (UpgradeSatchel, "+{0} satchel slot"),
            (UpgradeEssence, "+{0} max Essence"),
            (UpgradeSeats, "+{0} seat"),

            (SummaryTitle, "today"),
            (SummaryDelve, "delve"),
            (SummaryExtracted, "made it out"),
            (SummaryDied, "Essence ran out"),
            (SummarySkipped, "skipped"),
            (SummaryParts, "parts home"),
            (SummaryDishes, "served"),
            (SummaryEarned, "earned: {0} Gold + {1} tips = {2}"),
            (SummaryNextUpgrade, "cheapest next upgrade: {0} Gold"),
            (SummaryAllBought, "every upgrade is bought."),
            (SummaryShut, "kept shut"),
            (SummaryEvening, "evening"),
            (SummaryPartsValue, "{0} ({1} lost)"),
            (SummaryDishesValue, "{0} ({1} walked out)"),
            (NightBanked, "banked today"),
            (NightRenownToday, "Renown"),
            (NightRenownValue, "{0} ({1})"),
            (NightBuyCost, "{0} Gold"),
            (NightNextTime, "next: {0}"),
            (MorningBonuses, "tonight's delve: {0}"),
            (MorningNoBonuses, "tonight's delve: no bonuses yet"),
            (BonusSatchel, "satchel +{0}"),
            (BonusEssence, "Essence +{0}"),
            (BonusDrain, "drain -{0}%"),
            (BonusJoin, "{0} · {1}"),
            (MorningCookAt, "cook at the {0}"),
            (TransitionDelve, "closing time · the Cellars"),
            (TransitionFirstDelve, "night · into the Hollows"),
            (TransitionEvening, "evening · day {0}"),
            (MenuContinueFrom, "day {0}, {1}"),
            (PhaseMorning, "daytime"),
            (PhaseEvening, "evening"),
            (PhaseDelve, "the night's delve"),
            (PhaseNight, "night"),
            (MenuConfirm, "start a new game? your saved game will be replaced."),
            (MenuConfirmYes, "start over"),
            (MenuConfirmNo, "back"),
        };
    }
}
