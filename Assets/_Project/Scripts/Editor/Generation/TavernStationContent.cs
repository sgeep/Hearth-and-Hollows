using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4c step 3 content: dish icons on the recipes, the plate carried over a character's head, and the
    /// station panel (Grill, Tap, Chop) on the tavern's canvas.
    /// </summary>
    public static class TavernStationContent
    {
        const string k_UnlitSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>Which icon each recipe shows (the plate on the pass, in hand, and in a customer's bubble).</summary>
        static readonly (string recipe, string file, string sprite)[] k_DishIcons =
        {
            ("cellar_kebab", "DishIcons", "MeatSkewer"),
            ("shroom_skewer", "DishIcons", "GreenSkewer"),
            ("grilled_spider_leg", "DishIcons", "Drumstick"),
            ("cellar_stew", "DishIcons", "BrownStew"),
            ("offal_pottage", "DishIcons", "RedStew"),
            ("core_tonic", "PotionIcons", "BlueFlask"),
            ("gelbrew", "PotionIcons", "GreenFlask"),
        };

        public static void AssignDishIcons()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:RecipeDefinition", new[] { EditorPaths.Data }))
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var (id, file, sprite) in k_DishIcons)
                {
                    if (recipe == null || recipe.id != id) continue;
                    recipe.icon = MinifantasyImporter.Sprite(MinifantasySheets.CraftingAndProfessions, file, sprite);
                    EditorUtility.SetDirty(recipe);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static Material Unlit => AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);

        /// <summary>
        /// The plate over a character's head (4c decision 2): the dish icon in a small bubble, and an 8-pixel
        /// spill meter above it. Added to a character prefab's root (replacing any it had).
        /// </summary>
        public static CarryView AddCarryView(GameObject character)
        {
            Transform old = character.transform.Find("Carry");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Object.DestroyImmediate(character.GetComponent<CarryView>());

            var root = new GameObject("Carry").transform;
            root.SetParent(character.transform, false);
            // Clear of the head (about 1.4 tiles above the feet), at the same height as customers' bubbles.
            root.localPosition = new Vector3(0f, 2.1f, 0f);
            var sprites = new List<SpriteRenderer>
            {
                LookTestContent.AddSprite(root, "Back", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Bubble", "Body"), SortingLayers.Above, 4, Vector3.zero),
            };
            SpriteRenderer icon = LookTestContent.AddSprite(root, "Icon", null, SortingLayers.Above, 5, Vector3.zero);
            sprites.Add(icon);

            var meter = new GameObject("Spill").transform;
            meter.SetParent(root, false);
            // Above the bubble, so it never covers the carrier.
            meter.localPosition = new Vector3(0f, 0.875f, 0f);
            SpriteRenderer back = LookTestContent.AddSprite(meter, "Back", DungeonUI.Pixel(), SortingLayers.Above, 4, Vector3.zero);
            back.transform.localScale = new Vector3(10f, 3f, 1f);
            back.color = new Color(0.08f, 0.06f, 0.06f);
            var anchor = new GameObject("Anchor").transform;
            anchor.SetParent(meter, false);
            anchor.localPosition = new Vector3(-0.5f, 0f, 0f);
            SpriteRenderer fill = LookTestContent.AddSprite(anchor, "Fill", DungeonUI.Pixel(), SortingLayers.Above, 5, new Vector3(0.0625f, 0f, 0f));
            sprites.Add(back);
            sprites.Add(fill);
            if (Unlit != null) foreach (SpriteRenderer r in sprites) r.sharedMaterial = Unlit;

            var view = character.AddComponent<CarryView>();
            view.Configure(root.gameObject, icon, meter.gameObject, anchor, fill);
            root.gameObject.SetActive(false);
            return view;
        }

        // ------------------------------------------------------------------ station panel

        /// <summary>
        /// The panel over the room while cooking: a framed box near the bottom with the station's minigame.
        /// The chop board spans the share of the screen the pointer is mapped onto (ChopSettings).
        /// </summary>
        public static StationPanel BuildStationPanel(Canvas canvas)
        {
            Transform old = canvas.transform.Find("StationPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(canvas, "StationPanel");
            var panel = root.gameObject.AddComponent<StationPanel>();

            // Grill: a doneness meter with the golden band and a needle.
            RectTransform grill = Box(root, "Grill", TavernLocKeys.StationGrill, out LocalizedSuperText grillPrompt);
            LocalizedSuperText side = DungeonUI.Line(grill, "Side", TavernLocKeys.GrillSide, 9f);
            RectTransform meter = LookTestBuilder.UIRect(grill, "Meter", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -3f), new Vector2(150f, 8f));
            DungeonUI.AddImage(meter, DungeonUI.Pixel(), new Color(0.18f, 0.1f, 0.08f));
            RectTransform heat = Fill(meter, "Heat", new Color(0.55f, 0.2f, 0.1f));
            heat.anchorMin = new Vector2(0.8f, 0f);
            RectTransform band = Fill(meter, "Band", new Color(1f, 0.82f, 0.3f));
            RectTransform needle = LookTestBuilder.UIRect(meter, "Needle", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 12f));
            DungeonUI.AddImage(needle, DungeonUI.Pixel(), Color.white);
            panel.ConfigureGrill(grill.gameObject, side, grillPrompt, band, needle);

            // Tap: a glass that tilts, with beer, foam, the fill line and the band the head should end in.
            RectTransform tap = Box(root, "Tap", TavernLocKeys.StationTap, out LocalizedSuperText tapPrompt);
            RectTransform glass = LookTestBuilder.UIRect(tap, "Glass", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, -14f), new Vector2(18f, 26f));
            DungeonUI.AddImage(glass, DungeonUI.Pixel(), new Color(0.75f, 0.85f, 0.9f, 0.35f));
            RectTransform foamZone = Fill(glass, "FoamZone", new Color(1f, 0.82f, 0.3f, 0.35f));
            RectTransform liquid = Fill(glass, "Liquid", new Color(0.85f, 0.55f, 0.15f));
            RectTransform foam = Fill(glass, "Foam", new Color(0.97f, 0.95f, 0.88f));
            RectTransform line = Fill(glass, "Line", Color.white);
            line.sizeDelta = new Vector2(4f, 1f);
            panel.ConfigureTap(tap.gameObject, tapPrompt, glass, liquid, foam, line, foamZone);

            // Chop: the board (food icon, guide lines, cuts, knife) and the time left for this ingredient.
            // The board's anchors are screen fractions, so it sits in a full-screen group beside the box, not inside it.
            RectTransform chopGroup = LookTestBuilder.UIRect(root, "Chop", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            chopGroup.anchorMin = Vector2.zero;
            chopGroup.anchorMax = Vector2.one;
            chopGroup.sizeDelta = Vector2.zero;
            RectTransform chop = Box(chopGroup, "ChopBox", null, out LocalizedSuperText chopPrompt);
            LocalizedSuperText chopTitle = DungeonUI.Title(chop, TavernLocKeys.ChopTitle, rule: false);
            LocalizedSuperText chopProgress = DungeonUI.Line(chop, "Progress", TavernLocKeys.ChopProgress, 8f);
            RectTransform board = LookTestBuilder.UIRect(chopGroup, "Board", new Vector2(0.25f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 31f), new Vector2(0f, 14f));
            board.anchorMax = new Vector2(0.75f, 0f);
            DungeonUI.AddImage(board, DungeonUI.Pixel(), new Color(0.55f, 0.36f, 0.2f));
            RectTransform foodRect = LookTestBuilder.UIRect(board, "Food", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-3f, 0f), new Vector2(8f, 8f));
            Image food = DungeonUI.AddImage(foodRect, null, Color.white);
            var lines = new RectTransform[8];
            var cuts = new Image[8];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = LookTestBuilder.UIRect(board, $"Line{i}", new Vector2(0f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1f, 14f));
                DungeonUI.AddImage(lines[i], DungeonUI.Pixel(), new Color(0.98f, 0.92f, 0.75f, 0.85f));
                var cut = LookTestBuilder.UIRect(board, $"Cut{i}", new Vector2(0f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(2f, 14f));
                cuts[i] = DungeonUI.AddImage(cut, DungeonUI.Pixel(), Color.white);
            }
            RectTransform knife = LookTestBuilder.UIRect(board, "Knife", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(2f, 8f));
            DungeonUI.AddImage(knife, DungeonUI.Pixel(), new Color(0.85f, 0.88f, 0.92f));
            RectTransform timerBack = LookTestBuilder.UIRect(board, "TimerBack", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -2f), Vector2.zero);
            timerBack.anchorMax = new Vector2(1f, 0f);
            timerBack.sizeDelta = new Vector2(0f, 2f);
            DungeonUI.AddImage(timerBack, DungeonUI.Pixel(), new Color(0.15f, 0.1f, 0.08f));
            RectTransform timer = Fill(timerBack, "Timer", new Color(1f, 0.82f, 0.3f));
            panel.ConfigureChop(chopGroup.gameObject, board, food, lines, cuts, knife, timer, chopTitle, chopProgress, chopPrompt);

            // The Butcher Block (4f Checkpoint C): the part shown three times its size on a board, its cut lines as dots
            // (they colour as they're cut), the knife, and the time left. Like the chop board, it spans the pointer's share.
            RectTransform butcherGroup = LookTestBuilder.UIRect(root, "Butcher", Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            butcherGroup.anchorMin = Vector2.zero;
            butcherGroup.anchorMax = Vector2.one;
            butcherGroup.sizeDelta = Vector2.zero;
            RectTransform butcherBox = Box(butcherGroup, "ButcherBox", null, out LocalizedSuperText butcherPrompt);
            LocalizedSuperText butcherTitle = DungeonUI.Title(butcherBox, TavernLocKeys.ButcherPanelTitle, rule: false);
            // Between the box's title and its prompt line.
            RectTransform block = LookTestBuilder.UIRect(butcherGroup, "Board", new Vector2(0.3f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 33f), new Vector2(0f, 21f));
            block.anchorMax = new Vector2(0.7f, 0f);
            DungeonUI.AddImage(block, DungeonUI.Pixel(), new Color(0.55f, 0.36f, 0.2f));
            RectTransform partRect = LookTestBuilder.UIRect(block, "Part", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
            Image partImage = DungeonUI.AddImage(partRect, null, Color.white);
            const int butcherLines = 3, dotsPerLine = 7;
            var dots = new Image[butcherLines * dotsPerLine];
            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform dot = LookTestBuilder.UIRect(block, $"Dot{i}", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 2f));
                dots[i] = DungeonUI.AddImage(dot, DungeonUI.Pixel(), Color.white);
            }
            RectTransform blade = LookTestBuilder.UIRect(block, "Knife", new Vector2(0f, 1f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3f, 7f));
            DungeonUI.AddImage(blade, DungeonUI.Pixel(), new Color(0.85f, 0.88f, 0.92f));
            RectTransform butcherTimerBack = LookTestBuilder.UIRect(block, "TimerBack", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, -2f), Vector2.zero);
            butcherTimerBack.anchorMax = new Vector2(1f, 0f);
            butcherTimerBack.sizeDelta = new Vector2(0f, 2f);
            DungeonUI.AddImage(butcherTimerBack, DungeonUI.Pixel(), new Color(0.15f, 0.1f, 0.08f));
            RectTransform butcherTimer = Fill(butcherTimerBack, "Timer", new Color(1f, 0.82f, 0.3f));
            panel.ConfigureButcher(butcherGroup.gameObject, block, partImage, dots, dotsPerLine, blade, butcherTimer, butcherTitle, butcherPrompt);

            // Under every box: how to step away.
            LocalizedSuperText stepAway = LookTestBuilder.Text(root, "StepAway", TavernLocKeys.HintStepAway, 6f, DungeonUI.k_Light, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(240f, 12f));
            panel.ConfigureStepAway(stepAway);
            // The result flash: a wash over the box (every box sits in the same place).
            RectTransform flash = LookTestBuilder.UIRect(root, "Flash", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(256f, 66f));
            Image flashImage = DungeonUI.AddImage(flash, DungeonUI.Pixel(), new Color(1f, 1f, 1f, 0f));
            flashImage.raycastTarget = false;
            panel.ConfigureFlash(flashImage);
            return panel;
        }

        /// <summary>A framed box near the bottom of the screen, with a title (if any) and a prompt line at the bottom.</summary>
        static RectTransform Box(RectTransform root, string name, string titleKey, out LocalizedSuperText prompt)
        {
            RectTransform box = DungeonUI.Panel(root, new Vector2(256f, 66f), new Vector2(0f, -45f));
            box.name = name;
            if (titleKey != null) DungeonUI.Title(box, titleKey, rule: false);
            prompt = DungeonUI.Line(box, "Prompt", TavernLocKeys.HintStepAway, -23f);
            return box;
        }

        /// <summary>A coloured rect that fills its parent; the panel moves its anchors.</summary>
        static RectTransform Fill(RectTransform parent, string name, Color color)
        {
            RectTransform rect = LookTestBuilder.UIRect(parent, name, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            DungeonUI.AddImage(rect, DungeonUI.Pixel(), color);
            return rect;
        }
    }
}
