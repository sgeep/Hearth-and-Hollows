namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// Conversations the surface plays by title (4h Checkpoint A): things to look at (one line each, written in the Dialogue
    /// System's node editor like everything else) and small world moments. Gameplay names them; the graph owns the words.
    /// </summary>
    public static class SurfaceConversations
    {
        /// <summary>Conversations whose title starts with this are looked-at things: the box shows no speaker's name.</summary>
        public const string InspectPrefix = "Inspect/";

        public const string PhiPortrait = InspectPrefix + "PhiPortrait";
        public const string Tankards = InspectPrefix + "Tankards";
        public const string Hatch = InspectPrefix + "Hatch";
        public const string Memorial = InspectPrefix + "Memorial";

        /// <summary>Five o'clock (4h): Orik notices the village winding down, once a day, if the keeper is inside Tally Ho!.</summary>
        public const string OrikFive = "Orik/Five";

        public static bool IsInspect(string title) => title != null && title.StartsWith(InspectPrefix);
    }
}
