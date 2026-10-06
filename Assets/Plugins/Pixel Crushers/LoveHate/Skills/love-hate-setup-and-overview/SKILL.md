---
name: love-hate-setup-and-overview
description: "Use this skill whenever the user wants to install, set up, verify, or understand the Love/Hate relationship and faction system by Pixel Crushers. E.g. 'set up love hate', 'is love/hate installed?', 'how do I start with the faction system', 'add the faction manager', 'enable 2D support for love hate'. Covers install verification, first-time setup (2D physics and TextMesh Pro defines), creating a Faction Database, adding a Faction Manager, and an index of the other Love/Hate skills. Do NOT use for authoring faction data (see love-hate-faction-database), adding NPCs (see love-hate-faction-members), deeds (see love-hate-deeds-and-reactions), gossip/greetings (see love-hate-gossip-and-greetings), or runtime scripting (see love-hate-save-and-runtime-api). When in doubt whether a relationship, faction, or NPC-emotion request could involve Love/Hate, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Love/Hate"
  publisher: "Pixel Crushers"
  asset-version: "1.10.73"
  skill-version: "1.0.0"
  unity: "2022.3+"
  render-pipelines: "Built-in, URP, HDRP (pipeline-agnostic)"
  category: "tools/behavior-ai"
  asset-store-url: "https://assetstore.unity.com/packages/tools/behavior-ai/love-hate-33063"
  documentation-url: "https://www.pixelcrushers.com/lovehate/api/html/"
  support-url: "https://www.pixelcrushers.com/"
  last-verified: "2026-07-08"
---

# Set Up Love/Hate

Love/Hate is an emotion-driven relationship and faction system by Pixel Crushers. NPCs (called faction members) belong to factions, hold personality traits and a PAD emotional model (Pleasure/Arousal/Dominance plus Happiness), witness "deeds" performed by others, form memories, and change their affinity toward factions and individuals over time. This skill lets an agent confirm the asset is installed, perform first-time project configuration, create the core Faction Database asset, drop a Faction Manager into the scene, and route to the right sibling skill for everything else.

## When to use this skill

- "Set up Love/Hate" / "get the faction system working"
- "Is Love/Hate installed? Which version?"
- "Add the Faction Manager to my scene"
- "Create a faction database"
- "Enable 2D physics support for Love/Hate" / "enable TextMesh Pro support"
- "What is Love/Hate and what can it do?"

Not for:
- Authoring factions, traits, relationships → `love-hate-faction-database`
- Turning a character GameObject into an NPC in the sim → `love-hate-faction-members`
- Reporting actions and emotional reactions → `love-hate-deeds-and-reactions`
- Rumor spreading and greetings → `love-hate-gossip-and-greetings`
- Persisting or scripting relationships at runtime → `love-hate-save-and-runtime-api`

## Prerequisites

- Unity 2022.3 or newer. Love/Hate's core is pure C# logic and is render-pipeline-agnostic (Built-in, URP, HDRP).
- **Programmatic install check** — run this Editor snippet. If it logs INSTALLED, proceed; otherwise the asset is not present.

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var t = System.Type.GetType("PixelCrushers.LoveHate.FactionManager, Assembly-CSharp");
        bool installed = t != null;
        // Corroborate with the on-disk folder:
        bool folder = System.IO.Directory.Exists(
            System.IO.Path.Combine(Application.dataPath, "Plugins/Pixel Crushers/LoveHate"));
        result.Log("Love/Hate install check: type={0}, folder={1} => {2}",
            t != null, folder, (installed && folder) ? "INSTALLED" : "NOT INSTALLED");
    }
}
```

- If the check fails, tell the user: "Love/Hate does not appear to be installed. Import it from the Asset Store (https://assetstore.unity.com/packages/tools/behavior-ai/love-hate-33063), then re-run setup." Do not attempt to fabricate the API.
- **2D projects only:** to use 2D physics triggers (aura/gossip/greeting), the scripting define `USE_PHYSICS2D` must be set via `Tools > Pixel Crushers > Common > Misc > Enable Physics2D Support...`.
- **TextMesh Pro (pre-Unity 6) only:** define `TMP_PRESENT` via `Tools > Pixel Crushers > Common > Misc > Enable TextMesh Pro Support...`.

## Quick start

Goal: get a minimal, working Love/Hate scene skeleton — a Faction Database asset plus a Faction Manager in the scene.

1. Create the database: `Assets > Create > Pixel Crushers > Love∕Hate > Faction Database`. (The `∕` in the menu is a Unicode division slash, U+2215, not a normal `/`.) A `FactionDatabase` ScriptableObject appears in the selected Project folder, pre-seeded with a "Player" faction (ID 0) and the required "Affinity" relationship trait.
2. Add the manager: with a scene open, use `GameObject > Love∕Hate > Faction Manager`. A GameObject named "Faction Manager" is created carrying the `FactionManager` component.
3. Assign the database: select the Faction Manager GameObject and drag your new Faction Database asset into the `Faction Database` field of the `FactionManager` component.
4. Expected observable result: the Hierarchy contains a "Faction Manager" GameObject with a `FactionManager` component whose `Faction Database` field references your asset, and the Console is clear. The scene is now ready for faction members.

## Workflows

### Workflow: Enable 2D physics support

- **Goal:** allow Love/Hate's 2D trigger components (AuraTrigger2D, GossipTrigger2D, GreetingTrigger2D) to compile and run.
- **Steps:**
  1. Invoke `Tools > Pixel Crushers > Common > Misc > Enable Physics2D Support...`.
  2. Confirm the dialog. This adds the `USE_PHYSICS2D` scripting define symbol for the current build target and triggers a recompile.
- **Expected result:** after recompile, the Console is clear and 2D trigger components are available to add.

### Workflow: Create the Faction Database asset (standalone)

- **Goal:** create the central data asset all faction logic reads from.
- **Steps:**
  1. Select a target folder in the Project window (e.g. `Assets/`).
  2. Invoke `Assets > Create > Pixel Crushers > Love∕Hate > Faction Database`.
- **Expected result:** a new `FactionDatabase` asset is created and selected, containing a Player faction (ID 0) and one relationship trait named "Affinity". Author its contents with `love-hate-faction-database`.

### Workflow: Add the Faction Manager to the scene

- **Goal:** provide the single runtime coordinator that hosts the runtime copy of the database and drives witness updates.
- **Steps:**
  1. Invoke `GameObject > Love∕Hate > Faction Manager`.
  2. Assign the Faction Database asset to the component's `Faction Database` field.
- **Expected result:** a "Faction Manager" GameObject with a `FactionManager` component. At runtime it is reachable via `PixelCrushers.LoveHate.FactionManager.instance`. It clones the assigned database into a runtime instance on Awake, so edits at runtime do not modify the source asset.

## Verification

- The install check snippet logs INSTALLED.
- Project contains a `FactionDatabase` asset (`t:FactionDatabase` in the Project search).
- The active scene contains exactly one GameObject with a `FactionManager` component, and its `Faction Database` field is assigned.
- Console shows no errors after entering and exiting Play mode.
- Programmatic scene check:

```csharp
using UnityEngine;
using UnityEditor;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var mgrs = Object.FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        bool ok = mgrs.Length == 1 && mgrs[0].factionDatabase != null;
        result.Log("Setup verification: managers={0}, dbAssigned={1} => {2}",
            mgrs.Length, mgrs.Length == 1 && mgrs[0].factionDatabase != null,
            ok ? "PASS" : "FAIL");
    }
}
```

## API quick reference

| Entry point | Type | What it does |
| --- | --- | --- |
| `PixelCrushers.LoveHate.FactionManager` | MonoBehaviour | Scene coordinator; holds runtime `factionDatabase`; access via static `instance`. |
| `PixelCrushers.LoveHate.FactionDatabase` | ScriptableObject | Central data asset: factions, trait definitions, relationships, presets. |
| `PixelCrushers.LoveHate.FactionMember` | MonoBehaviour | Turns a GameObject into an NPC in the sim (see `love-hate-faction-members`). |
| `FactionDatabase.PlayerFactionID` | const int (= 0) | Reserved faction ID for the player. |

## Common issues

- **Symptom:** Faction members log errors about a missing manager at Start. **Cause:** no `FactionManager` in the scene. **Fix:** add one via `GameObject > Love∕Hate > Faction Manager` and assign the database.
- **Symptom:** A second Faction Manager destroys itself at runtime. **Cause:** `allowOnlyOneFactionManager` is enabled and more than one exists. **Fix:** keep exactly one Faction Manager per scene.
- **Symptom:** 2D trigger components are missing or fail to compile. **Cause:** `USE_PHYSICS2D` is not defined. **Fix:** run `Tools > Pixel Crushers > Common > Misc > Enable Physics2D Support...`.
- **Symptom:** `ExecuteMenuItem` on a Love/Hate create-menu path silently fails. **Cause:** used a normal `/` where the label uses the division slash `∕` (U+2215). **Fix:** copy the exact menu string including the `∕` character.

## Boundaries

- Love/Hate provides the relationship/faction/emotion simulation and data model only. It does not provide pathfinding, dialogue, combat, or animation — integrate those with your own systems or other assets.
- Third-party bridges (Dialogue System for Unity, PlayMaker, Opsive, etc.) are separate integrations and are out of scope for these skills.
- Saving relationships relies on the Pixel Crushers Common Save System (bundled). Only the Love/Hate-specific saver components are covered here (see `love-hate-save-and-runtime-api`).
- Other skills in this package: `love-hate-faction-database` (author factions/traits/relationships), `love-hate-faction-members` (add NPCs), `love-hate-deeds-and-reactions` (report actions and reactions), `love-hate-gossip-and-greetings` (rumor spreading and greetings), `love-hate-save-and-runtime-api` (persistence and runtime scripting).
