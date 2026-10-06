---
name: pixel-crushers-common-message-system
description: "Use this skill whenever you need to wire up an in-process event bus — when users say things like \"how do I send a message between scripts\", \"decouple my components without direct references\", \"fire a global event from one script to another\", \"listen for an event anywhere in the scene\", \"notify objects without holding a reference\", or \"how do I use the Message Events component\". Covers PixelCrushers.MessageSystem C# API, no-code Message Events component wiring in the Inspector, and debugging with MessageSystemLogger. Do NOT use for persisting game state across sessions (see pixel-crushers-common-save-system) or for translating UI text (see pixel-crushers-common-localization). When in doubt whether this skill applies, use this skill — the Prerequisites section shows how to confirm the asset is installed."
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

# Pixel Crushers Common — Message System

The Message System is a lightweight, synchronous, in-process publish/subscribe event bus. Senders broadcast a named string `message` with an optional string `parameter` and an arbitrary `params object[]` payload to all registered `IMessageHandler` listeners — with no direct reference between sender and receiver. Use it to decouple gameplay systems: a combat script sends `"HealthChanged"`, a HUD listens, neither imports the other's type.

## When to use this skill

- Implement `IMessageHandler` and register with `MessageSystem.AddListener` in C# code.
- Wire message send/receive in the Inspector using the **Message Events** component without writing code.
- Diagnose why a listener never fires, or keeps firing after its object is destroyed.
- Enable per-object or global debug logging to trace all messages through the system.
- Understand the `MessageArgs` payload structure (sender, target, message, parameter, values).

## Prerequisites

1. Confirm **Pixel Crushers Common** is installed — the menu `Tools > Pixel Crushers` must exist.
2. Run the snippet below via a RunCommand to verify programmatically:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        System.Type found = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            found = asm.GetType("PixelCrushers.MessageSystem");
            if (found != null) break;
        }
        if (found != null)
            result.Log("OK — PixelCrushers.MessageSystem is available.");
        else
            result.LogError("NOT FOUND — install Pixel Crushers Common from the Asset Store.");
    }
}
```

3. No scene-level setup is required. `PixelCrushers.MessageSystem` is a static class available at all times in Play mode.
4. For no-code wiring, add the **Message Events** component: select a GameObject → `Add Component > Pixel Crushers > Common > Message System > Message Events`.

## Quick start

**Send** a message from any MonoBehaviour:

```csharp
using PixelCrushers;

PixelCrushers.MessageSystem.SendMessage(this, "PlayerDied", string.Empty);
```

**Receive** it in another MonoBehaviour:

```csharp
using PixelCrushers;
using UnityEngine;

public class DeathListener : MonoBehaviour, PixelCrushers.IMessageHandler
{
    private void OnEnable()  => PixelCrushers.MessageSystem.AddListener(this, "PlayerDied", string.Empty);
    private void OnDisable() => PixelCrushers.MessageSystem.RemoveListener(this, "PlayerDied", string.Empty);

    public void OnMessage(PixelCrushers.MessageArgs args)
    {
        Debug.Log("Player died — sender: " + args.GetSenderString());
    }
}
```

## Workflows

### Workflow: Send and receive messages in C#

**Goal**: Decouple a sender and receiver using the `PixelCrushers.MessageSystem` scripting API.

**Steps**:

1. Add `PixelCrushers.IMessageHandler` to the receiving class declaration.
2. In `OnEnable`, call `PixelCrushers.MessageSystem.AddListener(this, "MessageName", parameter)`. Register with an empty string parameter to listen with an empty parameter and receive that message regardless of its parameter value.
3. In `OnDisable`, call `PixelCrushers.MessageSystem.RemoveListener(this, "MessageName", parameter)` using the same arguments passed to `AddListener`.
4. Implement `void OnMessage(PixelCrushers.MessageArgs args)` and read values via `args.intValue`, `args.firstValue`, or `args.values`.
5. From the sender, call `PixelCrushers.MessageSystem.SendMessage(this, "MessageName", "parameter", optionalValues)`.

```csharp
using PixelCrushers;
using UnityEngine;

// Receiver — attach to any scene GameObject.
public class HealthDisplay : MonoBehaviour, PixelCrushers.IMessageHandler
{
    private void OnEnable()
    {
        // Empty parameter: receives "HealthChanged" with any parameter value.
        PixelCrushers.MessageSystem.AddListener(this, "HealthChanged", string.Empty);
    }

    private void OnDisable()
    {
        PixelCrushers.MessageSystem.RemoveListener(this, "HealthChanged", string.Empty);
    }

    public void OnMessage(PixelCrushers.MessageArgs args)
    {
        Debug.Log("New health: " + args.intValue);
    }
}

// Sender — attach to the player GameObject.
public class PlayerHealth : MonoBehaviour
{
    private int _hp = 100;

    public void TakeDamage(int amount)
    {
        _hp -= amount;
        // parameter = "Player"; values[0] = _hp, readable as args.intValue.
        PixelCrushers.MessageSystem.SendMessage(this, "HealthChanged", "Player", _hp);
    }
}
```

**Expected result**: When `TakeDamage(10)` is called, `HealthDisplay.OnMessage` fires in the same frame. The Console prints `New health: 90`.

---

### Workflow: No-code wiring with Message Events

**Goal**: Configure message send/receive entirely in the Inspector using the Message Events component.

**Steps**:

1. Select the **receiving** GameObject. Add component: `Pixel Crushers > Common > Message System > Message Events`.
2. In **Messages To Listen For**, click **+** and set:
   - **Message** to the string to match (e.g. `DoorOpened`).
   - **Parameter** to a specific value, or leave blank to receive that message regardless of its parameter.
   - Expand **On Message** and wire callbacks (e.g. `Animator.SetTrigger("Open")`).
3. For a **sender** (e.g. a UI Button), add a Message Events component to the sending GameObject:
   - In **Messages To Send**, click **+** and fill in **Message** and **Parameter**.
   - On the Button's **On Click ()** list, drag the sending GameObject and choose `MessageEvents > SendToMessageSystem` with the index (`0`) of the entry in Messages To Send.
4. Enter Play mode and click the Button.

**Expected result**: `SendToMessageSystem(0)` routes through `MessageSystem.SendMessage`. The listener's **On Message** UnityEvent fires and all wired callbacks execute.

---

### Workflow: Debug message flow

**Goal**: Identify which messages are sent, which listeners match, and why `OnMessage` may not fire.

**Steps**:

1. **Global logging** — set `PixelCrushers.MessageSystem.debug = true` in a script's `Awake` or via a RunCommand. Every `SendMessage` call is printed to the Console.
2. **Per-object logging** — add `Pixel Crushers > Common > Message System > Message System Logger` to any GameObject. Enable **Log When Sending Messages** or **Log When Receiving Messages**.
3. Reproduce the scenario in Play mode and inspect Console output.
4. Compare the exact sent message/parameter strings against registered listener strings.
5. Disable `MessageSystem.debug` and remove the logger before shipping.

```csharp
using PixelCrushers;
using UnityEngine;

public class MsgDebugActivator : MonoBehaviour
{
    private void Awake()
    {
        PixelCrushers.MessageSystem.debug = true; // logs every SendMessage call
    }
}
```

**Expected result**: The Console prints a line for every `MessageSystem.SendMessage` call showing sender, message, and parameter. Use these to pinpoint a string mismatch between sender and listener.

## Verification

Attach the script below to a scene GameObject and enter Play mode. The Console must print `[PASS]`.

```csharp
using PixelCrushers;
using UnityEngine;

public class MsgVerifier : MonoBehaviour, PixelCrushers.IMessageHandler
{
    private void OnEnable()
    {
        PixelCrushers.MessageSystem.AddListener(this, "VerifyTest", string.Empty);
    }

    private void OnDisable()
    {
        PixelCrushers.MessageSystem.RemoveListener(this, "VerifyTest", string.Empty);
    }

    private void Start()
    {
        PixelCrushers.MessageSystem.SendMessage(this, "VerifyTest", "ping");
    }

    public void OnMessage(PixelCrushers.MessageArgs args)
    {
        Debug.Log("[PASS] Message delivered: " + args.message + " / " + args.parameter);
    }
}
```

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MessageSystem.AddListener(listener, message, parameter)` | `static void` | Register an `IMessageHandler`. Empty `parameter` receives the message regardless of its parameter. |
| `MessageSystem.RemoveListener(listener, message, parameter)` | `static void` | Unregister from a specific message+parameter pair. |
| `MessageSystem.RemoveListener(listener)` | `static void` | Unregister from all messages for this listener. |
| `MessageSystem.IsListenerRegistered(listener, message, parameter)` | `static bool` | Check registration without side effects. |
| `MessageSystem.SendMessage(sender, message, parameter, values)` | `static void` | Broadcast to all matching listeners; `values` is `params object[]`. |
| `MessageSystem.SendMessageWithTarget(sender, target, message, parameter, values)` | `static void` | Deliver only to listeners whose registered target matches `target`. |
| `MessageSystem.SendCompositeMessage(sender, message)` | `static void` | Send a composite (unparsed) message string. |
| `MessageSystem.debug` | `static bool` | Log all outgoing messages to the Console when `true`. |
| `MessageSystem.sendInEditMode` | `static bool` | Allow messages in Edit mode (default `false`). |
| `MessageSystem.allowReceiveSameFrameAdded` | `static bool` | Allow listeners registered in the current frame to receive immediately. |
| `MessageSystem.allowExceptions` | `static bool` | Re-throw exceptions thrown inside listener `OnMessage` calls. |
| `MessageArgs.message` | `string` | The message name that was delivered. |
| `MessageArgs.parameter` | `string` | The parameter string that was delivered. |
| `MessageArgs.firstValue` | `object` | First element of the `values` array. |
| `MessageArgs.intValue` | `int` | `firstValue` cast to `int`. |
| `MessageArgs.Matches(message, parameter)` | `bool` | Returns `true` when args match both strings. |
| `MessageArgs.GetSenderString()` | `string` | Returns the sender name or type as a string. |
| `MessageArgs.IsRequiredSender(name)` | `bool` | Returns `true` if sender name matches the given string. |
| `IMessageHandler.OnMessage(args)` | `void` | Implement on any class to receive dispatched messages. |
| `MessageEvents` component | Component | Inspector-driven listener and sender wiring — no C# required. |
| `MessageSystemLogger` component | Component | Per-object send/receive debug logging. |

## Common issues

**Symptom**: `OnMessage` is never called.  
**Cause**: Listener not registered, or message/parameter string mismatch.  
**Fix**: Confirm `AddListener` is called before the message is sent (use `OnEnable`). Register with an empty string parameter to receive the message regardless of its parameter. Set `MessageSystem.debug = true` to see the exact strings sent.

---

**Symptom**: Listener fires after the GameObject is destroyed (`MissingReferenceException`).  
**Cause**: `RemoveListener` was never called.  
**Fix**: Call `MessageSystem.RemoveListener(this, message, parameter)` in `OnDisable`. For safety, call `MessageSystem.RemoveListener(this)` in `OnDestroy` to clear all subscriptions.

---

**Symptom**: No messages fire in Edit mode (e.g. from `[ExecuteInEditMode]`).  
**Cause**: `MessageSystem.sendInEditMode` is `false` by default.  
**Fix**: Set `PixelCrushers.MessageSystem.sendInEditMode = true` only when Edit-mode messaging is intentional; do not enable this globally in production.

---

**Symptom**: Message Events **On Message** callback never executes.  
**Cause**: The component is disabled, or the **Message**/**Parameter** fields in Messages To Listen For do not exactly match the sent strings.  
**Fix**: Confirm the GameObject is active and enabled. Leave the Parameter field blank to receive that message regardless of its parameter.

## Boundaries

- **In-process only.** Messages are dispatched synchronously within the current Unity process. There is no networking, serialization, or cross-process delivery.
- **No persistence.** State is not saved across scene loads or sessions. For persistent game state use the Save System (see `pixel-crushers-common-save-system`).
- **Not a replacement for direct calls.** When two objects share a direct reference, a method call is simpler and cheaper. Use Message System for genuine decoupling across module boundaries.
- **Synchronous dispatch.** Messages fire in the same frame they are sent. There is no deferred queue or thread-safe delivery mechanism.
