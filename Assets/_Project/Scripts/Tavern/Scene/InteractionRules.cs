using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>One thing the player could use: where they must stand near, how near, and whether it can be used now.</summary>
    public readonly struct InteractionCandidate
    {
        public readonly Vector2 Point;
        public readonly float Reach;
        public readonly bool Available;

        public InteractionCandidate(Vector2 point, float reach, bool available)
        {
            Point = point;
            Reach = reach;
            Available = available;
        }
    }

    /// <summary>
    /// Which station, pass or seat the player is about to use (the prototype's rule, in 2D): the
    /// nearest available one in reach. The current choice is kept until another is clearly nearer,
    /// so standing between two stations doesn't make the highlight flicker.
    /// </summary>
    public static class InteractionRules
    {
        /// <summary>How much nearer (in tiles) another candidate must be to take over from the current one.</summary>
        public const float DefaultStickiness = 0.2f;

        /// <summary>The index of the candidate to use, or -1 if none is in reach. Ties go to the earlier one.</summary>
        public static int Pick(Vector2 from, IReadOnlyList<InteractionCandidate> candidates, int current = -1, float stickiness = DefaultStickiness)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                InteractionCandidate c = candidates[i];
                if (!c.Available) continue;
                float distance = Vector2.Distance(from, c.Point);
                if (distance > c.Reach || distance >= bestDistance) continue;
                best = i;
                bestDistance = distance;
            }
            if (current < 0 || current >= candidates.Count || current == best) return best;
            InteractionCandidate kept = candidates[current];
            if (!kept.Available) return best;
            float keptDistance = Vector2.Distance(from, kept.Point);
            return keptDistance <= kept.Reach && keptDistance <= bestDistance + stickiness ? current : best;
        }
    }
}
