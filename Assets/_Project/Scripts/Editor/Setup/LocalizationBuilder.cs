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

        /// <summary>
        /// The English names of every piece of content, as authored (CLAUDE.md, Localization: lower case, proper nouns
        /// capitalised). The source of truth for the Content table's English, written over it on every build.
        /// </summary>
        public static readonly (string key, string english)[] ContentEnglish =
        {
            ("ingredient.spider_leg", "spider leg"),
            ("ingredient.venom_sac", "venom sac"),
            ("ingredient.slime_gel", "slime gel"),
            ("ingredient.slime_core", "slime core"),
            ("ingredient.shroom_cap", "shroom cap"),
            ("ingredient.spore_sac", "spore sac"),
            ("ingredient.bat_wing", "bat wing"),
            ("weapon.butchers_cleaver", "butcher's cleaver"),
            ("enemy.bat", "bat"),
            ("enemy.green_slime", "green slime"),
            ("enemy.larder_troll", "the Larder Troll"),
            ("enemy.giant_spider", "giant spider"),
            ("enemy.training_dummy", "training dummy"),
            ("recipe.grilled_spider_leg", "grilled spider leg"),
            ("recipe.shroom_skewer", "shroom skewer"),
            ("recipe.cellar_kebab", "cellar kebab"),
            ("recipe.gelbrew", "gelbrew"),
            ("recipe.core_tonic", "core tonic"),
            ("recipe.cellar_stew", "cellar stew"),
            ("recipe.offal_pottage", "offal pottage"),
            ("customer.villager", "villager"),
            ("customer.adventurer", "adventurer"),
            ("customer.dwarf", "dwarf"),
            ("staff.pip", "Orik"),
            ("villager.musashi", "Musashi"),
            // 4h Checkpoint C: Kariaston's people.
            ("villager.maximo", "Maximo"),
            ("villager.kaloren", "Kaloren"),
            ("villager.grim", "Grim"),
            ("villager.ogrin", "Ogrin"),
            ("villager.bart", "Bart"),
            // 4f Checkpoint C: surface staples from the Kariaston market, butchered cuts, the Biome 1 menu, Boog (id gunta).
            ("ingredient.onion", "onions"),
            ("ingredient.herbs", "herbs"),
            ("ingredient.bread", "bread"),
            ("ingredient.eggs", "eggs"),
            ("ingredient.malt", "malt"),
            ("ingredient.spider_leg_cuts", "spider leg cuts"),
            ("ingredient.bat_wing_cuts", "bat wing cuts"),
            ("recipe.brackenford_ale", "Kariaston ale"),
            ("recipe.onion_broth", "onion broth"),
            ("recipe.eggs_on_toast", "eggs on toast"),
            ("recipe.crispy_bat_wings", "crispy bat wings"),
            ("recipe.spider_leg_steaks", "spider-leg steaks"),
            ("recipe.bat_wing_platter", "bat-wing platter"),
            ("staff.gunta", "Boog"),
            ("supply.brackenford_market", "the Kariaston market"),
            ("upgrade.satchel_slots", "bigger satchel"),
            ("upgrade.max_essence", "deeper reserves"),
        };

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

            // The furniture catalogue's names, descriptions and colourways, and the palette ramps, tiers and finishes, come from
            // their own tables (4f Checkpoint B).
            FillTable(Loc.UITable, LocKeys.English.Concat(TavernLocKeys.English).Concat(LoopLocKeys.English).Concat(DecorateLocKeys.English).Concat(StoryLocKeys.English).Concat(SurfaceLocKeys.English).Concat(GardenLocKeys.English)
                .Concat(CreatorLocKeys.English).Concat(OnboardingLocKeys.English).Concat(FurnitureCatalog.English()).Concat(FurnitureLooks.English()).Concat(KeeperContent.English()).Concat(QuestObjectContent.English), k_GeneratedPrefixes);
            FillTable(Loc.ContentTable, ContentEnglish.Concat(ContentEntries));
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Key prefixes whose keys are made from names by a generator (a colorway's "look." key comes from its name): a key
        /// under one of them that's no longer generated is removed, so a renamed colorway leaves no stale entry behind.
        /// </summary>
        static readonly string[] k_GeneratedPrefixes = { "look.", "keeper." };

        internal static void FillTable(string tableName, IEnumerable<(string key, string english)> entries, string[] prunePrefixes = null)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(tableName, EditorPaths.Localization + "/Tables");
            var table = collection.GetTable(k_English) as StringTable ?? collection.AddNewTable(k_English) as StringTable;

            var keys = new HashSet<string>();
            foreach (var (key, text) in entries)
            {
                keys.Add(key);
                var entry = table.GetEntry(key);
                if (entry == null) table.AddEntry(key, text);
                else if (entry.Value != text) entry.Value = text;
            }

            if (prunePrefixes != null)
                foreach (string stale in collection.SharedData.Entries.Select(e => e.Key)
                             .Where(k => prunePrefixes.Any(k.StartsWith) && !keys.Contains(k)).ToList())
                    collection.RemoveEntry(stale);

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
