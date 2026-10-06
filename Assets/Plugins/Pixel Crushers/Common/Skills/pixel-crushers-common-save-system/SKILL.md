---
name: pixel-crushers-common-save-system
description: "Use this skill whenever someone asks 'how do I save the game', 'how do I load a save file', 'save to slot', 'load from slot', 'auto-save between scenes', 'scene transitions with save data', 'persist player position', 'keep enemy state across scenes', 'serialize game data', 'add a saver component', 'set up a storer', 'PlayerPrefs save', 'disk save file', 'save file not found', 'SaveSystem null error', 'unique saver keys', 'SpawnedObject pooling', 'scene portal', 'spawnpoint on load', or any request to persist and restore gameplay state using the save and load pipeline in the Pixel Crushers Common Code Library. Covers save/load lifecycle, Saver components, data serializers, saved-game storers, scene portals, auto-save, SpawnedObject management, and code-driven save/load calls. Do NOT use for general project setup or namespace questions (see pixel-crushers-common-setup-and-overview). When in doubt whether a save/load request could involve this asset, use this skill — the Prerequisites section shows how to confirm the asset is installed."
metadata:
  asset: "Pixel Crushers Common"
  publisher: "Pixel Crushers"
  asset-version: "1.10.73"
  skill-version: "1.0.0"
  unity: "2022.3+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/behavior-ai"
  support-url: "https://www.pixelcrushers.com/support/"
  last-verified: "2026-08-21"
---

# Pixel Crushers Common: Save System

The Save System persists and restores gameplay state across scenes and sessions using a component-driven pipeline: `Saver` components record data, a `DataSerializer` converts it to/from a string, and a `SavedGameDataStorer` writes and reads from a backend (PlayerPrefs or disk). Exactly one `SaveSystem` object (DontDestroyOnLoad) coordinates the pipeline. Always add the wrapper component `Pixel Crushers/Save System/Save System` in scenes and call `PixelCrushers.SaveSystem` static methods from code.

## When to use this skill

Use this skill for any request involving saving or loading game state, persisting GameObjects across scenes, configuring serializers or storers, handling scene transitions with spawnpoints, auto-saving, wiring save/load to UI buttons without code, managing dynamically spawned objects, or troubleshooting save-related errors in the Pixel Crushers Common library.

## Prerequisites

**Install check** (iterate assemblies — `Type.GetType` alone will not work):

```csharp
using System;

bool IsInstalled()
{
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        if (asm.GetType("PixelCrushers.SaveSystem") != null) return true;
    return false;
}
```

Confirm `Assets/Plugins/Pixel Crushers/Common` exists and `Tools > Pixel Crushers > Common > Save System` submenu is present. If missing, see `pixel-crushers-common-setup-and-overview`.

**Required scene objects (exactly one of each on a persistent GameObject):**
- `SaveSystem` wrapper component
- A `DataSerializer` component (Json or Binary)
- A `SavedGameDataStorer` component (Disk or PlayerPrefs)

## Quick start

1. `GameObject > Create Empty` → rename it **Save System**.
2. `Add Component > Pixel Crushers/Save System/Save System`.
3. `Add Component > Pixel Crushers/Save System/Data Serializers/Json Data Serializer`.
4. `Add Component > Pixel Crushers/Save System/Saved Game Data Storers/Disk Saved Game Data Storer`.
5. On your target GameObject: `Add Component > Pixel Crushers/Save System/Savers/Position Saver`.
6. `Tools > Pixel Crushers > Common > Save System > Assign Unique Keys...` → click **Assign**.
7. Enter Play mode, then call `PixelCrushers.SaveSystem.SaveToSlot(1)`.

## Workflows

### Workflow: Set up the Save System rig

**Goal:** Create a persistent Save System that survives scene loads.

**Steps:**
1. `GameObject > Create Empty`, rename it **Save System**.
2. `Add Component > Pixel Crushers/Save System/Save System`. The core `PixelCrushers.SaveSystem` static class is now active.
3. Add a serializer. For production use `Json Data Serializer` (`Pixel Crushers/Save System/Data Serializers/Json Data Serializer`). Use `Binary Data Serializer` only if payload size is critical — see [Savers and Serializers reference](references/savers-and-serializers.md) for caveats.
4. Add a storer. Use `Disk Saved Game Data Storer` (`Pixel Crushers/Save System/Saved Game Data Storers/Disk Saved Game Data Storer`) for shipping games. Use `PlayerPrefs Saved Game Data Storer` for quick prototypes or WebGL.
5. On `DiskSavedGameDataStorer`, set **Store Save Files In** to `PersistentDataPath` (default). Enable **Encrypt** and change **Encryption Password** before shipping.
6. The Save System GameObject is automatically DontDestroyOnLoad. Add it only to the first scene that loads, not to every scene.

**Expected result:** Play mode starts with no Console errors; `PixelCrushers.SaveSystem.hasInstance` returns `true`.

---

### Workflow: Make a GameObject save its state

**Goal:** Persist and restore a component's data (e.g., player position, active state) across sessions.

**Steps:**
1. Select the target GameObject.
2. `Add Component > Pixel Crushers/Save System/Savers/Position Saver` (or another Saver — see [reference](references/savers-and-serializers.md) for the full catalogue).
3. In the Inspector: either set a meaningful **Key** manually, or leave it blank and tick **Append Saver Type To Key**.
4. Run `Tools > Pixel Crushers > Common > Save System > Assign Unique Keys...` → click **Assign**. This populates every empty Key in the project with a guaranteed-unique value.
5. Leave **Save Across Scene Changes** enabled (default `true`) to retain data during scene transitions.

**Expected result:** After `SaveToSlot(1)` and `LoadFromSlot(1)`, the GameObject returns to the saved position with no duplicate-key warnings in Console.

---

### Workflow: Save and load from code

**Goal:** Trigger save/load programmatically from a menu script, checkpoint, or game event.

**Steps:**

```csharp
using PixelCrushers;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    private void Start()
    {
        // Subscribe before loading so you can react when data is applied.
        SaveSystem.saveDataApplied += OnDataApplied;
    }

    public void Save() => SaveSystem.SaveToSlot(1);

    public void Load()
    {
        if (SaveSystem.HasSavedGameInSlot(1))
            SaveSystem.LoadFromSlot(1);
        else
            Debug.LogWarning("No save found in slot 1.");
    }

    public void Delete() => SaveSystem.DeleteSavedGameInSlot(1);

    private void OnDataApplied()
    {
        // Safe to read restored state here — not immediately after LoadFromSlot().
        Debug.Log("State restored.");
    }
}
```

> Do not read restored state in the same frame as `LoadFromSlot` — data is applied after `SaveSystem.framesToWaitBeforeApplyData` frames. Subscribe to `SaveSystem.saveDataApplied` instead.

**Expected result:** `HasSavedGameInSlot(1)` returns `true` after save; `saveDataApplied` event fires after load with no errors.

---

### Workflow: Wire save/load to UI buttons without code

**Goal:** Connect save/load actions to UI buttons using only the Inspector, with zero custom scripting.

**Steps:**
1. On the Save System GameObject: `Add Component > Pixel Crushers/Save System/Misc/Save System Methods`.
2. Set **Default Starting Scene Name** (used by `LoadOrRestart` when no save file exists).
3. On your **Save Button** — `On Click ()`: drag the Save System GameObject → select `SaveSystemMethods > SaveSlot(int)` → set the int argument to `1`.
4. On your **Load Button** — `On Click ()`: select `SaveSystemMethods > LoadFromSlot(int)` → argument `1`.
5. On your **New Game Button** — `On Click ()`: select `SaveSystemMethods > ResetGameState()`, then trigger your scene load.
6. For a combined "continue or new game" button: `SaveSystemMethods > LoadOrRestart(int)` — loads slot if found, otherwise loads the default starting scene.

**Expected result:** Clicking the button in Play mode triggers save or load without any custom MonoBehaviour.

---

### Workflow: Scene transitions and spawnpoints

**Goal:** Move the player to a different scene and spawn at a named location, with save data intact.

**Steps:**
1. On a trigger-collider GameObject in the source scene: `Add Component > Pixel Crushers/Save System/Misc/Scene Portal`.
2. Set **Required Tag** to `Player` (default).
3. Set **Destination Scene Name** (must exactly match the name in `File > Build Settings`).
4. Set **Spawnpoint Name In Destination Scene** (name of a GameObject in the destination scene that marks the spawn location).
5. On the Player's `PositionSaver`, enable **Use Player Spawnpoint** — this overrides the loaded position with the spawnpoint transform.
6. To trigger from code: `PixelCrushers.SaveSystem.LoadScene("MyScene@MySpawnpoint");`
7. For fade transitions: add `Pixel Crushers/Save System/Scene Transition Managers/Standard Scene Transition Manager` to the Save System GameObject.

**Expected result:** Player walks into the portal → destination scene loads → player appears at the named spawnpoint → PositionSaver is updated with the new location.

---

### Workflow: Auto-save

**Goal:** Save game state automatically on quit, mobile background, or pause without explicit `SaveToSlot` calls.

**Steps:**
1. On the Save System GameObject: `Add Component > Pixel Crushers/Save System/Misc/Auto Save Load`.
2. Configure:
   - **Save Slot Number**: `1`.
   - **Load On Start**: `true` — loads from slot when the game starts if data exists.
   - **Save On Quit**: `true` — saves on `OnApplicationQuit`.
   - **Save On Pause**: `true` — saves on `OnApplicationPause(true)` (essential for mobile).
   - **Save On Lose Focus**: `false` (enable for desktop if preferred).
   - **Dont Save In Scenes**: add build indices of scenes where auto-save is inappropriate (e.g., `0` for the main menu).

**Expected result:** Game state persists across sessions automatically; no explicit `SaveToSlot` calls are needed in any script.

## Verification

After completing Workflow 1 (rig) and Workflow 2 (at least one Saver with a unique key):

```csharp
using PixelCrushers;
using UnityEngine;

public class SaveSystemVerifier : MonoBehaviour
{
    private void Start()
    {
        Debug.Assert(SaveSystem.hasInstance, "SaveSystem not in scene — add the rig.");

        SaveSystem.SaveToSlot(1);
        Debug.Assert(SaveSystem.HasSavedGameInSlot(1), "SaveToSlot(1) failed.");

        Debug.Log("Save OK. Persistent path: " + Application.persistentDataPath);
    }
}
```

With `DiskSavedGameDataStorer`: confirm `save_1.dat` appears inside `Application.persistentDataPath` after `SaveToSlot(1)`.
With `PlayerPrefsSavedGameDataStorer`: confirm via `PlayerPrefs.HasKey("Save1")` (substitute the configured `playerPrefsKeyBase`).

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `SaveSystem.SaveToSlot(int)` | `static void` | Async save to a numbered slot |
| `SaveSystem.SaveToSlotImmediate(int)` | `static void` | Synchronous save (no yield) |
| `SaveSystem.LoadFromSlot(int)` | `static void` | Load saved game from slot |
| `SaveSystem.HasSavedGameInSlot(int)` | `static bool` | Returns `true` if slot contains data |
| `SaveSystem.DeleteSavedGameInSlot(int)` | `static void` | Erases a slot |
| `SaveSystem.RecordSavedGameData()` | `static SavedGameData` | Snapshot state without writing to storage |
| `SaveSystem.ApplySavedGameData(SavedGameData)` | `static void` | Apply a snapshot without loading a scene |
| `SaveSystem.LoadScene(string)` | `static void` | Load scene; optional `@Spawnpoint` suffix |
| `SaveSystem.RestartGame(string)` | `static void` | Clear state and load the named starting scene |
| `SaveSystem.ResetGameState()` | `static void` | Clear in-memory save data only |
| `SaveSystem.Serialize(object)` | `static string` | Serialize any object via the active serializer |
| `SaveSystem.Deserialize<T>(string, T)` | `static T` | Deserialize via the active serializer |
| `SaveSystem.saveDataApplied` | `static event Action` | Fires after load data is applied to all Savers |
| `SaveSystem.saveStarted` / `saveEnded` | `static event Action` | Save lifecycle hooks |
| `SaveSystem.loadStarted` / `loadEnded` | `static event Action` | Load lifecycle hooks |
| `SaveSystem.currentSavedGameData` | `static SavedGameData` | The currently loaded game state snapshot |
| `SaveSystem.framesToWaitBeforeApplyData` | `static int` | Frames to wait after scene load before applying Saver data |
| `Saver.key` | `string` field | Unique identifier for this Saver's stored data |
| `Saver.saveAcrossSceneChanges` | `bool` field | Retain data on scene change (default `true`) |
| `Saver.order` | `int` field | Load-apply order (lower = earlier, default `0`) |
| See [Savers and Serializers reference](references/savers-and-serializers.md) | — | Full Saver catalogue, serializer/storer fields, encryption, custom Saver authoring, SpawnedObject details |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `NullReferenceException` on `SaveSystem.SaveToSlot` | No SaveSystem wrapper in the active scene | Add the Save System rig (Workflow 1) to a persistent scene |
| State not restored after `LoadFromSlot` | Saver has an empty or duplicate key | Run `Tools > Pixel Crushers > Common > Save System > Assign Unique Keys...` |
| Restored state not visible in same frame as `LoadFromSlot` | Data applied after `framesToWaitBeforeApplyData` frames | Subscribe to `SaveSystem.saveDataApplied` event; do not read state immediately |
| `save_1.dat` not found on disk | Storer is `PlayerPrefsSavedGameDataStorer`, not `DiskSavedGameDataStorer` | Swap the storer component; verify path via `Application.persistentDataPath` |
| `InvalidOperationException` / corrupted load with Binary serializer | `BinaryFormatter` deserializing mismatched or tampered data | Switch to `JsonDataSerializer`; see encryption notes in [reference](references/savers-and-serializers.md) |

## Boundaries

The Save System serializes only registered `Saver` components — it does not automatically persist arbitrary `MonoBehaviour` fields. Built-in cloud save is not provided; implement a custom `SavedGameDataStorer` subclass to sync with a cloud backend. `BinaryDataSerializer` uses `BinaryFormatter`, which carries known security considerations when deserializing untrusted data — prefer `JsonDataSerializer` unless payload size is critical. For persisting only a small number of simple primitive values, native `PlayerPrefs` may be simpler than the full rig.
