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
        public const string MorningBreakfast = "morning.breakfast";
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
        public const string TransitionEvening = "transition.evening";
        public const string MenuContinueFrom = "menu.continue_from";
        public const string PhaseMorning = "phase.morning";
        public const string PhaseEvening = "phase.evening";
        public const string PhaseNight = "phase.night";
        public const string MenuConfirm = "menu.confirm";
        public const string MenuConfirmYes = "menu.confirm_yes";
        public const string MenuConfirmNo = "menu.confirm_no";

        public static readonly (string key, string english)[] English =
        {
            (MenuTitle, "Hearthdelve"),
            (MenuNewGame, "New Game"),
            (MenuContinue, "Continue"),
            (MenuNoSave, "No saved game yet."),
            (MenuQuit, "Quit"),

            (HudDay, "Day {0}"),
            (HudExit, "Press {0} to climb back to the tavern"),
            (SlotFreshness, "{0}% fresh"),

            (MorningTitle, "Morning · Day {0}"),
            (MorningStoreroom, "Storeroom"),
            (MorningBreakfast, "Breakfast (one dish, eaten before you go)"),
            (MorningBreakfastRow, "{0} · {1}"),
            (MorningNoBreakfast, "Nothing in the storeroom makes a breakfast."),
            (MorningAte, "Ate {0}: {1}"),
            (MorningCooking, "Cooking breakfast..."),
            (MorningLoadout, "Today's delve bonuses: +{0} satchel slots · +{1} max Essence · Essence drain {2}%"),
            (MorningDescend, "Descend into the dungeon"),

            (BuffMaxEssence, "+{0} max Essence"),
            (BuffSlowerDrain, "Essence drains {0}% slower"),

            (PrepClose, "Close for the night"),
            (ResultsToNight, "Close up for the night"),

            (NightTitle, "Night · Day {0}"),
            (NightPurse, "Gold: {0} · Renown: {1}"),
            (NightUpgrades, "Upgrades"),
            (NightUpgradeLevel, "{0} · level {1} of {2}"),
            (NightBuy, "Buy {0} · {1} gold"),
            (NightMaxed, "Fully upgraded"),
            (NightSleep, "Sleep"),
            (NightSaved, "Game saved."),

            (UpgradeSatchel, "+{0} satchel slot"),
            (UpgradeEssence, "+{0} max Essence"),
            (UpgradeSeats, "+{0} seat"),

            (SummaryTitle, "Today"),
            (SummaryDelve, "Delve: {0}"),
            (SummaryExtracted, "made it out"),
            (SummaryDied, "Essence ran out"),
            (SummarySkipped, "skipped"),
            (SummaryParts, "Parts brought back: {0} (lost: {1})"),
            (SummaryDishes, "Dishes served: {0} · walkouts: {1}"),
            (SummaryEarned, "Earned: {0} gold + {1} tips = {2}"),
            (SummaryNextUpgrade, "Cheapest next upgrade: {0} gold"),
            (SummaryAllBought, "Every upgrade is bought."),
            (SummaryShut, "The doors stayed shut tonight."),
            (NightBanked, "Banked tonight: {0} gold"),
            (NightRenownToday, "Renown today: {0}"),
            (NightBuyCost, "{0} gold"),
            (NightNextTime, "Next: {0}"),
            (MorningBonuses, "Today's delve: {0}"),
            (MorningNoBonuses, "Today's delve: no bonuses yet. Upgrades and breakfast add them."),
            (BonusSatchel, "satchel +{0}"),
            (BonusEssence, "Essence +{0}"),
            (BonusDrain, "drain -{0}%"),
            (BonusJoin, "{0} · {1}"),
            (MorningCookAt, "Cook at the {0}"),
            (TransitionDelve, "Into the dungeon"),
            (TransitionEvening, "Evening · Day {0}"),
            (MenuContinueFrom, "Day {0}, {1}"),
            (PhaseMorning, "morning"),
            (PhaseEvening, "evening"),
            (PhaseNight, "night"),
            (MenuConfirm, "Start a new game? Your saved game will be replaced."),
            (MenuConfirmYes, "Start over"),
            (MenuConfirmNo, "Back"),
        };
    }
}
