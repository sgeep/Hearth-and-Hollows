---
name: love-hate-faction-members
description: "Use this skill whenever the user wants to turn a character, NPC, creature, or the player into a living participant in the Love/Hate social simulation. E.g. 'make this NPC part of the guards faction', 'add the faction member component', 'give my character personality traits and emotions', 'let this NPC see and remember things', 'set up an NPC that can hold a grudge'. Covers adding the FactionMember component, assigning a faction, configuring PAD emotions, traits, memory, sight, and querying or modifying affinity from code. Do NOT use for authoring the faction list (see love-hate-faction-database), reporting deeds (see love-hate-deeds-and-reactions), gossip/greetings (see love-hate-gossip-and-greetings), or saving (see love-hate-save-and-runtime-api). When in doubt whether an NPC-personality, NPC-emotion, or NPC-relationship request could involve Love/Hate, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Add Faction Members to Characters

A `FactionMember` is what turns an ordinary GameObject into an NPC (or the player) inside the Love/Hate simulation. It gives the character a faction identity, a PAD emotional state (Pleasure/Arousal/Dominance plus Happiness), personality traits, a memory of witnessed deeds, and sight-based perception. This skill lets an agent add and configure faction members and query or change their feelings from other scripts.

## When to use this skill

- "Make this NPC a member of the Guards faction"
- "Add the FactionMember component to my character"
- "Give this character personality traits / emotions"
- "Let this NPC see, witness, and remember events"
- "Set how long this NPC holds a grudge (memory duration)"
- "Get/modify how much this NPC likes the player from code"

Not for:
- Defining the factions themselves → `love-hate-faction-database`
- Reporting actions that members react to → `love-hate-deeds-and-reactions`
- Gossip/greeting triggers → `love-hate-gossip-and-greetings`
- Persistence → `love-hate-save-and-runtime-api`

## Prerequisites

- Love/Hate installed and a `FactionManager` present in the scene with a Faction Database assigned (see `love-hate-setup-and-overview`).
- The faction the member will belong to must exist in the database (see `love-hate-faction-database`).
- Programmatic check that a member type and a manager are available:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool typeOk = typeof(FactionMember) != null;
        var mgr = Object.FindFirstObjectByType<FactionManager>();
        result.Log("FactionMember available={0}, FactionManager in scene={1}",
            typeOk, mgr != null);
        if (mgr == null)
            result.LogWarning("Add a Faction Manager first (GameObject > Love/Hate > Faction Manager).");
    }
}
```

- If Love/Hate is not installed, point the user to the Asset Store link and stop.

## Quick start

Goal: make an existing character GameObject a member of an existing faction.

1. Select the character GameObject in the scene.
2. Add the `FactionMember` component (Add Component → search "Faction Member").
3. Assign the `Faction Database` field (the same asset the Faction Manager uses) so the Inspector can show the faction dropdown.
4. Set `Faction ID` to the desired faction.
5. Expected observable result: the GameObject has a `FactionMember` component with a chosen faction. At runtime it registers with `FactionManager.instance` and can witness deeds and hold affinities.

Programmatic add:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var go = GameObject.Find("Guard_01"); // adjust
        var member = go.GetComponent<FactionMember>();
        if (member == null) member = go.AddComponent<FactionMember>();
        result.RegisterObjectModification(member);
        member.factionID = 1; // the Guards faction ID from the database
        result.Log("Added/updated FactionMember on {0}, factionID={1}", go, member.factionID);
    }
}
```

## Workflows

### Workflow: Configure emotions and personality

- **Goal:** tune how the NPC feels and how strongly its personality colors reactions.
- **Steps:**
  1. On the `FactionMember`, the `pad` field holds `happiness`, `pleasure`, `arousal`, `dominance` (each −100..100), plus `excitabilityThreshold` (default 20).
  2. Set `impressionability` (0..100, default 0) — how fast the member's own personality shifts from experiences.
  3. Set `traitAlignmentImportance` (0..100, default 50) — how much shared personality traits sway reactions.
  4. Set `arousalImportance` (0..100, default 50) — how much arousal amplifies reactions.
- **Expected result:** the member's temperament (derived from PAD) and reaction strength change accordingly. Add a `StabilizePAD` component if you want emotions to drift back toward baseline over time.

### Workflow: Configure memory

- **Goal:** control how much and how long an NPC remembers.
- **Steps:**
  1. `deedImpactThreshold` (default 5) — deeds weaker than this are not remembered.
  2. `maxMemories` (default 50) — cap on stored memories.
  3. `shortTermMemoryDuration` (default 300s) and `longTermMemoryDuration` (default 3600s).
  4. `memoryCleanupFrequency` (default 2s) — how often expired memories are purged; raise the interval on many NPCs to reduce cost.
- **Expected result:** the NPC retains significant events for the configured durations and discards trivial or expired ones.

### Workflow: Configure sight/perception

- **Goal:** control what the member can visually witness.
- **Steps:**
  1. Assign `eyes` (a Transform). If left null, it auto-binds to the Animator's head bone; on non-humanoid rigs it falls back to the transform root.
  2. Set `sightLayerMask` (default layer 1 / Default) to define which layers block line of sight.
  3. For advanced field-of-view and multi-height raycasts, add the `CanSeeAdvanced` component, which overrides the member's `CanSee` delegate.
- **Expected result:** the member only witnesses deeds it can actually see when a deed `requiresSight`.

### Workflow: Query and modify affinity from code

- **Goal:** read or change how a member feels about a faction or another member.
- **Steps:**
  ```csharp
  using UnityEngine;
  using PixelCrushers.LoveHate;

  internal class CommandScript : IRunCommand
  {
      public void Execute(ExecutionResult result)
      {
          var member = GameObject.Find("Guard_01").GetComponent<FactionMember>();
          float towardPlayer = member.GetAffinity(FactionDatabase.PlayerFactionID);
          result.Log("Guard affinity toward player = {0}", towardPlayer);

          result.RegisterObjectModification(member);
          member.ModifyPersonalAffinity(FactionDatabase.PlayerFactionID, +10f); // warms up
          // Or set directly:
          // member.SetPersonalAffinity(FactionDatabase.PlayerFactionID, 50f);
      }
  }
  ```
  Available overloads: `GetAffinity(int subjectFactionID)`, `GetAffinity(string subjectFactionName)`, `GetAffinity(FactionMember subject)`.
- **Expected result:** the returned value (−100..100) reflects the member's current feeling; modify calls change it immediately.

## Verification

- The character GameObject has a `FactionMember` with the intended `factionID`.
- Programmatic scene check:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var members = Object.FindObjectsByType<FactionMember>(FindObjectsSortMode.None);
        result.Log("FactionMembers in scene: {0}", members.Length);
        foreach (var m in members)
            result.Log("  {0}: factionID={1}", m.name, m.factionID);
        result.Log(members.Length > 0 ? "PASS" : "FAIL");
    }
}
```

- Console is clean in Play mode (no "FactionManager not found" errors).
- Optionally add a `FactionMemberDebugger` / `FactionMemberDebuggerCanvas` to visualize the member's state at runtime.

## API quick reference

| Entry point | Type | What it does |
| --- | --- | --- |
| `FactionMember` | MonoBehaviour | Core NPC component: `factionID`, `pad`, memory, sight, affinity API. |
| `FactionMember.GetAffinity(int/string/FactionMember)` | method → float | Current affinity toward a subject (−100..100). |
| `FactionMember.SetPersonalAffinity(int, float)` | method | Sets personal affinity toward a faction. |
| `FactionMember.ModifyPersonalAffinity(int, float)` | method | Adds to personal affinity. |
| `FactionMember.ModifyPAD(float h, float p, float a, float d)` | method | Adjusts the emotional state. |
| `FactionMember.SwitchFaction(int newFactionID)` | method | Moves the member to another faction. |
| `Pad` | [Serializable] | `happiness`, `pleasure`, `arousal`, `dominance`, `excitabilityThreshold`; `GetTemperament()`. |
| `StabilizePAD` | MonoBehaviour | Eases PAD values back toward targets over time. |
| `CanSeeAdvanced` | MonoBehaviour | Custom FOV + multi-height sight override. |

## Common issues

- **Symptom:** NPC never reacts to anything. **Cause:** no `FactionManager` in the scene, or the member's faction ID isn't in the database. **Fix:** add a manager and verify the faction exists.
- **Symptom:** NPC witnesses events through walls, or never sees anything. **Cause:** `eyes` fell back to the root transform, or `sightLayerMask` excludes/includes the wrong layers. **Fix:** assign an eyes Transform at head height and set the mask to include occluders.
- **Symptom:** Frame hitches with many NPCs. **Cause:** frequent memory cleanup. **Fix:** increase `memoryCleanupFrequency` interval and/or lower `maxMemories`; scale dynamically via LOD.
- **Symptom:** Emotions never calm down. **Cause:** no stabilization. **Fix:** add a `StabilizePAD` component.

## Boundaries

- FactionMember simulates perception, emotion, memory, and affinity — it does not move, animate, or path the character. Drive locomotion/animation from your own controller, reading affinity/temperament to decide behavior.
- Members must reference the same database the Faction Manager uses; per-scene divergent databases are not supported.
- Sight uses simple raycasts (or `CanSeeAdvanced`); it is not a full stealth/vision system.
