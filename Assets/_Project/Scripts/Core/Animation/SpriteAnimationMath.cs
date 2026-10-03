using System;

namespace Hearthdelve.Core.Animation
{
    /// <summary>Pure sprite-sheet timing: which frame shows after a given time.</summary>
    public static class SpriteAnimationMath
    {
        /// <summary>The frame index at <paramref name="time"/>. One-shots hold their last frame.</summary>
        public static int FrameAt(float time, int frameCount, float frameDuration, bool loop)
        {
            if (frameCount <= 1 || frameDuration <= 0f || time <= 0f) return 0;
            int frame = (int)Math.Floor(time / frameDuration + 1e-4f);
            return loop ? frame % frameCount : Math.Min(frame, frameCount - 1);
        }

        /// <summary>Whether a one-shot has shown every frame for its full duration.</summary>
        public static bool IsFinished(float time, int frameCount, float frameDuration, bool loop) =>
            !loop && time >= Length(frameCount, frameDuration) - 1e-4f;

        public static float Length(int frameCount, float frameDuration) => Math.Max(0, frameCount) * Math.Max(0f, frameDuration);
    }
}
