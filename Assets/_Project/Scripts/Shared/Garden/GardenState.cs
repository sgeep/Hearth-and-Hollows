using System.Collections.Generic;

namespace Hearthdelve.Shared.Garden
{
    /// <summary>
    /// One bed (or, one day, any plot) of the garden, by stable id (4h Checkpoint B). Saved exactly: what's planted, when, how
    /// many nights it has grown, how often it was tended, and the day markers that make tending once a day and growth once a
    /// night. Nothing about the bed's shape or position is here (that's the scene's), so a plot could be a grid cell later.
    /// </summary>
    public sealed class BedState
    {
        public string Id;
        /// <summary>The crop's id; empty when nothing is planted.</summary>
        public string Crop = string.Empty;
        public int PlantedDay;
        /// <summary>Nights of growth so far.</summary>
        public int Grown;
        /// <summary>Days it was tended while it grew.</summary>
        public int TendedDays;
        /// <summary>The day it was last tended (0: never).</summary>
        public int LastTendedDay;
        /// <summary>The day its growth was last counted (growth happens once per new day).</summary>
        public int LastGrownDay;

        public bool IsEmpty => string.IsNullOrEmpty(Crop);

        public void Clear()
        {
            Crop = string.Empty;
            PlantedDay = Grown = TendedDays = LastTendedDay = LastGrownDay = 0;
        }

        public BedState Clone() => (BedState)MemberwiseClone();
    }

    /// <summary>The garden: its beds in order, by id (4h Checkpoint B). Part of the game state; saved from version 10.</summary>
    public sealed class GardenState
    {
        readonly List<BedState> m_Beds = new();

        /// <summary>Whether the starter beds have been given (a new game, or a save migrated to version 10).</summary>
        public bool Initialized { get; private set; }

        public IReadOnlyList<BedState> Beds => m_Beds;

        public BedState Bed(string id)
        {
            foreach (BedState bed in m_Beds)
                if (bed.Id == id) return bed;
            return null;
        }

        /// <summary>
        /// Each of <paramref name="ids"/> the garden doesn't have yet arrives empty (the starter garden; beds added later). Beds it
        /// has are left exactly as they are.
        /// </summary>
        public void Ensure(IEnumerable<string> ids)
        {
            if (ids != null)
                foreach (string id in ids)
                    if (!string.IsNullOrEmpty(id) && Bed(id) == null) m_Beds.Add(new BedState { Id = id });
            Initialized = true;
        }

        /// <summary>From a save: these beds, as they were.</summary>
        public void Restore(bool initialized, IEnumerable<BedState> beds)
        {
            m_Beds.Clear();
            if (beds != null)
                foreach (BedState bed in beds)
                    if (bed != null && !string.IsNullOrEmpty(bed.Id) && Bed(bed.Id) == null) m_Beds.Add(bed);
            Initialized = initialized;
        }
    }
}
