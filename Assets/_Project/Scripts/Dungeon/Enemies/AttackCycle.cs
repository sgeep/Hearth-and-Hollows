using System;

namespace Hearthdelve.Dungeon.Enemies
{
    public enum EnemyAttackPhase
    {
        Ready,
        Telegraph,
        Active,
        Recovery,
        Cooldown,
    }

    /// <summary>
    /// Pure timing for an enemy attack: Telegraph → Active → Recovery → Cooldown → Ready.
    /// The telegraph is the player's window to read and react.
    /// </summary>
    public sealed class AttackCycle
    {
        float m_Elapsed;

        public AttackCycle(float telegraph, float active, float recovery, float cooldown)
        {
            Telegraph = telegraph;
            Active = active;
            Recovery = recovery;
            Cooldown = cooldown;
        }

        public float Telegraph { get; set; }
        public float Active { get; set; }
        public float Recovery { get; set; }
        public float Cooldown { get; set; }

        public EnemyAttackPhase Phase { get; private set; } = EnemyAttackPhase.Ready;
        public float PhaseElapsed => m_Elapsed;
        public bool IsAttacking => Phase is EnemyAttackPhase.Telegraph or EnemyAttackPhase.Active or EnemyAttackPhase.Recovery;

        /// <summary>Fired on every phase entry (including Ready).</summary>
        public event Action<EnemyAttackPhase> PhaseChanged;

        public bool TryStart()
        {
            if (Phase != EnemyAttackPhase.Ready) return false;
            Enter(EnemyAttackPhase.Telegraph);
            return true;
        }

        /// <summary>Stagger/interrupt: abandon the attack and go on cooldown.</summary>
        public void Interrupt()
        {
            if (!IsAttacking) return;
            Enter(EnemyAttackPhase.Cooldown);
        }

        /// <summary>Ends a cooldown at once (a frenzied boss's second slam, 4e). Nothing else changes.</summary>
        public void SkipCooldown()
        {
            if (Phase == EnemyAttackPhase.Cooldown) Enter(EnemyAttackPhase.Ready);
        }

        public void Tick(float dt)
        {
            if (Phase == EnemyAttackPhase.Ready || dt <= 0f) return;
            m_Elapsed += dt;

            // Loop so a large dt can pass through several short phases in one tick.
            for (int guard = 0; guard < 5; guard++)
            {
                float duration = DurationOf(Phase);
                if (Phase == EnemyAttackPhase.Ready || m_Elapsed < duration) break;
                float carry = m_Elapsed - duration;
                Enter(Next(Phase));
                m_Elapsed = Phase == EnemyAttackPhase.Ready ? 0f : carry;
            }
        }

        float DurationOf(EnemyAttackPhase phase) => phase switch
        {
            EnemyAttackPhase.Telegraph => Telegraph,
            EnemyAttackPhase.Active => Active,
            EnemyAttackPhase.Recovery => Recovery,
            EnemyAttackPhase.Cooldown => Cooldown,
            _ => float.MaxValue,
        };

        static EnemyAttackPhase Next(EnemyAttackPhase phase) => phase switch
        {
            EnemyAttackPhase.Telegraph => EnemyAttackPhase.Active,
            EnemyAttackPhase.Active => EnemyAttackPhase.Recovery,
            EnemyAttackPhase.Recovery => EnemyAttackPhase.Cooldown,
            _ => EnemyAttackPhase.Ready,
        };

        void Enter(EnemyAttackPhase phase)
        {
            Phase = phase;
            m_Elapsed = 0f;
            PhaseChanged?.Invoke(phase);
        }
    }
}
