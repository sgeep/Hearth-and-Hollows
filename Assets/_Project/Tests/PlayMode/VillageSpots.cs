using UnityEngine;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Where the village tests put the keeper in Kariaston (village tiles, y up; the village's origin is (200, 0)), by what each
    /// place is for, so a change of layout touches only this file (the Crossroads, 2026-10-10, with the owner's hand adjustments).
    /// Every spot is open ground reachable from Tally Ho!'s door (BatchLogs/kariaston_walk.txt from
    /// <c>KariastonDiagnostics.WalkMapBatch</c>). Villagers count as seen within 26 tiles of the keeper.
    /// </summary>
    public static class VillageSpots
    {
        public static readonly Vector2 Origin = new(200f, 0f);

        /// <summary>In the square, east of the memorial: most of the village is in view.</summary>
        public static readonly Vector2 Square = new(36f, 21.5f);
        /// <summary>Just south of the memorial (the square's south side).</summary>
        public static readonly Vector2 SquareSouth = new(32f, 16.5f);
        /// <summary>On the green west of the square: Bart's wagon (west road) and the market both in view.</summary>
        public static readonly Vector2 BetweenWagonAndMarket = new(18f, 18.5f);
        /// <summary>The lane's west end: more than 26 tiles from Kaloren's tower and the square's bench.</summary>
        public static readonly Vector2 FarWest = new(8f, 6.5f);
        /// <summary>On the east road: Kaloren's tower and Grim and Ogrin's door both in view (the herb walk).</summary>
        public static readonly Vector2 EastRoad = new(50f, 18.5f);
        /// <summary>On the lane before Grim and Ogrin's cottage (Ogrin's window).</summary>
        public static readonly Vector2 BeforeTheCottage = new(45f, 5.5f);
        /// <summary>Below the square's south-east corner, by the market cart.</summary>
        public static readonly Vector2 ByTheMarket = new(35f, 13.5f);
        /// <summary>In front of Musashi at his cart.</summary>
        public static readonly Vector2 BeforeMusashi = new(34.4f, 13.4f);
        /// <summary>On the green by the south road, where Bart plays.</summary>
        public static readonly Vector2 TheGreen = new(24f, 13.5f);

        /// <summary>Pond cells' centres, and the bank south of it (walking north runs into the water).</summary>
        public static readonly Vector2[] Pond = { new(9.5f, 25.5f), new(7.5f, 27.5f), new(12.5f, 26.5f) };
        public static readonly Vector2 PondSouthBank = new(9.5f, 22.6f);

        /// <summary>Framing Tally Ho! for the menu backdrop candidates (the keeper hidden): left, right, and each a little lower.</summary>
        public static readonly Vector2 TallyHoLeft = new(47f, 26.5f), TallyHoRight = new(18f, 27.5f), TallyHoRightLow = new(19f, 24.5f), TallyHoLeftLow = new(46f, 23.5f);
    }
}
