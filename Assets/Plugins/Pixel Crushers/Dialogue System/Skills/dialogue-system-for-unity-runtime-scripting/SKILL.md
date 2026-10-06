---
name: dialogue-system-for-unity-runtime-scripting
description: "Use this skill whenever someone wants to drive the Dialogue System for Unity from C# — e.g. 'start a conversation from code', 'trigger dialogue in a script', 'read a Lua variable in C#', 'set a Lua variable from code', 'how do I know when a conversation ends', 'subscribe to a conversation event', 'run Lua from C#', 'bark from code', 'show an alert message'. Covers DialogueManager.StartConversation/StopConversation/Bark/ShowAlert, DialogueLua.GetVariable/SetVariable, Lua.Run and Lua.RegisterFunction, and subscribing to lifecycle events via the DialogueSystemEvents component or OnConversation* message methods. Do NOT use for quest state (see dialogue-system-for-unity-quests), authoring content in the editor (see dialogue-system-for-unity-authoring-conversations), or no-code scene triggers (see dialogue-system-for-unity-scene-interaction). When in doubt whether a scripting request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Dialogue System for Unity — Runtime Scripting

Drive conversations, barks, alerts, and Lua variables from C# code using the `DialogueManager` and `DialogueLua` static classes, and subscribe to conversation lifecycle events.

## When to use this skill

Use this skill when the task involves any of the following:
- Starting or stopping a conversation from code (`DialogueManager.StartConversation`, `StopConversation`)
- Playing a bark (one-off line) from code (`DialogueManager.Bark`)
- Showing or hiding an alert (`DialogueManager.ShowAlert`, `HideAlert`)
- Reading or writing Lua variables (`DialogueLua.GetVariable`, `SetVariable`)
- Running arbitrary Lua code (`Lua.Run`)
- Registering or unregistering C# methods callable from Lua (`Lua.RegisterFunction`, `UnregisterFunction`)
- Subscribing to conversation or quest events via `DialogueSystemEvents` or MonoBehaviour messages

## Prerequisites

**Verify the asset is installed** before generating or editing any code:

```csharp
bool installed = false;
foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
{
    if (asm.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null)
    {
        installed = true;
        break;
    }
}
// Do NOT use Type.GetType("...") — the type lives in a separate assembly, not Assembly-CSharp.
```

**Namespace rule:**
- Core API used in C# code lives in: `PixelCrushers.DialogueSystem`
- Scene components added via **Add Component** menus must use the wrapper versions: `PixelCrushers.DialogueSystem.Wrappers` (e.g., add **Dialogue System Events** from the component menu, not the internal class directly)

**Scene requirement:** A **Dialogue Manager** GameObject must be present in the scene (it hosts the singleton `DialogueSystemController`). Without it, all `DialogueManager.*` static calls silently do nothing. Add one via `Tools > Pixel Crushers > Dialogue System > Setup Wizard`.

**Dialogue Editor:** `Tools > Pixel Crushers > Dialogue System > Dialogue Editor` — conversations must be authored there first and referenced by their exact title string at runtime.

## Quick start

```csharp
using UnityEngine;
using PixelCrushers.DialogueSystem;

public class ConversationStarter : MonoBehaviour
{
    [SerializeField] private string conversationTitle = "Village Elder";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            var player = GameObject.FindWithTag("Player");
            DialogueManager.StartConversation(conversationTitle, player.transform, transform);
        }
    }

    // MonoBehaviour message — fires on this GameObject when it is the actor or conversant.
    // Also fires on the Dialogue Manager GameObject itself.
    void OnConversationStart(Transform actor)
    {
        Debug.Log("Conversation started with actor: " + actor.name);
    }

    void OnConversationEnd(Transform actor)
    {
        Debug.Log("Conversation ended.");
    }
}
```

## Workflows

### Workflow: Start and stop a conversation

**Goal:** Trigger a specific conversation from a game event (e.g., player interacts with NPC, reaches a checkpoint).

**Steps:**
1. Confirm the conversation title exists in the Dialogue Database (Dialogue Editor → Conversations list).
2. Call `DialogueManager.StartConversation(title, actorTransform, conversantTransform)`.
   - `actor` is typically the player; `conversant` is typically the NPC.
   - Both may be `null` if the conversation has no Transform-dependent sequences.
3. Guard against double-starts by checking `DialogueManager.isConversationActive` first.
4. To end a conversation early: `DialogueManager.StopConversation()`.

```csharp
if (!DialogueManager.isConversationActive)
{
    DialogueManager.StartConversation("Shopkeeper", playerTr, npcTr);
}
```

**Expected result:** The dialogue UI opens and runs the conversation, then closes automatically at the END node.

---

### Workflow: Read and write Lua variables

**Goal:** Persist and query game state inside the Dialogue System's Lua environment so it can drive conditions and scripts authored in the Dialogue Editor.

**Steps:**

Variables are defined in **Dialogue Editor → Variables** tab. Access them with `DialogueLua`:

```csharp
using PixelCrushers.DialogueSystem;

// GetVariable returns Lua.Result; access typed value via .asString / .asBool / .asInt / .asFloat
int kills     = DialogueLua.GetVariable("Kills").asInt;
bool metHero  = DialogueLua.GetVariable("MetHero").asBool;
string name   = DialogueLua.GetVariable("PlayerName").asString;
float rating  = DialogueLua.GetVariable("Reputation").asFloat;

// SetVariable accepts any object; the Lua runtime converts the type
DialogueLua.SetVariable("Kills", kills + 1);
DialogueLua.SetVariable("MetHero", true);
DialogueLua.SetVariable("PlayerName", "Aria");
```

> **Name note:** Variable names with spaces are stored in Lua with underscores (e.g., `"Player Name"` → `Variable["Player_Name"]`). Always pass the original name string to `GetVariable`/`SetVariable`; the conversion is handled internally.

**Expected result:** Changes are immediately visible in the Dialogue Editor's **Variable Viewer** (`Tools > Pixel Crushers > Dialogue System > Tools > Variable Viewer`) during Play mode and take effect in any active or subsequent conversations.

---

### Workflow: Run arbitrary Lua code

**Goal:** Execute Lua statements or expressions that batch-set variables, call Lua built-ins, or manipulate tables not directly exposed by `DialogueLua` helpers.

**Steps:**

```csharp
using PixelCrushers.DialogueSystem;

// Run a statement (no return value needed)
Lua.Run("Variable['Kills'] = Variable['Kills'] + 1");

// Run and capture a return value; use .asInt / .asString / .asBool / .asFloat on Lua.Result
int kills = Lua.Run("return Variable['Kills']").asInt;

// Call a Dialogue System Lua function registered by QuestLog
Lua.Run("SetQuestState('Kill 5 Rats', 'active')");

// Evaluate a boolean condition
bool ready = Lua.IsTrue("Variable['Kills'] >= 5");
```

**Expected result:** Lua environment updated; return value accessible on the returned `Lua.Result` struct.

---

### Workflow: Register a C# method in Lua

**Goal:** Make a C# method callable from the Dialogue Editor's Condition or Script fields.

**Steps:**

```csharp
using System.Reflection;
using PixelCrushers.DialogueSystem;

public class MyLuaBridge : MonoBehaviour
{
    void OnEnable()
    {
        // Pass null as target for static methods; pass 'this' for instance methods.
        Lua.RegisterFunction("GiveItem", this,
            typeof(MyLuaBridge).GetMethod("GiveItem"));
    }

    void OnDisable()
    {
        Lua.UnregisterFunction("GiveItem");
    }

    // Callable in Lua as: GiveItem("Sword")
    public void GiveItem(string itemName)
    {
        Debug.Log("Giving item: " + itemName);
    }
}
```

> **IL2CPP note:** Use `typeof(MyClass).GetMethod("MethodName")` rather than lambda-based reflection helpers — lambdas break on IL2CPP platforms (Unity 2017.3+).

**Expected result:** Dialogue entries can call `GiveItem("Sword")` in their **Script** field; Unity executes the C# method.

---

### Workflow: Subscribe to conversation events

**Goal:** React to conversation lifecycle points (start, line displayed, end) from code or the Inspector.

**Inspector wiring (no code):**
1. Add the **Dialogue System Events** component (Wrappers version) to the Dialogue Manager or a participant GameObject.
2. Expand **Conversation Events** in the Inspector.
3. Wire `onConversationStart`, `onConversationEnd`, `onConversationLine`, etc. to UnityEvent target methods.

**From code at runtime:**

```csharp
var dsEvents = DialogueManager.instance.GetComponent<DialogueSystemEvents>();
if (dsEvents != null)
{
    // onConversationStart passes the actor Transform
    dsEvents.conversationEvents.onConversationStart.AddListener(OnConvStart);
    // onConversationEnd passes the actor Transform
    dsEvents.conversationEvents.onConversationEnd.AddListener(OnConvEnd);
}

void OnConvStart(Transform actor) { /* disable player movement, etc. */ }
void OnConvEnd(Transform actor)   { /* re-enable player movement, etc. */ }
```

**MonoBehaviour message alternative:** Implement `void OnConversationStart(Transform actor)` / `void OnConversationEnd(Transform actor)` on a MonoBehaviour attached to the actor, conversant, or Dialogue Manager GameObject. The Dialogue System uses `SendMessage` to notify those objects.

**Expected result:** Listener fires at the correct conversation lifecycle point without modifying the Dialogue System source.

---

### Workflow: Bark a line from code

**Goal:** Have a character speak a one-off bark line without opening the full conversation UI.

**Steps:**

```csharp
// Random bark from a conversation named "Guard Barks"
DialogueManager.Bark("Guard Barks", npcTransform);

// Bark at a specific target (listener can be null)
DialogueManager.Bark("Guard Barks", npcTransform, playerTransform);

// Cycle through bark lines sequentially using a BarkHistory
var history = new BarkHistory(BarkOrder.Sequential);
DialogueManager.Bark("Guard Barks", npcTransform, playerTransform, history);
```

**Expected result:** Bark UI (world-space bubble or subtitle) shows the chosen line above the speaker.

---

### Workflow: Interrupt or redirect a conversation mid-flight

**Goal:** Stop the active conversation, jump to a specific entry, or push/pop a saved position so dialogue can be interrupted and resumed cleanly.

**Steps:**
1. **Hard interrupt:** `DialogueManager.StopConversation()` then `DialogueManager.StartConversation(...)`.
2. **Push/pop resume:** call `ConversationPositionStack.PushConversationPosition()` before the interrupt; call `ConversationPositionStack.PopConversationPosition()` to resume at the saved entry later.
3. **Jump to a specific entry mid-conversation:**
   ```csharp
   var newState = DialogueManager.conversationModel.GetState(targetEntry, true);
   DialogueManager.conversationController.GotoState(newState);
   ```
   Always execute the jump inside a custom `SequencerCommand` subclass or after `yield return new WaitForEndOfFrame()` — the current node must finish processing before transitioning.

**Expected result:** The conversation resumes at the intended entry without replaying earlier Script fields or response menus.

---

### Workflow: End the game or load a scene from a terminal node

**Goal:** Quit, transition to a Game Over scene, or hold the final subtitle on screen indefinitely — triggered from a conversation endpoint.

**Steps:**
1. Register a C# method as a Lua function using `SymbolExtensions.GetMethodInfo` (IL2CPP-safe expression-tree reflection, `PixelCrushers` namespace):
   ```csharp
   void OnEnable() =>
       Lua.RegisterFunction(nameof(EndGame), this,
           SymbolExtensions.GetMethodInfo(() => EndGame()));
   public void EndGame() => Application.Quit();
   ```
2. Call `EndGame()` in the terminal entry's **Script** field.
3. Game Over scene transition: set the entry's **Sequence** to `required LoadLevel(GameOverScene); Continue()`.
4. Permanent last line: set **Sequence** to `SetContinueMode(false); WaitForMessage(Forever)`.

**Expected result:** The terminal node quits, loads a scene, or freezes on the final subtitle as configured.

---

### Workflow: Start a coroutine from a Lua call

**Goal:** Trigger a C# coroutine from an entry Script or Condition field without blocking dialogue progression.

**Steps:**
1. Register a `void` wrapper that starts the coroutine — Lua calls the wrapper, not the coroutine directly:
   ```csharp
   void OnEnable() =>
       Lua.RegisterFunction(nameof(LaunchRoutine), this,
           SymbolExtensions.GetMethodInfo(() => LaunchRoutine()));
   void OnDisable() => Lua.UnregisterFunction(nameof(LaunchRoutine));
   void LaunchRoutine() => StartCoroutine(MyCoroutine());
   ```
2. Call `LaunchRoutine()` in the entry **Script** field — dialogue continues immediately (fire-and-forget).
3. If dialogue must wait for the coroutine to complete, write a `SequencerCommand` subclass with `IEnumerator Start()` that calls `Stop()` when done.

**Expected result:** The coroutine fires without errors; dialogue waits only when a `SequencerCommand` enforces synchronisation.

---

### Workflow: Expose C# variables to Lua with getter/setter wrappers

**Goal:** Let Dialogue Editor Condition and Script fields read and write live C# values directly, without polling `DialogueLua.SetVariable`.

**Steps:**
```csharp
using PixelCrushers.DialogueSystem;
using PixelCrushers; // SymbolExtensions lives here

public class LuaBridge : MonoBehaviour
{
    private int _numCoins;

    void OnEnable()
    {
        Lua.RegisterFunction(nameof(GetNumCoins), this,
            SymbolExtensions.GetMethodInfo(() => GetNumCoins()));
        Lua.RegisterFunction(nameof(SetNumCoins), this,
            SymbolExtensions.GetMethodInfo(() => SetNumCoins(0d)));
    }
    void OnDisable()
    {
        Lua.UnregisterFunction(nameof(GetNumCoins));
        Lua.UnregisterFunction(nameof(SetNumCoins));
    }

    public double GetNumCoins() => (double)_numCoins;
    // Lua numbers are always double — cast to the desired C# type inside the setter.
    public void SetNumCoins(double v) { _numCoins = (int)v; }
}
```
Reference in dialogue text: `[lua(GetNumCoins())]`. Condition example: `GetNumCoins() >= 10`.

**Expected result:** Conditions and Scripts read live C# state with no extra synchronisation code.

---

### Workflow: Drive conversation logic without the standard UI

**Goal:** Run conversation state for voice-only output, a custom renderer, or automated tests — without any uGUI panels.

**Steps:**
1. **Read raw entries:**
   ```csharp
   var entries = DialogueManager.masterDatabase
       .GetConversation("Title").dialogueEntries;
   ```
2. **Step states with `ConversationModel`:**
   ```csharp
   var model = new ConversationModel(DialogueManager.masterDatabase,
       "Title", actorTransform, conversantTransform, true, null);
   ConversationState state = model.firstState;
   while (state != null && !state.isEnd)
   {
       // Inspect state.subtitle.formattedText.text and state.responses.
       state = model.GetState(chosenEntry, true);
   }
   ```
3. **Full custom view:** implement `IDialogueUI` and assign the instance to `DialogueManager.dialogueUI` before calling `StartConversation`.

**Expected result:** Conversation evaluates Lua, Scripts, and links entirely in C# without Unity UI components.

### Advanced runtime patterns

For extended techniques — runtime database creation, Addressables audio, multiple dialogue UIs, variadic Lua registration, Timeline animation looping, SMSDialogueUI history, and secondary-actor conversation notifications — see **[`references/forum-techniques.md`](references/forum-techniques.md)**.

## Verification

After implementing runtime scripting:
1. Open **Variable Viewer** (`Tools > Pixel Crushers > Dialogue System > Tools > Variable Viewer`) during Play mode to confirm variable values.
2. Enable **Debug Level: Info** on the Dialogue Manager component and watch the Console for `Dialogue System:` prefixed logs.
3. Assert `DialogueManager.isConversationActive == true` while a conversation should be running.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `DialogueManager.StartConversation(title, actor, conversant)` | `static void` | Starts named conversation; actor/conversant may be null |
| `DialogueManager.StartConversation(title)` | `static void` | Starts conversation with no participant Transforms |
| `DialogueManager.StopConversation()` | `static void` | Stops the active conversation immediately |
| `DialogueManager.isConversationActive` | `static bool` | True while a conversation is running |
| `DialogueManager.lastConversationStarted` | `static string` | Title of the most recently started conversation |
| `DialogueManager.Bark(title, speaker)` | `static void` | Random bark from a conversation |
| `DialogueManager.Bark(title, speaker, listener, history)` | `static void` | Bark with history tracking |
| `DialogueManager.ShowAlert(message)` | `static void` | Shows an alert in the dialogue UI |
| `DialogueManager.ShowAlert(message, duration)` | `static void` | Shows alert for a specific duration |
| `DialogueManager.HideAlert()` | `static void` | Hides the current alert |
| `DialogueManager.AddDatabase(db)` | `static void` | Adds a DialogueDatabase at runtime |
| `DialogueManager.RemoveDatabase(db)` | `static void` | Removes a DialogueDatabase at runtime |
| `DialogueLua.GetVariable(name)` | `static Lua.Result` | Returns variable; access via `.asString`, `.asBool`, `.asInt`, `.asFloat` |
| `DialogueLua.SetVariable(name, value)` | `static void` | Sets a Lua variable (string, bool, int, float, or object) |
| `DialogueLua.GetActorField(actor, field)` | `static Lua.Result` | Gets a field from the Actor Lua table |
| `DialogueLua.SetActorField(actor, field, value)` | `static void` | Sets a field in the Actor Lua table |
| `DialogueLua.GetQuestField(quest, field)` | `static Lua.Result` | Gets a raw field from the Quest/Item Lua table |
| `Lua.Run(code)` | `static Lua.Result` | Executes a Lua code string |
| `Lua.IsTrue(condition)` | `static bool` | Evaluates a Lua boolean expression |
| `Lua.RegisterFunction(name, target, method)` | `static void` | Registers a C# MethodInfo as a Lua function |
| `Lua.UnregisterFunction(name)` | `static void` | Removes a registered Lua function |
| `SymbolExtensions.GetMethodInfo(() => Method())` | `static MethodInfo` | Expression-tree helper (`PixelCrushers` namespace); IL2CPP-safe alternative to string-based reflection in `Lua.RegisterFunction` |
| `ConversationPositionStack.PushConversationPosition()` / `PopConversationPosition()` | `static void` | Saves and restores the active conversation's current entry position for interrupt/resume patterns |
| `ConversationModel` | class | Headless conversation state machine; step dialogue logic (Lua, links, conditions) without any uGUI panels |
| `DatabaseMerger.Merge(target, source, ConflictingIDRule.AssignUniqueIDs, ...)` | `static void` | Merges a runtime `DialogueDatabase` into another; resolves ID conflicts — see `references/forum-techniques.md` |
| `Lua.Environment.Register(name, func)` | `static void` | Registers a variadic `LuaValue[] → LuaValue` method in Lua; requires `Language.Lua` namespace — see `references/forum-techniques.md` |
| `DialogueManager.instance.activeConversations` | `IList` | Enumerable of active records; each exposes `.conversationTitle` and `.conversationController.Close()` to stop one conversation |
| `Field.LookupValue(asset.fields, name)` / `asset.LookupValue(name)` | `string` | Reads a static design-time field from a database asset; use `DialogueLua.GetActorField` etc. for runtime-mutated values |
| `CharacterInfo.GetRegisteredActorTransform(name)` | `static Transform` | Resolves an actor's scene `Transform` by internal name; works best when actors have `DialogueActor` components |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| `StartConversation` does nothing, no errors | No Dialogue Manager in scene | Add one via Setup Wizard or place the prefab manually |
| Conversation doesn't start; `isConversationActive` is true | Simultaneous conversations blocked | Set `allowSimultaneousConversations`, or call `StopConversation()` first |
| `GetVariable` returns `""` / `0` / `false` | Variable not defined or database not loaded | Confirm name in Dialogue Editor Variables tab; call `DialogueManager.PreloadMasterDatabase()` in `Start()` |
| `OnConversationStart` never fires | Script not on actor, conversant, or Dialogue Manager | Move the MonoBehaviour, or use `DialogueSystemEvents` component instead |
| Duplicate-function warning in Console | `RegisterFunction` called without matching `UnregisterFunction` | Always `UnregisterFunction` in `OnDisable` to balance `OnEnable` registrations |
| IL2CPP build fails at Lua registration | Lambda reflection unsupported on IL2CPP | Replace lambda helpers with `typeof(MyClass).GetMethod("MethodName")` |
| Outgoing link conditions evaluate unexpectedly early | Links evaluate immediately after Script/OnExecute on the current node | Set all state-gating variables in the current node's Script before any outgoing link leads to the next node |
| Interrupted conversation replays prior responses after `GotoState` | `GetState` executes entry Script fields before the node is active | Perform the jump inside a custom `SequencerCommand` or after `yield return new WaitForEndOfFrame()` |
| Subtitle bubble always uses the default background | Single `StandardUISubtitlePanel`; no per-entry selection | In `OnConversationLine`, read `Field.LookupInt(entry.fields, "Bubble")` and set `DialogueActor.standardDialogueUISettings.subtitlePanelNumber` accordingly |
| Lua error when calling a C# `IEnumerator` directly | Coroutine methods are not valid Lua function signatures | Register a `void` wrapper that calls `StartCoroutine`; use a `SequencerCommand` subclass if dialogue must wait |
| Response buttons confirm immediately with no intermediate prompt | `StandardUIResponseButton.OnClick()` fires without pause | Subclass `StandardUIResponseButton`, intercept `OnClick()`, show a confirmation panel, call `base.OnClick()` only on confirm |
| Concurrent alerts overwrite each other | Single `StandardUIAlertControls` queue handles all alerts | Subclass `StandardDialogueUI`, add secondary `StandardUIAlertControls` fields, and route by a `[panel=N]` markup tag in `ShowAlert` |
| Conversation freezes on the final line when using a Timeline | Missing final **Continue Conversation** clip or Timeline playback speed not restored | Subclass `StandardUIContinueButtonFastForward` as `ContinueButtonResumeTimeline`; call `playableDirector.playableGraph.GetRootPlayable(0).SetSpeed(1)` on click |
| Lua errors passing numbers to a C# registered setter | Setter parameter declared as `int` or `float` instead of `double` | Change the parameter type to `double` and cast inside: `void SetCoins(double v) { _coins = (int)v; }` |
| `DialogueSystemTrigger` set to **OnStart** fails to find conversations from extra databases | `Extra Databases` also runs `Start()`; Unity execution order is non-deterministic | Set Extra Databases **Add Trigger** to `OnEnable`, or trigger via Save System **OnSaveDataApplied**, or delay 1 frame with a `Timed Event` — see `references/forum-techniques.md` |
| `Dialogue System Events` on the Dialogue Manager loses the Player reference after a scene change | Persistent Dialogue Manager retains the destroyed previous-scene reference | Move `Dialogue System Events` onto the Player GameObject; leave `Conversation Actor` blank on triggers |

## Boundaries

This skill covers **runtime C# code → Dialogue System** integration only.

- Quest state management → **dialogue-system-for-unity-quests**
- Authoring conversation content → **dialogue-system-for-unity-authoring-conversations**
- Save/load of Lua and conversation state → **dialogue-system-for-unity-save-system**
- Inspector-only trigger wiring (no code) → **dialogue-system-for-unity-scene-interaction**
