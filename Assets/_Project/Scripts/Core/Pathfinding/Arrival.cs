using UnityEngine;

namespace Hearthdelve.Core.Pathfinding
{
    /// <summary>
    /// When a walker has reached its goal. The AI picks a heading once a frame, so a slow frame (a busy machine, a
    /// background browser tab) carries the walker a long way in one go: with a fixed radius smaller than that step it
    /// overshoots, turns back and overshoots again for ever (the 4f web check: Orik circling the pass at 3 frames a
    /// second, never picking up a plate). A goal within one frame's travel therefore counts as reached too.
    /// </summary>
    public static class Arrival
    {
        /// <summary>How close counts as there: <paramref name="radius"/>, or one frame's travel if that is longer.</summary>
        public static float Reach(float radius, float speed, float deltaTime) => Mathf.Max(radius, Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));

        /// <summary>How far the goal must move away before an arrived walker sets off again (always beyond the reach).</summary>
        public static float Resume(float resumeRadius, float reach) => Mathf.Max(resumeRadius, reach + 0.15f);
    }
}
