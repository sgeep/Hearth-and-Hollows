using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Garden
{
    /// <summary>The garden's rules switches (4h Checkpoint B).</summary>
    [Serializable]
    public struct GardenSettings
    {
        [Tooltip("Experiment, off by default: a crop not tended the day before doesn't grow that night.")]
        public bool untendedPausesGrowth;

        public static GardenSettings Default => new() { untendedPausesGrowth = false };
    }

    /// <summary>The starter garden (4h Checkpoint B): its beds' stable ids and its switches.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Garden/Garden Config", fileName = "GardenConfig")]
    public sealed class GardenConfig : ScriptableObject
    {
        public const string Bed1 = "garden_1", Bed2 = "garden_2", Bed3 = "garden_3", Bed4 = "garden_4";

        [Tooltip("The beds' stable ids (saved). More beds later are more ids.")]
        public List<string> bedIds = new() { Bed1, Bed2, Bed3, Bed4 };
        public GardenSettings settings = GardenSettings.Default;
    }
}
