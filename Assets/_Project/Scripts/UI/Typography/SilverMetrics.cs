namespace Hearthdelve.UI.Typography
{
    /// <summary>
    /// Silver (Poppy Works), the game's one font, measured: a 1900-unit em whose pixels are 100 units, so one font pixel is one
    /// game pixel at size 19. Capitals and ascenders are 9 pixels, the x-height 6, descenders 2; accented capitals reach 11–12
    /// and CJK glyphs 11–12 tall. A line is 12 pixels (the capitals, the descenders and a pixel between lines).
    /// </summary>
    public static class SilverMetrics
    {
        /// <summary>The font's native size: one font pixel per game pixel.</summary>
        public const int NativeSize = 19;
        public const int CapHeight = 9;
        public const int XHeight = 6;
        public const int Descender = 2;
        /// <summary>A 1× line in game pixels.</summary>
        public const int LinePixels = 12;
        /// <summary>The first baseline sits this many pixels under the text box's top at 1× (a pixel above the capitals).</summary>
        public const int BaselineFromTop = 10;
    }
}
