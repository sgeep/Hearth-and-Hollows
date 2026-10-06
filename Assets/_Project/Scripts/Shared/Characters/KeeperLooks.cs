using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Customization;
using UnityEngine;

namespace Hearthdelve.Shared.Characters
{
    /// <summary>
    /// One of the keeper's bodies (4g Checkpoint B): a complete, clothed and fully animated Minifantasy figure (idle, walk, attack,
    /// the charged attack, the roll, being hit, dying), with the parts of its drawing that can take another Minifantasy colourway.
    /// Only figures with the whole animation set the keeper uses are offered.
    /// </summary>
    [Serializable]
    public sealed class KeeperBody
    {
        [Tooltip("Stable id, saved in the keeper's profile.")]
        public string id;
        [Tooltip("Localization key (UI table) of its name in the creator.")]
        public string nameKey;
        public SpriteAnimationSet animations;
        public SpriteAnimationSet shadow;
        [Tooltip("The parts that can be recoloured: kind keeper_skin / keeper_orc_skin / keeper_hair / keeper_outfit, with their drawn colours.")]
        public List<PaletteChannel> channels = new();
        [Tooltip("The colourway each channel is drawn in (the creator starts there, so every row names a colour).")]
        public List<PalettePick> drawnAs = new();
    }

    /// <summary>
    /// What a keeper can look like (4g Checkpoint B): the bodies, and the colourways their parts can take, every ramp sampled
    /// from Minifantasy's own art (the A Myriad of NPCs layers, the base races), never free tinting. Read by the creator and by
    /// <see cref="KeeperAppearance"/> on the keeper in the tavern and the Hollows.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Keeper Looks", fileName = "KeeperLooks")]
    public sealed class KeeperLooks : ScriptableObject
    {
        public List<KeeperBody> bodies = new();
        [Tooltip("The colourways: ramps of kind keeper_skin, keeper_orc_skin, keeper_hair and keeper_outfit.")]
        public PaletteLibrary palettes;

        public KeeperBody Body(string id)
        {
            foreach (KeeperBody b in bodies)
                if (b != null && b.id == id) return b;
            return bodies.Count > 0 ? bodies[0] : null;
        }

        /// <summary>The ramps a body's channel of this kind can take, "as drawn" (null) first.</summary>
        public List<PaletteRamp> Choices(string kind)
        {
            var list = new List<PaletteRamp> { null };
            if (palettes != null) list.AddRange(palettes.For(kind));
            return list;
        }
    }

    /// <summary>The keeper's profile rules (pure): names, and stepping through choices.</summary>
    public static class KeeperRules
    {
        public const int MaxNameLength = 16;

        /// <summary>Letters (any script Silver draws, by Unicode category), spaces, apostrophes and hyphens.</summary>
        public static bool IsNameCharacter(char c) => char.IsLetter(c) || c == ' ' || c == '\'' || c == '-';

        /// <summary>The name as kept: allowed characters only, single spaces, trimmed, at most <see cref="MaxNameLength"/>; empty becomes Bram.</summary>
        public static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return Story.PlayerProfile.DefaultName;
            var text = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (!IsNameCharacter(c)) continue;
                if (c == ' ' && (text.Length == 0 || text[text.Length - 1] == ' ')) continue;
                if (text.Length >= MaxNameLength) break;
                text.Append(c);
            }
            string cleaned = text.ToString().Trim();
            return cleaned.Length == 0 ? Story.PlayerProfile.DefaultName : cleaned;
        }

        /// <summary>The choice <paramref name="step"/> places on from <paramref name="current"/> in a list, wrapping round.</summary>
        public static int Step(int current, int step, int count) => count <= 0 ? 0 : ((current + step) % count + count) % count;

        /// <summary>The palette a body can actually use: only choices for its own channels' kinds.</summary>
        public static string ForBody(string palette, KeeperBody body) =>
            body == null ? string.Empty : FurniturePalette.Restrict(palette, body.channels);
    }
}
