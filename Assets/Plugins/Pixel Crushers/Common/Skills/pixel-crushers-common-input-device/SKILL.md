---
name: pixel-crushers-common-input-device
description: "Use this skill whenever you need to detect which input device is active, manage cursor lock or visibility, auto-focus menus when a gamepad is used, or query buttons/axes in a way that works under both the legacy Input Manager and the new Input System — e.g., 'cursor won't hide when I pick up a controller', 'IsButtonDown always false with new Input System', 'how do I check which device is active', 'menu doesn't auto-select on gamepad', 'how do I lock the cursor in-game', 'works with keyboard but not gamepad'. Covers InputDeviceManager and its static helpers. Do NOT use for animated panel open/close or UI component wiring (see pixel-crushers-common-ui). When in doubt about input-device detection or cross-input-system queries in a Pixel Crushers Common project, use this skill — the Prerequisites section shows how to confirm the asset is installed."
metadata:
  asset: "Pixel Crushers Common"
  publisher: "Pixel Crushers"
  asset-version: "1.10.73"
  skill-version: "1.0.0"
  unity: "2022.3+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/behavior-ai"
  support-url: "https://www.pixelcrushers.com/support/"
  last-verified: "2026-08-21"
---

# Input Device Management (Pixel Crushers Common)

`InputDeviceManager` is a scene-level singleton component from Pixel Crushers Common that automatically detects the active input device (joystick, keyboard, mouse, or touch), controls cursor lock and visibility, and toggles menu auto-focus for seamless gamepad/mouse transitions. It also exposes a unified static API for querying buttons, axes, and keys that routes correctly under **both** the legacy Unity Input Manager and the new Input System — without requiring callers to know which backend is active. It pairs with `UIPanel` (see `pixel-crushers-common-ui`) to drive automatic first-selected focus when the player switches to a controller.

## When to use this skill

- The cursor doesn't hide when the player picks up a gamepad.
- Menus don't auto-select the first button when switching to gamepad or keyboard.
- You need button/axis queries that work under both the legacy and new Input System.
- `InputDeviceManager.IsButtonDown` always returns `false` after switching to the new Input System.
- You want to lock, unlock, or confine the cursor based on the active input device.
- You need to read `currentInputDevice` to branch logic by device type.
- You're configuring `InputDeviceManager.autoFocus` alongside `UIPanel.firstSelected` — also see `pixel-crushers-common-ui`.

## Prerequisites

Confirm the asset is installed by running this snippet in any Editor script:

```csharp
bool installed = System.Type.GetType("PixelCrushers.InputDeviceManager, Assembly-CSharp") != null
              || System.Type.GetType("PixelCrushers.InputDeviceManager, PixelCrushers.Common") != null;
UnityEngine.Debug.Log("Pixel Crushers Common installed: " + installed);
```

If `installed` is `false`, the folder `Assets/Plugins/Pixel Crushers/Common` is missing. Install the **Pixel Crushers Common** package (available on the Unity Asset Store, or bundled with Dialogue System / Quest Machine) and reopen the project.

Additional requirements:
- Unity 2022.3 or later.
- **New Input System:** enable the integration before querying via `Tools > Pixel Crushers > Common > Misc > Use New Input System...`.
- Use components from the `PixelCrushers.Wrappers` namespace when adding them to scene GameObjects for forwards-compatibility.

## Quick start

1. In the Hierarchy, `GameObject > Create Empty`. Rename it `InputDeviceManager`.
2. **Add Component** → search `Input Device Manager` → select **Pixel Crushers/Common/UI/Input Device Manager**.
3. Enter Play mode. Press a gamepad button — **Current Input Device** in the Inspector switches to `Joystick` and the cursor hides.
4. Move the mouse — it switches back to `Mouse` and the cursor reappears.

## Workflows

### Workflow: Add Input Device Manager to the Scene

**Goal:** Enable automatic cursor management and device-mode detection across the project.

**Steps:**

1. Create an empty GameObject at the scene root. Name it `InputDeviceManager`.
2. **Add Component** → `Pixel Crushers/Common/UI/Input Device Manager`.
3. Configure the Inspector fields:
   - **Cursor Lock Mode** — `None`, `Locked`, or `Confined` while the game runs.
   - **Auto Focus** — enable so `UIPanel` components automatically select `firstSelected` when the device switches to joystick or keyboard.
   - **Key Input Switches Mode To** — choose `Keyboard` or `Mouse` to control what keyboard input reports as.
4. If the menus span multiple scenes, prevent destruction: call `DontDestroyOnLoad` on this GameObject or place it in a persistent bootstrapping scene.

**Expected result:** In Play mode `InputDeviceManager.instance` is non-null. Pressing a gamepad button switches `currentInputDevice` to `Joystick`; cursor lock and visibility update to match the configured mode.

---

### Workflow: Query Input Cross-Input-System

**Goal:** Check button and axis state from C# in a way that works under both the legacy Input Manager and the new Input System.

**Steps:**

Use the static `InputDeviceManager` API instead of `UnityEngine.Input` directly:

```csharp
using PixelCrushers;
using UnityEngine;

public class CrossInputExample : MonoBehaviour
{
    private void Update()
    {
        // Button queries — work under both legacy and new Input System:
        if (InputDeviceManager.IsButtonDown("Fire1"))
            Debug.Log("Fire1 pressed this frame.");

        if (InputDeviceManager.IsButtonPressed("Jump"))
            Debug.Log("Jump held.");

        if (InputDeviceManager.IsButtonUp("Fire1"))
            Debug.Log("Fire1 released this frame.");

        // Key queries:
        if (InputDeviceManager.IsKeyDown(KeyCode.Space))
            Debug.Log("Space key down this frame.");

        if (InputDeviceManager.IsKeyPressed(KeyCode.LeftShift))
            Debug.Log("Left Shift held.");

        // Axes:
        float h = InputDeviceManager.GetAxis("Horizontal");
        float v = InputDeviceManager.GetAxis("Vertical");

        // Mouse position:
        Vector3 mousePos = InputDeviceManager.GetMousePosition();

        // Current device:
        InputDevice device  = InputDeviceManager.currentInputDevice;
        bool usesCursor     = InputDeviceManager.deviceUsesCursor;
        Debug.Log($"Device={device}  cursor={usesCursor}  h={h:F2}  v={v:F2}");
    }
}
```

> **New Input System:** If `IsButtonDown` always returns `false`, the integration define is not set. Run `Tools > Pixel Crushers > Common > Misc > Use New Input System...` and follow the prompts, then recompile.

**Expected result:** Button and axis queries return correct values regardless of which Input System backend the project uses.

---

### Workflow: React to Device Changes and Auto-Focus Menus

**Goal:** Show/hide the cursor and enable gamepad menu auto-selection when the active device changes.

**Steps:**

1. Ensure **Auto Focus** is enabled on the `InputDeviceManager` component.
2. Ensure each `UIPanel` that needs gamepad navigation has **First Selected** assigned — see `pixel-crushers-common-ui`.
3. Poll or react in code:

```csharp
using PixelCrushers;
using UnityEngine;

public class DeviceReactor : MonoBehaviour
{
    private InputDevice _lastDevice;

    private void Update()
    {
        InputDevice current = InputDeviceManager.currentInputDevice;
        if (current == _lastDevice) return;
        _lastDevice = current;

        bool showCursor = InputDeviceManager.deviceUsesCursor;
        Debug.Log($"Device changed to {current}. Cursor visible: {showCursor}");
    }
}
```

4. Poll `InputDeviceManager.isBackButtonDown` each frame for a frame-accurate back input that works across devices.
5. Wire panel back buttons through `UIPanel.onBackButtonDown` or `UIButtonKeyTrigger` — see `pixel-crushers-common-ui`.

**Expected result:** Switching from mouse to gamepad logs the change. The cursor hides per the lock mode. If a `UIPanel` is open with `firstSelected` assigned, it auto-selects the first button automatically.

## Verification

Attach this script to any GameObject in a scene that has `InputDeviceManager`, enter Play mode, and check the Console:

```csharp
using PixelCrushers;
using UnityEngine;

public class InputDeviceVerify : MonoBehaviour
{
    private void Start()
    {
        var mgr = InputDeviceManager.instance;
        Debug.Assert(mgr != null,
            "FAIL: InputDeviceManager.instance is null — add the component to the scene.");
        Debug.Log("Manager present: PASS");
        Debug.Log("CurrentDevice=" + InputDeviceManager.currentInputDevice);
        Debug.Log("IsInputAllowed=" + InputDeviceManager.isInputAllowed);
        Debug.Log("DeviceUsesCursor=" + InputDeviceManager.deviceUsesCursor);
    }

    private void Update()
    {
        if (InputDeviceManager.IsAnyKeyDown())
            Debug.Log("Input detected. Device=" + InputDeviceManager.currentInputDevice);
    }
}
```

Press a gamepad button in Play mode; the log should report `Device=Joystick`.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `InputDeviceManager.instance` | static `InputDeviceManager` | Active singleton; `null` if no component is in the scene. |
| `InputDeviceManager.currentInputDevice` | static `InputDevice` | `Joystick`, `Keyboard`, `Mouse`, or `Touch`. |
| `InputDeviceManager.deviceUsesCursor` | static `bool` | `true` when the current device uses a visible cursor. |
| `InputDeviceManager.cursorLockMode` | static `CursorLockMode` | Current cursor lock mode applied by the manager. |
| `InputDeviceManager.autoFocus` | static `bool` | When `true`, auto-selects `UIPanel.firstSelected` on gamepad/keyboard. |
| `InputDeviceManager.isBackButtonDown` | static `bool` | `true` on the frame the back input is pressed. |
| `InputDeviceManager.isInputAllowed` | static `bool` | Set to `false` to block all input queries globally. |
| `InputDeviceManager.IsKeyDown(KeyCode)` | static `bool` | `true` on the frame the key is first pressed. |
| `InputDeviceManager.IsKeyUp(KeyCode)` | static `bool` | `true` on the frame the key is released. |
| `InputDeviceManager.IsKeyPressed(KeyCode)` | static `bool` | `true` while the key is held down. |
| `InputDeviceManager.IsAnyKeyDown()` | static `bool` | `true` if any key or button was pressed this frame. |
| `InputDeviceManager.IsButtonDown(string)` | static `bool` | `true` on the frame the named button is pressed. |
| `InputDeviceManager.IsButtonUp(string)` | static `bool` | `true` on the frame the named button is released. |
| `InputDeviceManager.IsButtonPressed(string)` | static `bool` | `true` while the named button is held. |
| `InputDeviceManager.GetAxis(string)` | static `float` | Returns the current value of the named axis. |
| `InputDeviceManager.GetMousePosition()` | static `Vector3` | Returns the current mouse/pointer screen position. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `IsButtonDown` always returns `false` with new Input System | New Input System integration define not enabled | Run `Tools > Pixel Crushers > Common > Misc > Use New Input System...` and enable the define; recompile. |
| Cursor won't hide or show on device switch | No `InputDeviceManager` in scene, or `cursorLockMode` is overridden elsewhere | Add the component; ensure nothing else sets `Cursor.lockState` against the manager's intent. |
| Menu doesn't auto-select on gamepad | `autoFocus` is `false`, `UIPanel.firstSelected` not assigned, or no EventSystem | Enable **Auto Focus** on the manager; assign `firstSelected` on each `UIPanel`; add `GameObject > UI > Event System`. |
| `InputDeviceManager.instance` is `null` | Component not present in the scene | Add an empty GameObject with the **Input Device Manager** component. |
| Back button not detected | `isBackButtonDown` not polled, or back action not configured | Poll `isBackButtonDown` each `Update`; or wire `UIPanel.onBackButtonDown` — see `pixel-crushers-common-ui`. |

## Boundaries

- A thin cross-input-system query and device-detection helper — **not** a full input-rebinding or action-mapping UI; use Unity's Input System Action Assets for custom rebinding.
- Abstracts named button/axis strings; it does not define them — action names must already exist in the legacy Input Manager or the Input System Action Asset.
- Does not manage Input System Action Maps or enable/disable them; that remains application code.
- For animated panel open/close, focus management, and per-panel Back button wiring, see `pixel-crushers-common-ui`.
