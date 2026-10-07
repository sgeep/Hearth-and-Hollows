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
        /// <summary>Decorate Mode (4f): the cursor, picking up, turning, putting away.</summary>
        public const string Decorate = "Decorate";

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

        /// <summary>The maps enabled now, for <see cref="Restore"/> (a conversation takes the UI map and gives back whatever was on).</summary>
        public static string[] Snapshot()
        {
            var asset = InputSystem.actions;
            if (asset == null) return System.Array.Empty<string>();
            var enabled = new System.Collections.Generic.List<string>();
            foreach (var map in asset.actionMaps)
                if (map.enabled) enabled.Add(map.name);
            return enabled.ToArray();
        }

        /// <summary>Enables exactly the maps in <paramref name="snapshot"/> (UI always stays on).</summary>
        public static void Restore(string[] snapshot)
        {
            var asset = InputSystem.actions;
            if (asset == null || snapshot == null) return;
            foreach (var map in asset.actionMaps)
            {
                if (map.name == UI || System.Array.IndexOf(snapshot, map.name) >= 0) map.Enable();
                else map.Disable();
            }
        }

        public static InputAction Find(string map, string action) =>
            InputSystem.actions?.FindAction($"{map}/{action}", throwIfNotFound: false);
    }

    /// <summary>The UI map's actions that game code reads (uGUI reads the rest through the input module).</summary>
    public static class UIActions
    {
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
        /// <summary>4g: moves dialogue on (E), beside Submit.</summary>
        public const string Advance = "Advance";
    }

    public static class TavernActions
    {
        public const string Move = "Move";
        public const string Interact = "Interact";
        /// <summary>The right stick: where the keeper looks on a gamepad (4e playtest).</summary>
        public const string LookStick = "LookStick";
        public const string Cancel = "Cancel";
        public const string Pause = "Pause";
        /// <summary>4h: Decorate Mode from anywhere inside Tally Ho! in the daytime (Tab, or View on a gamepad).</summary>
        public const string Decorate = "Decorate";
    }

    /// <summary>Decorate Mode (4f): controller first; the mouse points at tiles.</summary>
    public static class DecorateActions
    {
        /// <summary>Moves the cursor a tile at a time (held: it repeats).</summary>
        public const string Move = "Move";
        /// <summary>Mouse / pointer position in screen pixels: the cursor follows it.</summary>
        public const string Point = "Point";
        /// <summary>Pick up, or put down.</summary>
        public const string Select = "Select";
        public const string Click = "Click";
        /// <summary>Put the carried piece back, or leave Decorate Mode.</summary>
        public const string Cancel = "Cancel";
        public const string Turn = "Turn";
        public const string Flip = "Flip";
        /// <summary>To storage.</summary>
        public const string Store = "Store";
        public const string Undo = "Undo";
        /// <summary>Picks the next piece under the cursor (a rug under a table).</summary>
        public const string Cycle = "Cycle";
        public const string Storage = "Storage";
        public const string Check = "Check";
        /// <summary>Mouse wheel: turns the carried piece.</summary>
        public const string Wheel = "Wheel";
        /// <summary>Held: free placement, to the pixel (the owner's request, amending D1).</summary>
        public const string Free = "Free";
        /// <summary>Opens the colour panel for the piece under the cursor or carried (4f step 5).</summary>
        public const string Style = "Style";
        /// <summary>Goes to the property's next area (the tavern, the guest room) without walking (4f step 6).</summary>
        public const string Area = "Area";
    }

    public static class MinigameActions
    {
        public const string Aim = "Aim";
        public const string Action = "Action";
        public const string AltAction = "AltAction";
        public const string Cancel = "Cancel";
        /// <summary>Mouse / pointer position in screen pixels (e.g. the chopping knife).</summary>
        public const string Point = "Point";
    }

    public static class DungeonActions
    {
        public const string Move = "Move";
        /// <summary>Mouse / pointer position in screen pixels; aim on keyboard and mouse.</summary>
        public const string AimPoint = "AimPoint";
        /// <summary>The right stick: aim on a gamepad (4e playtest); movement direction when it's centred.</summary>
        public const string AimStick = "AimStick";
        public const string Attack = "Attack";
        /// <summary>Heavy / charged attack (hold).</summary>
        public const string Heavy = "Heavy";
        public const string Dodge = "Dodge";
        public const string Interact = "Interact";
        public const string Skill1 = "Skill1";
        public const string Skill2 = "Skill2";
        public const string KitchenArts = "KitchenArts";
        public const string Finisher = "Finisher";
        public const string Pause = "Pause";
    }
}
