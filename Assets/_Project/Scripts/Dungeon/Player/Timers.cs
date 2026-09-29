namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// Remembers a button press for a short window so it can fire when it becomes legal
    /// (jump buffering, dodge buffering, combo input buffering).
    /// </summary>
    public sealed class InputBuffer
    {
        float m_Remaining;

        public InputBuffer(float window) => Window = window;

        public float Window { get; set; }
        public bool IsBuffered => m_Remaining > 0f;

        public void Press() => m_Remaining = Window;
        public void Consume() => m_Remaining = 0f;

        public void Tick(float deltaTime)
        {
            if (m_Remaining > 0f) m_Remaining -= deltaTime;
        }
    }

    /// <summary>Simple count-down timer; <see cref="IsRunning"/> while time remains.</summary>
    public sealed class Countdown
    {
        public float Remaining { get; private set; }
        public bool IsRunning => Remaining > 0f;

        public void Start(float duration) => Remaining = duration;
        public void Stop() => Remaining = 0f;

        public void Tick(float deltaTime)
        {
            if (Remaining > 0f) Remaining = Remaining > deltaTime ? Remaining - deltaTime : 0f;
        }
    }
}
