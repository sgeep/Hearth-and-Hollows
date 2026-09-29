using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Player Movement", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        public MovementSettings movement = new();
    }
}
