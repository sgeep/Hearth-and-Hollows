---
name: quest-machine-scene-setup
description: "Use this skill whenever someone asks 'how do I add Quest Machine to my scene', 'what prefabs do I need for Quest Machine', 'where do I put the QuestJournal', 'how do I add the quest UI to my scene', 'defaultQuestJournalUI is null in Play mode', 'my quest journal is not showing up', or needs to scaffold a playable quest scene from scratch. Covers adding the Quest Machine manager prefab, attaching QuestJournal to the player, wiring the Journal/HUD/Dialogue/Alert UI prefabs, and verifying correct component registration. Do NOT use for authoring quest content (see quest-machine-author-quests), configuring NPC quest givers (see quest-machine-quest-givers), or calling Quest Machine APIs at runtime (see quest-machine-runtime-scripting). When in doubt whether the task involves placing Quest Machine components in a Unity scene, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Quest Machine — Scene Setup

This skill scaffolds a playable quest scene from scratch: dropping in the Quest Machine manager (via the ready-made `Quest Machine.prefab` or individual components), attaching a `QuestJournal` to the player, and wiring the four standard UIs (Journal, HUD, Dialogue, Alert). By the end the scene contains a functional quest runtime ready to receive quests from NPCs or scripts, with all default UIs registered.

## When to use this skill

- User asks "how do I add Quest Machine to my scene?" or "what prefabs do I need?".
- User reports `QuestMachine.defaultQuestJournalUI` is null in Play mode.
- User needs to attach a `QuestJournal` to a player GameObject.
- User needs to add or rewire the Journal, HUD, Dialogue, or Alert UI prefabs.
- User is setting up a brand-new scene and needs the quest runtime ready to go.

## Prerequisites

1. Quest Machine is imported. Confirm with the install check in **quest-machine-setup-and-overview**.
2. The scene has a player GameObject (any name — you will attach `QuestJournal` to it).
3. No `QuestMachineConfiguration` already exists in the scene (avoid duplicates).

## Quick start

1. Drag `Assets/Plugins/Pixel Crushers/Quest Machine/Prefabs/Quest Machine.prefab` into the scene Hierarchy.
   - **Expected:** A `Quest Machine` root GameObject appears, already containing the four UI sub-objects.
2. Select your player GameObject and click **Add Component → Pixel Crushers/Quest Machine/Quest Journal**.
   - **Expected:** `Quest Journal (Script)` appears in the player's Inspector.
3. Enter Play mode.
   - **Expected:** No Console errors; the quest runtime is active.

## Workflows

### Workflow: Add the Quest Machine Manager (Prefab method — recommended)

`Quest Machine.prefab` bundles `QuestMachineConfiguration` plus the four default UI sub-objects into one drop, ensuring correct Awake load order.

1. In the Project window locate `Assets/Plugins/Pixel Crushers/Quest Machine/Prefabs/Quest Machine.prefab`.
   - **Expected:** Prefab is visible; Inspector preview shows `Quest Machine Configuration (Script)`.
2. Drag it into the scene Hierarchy.
   - **Expected:** `Quest Machine` root appears with four UI child objects (Journal UI, HUD, Dialogue UI, Alert UI).
3. Verify there is **exactly one** `QuestMachineConfiguration` in the scene — duplicates produce "duplicate manager" Console warnings.

**Programmatic alternative** — instantiates the prefab via RunCommand:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        const string path =
            "Assets/Plugins/Pixel Crushers/Quest Machine/Prefabs/Quest Machine.prefab";

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            result.LogError("Quest Machine prefab not found at: " + path);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        result.RegisterObjectCreation(instance);

        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        result.Log("Quest Machine manager added: {0}", instance);
    }
}
```

- **Expected:** `Quest Machine` appears in the Hierarchy with `Quest Machine Configuration (Script)` visible in the Inspector and the scene file saved.

### Workflow: Add QuestJournal to the Player

1. Select the player GameObject in the Hierarchy.
2. Click **Add Component** and choose `Pixel Crushers/Quest Machine/Quest Journal`.
   - **Expected:** `Quest Journal (Script)` appears on the player with an `Actor` field and an empty quest list.

**Programmatic alternative** — targets a GameObject named `"Player"`:

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            result.LogError(
                "No GameObject named 'Player' found. " +
                "Edit this script to match your player's exact name.");
            return;
        }

        if (player.GetComponent<PixelCrushers.QuestMachine.Wrappers.QuestJournal>() != null)
        {
            result.Log("QuestJournal already present on {0}.", player);
            return;
        }

        result.RegisterObjectModification(player);
        player.AddComponent<PixelCrushers.QuestMachine.Wrappers.QuestJournal>();

        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        result.Log("QuestJournal added to {0}.", player);
    }
}
```

- **Expected:** Player's Inspector shows `Quest Journal (Script)` with an `Actor` ID field.

### Workflow: Add UI Prefabs Individually (alternative to manager prefab)

Use this only when you need the four UIs placed on separate Canvases. If you used the `Quest Machine.prefab` in Workflow 1, the UIs are already bundled — skip this.

1. Drag each prefab from `Assets/Plugins/Pixel Crushers/Quest Machine/Prefabs/UI/` into the scene:
   - `Quest Journal UI.prefab`
   - `Quest Dialogue UI.prefab`
   - `Quest HUD.prefab`
   - `Quest Alert UI.prefab`
   - **Expected:** Four root GameObjects, each containing a Canvas.
2. Or add components manually via **Add Component**:
   - `Pixel Crushers/Quest Machine/UI/Unity UI Quest Journal UI`
   - `Pixel Crushers/Quest Machine/UI/Unity UI Quest HUD`
   - `Pixel Crushers/Quest Machine/UI/Unity UI Quest Dialogue UI`
   - `Pixel Crushers/Quest Machine/UI/Unity UI Quest Alert UI`
   - **Expected:** Each registers itself with `QuestMachineConfiguration` on Awake.
3. Enter Play mode and validate registration with a temporary log in any `MonoBehaviour.Start()`:

```csharp
// Temporary — remove after confirming.
void Start()
{
    UnityEngine.Debug.Log(
        "defaultQuestJournalUI: " +
        PixelCrushers.QuestMachine.QuestMachine.defaultQuestJournalUI);
}
```

- **Expected:** Console prints a non-null object reference.

## Verification

Before handing off to quest-authoring skills, confirm all items below:

- [ ] Scene contains **exactly one** `QuestMachineConfiguration` component.
- [ ] Player GameObject has a `Quest Journal (Script)` component.
- [ ] All four UI prefabs/components are present in the scene (or bundled inside the manager prefab).
- [ ] Play mode produces **zero** Quest Machine errors in the Console.
- [ ] `PixelCrushers.QuestMachine.QuestMachine.defaultQuestJournalUI` is non-null in Play mode.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `PixelCrushers.QuestMachine.Wrappers.QuestMachineConfiguration` | Component | Global config; registers default UIs on Awake; **one per scene** |
| `PixelCrushers.QuestMachine.Wrappers.QuestJournal` | Component | Tracks quests on the player; target of `GiveQuestToQuester` |
| `PixelCrushers.QuestMachine.Wrappers.UnityUIQuestJournalUI` | Component | Renders the quest journal panel |
| `PixelCrushers.QuestMachine.Wrappers.UnityUIQuestHUD` | Component | Renders tracked-quest HUD overlays |
| `PixelCrushers.QuestMachine.Wrappers.UnityUIQuestDialogueUI` | Component | Renders quest offer / turn-in dialogue |
| `PixelCrushers.QuestMachine.Wrappers.UnityUIQuestAlertUI` | Component | Shows brief quest-accepted / completed alerts |
| `AssetDatabase.LoadAssetAtPath<GameObject>` | Editor API | Load a prefab asset by project-relative path |
| `PrefabUtility.InstantiatePrefab` | Editor API | Instantiate a prefab while retaining its prefab link |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `defaultQuestJournalUI` is null in Play mode | UI prefab absent from scene, or UI loads after `QuestMachineConfiguration.Awake` | Use the bundled `Quest Machine.prefab` — it guarantees correct load order |
| "Duplicate manager" warning in Console | Two `QuestMachineConfiguration` instances in the scene | Delete one; the manager prefab already contains the configuration |
| `Quest Journal (Script)` missing from Add Component | Core class used instead of Wrapper subclass | Choose `Pixel Crushers/Quest Machine/Quest Journal`, not a namespace-qualified type |
| UI Canvas invisible in Play mode | Canvas Render Mode is World Space with no camera assigned | Set Render Mode to `Screen Space – Overlay`, or assign the Main Camera |

## Boundaries

This skill covers **scene scaffolding only**. It does NOT:

- Create player movement or character controllers — use your existing player setup or native Unity.
- Author quest assets or configure quest nodes — see **quest-machine-author-quests**.
- Configure NPC quest givers — see **quest-machine-quest-givers**.
- Call Quest Machine APIs at runtime — see **quest-machine-runtime-scripting**.
- Set up procedural quest generation — see **quest-machine-procedural-generation**.

Quest Machine's UI system is uGUI (Canvas-based); UI Toolkit (UITK) is not supported.
