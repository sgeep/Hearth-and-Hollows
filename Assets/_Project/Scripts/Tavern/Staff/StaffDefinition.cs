using Hearthdelve.Shared.Characters;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Tavern.Staff
{
    public enum StaffStation
    {
        None,
        Grill,
        Tap,
        Serving,
        StewPot,
    }

    /// <summary>The canonical staff's stable ids (4f Checkpoint C): what saves, events and 4g's dialogue use (<see cref="CharacterIds"/>).</summary>
    public static class StaffIds
    {
        public const string Pip = CharacterIds.Pip;
        /// <summary>Boog, the cook: his stable id is still <c>gunta</c> (renamed 2026-10-06; ids never change).</summary>
        public const string Boog = CharacterIds.Boog;
    }

    /// <summary>A hired helper who can run one station on their own (GDD §6.2, §7.2).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Staff Definition", fileName = "Staff_")]
    public sealed class StaffDefinition : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        [Range(0, 1), Tooltip("Drives the auto-player's timing accuracy: 1 plays like an expert.")]
        public float skill = 0.6f;
        [Range(0, 1), Tooltip("Hard cap on staff results, so staffed stations are always worse than good play.")]
        public float qualityCap = 0.85f;
        [Min(0), Tooltip("Pause between jobs, in seconds.")]
        public float restBetweenJobs = 1f;
        public Color placeholderColor = new(0.6f, 0.75f, 0.95f);
        [Tooltip("Who they are (4g): their name, portrait, conversation and relationship. Same id as the staff member.")]
        public CharacterDefinition character;
    }
}
