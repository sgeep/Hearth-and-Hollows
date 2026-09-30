using System;

namespace Hearthdelve.Shared.Game
{
    /// <summary>The four parts of a day (GDD §3.1).</summary>
    public enum DayPhase
    {
        /// <summary>Tavern: check stock, eat breakfast, set off.</summary>
        Morning,
        /// <summary>Dungeon run.</summary>
        Delve,
        /// <summary>Tavern: prep the menu and run service.</summary>
        Evening,
        /// <summary>Tavern: upgrades and saving, then sleep.</summary>
        Night,
    }

    /// <summary>
    /// Morning → Delve → Evening → Night → next Morning, and the day counter. Only the next
    /// phase can be entered. Pure logic.
    /// </summary>
    public sealed class DayCycle
    {
        public DayCycle(int day = 1, DayPhase phase = DayPhase.Morning)
        {
            if (day < 1) throw new ArgumentOutOfRangeException(nameof(day));
            Day = day;
            Phase = phase;
        }

        public int Day { get; private set; }
        public DayPhase Phase { get; private set; }
        public DayPhase Next => Phase == DayPhase.Night ? DayPhase.Morning : Phase + 1;

        /// <summary>Raised with (previous, current) after every change.</summary>
        public event Action<DayPhase, DayPhase> Changed;

        /// <summary>Moves to <paramref name="phase"/>, which must be the next one. Night → Morning starts a new day.</summary>
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
    }
}
