using System.IO;
using Hearthdelve.Core.Input;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds Hearthdelve.inputactions (Dungeon, Tavern, Minigame, UI maps, controller-first per
    /// GDD §9) and makes it the project-wide actions asset. Doesn't overwrite an existing file
    /// unless forced, so edits made in the Input Actions editor are kept.
    /// </summary>
    public static class InputActionsBuilder
    {
        const string KM = "Keyboard&Mouse";
        const string GP = "Gamepad";

        [MenuItem("Hearthdelve/Setup/Rebuild Input Actions (overwrites)", priority = 20)]
        static void RebuildMenu() => Build(force: true);

        public static InputActionAsset Build(bool force)
        {
            EditorPaths.Ensure(EditorPaths.Settings);
            if (force || !File.Exists(EditorPaths.InputActions))
            {
                var asset = Create();
                File.WriteAllText(EditorPaths.InputActions, asset.ToJson());
                Object.DestroyImmediate(asset);
                AssetDatabase.ImportAsset(EditorPaths.InputActions, ImportAssetOptions.ForceUpdate);
            }

            AddMissingActions();

            var imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(EditorPaths.InputActions);
            if (imported == null)
            {
                Debug.LogError("[Hearthdelve] Failed to import input actions.");
                return null;
            }
            InputSystem.actions = imported;
            return imported;
        }

        static InputActionAsset Create()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "Hearthdelve";
            asset.AddControlScheme(KM).WithRequiredDevice("<Keyboard>").WithOptionalDevice("<Mouse>");
            asset.AddControlScheme(GP).WithRequiredDevice("<Gamepad>");

            AddDungeonMap(asset);

            // --- Tavern (stub for Phase 2) ---
            var t = asset.AddActionMap(InputMaps.Tavern);
            AddMove(t, "Move");
            AddRightStick(t, TavernActions.LookStick);
            Button(t, "Interact", ("<Keyboard>/e", KM), ("<Keyboard>/space", KM), ("<Gamepad>/buttonSouth", GP));
            Button(t, "MinigameAction", ("<Keyboard>/j", KM), ("<Gamepad>/buttonWest", GP));
            Button(t, "MinigameAlt", ("<Keyboard>/k", KM), ("<Gamepad>/buttonNorth", GP));
            Button(t, "Cancel", ("<Keyboard>/backspace", KM), ("<Gamepad>/buttonEast", GP));
            Button(t, "CyclePrev", ("<Keyboard>/q", KM), ("<Gamepad>/leftShoulder", GP));
            Button(t, "CycleNext", ("<Keyboard>/r", KM), ("<Gamepad>/rightShoulder", GP));
            Button(t, "SpeedUp", ("<Keyboard>/leftShift", KM), ("<Gamepad>/rightTrigger", GP));
            Button(t, "Pause", ("<Keyboard>/escape", KM), ("<Gamepad>/start", GP));
            Button(t, TavernActions.Decorate, ("<Keyboard>/tab", KM), ("<Gamepad>/select", GP));

            // --- Minigame (stub for Phase 2) ---
            var m = asset.AddActionMap(InputMaps.Minigame);
            AddMove(m, "Aim");
            Button(m, "Action", ("<Keyboard>/space", KM), ("<Mouse>/leftButton", KM), ("<Gamepad>/buttonSouth", GP));
            Button(m, "AltAction", ("<Keyboard>/k", KM), ("<Mouse>/rightButton", KM), ("<Gamepad>/buttonNorth", GP));
            Button(m, "Cancel", ("<Keyboard>/escape", KM), ("<Gamepad>/buttonEast", GP));
            AddPoint(m);

            // --- UI (drives uGUI through InputSystemUIInputModule) ---
            var ui = asset.AddActionMap(InputMaps.UI);
            var navigate = ui.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            // A wide dead zone for menus: a released stick springs back past centre, and the default (0.125)
            // read that overshoot as a press the other way, so the selection stepped back (4c step 4 playtest).
            navigate.AddBinding("<Gamepad>/leftStick", groups: GP, processors: "StickDeadzone(min=0.5)");
            navigate.AddBinding("<Gamepad>/dpad", groups: GP);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KM).With("Down", "<Keyboard>/s", KM)
                .With("Left", "<Keyboard>/a", KM).With("Right", "<Keyboard>/d", KM);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", KM).With("Down", "<Keyboard>/downArrow", KM)
                .With("Left", "<Keyboard>/leftArrow", KM).With("Right", "<Keyboard>/rightArrow", KM);
            Button(ui, "Submit", ("<Keyboard>/enter", KM), ("<Keyboard>/space", KM), ("<Keyboard>/j", KM), ("<Gamepad>/buttonSouth", GP));
            Button(ui, "Cancel", ("<Keyboard>/escape", KM), ("<Gamepad>/buttonEast", GP));
            AddAdvance(ui);
            ui.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position", expectedControlLayout: "Vector2");
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
            ui.AddAction("RightClick", InputActionType.PassThrough, "<Mouse>/rightButton", expectedControlLayout: "Button");
            ui.AddAction("MiddleClick", InputActionType.PassThrough, "<Mouse>/middleButton", expectedControlLayout: "Button");
            ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");

            AddDecorateMap(asset);
            return asset;
        }

        /// <summary>
        /// Adds actions introduced after the asset was first generated, leaving everything else in
        /// the file (including edits made in the Input Actions editor) as it is.
        /// </summary>
        static void AddMissingActions()
        {
            var asset = InputActionAsset.FromJson(File.ReadAllText(EditorPaths.InputActions));
            bool changed = false;
            var dungeon = asset.FindActionMap(InputMaps.Dungeon);
            if (dungeon == null || dungeon.FindAction(DungeonActions.AimPoint) == null)
            {
                // The side-scroller Dungeon map (jump, drop-through) is replaced by the top-down one.
                if (dungeon != null) asset.RemoveActionMap(dungeon);
                AddDungeonMap(asset);
                changed = true;
                Debug.Log("[Hearthdelve] Rebuilt the Dungeon action map for top-down controls.");
            }
            dungeon = asset.FindActionMap(InputMaps.Dungeon);
            if (dungeon.FindAction(DungeonActions.AimStick) == null)
            {
                AddAimStick(dungeon);
                changed = true;
            }
            var tavern = asset.FindActionMap(InputMaps.Tavern);
            if (tavern != null && tavern.FindAction(TavernActions.LookStick) == null)
            {
                AddRightStick(tavern, TavernActions.LookStick);
                changed = true;
            }
            if (tavern != null && tavern.FindAction(TavernActions.Decorate) == null)
            {
                // 4h: decorating from anywhere inside in the daytime.
                Button(tavern, TavernActions.Decorate, ("<Keyboard>/tab", KM), ("<Gamepad>/select", GP));
                changed = true;
            }
            var minigame = asset.FindActionMap(InputMaps.Minigame);
            if (minigame != null && minigame.FindAction(MinigameActions.Point) == null)
            {
                AddPoint(minigame);
                changed = true;
            }
            var uiMap = asset.FindActionMap(InputMaps.UI);
            if (uiMap != null && uiMap.FindAction(UIActions.Advance) == null)
            {
                AddAdvance(uiMap);
                changed = true;
            }
            InputActionMap decorate = asset.FindActionMap(InputMaps.Decorate);
            if (decorate == null || decorate.FindAction(DecorateActions.Area) == null)
            {
                // Ours since 4f step 2: rebuilt whole when it gains an action (free placement took LB from Cycle).
                if (decorate != null) asset.RemoveActionMap(decorate);
                AddDecorateMap(asset);
                changed = true;
            }
            if (changed)
            {
                File.WriteAllText(EditorPaths.InputActions, asset.ToJson());
                AssetDatabase.ImportAsset(EditorPaths.InputActions, ImportAssetOptions.ForceUpdate);
            }
            Object.DestroyImmediate(asset);
        }

        /// <summary>Top-down Dungeon controls (GDD section 9).</summary>
        static void AddDungeonMap(InputActionAsset asset)
        {
            var d = asset.AddActionMap(InputMaps.Dungeon);
            AddMove(d, DungeonActions.Move);
            var aim = d.AddAction(DungeonActions.AimPoint, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            aim.AddBinding("<Pointer>/position", groups: KM);
            AddAimStick(d);
            Button(d, DungeonActions.Attack, ("<Mouse>/leftButton", KM), ("<Gamepad>/buttonWest", GP));
            Button(d, DungeonActions.Heavy, ("<Mouse>/rightButton", KM), ("<Gamepad>/buttonNorth", GP));
            Button(d, DungeonActions.Dodge, ("<Keyboard>/space", KM), ("<Gamepad>/buttonEast", GP));
            Button(d, DungeonActions.Interact, ("<Keyboard>/e", KM), ("<Gamepad>/buttonSouth", GP));
            Button(d, DungeonActions.Skill1, ("<Keyboard>/1", KM), ("<Gamepad>/leftShoulder", GP));
            Button(d, DungeonActions.Skill2, ("<Keyboard>/2", KM), ("<Gamepad>/rightShoulder", GP));
            Button(d, DungeonActions.KitchenArts, ("<Keyboard>/q", KM), ("<Gamepad>/rightTrigger", GP));
            Button(d, DungeonActions.Finisher, ("<Keyboard>/f", KM), ("<Gamepad>/leftTrigger", GP));
            Button(d, DungeonActions.Pause, ("<Keyboard>/escape", KM), ("<Gamepad>/start", GP));
        }

        /// <summary>Decorate Mode (4f step 2): controller first, every action on the keyboard too, the mouse pointing at tiles.</summary>
        static void AddDecorateMap(InputActionAsset asset)
        {
            var d = asset.AddActionMap(InputMaps.Decorate);
            AddMove(d, DecorateActions.Move);
            var point = d.AddAction(DecorateActions.Point, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Pointer>/position", groups: KM);
            Button(d, DecorateActions.Select, ("<Keyboard>/e", KM), ("<Keyboard>/enter", KM), ("<Keyboard>/space", KM), ("<Gamepad>/buttonSouth", GP));
            Button(d, DecorateActions.Click, ("<Mouse>/leftButton", KM));
            Button(d, DecorateActions.Cancel, ("<Keyboard>/escape", KM), ("<Mouse>/rightButton", KM), ("<Gamepad>/buttonEast", GP));
            Button(d, DecorateActions.Turn, ("<Keyboard>/r", KM), ("<Gamepad>/buttonWest", GP));
            Button(d, DecorateActions.Flip, ("<Keyboard>/f", KM), ("<Gamepad>/buttonNorth", GP));
            Button(d, DecorateActions.Store, ("<Keyboard>/delete", KM), ("<Keyboard>/backspace", KM), ("<Gamepad>/rightTrigger", GP));
            Button(d, DecorateActions.Undo, ("<Keyboard>/z", KM), ("<Gamepad>/leftTrigger", GP));
            Button(d, DecorateActions.Cycle, ("<Keyboard>/q", KM), ("<Gamepad>/rightShoulder", GP));
            Button(d, DecorateActions.Free, ("<Keyboard>/leftShift", KM), ("<Keyboard>/rightShift", KM), ("<Gamepad>/leftShoulder", GP));
            Button(d, DecorateActions.Storage, ("<Keyboard>/tab", KM), ("<Gamepad>/start", GP));
            Button(d, DecorateActions.Check, ("<Keyboard>/c", KM), ("<Gamepad>/select", GP));
            Button(d, DecorateActions.Style, ("<Keyboard>/v", KM), ("<Gamepad>/rightStickPress", GP));
            Button(d, DecorateActions.Area, ("<Keyboard>/g", KM), ("<Gamepad>/leftStickPress", GP));
            d.AddAction(DecorateActions.Wheel, InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");
        }

        /// <summary>The right stick aims on a gamepad (4e playtest).</summary>
        static void AddAimStick(InputActionMap map) => AddRightStick(map, DungeonActions.AimStick);

        /// <summary>The right stick, for aiming (the Hollows) or looking (the tavern).</summary>
        static void AddRightStick(InputActionMap map, string name)
        {
            var stick = map.AddAction(name, InputActionType.Value, expectedControlLayout: "Vector2");
            stick.AddBinding("<Gamepad>/rightStick", groups: GP, processors: "StickDeadzone(min=0.25)");
        }

        /// <summary>
        /// 4g: the keeper's Interact key also moves dialogue on (talking to someone is E, so the next line is too). Enter, Space
        /// and the gamepad's A do it through Submit.
        /// </summary>
        static void AddAdvance(InputActionMap ui) => Button(ui, UIActions.Advance, ("<Keyboard>/e", KM));

        static void AddPoint(InputActionMap map)
        {
            var point = map.AddAction(MinigameActions.Point, InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Pointer>/position", groups: KM);
        }

        static void AddMove(InputActionMap map, string name)
        {
            var move = map.AddAction(name, InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddBinding("<Gamepad>/leftStick", groups: GP);
            move.AddBinding("<Gamepad>/dpad", groups: GP);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KM).With("Down", "<Keyboard>/s", KM)
                .With("Left", "<Keyboard>/a", KM).With("Right", "<Keyboard>/d", KM);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", KM).With("Down", "<Keyboard>/downArrow", KM)
                .With("Left", "<Keyboard>/leftArrow", KM).With("Right", "<Keyboard>/rightArrow", KM);
        }

        static void Button(InputActionMap map, string name, params (string path, string group)[] bindings)
        {
            var action = map.AddAction(name, InputActionType.Button);
            foreach (var (path, group) in bindings) action.AddBinding(path, groups: group);
        }
    }
}
