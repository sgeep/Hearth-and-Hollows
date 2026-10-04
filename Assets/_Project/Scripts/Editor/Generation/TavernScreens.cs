using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The day's screens on the tavern canvas: Morning, Prep, the service HUD, Results and Night. Classic UI Overhaul
    /// panels, buttons and slots, as in the dungeon, with m5x7 text on a 10-pixel line (<see cref="Line"/>). Layouts are
    /// measured against the font's real widths (4c step 6). The canvas is never smaller than 320×180 (whole-pixel
    /// scaling), so the planning panels are 312×172; the HUD lives in the 48-pixel side margins the centred room leaves.
    /// </summary>
    public static class TavernScreens
    {
        static readonly Color k_Ink = DungeonUI.k_Ink;
        static readonly Color k_Light = DungeonUI.k_Light;
        static readonly Color k_Muted = new(0.72f, 0.66f, 0.58f);
        static readonly Color k_Gold = DungeonUI.k_Mark;
        static readonly Color k_Warning = new(1f, 0.55f, 0.35f);
        static readonly Color k_Note = new(0.45f, 0.3f, 0.2f);
        static readonly Color k_Card = new(0.82f, 0.66f, 0.46f);

        /// <summary>One line of m5x7: capitals, descenders and a pixel between lines.</summary>
        public const float Line = 10f;
        /// <summary>The planning panels: as large as the smallest canvas allows.</summary>
        static readonly Vector2 k_Panel = new(312f, 172f);
        const float k_PanelTop = 86f;
        const float k_CardWidth = 148f, k_CardHeight = 22f, k_CardPitch = 23f;

        /// <summary>
        /// Removes the day's screens from the canvas and builds them again (HUD first, at the back). The controls
        /// line moves into the HUD, so it shows (and fades) when service starts, not over Morning or Night panels.
        /// </summary>
        public static void Rebuild(Canvas canvas)
        {
            UiFeedbackContent.Ensure(canvas);
            Transform controls = canvas.transform.Find("Controls") ?? canvas.transform.Find("TavernHud/Content/Controls");
            if (controls != null) controls.SetParent(canvas.transform, false);
            foreach (string name in new[] { "TavernHud", "Prep", "Results", "Morning", "Night" })
            {
                Transform old = canvas.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            RectTransform hud = BuildHud(canvas);
            hud.SetAsFirstSibling();
            if (controls != null) controls.SetParent(hud.Find("Content"), false);
            BuildPrep(canvas);
            BuildResults(canvas);
            BuildMorning(canvas);
            BuildNight(canvas);
        }

        /// <summary>A text button anywhere (the plain parchment face, gold when selected).</summary>
        internal static Button SmallButton(RectTransform parent, string name, string key, Vector2 anchorAt, Vector2 position, float width, out LocalizedSuperText label)
        {
            RectTransform rect = LookTestBuilder.UIRect(parent, name, anchorAt, anchorAt, position, new Vector2(width, 14f));
            Button button = DungeonUI.ButtonFace(rect);
            label = LookTestBuilder.Text(rect, "Label", key, 6f, k_Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero);
            return button;
        }

        internal static LocalizedSuperText Label(RectTransform parent, string name, string key, float size, Color color, TextAnchor anchor, Vector2 anchorAt, Vector2 position, Vector2 box) =>
            LookTestBuilder.Text(parent, name, key, size, color, anchor, anchorAt, anchorAt, anchorAt, position, box);

        /// <summary>One line of body text whose box's top edge is at <paramref name="top"/> (panel-centre coordinates).</summary>
        static LocalizedSuperText TextLine(RectTransform parent, string name, string key, Color color, TextAnchor anchor, float x, float top, float width)
        {
            var at = new Vector2(0.5f, 0.5f);
            var pivot = anchor switch
            {
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                _ => new Vector2(0f, 1f),
            };
            return LookTestBuilder.Text(parent, name, key, 6f, color, anchor, at, at, pivot, new Vector2(x, top), new Vector2(width, Line));
        }

        internal static RectTransform Rect(RectTransform parent, string name, Vector2 anchorAt, Vector2 pivot, Vector2 position, Vector2 size, Color? color = null)
        {
            RectTransform rect = LookTestBuilder.UIRect(parent, name, anchorAt, pivot, position, size);
            if (color.HasValue) DungeonUI.AddImage(rect, DungeonUI.Pixel(), color.Value);
            return rect;
        }

        /// <summary>A child that fills its parent from the left; the HUD moves its right edge.</summary>
        static RectTransform Fill(RectTransform parent, string name, Color color)
        {
            RectTransform fill = Rect(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, color);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = Vector2.zero;
            return fill;
        }

        /// <summary>A bottom-row button, centred <paramref name="x"/> from the panel's centre.</summary>
        static Button BottomButton(RectTransform panel, string name, string key, float x, float width, out LocalizedSuperText label) =>
            SmallButton(panel, name, key, new Vector2(0.5f, 0f), new Vector2(x, 9f), width, out label);

        /// <summary>The storeroom as one row of satchel-style slots under the title, with its label.</summary>
        static SatchelSlotView[] Storeroom(RectTransform panel, out LocalizedSuperText empty)
        {
            TextLine(panel, "StoreroomLabel", TavernLocKeys.PrepStoreroom, k_Ink, TextAnchor.UpperLeft, -150f, 62f, 56f);
            var stock = new SatchelSlotView[15];
            for (int i = 0; i < stock.Length; i++)
            {
                stock[i] = DungeonUI.Slot(panel, i, new Vector2(-84f + i * 16f, 46f), false, out Button button);
                Object.DestroyImmediate(button);
                stock[i].GetComponent<Image>().raycastTarget = false;
            }
            empty = TextLine(panel, "StoreroomEmpty", TavernLocKeys.PrepStoreroomEmpty, k_Note, TextAnchor.UpperLeft, -90f, 62f, 230f);
            return stock;
        }

        /// <summary>The card grid: two columns of <see cref="k_CardWidth"/>, first row's top at y 29.</summary>
        static Vector2 CardCentre(int index) => new((index % 2 == 0 ? -1f : 1f) * (k_CardWidth / 2f + 2f), 29f - k_CardHeight / 2f - (index / 2) * k_CardPitch);

        // ------------------------------------------------------------------ Morning

        static void BuildMorning(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Morning");
            RectTransform panel = DungeonUI.Panel(root, k_Panel, Vector2.zero);
            LocalizedSuperText title = DungeonUI.Title(panel, LoopLocKeys.MorningTitle);
            SatchelSlotView[] stock = Storeroom(panel, out LocalizedSuperText empty);

            // Breakfast: what to cook, or what was eaten.
            LocalizedSuperText breakfast = TextLine(panel, "Breakfast", LoopLocKeys.MorningBreakfast, k_Ink, TextAnchor.UpperCenter, 0f, 41f, 296f);
            var cards = new DishCard[6];
            // Breakfast cards: the name with its station on the right, then the buff across the card.
            for (int i = 0; i < cards.Length; i++) cards[i] = Card(panel, i, CardCentre(i), 130f);

            // What today's delve starts with, from upgrades and breakfast: the tavern feeding the dungeon. Two lines at most.
            LocalizedSuperText bonuses = LookTestBuilder.Text(panel, "Bonuses", LoopLocKeys.MorningNoBonuses, 6f, new Color(0.3f, 0.2f, 0.45f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(296f, Line * 2f));
            Button descend = BottomButton(panel, "Descend", LoopLocKeys.MorningDescend, 0f, 152f, out _);
            UiFeedbackContent.Commit(descend);
            root.gameObject.AddComponent<MorningScreen>().Configure(panel.gameObject, title, stock, empty, cards, breakfast, bonuses, descend);
            panel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Night

        static void BuildNight(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Night");
            RectTransform panel = DungeonUI.Panel(root, k_Panel, Vector2.zero);
            LocalizedSuperText title = DungeonUI.Title(panel, LoopLocKeys.NightTitle);

            // How the day went, with the purse on the first line's right.
            var summary = new LocalizedSuperText[5];
            for (int i = 0; i < summary.Length; i++)
                summary[i] = TextLine(panel, $"Summary{i + 1}", LoopLocKeys.SummaryDelve, k_Ink, TextAnchor.UpperLeft, -150f, 64f - i * Line, i == 0 ? 160f : 300f);
            LocalizedSuperText purse = TextLine(panel, "Purse", LoopLocKeys.NightPurse, new Color(0.45f, 0.3f, 0.05f), TextAnchor.UpperRight, 150f, 64f, 136f);

            // The upgrades, bought with banked gold: a row each, with its price on a button.
            var rows = new UpgradeRow[3];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = Rect(panel, $"Upgrade{i + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 10f - i * 23f), new Vector2(300f, 22f), k_Card);
                LocalizedSuperText name = TextLine(row, "Name", LoopLocKeys.NightUpgradeLevel, k_Ink, TextAnchor.UpperLeft, -147f, 11f, 230f);
                LocalizedSuperText effect = TextLine(row, "Effect", LoopLocKeys.NightNextTime, k_Note, TextAnchor.UpperLeft, -147f, 1f, 230f);
                Button buy = SmallButton(row, "Buy", LoopLocKeys.NightBuyCost, new Vector2(1f, 0.5f), new Vector2(-3f, 0f), 60f, out LocalizedSuperText cost);
                ((RectTransform)buy.transform).pivot = new Vector2(1f, 0.5f);
                // A purchase has its own moment (the Night screen plays it when the buy goes through).
                Object.DestroyImmediate(buy.GetComponent<UiButtonFeedback>());
                rows[i] = new UpgradeRow { root = row.gameObject, name = name, effect = effect, buy = buy, cost = cost };
            }

            LocalizedSuperText saved = TextLine(panel, "Saved", LoopLocKeys.NightSaved, new Color(0.25f, 0.45f, 0.2f), TextAnchor.UpperLeft, -150f, -66f, 70f);
            Button sleep = BottomButton(panel, "Sleep", LoopLocKeys.NightSleep, 0f, 120f, out _);
            UiFeedbackContent.Commit(sleep);
            root.gameObject.AddComponent<NightScreen>().Configure(panel.gameObject, title, summary, purse, rows, saved, sleep);
            saved.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ HUD

        static RectTransform BuildHud(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "TavernHud");
            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var top = new Vector2(0.5f, 1f);

            // The clock: a bar that runs down as the evening goes. "Last orders!" goes top centre, over the room, where it's seen.
            RectTransform clock = Rect(root, "Clock", topLeft, topLeft, new Vector2(3f, -4f), new Vector2(42f, 4f), new Color(0.12f, 0.08f, 0.06f));
            RectTransform clockFill = Fill(clock, "Fill", new Color(1f, 0.78f, 0.35f));
            LocalizedSuperText lastOrders = LookTestBuilder.Text(root, "LastOrders", TavernLocKeys.HudLastOrders, 6f, k_Warning, TextAnchor.UpperCenter, top, top, top, new Vector2(0f, -4f), new Vector2(100f, Line));

            // Takings and Renown: a label over its number (the margin is 48 pixels wide).
            LocalizedSuperText gold = LookTestBuilder.Text(root, "Gold", TavernLocKeys.HudGold, 6f, k_Light, TextAnchor.UpperLeft, topLeft, topLeft, topLeft, new Vector2(3f, -12f), new Vector2(44f, Line * 2f));
            LocalizedSuperText tips = LookTestBuilder.Text(root, "Tips", TavernLocKeys.HudTips, 6f, k_Light, TextAnchor.UpperLeft, topLeft, topLeft, topLeft, new Vector2(3f, -35f), new Vector2(44f, Line * 2f));
            LocalizedSuperText renown = LookTestBuilder.Text(root, "Renown", TavernLocKeys.HudRenown, 6f, k_Light, TextAnchor.UpperLeft, topLeft, topLeft, topLeft, new Vector2(3f, -58f), new Vector2(44f, Line * 2f));

            // Tonight's menu: a sold-out dish dims and gets a red bar (two cues, not only colour).
            var menu = new Image[3];
            var soldOut = new GameObject[3];
            for (int i = 0; i < menu.Length; i++)
            {
                RectTransform icon = Rect(root, $"Menu{i + 1}", topLeft, topLeft, new Vector2(3f + i * 13f, -84f), new Vector2(8f, 8f));
                menu[i] = DungeonUI.AddImage(icon, null, Color.white);
                soldOut[i] = Rect(icon, "SoldOut", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 2f), new Color(0.9f, 0.2f, 0.15f)).gameObject;
                soldOut[i].transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                soldOut[i].SetActive(false);
            }

            // The order rail, right margin: per order, the dish and its customer's patience, with the state under them.
            LookTestBuilder.Text(root, "Orders", TavernLocKeys.HudOrders, 6f, k_Muted, TextAnchor.UpperLeft, topRight, topRight, topRight, new Vector2(-3f, -3f), new Vector2(44f, Line));
            var rows = new RailRow[8];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = Rect(root, $"Order{i + 1}", topRight, topRight, new Vector2(-3f, -15f - i * 20f), new Vector2(44f, 19f));
                RectTransform iconRect = Rect(row, "Icon", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 8f));
                Image icon = DungeonUI.AddImage(iconRect, null, Color.white);
                RectTransform patienceBack = Rect(row, "PatienceBack", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(11f, -3f), new Vector2(33f, 2f), new Color(0.12f, 0.08f, 0.06f));
                RectTransform patience = Fill(patienceBack, "Patience", Color.white);
                LocalizedSuperText state = LookTestBuilder.Text(row, "State", TavernLocKeys.TicketQueued, 6f, k_Light, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -8f), new Vector2(44f, Line));
                rows[i] = new RailRow { root = row.gameObject, icon = icon, state = state, patience = patience, patienceFill = patience.GetComponent<Image>() };
                row.gameObject.SetActive(false);
            }

            var hud = root.gameObject.AddComponent<TavernHud>();
            var content = new GameObject("Content").AddComponent<RectTransform>();
            content.SetParent(root, false);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.sizeDelta = Vector2.zero;
            // Everything above goes under one object the HUD shows and hides.
            for (int i = root.childCount - 1; i >= 0; i--)
                if (root.GetChild(i) != content) root.GetChild(i).SetParent(content, true);
            hud.Configure(content.gameObject, clockFill, lastOrders.gameObject, gold, tips, renown, menu, soldOut, rows);
            content.gameObject.SetActive(false);
            return root;
        }

        // ------------------------------------------------------------------ Prep

        static void BuildPrep(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Prep");
            RectTransform panel = DungeonUI.Panel(root, k_Panel, Vector2.zero);
            DungeonUI.Title(panel, TavernLocKeys.PrepTitle);
            // Debug builds: on the title row's right.
            LocalizedSuperText fill = TextLine(panel, "FillHint", TavernLocKeys.PrepFillKey, k_Note, TextAnchor.UpperRight, 150f, k_PanelTop - 7f, 110f);

            // The storeroom: what came back from the dungeon, as satchel-style slots.
            SatchelSlotView[] stock = Storeroom(panel, out LocalizedSuperText empty);

            // Tonight's menu: a card per dish. The "nothing cookable" line replaces the menu count when it applies.
            LocalizedSuperText tonight = TextLine(panel, "Tonight", TavernLocKeys.PrepTonight, k_Ink, TextAnchor.UpperCenter, 0f, 41f, 296f);
            LocalizedSuperText nothing = TextLine(panel, "Nothing", TavernLocKeys.PrepNothingCookable, new Color(0.6f, 0.15f, 0.1f), TextAnchor.UpperCenter, 0f, 41f, 296f);
            var cards = new DishCard[8];
            for (int i = 0; i < cards.Length; i++) cards[i] = Card(panel, i, CardCentre(i), 66f);

            Button staff = BottomButton(panel, "Staff", TavernLocKeys.PrepStaffJob, -107f, 80f, out LocalizedSuperText staffLabel);
            Button close = BottomButton(panel, "Close", LoopLocKeys.PrepClose, -6f, 114f, out _);
            Button open = BottomButton(panel, "Open", TavernLocKeys.PrepOpen, 101f, 92f, out _);
            UiFeedbackContent.Commit(open);
            root.gameObject.AddComponent<PrepScreen>().Configure(panel.gameObject, stock, empty, tonight, cards, staff, staffLabel, close, open, nothing, fill.gameObject);
            // The screen object stays active (it listens); the panel shows and hides.
            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// A dish card, two lines beside its icon: the name with the price on the right, then how many there are with
        /// how it's prepared on the right (however many steps). Gold corners when chosen.
        /// </summary>
        static DishCard Card(RectTransform panel, int index, Vector2 centre, float amountWidth)
        {
            RectTransform rect = Rect(panel, $"Dish{index + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre, new Vector2(k_CardWidth, k_CardHeight));
            Image back = DungeonUI.AddImage(rect, DungeonUI.Pixel(), k_Card);
            back.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            ColorBlock colors = button.colors;
            // Focus lightens a tile a little; being chosen (gold tile and corners) is the stronger mark.
            colors.highlightedColor = colors.selectedColor = new Color(1.08f, 1.08f, 1.08f);
            colors.colorMultiplier = 1.1f;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0f;
            button.colors = colors;
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            rect.gameObject.AddComponent<UiButtonFeedback>();

            var left = new Vector2(0f, 0.5f);
            RectTransform iconRect = Rect(rect, "Icon", left, left, new Vector2(2f, 0f), new Vector2(8f, 8f));
            Image icon = DungeonUI.AddImage(iconRect, null, Color.white);
            float x = -k_CardWidth / 2f + 12f, right = k_CardWidth / 2f - 3f;
            LocalizedSuperText name = TextLine(rect, "Name", TavernLocKeys.Plain, k_Ink, TextAnchor.UpperLeft, x, 10f, 96f);
            LocalizedSuperText detail = TextLine(rect, "Detail", TavernLocKeys.Plain, k_Ink, TextAnchor.UpperRight, right, 10f, 70f);
            LocalizedSuperText amount = TextLine(rect, "Amount", TavernLocKeys.Plain, k_Ink, TextAnchor.UpperLeft, x, 0f, amountWidth);
            LocalizedSuperText steps = TextLine(rect, "Steps", TavernLocKeys.Plain, k_Note, TextAnchor.UpperRight, right, 0f, 100f);

            // Chosen: the gold corners of a highlighted station, so the choice reads by shape too.
            var selected = Rect(rect, "Selected", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(k_CardWidth + 4f, k_CardHeight + 4f));
            foreach (var (corner, at) in new[] { ("CornerTL", new Vector2(0f, 1f)), ("CornerTR", new Vector2(1f, 1f)), ("CornerBL", new Vector2(0f, 0f)), ("CornerBR", new Vector2(1f, 0f)) })
            {
                RectTransform c = Rect(selected, corner, at, at, Vector2.zero, new Vector2(4f, 4f));
                DungeonUI.AddImage(c, MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", corner), k_Gold);
            }
            selected.gameObject.SetActive(false);
            return new DishCard { button = button, group = group, icon = icon, name = name, detail = detail, amount = amount, steps = steps, selected = selected.gameObject, back = back };
        }

        // ------------------------------------------------------------------ Results

        static void BuildResults(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Results");
            RectTransform panel = DungeonUI.Panel(root, new Vector2(236f, 150f), Vector2.zero);
            DungeonUI.Title(panel, TavernLocKeys.ResultsTitle);
            LocalizedSuperText note = TextLine(panel, "Note", TavernLocKeys.ResultsClosedEarly, k_Note, TextAnchor.UpperCenter, 0f, 56f, 224f);
            var lines = new LocalizedSuperText[7];
            for (int i = 0; i < lines.Length; i++) lines[i] = TextLine(panel, $"Line{i + 1}", TavernLocKeys.ResultsServed, k_Ink, TextAnchor.UpperCenter, 0f, 44f - i * Line, 224f);
            LocalizedSuperText takings = TextLine(panel, "Takings", TavernLocKeys.ResultsTakings, new Color(0.45f, 0.3f, 0.05f), TextAnchor.UpperCenter, 0f, -32f, 224f);
            Button done = BottomButton(panel, "Done", TavernLocKeys.ResultsAgain, 0f, 140f, out LocalizedSuperText doneLabel);
            UiFeedbackContent.Commit(done);
            root.gameObject.AddComponent<EveningResultsScreen>().Configure(panel.gameObject, note, lines, takings, done, doneLabel);
            panel.gameObject.SetActive(false);
        }
    }
}
