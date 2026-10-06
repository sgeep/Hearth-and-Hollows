using System.Collections.Generic;

namespace Hearthdelve.Shared.Characters
{
    /// <summary>A reference to anyone: an authored character (with a definition) or a generated Visitor (id only, transient).</summary>
    public readonly struct CharacterRef
    {
        public readonly string Id;
        public readonly CharacterKind Kind;
        /// <summary>Null for a transient Visitor.</summary>
        public readonly CharacterDefinition Definition;

        public CharacterRef(string id, CharacterKind kind, CharacterDefinition definition)
        {
            Id = id;
            Kind = kind;
            Definition = definition;
        }

        public bool IsValid => !string.IsNullOrEmpty(Id);
        /// <summary>Saved and remembered: everyone but a transient Visitor.</summary>
        public bool IsPersistent => IsValid && Kind != CharacterKind.Visitor;
    }

    /// <summary>
    /// Resolves character ids the same way for every kind of person (4g), so dialogue, quests and relationships take one kind of
    /// reference: authored characters by their definition, Visitors by their transient id. Promoted Visitors and residents
    /// (Phase 5) are added at runtime with <see cref="Add"/>, from their saved records.
    /// </summary>
    public sealed class CharacterDirectory
    {
        readonly Dictionary<string, CharacterDefinition> m_ById = new();

        public CharacterDirectory(IEnumerable<CharacterDefinition> characters)
        {
            if (characters == null) return;
            foreach (CharacterDefinition c in characters) Add(c);
        }

        public IEnumerable<CharacterDefinition> All => m_ById.Values;

        /// <summary>Adds an authored or promoted character. False for a missing, invalid or duplicate id.</summary>
        public bool Add(CharacterDefinition character)
        {
            if (character == null || !CharacterIds.IsAuthored(character.id) || m_ById.ContainsKey(character.id)) return false;
            m_ById.Add(character.id, character);
            return true;
        }

        public CharacterDefinition Definition(string id) => id != null && m_ById.TryGetValue(id, out CharacterDefinition c) ? c : null;

        public CharacterRef Resolve(string id)
        {
            CharacterDefinition definition = Definition(id);
            if (definition != null) return new CharacterRef(id, definition.kind, definition);
            return CharacterIds.IsVisitor(id) ? new CharacterRef(id, CharacterKind.Visitor, null) : default;
        }

        /// <summary>Everyone whose opinion of the player is tracked.</summary>
        public IEnumerable<CharacterDefinition> Tracked()
        {
            foreach (CharacterDefinition c in m_ById.Values)
                if (c.tracked) yield return c;
        }
    }
}
