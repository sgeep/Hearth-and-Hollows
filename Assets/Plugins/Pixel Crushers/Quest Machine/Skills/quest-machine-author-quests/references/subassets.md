# Quest Machine – Subasset Type Catalog

Full reference for all built-in **QuestCondition**, **QuestAction**, and **QuestContent** subclasses. Read this file when you need parameter details for a specific subasset type while authoring a quest in the Quest Editor.

All types live under namespace `PixelCrushers.QuestMachine`. Wrapper subclasses (for serialization safety when switching between compiled and source versions) exist under `PixelCrushers.QuestMachine.Wrappers`.

Source files are under `Assets/Plugins/Pixel Crushers/Quest Machine/Scripts/Quest/Quest Subasset/`.

---

## QuestCondition Subclasses

Conditions are attached to **Condition** nodes in the Quest Editor. A Condition node transitions to **True** when all attached conditions are simultaneously satisfied. Conditions begin checking when their node becomes **Active**.

### CounterQuestCondition

Becomes true when a quest counter meets a numeric threshold.

| Field | Type | Description |
|---|---|---|
| `counterIndex` | `int` | Zero-based index into the Quest's Counters list |
| `counterValueMode` | `CounterValueConditionMode` | `AtLeast` (≥) or `AtMost` (≤) |
| `requiredCounterValue` | `QuestNumber` | Target value; can be a literal or reference another counter |

**Drive it:** send `"Increment Quest Counter"` or `"Set Quest Counter"` messages via `PixelCrushers.MessageSystem`. The counter's `changed` event triggers the re-check.

---

### MessageQuestCondition

Becomes true when a specific message is received from `PixelCrushers.MessageSystem`.

| Field | Type | Description |
|---|---|---|
| `senderSpecifier` | `QuestMessageParticipant` | `Any`, `Quester`, `QuestGiver`, or `Other` |
| `senderID` | `StringField` | Optional sender ID filter; supports `{QUESTERID}`, `{QUESTGIVERID}` |
| `targetSpecifier` | `QuestMessageParticipant` | Same as senderSpecifier |
| `targetID` | `StringField` | Optional target ID filter |
| `message` | `StringField` | Required message name |
| `parameter` | `StringField` | Optional parameter filter (blank = accept any) |
| `value` | `MessageValue` | Optional value filter: `None`, `String`, or `Int` |

**Note:** the listener is registered at end-of-frame to avoid false positives from the message that activated the node.

---

### QuestStateQuestCondition

Becomes true when another quest reaches a specific `QuestState`.

| Field | Type | Description |
|---|---|---|
| `questID` | `StringField` | ID of the quest to monitor |
| `questState` | `QuestState` | Required state (e.g., `Successful`) |

---

### QuestNodeStateQuestCondition

Becomes true when a specific node in another quest reaches a specific `QuestNodeState`.

| Field | Type | Description |
|---|---|---|
| `questID` | `StringField` | ID of the quest containing the node |
| `questNodeID` | `StringField` | ID of the node to monitor |
| `questNodeState` | `QuestNodeState` | Required state: `Inactive`, `Active`, or `True` |

---

### TimerQuestCondition

Becomes true after a fixed number of seconds have elapsed while the condition is active.

| Field | Type | Description |
|---|---|---|
| `timeInSeconds` | `QuestNumber` | Duration in seconds (literal or counter reference) |

**Driven by:** `"Timer Tick"` messages broadcast by `QuestTimerManager` every second.

---

### ParentQuestCondition

Becomes true when a node in the parent (enclosing) quest reaches a specified state. Used for sub-quest / nested quest patterns.

| Field | Type | Description |
|---|---|---|
| `questNodeID` | `StringField` | Node ID within the parent quest |
| `questNodeState` | `QuestNodeState` | Required state |

---

## QuestAction Subclasses

Actions are attached to **State Info** slots on any node (e.g., "On Become Active" on the Active state, or "On Become True" on a Condition node). They execute when the node enters that state.

### AlertQuestAction

Fires a quest alert in the Alert UI.

| Field | Type | Description |
|---|---|---|
| `contentList` | `List<QuestContent>` | Content items (Heading, Body, Icon, etc.) to display |

---

### AudioQuestAction

Plays an `AudioClip`.

| Field | Type | Description |
|---|---|---|
| `audioClip` | `AudioClip` | Clip to play via the Quest Machine audio system |

---

### AnimatorQuestAction

Sets a parameter on an `Animator` component attached to a scene entity.

| Field | Type | Description |
|---|---|---|
| `animatorID` | `StringField` | ID of the target entity (must have a Quest Entity component) |
| `parameterName` | `StringField` | Animator parameter name |
| `parameterType` | `AnimatorParameterType` | `Trigger`, `Bool`, `Int`, or `Float` |
| `boolValue` | `bool` | Value when type is `Bool` |
| `intValue` | `int` | Value when type is `Int` |
| `floatValue` | `float` | Value when type is `Float` |

---

### ActivateGameObjectQuestAction

Activates or deactivates a scene GameObject by name.

| Field | Type | Description |
|---|---|---|
| `gameObjectName` | `StringField` | Name of the scene GameObject to affect |
| `state` | `bool` | `true` = activate, `false` = deactivate |

---

### ControlSpawnerQuestAction

Starts, stops, or despawns a named spawner.

| Field | Type | Description |
|---|---|---|
| `spawnerName` | `StringField` | Name of the spawner to control |
| `controlMode` | `ControlSpawnerQuestActionMode` | `Start`, `Stop`, or `Despawn` |

---

### GiveQuestToQuesterQuestAction

Automatically gives another Quest asset to the active quester (player).

| Field | Type | Description |
|---|---|---|
| `quest` | `Quest` | Quest asset reference to give |

---

### InstantiatePrefabQuestAction

Instantiates a prefab into the scene.

| Field | Type | Description |
|---|---|---|
| `prefab` | `GameObject` | Prefab to instantiate |
| `spawnPointID` | `StringField` | Optional entity ID to use as spawn position |

---

### MessageQuestAction

Sends a `MessageSystem` message from this action.

| Field | Type | Description |
|---|---|---|
| `senderID` | `StringField` | Message sender ID (blank = quester) |
| `targetID` | `StringField` | Message target ID (blank = broadcast) |
| `message` | `StringField` | Message name |
| `parameter` | `StringField` | Message parameter |
| `value` | `MessageValue` | Optional message value (`None`, `String`, or `Int`) |

---

### SceneEventQuestAction

Invokes a named method on a `QuestMachineSceneEvents` component present in the scene.

| Field | Type | Description |
|---|---|---|
| `sceneEventName` | `StringField` | Name of the scene event entry to invoke |

---

### SetCounterValueQuestAction

Sets or increments a quest counter programmatically.

| Field | Type | Description |
|---|---|---|
| `counterIndex` | `int` | Index in the quest's Counters list |
| `operation` | `SetQuestCounterQuestActionOperation` | `Set` (assign) or `Increment` (add) |
| `value` | `QuestNumber` | Value to set or increment by |

---

### SetIndicatorQuestAction

Sets the quest indicator state displayed above an entity.

| Field | Type | Description |
|---|---|---|
| `entityID` | `StringField` | ID of the entity to update |
| `questIndicatorState` | `QuestIndicatorState` | `None`, `Offer`, `Active`, `TurnIn`, etc. |

---

### SetQuestStateQuestAction

Changes another quest's `QuestState`.

| Field | Type | Description |
|---|---|---|
| `questID` | `StringField` | ID of the quest to change |
| `questState` | `QuestState` | New state |

---

### SetQuestNodeStateQuestAction

Changes a specific node's state in another quest.

| Field | Type | Description |
|---|---|---|
| `questID` | `StringField` | ID of the quest containing the node |
| `questNodeID` | `StringField` | ID of the node |
| `questNodeState` | `QuestNodeState` | New state |

---

### SetTrackingQuestAction

Enables or disables quest tracking in the HUD.

| Field | Type | Description |
|---|---|---|
| `track` | `bool` | `true` = show in HUD, `false` = hide |

---

### UnityEventQuestAction

Invokes a serialized `UnityEvent`. Wire targets in the Inspector at runtime; this action has no designer-time parameters beyond the event.

| Field | Type | Description |
|---|---|---|
| `onExecute` | `UnityEvent` | The event to invoke |

---

## QuestContent Subclasses

Content subassets define what is displayed in the Quest **Journal**, **HUD**, **Alert**, and **Dialogue** UIs. Add them to **State Info → [State] → Journal Content / HUD Content / Alert Content / Dialogue Content** slots on any node.

### BodyTextQuestContent

A paragraph of body text. Supports runtime tags.

| Field | Type | Description |
|---|---|---|
| `bodyText` | `StringField` | Display text; may contain tags (see Common Tags below) |

---

### HeadingTextQuestContent

A heading / title line, rendered larger than body text.

| Field | Type | Description |
|---|---|---|
| `headingText` | `StringField` | Heading text; may contain tags |
| `headingLevel` | `int` | Visual prominence level (1 = largest) |

---

### IconQuestContent

Displays a sprite icon, optionally with a count label.

| Field | Type | Description |
|---|---|---|
| `image` | `Sprite` | Sprite to display |
| `count` | `QuestNumber` | Optional count value rendered beside the icon |

---

### ButtonQuestContent

An interactive button shown in the Dialogue UI.

| Field | Type | Description |
|---|---|---|
| `buttonText` | `StringField` | Button label |
| `message` | `StringField` | `MessageSystem` message name to send when clicked |
| `parameter` | `StringField` | Message parameter |

---

### AudioClipQuestContent

Plays an audio clip when the content panel is shown.

| Field | Type | Description |
|---|---|---|
| `audioClip` | `AudioClip` | Clip to play |

---

### LinkQuestContent

A clickable link that navigates to another quest in the Dialogue UI.

| Field | Type | Description |
|---|---|---|
| `questID` | `StringField` | ID of the quest to navigate to |
| `linkText` | `StringField` | Display text for the link |

---

## Common Tags

Use these tag placeholders in any `StringField` content; Quest Machine resolves them at display time.

| Tag | Resolves to |
|---|---|
| `{QUESTGIVERNAME}` | Display name of the quest giver |
| `{QUESTGIVERID}` | ID of the quest giver |
| `{QUESTERID}` | ID of the quester (player) |
| `{counterName}` | Current value of the counter named `counterName` |
| `{counterNameMax}` | Maximum value of the counter named `counterName` |
| `{counterNameMin}` | Minimum value of the counter named `counterName` |
