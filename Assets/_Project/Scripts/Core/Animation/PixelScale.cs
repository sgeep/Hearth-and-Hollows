using System;

namespace Hearthdelve.Core.Animation
{
    /// <summary>
    /// The whole-number scale from the 320×180 reference to a screen: the largest that fits both ways, at least 1. The
    /// Pixel Perfect Camera zooms the world by it, and the UI scales by it too, so a UI pixel is always a whole number
    /// of screen pixels (a fractional scale makes pixel text uneven or blurry). Pure logic.
    /// </summary>
    public static class PixelScale
    {
        public const int ReferenceWidth = 320;
        public const int ReferenceHeight = 180;

        public static int For(int screenWidth, int screenHeight) =>
            Math.Max(1, Math.Min(screenWidth / ReferenceWidth, screenHeight / ReferenceHeight));
    }
}
