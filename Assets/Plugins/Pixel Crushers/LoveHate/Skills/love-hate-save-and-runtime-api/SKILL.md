---
name: love-hate-save-and-runtime-api
description: "Use this skill whenever the user wants to persist Love/Hate relationship state across sessions or scenes, or query/modify factions and affinities from C# at runtime. E.g. 'save the NPC relationships', 'make reputation persist between sessions', 'why do my faction changes reset on reload', 'get the affinity between two factions in code', 'change an NPC's opinion from a script', 'reset all relationships on new game'. Covers FactionMemberSaver / FactionManagerSaver (Save System integration), runtime queries via FactionManager and FactionMember, and serialization/reset. Do NOT use for authoring factions (see love-hate-faction-database), adding NPCs (see love-hate-faction-members), reporting deeds (see love-hate-deeds-and-reactions), or gossip/greetings (see love-hate-gossip-and-greetings). When in doubt whether a 'save/load relationships', 'persist reputation', or 'read affinity in code' request could involve Love/Hate, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Save State and Use the Runtime API

Love/Hate's relationships, emotions, and memories change during play. This skill covers two related jobs: persisting that runtime state across save/load and scene changes using the bundled Pixel Crushers Save System, and reading or modifying factions and affinities directly from C# at runtime.

## When to use this skill

- "Save / load NPC relationships"
- "Make reputation persist between sessions"
- "My faction changes reset on reload — fix it"
- "Reset all relationships when starting a new game"
- "Get the affinity between two factions / members in code"
- "Change an NPC's opinion of the player from a script"

Not for:
- Editing factions in the editor → `love-hate-faction-database`
- Adding NPC components → `love-hate-faction-members`
- Reporting actions → `love-hate-deeds-and-reactions`
- Rumor/greeting triggers → `love-hate-gossip-and-greetings`

## Prerequisites

- Love/Hate installed; a `FactionManager` in the scene with a database assigned; NPCs have `FactionMember` components.
- Saving uses the Pixel Crushers **Common** Save System (bundled with Love/Hate). A `SaveSystem` component must be present in the scene for save/load workflows.
- Programmatic check:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool mgr = typeof(FactionManager) != null;
        bool saver = typeof(FactionMemberSaver) != null;
        bool saveSys = System.Type.GetType(
            "PixelCrushers.SaveSystem, Assembly-CSharp") != null;
        result.Log("FactionManager={0}, FactionMemberSaver={1}, SaveSystem type={2}",
            mgr, saver, saveSys);
    }
}
```

- If Love/Hate types are missing, point to the Asset Store link and stop.

## Quick start

Goal: query the affinity between two factions at runtime.

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var fm = FactionManager.instance;
        if (fm == null) { result.LogError("No FactionManager in scene."); return; }
        // Look up faction IDs by name, then query affinity:
        var guards = fm.factionDatabase.GetFaction("Guards");
        var bandits = fm.factionDatabase.GetFaction("Bandits");
        if (guards != null && bandits != null)
        {
            float aff = fm.GetAffinity(guards.id, bandits.id);
            result.Log("Guards->Bandits affinity = {0}", aff);
        }
    }
}
```

Expected observable result: the Console logs the effective affinity (−100..100), with faction inheritance folded in.

## Workflows

### Workflow: Persist a member's relationships (FactionMemberSaver)

- **Goal:** save/restore a single NPC's faction ID, PAD state, and memories.
- **Steps:**
  1. Ensure the scene has a `SaveSystem` (Pixel Crushers Common). Add via the Save System's setup menu if absent.
  2. Add a `FactionMemberSaver` component to the same GameObject as the `FactionMember`. Give it a stable, unique `Key` so it can be matched on load.
  3. Save/load via the Save System, e.g. `PixelCrushers.SaveSystem.SaveToSlot(1);` and `PixelCrushers.SaveSystem.LoadFromSlot(1);`.
  4. Internally the saver's `RecordData()` calls `member.SerializeToString()` and `ApplyData()` calls `member.DeserializeFromString()`.
- **Expected result:** after load, the member's affinities, emotions, and memories match what they were at save time.

### Workflow: Persist global faction state (FactionManagerSaver)

- **Goal:** save/restore the whole runtime faction database (all group-level relationships).
- **Steps:**
  1. Add a `FactionManagerSaver` to the Faction Manager GameObject with a unique `Key`.
  2. It records `manager.SerializeToString()` and restores via `DeserializeFromString()`.
  3. On new game, `OnRestartGame()` calls `manager.ResetAll()` to clear runtime modifications back to the source database.
- **Expected result:** group-level relationship changes survive save/load; a new game resets them.

### Workflow: Modify relationships from code

- **Goal:** change opinions at runtime.
- **Steps:**
  ```csharp
  using UnityEngine;
  using PixelCrushers.LoveHate;

  internal class CommandScript : IRunCommand
  {
      public void Execute(ExecutionResult result)
      {
          var guard = GameObject.Find("Guard_01").GetComponent<FactionMember>();
          result.RegisterObjectModification(guard);
          // Warm the guard toward the player:
          guard.ModifyPersonalAffinity(FactionDatabase.PlayerFactionID, +25f);
          // Or set an absolute value:
          guard.SetPersonalAffinity(FactionDatabase.PlayerFactionID, 60f);
          // Nudge emotions directly:
          guard.ModifyPAD(0f, +10f, +5f, 0f); // happiness, pleasure, arousal, dominance
      }
  }
  ```
- **Expected result:** `guard.GetAffinity(FactionDatabase.PlayerFactionID)` reflects the change immediately.

### Workflow: Reset all relationships (new game)

- **Goal:** discard runtime changes and return to the authored database.
- **Steps:** call `FactionManager.instance` reset via a `FactionManagerSaver.OnRestartGame()` on restart, or trigger a Save System restart. The manager rebuilds its runtime database clone from the source asset.
- **Expected result:** all runtime relationship/emotion drift is cleared.

## Verification

- Runtime query returns expected values:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var guard = GameObject.Find("Guard_01").GetComponent<FactionMember>();
        float before = guard.GetAffinity(FactionDatabase.PlayerFactionID);
        guard.ModifyPersonalAffinity(FactionDatabase.PlayerFactionID, +5f);
        float after = guard.GetAffinity(FactionDatabase.PlayerFactionID);
        result.Log("affinity before={0} after={1} => {2}",
            before, after, Mathf.Approximately(after, before + 5f) ? "PASS" : "check clamping");
    }
}
```

- For persistence: members carry `FactionMemberSaver` (and the manager a `FactionManagerSaver`) each with a unique `Key`; a `SaveSystem` exists; save then load restores values.
- Console clean.

## API quick reference

| Entry point | Type | What it does |
| --- | --- | --- |
| `FactionManager.instance` | static property | The active scene manager. |
| `FactionManager.GetFaction(int id)` | method → Faction | Runtime faction lookup. |
| `FactionManager.GetAffinity(int judgeID, int subjectID)` | method → float | Effective affinity (inheritance folded in). |
| `FactionMember.GetAffinity(int/string/FactionMember)` | method → float | A member's affinity toward a subject. |
| `FactionMember.SetPersonalAffinity(int, float)` / `ModifyPersonalAffinity(int, float)` | method | Set/adjust personal affinity. |
| `FactionMember.SerializeToString()` / `DeserializeFromString(string)` | method | Save/restore member state. |
| `FactionMemberSaver` | MonoBehaviour (Saver) | Persists one member via the Save System. |
| `FactionManagerSaver` | MonoBehaviour (Saver) | Persists global faction database state; resets on new game. |

See `references/persistence.md` for Save System setup details and the exact save/reset lifecycle — read it when integrating with an existing save architecture.

## Common issues

- **Symptom:** Faction/relationship changes reset every time you enter Play mode. **Cause:** this is expected — the manager clones the database into a runtime instance on Awake so the source asset is never mutated. **Fix:** use savers to persist runtime state; don't expect edits to write back to the asset.
- **Symptom:** Saved relationships don't restore. **Cause:** missing or non-unique saver `Key`, or no `SaveSystem` in the scene. **Fix:** give each saver a stable unique Key and add a SaveSystem.
- **Symptom:** New game keeps old grudges. **Cause:** `ResetAll()` / restart flow not invoked. **Fix:** route new-game through the Save System restart so `FactionManagerSaver.OnRestartGame()` runs.
- **Symptom:** Affinity change is smaller than requested. **Cause:** values clamp to −100..100. **Fix:** expected behavior.

## Boundaries

- Persistence depends on the bundled Pixel Crushers Common Save System. Integrating a third-party save framework is out of scope; bridge it to the Save System's saver model.
- The runtime database is a clone — editing it at runtime never changes the source `.asset`. Author permanent data with `love-hate-faction-database`.
- Serialized strings are Love/Hate's own format; do not hand-edit them.
