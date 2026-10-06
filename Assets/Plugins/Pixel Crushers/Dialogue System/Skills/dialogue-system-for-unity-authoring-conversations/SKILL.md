---
name: "dialogue-system-for-unity-authoring-conversations"
description: "Use this skill whenever someone wants to write, build, or edit conversation content for the Dialogue System for Unity — e.g. 'make a dialogue tree', 'add a conversation', 'create branching dialogue', 'add player response choices', 'set up a dialogue database', 'add an npc actor', 'make this line only show if a variable is set', 'add a cutscene sequence to a line'. Covers creating a DialogueDatabase, adding Actors, building conversation nodes and links in the Dialogue Editor, Menu Text vs Dialogue Text, Lua Conditions and Script for branching, and assigning Sequences. Do NOT use for starting conversations from C# (see dialogue-system-for-unity-runtime-scripting), wiring NPCs and triggers in the scene (see dialogue-system-for-unity-scene-interaction), or quest state (see dialogue-system-for-unity-quests). When in doubt whether a conversation-content request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Dialogue System for Unity — Authoring Conversations

Dialogue content lives in a **DialogueDatabase** ScriptableObject. Each database holds **Actors**, **Conversations**, **Items** (including quests), **Variables**, and **Locations**. Conversations are directed graphs of **DialogueEntry** nodes connected by **Links**. The **Dialogue Editor** (`Tools > Pixel Crushers > Dialogue System > Dialogue Editor`) is the primary authoring window. This skill covers creating a database, adding actors, building conversation trees, writing Lua conditions and scripts for branching, and assigning Sequencer commands per node.

## When to use this skill

- "how do I create a dialogue database"
- "how do I add a new conversation in the dialogue editor"
- "how do I make branching dialogue"
- "how do I give an npc different responses based on a variable"
- "how do I add conditions to a dialogue node"
- "how do I write lua in the conditions field"
- "what is the difference between menu text and dialogue text"
- "how do I add an actor to the database"
- "how do I assign a sequence to play audio when a line is spoken"
- "how do I set up a group node"

**Not for:**
- Wiring GameObjects in scene (triggers, selectors, proximity) → `dialogue-system-for-unity-scene-interaction`
- Calling `StartConversation` or other runtime C# API → `dialogue-system-for-unity-runtime-scripting`
- Quest item data, states, and QuestLog → `dialogue-system-for-unity-quests`
- Importing conversation data from external tools → `dialogue-system-for-unity-import`

## Prerequisites

- Dialogue System for Unity 2.2.73.2 installed (see `dialogue-system-for-unity-setup-and-overview`)
- A Dialogue Manager GameObject in the scene (created via **Tools > Pixel Crushers > Dialogue System > Wizards > Dialogue Manager Wizard**)
- The Dialogue Manager's **Initial Database** field must reference the DialogueDatabase asset you create

**Programmatic install check:**

```csharp
bool IsDialogueSystemInstalled()
{
    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
    {
        if (asm.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null)
            return true;
    }
    return false;
}
```

## Quick start

**Task: Create a database and add a one-line NPC conversation.**

1. In the Project window, right-click the desired folder and choose **Create > Pixel Crushers > Dialogue System > Dialogue Database**. Name it (e.g. `MyDatabase`).
2. Open the Dialogue Editor: **Tools > Pixel Crushers > Dialogue System > Dialogue Editor**.
3. In the Dialogue Editor, click the **Actors** tab. Add two actors: `Player` and `Guard` (click the **+** button).
4. Click the **Conversations** tab, then click **+** to create a new conversation. Name it `Guard Greeting`.
5. In the node canvas, a `START` node and a linked first entry appear automatically. Click the first entry node to select it.
6. In the node inspector (right panel), set **Actor** to `Guard`, **Conversant** to `Player`, and type `Hello, traveller.` into the **Dialogue Text** field.
7. Assign the database to the Dialogue Manager: select the `Dialogue Manager` GameObject, and drag `MyDatabase` into the **Initial Database** field of the `DialogueSystemController` component.
8. Press **Play** and call `DialogueManager.StartConversation("Guard Greeting")` from any script to test.

**Expected result:** The conversation panel appears with the text "Hello, traveller."

## Workflows

### Workflow: Create a DialogueDatabase and add Actors

**Goal:** Set up the data container and participants for conversations.

**Steps:**
1. Right-click in the Project panel → **Create > Pixel Crushers > Dialogue System > Dialogue Database**.
2. Open the Dialogue Editor (**Tools > Pixel Crushers > Dialogue System > Dialogue Editor**) and select the new database in the **Database** dropdown at the top.
3. Click the **Actors** tab. Click **+** to add each actor. Give each actor a unique **Name** (used in Lua as `Actor["Name"].xxx`) and optionally a **Display Name**.
4. Check **Is Player** on the player actor.

**Expected result:** The database asset contains the actors; they appear in the actor dropdown when editing dialogue entry nodes.

---

### Workflow: Build a branching conversation tree

**Goal:** Create a conversation where the player chooses from multiple responses, each leading to a different NPC reply.

**Steps:**
1. In the Dialogue Editor, open the **Conversations** tab and select or create a conversation.
2. Click the `START` node's output port and drag to create a new entry node (or double-click in empty canvas space).
3. Select the new node. Set **Actor** (speaker), **Conversant** (listener), and **Dialogue Text** (what the actor says).
4. **Menu Text** is the text shown on the player's response button. Leave it blank to use Dialogue Text as the button label.
5. To create a player response branch: select an NPC entry node, click **+** in the **Links** section, and create two or more child nodes. Set each child's Actor to `Player` and enter different response lines in **Menu Text**.
6. Link each player response to a different NPC reply node by dragging from the child node's output port.
7. Leave a node with no outgoing links — it becomes a terminal (END) node.

**Expected result:** In Play mode the conversation shows the NPC line, then a response menu with the player choices. Each choice leads to its connected NPC reply, then ends.

---

### Workflow: Add Lua Conditions and Script for branching

**Goal:** Show or hide dialogue entries based on a Lua variable; run Lua when an entry is reached.

**Steps:**
1. Select a dialogue entry node in the Dialogue Editor.
2. Expand the **Conditions** section (bottom of node inspector). Enter a Lua expression in the **Condition** field, e.g.:

   ```lua
   Variable["VisitedGuard"] == true
   ```

   The entry is only reachable if this condition is true at runtime. Sibling entries without passing conditions are skipped.

3. Expand the **Script** section. Enter Lua code in the **Script** field to run when the entry is reached, e.g.:

   ```lua
   Variable["VisitedGuard"] = true
   ```

4. Variables used in Lua must exist in the database: click the **Variables** tab and add them there with a **Name** and **Initial Value**.

**Expected result:** In Play mode, the entry only appears / is spoken when the condition is satisfied, and the Script runs immediately as the entry is displayed.

---

### Workflow: Assign a Sequence to a dialogue entry

**Goal:** Play a Sequencer command (e.g. audio clip, camera movement, animation) when a line is delivered.

**Steps:**
1. Select a dialogue entry node in the Dialogue Editor.
2. Expand the **Sequence** section and type a Sequencer command, e.g.:

   ```
   Audio(Guard_Hello)
   ```

   or combine commands with semicolons:

   ```
   Audio(Guard_Hello); Camera(Closeup, listener, 0.5)
   ```

3. Common built-in commands: `Audio(clipName)`, `Camera(angleName, subject, blendTime)`, `AnimatorBool(param, value, subject)`, `Delay(seconds)`.

**Expected result:** When the entry plays at runtime, the Sequencer command fires: the audio clip is heard, camera cuts to the specified angle, etc.

---

### Workflow: Run a conversation only once

**Goal:** Prevent a trigger from starting the same conversation more than once.

**Steps:**
1. In the Dialogue Editor's **Variables** tab, add a bool variable, e.g. `NPCName.Talked`, initial value `false`. (Dot notation groups variables by actor; it carries no special Lua meaning.)
2. On the `DialogueSystemTrigger`, expand **Conditions > Lua Conditions** and add: `Variable["NPCName.Talked"] ~= true`
3. In the conversation's opening node **Script** field, add: `Variable["NPCName.Talked"] = true`
4. For a follow-up conversation, create a second trigger with condition `Variable["NPCName.Talked"] == true`.

**Expected result:** The conversation fires exactly once; subsequent trigger activations are silently skipped.

---

### Workflow: Player monologues — suppress the response menu

**Goal:** Make solo player-actor entries speak as subtitles instead of appearing as a single-button response menu.

**Steps:**
1. Select the Dialogue Manager → **Display Settings > Input Settings** → uncheck **Always Force Response Menu**.
2. In **Display Settings > Subtitle Settings**, check **Show PC Subtitles During Line**.
3. To override per node, add `[auto]` to the entry's **Dialogue Text** (forces subtitle) or `[f]` (forces menu).
4. For per-conversation override: Dialogue Editor → **Menu > Conversation Properties** → enable **Override Display Settings**.
5. Alternative: assign the line to a **Narrator** actor and use **Override Dialogue UI** on a `DialogueActor` targeting a dedicated `StandardUISubtitlePanel`.

**Expected result:** Single-response player nodes display as subtitle text and auto-advance (or wait for Continue), not as a clickable menu button.

---

### Workflow: Continue without a UI button

**Goal:** Auto-advance lines or use custom input instead of the on-screen Continue button.

**Steps:**
1. **Timed auto-advance:** In the node's **Sequence** field, add `Continue()@2` (advances after 2 s). Use `Delay(2)` to control display duration without clicking.
2. **Tap-anywhere:** Add a full-screen transparent `Button` (alpha 0, `LayoutElement > Ignore Layout`) under the Dialogue Panel; wire its `OnClick` to `StandardUIDialogueControls.OnContinue`.
3. **Custom key:** Add `UIButtonKeyTrigger` to the continue button and bind a key or gamepad button. Do not map **Space** or **Return** — the EventSystem already handles those and a double-trigger results.

**Expected result:** The conversation advances without the visible Continue button, via timer, screen tap, or a custom input binding.

---

### Workflow: Subtitle timing and typewriter fast-forward

**Goal:** Control how long lines stay on screen and configure click-to-complete-typing behaviour.

**Steps:**
1. **Duration:** The Default Sequence (`Dialogue Manager > Display Settings > Camera & Cutscene Settings`) is initially `Delay({{end}})`. `{{end}}` is derived from text length, **Subtitle Chars Per Second**, and **Min Subtitle Seconds**. For a fixed duration write `Delay(2)` in the node's **Sequence**. Embed the global default inside a custom sequence with `{{default}}` (e.g. `{{default}}; Audio(Hello)`). Per-conversation override: Dialogue Editor → **Menu > Conversation Properties** → enable **Override Display Settings**.
2. **Typewriter speed:** On the subtitle panel's `TextMeshProTypewriterEffect` (or `UnityUITypewriterEffect`), set **Characters Per Second**. Keep **Dialogue Manager > Subtitle Settings > Subtitle Chars Per Second** ≤ typewriter CPS or the panel closes before typing finishes.
3. **Typed message:** The typewriter fires a `Typed` sequencer message on completion. Use it in sequences: `required AnimatorPlay(Idle, speakerportrait)@Message(Typed)`.
4. **Fast-forward-then-continue:** Add `StandardUIContinueButtonFastForward` to the continue button. Assign the panel's typewriter to its **Typewriter Effect** field. Route the button's `OnClick` to `StandardUIContinueButtonFastForward.OnFastForward`. First click completes typing; second click (or first if already done) advances the line. Prefabs ship pre-wired.

**Expected result:** Lines stay on screen for the correct duration; typewriter finishes before the panel closes; first continue click completes typing.

---

### Workflow: Text formatting — indented text and coloured actor names

**Goal:** Prefix actor names in their assigned colour with wrapped text indented below (Disco Elysium style).

**Steps:**
1. **Quick TMP indent:** Add a script to the Dialogue Manager implementing `OnConversationLine(Subtitle subtitle)`:
   ```csharp
   void OnConversationLine(Subtitle subtitle)
   {
       subtitle.formattedText.text = subtitle.speakerInfo.Name
           + " - <indent=15%>" + subtitle.formattedText.text + "</indent>";
   }
   ```
   Disable **Add Speaker Name** on the `StandardUISubtitlePanel` to avoid duplicating the name.
2. **Full paragraph panel:** Use the community `ContinueParagraphSubtitlePanel : StandardUISubtitlePanel` subclass. It reads each Actor's **Node Color** (set in Dialogue Editor → **Actors** tab → **Node Color**), prefixes the name in that colour via TMP rich text, adds `<indent>` automatically, concatenates consecutive same-speaker lines, and dims history text. Replace `StandardUISubtitlePanel` with this subclass. Requires TextMesh Pro. Assign `InputDeviceManager > Always Auto Focus` and add `DeselectPreviousOnPointerEnter` to continue buttons for best gamepad/mouse compatibility.

**Expected result:** Actor names appear in their assigned colour; wrapped text is indented; consecutive lines from the same speaker merge into one paragraph.

---

### Workflow: Randomize text, quizzes, and skill checks

**Goal:** Add variety through random lines, dynamic quiz values, and probability-gated skill checks.

**Steps:**
1. **Random inline text:** In **Dialogue Text**, use `[lua(RandomElement("Option A|Option B|Option C"))]`. A random pipe-delimited element is substituted at runtime.
2. **Random branching:** Call `RandomizeNextEntry()` in a group node's **Script**, or use the built-in **Group** node type with randomized link order.
3. **Quizzes:** Store score in `Variable["Score"]`; display with `[var=Score]`. Compute dynamic values inline with `[lua( Variable["sum"] + math.random(5) )]`. Accept typed answers with `TextInput()` (see Read player text input workflow). Normalise with `string.lower()`.
4. **Skill checks:** Call a registered C# function from a **Condition** or use inline `math.random(1,20) >= threshold`. Style locked responses with `[em1]` tags (define emphasis in Dialogue Editor > Database). To display failed checks as disabled (non-interactable) buttons: **Dialogue Manager > Display Settings > Input Settings** → tick **Include Invalid Entries**.
5. **Shuffle responses:** Implement `OnConversationResponseMenu(Response[] responses)` on a Dialogue Manager script and shuffle the array in place before display.

**Expected result:** Lines vary each play; quiz values compute at runtime; failed skill-check responses appear greyed out instead of hidden.

---

### Workflow: Read player text input during a conversation

**Goal:** Let the player type a value stored as a Lua variable and used in later nodes.

**Steps:**
1. In the entry's **Sequence** field: `SetContinueMode(false); TextInput(None, "What is your name?", playerName, 20, true)`. `SetContinueMode(false)` prevents the continue button from firing while the field is open.
2. In the **next** node's **Sequence**: `SetContinueMode(true); {{default}}` to restore normal continue behaviour.
3. In the next node's **Script** field:
   ```lua
   ChangeActorName("Player", Variable["playerName"])
   Variable["Actor"] = Variable["playerName"]
   ```
   `Variable["Actor"]` must be set explicitly — `ChangeActorName` only updates the display name, not the Lua variable that `[var=Actor]` reads.
4. Reference the stored value in later **Dialogue Text** with `[var=playerName]`.

**Expected result:** A text field appears during the conversation; the player's input is stored; subsequent lines display or branch on the entered value.

---

### Workflow: Audio effects, fades, and per-actor typewriter sound

**Goal:** Apply DSP to dialogue audio, fade ambient sources on dialogue start, and vary typewriter sounds per speaker.

**Steps:**
1. **Mixer routing:** Create an Audio Mixer Group with the desired effects (reverb, radio static). On the actor's `AudioSource`, set **Output** to that group. Add `DialogueActor` to map the actor to that AudioSource. Or target it in a Sequence: `AudioWait(clip, NPCGameObject)`.
2. **Gradual fade:** Create a custom `SequencerCommandAudioFade : SequencerCommand`. In the `Start()` coroutine, call `GetSubject(0, speaker)` for the AudioSource's Transform, `GetParameterAsFloat(1)` for duration, interpolate `AudioSource.volume` to 0, then call `Stop()`. Syntax: `AudioFade(speaker, 1.5)` in the Sequence field.
3. **Per-actor typewriter sound:** Hook the typewriter's `OnBegin()` UnityEvent in a script, or subclass `TextMeshProTypewriterEffect` and override `PlayCharacterAudio(char c)`. Retrieve the clip with `DialogueManager.masterDatabase.GetActor(name).LookupValue("AudioClip")` and assign to `AbstractTypewriterEffect.audioClip`.
4. **Sync talk animation:** Use `required AnimatorPlay(Talk, speakerportrait); required AnimatorPlay(Idle, speakerportrait)@Message(Typed)` so the talk animation stops exactly when typing finishes.

**Expected result:** Audio plays through the correct DSP chain; ambient sources fade smoothly; typewriter sounds vary per actor; talk animations stop on typing completion.

---

### Workflow: Copy conversations and inspect the runtime database

**Goal:** Duplicate or transfer conversations between databases; inspect the live merged database in Play Mode.

**Steps:**
1. **Duplicate within a database:** Right-click the conversation canvas → **Duplicate Conversation**.
2. **Copy across databases:** Lasso-select all nodes except `<START>` in the source canvas, right-click → **Copy**. In the destination, create a new conversation, right-click canvas → **Paste**, then wire `<START>` to the first pasted node.
3. **Template JSON:** Source conversation → **Menu > Templates > Save Template JSON**. Destination → **Menu > Templates > New From Template > From Template JSON**.
4. **Inspect the runtime DB in Play Mode:** Add an Editor-only script with a `[MenuItem]`:
   ```csharp
   [MenuItem("Tools/Pixel Crushers/Inspect Runtime DB")]
   static void InspectRuntimeDB()
   {
       DialogueEditorWindow.OpenDialogueEditorWindow();
       DialogueEditorWindow.instance.SelectObject(DialogueManager.masterDatabase);
   }
   ```
   Call while the game is running to see the merged database including all `AddDatabase` calls.

**Expected result:** Conversations transfer intact; the Dialogue Editor shows the live runtime-merged database during Play Mode.

---

### Workflow: Different default sequences for the first and last lines

**Goal:** Play a distinct opening or closing sequence on only the first or last entry of each conversation, without overriding per-node sequences.

**Steps:**
1. Add a script to the Dialogue Manager with public string fields `DefaultFirstSequence` and `DefaultLastSequence`, and a private `bool isFirstEntry`.
2. Implement the callbacks:
   ```csharp
   void OnConversationStart(Transform actor) { isFirstEntry = true; }
   void OnConversationEnd(Transform actor)   { isFirstEntry = true; }
   void OnConversationLine(Subtitle subtitle)
   {
       if (subtitle.dialogueEntry.id == 0) return; // skip START node
       if (string.IsNullOrEmpty(subtitle.sequence))
       {
           if (isFirstEntry)
               subtitle.sequence = DefaultFirstSequence;
           else if (!DialogueManager.currentConversationState.hasAnyResponses)
               subtitle.sequence = DefaultLastSequence;
       }
       isFirstEntry = false;
   }
   ```
3. Set `DefaultFirstSequence` and `DefaultLastSequence` in the Inspector (e.g. `Camera(Closeup, listener, 0.5); {{default}}`). Nodes with a non-blank **Sequence** field are unaffected.

**Expected result:** Every conversation's first line plays the opening sequence and last line plays the closing sequence; intermediate and explicitly sequenced nodes are unchanged.

---

### Workflow: Fallback hub navigation

**Goal:** Let the player return to a menu hub from deep inside a branch without wiring back-links on every path.

**Steps:**
1. Create a `FallbackButton` MonoBehaviour. In `OnEnable` register Lua functions; unregister in `OnDisable`:
   ```csharp
   Lua.RegisterFunction("RecordFallback", this,
       SymbolExtensions.GetMethodInfo(() => RecordFallback()));
   Lua.RegisterFunction("GotoFallback", this,
       SymbolExtensions.GetMethodInfo(() => GotoFallback()));
   ```
2. `RecordFallback()` pushes the current entry: `_stack.Push(DialogueManager.currentConversationState.subtitle.dialogueEntry);`
3. `GotoFallback()` pops and jumps: `var e = _stack.Pop(); DialogueManager.conversationController.GotoState(DialogueManager.conversationModel.GetState(e));`
4. In each hub node's **Script** field, call `RecordFallback()`.
5. At the end of branches, add a "Back" player response node with `GotoFallback()` in its **Script**.

**Expected result:** Selecting "Back" returns the player to the last hub node from any depth without explicit back-links in the conversation graph.

For additional authoring techniques sourced from the Pixel Crushers support forum — including player-selectable portraits, Accumulate Text across panels, auto-play / skip-all controls, mobile TextInput, gender-aware text, platform-specific audio tokens, and continue-button label updates — see [`references/forum-techniques.md`](references/forum-techniques.md).

For TextMesh Pro link tag events, number rounding, keyword injection via regex, and typewriter-to-audio-length matching, see [`references/text-formatting-and-tmp.md`](references/text-formatting-and-tmp.md).

## Verification

- The DialogueDatabase asset appears in the Project panel with actor and conversation data visible in the Inspector
- In the Dialogue Editor, conversations display correct node layouts with linked entries
- Lua variables referenced in Conditions/Script fields are listed in the **Variables** tab
- `DialogueManager.masterDatabase` is non-null at runtime and `masterDatabase.conversations.Count > 0`
- In Play mode, the correct entries appear or are hidden based on Lua conditions

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `DialogueDatabase` | `ScriptableObject` | Container for all actors, conversations, items, variables |
| `Actor` | `class` | A participant (NPC or player); accessed via `db.actors` |
| `Conversation` | `class` | Named graph of `DialogueEntry` nodes; accessed via `db.conversations` |
| `DialogueEntry` | `class` | A single node; key fields: `DialogueText`, `MenuText`, `Sequence`, `conditionsString` (Lua), `userScript` (Lua) |
| `Link` | `class` | Directed edge from one `DialogueEntry` to another; stored in `DialogueEntry.outgoingLinks` |
| `DialogueManager.masterDatabase` | `static DialogueDatabase` | The runtime-merged database |
| `TextInput(panel, prompt, varName, maxLen, clear)` | Sequencer command | Prompts for typed player input and stores the result in a Lua variable |
| `SetContinueMode(bool)` | Sequencer command | Enables or disables the continue button dynamically mid-conversation |
| `RandomElement("a\|b\|c")` | Lua function | Returns a random pipe-delimited element; embed in `[lua(...)]` text markup |
| `StandardUIContinueButtonFastForward.OnFastForward()` | Method (Button OnClick target) | First click completes the typewriter; second click advances the line |
| `ContinueParagraphSubtitlePanel` | `StandardUISubtitlePanel` subclass | Coloured actor name, `<indent>` wrapping, same-speaker paragraph merging |
| `SequencerCommand` | Base class | Subclass to create custom sequencer commands using the Awake/Start/Stop pattern |
| `DialogueLua.SetActorField(actorName, fieldName, value)` | Static method | Sets a runtime actor field in Lua; use `DialogueSystemFields.CurrentPortrait` for portrait changes |
| `DialogueManager.instance.SetActorPortraitSprite(actorName, sprite)` | Method | Refreshes the UI portrait cache after a `SetActorField` portrait change |
| `ConversationControl` | Component | Auto-play and skip-all buttons; `ToggleAutoPlay()` and `SkipAll()` methods; requires `AbstractDialogueUI` |

All types are in namespace `PixelCrushers.DialogueSystem`.

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Conversation does not start | Database not assigned to Dialogue Manager | Drag the database into **Dialogue Manager > Dialogue System Controller > Initial Database** |
| Entry never shows despite correct Lua | Variable not in database | Add the variable in the Dialogue Editor **Variables** tab with the correct initial value |
| All player responses are hidden | Conditions on every response node are false | Check Lua variable values at runtime with `Lua.Run("return Variable['X']")` in Console |
| Dialogue Text field is blank at runtime | **Dialogue Text** left empty on node; **Menu Text** only affects response buttons | Fill in **Dialogue Text** on the NPC node |
| Sequence command not playing | Command syntax error or missing audio asset | Check Console for Sequencer warnings; verify the clip name matches exactly |
| Speaker name appears twice | **Add Speaker Name** on the subtitle panel is on while `OnConversationLine` also prepends the name | Disable **Add Speaker Name** on the `StandardUISubtitlePanel` |
| Single player line shows as a response menu button | **Always Force Response Menu** is enabled | Uncheck it under **Dialogue Manager > Display Settings > Input Settings**; also enable **Show PC Subtitles During Line**, or add `[auto]` to the node's Dialogue Text |
| Talk animation continues after typing ends | Animation not synced to typewriter completion | Add `required AnimatorPlay(Idle, speakerportrait)@Message(Typed)` to the Sequence |
| Continue button triggers twice | `UIButtonKeyTrigger` maps Space or Return, which the EventSystem already handles | Use a different key on `UIButtonKeyTrigger`; leave Space/Return to the EventSystem |
| Subtitle panel closes before typewriter finishes | **Subtitle Chars Per Second** is higher than the typewriter's **Characters Per Second** | Lower **Subtitle Chars Per Second** on the Dialogue Manager, or raise the typewriter CPS to match |
| Lines disappear too fast or linger too long | `{{end}}` timing miscalculated for the text length | Tune **Min Subtitle Seconds** and **Subtitle Chars Per Second**, or set an explicit `Delay(seconds)` in the Sequence field |
| Continue button skips the whole line without completing typing | Button is wired directly to continue, not to `OnFastForward` | Add `StandardUIContinueButtonFastForward` to the button, assign the typewriter, route `OnClick` to `OnFastForward()` |
| Failed skill-check response options vanish | **Include Invalid Entries** is unchecked | Enable it under **Dialogue Manager > Display Settings > Input Settings** to render locked options as non-interactable buttons |
| `[var=Actor]` shows the old name after `ChangeActorName` | `ChangeActorName` updates the display name only, not `Variable["Actor"]` | Also execute `Variable["Actor"] = Variable["playerName"]` in the node's Script field |
| Voice plays without reverb or filter effects | `AudioWait()` target AudioSource has no mixer output assigned | Set `AudioSource.outputAudioMixerGroup` to the effect bus, or use `AudioWait(clip, subjectGO)` targeting an already-routed source |
| Ambient audio cuts abruptly when dialogue starts | No fade — `Audio()` switches sources immediately | Write a custom `SequencerCommandAudioFade` and call `AudioFade(speaker, duration)` in the Sequence |
| Single-use conversation fires again after the first time | Trigger has no Lua condition guarding the flag variable | Add `Variable["NPCName.Talked"] ~= true` to the trigger's Lua Conditions; set `Variable["NPCName.Talked"] = true` in the opening node's Script |
| Dynamically added database not visible in Dialogue Editor during Play Mode | Editor opens the static asset, not the runtime-merged database | Use the Inspect Runtime DB menu command (see **Copy conversations** workflow) to call `SelectObject(DialogueManager.masterDatabase)` |
| Returning to a hub node requires many manual back-links | Each branch needs explicit return edges wired in the graph | Use `FallbackButton` with `RecordFallback()` in hub Scripts and `GotoFallback()` on "Back" response nodes (see **Fallback hub navigation** workflow) |
| `CS0115`: no suitable method to override on `ContinueParagraphSubtitlePanel` | Dialogue System version predates the virtual `SetSubtitleTextContent` method | Update the Dialogue System package to the latest version |
| **Accumulate Text** panel stops rendering or crashes after many lines | Single `UnityEngine.UI.Text` component exceeds Unity's 65,000-vertex mesh limit | Upgrade to TextMesh Pro, switch to `SMSDialogueUI`, or hook `OnConversationLine` to trim older lines when a `maxLines` threshold is exceeded |

## Boundaries

- The Dialogue Editor is an in-Editor tool; conversations cannot be created at runtime through this UI.
- Lua scripting uses the built-in Lua interpreter (MoonSharp-compatible subset); Unity C# assemblies are not directly callable from Lua without custom registration.
- For importing conversation content from external spreadsheets or tools, see `dialogue-system-for-unity-import`.
- For quest-specific Item fields (State, Entry states), see `dialogue-system-for-unity-quests`.
