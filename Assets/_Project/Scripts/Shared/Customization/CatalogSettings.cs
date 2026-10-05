using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// The furniture catalogue's tuning (D14; 4f plan §12–13): the Renown that opens each tier. Tier 0 is open from the
    /// start. Renown is earned, never spent (GDD §7.1): it only opens tiers.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Catalog Settings", fileName = "CatalogSettings")]
    public sealed class CatalogSettings : ScriptableObject
    {
        public List<CatalogTier> tiers = new()
        {
            new CatalogTier { renown = 0 },
            new CatalogTier { renown = 25 },
            new CatalogTier { renown = 60 },
            new CatalogTier { renown = 100 },
        };

        public int[] Thresholds()
        {
            var t = new int[tiers.Count];
            for (int i = 0; i < t.Length; i++) t[i] = tiers[i]?.renown ?? 0;
            return t;
        }
    }
}
