using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The keeper's looks (4g Checkpoint B): the four bodies the creator offers and the colourways their parts can take. Every body is
    /// a complete Minifantasy figure with all the animations the keeper plays (checked in <c>KeeperLooksTests</c>); every ramp is
    /// sampled from Minifantasy's own drawings (A Myriad of NPCs' skin, hair and top layers, and the base races), never tinted.
    /// The bodies' drawn colours are ramps too, so each body's own look is one of the choices and "as drawn" is exact. Run by the
    /// story updater; the keeper prefabs get a <see cref="KeeperAppearance"/>. Rerunning rewrites these assets (nothing here is
    /// hand-tuned).
    /// </summary>
    public static class KeeperContent
    {
        const string Folder = EditorPaths.Data + "/Characters/Keeper";
        public const string LooksPath = Folder + "/KeeperLooks.asset";
        public const string PalettesPath = Folder + "/KeeperPalettes.asset";

        public const string Skin = "keeper_skin";
        public const string OrcSkin = "keeper_orc_skin";
        public const string Hair = "keeper_hair";
        public const string Outfit = "keeper_outfit";

        // Skins: the top four shades of A Myriad of NPCs' skin layers (the Townsfolk's and the Amazon's skin are "light" exactly),
        // and the dwarf's own.
        static readonly (string id, string name, string[] colors)[] k_Skins =
        {
            ("light", "light", new[] { "cdac85", "ddb78f", "eec39a", "facba6" }),
            ("rosy", "rosy", new[] { "cb9883", "dba28e", "eeae9a", "fdb8a8" }),
            ("pale", "pale", new[] { "b59f9e", "c3afac", "dfc6bb", "f8dcc6" }),
            ("tan", "tan", new[] { "7e6b4c", "9a815d", "b1906c", "ca9f7b" }),
            ("brown", "brown", new[] { "3a2311", "502c1a", "774033", "914e49" }),
        };

        // An orc's skin: its own green (every orc in the packs shares it) and the goblins' green, from Base Humanoids.
        static readonly (string id, string name, string[] colors)[] k_OrcSkins =
        {
            ("orc_green", "moss green", new[] { "2e6d34", "36803b", "3c8f40", "439f43", "4fad4b" }),
            ("goblin_green", "leaf green", new[] { "538d36", "5f9b3c", "69a640", "76b846", "87c64f" }),
        };

        // Hair (and beards): A Myriad of NPCs' hair layers, then the bodies' own.
        static readonly (string id, string name, string[] colors)[] k_Hair =
        {
            ("chestnut", "chestnut", new[] { "49240b", "5a2b0c", "743810", "924614", "a3511b" }),
            ("ginger", "ginger", new[] { "a76224", "d37631", "e99166" }),
            ("golden", "golden", new[] { "a25c0f", "c27117", "dd8a2f" }),
            ("crimson", "crimson", new[] { "660f1b", "861626", "aa2f41" }),
            ("black", "black", new[] { "323232", "575757", "787878" }),
            ("blonde", "blonde", new[] { "d1800e", "df9c0f", "e3b430" }),
            ("brown", "brown", new[] { "53240b", "803b20", "a15949" }),
            ("red", "red", new[] { "b52b37", "cc5337", "dd7068" }),
            ("white", "white", new[] { "979797", "b5b5b5", "e1e1e1" }),
        };

        // Clothes: the bodies' own, then A Myriad of NPCs' tops.
        static readonly (string id, string name, string[] colors)[] k_Outfits =
        {
            ("navy", "navy", new[] { "404e8f", "4757a2", "5062b3" }),
            ("forest", "forest green", new[] { "17461f", "2e7238", "3d924a" }),
            ("rust", "rust", new[] { "792e0d", "9c3820" }),
            ("leather", "leather", new[] { "884524", "a0522c", "b35526" }),
            ("red", "red", new[] { "a51515", "cc3838", "e65858" }),
            ("blue", "sky blue", new[] { "3c6b95", "488ca6", "56a1b5" }),
            ("purple", "purple", new[] { "613f87", "745aac", "8c70c8" }),
            ("turquoise", "turquoise", new[] { "1e8374", "339e80", "43b184" }),
            ("magenta", "magenta", new[] { "8e3970", "a84a99", "bd64af" }),
            ("orange", "orange", new[] { "a56415", "cc7638", "dc8257" }),
            ("yellow", "yellow", new[] { "c59419", "dda312", "f3b34d" }),
            ("white", "white", new[] { "acacac", "c5c5c5", "e6e6e6" }),
        };

        /// <summary>Localization key of a body's name, and of a ramp's.</summary>
        public static string BodyKey(string id) => $"keeper.body.{id}";
        public static string RampKey(string kind, string id) => $"keeper.{kind.Replace("keeper_", string.Empty)}.{id}";

        /// <summary>The bodies' names: what the figure is, not a promise of a class or a background.</summary>
        static readonly (string id, string name)[] k_Bodies =
        {
            ("townsfolk", "townsfolk"),
            ("amazon", "warrior"),
            ("dwarf", "dwarf"),
            ("orc", "orc"),
        };

        /// <summary>The English the creator's options need (the localization pass writes them into the UI table).</summary>
        public static IEnumerable<(string key, string english)> English()
        {
            foreach (var (id, name) in k_Bodies) yield return (BodyKey(id), name);
            foreach (var (kind, ramps) in Kinds())
                foreach (var (id, name, _) in ramps) yield return (RampKey(kind, id), name);
        }

        static IEnumerable<(string kind, (string id, string name, string[] colors)[] ramps)> Kinds()
        {
            yield return (Skin, k_Skins);
            yield return (OrcSkin, k_OrcSkins);
            yield return (Hair, k_Hair);
            yield return (Outfit, k_Outfits);
        }

        public static KeeperLooks Build()
        {
            EditorPaths.Ensure(Folder);
            SpriteAnimationSet townsfolk = LookTestContent.Load<SpriteAnimationSet>($"{EditorPaths.Animations}/Anim_HumanTownsfolk.asset");
            SpriteAnimationSet humanShadow = LookTestContent.Load<SpriteAnimationSet>($"{EditorPaths.Animations}/Anim_HumanTownsfolk_Shadow.asset");
            if (townsfolk == null || humanShadow == null)
            {
                LookTestContent.BuildAnimationSets(out townsfolk, out humanShadow, out _, out _, out _);
            }
            SpriteAnimationSet amazon = BodySet("Anim_KeeperAmazon", "Amazon", "Die");
            SpriteAnimationSet orc = BodySet("Anim_KeeperWildOrc", "WildOrc", "Die");
            SpriteAnimationSet dwarf = BodySet("Anim_KeeperDwarf", "DwarfYellowBeard", "SpinDie");
            SpriteAnimationSet dwarfShadow = DwarfShadow();

            PaletteLibrary palettes = LookTestContent.CreateOrUpdate<PaletteLibrary>(PalettesPath, library =>
            {
                library.ramps = new List<PaletteRamp>();
                library.presets = new List<PalettePreset>();
                foreach (var (kind, ramps) in Kinds())
                    foreach (var (id, _, colors) in ramps)
                        library.ramps.Add(new PaletteRamp { id = $"{kind}.{id}", kind = kind, nameKey = RampKey(kind, id), colors = colors.Select(Hex).ToArray() });
            });

            KeeperLooks looks = LookTestContent.CreateOrUpdate<KeeperLooks>(LooksPath, l =>
            {
                l.palettes = palettes;
                l.bodies = new List<KeeperBody>
                {
                    Body("townsfolk", townsfolk, humanShadow, (Skin, "light", Ramp(k_Skins, "light")), (Hair, "chestnut", Ramp(k_Hair, "chestnut")),
                        (Outfit, "navy", Ramp(k_Outfits, "navy"))),
                    Body("amazon", amazon, humanShadow, (Skin, "light", Ramp(k_Skins, "light")), (Hair, "ginger", Ramp(k_Hair, "ginger")),
                        (Outfit, "forest", Ramp(k_Outfits, "forest"))),
                    Body("dwarf", dwarf, dwarfShadow, (Skin, "rosy", Ramp(k_Skins, "rosy")), (Hair, "golden", Ramp(k_Hair, "golden")),
                        (Outfit, "rust", Ramp(k_Outfits, "rust"))),
                    Body("orc", orc, humanShadow, (OrcSkin, "orc_green", Ramp(k_OrcSkins, "orc_green")), (Hair, "crimson", Ramp(k_Hair, "crimson")),
                        (Outfit, "leather", Ramp(k_Outfits, "leather"))),
                };
            });

            GameDatabase database = CurioContent.Database();
            if (database != null && database.keeperLooks != looks)
            {
                database.keeperLooks = looks;
                EditorUtility.SetDirty(database);
            }
            foreach (string prefab in new[] { LookTestContent.PlayerPrefab, LookTestContent.TavernPlayerPrefab }) AddAppearance(prefab, looks);
            AssetDatabase.SaveAssets();
            return looks;
        }

        static string[] Ramp((string id, string name, string[] colors)[] ramps, string id) => ramps.First(r => r.id == id).colors;

        static KeeperBody Body(string id, SpriteAnimationSet set, SpriteAnimationSet shadow, params (string kind, string ramp, string[] drawn)[] channels) => new()
        {
            id = id,
            nameKey = BodyKey(id),
            animations = set,
            shadow = shadow,
            channels = channels.Select(c => new PaletteChannel { kind = c.kind, source = c.drawn.Select(Hex).ToArray() }).ToList(),
            drawnAs = channels.Select(c => new PalettePick { kind = c.kind, ramp = $"{c.kind}.{c.ramp}" }).ToList(),
        };

        /// <summary>A body in the Townsfolk's layout, with four-facing jumps (the Townsfolk's has one row) and its own death.</summary>
        static SpriteAnimationSet BodySet(string name, string file, string die)
        {
            const string c = MinifantasySheets.Creatures;
            return LookTestContent.Set(name,
                LookTestContent.StillHead(LookTestContent.Anim(CharacterAnim.Idle, c, $"{file}Idle", 16, 4, 0.2f, true)),
                LookTestContent.Anim(CharacterAnim.Walk, c, $"{file}Walk", 4, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Attack, c, $"{file}Attack", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, c, $"{file}Dmg", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Dodge, c, $"{file}Jump", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Die, c, $"{file}{die}", 12, 1, 0.1f, false),
                LookTestContent.MirrorLeft(LookTestContent.AnimRow(CharacterAnim.Charge, c, $"{file}ChargedAttack", 6, 0, 0.1f, false)),
                LookTestContent.MirrorLeft(LookTestContent.AnimRow(CharacterAnim.ChargeHold, c, $"{file}ChargedAttack", 6, 1, 0.1f, true)),
                LookTestContent.MirrorLeft(LookTestContent.AnimRow(CharacterAnim.HeavyAttack, c, $"{file}ChargedAttack", 6, 2, 0.1f, false)));
        }

        static SpriteAnimationSet DwarfShadow()
        {
            const string c = MinifantasySheets.Creatures;
            return LookTestContent.Set("Anim_KeeperDwarf_Shadow",
                LookTestContent.StillHead(LookTestContent.Anim(CharacterAnim.Idle, c, "ShadowDwarfIdle", 16, 4, 0.2f, true)),
                LookTestContent.Anim(CharacterAnim.Walk, c, "ShadowDwarfWalk", 4, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Attack, c, "ShadowDwarfAttack", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, c, "ShadowDwarfDmg", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Dodge, c, "ShadowDwarfJump", 4, 4, 0.1f, false),
                // The pack's dwarf shadow spin has 11 frames to the body's 12: the last frame holds.
                LookTestContent.Anim(CharacterAnim.Die, c, "ShadowDwarfSpinDie", 11, 1, 0.1f, false),
                LookTestContent.MirrorLeft(LookTestContent.AnimRow(CharacterAnim.Charge, c, "ShadowDwarfChargedAttack", 6, 0, 0.1f, false)),
                LookTestContent.MirrorLeft(LookTestContent.AnimRow(CharacterAnim.ChargeHold, c, "ShadowDwarfChargedAttack", 6, 1, 0.1f, true)),
                LookTestContent.MirrorLeft(LookTestContent.AnimRow(CharacterAnim.HeavyAttack, c, "ShadowDwarfChargedAttack", 6, 2, 0.1f, false)));
        }

        static void AddAppearance(string path, KeeperLooks looks)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CharacterSpriteAnimator animator = contents.GetComponentInChildren<CharacterSpriteAnimator>(true);
                if (animator == null) throw new System.InvalidOperationException($"{path} has no sprite animator.");
                KeeperAppearance appearance = animator.GetComponent<KeeperAppearance>() ?? animator.gameObject.AddComponent<KeeperAppearance>();
                appearance.Configure(looks);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Color32 Hex(string hex) => new(System.Convert.ToByte(hex.Substring(0, 2), 16), System.Convert.ToByte(hex.Substring(2, 2), 16),
            System.Convert.ToByte(hex.Substring(4, 2), 16), 255);
    }
}
