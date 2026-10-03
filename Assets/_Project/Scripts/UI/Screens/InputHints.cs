using Hearthdelve.Core.Input;
using UnityEngine.InputSystem;

namespace Hearthdelve.UI.Screens
{
    /// <summary>What to call a button in a hint ("press E" / "press A"), for the device in use.</summary>
    public static class InputHints
    {
        public static string Binding(string map, string action)
        {
            InputAction input = InputMaps.Find(map, action);
            if (input == null) return string.Empty;
            string group = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame ? "Gamepad" : "Keyboard&Mouse";
            string shown = input.GetBindingDisplayString(InputBinding.MaskByGroup(group));
            return string.IsNullOrEmpty(shown) ? input.GetBindingDisplayString() : shown;
        }

        public static string Interact() => Binding(InputMaps.Dungeon, DungeonActions.Interact);
    }
}
