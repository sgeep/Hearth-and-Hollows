---
name: "quest-machine-author-quests"
description: "Use this skill whenever you need to create or configure a Quest asset — e.g., 'make a fetch quest', 'add a kill 10 wolves quest', 'collect 5 herbs quest', 'add a counter to a quest', 'set up quest objectives', 'my quest never completes'. Covers creating Quest assets via Assets > Create > Pixel Crushers > Quest Machine > Quest, the Quest Editor node graph, Counters, wiring Condition/Success/Failure nodes with QuestCondition and QuestAction subassets, and Journal/HUD/Alert QuestContent. The full subasset catalog lives in references/subassets.md. Do NOT use for attaching quest givers to NPCs (see quest-machine-quest-givers), procedural generation (see quest-machine-procedural-generation), runtime scripting (see quest-machine-runtime-scripting), or scene wiring (see quest-machine-scene-setup). When in doubt about a quest-authoring question, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Quest Machine – Authoring Quests

Quest Machine stores each quest as a `Quest` ScriptableObject asset edited in the Quest Editor (`Tools > Pixel Crushers > Quest Machine > Quest Editor`). The editor presents a node-based graph where you wire Start, Condition, Passthrough, Success, and Failure nodes, attach **QuestCondition** subassets to Condition nodes, and attach **QuestAction** and **QuestContent** subassets to state slots on any node. Counters defined on the quest feed `CounterQuestCondition` nodes and are incremented at runtime via `MessageSystem` messages sent from gameplay code.

## When to use this skill

Use this skill when you need to:

- Create a new Quest asset (fetch, collect, kill, escort, timed, talk-to-NPC, branching, etc.)
- Add Counters and wire `CounterQuestCondition` or `MessageQuestCondition` Condition nodes
- Add `QuestContent` for the Journal, HUD, Alert, or Dialogue UIs
- Add `QuestAction` subassets for rewards or side-effects on node state transitions
- Debug a quest that stays `WaitingToStart` or never transitions to `Successful`
- Script Quest asset creation programmatically via `AssetDatabase.CreateAsset`

For the full subasset catalog (all condition/action/content types and their serialized fields), read **`references/subassets.md`**.

## Prerequisites

Confirm Quest Machine is installed — choose one method:

**Method 1 — menu check:** open `Tools > Pixel Crushers > Quest Machine > Quest Editor`. If the menu is absent, install the package from the Asset Store URL in the frontmatter.

**Method 2 — code check** (run as a RunCommand):

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var t = System.Type.GetType(
            "PixelCrushers.QuestMachine.Quest, PixelCrushers.QuestMachine");
        result.Log(t != null
            ? "Quest Machine is installed."
            : "Quest Machine NOT found — install from the Asset Store.");
    }
}
```

*Expected: log prints "Quest Machine is installed."*

**Scene requirement:** the scene must contain a `QuestMachineConfiguration` object. Add it via `Tools > Pixel Crushers > Quest Machine > Quest Machine Configuration` if it is absent.

## Quick start

**Goal:** create a "Collect 5 Herbs" quest from scratch in the Quest Editor.

1. **Create the Quest asset**
   `Assets > Create > Pixel Crushers > Quest Machine > Quest` → name the file `CollectHerbs`.
   *Expected: `CollectHerbs.asset` appears in the Project window with a Quest icon.*

2. **Open the Quest Editor**
   `Tools > Pixel Crushers > Quest Machine > Quest Editor`, then click `CollectHerbs.asset` in the Project window.
   *Expected: the graph area shows a single Start node; the left panel shows quest identity fields.*

3. **Set quest identity**
   In the left panel set **ID** = `collectHerbs`, **Title** = `Collect 5 Herbs`.
   *Expected: the title updates in the Quest Editor header.*

4. **Add a Counter**
   Left panel → **Counters** → **+**. Set name = `herbCount`, Min = 0, Max = 5, **Update Mode** = `Messages`.
   *Expected: a `herbCount` row appears in the Counters list.*

5. **Add a Condition node**
   Right-click the graph background → **Add Node > Condition**. Drag from the Start node's right port to the Condition node's left port.
   *Expected: an arrow connects Start → Condition.*

6. **Attach a CounterQuestCondition**
   Select the Condition node → right panel → **Conditions** → **+** → **Counter Quest Condition**.
   Set **Counter Index** = 0, **Mode** = `AtLeast`, **Required Value** = 5.
   *Expected: the condition row displays "Counter: herbCount >= 5".*

7. **Connect to a Success node**
   Right-click graph → **Add Node > Success**. Wire Condition output → Success input.
   *Expected: graph reads Start → Condition → Success.*

8. **Add Journal content**
   Select the Condition node → right panel → **State Info** → **Active** tab → **Journal Content** → **+** → **Body Text Quest Content**.
   Set the text to: `Collect {herbCount}/5 herbs.`
   *Expected: the body text entry appears in the content list.*

9. **Save**
   Press `Ctrl+S` or click **Save** in the Quest Editor toolbar.
   *Expected: no unsaved-changes indicator remains.*

To test: assign the quest to a QuestGiver or QuestJournal (see **quest-machine-quest-givers**) and in Play mode send increment messages. The Quest Editor's live view shows node states in colour.

## Workflows

### Workflow: Create a collect-N quest (counter-driven)

This generalises the Quick Start for any collectible-count quest.

1. Create Quest asset; add a Counter named `itemCount`, Update Mode `Messages`.
2. Build graph: Start → Condition (CounterQuestCondition: index 0, AtLeast, N) → Success.
3. In gameplay pickup code, increment the counter:

```csharp
using PixelCrushers;
using PixelCrushers.QuestMachine;

// questID must exactly match the Quest asset's ID field.
PixelCrushers.MessageSystem.SendMessage(
    this,
    PixelCrushers.QuestMachine.QuestMachineMessages.IncrementQuestCounterMessage,
    questID,
    new PixelCrushers.StringField("itemCount"),
    1);
```

4. Add **Body Text Quest Content** on the Condition node's Active state:
   `Items collected: {itemCount}/{itemCountMax}`

*Expected: each pickup increments the counter; at N the Condition node turns True and the quest becomes Successful.*

---

### Workflow: Create a kill-N quest (message/counter-driven)

1. Create Quest asset; add a Counter named `killCount`, Update Mode `Messages`.
2. Build graph: Start → Condition (CounterQuestCondition: AtLeast, N) → Success.
3. In the enemy's death handler, send the same `IncrementQuestCounterMessage` pattern with `"killCount"` and the quest's ID string.
4. **Alternative – single unique boss:** use a **MessageQuestCondition** instead of a counter.
   - **Message** = `BossDefeated` (any agreed name), **Parameter** = leave blank.
   - From boss death code: `PixelCrushers.MessageSystem.SendMessage(this, "BossDefeated", "");`
5. Add HUD Body Text: `Wolves slain: {killCount}/{killCountMax}`

*Expected: each kill increments the counter (or the boss death fires the condition) and the quest completes at N.*

---

### Workflow: Create a talk-to-NPC / return-to-giver quest

1. Create Quest asset (no counter needed).
2. Build graph: Start → Condition (MessageQuestCondition) → Success.
3. Configure the MessageQuestCondition:
   - **Message** = `Greet`
   - **Parameter** = `{QUESTGIVERID}` (resolves at runtime to the giver's ID)
   - Sender / Target specifiers: leave as `Any`.
4. No extra gameplay code needed — the `Greet` message (`QuestMachineMessages.GreetMessage`) is automatically sent when the player initiates dialogue with a `QuestGiver`.
5. Add Body Text content on the Condition node's Active state:
   `Return to {QUESTGIVERNAME} to complete the quest.`

*Expected: interacting with the quest-giver NPC sends "Greet", the Condition node becomes True, and the quest transitions to Successful.*

---

### Workflow: Add rewards and alerts on success (QuestAction)

1. Select the **Success** node in the Quest Editor.
2. Right panel → **State Info** → **Successful** tab → **On Become Active Actions** → **+** → choose a type:
   - **Alert Quest Action** — shows a pop-up alert; populate its Content list with Heading/Body text.
   - **Message Quest Action** — fires a `MessageSystem` message (grant XP, items, unlock area, etc.).
   - **Unity Event Quest Action** — calls a serialized `UnityEvent`; wire targets in the Inspector at runtime.
   - **Activate Game Object Quest Action** — enables or disables a named scene GameObject.
   - **Set Quest State Quest Action** — changes another quest's state (e.g., unlocks a follow-up quest).
3. Optionally add **Journal Content** (HeadingTextQuestContent + BodyTextQuestContent) to the Successful state for a completion journal entry.

*Expected: when the quest transitions to Successful all On Become Active actions execute in list order.*

> For full parameter details of every QuestAction, QuestCondition, and QuestContent subclass, read **`references/subassets.md`**.

## Verification

After authoring a quest, confirm:

- [ ] Quest `.asset` file exists in the Project window.
- [ ] Opening it in the Quest Editor shows a connected graph from Start to at least one Success or Failure node.
- [ ] The quest **ID** field is non-empty and unique across the project.
- [ ] In Play mode with the quest active: the Quest Editor live view shows the Start node as True (green) and the Condition node as Active (yellow).
- [ ] Sending a counter increment message while in Play mode changes the counter value shown in the Quest Editor live view.
- [ ] The quest transitions to `Successful` when the counter / message condition is satisfied.

## API quick reference

| Symbol | Namespace | Purpose |
|---|---|---|
| `Quest` | `PixelCrushers.QuestMachine.Wrappers` | ScriptableObject quest asset class |
| `QuestDatabase` | `PixelCrushers.QuestMachine` | Collection of quests + text tables |
| `QuestState` | `PixelCrushers.QuestMachine` | `WaitingToStart`, `Active`, `Successful`, `Failed`, `Abandoned`, `Disabled` |
| `QuestNodeType` | `PixelCrushers.QuestMachine` | `Start`, `Condition`, `Passthrough`, `Success`, `Failure` |
| `QuestNodeState` | `PixelCrushers.QuestMachine` | `Inactive`, `Active`, `True` |
| `QuestCounter` | `PixelCrushers.QuestMachine` | Per-quest named counter with `changed` event |
| `QuestMachineMessages` | `PixelCrushers.QuestMachine` | Message name constants and send helpers |
| `MessageSystem` | `PixelCrushers` | Global event bus; drives counters and conditions |

**Key message names** (constants on `QuestMachineMessages`):

| Message name | param | arg0 | arg1 |
|---|---|---|---|
| `"Increment Quest Counter"` | questID | `StringField` counterName | `int` amount |
| `"Set Quest Counter"` | questID | `StringField` counterName | `int` new value |
| `"Quest State Changed"` | questID | `StringField` nodeID (null = quest) | `QuestState`/`QuestNodeState` new state |

**Programmatic Quest asset creation:**

```csharp
using UnityEngine;
using UnityEditor;
using PixelCrushers.QuestMachine.Wrappers;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var quest = ScriptableObject.CreateInstance<Quest>();
        quest.isInstance = false;
        AssetDatabase.CreateAsset(quest, "Assets/Quests/MyQuest.asset");
        AssetDatabase.SaveAssets();
        result.RegisterObjectCreation(quest);
        result.Log("Created quest asset.", quest);
    }
}
```

> Full subasset catalog — all conditions, actions, and content types with serialized fields: **`references/subassets.md`**.

## Common issues

| Symptom | Likely cause | Fix |
|---|---|---|
| Quest stays `WaitingToStart` | Not assigned to a QuestGiver or autostart condition unmet | Assign quest to a QuestGiver; or enable and configure autostart conditions in the left panel |
| Counter never increments | Wrong `questID` string or `counterName` in the sent message | Log message arguments; confirm `questID` exactly matches the Quest asset's **ID** field |
| Condition node stays `Inactive` | Node not wired from an active predecessor | Trace graph wiring; every node must have a path back to Start |
| Quest completes immediately | Counter initial value already satisfies the condition | Lower the counter's **Initial Value** or raise **Required Value** |
| Journal content missing | Content added to the wrong state tab | Ensure Body Text is under the **Active** state of the Condition node, not Start or Success |
| `"Increment Quest Counter"` ignored | Quest is not yet in `Active` state | Counter messages are only processed while the quest is `Active` |

## Boundaries

- **Node graph editing is a visual task.** The Quest Editor is the canonical authoring tool. Scripting complex multi-branch graphs via `AssetDatabase` is possible but tedious; guide users to the Quest Editor for anything beyond simple linear graphs.
- **Procedural generation** — quests auto-built from entity/domain data at runtime — belongs to **quest-machine-procedural-generation**.
- **Runtime scripting** — reading quest state in code, subscribing to quest events, save/load integration — belongs to **quest-machine-runtime-scripting**.
- **Scene and UI setup** — `QuestMachineConfiguration`, Quest Database assignment, Unity UI Quest Journal/HUD/Dialogue prefabs — belongs to **quest-machine-scene-setup**.
