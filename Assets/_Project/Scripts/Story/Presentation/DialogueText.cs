using Hearthdelve.Story.Dialogue;
using Hearthdelve.UI.Localization;
using PixelCrushers.DialogueSystem;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>
    /// A dialogue line's text in the player's language (4g): the Dialogue table's entry for the line's Guid field (a response's
    /// menu text under <c>&lt;guid&gt;_MenuText</c>, the Localization bridge's convention), with the Dialogue System's markup applied
    /// (<c>[lua(HH_PlayerName())]</c>); the database's own text when the table hasn't got it.
    /// </summary>
    public static class DialogueText
    {
        public const string MenuSuffix = "_MenuText";

        public static string Line(Subtitle subtitle)
        {
            if (subtitle == null) return string.Empty;
            string fallback = subtitle.formattedText != null ? subtitle.formattedText.text : string.Empty;
            return Localized(subtitle.dialogueEntry, false) ?? fallback;
        }

        public static string Response(Response response)
        {
            if (response == null) return string.Empty;
            string fallback = response.formattedText != null ? response.formattedText.text : string.Empty;
            return Localized(response.destinationEntry, true) ?? fallback;
        }

        static string Localized(DialogueEntry entry, bool menu)
        {
            string guid = entry != null ? Field.LookupValue(entry.fields, DialogueAdapter.GuidField) : null;
            if (string.IsNullOrEmpty(guid)) return null;
            if (menu && Loc.TryGet(Loc.DialogueTable, guid + MenuSuffix, out string menuText)) return FormattedText.Parse(menuText).text;
            return Loc.TryGet(Loc.DialogueTable, guid, out string text) ? FormattedText.Parse(text).text : null;
        }

        /// <summary>The keeper's name as dialogue shows it.</summary>
        public static string PlayerName() => StoryLua.HH_PlayerName();
    }
}
