# Text Formatting and TextMesh Pro Techniques

Supplementary reference for the `dialogue-system-for-unity-authoring-conversations` skill.
All techniques are sourced from verified Pixel Crushers support-forum [HOWTO] posts.
All types are in namespace `PixelCrushers.DialogueSystem` unless stated otherwise.

---

## Number Formatting

### Round numbers in dialogue text (entry 109)

Use `string.format()` inside a `[lua(...)]` markup tag. Dialogue System Lua uses standard
.NET format string syntax:

```
Your GPA is [lua(string.format("{0:0.00}", Variable["x"]))]
```

`Variable["x"]` value `2.499999` renders as `2.50`. Use `{0:0}` to round to a whole
number, `{0:0.000}` for three decimal places, and so on.

---

## Text Injection and Keyword Tags

### Inject rich-text tags around keywords in subtitle text (entry 93)

Implement `OnConversationLine(Subtitle subtitle)` on a MonoBehaviour on the Dialogue
Manager GameObject and apply a `Regex.Replace` with word-boundary anchors:

```csharp
void OnConversationLine(Subtitle subtitle)
{
    subtitle.formattedText.text = Regex.Replace(
        subtitle.formattedText.text,
        @"\bPoison\b",                            // word boundaries prevent partial matches
        m => $"<color=green>{m.Value}</color>");
}
```

As an alternative to `OnConversationLine`, subscribe to
`DialogueManager.instance.conversationLinePrepared` with a delegate of the same signature.

---

## Typewriter Events and Custom Tags

### Trigger events at specific characters during typewriter playback (entry 79)

Three approaches, listed by complexity:

1. **Text Animator for Unity** — natively supported by the Dialogue System; attach a
   `TextAnimatorPlayer` component alongside the typewriter and use built-in animation tags
   directly in dialogue text (e.g. `<wiggle>word</wiggle>`).

2. **Subclass the typewriter** — override `Play()` in `TextMeshProTypewriterEffect` or
   `UnityUITypewriterEffect` to intercept character delivery.

3. **Strip-and-schedule approach** — in `OnConversationLine` on the Dialogue Manager:
   - Detect a custom tag (e.g. `[Shake]`) in `subtitle.formattedText.text`.
   - Strip it: use `Tools.StripTextMeshProTags(text)` or `Tools.StripRichTextCodes(text)`.
   - Calculate the sequencer fire time: `float t = tagCharIndex / typewriter.charactersPerSecond`.
   - Prepend a timed sequencer command to the line:

```csharp
subtitle.sequence = $"AC(Shake)@{fireTime:F2}; " + subtitle.sequence;
subtitle.formattedText.text = textWithTagStripped;
```

### TMP link tag events during typewriter playback (entry 111)

Embed event triggers in dialogue text as TMP `<link>` tags:

```
Examine <link="torch">the torch</link> carefully.
```

In C#, attach a component to the TMP text GameObject and subscribe to the typewriter's
`onCharacter` UnityEvent (and `onBegin` to pre-cache link index data):

```csharp
TMP_LinkInfo[] _links;

void OnEnable()
{
    var tw = GetComponent<TextMeshProTypewriterEffect>();
    tw.onBegin.AddListener(OnBegin);
    tw.onCharacter.AddListener(OnCharacter);
}

void OnBegin()   => _links = textMeshPro.textInfo.linkInfo;

void OnCharacter()
{
    if (_links == null) return;
    foreach (var link in _links)
    {
        if (link.linkTextFirstCharacterIndex == textMeshPro.maxVisibleCharacters)
            HandleLink(link.GetLinkID());   // e.g. "torch"
    }
}
```

When pre-processing text (e.g. stripping RPG Maker codes), preserve link tags using:

```csharp
string processed = TextMeshProTypewriterEffect.StripRPGMakerCodes(
    Tools.StripTextMeshProTags(textMeshPro.text));
```

---

## Actor Name and Indent Formatting

### Prepend actor name and indent in TextMesh Pro (entry 132)

Attach a script to the Dialogue Manager implementing `OnConversationLine`. Track the
previous speaker name so the colored header is emitted only on speaker changes:

```csharp
string _lastSpeaker;

void OnConversationStart(Transform actor) => _lastSpeaker = null;

void OnConversationLine(Subtitle subtitle)
{
    string name = subtitle.speakerInfo.Name;
    string body = $"<indent=20%>{subtitle.formattedText.text}</indent>";

    if (name != _lastSpeaker)
    {
        var da = DialogueActor.GetDialogueActorComponent(
            subtitle.speakerInfo.transform);
        string hex = (da != null)
            ? Tools.ToWebColor(da.standardDialogueUISettings.subtitleColor)
            : "ffffff";
        body = $"<color=#{hex}>{name}</color> - " + body;
        _lastSpeaker = name;
    }
    subtitle.formattedText.text = body;
}
```

Untick **Set Subtitle Color** on every `DialogueActor` component to avoid duplicate color
application from the panel.

---

## Typewriter and Subtitle Timing

### Match typewriter speed and subtitle display duration (entry 108)

`TextMeshProTypewriterEffect.charactersPerSecond` (default `50`) controls how fast
characters type. `Dialogue Manager > Display Settings > Subtitle Settings > Subtitle Chars
Per Second` (default `30`) drives the `{{end}}` duration calculation for the default
`Delay({{end}})` sequence.

**To prevent subtitles from lingering after typing completes:**
- Set **Subtitle Chars Per Second** equal to the typewriter CPS (e.g. `50`).
- Set **Min Subtitle Seconds** to `0.5` (prevents flash-dismissal on very short lines).

**To synchronize end-of-typing animations**, add to the node's Sequence field:

```
AnimatorPlay(Idle, speaker)@Message(Typed)
```

The typewriter automatically fires the `Typed` sequencer message when it finishes, which
matches how `required AnimatorPlay(Idle, speakerportrait)@Message(Typed)` is used to stop
talk animations at exactly the right moment.
