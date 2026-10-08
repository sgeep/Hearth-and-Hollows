using System.Collections.Generic;
using System.Linq;

namespace Hearthdelve.UI.Localization
{
    /// <summary>
    /// The credits screen's text (4i-C): the Credits table, its English authored here and checked against docs/CREDITS.md
    /// (<c>CreditsTests</c>). Each line is a heading or an entry, with the number of 12-pixel lines its box gives it.
    /// </summary>
    public static class CreditsLocKeys
    {
        public enum Kind
        {
            /// <summary>The game's name at the top (2×).</summary>
            Title,
            /// <summary>A section (1×, the title colour).</summary>
            Heading,
            /// <summary>An ordinary line.</summary>
            Line,
            /// <summary>The music's link: a selectable line that opens <see cref="HeatleyBrosUrl"/>.</summary>
            Link,
            /// <summary>Space between sections.</summary>
            Gap,
        }

        /// <summary>The working link HeatleyBros' licence requires in-game (their channel, per docs/CREDITS.md).</summary>
        public const string HeatleyBrosUrl = "https://www.youtube.com/c/heatleybros";

        public const string ScreenTitle = "credits.title", Back = "credits.back", BackPad = "credits.back_pad", Link = "credits.music.link";

        /// <summary>Top to bottom: kind, key, English, lines of box.</summary>
        public static readonly (Kind kind, string key, string english, int lines)[] Lines =
        {
            (Kind.Title, "credits.game", "Hearth & Hollows", 2),
            (Kind.Gap, null, null, 1),
            (Kind.Heading, "credits.art.heading", "art", 1),
            (Kind.Line, "credits.art.minifantasy", "Minifantasy by Krishna Palacio", 1),
            (Kind.Line, "credits.art.portraits", "portraits made with the Minifantasy Portrait Generator (art by Krishna Palacio, app by Pixel_Pincher)", 2),
            (Kind.Gap, null, null, 1),
            (Kind.Heading, "credits.music.heading", "music", 1),
            (Kind.Line, "credits.music.heatleybros", "music by HeatleyBros: \"Quirkii\", \"Continue\", \"Coastal Market\" and \"Otherworld\" from HeatleyBros V", 2),
            (Kind.Link, Link, "youtube.com/c/heatleybros", 1),
            (Kind.Gap, null, null, 1),
            (Kind.Heading, "credits.font.heading", "font", 1),
            (Kind.Line, "credits.font.silver", "Silver by Poppy Works (poppyworks.itch.io/silver), CC BY 4.0, with Itou Hiro (PixelMplus), leedheo (DOSGothic) and ぶち; punctuation adapted for Hearth & Hollows", 3),
            (Kind.Gap, null, null, 1),
            (Kind.Heading, "credits.tools.heading", "tools", 1),
            (Kind.Line, "credits.tools.moremountains", "TopDown Engine, MMFeedbacks and Nice Vibrations by More Mountains", 2),
            (Kind.Line, "credits.tools.stm", "Super Text Mesh by Kai Clavier", 1),
            (Kind.Line, "credits.tools.pixelcrushers", "Dialogue System for Unity, Quest Machine and Love/Hate by Pixel Crushers", 2),
            (Kind.Line, "credits.tools.unity", "made with Unity", 1),
            (Kind.Gap, null, null, 1),
            (Kind.Line, "credits.thanks", "thank you for playing", 1),
        };

        public static IEnumerable<(string key, string english)> English =>
            Lines.Where(l => l.key != null).Select(l => (l.key, l.english))
                .Concat(new[] { (ScreenTitle, "credits"), (Back, "up / down: scroll · Enter: the link · Esc: back"), (BackPad, "stick: scroll · A: the link · B: back") });
    }
}
