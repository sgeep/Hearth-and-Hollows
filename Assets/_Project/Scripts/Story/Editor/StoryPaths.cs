using Hearthdelve.Editor;

namespace Hearthdelve.Story.Editor
{
    /// <summary>Where the story's content lives (4g).</summary>
    public static class StoryPaths
    {
        public const string Root = EditorPaths.Data + "/Story";
        public const string Characters = Root + "/Characters";
        public const string Deeds = Root + "/Deeds";
        public const string Portraits = Root + "/Portraits";
        public const string Quests = Root + "/Quests";
        public const string Database = Root + "/StoryDatabase.asset";
        public const string Dialogue = Root + "/HearthDialogue.asset";
        public const string Factions = Root + "/HearthFactions.asset";
        public const string QuestDatabase = Root + "/HearthQuests.asset";
        /// <summary>The portraits' imported frames (from Tools/portraits/derived).</summary>
        public const string PortraitArt = "Assets/ThirdParty/Minifantasy/Portraits";
    }
}
