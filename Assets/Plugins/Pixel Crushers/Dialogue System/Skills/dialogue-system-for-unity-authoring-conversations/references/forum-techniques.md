# Forum Techniques — Authoring Conversations

Supplementary reference for the `dialogue-system-for-unity-authoring-conversations` skill.
All techniques are sourced from verified Pixel Crushers support-forum [HOWTO] posts.
All types are in namespace `PixelCrushers.DialogueSystem` unless stated otherwise.

---

## Input and Text Entry

### TextInput on mobile devices (entry 62)

When using the `TextInput()` sequencer command on touch devices, open the dialogue UI's
`StandardUIInputField` component and enable **Show Touch Screen Keyboard** so the OS
virtual keyboard appears. Add a Submit UI Button and wire its **OnClick()** to
`StandardUIInputField.AcceptTextInput`.

### Handle player gender in dialogue text (entry 97)

For pronoun substitution, initialize a Lua variable at startup:

```csharp
DialogueLua.SetVariable("PlayerHeShe", isMale ? "he" : "she");
```

Insert `[var=PlayerHeShe]` in dialogue entries. For gendered word agreements (e.g. French
adjectives), use custom markup like `{{danseur/danseuse}}` and resolve it in
`OnConversationLine` on the Dialogue Manager:

```csharp
void OnConversationLine(Subtitle subtitle)
{
    subtitle.formattedText.text = Regex.Replace(
        subtitle.formattedText.text,
        @"\{\{(\w+)/(\w+)\}\}",
        m => isMale ? m.Groups[1].Value : m.Groups[2].Value);
}
```

Alternatively, register `Lua.RegisterFunction("Gender", ...)` and call
`[lua(Gender("danseur/danseuse"))]` inline in dialogue text.

### Platform-specific text and audio (entry 99)

For platform-specific text, use `[var=MenuKey]` in dialogue entries and set the variable
at startup inside `#if` blocks: `DialogueLua.SetVariable("MenuKey", "Tab")`.

For platform-specific audio, embed a token in the Sequence field (e.g.
`AudioWait(entrytaglocal{{platform}})`), then replace it in `OnConversationLine`:

```csharp
void OnConversationLine(Subtitle subtitle)
{
#if UNITY_WEBGL
    subtitle.sequence = subtitle.sequence?.Replace("{{platform}}", "_webgl");
#else
    subtitle.sequence = subtitle.sequence?.Replace("{{platform}}", "");
#endif
}
```

---

## Response Menus and Continue Behavior

### Create conversations without response menus (entry 64)

For NPC-only linear sequences, set the **Actor** on all nodes to an NPC actor (Is Player
= unticked). Hold **Shift** while right-clicking → **Create Child Node** to retain the
same actor on subsequent nodes. To include player lines without an interactive menu:
link each player node to at most one valid child, enable **Dialogue Manager > Display
Settings > Subtitle Settings > Show PC Subtitles During Line**, and untick **Input
Settings > Always Force Response Menu**. Set **Continue Button** to `Always` for
explicit per-line progression.

### Bypass response menu when player has one choice (entry 118)

Enable **Subtitle Settings > Show PC Subtitles During Line** and disable **Input Settings
> Always Force Response Menu**. Optionally enable **Skip PC Subtitle After Response Menu**.
Override per-conversation via **Conversation Properties > Override Display Settings**, or
per-node with `[auto]` in **Dialogue Text** (forces subtitle) or `[f]` (forces menu).

### Continue with a dedicated key or button (entry 83)

Set global continue behavior via **Dialogue Manager > Display Settings > Subtitle Settings
> Continue Button**. Override per-conversation via **Dialogue Editor > Conversation
Properties > Override Display Settings**. Override dynamically with the `SetContinueMode()`
sequencer command. Wire buttons to `StandardUISubtitlePanel.OnContinue()` or to
`StandardUIContinueButtonFastForward.OnFastForward()`. To map a dedicated hotkey, attach
`UIButtonKeyTrigger` and assign the key or button name; do not map Space or Return (the
EventSystem already handles those and causes double-triggers).

### Auto-play and skip to player response (entry 75)

Attach `ConversationControl` (requires `AbstractDialogueUI` on the same GameObject) to
the dialogue UI and wire UI buttons to its public methods:

- `ToggleAutoPlay()` — toggles `DialogueManager.displaySettings.subtitleSettings.continueButton`
  between `ContinueButtonMode.Always` and `ContinueButtonMode.Never`; calls
  `dialogueUI.OnContinueConversation()` when switching to Never.
- `SkipAll()` — sets an internal `skipAll` flag; while active, `OnConversationLine`
  overrides `subtitle.sequence = "Continue()"`. `OnConversationResponseMenu` clears the
  flag and redisplays the last subtitle via
  `dialogueUI.ShowSubtitle(DialogueManager.currentConversationState.subtitle)`.

### Set continue button label to Continue or End (entry 130)

Attach a script to the continue button GameObject. In `OnEnable()`, read conversation
state and assign a localized label:

```csharp
void OnEnable()
{
    if (!DialogueManager.isConversationActive) return;
    bool hasMore = DialogueManager.currentConversationState.hasAnyResponses;
    string key   = hasMore ? "Continue" : "End";
    GetComponent<TMPro.TextMeshProUGUI>().text =
        DialogueManager.GetLocalizedText(key);
    // or: GetComponent<UnityEngine.UI.Text>().text = ...
}
```

### Apply previously-shown formatting to response menus and subtitle text (entry 121)

Enable **Dialogue Manager > Other Settings > Include SimStatus** and assign **Input
Settings > [em#] Tag For Old Responses** (configured under Emphasis Settings). The
`[em#]` tags format response menu buttons automatically. To also style the subtitle text
of a selected entry that was previously shown:

```csharp
[SerializeField] int _emTagNum = 1;

void OnConversationLine(Subtitle subtitle)
{
    if (DialogueLua.GetSimStatus(subtitle.dialogueEntry) == DialogueLua.WasDisplayed)
        subtitle.formattedText = FormattedText.Parse(
            $"[em{_emTagNum}]{subtitle.formattedText.text}[/em{_emTagNum}]");
}
```

---

## Accumulate Text and Subtitle Panels

### Share accumulated text across sibling subtitle panels (entry 77)

`StandardUISubtitlePanel.accumulatedText` is local to each panel instance. To synchronize
history across panels that share a common parent (e.g. when portrait panels switch between
characters), subclass as `SharedTextSubtitlePanel`:

```csharp
namespace PixelCrushers.DialogueSystem
{
    public class SharedTextSubtitlePanel : StandardUISubtitlePanel
    {
        List<SharedTextSubtitlePanel> _siblings;

        void Awake() =>
            _siblings = transform.parent
                .GetComponentsInChildren<SharedTextSubtitlePanel>()
                .Where(p => p != this).ToList();

        public override void ClearText()
        {
            base.ClearText();
            _siblings.ForEach(p => p.accumulatedText = string.Empty);
        }

        protected override void SetSubtitleTextContent(Subtitle subtitle)
        {
            base.SetSubtitleTextContent(subtitle);
            _siblings.ForEach(p => p.accumulatedText = accumulatedText);
        }
    }
}
```

### Limit accumulated text length (entry 78)

When `accumulatedText` grows very large, `UnityEngine.UI.Text` fails at Unity's
65,000-vertex mesh limit per text component. Resolution options (in order of preference):

1. **Upgrade to TextMesh Pro** — supports much larger vertex buffers; no per-mesh limit.
2. **Switch to `SMSDialogueUI`** — a `StandardDialogueUI` subclass that instantiates a
   separate text object per line, optionally combined with Enhanced Scroller for scrolling.
3. **Trim in script** — implement `OnConversationLine(Subtitle subtitle)` on the Dialogue
   Manager and strip leading lines from the accumulated string whenever the line count
   exceeds a configurable `maxLines` Inspector field.

---

## Portrait and Asset Fields

### Player-selectable portraits at runtime (entry 69)

When players choose a portrait from `Resources`, update both the Lua runtime and the UI
portrait cache:

```csharp
DialogueLua.SetActorField(actorName,
    DialogueSystemFields.CurrentPortrait, sprite.name);
DialogueManager.instance.SetActorPortraitSprite(actorName, sprite);
```

For portraits stored in the database's Actor portrait list (indexed), resolve with
`playerActor.GetPortraitSprite(picNumber)` and pass `$"pic={picNumber}"` as the value to
`SetActorField`. Enable **Dialogue Manager > Persistent Data Settings > Include All Actor
Data** so `DialogueSystemSaver` persists the selected portrait into saved games.

### Assign Unity asset references to database fields (entry 89)

The Dialogue System Extras package provides a downloadable **Asset** field type that exposes
an object picker for `Sprite`, `AudioClip`, `Texture2D`, and other `UnityEngine.Object`
subtypes in the Dialogue Editor — without modifying core source code. Refer to the package
script header for full setup instructions.

### Custom Sprite field type for database entries (entry 110)

To implement a custom `Sprite` field type manually, create an Editor script inside an
`Editor/` folder:

```csharp
[CustomFieldTypeService.Name("Sprite")]
public class CustomFieldTypeSprite : CustomFieldType
{
    public override string Draw(string currentValue, DialogueDatabase database)
    {
        var existing = string.IsNullOrEmpty(currentValue)
            ? null : Resources.Load<Sprite>(currentValue);
        var sprite = EditorGUILayout.ObjectField(
            existing, typeof(Sprite), false) as Sprite;
        return sprite != null ? sprite.name : string.Empty;
    }

    public override string Draw(Rect rect, string currentValue, DialogueDatabase database)
    {
        var existing = string.IsNullOrEmpty(currentValue)
            ? null : Resources.Load<Sprite>(currentValue);
        var sprite = EditorGUI.ObjectField(
            rect, existing, typeof(Sprite), false) as Sprite;
        return sprite != null ? sprite.name : string.Empty;
    }
}
```

Store sprite assets in a `Resources/` folder; load at runtime via
`Resources.Load<Sprite>(field.value)`.

### Actor/Item/Location names in custom field types (entry 123)

Fields typed as `Actor`, `Item`, or `Location` in the Dialogue Editor store integer IDs
by default (e.g. Lua condition `Variable["CurrentLocation"] == 2`). To author and evaluate
conditions using human-readable strings (`Variable["CurrentLocation"] == "Field"`), create
and register a custom field type drawer that serializes the string `Name` property rather
than the integer ID.

---

## Typewriter and Audio Sync

### Match typewriter speed to voiceover clip length (entry 68)

In `OnConversationLine`, parse the Sequence field for `AudioWait(clipName)`, load the clip,
and assign `charactersPerSecond` proportionally:

```csharp
void OnConversationLine(Subtitle subtitle)
{
    if (string.IsNullOrEmpty(subtitle.sequence)) return;
    var match = Regex.Match(subtitle.sequence, @"AudioWait\((\w+)\)");
    if (!match.Success) return;
    var clip = DialogueManager.LoadAsset(
        match.Groups[1].Value, typeof(AudioClip)) as AudioClip;
    if (clip == null || clip.length <= 0) return;
    var panel = DialogueManager.standardDialogueUI.conversationUIElements
        .standardSubtitleControls.GetPanel(subtitle, out _);
    if (panel != null)
        panel.GetTypewriter().charactersPerSecond =
            subtitle.formattedText.text.Length / clip.length;
}
```

---

## Dialogue Editor Extensions

### Custom drag-and-drop for the Sequence field (entry 128)

Subscribe to `SequenceEditorTools.tryDragAndDrop` inside an `[InitializeOnLoad]` static
constructor to extend drag-drop support in the Dialogue Editor's Sequence field:

```csharp
using PixelCrushers.DialogueSystem.DialogueEditor;
using UnityEditor;

[InitializeOnLoad]
static class SequenceDragDropExtension
{
    static SequenceDragDropExtension()
    {
        SequenceEditorTools.tryDragAndDrop += TryDrop;
    }

    static bool TryDrop(UnityEngine.Object obj, ref string sequence)
    {
        if (obj is Sprite sprite &&
            DialogueEditorWindow.inspectorSelection is DialogueEntry entry)
        {
            string actorName = DialogueEditorWindow
                .GetCurrentlyEditedDatabase()
                .GetActor(entry.ActorID)?.Name;
            if (actorName != null)
                sequence += $"SetPortrait({actorName}, {sprite.name})";
            return true;
        }
        return false;
    }
}
```
