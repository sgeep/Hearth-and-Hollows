# Love/Hate data model reference

Read this when tuning custom traits, configuring inheritance, or debugging unexpected affinity values. All facts here are verified against Love/Hate 1.10.73 source.

## Value ranges

- Every personality trait and relationship trait value is clamped to **−100 .. 100**.
- `TraitDefinition.minValue` defaults to −100, `maxValue` to 100.

## Affinity

- Affinity is a relationship trait, always at **index 0** (`Relationship.AffinityTraitIndex = 0`, `Relationship.AffinityTraitName = "Affinity"`).
- Every database has a required "Affinity" relationship trait (seeded on creation).
- `Relationship.affinity` is a property that gets/sets `traits[0]`.
- Default relationship value between two different factions is **0** (neutral).
- Default self-affinity (a faction's relationship trait toward itself, index 0) is **100** (`Relationship.GetDefaultValue`).

## Factions and inheritance

- `Faction` fields: `id`, `name`, `description`, `color` (gizmo color index), `parents[]` (direct parent IDs), `traits[]` (personality trait values), `relationships` (List<Relationship>), `percentJudgeParents`.
- `percentJudgeParents` (default 0) scales how much a relationship change toward this faction also propagates up to its parents.
- `FactionDatabase.traitInheritanceType` and `relationshipInheritanceType` are `FactionInheritanceType` — `Average` (default) or `Sum`.
- With `Average`, a faction's effective trait/relationship (when it has no personal value) is the average across its parents; with `Sum`, values are summed.

## Trait alignment

- `Traits.Alignment(float[] a, float[] b)` returns a similarity in **0..1**, computed per trait as `1 - (Mathf.Abs(a[i] - b[i]) / 200)` and combined across traits.
- Alignment is used by aura effects and by faction members to decide how strongly they identify with an actor or object's traits.

## Presets

- `Preset` = `name`, `description`, `traits[]`. Presets stamp a full trait profile onto a faction quickly.

## Key relationship methods on Faction

- `bool FindPersonalRelationship(int factionID, out Relationship relationship)`
- `float GetPersonalRelationshipTrait(int factionID, int traitID)`
- `void SetPersonalRelationshipTrait(int factionID, int traitID, float value, int numTraits)`
- `void SetPersonalRelationshipInheritable(int factionID, bool inheritable)`
- `bool HasDirectParent(int parentID)`, `void AddDirectParent(int parentID)`, `void RemoveDirectParent(int parentID)`

## Runtime affinity query

- `FactionManager.instance.GetAffinity(int judgeFactionID, int subjectFactionID)` returns the effective affinity, folding in inheritance. Prefer this over reading raw relationship arrays at runtime.
