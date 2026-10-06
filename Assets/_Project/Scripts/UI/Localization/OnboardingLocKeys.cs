namespace Hearthdelve.UI.Localization
{
    /// <summary>UI-table keys for the first delve's prompts (4g Checkpoint B). Control labels come from the bindings.</summary>
    public static class OnboardingLocKeys
    {
        public const string Move = "onboarding.move";
        public const string MovePad = "onboarding.move_pad";
        public const string Fight = "onboarding.fight";
        public const string Essence = "onboarding.essence";
        public const string Harvest = "onboarding.harvest";
        public const string Finisher = "onboarding.finisher";
        public const string Extract = "onboarding.extract";

        public static readonly (string key, string english)[] English =
        {
            (Move, "move with WASD. you face the mouse."),
            (MovePad, "move with the left stick. aim with the right."),
            (Fight, "{0} attacks. {1} rolls: nothing touches you mid-roll."),
            (Essence, "that was Essence: your life down here. it drains as you go and when you're hit. at zero you're dragged home without the haul."),
            (Harvest, "what they leave goes in your satchel. a clean kill leaves more."),
            (Finisher, "the drumstick means it's ready: {0} finishes it for the best parts."),
            (Extract, "the rope leads home. leave whenever you like: what you carry comes with you."),
        };
    }
}
