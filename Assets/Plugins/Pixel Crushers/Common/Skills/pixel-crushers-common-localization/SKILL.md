---
name: pixel-crushers-common-localization
description: "Use this skill whenever you need to add multiple languages to a game or look up translated strings at runtime — when users say things like \"how do I localize my UI\", \"add a second language\", \"switch languages at runtime\", \"set up a text table\", \"translate TextMeshPro text\", \"export strings for translators\", \"change the game language in code\", or \"how do I use GlobalTextTable.Lookup\". Covers TextTable ScriptableObject creation and editing, UILocalizationManager and LocalizeUI component setup, runtime language switching, and bulk CSV export via Text Table Mass Export. Do NOT use for in-process event messaging between scripts (see pixel-crushers-common-message-system) or for saving and loading game state (see pixel-crushers-common-save-system). When in doubt whether this skill applies, use this skill — the Prerequisites section shows how to confirm the asset is installed."
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

# Pixel Crushers Common — Localization

The Pixel Crushers Common localization system provides a lightweight text-table approach to multilingual games: create a `TextTable` ScriptableObject with named fields and per-language translations, wire it to `UILocalizationManager` and `LocalizeUI` components, and switch the active language at runtime with a single property assignment. `GlobalTextTable.Lookup` returns the current language's string in code. A built-in Mass Export tool round-trips strings to CSV for external translators.

## When to use this skill

- Create a `TextTable` asset and populate it with languages and translated string fields.
- Set up `UILocalizationManager` and `LocalizeUI` to drive runtime language switching on uGUI Text or TextMeshPro elements.
- Switch the active language from code or a UI language selector.
- Look up localized strings by field name via `GlobalTextTable.Lookup` inside scripts.
- Export and import translations in bulk using `Tools > Pixel Crushers > Common > Text Table Mass Export`.

## Prerequisites

1. Confirm **Pixel Crushers Common** is installed — the menu `Tools > Pixel Crushers > Common > Text Table Editor` must exist.
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
            found = asm.GetType("PixelCrushers.TextTable");
            if (found != null) break;
        }
        if (found != null)
            result.Log("OK — PixelCrushers.TextTable is available.");
        else
            result.LogError("NOT FOUND — install Pixel Crushers Common from the Asset Store.");
    }
}
```

3. Each scene that displays localized UI must contain exactly one active `UILocalizationManager` component.
4. Every Text or TextMeshPro element to be localized needs a `LocalizeUI` component attached to the same GameObject.

## Quick start

1. Create a TextTable: `Assets > Create > Pixel Crushers > Common > Text > Text Table`. Name it `GameStrings`.
2. Open `Tools > Pixel Crushers > Common > Text Table Editor`. Add languages (`English`, `Spanish`) and add a field `greeting` with values `Hello` / `Hola`.
3. Add `UILocalizationManager` to any scene GameObject. Assign the `GameStrings` TextTable. Set **Current Language** to `English`.
4. On each Text or TextMeshPro GameObject to localize, add `Pixel Crushers > Common > UI > Localize UI`. Set the element's text value to the field name (e.g. `greeting`).
5. In Play mode, run: `PixelCrushers.UILocalizationManager.instance.currentLanguage = "Spanish";`  
   The text element updates immediately to `Hola`.

## Workflows

### Workflow: Create a Text Table and add languages and fields

**Goal**: Produce a `TextTable` asset that holds all translated strings for the project.

**Steps**:

1. In the Project window, right-click a folder and choose `Create > Pixel Crushers > Common > Text > Text Table`. Name it (e.g. `GameStrings`).
2. Open `Tools > Pixel Crushers > Common > Text Table Editor`.
3. With the TextTable asset selected (or dragged into the editor window), click **Add Language** and enter each language name (e.g. `English`, `French`, `Spanish`).
4. Click **Add Field** and type a field name (e.g. `mainMenu_start`). Fill in the translation for each language column.
5. Repeat step 4 for every string in the game. Use a consistent naming convention (e.g. `screen_element_context`) to avoid collisions.
6. Save the project (`Ctrl+S` / `Cmd+S`).

```csharp
// Optional: add languages and fields via an editor script.
using PixelCrushers;
using UnityEngine;

public static class TextTableSetup
{
    public static void AddExampleData(PixelCrushers.TextTable table)
    {
        table.AddLanguage("English");
        table.AddLanguage("Spanish");
        table.AddField("greeting");
        table.SetFieldTextForLanguage("greeting", "English", "Hello");
        table.SetFieldTextForLanguage("greeting", "Spanish", "Hola");
    }
}
```

**Expected result**: The TextTable asset contains all listed languages. Opening it in the Text Table Editor shows one column per language and one row per field, each cell populated with a translation.

---

### Workflow: Localize UI text with UILocalizationManager and LocalizeUI

**Goal**: Make Unity UI text elements automatically display the correct language string at runtime.

**Steps**:

1. Create an empty GameObject in the scene; name it `LocalizationManager`.
2. Add component: `Pixel Crushers > Common > UI > UI Localization Manager`.
3. Assign the **Text Table** field to the `GameStrings` TextTable. Set **Current Language** to the default language (e.g. `English`).
4. Select each Text or TextMeshPro GameObject that should be localized.
5. Add component: `Pixel Crushers > Common > UI > Localize UI`.
6. In the text field of the Text / TextMeshPro component (not in LocalizeUI), enter the TextTable field name exactly (e.g. `greeting`). `LocalizeUI` reads this value as the lookup key.
7. Enter Play mode.

**Expected result**: Each localized text element displays the English translation. No Console errors appear. Assigning `UILocalizationManager.instance.currentLanguage = "Spanish"` immediately swaps all `LocalizeUI` elements in the scene.

---

### Workflow: Switch language at runtime

**Goal**: Change the active language from code — for example, in response to a language selector dropdown.

**Steps**:

1. Confirm `UILocalizationManager` is present and a TextTable is assigned (see previous workflow).
2. Assign `currentLanguage` to trigger the switch:

```csharp
using PixelCrushers;
using UnityEngine;

public class LanguageSelector : MonoBehaviour
{
    // Call from a UI Dropdown's OnValueChanged or from a Button's OnClick.
    public void SetLanguage(string languageName)
    {
        PixelCrushers.UILocalizationManager.instance.currentLanguage = languageName;
    }

    // Retrieve a localized string in code without a UI element.
    public string GetLocalizedString(string fieldName)
    {
        return PixelCrushers.GlobalTextTable.Lookup(fieldName);
    }
}
```

3. Subscribe to `UILocalizationManager.languageChanged` for custom post-switch logic:

```csharp
using PixelCrushers;

private void OnEnable()
{
    PixelCrushers.UILocalizationManager.languageChanged += OnLanguageChanged;
}

private void OnDisable()
{
    PixelCrushers.UILocalizationManager.languageChanged -= OnLanguageChanged;
}

private void OnLanguageChanged(string language)
{
    Debug.Log("Language changed to: " + language);
}
```

4. The selected language is persisted automatically to `PlayerPrefs` under `currentLanguagePlayerPrefsKey` (default `"Language"`) when `saveLanguageInPlayerPrefs` is `true` (default).
5. Call `UILocalizationManager.instance.Initialize()` on `Start` to restore the saved language on the next session.

**Expected result**: Assigning `currentLanguage` fires `languageChanged` and calls `UpdateUIs`, updating all `LocalizeUI` components. `GlobalTextTable.Lookup("greeting")` returns `"Hola"` when Spanish is active.

---

### Workflow: Bulk translation export and import

**Goal**: Export all strings to CSV for external translators, then re-import the completed translations.

**Steps**:

1. Open `Tools > Pixel Crushers > Common > Text Table Mass Export`.
2. In the **Text Table Mass Export/Import** window, assign the TextTable asset(s) to export.
3. Click **Export** and choose a CSV file destination. Send the CSV to translators.
4. When translations are returned, open the same window, load the updated CSV, and click **Import**.
5. Save the project.

**Expected result**: The exported CSV contains one row per field and one column per language. After import the TextTable asset contains the translated values, and all `LocalizeUI` elements display the new translations when Play mode is entered.

## Verification

Set up a scene with `UILocalizationManager` assigned a TextTable that contains a field `verifyField` with the English value `"OK_EN"` and the Spanish value `"OK_ES"`. Attach the script below and enter Play mode. The Console must print `[PASS]`.

```csharp
using PixelCrushers;
using UnityEngine;

public class LocalizationVerifier : MonoBehaviour
{
    private void Start()
    {
        PixelCrushers.UILocalizationManager.instance.currentLanguage = "Spanish";
        string result = PixelCrushers.GlobalTextTable.Lookup("verifyField");
        if (result == "OK_ES")
            Debug.Log("[PASS] Localization returned: " + result);
        else
            Debug.LogError("[FAIL] Expected 'OK_ES', got: '" + result + "'");
    }
}
```

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `TextTable.AddLanguage(name)` | `void` | Add a new language column to the table. |
| `TextTable.AddField(name)` | `void` | Add a new string field row. |
| `TextTable.HasField(name)` | `bool` | Returns `true` if the field exists. |
| `TextTable.GetFieldText(fieldName)` | `string` | Returns text for `currentLanguageID`. |
| `TextTable.GetFieldTextForLanguage(field, language)` | `string` | Returns text for a specific language by name. |
| `TextTable.SetFieldTextForLanguage(field, language, text)` | `void` | Write a translation for a field/language pair. |
| `TextTable.GetLanguageID(language)` | `int` | Get the numeric ID of a named language. |
| `TextTable.ImportOtherTextTable(other)` | `void` | Merge another TextTable's fields into this one. |
| `TextTable.currentLanguageID` | `static int` | Active language index, shared across all tables. |
| `TextTable.useDefaultLanguageForBlankTranslations` | `static bool` | Fall back to default language when a translation is blank. |
| `GlobalTextTable.Lookup(fieldName)` | `static string` | Retrieve the current-language string from the global table. |
| `GlobalTextTable.currentLanguage` | `static string` | Active language name on the global table. |
| `GlobalTextTable.textTable` | `static TextTable` | The globally assigned TextTable. |
| `UILocalizationManager.instance` | `static UILocalizationManager` | Singleton access to the scene's manager. |
| `UILocalizationManager.currentLanguage` | `string` | Assign to switch language and update all `LocalizeUI` components. |
| `UILocalizationManager.languageChanged` | `static event Action<string>` | Fired after every language switch; subscribe for custom logic. |
| `UILocalizationManager.GetLocalizedText(fieldName)` | `string` | Look up a field via the manager's assigned table(s). |
| `UILocalizationManager.HasLanguage(name)` | `bool` | Check whether a language is defined in the table. |
| `UILocalizationManager.UpdateUIs(language)` | `void` | Manually trigger a UI refresh for the given language. |
| `UILocalizationManager.Initialize()` | `void` | Restore language from PlayerPrefs and refresh all UIs. |
| `UILocalizationManager.saveLanguageInPlayerPrefs` | `bool` | Persist the active language to `PlayerPrefs` (default `true`). |
| `UILocalizationManager.additionalTextTables` | `List<TextTable>` | Extra tables merged alongside the primary table. |
| `LocalizeUI` component | Component | Attach to Text/TextMeshPro to display the localized string automatically. |
| `SetLocalizedFont` component | Component | Attach to swap fonts per active language. |

## Common issues

**Symptom**: UI text displays the field name key (e.g. `greeting`) instead of the translation.  
**Cause**: The field is missing from the TextTable, the language has a blank translation, or `UILocalizationManager` is absent from the scene.  
**Fix**: Open `Tools > Pixel Crushers > Common > Text Table Editor` and verify the field and translation exist for the active language. Enable `useDefaultLanguageForBlankTranslations` on the TextTable or UILocalizationManager. Confirm a `UILocalizationManager` component is active in the scene with the TextTable assigned.

---

**Symptom**: Changing `currentLanguage` does not update the UI text.  
**Cause**: `LocalizeUI` is missing from the text elements, or `UILocalizationManager` is not in the scene.  
**Fix**: Add `Pixel Crushers > Common > UI > Localize UI` to every Text / TextMeshPro element that should localize. Confirm exactly one `UILocalizationManager` is active in the scene.

---

**Symptom**: `GlobalTextTable.Lookup("fieldName")` returns an empty string.  
**Cause**: `GlobalTextTable.textTable` is null — no `Global Text Table` component is in the scene, or it has no TextTable assigned.  
**Fix**: Add a `Global Text Table` component (`Pixel Crushers > Common > Text > Global Text Table`) to any scene GameObject and assign the TextTable. Alternatively, call `UILocalizationManager.instance.GetLocalizedText(fieldName)`.

---

**Symptom**: Language selection does not restore between sessions.  
**Cause**: `saveLanguageInPlayerPrefs` is `false`, or `Initialize()` is not called on Start.  
**Fix**: Confirm `saveLanguageInPlayerPrefs` is `true` (default) on `UILocalizationManager`. Call `UILocalizationManager.instance.Initialize()` in `Start` to restore the saved language on load.

## Boundaries

- **Strings and fonts only.** This system localizes text strings and can swap fonts per language via `SetLocalizedFont`. Audio clip or sprite localization is not built in; for that, consider Unity's Localization package.
- **No pluralization or grammatical rules.** The TextTable maps one field name to one string per language. Complex grammatical forms (pluralization, gender agreement) must be handled in application code.
- **Simple CSV round-trip, not XLIFF/TMX.** Text Table Mass Export produces flat CSV. Advanced translation memory or CAT-tool workflows require external tooling.
- **Flat field namespace.** All fields share one namespace per TextTable. Use consistent naming conventions (e.g. `screen_element_description`) to avoid collisions in large projects.
