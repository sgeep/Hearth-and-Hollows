---
name: quest-machine-procedural-generation
description: "Use this skill whenever you need to make quests generate randomly or set up a procedural quest system — e.g., 'make quests generate randomly', 'set up the Quest Generator', 'create entity types for the world model', 'configure drives and actions for procedural quests', 'add faction relationships so NPCs generate quests', or 'get a QuestGiver to produce quests at runtime'. Covers the QuestGenerator system, world-model ScriptableObject assets (EntityType, Faction, Drive, Action, DomainType), urgency and requirement functions, and configuring a generating QuestGiver. Do NOT use for hand-authored quest graphs in the visual editor (see quest-machine-author-quests) or for runtime C# quest state control (see quest-machine-runtime-scripting). When in doubt whether a procedural generation question touches Quest Machine, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Quest Machine — Procedural Generation

Quest Machine's procedural generation system builds quests automatically at runtime from a world model composed of ScriptableObject assets — EntityTypes, Factions, Drives, Actions, DomainTypes, and urgency functions. A QuestGiver configured as a generator observes world entities, evaluates drive urgency, selects an appropriate Action, and synthesises a playable Quest instance from the Action's quest template — no hand-authored quest graph required. Start from the included Demo scene for a complete working reference before building your own world model.

## When to use this skill

Use this skill when you need to:

- Open or use the Quest Generator window (**Tools > Pixel Crushers > Quest Machine > Quest Generator**)
- Create world-model ScriptableObject assets: EntityType, Faction, Drive, Action, DomainType
- Define or tune urgency functions (DriveAlignment, Faction, Literal, Threat) and requirement functions
- Configure a QuestGiver as a procedural quest generator
- Add `QuestEntity` components to scene objects so the generator can observe them
- Inspect or debug quests produced at runtime in the Quest Editor
- Use or adapt the Demo scene (`Assets/Plugins/Pixel Crushers/Quest Machine/Demo/Demo.unity`)

## Prerequisites

1. Quest Machine is imported from the Asset Store: `https://assetstore.unity.com/packages/tools/game-toolkits/quest-machine-39834`
2. Confirm installation: open **Tools > Pixel Crushers > Quest Machine > Quest Generator**. If the menu item is missing, reimport the package.
   - Alternatively, verify that `PixelCrushers.QuestMachine.QuestGenerator` or `PixelCrushers.QuestMachine.EntityType` resolves in a C# script without errors.
3. Open the **Demo scene** (`Assets/Plugins/Pixel Crushers/Quest Machine/Demo/Demo.unity`) as a reference. The Orc NPC in that scene is a fully configured procedural quest generator — study its ScriptableObject assets before creating your own.

## Quick start

Minimum viable procedural quest setup:

1. **Create an EntityType** — **Assets > Create > Pixel Crushers > Quest Machine > Generator > Entity Type**. Name it `Orc`. This represents a category of world object.
2. **Create a Drive** — **Assets > Create > Pixel Crushers > Quest Machine > Generator > Drive**. Name it `Hostile`. This is the motivation that causes the generator to produce a quest.
3. **Create an Action** — **Assets > Create > Pixel Crushers > Quest Machine > Generator > Action**. Assign `Hostile` as its **Drive**, set a **Quest Template** (a hand-authored Quest asset that provides the node structure), and set the target EntityType.
4. **Create a DomainType** — **Assets > Create > Pixel Crushers > Quest Machine > Generator > Domain Type**. Name it `Forest`.
5. **Configure a QuestGiver**: Select the NPC GameObject, open the **QuestGiver** component Inspector, enable **Generate Quests**, assign the `Orc` EntityType, `Hostile` Drive, and `Attack` Action.

**Expected result:** Enter Play mode and interact with the NPC. A quest is generated and offered. Open **Tools > Pixel Crushers > Quest Machine > Quest Editor** in Play mode to inspect the generated quest's node graph.

## Workflows

### Workflow: Create the world model assets

All world-model types are ScriptableObjects. Create them via **Assets > Create > Pixel Crushers > Quest Machine > Generator > ...**:

| Asset type | Create-menu suffix | Purpose |
|---|---|---|
| EntityType | `Entity Type` | A category of world object (orc, villager, carrot) |
| Faction | `Faction` | Relationship affinities between entity types (−100 to +100) |
| Drive | `Drive` | Motivation that pushes an entity to generate quests |
| Action | `Action` | What the entity does to satisfy a drive; holds a quest template |
| DomainType | `Domain Type` | An environment or area where an action takes place |

Wrappers subclasses in `PixelCrushers.QuestMachine.Wrappers` are the scene-safe equivalents; prefer them when placing assets on GameObjects.

**Expected result:** ScriptableObject assets appear in the Project window; their Inspectors expose configuration fields matching the Demo scene's assets.

---

### Workflow: Define drives and actions

1. Open your **Drive** asset. Set a descriptive **Display Name**. Link it to the EntityTypes that feel this drive via the EntityType's **Drives** list.
2. Open your **Action** asset and configure:
   - **Drive** — the Drive this action satisfies.
   - **Domain Type** — where the action takes place.
   - **Min/Max Count** — how many targets the generated quest requires.
   - **Quest Template** — a hand-authored Quest asset providing the structural blueprint (nodes, rewards). Without this the generator has nothing to fill in.
3. In the EntityType asset, add the Action to its **Actions** list.

**Expected result:** The Action Inspector shows all fields populated and the EntityType lists the Action. The Demo scene's Orc EntityType and Attack Action show a correct example.

---

### Workflow: Configure urgency functions

Urgency functions determine how strongly an entity wants to act. Create them via **Assets > Create > Pixel Crushers > Quest Machine > Generator > Urgency Functions > ...**:

| Function type | When to use |
|---|---|
| `DriveAlignmentUrgencyFunction` | Urgency scales with how well the action aligns to the entity's active drive |
| `FactionUrgencyFunction` | Urgency scales with faction affinity towards a target entity type |
| `LiteralUrgencyFunction` | Fixed urgency value — useful for always-available quests |
| `ThreatUrgencyFunction` | Urgency based on perceived threat level of nearby entities |

Assign urgency function assets to the **Urgency Functions** list on an EntityType or Action asset. For requirement functions, create a **FactionRequirementFunction** via the same menu (**Requirement Functions > Faction Requirement Function**) and add it to an Action's **Requirements** list.

**Expected result:** With `QuestMachine.debug = true`, the Console logs urgency scores for each candidate action before a quest is generated in Play mode.

---

### Workflow: Set up a generating QuestGiver

1. Select the NPC GameObject that will generate quests.
2. Add **Component > Pixel Crushers > Quest Machine > Quest Giver** if not already present.
3. In the QuestGiver Inspector:
   - Enable **Generate Quests**.
   - Set **Quester** to the player QuestJournal's ID (e.g., `"Player"`).
   - Add the relevant EntityTypes under **Entity Types**.
   - Add Drives under **Drives**.
4. In the scene, add world-entity GameObjects and attach **Component > Pixel Crushers > Quest Machine > Quest Entity** to each. Set each entity's **Entity Type** to an EntityType the generator references.
5. Enter Play mode and interact with the NPC.

**Expected result:** The QuestGiver generates a quest derived from the world model. The quest appears in the dialogue UI when the player talks to the NPC, and in the journal after acceptance.

---

### Workflow: Inspect generated quests in the Quest Editor at runtime

1. Enter Play mode.
2. Open **Tools > Pixel Crushers > Quest Machine > Quest Editor**.
3. Select the generating QuestGiver GameObject in the Hierarchy. Its generated Quest instances appear in the quest list on the left.
4. Select a generated Quest to view its auto-built node graph.

**Expected result:** The Quest Editor shows a valid node graph (Start → objective node(s) → Success) built from the Action's quest template with entity names substituted in.

## Verification

| Check | How | Expected result |
|---|---|---|
| Generator menu opens | **Tools > Pixel Crushers > Quest Machine > Quest Generator** | Window opens, no errors |
| EntityType type resolves | Reference `PixelCrushers.QuestMachine.EntityType` in a script | Compiles cleanly |
| Quest generated | Play mode + NPC interaction | Quest offered and accepted |
| Node graph valid | Quest Editor in Play mode | Start → node(s) → Success |
| Demo scene runs | Open Demo.unity, Enter Play mode | Orc NPC offers procedural quest |
| Urgency logged | Set `QuestMachine.debug = true`, Play mode | Urgency scores printed in Console |
| Console clean | **Window > General > Console** | No errors or exceptions |

## API quick reference

```csharp
// All world-model types are ScriptableObjects configured via the Inspector.
// Runtime generation is driven by QuestGiver component settings, not direct API calls.

// Enable generation debug logging to trace urgency evaluation:
using PixelCrushers.QuestMachine;
QuestMachine.debug = true;

// Access the generating QuestGiver component at runtime:
using PixelCrushers.QuestMachine;
var giver = GetComponent<PixelCrushers.QuestMachine.Wrappers.QuestGiver>();

// ScriptableObject asset types — create via Assets > Create > Pixel Crushers > Quest Machine > Generator:
// PixelCrushers.QuestMachine.EntityType
// PixelCrushers.QuestMachine.Faction
// PixelCrushers.QuestMachine.Drive
// PixelCrushers.QuestMachine.Action          (not System.Action — use full namespace)
// PixelCrushers.QuestMachine.DomainType
// PixelCrushers.QuestMachine.DriveAlignmentUrgencyFunction
// PixelCrushers.QuestMachine.FactionUrgencyFunction
// PixelCrushers.QuestMachine.LiteralUrgencyFunction
// PixelCrushers.QuestMachine.ThreatUrgencyFunction
// PixelCrushers.QuestMachine.FactionRequirementFunction

// Avoid System.Action ambiguity with a using alias:
using QMAction = PixelCrushers.QuestMachine.Action;
```

## Common issues

**No quests generated in Play mode:** Ensure `QuestEntity` GameObjects are present in the scene and their **Entity Type** assets match those listed in the generating QuestGiver. Enable `QuestMachine.debug = true` to trace urgency evaluation.

**Quest template is missing:** Every Action asset must reference a hand-authored Quest asset as its **Quest Template**. Without it the generator has no node structure to fill in. Open the Demo scene's Orc Action for a complete working example.

**Generated quest body is empty or has placeholder text:** The Quest Template's text nodes must use placeholder tokens (e.g., `{0}` for target entity name) for the generator to substitute. Compare with the Demo template quest.

**Faction affinities not affecting generation:** Confirm the Faction asset is listed in the EntityType's **Factions** array and the FactionUrgencyFunction asset is listed in the **Urgency Functions** array of the relevant EntityType or Action.

**`Action` namespace collision:** `PixelCrushers.QuestMachine.Action` conflicts with `System.Action`. Always use the full namespace or declare a using alias: `using QMAction = PixelCrushers.QuestMachine.Action;`

**QuestEntity not detected:** The generating QuestGiver must be able to find `QuestEntity` components — typically within its configured search radius. Ensure world entities are within range and have the QuestEntity component with the correct EntityType assigned.

## Boundaries

- **Hand-authored quest graphs** (nodes, conditions, rewards, branching logic) → **quest-machine-author-quests**
- **Runtime C# quest state control** (`GiveQuestToQuester`, `SetQuestState`, counters) → **quest-machine-runtime-scripting**
- **Scene object setup** (NPC placement, QuestJournal on player, UI wiring) → **quest-machine-scene-setup**
- **Quest Giver NPC dialogue and offer configuration** → **quest-machine-quest-givers**
- **Initial project install and package overview** → **quest-machine-setup-and-overview**
- Procedural generation is a large system with many interacting ScriptableObjects. Always start by adapting the Demo scene (`Assets/Plugins/Pixel Crushers/Quest Machine/Demo/Demo.unity`) rather than building from scratch.
