using System.Collections.Generic;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Story;
using PixelCrushers.DialogueSystem;
using PixelCrushers.LoveHate;
using PixelCrushers.QuestMachine;
using UnityEngine;

namespace Hearthdelve.Story
{
    /// <summary>
    /// The story's content (4g): the cast, the deeds, and the middleware databases they live in. The Dialogue System database is
    /// authored in the Dialogue System's own editor (decision D1); the Love/Hate faction database is built from the cast
    /// (<c>Hearthdelve → Story → Update Story Content</c>), so a character's values and starting feelings are tuned on their
    /// <see cref="CharacterDefinition"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Story Database", fileName = "StoryDatabase")]
    public sealed class StoryDatabase : ScriptableObject
    {
        public List<CharacterDefinition> characters = new();
        public List<DeedDefinition> deeds = new();
        public DialogueDatabase dialogue;
        public QuestDatabase quests;
        public FactionDatabase factions;

        public CharacterDirectory Directory() => new(characters);
    }
}
