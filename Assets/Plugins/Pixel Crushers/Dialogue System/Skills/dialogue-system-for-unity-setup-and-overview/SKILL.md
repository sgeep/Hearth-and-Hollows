---
name: "dialogue-system-for-unity-setup-and-overview"
description: "Use this skill whenever someone wants to install, set up, verify, or get started with the Dialogue System for Unity — e.g. 'how do I set up the dialogue system', 'where is the dialogue manager wizard', 'add the dialogue manager to my scene', 'how do I enable textmesh pro support', 'wheres the demo scene', 'what scripting defines do I need for 2D'. Covers install verification, Welcome Window, Dialogue Manager Wizard, USE_PHYSICS2D and TMP_PRESENT defines, key file locations, demo import, and an index of all sibling skills. Do NOT use for building conversations (see dialogue-system-for-unity-authoring-conversations), scene NPC wiring (see dialogue-system-for-unity-scene-interaction), runtime code (see dialogue-system-for-unity-runtime-scripting), or quests, save/load, localization and imports (see the matching dialogue-system-for-unity-* skills). When in doubt whether a dialogue request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Dialogue System for Unity"
  publisher: "Pixel Crushers"
  asset-version: "2.2.73.2"
  skill-version: "1.2.0"
  unity: "2022.3+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/behavior-ai"
  asset-store-url: "https://assetstore.unity.com/packages/tools/behavior-ai/dialogue-system-for-unity-11672"
  documentation-url: "https://pixelcrushers.com/dialogue_system/manual2x/html/"
  support-url: "https://www.pixelcrushers.com/support/"
  last-verified: "2026-09-01"
---

# Dialogue System for Unity — Setup & Overview

The Dialogue System for Unity (Pixel Crushers, v2.2.73.2) is a comprehensive AAA-quality dialogue, conversation, quest, and save system. It centres on a node-based **Dialogue Editor**, a **DialogueDatabase** ScriptableObject, a singleton **Dialogue Manager** GameObject, and a **Sequencer** for camera and audio events. This skill covers first-time install verification, the Welcome Window, creating the Dialogue Manager, configuring scripting defines, locating key files, running the demo, and an index of all sibling skills.

## When to use this skill

- "how do I install the dialogue system"
- "where is the dialogue manager wizard"
- "how do I add the dialogue manager to my scene"
- "nothing shows up when my npc talks — is the system set up?"
- "what scripting symbols do I need for 2D physics triggers"
- "how do I enable textmesh pro support"
- "where's the welcome window"
- "how do I run the demo"
- "what version is installed"
- "where do the dialogue system files live"

**Not for:**
- Building conversation content → `dialogue-system-for-unity-authoring-conversations`
- Wiring NPCs and triggers in scene → `dialogue-system-for-unity-scene-interaction`
- C# runtime API → `dialogue-system-for-unity-runtime-scripting`
- Quest state management → `dialogue-system-for-unity-quests`
- Save/load → `dialogue-system-for-unity-save-system`
- Localization → `dialogue-system-for-unity-localization`
- Importing from Articy, Arcweave, etc. → `dialogue-system-for-unity-import`

## Prerequisites

- Unity 2022.3 LTS or newer
- Dialogue System for Unity 2.2.73.2 imported from the Asset Store
- All render pipelines are supported; the demo defaults to URP — import the matching `.unitypackage` from `Assets/Plugins/Pixel Crushers/Dialogue System/Demo/` for BiRP or HDRP

**Programmatic install check:**

```csharp
// Run in an Editor script or via RunCommand to confirm the asset is present.
bool IsDialogueSystemInstalled()
{
    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
    {
        if (asm.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null)
            return true;
    }
    return false;
}
```

If this returns `false`: the asset is not imported or has not compiled. Import from the Asset Store:
<https://assetstore.unity.com/packages/tools/behavior-ai/dialogue-system-for-unity-11672>

## Quick start

**Task: Add the Dialogue Manager to a scene.**

1. Open the target scene.
2. Choose **Tools > Pixel Crushers > Dialogue System > Wizards > Dialogue Manager Wizard**.
3. Follow the wizard: select a UI prefab (Standard UI is recommended), then click **Create Dialogue Manager**.
4. A `Dialogue Manager` GameObject appears in the hierarchy containing a `DialogueSystemController` component and a linked `StandardDialogueUI` prefab.
5. Press **Play** — the Console should show no errors and `DialogueManager.hasInstance` evaluates to `true`.

## Workflows

### Workflow: Open Welcome Window and verify version

**Goal:** Confirm install and access quick links.

**Steps:**
1. Choose **Tools > Pixel Crushers > Dialogue System > Welcome Window**.
2. The Welcome Window opens, displaying version number, quick links to the manual, video tutorials, and FAQ, plus scripting symbol checkboxes.
3. Confirm the version shown matches `Assets/Plugins/Pixel Crushers/Dialogue System/_README.txt` (2.2.73.2).

**Expected result:** Welcome Window is open; version reads 2.2.73.2; no Console errors.

---

### Workflow: Enable scripting define symbols

**Goal:** Enable 2D physics support (`USE_PHYSICS2D`) and/or TextMesh Pro support (`TMP_PRESENT`).

**Steps:**
1. Open **Tools > Pixel Crushers > Dialogue System > Welcome Window**.
2. Under **Scripting Symbols**:
   - Tick **USE_PHYSICS2D** to enable 2D collider and trigger events on `DialogueSystemTrigger`, `Selector`, and `ProximitySelector`. Required for all 2D projects using physics-based interaction.
   - Tick **TMP_PRESENT** only if you are on a Unity version older than Unity 6 and want TextMesh Pro support. (Unity 6+ bundles TMP; the define is not required.)
3. Click **Apply**. Unity recompiles the project.
4. Alternatively, for TMP only: **Tools > Pixel Crushers > Dialogue System > Tools > Enable TextMesh Pro Support...**.

**Expected result:** **Edit > Project Settings > Player > Scripting Define Symbols** contains the applied symbols. Scripts guarded by `#if USE_PHYSICS2D` or `#if TMP_PRESENT` now compile.

---

### Workflow: Import and run the demo scene

**Goal:** Verify a working end-to-end dialogue flow.

**Steps:**
1. Open `Assets/Plugins/Pixel Crushers/Dialogue System/Demo/Scenes/DemoScene1.unity`.
2. If using Built-in RP or HDRP, first import the matching `.unitypackage` from the `Demo/` folder.
3. Press **Play**.
4. Move the player character near the NPC and press the use key (default: **F**) to start a conversation.

**Expected result:** A conversation UI panel appears with NPC dialogue text and player response buttons. No Console errors.

---

### Workflow: Set up the New Input System

**Goal:** Use Unity's Input System package for Selector / ProximitySelector use-button input.

**Steps:**
1. Install the **Input System** package via the Package Manager.
2. Open **Tools > Pixel Crushers > Dialogue System > Welcome Window**, tick **USE_NEW_INPUT**, and click **Apply**. Unity recompiles.
3. On your Input Actions asset, enable **Generate C# Class** and click **Apply** to regenerate the wrapper class.
4. In a script on the Dialogue Manager, register each action in `OnEnable` and unregister in `OnDisable`:

   ```csharp
   void OnEnable()
   {
       InputDeviceManager.RegisterInputAction("Interact", controls.Gameplay.Interact);
   }
   void OnDisable()
   {
       InputDeviceManager.UnregisterInputAction("Interact");
   }
   ```

5. On the **Selector** or **ProximitySelector** component, set **Use Button** to the registered action name (e.g., `Interact`). Plain key names (`F`, `Esc`) translate automatically.

**Expected result:** Use-button input fires under the Input System package; no "button not found" Console warnings.

---

### Workflow: Work around missing macOS Tool menu items

**Goal:** Access Dialogue System tool windows hidden by Unity macOS bug IN-70913 (deep submenus under **Tools > Pixel Crushers > Dialogue System > Tools** are invisible on macOS).

**Steps:**
1. Create `Assets/Editor/ShortDSMenuItems.cs` with shallow `[MenuItem]` paths:

   ```csharp
   using UnityEditor;
   using PixelCrushers.DialogueSystem;
   public static class ShortDSMenuItems
   {
       [MenuItem("Tools/Pixel Crushers/Dialogue System/Variable Viewer")]
       static void OpenVariableViewer() => VariableViewWindow.OpenVariableViewWindow();
       [MenuItem("Tools/Pixel Crushers/Dialogue System/Asset Renamer")]
       static void OpenAssetRenamer() => DialogueSystemAssetRenamerWindow.Open();
       [MenuItem("Tools/Pixel Crushers/Dialogue System/Localization Tools")]
       static void OpenLocalizationTools() => LocalizationToolsWindow.Init();
       [MenuItem("Tools/Pixel Crushers/Dialogue System/Unique ID Tool")]
       static void OpenUniqueIDTool() => UniqueIDWindow.OpenUniqueIDWindow();
   }
   ```

2. Unity recompiles. The new items appear under **Tools > Pixel Crushers > Dialogue System** and remain visible on macOS.

**Expected result:** All tool windows accessible from the shortened menu paths; no invisible submenu items.

---

### Workflow: Reduce dialogue database size and memory

**Goal:** Lower database asset size and runtime memory when Include SimStatus, unused fields, or multiple languages inflate the database.

**Steps:**
1. **Disable Include SimStatus** — on the Dialogue Manager, uncheck **Other Settings > Include SimStatus** if you do not track which entries have been offered or spoken. *Note:* SimStatus is required when **Display Settings > Input Settings > Em Tag For Old Responses** is enabled.
2. **Remove unused fields** — in the Dialogue Editor, open the **Templates** tab, choose **Menu > Update Template From Assets**, delete unused field rows selecting **Remove All** when prompted. Optionally run **Menu > Remove Empty Fields** (back up first).
3. **Split multilingual databases** — load per-language databases at runtime instead of one monolithic file:

   ```csharp
   DialogueManager.RemoveDatabase(previousLanguageDb);
   DialogueManager.AddDatabase(newLanguageDb);
   ```

**Expected result:** Measurably smaller database asset and lower runtime memory; conversation logic is unchanged.

---

### Workflow: Set up split-screen local multiplayer

**Goal:** Run simultaneous independent conversations for multiple local players in a split-screen layout.

**Steps:**
1. For each player prefab, add: a dedicated `Camera` (with its `AudioListener` disabled), a child `Canvas` (Screen Space – Camera, targeting that camera), an `OverrideDialogueUI` referencing a per-player dialogue UI prefab, a `PlayerInput` component, and a `MultiplayerEventSystem` (Unity Input System package) whose `InputSystemUIInputModule` references the Canvas as **Player Root**.
2. On the Dialogue Manager, enable **Other Settings > Allow Simultaneous Conversations** and **Input Device Manager > Always Auto Focus**. Untick **Add EventSystem If Needed** on all globally instantiated UI prefabs.
3. On each subtitle and response panel, attach a script that sets `GetComponent<UIPanel>().eventSystem = multiplayerEventSystem` in `Awake()` to route UI navigation to the correct player.

**Expected result:** Each player opens and drives their own conversation UI simultaneously, navigated by their own input device.

---

### Workflow: Integrate Unity Starter Assets First/Third Person controller

**Goal:** Disable the Starter Assets character controller during conversations, then restore it on close.

**Steps:**
1. Enable `USE_NEW_INPUT` via **Tools > Pixel Crushers > Dialogue System > Welcome Window**.
2. Add a **Dialogue Actor** component (set to `Player`) to the character GameObject.
3. Add a **Dialogue System Events** component to the character. Wire **OnConversationStart** to: `FirstPersonController.enabled = false` (or `ThirdPersonController.enabled = false`), `PlayerInput.DeactivateInput()`, and disabling any `Selector`/`ProximitySelector` and the Main Camera HeadBob script if present.
4. Wire **OnConversationEnd** to re-enable all of the above.
5. On NPC `DialogueSystemTrigger` components, tick **Show Cursor During Conversation**.

**Expected result:** Movement and camera controls are disabled during conversations and restored immediately after.

---

### Workflow: Use group nodes to reduce condition-check time

**Goal:** Short-circuit evaluation of expensive branch conditions using a parent group node.

**Steps:**
1. In the Dialogue Editor, select the entry that gates a set of conditional branches.
2. In its node inspector, tick the **Group** checkbox. The node becomes a pass-through: if its own Condition evaluates to `false`, all child branches are skipped without being individually tested.
3. Nest related sub-branches under their own group nodes to build a shallow, prunable evaluation tree.

**Expected result:** Conversations with many conditional branches evaluate faster; entire sub-trees are bypassed when the parent group condition fails.

---

### Workflow: Accept spoken player responses via voice recognition

**Goal:** Let players speak responses instead of clicking buttons, using Whisper, Wit.ai, Windows Speech Recognition, Google Cloud Speech, or Recognissimo.

**Steps:**
1. Subclass `StandardDialogueUI` (e.g. `VoiceDialogueUI`) and override `ShowResponses(Subtitle subtitle, Response[] responses, float timeout)`.
2. Cache the `responses` array and start the voice SDK's listening session.
3. In the SDK's transcript callback, find the response whose `formattedText.menuText` best matches the spoken text and call `OnClick(matchingResponse)` to select it.
4. Assign the `VoiceDialogueUI` prefab to the Dialogue Manager's **Dialogue UI** field.

**Expected result:** The conversation's response menu waits for spoken input; the closest matching branch is selected automatically when speech is recognized.

## Verification

- `IsDialogueSystemInstalled()` returns `true`
- Welcome Window opens without error and shows version **2.2.73.2**
- Scene contains a `Dialogue Manager` GameObject with a `DialogueSystemController` component (wrapper type `PixelCrushers.DialogueSystem.Wrappers.DialogueSystemController`)
- `DialogueManager.hasInstance` is `true` at runtime
- After applying defines, no new compile errors appear in the Console

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `DialogueManager.hasInstance` | `static bool` | `true` when a `DialogueSystemController` is active in the scene |
| `DialogueManager.instance` | `static DialogueSystemController` | The singleton controller |
| `DialogueManager.masterDatabase` | `static DialogueDatabase` | The currently loaded dialogue database |
| `DialogueManager.isConversationActive` | `static bool` | `true` while a full conversation is playing |
| `DialogueManager.debugLevel` | `static DialogueDebug.DebugLevel` | Set to `Info` to trace system events to Console |
| `USE_NEW_INPUT` | Welcome Window checkbox | Enables Input System package support; required before `InputDeviceManager.RegisterInputAction` has any effect |
| `InputDeviceManager.RegisterInputAction(name, action)` | `static void` | Maps a Unity Input System `InputAction` to a named button used by Selector and ProximitySelector |
| `Include SimStatus` | Dialogue Manager > Other Settings | When unchecked, omits per-entry SimStatus tracking to reduce memory; required when Em Tag For Old Responses is enabled |
| `Tools > … > Tools > Run 1x to 2x Updater` | Editor menu | Converts legacy 1.x conversation content and scripting calls to the 2.x API |
| `Allow Simultaneous Conversations` | Dialogue Manager > Other Settings | Enables two or more conversations to run at the same time; required for split-screen local multiplayer |
| `MultiplayerEventSystem` | Input System package component | Per-player `EventSystem` for split-screen; assign the player's `Canvas` as **Player Root** and link to a per-player `InputSystemUIInputModule` |
| `Warm Up Conversation Controller` | Dialogue Manager > Other Settings | Set to `On` or `Extra` to pre-initialise the conversation controller before the first conversation, eliminating startup hitches |
| `Group` checkbox | Dialogue Editor node inspector | Marks a dialogue entry as a pass-through group node; if the group's own Condition is false, all child branches are skipped without individual evaluation |

All types live in namespace `PixelCrushers.DialogueSystem`. In scenes and prefabs, add **wrapper** components(namespace `PixelCrushers.DialogueSystem.Wrappers`) so that references survive assembly changes.

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `DialogueManager.hasInstance` is `false` at runtime | No `Dialogue Manager` GameObject in scene | Run **Wizards > Dialogue Manager Wizard** or manually add `Add Component > Pixel Crushers/Dialogue System/Misc/Dialogue System Controller` |
| 2D physics triggers on `DialogueSystemTrigger` never fire | `USE_PHYSICS2D` define is missing | Tick **USE_PHYSICS2D** in Welcome Window → Apply |
| TMP text blank in dialogue UI | `TMP_PRESENT` define missing (pre-Unity 6) | Tick in Welcome Window → Apply, or use **Tools > Enable TextMesh Pro Support...** |
| Demo scene renders pink or unlit | Wrong render pipeline | Import the BiRP or HDRP `.unitypackage` from `Demo/` |
| Install check returns `false` after import | Assembly not compiled yet | Close and reopen Unity; resolve any Console compile errors first |
| Package Manager imports a stale version after update | Asset Store cache holds an old `.unitypackage` | Delete the cached file from `%APPDATA%\Unity\Asset Store 5.x` (Windows) or `~/Library/Unity/Asset Store-5.x` (macOS), restart Unity, and redownload; verify the imported version in `Assets/Plugins/Pixel Crushers/Dialogue System/_README.txt` |
| A dialogue line, condition, or sequencer command fails silently | `Debug Level` is `Warning` or `Error` | Set **Dialogue Manager > Other Settings > Debug Level** to `Info` to trace node transitions, condition evaluations, actor bindings, and sequencer commands to the Console |
| Multiple `EventSystem` warnings at runtime | `Add EventSystem If Needed` is ticked on `StandardDialogueUI`, `StandardUIQuestLogWindow`, or prefabs auto-spawned via Dialogue Manager **Instantiate Prefabs** | Untick **Add EventSystem If Needed** on `StandardDialogueUI` and `StandardUIQuestLogWindow`; also check every prefab in the **Instantiate Prefabs** list |
| Dialogue UI ignores mouse clicks | Missing input module, missing `GraphicRaycaster`, or invisible UI intercepting raycasts | Verify the scene `EventSystem` has an active input module (`StandaloneInputModule` or `InputSystemUIInputModule`); add a `GraphicRaycaster` to the Dialogue Manager Canvas; inspect the `EventSystem` in Play Mode to identify blocking UI elements |
| Wrong typewriter scroll speed, missing autoscroll, or unwanted player portrait in dialogue UI | Mismatched Characters Per Second, disabled Auto Scroll Settings, or portrait fields not cleared | Match CPS on `TextMeshProTypewriterEffect` / `UnityUITypewriterEffect`; configure **Auto Scroll Settings** on `StandardUISubtitlePanel`; toggle **Only Show NPC Portrait** |
| Dialogue System tool windows hidden on macOS (Variable Viewer, Localization Tools, etc.) | Unity bug IN-70913 hides deep **Tools > Pixel Crushers > Dialogue System > Tools** submenus | Add an Editor script with shallow `[MenuItem]` paths — see **Workflow: Work around missing macOS Tool menu items** |
| Input actions do not respond under the Input System package | `USE_NEW_INPUT` not set or `RegisterInputAction` not called | Tick **USE_NEW_INPUT** in the Welcome Window, generate the C# class for your Input Actions asset, and call `InputDeviceManager.RegisterInputAction` in `OnEnable` — see **Workflow: Set up the New Input System** |
| Database asset or runtime memory is excessively large | Include SimStatus, unused custom fields, or all languages packed into one database | Uncheck **Include SimStatus** (if Em Tag For Old Responses is off); prune unused fields via the Templates tab **Menu > Remove Empty Fields**; split per-language databases and swap them at runtime — see **Workflow: Reduce dialogue database size and memory** |
| Player falls through the floor in `DemoScene1` | Project Physics settings have disabled collision on the `Default` layer | Open **Edit > Project Settings > Physics** and re-enable collision for the `Default` layer in the Layer Collision Matrix, or reassign `Room > Plane` and `Player` to layers configured to collide |
| Actor dropdowns are unwieldy with large actor rosters | Actors are listed flat with no grouping | Insert forward slashes in the actor's **Name** field (e.g. `NPCs/Merchants/Bob`) to create Inspector submenu groups; populate **Display Name** (e.g. `Bob`) to keep subtitle text clean |
| Noticeable framerate hitch when the first conversation opens (`Font.CacheFontForText` visible in profiler) | Legacy `UnityEngine.UI.Text` components rasterize font glyphs on first dialogue render | Migrate subtitle panel `Text` components to `TextMeshProUGUI`; set Dialogue Manager **Other Settings > Warm Up Conversation Controller** to `On` or `Extra` |

## Boundaries

- Does not cover conversation content authoring — see `dialogue-system-for-unity-authoring-conversations`.
- Does not cover scene wiring of NPCs and triggers — see `dialogue-system-for-unity-scene-interaction`.
- The Dialogue System does **not** auto-instantiate a Dialogue Manager at runtime; one must exist in the scene or be loaded via prefab.
- Render-pipeline-specific post-processing and shader issues are outside this skill's scope.

## Sibling skill index

| Skill | Purpose |
|---|---|
| `dialogue-system-for-unity-setup-and-overview` | ← **this skill**: install, defines, Dialogue Manager |
| `dialogue-system-for-unity-authoring-conversations` | Create databases, actors, conversation trees in the Dialogue Editor |
| `dialogue-system-for-unity-scene-interaction` | Wire NPCs, triggers, selectors, barks in scene |
| `dialogue-system-for-unity-runtime-scripting` | C# API: StartConversation, Lua, events |
| `dialogue-system-for-unity-quests` | Quest states, QuestLog, QuestLogWindow |
| `dialogue-system-for-unity-save-system` | Save/load dialogue variables and quest states |
| `dialogue-system-for-unity-localization` | Multi-language text table setup |
| `dialogue-system-for-unity-import` | Import from Articy, Arcweave, CSV, Yarn |
