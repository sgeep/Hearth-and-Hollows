using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>Every palette ramp and preset (one asset, in the game database).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Palette Library", fileName = "PaletteLibrary")]
    public sealed class PaletteLibrary : ScriptableObject
    {
        public List<PaletteRamp> ramps = new();
        public List<PalettePreset> presets = new();

        public PaletteRamp Ramp(string id) => string.IsNullOrEmpty(id) ? null : ramps.Find(r => r != null && r.id == id);

        /// <summary>The ramps a channel of this kind can take, in library order.</summary>
        public List<PaletteRamp> For(string kind) => ramps.FindAll(r => r != null && r.kind == kind);

        /// <summary>The presets that change at least one of these channels.</summary>
        public List<PalettePreset> PresetsFor(IReadOnlyList<PaletteChannel> channels)
        {
            var found = new List<PalettePreset>();
            foreach (PalettePreset p in presets)
                if (p != null && p.picks.Exists(pick => Has(channels, pick.kind))) found.Add(p);
            return found;
        }

        static bool Has(IReadOnlyList<PaletteChannel> channels, string kind)
        {
            foreach (PaletteChannel c in channels)
                if (c != null && c.kind == kind) return true;
            return false;
        }
    }
}
