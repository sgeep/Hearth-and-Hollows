---
name: quest-machine-setup-and-overview
description: "Use this skill whenever someone asks 'how do I install Quest Machine', 'where is the Quest Machine menu', 'what is Quest Machine', 'how do I verify Quest Machine is installed', 'what scripting defines do I need for 2D or TextMesh Pro', 'where are the demo scenes', 'how do I open the Welcome Window', or needs orientation inside the Quest Machine asset before diving into a specific subsystem. Covers install verification, Welcome Window setup, scripting defines (USE_PHYSICS2D, TMP_PRESENT), New Input System support, key file locations (Prefabs/, Documentation/, Demo/), and opening the two demo scenes. Do NOT use for authoring quest content (see quest-machine-author-quests), adding quest givers (see quest-machine-quest-givers), scene scaffolding (see quest-machine-scene-setup), or runtime scripting (see quest-machine-runtime-scripting). When in doubt whether an orientation or setup question involves Quest Machine, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Quest Machine"
  publisher: "Pixel Crushers"
  asset-version: "1.2.73"
  skill-version: "1.0.0"
  unity: "2022.3+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/game-toolkits"
  asset-store-url: "https://assetstore.unity.com/packages/tools/game-toolkits/quest-machine-39834"
  documentation-url: "https://www.pixelcrushers.com/quest_machine/"
  support-url: "https://www.pixelcrushers.com/support/"
  last-verified: "2026-07-08"
---

# Quest Machine — Setup and Overview

Quest Machine is a comprehensive, message-driven quest system for Unity supporting both hand-authored and procedurally-generated quests. It ships with journal, HUD, dialogue, and alert UIs (uGUI; TextMesh Pro optional), a Quest Editor window, a Quest Generator, and a full demo sandbox. This skill orients you inside the asset — verifying installation, enabling optional feature defines, locating key files, and running demo scenes — before handing off to the specialised skills listed at the bottom.

## When to use this skill

- User asks "what is Quest Machine?" or "is Quest Machine installed?".
- User needs to open the Welcome Window or enable scripting defines.
- User wants to know where demo scenes, prefabs, or documentation live.
- User needs general orientation before starting quest authoring, scene setup, or runtime scripting.

## Prerequisites

Quest Machine must be imported from the Unity Asset Store before any other step.

**Programmatic install check** — run as a RunCommand to confirm the core type is accessible:

```csharp
using System;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var t = Type.GetType(
                "PixelCrushers.QuestMachine.QuestMachine, Assembly-CSharp", false)
            ?? Type.GetType(
                "PixelCrushers.QuestMachine.QuestMachine, PixelCrushers.QuestMachine", false);

        if (t != null)
            result.Log("Quest Machine installed. Core type: " + t.FullName);
        else
            result.LogWarning(
                "Quest Machine NOT found. Import from: " +
                "https://assetstore.unity.com/packages/tools/game-toolkits/quest-machine-39834");
    }
}
```

If the type is not found, stop and direct the user to the Asset Store URL above before continuing.

## Quick start

1. Import Quest Machine from the Asset Store.
   - **Expected:** `Assets/Plugins/Pixel Crushers/Quest Machine/` folder appears in the Project window.
2. Open `Tools > Pixel Crushers > Quest Machine > Welcome Window`.
   - **Expected:** Welcome Window opens showing define checkboxes.
3. Enable any required defines (see Workflows below), then open the Quick Start demo to validate.
   - **Expected:** Demo scene loads and plays without Console errors.

## Workflows

### Workflow: Verify Installation

1. In the menu bar check that `Tools > Pixel Crushers > Quest Machine` exists.
   - **Expected:** Submenu lists Welcome Window, Quest Editor, Quest Generator, Quest Reference.
2. If the menu is absent, run the install-check RunCommand from Prerequisites.
   - **Expected:** Console prints `Quest Machine installed. Core type: ...` — or warns with the Asset Store URL.
3. In the Project window navigate to `Assets/Plugins/Pixel Crushers/Quest Machine/`.
   - **Expected:** Subfolders `Prefabs/`, `Scripts/`, `Wrappers/`, `Demo/`, and `Documentation/` are all present.

### Workflow: Configure Scripting Defines (Welcome Window)

1. Open `Tools > Pixel Crushers > Quest Machine > Welcome Window`.
   - **Expected:** Welcome Window opens with checkboxes for optional features.
2. **2D physics** — check **Use Physics 2D** to set the `USE_PHYSICS2D` define.
   - **Expected:** Unity recompiles; QuestControl components now detect Collider2D / Rigidbody2D.
3. **TextMesh Pro** (Unity < 6 only) — check **TextMesh Pro** to set `TMP_PRESENT`.
   - **Expected:** Unity recompiles; Quest Machine UIs switch to TMP text components.
4. **New Input System** — install `com.unity.inputsystem` via Package Manager, then enable it in the Welcome Window.
   - **Expected:** `Input Device Manager` component becomes available; see `Assets/Plugins/Pixel Crushers/Quest Machine/Demo/New Input System/_New_Input_System_Setup.txt` for full wiring details.

### Workflow: Open Demo Scenes

1. Open `Assets/Plugins/Pixel Crushers/Quest Machine/Demo/Quick Start/Quick Start.unity`.
   - **Expected:** Scene loads with a single NPC quest giver and a minimal HUD; no Console errors.
2. Enter Play mode.
   - **Expected:** Approaching the NPC triggers a quest-offer dialogue; accepting it adds the quest to the journal.
3. Open `Assets/Plugins/Pixel Crushers/Quest Machine/Demo/Demo.unity` (requires `USE_PHYSICS2D`).
   - **Expected:** Full sandbox with hand-authored and procedural quests; all UI panels respond correctly.

## Verification

- `Tools > Pixel Crushers > Quest Machine > Welcome Window` opens without errors after import.
- Console shows no Quest Machine errors on domain reload.
- Quick Start demo runs through quest acceptance without errors in Play mode.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `PixelCrushers.QuestMachine.QuestMachine` | `static class` | Global manager; holds default UI refs and quest-state query/set methods |
| `PixelCrushers.QuestMachine.QuestMachineConfiguration` | Component | Global config; registers default UIs on Awake; one instance per scene |
| `PixelCrushers.QuestMachine.QuestJournal` | Component | Attached to player; holds and tracks active quests |
| `PixelCrushers.QuestMachine.QuestGiver` | Component | Attached to NPCs; offers quests to a quester |
| `PixelCrushers.QuestMachine.QuestControl` | Component | Trigger/collision-based quest interactions |
| `PixelCrushers.QuestMachine.Wrappers.*` | Subclasses | Scene-safe wrappers with `AddComponentMenu` paths — use these in scenes |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `Tools > Pixel Crushers > Quest Machine` menu missing | Package not imported or import failed | Re-import from Asset Store; check Console for compile errors |
| 2D colliders not detected by QuestControl | `USE_PHYSICS2D` define not set | Welcome Window → enable **Use Physics 2D** → allow recompile |
| TMP text components not found | `TMP_PRESENT` missing on Unity < 6 | Welcome Window → enable **TextMesh Pro** → allow recompile |
| Demo scene plays but no quest is offered | Wrong physics mode for the Demo | Enable `USE_PHYSICS2D` before running the Demo scene |

## Boundaries

This skill covers **installation and orientation only**. Hand off to the skills below:

| Skill | Purpose |
|---|---|
| `quest-machine-scene-setup` | Add Quest Machine prefabs and components to a new scene |
| `quest-machine-author-quests` | Create and edit Quest assets in the Quest Editor |
| `quest-machine-quest-givers` | Configure NPC quest givers and offer logic |
| `quest-machine-runtime-scripting` | Call `QuestMachine.GiveQuestToQuester`, `GetQuestState`, etc. at runtime |
| `quest-machine-procedural-generation` | Configure the Quest Generator for procedural quest creation |
