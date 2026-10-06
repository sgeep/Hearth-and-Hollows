using Hearthdelve.Shared.Story;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Shared.Characters
{
    /// <summary>
    /// One person in the game (4g): their stable id, name, portrait, the conversation they open, and how they judge the player.
    /// The same definition serves staff, named villagers, story characters and, later, residents; generated Visitors are
    /// runtime <see cref="CharacterRef"/>s that never get one. The Dialogue System actor, the Love/Hate faction and the quest
    /// giver are all found by <see cref="id"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Character", fileName = "Character_")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Stable id (CharacterIds): saves, dialogue actors, relationships, quests. Never changed once shipped.")]
        public string id;
        public CharacterKind kind = CharacterKind.Staff;
        public LocalizedString displayName;
        [Tooltip("Their dialogue portrait (none for the player).")]
        public PortraitDefinition portrait;
        [Tooltip("The conversation (its Dialogue System title) that talking to them opens. Empty: nothing to say yet.")]
        public string conversation;

        [Header("Relationship (Love/Hate)")]
        [Tooltip("Their opinion of the player is tracked: affinity, respect and remembered deeds.")]
        public bool tracked;
        [Tooltip("What they value: deeds that show the same are respected more.")]
        public SocialTraits values;
        [Range(-100, 100), Tooltip("Where they start: how much they like the player.")]
        public float affinityToPlayer;
        [Range(-100, 100), Tooltip("Where they start: how much they respect the player.")]
        public float respectForPlayer;
        [Range(-100, 100), Tooltip("How much they care for the tavern: deeds that help it please them this much.")]
        public float affinityToTavern = 50f;
        [Range(-100, 100), Tooltip("How much they care for Kariaston.")]
        public float affinityToVillage = 30f;

        public bool HasConversation => !string.IsNullOrEmpty(conversation);
    }
}
