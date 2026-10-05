using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The day's screens on the tavern canvas: Morning, Prep, the service HUD, Results and Night. Classic UI Overhaul
    /// panels and slots, plain buttons, and Silver text on a 12-pixel line (<see cref="Line"/>), laid out against the
    /// font's measured widths. The canvas is never smaller than 320×180 (whole-pixel scaling), so the planning panels
    /// are 312×172; the HUD lives in the 48-pixel side margins the centred room leaves. A text hierarchy keeps
    /// titles, labels and amounts apart: titles in deep red, labels muted, amounts in ink or gold.
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
        static readonly Color k_Title = DungeonUI.k_Title;
        static readonly Color k_Label = DungeonUI.k_Label;
        static readonly Color k_Accent = DungeonUI.k_Accent;
        /// <summary>On the dark HUD: labels step back, numbers stand out.</summary>
        static readonly Color k_HudValue = new(1f, 0.9f, 0.6f);
        static readonly Color k_HudLabel = new(0.55f, 0.5f, 0.45f);

        /// <summary>One line of text: 9-pixel capitals, 2-pixel descenders and a pixel between lines.</summary>
        public const float Line = GameFonts.LinePixels;
        /// <summary>The planning panels: as large as the smallest canvas allows.</summary>
        static readonly Vector2 k_Panel = new(316f, 172f);
        const float k_PanelTop = 86f;
        /// <summary>The title row's top (and the title's), 7 pixels under the panel's edge.</summary>
        const float k_TitleTop = k_PanelTop - 7f;
        /// <summary>Two-line cards: a line each, exactly.</summary>
        const float k_CardWidth = 150f, k_CardHeight = 24f, k_CardPitch = 25f;
        /// <summary>The first card row's top: under the storeroom row.</summary>
        const float k_CardsTop = 41f;

        /// <summary>
        /// Removes the day's screens from the canvas and builds them again (HUD first, at the back). The controls
        /// line moves into the HUD, so it shows (and fades) when service starts, not over Morning or Night panels.
        /// </summary>
        public static void Rebuild(Canvas canvas)
        {
            UiFeedbackContent.Ensure(canvas);
            Transform controls = canvas.transform.Find("Controls") ?? canvas.transform.Find("TavernHud/Content/Controls");
            if (controls != null) controls.SetParent(canvas.transform, false);
            foreach (string name in new[] { "TavernHud", "Prep", "Results", "Morning", "Night", "Decorate" })
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
            BuildDecorate(canvas);
        }

        /// <summary>A text button anywhere (the plain parchment face, gold when selected).</summary>
        internal static Button SmallButton(RectTransform parent, string name, string key, Vector2 anchorAt, Vector2 position, float width, out LocalizedSuperText label)
        {
            RectTransform rect = LookTestBuilder.UIRect(parent, name, anchorAt, anchorAt, position, new Vector2(width, DungeonUI.ButtonHeight));
            Button button = DungeonUI.ButtonFace(rect);
            label = LookTestBuilder.Text(rect, "Label", key, 6f, k_Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        internal static LocalizedSuperText Label(RectTransform parent, string name, string key, float size, Color color, TextAnchor anchor, Vector2 anchorAt, Vector2 position, Vector2 box) =>
            LookTestBuilder.Text(parent, name, key, size, color, anchor, anchorAt, anchorAt, anchorAt, position, box);

        /// <summary>One line of body text whose box's top edge is at <paramref name="top"/> (parent-centre coordinates).</summary>
        static LocalizedSuperText TextLine(RectTransform parent, string name, string key, Color color, TextAnchor anchor, float x, float top, float width, int lines = 1)
        {
            var at = new Vector2(0.5f, 0.5f);
            var pivot = anchor switch
            {
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                _ => new Vector2(0f, 1f),
            };
            return LookTestBuilder.Text(parent, name, key, 6f, color, anchor, at, at, pivot, new Vector2(x, top), new Vector2(width, Line * lines));
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
            SmallButton(panel, name, key, new Vector2(0.5f, 0f), new Vector2(x, 8f), width, out label);

        /// <summary>The storeroom as one row of satchel-style slots under the title, with its label.</summary>
        static SatchelSlotView[] Storeroom(RectTransform panel, out LocalizedSuperText empty)
        {
            TextLine(panel, "StoreroomLabel", TavernLocKeys.PrepStoreroom, k_Label, TextAnchor.UpperLeft, -150f, 62f, 56f);
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

        /// <summary>The card grid: two columns of <see cref="k_CardWidth"/>, starting at <paramref name="top"/>.</summary>
        static Vector2 CardCentre(int index, float top) =>
            new((index % 2 == 0 ? -1f : 1f) * (k_CardWidth / 2f + 1f), top - k_CardHeight / 2f - (index / 2) * k_CardPitch);

        // ------------------------------------------------------------------ Morning

        static void BuildMorning(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Morning");
            RectTransform panel = DungeonUI.Panel(root, k_Panel, Vector2.zero);
            LocalizedSuperText title = DungeonUI.Title(panel, LoopLocKeys.MorningTitle);
            SatchelSlotView[] stock = Storeroom(panel, out LocalizedSuperText empty);

            // The delve meal: what to cook, or what was eaten.
            LocalizedSuperText meal = TextLine(panel, "Breakfast", LoopLocKeys.MorningBreakfast, k_Title, TextAnchor.UpperCenter, 0f, 40f, 296f);
            var cards = new DishCard[6];
            // The delve meal cards: the name with its station on the right, then the buff across the card.
            for (int i = 0; i < cards.Length; i++) cards[i] = Card(panel, i, CardCentre(i, 27f), 130f);

            // What today's delve starts with, from upgrades and delve meal: the tavern feeding the dungeon.
            LocalizedSuperText bonuses = TextLine(panel, "Bonuses", LoopLocKeys.MorningNoBonuses, new Color(0.3f, 0.2f, 0.45f), TextAnchor.UpperCenter, 0f, -49f, 296f);
            Button descend = BottomButton(panel, "Descend", LoopLocKeys.MorningDescend, 0f, 156f, out _);
            UiFeedbackContent.Commit(descend);
            Button decorate = BottomButton(panel, "Decorate", DecorateLocKeys.Button, 117f, 70f, out _);
            root.gameObject.AddComponent<MorningScreen>().Configure(panel.gameObject, title, stock, empty, cards, meal, bonuses, descend, decorate);
            panel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Night

        static void BuildNight(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Night");
            RectTransform panel = DungeonUI.Panel(root, k_Panel, Vector2.zero);
            LocalizedSuperText title = DungeonUI.Title(panel, LoopLocKeys.NightTitle);

            // How the day went, as a ledger in two columns: the day on the left (the delve, what came home, the
            // evening), the money and standing on the right (banked tonight, the purse, Renown). Labels step back.
            var labels = new LocalizedSuperText[6];
            var summary = new LocalizedSuperText[6];
            for (int i = 0; i < 3; i++)
            {
                float top = 62f - i * Line;
                labels[i] = TextLine(panel, $"SummaryLabel{i + 1}", LoopLocKeys.SummaryDelve, k_Label, TextAnchor.UpperLeft, -150f, top, 62f);
                summary[i] = TextLine(panel, $"Summary{i + 1}", LoopLocKeys.SummaryDelve, k_Ink, TextAnchor.UpperLeft, -86f, top, 102f);
                labels[i + 3] = TextLine(panel, $"SummaryLabel{i + 4}", LoopLocKeys.NightBanked, k_Label, TextAnchor.UpperLeft, 18f, top, 82f);
                summary[i + 3] = TextLine(panel, $"Summary{i + 4}", LoopLocKeys.NightBanked, k_Accent, TextAnchor.UpperRight, 152f, top, 52f);
            }

            // The upgrades, bought with banked Gold: a row each, with its price on a button.
            var rows = new UpgradeRow[3];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = Rect(panel, $"Upgrade{i + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 24f - i * 25f), new Vector2(300f, 24f), k_Card);
                LocalizedSuperText name = TextLine(row, "Name", LoopLocKeys.NightUpgradeLevel, k_Ink, TextAnchor.UpperLeft, -147f, 12f, 230f);
                LocalizedSuperText effect = TextLine(row, "Effect", LoopLocKeys.NightNextTime, k_Note, TextAnchor.UpperLeft, -147f, 0f, 230f);
                Button buy = SmallButton(row, "Buy", LoopLocKeys.NightBuyCost, new Vector2(1f, 0.5f), new Vector2(-4f, 0f), 60f, out LocalizedSuperText cost);
                ((RectTransform)buy.transform).pivot = new Vector2(1f, 0.5f);
                // A purchase has its own moment (the Night screen plays it when the buy goes through).
                Object.DestroyImmediate(buy.GetComponent<UiButtonFeedback>());
                rows[i] = new UpgradeRow { root = row.gameObject, name = name, effect = effect, buy = buy, cost = cost };
            }

            LocalizedSuperText saved = TextLine(panel, "Saved", LoopLocKeys.NightSaved, new Color(0.25f, 0.45f, 0.2f), TextAnchor.UpperLeft, -150f, -64f, 76f);
            Button sleep = BottomButton(panel, "Sleep", LoopLocKeys.NightSleep, 0f, 120f, out _);
            UiFeedbackContent.Commit(sleep);
            Button decorate = BottomButton(panel, "Decorate", DecorateLocKeys.Button, 110f, 70f, out _);
            root.gameObject.AddComponent<NightScreen>().Configure(panel.gameObject, title, labels, summary, null, rows, saved, sleep, decorate);
            saved.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Decorate (4f step 2)

        /// <summary>
        /// Decorate Mode over the room: a strip at the top (what you're doing; whether the doors could open), the piece
        /// under the cursor or carried with the reason it won't go, the controls at the bottom, and the storage and
        /// layout-check panels. No panel over the room itself: it's what you're looking at.
        /// </summary>
        static void BuildDecorate(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Decorate");
            RectTransform content = Rect(root, "Content", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.sizeDelta = Vector2.zero;
            var strip = new Color(0.08f, 0.06f, 0.06f, 0.8f);

            RectTransform top = Rect(content, "Top", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(320f, 14f), strip);
            Label(top, "Title", DecorateLocKeys.Title, 6f, k_Light, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(170f, 12f));
            LocalizedSuperText status = Label(top, "Status", DecorateLocKeys.StatusReady, 6f, new Color(0.55f, 0.9f, 0.5f), TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f), new Vector2(-5f, 0f), new Vector2(140f, 12f));
            RectTransform pieceBar = Rect(content, "PieceBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(320f, 13f),
                new Color(0.08f, 0.06f, 0.06f, 0.55f));
            LocalizedSuperText piece = Label(pieceBar, "Piece", DecorateLocKeys.Empty, 6f, k_Light, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(310f, 12f));
            RectTransform bottom = Rect(content, "Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320f, 2f * Line + 3f), strip);
            LocalizedSuperText controls = Label(bottom, "Controls", DecorateLocKeys.Controls, 6f, new Color(0.85f, 0.8f, 0.7f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(312f, 2f * Line));

            // Storage: what's owned and not placed; choosing one puts it on the cursor.
            RectTransform storage = DungeonUI.Panel(content, new Vector2(170f, 128f), Vector2.zero);
            storage.name = "Storage";
            DungeonUI.Title(storage, DecorateLocKeys.Storage);
            var rows = new Button[7];
            var labels = new LocalizedSuperText[rows.Length];
            for (int i = 0; i < rows.Length; i++)
                rows[i] = SmallButton(storage, $"Row{i + 1}", DecorateLocKeys.StorageRow, new Vector2(0.5f, 1f), new Vector2(0f, -22f - i * 15f), 150f, out labels[i]);
            LocalizedSuperText empty = Label(storage, "Empty", DecorateLocKeys.StorageEmpty, 6f, k_Note, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(150f, 12f));

            // The layout check: each problem in words, blocking ones in red; put it all back; done.
            RectTransform check = DungeonUI.Panel(content, new Vector2(220f, 112f), Vector2.zero);
            check.name = "Check";
            DungeonUI.Title(check, DecorateLocKeys.Check);
            var issues = new LocalizedSuperText[5];
            for (int i = 0; i < issues.Length; i++)
                issues[i] = Label(check, $"Issue{i + 1}", TavernLocKeys.Plain, 6f, k_Ink, TextAnchor.UpperLeft, new Vector2(0.5f, 1f), new Vector2(0f, -22f - i * Line),
                    new Vector2(204f, Line));
            Button putAllBack = BottomButton(check, "PutAllBack", DecorateLocKeys.PutAllBack, -48f, 104f, out _);
            Button done = BottomButton(check, "Done", DecorateLocKeys.Done, 64f, 70f, out _);
            UiFeedbackContent.Commit(done);

            root.gameObject.AddComponent<DecorateScreen>().Configure(content.gameObject, status, piece, controls, storage.gameObject, rows, labels, empty,
                check.gameObject, issues, putAllBack, done);
            storage.gameObject.SetActive(false);
            check.gameObject.SetActive(false);
            content.gameObject.SetActive(false);
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

            // Takings and Renown: a dim label over a bright number, with a gap between them (the margin is 48 pixels wide).
            LocalizedSuperText Stat(string name, string labelKey, float statTop)
            {
                LookTestBuilder.Text(root, name + "Label", labelKey, 6f, k_HudLabel, TextAnchor.UpperLeft, topLeft, topLeft, topLeft, new Vector2(3f, statTop), new Vector2(44f, Line));
                return LookTestBuilder.Text(root, name, TavernLocKeys.Plain, 6f, k_HudValue, TextAnchor.UpperLeft, topLeft, topLeft, topLeft, new Vector2(3f, statTop - Line), new Vector2(44f, Line));
            }
            LocalizedSuperText gold = Stat("Gold", TavernLocKeys.HudGold, -12f);
            LocalizedSuperText tips = Stat("Tips", TavernLocKeys.HudTips, -41f);
            LocalizedSuperText renown = Stat("Renown", TavernLocKeys.HudRenown, -70f);

            // Tonight's menu: a sold-out dish dims and gets a red bar (two cues, not only colour).
            var menu = new Image[3];
            var soldOut = new GameObject[3];
            for (int i = 0; i < menu.Length; i++)
            {
                RectTransform icon = Rect(root, $"Menu{i + 1}", topLeft, topLeft, new Vector2(3f + i * 13f, -102f), new Vector2(8f, 8f));
                menu[i] = DungeonUI.AddImage(icon, null, Color.white);
                soldOut[i] = Rect(icon, "SoldOut", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 2f), new Color(0.9f, 0.2f, 0.15f)).gameObject;
                soldOut[i].transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                soldOut[i].SetActive(false);
            }

            // The order rail, right margin: per order, the dish and its customer's patience, with the state under them.
            LookTestBuilder.Text(root, "Orders", TavernLocKeys.HudOrders, 6f, k_HudLabel, TextAnchor.UpperLeft, topRight, topRight, topRight, new Vector2(-3f, -3f), new Vector2(44f, Line));
            // A row per seat (eight with every seating upgrade), packed to fit the 180-pixel column.
            var rows = new RailRow[8];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = Rect(root, $"Order{i + 1}", topRight, topRight, new Vector2(-3f, -16f - i * 20f), new Vector2(44f, 20f));
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
            // The title row carries the debug fill key on the left and tonight's count on the right.
            LocalizedSuperText fill = TextLine(panel, "FillHint", TavernLocKeys.PrepFillKey, k_Note, TextAnchor.UpperLeft, -150f, k_TitleTop, 100f);
            LocalizedSuperText tonight = TextLine(panel, "Tonight", TavernLocKeys.PrepTonight, k_Title, TextAnchor.UpperRight, 150f, k_TitleTop, 100f);

            // The storeroom: what came back from the dungeon, as satchel-style slots.
            SatchelSlotView[] stock = Storeroom(panel, out LocalizedSuperText empty);

            // Tonight's menu: a card per dish.
            var cards = new DishCard[8];
            for (int i = 0; i < cards.Length; i++) cards[i] = Card(panel, i, CardCentre(i, k_CardsTop), 66f);
            // When nothing can be cooked: a banner over the (dimmed) cards, so closing for the night is the clear choice.
            RectTransform banner = Rect(panel, "NothingBanner", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(296f, Line + 6f), new Color(0.95f, 0.85f, 0.62f));
            LocalizedSuperText nothing = TextLine(banner, "Nothing", TavernLocKeys.PrepNothingCookable, new Color(0.6f, 0.15f, 0.1f), TextAnchor.UpperCenter, -32f, Line / 2f + 1f, 226f);
            // 4f: when the layout keeps the doors shut, the banner says why and offers Decorate Mode.
            Button decorate = SmallButton(banner, "Decorate", DecorateLocKeys.Button, new Vector2(1f, 0.5f), new Vector2(-3f, 0f), 60f, out _);

            Button staff = BottomButton(panel, "Staff", TavernLocKeys.PrepStaffJob, -107f, 80f, out LocalizedSuperText staffLabel);
            Button close = BottomButton(panel, "Close", LoopLocKeys.PrepClose, -6f, 114f, out _);
            Button open = BottomButton(panel, "Open", TavernLocKeys.PrepOpen, 101f, 92f, out _);
            UiFeedbackContent.Commit(open);
            root.gameObject.AddComponent<PrepScreen>().Configure(panel.gameObject, stock, empty, tonight, cards, staff, staffLabel, close, open, nothing, fill.gameObject, banner.gameObject,
                decorate);
            decorate.gameObject.SetActive(false);
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
            RectTransform iconRect = Rect(rect, "Icon", left, left, new Vector2(1f, 0f), new Vector2(8f, 8f));
            Image icon = DungeonUI.AddImage(iconRect, null, Color.white);
            // Every pixel counts: the longest pair ("offal pottage", "8 gold/bowl") leaves a two-pixel gap.
            float x = -k_CardWidth / 2f + 11f, right = k_CardWidth / 2f - 1f;
            LocalizedSuperText name = TextLine(rect, "Name", TavernLocKeys.Plain, k_Ink, TextAnchor.UpperLeft, x, Line, 98f);
            LocalizedSuperText detail = TextLine(rect, "Detail", TavernLocKeys.Plain, k_Accent, TextAnchor.UpperRight, right, Line, 70f);
            LocalizedSuperText amount = TextLine(rect, "Amount", TavernLocKeys.Plain, k_Ink, TextAnchor.UpperLeft, x, 0f, amountWidth);
            LocalizedSuperText steps = TextLine(rect, "Steps", TavernLocKeys.Plain, k_Note, TextAnchor.UpperRight, right, 0f, 100f);

            // Chosen: the gold corners of a highlighted station, so the choice reads by shape too.
            var selected = Rect(rect, "Selected", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(k_CardWidth + 4f, k_CardHeight + 2f));
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
            RectTransform panel = DungeonUI.Panel(root, new Vector2(236f, 172f), Vector2.zero);
            DungeonUI.Title(panel, TavernLocKeys.ResultsTitle);
            LocalizedSuperText note = TextLine(panel, "Note", TavernLocKeys.ResultsClosedEarly, k_Note, TextAnchor.UpperCenter, 0f, 63f, 224f);
            var labels = new LocalizedSuperText[7];
            var lines = new LocalizedSuperText[7];
            for (int i = 0; i < lines.Length; i++)
            {
                labels[i] = TextLine(panel, $"Label{i + 1}", TavernLocKeys.ResultsServed, k_Label, TextAnchor.UpperRight, -4f, 50f - i * Line, 104f);
                lines[i] = TextLine(panel, $"Line{i + 1}", TavernLocKeys.Plain, k_Ink, TextAnchor.UpperLeft, 4f, 50f - i * Line, 104f);
            }
            // The takings, set apart under a rule.
            RectTransform takingsRow = Rect(panel, "TakingsRow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(224f, 172f));
            Rect(takingsRow, "Rule", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(120f, 1f), new Color(k_Title.r, k_Title.g, k_Title.b, 0.35f));
            TextLine(takingsRow, "TakingsLabel", TavernLocKeys.ResultsTakings, k_Label, TextAnchor.UpperRight, -4f, -41f, 104f);
            LocalizedSuperText takings = TextLine(takingsRow, "Takings", TavernLocKeys.PrepValue, k_Accent, TextAnchor.UpperLeft, 4f, -41f, 104f);
            Button done = BottomButton(panel, "Done", TavernLocKeys.ResultsAgain, 0f, 144f, out LocalizedSuperText doneLabel);
            UiFeedbackContent.Commit(done);
            root.gameObject.AddComponent<EveningResultsScreen>().Configure(panel.gameObject, note, labels, lines, takingsRow.gameObject, takings, done, doneLabel);
            panel.gameObject.SetActive(false);
        }
    }
}
