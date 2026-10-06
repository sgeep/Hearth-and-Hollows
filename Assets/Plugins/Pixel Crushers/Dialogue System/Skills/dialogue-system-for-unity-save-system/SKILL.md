---
name: dialogue-system-for-unity-save-system
description: "Use this skill whenever someone wants to save or restore Dialogue System state — e.g. 'save quest progress', 'save conversation position', 'persist Lua variables across scenes', 'set up a save system', 'save the game to a slot', 'restore npc memory after a scene reload', 'wire up DialogueSystemSaver'. Covers the Save System GameObject setup (SaveSystem component plus a serializer and storer), the DialogueSystemSaver and ConversationStateSaver components, slot-based save/load via the PixelCrushers.SaveSystem API, and cross-scene persistence with SaveSystem.LoadScene. Do NOT use for the generic Pixel Crushers Common save framework or custom Saver subclasses unrelated to Dialogue System data (see pixel-crushers-common-save-system). When in doubt whether a save/load request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "Dialogue System for Unity"
  publisher: "Pixel Crushers"
  asset-version: "2.2.73.2"
  skill-version: "1.2.0"
  unity: "2022.3+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/behavior-ai"
  asset-store-url: "https://assetstore.unity.com/packages/tools/behavior-ai/dialogue-system-for-unity-11672"
  documentation-url: "https://pixelcrushers.com/dialogue_system/manual2x/html/"
  support-url: "https://www.pixelcrushers.com/support/"
  last-verified: "2026-09-01"
---

# Dialogue System for Unity — Save System

The Dialogue System save integration is built on the Pixel Crushers Common Save System. A **Save System GameObject** runs a `SaveSystem` MonoBehaviour plus a `DataSerializer` and a `SavedGameDataStorer`. The `DialogueSystemSaver` Saver component captures all Dialogue System state (Lua environment, quest flags, conversation history) and plugs into that framework automatically. The `PixelCrushers.SaveSystem` static class provides all slot-based and scene-transition APIs that the rest of the game calls.

## When to use this skill

Use when:
- Setting up the Save System GameObject for a project that uses the Dialogue System.
- Adding `DialogueSystemSaver` or `ConversationStateSaver` to a prefab.
- Calling `PixelCrushers.SaveSystem.SaveToSlot` / `LoadFromSlot` to trigger saves and loads.
- Wiring scene transitions so dialogue state persists across scene loads.
- Resetting or restarting all game state cleanly.

Do **not** use this skill for:
- Generic Save System setup, custom `Saver` subclass authoring, serializer/storer swaps, or save scripting not specific to the Dialogue System → see **pixel-crushers-common-save-system**.
- Quest scripting, Lua API calls, or conversation triggers → handled by the runtime-scripting and quests skills.

## Prerequisites

**Verify installation** before making any changes:

```csharp
using System.Linq;
bool installed = System.AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null);
```

Do **not** use `Type.GetType("..., Assembly-CSharp")` — the Dialogue System ships in its own compiled assembly.

Required project state:
- A Dialogue Manager GameObject exists in the scene (or use the Setup Wizard: **Tools → Pixel Crushers → Dialogue System → Wizards → Setup Wizard**).
- A `DialogueDatabase` asset is assigned to the Dialogue Manager.
- The Pixel Crushers Common package is present (ships alongside the Dialogue System).
- Use wrapper components (via Add Component menus) rather than core namespace classes directly in scenes.

## Quick start

1. Create an empty GameObject in the scene named `Save System`.
2. **Add Component → Pixel Crushers → Save System → Save System** (wrapper).
3. **Add Component → Pixel Crushers → Save System → Serializers → Json Data Serializer**.
4. **Add Component → Pixel Crushers → Save System → Storers → Player Prefs Saved Game Data Storer**.
5. On the Dialogue Manager GameObject: **Add Component → Pixel Crushers → Save System → Savers → Dialogue System → Dialogue System Saver**.
6. To save: `PixelCrushers.SaveSystem.SaveToSlot(0);`
7. To load: `PixelCrushers.SaveSystem.LoadFromSlot(0);`

## Workflows

### Workflow: Configure Save System for Dialogue System state persistence

**Goal:** A fully working Save System that captures all Dialogue System state (Lua, quests, conversations).

**Steps:**
1. Create the `Save System` GameObject with SaveSystem + JsonDataSerializer + PlayerPrefsSavedGameDataStorer (see Quick start steps 1–4).
2. On the `SaveSystem` component, confirm **Save Current Scene** is ticked — this ensures the correct scene is reloaded when the player loads a saved game.
3. Add `DialogueSystemSaver` to the Dialogue Manager GameObject (Add Component menu path above).
   - In the Inspector, confirm `saveAcrossSceneChanges` is **true** (the default). This keeps dialogue data alive across scene transitions.
   - Leave `saveRawData` unchecked unless the database is extremely large (if enabled, use `BinaryDataSerializer` not `JsonDataSerializer`).
4. Optionally add `ConversationStateSaver` to the Dialogue Manager if you need to resume from a mid-conversation node after loading.
5. Save the scene (`Ctrl+S` / `Cmd+S`).

**Expected result:** `PixelCrushers.SaveSystem.SaveToSlot(0)` writes Lua variables, quest flags, and conversation history to PlayerPrefs slot 0. `LoadFromSlot(0)` reloads the saved scene and restores all Dialogue System state.

---

### Workflow: Scene transition with state persistence

**Goal:** Player moves from Scene A to Scene B and all dialogue state (quests, Lua vars, spoken lines) carries over.

**Steps:**
1. Confirm the Save System GameObject persists across scenes — the `SaveSystem` component calls `DontDestroyOnLoad` automatically in `Awake`.
2. Locate every call to `SceneManager.LoadScene("SceneB")` that should preserve dialogue state.
3. Replace each one with:
   ```csharp
   PixelCrushers.SaveSystem.LoadScene("SceneB");
   ```
   To place the player at a named spawnpoint: `PixelCrushers.SaveSystem.LoadScene("SceneB@SpawnpointName")`.
4. `DialogueSystemSaver.OnBeforeSceneChange()` fires automatically before the scene unloads — it calls `PersistentDataManager.LevelWillBeUnloaded()` to snapshot Lua state.
5. On the new scene, `DialogueSystemSaver.ApplyDataImmediate()` restores Lua immediately so other scripts' `Start()` methods can read Lua variables.

**Expected result:** All quest flags and Lua variables are present in Scene B without any manual serialisation code.

---

### Workflow: Restart or new game (reset all dialogue state)

**Goal:** Return to a completely clean game state as if the player is starting a new game.

**Steps:**
1. Call `PixelCrushers.SaveSystem.RestartGame("MainScene")` — clears all saved game data and loads the starting scene.
2. Internally, `DialogueSystemSaver.OnRestartGame()` fires automatically. It calls `DialogueManager.StopAllConversations()`, `DialogueManager.ResetDatabase()`, and `DialogueManager.SendUpdateTracker()`.
3. To reset state without loading a different scene: `PixelCrushers.SaveSystem.ResetGameState()`.

**Expected result:** All Lua variables, quest states, and conversation histories return to their database defaults.

---

### Workflow: Inspect and migrate saved game data across format versions

**Goal:** Detect an outdated save format before loading, apply migration logic, or clear stale keys — without breaking existing saves.

**Steps:**
1. Increment the **Version** integer on the `SaveSystem` component whenever the save format changes in a shipped build.
2. Retrieve raw slot data without triggering a scene load:
   ```csharp
   var saved = PixelCrushers.SaveSystem.storer.RetrieveSavedGameData(slotNumber);
   int savedVersion = saved.version;
   ```
3. Remove keys whose format has changed irreparably, then load:
   ```csharp
   saved.DeleteData("ObsoleteKey");
   PixelCrushers.SaveSystem.LoadGame(saved);
   ```
4. For per-saver migration, branch inside `Saver.ApplyData(string s)`:
   ```csharp
   if (PixelCrushers.SaveSystem.currentSavedGameData.version < 2)
   { /* migrate from format 1 */ }
   ```
5. Enable **Dialogue Manager → Persistent Data Settings → Initialize New Variables** when shipping new Lua variables or quests to existing players.

**Expected result:** Old saves load cleanly; new variables and quests receive their default values even when absent from stored data.

---

### Workflow: Scene loading via More Mountains MMSceneLoadingManager

**Goal:** Preserve Dialogue System state across scene transitions managed by Corgi Engine or TopDown Engine's `MMSceneLoadingManager`.

**Steps:**
1. Uncheck **Save Current Scene** on the `SaveSystem` component — the More Mountains loader handles scene management.
2. Subclass `MMSceneLoadingManager` (or `MMLoadingSceneManager`) and override `LoadAsynchronously()`:
   ```csharp
   protected override IEnumerator LoadAsynchronously()
   {
       PixelCrushers.SaveSystem.RecordSavedGameData();
       PixelCrushers.SaveSystem.BeforeSceneChange();
       yield return base.LoadAsynchronously();
       yield return new WaitForEndOfFrame();
       PixelCrushers.SaveSystem.ApplySavedGameData(
           PixelCrushers.SaveSystem.currentSavedGameData);
   }
   ```
3. Place your subclass in the loading scene and ensure every `Saver` component has a unique **Key** value.

**Expected result:** Quests, Lua variables, and conversation history survive the MM-managed scene transition without manual serialisation.

---

### Workflow: Display save-slot summary info without loading the game

**Goal:** Show metadata (scene name, playtime, screenshot) in a slot-selection UI without loading the saved scene.

**Steps:**
1. Add a `Saver` subclass (e.g. `GameSummarySaver`) to the Dialogue Manager with a unique **Key** (e.g. `"GameSummary"`).
2. Define a serialisable data class and override `RecordData()`:
   ```csharp
   [System.Serializable]
   class Data { public string scene; public float playtime; }

   public override string RecordData()
   {
       return SaveSystem.Serialize(new Data {
           scene    = SceneManager.GetActiveScene().name,
           playtime = Time.timeSinceLevelLoad });
   }
   public override void ApplyData(string s) { } // read-only saver
   ```
3. Read from the slot-selection screen without loading:
   ```csharp
   if (PixelCrushers.SaveSystem.HasSavedGameInSlot(slot))
   {
       var saved = PixelCrushers.SaveSystem.storer.RetrieveSavedGameData(slot);
       var data  = PixelCrushers.SaveSystem.Deserialize<Data>(
                       saved.GetData("GameSummary"));
   }
   ```

**Expected result:** Slot thumbnails display accurate metadata fetched directly from the save file with no scene load triggered.

---

### Workflow: Trigger scene transitions from sequences, ScenePortal, and UI buttons

**Goal:** Initiate a save-aware scene transition using methods beyond direct C# `SaveSystem.LoadScene` calls.

**Steps:**

- **From a dialogue sequence:** Add `LoadLevel(SceneB)` or `LoadLevel(SceneB, SpawnpointName)` to a node's Sequence field. The sequencer command calls `SaveSystem.LoadScene` internally.
- **Via a zone trigger (ScenePortal):** Add a `ScenePortal` component to a collider trigger volume. Set its **Scene Name** and optional **Spawn Point Name**. When the player enters, it calls `SaveSystem.LoadScene` automatically.
- **From a UI button:** Add a `SaveSystemMethods` component to any persistent GameObject. Wire the button's `onClick` to `SaveSystemMethods.LoadScene` and configure the target scene name in the Inspector.
- **External loader (manual):** When a third-party system controls scene loading, call the save hooks before and after:
  ```csharp
  PixelCrushers.SaveSystem.RecordSavedGameData();
  PixelCrushers.SaveSystem.BeforeSceneChange();
  // … load scene externally …
  // In the new scene (e.g., a Start() method):
  PixelCrushers.SaveSystem.ApplySavedGameData(
      PixelCrushers.SaveSystem.currentSavedGameData);
  ```

**Expected result:** Dialogue state, quests, and Lua variables carry over correctly regardless of which mechanism initiates the scene change.

---

### Workflow: Run startup logic after save data is applied

**Goal:** Execute code that reads Lua variables or quest state only after the Save System has fully restored the game.

**Steps:**
1. By default, `SaveSystem` delays applying data by **Frames To Wait Before Apply Data** frames (default: 1) so scene objects complete `Start()` first. Scripts reading Lua state in `Start()` may still see default values.
2. Subscribe to `SaveSystem.saveDataApplied` in `OnEnable` / `OnDisable`:

```csharp
using PixelCrushers;
using PixelCrushers.DialogueSystem;

void OnEnable()  => SaveSystem.saveDataApplied += OnSaveDataApplied;
void OnDisable() => SaveSystem.saveDataApplied -= OnSaveDataApplied;

void OnSaveDataApplied()
{
    // Lua state is fully restored here.
    if (QuestLog.IsQuestSuccessful("Tutorial")) EnableNextChapter();
}
```

3. Alternatively, set **Frames To Wait Before Apply Data** to `0` on the `SaveSystem` component if no scene objects need `Start()` to complete before data is applied.

**Expected result:** Startup logic that depends on saved quest or Lua state always runs after restoration is complete.

---

### Workflow: Integrate with third-party save frameworks

**Goal:** Serialize Dialogue System state into a string payload that an external save system (e.g., Easy Save) can write and read.

**Steps:**
1. Untick **Save Current Scene** on the `SaveSystem` component and set **Apply Save Data After Frames** to `0`.
2. On save, call:
```csharp
string data = PixelCrushers.SaveSystem.Serialize(
                  PixelCrushers.SaveSystem.RecordSavedGameData());
// Pass `data` to your external save framework.
```
3. On load, restore:
```csharp
// Retrieve `data` string from your external save framework, then:
PixelCrushers.SaveSystem.ApplySavedGameData(
    PixelCrushers.SaveSystem.Deserialize<PixelCrushers.SavedGameData>(data));
```

**Expected result:** All Dialogue System Lua tables, quest flags, and `DialogueSystemSaver` state are preserved and restored by the external framework without duplicating save logic.

---

### Workflow: Use different loading screens with StandardSceneTransitionManager

**Goal:** Display a context-specific loading scene per transition rather than a single global one.

**Steps:**
1. `StandardSceneTransitionManager` (placed on the Save System GameObject) exposes a **Loading Scene** field that sets the default loading scene name.
2. To vary the loading scene per transition, assign the field before triggering the scene change:
```csharp
using PixelCrushers;
void Start()
{
    var mgr = FindObjectOfType<StandardSceneTransitionManager>();
    if (mgr != null) mgr.loadingScene = "LoadingScene_Chapter2";
}
```
3. For full control, subclass `StandardSceneTransitionManager` and override its virtual methods to select loading scenes contextually.

**Expected result:** Each scene transition uses a loading screen appropriate to that game section.

## Verification

```csharp
// Run in Play Mode to confirm the save/load round-trip works:
PixelCrushers.SaveSystem.SaveToSlot(0);
bool hasSave = PixelCrushers.SaveSystem.HasSavedGameInSlot(0);
Debug.Assert(hasSave, "Save failed — slot 0 is empty.");
PixelCrushers.SaveSystem.LoadFromSlot(0);
// Check Console for errors. A clean load logs no exceptions from DialogueSystemSaver.
```

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `PixelCrushers.SaveSystem.SaveToSlot(int)` | static method | Saves current game state to a numbered slot (async coroutine) |
| `PixelCrushers.SaveSystem.SaveToSlotImmediate(int)` | static method | Saves synchronously — use only when a coroutine cannot be started |
| `PixelCrushers.SaveSystem.LoadFromSlot(int)` | static method | Loads saved game from slot; restores scene and all saver data |
| `PixelCrushers.SaveSystem.HasSavedGameInSlot(int)` | static method | Returns `true` if the slot contains saved data |
| `PixelCrushers.SaveSystem.DeleteSavedGameInSlot(int)` | static method | Permanently deletes saved data in the given slot |
| `PixelCrushers.SaveSystem.LoadScene(string)` | static method | Records state then loads named scene (supports `"Scene@Spawnpoint"`) |
| `PixelCrushers.SaveSystem.RestartGame(string)` | static method | Clears all saved data then loads the named starting scene |
| `PixelCrushers.SaveSystem.ResetGameState()` | static method | Clears saved data without loading a different scene |
| `DialogueSystemSaver` | Saver component | Captures/restores Lua environment, quest states, conversation history |
| `ConversationStateSaver` | Saver component | Captures/restores active conversation node position |
| `SaveSystem.storer.RetrieveSavedGameData(int slot)` | instance method | Reads raw `SavedGameData` from a slot without loading the scene; use for slot summaries and version checks |
| `SavedGameData.GetData(string key)` / `DeleteData(string key)` | instance methods | Read or remove a saver key's serialised string from raw saved data during migration |
| `SaveSystem.RecordSavedGameData()` | static method | Triggers all Savers to snapshot their current state into `currentSavedGameData` |
| `SaveSystem.BeforeSceneChange()` / `ApplySavedGameData(SavedGameData)` | static methods | Pre-transition record and post-transition restore hooks; required when bypassing the built-in scene loader |
| `PixelCrushers.SaveSystem.saveDataApplied` | `static event Action` | Fired after save data is fully applied to the scene; subscribe to run code that depends on restored Lua state |
| `SaveSystem.Serialize(SavedGameData)` | `static string` | Serializes a `SavedGameData` snapshot to a JSON string for use with external save systems |
| `SaveSystem.Deserialize<SavedGameData>(string)` | `static T` | Deserializes a JSON string produced by `Serialize` back to `SavedGameData` for `ApplySavedGameData` |
| `StandardSceneTransitionManager` | `MonoBehaviour` | Default `SceneTransitionManager` implementation; controls loading-scene transitions; place on the Save System GameObject |

Add Component menu paths (wrapper versions, preferred for scene use):
- `Pixel Crushers/Save System/Savers/Dialogue System/Dialogue System Saver`
- `Pixel Crushers/Save System/Savers/Dialogue System/Conversation State Saver`

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Dialogue state not restored after `LoadFromSlot` | `saveAcrossSceneChanges` is `false`, or `DialogueSystemSaver` is not on a DontDestroyOnLoad object | Confirm `saveAcrossSceneChanges` is ticked; ensure the Dialogue Manager persists across scenes |
| Lua variables reset on scene change | `SceneManager.LoadScene` used instead of `SaveSystem.LoadScene` | Replace with `PixelCrushers.SaveSystem.LoadScene("SceneName")` |
| "No DataSerializer found" in Console | Save System GameObject is missing a serializer component | Add **Json Data Serializer** to the Save System GameObject |
| Quest states revert after load | `DialogueSystemSaver` key clashes with another Saver component | Ensure every `Saver` has a unique `key` value in the Inspector |
| `LoadFromSlot` silently does nothing | Slot is empty (no previous save) | Call `HasSavedGameInSlot(slot)` before calling `LoadFromSlot` |
| Saved game file is enormous | `saveRawData` ticked with `JsonDataSerializer` | Either untick `saveRawData`, or swap to `BinaryDataSerializer` |
| GameObjects or coroutines freeze during scene exit | `StandardSceneTransitionManager` sets `Time.timeScale = 0` immediately when **Pause During Transition** is enabled | Use `Time.unscaledDeltaTime` in transition animations, or disable **Pause During Transition** on the `StandardSceneTransitionManager` |
| Old save breaks after adding new variables or quests | Serialised structure mismatches new format; version not incremented | Increment **Version** on `SaveSystem`, branch in `Saver.ApplyData` on version number, and enable **Initialize New Variables** on the Dialogue Manager |
| Corgi Engine `ResetProgress` leaves Dialogue System state intact | Default `ResetProgress()` does not clear the `"DialogueSystem"` save folder | Subclass `RetroAdventureProgressManager`, override `ResetProgress()`, and call `MMSaveLoadManager.DeleteSaveFolder("DialogueSystem")` alongside the base reset |
| Dialogue state lost after a More Mountains scene transition | Scene loads before `ApplySavedGameData` is called | Subclass `MMSceneLoadingManager`, call `RecordSavedGameData` and `BeforeSceneChange` before `yield return base.LoadAsynchronously()`, then `ApplySavedGameData` after a `WaitForEndOfFrame` |
| Cannot show slot summary without triggering a scene load | Summary data is not stored in a retrievable Saver | Add a `GameSummarySaver` Saver subclass; read with `storer.RetrieveSavedGameData(slot).GetData("GameSummary")` |
| Startup scripts see default (unrestored) Lua values | `SaveSystem` applies data after `Start()` by default (1-frame delay) | Subscribe to `PixelCrushers.SaveSystem.saveDataApplied` instead of reading state in `Start()`, or set **Frames To Wait Before Apply Data** to `0` |
| Third-party save integration fails to restore Dialogue System state | External load bypasses `ApplySavedGameData`, leaving Lua tables at defaults | Use `SaveSystem.Serialize(RecordSavedGameData())` to capture and `ApplySavedGameData(Deserialize<SavedGameData>(str))` to restore |

## Boundaries

- This skill covers only Dialogue System–specific Saver components and the slot/scene API. For custom `Saver` subclass authoring, storer/serializer configuration, Addressable-based scenes, or save file encryption, see **pixel-crushers-common-save-system**.
- `ConversationStateSaver` records which node was active; it does **not** replay audio, animations, or sequences from before the save point.
- Additive scene loading (`SaveSystem.LoadAdditiveScene` / `UnloadAdditiveScene`) is supported but its configuration is covered by the Common save system skill.
