using Hearthdelve.Shared.Game;
using UnityEngine;

namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// Keeps the story middleware's state in step with the game's (4g): the story adapters record their state into
    /// <see cref="GameState.Story"/> before every save, put it back after Continue, and start afresh for a new game or the menu.
    /// <see cref="SaveSystem"/> stays the only save: nothing else writes to disk.
    /// </summary>
    public interface IStoryStateParticipant
    {
        /// <summary>Records the middleware's state into <paramref name="state"/>, just before it's saved.</summary>
        void Capture(GameState state);
        /// <summary>Puts a loaded game's story back (after Continue), before its scene loads.</summary>
        void Restore(GameState state);
        /// <summary>Forgets everything: a new game, or back to the menu.</summary>
        void Clear();
    }

    /// <summary>What gameplay may ask of the story without knowing its middleware: can this person be talked to, and talk to them.</summary>
    public interface IConversationService
    {
        bool IsTalking { get; }
        /// <summary>Whether talking to <paramref name="characterId"/> opens a conversation now.</summary>
        bool CanTalk(string characterId);
        /// <summary>Opens their conversation. False if there's none, or one is already open.</summary>
        bool Talk(string characterId);
        /// <summary>Whether a conversation with this title exists (4h: things to look at, small world moments).</summary>
        bool HasConversation(string title);
        /// <summary>Plays a conversation by its title (4h). False if there's none, or one is already open.</summary>
        bool Play(string title);
    }

    /// <summary>
    /// Where the story layer registers its services (4g). Gameplay assemblies reach the story only through these interfaces and
    /// the facts they publish, never through Pixel Crushers types; with no story layer loaded, both are null and nothing happens.
    /// </summary>
    public static class StoryServices
    {
        public static IStoryStateParticipant State { get; private set; }
        public static IConversationService Conversations { get; private set; }

        public static void Register(IStoryStateParticipant state, IConversationService conversations)
        {
            State = state;
            Conversations = conversations;
        }

        public static void Unregister(IStoryStateParticipant state)
        {
            if (State != state) return;
            State = null;
            Conversations = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            State = null;
            Conversations = null;
        }
    }
}
