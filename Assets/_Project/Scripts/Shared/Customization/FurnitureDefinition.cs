using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// One catalogue piece (GDD §6.6; 4f plan §2): how it looks and stands in each orientation, how it turns (D2) and
    /// flips (D3), what it does, and how it's owned and sold. Placed copies are <see cref="PlacedFurniture"/>.
    /// Registering many pieces is data: definitions are generated, not hand-coded per chair.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Furniture Definition", fileName = "Furniture_")]
    public sealed class FurnitureDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by saves and layouts. Never change after shipping.")]
        public string id;
        [Tooltip("Localization key (UI table) of its name.")]
        public string nameKey;
        [Tooltip("Localization key (UI table) of its catalogue description.")]
        public string descriptionKey;
        public FurnitureCategory category;
        public FurnitureTheme theme;
        public FurnitureLayer layer = FurnitureLayer.Standing;
        public RotationMode rotation = RotationMode.None;
        [Tooltip("D3: on only where the mirrored art still reads correctly.")]
        public bool flippable;
        [Tooltip("Only stands against the back wall (the kitchen range).")]
        public bool wallBound;

        [Header("Function")]
        public FurnitureFunction function;
        public StationKind station;
        [Tooltip("Localization key (UI table) of the hint name, for stations and the pass.")]
        public string useNameKey;
        [Min(0.1f), Tooltip("How near the user's feet must be to a use point, in tiles.")]
        public float reach = 1f;

        [Header("Ownership")]
        [Min(0), Tooltip("Gold; 0 = not for sale.")]
        public int price;
        [Tooltip("At most one can be owned (boss trophies, some discoveries).")]
        public bool unique;
        [Min(0), Tooltip("The Renown catalogue tier that unlocks it (D14).")]
        public int catalogTier;
        public FurnitureSource sources = FurnitureSource.Starter;
        [Range(0f, 1f), Tooltip("Share of the price a bought copy sells back for (D12: half). Discoveries, trophies and uniques never sell.")]
        public float sellBack = 0.5f;

        [Header("Looks")]
        [Tooltip("Authored colourways (D11): Minifantasy's own variants of the drawing. Empty: one look. The first is the default.")]
        public List<FurnitureVariant> variants = new();
        [Tooltip("Colour channels that can be remapped to palette ramps (D11): wood, cushion, pot…")]
        public List<PaletteChannel> paletteChannels = new();

        [Header("Orientations")]
        [Tooltip("None and QuarterTurnSprite: one facing (unrotated). AuthoredFacings: one per drawn quarter turn.")]
        public List<FurnitureFacing> facings = new();

        public bool BlocksMovement => layer == FurnitureLayer.Standing;

        /// <summary>For sale in the catalogue (a price, and copies can be bought).</summary>
        public bool ForSale => price > 0 && (sources & FurnitureSource.Bought) != 0;

        /// <summary>D12: bought pieces sell back; discoveries, trophies and uniques don't.</summary>
        public bool CanSell => ForSale && !unique && sellBack > 0f;

        public int SellPrice => CanSell ? Mathf.Max(1, Mathf.FloorToInt(price * sellBack)) : 0;

        /// <summary>Whether it can be recoloured or has drawn colourways (the colour panel opens for it).</summary>
        public bool HasLooks => variants.Count > 1 || paletteChannels.Count > 0;

        /// <summary>The index of a variant id (0, the default, for null, empty or unknown).</summary>
        public int VariantIndex(string variant)
        {
            if (string.IsNullOrEmpty(variant)) return 0;
            int i = variants.FindIndex(v => v != null && v.id == variant);
            return i < 0 ? 0 : i;
        }

        /// <summary>The drawing shown in the catalogue: the first art layer of its first facing, in a variant.</summary>
        public Sprite Icon(int variant = 0)
        {
            if (facings.Count == 0 || facings[0] == null) return null;
            foreach (FurnitureArt art in facings[0].art)
            {
                Sprite s = art?.SpriteFor(variant);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>The quarter turns this piece can stand at, in order.</summary>
        public IEnumerable<int> AllowedTurns()
        {
            switch (rotation)
            {
                case RotationMode.QuarterTurnSprite:
                    if (facings.Count > 0) for (int t = 0; t < 4; t++) yield return t;
                    break;
                case RotationMode.AuthoredFacings:
                    for (int t = 0; t < 4; t++)
                        if (facings.Exists(f => f != null && f.turns == t)) yield return t;
                    break;
                default:
                    if (facings.Count > 0) yield return 0;
                    break;
            }
        }

        /// <summary>
        /// The drawing to use at <paramref name="turns"/>, and how far it must still be turned: an authored facing is
        /// already drawn turned (0 left), a quarter-turn piece turns its one drawing. False when it can't stand that way.
        /// </summary>
        public bool TryGetFacing(int turns, out FurnitureFacing facing, out int rotate)
        {
            turns = ((turns % 4) + 4) % 4;
            facing = null;
            rotate = 0;
            if (facings.Count == 0) return false;
            switch (rotation)
            {
                case RotationMode.QuarterTurnSprite:
                    facing = facings[0];
                    rotate = turns;
                    return facing != null;
                case RotationMode.AuthoredFacings:
                    facing = facings.Find(f => f != null && f.turns == turns);
                    return facing != null;
                default:
                    facing = turns == 0 ? facings[0] : null;
                    return facing != null;
            }
        }

        /// <summary>The next orientation after <paramref name="turns"/> (Decorate Mode's rotate), or the same if it doesn't turn.</summary>
        public int NextTurns(int turns)
        {
            var allowed = new List<int>(AllowedTurns());
            if (allowed.Count == 0) return turns;
            int at = allowed.IndexOf(turns);
            return allowed[(at + 1) % allowed.Count];
        }
    }
}
