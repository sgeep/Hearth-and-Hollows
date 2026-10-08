using Hearthdelve.Core.Input;
using UnityEngine.InputSystem;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// What to call a button in a hint ("press E" / "press A"), for the device in use. Since 4i-A the device is the one the player
    /// last used meaningfully (<see cref="InputDevices"/>), the same for every prompt in the game, rather than whichever happened to
    /// update this frame.
    /// </summary>
    public static class InputHints
    {
        /// <summary>The action's bindings for the device in use ("E | Space", "A").</summary>
        public static string Binding(string map, string action) => Binding(map, action, compact: false);

        /// <summary>
        /// The action's bindings for the device in use; <paramref name="compact"/>: only the first ("W/A/S/D" rather than every
        /// alternative), for prompts that must fit a narrow panel.
        /// </summary>
        public static string Binding(string map, string action, bool compact)
        {
            InputAction input = InputMaps.Find(map, action);
            if (input == null) return string.Empty;
            string group = InputDevices.Group;
            string shown = compact ? First(input, group) : input.GetBindingDisplayString(InputBinding.MaskByGroup(group));
            if (string.IsNullOrEmpty(shown)) shown = input.GetBindingDisplayString();
            return InputDevices.Current == InputDeviceKind.Gamepad ? PadNames(shown) : shown;
        }

        /// <summary>The first binding of the action in the group (a composite counts as one, shown whole).</summary>
        static string First(InputAction input, string group)
        {
            InputBinding mask = InputBinding.MaskByGroup(group);
            var bindings = input.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding b = bindings[i];
                if (b.isPartOfComposite) continue;
                bool inGroup = mask.Matches(b);
                if (b.isComposite)
                    for (int j = i + 1; j < bindings.Count && bindings[j].isPartOfComposite; j++)
                        inGroup |= mask.Matches(bindings[j]);
                if (!inGroup) continue;
                string shown = input.GetBindingDisplayString(i);
                if (!string.IsNullOrEmpty(shown)) return shown;
            }
            return null;
        }

        static readonly (string from, string to)[] k_Pad =
        {
            ("Button South", "A"), ("Button East", "B"), ("Button West", "X"), ("Button North", "Y"),
            ("Left Shoulder", "LB"), ("Right Shoulder", "RB"), ("Left Trigger", "LT"), ("Right Trigger", "RT"),
            ("Start", "Start"), ("Select", "View"), ("Left Stick Press", "L3"), ("Right Stick Press", "R3"),
            ("Left Stick", "left stick"), ("Right Stick", "right stick"), ("D-Pad", "d-pad"),
        };

        /// <summary>The Input System's generic pad names as players know them (the controls reference uses the same words).</summary>
        public static string PadNames(string shown)
        {
            if (string.IsNullOrEmpty(shown)) return shown;
            foreach (var (from, to) in k_Pad) shown = shown.Replace(from, to);
            return shown;
        }

        public static string Interact() => Binding(InputMaps.Dungeon, DungeonActions.Interact);

        /// <summary>The tavern's Interact key or button, as shown in hints.</summary>
        public static string TavernInteract() => Binding(InputMaps.Tavern, TavernActions.Interact);
    }
}
