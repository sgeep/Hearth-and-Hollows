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
    /// Minifantasy UI sprites and Super Text Mesh: for now the satchel-full hint and the swap prompt.
    /// </summary>
    public static class DungeonUI
    {
        const string k_PixelPath = EditorPaths.Art + "/UI/Pixel.png";
        static readonly Color k_Ink = new(0.25f, 0.16f, 0.1f);
        static readonly Color k_Light = new(0.95f, 0.92f, 0.85f);

        /// <summary>A 1×1 white sprite for pips and bars, point filtered.</summary>
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

        static Sprite UISprite(string name) => MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "ClassicUI", name);

        static Image AddImage(RectTransform rect, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>The satchel-full hint and the swap prompt, on <paramref name="canvas"/>.</summary>
        public static SwapPromptScreen BuildSwapPrompt(Canvas canvas)
        {
            Sprite pixel = Pixel();
            RectTransform root = LookTestBuilder.UIRect(canvas.transform, "SwapPrompt", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = Vector2.zero;
            root.SetAsLastSibling();

            // The hint, above the bottom line.
            RectTransform hint = LookTestBuilder.UIRect(root, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(150f, 10f));
            AddImage(hint, pixel, new Color(0.05f, 0.04f, 0.06f, 0.75f));
            LocalizedSuperText hintText = LookTestBuilder.Text(hint, "Text", LocKeys.HudSatchelFull, 6f, k_Light, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, 0f));

            // The prompt.
            RectTransform panel = LookTestBuilder.UIRect(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(184f, 92f));
            AddImage(panel, UISprite("Panel"), Color.white, Image.Type.Sliced);
            LookTestBuilder.Text(panel, "Title", LocKeys.SwapTitle, 7f, k_Ink, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(170f, 10f));
            RectTransform incoming = LookTestBuilder.UIRect(panel, "Incoming", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-78f, -24f), new Vector2(8f, 8f));
            Image incomingIcon = AddImage(incoming, null, Color.white);
            LocalizedSuperText subtitle = LookTestBuilder.Text(panel, "Subtitle", LocKeys.SwapSubtitle, 6f, k_Ink, TextAnchor.UpperLeft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-72f, -19f), new Vector2(152f, 16f));

            var slots = new SatchelSlotView[6];
            var buttons = new Button[6];
            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform slot = LookTestBuilder.UIRect(panel, $"Slot{i}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(-50f + i * 20f, -10f), new Vector2(14f, 17f));
                Image frame = AddImage(slot, UISprite("Slot"), Color.white);
                frame.raycastTarget = true;
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                ColorBlock colors = button.colors;
                colors.highlightedColor = colors.selectedColor = new Color(1f, 1f, 0.82f);
                colors.fadeDuration = 0f;
                button.colors = colors;
                buttons[i] = button;

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

                slots[i] = slot.gameObject.AddComponent<SatchelSlotView>();
                slots[i].Configure(frame, UISprite("Slot"), UISprite("SlotSelected"), icon, count, pips, back, fill);
            }

            LocalizedSuperText detail = LookTestBuilder.Text(panel, "Detail", LocKeys.SlotEmpty, 6f, k_Ink, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(170f, 8f));

            RectTransform leave = LookTestBuilder.UIRect(panel, "LeaveIt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(56f, 13f));
            Image leaveImage = AddImage(leave, UISprite("Bar"), Color.white, Image.Type.Sliced);
            leaveImage.raycastTarget = true;
            var leaveButton = leave.gameObject.AddComponent<Button>();
            leaveButton.targetGraphic = leaveImage;
            ColorBlock leaveColors = leaveButton.colors;
            leaveColors.highlightedColor = leaveColors.selectedColor = new Color(1f, 1f, 0.75f);
            leaveColors.fadeDuration = 0f;
            leaveButton.colors = leaveColors;
            LookTestBuilder.Text(leave, "Label", LocKeys.SwapCancel, 6f, k_Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Left/right along the slots, down to "Leave it" and back up to the slot above it.
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? buttons[i - 1] : null,
                    selectOnRight = i < buttons.Length - 1 ? buttons[i + 1] : null,
                    selectOnDown = leaveButton,
                };
            }
            leaveButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = buttons[2] };

            var screen = root.gameObject.AddComponent<SwapPromptScreen>();
            screen.Configure(panel.gameObject, subtitle, incomingIcon, detail, slots, leaveButton, hint.gameObject, hintText);
            panel.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            return screen;
        }
    }
}
