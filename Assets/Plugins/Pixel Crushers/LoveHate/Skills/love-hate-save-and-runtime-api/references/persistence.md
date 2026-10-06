# Persistence reference

Read this when integrating Love/Hate saving with an existing save architecture. Verified against Love/Hate 1.10.73 source.

## Runtime database cloning

- `FactionManager` holds a `factionDatabase` reference (the source asset).
- On `Awake`, it instantiates a **runtime clone** of that database. All runtime relationship/trait changes happen on the clone, so the source `.asset` is never modified. This is why editor-authored data "resets" between play sessions — by design.

## Savers (Pixel Crushers Common Save System)

Both savers derive from the Common `Saver` base class and require a scene `SaveSystem`.

### FactionMemberSaver
- Attach to the same GameObject as a `FactionMember`.
- `RecordData()` returns `member.SerializeToString()` — encodes faction ID, PAD state, and memory (rumors).
- `ApplyData(string s)` calls `member.DeserializeFromString(s)`.
- Needs a stable unique `Key` (from the Saver base) so the saved record is matched to the right member on load.

### FactionManagerSaver
- Attach to the Faction Manager GameObject.
- `RecordData()` / `ApplyData()` map to `manager.SerializeToString()` / `DeserializeFromString()` — persists group-level relationships across the runtime database.
- `OnRestartGame()` calls `manager.ResetAll()` to clear runtime modifications back to the source database (new-game behavior).

## Typical save/load calls

```csharp
using PixelCrushers;

SaveSystem.SaveToSlot(1);
SaveSystem.LoadFromSlot(1);
SaveSystem.RestartGame("YourStartScene"); // triggers OnRestartGame on savers
```

Note: `SaveSystem.isOnlyChangingScene` (added in 1.10.72/1.10.73) is available to distinguish a scene change from a full load if you need custom logic.

## Checklist for working persistence

1. A single `SaveSystem` exists in the scene (or persists across scenes).
2. Each `FactionMember` that must persist has a `FactionMemberSaver` with a unique `Key`.
3. The Faction Manager has a `FactionManagerSaver` with a unique `Key` if group-level relationships change at runtime.
4. New game is routed through `SaveSystem.RestartGame(...)` so `ResetAll()` runs.

## Order-of-operations gotcha

- If `maxMemories` on a member is lowered after a save was created, deserialized memory lists may briefly exceed the new cap. Clearing/regenerating save slots after changing memory settings avoids stale oversized lists.
