using System;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// A room's fight (4d): the exits stay sealed while any enemy in the room is alive, and open once, when the
    /// last one falls. A room with no enemies is clear from the start. Pure logic; the room runner feeds it the
    /// living count.
    /// </summary>
    public sealed class RoomEncounter
    {
        public RoomEncounter(int enemies)
        {
            Remaining = Math.Max(0, enemies);
            IsCleared = Remaining == 0;
        }

        public int Remaining { get; private set; }
        /// <summary>True once nothing is left alive; the exits are open.</summary>
        public bool IsCleared { get; private set; }
        public bool IsSealed => !IsCleared;

        /// <summary>Raised once, when the room becomes clear (never for a room that started clear).</summary>
        public event Action Cleared;

        /// <summary>The number of enemies still alive. True on the call that clears the room.</summary>
        public bool Update(int living)
        {
            if (IsCleared) return false;
            Remaining = Math.Max(0, living);
            if (Remaining > 0) return false;
            IsCleared = true;
            Cleared?.Invoke();
            return true;
        }
    }
}
