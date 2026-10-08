namespace Hearthdelve.UI.Localization
{
    /// <summary>
    /// 4i-A, first impressions: the main menu's additions, the pause menu, quitting, the save's problems and the "saved" mark, the
    /// controls reference, and the first free day's prompts. English is authored here (lower case; see CLAUDE.md, English text
    /// style) and written into the UI table by the localization builder.
    /// </summary>
    public static class MenuLocKeys
    {
        // ---------- the main menu ----------
        public const string Controls = "menu.controls";
        /// <summary>The main menu's quit (an existing key, "quit").</summary>
        public const string Quit = LoopLocKeys.MenuQuit;
        public const string Version = "menu.version";
        public const string BackupFrom = "menu.backup_from";
        public const string SaveUnreadable = "menu.save_unreadable";
        public const string SaveUnreadableBackup = "menu.save_unreadable_backup";
        public const string SaveNewer = "menu.save_newer";

        // ---------- the pause menu ----------
        public const string PauseTitle = "pause.title";
        public const string Resume = "pause.resume";
        public const string QuitToMenu = "pause.quit_menu";
        public const string QuitGame = "pause.quit_game";
        public const string QuitYes = "pause.quit_yes";
        public const string QuitBack = "pause.quit_back";
        public const string QuitArrival = "quit.arrival";
        public const string QuitEvening = "quit.evening";
        public const string QuitDelve = "quit.delve";
        public const string Saved = "save.saved";

        // ---------- the controls reference ----------
        public const string ControlsTitle = "controls.title";
        public const string ControlsKeyboard = "controls.keyboard";
        public const string ControlsGamepad = "controls.gamepad";
        public const string ControlsPage = "controls.page";
        public const string ControlsFooter = "controls.footer";

        public const string PageDay = "controls.page.day";
        public const string PageEvening = "controls.page.evening";
        public const string PageHollows = "controls.page.hollows";
        public const string PageDecorate = "controls.page.decorate";
        public const string PageMenus = "controls.page.menus";

        // ---------- the first free day's prompts (4i-A, D3) ----------
        public const string PromptGarden = "prompt.garden";
        public const string PromptMarket = "prompt.market";
        public const string PromptMenuBoard = "prompt.menu_board";

        /// <summary>One row of a controls page: what it does, then keyboard and mouse, then controller.</summary>
        public readonly struct Row
        {
            public readonly string Action, Keyboard, Gamepad;

            public Row(string action, string keyboard, string gamepad)
            {
                Action = action;
                Keyboard = keyboard;
                Gamepad = gamepad;
            }
        }

        /// <summary>One page: its title and rows. At most <see cref="MaxRows"/> rows, so a page fits 320×180.</summary>
        public readonly struct Page
        {
            public readonly string Title;
            public readonly Row[] Rows;

            public Page(string title, params Row[] rows)
            {
                Title = title;
                Rows = rows;
            }
        }

        public const int MaxRows = 9;

        static Row R(string id) => new($"controls.{id}", $"controls.{id}.kb", $"controls.{id}.pad");

        /// <summary>
        /// The controls reference, page by page (4i-A, A3). Written by hand from the actions asset (the tests check each page's
        /// claims against the bindings that matter most), in the words players use rather than the Input System's names.
        /// </summary>
        public static readonly Page[] Pages =
        {
            new(PageDay, R("walk"), R("look"), R("use"), R("decorate"), R("pause")),
            new(PageEvening, R("walk"), R("use_station"), R("cook"), R("cook_move"), R("step_away"), R("pause")),
            new(PageHollows, R("move"), R("aim"), R("light"), R("heavy"), R("dodge"), R("finisher"), R("interact"), R("pause")),
            new(PageDecorate, R("cursor"), R("place"), R("turn"), R("free"), R("store"), R("catalog"), R("style"), R("check"), R("cancel")),
            new(PageMenus, R("navigate"), R("choose"), R("back"), R("advance")),
        };

        public static readonly (string key, string english)[] English =
        {
            (Controls, "controls"),
            (Version, "version {0}"),
            (BackupFrom, "the backup, day {0}"),
            (SaveUnreadable, "your saved game couldn't be read."),
            (SaveUnreadableBackup, "your saved game couldn't be read. your last good save is kept as a backup."),
            (SaveNewer, "your saved game is from a newer version of Hearth & Hollows."),

            (PauseTitle, "paused"),
            (Resume, "resume"),
            (QuitToMenu, "quit to menu"),
            (QuitGame, "quit game"),
            (QuitYes, "quit"),
            (QuitBack, "back"),
            (QuitArrival, "arrival day isn't saved part-way. if you quit now, it will start again from the beginning."),
            (QuitEvening, "the evening isn't saved part-way. if you quit now, tonight's prep and service are lost, and you'll continue from just before the evening began."),
            (QuitDelve, "if you quit now, this delve is abandoned: what you carry and the delve's gold are lost. you'll begin the night's delve again."),
            (Saved, "saved"),

            (ControlsTitle, "controls"),
            (ControlsKeyboard, "keyboard"),
            (ControlsGamepad, "controller"),
            (ControlsPage, "{0} ({1}/{2})"),
            (ControlsFooter, "left / right: page · Esc / B: back"),
            (PageDay, "the day"),
            (PageEvening, "the evening"),
            (PageHollows, "the Hollows"),
            (PageDecorate, "decorating"),
            (PageMenus, "menus and talking"),

            ("controls.walk", "walk"), ("controls.walk.kb", "WASD / arrows"), ("controls.walk.pad", "left stick"),
            ("controls.look", "look around"), ("controls.look.kb", "mouse"), ("controls.look.pad", "right stick"),
            ("controls.use", "use, talk, buy"), ("controls.use.kb", "E / Space"), ("controls.use.pad", "A"),
            ("controls.decorate", "decorate"), ("controls.decorate.kb", "Tab"), ("controls.decorate.pad", "View"),
            ("controls.pause", "pause"), ("controls.pause.kb", "Esc"), ("controls.pause.pad", "Start"),

            ("controls.use_station", "use a station, serve"), ("controls.use_station.kb", "E / Space"), ("controls.use_station.pad", "A"),
            ("controls.cook", "flip, pour, chop"), ("controls.cook.kb", "Space / click"), ("controls.cook.pad", "A"),
            ("controls.cook_move", "tilt, move the knife"), ("controls.cook_move.kb", "WASD"), ("controls.cook_move.pad", "left stick"),
            ("controls.step_away", "step away"), ("controls.step_away.kb", "Esc"), ("controls.step_away.pad", "B"),

            ("controls.move", "move"), ("controls.move.kb", "WASD"), ("controls.move.pad", "left stick"),
            ("controls.aim", "aim"), ("controls.aim.kb", "mouse"), ("controls.aim.pad", "right stick"),
            ("controls.light", "light combo"), ("controls.light.kb", "click"), ("controls.light.pad", "X"),
            ("controls.heavy", "heavy spin (hold)"), ("controls.heavy.kb", "right click"), ("controls.heavy.pad", "Y"),
            ("controls.dodge", "dodge roll"), ("controls.dodge.kb", "Space"), ("controls.dodge.pad", "B"),
            ("controls.finisher", "harvest finisher"), ("controls.finisher.kb", "F"), ("controls.finisher.pad", "LT"),
            ("controls.interact", "rope, swap a part"), ("controls.interact.kb", "E"), ("controls.interact.pad", "A"),

            ("controls.cursor", "move the cursor"), ("controls.cursor.kb", "WASD / mouse"), ("controls.cursor.pad", "left stick"),
            ("controls.place", "pick up, place"), ("controls.place.kb", "E / click"), ("controls.place.pad", "A"),
            ("controls.turn", "turn / flip"), ("controls.turn.kb", "R / F"), ("controls.turn.pad", "X / Y"),
            ("controls.free", "place to the pixel"), ("controls.free.kb", "hold Shift"), ("controls.free.pad", "hold LB"),
            ("controls.store", "put away / undo"), ("controls.store.kb", "Del / Z"), ("controls.store.pad", "RT / LT"),
            ("controls.catalog", "catalog and storage"), ("controls.catalog.kb", "Tab"), ("controls.catalog.pad", "Start"),
            ("controls.style", "colors / other room"), ("controls.style.kb", "V / G"), ("controls.style.pad", "R3 / L3"),
            ("controls.check", "check the layout"), ("controls.check.kb", "C"), ("controls.check.pad", "View"),
            ("controls.cancel", "cancel, leave"), ("controls.cancel.kb", "Esc"), ("controls.cancel.pad", "B"),

            ("controls.navigate", "move"), ("controls.navigate.kb", "WASD / arrows"), ("controls.navigate.pad", "left stick"),
            ("controls.choose", "choose"), ("controls.choose.kb", "Enter / Space"), ("controls.choose.pad", "A"),
            ("controls.back", "back"), ("controls.back.kb", "Esc"), ("controls.back.pad", "B"),
            ("controls.advance", "next line"), ("controls.advance.kb", "E / click"), ("controls.advance.pad", "A"),

            (PromptGarden, "planting and tending use Vigor, the pips by the clock. walking and talking don't. sleep refills it."),
            (PromptMarket, "Musashi sells everyday ingredients here from eight until five."),
            (PromptMenuBoard, "the menu board starts evening prep whenever you choose. the day never ends it for you."),
        };
    }
}
