using System;

namespace Hearthdelve.Core.Services
{
    /// <summary>
    /// Pure hit-stop timer. Overlapping requests do not add up: the longest remaining
    /// freeze wins, so a flurry of hits can't lock the game for seconds.
    /// </summary>
    public sealed class HitStop
    {
        public float Remaining { get; private set; }
        public bool IsActive => Remaining > 0f;

        /// <summary>Clamp for any single request, so bad data can't freeze the game.</summary>
        public float MaxDuration { get; set; } = 0.5f;

        public void Request(float duration)
        {
            if (duration <= 0f) return;
            Remaining = Math.Max(Remaining, Math.Min(duration, MaxDuration));
        }

        /// <summary>Advance by unscaled time. Returns true while the freeze is active.</summary>
        public bool Tick(float unscaledDeltaTime)
        {
            if (Remaining <= 0f) return false;
            Remaining = Math.Max(0f, Remaining - unscaledDeltaTime);
            return Remaining > 0f;
        }

        public void Cancel() => Remaining = 0f;
    }
}
