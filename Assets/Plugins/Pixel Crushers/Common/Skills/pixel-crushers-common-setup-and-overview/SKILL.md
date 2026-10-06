---
name: pixel-crushers-common-setup-and-overview
description: "Use this skill whenever someone asks 'how do I install Pixel Crushers Common', 'where is the Pixel Crushers menu', 'what namespace should I use', 'what is PixelCrushers.Wrappers for', 'should I use core or wrapper components', 'how do I verify the plugin is installed', 'what does this common library provide', or needs orientation inside the shared code library that ships with every Pixel Crushers asset before diving into a specific subsystem. Covers namespace structure (PixelCrushers core vs PixelCrushers.Wrappers scene-safe wrappers), Tools menu layout, available subsystems, and cross-skill routing. Do NOT use for save/load implementation details (see pixel-crushers-common-save-system), messaging APIs (see pixel-crushers-common-message-system), or UI panel components (see pixel-crushers-common-ui). When in doubt whether a setup or namespace question could involve this asset, use this skill — the Prerequisites section shows how to confirm the asset is installed."
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

# Pixel Crushers Common: Setup & Skill Index

The Pixel Crushers Common Code Library is a shared foundation bundled inside every Pixel Crushers asset (Dialogue System, Quest Machine, etc.). It provides a Save System, a lightweight Message System, UI helper panels, localization utilities, and input-device detection — all under a consistent namespace and assembly structure. Understand the install and namespace conventions once and every Pixel Crushers asset becomes easier to integrate and extend.

## When to use this skill

Use this skill when the request concerns installing or confirming the library, understanding which namespace to reference in code, choosing between core and wrapper components, navigating the `Tools > Pixel Crushers` menu, or deciding which sibling skill to activate for a specific subsystem such as save, messaging, UI, localization, or input-device handling.

## Prerequisites

**Programmatic install check** (robust — iterates all loaded assemblies):

```csharp
using System;
using UnityEngine;

public static bool IsPixelCrushersCommonInstalled()
{
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
        if (assembly.GetType("PixelCrushers.SaveSystem") != null)
            return true;
    }
    return false;
}
```

> Note: `System.Type.GetType("PixelCrushers.SaveSystem, Assembly-CSharp")` returns `null` because Common ships in its own assembly. Always use the `AppDomain` iteration form above.

**Visual checks (any one suffices):**
- Folder `Assets/Plugins/Pixel Crushers/Common` is present in the Project window.
- Menu `Tools > Pixel Crushers > Common` exists in the Unity Editor menu bar.

**If missing:** Common is bundled automatically when you import any Pixel Crushers package (Dialogue System, Quest Machine, etc.) from the Unity Asset Store, publisher Pixel Crushers. Import one of those packages and the Common folder appears under `Assets/Plugins/Pixel Crushers/Common`.

## Quick start

1. Import a Pixel Crushers asset package — Common is included automatically.
2. Confirm `Tools > Pixel Crushers > Common > Text Table Editor` opens without errors.
3. Read `Assets/Plugins/Pixel Crushers/Common/_README.txt` for the changelog and full feature list.
4. Identify your task subsystem in the **Skill index table** (Workflow: Pick the right sibling skill) and activate that skill.

## Workflows

### Workflow: Verify installation

**Goal:** Confirm the library is installed at the expected version and no assembly errors exist.

**Steps:**
1. Open `Window > General > Console` and check for compiler errors referencing `PixelCrushers`.
2. In the Editor menu, open `Tools > Pixel Crushers > Common > Text Table Editor`. It should open cleanly.
3. Create a temporary Editor script and call `Debug.Log(IsPixelCrushersCommonInstalled())` using the snippet from **Prerequisites**.
4. Open `Assets/Plugins/Pixel Crushers/Common/_README.txt` — the first lines state the version number.

**Expected result:** Console prints `True`; the Text Table Editor window opens; `_README.txt` confirms version `1.10.73` or later; no `CS0246` errors in Console.

---

### Workflow: Understand core vs wrapper namespaces

**Goal:** Know when to use `PixelCrushers` (code APIs) vs `PixelCrushers.Wrappers` (scene components), and why the distinction exists.

**Steps:**
1. **In scenes and prefabs, always add wrapper components.** Use `Add Component > Pixel Crushers/...` menus. Wrapper classes live in `PixelCrushers.Wrappers` and provide stable scene references even if internal code is reorganised in future updates.
2. **In C# code, call core static APIs.** Use `PixelCrushers.SaveSystem.SaveToSlot(1)`, `PixelCrushers.MessageSystem.SendMessage(...)`, etc. The core namespace is `PixelCrushers`.
3. In `using` directives, add `using PixelCrushers;` for API calls. Add `using PixelCrushers.Wrappers;` only if you need to reference a wrapper type by name (uncommon).
4. Never call `PixelCrushers.Wrappers.SaveSystem` static methods — always call through `PixelCrushers.SaveSystem`.

**Expected result:** Scene references remain stable across asset updates; code compiles cleanly against the core namespace; no breaking changes when Pixel Crushers releases updates.

---

### Workflow: Pick the right sibling skill

**Goal:** Route a user request to the correct specialist skill rather than attempting full implementation from this overview skill.

**Steps:**
1. Identify the user's subsystem from the table below.
2. Activate the matching skill to get full API detail, workflows, and common-issue guidance.
3. Return to this skill only for installation or namespace questions.

| Skill name | Scope |
|---|---|
| `pixel-crushers-common-save-system` | Save/load state, Saver components, serializers, storers, scene portals, auto-save |
| `pixel-crushers-common-message-system` | Lightweight in-process string-keyed message bus (`MessageSystem.SendMessage`) |
| `pixel-crushers-common-ui` | uGUI panel components (`UIPanel`), animated open/close, focus and back-button handling |
| `pixel-crushers-common-localization` | Text tables, locale switching, string key/value localization |
| `pixel-crushers-common-input-device` | Keyboard/gamepad/mobile input detection and UI cursor management |

**Expected result:** The correct specialist skill is active, and the user's specific request is handled with full API and workflow detail.

## Verification

After import, all of the following should be true simultaneously:
- `Tools > Pixel Crushers > Common` submenu is visible with child items.
- No `CS0246` or other `PixelCrushers`-related compiler errors in Console.
- Programmatic install check returns `true`.
- `Assets/Plugins/Pixel Crushers/Common/_README.txt` is present and readable.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `PixelCrushers.SaveSystem` | `static class` | Core save/load API — see `pixel-crushers-common-save-system` |
| `PixelCrushers.MessageSystem` | `static class` | In-process message bus — see `pixel-crushers-common-message-system` |
| `PixelCrushers.UIPanel` | `MonoBehaviour` | Animated panel base — see `pixel-crushers-common-ui` |
| `PixelCrushers.TextTable` | `ScriptableObject` | Localization key/value table — see `pixel-crushers-common-localization` |
| `PixelCrushers.InputDeviceManager` | `MonoBehaviour` | Runtime input-device detection — see `pixel-crushers-common-input-device` |
| `PixelCrushers.Wrappers.*` | namespace | Scene-stable wrapper components for all subsystems above |
| `Tools > Pixel Crushers > Common > Text Table Editor` | Editor menu | Open the Text Table authoring window |
| `Tools > Pixel Crushers > Common > Save System > Assign Unique Keys...` | Editor menu | Auto-populate Saver keys in the project |
| `Tools > Pixel Crushers > Common > Misc > Enable TextMesh Pro Support...` | Editor menu | Add TextMesh Pro scripting define |
| `Tools > Pixel Crushers > Common > Misc > Use New Input System...` | Editor menu | Switch to the new Input System integration |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `CS0246: The type or namespace 'PixelCrushers' could not be found` | Library not imported or assembly missing | Import a Pixel Crushers package; confirm `Assets/Plugins/Pixel Crushers/Common` exists |
| `Tools > Pixel Crushers` menu is absent | Package not imported or scripts have compile errors | Fix all errors first; reimport if folder is missing — menu appears only when scripts compile cleanly |
| `System.Type.GetType("PixelCrushers.SaveSystem, Assembly-CSharp")` returns `null` | Common ships in its own assembly, not `Assembly-CSharp` | Use the `AppDomain.CurrentDomain.GetAssemblies()` iteration from Prerequisites |
| Wrapper component missing from Add Component menu | Scripts contain compile errors preventing menu rebuild | Resolve all Console errors; wrapper menus reappear automatically after a clean compile |

## Boundaries

This skill covers installation verification, namespace orientation, Tools menu layout, and cross-skill routing only. It does not cover runtime API usage, save data formats, message routing, UI animation, localization workflows, or input configuration — activate the appropriate sibling skill for those. If Common is not installed, no sibling skill can function; resolve the install before proceeding with any subsystem request.
