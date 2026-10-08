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
    /// Overheard exchanges (4h Checkpoint D): a short authored conversation between characters, shown line by line over the
    /// speakers' heads (<see cref="Speakers"/>), never interactive, never holding the clock. One at a time; it gives way to a
    /// conversation, a menu or a held moment (Decorate Mode, a panel) at once.
    /// </summary>
    public interface IBarkService
    {
        bool IsBarking { get; }
        /// <summary>Plays the exchange titled <paramref name="title"/>; false if none is playing and it had nothing to say now.</summary>
        bool Play(string title);
        void Stop();
    }

    /// <summary>
    /// Who is standing where, by stable character id (4h Checkpoint D): villagers and staff register themselves while they're
    /// about, so an overheard line can be shown over the right head without the story knowing any scene.
    /// </summary>
    public static class Speakers
    {
        static readonly System.Collections.Generic.Dictionary<string, UnityEngine.Transform> s_All = new();

        public static void Set(string id, UnityEngine.Transform at)
        {
            if (!string.IsNullOrEmpty(id) && at != null) s_All[id] = at;
        }

        public static void Remove(string id, UnityEngine.Transform at)
        {
            if (id != null && s_All.TryGetValue(id, out UnityEngine.Transform t) && t == at) s_All.Remove(id);
        }

        public static UnityEngine.Transform Find(string id) => id != null && s_All.TryGetValue(id, out UnityEngine.Transform t) && t != null ? t : null;

        /// <summary>Tests.</summary>
        public static void Clear() => s_All.Clear();
    }

    /// <summary>
    /// Where the story layer registers its services (4g). Gameplay assemblies reach the story only through these interfaces and
    /// the facts they publish, never through Pixel Crushers types; with no story layer loaded, both are null and nothing happens.
    /// </summary>
    public static class StoryServices
    {
        public static IStoryStateParticipant State { get; private set; }
        public static IConversationService Conversations { get; private set; }
        /// <summary>4h Checkpoint D: short overheard exchanges between characters (null with no story layer).</summary>
        public static IBarkService Barks { get; private set; }

        public static void RegisterBarks(IBarkService barks) => Barks = barks;

        public static void UnregisterBarks(IBarkService barks)
        {
            if (Barks == barks) Barks = null;
        }

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
            Barks = null;
        }
    }
}
