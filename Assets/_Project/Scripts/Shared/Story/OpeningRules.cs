namespace Hearthdelve.Shared.Story
{
    /// <summary>One beat of the Act I opening: the conversation played, and where the opening is once it ends.</summary>
    public readonly struct OpeningBeat
    {
        /// <summary>The Dialogue System conversation's title.</summary>
        public readonly string Conversation;
        /// <summary>A beat played once per game (by this id in <see cref="StoryState.SeenHints"/>), or null if it moves the stage on.</summary>
        public readonly string OnceId;
        public readonly OpeningStage After;

        public OpeningBeat(string conversation, string onceId, OpeningStage after)
        {
            Conversation = conversation;
            OnceId = onceId;
            After = after;
        }
    }

    /// <summary>
    /// When the Act I opening's conversations play (4g Checkpoint B; pure, EditMode-tested): each is tied to a stage of the opening and
    /// the part of the day it begins in, so the story moves only on explicit state, never on the day number. The conversations
    /// themselves are authored in the Dialogue System's editor.
    /// </summary>
    public static class OpeningRules
    {
        public const string Arrival = "Act1/Arrival";
        public const string Homecoming = "Act1/Homecoming";
        public const string FirstEvening = "Act1/FirstEvening";
        public const string FirstTakings = "Act1/FirstTakings";

        public static readonly string[] Conversations = { Arrival, Homecoming, FirstEvening, FirstTakings };

        /// <summary>The beat for this stage as the tavern begins <paramref name="tavernPhase"/>, if there's one still to play.</summary>
        public static OpeningBeat? Beat(OpeningStage stage, string tavernPhase, System.Collections.Generic.ICollection<string> seen)
        {
            OpeningBeat? beat = (stage, tavernPhase) switch
            {
                (OpeningStage.Arrival, "Arrival") => new OpeningBeat(Arrival, "beat:arrival", OpeningStage.Arrival),
                (OpeningStage.Homecoming, "Night") => new OpeningBeat(Homecoming, null, OpeningStage.FirstEvening),
                (OpeningStage.FirstEvening, "Prep") => new OpeningBeat(FirstEvening, "beat:first_evening", OpeningStage.FirstEvening),
                (OpeningStage.FirstEvening, "Results") => new OpeningBeat(FirstTakings, null, OpeningStage.Complete),
                _ => null,
            };
            if (beat is { OnceId: not null } b && seen != null && seen.Contains(b.OnceId)) return null;
            return beat;
        }
    }
}
