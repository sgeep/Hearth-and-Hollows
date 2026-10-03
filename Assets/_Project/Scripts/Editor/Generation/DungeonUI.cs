using System.IO;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the dungeon's screens on the 320×180 canvas (1 canvas unit = 1 game pixel) with
    /// Minifantasy UI sprites and Super Text Mesh: the satchel-full hint and swap prompt, the exit hint,
    /// the death screen with the Lockbox, and the delve result. Generated UI: rebuilt on each update.
    /// </summary>
    public static class DungeonUI
    {
        const string k_PixelPath = EditorPaths.Art + "/UI/Pixel.png";
        internal static readonly Color k_Ink = new(0.25f, 0.16f, 0.1f);
        internal static readonly Color k_Light = new(0.95f, 0.92f, 0.85f);
        internal static readonly Color k_Mark = new(1f, 0.82f, 0.3f);
        /// <summary>Prompts sit above the satchel row.</summary>
        const float k_HintY = 34f;

        /// <summary>
        /// The dungeon HUD: Essence top left (Minifantasy bar, red and pulsing when low, flashing on a hit),
        /// the satchel bottom left (the same slots as the swap prompt), the harvest feed top right.
        /// Below every screen.
        /// </summary>
        public static void BuildHud(Canvas canvas)
        {
            RectTransform root = FullScreen(canvas, "Hud");
            root.SetAsFirstSibling();

            RectTransform trough = LookTestBuilder.UIRect(root, "Essence", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f, -4f), new Vector2(48f, 12f));
            AddImage(trough, UISprite("BarTrough"), Color.white);
            RectTransform fillRect = LookTestBuilder.UIRect(trough, "Fill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 6f));
            Image fill = AddImage(fillRect, UISprite("BarFillBlue"), Color.white, Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;
            RectTransform flashRect = LookTestBuilder.UIRect(trough, "Flash", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 6f));
            Image flash = AddImage(flashRect, Pixel(), new Color(1f, 1f, 1f, 0.8f));
            flash.enabled = false;
            var bar = trough.gameObject.AddComponent<Hearthdelve.UI.Hud.EssenceBar>();
            bar.Configure(fill);
            bar.ConfigureArt(UISprite("BarFillBlue"), UISprite("BarFillRed"), flash);
            LookTestBuilder.Text(root, "EssenceLabel", LocKeys.HudEssence, 6f, k_Light, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -17f), new Vector2(46f, 8f));

            RectTransform satchel = LookTestBuilder.UIRect(root, "Satchel", Vector2.zero, Vector2.zero, new Vector2(4f, 8f), new Vector2(94f, 17f));
            var slots = new SatchelSlotView[6];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = Slot(satchel, i, new Vector2(-40f + i * 16f, -8.5f), false, out Button button);
                // Display only.
                Object.DestroyImmediate(button);
                slots[i].GetComponent<Image>().raycastTarget = false;
            }
            satchel.gameObject.AddComponent<Hearthdelve.UI.Hud.SatchelHud>().Configure(slots);

            RectTransform feed = LookTestBuilder.UIRect(root, "HarvestFeed", Vector2.one, Vector2.one, new Vector2(-4f, -16f), new Vector2(130f, 36f));
            var lines = new Hearthdelve.UI.Hud.HarvestFeedLine[4];
            for (int i = 0; i < lines.Length; i++)
            {
                RectTransform line = LookTestBuilder.UIRect(feed, $"Line{i}", Vector2.one, Vector2.one, new Vector2(0f, -i * 9f), new Vector2(130f, 8f));
                var group = line.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                group.blocksRaycasts = false;
                RectTransform iconRect = LookTestBuilder.UIRect(line, "Icon", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
                LocalizedSuperText text = LookTestBuilder.Text(line, "Text", LocKeys.HarvestGot, 6f, k_Light, TextAnchor.MiddleRight,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-11f, 0f), new Vector2(118f, 8f));
                lines[i] = new Hearthdelve.UI.Hud.HarvestFeedLine { group = group, icon = AddImage(iconRect, null, Color.white), text = text };
            }
            feed.gameObject.AddComponent<Hearthdelve.UI.Hud.HarvestFeed>().Configure(lines);
        }

        /// <summary>
        /// Removes the dungeon HUD and screens from <paramref name="canvas"/> (and the look test's placeholder
        /// Essence bar) and builds them again.
        /// </summary>
        public static void RebuildScreens(Canvas canvas)
        {
            foreach (string name in new[] { "Hud", "SwapPrompt", "ExitHint", "DeathScreen", "DelveResult", "PH_EssenceBar", "EssenceLabel" })
            {
                Transform old = canvas.transform.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            // The look test's controls line fades so it doesn't sit over the satchel.
            Transform controls = canvas.transform.Find("Hint");
            if (controls != null && controls.GetComponent<Hearthdelve.UI.Debugging.FadeOutAfter>() == null)
                controls.gameObject.AddComponent<Hearthdelve.UI.Debugging.FadeOutAfter>();
            BuildHud(canvas);
            BuildSwapPrompt(canvas);
            BuildExitHint(canvas);
            BuildDeathScreen(canvas);
            BuildResultScreen(canvas);
        }

        /// <summary>A 1×1 white sprite for pips, bars and outlines, point filtered.</summary>
        public static Sprite Pixel()
        {
            if (!File.Exists(k_PixelPath))
            {
                EditorPaths.Ensure(Path.GetDirectoryName(k_PixelPath)?.Replace('\\', '/'));
                var texture = new Texture2D(1, 1);
                texture.SetPixel(0, 0, Color.white);
                File.WriteAllBytes(k_PixelPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(k_PixelPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(k_PixelPath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(k_PixelPath);
        }

        internal static Sprite UISprite(string name) => MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "ClassicUI", name);

        internal static Image AddImage(RectTransform rect, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        internal static RectTransform FullScreen(Canvas canvas, string name)
        {
            RectTransform root = LookTestBuilder.UIRect(canvas.transform, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            root.SetAsLastSibling();
            return root;
        }

        internal static RectTransform Panel(RectTransform root, Vector2 size, Vector2 offset)
        {
            RectTransform panel = LookTestBuilder.UIRect(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), offset, size);
            AddImage(panel, UISprite("Panel"), Color.white, Image.Type.Sliced);
            return panel;
        }

        internal static LocalizedSuperText Title(RectTransform panel, string key) =>
            LookTestBuilder.Text(panel, "Title", key, 7f, k_Ink, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(panel.sizeDelta.x - 14f, 10f));

        internal static LocalizedSuperText Line(RectTransform panel, string name, string key, float y, float height = 8f) =>
            LookTestBuilder.Text(panel, name, key, 6f, k_Ink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(panel.sizeDelta.x - 16f, height));

        internal static Button TextButton(RectTransform panel, string name, string key, Vector2 position, float width, out LocalizedSuperText label)
        {
            RectTransform rect = LookTestBuilder.UIRect(panel, name, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(width, 13f));
            Image image = AddImage(rect, UISprite("Bar"), Color.white, Image.Type.Sliced);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 1f, 0.75f);
            colors.fadeDuration = 0f;
            button.colors = colors;
            label = LookTestBuilder.Text(rect, "Label", key, 6f, k_Ink, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        /// <summary>
        /// One satchel slot (icon, count, quality pips, freshness bar), with an optional outline mark.
        /// Its bottom-centre sits at <paramref name="position"/> from the panel's centre.
        /// </summary>
        internal static SatchelSlotView Slot(RectTransform panel, int index, Vector2 position, bool withMark, out Button button)
        {
            Sprite pixel = Pixel();
            RectTransform slot = LookTestBuilder.UIRect(panel, $"Slot{index}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), position, new Vector2(14f, 17f));
            Image frame = AddImage(slot, UISprite("Slot"), Color.white);
            frame.raycastTarget = true;
            button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            ColorBlock colors = button.colors;
            colors.highlightedColor = colors.selectedColor = new Color(1f, 1f, 0.82f);
            colors.disabledColor = new Color(0.85f, 0.85f, 0.85f);
            colors.fadeDuration = 0f;
            button.colors = colors;

            RectTransform iconRect = LookTestBuilder.UIRect(slot, "Icon", new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 7f), new Vector2(8f, 8f));
            Image icon = AddImage(iconRect, null, Color.white);
            LocalizedSuperText count = LookTestBuilder.Text(slot, "Count", LocKeys.SlotCount, 5f, k_Light, TextAnchor.LowerRight,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(12f, 6f));
            var pips = new Image[4];
            for (int p = 0; p < pips.Length; p++)
            {
                RectTransform pip = LookTestBuilder.UIRect(slot, $"Pip{p}", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-3f + p * 2f, -2f), new Vector2(1f, 1f));
                pips[p] = AddImage(pip, pixel, Color.white);
            }
            RectTransform freshBack = LookTestBuilder.UIRect(slot, "Freshness", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(12f, 1f));
            Image back = AddImage(freshBack, pixel, new Color(0.15f, 0.1f, 0.08f));
            RectTransform fillRect = LookTestBuilder.UIRect(freshBack, "Fill", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fill = AddImage(fillRect, pixel, Color.green, Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;

            var view = slot.gameObject.AddComponent<SatchelSlotView>();
            view.Configure(frame, UISprite("Slot"), UISprite("SlotSelected"), icon, count, pips, back, fill);
            if (withMark)
            {
                // An outline one pixel outside the 14×14 slot: the Lockbox choice, by shape as well as colour.
                RectTransform mark = LookTestBuilder.UIRect(slot, "Mark", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(18f, 18f));
                foreach (var (min, max) in new[] { (new Vector2(0f, 0f), new Vector2(1f, 0f)), (new Vector2(0f, 1f), new Vector2(1f, 1f)), (new Vector2(0f, 0f), new Vector2(0f, 1f)), (new Vector2(1f, 0f), new Vector2(1f, 1f)) })
                {
                    RectTransform edge = LookTestBuilder.UIRect(mark, "Edge", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    edge.anchorMin = min;
                    edge.anchorMax = max;
                    edge.sizeDelta = new Vector2(min.x == max.x ? 1f : 0f, min.y == max.y ? 1f : 0f);
                    AddImage(edge, pixel, k_Mark);
                }
                view.SetMark(mark.gameObject);
            }
            return view;
        }

        static SatchelSlotView[] SlotRow(RectTransform panel, float bottom, bool withMarks, out Button[] buttons)
        {
            var slots = new SatchelSlotView[6];
            buttons = new Button[6];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = Slot(panel, i, new Vector2(-50f + i * 20f, bottom), withMarks, out buttons[i]);
            return slots;
        }

        static void Navigate(Button[] row, Selectable down)
        {
            for (int i = 0; i < row.Length; i++)
            {
                row[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? row[i - 1] : null,
                    selectOnRight = i < row.Length - 1 ? row[i + 1] : null,
                    selectOnDown = down,
                };
            }
        }

        internal static GameObject Hint(RectTransform root, string key, float y, out LocalizedSuperText text)
        {
            RectTransform hint = LookTestBuilder.UIRect(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(150f, 10f));
            AddImage(hint, Pixel(), new Color(0.05f, 0.04f, 0.06f, 0.75f));
            text = LookTestBuilder.Text(hint, "Text", key, 6f, k_Light, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, 0f));
            return hint.gameObject;
        }

        /// <summary>The satchel-full hint and the swap prompt.</summary>
        public static SwapPromptScreen BuildSwapPrompt(Canvas canvas)
        {
            RectTransform root = FullScreen(canvas, "SwapPrompt");
            GameObject hint = Hint(root, LocKeys.HudSatchelFull, k_HintY, out LocalizedSuperText hintText);

            RectTransform panel = Panel(root, new Vector2(184f, 92f), new Vector2(0f, 6f));
            Title(panel, LocKeys.SwapTitle);
            RectTransform incoming = LookTestBuilder.UIRect(panel, "Incoming", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-78f, -24f), new Vector2(8f, 8f));
            Image incomingIcon = AddImage(incoming, null, Color.white);
            LocalizedSuperText subtitle = LookTestBuilder.Text(panel, "Subtitle", LocKeys.SwapSubtitle, 6f, k_Ink, TextAnchor.UpperLeft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-72f, -19f), new Vector2(152f, 16f));
            SatchelSlotView[] slots = SlotRow(panel, -10f, false, out Button[] buttons);
            LocalizedSuperText detail = Line(panel, "Detail", LocKeys.SlotEmpty, -22f);
            Button leave = TextButton(panel, "LeaveIt", LocKeys.SwapCancel, new Vector2(0f, 7f), 56f, out _);
            Navigate(buttons, leave);
            leave.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = buttons[2] };

            var screen = root.gameObject.AddComponent<SwapPromptScreen>();
            screen.Configure(panel.gameObject, subtitle, incomingIcon, detail, slots, leave, hint, hintText);
            panel.gameObject.SetActive(false);
            hint.SetActive(false);
            return screen;
        }

        /// <summary>"Press E to climb back to the tavern", at the way out.</summary>
        public static ExitHintView BuildExitHint(Canvas canvas)
        {
            RectTransform root = FullScreen(canvas, "ExitHint");
            GameObject hint = Hint(root, LoopLocKeys.HudExit, k_HintY, out LocalizedSuperText text);
            var view = root.gameObject.AddComponent<ExitHintView>();
            view.Configure(hint, text);
            hint.SetActive(false);
            return view;
        }

        /// <summary>The death screen: why the delve ended, the satchel, and the Lockbox choice.</summary>
        public static DeathScreen BuildDeathScreen(Canvas canvas)
        {
            RectTransform root = FullScreen(canvas, "DeathScreen");
            // One window shown and hidden as a whole: a shade over the dungeon, and the panel on it.
            RectTransform window = LookTestBuilder.UIRect(root, "Window", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            window.anchorMin = Vector2.zero;
            window.anchorMax = Vector2.one;
            RectTransform shade = LookTestBuilder.UIRect(window, "Shade", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            shade.anchorMin = Vector2.zero;
            shade.anchorMax = Vector2.one;
            AddImage(shade, Pixel(), new Color(0.03f, 0.02f, 0.04f, 0.55f));
            RectTransform panel = Panel(window, new Vector2(204f, 104f), new Vector2(0f, 4f));
            Title(panel, LocKeys.DeathTitle);
            LocalizedSuperText subtitle = LookTestBuilder.Text(panel, "Subtitle", LocKeys.DeathSubtitle, 6f, k_Ink, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(188f, 22f));
            SatchelSlotView[] slots = SlotRow(panel, -14f, true, out Button[] buttons);
            LocalizedSuperText chosen = Line(panel, "Chosen", LocKeys.DeathSelectedNone, -26f);
            Button keepNothing = TextButton(panel, "KeepNothing", LocKeys.DeathKeepNothing, new Vector2(-46f, 7f), 76f, out _);
            Button confirm = TextButton(panel, "Confirm", LocKeys.DeathConfirm, new Vector2(46f, 7f), 92f, out _);
            Navigate(buttons, confirm);
            confirm.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = buttons[3], selectOnLeft = keepNothing };
            keepNothing.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = buttons[1], selectOnRight = confirm };

            var screen = root.gameObject.AddComponent<DeathScreen>();
            screen.Configure(window.gameObject, subtitle, chosen, slots, confirm, keepNothing);
            window.gameObject.SetActive(false);
            return screen;
        }

        /// <summary>The delve's result: how it ended, what came home, what was lost.</summary>
        public static DelveResultScreen BuildResultScreen(Canvas canvas)
        {
            RectTransform root = FullScreen(canvas, "DelveResult");
            RectTransform panel = Panel(root, new Vector2(184f, 84f), new Vector2(0f, 4f));
            LocalizedSuperText title = Title(panel, LocKeys.ResultTitleExtracted);
            SatchelSlotView[] slots = SlotRow(panel, 0f, false, out Button[] buttons);
            // Display only: nothing to choose here.
            foreach (Button button in buttons)
            {
                button.interactable = false;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            LocalizedSuperText summary = Line(panel, "Summary", LocKeys.ResultSummary, -14f);
            Button proceed = TextButton(panel, "Continue", LocKeys.ResultDelveAgain, new Vector2(0f, 7f), 84f, out LocalizedSuperText proceedLabel);
            proceed.navigation = new Navigation { mode = Navigation.Mode.None };

            var screen = root.gameObject.AddComponent<DelveResultScreen>();
            screen.Configure(panel.gameObject, title, summary, slots, proceed, proceedLabel);
            panel.gameObject.SetActive(false);
            return screen;
        }
    }
}
