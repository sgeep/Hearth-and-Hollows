using System;
using System.Collections.Generic;
using Hearthdelve.Dungeon.Player;

namespace Hearthdelve.Dungeon.Combat
{
    public enum AttackPhase
    {
        Idle,
        Startup,
        Active,
        Recovery,
    }

    /// <summary>
    /// Frame-data-driven combo state machine. Presses are buffered; a buffered press chains
    /// into the next hit once the current attack reaches its cancel frame. After the last hit
    /// (or if the link window lapses) the combo restarts from the first attack.
    /// </summary>
    public sealed class ComboLogic
    {
        readonly IReadOnlyList<AttackData> m_Attacks;
        readonly InputBuffer m_Buffer;
        readonly float m_LinkWindow;

        float m_Elapsed;
        float m_SinceFinished = float.MaxValue;
        int m_LastFinishedIndex = -1;

        /// <param name="inputBuffer">How early a press may be made and still count.</param>
        /// <param name="linkWindow">After an attack fully recovers, how long a press still continues the combo.</param>
        public ComboLogic(IReadOnlyList<AttackData> attacks, float inputBuffer, float linkWindow)
        {
            if (attacks == null || attacks.Count == 0) throw new ArgumentException("A combo needs at least one attack.", nameof(attacks));
            m_Attacks = attacks;
            m_Buffer = new InputBuffer(inputBuffer);
            m_LinkWindow = linkWindow;
        }

        public int Count => m_Attacks.Count;
        public AttackPhase Phase { get; private set; } = AttackPhase.Idle;
        /// <summary>Index of the attack in progress, or -1 when idle.</summary>
        public int Index { get; private set; } = -1;
        public AttackData Current => Index >= 0 ? m_Attacks[Index] : null;
        public float Elapsed => m_Elapsed;
        public bool IsBusy => Phase != AttackPhase.Idle;
        public bool IsActive => Phase == AttackPhase.Active;

        /// <summary>True once the current attack may be cancelled into another action.</summary>
        public bool InCancelWindow => IsBusy && m_Elapsed >= FrameData.ToSeconds(Current.cancelFrame);

        public event Action<int> AttackStarted;
        public event Action<int> ActiveStarted;
        public event Action<int> ActiveEnded;
        public event Action<int> AttackFinished;

        public void PressAttack() => m_Buffer.Press();

        /// <summary>Abort the current attack (dodge, hurt). Breaks the combo chain.</summary>
        public void Cancel()
        {
            if (Phase == AttackPhase.Active) ActiveEnded?.Invoke(Index);
            Phase = AttackPhase.Idle;
            Index = -1;
            m_Elapsed = 0f;
            m_LastFinishedIndex = -1;
            m_SinceFinished = float.MaxValue;
            m_Buffer.Consume();
        }

        public void Tick(float dt)
        {
            if (Phase == AttackPhase.Idle)
            {
                m_SinceFinished += dt;
                if (m_Buffer.IsBuffered)
                {
                    bool linked = m_LastFinishedIndex >= 0 && m_SinceFinished <= m_LinkWindow && m_LastFinishedIndex < Count - 1;
                    Begin(linked ? m_LastFinishedIndex + 1 : 0);
                }
                m_Buffer.Tick(dt);
                return;
            }

            m_Elapsed += dt;
            UpdatePhase();

            if (m_Buffer.IsBuffered && InCancelWindow && Index < Count - 1)
            {
                Begin(Index + 1);
            }
            else if (m_Elapsed >= FrameData.ToSeconds(Current.TotalFrames))
            {
                int finished = Index;
                Phase = AttackPhase.Idle;
                Index = -1;
                m_LastFinishedIndex = finished;
                m_SinceFinished = 0f;
                AttackFinished?.Invoke(finished);
            }

            m_Buffer.Tick(dt);
        }

        void Begin(int index)
        {
            if (Phase == AttackPhase.Active) ActiveEnded?.Invoke(Index);
            m_Buffer.Consume();
            Index = index;
            m_Elapsed = 0f;
            Phase = AttackPhase.Startup;
            AttackStarted?.Invoke(index);
            UpdatePhase();
        }

        void UpdatePhase()
        {
            var attack = Current;
            float activeStart = FrameData.ToSeconds(attack.startupFrames);
            float activeEnd = FrameData.ToSeconds(attack.startupFrames + attack.activeFrames);

            if (Phase == AttackPhase.Startup && m_Elapsed >= activeStart)
            {
                Phase = AttackPhase.Active;
                ActiveStarted?.Invoke(Index);
            }
            if (Phase == AttackPhase.Active && m_Elapsed >= activeEnd)
            {
                Phase = AttackPhase.Recovery;
                ActiveEnded?.Invoke(Index);
            }
        }
    }
}
