using System;
using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Hearthdelve.UI.Localization
{
    /// <summary>
    /// String-table lookups for player-facing text. Every visible string goes through here
    /// (CLAUDE.md: no literal UI text in C# or UXML). Missing keys render as the key itself
    /// so they're obvious in playtests.
    /// </summary>
    public static class Loc
    {
        public const string UITable = "UI";
        public const string ContentTable = "Content";

        static readonly Dictionary<string, StringTable> s_Tables = new();

        /// <summary>True once <see cref="Preload"/> has loaded the string tables.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>Raised when the tables finish loading; text set earlier should refresh.</summary>
        public static event Action Ready;

        /// <summary>
        /// Loads the string tables without blocking. Web builds can't wait synchronously for
        /// Addressables, so every scene preloads at boot (<see cref="LocalizationBoot"/>) and
        /// lookups read the loaded tables.
        /// </summary>
        public static IEnumerator Preload()
        {
            if (IsReady) yield break;
            yield return LocalizationSettings.InitializationOperation;
            foreach (string name in new[] { UITable, ContentTable })
            {
                var handle = LocalizationSettings.StringDatabase.GetTableAsync(name);
                yield return handle;
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null) s_Tables[name] = handle.Result;
                else Debug.LogWarning($"Localization table '{name}' failed to load.");
            }
            IsReady = true;
            Ready?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Tables.Clear();
            IsReady = false;
            Ready = null;
        }

        public static string UI(string key, params object[] args) => Get(UITable, key, args);

        public static string Get(string table, string key, params object[] args)
        {
            try
            {
                if (s_Tables.TryGetValue(table, out StringTable loaded))
                {
                    StringTableEntry entry = loaded.GetEntry(key);
                    if (entry == null) return $"#{key}";
                    string text = args != null && args.Length > 0 ? entry.GetLocalizedString(args) : entry.GetLocalizedString();
                    return string.IsNullOrEmpty(text) ? $"#{key}" : text;
                }
                // Not preloaded: a synchronous lookup works everywhere except the web.
                if (Application.platform == RuntimePlatform.WebGLPlayer) return $"#{key}";

                var value = args != null && args.Length > 0
                    ? LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args)
                    : LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
                return string.IsNullOrEmpty(value) ? $"#{key}" : value;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Localization lookup failed for {table}/{key}: {e.Message}");
                return $"#{key}";
            }
        }

        public static string Get(LocalizedString text)
        {
            if (text == null || text.IsEmpty) return "#missing";
            try
            {
                if (s_Tables.TryGetValue(text.TableReference.TableCollectionName ?? string.Empty, out StringTable loaded))
                {
                    var entry = loaded.GetEntryFromReference(text.TableEntryReference);
                    return entry != null ? entry.GetLocalizedString() : "#missing";
                }
                if (Application.platform == RuntimePlatform.WebGLPlayer) return "#missing";
                return text.GetLocalizedString();
            }
            catch (Exception) { return "#missing"; }
        }

        public static string Quality(Quality quality) => UI(quality switch
        {
            Shared.Ingredients.Quality.Poor => LocKeys.QualityPoor,
            Shared.Ingredients.Quality.Standard => LocKeys.QualityStandard,
            Shared.Ingredients.Quality.Fine => LocKeys.QualityFine,
            _ => LocKeys.QualityPremium,
        });

        /// <summary>Empty for Raw (not worth a label).</summary>
        public static string Prep(PrepState prep) => prep switch
        {
            PrepState.Seared => UI(LocKeys.PrepSeared),
            PrepState.Chilled => UI(LocKeys.PrepChilled),
            PrepState.Inedible => UI(LocKeys.PrepInedible),
            _ => string.Empty,
        };

        /// <summary>"Fine Rat Haunch" / "Fine Rat Haunch (Seared)".</summary>
        public static string ItemName(IngredientItem item)
        {
            if (!item.IsValid) return string.Empty;
            string name = Get(item.Definition.displayName);
            string quality = Quality(item.Quality);
            string prep = Prep(item.Prep);
            return string.IsNullOrEmpty(prep)
                ? UI(LocKeys.ItemFormat, quality, name)
                : UI(LocKeys.ItemFormatPrep, quality, name, prep);
        }
    }

    /// <summary>Keys in the UI string table. The table itself is generated by Hearthdelve/Setup/Localization.</summary>
    public static class LocKeys
    {
        public const string HudEssence = "hud.essence";
        public const string HudSatchel = "hud.satchel";
        public const string HudSatchelFull = "hud.satchel_full";

        public const string HarvestCleanKill = "harvest.clean_kill";
        public const string HarvestOverkill = "harvest.overkill";
        public const string HarvestDestroyed = "harvest.destroyed";
        public const string HarvestFinisher = "harvest.finisher";
        public const string HarvestGot = "harvest.got";

        public const string QualityPoor = "quality.poor";
        public const string QualityStandard = "quality.standard";
        public const string QualityFine = "quality.fine";
        public const string QualityPremium = "quality.premium";

        public const string PrepSeared = "prep.seared";
        public const string PrepChilled = "prep.chilled";
        public const string PrepInedible = "prep.inedible";

        public const string ItemFormat = "item.format";
        public const string ItemFormatPrep = "item.format_prep";
        public const string SlotEmpty = "slot.empty";
        public const string SlotCount = "slot.count";

        public const string DeathTitle = "death.title";
        public const string DeathSubtitle = "death.subtitle";
        public const string DeathSubtitleEmpty = "death.subtitle_empty";
        public const string DeathSelected = "death.selected";
        public const string DeathSelectedNone = "death.selected_none";
        public const string DeathConfirm = "death.confirm";
        public const string DeathKeepNothing = "death.keep_nothing";

        public const string SwapTitle = "swap.title";
        public const string SwapSubtitle = "swap.subtitle";
        public const string SwapCancel = "swap.cancel";

        public const string LookTestResolution = "looktest.resolution";
        public const string LookTestResolutionHalf = "looktest.resolution_half";
        public const string LookTestResolutionSmooth = "looktest.resolution_smooth";
        public const string LookTestHintDungeon = "looktest.hint_dungeon";
        public const string LookTestHintTavern = "looktest.hint_tavern";
        public const string LookTestGreeting = "looktest.greeting";
        public const string TestFloorHint = "testfloor.hint";

        /// <summary>Every key with its English text. Used by the editor to build the table.</summary>
        public static readonly (string key, string english)[] English =
        {
            (HudEssence, "Essence"),
            (HudSatchel, "Satchel"),
            (HudSatchelFull, "Satchel full — press {0} to swap"),

            (HarvestCleanKill, "Clean kill!"),
            (HarvestOverkill, "Overkill!"),
            (HarvestDestroyed, "{0} was destroyed"),
            (HarvestFinisher, "Finisher!"),
            (HarvestGot, "{0} ×{1}"),

            (QualityPoor, "Poor"),
            (QualityStandard, "Standard"),
            (QualityFine, "Fine"),
            (QualityPremium, "Premium"),

            (PrepSeared, "Seared"),
            (PrepChilled, "Chilled"),
            (PrepInedible, "Inedible"),

            (ItemFormat, "{0} {1}"),
            (ItemFormatPrep, "{0} {1} ({2})"),
            (SlotEmpty, "Empty"),
            (SlotCount, "×{0}"),

            (DeathTitle, "Your Essence Fades"),
            (DeathSubtitle, "The dungeon casts you back to the surface. Choose one slot to save in your Lockbox — its whole stack is kept, the rest of the haul is lost."),
            (DeathSubtitleEmpty, "The dungeon casts you back to the surface. Your satchel is empty."),
            (DeathSelected, "Lockbox: {0} ×{1}"),
            (DeathSelectedNone, "Lockbox: nothing"),
            (DeathConfirm, "Return to the surface"),
            (DeathKeepNothing, "Keep nothing"),

            (SwapTitle, "Satchel Full"),
            (SwapSubtitle, "Found {0} ×{1}. Choose a slot to drop for it."),
            (SwapCancel, "Leave it"),

            (LookTestResolution, "{0}×{1}  (F2)   Scrolling: pixel-perfect (F4)"),
            (LookTestResolutionHalf, "{0}×{1}  (F2)   Scrolling: half-pixel (F4)"),
            (LookTestResolutionSmooth, "{0}×{1}  (F2)   Scrolling: smooth (F4)"),
            (LookTestHintDungeon, "Move: WASD / stick   Attack: left mouse / X   Dodge: Space / B   F3: tavern"),
            (LookTestHintTavern, "Move: WASD / stick   F3: dungeon"),
            (LookTestGreeting, "Welcome to the Hearth!"),
            (TestFloorHint, "Move: WASD / stick   Attack: left mouse / X   Dodge: Space / B   F3: tavern look test"),
        };
    }
}
