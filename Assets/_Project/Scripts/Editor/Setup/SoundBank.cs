using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-C's approved sound effects (the owner's choices from the listening list, 2026-10-08: every recommendation, with family
    /// 15 as option B), by family: where each file comes from (the CC0 libraries in <c>C:\Dev\Music\SFX</c>, or an edit of one in
    /// <c>Tools/audio/derived</c>), where it's imported, and which feedback uses it, by the placeholder it replaces or, where one
    /// placeholder served two moments, by the feedback's name. The generators ask it when they build (<see cref="Apply"/>);
    /// <see cref="SoundSwap"/> applies it in place. Recorded in docs/CREDITS.md and docs/THIRD_PARTY.md.
    /// </summary>
    public static class SoundBank
    {
        public const string Folder = EditorPaths.Root + "/Audio/SFX/Library";
        const string Library = @"C:\Dev\Music\SFX";

        /// <summary>How loud a kind of sound sits (the 4i-C balance pass): its target RMS in dBFS over the sounding part.</summary>
        public enum Kind
        {
            /// <summary>Hits, the troll, gates slamming, crashes, deaths: the loudest moments.</summary>
            Impact,
            /// <summary>The work and the world: chopping, plates, pours, doors, coins, pickups.</summary>
            Action,
            /// <summary>Menus and notices.</summary>
            Interface,
            /// <summary>What repeats under everything: dialogue blips.</summary>
            Quiet,
            /// <summary>The keeper's footsteps, under everything else (round 2, the owner's note: 6 dB under the quiet tier).</summary>
            Footsteps,
        }

        public static float TargetDb(Kind kind) => kind switch
        {
            Kind.Impact => -16f,
            Kind.Action => -20f,
            Kind.Interface => -24f,
            Kind.Footsteps => -34f,
            _ => -28f,
        };

        public sealed class Family
        {
            public string Key;
            public string[] Sources;
            public float MinPitch = 1f, MaxPitch = 1f, Volume = 1f;
            public Kind Target = Kind.Action;
        }

        static readonly string[] k_Impact = { "Hit", "HitHeavy", "Hurt", "TrollImpact", "GateSlam", "Crash", "EnemyDeath" };
        static readonly string[] k_Interface = { "UiConfirm", "UiTick", "UiBuy", "UiChime", "UiBack", "Walkout", "SatchelFull", "FurnitureTurn", "FurnitureUndo", "Discovery" };
        static readonly string[] k_Quiet = { "Blip", "Blip.Low" };
        static readonly string[] k_Footsteps = { "Footsteps.Village", "Footsteps.Tavern", "Footsteps.Hollows" };

        static Kind KindOf(string key) =>
            k_Impact.Contains(key) ? Kind.Impact : k_Interface.Contains(key) ? Kind.Interface : k_Quiet.Contains(key) ? Kind.Quiet
            : k_Footsteps.Contains(key) ? Kind.Footsteps : Kind.Action;

        /// <summary>
        /// The family's playing volume: what brings its measured loudness to its kind's target (never above 1: a quiet clip plays
        /// at full), times its own trim. A family that can't be measured plays at its trim.
        /// </summary>
        public static float Volume(Family family)
        {
            var level = SoundLevels.Measure(family);
            if (!level.HasValue) return family.Volume;
            float gain = Mathf.Pow(10f, (TargetDb(family.Target) - level.Value.rms) / 20f);
            return Mathf.Clamp(gain * family.Volume, 0.05f, 1f);
        }

        static string K(string name) => Path.Combine(Library, "Kenney Audio", name);
        static string O(string relative) => Path.Combine(Library, relative);
        static string D(string name) => Path.Combine("Tools", "audio", "derived", name);
        /// <summary>The Leohpaz packs (round 2, 2026-10-09), in their own folder beside the other libraries.</summary>
        public const string Leohpaz = @"C:\Dev\Music\Leohpaz SFX";
        static string L(string relative) => Path.Combine(Leohpaz, relative);
        const string Farm = @"Farm_SFX_Pack_Minifantasy_Compatible\", Inventory = @"Inventory_SFX_Pack\Inventory_SFX\",
            Grunts = @"LEOHPAZ_HumanoidsGrunts_SFX\", Craft = @"Minifantasy_CraftingAndProfessions2_SFX\",
            Blips = @"Leohpaz_RetroDialogue_SFX\RetroDialogue_SFX\Synth_Blips\";
        // The four packs sorted into their own folders on 2026-10-09 (their licence is on their store pages).
        const string Movement = @"90 Player Movement\", Battle = @"90 RPG Battle\", Dungeon = @"Minifantasy Dungeon\", Plains = @"Minifantasy Forgotten Plains\";
        static string[] Ks(string stem, int count, int digits = 3, int start = 0) =>
            Enumerable.Range(start, count).Select(i => K($"{stem}{i.ToString().PadLeft(digits, '0')}.ogg")).ToArray();

        static Family F(string key, string[] sources, float pitch = 0f, float volume = 1f) =>
            new() { Key = key, Sources = sources, MinPitch = 1f - pitch, MaxPitch = 1f + pitch, Volume = volume, Target = KindOf(key) };

        /// <summary>The families (the listening list's numbers in comments).</summary>
        public static readonly Family[] Families =
        {
            F("Footsteps.Village", new[] { L(Farm + @"_Generic_Human\Step_dirt_1.wav"), L(Farm + @"_Generic_Human\Step_dirt_2.wav"), L(Farm + @"_Generic_Human\Step_dirt_3.wav") }), // 1, round 2 B
            F("Footsteps.Tavern", new[] { L(Movement + "10_Step_wood_01.wav"), L(Movement + "11_Step_wood_02.wav"), L(Movement + "12_Step_wood_03.wav") }), // 2, round 2 A
            F("Footsteps.Hollows", new[] { L(Farm + @"_Generic_Human\Step_stone_1.wav"), L(Farm + @"_Generic_Human\Step_stone_2.wav"), L(Farm + @"_Generic_Human\Step_stone_3.wav") }), // 3, round 2 B
            F("Dodge", new[] { L(Movement + "65_Dash_evade_01.wav"), L(Movement + "66_Dash_evade_02.wav"), L(Movement + "67_Dash_evade_03.wav") }, 0.04f), // 4, round 2 A
            F("Swing", new[] { L(Dungeon + "27_sword_miss_1.wav"), L(Dungeon + "27_sword_miss_2.wav"), L(Dungeon + "27_sword_miss_3.wav") }, 0.05f, 0.7f), // 5, round 2 A
            F("Hit", Enumerable.Range(14, 5).Select(i => L(Battle + $"{i}_Impact_flesh_0{i - 13}.wav")).ToArray(), 0.04f), // 6, round 2 B
            F("HitHeavy", Enumerable.Range(9, 5).Select(i => L(Battle + $"{i:00}_Impact_0{i - 8}.wav")).ToArray(), 0.04f), // 7, round 2 A
            F("Hurt", new[] { L(Dungeon + "11_human_damage_1.wav"), L(Dungeon + "11_human_damage_2.wav"), L(Dungeon + "11_human_damage_3.wav") }, 0.03f), // 8, round 2 A
            F("Telegraph", new[] { K("impactBell_heavy_004.ogg") }),                                                     // 9 A
            F("EnemyDeath", new[] { L(Battle + "69_Enemy_death_01.wav"), L(Battle + "70_Enemy_death_02.wav"), L(Battle + "72_Enemy_death_04.wav") }, 0.04f), // 10, round 2 A // 10 A
            F("Pickup", new[] { L(Inventory + @"Managing\Item_Pick.wav") }, 0.05f),                                     // 11, round 2 B
            F("SatchelFull", new[] { L(Inventory + @"Bag\Bag_Full.wav") }),                                              // 12, round 2 A
            F("Coins", new[] { L(Inventory + @"Drops\Coins.wav"), K("handleCoins.ogg"), K("handleCoins2.ogg") }, 0.04f), // 13, round 2 A with the Kenney pair
            F("Door", new[] { K("doorClose_1.ogg"), K("doorClose_2.ogg"), K("doorClose_3.ogg"), K("doorClose_4.ogg") }, 0f, 0.8f), // 14 A
            F("Stairs", Ks("impactPlank_medium_", 5), 0.05f, 0.7f),                                                      // 15 B (the owner's choice)
            F("GateSlam", new[] { L(Plains + "16_Hit_on_brick_1.wav"), L(Plains + "16_Hit_on_brick_2.wav") }, 0.03f),  // 16, round 2 A
            F("GateRise", new[] { L(Movement + "19_Slide_01.wav") }),                                                     // 16, round 2 C
            F("Rope", new[] { L(Movement + "40_Cling_climb_01.wav"), L(Movement + "41_Cling_climb_02_.wav"), L(Movement + "42_Cling_climb_03.wav") }, 0.04f), // 15, round 2 A (the rope only)
            F("Fall", new[] { D("Fall1.wav"), D("Fall2.wav") }),                                                         // round 2: the fall into a hole (an edit)
            F("ChargeTick2", new[] { L(Battle + "65_ATB_1.wav") }),                                                      // round 2: the charge's second level
            F("ChargeTick3", new[] { L(Battle + "66_ATB_2.wav") }),                                                      // and its third
            F("Splash", new[] { L(Movement + "13_Step_water_01.wav"), L(Movement + "14_Step_water_02.wav"), L(Movement + "15_Step_water_03.wav") }, 0.05f), // round 2 B: the tap's overflow                               // 16 C
            F("GrillFlip", new[] { D("EggFlip.wav") }, 0.05f),                                                           // 17, round 2 A (an edit)
            F("Sizzle", new[] { L(Craft + @"Crafting_Professions\Cooking\Loop_with_eggs.wav") }),                      // round 2: the grill's loop
            F("TapPour", new[] { D("TapPourLoop.wav") }),                                                                // 18 A (an edit)
            F("Clink", Ks("impactGlass_light_", 5), 0.04f),                                                              // 18 B
            F("Chop", new[] { L(Craft + @"Crafting_Professions\Food_Preparation\Food_Preparation_Cut_1.wav"), L(Craft + @"Crafting_Professions\Food_Preparation\Food_Preparation_Cut_2.wav") }, 0.05f), // 19, round 2 A
            F("Knife", new[] { K("chop.ogg"), K("knifeSlice.ogg"), K("knifeSlice2.ogg") }, 0.05f),                     // 19 B
            F("Butchery", Ks("impactWood_medium_", 5), 0.05f),                                                           // 19 C
            F("ButcherDone", new[] { L(Craft + @"Gathering_Professions\Hunting\Carving_Butcher.wav") }),                 // 19, round 2 C (done)
            F("Plate", Ks("impactPlate_light_", 5), 0.04f),                                                              // 20 A
            F("Crash", Enumerable.Range(1, 11).Select(i => D($"clamour{i}.wav")).ToArray()),                   // 20 C
            F("Stew", new[] { K("metalPot1.ogg"), K("metalPot2.ogg"), K("metalPot3.ogg") }),                           // 21 A
            F("UiConfirm", new[] { K("select_001.ogg"), K("select_002.ogg"), K("select_007.ogg") }),                   // 22 A
            F("UiTick", new[] { K("tick_002.ogg") }),                                                                    // 22 B (tick_002)
            F("UiBuy", new[] { K("confirmation_001.ogg") }),                                                             // 23 A
            F("UiChime", new[] { K("confirmation_002.ogg"), K("confirmation_004.ogg") }),                                // 23 A
            F("Discovery", new[] { K("maximize_004.ogg"), K("maximize_005.ogg"), K("maximize_006.ogg") }),             // 23 B
            F("UiBack", Ks("back_", 4, start: 1)),                                                                       // 24 A
            F("Walkout", new[] { K("error_005.ogg") }),                                                                  // 24 B (error_005)
            F("FurniturePlace", Ks("impactWood_medium_", 5), 0.04f),                                                     // 25 A
            F("FurnitureLift", new[] { K("cloth3.ogg") }, 0.05f),                                                        // 25 B
            F("FurnitureStore", new[] { K("bookPlace1.ogg"), K("bookPlace2.ogg") }),                                     // 25 B
            F("FurnitureTurn", new[] { K("switch_002.ogg") }),                                                           // 25 C
            F("FurnitureUndo", new[] { K("minimize_007.ogg") }),                                                         // 25 C
            F("Soil", new[] { L(Farm + @"Actions\Seeds_1.wav"), L(Farm + @"Actions\Seeds_2.wav"), L(Farm + @"Actions\Seeds_3.wav"), L(Farm + @"Actions\Seeds_4.wav") }, 0.04f), // 26, round 2 A (plant)
            F("Water", new[] { L(Farm + @"Actions\Watering_1.wav"), L(Farm + @"Actions\Watering_2.wav") }, 0.04f),    // 26, round 2 B (tend)
            F("Harvest", new[] { L(Farm + @"Actions\Harvest_1.wav"), L(Farm + @"Actions\Harvest_2.wav") }, 0.04f),    // 26, round 2 C
            // 27, round 2 (the owner's hard rule: only Sawtooth, Sine, Square or Triangular from Synth_Blips): Triangular, one voice
            // for everyone, its Low file for the deep voices; no random pitch (each speaker on their own semitone).
            F("Blip", new[] { L(Blips + "Triangular_High.wav") }),
            F("Blip.Low", new[] { L(Blips + "Triangular_Low.wav") }),
            F("TrollImpact", Ks("impactMining_", 5), 0.04f),                                                             // 28 A
            F("Gulp", new[] { O(@"Impacts\gulp1.wav"), O(@"Impacts\gulp2.wav") }),                                       // 28 B
            // 28, round 2: the troll's voice, layered over his impacts (a second sound in the same feedback), and his roar.
            F("TrollAttackVoice", Enumerable.Range(1, 5).Select(i => L(Grunts + $@"Troll\Troll_Attack_{i}.wav")).ToArray(), 0.03f),
            F("TrollDamageVoice", Enumerable.Range(1, 5).Select(i => L(Grunts + $@"Troll\Troll_Damage_{i}.wav")).ToArray(), 0.03f),
            F("TrollDeathVoice", new[] { L(Grunts + @"Troll\Troll_Death_1.wav"), L(Grunts + @"Troll\Troll_Death_2.wav") }),
            F("TrollRoar", Enumerable.Range(1, 3).Select(i => L(Grunts + $@"Troll\Troll_Wind_up_{i}.wav")).ToArray()),
            F("AreaWhoosh", new[] { L(Inventory + @"Drops\Drop_Whoosh.wav") }),                                       // Decorate's area change
        };

        /// <summary>Each placeholder the approved families replace. Placeholders not here stay (the gaps).</summary>
        public static readonly Dictionary<string, string> ByPlaceholder = new()
        {
            ["PH_Dodge"] = "Dodge", ["PH_Hit"] = "Hit", ["PH_HitHeavy"] = "HitHeavy", ["PH_Finisher"] = "HitHeavy", ["PH_Hurt"] = "Hurt",
            ["PH_Telegraph"] = "Telegraph", ["PH_KillClean"] = "EnemyDeath", ["PH_Thud"] = "EnemyDeath", ["PH_Pickup"] = "Pickup",
            ["PH_SatchelFull"] = "SatchelFull", ["PH_Coin"] = "Coins", ["PH_Climb"] = "Rope", ["PH_GateSlam"] = "GateSlam",
            ["PH_GateRise"] = "GateRise", ["PH_Flip"] = "GrillFlip", ["PH_FlipPerfect"] = "GrillFlip", ["PH_PourLoop"] = "TapPour",
            ["PH_Clink"] = "Clink", ["PH_LineTing"] = "Clink", ["PH_Chop"] = "Chop", ["PH_ChopDone"] = "Chop", ["PH_ChopRagged"] = "Knife",
            ["PH_KnifeIn"] = "Knife", ["PH_Cleave"] = "Butchery", ["PH_ButcherDone"] = "ButcherDone", ["PH_PlateUp"] = "Plate",
            ["PH_PlateDown"] = "Plate", ["PH_Serve"] = "Plate", ["PH_Crash"] = "Crash", ["PH_StewReady"] = "Stew",
            ["PH_UiConfirm"] = "UiConfirm", ["PH_UiTick"] = "UiTick", ["PH_UiBuy"] = "UiBuy", ["PH_UiChime"] = "UiChime",
            ["PH_Discovery"] = "Discovery", ["PH_PowerUp"] = "Discovery", ["PH_Homecoming"] = "Discovery", ["PH_FurnitureNo"] = "UiBack",
            ["PH_Walkout"] = "Walkout", ["PH_FurnitureLift"] = "FurnitureLift", ["PH_FurniturePlace"] = "FurniturePlace",
            ["PH_FurnitureTurn"] = "FurnitureTurn", ["PH_FurnitureStore"] = "FurnitureStore", ["PH_FurnitureUndo"] = "FurnitureUndo",
            ["PH_TrollSlam"] = "TrollImpact", ["PH_TrollThud"] = "TrollImpact", ["PH_TrollFall"] = "TrollImpact",
            ["PH_TrollGulp"] = "Gulp", ["PH_TrollSpoil"] = "Gulp", ["PH_TrollRoar"] = "TrollRoar", ["PH_SizzleLoop"] = "Sizzle",
            ["PH_Splash"] = "Splash",
        };

        /// <summary>Feedbacks whose placeholder served another moment too: by name, first.</summary>
        public static readonly Dictionary<string, string> ByPlayer = new()
        {
            ["Feedback_Vigor"] = "Soil",       // was the interface tick: planting is heard as soil
            ["Feedback_Harvest"] = "Harvest",  // was the interface chime
            ["Feedback_Water"] = "Water",
            ["Feedback_Swing"] = "Swing",
            ["Feedback_Door"] = "Door",
            ["Feedback_Stairs"] = "Stairs",
            ["Feedback_Area"] = "AreaWhoosh",  // Decorate's area change
            ["Feedback_Fall"] = "Fall",        // the fall into a hole (both were PH_Whoosh)
            ["Feedback_Climb"] = "Rope",       // climbing out (it shared the stairs' planks in round 1)
            ["Feedback_ChargeLevel_2"] = "ChargeTick2",
            ["Feedback_ChargeLevel_3"] = "ChargeTick3",
        };

        /// <summary>A second sound layered on a feedback, by its name (round 2: the troll's voice over his impacts).</summary>
        public static readonly Dictionary<string, string> Layers = new()
        {
            ["Feedback_Slam"] = "TrollAttackVoice",
            ["Feedback_Stun"] = "TrollDamageVoice",
            ["Feedback_Defeat"] = "TrollDeathVoice",
        };

        public static Family Get(string key) => Families.FirstOrDefault(f => f.Key == key);

        /// <summary>The family for a feedback named <paramref name="player"/> whose sound is <paramref name="clip"/> (null: none, it stays).</summary>
        public static Family For(string player, string clip)
        {
            if (player != null && ByPlayer.TryGetValue(player, out string key)) return Get(key);
            return clip != null && ByPlaceholder.TryGetValue(clip, out key) ? Get(key) : null;
        }

        static string PackFolder(string source) =>
            source.StartsWith("Tools") ? "Derived" : source.StartsWith(Leohpaz) ? "Leohpaz" : source.Contains("Kenney Audio") ? "Kenney" : "OwlishMedia";

        /// <summary>Whether an approved file comes from Leohpaz (directly; an edit of one counts as Derived).</summary>
        public static bool IsLeohpaz(string source) => source.StartsWith(Leohpaz);

        public static string AssetPath(string source) => $"{Folder}/{PackFolder(source)}/{Path.GetFileName(source)}";

        /// <summary>Every approved file, once.</summary>
        public static IEnumerable<string> Sources => Families.SelectMany(f => f.Sources).Distinct();

        /// <summary>Copies the approved files in (only those), and sets their import: short effects decompressed on load, loops compressed.</summary>
        public static int Import()
        {
            int copied = 0;
            foreach (string source in Sources)
            {
                string target = AssetPath(source);
                if (File.Exists(target)) continue;
                if (!File.Exists(source)) throw new FileNotFoundException($"Approved sound missing: {source}");
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(source, target);
                copied++;
            }
            // Anything imported that the bank no longer uses (a clip replaced by its edited copy) goes: only approved, used files stay.
            var used = new HashSet<string>(Sources.Select(s => AssetPath(s).Replace('\\', '/')));
            if (Directory.Exists(Folder))
                foreach (string file in Directory.GetFiles(Folder, "*", SearchOption.AllDirectories).Where(f => !f.EndsWith(".meta")))
                    if (!used.Contains(file.Replace('\\', '/'))) AssetDatabase.DeleteAsset(file.Replace('\\', '/'));
            AssetDatabase.Refresh();
            foreach (string source in Sources)
            {
                string target = AssetPath(source);
                if (AssetImporter.GetAtPath(target) is not AudioImporter importer) continue;
                AudioImporterSampleSettings s = importer.defaultSampleSettings;
                bool loop = source.EndsWith("TapPourLoop.wav") || source.EndsWith("Loop_with_eggs.wav");
                s.loadType = loop ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.7f;
                s.preloadAudioData = true;
                if (importer.defaultSampleSettings.loadType != s.loadType || !importer.forceToMono)
                {
                    importer.defaultSampleSettings = s;
                    importer.forceToMono = true;
                    importer.SaveAndReimport();
                }
            }
            return copied;
        }

        public static AudioClip[] Clips(Family family) =>
            family.Sources.Select(s => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetPath(s))).Where(c => c != null).ToArray();

        /// <summary>
        /// Puts <paramref name="family"/> on <paramref name="sound"/>: one clip as itself, several as random variations, with the
        /// family's pitch and volume. False if the family's clips aren't imported (the sound is left as it was).
        /// </summary>
        public static bool Apply(MMF_Sound sound, Family family)
        {
            if (sound == null || family == null) return false;
            AudioClip[] clips = Clips(family);
            if (clips.Length == 0) return false;
            sound.Sfx = clips.Length == 1 ? clips[0] : null;
            sound.RandomSfx = clips.Length == 1 ? new AudioClip[0] : clips;
            sound.MinPitch = family.MinPitch;
            sound.MaxPitch = family.MaxPitch;
            sound.MinVolume = sound.MaxVolume = Volume(family);
            sound.Label = $"Sound ({family.Key})";
            return true;
        }

        /// <summary>For the generators: the bank's family for this feedback, if it has one.</summary>
        public static void Apply(MMF_Sound sound, string player) => Apply(sound, For(player, sound?.Sfx != null ? sound.Sfx.name : null));
    }
}
