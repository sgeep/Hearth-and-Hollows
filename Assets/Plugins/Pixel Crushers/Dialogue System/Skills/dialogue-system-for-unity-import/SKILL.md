---
name: dialogue-system-for-unity-import
description: "Use this skill whenever someone wants to bring conversation content from an external authoring tool into a Dialogue System DialogueDatabase — e.g. 'import from Twine', 'convert my articy project', 'bring in Yarn scripts', 'import CSV conversations', 'migrate from Chat Mapper', 'convert Arcweave', 'import a JSON database', 'how do I add my dialogue to Unity'. Covers the Import menu converters (articy:draft, Arcweave, Aurora/NWN, Gem 3/Celtx, Chat Mapper, CSV, JSON, Twine 2, Yarn 1/2/3), the converter window workflow (source file, output folder, database name, then Import), and format-specific scripting-define prerequisites. Do NOT use for editing conversations after import (see dialogue-system-for-unity-authoring-conversations), translating text (see dialogue-system-for-unity-localization), or saving game state (see dialogue-system-for-unity-save-system). When in doubt whether an import request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Dialogue System for Unity — Import

The Dialogue System provides purpose-built converter windows for every major dialogue authoring tool. Each converter reads source file(s), maps the tool's concepts (nodes, actors, variables, links) to `DialogueDatabase` assets, and writes a `.asset` file into the chosen output folder. Most converters follow the same `AbstractConverterWindow` pattern: select the source file, set the output folder and database name, then click **Import**. Some converters require a Scripting Define Symbol before their import window becomes available.

## When to use this skill

Use when:
- Importing a `.csv` file formatted in the Dialogue System CSV layout into a new `DialogueDatabase`.
- Importing a Twine 2 story (exported via the Twison format as a JSON file).
- Importing Yarn 1, Yarn 2, or Yarn 3 `.yarn` scripts.
- Importing an articy:draft XML export, Chat Mapper XML, Arcweave project, Aurora (NWN) module, Celtx/Gem 3, or a Dialogue System JSON database.
- Understanding which Scripting Define Symbol to add to enable a specific importer.

Do **not** use this skill for:
- Editing conversations inside the Dialogue Editor after import → see the authoring-conversations skill.
- Translating imported dialogue into other languages → see **dialogue-system-for-unity-localization**.
- Exporting from the Dialogue System to external formats → use the Dialogue Editor's Export section.
- Saving or restoring dialogue state at runtime → see **dialogue-system-for-unity-save-system**.

## Prerequisites

**Verify installation:**

```csharp
using System.Linq;
bool installed = System.AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null);
```

**Create a target DialogueDatabase** (if one doesn't exist yet):
**Assets menu → Create → Pixel Crushers → Dialogue System → Dialogue Database**

**Scripting Define Symbols** — some importers are compiled only when the matching symbol is present. Add these in **Edit → Project Settings → Player → Other Settings → Scripting Define Symbols**, then wait for recompilation:

| Importer | Required Define Symbol |
|---|---|
| articy:draft | `USE_ARTICY` |
| Twine 2 (Twison) | `USE_TWINE` |
| Yarn 1 | `USE_YARN` |
| Yarn 2 | `USE_YARN2` |
| Yarn 3 | `USE_YARN3` |
| CSV, JSON, Chat Mapper, Aurora, Arcweave, Gem 3 | *(none required)* |

## Quick start

1. Open **Tools → Pixel Crushers → Dialogue System → Import → CSV…** (window title: "CSV Import").
2. **Source File** field: click `…` and select your `.csv` file.
3. **Output Folder** field: set the folder where the `DialogueDatabase` asset will be created (default: `Assets`).
4. **Database Filename** field: name of the output `.asset` file (e.g., `MyDatabase`).
5. Tick **Overwrite** if updating an existing database; tick **Merge** (sub-option) to merge rather than replace.
6. Click **Import**.

## Workflows

### Workflow: Import a CSV file into a DialogueDatabase

**Goal:** Convert a dialogue spreadsheet in Dialogue System CSV format into a usable `DialogueDatabase`.

**Steps:**
1. Prepare the CSV. The file must use the Dialogue System section layout — named sections: `Database`, `Actors`, `Items`, `Locations`, `Variables`, `Conversations`, `DialogueEntries`, `OutgoingLinks`. Omitted cell values should be `{{omit}}`. All sections are optional except `DialogueEntries` for conversation data.
2. Open **Tools → Pixel Crushers → Dialogue System → Import → CSV…**.
3. Set **Source File** to your `.csv` file path (click `…` to browse).
4. Set **Output Folder** (e.g., `Assets/Dialogues`) and **Database Filename** (e.g., `GameDialogue`).
5. To update an existing database: tick **Overwrite**. To merge assets into the existing database instead of replacing: also tick **Merge**.
6. Click **Import**. A progress bar appears. The Console logs the count of actors, conversations, items, variables, and locations created.
7. Assign the resulting `GameDialogue.asset` to the Dialogue Manager's **Initial Database** field in the Inspector.

**Expected result:** A `DialogueDatabase` asset at the output path containing all actors, conversations, and variables from the CSV.

---

### Workflow: Import a Twine 2 story

**Goal:** Bring a Twine 2 story published via the Twison story format into the Dialogue System.

**Steps:**
1. In Twine 2, install the **Twison** story format and publish your story — this produces a `.json` file.
2. Add the Scripting Define Symbol `USE_TWINE` in **Edit → Project Settings → Player → Scripting Define Symbols**. Wait for recompilation.
3. Open **Tools → Pixel Crushers → Dialogue System → Import → Twine 2 (Twison)…** (window title: "Twine Import").
4. In the **JSON Files** list, click **+** and browse to select the Twison `.json` file. Assign actor and conversant IDs using the dropdowns on each row.
5. Set the **Database** field to your target `DialogueDatabase` asset (use the object picker).
6. Click **Import**.

**Expected result:** Each Twine passage becomes a dialogue entry. Passage links become `OutgoingLinks` between entries in the database.

---

### Workflow: Import Yarn scripts (Yarn 1, 2, or 3)

**Goal:** Convert Yarn Spinner `.yarn` scripts into a `DialogueDatabase`.

**Steps:**
1. Add the Scripting Define Symbol matching your Yarn version:
   - Yarn 1 → `USE_YARN`
   - Yarn 2 → `USE_YARN2`
   - Yarn 3 → `USE_YARN3`
   Wait for recompilation.
2. Open the corresponding import window:
   - **Tools → … → Import → Yarn → Yarn 1…** (window: "Yarn Importer")
   - **Tools → … → Import → Yarn → Yarn 2…** (window: "Yarn 2 Importer")
   - **Tools → … → Import → Yarn → Yarn 3…** (window: "Yarn 3 Importer")
3. Add `.yarn` source files to the **source files** list in the importer. Configure player actor name and actor detection regex as needed.
4. Set **Output Folder** and **Database Filename**.
5. Click **Import**.

**Expected result:** Each Yarn node becomes a Dialogue System conversation. Yarn options become branching dialogue entries.

---

### Workflow: Import articy:draft project

**Goal:** Convert an articy:draft XML export into a `DialogueDatabase`.

**Steps:**
1. Add the Scripting Define Symbol `USE_ARTICY` and wait for recompilation.
2. In articy:draft, export your project as an XML file (use **Export → Unreal Text Exchange Format** or similar XML export depending on your articy version).
3. Open **Tools → Pixel Crushers → Dialogue System → Import → articy:draft…** (window title: "articy Import").
4. Set the **articy:draft Project** field to the exported XML file (click the `…` browse button).
5. Click **Read XML** to load the project. Review and configure entity, variable, and dialogue mapping options.
6. Set the **Save To** output folder.
7. Click **Save Database** to generate the `DialogueDatabase` asset.

**Expected result:** articy entities are mapped to DS actors, articy flows become conversations, and articy variables become Lua variables.

---

### Workflow: Process custom Ink tags into dialogue entry fields

**Goal:** Map game-specific Ink tags (e.g. `# emotion: happy`) to custom `DialogueEntry` fields at runtime during Ink story execution.

**Steps:**
1. Subclass `DialogueSystemInkIntegration` (from the Dialogue System Ink integration):
   ```csharp
   using PixelCrushers.DialogueSystem.InkSupport;
   using PixelCrushers.DialogueSystem;
   using Ink.Runtime;

   public class CustomInkIntegration : DialogueSystemInkIntegration
   {
       protected override void ProcessTag(string tag, DialogueEntry entry)
       {
           if (tag.StartsWith("emotion:"))
               Field.SetValue(entry.fields, "Emotion", tag.Substring(8).Trim());
           else
               base.ProcessTag(tag, entry);
       }
   }
   ```
2. Replace the `DialogueSystemInkIntegration` component on the Dialogue Manager with `CustomInkIntegration`.
3. In the Dialogue Editor **Templates** tab, add the `Emotion` custom field so it appears in entry inspectors.

**Expected result:** Ink lines tagged with `# emotion: happy` populate the `Emotion` field on the corresponding `DialogueEntry` at runtime.

## Verification

After any import, verify in the Editor:
1. Select the generated `.asset` file in the Project window.
2. Confirm **Actors**, **Conversations**, and **Variables** lists are populated in the Inspector preview.
3. Open the Dialogue Editor (**Tools → Pixel Crushers → Dialogue System → Dialogue Editor**), select the database, and browse the Conversations tab to confirm node structure.
4. Assign the database to the Dialogue Manager and enter Play Mode; call `DialogueManager.StartConversation("ConversationTitle")` to confirm dialogue appears.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| **Tools → … → Import → CSV…** | Editor menu | Opens `CSVConverterWindow` ("CSV Import") |
| **Tools → … → Import → JSON…** | Editor menu | Opens `JsonImportWindow` ("JSON to DS") |
| **Tools → … → Import → Chat Mapper…** | Editor menu | Opens `ChatMapperConverter` |
| **Tools → … → Import → articy:draft…** | Editor menu | Opens `ArticyConverterWindow` ("articy Import") — requires `USE_ARTICY` |
| **Tools → … → Import → Arcweave…** | Editor menu | Opens `ArcweaveImporterWindow` |
| **Tools → … → Import → Aurora (Neverwinter Nights)…** | Editor menu | Opens `AuroraConverterWindow` |
| **Tools → … → Import → Gem 3…** | Editor menu | Opens `CeltxConverterWindow` (Celtx/Gem 3) |
| **Tools → … → Import → Twine 2 (Twison)…** | Editor menu | Opens `TwineImportWindow` — requires `USE_TWINE` |
| **Tools → … → Import → Yarn → Yarn 1…** | Editor menu | Opens `YarnConverterWindow` — requires `USE_YARN` |
| **Tools → … → Import → Yarn → Yarn 2…** | Editor menu | Opens `Yarn2ImporterWindow` — requires `USE_YARN2` |
| **Tools → … → Import → Yarn → Yarn 3…** | Editor menu | Opens `Yarn3ImporterWindow` — requires `USE_YARN3` |
| **Assets → Create → … → Dialogue Database** | Editor menu | Creates a new empty `DialogueDatabase` asset |
| `DialogueSystemInkIntegration` | `MonoBehaviour` (Ink integration) | Bridges an Ink `Story` with the Dialogue System; subclass and override `ProcessTag(string tag, DialogueEntry entry)` to map custom Ink tags to `DialogueEntry` fields at runtime |

Common `AbstractConverterWindow` fields (CSV, JSON, Yarn 1, Aurora, Arcweave, Gem 3):
- **Source File** — path to the source file; click `…` to browse.
- **Output Folder** — destination folder for the generated database asset.
- **Database Filename** — name of the output `.asset` file.
- **Overwrite** / **Merge** — overwrite existing database, or merge rather than replace.
- **Import** button — runs the conversion.

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Import menu item is missing or greyed out | Required Scripting Define Symbol not set | Add the correct define (e.g., `USE_TWINE`) in Player Settings and wait for recompile |
| "Couldn't create asset" error | Output folder does not exist | Create the output folder in the Project window before importing |
| Imported database is empty | CSV section headings are wrong or the file is not UTF-8 | Confirm headings are exactly `Database`, `Actors`, `DialogueEntries`, etc. and encoding is UTF-8 |
| articy entities not mapped to DS actors | articy XML exported without technical names | Re-export from articy with the technical name option enabled |
| Conversations imported but have no links | Source syntax not supported by the chosen importer version | Verify the Yarn/Twine version and importer version match; check the articy flow type |
| Importer window opens but **Import** button is disabled | Source file path or output folder is empty | Fill in all required fields (Source File, Output Folder, Database Filename) |
| Celtx/Gem 3 character imports as NPC instead of player | Character type not set to `PC` in the Celtx catalog before export | In Celtx, open the catalog assets inspector, set the character's type to `PC`, then re-export and re-import; the importer will then set `Is Player = true` on that actor automatically |

## Boundaries

- Import converters are one-way: source → `DialogueDatabase`. They do **not** sync changes made in the Dialogue Editor back to the source tool.
- Re-importing overwrites the database (or merges, if Merge is ticked). Work done in the Dialogue Editor after a previous import may be lost on re-import unless Merge is used carefully.
- Post-import conversation editing (adding conditions, sequences, Lua scripts) is done in the Dialogue Editor; that workflow is covered by the authoring-conversations skill, not this one.
- articy, Twine, and all three Yarn importers require their matching Scripting Define Symbols; the menu items appear but the windows will not function (or will not compile) without the defines.
