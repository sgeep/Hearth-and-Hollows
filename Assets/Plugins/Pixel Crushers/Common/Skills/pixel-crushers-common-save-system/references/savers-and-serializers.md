# Savers and Serializers — Deep Reference

Supplementary detail for `pixel-crushers-common-save-system`. Covers the complete built-in Saver catalogue with all Inspector fields, serializer internals and selection guidance, storer configuration and encryption caveats, SpawnedObject management, helper component reference, and a guide to authoring custom Saver subclasses.

---

## Base Saver fields

All Savers inherit from `PixelCrushers.Saver` (core namespace). Every Saver exposes these fields in the Inspector:

| Field | Type | Default | Description |
|---|---|---|---|
| `key` | `string` | `""` | Unique identifier for this Saver's stored data. Must be unique across all Savers in the project. |
| `appendSaverTypeToKey` | `bool` | `false` | Appends the C# class name to `key`, reducing collision risk without manual naming. |
| `saveAcrossSceneChanges` | `bool` | `true` | Retain data in `SaveSystem.currentSavedGameData` when transitioning between scenes. |
| `skipSaveWhenChangingScenes` | `bool` | `false` | Do not write new data on scene change — treat the current state as read-only during the transition. |
| `restoreStateOnStart` | `bool` | `false` | Call `ApplyData` in `Start()` automatically without a `LoadFromSlot` call (useful for single-scene or checkpoint-based restoration). |
| `order` | `int` | `0` | Determines the sequence in which Savers are applied during load. Lower values are applied first. Use when one Saver's restored state depends on another (e.g., instantiate an object before applying its position). |

**Key uniqueness rule:** Two Savers sharing the same `key` will overwrite each other's data silently. Run `Tools > Pixel Crushers > Common > Save System > Assign Unique Keys...` after adding any Saver to guarantee uniqueness across the entire project.

---

## Built-in Saver components

### Position Saver
`Add Component > Pixel Crushers/Save System/Savers/Position Saver`

Saves and restores a `Transform`'s world position, rotation, and scale.

| Field | Type | Default | Description |
|---|---|---|---|
| `target` | `Transform` | `null` (own Transform) | Transform to record. Leave null to track the Saver's own GameObject. |
| `usePlayerSpawnpoint` | `bool` | `false` | On restore, override position/rotation with `SaveSystem.playerSpawnpoint` (the named spawnpoint from a `LoadScene("Scene@Spawnpoint")` call). |
| `multiscene` | `bool` | `false` | Save position data even when the object is configured to not save across scene changes. |

### Active Saver
`Add Component > Pixel Crushers/Save System/Savers/Active Saver`

Records and restores the `activeSelf` flag of a single GameObject.

| Field | Type | Description |
|---|---|---|
| `gameObjectToWatch` | `GameObject` | The GameObject whose active state is persisted. Defaults to own GameObject if null. |

### Multi Active Saver
`Add Component > Pixel Crushers/Save System/Savers/Multi Active Saver`

Records and restores `activeSelf` for an array of GameObjects in a single Saver entry.

| Field | Type | Description |
|---|---|---|
| `gameObjectsToWatch` | `GameObject[]` | List of GameObjects whose active states are persisted together. |

### Enabled Saver
`Add Component > Pixel Crushers/Save System/Savers/Enabled Saver`

Records and restores the `enabled` flag on a single `Behaviour` (e.g., a script or renderer).

| Field | Type | Description |
|---|---|---|
| `componentToWatch` | `Component` | The `Behaviour` whose `enabled` property is persisted. |

### Multi Enabled Saver
`Add Component > Pixel Crushers/Save System/Savers/Multi Enabled Saver`

Records and restores `enabled` for an array of `Behaviour`s.

| Field | Type | Description |
|---|---|---|
| `componentsToWatch` | `Component[]` | List of Behaviours whose enabled states are persisted together. |

### Animator Saver
`Add Component > Pixel Crushers/Save System/Savers/Animator Saver`

Saves all Animator parameter values and the current state on each layer. Requires an `Animator` component on the same GameObject. Exposes only base Saver fields; no additional configuration is required.

### Destructible Saver
`Add Component > Pixel Crushers/Save System/Savers/Destructible Saver`

Tracks whether a GameObject has been destroyed or deactivated, and restores that state on load.

| Field | Type | Description |
|---|---|---|
| `mode` | `Mode` enum | `OnDisable` — record destruction when the object is disabled; `OnDestroy` — record on `OnDestroy`. |
| `destroyMode` | `DestroyMode` enum | `Destroy` — call `Destroy` on load if previously destroyed; `Deactivate` — call `SetActive(false)` instead. |
| `destroyedVersionPrefab` | `GameObject` | Optional prefab to instantiate in the object's place when restoring the destroyed state (e.g., rubble or gibs). |

---

## SpawnedObject and SpawnedObjectManager

Use these components when gameplay creates GameObjects at runtime (enemies, loot, projectiles, etc.) that must survive save/load cycles.

### SpawnedObject
`Add Component > Pixel Crushers/Save System/Misc/Spawned Object`

Attach to a prefab (not a scene object). Gives each runtime instance a unique identity.

| Field | Type | Default | Description |
|---|---|---|---|
| `saveUniqueSaverData` | `bool` | `true` | Each instance records its own Saver data under a runtime-generated GUID, rather than sharing the prefab's key. |
| `guid` | `string` | auto-generated | Unique identifier for this specific instance across sessions. Populated automatically at runtime; do not set manually. |

### SpawnedObjectManager
`Add Component > Pixel Crushers/Save System/Misc/Spawned Object Manager`

Place on the Save System GameObject (or any persistent GameObject). Manages the lifecycle of all `SpawnedObject` instances.

| Field | Type | Default | Description |
|---|---|---|---|
| `spawnedObjectPrefabs` | `List<GameObject>` | — | Prefabs the manager is authorised to instantiate on load. Every prefab that can be saved must appear here. |
| `applySaveDataToSpawnedObjectsOnRestore` | `bool` | `true` | After re-instantiating, apply each instance's saved Saver data. Disable only if applying data manually. |

**How the pool lifecycle works:**
1. **Save:** `SpawnedObjectManager` records each live `SpawnedObject` instance (its prefab list index + instance GUID + full Saver data).
2. **Load:** Existing instances are destroyed; for each record, the manager instantiates the corresponding prefab, restores the GUID, then applies Saver data in `order` sequence.
3. **Prefab list integrity:** If a prefab is removed from `spawnedObjectPrefabs` between saves, its instances cannot be restored. Keep the list append-only in shipped builds.

---

## Serializers

Exactly one serializer must be present on the Save System's persistent GameObject. The serializer converts Saver data to/from `string`.

### JsonDataSerializer
`Add Component > Pixel Crushers/Save System/Data Serializers/Json Data Serializer`

| Field | Type | Default | Description |
|---|---|---|---|
| `prettyPrint` | `bool` | `false` | Indent JSON output (useful for debugging; increases save file size). |

Uses `UnityEngine.JsonUtility` internally. **Type constraints:** only fields in `[System.Serializable]` inner classes with Unity-supported field types (primitives, arrays, nested serializable structs) are serialized. Properties and non-serializable types are silently ignored.

**Recommended for:** most shipped games, WebGL, and any scenario where save files may be inspected or modded.

### BinaryDataSerializer
`Add Component > Pixel Crushers/Save System/Data Serializers/Binary Data Serializer`

Uses `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter` with custom `ISerializationSurrogate` implementations for `Vector3` and `Quaternion`. The resulting binary is Base64-encoded before storage.

**Security consideration:** `BinaryFormatter` is flagged as insecure by Microsoft for deserializing data from untrusted sources (potential for remote code execution gadget chains). Acceptable only when:
- Save files are generated exclusively by your own build (not user-supplied or network-received).
- The platform and .NET runtime have not deprecated `BinaryFormatter` (check your Unity version's .NET configuration).

**Recommended for:** legacy projects already using Binary; new projects should prefer `JsonDataSerializer`.

---

## Storers

Exactly one storer must be present on the Save System's persistent GameObject. The storer writes/reads serialized strings to a backend.

### PlayerPrefsSavedGameDataStorer
`Add Component > Pixel Crushers/Save System/Saved Game Data Storers/PlayerPrefs Saved Game Data Storer`

| Field | Type | Default | Description |
|---|---|---|---|
| `playerPrefsKeyBase` | `string` | `"Save"` | Prefix for PlayerPrefs keys. Slot 1 is stored as `Save1`, slot 2 as `Save2`, etc. |
| `encrypt` | `bool` | `false` | Apply a simple XOR cipher before writing to PlayerPrefs. |
| `encryptionPassword` | `string` | `"My Password"` | Password for the XOR cipher. Change before shipping. |

**Best for:** rapid prototypes; WebGL builds where file system access is unavailable; single-slot auto-save games.

**Limitations:** PlayerPrefs are stored in plaintext (even without encryption, the XOR is trivial to reverse); values are wiped when the user clears application data or uninstalls on mobile. Not recommended for data that must be robust against user modification.

### DiskSavedGameDataStorer
`Add Component > Pixel Crushers/Save System/Saved Game Data Storers/Disk Saved Game Data Storer`

| Field | Type | Default | Description |
|---|---|---|---|
| `storeSaveFilesIn` | `BasePath` enum | `PersistentDataPath` | Root directory: `Application.persistentDataPath` (recommended), `Application.dataPath`, or `Custom`. |
| `customPath` | `string` | `""` | Absolute or relative path when `BasePath.Custom` is selected. |
| `encrypt` | `bool` | `true` | Encrypt save files before writing to disk. |
| `encryptionPassword` | `string` | `"My Password"` | **Change this before shipping any build.** |

**Files written:**
- `save_<slot>.dat` — serialized save data for each slot.
- `saveinfo.dat` — slot metadata (timestamps, custom info strings) used for save-slot UI.

Both files are written under the configured base path.

**Encryption caveat:** The built-in cipher is a deterrent against casual editing, not cryptographic-strength protection. A determined user with access to your binary can reverse-engineer the password and algorithm. For competitive titles or anti-cheat requirements, implement a custom `SavedGameDataStorer` that posts data to a server-side endpoint.

---

## Helper components

### SaveSystemMethods
`Add Component > Pixel Crushers/Save System/Misc/Save System Methods`

Exposes `SaveSystem` calls as Inspector-assignable UnityEvent targets, removing the need for custom save-UI scripts.

| Method | Signature | Description |
|---|---|---|
| `SaveSlot` | `void SaveSlot(int slot)` | Calls `SaveSystem.SaveToSlot(slot)`. |
| `LoadFromSlot` | `void LoadFromSlot(int slot)` | Calls `SaveSystem.LoadFromSlot(slot)`. |
| `LoadOrRestart` | `void LoadOrRestart(int slot)` | Loads `slot` if data exists; otherwise loads `defaultStartingSceneName`. |
| `LoadScene` | `void LoadScene(string scene)` | Calls `SaveSystem.LoadScene(scene)`. |
| `ResetGameState` | `void ResetGameState()` | Clears in-memory save data. |
| `RestartGame` | `void RestartGame(string scene)` | Clears state and loads the named scene. |
| `defaultStartingSceneName` | `string` field | Scene to load when `LoadOrRestart` finds no save data. |

### AutoSaveLoad
`Add Component > Pixel Crushers/Save System/Misc/Auto Save Load`

| Field | Type | Default | Description |
|---|---|---|---|
| `saveSlotNumber` | `int` | `1` | Slot used for all auto-save and auto-load operations. |
| `loadOnStart` | `bool` | `true` | Load from slot on `Start()` if data exists. |
| `saveOnQuit` | `bool` | `true` | Save on `OnApplicationQuit`. |
| `saveOnPause` | `bool` | `true` | Save on `OnApplicationPause(true)` (mobile background). |
| `saveOnLoseFocus` | `bool` | `false` | Save on `OnApplicationFocus(false)` (desktop alt-tab). |
| `dontSaveInScenes` | `int[]` | `{}` | Build indices of scenes where auto-save is skipped (e.g., main menu). |

### SaveSystemEvents
`Add Component > Pixel Crushers/Save System/Misc/Save System Events`

Wraps all `SaveSystem` static events as `UnityEvent`s for Inspector wiring:
`onSaveStart`, `onSaveEnd`, `onLoadStart`, `onLoadEnd`, `onSaveDataApplied`, `onSceneLoad`.

### SaveSystemTestMenu
`Add Component > Pixel Crushers/Save System/Misc/Save System Test Menu`

IMGUI debug overlay in Play mode. Toggle with `menuInputKey` (KeyCode). `saveSlot` (int) configures which slot the overlay targets. Use during development to test save/load/restart cycles without building production UI.

### ScenePortal
`Add Component > Pixel Crushers/Save System/Misc/Scene Portal`

| Field | Type | Default | Description |
|---|---|---|---|
| `requiredTag` | `string` | `"Player"` | Only GameObjects with this tag trigger the portal on `OnTriggerEnter` / `OnTriggerEnter2D`. |
| `destinationSceneName` | `string` | `""` | Exact scene name as listed in `File > Build Settings`. |
| `spawnpointNameInDestinationScene` | `string` | `""` | Name of the spawnpoint GameObject in the destination scene. |
| `onUsePortal` | `UnityEvent` | — | Fired when the portal triggers (before scene load). |

Call `UsePortal()` from code to trigger the transition without a physics event.

### StandardSceneTransitionManager
`Add Component > Pixel Crushers/Save System/Scene Transition Managers/Standard Scene Transition Manager`

Provides a fade-to-black transition during scene loads. Attach to the Save System GameObject or any persistent GameObject.

### LoadingScreenProgressBar
`Add Component > Pixel Crushers/Save System/Scene Transition Managers/Loading Screen Progress Bar`

Reads `SaveSystem.currentAsyncOperation.progress` to drive a UI Slider or Image fill during async scene loads. Pair with `StandardSceneTransitionManager` for a complete loading screen.

---

## Writing a custom Saver

Subclass `PixelCrushers.Saver` and override `RecordData()` and `ApplyData(string)`:

```csharp
using System;
using PixelCrushers;
using UnityEngine;

[AddComponentMenu("My Game/Stats Saver")]
public class StatsSaver : Saver
{
    // Inner data class — must be [Serializable] for JsonUtility.
    [Serializable]
    private class Data
    {
        public int health;
        public float mana;
        public int[] inventoryIds;
    }

    [SerializeField] private int _health = 100;
    [SerializeField] private float _mana = 50f;
    [SerializeField] private int[] _inventoryIds = Array.Empty<int>();

    /// <summary>
    /// Called by SaveSystem when recording state.
    /// Must return a serialized string; use SaveSystem.Serialize so the
    /// active serializer (Json or Binary) is honoured automatically.
    /// </summary>
    public override string RecordData()
    {
        var data = new Data
        {
            health      = _health,
            mana        = _mana,
            inventoryIds = _inventoryIds
        };
        return SaveSystem.Serialize(data);
    }

    /// <summary>
    /// Called by SaveSystem when restoring state.
    /// Always guard against null/empty — no prior save means an empty string.
    /// </summary>
    public override void ApplyData(string s)
    {
        if (string.IsNullOrEmpty(s)) return;

        // Pass a default instance as the second argument so fields not in
        // the JSON (e.g., added in a later version) get sensible defaults.
        var data = SaveSystem.Deserialize<Data>(s, new Data());
        _health      = data.health;
        _mana        = data.mana;
        _inventoryIds = data.inventoryIds ?? Array.Empty<int>();
    }
}
```

**Authoring rules:**
1. The inner `Data` class must be `[System.Serializable]`. Nest it privately inside the Saver to keep the namespace clean.
2. Use only `JsonUtility`-compatible field types: primitives, strings, arrays of primitives/structs, and nested `[Serializable]` structs. Dictionaries, generics (except arrays), and non-serializable types are dropped silently.
3. Always use `SaveSystem.Serialize` / `SaveSystem.Deserialize<T>` — never `JsonUtility.ToJson` directly — so that switching serializers requires no code changes.
4. Guard `ApplyData` with a null/empty check. The string is empty when a key exists in the Saver but has no corresponding entry in the save file (new Saver added after a save was created).
5. Assign a unique `key` via the Inspector or via `Tools > Pixel Crushers > Common > Save System > Assign Unique Keys...`.
6. Set `order` if this Saver must be applied after another (e.g., a Saver that reads a `SpawnedObject`'s restored component should have a higher `order` than the `SpawnedObjectManager`'s Saver).

**Versioning saves:** When you add new fields to `Data` in a later build, always provide defaults in the `new Data()` passed to `Deserialize`. This ensures existing save files (which lack the new field) still load correctly.
