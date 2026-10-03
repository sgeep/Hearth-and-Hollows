using UnityEngine;

namespace Hearthdelve.Core.Events
{
    /// <summary>Request a screen shake. Force is scaled by the player's shake setting.</summary>
    public readonly struct ScreenShakeRequested : IEvent
    {
        public readonly float Force;
        public ScreenShakeRequested(float force) => Force = force;
    }

    // Engine events, bridged from TopDown Engine at the boundary (Shared/Engine/TdeEventBridge).
    // Our systems listen to these instead of MMEventManager.

    /// <summary>A character (player or enemy) lost health.</summary>
    public readonly struct CharacterDamaged : IEvent
    {
        public readonly GameObject Target;
        public readonly GameObject Instigator;
        public readonly bool TargetIsPlayer;
        public readonly float Damage;
        public readonly float HealthBefore;
        public readonly float HealthAfter;

        public CharacterDamaged(GameObject target, GameObject instigator, bool targetIsPlayer, float damage, float healthBefore, float healthAfter)
        {
            Target = target;
            Instigator = instigator;
            TargetIsPlayer = targetIsPlayer;
            Damage = damage;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
        }
    }

    /// <summary>A character's health reached zero.</summary>
    public readonly struct CharacterDied : IEvent
    {
        public readonly GameObject Target;
        public readonly bool IsPlayer;
        public readonly Vector2 Position;
        public readonly float MaxHealth;
        /// <summary>Damage the killing hit dealt beyond the health that was left (0 if it died another way).</summary>
        public readonly float Overkill;

        public CharacterDied(GameObject target, bool isPlayer, Vector2 position, float maxHealth, float overkill)
        {
            Target = target;
            IsPlayer = isPlayer;
            Position = position;
            MaxHealth = maxHealth;
            Overkill = overkill;
        }
    }

    /// <summary>A character came back (respawn).</summary>
    public readonly struct CharacterRevived : IEvent
    {
        public readonly GameObject Target;
        public readonly bool IsPlayer;

        public CharacterRevived(GameObject target, bool isPlayer)
        {
            Target = target;
            IsPlayer = isPlayer;
        }
    }

    /// <summary>The level finished spawning its player and is ready to play.</summary>
    public readonly struct LevelStarted : IEvent { }
}
