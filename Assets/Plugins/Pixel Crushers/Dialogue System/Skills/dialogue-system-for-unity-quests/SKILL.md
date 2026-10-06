---
name: dialogue-system-for-unity-quests
description: "Use this skill whenever someone wants to create, track, or update quests with the Dialogue System for Unity — e.g. 'add a quest', 'make a fetch quest', 'mark the quest complete', 'track quest progress', 'show a quest log window', 'add a quest tracker hud', 'update quest state when the player does X', 'react when a quest is finished'. Covers authoring quests in the Dialogue Editor Items/Quests tab, the QuestLog API (GetQuestState/SetQuestState and quest entries), the QuestState enum, QuestStateListener/QuestStateIndicator components, and the StandardUIQuestLogWindow and StandardUIQuestTracker UI. Do NOT use for general conversation content (see dialogue-system-for-unity-authoring-conversations), non-quest runtime scripting (see dialogue-system-for-unity-runtime-scripting), or persisting quest state across saves (see dialogue-system-for-unity-save-system). When in doubt whether a quest request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Dialogue System for Unity — Quests

Author quests in the Dialogue Editor and manage their state, objectives, and UI at runtime using `QuestLog`, `QuestStateListener`, `StandardUIQuestLogWindow`, and `StandardUIQuestTracker`.

## When to use this skill

Use this skill when the task involves any of the following:
- Creating or editing quests in the Dialogue Editor (Items/Quests tab)
- Getting or setting quest state from C# (`QuestLog.GetQuestState`, `SetQuestState`)
- Working with quest objectives / entries (`GetQuestEntryState`, `SetQuestEntryState`)
- Reacting to quest state changes in a scene object (`QuestStateListener` component)
- Showing a full quest log to the player (`StandardUIQuestLogWindow`)
- Showing a tracker HUD for active quests (`StandardUIQuestTracker`)
- Reading or writing quest fields via `DialogueLua.GetQuestField` / `SetQuestField`
- Setting quest state from inside a conversation's Script field (Lua: `SetQuestState("name","active")`)

## Prerequisites

**Verify the asset is installed** before generating or editing any code:

```csharp
bool installed = false;
foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
{
    if (asm.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null)
    {
        installed = true;
        break;
    }
}
// Do NOT use Type.GetType("...") — the type lives in a separate assembly.
```

**Namespace rule:**
- C# code uses types in: `PixelCrushers.DialogueSystem`
- Scene components added via **Add Component** must use the wrapper versions in `PixelCrushers.DialogueSystem.Wrappers`

**Authoring location:** Quests are **Items** in the Dialogue Database where the **Is Item** checkbox is **unchecked**. Open the editor at `Tools > Pixel Crushers > Dialogue System > Dialogue Editor`, then select the **Items/Quests** tab.

**Required fields per quest:**
| Field | Notes |
|---|---|
| **Name** | Unique identifier used in all code calls |
| **Is Item** | Must be **unchecked** (false) — this marks it as a quest |
| **State** | Initial state string; typically `unassigned` |
| **Description** | Shown when the quest is active |
| **Success Description** | Optional; shown when state = success |
| **Failure Description** | Optional; shown when state = failure |
| **Entry Count** | Number of objective entries (0 = no entries) |
| **Entry 1**, **Entry 2**, … | Objective text for each entry |
| **Entry 1 State**, … | Initial state of each entry (typically `unassigned`) |

## Quick start

```csharp
using PixelCrushers.DialogueSystem;

// Activate a quest
QuestLog.SetQuestState("Kill 5 Rats", QuestState.Active);

// Check state
if (QuestLog.GetQuestState("Kill 5 Rats") == QuestState.Active)
    Debug.Log("Quest is active!");

// Complete it
QuestLog.SetQuestState("Kill 5 Rats", QuestState.Success);
```

## Workflows

### Workflow: Author a quest in the Dialogue Editor

**Goal:** Create a quest with description and objectives, ready to manage at runtime.

**Steps:**
1. Open `Tools > Pixel Crushers > Dialogue System > Dialogue Editor`.
2. Select the **Items/Quests** tab.
3. Click **+** to add a new item. Enter a unique **Name** (e.g., `Kill 5 Rats`).
4. **Uncheck Is Item** — this is the critical step that marks it as a quest.
5. Set **State** to `unassigned`.
6. Fill in **Description**, and optionally **Success Description** and **Failure Description**.
7. For objectives: set **Entry Count** to the number of objectives, then fill in **Entry 1**, **Entry 2**, … and their initial state fields (`unassigned`).
8. Save the database (Ctrl+S or the Save button in the Dialogue Editor toolbar).

**Expected result:** The quest exists in the Lua `Item` table and can be managed by `QuestLog.*` at runtime.

---

### Workflow: Get and set quest state at runtime

**Goal:** Progress a quest through its lifecycle — from `Unassigned` → `Active` → `Success` or `Failure`.

**Steps:**

```csharp
using PixelCrushers.DialogueSystem;

// QuestState is a [Flags] enum.
// Core values: Unassigned, Active, Success, Failure, Abandoned
// Extended (unused by DS internally): Grantable, ReturnToNPC, Done
QuestState state = QuestLog.GetQuestState("Kill 5 Rats");

// Explicit state transition
QuestLog.SetQuestState("Kill 5 Rats", QuestState.Active);
QuestLog.SetQuestState("Kill 5 Rats", QuestState.Success);
QuestLog.SetQuestState("Kill 5 Rats", QuestState.Failure);

// Convenience wrappers (same effect)
QuestLog.StartQuest("Kill 5 Rats");    // → Active
QuestLog.CompleteQuest("Kill 5 Rats"); // → Success
QuestLog.FailQuest("Kill 5 Rats");     // → Failure
QuestLog.AbandonQuest("Kill 5 Rats");  // → Abandoned

// Bool helpers
bool isActive   = QuestLog.IsQuestActive("Kill 5 Rats");
bool isSuccess  = QuestLog.IsQuestSuccessful("Kill 5 Rats");
bool isFailed   = QuestLog.IsQuestFailed("Kill 5 Rats");

// Get all active quests
string[] activeQuests = QuestLog.GetAllQuests(QuestState.Active);
```

> **Lua alternative (from a Dialogue Editor Script field):**
> `SetQuestState("Kill 5 Rats", "active")` — same effect as `QuestLog.SetQuestState`. State string values: `unassigned`, `active`, `success`, `failure`, `abandoned`.

**Expected result:** `GetQuestState` returns the new state; `QuestStateListener` components on scene objects react; quest tracker UI updates automatically.

---

### Workflow: Work with quest entries (objectives)

**Goal:** Track and update individual objectives within a multi-step quest.

**Steps:**

```csharp
using PixelCrushers.DialogueSystem;

// Get the number of entries defined for the quest
int count = QuestLog.GetQuestEntryCount("Kill 5 Rats");

// Get and set the state of a specific entry (1-based index)
QuestState entryState = QuestLog.GetQuestEntryState("Kill 5 Rats", 1);

if (entryState == QuestState.Active)
{
    QuestLog.SetQuestEntryState("Kill 5 Rats", 1, QuestState.Success);
}

// Log all entries
for (int i = 1; i <= count; i++)
{
    var es = QuestLog.GetQuestEntryState("Kill 5 Rats", i);
    Debug.Log($"Entry {i}: {es} — {QuestLog.GetQuestEntry("Kill 5 Rats", i)}");
}
```

> **Entry state is separate from quest state.** Setting all entries to `Success` does **not** automatically set the quest itself to `Success` — you must call `QuestLog.SetQuestState` explicitly.

**Expected result:** Quest tracker UI reflects the updated entry states; the main quest state is managed independently.

---

### Workflow: React to quest state changes with QuestStateListener

**Goal:** Make a scene object (e.g., NPC overhead indicator, a locked door, a waypoint marker) respond when a specific quest reaches a target state.

**Inspector setup (no code):**
1. Add the **Quest State Listener** component (Wrappers version: `PixelCrushers.DialogueSystem.Wrappers`) to the scene object.
2. Set **Quest Name** to the exact quest name string (e.g., `Kill 5 Rats`).
3. Under **Quest State Indicator Levels**, add one entry per state you want to listen for:
   - Set **Quest State** (e.g., `Active`).
   - Wire **On Enter State** to the UnityEvent target (e.g., activate an exclamation GameObject).
4. Optionally add **Quest State Indicator** to the same or a child object to control a numeric indicator level.

**Code-based subscription:**

```csharp
using PixelCrushers.DialogueSystem;

var dsEvents = DialogueManager.instance.GetComponent<DialogueSystemEvents>();
if (dsEvents != null)
{
    // onQuestStateChange fires whenever any quest state changes; string = quest title
    dsEvents.questEvents.onQuestStateChange.AddListener(OnAnyQuestChanged);
}

void OnAnyQuestChanged(string questTitle)
{
    if (questTitle == "Kill 5 Rats")
    {
        var state = QuestLog.GetQuestState(questTitle);
        Debug.Log($"'{questTitle}' is now: {state}");
    }
}
```

**Expected result:** The listener fires every time `QuestLog.SetQuestState` is called (including from inside conversations), allowing scene objects to update instantly.

---

### Workflow: Set up the Quest Log Window UI

**Goal:** Show the player a full UI listing all active and completed quests.

**Steps:**
1. Find and instantiate a `StandardUIQuestLogWindow` prefab from the Dialogue System samples, or add the **Standard UI Quest Log Window** component (Wrappers version) to a Canvas in your scene.
2. Open the window from code:

```csharp
using PixelCrushers.DialogueSystem;

var logWindow = FindObjectOfType<StandardUIQuestLogWindow>();
if (logWindow != null)
    logWindow.Open();
```

3. The window reads from `QuestLog.GetAllQuests(...)` automatically and displays descriptions based on the current quest state.

**Expected result:** Window shows active quests (with entry states) and completed quests (with success/failure descriptions).

---

### Workflow: Set up the Quest Tracker HUD

**Goal:** Display active quest objectives in the player's HUD during gameplay.

**Steps:**
1. Add the **Standard UI Quest Tracker** component (Wrappers version: `PixelCrushers.DialogueSystem.Wrappers`) to a Canvas in the scene.
2. Assign a **Quest Track Template** prefab to the component. The template contains Text components for the quest name and entry text.
3. The tracker subscribes to `OnQuestStateChange` and `UpdateTracker` messages automatically — no additional code required.
4. To force a tracker refresh from code: `QuestLog.SetQuestState("My Quest", QuestLog.GetQuestState("My Quest"))` (no-op that still fires the update) — or use any Dialogue Manager method that triggers `SendUpdateTracker`.

**Expected result:** The HUD updates whenever quest state or entry state changes, showing tracked active quests.

---

### Workflow: Add a timed countdown to a quest

**Goal:** Display a live countdown timer in the quest tracker and fail the quest when time expires.

**Steps:**
1. In the Dialogue Editor, add an **int Variable** to the database (e.g. `secondsLeft`).
2. Embed the timer display in the quest **Description** or an entry text field:
   `Time left: [lua(string.format("{0:0}:{1:00}", math.floor(Variable["secondsLeft"]/60), Variable["secondsLeft"] % 60))]`
3. Drive the countdown from a MonoBehaviour and refresh the HUD after each tick:
   ```csharp
   using PixelCrushers.DialogueSystem;

   IEnumerator Countdown()
   {
       while (DialogueLua.GetVariable("secondsLeft").AsInt > 0)
       {
           yield return new WaitForSeconds(1f);
           int val = DialogueLua.GetVariable("secondsLeft").AsInt - 1;
           DialogueLua.SetVariable("secondsLeft", val);
           DialogueManager.SendUpdateTracker(); // refreshes [lua(...)] text in tracker
       }
       QuestLog.FailQuest("My Timed Quest");
   }
   ```
4. Start the coroutine when the quest becomes active — for example, wire a `Quest State Listener` **On Enter State** UnityEvent for `Active` to call `StartCoroutine(Countdown())`.

**Expected result:** The quest tracker shows a live `M:SS` countdown; the quest automatically fails when the timer reaches zero.

---

### Workflow: Set up NPC overhead quest indicators

**Goal:** Display context-sensitive overhead icons above an NPC (e.g. `!` Grantable, `?` Active, `✓` Complete) that update automatically as quest state changes.

**Steps:**
1. Add two or three inactive child GameObjects to the NPC (e.g. `Indicator_Grantable`, `Indicator_Active`, `Indicator_Complete`) containing your icon sprites or world-space UI elements.
2. Add **Quest State Indicator** (`PixelCrushers.DialogueSystem.Wrappers`) to the NPC. Assign each indicator GameObject to a numbered slot (index 0, 1, 2…). Higher index = higher display precedence when multiple states are simultaneously true.
3. Add **Quest State Listener** (`PixelCrushers.DialogueSystem.Wrappers`) to the same NPC:
   - Set **Quest Name** to the exact quest name as it appears in the database.
   - Add one entry per state: choose **Quest State** (e.g. `Grantable`, `Active`, `Success`) and set **Indicator Level** to the matching index in the `Quest State Indicator` list.
4. Leave all indicator GameObjects **inactive** by default — `Quest State Indicator` activates only the one matching the current highest-priority state.

**Expected result:** The correct overhead icon activates automatically as the quest progresses through states, with no additional code required.

---

### Workflow: React to quest state changes with events, messages, and delegates

**Goal:** Execute C# logic (alerts, XP rewards, unlock events) whenever any quest changes state.

**Steps:**

**Option A — UnityEvent (Inspector, no code):** Add a `Dialogue System Events` component (wrapper) to the Dialogue Manager. Expand **Quest Events → On Quest State Change** and wire a listener. The string argument is the quest title.

**Option B — MonoBehaviour message handler** on the Dialogue Manager GameObject:

```csharp
using PixelCrushers.DialogueSystem;

void OnQuestStateChange(string questName)
{
    if (QuestLog.IsQuestActive(questName))
        DialogueManager.ShowAlert("New Quest: " + QuestLog.GetQuestTitle(questName));
    else if (QuestLog.IsQuestDone(questName))
        DialogueManager.ShowAlert("Completed: " + QuestLog.GetQuestTitle(questName));
}
```

**Option C — Delegate intercept** (intercepts all state changes including from Lua):

```csharp
QuestLog.SetQuestStateOverride = (name, stateStr) =>
{
    QuestLog.DefaultSetQuestState(name, stateStr);
    if (QuestLog.StringToState(stateStr) == QuestState.Success) AwardXP(name);
};
```

**Expected result:** Quest alerts and reward logic fire automatically, including when state is changed from Lua or a conversation Script field.

---

### Workflow: Manage quests without conversations

**Goal:** Grant and complete quests through gameplay events (kill counts, pickups) without starting a conversation.

**Steps:**
1. Add `IncrementOnDestroy` to each enemy or collectible. Set **Variable Name** (e.g., `enemiesKilled`) and **Increment Amount** to `1`. Wire its **On Destroy** UnityEvent to `DialogueSystemTrigger.OnUse` on a quest-manager GameObject.
2. On the quest-manager `DialogueSystemTrigger` (Trigger = **None**): add a Lua Condition (`Variable["enemiesKilled"] >= 5`), then set a **Quest** action to mark the quest `Success` and optionally an **Alert** action.
3. For quest assignment on scene start, add a second `DialogueSystemTrigger` (Trigger = **OnStart**) with condition `QuestLog.CurrentQuestState("My Quest") == "unassigned"` and action to set state to `Active`.
4. **Scene-unload caveat:** `IncrementOnDestroy` also fires when Unity destroys objects during scene unload. Always load scenes via `PixelCrushers.SaveSystem.LoadScene()` or the `LoadLevel()` sequencer command — these invoke `SaveSystem.BeforeSceneChange()` first, suppressing false increments during unload.

**Expected result:** Kill or pickup counts accumulate through gameplay; the quest transitions states automatically when the threshold is reached.

---

### Workflow: Sort, filter, and extend the Quest Log Window

**Goal:** Control quest display order, hide internal tracking quests, add a tracking toggle, and optionally merge active and completed quests into one list.

**Sorting and filtering (Inspector):**
- `StandardUIQuestLogWindow` sorts by **Name** and **Group** fields alphabetically; **Display Name** and **Group Display Name** are shown on screen. Prefix **Name** with e.g. `01_Main` to control sort order without affecting displayed text.
- Enable **Keep Groups Expanded** to lock foldouts open. Enable **Check Visible Field** and add a boolean `Visible` field to quests (default `true`) to suppress backend-only quests from the log.

**Tracking toggle via button (v2.2.51+):**
Add `QuestLogWindowHotkey` to a UI Button GameObject and wire its `onClick` to `QuestLogWindowHotkey.ToggleQuestLogWindow`.

**Show active and completed quests in one list (subclass):**

```csharp
using PixelCrushers.DialogueSystem;
public class MergedQuestLogWindow : StandardUIQuestLogWindow
{
    public override bool isShowingActiveQuests => true;
    public override void ShowQuests(QuestState s) =>
        base.ShowQuests(QuestState.Active | QuestState.ReturnToNPC |
                        QuestState.Success | QuestState.Failure | QuestState.Abandoned);
    protected override void OnQuestStateChange(string q)
    {
        if (!QuestLog.IsQuestActive(q))
            DialogueLua.SetQuestField(q, "Group", "Completed");
        base.OnQuestStateChange(q);
    }
}
```

Replace the component using Debug mode (drag the new script to the component's `m_Script` field) to keep all UI references intact.

**Expected result:** Quests appear in a predictable order; completed quests coexist with active ones in a single list; tracking is togglable via button.

---

### Workflow: Execute Lua code stored in quest fields

**Goal:** Author per-quest reward scripts directly in the database and run them automatically when quest state changes.

**Steps:**
1. In the Dialogue Editor **Templates** tab, add a `Text` field named `Reward Script` to the Quest template (tick **Main** to display it on every quest).
2. Fill in the `Reward Script` field on each quest with a Lua snippet, e.g. `Variable["Gold"] = Variable["Gold"] + 50`.
3. Add a MonoBehaviour to the Dialogue Manager that executes the stored Lua on success:

```csharp
using PixelCrushers.DialogueSystem;

void OnQuestStateChange(string questName)
{
    if (QuestLog.IsQuestSuccessful(questName))
    {
        string script = DialogueLua.GetQuestField(questName, "Reward Script").asString;
        if (!string.IsNullOrEmpty(script)) Lua.Run(script);
    }
}
```

**Expected result:** Each quest's reward logic fires on completion; adding new quests requires no C# changes.

## Verification

In Play mode:
1. Open **Variable Viewer** (`Tools > Pixel Crushers > Dialogue System > Tools > Variable Viewer`) — quest states live in the Lua `Item[questName].State` field.
2. Log `QuestLog.GetQuestState("My Quest")` to confirm the state is being read correctly.
3. If `QuestStateListener.OnChange` never fires, confirm the Dialogue Manager is in the scene (the `QuestStateDispatcher` component is auto-added to it when needed).
4. If the quest tracker doesn't update, confirm `StandardUIQuestTracker` is enabled and in the scene hierarchy.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `QuestLog.GetQuestState(name)` | `static QuestState` | Returns the current QuestState enum value |
| `QuestLog.SetQuestState(name, state)` | `static void` | Sets quest state and notifies all listeners |
| `QuestLog.StartQuest(name)` | `static void` | Shorthand: sets state to Active |
| `QuestLog.CompleteQuest(name)` | `static void` | Shorthand: sets state to Success |
| `QuestLog.FailQuest(name)` | `static void` | Shorthand: sets state to Failure |
| `QuestLog.AbandonQuest(name)` | `static void` | Shorthand: sets state to Abandoned |
| `QuestLog.IsQuestActive(name)` | `static bool` | True if state is Active |
| `QuestLog.IsQuestSuccessful(name)` | `static bool` | True if state is Success |
| `QuestLog.IsQuestFailed(name)` | `static bool` | True if state is Failure |
| `QuestLog.GetAllQuests(stateMask)` | `static string[]` | Returns names of quests in the given state(s) |
| `QuestLog.GetQuestEntryCount(name)` | `static int` | Number of objective entries for this quest |
| `QuestLog.GetQuestEntryState(name, entryNum)` | `static QuestState` | State of a specific objective entry (1-based) |
| `QuestLog.SetQuestEntryState(name, entryNum, state)` | `static void` | Sets the state of an objective entry |
| `QuestLog.GetQuestEntry(name, entryNum)` | `static string` | Localized text of an entry for its current state |
| `QuestLog.GetQuestDescription(name)` | `static string` | Description appropriate for the current quest state |
| `DialogueLua.GetQuestField(name, field)` | `static Lua.Result` | Raw Lua field from the Item/Quest table |
| `DialogueLua.SetQuestField(name, field, value)` | `static void` | Sets a raw Lua field on the quest |
| `QuestState` (Flags enum) | `enum` | Unassigned, Active, Success, Failure, Abandoned, Grantable, ReturnToNPC, Done |
| `Quest State Indicator` | `MonoBehaviour` (wrapper) | Manages indexed indicator GameObjects; activates the highest-priority slot matching current quest state |
| `DialogueManager.SendUpdateTracker()` | `static void` | Broadcasts a tracker-refresh message; call after `DialogueLua.SetVariable` changes that affect `[lua(...)]` in quest descriptions |
| `QuestLog.GetQuestTitle(name)` | `static string` | Returns the localized display name of the quest |
| `QuestLog.IsQuestDone(name)` | `static bool` | True when the quest is in Success, Failure, or Abandoned state |
| `QuestLog.SetQuestStateOverride` | `static delegate` | Assign a custom delegate to intercept all quest state changes; call `QuestLog.DefaultSetQuestState` inside it |
| `QuestLog.DefaultSetQuestState(name, stateStr)` | `static void` | Default state-change implementation; call from a custom `SetQuestStateOverride` |
| `IncrementOnDestroy` | `MonoBehaviour` | Increments a named Lua variable when the GameObject is destroyed; use with `DialogueSystemTrigger` for kill or pickup counting |
| `QuestLogWindowHotkey.ToggleQuestLogWindow()` | `public void` | Opens or closes the active quest log window; attach to a UI Button's `onClick` (v2.2.51+) |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `SetQuestState` logs "Quest doesn't exist" warning | Quest name mismatch or database not loaded | Confirm exact Name in Dialogue Editor Items/Quests tab; call `DialogueManager.PreloadMasterDatabase()` |
| `QuestStateListener.OnChange` never fires | `QuestStateDispatcher` missing | It auto-adds to the Dialogue Manager; confirm a Dialogue Manager is in the scene |
| Quest tracker doesn't update after `SetQuestState` | `StandardUIQuestTracker` disabled or not in scene | Ensure the tracker component is active and its Canvas is enabled |
| Quest log window shows no quests | All quests still `Unassigned` | Call `StartQuest` / `SetQuestState( ..., Active)` before opening the window |
| `GetQuestState` returns `Unassigned` when it should be active | `SetQuestState` inside a Lua Script field used string "active" but DB not updated | Correct behaviour — they both write to the same Lua table; refresh with `QuestLog.GetQuestState` after conversation ends |
| Entries appear but main quest state doesn't change | Entry state and quest state are independent | Call `QuestLog.SetQuestState` separately for the main quest |
| Quest tracker does not update when a `[lua(...)]` variable changes | `DialogueManager.SendUpdateTracker()` not called after the variable is set | Call `DialogueManager.SendUpdateTracker()` after each `DialogueLua.SetVariable(...)` that affects tracked quest text |
| Overhead quest indicator never changes | **Quest Name** in `Quest State Listener` does not match the database name exactly, or `QuestStateDispatcher` is missing | Confirm exact spelling; a Dialogue Manager in the scene auto-adds `QuestStateDispatcher` |
| Quest counter increments during scene unload | `IncrementOnDestroy` fires when Unity destroys GameObjects while unloading a scene | Load scenes via `PixelCrushers.SaveSystem.LoadScene()` or the `LoadLevel()` sequencer command so `SaveSystem.BeforeSceneChange()` suppresses increments during unload |
| Quest is visible in the log when it should be hidden | **Check Visible Field** not enabled, or the quest's `Visible` field is missing | Enable **Check Visible Field** on `StandardUIQuestLogWindow` and add a boolean `Visible` custom field to the Quest template; set it to `false` on tracking-only quests |

## Boundaries

This skill covers quest authoring, runtime state management, listener and indicator components, and quest UI setup.

- Saving and loading quest state across sessions → **dialogue-system-for-unity-save-system**
- Starting conversations that grant quests → **dialogue-system-for-unity-runtime-scripting**
- Authoring conversation trees and branching logic → **dialogue-system-for-unity-authoring-conversations**
- Localization of quest text → **dialogue-system-for-unity-localization**
