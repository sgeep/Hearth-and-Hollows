using System;

namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// The parts of a day (GDD §3.1, the v0.5 order): daytime, the evening's tavern service, the night's delve, and the
    /// night back home. Saved by name.
    /// </summary>
    public enum DayPhase
    {
        /// <summary>
        /// Daytime. For now a placeholder in the tavern (the storeroom, tonight's delve meal, what the delve starts with,
        /// opening for the evening); the free-roaming village day replaces it later and hands over to the evening the same way.
        /// </summary>
        Daytime,
        /// <summary>Tavern: prep the menu, run service, close up.</summary>
        Evening,
        /// <summary>The night's dungeon run.</summary>
        Delve,
        /// <summary>Back home: the day's summary, upgrades, then sleep.</summary>
        Night,
    }

    /// <summary>
    /// Daytime → Evening → Delve → Night → next Daytime, and the day counter. Only the next phase can be entered.
    /// Pure logic.
    /// </summary>
    public sealed class DayCycle
    {
        public DayCycle(int day = 1, DayPhase phase = DayPhase.Daytime)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            Day = day;
            Phase = phase;
        }

        public int Day { get; private set; }
        public DayPhase Phase { get; private set; }
        public DayPhase Next => Phase == DayPhase.Night ? DayPhase.Daytime : Phase + 1;

        /// <summary>Raised with (previous, current) after every change.</summary>
        public event Action<DayPhase, DayPhase> Changed;

        /// <summary>Moves to <paramref name="phase"/>, which must be the next one. Night → Daytime starts a new day.</summary>
        public void AdvanceTo(DayPhase phase)
        {
            if (phase != Next) throw new InvalidOperationException($"Can't go from {Phase} to {phase}; next is {Next}.");
            Advance();
        }

        public void Advance()
        {
            var previous = Phase;
            if (Phase == DayPhase.Night) Day++;
            Phase = Next;
            Changed?.Invoke(previous, Phase);
        }

        /// <summary>A phase saved under an older name ("Morning", before the v0.5 day order), or an unknown one, as today's.</summary>
        public static DayPhase Parse(string saved) => saved switch
        {
            "Morning" => DayPhase.Daytime,
            _ => Enum.TryParse(saved, out DayPhase phase) && Enum.IsDefined(typeof(DayPhase), phase) ? phase : DayPhase.Daytime,
        };
    }
}
