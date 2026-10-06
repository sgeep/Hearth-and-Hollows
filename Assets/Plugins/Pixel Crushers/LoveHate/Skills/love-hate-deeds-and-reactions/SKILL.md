---
name: love-hate-deeds-and-reactions
description: "Use this skill whenever the user wants NPCs to perceive and emotionally react to actions in a Love/Hate project — the 'deeds' system. E.g. 'make NPCs get angry when the player steals', 'report a deed when the player attacks', 'guards should react when they witness a crime', 'set up good and bad actions that change reputation', 'define a deed template for helping villagers'. Covers reporting deeds via DeedReporter, authoring Deed Template Libraries, aura-based passive perception, witness/sight rules, and firing reaction events (DeedReactionEvents / FactionMemberEvents). Do NOT use for defining factions (see love-hate-faction-database), adding NPC components (see love-hate-faction-members), gossip/greetings (see love-hate-gossip-and-greetings), or saving (see love-hate-save-and-runtime-api). When in doubt whether a crime, reputation-change, or 'NPC reacts to an action' request could involve Love/Hate, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Report Deeds and React to Them

A "deed" is an action a faction member performs that others can witness and judge — stealing, helping, attacking, sharing food. When a deed is committed, nearby members who can perceive it evaluate it against their personality and relationships, adjust their PAD emotions and affinities, and optionally fire reaction events. This skill lets an agent report deeds, author reusable deed templates, set up passive "aura" perception, and wire reactions.

## When to use this skill

- "Make NPCs angry when the player steals / attacks"
- "Report a deed when X happens"
- "Guards should react when they witness a crime"
- "Define good/bad actions that change reputation"
- "Fire an event / animation when an NPC's opinion changes"
- "Set up an aura so NPCs feel uneasy near a scary object"

Not for:
- Creating the factions → `love-hate-faction-database`
- Adding the FactionMember/perception components → `love-hate-faction-members`
- Rumor spreading between NPCs → `love-hate-gossip-and-greetings`
- Saving reaction state → `love-hate-save-and-runtime-api`

## Prerequisites

- Love/Hate installed; a `FactionManager` in the scene; the acting and witnessing GameObjects have `FactionMember` components (see `love-hate-faction-members`).
- For 2D trigger-based aura, `USE_PHYSICS2D` must be defined (see `love-hate-setup-and-overview`).
- Programmatic check:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        bool reporter = typeof(DeedReporter) != null;
        bool lib = typeof(DeedTemplateLibrary) != null;
        result.Log("DeedReporter={0}, DeedTemplateLibrary={1}", reporter, lib);
    }
}
```

- If types are missing, Love/Hate is not installed — point to the Asset Store link and stop.

## Quick start

Goal: report a "stole" deed by the player against a target, so witnesses react.

1. Create a Deed Template Library: `Assets > Create > Pixel Crushers > Love∕Hate > Deed Template Library`.
2. In the library, add a template with `tag` = "stole", a negative `impact` (e.g. −50) and some `aggression` (e.g. 20). Both are −100..100.
3. On the actor GameObject (the player's `FactionMember`), add a `DeedReporter` and assign the Deed Template Library. Set its `dimension` (`Is3D` default, or `Is2D`).
4. From gameplay code, call `deedReporter.ReportDeed("stole", targetFactionMember);`.
5. Expected observable result: faction members within the deed's `radius` (default 10) who can perceive it evaluate the deed, their PAD/affinity toward the player shifts, and any `DeedReactionEvents` / `FactionMemberEvents` fire.

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var actor = GameObject.Find("Player").GetComponent<FactionMember>();
        var reporter = actor.GetComponent<DeedReporter>();
        var target = GameObject.Find("Merchant").GetComponent<FactionMember>();
        reporter.ReportDeed("stole", target);
        result.Log("Reported 'stole' deed by {0} against {1}", actor.name, target.name);
    }
}
```

## Workflows

### Workflow: Author a Deed Template Library

- **Goal:** define reusable action definitions so code only passes a tag.
- **Steps:**
  1. `Assets > Create > Pixel Crushers > Love∕Hate > Deed Template Library`.
  2. Add templates; per template set:
     - `tag` (string key used by `ReportDeed`)
     - `impact` (−100..100) — how good/bad the act is
     - `aggression` (−100..100) — how forceful; feeds the dominance dimension
     - `traits[]` — objective traits of the deed (aligned against witness personality)
     - `requiresSight` (default false) — witnesses must see it
     - `radius` (default 10) — perception radius
     - `permittedEvaluators` — `Everyone` (default), `OnlyTarget`, `EveryoneExceptTarget`
     - `minAffinityEffect` / `maxAffinityEffect` (default −100 / 100) — clamp the affinity change
     - `noRepeatDuration` (default 0) — suppress identical repeats within this window
- **Expected result:** the library lists your templates; `DeedReporter.ReportDeed(tag, target)` finds them by tag.

### Workflow: Report deeds from code

- **Goal:** fire a deed at the moment an action happens.
- **Steps:**
  1. Ensure the actor has both `FactionMember` and `DeedReporter` (the reporter requires a FactionMember).
  2. Call `ReportDeed(string tag, FactionMember target)` (reports as the host), or `ReportDeedByActor(FactionMember actor, string tag, FactionMember target)` to report on behalf of another member.
  3. Internally this builds a `Deed` from the matching template and calls `actor.factionManager.CommitDeed(...)`, which distributes it to eligible witnesses.
- **Expected result:** witnesses within radius who pass sight/evaluator checks update emotions and affinity.

### Workflow: Passive perception with Aura triggers

- **Goal:** make members feel something just by being near an object or character (no explicit deed).
- **Steps:**
  1. Add an `AuraTrigger` (3D) or `AuraTrigger2D` (2D) to a GameObject with a trigger collider. The object also needs a `Traits` component (defines the aura's personality traits).
  2. Set `impact` and `aggression` (−100..100) and `timeBetweenEffects` (default 300s cooldown per member).
  3. When a `FactionMember` enters, the aura computes trait `Alignment` and calls `ModifyPAD` on them (pleasure/arousal/dominance changes scaled by alignment).
- **Expected result:** members entering the aura shift emotionally; `IAuraEventHandler` / `IEnterAuraEventHandler` handlers on the member fire.

### Workflow: Wire reaction events

- **Goal:** run game logic/animation when a member reacts.
- **Steps:**
  1. Add `FactionMemberEvents` to a member to expose UnityEvents: `onModifyPad`, `onWitnessDeed`, `onRememberDeed`, `onForgetDeed`, `onShareRumors`, `onGossip`, `onGreet`.
  2. Or add `DeedReactionEvents` to fire `UnityEvent onReact` based on the pleasure change of a witnessed deed; it ships with 3 default ranges (−100..−25, −25..25, 25..100).
- **Expected result:** the assigned UnityEvents run when the member witnesses/reacts.

## Verification

- The Deed Template Library asset exists and contains the intended tags.
- Enter Play mode, report a deed, and confirm a witness's affinity changed:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var witness = GameObject.Find("Guard_01").GetComponent<FactionMember>();
        float before = witness.GetAffinity(FactionDatabase.PlayerFactionID);
        var reporter = GameObject.Find("Player").GetComponent<DeedReporter>();
        reporter.ReportDeed("stole",
            GameObject.Find("Merchant").GetComponent<FactionMember>());
        float after = witness.GetAffinity(FactionDatabase.PlayerFactionID);
        result.Log("Guard affinity before={0} after={1} => {2}",
            before, after, (after < before) ? "PASS (dropped)" : "check radius/sight/threshold");
    }
}
```

- Console shows no errors; assigned reaction UnityEvents fire.

## API quick reference

| Entry point | Type | What it does |
| --- | --- | --- |
| `DeedReporter.ReportDeed(string tag, FactionMember target)` | method | Reports a deed as the host member. |
| `DeedReporter.ReportDeedByActor(FactionMember actor, string tag, FactionMember target)` | method | Reports a deed on behalf of another member. |
| `DeedTemplateLibrary` | ScriptableObject | Collection of `DeedTemplate` definitions. |
| `DeedTemplate` | [Serializable] | `tag`, `impact`, `aggression`, `traits[]`, `requiresSight`, `radius`, `permittedEvaluators`, `minAffinityEffect`, `maxAffinityEffect`, `noRepeatDuration`. |
| `FactionManager.CommitDeed(FactionMember actor, Deed deed, bool requiresSight, Dimension dimension, float radius)` | method | Distributes a deed to witnesses (called for you by DeedReporter). |
| `AuraTrigger` / `AuraTrigger2D` | MonoBehaviour | Passive proximity emotional effect; needs a `Traits` component. |
| `FactionMemberEvents` | MonoBehaviour | UnityEvents for witness/remember/forget/pad/gossip/greet. |
| `DeedReactionEvents` | MonoBehaviour | Fires `onReact` by pleasure-change range. |
| `PermittedEvaluators` | enum | `Everyone`, `OnlyTarget`, `EveryoneExceptTarget`. |

See `references/deed-evaluation.md` for how impact, aggression, trait alignment, sight, power difference, and memory thresholds combine — read it when reactions are weaker/stronger than expected.

## Common issues

- **Symptom:** No one reacts to a reported deed. **Cause:** witnesses out of `radius`, `requiresSight` true but line of sight blocked, or `impact` below the witness's `deedImpactThreshold` (default 5). **Fix:** increase radius/impact, clear sight, or lower the threshold.
- **Symptom:** `ReportDeed` does nothing and logs nothing. **Cause:** no template matches the `tag`, or the `DeedReporter` has no library assigned. **Fix:** verify the tag exists in the assigned Deed Template Library.
- **Symptom:** A repeated deed only counts once. **Cause:** `noRepeatDuration` is suppressing repeats. **Fix:** set it to 0 or a shorter window.
- **Symptom:** Only the target reacts. **Cause:** `permittedEvaluators` is `OnlyTarget`. **Fix:** set it to `Everyone`.
- **Symptom:** 2D aura never triggers. **Cause:** `USE_PHYSICS2D` not defined, or no trigger collider / Rigidbody2D. **Fix:** enable 2D support and add the collider setup.

## Boundaries

- Deeds change emotions and affinities; they do not directly drive AI actions. Read the resulting affinity/temperament to decide behavior in your own AI.
- The evaluation model (impact/aggression/alignment/power) is built in; you can override via the FactionMember evaluation delegates but not replace the pipeline wholesale from these skills.
- Aura effects modify PAD, not long-term affinity, unless combined with deeds.
