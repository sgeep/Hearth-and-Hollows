---
name: "quest-machine-quest-givers"
description: "Use this skill whenever you need to set up an NPC to offer quests or receive turn-ins — e.g., 'why won't my NPC give the quest', 'add a quest giver', 'NPC not offering quest', 'show a quest indicator over my NPC', 'player can't turn in quest', 'quest giver dialogue not showing', 'wire a quest to an NPC'. Covers adding the QuestGiver component (Component > Pixel Crushers > Quest Machine > Quest Giver), assigning quests, offer conditions, dialogue content for offer/active/turn-in states, overhead indicator UI, and the player QuestJournal, plus runnable C# to add QuestGiver in code. Do NOT use for authoring quest graphs or counters (see quest-machine-author-quests), procedural generation (see quest-machine-procedural-generation), full branching dialogue trees (use Pixel Crushers Dialogue System), or scene/UI setup (see quest-machine-scene-setup). When in doubt about NPC quest-giving, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Quest Machine – Quest Givers

A **QuestGiver** (`PixelCrushers.QuestMachine.Wrappers.QuestGiver`) is a MonoBehaviour that sits on an NPC and manages the quest offer → accept → track → turn-in lifecycle. When the player enters range or interacts, a Quest Dialogue UI opens showing available quests; on acceptance the quest is handed to the player's **QuestJournal** and transitions to `Active`; when objectives are complete the player returns to turn in. QuestGiver also manages offer conditions, per-quest dialogue content, and overhead indicator icons — all configured in the Inspector without code.

## When to use this skill

Use this skill when you need to:

- Attach a `QuestGiver` component to an NPC and assign quests to it
- Configure when quests appear in the offer dialogue (offer conditions)
- Set up dialogue text for offer, active-reminder, and turn-in states
- Enable overhead quest indicators (the !, ?, ✓ icons above NPCs)
- Wire trigger/collision-based interaction via `QuestControl`
- Script `QuestGiver` or `QuestJournal` from C# code
- Debug: NPC not offering, indicator not visible, turn-in not triggering

## Prerequisites

Confirm Quest Machine is installed — choose one method:

**Method 1 — menu check:** `Tools > Pixel Crushers > Quest Machine > Quest Editor` must exist. If absent, install from the Asset Store URL in the frontmatter.

**Method 2 — code check** (run as a RunCommand):

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var t = System.Type.GetType(
            "PixelCrushers.QuestMachine.QuestGiver, PixelCrushers.QuestMachine");
        result.Log(t != null
            ? "Quest Machine is installed."
            : "Quest Machine NOT found — install from the Asset Store.");
    }
}
```

*Expected: log prints "Quest Machine is installed."*

**Scene requirements** — the following must be present before testing:

| Component | Where | Menu path |
|---|---|---|
| `QuestMachineConfiguration` | Any scene GameObject | `Tools > Pixel Crushers > Quest Machine > Quest Machine Configuration` |
| Quest Dialogue UI prefab | Assigned in `QuestMachineConfiguration` | Use the `UnityUIQuestDialogueUI` prefab from the demo |
| `QuestJournal` | Player GameObject | `Component > Pixel Crushers > Quest Machine > Quest Journal` |

## Quick start

**Goal:** make an NPC named `Merchant` offer a pre-built `CollectHerbs` quest.

1. **Select the NPC** `Merchant` in the Hierarchy.

2. **Add the QuestGiver component**
   `Component > Pixel Crushers > Quest Machine > Quest Giver`
   *Expected: a `QuestGiver` component appears on `Merchant`.*

3. **Set the QuestGiver ID**
   In the Inspector set **ID** = `Merchant`.
   *Expected: ID field shows "Merchant".*

4. **Assign the quest**
   In the Inspector expand **Quest List** → **+** → drag `CollectHerbs.asset` into the new slot.
   *Expected: `CollectHerbs` appears in the Quest List.*

5. **Add a trigger Collider (3D)**
   Add a `Sphere Collider` to the NPC, enable **Is Trigger**, set **Radius** = 2.
   Add a `Rigidbody` (Is Kinematic = true) so Unity fires trigger callbacks.
   *Expected: the trigger gizmo surrounds the NPC in the Scene view.*
   *(For 2D: add `Circle Collider 2D` (Is Trigger) + `Rigidbody 2D` (Body Type = Kinematic), and ensure `USE_PHYSICS2D` is in Scripting Define Symbols.)*

6. **Add QuestControl for trigger → dialogue wiring**
   `Component > Pixel Crushers > Quest Machine > Quest Control`
   In the Inspector set **Quest Giver** = `Merchant`'s `QuestGiver` and **Interaction Type** = `On Trigger Enter`.
   *Expected: QuestControl Inspector shows the wired QuestGiver.*

7. **Enter Play mode and walk into range**
   *Expected: the Quest Dialogue UI opens and shows the "Collect 5 Herbs" offer. Accepting it adds the quest to the player's QuestJournal.*

## Workflows

### Workflow: Attach a QuestGiver and assign quests

**Inspector steps:**
1. Select the NPC GameObject.
2. `Component > Pixel Crushers > Quest Machine > Quest Giver`.
3. Set **ID** to a unique string (referenced by indicators and messaging).
4. Expand **Quest List** → **+** for each quest → assign the Quest `.asset`.

**Programmatic alternative** (RunCommand, EditMode only):

```csharp
using UnityEngine;
using UnityEditor;
using PixelCrushers.QuestMachine.Wrappers;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var npc = UnityEditor.Selection.activeGameObject;
        if (npc == null) { result.LogError("Select an NPC GameObject first."); return; }

        var giver = npc.GetComponent<QuestGiver>()
                    ?? npc.AddComponent<QuestGiver>();
        result.RegisterObjectModification(npc);

        // ID must match the entity name used elsewhere in the quest graphs.
        giver.id = new PixelCrushers.StringField(npc.name);

        // Assign an existing Quest asset:
        var questAsset = AssetDatabase.LoadAssetAtPath<Quest>(
            "Assets/Quests/CollectHerbs.asset");
        if (questAsset != null)
        {
            giver.AddQuest(questAsset);
            result.Log("Quest assigned to " + npc.name, npc);
        }
        else
        {
            result.LogWarning("Quest asset not found at Assets/Quests/CollectHerbs.asset");
        }

        EditorUtility.SetDirty(npc);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        result.Log("QuestGiver added to " + npc.name, npc);
    }
}
```

*Expected: the selected NPC gains a QuestGiver component with its name as ID and CollectHerbs in the Quest List.*

---

### Workflow: Set offer conditions

Offer conditions gate which quests appear in the dialogue. Each Quest asset defines its own offer conditions.

1. Select the Quest asset in the Project window (e.g., `CollectHerbs.asset`).
2. Open it in `Tools > Pixel Crushers > Quest Machine > Quest Editor`.
3. In the left panel click **Offer Conditions** → **+** → choose a condition type:
   - **Quest State Quest Condition** — requires another quest to be in a specified `QuestState` (e.g., `Successful`).
   - **Counter Quest Condition** — requires a counter value (e.g., player level counter ≥ 5).
   - **Message Quest Condition** — requires a specific `MessageSystem` message to have been received once.
4. The QuestGiver re-checks offer conditions each time the player initiates dialogue.

*Expected: the quest only appears in the offer dialogue when all offer conditions evaluate to true.*

---

### Workflow: Configure dialogue content (offer / active / turn-in)

Each quest defines per-state UI content that the Quest Dialogue UI renders.

1. Open the Quest asset in the Quest Editor (`Tools > Pixel Crushers > Quest Machine > Quest Editor`).
2. In the left panel click **State Info**:
   - **WaitingToStart** → **Dialogue Content** — shown when offering the quest.
   - **Active** → **Dialogue Content** — shown when the player re-talks to the giver mid-quest.
   - **Successful** → **Dialogue Content** — shown at turn-in.
3. For each state, add:
   - **Heading Text Quest Content** — quest title line.
   - **Body Text Quest Content** — instructions or flavour text.
4. Use tags in text fields: `{QUESTGIVERNAME}`, `{QUESTGIVERID}`, `{herbCount}`, `{herbCountMax}`, etc. Quest Machine resolves them at display time.

*Expected: the Quest Dialogue UI shows the appropriate content for the current quest state during Play mode.*

---

### Workflow: Enable overhead quest indicators

Quest indicators are the icons (!, ?, ✓) shown above NPC heads to communicate quest availability.

1. Ensure a **Quest Indicator UI** prefab is assigned in `QuestMachineConfiguration` (the demo ships `UnityUIQuestIndicatorUI`).
2. Add **Quest Indicator Manager** to the NPC:
   `Component > Pixel Crushers > Quest Machine > Quest Indicator Manager`
3. Set the NPC's **QuestGiver ID** in the `QuestGiver` Inspector and confirm the `QuestIndicatorManager` references the same NPC's `QuestGiver` component (or they share the same ID string).
4. Indicator state is updated automatically: offer-available → `Offer` icon; quest active → `Active` icon; ready to turn in → `TurnIn` icon.
5. To change indicator sprites, edit the `QuestIndicatorState` entries on the Quest Indicator UI prefab.

*Expected: in Play mode the NPC's indicator icon changes as quest state progresses.*

---

### Workflow: Turn in a quest at the giver

Turn-in is automatic — no extra code needed if the workflow is correctly set up.

1. Verify the Quest **Success** node has **Dialogue Content** configured (see configure dialogue content workflow above).
2. When the player re-triggers the QuestGiver dialogue after the quest reaches `Successful`, Quest Machine detects the state and shows the turn-in dialogue.
3. Actions on the Success node's **On Become Active** slot fire (rewards, alerts, follow-up quests). See **quest-machine-author-quests** for how to add those.
4. After turn-in, the quest moves to the journal's completed list if **Remember Completed Quests** is enabled on the player's `QuestJournal`.

*Expected: the Dialogue UI shows the Success state content, reward actions execute, and the quest no longer appears in the active HUD.*

## Verification

After setting up a QuestGiver, confirm:

- [ ] NPC has a `QuestGiver` component with a non-empty unique **ID**.
- [ ] At least one Quest asset appears in the **Quest List**.
- [ ] NPC has a trigger Collider and a Rigidbody (or 2D equivalents) for interaction.
- [ ] Scene contains `QuestMachineConfiguration`, a Quest Dialogue UI prefab, and a player `QuestJournal`.
- [ ] In Play mode: entering the trigger zone opens the Quest Dialogue UI showing the quest offer.
- [ ] After accepting: the quest appears in the player's `QuestJournal` with state `Active`.
- [ ] After objectives complete: returning to the NPC triggers the turn-in dialogue.
- [ ] Overhead indicator icon updates correctly through offer → active → turn-in states.

## API quick reference

| Symbol | Namespace | Purpose |
|---|---|---|
| `QuestGiver` | `PixelCrushers.QuestMachine.Wrappers` | MonoBehaviour; offers quests and receives turn-ins |
| `QuestJournal` | `PixelCrushers.QuestMachine.Wrappers` | MonoBehaviour on player; holds accepted quests |
| `QuestControl` | `PixelCrushers.QuestMachine.Wrappers` | Helper; wires trigger/collision events to QuestGiver |
| `QuestIndicatorManager` | `PixelCrushers.QuestMachine.Wrappers` | Manages overhead indicator icons for an entity |
| `QuestMachineMessages.GreetMessage` | `PixelCrushers.QuestMachine` | `"Greet"` — sent when player initiates dialogue |
| `QuestMachineMessages.GreetedMessage` | `PixelCrushers.QuestMachine` | `"Greeted"` — sent just after dialogue starts |
| `QuestMachineMessages.DiscussQuestMessage` | `PixelCrushers.QuestMachine` | `"Discuss Quest"` — sent when a specific quest is opened |

**Open dialogue from code (runtime):**

```csharp
using UnityEngine;
using PixelCrushers.QuestMachine;

public class InteractTrigger : MonoBehaviour
{
    [SerializeField] private PixelCrushers.QuestMachine.QuestGiver _questGiver;

    private void OnTriggerEnter(Collider other)
    {
        // 'other' should be the player with a QuestJournal.
        _questGiver.StartDialogue(other.gameObject);
    }
}
```

**Check whether a quest is currently offerable:**

```csharp
using PixelCrushers.QuestMachine;

bool canOffer = _questGiver.IsQuestOfferable(questAsset);
```

**Programmatically give a quest to the player:**

```csharp
using PixelCrushers.QuestMachine;

// Call on the player's QuestJournal to add a quest directly (bypasses dialogue UI).
PixelCrushers.QuestMachine.QuestJournal playerJournal =
    playerGameObject.GetComponent<PixelCrushers.QuestMachine.QuestJournal>();
playerJournal.AcceptQuest(questAsset);
```

## Common issues

| Symptom | Likely cause | Fix |
|---|---|---|
| Quest Dialogue UI never opens | Missing trigger Collider or Rigidbody on NPC | Add trigger Sphere Collider + Kinematic Rigidbody; verify `QuestControl` is wired |
| Quest not shown in offer list | Offer conditions not met | Inspect each offer condition in the Quest Editor; temporarily remove them to confirm |
| Accepting quest does nothing | No `QuestJournal` on player | Add `Component > Pixel Crushers > Quest Machine > Quest Journal` to the player |
| Overhead indicator not visible | `QuestIndicatorManager` missing or wrong ID | Add `QuestIndicatorManager` to NPC; verify its ID matches the `QuestGiver` ID |
| 2D trigger not firing | Missing `USE_PHYSICS2D` scripting define | Add `USE_PHYSICS2D` to `Project Settings > Player > Scripting Define Symbols` |
| Turn-in dialogue not appearing | Success node has no Dialogue Content | Add Heading + Body content to the Successful state in the Quest Editor |
| Quest re-offered after completion | `Remember Completed Quests` disabled on `QuestJournal` | Enable **Remember Completed Quests** on the player's `QuestJournal`, or set **Delete When Complete** on the Quest asset |
| Offer dialogue shows "no quests" | Quest already accepted or cooldown active | Check the quest's state in Play mode via the Quest Editor live view |

## Boundaries

- **Quest node graph authoring** — conditions, counters, objectives, node wiring — belongs to **quest-machine-author-quests**.
- **Quest giver dialogue is quest-scoped.** The Dialogue UI shows per-quest offer/active/turn-in content. For a full branching NPC conversation system with complex trees, integrate **Pixel Crushers Dialogue System** (a separate asset that also integrates with Quest Machine).
- **Procedural generation** — auto-building quest givers from entity and domain data — belongs to **quest-machine-procedural-generation**.
- **Scene/UI setup** — `QuestMachineConfiguration`, Quest Database, Unity UI prefab assignment — belongs to **quest-machine-scene-setup**.
- **Runtime scripting** — subscribing to quest events, reading state from code, save/load — belongs to **quest-machine-runtime-scripting**.
