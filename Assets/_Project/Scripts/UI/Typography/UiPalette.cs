using UnityEngine;

namespace Hearthdelve.UI.Typography
{
    /// <summary>
    /// The text colours used on parchment (4i-B's contrast audit): each keeps its hue but is dark enough to reach 4.5:1 on the
    /// parchment panel, the lighter button face and the tan dish cards (<c>ContrastTests</c>). The generators and the screens that
    /// colour text at runtime both read them from here; <see cref="Audited"/> maps the tones they replaced.
    /// </summary>
    public static class UiPalette
    {
        /// <summary>Main text (unchanged: it already passed).</summary>
        public static readonly Color Ink = new(0.25f, 0.16f, 0.1f);
        /// <summary>Titles and things that are wrong or blocked.</summary>
        public static readonly Color Title = new(0.48f, 0.13f, 0.09f);
        /// <summary>Secondary lines: labels, hints, details.</summary>
        public static readonly Color Label = new(0.32f, 0.24f, 0.18f);
        /// <summary>Gold, prices, the thing to notice.</summary>
        public static readonly Color Accent = new(0.36f, 0.23f, 0.01f);
        /// <summary>Discoveries and what's new.</summary>
        public static readonly Color Discovery = new(0.09f, 0.26f, 0.43f);
        /// <summary>Notes under a line.</summary>
        public static readonly Color Note = new(0.35f, 0.23f, 0.15f);
        /// <summary>Done, saved, all right.</summary>
        public static readonly Color Good = new(0.16f, 0.28f, 0.13f);
        /// <summary>A warning: too little gold, a missing piece.</summary>
        public static readonly Color Warning = new(0.46f, 0.15f, 0.09f);

        /// <summary>The tones before the audit, and what each became (the in-place recolour uses it; tests check it's complete).</summary>
        public static readonly (Color from, Color to)[] Audited =
        {
            (new Color(0.55f, 0.15f, 0.1f), Title),
            (new Color(0.6f, 0.15f, 0.1f), Title),
            (new Color(0.5f, 0.37f, 0.27f), Label),
            (new Color(0.55f, 0.35f, 0.02f), Accent),
            (new Color(0.12f, 0.33f, 0.55f), Discovery),
            (new Color(0.45f, 0.3f, 0.2f), Note),
            (new Color(0.25f, 0.45f, 0.2f), Good),
            (new Color(0.62f, 0.2f, 0.12f), Warning),
        };

        /// <summary>The audited tone for <paramref name="c"/> if it was one of the old ones (alpha kept); otherwise <paramref name="c"/>.</summary>
        public static Color Audit(Color c)
        {
            foreach ((Color from, Color to) in Audited)
                if (Mathf.Abs(c.r - from.r) < 0.004f && Mathf.Abs(c.g - from.g) < 0.004f && Mathf.Abs(c.b - from.b) < 0.004f)
                    return new Color(to.r, to.g, to.b, c.a);
            return c;
        }
    }
}
