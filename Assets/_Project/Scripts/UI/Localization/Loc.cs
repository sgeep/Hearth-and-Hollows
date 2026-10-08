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
        /// <summary>
        /// 4g: every line of dialogue, keyed by its entry's Guid field. Written from the Dialogue System database (where English is
        /// authored) by <c>Hearthdelve → Story → Update Story Content</c>; translations live here.
        /// </summary>
        public const string DialogueTable = "Dialogue";
        /// <summary>4i-C: the credits screen's text, checked against docs/CREDITS.md.</summary>
        public const string CreditsTable = "Credits";

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
            foreach (string name in new[] { UITable, ContentTable, DialogueTable, CreditsTable })
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

        /// <summary>The entry's text, or false when the table (or the key) isn't there: the caller has its own fallback.</summary>
        public static bool TryGet(string table, string key, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(key)) return false;
            try
            {
                if (s_Tables.TryGetValue(table, out StringTable loaded))
                {
                    StringTableEntry entry = loaded.GetEntry(key);
                    text = entry?.GetLocalizedString();
                }
                else if (Application.platform != RuntimePlatform.WebGLPlayer)
                {
                    StringTableEntry entry = LocalizationSettings.StringDatabase.GetTable(table)?.GetEntry(key);
                    text = entry?.GetLocalizedString();
                }
            }
            catch (Exception)
            {
                text = null;
            }
            return !string.IsNullOrEmpty(text);
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

        /// <summary>"Fine Spider Leg" / "Fine Spider Leg (Seared)".</summary>
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
        public const string HarvestGotClean = "harvest.got_clean";
        public const string HarvestGotOverkill = "harvest.got_overkill";
        public const string HarvestGotFinisher = "harvest.got_finisher";

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
        public const string SwapSlotDetail = "swap.slot_detail";

        public const string ResultTitleExtracted = "result.title_extracted";
        public const string ResultTitleDied = "result.title_died";
        public const string ResultSummary = "result.summary";
        public const string ResultNothing = "result.nothing";
        public const string ResultNothingLost = "result.nothing_lost";
        public const string ResultDelveAgain = "result.delve_again";
        public const string ResultBackToTavern = "result.back_to_tavern";

        public const string LookTestResolution = "looktest.resolution";
        public const string LookTestResolutionPixel = "looktest.resolution_pixel";
        public const string LookTestResolutionHalf = "looktest.resolution_half";
        public const string LookTestHintDungeon = "looktest.hint_dungeon";
        public const string LookTestHintTavern = "looktest.hint_tavern";
        public const string LookTestGreeting = "looktest.greeting";
        public const string TestFloorHint = "testfloor.hint";
        /// <summary>The delve's controls in the day loop: no debug keys, and narrow enough for the 320-pixel screen.</summary>
        public const string DelveControls = "delve.controls";
        /// <summary>4d debug: which run this is (the seed replays it), the floor and the room.</summary>
        public const string DelveDebug = "delve.debug";
        /// <summary>4d step 3: the run's unbanked Gold, and what became of it.</summary>
        public const string HudRunGold = "hud.run_gold";
        public const string ResultGoldSecured = "result.gold_secured";
        public const string ResultGoldLost = "result.gold_lost";
        /// <summary>4d step 4: the power room's choice. Each power's name and description are keyed by its id.</summary>
        public const string PowerTitle = "power.title";
        public const string PowerFooter = "power.footer";
        public static string PowerName(string id) => $"power.{id}";
        /// <summary>4e: a boss's name, by its id.</summary>
        public static string BossName(string id) => $"boss.{id}";
        public const string BossDefeated = "boss.defeated";
        /// <summary>The boss bar during its frenzy's roar (4e sign-off): a text cue besides the colour.</summary>
        public const string BossFrenzy = "boss.frenzy";
        /// <summary>The delve result's acknowledgement of a boss defeated on the delve.</summary>
        public const string ResultBoss = "result.boss";
        // 4f Checkpoint C: furnishing discoveries and the boss trophy.
        public const string HudCurios = "hud.curios";
        public const string HarvestCurio = "harvest.curio";
        public const string ResultCuriosKept = "result.curios_kept";
        public const string ResultCuriosLost = "result.curios_lost";
        public const string ResultTrophy = "result.trophy";
        public const string ListMore = "list.more";
        public const string ListTwo = "list.two";
        public const string DeathCuriosLost = "death.curios_lost";
        public static string PowerDescription(string id) => $"power.{id}.desc";

        /// <summary>A furnishing's name (the catalogue's key).</summary>
        public static string FurnitureName(string id) => Loc.UI($"furniture.{id}");

        /// <summary>A quest object's name (4g Checkpoint B): "Boog's bomb".</summary>
        public static string QuestObjectName(string id) => Loc.UI($"quest_object.{id}");

        public const string HarvestQuestObject = "harvest.quest_object";
        public const string ResultQuestObjectHome = "result.quest_object_home";
        public const string ResultQuestObjectLost = "result.quest_object_lost";

        /// <summary>Furnishings by name for one line: "a", "a and b", or "a and 2 more".</summary>
        public static string FurnitureList(System.Collections.Generic.IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0) return string.Empty;
            if (ids.Count == 1) return FurnitureName(ids[0]);
            if (ids.Count == 2) return Loc.UI(ListTwo, FurnitureName(ids[0]), FurnitureName(ids[1]));
            return Loc.UI(ListMore, FurnitureName(ids[0]), ids.Count - 1);
        }

        /// <summary>Every key with its English text. Used by the editor to build the table.</summary>
        public static readonly (string key, string english)[] English =
        {
            (HudEssence, "Essence"),
            (HudSatchel, "satchel"),
            (HudSatchelFull, "satchel full: press {0} to swap"),

            (HarvestCleanKill, "clean kill!"),
            (HarvestOverkill, "overkill!"),
            (HarvestDestroyed, "{0} was destroyed"),
            (HarvestFinisher, "finisher!"),
            (HarvestGot, "{0} ×{1}"),
            (HarvestGotClean, "clean kill! {0} ×{1}"),
            (HarvestGotOverkill, "overkill! {0} ×{1}"),
            (HarvestGotFinisher, "finisher! {0} ×{1}"),

            (QualityPoor, "poor"),
            (QualityStandard, "standard"),
            (QualityFine, "fine"),
            (QualityPremium, "premium"),

            (PrepSeared, "seared"),
            (PrepChilled, "chilled"),
            (PrepInedible, "inedible"),

            (ItemFormat, "{0} {1}"),
            (ItemFormatPrep, "{0} {1} ({2})"),
            (SlotEmpty, "empty"),
            (SlotCount, "{0}"),

            (DeathTitle, "your Essence fades"),
            (DeathSubtitle, "the Hollows cast you back to the surface. choose one slot to save in your lockbox. its whole stack is kept; the rest of the haul is lost."),
            (DeathSubtitleEmpty, "the Hollows cast you back to the surface. your satchel is empty."),
            (DeathSelected, "lockbox: {0} ×{1}"),
            (DeathSelectedNone, "lockbox: nothing"),
            (DeathConfirm, "return to the surface"),
            (DeathKeepNothing, "keep nothing"),

            (SwapTitle, "satchel full"),
            (SwapSubtitle, "found {0} ×{1}. choose a slot to drop for it."),
            (SwapCancel, "leave it"),
            (SwapSlotDetail, "{0} ×{1}, {2}% fresh"),

            (ResultTitleExtracted, "back from the Cellars"),
            (ResultTitleDied, "dragged back to the surface"),
            (ResultSummary, "brought home: {0} parts. lost: {1}."),
            (ResultNothing, "no parts brought home."),
            (ResultNothingLost, "no parts brought home. lost: {0} parts."),
            (ResultDelveAgain, "delve again"),
            (ResultBackToTavern, "back to the tavern"),

            (LookTestResolution, "{0}×{1}  (F2)   scrolling: smooth (F4)"),
            (LookTestResolutionPixel, "{0}×{1}  (F2)   scrolling: pixel-perfect (F4)"),
            (LookTestResolutionHalf, "{0}×{1}  (F2)   scrolling: half-pixel (F4)"),
            (LookTestHintDungeon, "move: WASD / stick   attack: left mouse / X   dodge: Space / B   F3: tavern"),
            (LookTestHintTavern, "move: WASD / stick   F3: dungeon"),
            (LookTestGreeting, "welcome to the hearth!"),
            (TestFloorHint, "move: WASD / stick   attack: left mouse / X   dodge: Space / B   F3: tavern look test"),
            (DelveControls, "move: WASD / stick   attack: click / X   dodge: Space / B"),
            (DelveDebug, "seed {0} · floor {1} · room {2}"),
            (HudRunGold, "{0} gold"),
            (ResultGoldSecured, "+{0} gold to the purse"),
            (ResultGoldLost, "{0} gold left in the dark"),
            (PowerTitle, "choose a power"),
            ("boss.larder_troll", "the Larder Troll"),
            (BossDefeated, "{0} falls"),
            (BossFrenzy, "{0} rages!"),
            (ResultBoss, "you felled {0}"),
            (HudCurios, "{0}"),
            (HarvestCurio, "found: {0}"),
            (HarvestQuestObject, "found: {0}!"),
            (ResultQuestObjectHome, "brought home: {0}"),
            (ResultQuestObjectLost, "lost: {0}. still down there somewhere"),
            (ResultCuriosKept, "found for Tally Ho!: {0}"),
            (ResultCuriosLost, "found, then lost: {0}"),
            (ResultTrophy, "a trophy: {0}"),
            (ListTwo, "{0} and {1}"),
            (ListMore, "{0} and {1} more"),
            (DeathCuriosLost, "lost with you: {0}"),
            (PowerFooter, "it lasts until you leave the Hollows"),
            ("power.deep_reserves", "deep reserves"),
            ("power.deep_reserves.desc", "+{0} max Essence, filled at once"),
            ("power.slow_burn", "slow burn"),
            ("power.slow_burn.desc", "Essence drains {0}% slower"),
            ("power.thick_hide", "thick hide"),
            ("power.thick_hide.desc", "hits cost {0}% less Essence"),
            ("power.keen_edge", "keen edge"),
            ("power.keen_edge.desc", "light attacks deal {0}% more damage"),
            ("power.heavy_hand", "heavy hand"),
            ("power.heavy_hand.desc", "charged attacks deal {0}% more damage"),
            ("power.light_feet", "light feet"),
            ("power.light_feet.desc", "the dodge roll recovers {0}% sooner"),
            ("power.second_wind", "second wind"),
            ("power.second_wind.desc", "each room cleared restores {0} Essence"),
            ("power.butchers_eye", "butcher's eye"),
            ("power.butchers_eye.desc", "overkill takes {0}% more spare damage: fewer bruised parts"),
        };
    }
}
