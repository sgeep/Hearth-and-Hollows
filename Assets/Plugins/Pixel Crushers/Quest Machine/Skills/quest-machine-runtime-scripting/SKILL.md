---
name: quest-machine-runtime-scripting
description: "Use this skill whenever you need to control Quest Machine at runtime from C# code — e.g., 'give the player a quest from code', 'check if the quest is complete', 'set the quest to successful', 'query a quest counter', 'advance an objective from gameplay code', 'react when quest state changes', or 'trigger quest logic from a collider'. Covers the QuestMachine static API, QuestJournal component, quest/node state read-write, MessageSystem-based objective advancement, QuestMachineMessageEvents for no-code Inspector wiring, and QuestControl for trigger/collision-based quest interactions. Do NOT use for hand-authoring quest graphs in the visual editor (see quest-machine-author-quests) or for scene object setup and NPC wiring (see quest-machine-scene-setup). When in doubt whether a C# runtime question touches Quest Machine, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Quest Machine — Runtime Scripting

Control Quest Machine entirely from C# at runtime: give quests to players, read and write quest and node states, advance objectives through the MessageSystem and counters, react to state changes with QuestMachineMessageEvents, and trigger quest interactions from colliders and UnityEvents using QuestControl. All runtime control flows through the static `PixelCrushers.QuestMachine.QuestMachine` class plus the MessageSystem from Pixel Crushers Common.

## When to use this skill

Use this skill for any task that drives Quest Machine from C# code during Play mode:

- Giving a quest programmatically (`QuestMachine.GiveQuestToQuester`)
- Reading or writing quest/node state (`GetQuestState`, `SetQuestState`, `GetQuestNodeState`, `SetQuestNodeState`)
- Advancing kill, collect, or custom objectives via `PixelCrushers.MessageSystem`
- Querying journals, quest instances, or counters at runtime
- Reacting to quest events in the Inspector (QuestMachineMessageEvents component)
- Triggering quest state changes from colliders or physics events (QuestControl component)

## Prerequisites

1. Quest Machine is imported from the Asset Store: `https://assetstore.unity.com/packages/tools/game-toolkits/quest-machine-39834`
2. Confirm installation: verify that `PixelCrushers.QuestMachine.QuestMachine` resolves in a C# script. If the type is missing, reimport the package and let the project recompile.
3. A **QuestMachineConfiguration** component (or a GameObject with the `QuestMachine` component) exists in the scene — it initialises the static API.
4. A **QuestJournal** component (`PixelCrushers.QuestMachine.Wrappers.QuestJournal`) is on the player GameObject with its **ID** field set (e.g., `"Player"`).

## Quick start

Give a quest from code and verify it was received:

```csharp
using PixelCrushers.QuestMachine;
using UnityEngine;

public class QuestStarter : MonoBehaviour
{
    [SerializeField] private string _questID   = "SlayOrcs";
    [SerializeField] private string _questerID = "Player";

    private void Start()
    {
        // Give the quest to the named quester's QuestJournal
        Quest instance = QuestMachine.GiveQuestToQuester(_questID, _questerID);
        if (instance != null)
        {
            QuestState state = QuestMachine.GetQuestState(_questID, _questerID);
            Debug.Log($"Quest '{instance.id}' is now {state}");
        }
    }
}
```

**Expected result:** Console prints `Quest 'SlayOrcs' is now Active`. The quest entry appears in the in-game journal UI.

## Workflows

### Workflow: Give a quest from code

1. Open the Quest Editor (**Tools > Pixel Crushers > Quest Machine > Quest Editor**) and note the exact string in the quest's **ID** field (case-sensitive).
2. In your MonoBehaviour, call:

```csharp
using PixelCrushers.QuestMachine;

// By string ID and quester ID
Quest given = QuestMachine.GiveQuestToQuester("SlayOrcs", "Player");

// By Quest asset reference
// Quest given = QuestMachine.GiveQuestToQuester(questAssetRef, "Player");

// By Quest asset reference and QuestJournal component reference
// Quest given = QuestMachine.GiveQuestToQuester(questAssetRef, questJournal);
```

3. **Expected result:** `given != null`; `QuestMachine.GetQuestState("SlayOrcs", "Player")` returns `QuestState.Active`.

---

### Workflow: Query and set quest / node state

```csharp
using PixelCrushers.QuestMachine;

// --- Quest state ---
QuestState state = QuestMachine.GetQuestState("SlayOrcs", "Player");

QuestMachine.SetQuestState("SlayOrcs", QuestState.Successful, "Player");
QuestMachine.SetQuestState("SlayOrcs", QuestState.Failed,     "Player");

// --- Node state ---
QuestNodeState nodeState = QuestMachine.GetQuestNodeState("SlayOrcs", "KillNode", "Player");

QuestMachine.SetQuestNodeState("SlayOrcs", "KillNode", QuestNodeState.Active, "Player");
QuestMachine.SetQuestNodeState("SlayOrcs", "KillNode", QuestNodeState.True,   "Player");
```

**QuestState values:** `WaitingToStart`, `Active`, `Successful`, `Failed`, `Abandoned`, `Disabled`  
**QuestNodeState values:** `Inactive`, `Active`, `True`

**Expected result:** Journal UI and HUD update immediately to reflect the new state.

---

### Workflow: Advance objectives via MessageSystem and counters

Quest objectives backed by `CounterQuestCondition` or `MessageQuestCondition` listen for MessageSystem messages. Send them from gameplay code:

```csharp
using PixelCrushers;
using PixelCrushers.QuestMachine;

// Increment a counter the quest tracks (e.g., each orc killed).
// Constant QuestMachineMessages.IncrementQuestCounterMessage == "Increment Quest Counter".
MessageSystem.SendMessage(this, "Player",
    QuestMachineMessages.IncrementQuestCounterMessage, "OrcKillCount", 1);

// Or use the convenience helper (sender, questID, counterName, amount):
QuestMachineMessages.IncrementQuestCounter(this, "KillOrcs", "OrcKillCount", 1);

// Set a counter to an absolute value.
// Constant QuestMachineMessages.SetQuestCounterMessage == "Set Quest Counter".
MessageSystem.SendMessage(this, "Player",
    QuestMachineMessages.SetQuestCounterMessage, "OrcKillCount", 5);

// Send a custom message that a MessageQuestCondition is listening for
MessageSystem.SendMessage(this, "Player", "BossDefeated", string.Empty);
```

> **Note:** The target parameter (`"Player"`) must match the quester ID on the QuestJournal. The counter name must match exactly what is set in the quest's Counter asset.

**Expected result:** The quest counter increments; when the threshold is reached the condition node flips to `True` and the quest advances automatically.

---

### Workflow: React to quest changes with QuestMachineMessageEvents

**No-code (Inspector) approach:**

1. Add **Component > Pixel Crushers > Quest Machine > Quest Machine Message Events** to any GameObject.
2. Set **Quest ID** (and optionally **Quester ID**) in the Inspector.
3. Wire **On Quest State Changed** to any method — e.g., enable a reward panel.

**C# listener approach:**

```csharp
using PixelCrushers;
using PixelCrushers.QuestMachine;
using UnityEngine;

public class QuestStateListener : MonoBehaviour
{
    private void OnEnable()
    {
        MessageSystem.AddListener(this, OnQuestStateChanged,
            QuestMachineMessages.QuestStateChangedMessage);
    }

    private void OnDisable()
    {
        MessageSystem.RemoveListener(this,
            QuestMachineMessages.QuestStateChangedMessage);
    }

    private void OnQuestStateChanged(MessageArgs args)
    {
        // args.parameter = questID  |  args.intValue = (int)QuestState
        Debug.Log($"Quest '{args.parameter}' changed to {(QuestState)args.intValue}");
    }
}
```

**Expected result:** The callback fires whenever any quest state changes; `args.parameter` contains the quest ID.

---

### Workflow: Trigger quest interactions with QuestControl

`QuestControl` drives quest state from colliders, triggers, or UnityEvents without custom MonoBehaviours.

1. Add **Component > Pixel Crushers > Quest Machine > Quest Control** to a pickup, door, or zone GameObject.
2. In the Inspector, set **Quest ID** and **Quest Node ID**.
3. Wire a trigger or UnityEvent (e.g., **On Trigger Enter**) to **QuestControl.SetQuestNodeState** or **QuestControl.SendToMessageSystem**.
4. Set the target state in the component's fields.

**Expected result:** When the player enters the trigger zone, the specified quest node flips state and the journal UI updates immediately.

## Verification

| Check | Method | Expected result |
|---|---|---|
| Quest given | `QuestMachine.GetQuestState(id, "Player")` in Console | Returns `Active` |
| Quest in journal | Observe in-game journal UI at runtime | Quest entry visible |
| Counter advancing | Log counter; kill an enemy | Value increments |
| State-changed listener | Add `Debug.Log` in `OnQuestStateChanged` | Fires on state change |
| Console | **Window > General > Console** | No errors or exceptions |

## API quick reference

```csharp
// Namespace: PixelCrushers.QuestMachine
static class QuestMachine
{
    // --- Lookup ---
    static QuestJournal GetQuestJournal(string id = "");
    static Quest        GetQuestAsset(string id);
    static Quest        GetQuestInstance(string questID, string questerID = "");
    static QuestCounter GetQuestCounter(string questID, string counterName, string questerID = null);

    // --- Give ---
    static Quest GiveQuest(string questID);
    static Quest GiveQuestToQuester(string questID, string questerID);
    static Quest GiveQuestToQuester(Quest quest, string questerID);
    static Quest GiveQuestToQuester(Quest quest, QuestJournal questJournal);

    // --- State ---
    static QuestState     GetQuestState(string questID, string questerID = null);
    static void           SetQuestState(string questID, QuestState state, string questerID = null);
    static QuestNodeState GetQuestNodeState(string questID, string questNodeID, string questerID = null);
    static void           SetQuestNodeState(string questID, string questNodeID, QuestNodeState state, string questerID = null);

    // --- Debug ---
    static bool debug         { get; set; }
    static bool isLoadingGame { get; set; }
}

// Enums
enum QuestState     { WaitingToStart, Active, Successful, Failed, Abandoned, Disabled }
enum QuestNodeState { Inactive, Active, True }

// MessageSystem constants (PixelCrushers.QuestMachine.QuestMachineMessages)
// "Quest State Changed"      — broadcast when any quest state changes
// "Increment Quest Counter"  — increment a named counter on the quester
// "Set Quest Counter"        — set a named counter to an absolute value
// "Quest Alert"              — trigger a quest alert notification
// "Refresh UIs"              — force all quest UIs to redraw
```

## Common issues

**`GiveQuestToQuester` returns `null`:** The quest asset ID is case-sensitive. Open the Quest Editor, select the quest, and copy the **ID** field verbatim.

**QuestJournal not found:** Ensure the player GameObject has a `QuestJournal` component and its **ID** field exactly matches the `questerID` passed to the API.

**Counter never advances:** The counter name in `SendMessage` must match exactly the counter name defined inside the quest asset. Enable `QuestMachine.debug = true` to trace message traffic in the Console.

**State-changed event fires multiple times:** `SetQuestState` is synchronous and may cascade through node logic. Avoid calling `SetQuestState` inside a listener for the same quest to prevent re-entrancy loops.

**Multi-quester games:** Omitting `questerID` defaults to the first registered QuestJournal. Always pass an explicit `questerID` when multiple questers are active in the scene.

## Boundaries

- **Quest graph authoring** (nodes, conditions, reward systems, branching) → **quest-machine-author-quests**
- **Scene object setup** (QuestGiver NPC wiring, QuestJournal on player, UI component setup) → **quest-machine-scene-setup**
- **Procedural quest generation** (EntityType, Drive, Action ScriptableObjects, QuestGenerator) → **quest-machine-procedural-generation**
- **Initial project install and package overview** → **quest-machine-setup-and-overview**
- **Quest Giver NPC dialogue configuration** → **quest-machine-quest-givers**
