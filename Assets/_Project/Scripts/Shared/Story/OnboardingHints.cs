namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// The first delve's prompts (4g Checkpoint B): only what's needed to survive and get home, each shown once, when it first
    /// matters. Optimisation (clean-kill maths, overkill, the Butcher Block, powers) is left for the player to discover.
    /// </summary>
    public static class OnboardingHints
    {
        public const string Move = "move";
        public const string Fight = "fight";
        public const string Essence = "essence";
        public const string Harvest = "harvest";
        public const string Finisher = "finisher";
        public const string Extract = "extract";

        public static readonly string[] All = { Move, Fight, Essence, Harvest, Finisher, Extract };
    }
}
