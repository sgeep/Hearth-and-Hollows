namespace Hearthdelve.Shared.Garden
{
    /// <summary>
    /// UI table keys a garden bed's prompt uses (4h Checkpoint B). Kept here so the village's beds can name them without the
    /// UI assembly; their English is in <c>Hearthdelve.UI.Localization.GardenLocKeys</c>.
    /// </summary>
    public static class GardenText
    {
        public const string Plant = "garden.plant";
        public const string Tend = "garden.tend";
        public const string Harvest = "garden.harvest";
        public const string TooTiredToPlant = "garden.too_tired";
    }
}
