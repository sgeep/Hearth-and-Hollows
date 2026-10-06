---
name: love-hate-faction-database
description: "Use this skill whenever the user wants to author or edit the social structure of a Love/Hate project — factions, personality traits, and relationships between groups. E.g. 'add a Bandits faction', 'make the guards hate the thieves', 'set up factions for my RPG', 'define personality traits', 'import my factions from a spreadsheet', 'export the faction database to JSON'. Covers creating/editing the Faction Database asset, adding factions and parent (inheritance) relationships, setting affinity and custom traits, defining trait definitions and presets, and CSV/JSON import-export. Do NOT use for adding characters to the sim (see love-hate-faction-members), reporting actions (see love-hate-deeds-and-reactions), or install (see love-hate-setup-and-overview). When in doubt whether a faction, reputation, or 'who likes whom' request could involve Love/Hate, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Author the Love/Hate Faction Database

The Faction Database is the central `ScriptableObject` that defines your game's social world: the factions, the personality traits every faction carries, the relationship traits (starting with the required "Affinity"), and how child factions inherit values from parents. This skill lets an agent create and populate that asset — adding factions, wiring inheritance, setting who likes or dislikes whom, defining trait definitions and presets, and moving data in and out via CSV/JSON.

## When to use this skill

- "Add a Bandits / Guards / Villagers faction"
- "Make faction A hate / like / distrust faction B"
- "Set the player's starting reputation with the guards"
- "Define personality traits (e.g. Bravery, Honesty)"
- "Set up parent factions so subgroups inherit relationships"
- "Import factions from a CSV" / "export the database to JSON"

Not for:
- Attaching factions to actual character GameObjects → `love-hate-faction-members`
- Making NPCs react to actions → `love-hate-deeds-and-reactions`
- Install/first-time setup → `love-hate-setup-and-overview`

## Prerequisites

- Love/Hate installed (confirm with `love-hate-setup-and-overview`). Quick check: the type `PixelCrushers.LoveHate.FactionDatabase` must resolve.
- A Faction Database asset. If none exists, create one via `Assets > Create > Pixel Crushers > Love∕Hate > Faction Database`. If creation fails, direct the user to the Asset Store link and stop.
- Programmatic check for an existing database:

```csharp
using UnityEngine;
using UnityEditor;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var guids = AssetDatabase.FindAssets("t:FactionDatabase");
        result.Log("Found {0} FactionDatabase asset(s).", guids.Length);
        foreach (var g in guids) result.Log("  {0}", AssetDatabase.GUIDToAssetPath(g));
    }
}
```

## Quick start

Goal: add two factions and make one dislike the other.

1. Select your Faction Database asset (or create one via `Assets > Create > Pixel Crushers > Love∕Hate > Faction Database`).
2. In the Inspector's Factions section, add two factions, e.g. "Guards" and "Bandits". Each new faction gets a unique auto-assigned ID.
3. On the "Guards" faction, add a relationship toward "Bandits" and set its **Affinity** trait to a negative value (range −100..100), e.g. −80.
4. Expected observable result: the database now lists Guards and Bandits; Guards → Bandits has Affinity −80. At runtime `FactionManager.instance.GetAffinity(guardsID, banditsID)` returns approximately −80.

Trait/affinity value semantics (verified in source):
- All personality and relationship trait values are clamped to **−100..100**.
- **Affinity is relationship trait index 0** and is required in every database.
- Default affinity toward another faction is **0** (neutral); default self-affinity (a faction toward itself) is **100**.

## Workflows

### Workflow: Create the database and add factions

- **Goal:** establish the faction list.
- **Steps:**
  1. `Assets > Create > Pixel Crushers > Love∕Hate > Faction Database` (skip if one exists). It is pre-seeded with faction "Player" (ID 0) and the required "Affinity" relationship trait.
  2. In the Inspector, add each faction with a name and description. IDs are assigned automatically and are stable.
  3. Programmatic alternative (Editor):
     ```csharp
     using UnityEngine;
     using UnityEditor;
     using PixelCrushers.LoveHate;

     internal class CommandScript : IRunCommand
     {
         public void Execute(ExecutionResult result)
         {
             var db = AssetDatabase.LoadAssetAtPath<FactionDatabase>(
                 "Assets/FactionDatabase.asset"); // adjust path
             result.RegisterObjectModification(db);
             int guards = db.CreateNewFaction("Guards", "City watch");
             int bandits = db.CreateNewFaction("Bandits", "Highwaymen");
             EditorUtility.SetDirty(db);
             AssetDatabase.SaveAssets();
             result.Log("Created factions: Guards={0}, Bandits={1}", guards, bandits);
         }
     }
     ```
- **Expected result:** the database's Factions list contains the new entries with unique IDs.

### Workflow: Set relationships (who likes whom)

- **Goal:** define a directed relationship trait (usually Affinity) from one faction to another.
- **Steps:**
  1. Select the judging faction in the database Inspector.
  2. Add/edit a relationship toward the target faction; set **Affinity** (index 0) in −100..100. Negative = dislike/hostile, positive = like/friendly, 0 = neutral.
  3. Relationships are directed — set the reverse relationship separately if you want mutual feelings.
  4. Optionally mark a relationship `inheritable` so child factions inherit it.
- **Expected result:** the relationship appears under the judging faction; `FactionManager.instance.GetAffinity(judgeID, subjectID)` reflects it (inheritance from parents is folded in per the database's inheritance type).

### Workflow: Parent factions and inheritance

- **Goal:** let subgroups inherit personality traits and relationships from parent factions.
- **Steps:**
  1. On a child faction, add one or more parent faction IDs.
  2. Choose the database's `traitInheritanceType` and `relationshipInheritanceType`: `Average` (default) or `Sum`.
  3. Use `percentJudgeParents` on a faction to scale how much relationship changes propagate to its parents (default 0 = no propagation).
- **Expected result:** a child with no personal relationship toward a target uses the inherited/averaged (or summed) value from its parents.

### Workflow: Define personality traits and presets

- **Goal:** add measurable personality dimensions (e.g. Bravery, Honesty) and reusable value presets.
- **Steps:**
  1. In the database, add `personalityTraitDefinitions` entries; each `TraitDefinition` has `name`, `description`, `minValue` (default −100), `maxValue` (default 100).
  2. Optionally add `relationshipTraitDefinitions` beyond the required "Affinity" for custom relationship dimensions.
  3. Add `Preset` entries (name + trait value array) to quickly stamp common personality profiles onto factions.
- **Expected result:** faction Inspectors show sliders for each defined trait; presets appear in the faction trait dropdown.

### Workflow: CSV / JSON import-export

- **Goal:** move faction data in and out for bulk editing or backup.
- **Steps:**
  1. CSV: open `Tools > Pixel Crushers > Love∕Hate > CSV Export∕Import...`, or right-click the FactionDatabase Inspector header → `CSV Export∕Import...`.
  2. JSON: right-click the FactionDatabase Inspector header → `Export JSON...` or `Import JSON...`.
- **Expected result:** a CSV/JSON file is written to (or read from) the chosen path; imported data replaces/updates the database contents.

## Verification

- The database asset exists and contains the expected factions (check `t:FactionDatabase` in Project search).
- Programmatic relationship check:

```csharp
using UnityEngine;
using UnityEditor;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var db = AssetDatabase.LoadAssetAtPath<FactionDatabase>(
            "Assets/FactionDatabase.asset"); // adjust path
        Faction guards = null, bandits = null;
        foreach (var f in db.factions)
        {
            if (f.name == "Guards") guards = f;
            if (f.name == "Bandits") bandits = f;
        }
        float aff = float.NaN;
        Relationship rel;
        if (guards != null && bandits != null &&
            guards.FindPersonalRelationship(bandits.id, out rel))
        {
            aff = rel.affinity;
        }
        result.Log("Guards->Bandits affinity = {0} (found={1})",
            aff, guards != null && bandits != null);
    }
}
```

- Exported CSV/JSON file is present on disk when export was requested.
- Console is clean after editing.

## API quick reference

| Entry point | Type | What it does |
| --- | --- | --- |
| `FactionDatabase` | ScriptableObject | Root data asset: `factions`, `personalityTraitDefinitions`, `relationshipTraitDefinitions`, `presets`. |
| `FactionDatabase.CreateNewFaction(string name, string description)` | method → int | Adds a faction, returns its unique ID. |
| `FactionDatabase.DestroyFaction(int id)` | method | Removes a faction by ID (overload by name exists). |
| `Faction` | [Serializable] | `id`, `name`, `parents[]`, `traits[]`, `relationships`, `percentJudgeParents`. |
| `Relationship` | [Serializable] | `factionID`, `inheritable`, `traits[]`; `affinity` = `traits[0]`. |
| `TraitDefinition` | [Serializable] | `name`, `description`, `minValue` (−100), `maxValue` (100). |
| `FactionInheritanceType` | enum | `Average` (default) or `Sum`. |
| `FactionDatabase.PlayerFactionID` | const int (= 0) | Reserved player faction ID. |

See `references/data-model.md` for the full trait/relationship math (alignment, inheritance) — read it when tuning custom traits or debugging unexpected affinity values.

## Common issues

- **Symptom:** Two factions look neutral even though you set a relationship. **Cause:** relationships are directed; you set only one direction, or the value is being averaged with inherited parent values. **Fix:** set both directions if mutual, and check parent inheritance / `traitInheritanceType`.
- **Symptom:** A faction unexpectedly "likes itself" at 100. **Cause:** self-affinity defaults to 100 by design. **Fix:** this is expected; only override if you need self-hostility.
- **Symptom:** `ExecuteMenuItem("Tools/Pixel Crushers/Love/Hate/CSV Export/Import...")` fails. **Cause:** used normal `/` instead of the division slash `∕` (U+2215). **Fix:** use the exact string `Tools/Pixel Crushers/Love∕Hate/CSV Export∕Import...`.
- **Symptom:** Trait sliders don't appear on factions. **Cause:** no `TraitDefinition` entries added. **Fix:** add personality trait definitions first.

## Boundaries

- The database defines *data*, not behavior. To make characters act on it, add faction members (`love-hate-faction-members`) and report deeds (`love-hate-deeds-and-reactions`).
- There is no built-in UI for players to view factions at runtime beyond debug helpers; build your own reputation UI.
- Trait values are hard-clamped to −100..100; wider ranges are not supported.
