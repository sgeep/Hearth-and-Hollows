using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>Movement and dodge tuning for the player's TDE character (tiles and seconds).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Player Move", fileName = "PlayerMoveConfig")]
    public sealed class PlayerMoveConfig : ScriptableObject
    {
        [Header("Walking")]
        [Min(0)] public float walkSpeed = 6f;
        [Min(0)] public float acceleration = 60f;
        [Min(0)] public float deceleration = 60f;

        [Header("Dodge roll")]
        [Min(0)] public float dodgeDistance = 3.5f;
        [Min(0.01f)] public float dodgeDuration = 0.25f;
        [Tooltip("The player can't be damaged for the length of the roll.")]
        public bool dodgeInvulnerable = true;
        [Min(0), Tooltip("Seconds before the roll can be used again.")]
        public float dodgeCooldown = 0.5f;
    }
}
