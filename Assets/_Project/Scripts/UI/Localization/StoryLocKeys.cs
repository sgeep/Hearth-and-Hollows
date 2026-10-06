namespace Hearthdelve.UI.Localization
{
    /// <summary>UI-table keys for the story (4g): the dialogue box's marks and quest text. Lines of dialogue are in the Dialogue table.</summary>
    public static class StoryLocKeys
    {
        /// <summary>The line is all there: the next press moves on.</summary>
        public const string Continue = "dialogue.continue";
        /// <summary>Beside the chosen response.</summary>
        public const string Pointer = "dialogue.pointer";
        /// <summary>Boog's Bomb (4g Checkpoint B): the quest's title in the journal.</summary>
        public const string BoogsBombTitle = "quest.boogs_bomb.title";

        public static readonly (string key, string english)[] English =
        {
            (Continue, "▼"),
            // U+2023 (‣), not ▶: Super Text Mesh draws U+25B6 as an emoji, which Silver hasn't got (TextStyleTests checks).
            (Pointer, "‣"),
            (BoogsBombTitle, "Boog's bomb"),
        };
    }
}
