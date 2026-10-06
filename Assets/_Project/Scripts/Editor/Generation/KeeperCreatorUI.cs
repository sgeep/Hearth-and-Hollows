using System.Collections.Generic;
using Hearthdelve.UI;
using Hearthdelve.UI.Typography;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The character creator's layout (4g Checkpoint B), on the 320×180 menu canvas: the preview at 2× on the left, five rows (name,
    /// look, skin, hair, clothes) on the right, back and begin under them, and the letters for the name over the top. Built with
    /// the main menu (rebuilt in place with it).
    /// </summary>
    static class KeeperCreatorUI
    {
        const float RowWidth = 184f, RowHeight = 16f, RowPitch = 18f, RowX = 46f, RowTop = 44f;
        const float KeyWidth = 18f, KeyPitchX = 20f, KeyPitchY = 18f;

        /// <summary>Each key's two cases, written out (the screen never converts case).</summary>
        static readonly string[][] k_Letters =
        {
            new[] { "a|A", "b|B", "c|C", "d|D", "e|E", "f|F", "g|G", "h|H", "i|I", "j|J" },
            new[] { "k|K", "l|L", "m|M", "n|N", "o|O", "p|P", "q|Q", "r|R", "s|S", "t|T" },
            new[] { "u|U", "v|V", "w|W", "x|X", "y|Y", "z|Z", "'|'", "-|-" },
        };

        public static CharacterCreatorScreen Build(RectTransform menu)
        {
            var centre = new Vector2(0.5f, 0.5f);
            // A full-screen layer over the menu, the menu's own dark.
            RectTransform root = TavernScreens.Rect(menu, "Creator", centre, centre, Vector2.zero, Vector2.zero, new Color(0.07f, 0.05f, 0.05f));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            root.GetComponent<Image>().raycastTarget = true;
            TavernScreens.Label(root, "Title", CreatorLocKeys.Title, TextStyle.Heading, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter, centre,
                new Vector2(0f, 74f), new Vector2(300f, 24f));
            RectTransform panel = DungeonUI.Panel(root, new Vector2(300f, 140f), new Vector2(0f, -14f));

            // The preview: the keeper walking round, at 3× (whole pixels), in a dark frame.
            RectTransform frame = TavernScreens.Rect(panel, "PreviewFrame", centre, centre, new Vector2(-97f, 10f), new Vector2(96f, 96f), DungeonUI.k_Ink);
            RectTransform previewRect = TavernScreens.Rect(frame, "Preview", centre, centre, new Vector2(0f, 0f), new Vector2(96f, 96f));
            Image preview = DungeonUI.AddImage(previewRect, null, Color.white);
            preview.preserveAspect = true;

            var rows = new List<CharacterCreatorScreen.Row>
            {
                Row(panel, 0, CharacterCreatorScreen.NameRow, CreatorLocKeys.Name, arrows: false),
                Row(panel, 1, CharacterCreatorScreen.BodyRow, CreatorLocKeys.Body, arrows: true),
                Row(panel, 2, CharacterCreatorScreen.SkinRow, CreatorLocKeys.Skin, arrows: true),
                Row(panel, 3, CharacterCreatorScreen.HairRow, CreatorLocKeys.Hair, arrows: true),
                Row(panel, 4, CharacterCreatorScreen.OutfitRow, CreatorLocKeys.Outfit, arrows: true),
            };
            TavernScreens.Label(panel, "Hint", CreatorLocKeys.Hint, TextStyle.Secondary, DungeonUI.k_Label, TextAnchor.MiddleCenter, centre,
                new Vector2(RowX, RowTop - 5 * RowPitch + 4f), new Vector2(RowWidth + 8f, 12f));
            Button back = TavernScreens.SmallButton(panel, "Back", CreatorLocKeys.Back, centre, new Vector2(-60f, -58f), 80f, out _);
            Button begin = TavernScreens.SmallButton(panel, "Begin", CreatorLocKeys.Begin, centre, new Vector2(60f, -58f), 80f, out _);
            UiFeedbackContent.Commit(begin);

            // The name: the letters (a controller's way), or type on a keyboard.
            RectTransform naming = DungeonUI.Panel(root, new Vector2(300f, 140f), new Vector2(0f, -14f));
            naming.name = "Naming";
            TavernScreens.Label(naming, "Title", CreatorLocKeys.NameTitle, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.MiddleCenter, centre,
                new Vector2(0f, 56f), new Vector2(280f, 12f));
            TavernScreens.Rect(naming, "FieldBack", centre, centre, new Vector2(0f, 40f), new Vector2(150f, 14f), new Color(0.85f, 0.76f, 0.6f));
            LocalizedSuperText field = TavernScreens.Label(naming, "Field", TavernLocKeys.Plain, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.MiddleCenter,
                centre, new Vector2(0f, 40f), new Vector2(146f, 12f));
            var keys = new List<Button>();
            var values = new List<string>();
            var labels = new List<LocalizedSuperText>();
            for (int r = 0; r < k_Letters.Length; r++)
                for (int c = 0; c < k_Letters[r].Length; c++)
                {
                    string letter = k_Letters[r][c];
                    Button key = TavernScreens.SmallButton(naming, $"Key {letter.Substring(0, 1)}", TavernLocKeys.Plain, centre,
                        new Vector2(-90f + c * KeyPitchX, 18f - r * KeyPitchY), KeyWidth, out LocalizedSuperText label);
                    keys.Add(key);
                    values.Add(letter);
                    labels.Add(label);
                }
            foreach (var (value, key, x, width) in new[]
                     {
                         (CharacterCreatorScreen.KeyShift, CreatorLocKeys.Shift, -84f, 48f),
                         (CharacterCreatorScreen.KeySpace, CreatorLocKeys.Space, -28f, 52f),
                         (CharacterCreatorScreen.KeyDelete, CreatorLocKeys.Delete, 30f, 52f),
                         (CharacterCreatorScreen.KeyDone, CreatorLocKeys.Done, 88f, 48f),
                     })
            {
                keys.Add(TavernScreens.SmallButton(naming, $"Key {key}", key, centre, new Vector2(x, -38f), width, out LocalizedSuperText label));
                values.Add(value);
                labels.Add(label);
            }
            TavernScreens.Label(naming, "Hint", CreatorLocKeys.NameHint, TextStyle.Secondary, DungeonUI.k_Label, TextAnchor.MiddleCenter, centre,
                new Vector2(0f, -56f), new Vector2(280f, 12f));
            UiFeedbackContent.Commit(keys[keys.Count - 1]);

            // Up and down move between the rows, back and begin; left and right change a row (the screen does that), never the focus.
            for (int i = 0; i < rows.Count; i++)
                rows[i].button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? rows[i - 1].button : null,
                    selectOnDown = i < rows.Count - 1 ? rows[i + 1].button : begin,
                };
            back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = rows[rows.Count - 1].button, selectOnRight = begin };
            begin.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = rows[rows.Count - 1].button, selectOnLeft = back };

            var screen = menu.gameObject.AddComponent<CharacterCreatorScreen>();
            screen.Configure(root.gameObject, preview, rows.ToArray(), begin, back, naming.gameObject, field, keys.ToArray(), values.ToArray(), labels.ToArray());
            naming.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>
        /// A row: one button the size of the row (it takes the focus: up and down move between rows), its label on the left, the
        /// value in the middle, and the two arrows for the mouse (left and right on the stick or keys do the same).
        /// </summary>
        static CharacterCreatorScreen.Row Row(RectTransform panel, int index, string kind, string labelKey, bool arrows)
        {
            var centre = new Vector2(0.5f, 0.5f);
            RectTransform rect = LookTestBuilder.UIRect(panel, $"Row {kind}", centre, centre, new Vector2(RowX, RowTop - index * RowPitch),
                new Vector2(RowWidth, RowHeight));
            Button button = DungeonUI.ButtonFace(rect);
            var left = new Vector2(0f, 0.5f);
            LookTestBuilder.Text(rect, "Label", labelKey, TextStyle.Prompt, DungeonUI.k_Label, TextAnchor.MiddleLeft, left, left, left,
                new Vector2(4f, 0f), new Vector2(52f, 12f));
            var row = new CharacterCreatorScreen.Row { kind = kind, button = button };
            // The name has no arrows: its value takes their room too (sixteen letters).
            row.value = TavernScreens.Label(rect, "Value", TavernLocKeys.Plain, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.MiddleCenter, centre,
                new Vector2(28f, 0f), arrows ? new Vector2(92f, 12f) : new Vector2(124f, 12f));
            if (arrows)
            {
                row.left = Arrow(rect, "Left", CreatorLocKeys.Left, -28f);
                row.right = Arrow(rect, "Right", CreatorLocKeys.Right, 84f);
            }
            return row;
        }

        /// <summary>A small arrow for the mouse: never focused (the stick and keys change the row itself).</summary>
        static Button Arrow(RectTransform row, string name, string key, float x)
        {
            Button arrow = TavernScreens.SmallButton(row, name, key, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), 14f, out _);
            arrow.navigation = new Navigation { mode = Navigation.Mode.None };
            return arrow;
        }
    }
}
