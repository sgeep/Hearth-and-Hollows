---
name: "dialogue-system-for-unity-scene-interaction"
description: "Use this skill whenever someone wants an NPC or object in the scene to talk, bark, or respond to the player with the Dialogue System for Unity — e.g. 'add a talking npc', 'start a conversation when I press E near this guy', 'make this trigger start dialogue on collision', 'set up player interaction', 'show a bark above the enemy', 'why wont my npc talk'. Covers the NPC and Player Setup Wizards, DialogueSystemTrigger (OnUse, OnStart, OnTriggerEnter), Usable with Selector/ProximitySelector, DialogueActor mapping, and barks via BarkOnIdle. Do NOT use for authoring the conversation content itself (see dialogue-system-for-unity-authoring-conversations) or driving dialogue purely from C# (see dialogue-system-for-unity-runtime-scripting). When in doubt whether an NPC-interaction request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Dialogue System for Unity — Scene Interaction

This skill covers connecting scene GameObjects to the Dialogue System: making NPCs interactable, starting conversations and barks via triggers, and setting up player-side interaction components. The primary components are `DialogueSystemTrigger` (fires actions on Unity events), `Usable` + `Selector`/`ProximitySelector` (player-initiated use), `DialogueActor` (maps a GameObject to a database actor), and `BarkOnIdle` (timed ambient barks). All components are added via the **Add Component** menu using the wrapper types in `PixelCrushers.DialogueSystem.Wrappers` — these keep scene references stable.

## When to use this skill

- "how do I make my npc start a conversation when I walk up to them"
- "how do I wire up a talking npc"
- "I want my npc to start talking when the player enters a trigger zone"
- "how do I make an npc bark a random line"
- "why won't my conversation start when I press F"
- "how do I add a usable component to an npc"
- "what is the selector / proximity selector"
- "how do I link a game object to its dialogue actor"
- "how do I make an npc idle bark on a timer"
- "my 2D trigger isn't firing — I have USE_PHYSICS2D?"

**Not for:**
- Building conversation content (actors, nodes, Lua) → `dialogue-system-for-unity-authoring-conversations`
- C# runtime API (`StartConversation` in code) → `dialogue-system-for-unity-runtime-scripting`
- First-time Dialogue Manager setup → `dialogue-system-for-unity-setup-and-overview`
- Quest UI and state → `dialogue-system-for-unity-quests`

## Prerequisites

- Unity 2022.3 LTS or newer
- Dialogue System for Unity 2.2.73.2 installed
- A `Dialogue Manager` GameObject in the scene (created via **Tools > Pixel Crushers > Dialogue System > Wizards > Dialogue Manager Wizard**)
- A `DialogueDatabase` asset assigned to the Dialogue Manager's **Initial Database** field, containing the conversations you want to start
- **2D projects:** The scripting define `USE_PHYSICS2D` must be enabled (Welcome Window → tick **USE_PHYSICS2D** → Apply). Without it, 2D collider events (`OnTriggerEnter2D`, `OnCollisionEnter2D`) are not compiled into the trigger components.

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

**Task: Make an NPC start a conversation when the player presses Use (F key).**

1. Select the NPC GameObject.
2. **Add Component > Pixel Crushers > Dialogue System > Actor > Usable**. (This marks the NPC as interactable and shows a "Press F to talk" prompt.)
3. **Add Component > Pixel Crushers > Dialogue System > Trigger > Dialogue System Trigger**.
   - Set **Trigger** to `OnUse`.
   - Under **Conversation**, type (or select via popup) the conversation title from your database, e.g. `Guard Greeting`.
   - Set **Actor** to the Player transform; set **Conversant** to the NPC transform (or leave blank to auto-assign).
4. Select the Player GameObject.
5. **Add Component > Pixel Crushers > Dialogue System > Actor > Player > Selector**.
   - The Selector listens for the use key input and sends `OnUse` to the targeted `Usable` object.
6. Press **Play**, walk near the NPC, and press **F**.

**Expected result:** The conversation panel opens and plays `Guard Greeting`.

## Workflows

### Workflow: Use the NPC Setup Wizard

**Goal:** Quickly configure an NPC with dialogue and bark components.

**Steps:**
1. Select the NPC GameObject in the scene.
2. Choose **Tools > Pixel Crushers > Dialogue System > Wizards > NPC Setup Wizard**.
3. In the wizard:
   - Assign the NPC's **Actor** (select from the database dropdown).
   - Choose whether to add a **Dialogue System Trigger** (conversation on use) and/or **Bark On Idle** (timed ambient barks).
   - Select the conversation and bark conversation titles.
4. Click **Setup NPC**. The wizard adds and configures the appropriate components.

**Expected result:** The NPC GameObject has `Usable`, `DialogueActor`, and `DialogueSystemTrigger` (and optionally `BarkOnIdle`) components with correct field values.

---

### Workflow: Use the Player Setup Wizard

**Goal:** Configure the player to initiate conversations and display the use-prompt UI.

**Steps:**
1. Select the Player GameObject in the scene.
2. Choose **Tools > Pixel Crushers > Dialogue System > Wizards > Player Setup Wizard**.
3. In the wizard:
   - Confirm the **Actor** entry for the player.
   - Choose **Selector** (raycast-based, 3D) or **Proximity Selector** (trigger-based, no raycast).
   - Configure the use-key binding and UI display options.
4. Click **Setup Player**.

**Expected result:** The Player GameObject has a `Selector` or `ProximitySelector` component correctly configured.

---

### Workflow: Start a conversation via OnTriggerEnter

**Goal:** Automatically start a conversation when the player enters a trigger zone (e.g. a doorway cutscene).

**Steps:**
1. Create a trigger volume: add an empty GameObject with a `BoxCollider` (3D) and tick **Is Trigger**, or use a `BoxCollider2D` with **Is Trigger** for 2D. Ensure its layer mask is visible to physics.
2. Add component **Pixel Crushers > Dialogue System > Trigger > Dialogue System Trigger** to the trigger volume.
3. Set **Trigger** to `OnTriggerEnter`.
4. Under **Conversation**, enter the conversation title.
5. Set **Actor** and **Conversant** as needed.
6. Optionally set **Condition** (Lua) so the conversation only starts once or under certain conditions.
7. For 2D: ensure `USE_PHYSICS2D` is defined (see Prerequisites).

**Expected result:** When the player's collider enters the trigger zone, the conversation starts automatically.

---

### Workflow: Add a DialogueActor component

**Goal:** Map a scene GameObject to a named Actor in the database, so portraits, audio, and camera angles resolve correctly.

**Steps:**
1. Select the NPC (or Player) GameObject.
2. **Add Component > Pixel Crushers > Dialogue System > Actor > Dialogue Actor**.
3. Set **Actor** to the actor's name as it appears in the database (type or use the popup picker).
4. Optionally assign a **Dialogue UI Settings** override or a bark UI prefab in the same component.

**Expected result:** When a conversation involves this actor, the system uses the matching database actor's portrait, name, and any audio settings.

---

### Workflow: Add timed ambient barks with BarkOnIdle

**Goal:** Make an NPC bark a random line at regular intervals (ambient flavor text).

**Steps:**
1. In the Dialogue Editor, create a conversation (e.g. `Guard Barks`) where each entry linked from START is a bark line. Set the Actor on each entry to the NPC.
2. Select the NPC GameObject.
3. **Add Component > Pixel Crushers > Dialogue System > Trigger > Bark On Idle**.
4. Set **Conversation** to `Guard Barks`.
5. Set **Min Seconds** and **Max Seconds** for the interval range (e.g. 5 and 10).
6. Optionally set **Target** (the transform barked at) or leave blank to bark to the air.
7. The NPC must have (or the component will create) a bark UI. Either:
   - Add **Add Component > Pixel Crushers > Dialogue System > UI > Standard UI > Bark > Standard Bark UI** directly, or
   - Assign a bark UI prefab in the **Dialogue Actor** component's **Bark UI** field.

**Expected result:** In Play mode, the NPC says a random line from `Guard Barks` every 5–10 seconds via a floating bark bubble.

---

### Workflow: Use ProximitySelector for trigger-based interaction (no raycast)

**Goal:** Allow the player to use NPCs or objects by entering their trigger collider, without needing line-of-sight raycast.

**Steps:**
1. On the Player, add **Add Component > Pixel Crushers > Dialogue System > Actor > Player > Proximity Selector** (instead of `Selector`).
2. On the NPC, ensure it has a trigger collider (`BoxCollider`/`CircleCollider2D` with **Is Trigger** = true) and the **Usable** component.
3. Set the NPC's `Usable.maxUseDistance` to a suitable value.
4. Press **Play**: when the player enters the NPC's trigger, a prompt appears; pressing the use key starts the conversation.

**Expected result:** Conversation starts when the player is inside the NPC's trigger area and presses the use key, without requiring line-of-sight.

---

### Workflow: Integrate Cinemachine cameras

**Goal:** Control virtual camera priority during conversations and eliminate Selector raycast jitter when Cinemachine drives the camera.

**Steps:**
1. Enable Cinemachine support: **Tools > Pixel Crushers > Dialogue System > Welcome Window**, tick **Cinemachine**, click **Apply**.
2. Add a **Cinemachine Priority On Dialogue Event** component to each vcam. Set **On Conversation** priority (e.g. `20`) and **On Conversation End** priority (e.g. `0`). Optionally filter by **Actor**/**Conversant** to scope which conversation raises the camera.
3. Alternatively, control priority from a node Sequence field: `CinemachinePriority(vcamName, 20)`.
4. If the conversation pauses `Time.timeScale` via **Pause Game During Conversations**, enable **CinemachineBrain > Ignore Time Scale** on the scene's `CinemachineBrain` component so camera blends continue.
5. If the player's Selector highlights the wrong target (jitter): subclass `Selector` as `CinemachineSelector` — leave `Update()` empty and subscribe/unsubscribe `base.Update()` to `CinemachineCore.CameraUpdatedEvent` in `OnEnable`/`OnDisable` so raycasts only fire after the camera stabilises.
6. When using multi-NPC conversations, assign the specific NPC GameObject to the Dialogue System Trigger's **Conversation Conversant** field so the correct vcam activates.

**Expected result:** The correct vcam activates at conversation start, camera blends continue through timeScale pauses, and the Selector highlights the correct Usable without jitter.

---

### Workflow: Bark in range with proximity zones

**Goal:** Make an NPC bark once when the player enters an outer range, and start a conversation once in an inner range.

**Steps:**
1. Add two child empty GameObjects to the NPC (e.g. `BarkZone`, `TalkZone`). On each, add a kinematic `Rigidbody` (3D) or `Rigidbody2D` (2D) and a `SphereCollider`/`CircleCollider2D` with **Is Trigger** ticked, sized for the respective range.
2. On `BarkZone`: add **Dialogue System Trigger**, set **Trigger** = `OnTriggerEnter`, **Use Tag** filter = `Player`, action = **Bark** with the bark conversation.
3. On `TalkZone`: add **Dialogue System Trigger**, set **Trigger** = `OnTriggerEnter`, **Use Tag** filter = `Player`, action = **Start Conversation**.
4. Fire each trigger only once: create a Lua bool variable (e.g. `Guard.Barked`). Add a **Condition** `Variable["Guard.Barked"] == false` and in **Run Lua Code** on fire: `Variable["Guard.Barked"] = true`.

**Expected result:** The NPC barks once on entering the outer zone and starts the conversation once in the inner zone, ignoring subsequent re-entries.

---

### Workflow: Pause dialogue during a pause menu

**Goal:** Open a non-Dialogue System pause menu without dialogue hotkeys or timers interfering.

**Steps:**
1. In **Dialogue Manager > Other Settings**, set **Dialogue Time Mode** to **Realtime** so subtitle timers run on unscaled time (or manage `Time.timeScale` manually if needed).
2. On pause menu open:
   ```csharp
   UIPanel.monitorSelection = false;        // stops UIPanel from stealing EventSystem focus
   UIButtonKeyTrigger.monitorInput = false;  // stops hotkeys (continue, use key)
   DialogueManager.Pause();                 // pauses sequencer and subtitle timers
   playerSelector.enabled = false;          // prevents use-key NPC interaction
   ```
3. On pause menu close:
   ```csharp
   UIPanel.monitorSelection = true;
   UIButtonKeyTrigger.monitorInput = true;
   DialogueManager.Unpause();
   playerSelector.enabled = true;
   ```

**Expected result:** The pause menu opens and closes cleanly with no dialogue hotkey conflicts or timer drift; dialogue resumes at exactly the right point.

---

### Workflow: Show an overhead conversation bubble

**Goal:** Display subtitle text in a world-space bubble anchored above the NPC instead of (or alongside) the fullscreen dialogue panel.

**Steps:**
1. Locate the prefab at `Plugins/Pixel Crushers/Dialogue System/Prefabs/Standard UI Prefabs/Template/Bubble/Bubble Template Standard UI Subtitle Panel` and add it as a **child of the NPC** in the scene.
2. On the NPC, add (or select) the **Dialogue Actor** component. In **Dialogue UI Settings**, set **Subtitle Panel Number** = `Custom` and assign the bubble panel to **Custom Subtitle Panel**.
3. If the main dialogue panel shows a blank frame behind the bubble, add a **Basic Standard Dialogue UI** prefab as an **Override Dialogue UI** on the same `Dialogue Actor`.
4. Add `UnityUITypewriterEffect` or `TextMeshProTypewriterEffect` to the bubble's Text/TMP component for a typing effect.
5. Constrain bubble width: add a **Content Size Fitter** (Horizontal = Unconstrained, Vertical = Preferred) and a **Vertical Layout Group** with **Control Child Size** enabled to the bubble panel.
6. Verify the NPC Animator has **Apply Root Motion** unchecked — root motion offsets the NPC Transform, dragging the anchored bubble off-screen.

**Expected result:** Subtitle text types into a bubble above the NPC and follows character movement; the main fullscreen panel is unaffected.

---

### Workflow: Make an NPC face the player in 2D

**Goal:** Flip an NPC sprite to face the player at conversation start using a custom sequencer command.

**Steps:**
1. Create `Assets/Scripts/SequencerCommandLookAt2D.cs`:
   ```csharp
   using UnityEngine;
   using PixelCrushers.DialogueSystem.SequencerCommands;

   public class SequencerCommandLookAt2D : SequencerCommand
   {
       void Awake()
       {
           Transform subject   = GetSubject(0); // param 0: e.g. "listener"
           Transform character = GetSubject(1); // param 1: e.g. "speaker"
           if (subject != null && character != null)
           {
               var sr = subject.GetComponentInChildren<SpriteRenderer>();
               if (sr != null)
                   sr.flipX = subject.position.x < character.position.x;
           }
           Stop();
       }
   }
   ```
2. In the NPC conversation node's **Sequence** field: `LookAt2D(listener, speaker)`.
3. Alternative without a sequencer command: implement `void OnConversationStart(Transform actor)` on the NPC and set `spriteRenderer.flipX = transform.position.x < actor.position.x`.

**Expected result:** The NPC sprite flips to face the player each time a conversation starts; no further code is required mid-conversation.

---

### Workflow: Branch conversation based on 3D object clicks

**Goal:** Pause a conversation until the player clicks a 3D object, then continue on the branch matching the clicked object.

**Steps:**
1. In the conversation node that should wait, set its **Sequence** field to `WaitForMessage(ClickedObject)`.
2. On each clickable 3D object, add a `DialogueSystemTrigger` set to `OnMouseDown`. In its **Run Lua Code** action, record which object was clicked: `Variable["Object"] = "Sphere"`. In its **Sequence** field, add `SendMessage(ClickedObject)` to resume the conversation.
3. Add child nodes to the waiting entry with **Conditions** matching `Variable["Object"]` values. To re-evaluate conditions on the same node without an intermediate entry, enable **Dialogue Manager > Other Settings > Reevaluate Links After Subtitle**.

**Expected result:** The conversation pauses, resumes when the player clicks an object, and follows the matching conditional branch.

---

### Workflow: Restrict conversation start to when the player faces the NPC

**Goal:** Only fire a `DialogueSystemTrigger` when the player's forward vector is within a set angle of the NPC.

**Steps:**
1. Create `Assets/Scripts/CustomDialogueSystemTrigger.cs`:
   ```csharp
   using UnityEngine;
   using PixelCrushers.DialogueSystem;

   public class CustomDialogueSystemTrigger : DialogueSystemTrigger
   {
       [SerializeField] private float _fovAngle = 30f;

       public override void TryStart(Transform actor, Transform interactor)
       {
           if (interactor == null) { base.TryStart(actor, interactor); return; }
           Vector3 toNPC = transform.position - interactor.position;
           if (Vector3.Angle(interactor.forward, toNPC) < _fovAngle)
               base.TryStart(actor, interactor);
       }
   }
   ```
2. Replace the NPC's `DialogueSystemTrigger` with `CustomDialogueSystemTrigger`; reconfigure the trigger event, conversation, and actor fields identically.
3. Adjust **Fov Angle** in the Inspector (default: 30°). Use in conjunction with a player `ProximitySelector`.

**Expected result:** Approaching from behind or the side does not start the conversation; only the correct facing arc triggers it.

---

### Workflow: Pause game and show cursor for C#-initiated conversations

**Goal:** Apply time-scale pause and cursor changes when starting conversations from code rather than from a `DialogueSystemTrigger`.

**Steps:**
1. Attach a `Dialogue System Events` component (or a custom MonoBehaviour) to the player or actor GameObject.
2. Implement the lifecycle callbacks:
   ```csharp
   float _prevTimeScale;
   void OnConversationStart(Transform actor)
   {
       _prevTimeScale = Time.timeScale;
       Time.timeScale = 0f;
       Cursor.visible = true;
       Cursor.lockState = CursorLockMode.None;
   }
   void OnConversationEnd(Transform actor)
   {
       Time.timeScale = _prevTimeScale;
       Cursor.visible = false;
       Cursor.lockState = CursorLockMode.Locked;
   }
   ```
3. Start conversations via `DialogueManager.StartConversation("MyConversation", playerTransform, npcTransform)` as normal.

**Expected result:** The game pauses and the cursor appears when the conversation opens; both restore on close.

---

### Workflow: Position GameObjects by quest state on scene load

**Goal:** Automatically place an NPC or object at the correct location based on whether its quest is unassigned, active, or complete.

**Steps:**
1. Attach a MonoBehaviour with serialized fields:
   ```csharp
   [QuestPopup] public string quest;
   public Transform inactiveLocation;
   public Transform activeLocation;
   public Transform completedLocation;
   ```
2. In `Start()`, read the quest state and reposition:
   ```csharp
   void Start()
   {
       var state = QuestLog.GetQuestState(quest);
       Transform target = state == QuestState.Unassigned ? inactiveLocation
                        : state == QuestState.Active     ? activeLocation
                        : completedLocation;
       if (target != null)
           transform.SetPositionAndRotation(target.position, target.rotation);
       else
           gameObject.SetActive(false);
   }
   ```
3. In the Inspector, assign the quest via the `[QuestPopup]` dropdown and drag each target Transform into its field.

**Expected result:** On scene load, the GameObject appears at the location matching the current quest state.

## Verification

- NPC GameObject has `PixelCrushers.DialogueSystem.Wrappers.DialogueSystemTrigger` component with **Trigger** = `OnUse` and a non-empty **Conversation** field
- Player GameObject has a `Selector` or `ProximitySelector` wrapper component
- At runtime, pressing the use key near a `Usable` NPC opens the conversation panel
- `DialogueManager.isConversationActive` returns `true` during the conversation
- BarkOnIdle fires bark lines in Play mode within the configured interval

Machine-checkable (C# in Editor):

```csharp
// Confirm NPC has the required components
var npc = GameObject.Find("Guard");
bool hasUsable   = npc.GetComponent<PixelCrushers.DialogueSystem.Usable>() != null;
bool hasTrigger  = npc.GetComponent<PixelCrushers.DialogueSystem.DialogueSystemTrigger>() != null;
UnityEngine.Debug.Log($"Usable={hasUsable}, Trigger={hasTrigger}");
```

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `DialogueSystemTrigger` | `MonoBehaviour` (wrapper) | General trigger: fires conversations, barks, sequences, Lua, alerts on Unity events |
| `DialogueSystemTriggerEvent` | `[Flags] enum` | Events: `OnUse`, `OnStart`, `OnEnable`, `OnTriggerEnter`, `OnTriggerExit`, `OnCollisionEnter`, `None`, and more |
| `Usable` | `MonoBehaviour` (wrapper) | Marks a GameObject as interactable; broadcasts `OnUse` when the player activates it |
| `Selector` | `MonoBehaviour` (wrapper) | Raycast-based player interaction; sends `OnUse` to the targeted `Usable` |
| `ProximitySelector` | `MonoBehaviour` (wrapper) | Trigger-based player interaction; tracks the most recent `Usable` in range |
| `DialogueActor` | `MonoBehaviour` (wrapper) | Maps a GameObject to a database Actor by name |
| `BarkOnIdle` | `MonoBehaviour` (wrapper) | Fires a bark from a conversation at randomised timed intervals |
| `Bark Group Member` | `MonoBehaviour` (wrapper) | Attach to a barker; **Hide Bark On Conversation Start** suppresses the bark UI when any conversation begins (v2.2.26+) |
| `Cinemachine Priority On Dialogue Event` | `MonoBehaviour` | Raises/lowers a vcam's priority when a matching conversation starts or ends |
| `CinemachineSelector` | `class : Selector` | Selector subclass that defers raycasts to `CinemachineCore.CameraUpdatedEvent`, eliminating highlight jitter with Cinemachine |
| `SequencerCommandLookAt2D` | `class : SequencerCommand` | Custom sequencer command pattern; flips a `SpriteRenderer` on the subject to face another transform in 2D |
| `CustomDialogueSystemTrigger` | `class : DialogueSystemTrigger` | Subclass pattern; override `TryStart(Transform actor, Transform interactor)` to add pre-conditions (e.g. facing-angle check) before calling `base.TryStart` |
| `Reevaluate Links After Subtitle` | Dialogue Manager > Other Settings | Re-evaluates outgoing link conditions immediately after the subtitle plays, enabling same-node branching on variables set mid-subtitle |

Add Component menu paths (all under **Pixel Crushers/Dialogue System/**):

| Component | Add Component path |
|---|---|
| Dialogue System Trigger | `Trigger/Dialogue System Trigger` |
| Usable | `Actor/Usable` |
| Dialogue Actor | `Actor/Dialogue Actor` |
| Selector | `Actor/Player/Selector` |
| Proximity Selector | `Actor/Player/Proximity Selector` |
| Bark On Idle | `Trigger/Bark On Idle` |
| Standard Dialogue UI | `UI/Standard UI/Dialogue/Standard Dialogue UI` |
| Standard Bark UI | `UI/Standard UI/Bark/Standard Bark UI` |

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Pressing F does nothing | Player has no `Selector` or `ProximitySelector` | Add the correct component to the Player via **Add Component** |
| Conversation starts but no UI appears | No `StandardDialogueUI` prefab linked to Dialogue Manager | Assign a UI prefab in the Dialogue Manager's **Display Settings > Default UI** field, or re-run the Dialogue Manager Wizard |
| 2D `OnTriggerEnter` never fires | `USE_PHYSICS2D` define missing | Tick **USE_PHYSICS2D** in Welcome Window → Apply |
| Conversation starts but wrong actor portrait shown | `DialogueActor` name doesn't match database actor name | Verify the **Actor** field in `DialogueActor` is spelled exactly as the actor's **Name** in the database |
| `BarkOnIdle` plays but no text appears | No bark UI on the NPC | Add `StandardBarkUI` or assign a bark UI prefab in `DialogueActor` |
| Conversation fires repeatedly on `OnTriggerEnter` | Trigger fires every frame while overlapping | Set **Once** checkbox on `DialogueSystemTrigger` or use a Condition to guard it |
| Bark bubbles stay visible when a conversation starts | Bark UI does not auto-close | Add **Bark Group Member** to each barker and enable **Hide Bark On Conversation Start**, or call `barkUI.Hide()` in an `OnConversationStart` handler on the Dialogue Manager |
| `OnUseUsable()` audio cuts off immediately | The `Usable` object disables or destroys itself the same frame, stopping its `AudioSource` | Play audio through an `AudioSource` on the Player or Dialogue Manager instead |
| World-space bubble is clipped by 3D geometry | World Canvas shares the main camera's depth buffer | Add a child `UICamera` under MainCamera (Depth-only clear, UI-only Culling Mask); remove the UI layer from MainCamera's Culling Mask |
| Selector UI customizations revert at runtime | **Instantiate Prefabs** on the Dialogue Manager re-spawns the default prefab | Remove **Basic Standard UI Selector Elements** from Instantiate Prefabs; place and style your instance directly under the Dialogue Manager's Canvas |
| Selector raycasts miss or jitter with Cinemachine | Raycasts execute in `Update()` before Cinemachine repositions the camera | Subclass `Selector` as `CinemachineSelector`; leave `Update()` empty and invoke `base.Update()` from a `CinemachineCore.CameraUpdatedEvent` handler |
| `Selector` or `ProximitySelector` fails to detect `Usable` objects in 2D | `Rigidbody2D` missing or its **Simulated** checkbox is unchecked; camera not tagged `MainCamera`; **Layer Mask** excludes NPC layer; or UI element blocks raycasts | Ensure `USE_PHYSICS2D` is defined; add `Rigidbody2D` with **Simulated** checked; set **Run Raycasts** to `In 2D`; verify camera tag is `MainCamera`; include NPC layer in **Layer Mask**; enable **Debug** on `Selector` to visualize rays |
| Joystick buttons cannot directly select response menu options | Response buttons have no keyboard/joystick trigger assigned | Add `UIButtonKeyTrigger` to each `StandardUIResponseButton` in the panel's **Buttons** list and set the button name (e.g. `JoystickButton0`); use `[position=#]` tags on dialogue entries to direct responses to specific button slots |
| `ProximitySelector` or `Selector` highlights a `Usable` NPC even when its `DialogueSystemTrigger` conditions evaluate to false | `Selector` and `Usable` are generic interaction systems; they do not automatically check `DialogueSystemTrigger` conditions | Subclass `ProximitySelector` (or `Selector`) and override `CheckTriggerEnter`; fetch the target's `DialogueSystemTrigger` and return early if `!dsTrigger.condition.IsTrue(transform)`; optionally also check `DialogueManager.ConversationHasValidEntry(dsTrigger.conversation)` |
| Animated portrait with multiple nested child `Image` components does not animate correctly | Multi-part portrait setup requires Animator Controller clips that keyframe child `Image` properties | Follow the standard Animated Portraits workflow; animate child Image sprite properties and transforms in the Animator Controller clips; see "Animated Portraits Child Elements Example" in the Dialogue System Extras package |

## Boundaries

- Component wiring does not create conversation content — see `dialogue-system-for-unity-authoring-conversations`.
- C# code-driven conversation start (`DialogueManager.StartConversation(...)`) is covered in `dialogue-system-for-unity-runtime-scripting`.
- NavMesh, pathfinding, and general NPC AI are outside this skill's scope.
- 2D physics (2D colliders, 2D trigger events) requires the `USE_PHYSICS2D` scripting define; this skill does not cover enabling it (see `dialogue-system-for-unity-setup-and-overview`).
