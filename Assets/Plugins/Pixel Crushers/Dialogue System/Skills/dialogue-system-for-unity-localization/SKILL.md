---
name: dialogue-system-for-unity-localization
description: "Use this skill whenever someone wants to translate Dialogue System text or switch the game language at runtime — e.g. 'translate my conversations', 'export CSV for translators', 'import translated CSV back', 'add a second language', 'change language at runtime', 'localise npc names', 'call SetLanguage', 'set up LocalizeUI for my buttons'. Covers the Dialogue Editor CSV export/import workflow, the Localization Tools window, runtime language switching via DialogueManager.SetLanguage and Localization.language, and the LocalizeUI component for non-database UI text. Do NOT use for TextTable authoring or font-fallback config not specific to the Dialogue System (see pixel-crushers-common-localization). When in doubt whether a localization request could involve the Dialogue System, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Dialogue System for Unity — Localization

The Dialogue System supports multi-language dialogue through two complementary paths. **Database localization** stores per-language text inside `DialogueDatabase` asset fields (e.g., `Dialogue Text es` alongside the default `Dialogue Text`). The Dialogue Editor exports and re-imports these fields as CSV files for translation teams. **UI localization** for non-database strings (HUD labels, buttons, menus) uses the Pixel Crushers Common `LocalizeUI` component backed by a `TextTable` asset. At runtime, `DialogueManager.SetLanguage` switches both systems simultaneously.

## When to use this skill

Use when:
- Exporting dialogue text from a `DialogueDatabase` to CSV files for a translation team.
- Importing translated CSV files back into the database.
- Switching the active game language at runtime from a settings menu.
- Adding `LocalizeUI` to a uGUI `Text` or `TextMeshProUGUI` element.
- Using the Localization Tools window to bulk-copy source text into localization fields.
- Resetting a saved language preference via the Clear Saved Localization Settings menu item.

Do **not** use this skill for:
- `TextTable` ScriptableObject authoring, `UILocalizationManager` component configuration, or font-fallback setup not specific to the Dialogue System → see **pixel-crushers-common-localization**.
- Importing dialogue content from third-party tools → see **dialogue-system-for-unity-import**.
- Saving or loading dialogue state across sessions → see **dialogue-system-for-unity-save-system**.

## Prerequisites

**Verify installation:**

```csharp
using System.Linq;
bool installed = System.AppDomain.CurrentDomain.GetAssemblies()
    .Any(a => a.GetType("PixelCrushers.DialogueSystem.DialogueManager") != null);
```

Required project state:
- A `DialogueDatabase` asset with at least one conversation.
- A Dialogue Manager in the scene (created via **Tools → Pixel Crushers → Dialogue System → Wizards → Setup Wizard** if needed).
- For UI text localization outside the database: a `TextTable` asset assigned to a `UILocalizationManager` component.
- Use wrapper component names when adding via Add Component menus (namespace `PixelCrushers.DialogueSystem.Wrappers` / `PixelCrushers.Wrappers`).

## Quick start

**Export database text for translation:**
1. Double-click the `DialogueDatabase` asset (or select it and click **Open Editor** in the Inspector).
2. In the Dialogue Editor, select the **Database** tab and expand the **Localization** foldout.
3. Click **+** in the **Languages** list and type your target language code (e.g., `es`).
4. Click **Export...** — choose an output folder. One CSV file per language is created.
5. Send the CSV(s) to translators to fill in the translation column.
6. Click **Import...** — select the translated CSV files. Database fields (e.g., `Dialogue Text es`) are updated.

**Switch language at runtime:**
```csharp
// Primary API — switches database lookups and refreshes LocalizeUI components:
PixelCrushers.DialogueSystem.DialogueManager.SetLanguage("es");

// Lower-level alternative (also triggers UILocalizationManager update):
PixelCrushers.DialogueSystem.Localization.language = "es";

// Revert to default language:
PixelCrushers.DialogueSystem.DialogueManager.SetLanguage(string.Empty);
```

## Workflows

### Workflow: Export dialogue database to CSV and import translated file

**Goal:** Produce translatable CSV files from a `DialogueDatabase` and re-import the completed translations.

**Steps:**
1. Open the Dialogue Editor: double-click the `DialogueDatabase` in the Project window.
2. Go to **Database tab → Localization** foldout.
3. In the **Languages** list, add each target language code (e.g., `es`, `fr`, `de`). Click **Find Languages** to auto-detect language codes already present in database fields.
4. Configure export options as needed:
   - **Export Conversation Title Instead Of ID** — uses the conversation title string as the row identifier.
   - **Use Key Field** — ties rows to a custom field (e.g., `Articy Id`) instead of numeric IDs.
   - **Include Blank Entries** — exports entries that have no Dialogue Text or Menu Text.
5. Click **Export...** and choose an output folder. A CSV file is created for each language in the list.
6. In the CSV, each row identifies a dialogue entry and has a column for each language. Translators fill in the language column(s).
7. Click **Import...**, select the translated CSV files. The database field for each entry (e.g., `Dialogue Text es`) is written.
8. Save the project (`Ctrl+S` / `Cmd+S`) to persist changes to the database asset.

**Expected result:** Each dialogue entry in the database gains a `Dialogue Text es` (or other language code) field containing the translated text. When the language is set to `es` at runtime, the Dialogue System serves that text automatically.

---

### Workflow: Runtime language switch from a settings menu

**Goal:** A settings button allows the player to switch the game language while the game is running.

**Steps:**
1. Create a script on the settings menu button that calls:
   ```csharp
   using PixelCrushers.DialogueSystem;

   public void SetSpanish()
   {
       DialogueManager.SetLanguage("es");
   }
   ```
2. Wire the method to the button's `onClick` UnityEvent in the Inspector, or call it from your settings manager.
3. `DialogueManager.SetLanguage` internally sets `Localization.language` and calls `UILocalizationManager.currentLanguage = "es"`, which refreshes all active `LocalizeUI` components in the scene.
4. To persist the language selection across sessions, add **Add Component → Pixel Crushers → UI → UI Localization Manager** to the Dialogue Manager (or a separate GameObject) and tick **Save Language In Player Prefs**.
5. For UI strings outside the database (buttons, tooltips), add **Add Component → Pixel Crushers → UI → Localize UI** to each `Text` or `TextMeshProUGUI` GameObject. Assign a `TextTable` to its **Text Table** field.
6. To reset the saved language in the Editor: **Tools → Pixel Crushers → Dialogue System → Tools → Clear Saved Localization Settings**.

**Expected result:** Clicking the button sets `Localization.language` to `"es"`. Active conversations serve `Dialogue Text es` content. `LocalizeUI` components refresh immediately.

---

### Workflow: Bulk-copy source text to localization fields with Localization Tools

**Goal:** Pre-fill localization fields with the source language text so translators have a reference baseline.

**Steps:**
1. Open **Tools → Pixel Crushers → Dialogue System → Tools → Localization Tools**.
2. Add the target `DialogueDatabase` asset(s) to the **Databases** list (use the **+** button or the **All** / **Folder** helpers).
3. Set **Main Language** to the target language code (e.g., `es`).
4. Click **Copy to es** — all default `Dialogue Text`, `Menu Text`, actor `Display Name`, and quest field values are copied into the corresponding `[field name] es` fields.
5. Export CSVs via the Dialogue Editor (Workflow 1 above) and distribute to translators.

**Expected result:** Every entry's language-specific field is pre-filled with the source text, giving translators an editable baseline rather than blank cells.

---

### Workflow: Vary per-actor dialect using TextTable token substitution

**Goal:** Give characters distinct speech patterns within a shared conversation tree, without duplicating dialogue entries.

**Steps:**
1. Create a `TextTable` ScriptableObject for each actor that needs a dialect. Add a key for each substitutable word (e.g., key `greeting`, value `Ahoy there`).
2. Store a reference to each actor's `TextTable` (e.g., in a `Dictionary<string, TextTable>` keyed by actor name, populated in `Start()`).
3. Add a script to the Dialogue Manager implementing `OnConversationLine(Subtitle subtitle)`:
   ```csharp
   void OnConversationLine(Subtitle subtitle)
   {
       var table = GetTableForActor(subtitle.speakerInfo.Name);
       if (table == null) return;
       foreach (var key in table.fieldNames)
           subtitle.formattedText.text = subtitle.formattedText.text
               .Replace("{" + key + "}", table.GetFieldValue(key));
   }
   ```
4. Wrap substitutable words in conversation text with `{token}` braces, e.g., `Good {greeting}, traveller.`

**Expected result:** Each actor's lines show dialect words from their own TextTable; the shared conversation tree requires no duplication.

---

### Workflow: Add Arabic RTL support to dialogue text and response menus

**Goal:** Display Arabic text with correct right-to-left glyph shaping and character joining, using the free Arabic Support for Unity package.

**Steps:**
1. Import the **Arabic Support for Unity** package from the Asset Store (free).
2. Add a script to the Dialogue Manager implementing both message methods:
   ```csharp
   using ArabicSupport;
   using PixelCrushers.DialogueSystem;

   public class FixToArabic : MonoBehaviour
   {
       void OnConversationLine(Subtitle subtitle)
       {
           subtitle.formattedText.text =
               ArabicFixer.Fix(subtitle.formattedText.text);
       }
       void OnConversationResponseMenu(Response[] responses)
       {
           foreach (var r in responses)
               r.formattedText.text = ArabicFixer.Fix(r.formattedText.text);
       }
   }
   ```

**Expected result:** Arabic dialogue lines and response buttons render with correct RTL glyph ordering and character joining.

---

### Workflow: Refresh a live bark immediately when the player changes language

**Goal:** Update a bark already displayed on screen to the newly selected language when `SetLanguage` is called, without waiting for the next bark cycle.

**Steps:**
1. Subclass `StandardBarkUI`:
   ```csharp
   using PixelCrushers;
   using PixelCrushers.DialogueSystem;

   public class LocalizedBarkUI : StandardBarkUI
   {
       private Subtitle _current;

       protected override void Start()
       {
           base.Start();
           UILocalizationManager.instance.languageChanged += Refresh;
       }
       protected override void OnDestroy()
       {
           if (UILocalizationManager.instance != null)
               UILocalizationManager.instance.languageChanged -= Refresh;
           base.OnDestroy();
       }
       public override void Bark(Subtitle subtitle)
       {
           _current = subtitle;
           base.Bark(subtitle);
       }
       void Refresh()
       {
           if (_current != null && isPlaying)
               barkText.text = _current.dialogueEntry.currentDialogueText;
       }
   }
   ```
2. Replace the `StandardBarkUI` component on bark prefabs with `LocalizedBarkUI`.

**Expected result:** Visible barks switch to the new language's text immediately when `DialogueManager.SetLanguage` is called.

---

### Workflow: Handle missing localized voiceover during development

**Goal:** Prevent subtitles from cutting off prematurely when localized audio files do not yet exist.

**Steps:**
1. `AudioWait(entrytaglocal)` completes immediately (without error) when the localized clip is missing, ending the subtitle too soon. Add a fallback duration in the Sequence field:
   ```
   AudioWait(entrytaglocal); Delay({{end}})
   ```
   Or wait for the typewriter to finish typing:
   ```
   AudioWait(entrytaglocal); WaitForMessage(Typed)
   ```
2. To surface missing files in the Unity Console, enable **Dialogue Manager → Display Settings → Camera & Cutscene Settings → Report Missing Audio Files**.
3. For a code-level fallback, subclass `SequencerCommandAudioWait` and override `TryAudioClip()` to fall back from `entrytaglocal` to `entrytag` when the localized clip is absent.

**Expected result:** Subtitles remain on screen for their full natural duration even when localized audio is absent; missing clips appear in the console for the audio team.

---

### Workflow: Localize alert and HUD text using GetLocalizedText

**Goal:** Display localized alerts and HUD notifications with correct grammar and word order for each language.

**Steps:**
1. Assign a `TextTable` asset to **Dialogue Manager → Display Settings → Localization Settings → Text Table**.
2. In the `TextTable`, create entry keys with positional format placeholders to support varying word order (e.g., key `quest_complete`, value `"{0}" complete!` in English, `"Terminada: {0}"` in Spanish).
3. Retrieve the translated template and format in C#:

```csharp
using PixelCrushers.DialogueSystem;

string template = DialogueManager.GetLocalizedText("quest_complete");
string msg = string.Format(template, QuestLog.GetQuestTitle(questName));
DialogueManager.ShowAlert(msg);
```

**Expected result:** Alerts and HUD labels switch language together with the rest of the game, with correct grammar per locale.

---

### Workflow: Set up per-button language selection with interactability feedback

**Goal:** Create language-select buttons that automatically disable themselves when their language is already active.

**Steps:**
Attach a `LanguageButton` script to each language-select button:

```csharp
using PixelCrushers.DialogueSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LanguageButton : MonoBehaviour
{
    [SerializeField] private string _languageCode;
    private Button _button;

    void Awake() { _button = GetComponent<Button>(); }
    void OnEnable() { Refresh(); }

    public void SetThisLanguage()
    {
        DialogueManager.SetLanguage(_languageCode);
        foreach (var lb in FindObjectsOfType<LanguageButton>()) lb.Refresh();
    }

    public void Refresh() =>
        _button.interactable = (Localization.language != _languageCode);
}
```

Wire each button's `onClick` to `LanguageButton.SetThisLanguage`.

**Expected result:** The active language's button is non-interactable; selecting any button switches the language and refreshes all button states.

---

### Workflow: Force live text update when changing language mid-conversation

**Goal:** Immediately re-display the current subtitle and responses in the new language when `SetLanguage` is called during an active conversation.

**Steps:**
`DialogueManager.SetLanguage` updates only subsequent lines. To force an instant refresh of the currently visible line:

```csharp
using PixelCrushers.DialogueSystem;

public void SwitchLanguageLive(string newLanguage)
{
    DialogueManager.SetLanguage(newLanguage);
    if (!DialogueManager.isConversationActive) return;

    var subtitle = DialogueManager.currentConversationState.subtitle;
    subtitle.formattedText.text =
        FormattedText.Parse(subtitle.dialogueEntry.currentDialogueText).text;

    var controls = DialogueManager.standardDialogueUI
        .conversationUIElements.standardSubtitleControls;
    var panel = controls.GetPanel(subtitle, out DialogueActor _);
    if (panel != null) panel.ShowSubtitle(subtitle);

    DialogueManager.UpdateResponses();
}
```

To also refresh actor name labels, call `CharacterInfo.GetLocalizedDisplayNameInDatabase(actorId)` and reassign each name field.

**Expected result:** The subtitle text, response buttons, and actor names all display in the new language in the same frame.

## Verification

```csharp
// In Play Mode, after calling SetLanguage("es"):
string lang = PixelCrushers.DialogueSystem.Localization.language;
Debug.Log("Current language: " + lang); // Should print "es"

// Start a conversation and confirm localised text appears in the dialogue UI.
// DialogueManager.StartConversation("MyConversation");
```

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `DialogueManager.SetLanguage(string)` | static method | Sets the Dialogue System language; refreshes database lookups and `LocalizeUI` components |
| `Localization.language` (get/set) | static property | Lower-level language setter (`PixelCrushers.DialogueSystem` namespace) |
| `UILocalizationManager.currentLanguage` (get/set) | instance property | Sets language for all `LocalizeUI` components; fires `languageChanged` event |
| `LocalizeUI` | MonoBehaviour | Localises a single uGUI text element from a `TextTable` |
| Dialogue Editor → Database → Localization → **Export…** | Editor button | Exports per-language CSV files from the open database |
| Dialogue Editor → Database → Localization → **Import…** | Editor button | Imports translated CSV files back into database fields |
| Dialogue Editor → Database → Localization → **Find Languages** | Editor button | Auto-detects language codes already present in the database |
| **Tools → … → Localization Tools** | Editor menu (priority 3) | Opens `LocalizationToolsWindow` for bulk copy and multi-database ops |
| **Tools → … → Clear Saved Localization Settings** | Editor menu | Deletes the saved language `PlayerPrefs` key |
| `UILocalizationManager.instance.languageChanged` | `event Action` | Fired after `SetLanguage`; subscribe to refresh any live UI not handled by a `LocalizeUI` component |
| `AudioWait(entrytaglocal)` | Sequencer command | Plays the localized audio clip matching the active language suffix (e.g., `Player_1_5_fr.ogg` for French); use instead of `AudioWait(entrytag)` |
| `DialogueManager.GetLocalizedText(string key)` | `static string` | Looks up a key in the assigned `TextTable` and returns the translated string for the current language |
| `DialogueManager.UpdateResponses()` | `static void` | Re-evaluates and re-displays the current response menu; call after a live language switch during an active conversation |

Namespaces:`PixelCrushers.DialogueSystem` for `DialogueManager` and `Localization`; `PixelCrushers` for `UILocalizationManager` and `LocalizeUI`.

## Common issues

| Symptom | Cause | Fix |
|---|---|---|
| Dialogue still shows default language after `SetLanguage` | Language code passed to `SetLanguage` doesn't match the suffix in database fields | Confirm the code (e.g., `es`) exactly matches the field suffix (e.g., `Dialogue Text es`) |
| `LocalizeUI` text doesn't update on language change | No `UILocalizationManager` in scene, or `LocalizeUI` has no `TextTable` assigned | Add `UILocalizationManager` to the scene and assign a `TextTable`; or let `DialogueManager.SetLanguage` create one automatically |
| Export produces empty CSV | No language codes added to the Languages list | Add language codes in the Localization foldout before clicking Export |
| Translations not visible after Import | Database asset not saved | Click **File → Save Project** or press `Ctrl+S` after import |
| Language reverts to default on restart | Language not saved to `PlayerPrefs` | Enable **Save Language In Player Prefs** on `UILocalizationManager`, or manually save/restore the language string |
| Localization Tools "Copy to" button is greyed out | **Main Language** field is empty or no databases in the list | Set the **Main Language** field and add at least one database |
| Shared conversations sound identical across all characters | No per-character overrides exist in the shared conversation tree | Add `{token}` placeholders to dialogue text and replace them in `OnConversationLine` using each speaker's `TextTable` |
| Audio plays in the default language after `SetLanguage` | Sequence uses `AudioWait(entrytag)` without a locale suffix | Change to `AudioWait(entrytaglocal)`; name audio files with the language code suffix (e.g., `Player_1_5_fr.ogg`) |
| Arabic text renders left-to-right with disconnected glyphs | Unity text components do not reshape or join RTL characters | Import the free Arabic Support for Unity package; apply `ArabicFixer.Fix()` in `OnConversationLine` and `OnConversationResponseMenu` |
| On-screen bark shows old language text after language switch | `StandardBarkUI` does not listen for `languageChanged` | Subclass `StandardBarkUI`, subscribe to `UILocalizationManager.instance.languageChanged`, and reassign `barkText.text` from `entry.currentDialogueText` |
| Localized audio ends the subtitle line too early during development | Missing localized clip causes `AudioWait(entrytaglocal)` to complete instantly | Append `; Delay({{end}})` or `; WaitForMessage(Typed)` to the Sequence as a fallback; enable **Report Missing Audio Files** on the Dialogue Manager |
| Active subtitle text does not update immediately after `SetLanguage` | `SetLanguage` only affects subsequent lines; the current line is not re-parsed | Re-parse `entry.currentDialogueText` via `FormattedText.Parse`, call `panel.ShowSubtitle(subtitle)`, and `DialogueManager.UpdateResponses()` to force a live refresh |
| Language-select button remains interactable for the current language | Button interactable state not refreshed after `SetLanguage` | Call `Refresh()` on each `LanguageButton` after `SetLanguage`, or subscribe to `UILocalizationManager.instance.languageChanged` |

## Boundaries

- This skill covers database text translation (CSV export/import) and runtime language switching. For `TextTable` authoring, font-fallback configuration, `GlobalTextTable`, and the Common localization API, see **pixel-crushers-common-localization**.
- The CSV format produced by the Dialogue Editor's **Export…** button is specific to localization export — it differs from the generic CSV import format used by **Tools → … → Import → CSV…**.
- The Localization Tools window "Copy to [language]" fills localization fields with *source* text. Translators must overwrite that text with actual translations.
