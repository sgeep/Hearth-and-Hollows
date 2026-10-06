---
name: love-hate-gossip-and-greetings
description: "Use this skill whenever the user wants NPCs in a Love/Hate project to share rumors/opinions with each other or greet each other based on how they feel. E.g. 'make NPCs gossip about the player', 'let villagers spread rumors when they meet', 'guards should warn each other about the thief', 'NPCs should wave or scowl depending on if they like each other', 'set up greetings based on relationship'. Covers GossipTrigger / GossipTrigger2D (rumor sharing on proximity), GreetingTrigger / GreetingTrigger2D (affinity- and mood-based greetings), and their event handlers. Do NOT use for defining factions (see love-hate-faction-database), adding NPC components (see love-hate-faction-members), reporting actions (see love-hate-deeds-and-reactions), or saving (see love-hate-save-and-runtime-api). When in doubt whether an NPC 'spread the word', rumor, or 'greet based on relationship' request could involve Love/Hate, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Gossip and Greetings

Faction members don't only react to what they witness directly — they can spread rumors to each other and greet one another based on how they feel. When two members meet, gossip lets them exchange memories (so a crime witnessed by one guard becomes known to others), and greetings play affinity- and mood-appropriate animations. This skill lets an agent set up both.

## When to use this skill

- "Make NPCs gossip / spread rumors when they meet"
- "Guards should warn each other about the thief"
- "Villagers spread word about the player's reputation"
- "NPCs wave or scowl depending on whether they like each other"
- "Set up greetings based on relationship"

Not for:
- Defining factions → `love-hate-faction-database`
- Adding the FactionMember component → `love-hate-faction-members`
- The original witnessed actions that become rumors → `love-hate-deeds-and-reactions`
- Persisting shared knowledge → `love-hate-save-and-runtime-api`

## Prerequisites

- Love/Hate installed; a `FactionManager` in the scene; participating GameObjects have `FactionMember` components (see `love-hate-faction-members`).
- Triggers use physics colliders: 3D versions need a trigger `Collider` (and a Rigidbody on at least one party); 2D versions (`GossipTrigger2D`, `GreetingTrigger2D`) need trigger `Collider2D` + `Rigidbody2D` and the `USE_PHYSICS2D` define (see `love-hate-setup-and-overview`).
- Programmatic check:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        result.Log("GossipTrigger={0}, GreetingTrigger={1}",
            typeof(GossipTrigger) != null, typeof(GreetingTrigger) != null);
    }
}
```

- If the types are missing, Love/Hate is not installed — point to the Asset Store link and stop.

## Quick start

Goal: let two NPCs gossip when they get close.

1. On each NPC GameObject (already a `FactionMember`), add a `GossipTrigger` (3D) or `GossipTrigger2D` (2D).
2. Add a trigger collider covering the NPC's "social" radius, and ensure at least one party has a Rigidbody (3D) / Rigidbody2D (2D).
3. Optionally set `timeBetweenGossip` (default 300s) to throttle repeats.
4. Expected observable result: when two NPCs who like each other (mutual affinity > 0) enter each other's triggers, they exchange rumors via `ShareRumors`, and any `IGossipEventHandler` (e.g. `GossipAnimation`) fires.

## Workflows

### Workflow: Set up gossip

- **Goal:** propagate memories/opinions between members on contact.
- **Steps:**
  1. Add `GossipTrigger` (3D) or `GossipTrigger2D` (2D) to each member that should initiate gossip.
  2. Provide a trigger collider (the gossip range) and required Rigidbody.
  3. Tune `timeBetweenGossip` (default 300s).
  4. Rules enforced by the trigger: members only gossip if the initiator's affinity toward the other is **> 0**, and members do **not** gossip with the Player or Player-faction members.
  5. On gossip, both members call `ShareRumors` on each other, exchanging remembered deeds.
- **Expected result:** a rumor known to one member becomes known to the other; downstream that changes the recipient's affinities.

### Workflow: Add gossip animation/feedback

- **Goal:** play an animation or run logic when gossip happens.
- **Steps:**
  1. Add `GossipAnimation` to the member; set `triggerParameter` to an Animator trigger name.
  2. Alternatively add `FactionMemberEvents` and hook `onGossip` / `onShareRumors` to your own UnityEvents.
- **Expected result:** the Animator trigger fires (or your event runs) each time the member gossips.

### Workflow: Set up greetings

- **Goal:** play a greeting that matches how the members feel about each other.
- **Steps:**
  1. Add `GreetingTrigger` (3D) or `GreetingTrigger2D` (2D) to the member, with a trigger collider and Rigidbody.
  2. Tune `timeBetweenGreetings` (default 300s).
  3. Configure the `greetings` ranges (a `RangeAnimation[]`). Defaults split affinity into three bands: −100..−25 (hostile), −25..25 (neutral), 25..100 (friendly). Each range can also require a matching temperament.
  4. The trigger picks the greeting whose affinity range contains the current affinity and whose temperament matches the member's current PAD-derived temperament, then plays its Animator trigger.
- **Expected result:** friendly members play friendly greetings, hostile members play hostile ones; `IGreetEventHandler` fires.

## Verification

- Members have the intended trigger components and colliders.
- Programmatic component check:

```csharp
using UnityEngine;
using PixelCrushers.LoveHate;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int gossip = Object.FindObjectsByType<GossipTrigger>(FindObjectsSortMode.None).Length;
        int greet = Object.FindObjectsByType<GreetingTrigger>(FindObjectsSortMode.None).Length;
        result.Log("GossipTriggers={0}, GreetingTriggers={1} => {2}",
            gossip, greet, (gossip + greet > 0) ? "PASS" : "FAIL");
    }
}
```

- In Play mode, move two friendly NPCs together and confirm rumors transfer (affinity toward a third party changes on the recipient) and greeting animations play. Console clean.

## API quick reference

| Entry point | Type | What it does |
| --- | --- | --- |
| `GossipTrigger` / `GossipTrigger2D` | MonoBehaviour | On proximity, makes two members share rumors (affinity > 0 required; excludes player). |
| `GreetingTrigger` / `GreetingTrigger2D` | MonoBehaviour | On proximity, plays an affinity- and temperament-matched greeting. |
| `FactionMember.ShareRumors(FactionMember other)` | method | Transfers remembered deeds/opinions to another member. |
| `GossipAnimation` | MonoBehaviour (IGossipEventHandler) | Sets an Animator `triggerParameter` on gossip. |
| `FactionMemberEvents` | MonoBehaviour | `onGossip`, `onShareRumors`, `onGreet` UnityEvents. |
| `RangeAnimation` | [Serializable] | Affinity range + temperament + Animator trigger for a greeting. |

## Common issues

- **Symptom:** NPCs never gossip. **Cause:** initiator's affinity toward the other is ≤ 0, one party is in the Player faction, colliders aren't triggers, or no Rigidbody. **Fix:** ensure mutual positive affinity, non-player factions, trigger colliders, and a Rigidbody.
- **Symptom:** Gossip happens far too often. **Cause:** `timeBetweenGossip` too low. **Fix:** raise it (default 300s).
- **Symptom:** Greeting always plays the neutral animation. **Cause:** affinity sits in the −25..25 band, or temperament requirements don't match. **Fix:** verify affinity values and the configured `greetings` ranges/temperaments.
- **Symptom:** 2D triggers do nothing. **Cause:** `USE_PHYSICS2D` not defined or missing Collider2D/Rigidbody2D. **Fix:** enable 2D support and add the 2D physics components.

## Boundaries

- Gossip transfers *rumors* (remembered deeds); it does not invent new opinions. Members must first witness deeds (`love-hate-deeds-and-reactions`) for there to be anything to spread.
- The player is intentionally excluded from gossip; model player reputation via deeds instead.
- Greetings only play animations/events — they don't move NPCs together. Pair with your own locomotion/AI to bring members into range.
