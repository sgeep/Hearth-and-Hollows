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

        /// <summary>
        /// The frame of a telegraphed attack animation at <paramref name="time"/> since the telegraph
        /// began. The frames before <paramref name="releaseFrame"/> are spread evenly across the
        /// telegraph, however long it is tuned, so the attack visibly lands when the telegraph ends;
        /// from then on the remaining frames play at their normal rate and the last one holds.
        /// </summary>
        public static int TelegraphedFrame(float time, float telegraph, int releaseFrame, int frameCount, float frameDuration)
        {
            if (frameCount <= 1) return 0;
            releaseFrame = Math.Max(0, Math.Min(releaseFrame, frameCount - 1));
            if (time < telegraph)
            {
                if (releaseFrame == 0 || telegraph <= 0f) return 0;
                return Math.Min(releaseFrame - 1, (int)Math.Floor(Math.Max(0f, time) / telegraph * releaseFrame));
            }
            if (frameDuration <= 0f) return releaseFrame;
            int after = (int)Math.Floor((time - telegraph) / frameDuration + 1e-4f);
            return Math.Min(frameCount - 1, releaseFrame + after);
        }
    }
}
