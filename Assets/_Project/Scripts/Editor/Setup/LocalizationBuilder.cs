using System.Collections.Generic;
using System.Linq;
using Hearthdelve.UI.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Creates Localization settings, the English locale, and the "UI" and "Content" string
    /// tables. English is the source language and is owned by code (LocKeys.English and the
    /// content generator), so its text is synced on every run; other locales are never touched.
    /// </summary>
    public static class LocalizationBuilder
    {
        static readonly LocaleIdentifier k_English = new("en");

        /// <summary>Content-table entries (ingredient/enemy/weapon names).</summary>
        public static readonly List<(string key, string english)> ContentEntries = new();

        [MenuItem("Hearthdelve/Setup/Localization Tables", priority = 30)]
        public static void BuildMenu() => Build();

        public static void Build()
        {
            EditorPaths.Ensure(EditorPaths.Localization);
            EditorPaths.Ensure(EditorPaths.Localization + "/Locales");
            EditorPaths.Ensure(EditorPaths.Localization + "/Tables");

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Localization Settings";
                AssetDatabase.CreateAsset(settings, EditorPaths.Localization + "/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            // Fall back to English when the system language has no locale.
            foreach (var selector in settings.GetStartupLocaleSelectors())
                if (selector is SpecificLocaleSelector specific) specific.LocaleId = k_English;
            EditorUtility.SetDirty(settings);

            var english = LocalizationEditorSettings.GetLocale(k_English);
            if (english == null)
            {
                english = Locale.CreateLocale(k_English);
                AssetDatabase.CreateAsset(english, EditorPaths.Localization + "/Locales/English (en).asset");
                LocalizationEditorSettings.AddLocale(english);
            }

            FillTable(Loc.UITable, LocKeys.English.Concat(TavernLocKeys.English));
            FillTable(Loc.ContentTable, ContentEntries);
            AssetDatabase.SaveAssets();
        }

        static void FillTable(string tableName, IEnumerable<(string key, string english)> entries)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(tableName, EditorPaths.Localization + "/Tables");
            var table = collection.GetTable(k_English) as StringTable ?? collection.AddNewTable(k_English) as StringTable;

            foreach (var (key, text) in entries)
            {
                var entry = table.GetEntry(key);
                if (entry == null) table.AddEntry(key, text);
                else if (entry.Value != text) entry.Value = text;
            }

            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            EditorUtility.SetDirty(collection);
        }

        /// <summary>A reference to a Content-table entry, creating the key if needed.</summary>
        public static LocalizedString ContentString(string key, string english)
        {
            if (!ContentEntries.Exists(e => e.key == key)) ContentEntries.Add((key, english));
            return new LocalizedString(Loc.ContentTable, key);
        }
    }
}
