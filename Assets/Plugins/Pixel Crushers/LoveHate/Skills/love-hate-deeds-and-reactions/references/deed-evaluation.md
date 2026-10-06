# Deed evaluation reference

Read this when a witnessed deed produces weaker or stronger reactions than expected. Verified against Love/Hate 1.10.73 source.

## Flow of a reported deed

1. `DeedReporter.ReportDeed(tag, target)` (or `ReportDeedByActor`) looks up a `DeedTemplate` by `tag` in the assigned `DeedTemplateLibrary`.
2. It builds a runtime `Deed` (pooled via `Deed.GetNew()` to avoid GC) and calls `actor.factionManager.CommitDeed(actor, deed, requiresSight, dimension, radius)`.
3. `FactionManager` finds candidate witnesses within `radius` and, over several frames (`witnessesPerUpdate`, default 60), calls `FactionMember.WitnessDeed(deed, actor, requiresSight, dimension)` on each.
4. Each witness evaluates the deed and, if memorable, stores it as a rumor and adjusts PAD + affinity.

## What a DeedTemplate carries

- `tag` — lookup key.
- `impact` (−100..100) — base good/bad magnitude.
- `aggression` (−100..100) — forcefulness; contributes to the dominance dimension.
- `traits[]` — objective personality traits of the deed, aligned against the witness's own traits.
- `requiresSight` (default false) — if true, only witnesses whose `CanSee` passes react.
- `radius` (default 10) — perception distance.
- `permittedEvaluators` — `Everyone` (default) / `OnlyTarget` / `EveryoneExceptTarget`.
- `minAffinityEffect` / `maxAffinityEffect` (default −100 / 100) — clamp the resulting affinity change.
- `noRepeatDuration` (default 0) — identical deeds within this window are silently discarded.

## Factors that scale a witness's reaction

- **deedImpactThreshold** (FactionMember, default 5): deeds weaker than this are not memorable — the member effectively ignores them.
- **traitAlignmentImportance** (0..100, default 50): how much `Traits.Alignment(deed.traits, witness.traits)` sways the reaction. Alignment is `1 - |a−b|/200` per trait, range 0..1.
- **arousalImportance** (0..100, default 50): how much the witness's arousal amplifies the reaction.
- **impressionability** (0..100, default 0): how much witnessing shifts the witness's own personality traits.
- **acclimatizationCurve** (default keys (0,1)→(20,0)): repeated similar deeds have diminishing effect.
- **powerDifferenceCurve** (default keys (−10,0),(1,0.1),(10,1)): relative power between actor and witness scales dominance response.
- **Existing affinity toward the actor**: a witness who already likes the actor judges the same deed differently than one who dislikes them.

## Aura effect math (AbstractAuraTrigger)

For a member entering an aura:
- `alignment = Traits.Alignment(auraTraits, memberFactionTraits)`
- `pleasureChange = alignment * impact`
- `arousalChange = max(-alignment * impact, -member.pad.arousal)`
- `dominanceChange = alignment * (aggression / 100) * impact`
- Applied via `member.ModifyPAD(0, pleasureChange, arousalChange, dominanceChange)`.
- `timeBetweenEffects` (default 300s) throttles repeat effects per member.

## Reaction events

- `FactionMemberEvents`: `onModifyPad`, `onWitnessDeed`, `onRememberDeed`, `onForgetDeed`, `onShareRumors`, `onGossip`, `onGreet`.
- `DeedReactionEvents.reactions[]`: default 3 pleasure-change ranges (−100..−25, −25..25, 25..100), each firing a `UnityEvent onReact`.
