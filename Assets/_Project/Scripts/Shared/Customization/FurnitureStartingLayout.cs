using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>One area's pieces, by area id.</summary>
    [Serializable]
    public sealed class AreaLayoutData
    {
        public string area;
        public List<PlacedFurniture> pieces = new();
    }

    /// <summary>Copies owned beyond those placed (in storage from the start).</summary>
    [Serializable]
    public sealed class OwnedFurnitureData
    {
        public string definition;
        [Min(0)] public int count;
    }

    /// <summary>
    /// What a new game (and an old save meeting furniture for the first time) starts with: the starting layout of each
    /// area, owned as Starter pieces, plus any extra copies in storage. The tavern's is the 4e room (4f step 1).
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Furniture Starting Layout", fileName = "FurnitureStartingLayout")]
    public sealed class FurnitureStartingLayout : ScriptableObject
    {
        public List<AreaLayoutData> areas = new();
        public List<OwnedFurnitureData> storage = new();

        public IReadOnlyList<PlacedFurniture> Layout(string area)
        {
            foreach (AreaLayoutData a in areas)
                if (a != null && a.area == area) return a.pieces;
            return Array.Empty<PlacedFurniture>();
        }
    }
}
