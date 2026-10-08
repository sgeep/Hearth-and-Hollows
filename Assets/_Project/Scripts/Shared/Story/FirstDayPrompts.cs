using System.Collections.Generic;
using Hearthdelve.Shared.Game;

namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// The first free day's prompts (4i-A, D3; pure, EditMode-tested): one short line the first time the keeper reaches the garden,
    /// the market and the menu board in the daytime, after arrival day. Each is shown once per game (by its id in
    /// <see cref="StoryState.SeenHints"/>, saved with the day) and never again; doing the thing needs no prompt at all.
    /// </summary>
    public static class FirstDayPrompts
    {
        public const string Garden = "prompt:garden";
        public const string Market = "prompt:market";
        public const string MenuBoard = "prompt:menu_board";

        public static readonly string[] All = { Garden, Market, MenuBoard };

        public static bool Due(string id, bool inGame, DayPhase phase, OpeningStage opening, ICollection<string> seen) =>
            inGame && !string.IsNullOrEmpty(id) && phase == DayPhase.Daytime && opening != OpeningStage.Arrival && (seen == null || !seen.Contains(id));
    }
}
