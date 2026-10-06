namespace Hearthdelve.UI.Localization
{
    /// <summary>UI-table keys for the character creator (4g Checkpoint B). The bodies' and colourways' names come from the keeper looks.</summary>
    public static class CreatorLocKeys
    {
        public const string Title = "creator.title";
        public const string Name = "creator.name";
        public const string Body = "creator.body";
        public const string Skin = "creator.skin";
        public const string Hair = "creator.hair";
        public const string Outfit = "creator.outfit";
        public const string Hint = "creator.hint";
        public const string Begin = "creator.begin";
        public const string Back = "creator.back";
        public const string Left = "creator.left";
        public const string Right = "creator.right";
        public const string NameTitle = "creator.name_title";
        public const string NameHint = "creator.name_hint";
        public const string Shift = "creator.shift";
        public const string Space = "creator.space";
        public const string Delete = "creator.delete";
        public const string Done = "creator.done";

        public static readonly (string key, string english)[] English =
        {
            (Title, "a new keeper"),
            (Name, "name"),
            (Body, "look"),
            (Skin, "skin"),
            (Hair, "hair"),
            (Outfit, "clothes"),
            (Hint, "left and right change a row"),
            (Begin, "begin"),
            (Back, "back"),
            (Left, "<"),
            (Right, ">"),
            (NameTitle, "what are you called?"),
            (NameHint, "type, or choose letters"),
            (Shift, "shift"),
            (Space, "space"),
            (Delete, "delete"),
            (Done, "done"),
        };
    }
}
