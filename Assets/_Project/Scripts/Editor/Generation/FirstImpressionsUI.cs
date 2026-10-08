using System.Linq;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using Hearthdelve.UI.Typography;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-A's interface, built by the updaters in place: the controls reference (shared by the main menu and the pause menu), the
    /// pause menu and the "saved" mark on Boot's own Menus canvas, and the first free day's prompt strip on the tavern's HUD.
    /// Every element is at 320×180 on the type scale.
    /// </summary>
    internal static class FirstImpressionsUI
    {
        static readonly Color k_Ink = DungeonUI.k_Ink;
        static readonly Color k_Light = DungeonUI.k_Light;
        static readonly Color k_Title = DungeonUI.k_Title;
        static readonly Color k_Label = DungeonUI.k_Label;
        static readonly Vector2 k_Centre = new(0.5f, 0.5f);

        public const string MenusCanvas = "Menus";
        const float Line = 12f;

        // ---------- the controls reference ----------

        /// <summary>The controls page: a parchment panel over a dimmed screen, a title, two column headers and rows.</summary>
        public static ControlsPage BuildControlsPage(RectTransform parent)
        {
            RectTransform root = LookTestBuilder.UIRect(parent, "Controls", k_Centre, k_Centre, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            DungeonUI.AddImage(root, DungeonUI.Pixel(), new Color(0.03f, 0.02f, 0.03f, 0.6f)).raycastTarget = true;
            RectTransform panel = DungeonUI.Panel(root, new Vector2(304f, 172f), Vector2.zero);

            LocalizedSuperText title = Text(panel, "Title", MenuLocKeys.ControlsTitle, k_Title, TextAnchor.MiddleCenter, 0f, 72f, 280f);
            const float actionX = -142f, keyboardX = -16f, padX = 80f;
            const float actionW = 122f, keyboardW = 92f, padW = 62f;
            LocalizedSuperText kb = Text(panel, "KeyboardHeader", MenuLocKeys.ControlsKeyboard, k_Label, TextAnchor.MiddleLeft, keyboardX, 58f, keyboardW, left: true);
            LocalizedSuperText pad = Text(panel, "GamepadHeader", MenuLocKeys.ControlsGamepad, k_Label, TextAnchor.MiddleLeft, padX, 58f, padW, left: true);
            TavernScreens.Rect(panel, "Rule", k_Centre, k_Centre, new Vector2(0f, 51f), new Vector2(284f, 1f), k_Label);

            var actions = new LocalizedSuperText[MenuLocKeys.MaxRows];
            var keyboard = new LocalizedSuperText[MenuLocKeys.MaxRows];
            var gamepad = new LocalizedSuperText[MenuLocKeys.MaxRows];
            for (int i = 0; i < MenuLocKeys.MaxRows; i++)
            {
                float y = 43f - i * Line;
                actions[i] = Text(panel, $"Action{i}", "controls.walk", k_Ink, TextAnchor.MiddleLeft, actionX, y, actionW, left: true);
                keyboard[i] = Text(panel, $"Keyboard{i}", "controls.walk.kb", k_Ink, TextAnchor.MiddleLeft, keyboardX, y, keyboardW, left: true);
                gamepad[i] = Text(panel, $"Gamepad{i}", "controls.walk.pad", k_Ink, TextAnchor.MiddleLeft, padX, y, padW, left: true);
            }
            Text(panel, "Footer", MenuLocKeys.ControlsFooter, k_Label, TextAnchor.MiddleCenter, 0f, -74f, 280f);

            var page = root.gameObject.AddComponent<ControlsPage>();
            page.Configure(root.gameObject, title, kb, pad, actions, keyboard, gamepad);
            root.gameObject.SetActive(false);
            return page;
        }

        /// <summary>One line of text; <paramref name="left"/>: <paramref name="x"/> is its left edge, otherwise its centre.</summary>
        static LocalizedSuperText Text(RectTransform parent, string name, string key, Color color, TextAnchor anchor, float x, float y, float width, bool left = false)
        {
            Vector2 pivot = left ? new Vector2(0f, 0.5f) : k_Centre;
            return LookTestBuilder.Text(parent, name, key, TextStyle.Body, color, anchor, k_Centre, k_Centre, pivot, new Vector2(x, y), new Vector2(width, Line));
        }

        // ---------- Credits (4i-C) ----------

        /// <summary>
        /// The credits over a dimmed screen: a parchment panel with its title, a masked viewport of the Credits table's lines (each a
        /// whole number of 12-pixel lines tall), HeatleyBros' link as a selectable line, and the controls note at the foot.
        /// </summary>
        public static CreditsScreen BuildCredits(RectTransform parent)
        {
            RectTransform root = LookTestBuilder.UIRect(parent, "Credits", k_Centre, k_Centre, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            DungeonUI.AddImage(root, DungeonUI.Pixel(), new Color(0.03f, 0.02f, 0.03f, 0.6f)).raycastTarget = true;
            RectTransform panel = DungeonUI.Panel(root, new Vector2(304f, 172f), Vector2.zero);
            Text(panel, "Title", CreditsLocKeys.ScreenTitle, k_Title, TextAnchor.MiddleCenter, 0f, 72f, 280f).Configure(Loc.CreditsTable, CreditsLocKeys.ScreenTitle);
            TavernScreens.Rect(panel, "Rule", k_Centre, k_Centre, new Vector2(0f, 64f), new Vector2(284f, 1f), k_Label);

            RectTransform viewport = TavernScreens.Rect(panel, "Viewport", k_Centre, k_Centre, new Vector2(0f, -2f), new Vector2(284f, 128f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var top = new Vector2(0.5f, 1f);
            RectTransform content = LookTestBuilder.UIRect(viewport, "Content", top, top, Vector2.zero, new Vector2(284f, 0f));
            float y = -Line * 2f;
            Button link = null;
            foreach (var (kind, key, _, lines) in CreditsLocKeys.Lines)
            {
                float height = lines * Line;
                if (kind == CreditsLocKeys.Kind.Gap)
                {
                    y -= height;
                    continue;
                }
                var at = new Vector2(0f, y);
                if (kind == CreditsLocKeys.Kind.Link)
                {
                    RectTransform rect = LookTestBuilder.UIRect(content, "Link", top, top, at, new Vector2(200f, height));
                    Image back = DungeonUI.AddImage(rect, DungeonUI.Pixel(), Color.white);
                    back.raycastTarget = true;
                    link = rect.gameObject.AddComponent<Button>();
                    link.targetGraphic = back;
                    ColorBlock colors = link.colors;
                    colors.normalColor = new Color(1f, 1f, 1f, 0f);
                    colors.highlightedColor = new Color(1f, 0.86f, 0.45f, 0.55f);
                    colors.selectedColor = new Color(1f, 0.86f, 0.45f, 0.9f);
                    colors.pressedColor = new Color(1f, 0.8f, 0.35f, 1f);
                    link.colors = colors;
                    link.navigation = new Navigation { mode = Navigation.Mode.None };
                    LookTestBuilder.Text(rect, "Label", key, TextStyle.Body, DungeonUI.k_Discovery, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                        k_Centre, Vector2.zero, Vector2.zero).Configure(Loc.CreditsTable, key);
                }
                else
                {
                    TextStyle style = kind == CreditsLocKeys.Kind.Title ? TextStyle.Heading : kind == CreditsLocKeys.Kind.Heading ? TextStyle.Secondary : TextStyle.Body;
                    Color colour = kind == CreditsLocKeys.Kind.Line ? k_Ink : k_Title;
                    LookTestBuilder.Text(content, key, key, style, colour, TextAnchor.UpperCenter, top, top, top, at, new Vector2(276f, height))
                        .Configure(Loc.CreditsTable, key);
                }
                y -= height;
            }
            // Room to finish: the last line scrolls up to the middle of the viewport.
            content.sizeDelta = new Vector2(284f, -y + 64f);
            LocalizedSuperText footer = Text(panel, "Footer", CreditsLocKeys.Back, k_Label, TextAnchor.MiddleCenter, 0f, -74f, 280f);
            footer.Configure(Loc.CreditsTable, CreditsLocKeys.Back);

            var screen = root.gameObject.AddComponent<CreditsScreen>();
            screen.Configure(root.gameObject, viewport, content, link, footer);
            root.gameObject.SetActive(false);
            return screen;
        }

        // ---------- Options (4i-B) ----------

        public const int OptionRows = 6;

        /// <summary>
        /// The Options screen over a dimmed screen: a parchment panel with five tabs across the top, six lines under them and a note at
        /// the foot. Its controls tab is <paramref name="controls"/> (the 4i-A controls reference, a sibling).
        /// </summary>
        public static OptionsScreen BuildOptions(RectTransform parent, ControlsPage controls)
        {
            RectTransform root = LookTestBuilder.UIRect(parent, "Options", k_Centre, k_Centre, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            DungeonUI.AddImage(root, DungeonUI.Pixel(), new Color(0.03f, 0.02f, 0.03f, 0.6f)).raycastTarget = true;
            RectTransform panel = DungeonUI.Panel(root, new Vector2(304f, 172f), Vector2.zero);

            // The tabs, left to right, each just wide enough for its word.
            var tabKeys = new[] { MenuLocKeys.TabAudio, MenuLocKeys.TabFeel, MenuLocKeys.TabDisplay, MenuLocKeys.TabAccess, MenuLocKeys.TabControls };
            var widths = new[] { 44f, 40f, 52f, 76f, 56f };
            var tabs = new Button[tabKeys.Length];
            float x = -140f;
            for (int i = 0; i < tabKeys.Length; i++)
            {
                tabs[i] = TavernScreens.SmallButton(panel, $"Tab{i}", tabKeys[i], k_Centre, new Vector2(x + widths[i] / 2f, 70f), widths[i], out _);
                x += widths[i] + 3f;
            }
            for (int i = 0; i < tabs.Length; i++)
            {
                Navigation n = tabs[i].navigation;
                n.mode = Navigation.Mode.Explicit;
                n.selectOnLeft = tabs[(i - 1 + tabs.Length) % tabs.Length];
                n.selectOnRight = tabs[(i + 1) % tabs.Length];
                tabs[i].navigation = n;
            }

            var rows = new OptionRow[OptionRows];
            for (int i = 0; i < OptionRows; i++) rows[i] = OptionRowAt(panel, i, 47f - i * 17f);
            LocalizedSuperText note = LookTestBuilder.Text(panel, "Note", MenuLocKeys.OptionsNoteChange, TextStyle.Secondary, k_Label, TextAnchor.MiddleCenter,
                k_Centre, k_Centre, k_Centre, new Vector2(0f, -65f), new Vector2(284f, 3f * Line));

            var screen = root.gameObject.AddComponent<OptionsScreen>();
            screen.Configure(root.gameObject, panel.gameObject, tabs, rows, note, controls);
            root.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>One line: its name on the left, "« value »" on the right; the whole line is the selectable (gold when selected).</summary>
        static OptionRow OptionRowAt(RectTransform panel, int index, float y)
        {
            RectTransform rect = TavernScreens.Rect(panel, $"Row{index}", k_Centre, k_Centre, new Vector2(0f, y), new Vector2(276f, 15f));
            Image back = DungeonUI.AddImage(rect, DungeonUI.Pixel(), Color.white);
            back.raycastTarget = true;
            rect.gameObject.AddComponent<CanvasGroup>();
            LocalizedSuperText label = Text(rect, "Label", MenuLocKeys.OptMaster, k_Ink, TextAnchor.MiddleLeft, -134f, 0f, 150f, left: true);
            LocalizedSuperText value = Text(rect, "Value", TavernLocKeys.Plain, k_Ink, TextAnchor.MiddleCenter, 82f, 0f, 96f);
            RectTransform arrows = TavernScreens.Rect(rect, "Arrows", k_Centre, k_Centre, new Vector2(82f, 0f), new Vector2(120f, 15f));
            Text(arrows, "Less", MenuLocKeys.ArrowLess, k_Title, TextAnchor.MiddleCenter, -54f, 0f, 12f);
            Text(arrows, "More", MenuLocKeys.ArrowMore, k_Title, TextAnchor.MiddleCenter, 54f, 0f, 12f);
            var row = rect.gameObject.AddComponent<OptionRow>();
            row.targetGraphic = back;
            ColorBlock colors = row.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.86f, 0.45f, 0.55f);
            colors.selectedColor = new Color(1f, 0.86f, 0.45f, 0.9f);
            colors.pressedColor = new Color(1f, 0.8f, 0.35f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            row.colors = colors;
            row.Configure(label, value, arrows.gameObject);
            return row;
        }

        // ---------- Boot: the pause menu and the "saved" mark ----------

        /// <summary>Boot's Menus canvas, rebuilt in place (it's the tooling's own): the pause menu, its controls page, the saved mark.</summary>
        public static void BuildBootMenus()
        {
            GameObject old = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(c => c.gameObject)
                .FirstOrDefault(g => g.name == MenusCanvas);
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject(MenusCanvas);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Over the dialogue box (500) and the transition (1000): the pause menu never opens during a transition, and the saved
            // mark should show over one.
            canvas.sortingOrder = 1001;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LookTestBuilder.ReferenceWidth, LookTestBuilder.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            scaler.referencePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<Hearthdelve.UI.PixelCanvasScaler>();
            UiFeedbackContent.Ensure(canvas);

            BuildPauseMenu(canvas);
            BuildSaveIndicator(canvas);
        }

        static void BuildPauseMenu(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Pause");
            DungeonUI.AddImage(root, DungeonUI.Pixel(), new Color(0.03f, 0.02f, 0.03f, 0.55f)).raycastTarget = true;

            // The main panel: the title and four buttons (quit game only on desktop).
            RectTransform main = TavernScreens.Rect(root, "Main", k_Centre, k_Centre, Vector2.zero, new Vector2(150f, 152f));
            DungeonUI.AddImage(main, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            TavernScreens.Label(main, "Title", MenuLocKeys.PauseTitle, TextStyle.Heading, k_Title, TextAnchor.MiddleCenter, k_Centre, new Vector2(0f, 58f), new Vector2(130f, 24f));
            Button resume = TavernScreens.SmallButton(main, "Resume", MenuLocKeys.Resume, k_Centre, new Vector2(0f, 34f), 110f, out _);
            Button options = TavernScreens.SmallButton(main, "Options", MenuLocKeys.Options, k_Centre, new Vector2(0f, 15f), 110f, out _);
            Button controls = TavernScreens.SmallButton(main, "Controls", MenuLocKeys.Controls, k_Centre, new Vector2(0f, -4f), 110f, out _);
            Button credits = TavernScreens.SmallButton(main, "Credits", MenuLocKeys.Credits, k_Centre, new Vector2(0f, -23f), 110f, out _);
            Button quitMenu = TavernScreens.SmallButton(main, "QuitToMenu", MenuLocKeys.QuitToMenu, k_Centre, new Vector2(0f, -42f), 110f, out _);
            Button quitGame = TavernScreens.SmallButton(main, "QuitGame", MenuLocKeys.QuitGame, k_Centre, new Vector2(0f, -61f), 110f, out _);
            Vertical(resume, options, controls, credits, quitMenu, quitGame);

            // The question: what will be lost, then quit or back.
            RectTransform confirm = TavernScreens.Rect(root, "Confirm", k_Centre, k_Centre, Vector2.zero, new Vector2(280f, 76f));
            DungeonUI.AddImage(confirm, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            LocalizedSuperText question = LookTestBuilder.Text(confirm, "Question", MenuLocKeys.QuitEvening, TextStyle.Body, k_Ink, TextAnchor.MiddleCenter,
                k_Centre, k_Centre, k_Centre, new Vector2(0f, 12f), new Vector2(264f, 4f * Line));
            Button yes = TavernScreens.SmallButton(confirm, "Yes", MenuLocKeys.QuitYes, k_Centre, new Vector2(-45f, -24f), 80f, out _);
            Button no = TavernScreens.SmallButton(confirm, "No", MenuLocKeys.QuitBack, k_Centre, new Vector2(45f, -24f), 80f, out _);
            Horizontal(yes, no);
            confirm.gameObject.SetActive(false);

            ControlsPage page = BuildControlsPage(root);
            OptionsScreen optionsScreen = BuildOptions(root, page);
            CreditsScreen creditsScreen = BuildCredits(root);
            var menu = canvas.gameObject.AddComponent<PauseMenu>();
            menu.Configure(root.gameObject, main.gameObject, resume, controls, quitMenu, quitGame, confirm.gameObject, question, yes, no, page);
            menu.ConfigureOptions(options, optionsScreen);
            menu.ConfigureCredits(credits, creditsScreen);
            root.gameObject.SetActive(false);
        }

        static void BuildSaveIndicator(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Saved");
            var corner = new Vector2(1f, 0f);
            RectTransform strip = LookTestBuilder.UIRect(root, "Strip", corner, corner, new Vector2(-3f, 3f), new Vector2(44f, Line + 2f));
            DungeonUI.AddImage(strip, DungeonUI.Pixel(), new Color(0.05f, 0.04f, 0.06f, 0.7f));
            var group = strip.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            LookTestBuilder.Text(strip, "Text", MenuLocKeys.Saved, TextStyle.Secondary, k_Light, TextAnchor.MiddleCenter, k_Centre, k_Centre, k_Centre, Vector2.zero,
                new Vector2(40f, Line));
            root.gameObject.AddComponent<SaveIndicator>().Configure(group);
        }

        /// <summary>Up and down between buttons, wrapping (a controller never gets stuck at an end).</summary>
        internal static void Vertical(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Navigation n = buttons[i].navigation;
                n.mode = Navigation.Mode.Explicit;
                n.selectOnUp = buttons[(i - 1 + buttons.Length) % buttons.Length];
                n.selectOnDown = buttons[(i + 1) % buttons.Length];
                n.selectOnLeft = n.selectOnRight = null;
                buttons[i].navigation = n;
            }
        }

        static void Horizontal(Button left, Button right)
        {
            Navigation a = left.navigation;
            a.mode = Navigation.Mode.Explicit;
            a.selectOnRight = right;
            a.selectOnLeft = right;
            left.navigation = a;
            Navigation b = right.navigation;
            b.mode = Navigation.Mode.Explicit;
            b.selectOnLeft = left;
            b.selectOnRight = left;
            right.navigation = b;
        }

        // ---------- the tavern's HUD: the first free day's prompts ----------

        /// <summary>The prompt strip, rebuilt in place on the tavern's UI canvas: over the interaction hint at the foot of the screen.</summary>
        public static void BuildSurfacePrompts(Canvas ui)
        {
            Transform old = ui.transform.Find("FirstDayPrompts");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(ui, "FirstDayPrompts");
            var bottom = new Vector2(0.5f, 0f);
            RectTransform strip = LookTestBuilder.UIRect(root, "Strip", bottom, bottom, new Vector2(0f, 30f), new Vector2(288f, 2f * Line + 4f));
            DungeonUI.AddImage(strip, DungeonUI.Pixel(), new Color(0.05f, 0.04f, 0.06f, 0.78f));
            var group = strip.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            LocalizedSuperText text = LookTestBuilder.Text(strip, "Text", MenuLocKeys.PromptGarden, TextStyle.Body, k_Light, TextAnchor.MiddleCenter,
                k_Centre, k_Centre, k_Centre, Vector2.zero, new Vector2(280f, 2f * Line));
            root.gameObject.AddComponent<SurfacePrompts>().Configure(group, text);
        }
    }
}
