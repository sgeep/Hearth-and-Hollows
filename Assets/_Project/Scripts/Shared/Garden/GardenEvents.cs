using Hearthdelve.Core.Events;

namespace Hearthdelve.Shared.Garden
{
    /// <summary>The keeper used an empty bed with Vigor enough to plant it: the UI asks which crop (4h Checkpoint B).</summary>
    public readonly struct GardenPlantAsked : IEvent
    {
        public readonly string BedId;
        public GardenPlantAsked(string bedId) => BedId = bedId;
    }
}
