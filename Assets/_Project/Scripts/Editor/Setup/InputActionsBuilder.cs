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

            // --- Dungeon (GDD §9) ---
            var d = asset.AddActionMap(InputMaps.Dungeon);
            AddMove(d, DungeonActions.Move);
            Button(d, DungeonActions.Jump, ("<Keyboard>/space", KM), ("<Gamepad>/buttonSouth", GP));
            Button(d, DungeonActions.Attack, ("<Keyboard>/j", KM), ("<Mouse>/leftButton", KM), ("<Gamepad>/buttonWest", GP));
            Button(d, DungeonActions.Secondary, ("<Keyboard>/k", KM), ("<Mouse>/rightButton", KM), ("<Gamepad>/buttonNorth", GP));
            Button(d, DungeonActions.Dodge, ("<Keyboard>/leftShift", KM), ("<Keyboard>/l", KM), ("<Gamepad>/buttonEast", GP));
            Button(d, DungeonActions.Interact, ("<Keyboard>/e", KM), ("<Gamepad>/dpad/up", GP));
            Button(d, DungeonActions.Skill1, ("<Keyboard>/q", KM), ("<Gamepad>/leftShoulder", GP));
            Button(d, DungeonActions.Skill2, ("<Keyboard>/r", KM), ("<Gamepad>/rightShoulder", GP));
            Button(d, DungeonActions.KitchenArts, ("<Keyboard>/f", KM), ("<Gamepad>/rightTrigger", GP));
            Button(d, DungeonActions.Finisher, ("<Keyboard>/c", KM), ("<Gamepad>/leftTrigger", GP));
            Button(d, DungeonActions.Pause, ("<Keyboard>/escape", KM), ("<Gamepad>/start", GP));

            // --- Tavern (stub for Phase 2) ---
            var t = asset.AddActionMap(InputMaps.Tavern);
            AddMove(t, "Move");
            Button(t, "Interact", ("<Keyboard>/e", KM), ("<Keyboard>/space", KM), ("<Gamepad>/buttonSouth", GP));
            Button(t, "MinigameAction", ("<Keyboard>/j", KM), ("<Gamepad>/buttonWest", GP));
            Button(t, "MinigameAlt", ("<Keyboard>/k", KM), ("<Gamepad>/buttonNorth", GP));
            Button(t, "Cancel", ("<Keyboard>/backspace", KM), ("<Gamepad>/buttonEast", GP));
            Button(t, "CyclePrev", ("<Keyboard>/q", KM), ("<Gamepad>/leftShoulder", GP));
            Button(t, "CycleNext", ("<Keyboard>/r", KM), ("<Gamepad>/rightShoulder", GP));
            Button(t, "SpeedUp", ("<Keyboard>/leftShift", KM), ("<Gamepad>/rightTrigger", GP));
            Button(t, "Pause", ("<Keyboard>/escape", KM), ("<Gamepad>/start", GP));

            // --- Minigame (stub for Phase 2) ---
            var m = asset.AddActionMap(InputMaps.Minigame);
            AddMove(m, "Aim");
            Button(m, "Action", ("<Keyboard>/space", KM), ("<Mouse>/leftButton", KM), ("<Gamepad>/buttonSouth", GP));
            Button(m, "AltAction", ("<Keyboard>/k", KM), ("<Mouse>/rightButton", KM), ("<Gamepad>/buttonNorth", GP));
            Button(m, "Cancel", ("<Keyboard>/escape", KM), ("<Gamepad>/buttonEast", GP));
            AddPoint(m);

            // --- UI (drives UI Toolkit through InputSystemUIInputModule) ---
            var ui = asset.AddActionMap(InputMaps.UI);
            var navigate = ui.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            navigate.AddBinding("<Gamepad>/leftStick", groups: GP);
            navigate.AddBinding("<Gamepad>/dpad", groups: GP);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KM).With("Down", "<Keyboard>/s", KM)
                .With("Left", "<Keyboard>/a", KM).With("Right", "<Keyboard>/d", KM);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", KM).With("Down", "<Keyboard>/downArrow", KM)
                .With("Left", "<Keyboard>/leftArrow", KM).With("Right", "<Keyboard>/rightArrow", KM);
            Button(ui, "Submit", ("<Keyboard>/enter", KM), ("<Keyboard>/space", KM), ("<Keyboard>/j", KM), ("<Gamepad>/buttonSouth", GP));
            Button(ui, "Cancel", ("<Keyboard>/escape", KM), ("<Gamepad>/buttonEast", GP));
            ui.AddAction("Point", InputActionType.PassThrough, "<Pointer>/position", expectedControlLayout: "Vector2");
            ui.AddAction("Click", InputActionType.PassThrough, "<Mouse>/leftButton", expectedControlLayout: "Button");
            ui.AddAction("RightClick", InputActionType.PassThrough, "<Mouse>/rightButton", expectedControlLayout: "Button");
            ui.AddAction("MiddleClick", InputActionType.PassThrough, "<Mouse>/middleButton", expectedControlLayout: "Button");
            ui.AddAction("ScrollWheel", InputActionType.PassThrough, "<Mouse>/scroll", expectedControlLayout: "Vector2");

            return asset;
        }

        /// <summary>
        /// Adds actions introduced after the asset was first generated, leaving everything else in
        /// the file (including edits made in the Input Actions editor) as it is.
        /// </summary>
        static void AddMissingActions()
        {
            var asset = InputActionAsset.FromJson(File.ReadAllText(EditorPaths.InputActions));
            var minigame = asset.FindActionMap(InputMaps.Minigame);
            bool changed = false;
            if (minigame != null && minigame.FindAction(MinigameActions.Point) == null)
            {
                AddPoint(minigame);
                changed = true;
            }
            if (changed)
            {
                File.WriteAllText(EditorPaths.InputActions, asset.ToJson());
                AssetDatabase.ImportAsset(EditorPaths.InputActions, ImportAssetOptions.ForceUpdate);
                Debug.Log("[Hearthdelve] Added missing input actions (Minigame/Point).");
            }
            Object.DestroyImmediate(asset);
        }

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
