namespace Hearthdelve.UI.Typography
{
    /// <summary>
    /// What a piece of player-facing text is, for its size and line (<see cref="TypeScale"/>). Every text in the game is one of
    /// these. Silver is pixel-clean only at whole multiples of its native size on the 320×180 grid, so the scale has three sizes
    /// (Display 3×, Heading 2×, and 1× for the rest); the 1× roles stay apart by colour, alignment and their reserved columns,
    /// and are named so each can be tuned on its own later (prompts for controller distance, say) from one place.
    /// </summary>
    public enum TextStyle
    {
        /// <summary>Primary readable information: rows, names, values, descriptions; dialogue in 4g.</summary>
        Body = 0,
        /// <summary>Supporting information: labels, counts, sources, notes and hints. Never the only place something important is said.</summary>
        Secondary = 1,
        /// <summary>What the player presses: button labels and control lines.</summary>
        Prompt = 2,
        /// <summary>A screen's title and major state titles (the evening's results, a delve's end).</summary>
        Heading = 3,
        /// <summary>The game's name on the title screen.</summary>
        Display = 4,
    }
}
