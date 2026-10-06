# Dialogue System for Unity — Runtime Scripting: Forum Techniques Reference

Detailed workflows and runnable C# snippets derived from official Pixel Crushers forum [HOWTO] posts.
Read the relevant section when the matching scenario arises; do not treat this file as a checklist.

---

## Visual Scripting Integration

**Source:** HOWTO 61 — <https://forum.pixelcrushers.com/post/howto-how-to-get-lua-values-with-visual-scripting-13718235>

Read Dialogue System variables in Unity Visual Scripting (Bolt) with the `Dialogue Lua > Get Variable`
node. Its output is a `Lua.Result` object; unbox it with a
`Dialogue System Visual Scripting Lua > Lua Result As [Type]` node (String, Bool, Float, or Int).

For Dialogue System ≤ 2.2.32: add `[Unity.VisualScripting.IncludeInSettings(true)]` above the
`PixelCrushers.DialogueSystem.DialogueLua` class declaration in `DialogueLua.cs`, or download the
updated Visual Scripting Support package from Dialogue System Extras.

---

## Custom Sequencer Commands

**Sources:** HOWTO 63 (light intensity) | HOWTO 131 (camera home position)

### Setting Light Intensity

```csharp
using UnityEngine;
using PixelCrushers.DialogueSystem.SequencerCommands;

// Sequence syntax: LightIntensity(SubjectName, 0.5)
public class SequencerCommandLightIntensity : SequencerCommand
{
    private Light _light;
    private float _targetIntensity;

    void Awake()
    {
        Transform subject = GetSubject(0);
        _targetIntensity  = GetParameterAsFloat(1);
        _light = subject != null ? subject.GetComponent<Light>() : null;
        if (_light != null) _light.intensity = _targetIntensity;
        Stop();
    }

    // Apply final value if the command is interrupted mid-coroutine.
    void OnDestroy()
    {
        if (_light != null) _light.intensity = _targetIntensity;
    }
}
```

### Overriding the Conversation Camera Home Position

Prevents conversation-end from snapping the camera back to its pre-conversation transform.

```csharp
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.SequencerCommands;

// Sequence syntax: SetCameraHomePosition()
public class SequencerCommandSetCameraHomePosition : SequencerCommand
{
    void Awake()
    {
        var sequencer = DialogueManager.conversationView.sequencer;
        sequencer.originalCameraPosition = sequencer.sequencerCameraTransform.position;
        Stop();
    }
}
```

---

## Runtime Database Creation

**Source:** HOWTO 86 — <https://forum.pixelcrushers.com/post/howto-how-to-create-dialogue-database-at-runtime-13714484>

```csharp
using UnityEngine;
using PixelCrushers.DialogueSystem;

var db       = ScriptableObject.CreateInstance<DialogueDatabase>();
db.baseID    = 10000; // Offset to avoid ID collisions with shipped databases.
var template = Template.FromDefault();

// Actor.
var actor = template.CreateActor(db.baseID + 1, "Gunnery Sergeant", false);
Field.SetValue(actor.fields, "Title", "Gunnery Sergeant");
db.actors.Add(actor);

// Conversation with a START entry.
var conv       = template.CreateConversation(db.baseID + 100, "Greeting");
var startEntry = template.CreateDialogueEntry(0, conv.id, "START");
conv.dialogueEntries.Add(startEntry);
db.conversations.Add(conv);

// Register.
DialogueManager.AddDatabase(db);
// Or merge ID-safely into the master database:
// DatabaseMerger.Merge(DialogueManager.masterDatabase, db,
//     ConflictingIDRule.AssignUniqueIDs, true, true, true);

// Release when done.
DialogueManager.RemoveDatabase(db);
Destroy(db);
```

---

## Procedurally-Generated Barkers

**Source:** HOWTO 72 — <https://forum.pixelcrushers.com/post/howto-how-to-get-procedurallygenerated-barker-transform-13719439>

The Dialogue System populates `Variable["Actor"]` (display name) and `Variable["ActorIndex"]`
(sanitized internal name) at bark start. Resolve the barker's Transform:

```csharp
string actorIndex    = DialogueLua.GetVariable("ActorIndex").asString;
Transform barkerTr   = CharacterInfo.GetRegisteredActorTransform(actorIndex);
```

For multiple procedural barkers sharing a display name, create per-instance database actors:

```csharp
var db   = ScriptableObject.CreateInstance<DialogueDatabase>();
db.baseID = 20000;
var template = Template.FromDefault();
// uniqueInternalName is distinct per instance; displayName may be shared.
var actor = template.CreateActor(db.baseID + instanceId, uniqueInternalName, false);
actor.Name = uniqueInternalName;
db.actors.Add(actor);
DialogueManager.AddDatabase(db);
CharacterInfo.RegisterActorTransform(uniqueInternalName, go.transform);
```

---

## Addressables Audio

**Source:** HOWTO 73 — <https://forum.pixelcrushers.com/post/howto-how-to-use-addressables-to-play-audio-files-13719107>

1. Import `com.unity.addressables` via Package Manager.
2. Enable `USE_ADDRESSABLES` via `Tools > Pixel Crushers > Dialogue System > Welcome Window`.
3. Name audio clips to match their entrytags, tick **Addressable** on each asset in the Inspector,
   and confirm Unity's move prompt so the Addressable key equals the file name.
4. Use `AudioWait(entrytag)` or `AudioWait(entrytaglocal)` in sequences as normal — the sequencer
   resolves and streams addressable clips, releasing memory automatically after playback.

> **Note:** `DialogueManager.RegisterAssetBundle()` is the alternative for AssetBundle workflows;
> Addressables and AssetBundles are mutually exclusive per session.

---

## Extra Databases and Startup Timing

**Source:** HOWTO 80 — <https://forum.pixelcrushers.com/post/howto-how-to-start-conversation-on-start-with-extra-databases-13719986>

`Extra Databases` loads its assets in `Start()`. A `DialogueSystemTrigger` also set to **OnStart**
may run before the databases are available because Unity's execution order is non-deterministic.

**Solutions (pick one):**
- Set `Extra Databases > Add Trigger` to **OnEnable** — assets load before any `Start()` runs.
- Place the Dialogue Manager in an initialization scene loaded before gameplay scenes.
- Set `Save System > Frames To Wait Before Apply Data` to `1` and change the trigger event to
  **OnSaveDataApplied**.
- Set the trigger to **OnUse** and invoke it via a `Timed Event` component (1-frame delay calling
  `DialogueSystemTrigger.OnUse`).

---

## External Data Bridge via Lua

**Source:** HOWTO 81 — <https://forum.pixelcrushers.com/post/howto-how-to-tie-external-data-to-dialogue-database-13716161>

Mirror external database values into the Lua environment:

```csharp
// Push values in (e.g., on a game event or each frame).
DialogueLua.SetVariable("Score", externalDB.score);
DialogueLua.SetActorField("Narrator", "IsPlayer", false);

// Pull values out (e.g., in OnConversationStart).
int score = DialogueLua.GetVariable("Score").asInt;

// Or bridge C# queries directly — no variable duplication required.
void OnEnable()
{
    Lua.RegisterFunction("IsInvulnerable", this,
        SymbolExtensions.GetMethodInfo(() => IsInvulnerable()));
}
void OnDisable() => Lua.UnregisterFunction("IsInvulnerable");
public bool IsInvulnerable() => player.isInvulnerable;
```

Use `Lua.Run("Actor['Narrator'] = { Name='Narrator' }")` to batch-initialize actor tables when
the external source is the system of record for actor data.

---

## Multiple Dialogue UIs for Same Participants

**Source:** HOWTO 85 — <https://forum.pixelcrushers.com/post/howto-how-to-use-multiple-dialogue-uis-for-same-participants-13716103>

Subclass `StandardDialogueUI` to swap panel mode (overlay vs. world-space bubble) per conversation:

```csharp
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;

public class SwappableDialogueUI : StandardDialogueUI
{
    private Dictionary<DialogueActor, SubtitlePanelNumber> _saved = new();

    public override void Open()
    {
        base.Open();
        var conv = DialogueManager.masterDatabase
            .GetConversation(DialogueManager.lastConversationStarted);
        if (conv != null && conv.LookupBool("Overlay"))
        {
            foreach (var da in FindObjectsOfType<DialogueActor>())
            {
                _saved[da] = da.standardDialogueUISettings.subtitlePanelNumber;
                da.standardDialogueUISettings.subtitlePanelNumber = SubtitlePanelNumber.Default;
            }
        }
    }

    public override void Close()
    {
        foreach (var kv in _saved)
            kv.Key.standardDialogueUISettings.subtitlePanelNumber = kv.Value;
        _saved.Clear();
        base.Close();
    }
}
```

---

## Player Controls Across Scene Changes

**Source:** HOWTO 88 — <https://forum.pixelcrushers.com/post/howto-how-to-manage-player-controls-and-scene-changes-13717949>

The Dialogue Manager is a persistent singleton (`Don't Destroy On Load`). Inspector references
wired to scene-specific GameObjects on the Dialogue Manager become stale after scene transitions.

**Pattern:** Place `Dialogue System Events` on the Player GameObject (reloads with each scene).
Leave `Conversation Actor` blank on `DialogueSystemTrigger` so the trigger auto-resolves
the active scene Player.

```csharp
// On the Player GameObject — NOT on the Dialogue Manager.
void OnConversationStart(Transform actor)
{
    playerController.enabled = false;
    selector.enabled = false;
}
void OnConversationEnd(Transform actor)
{
    playerController.enabled = true;
    selector.enabled = true;
}
```

---

## Running Events After a Specific Conversation

**Source:** HOWTO 92 — <https://forum.pixelcrushers.com/post/howto-how-to-run-event-after-specific-conversation-13716597>

```csharp
void OnEnable()
    => DialogueManager.instance.conversationEnded += OnConversationEnded;

void OnDisable()
{
    if (DialogueManager.instance != null)
        DialogueManager.instance.conversationEnded -= OnConversationEnded;
}

void OnConversationEnded(Transform actor)
{
    if (DialogueManager.lastConversationStarted == "Village Elder")
        TriggerCutscene();
}
```

Alternatively, attach a `Dialogue System Events` component or a `DialogueSystemTrigger` set to
**OnConversationEnd** directly to the specific NPC so the event fires only for that participant.
For final-frame actions, use a terminal node with Sequence `required SetDialoguePanel(false); Continue()`.

---

## Stopping One Conversation When Multiple Are Active

**Source:** HOWTO 98 — <https://forum.pixelcrushers.com/post/howto-how-to-kill-specific-conversation-when-simultaneous-are-active-13721230>

`DialogueManager.StopAllConversations()` terminates every active conversation. To stop only one:

```csharp
foreach (var record in DialogueManager.instance.activeConversations)
{
    if (record.conversationTitle == "Ambient Gossip")
    {
        record.conversationController.Close();
        break;
    }
}
```

---

## Reading Custom Database Fields at Runtime

**Source:** HOWTO 102 — <https://forum.pixelcrushers.com/post/howto-how-to-read-custom-fields-from-dialogue-database-13720673>

```csharp
// Static design-time value (not affected by Lua mutations during play):
var actor   = DialogueManager.masterDatabase.GetActor("Charles");
string rank = actor.LookupValue("Rank");
// Equivalent: Field.LookupValue(actor.fields, "Rank")

// Dynamic runtime value (reflects Lua changes made during the session):
string rank = DialogueLua.GetActorField("Charles", "Rank").asString;

// Equivalent helpers for other asset types:
// DialogueLua.GetQuestField(questName, fieldName)
// DialogueLua.GetItemField(itemName, fieldName)
// DialogueLua.GetVariable(variableName)
```

Use `masterDatabase` reads only for authoring-time constants. Use `DialogueLua` for any value
that Conditions or Scripts in the Dialogue Editor may have mutated at runtime.

---

## Looping Timeline Animation While Awaiting Continue

**Source:** HOWTO 105 — <https://forum.pixelcrushers.com/post/howto-how-to-loop-timeline-animation-while-waiting-for-continue-13721099>

A paused `PlayableDirector` controlling Animator tracks freezes character animation during
continue-button waits. Two approaches:

**Option A — Per-entry timelines with an Animator fallback:**
```
// Node Sequence field:
Timeline(play, entrytag)->Message(Waiting); AnimatorPlay(Idle, speaker)@Message(Waiting);
```
The timeline plays and sends a message on completion; `AnimatorPlay` then loops the idle clip.

**Option B — Single conversation timeline with Animator parameter tracks:**
Use custom timeline tracks that drive Animator parameters (not direct control clips), so parameters
continue animating even when the director is paused at a signal emitter.

For response menus, enable **Add Response Menu Sequence** on the node to run looping sequences
while the player reads and selects a response.

---

## SMSDialogueUI / TextlineDialogueUI History

**Source:** HOWTO 107 — <https://forum.pixelcrushers.com/post/howto-how-to-get-last-subtitle-from-smsdialogueui-textlinedialogueui-conversations-13717350>

`SMSDialogueUI` and `TextlineDialogueUI` persist history as a semicolon-delimited Lua string.
To retrieve the last spoken line after a conversation ends:

```csharp
// Commit state first.
dialogueUI.OnRecordPersistentData();

string records = DialogueLua.GetVariable(dialogueUI.currentDialogueEntryRecords).AsString;
string[] parts = records.Split(';');
// Last two non-empty tokens encode conversationID and entryID.
int convId  = Tools.StringToInt(parts[parts.Length - 3]);
int entryId = Tools.StringToInt(parts[parts.Length - 2]);
string lastLine = DialogueManager.masterDatabase
    .GetDialogueEntry(convId, entryId).currentDialogueText;
```

---

## Conversation Notifications on Non-Primary Characters

**Source:** HOWTO 115 — <https://forum.pixelcrushers.com/post/howto-how-to-get-conversation-startend-notifications-on-nonprimary-characters-13720684>

The Dialogue System sends `OnConversationStart`/`OnConversationEnd` only to the primary actor,
conversant, and Dialogue Manager. Forward events to secondary participants:

```csharp
void OnConversationEnd(Transform actor)
{
    var conv    = DialogueManager.masterDatabase
        .GetConversation(DialogueManager.lastConversationID);
    var notified = new HashSet<int> { conv.ActorID, conv.ConversantID };

    foreach (var entry in conv.dialogueEntries)
    {
        if (notified.Contains(entry.ActorID)) continue;
        notified.Add(entry.ActorID);
        var dbActor = DialogueManager.masterDatabase.GetActor(entry.ActorID);
        if (dbActor == null) continue;
        var t = CharacterInfo.GetRegisteredActorTransform(dbActor.Name);
        if (t != null)
            t.BroadcastMessage(DialogueSystemMessages.OnConversationEnd,
                DialogueManager.currentActor,
                SendMessageOptions.DontRequireReceiver);
    }
}
```

Attach the script above to the Dialogue Manager. Characters are found via `CharacterInfo`; having
`DialogueActor` components on scene NPCs ensures their transforms are registered automatically.

---

## Replacing a Script with a Subclass

**Source:** HOWTO 116 — <https://forum.pixelcrushers.com/post/howto-how-to-replace-script-with-subclass-and-keep-field-assignments-13716105>

Switch a component from its base class to a subclass while retaining all Inspector field values:

1. In the Inspector header, click ⋮ → **Debug**.
2. Locate the `Script` (`m_Script`) field at the top of the component.
3. Drag the new subclass `.cs` asset into the `Script` field.
4. Switch back to **Normal** mode and confirm fields are intact.

In Unity 2022+, unpack prefab instances in the Hierarchy before swapping, then re-apply to the prefab.

---

## Sequences Without UI at Conversation Boundaries

**Source:** HOWTO 126 — <https://forum.pixelcrushers.com/post/howto-how-to-play-sequences-without-ui-at-conversation-startend-13718178>

### Opening Cutscene (hide UI, play audio, re-show UI)

```
// First node Sequence (passthrough — no subtitle text):
SetDialoguePanel(false, immediate)

// Child node Sequence:
AudioWait(OpeningVO)->Message(Done); SetDialoguePanel(true)@Message(Done);
```

### Closing Cutscene (hide UI for the final node)

```
// Final node Sequence:
SetDialoguePanel(false); AudioWait(ClosingVO)
```

---

## Variadic Lua Functions

**Source:** HOWTO 133 — <https://forum.pixelcrushers.com/post/howto-how-to-register-lua-function-with-arbitrary-number-of-parameters-13782915>

Use `Lua.Environment.Register` (not `Lua.RegisterFunction`) to expose a C# method that accepts
any number of Lua arguments:

```csharp
using Language.Lua;
using PixelCrushers.DialogueSystem;

public class VariadicLuaBridge : MonoBehaviour
{
    void OnEnable()
        => Lua.Environment.Register(nameof(SumValues), SumValues);

    // Called from Lua as: SumValues(1, 2, 3)
    public static LuaValue SumValues(LuaValue[] values)
    {
        double total = 0;
        foreach (var v in values)
            if (v is LuaNumber n) total += n.value;
        return new LuaNumber(total);
    }
}
```

Supported return types: `new LuaNumber(double)`, `new LuaBoolean(bool)`, `new LuaString(string)`.
Inspect argument types with `value is LuaNumber luaNumber`, `value is LuaString luaString`, etc.
`Language.Lua` does not expose a symmetric unregister API; disable the MonoBehaviour to
make the function unavailable.
