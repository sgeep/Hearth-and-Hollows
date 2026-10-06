using Hearthdelve.Shared.Characters;
using PixelCrushers.DialogueSystem;
using UnityEngine;

namespace Hearthdelve.Story.Dialogue
{
    /// <summary>
    /// Hearth &amp; Hollows' only door to the Dialogue System (4g). The Dialogue System owns conversations, branching, conversation
    /// variables and who speaks when; this starts a character's conversation, maps its actors to stable character ids (each actor's
    /// <see cref="CharacterIdField"/>), and records its variables into the game's save. Conversations ask about the game only
    /// through the <c>HH_</c> functions (<see cref="StoryLua"/>), never through Love/Hate's or Quest Machine's own Lua.
    /// </summary>
    public sealed class DialogueAdapter
    {
        /// <summary>The actor field holding the character id the actor speaks for ("gunta" for Boog).</summary>
        public const string CharacterIdField = "Character Id";
        /// <summary>The entry field whose value keys the line in the Dialogue string table (the Localization bridge's convention).</summary>
        public const string GuidField = "Guid";
        /// <summary>The Dialogue System actor every conversation's player lines belong to.</summary>
        public const string PlayerActor = "Player";

        readonly CharacterDirectory m_Cast;

        public DialogueAdapter(CharacterDirectory cast) => m_Cast = cast;

        public bool IsTalking => DialogueManager.isConversationActive;

        public bool CanTalk(string characterId)
        {
            CharacterDefinition c = m_Cast.Definition(characterId);
            return c != null && c.HasConversation && DialogueManager.hasInstance && DialogueManager.masterDatabase != null
                   && DialogueManager.masterDatabase.GetConversation(c.conversation) != null;
        }

        public bool Talk(string characterId)
        {
            if (IsTalking || !CanTalk(characterId)) return false;
            DialogueManager.StartConversation(m_Cast.Definition(characterId).conversation);
            return DialogueManager.isConversationActive;
        }

        System.Action m_Ended;

        /// <summary>
        /// Plays a conversation by its title (the opening's beats, 4g Checkpoint B) and calls <paramref name="ended"/> when it ends,
        /// however it ends. False if there's no such conversation or one is already open.
        /// </summary>
        public bool Play(string title, System.Action ended = null)
        {
            if (IsTalking || !DialogueManager.hasInstance || DialogueManager.masterDatabase == null || DialogueManager.masterDatabase.GetConversation(title) == null)
                return false;
            m_Ended = ended;
            DialogueManager.instance.conversationEnded -= OnEnded;
            DialogueManager.instance.conversationEnded += OnEnded;
            DialogueManager.StartConversation(title);
            if (DialogueManager.isConversationActive) return true;
            DialogueManager.instance.conversationEnded -= OnEnded;
            m_Ended = null;
            return false;
        }

        void OnEnded(Transform actor)
        {
            if (DialogueManager.hasInstance) DialogueManager.instance.conversationEnded -= OnEnded;
            System.Action ended = m_Ended;
            m_Ended = null;
            ended?.Invoke();
        }

        /// <summary>The character an actor speaks for, from its <see cref="CharacterIdField"/>.</summary>
        public static string CharacterId(Actor actor)
        {
            if (actor == null) return null;
            string id = actor.LookupValue(CharacterIdField);
            return string.IsNullOrEmpty(id) ? null : id;
        }

        /// <summary>The player's name, for the player actor's lines and <c>HH_PlayerName()</c>.</summary>
        public void SetPlayerName(string name)
        {
            if (!DialogueManager.hasInstance) return;
            DialogueLua.SetActorField(PlayerActor, "Display Name", name);
        }

        /// <summary>The Dialogue System's variables and conversation state, as it records them.</summary>
        public string Record() => DialogueManager.hasInstance ? PersistentDataManager.GetSaveData() : string.Empty;

        public void Apply(string data)
        {
            Clear();
            if (!string.IsNullOrEmpty(data)) PersistentDataManager.ApplySaveData(data);
        }

        /// <summary>Every variable back to the database's start: a new game, the menu, or before a load.</summary>
        public void Clear()
        {
            if (!DialogueManager.hasInstance) return;
            // A beat cut short by a new game or the menu doesn't move the story on.
            m_Ended = null;
            DialogueManager.instance.conversationEnded -= OnEnded;
            if (DialogueManager.isConversationActive) DialogueManager.StopAllConversations();
            DialogueManager.ResetDatabase(DatabaseResetOptions.KeepAllLoaded);
        }

        /// <summary>A line for the debug log when the Dialogue System isn't set up.</summary>
        public static void Warn(string message) => Debug.LogWarning($"[Hearthdelve] Dialogue: {message}");
    }
}
