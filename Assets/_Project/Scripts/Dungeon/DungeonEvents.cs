using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.Dungeon
{
    /// <summary>An enemy died. Harvest listens to this to decide drops.</summary>
    public readonly struct EnemyKilled : IEvent
    {
        public readonly EnemyDefinition Definition;
        public readonly KillContext Kill;
        public readonly Vector2 Position;

        public EnemyKilled(EnemyDefinition definition, KillContext kill, Vector2 position)
        {
            Definition = definition;
            Kill = kill;
            Position = position;
        }
    }

    /// <summary>The player's Essence ran out. The run controller shows the death screen.</summary>
    public readonly struct PlayerDefeated : IEvent
    {
        public readonly DefeatReason Reason;
        public PlayerDefeated(DefeatReason reason) => Reason = reason;
    }

    /// <summary>Scene-wide access to the player's transform for enemy AI.</summary>
    public static class PlayerLocator
    {
        public static Transform Player { get; private set; }

        public static void Register(Transform player) => Player = player;

        public static void Unregister(Transform player)
        {
            if (Player == player) Player = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Player = null;
    }
}
