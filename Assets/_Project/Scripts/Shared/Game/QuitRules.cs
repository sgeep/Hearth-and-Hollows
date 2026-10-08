using Hearthdelve.Shared.Story;

namespace Hearthdelve.Shared.Game
{
    /// <summary>What quitting to the menu does at a moment of the day (4i-A, decision D2; pure, EditMode-tested).</summary>
    public enum QuitKind
    {
        /// <summary>The free day or the night: the game saves now and Continue comes back to this moment.</summary>
        SaveAndQuit,
        /// <summary>Arrival day: nothing of it is saved mid-way; Continue begins arrival day again (the new game's own save).</summary>
        RestartArrival,
        /// <summary>
        /// Evening prep or service: the evening is never saved part-way (its customers, orders and stations aren't state), so
        /// Continue comes back to the last save, normally the day as it was left when the evening began. Asks first.
        /// </summary>
        DiscardEvening,
        /// <summary>The evening's results are showing: the takings are banked exactly as closing up would, then Continue is the night's delve.</summary>
        BankEvening,
        /// <summary>Mid-delve: the delve is abandoned (its haul and gold, never banked); Continue offers the night's delve again. Asks first.</summary>
        AbandonDelve,
        /// <summary>The delve is over and its result is showing: it's brought home exactly as the result screen's button would, then Continue is the night.</summary>
        BankDelve,
    }

    /// <summary>The plan for one quit: what happens, whether it asks first, and whether the game saves on the way out.</summary>
    public readonly struct QuitPlan
    {
        public readonly QuitKind Kind;
        public QuitPlan(QuitKind kind) => Kind = kind;

        /// <summary>Some progress since the last save is given up: the player is asked, with what will be lost.</summary>
        public bool Warns => Kind is QuitKind.DiscardEvening or QuitKind.AbandonDelve or QuitKind.RestartArrival;

        /// <summary>The game saves on the way out (after banking, for the two that bank).</summary>
        public bool Saves => Kind is QuitKind.SaveAndQuit or QuitKind.BankEvening or QuitKind.BankDelve;
    }

    /// <summary>
    /// Quitting to the menu, by the part of the day (4i-A, D2). Only the moments the save already supports are resume points (the
    /// day at any minute, the start of the night's delve, the night); anything part-way through is either banked as the game would
    /// bank it, or returned to the last save with a warning, never serialized half-done.
    /// </summary>
    public static class QuitRules
    {
        /// <param name="phase">The day's phase.</param>
        /// <param name="opening">Where the Act I opening is.</param>
        /// <param name="tavernPhase">In the evening, the tavern's part of it ("Prep", "Service", "Results").</param>
        /// <param name="delveOver">In the delve, whether the run has ended (its result showing).</param>
        public static QuitPlan Plan(DayPhase phase, OpeningStage opening, string tavernPhase, bool delveOver) => new(phase switch
        {
            DayPhase.Daytime when opening == OpeningStage.Arrival => QuitKind.RestartArrival,
            DayPhase.Daytime => QuitKind.SaveAndQuit,
            DayPhase.Evening when tavernPhase == "Results" => QuitKind.BankEvening,
            DayPhase.Evening => QuitKind.DiscardEvening,
            DayPhase.Delve when delveOver => QuitKind.BankDelve,
            DayPhase.Delve => QuitKind.AbandonDelve,
            _ => QuitKind.SaveAndQuit,
        });
    }
}
