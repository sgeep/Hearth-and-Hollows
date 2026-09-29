using UnityEngine.InputSystem;

namespace Hearthdelve.Core.Input
{
    /// <summary>
    /// Names of the action maps and actions in Hearthdelve.inputactions (the project-wide
    /// actions asset), plus a helper to make exactly one gameplay map active.
    /// </summary>
    public static class InputMaps
    {
        public const string Dungeon = "Dungeon";
        public const string Tavern = "Tavern";
        public const string Minigame = "Minigame";
        public const string UI = "UI";

        /// <summary>Enables <paramref name="mapName"/> and disables the other gameplay maps. UI stays enabled.</summary>
        public static void Activate(string mapName)
        {
            var asset = InputSystem.actions;
            if (asset == null) return;
            foreach (var map in asset.actionMaps)
            {
                if (map.name == UI || map.name == mapName) map.Enable();
                else map.Disable();
            }
        }

        /// <summary>Disables every gameplay map, leaving only UI (menus, death screen).</summary>
        public static void ActivateUIOnly() => Activate(UI);

        public static InputAction Find(string map, string action) =>
            InputSystem.actions?.FindAction($"{map}/{action}", throwIfNotFound: false);
    }

    public static class DungeonActions
    {
        public const string Move = "Move";
        public const string Jump = "Jump";
        public const string Attack = "Attack";
        public const string Secondary = "Secondary";
        public const string Dodge = "Dodge";
        public const string Interact = "Interact";
        public const string Skill1 = "Skill1";
        public const string Skill2 = "Skill2";
        public const string KitchenArts = "KitchenArts";
        public const string Finisher = "Finisher";
        public const string Pause = "Pause";
    }
}
