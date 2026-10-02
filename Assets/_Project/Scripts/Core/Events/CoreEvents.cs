namespace Hearthdelve.Core.Events
{
    /// <summary>Request a global hit-stop (freeze frame) of the given real-time duration.</summary>
    public readonly struct HitStopRequested : IEvent
    {
        public readonly float Duration;
        public HitStopRequested(float duration) => Duration = duration;
    }

    /// <summary>Request a screen shake. Force is scaled by the player's shake setting.</summary>
    public readonly struct ScreenShakeRequested : IEvent
    {
        public readonly float Force;
        public ScreenShakeRequested(float force) => Force = force;
    }
}
