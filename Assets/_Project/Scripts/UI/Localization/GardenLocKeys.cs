namespace Hearthdelve.UI.Localization
{
    /// <summary>The garden and Vigor (4h Checkpoint B): UI table keys and their English.</summary>
    public static class GardenLocKeys
    {
        public const string Plant = Hearthdelve.Shared.Garden.GardenText.Plant;
        public const string Tend = Hearthdelve.Shared.Garden.GardenText.Tend;
        public const string Harvest = Hearthdelve.Shared.Garden.GardenText.Harvest;
        /// <summary>"{0}: ready in {1} days" (the crop, the days left).</summary>
        public const string ReadyIn = "garden.ready_in";
        /// <summary>"{0}: ready tomorrow".</summary>
        public const string ReadyTomorrow = "garden.ready_tomorrow";
        /// <summary>At 0 Vigor, at an empty bed.</summary>
        public const string TooTiredToPlant = Hearthdelve.Shared.Garden.GardenText.TooTiredToPlant;

        /// <summary>"plant a crop · {0} Vigor" (the cost).</summary>
        public const string PanelTitle = "garden.panel.title";
        /// <summary>"{0} · {1} days" (the crop, its days to grow).</summary>
        public const string PanelCrop = "garden.panel.crop";
        public const string PanelCancel = "garden.panel.cancel";

        /// <summary>The harvest line: "{0} {1} into the storeroom" (count, ingredient); Fine adds its own word.</summary>
        public const string Harvested = "garden.harvested";
        public const string HarvestedFine = "garden.harvested_fine";

        public const string CropHerbs = "crop.herbs";
        public const string CropOnions = "crop.onions";
        public const string CropBarley = "crop.barley";

        public static readonly (string key, string english)[] English =
        {
            (Plant, "plant a crop"),
            (Tend, "tend the bed"),
            (Harvest, "harvest"),
            (ReadyIn, "{0}: ready in {1} days"),
            (ReadyTomorrow, "{0}: ready tomorrow"),
            (TooTiredToPlant, "too tired to dig a bed today"),
            (PanelTitle, "plant a crop · {0} Vigor"),
            (PanelCrop, "{0} · {1} days"),
            (PanelCancel, "not now"),
            (Harvested, "{0} {1} into the storeroom"),
            (HarvestedFine, "{0} fine {1} into the storeroom"),
            (CropHerbs, "herbs"),
            (CropOnions, "onions"),
            (CropBarley, "barley"),
        };
    }
}
