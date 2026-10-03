using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using Hearthdelve.UI.Tavern;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The evening's screens on the tavern canvas (4c step 4): Prep, the service HUD and Results. Classic UI
    /// Overhaul panels, buttons and slots, as in the dungeon. The HUD lives in the side margins the centred
    /// room leaves (48 px each side at 16:9); Prep and Results are panels with the room in view around them.
    /// </summary>
    public static class TavernScreens
    {
        static readonly Color k_Ink = DungeonUI.k_Ink;
        static readonly Color k_Light = DungeonUI.k_Light;
        static readonly Color k_Gold = DungeonUI.k_Mark;
        static readonly Color k_Warning = new(1f, 0.55f, 0.35f);

        /// <summary>Removes the evening's screens from the canvas and builds them again (HUD first, at the back).</summary>
        public static void Rebuild(Canvas canvas)
        {
            foreach (string name in new[] { "TavernHud", "Prep", "Results" })
            {
                Transform old = canvas.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            BuildHud(canvas).SetAsFirstSibling();
            BuildPrep(canvas);
            BuildResults(canvas);
        }

        static LocalizedSuperText Label(RectTransform parent, string name, string key, float size, Color color, TextAnchor anchor, Vector2 anchorAt, Vector2 position, Vector2 box) =>
            LookTestBuilder.Text(parent, name, key, size, color, anchor, anchorAt, anchorAt, anchorAt, position, box);

        static RectTransform Rect(RectTransform parent, string name, Vector2 anchorAt, Vector2 pivot, Vector2 position, Vector2 size, Color? color = null)
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

        // ------------------------------------------------------------------ HUD

        static RectTransform BuildHud(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "TavernHud");
            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);

            // The clock: a bar that runs down as the evening goes.
            RectTransform clock = Rect(root, "Clock", topLeft, topLeft, new Vector2(4f, -5f), new Vector2(40f, 4f), new Color(0.12f, 0.08f, 0.06f));
            RectTransform clockFill = Fill(clock, "Fill", new Color(1f, 0.78f, 0.35f));
            LocalizedSuperText lastOrders = Label(root, "LastOrders", TavernLocKeys.HudLastOrders, 6f, k_Warning, TextAnchor.UpperLeft, topLeft, new Vector2(4f, -11f), new Vector2(42f, 8f));

            LocalizedSuperText gold = Label(root, "Gold", TavernLocKeys.HudGold, 6f, k_Light, TextAnchor.UpperLeft, topLeft, new Vector2(4f, -20f), new Vector2(42f, 8f));
            LocalizedSuperText tips = Label(root, "Tips", TavernLocKeys.HudTips, 6f, k_Light, TextAnchor.UpperLeft, topLeft, new Vector2(4f, -28f), new Vector2(42f, 8f));
            LocalizedSuperText renown = Label(root, "Renown", TavernLocKeys.HudRenown, 6f, k_Light, TextAnchor.UpperLeft, topLeft, new Vector2(4f, -36f), new Vector2(42f, 8f));

            // Tonight's menu: a sold-out dish dims and gets a red bar (two cues, not only colour).
            var menu = new Image[3];
            var soldOut = new GameObject[3];
            for (int i = 0; i < menu.Length; i++)
            {
                RectTransform icon = Rect(root, $"Menu{i + 1}", topLeft, topLeft, new Vector2(4f + i * 12f, -50f), new Vector2(8f, 8f));
                menu[i] = DungeonUI.AddImage(icon, null, Color.white);
                soldOut[i] = Rect(icon, "SoldOut", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 2f), new Color(0.9f, 0.2f, 0.15f)).gameObject;
                soldOut[i].transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                soldOut[i].SetActive(false);
            }

            // The order rail, right margin.
            Label(root, "Orders", TavernLocKeys.HudOrders, 6f, k_Light, TextAnchor.UpperLeft, topRight, new Vector2(-4f, -3f), new Vector2(40f, 8f));
            var rows = new RailRow[10];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = Rect(root, $"Order{i + 1}", topRight, topRight, new Vector2(-4f, -13f - i * 11f), new Vector2(40f, 10f));
                RectTransform iconRect = Rect(row, "Icon", new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(8f, 8f));
                Image icon = DungeonUI.AddImage(iconRect, null, Color.white);
                LocalizedSuperText state = Label(row, "State", TavernLocKeys.TicketQueued, 6f, k_Light, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(10f, 0f), new Vector2(30f, 8f));
                RectTransform patienceBack = Rect(row, "PatienceBack", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 0f), new Vector2(30f, 1f), new Color(0.12f, 0.08f, 0.06f));
                RectTransform patience = Fill(patienceBack, "Patience", Color.white);
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
            // 284 wide: it fits a 16:10 screen too (the canvas is 288 wide there).
            RectTransform panel = DungeonUI.Panel(root, new Vector2(284f, 152f), new Vector2(0f, 2f));
            DungeonUI.Title(panel, TavernLocKeys.PrepTitle);
            var centre = new Vector2(0.5f, 0.5f);

            // The storeroom: what came back from the dungeon, as satchel-style slots.
            Label(panel, "StoreroomLabel", TavernLocKeys.PrepStoreroom, 6f, k_Ink, TextAnchor.UpperLeft, centre, new Vector2(-96f, 52f), new Vector2(78f, 8f));
            var stock = new SatchelSlotView[15];
            for (int i = 0; i < stock.Length; i++)
            {
                stock[i] = DungeonUI.Slot(panel, i, new Vector2(-128f + (i % 5) * 16f, 24f - (i / 5) * 20f), false, out Button button);
                Object.DestroyImmediate(button);
                stock[i].GetComponent<Image>().raycastTarget = false;
            }
            LocalizedSuperText empty = Label(panel, "StoreroomEmpty", TavernLocKeys.PrepStoreroomEmpty, 6f, k_Ink, TextAnchor.UpperLeft, centre, new Vector2(-96f, 36f), new Vector2(78f, 24f));

            // Tonight's menu: one card per dish.
            LocalizedSuperText tonight = Label(panel, "Tonight", TavernLocKeys.PrepTonight, 6f, k_Ink, TextAnchor.UpperCenter, centre, new Vector2(44f, 52f), new Vector2(186f, 8f));
            var cards = new DishCard[8];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = Card(panel, i, new Vector2(-3f + (i % 2) * 94f, 34f - (i / 2) * 25f));
            // Shown in place of the menu count when nothing can be cooked: closing for the night is the honest choice.
            LocalizedSuperText nothing = Label(panel, "Nothing", TavernLocKeys.PrepNothingCookable, 6f, new Color(0.6f, 0.15f, 0.1f), TextAnchor.UpperCenter, centre, new Vector2(44f, 52f), new Vector2(186f, 8f));
            Button staff = DungeonUI.TextButton(panel, "Staff", TavernLocKeys.PrepStaffJob, new Vector2(-95f, 6f), 80f, out LocalizedSuperText staffLabel);
            Button close = DungeonUI.TextButton(panel, "Close", LoopLocKeys.PrepClose, new Vector2(-5f, 6f), 80f, out _);
            Button open = DungeonUI.TextButton(panel, "Open", TavernLocKeys.PrepOpen, new Vector2(90f, 6f), 80f, out _);

            // Debug builds: in the strip above the panel, clear of the controls line at the bottom.
            LocalizedSuperText fill = Label(root, "FillHint", TavernLocKeys.PrepFillKey, 6f, k_Light, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(4f, -1f), new Vector2(150f, 8f));
            root.gameObject.AddComponent<PrepScreen>().Configure(panel.gameObject, stock, empty, tonight, cards, staff, staffLabel, close, open, nothing, fill.gameObject);
            // The screen object stays active (it listens); the panel shows and hides.
            panel.gameObject.SetActive(false);
            fill.transform.SetParent(panel, true);
        }

        /// <summary>A dish card: its icon, name, value and how many there are, and how it's prepared; gold corners when chosen.</summary>
        static DishCard Card(RectTransform panel, int index, Vector2 centre)
        {
            // A plain tile a shade darker than the panel: the ornate bar border left no room for three lines.
            RectTransform rect = Rect(panel, $"Dish{index + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centre, new Vector2(92f, 24f));
            Image back = DungeonUI.AddImage(rect, DungeonUI.Pixel(), new Color(0.82f, 0.66f, 0.46f));
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

            var left = new Vector2(0f, 0.5f);
            RectTransform iconRect = Rect(rect, "Icon", left, left, new Vector2(4f, 0f), new Vector2(8f, 8f));
            Image icon = DungeonUI.AddImage(iconRect, null, Color.white);
            LocalizedSuperText name = Label(rect, "Name", TavernLocKeys.Plain, 6f, k_Ink, TextAnchor.UpperLeft, left, new Vector2(15f, 7f), new Vector2(76f, 7f));
            LocalizedSuperText detail = Label(rect, "Detail", TavernLocKeys.Plain, 6f, k_Ink, TextAnchor.UpperLeft, left, new Vector2(15f, 0f), new Vector2(76f, 7f));
            LocalizedSuperText steps = Label(rect, "Steps", TavernLocKeys.Plain, 6f, new Color(0.45f, 0.3f, 0.2f), TextAnchor.UpperLeft, left, new Vector2(15f, -7f), new Vector2(76f, 7f));

            // Chosen: the gold corners of a highlighted station, so the choice reads by shape too.
            var selected = Rect(rect, "Selected", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 28f));
            foreach (var (corner, at) in new[] { ("CornerTL", new Vector2(0f, 1f)), ("CornerTR", new Vector2(1f, 1f)), ("CornerBL", new Vector2(0f, 0f)), ("CornerBR", new Vector2(1f, 0f)) })
            {
                RectTransform c = Rect(selected, corner, at, at, Vector2.zero, new Vector2(4f, 4f));
                DungeonUI.AddImage(c, MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", corner), k_Gold);
            }
            selected.gameObject.SetActive(false);
            return new DishCard { button = button, group = group, icon = icon, name = name, detail = detail, steps = steps, selected = selected.gameObject, back = back };
        }

        // ------------------------------------------------------------------ Results

        static void BuildResults(Canvas canvas)
        {
            RectTransform root = DungeonUI.FullScreen(canvas, "Results");
            RectTransform panel = DungeonUI.Panel(root, new Vector2(176f, 128f), new Vector2(0f, 4f));
            DungeonUI.Title(panel, TavernLocKeys.ResultsTitle);
            LocalizedSuperText note = DungeonUI.Line(panel, "Note", TavernLocKeys.ResultsClosedEarly, 38f);
            var lines = new LocalizedSuperText[7];
            for (int i = 0; i < lines.Length; i++) lines[i] = DungeonUI.Line(panel, $"Line{i + 1}", TavernLocKeys.ResultsServed, 27f - i * 9f);
            LocalizedSuperText takings = DungeonUI.Line(panel, "Takings", TavernLocKeys.ResultsTakings, -40f, 10f);
            takings.GetComponent<SuperTextMesh>().size = 7f;
            Button done = DungeonUI.TextButton(panel, "Done", TavernLocKeys.ResultsAgain, new Vector2(0f, 6f), 120f, out _);
            root.gameObject.AddComponent<EveningResultsScreen>().Configure(panel.gameObject, note, lines, takings, done);
            panel.gameObject.SetActive(false);
        }
    }
}
