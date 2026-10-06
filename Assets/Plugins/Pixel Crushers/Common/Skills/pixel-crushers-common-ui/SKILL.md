---
name: pixel-crushers-common-ui
description: "Use this skill whenever you need to create, animate, or script uGUI Canvas panels and menus using the Pixel Crushers Common library — e.g., 'my panel won't animate', 'how do I open a menu from code', 'wire Escape to close a panel', 'gamepad can't navigate my menu', 'back button does nothing', 'keep a tooltip on screen', 'show cursor while UI is open'. Covers UIPanel, UIButtonKeyTrigger, UITextColor, UIScrollbarEnabler, KeepRectTransformOnscreen, DeselectPreviousOnPointerEnter, and ShowCursorWhileEnabled. Do NOT use for input-device detection or cursor-lock management (see pixel-crushers-common-input-device). When in doubt about any Pixel Crushers Common uGUI panel or menu component, use this skill — the Prerequisites section shows how to confirm the asset is installed."
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

# UI Panels & Menus (Pixel Crushers Common)

Pixel Crushers Common provides lightweight uGUI (Canvas-based) components that wrap Unity's standard UI with animated show/hide states, panel focus management, keyboard/gamepad navigation, and utility helpers. `UIPanel` is the centrepiece: it drives Animator triggers to open and close a Canvas panel and tracks `panelState`, optionally deactivating the GameObject when hidden. Companion components handle key-to-button binding, text colouring, scrollbar visibility, screen-edge clamping, pointer-enter deselection, and cursor display. All components are added via **Add Component** in the Inspector and work with any standard Unity Canvas; they are **not** UI Toolkit (UXML/USS).

## When to use this skill

- Animating a menu panel open/closed using Animator triggers (`Show` / `Hide`).
- Calling `Open()`, `Close()`, or `Toggle()` on a panel from C# or from a Button's **On Click**.
- Managing `firstSelected` focus and Back button wiring for gamepad/keyboard navigation.
- Binding a key or gamepad button to click a UI element using `UIButtonKeyTrigger`.
- Keeping a world-space RectTransform inside the camera viewport with `KeepRectTransformOnscreen`.
- Showing/hiding a Scrollbar based on content overflow with `UIScrollbarEnabler`.
- Tinting UI Text on hover with `UITextColor`.
- Showing the cursor only while a specific panel is active using `ShowCursorWhileEnabled`.

## Prerequisites

Confirm the asset is installed by running this snippet in any Editor script:

```csharp
bool installed = System.Type.GetType("PixelCrushers.UIPanel, Assembly-CSharp") != null
              || System.Type.GetType("PixelCrushers.UIPanel, PixelCrushers.Common") != null;
UnityEngine.Debug.Log("Pixel Crushers Common installed: " + installed);
```

If `installed` is `false`, the folder `Assets/Plugins/Pixel Crushers/Common` is missing. Install the **Pixel Crushers Common** package (available on the Unity Asset Store, or bundled with Dialogue System / Quest Machine) and reopen the project.

Additional requirements:
- Unity 2022.3 or later.
- An **EventSystem** in the scene (`GameObject > UI > Event System`) for UI navigation.
- Use components from the `PixelCrushers.Wrappers` namespace when adding them to scene GameObjects for forwards-compatibility.

## Quick start

1. Create a Canvas: `GameObject > UI > Canvas`.
2. Right-click the Canvas in the Hierarchy: `UI > Panel`. Rename it `MyPanel`.
3. Select `MyPanel`. In the Inspector: **Add Component** → search `UI Panel` → select **Pixel Crushers/Common/UI/UI Panel**.
4. Set **Start State** to `Closed`.
5. Enter Play mode and run in any script:

```csharp
FindObjectOfType<PixelCrushers.UIPanel>().Open();
```

6. `MyPanel` becomes active and `panelState` reports `Open`.

## Workflows

### Workflow: Create an Animated Canvas Panel

**Goal:** Build a panel that slides or fades in/out via Animator when opened or closed.

**Steps:**

1. Select the panel GameObject. **Add Component** → `Animator`.
2. Create an Animator Controller asset (`Project > Create > Animator Controller`) and assign it to the Animator.
3. Open `Window > Animation > Animator`. Add two **Trigger** parameters: `Show` and `Hide`.
4. Create states `Opening` and `Closing`. Add transitions: `Any State → Opening` (trigger `Show`); `Any State → Closing` (trigger `Hide`).
5. Add the **UI Panel** component. Verify **Show Animation Trigger** = `Show` and **Hide Animation Trigger** = `Hide`. Names are **case-sensitive** and must exactly match the Animator trigger parameters.
6. Enable **Deactivate On Hidden** (default `true`) to deactivate the GameObject after the close animation finishes.
7. Optionally enable **Wait For Show Animation To Set Open** so `isOpen` becomes `true` only after the open animation completes.

**Expected result:** `panel.Open()` fires the `Show` trigger and animates the panel in; `panelState` transitions `Opening → Open`. `panel.Close()` fires `Hide`; after the animation the GameObject deactivates.

> **No Animator?** `UIPanel` works without one — it activates/deactivates the GameObject immediately with no animation.

---

### Workflow: Open, Close, and Toggle from Code and Buttons

**Goal:** Drive panel visibility from C# and from uGUI Button OnClick events.

**Steps:**

1. Reference `UIPanel` in a MonoBehaviour:

```csharp
using PixelCrushers;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private UIPanel _panel;

    public void OpenMenu()   => _panel.Open();
    public void CloseMenu()  => _panel.Close();
    public void ToggleMenu() => _panel.Toggle();

    private void Update()
    {
        if (_panel.isOpen)
            Debug.Log("State: " + _panel.panelState); // Open or Opening
    }
}
```

2. Assign `_panel` in the Inspector.
3. To wire a **Button**: select it → **On Click ()** → `+` → drag the panel GameObject → choose `UIPanel > Open`.
4. `SetOpen(true/false)` is equivalent to calling `Open()`/`Close()`.

**Expected result:** `Open()` activates the panel and sets `panelState` to `Open` (or `Opening` while animating). The wired Button click triggers the same flow. `Toggle()` switches state on each call.

---

### Workflow: Gamepad/Keyboard Focus and Back Button

**Goal:** Enable full gamepad and keyboard navigation inside a panel, with a functional Back button.

**Steps:**

1. On the **UI Panel** component, assign a Selectable (e.g., the first Button) to **First Selected** — this receives focus when the panel opens.
2. Confirm **Select Previous On Disable** is enabled (default `true`) so focus returns to the previous panel on close.
3. Wire **On Back Button Down** (UnityEvent) to your close/back method via the Inspector or code:

```csharp
using PixelCrushers;
using UnityEngine;

public class BackHandler : MonoBehaviour
{
    [SerializeField] private UIPanel _panel;

    private void Awake() => _panel.onBackButtonDown.AddListener(() => _panel.Close());
}
```

4. To map a key to a Button click: add a child GameObject → **Add Component** → `Pixel Crushers/Common/UI/UI Button Key Trigger`. Set **Key Code** to e.g. `Escape`; assign the Back Button in **Button**.
5. Ensure an EventSystem exists: `GameObject > UI > Event System`.
6. For automatic cursor hide and menu auto-select when the device switches to gamepad, add an **Input Device Manager** — see `pixel-crushers-common-input-device`.

**Expected result:** Opening the panel selects `firstSelected`. Pressing the mapped key fires `onBackButtonDown` and closes the panel. Gamepad stick/D-pad navigates between Selectables.

## Verification

Attach the script below to the same GameObject as `UIPanel`, enter Play mode, and check the Console:

```csharp
using PixelCrushers;
using UnityEngine;

public class UIPanelVerify : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        var panel = GetComponent<UIPanel>();
        Debug.Assert(panel != null, "UIPanel missing on this GameObject.");

        panel.Open();
        yield return new WaitForSeconds(0.1f);
        Debug.Assert(panel.isOpen || panel.panelState == PanelState.Opening,
            "FAIL: panel should be open or opening.");
        Debug.Log("Open: PASS — panelState=" + panel.panelState);

        panel.Close();
        yield return new WaitForSeconds(0.6f); // allow animation
        Debug.Assert(!panel.isOpen, "FAIL: panel should be closed.");
        Debug.Log("Close: PASS");
    }
}
```

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `UIPanel.Open()` | instance method | Shows the panel; fires Show trigger; raises `onOpen`. |
| `UIPanel.Close()` | instance method | Hides the panel; fires Hide trigger; raises `onClose` / `onClosed`. |
| `UIPanel.Toggle()` | instance method | Opens if closed; closes if open. |
| `UIPanel.SetOpen(bool)` | instance method | Calls `Open()` or `Close()` based on argument. |
| `UIPanel.TakeFocus()` | instance method | Makes this the top panel; selects `firstSelected`. |
| `UIPanel.SetFocus(GameObject)` | instance method | Selects a specific Selectable inside the panel. |
| `UIPanel.isOpen` | `bool` property | `true` when `panelState == Open`. |
| `UIPanel.panelState` | `PanelState` property | `Uninitialized`, `Opening`, `Open`, `Closing`, or `Closed`. |
| `UIPanel.topPanel` | static `UIPanel` | The currently focused top panel. |
| `UIPanel.firstSelected` | `GameObject` field | Selectable auto-focused when the panel opens. |
| `UIPanel.showAnimationTrigger` | `string` field | Animator trigger for opening (default `"Show"`). |
| `UIPanel.hideAnimationTrigger` | `string` field | Animator trigger for closing (default `"Hide"`). |
| `UIPanel.deactivateOnHidden` | `bool` field | Deactivates GameObject after close animation (default `true`). |
| `UIPanel.startState` | `StartState` field | `GameObjectState`, `Open`, or `Closed` at scene start. |
| `UIPanel.onOpen` | `UnityEvent` | Raised when opening begins. |
| `UIPanel.onClose` | `UnityEvent` | Raised when closing begins. |
| `UIPanel.onClosed` | `UnityEvent` | Raised after close animation completes. |
| `UIPanel.onBackButtonDown` | `UnityEvent` | Raised when back input is detected. |
| `UIButtonKeyTrigger` | component | Clicks a Selectable when a KeyCode or input button is pressed. |
| `UITextColor.ApplyColor()` | instance method | Tints the UI Text to the configured colour. |
| `UITextColor.UndoColor()` | instance method | Reverts the UI Text tint. |
| `UIScrollbarEnabler` | component | Shows/hides a Scrollbar based on content vs viewport height. |
| `KeepRectTransformOnscreen` | component | Clamps a world-space RectTransform inside the camera viewport. |
| `DeselectPreviousOnPointerEnter` | component | Selects this element on pointer enter (mouse + gamepad harmony). |
| `ShowCursorWhileEnabled` | component | Shows the cursor while this GameObject is active. |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Panel doesn't animate | Animator missing or trigger names mismatch `showAnimationTrigger` / `hideAnimationTrigger` | Match trigger parameter names exactly (case-sensitive); remove the Animator if animation isn't needed. |
| Panel never deactivates after Close | `deactivateOnHidden` is `false`, or close animation never reaches a terminal state | Enable **Deactivate On Hidden**; verify Animator transitions complete. |
| Gamepad can't navigate the menu | `firstSelected` not assigned or no EventSystem in scene | Assign a Selectable to **First Selected**; add `GameObject > UI > Event System`. |
| Back button does nothing | `onBackButtonDown` not wired or no `UIButtonKeyTrigger` configured | Wire `onBackButtonDown` to your close method; add `UIButtonKeyTrigger` with the correct key. |
| `PixelCrushers.UIPanel` type not found in code | Asset not installed | Verify `Assets/Plugins/Pixel Crushers/Common` exists; follow the Prerequisites install check. |

## Boundaries

- Works exclusively with **uGUI Canvas** hierarchies — **not** compatible with UI Toolkit (UXML/USS).
- Does not build or lay out UI for you; use Unity's standard Layout Groups and uGUI tools for that.
- Animations are driven by Animator Controllers you author; `UIPanel` only sets Animator triggers.
- For cross-input-system button/axis queries, cursor lock, and device-aware auto-focus, see `pixel-crushers-common-input-device`.
