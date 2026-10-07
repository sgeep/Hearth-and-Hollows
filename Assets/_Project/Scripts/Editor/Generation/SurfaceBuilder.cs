using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using Hearthdelve.UI.Typography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The surface day (4h Checkpoint A), applied in place: the clock's tuning asset; the clock in Boot; Kariaston (generated once,
    /// its gameplay objects maintained); and in Tally Ho! (through the tavern updater) the surface areas, the camera that follows
    /// the keeper outside, the front door, the daytime places (the menu board, the storeroom shelves, Phi's portrait upstairs),
    /// five o'clock, and the clock's face and the Prep question on the tavern canvas. Idempotent.
    /// </summary>
    public static class SurfaceBuilder
    {
        public const string ClockConfigPath = EditorPaths.Config + "/SurfaceClockConfig.asset";
        const string k_Surface = "Surface";

        /// <summary>The tavern cells the daytime places stand on, kept clear of furniture: the menu board by the door, its step, the storeroom shelves.</summary>
        public static readonly Vector2Int[] TavernReserved = { new(15, 2), new(15, 3) };
        /// <summary>The menu board and the storeroom shelves (area tiles): solid fixtures the layout check walks round.</summary>
        public static readonly Rect[] TavernFixtures = { new(15f, 2f, 1f, 1f) };
        /// <summary>Fixtures given up since (the storeroom shelves became furniture after the Checkpoint A playtest).</summary>
        static readonly Rect[] k_RetiredFixtures = { new(25f, 9f, 1f, 1f) };
                /// <summary>Phi's portrait on the upstairs room's back wall (area tiles): wall decor can't hang behind it.</summary>
        public static readonly Rect PortraitWall = new(7f, 10f, 3f, 3f);

        [MenuItem("Hearthdelve/Generate/Update Surface (4h)", priority = 4)]
        public static void UpdateSurface()
        {
            ClockConfig();
            // The village's sheets (the garden's crops and action icons among them) are imported first, then its data.
            MinifantasyImporter.Import(KariastonSheets.Sheets());
            GardenContent.Build();
            KariastonBuilder.Ensure(rebuildApproved: false);
            UpdateBoot();
            // The tavern updater adds Tally Ho!'s side (ApplyToTavern) with everything else it maintains.
            TavernBuilder.UpdateTavern();
            ProjectConfigurator.SetBuildOrder(BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] Surface updated.");
        }

        public static void UpdateSurfaceBatch()
        {
            try
            {
                UpdateSurface();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>The clock's tuning (created once; its values are then tuned on the asset).</summary>
        public static SurfaceClockConfig ClockConfig() =>
            LookTestContent.CreateOrUpdate<SurfaceClockConfig>(ClockConfigPath, c =>
            {
                if (c.settings.realSecondsPerGameMinute <= 0f) c.settings = SurfaceClockSettings.Default;
                // Once: an earlier default day (35 minutes, then 12) becomes today's, unless the asset has been tuned since.
                foreach (float earlier in new[] { 35f * 60f / 540f, 35f / 3f * 60f / 540f })
                    if (Mathf.Abs(c.settings.realSecondsPerGameMinute - earlier) < 0.01f)
                        c.settings.realSecondsPerGameMinute = SurfaceClockSettings.Default.realSecondsPerGameMinute;
            });

        /// <summary>Boot, in place: the one surface clock beside GameFlow.</summary>
        static void UpdateBoot()
        {
            var scene = EditorSceneManager.OpenScene(BootBuilder.BootScene, OpenSceneMode.Single);
            GameFlow flow = Object.FindAnyObjectByType<GameFlow>(FindObjectsInactive.Include);
            if (flow == null) throw new InvalidOperationException("Boot has no GameFlow.");
            SurfaceTime time = flow.GetComponent<SurfaceTime>() ?? flow.gameObject.AddComponent<SurfaceTime>();
            time.Configure(ClockConfig());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ Tally Ho! (called by the tavern updater, scene open)

        internal static void ApplyToTavern(Canvas ui)
        {
            foreach (Transform old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Where(t => t.parent == null && t.name == k_Surface).ToList())
                Object.DestroyImmediate(old.gameObject);
            Transform surface = new GameObject(k_Surface).transform;

            Light2D ambient = GameObject.Find("Lights").transform.Find("Ambient").GetComponent<Light2D>();
            PropertyArea tavern = Area(PropertyArea.TavernId), upstairs = Area(PropertyArea.GuestRoomId);
            SurfaceArea tavernSurface = SurfaceOf(tavern, isDefault: true, ambient);
            SurfaceOf(upstairs, isDefault: false, ambient);
            upstairs.SetFixtures(PortraitWall);
            // The menu board and the storeroom shelves stand on the floor: the layout check walks round them, like the stairs.
            tavern.SetFixtures(tavern.Fixtures.Where(f => !TavernFixtures.Contains(f) && !k_RetiredFixtures.Contains(f)).Concat(TavernFixtures).ToArray());

            GameObject camera = GameObject.Find(TavernView.CameraName);
            if (camera.GetComponent<SurfaceCamera>() == null) camera.AddComponent<SurfaceCamera>();

            // The front door, inside: just above the threshold. Out into Kariaston, and back to a step inside it.
            var door = new GameObject("Front Door");
            door.transform.SetParent(surface, false);
            door.transform.position = new Vector2(TavernBuilder.DoorColumn + 0.5f, TavernBuilder.FloorBottom + 0.22f);
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.4f, 0.7f);
            door.AddComponent<SurfaceDoor>().Configure(SurfaceDoor.FrontInside, SurfaceDoor.FrontOutside, tavernSurface, new Vector2(0f, 1.3f),
                through: Vector2.down);

            Transform places = new GameObject("Daytime Places").transform;
            places.SetParent(surface, false);
            // The menu board, standing just inside the door: the evening begins here, when the player says.
            Fixture(places, "Menu Board", Sprite(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "MenuBoard"), new Vector2(15.5f, 2.05f),
                TavernInteractableKind.MenuBoard, SurfaceLocKeys.MenuBoard, new Vector2(0f, 1.1f), solid: new Vector2(0.8f, 0.4f));
            // The storeroom shelves are furniture now (FurnitureContent): placed in the room, movable in Decorate Mode.
            // Phi's portrait, upstairs, on the wall by the bed the keeper wakes in.
            Vector2 portrait = upstairs.Origin + new Vector2(PortraitWall.center.x, PortraitWall.yMin);
            DaytimeFixture look = Fixture(places, "Phi's Portrait", MinifantasyImporter.Sprites(MinifantasySheets.Portraits, "PhiFramed").Values.First(), portrait,
                TavernInteractableKind.Inspect, SurfaceLocKeys.LookPortrait, new Vector2(0f, -0.6f), solid: Vector2.zero, wall: true);
            look.Configure(SurfaceConversations.PhiPortrait);

            GameObject service = GameObject.Find("Service");
            if (service.GetComponent<FiveOClock>() == null) service.AddComponent<FiveOClock>();

            BuildClockFace(ui);
            BuildPrepConfirm(ui);
            BuildGardenPanel(ui);
        }

        static PropertyArea Area(string id) =>
            Object.FindObjectsByType<PropertyArea>(FindObjectsInactive.Include).FirstOrDefault(a => a.Id == id)
            ?? throw new InvalidOperationException($"The tavern scene has no '{id}' area.");

        static SurfaceArea SurfaceOf(PropertyArea area, bool isDefault, Light2D ambient)
        {
            SurfaceArea surface = area.GetComponent<SurfaceArea>() ?? area.gameObject.AddComponent<SurfaceArea>();
            surface.Configure(area.Id, indoors: true, isDefault, SurfaceCameraMode.Hold, area.CameraPoint, default, ambient);
            return surface;
        }

        static Sprite Sprite(string pack, string file, string name) => MinifantasyImporter.Sprite(pack, file, name);

        static DaytimeFixture Fixture(Transform parent, string name, Sprite sprite, Vector2 at, TavernInteractableKind kind, string nameKey, Vector2 use,
            Vector2 solid, bool wall = false)
        {
            if (sprite == null) throw new InvalidOperationException($"No sprite for {name}.");
            SpriteRenderer art = LookTestContent.AddSprite(parent, name, sprite, wall ? SortingLayers.Floor : SortingLayers.YSorted, wall ? 6 : 0, Vector3.zero);
            art.transform.position = at;
            if (solid != Vector2.zero) LookTestBuilder.Solid(art.transform, "Solid", new Vector2(0f, solid.y / 2f), solid);
            var interactable = art.gameObject.AddComponent<TavernInteractable>();
            interactable.Configure(kind, nameKey, use, 1f, null);
            return art.gameObject.AddComponent<DaytimeFixture>();
        }

        // ---- the canvas

        /// <summary>Pips the HUD has room for (the day's maximum are shown).</summary>
        const int k_MaxPips = 10;

        /// <summary>
        /// The surface HUD, top left (only in the free daytime): the clock on a small parchment tab, today's Vigor as a row of
        /// pips beside it (4h Checkpoint B), a one-line harvest note under it; indoors, the Decorate key's reminder in the bottom
        /// left corner, clear of the room (Checkpoint A's reminder sat over the room's top-left corner).
        /// </summary>
        static void BuildClockFace(Canvas ui)
        {
            Transform old = ui.transform.Find("SurfaceClock");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(ui, "SurfaceClock");
            root.SetAsFirstSibling();
            // The panel's visible frame is about 4 px deep with rounded corners: 24 px tall leaves the 12-px line clear of its red rule.
            RectTransform tab = LookTestBuilder.UIRect(root, "Tab", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, -1f), new Vector2(64f, 24f));
            DungeonUI.AddImage(tab, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            LocalizedSuperText text = LookTestBuilder.Text(tab, "Time", SurfaceLocKeys.Clock, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // Vigor: small pips right of the tab, each a dark frame round a 4×6 light (full: warm; spent: dark).
            RectTransform vigor = LookTestBuilder.UIRect(tab, "Vigor", new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(k_MaxPips * 7f, 8f));
            var pips = new Image[k_MaxPips];
            for (int i = 0; i < k_MaxPips; i++)
            {
                RectTransform frame = LookTestBuilder.UIRect(vigor, $"Pip {i + 1}", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 7f, 0f), new Vector2(6f, 8f));
                DungeonUI.AddImage(frame, DungeonUI.Pixel(), new Color(0.1f, 0.07f, 0.06f, 1f));
                RectTransform light = LookTestBuilder.UIRect(frame, "Light", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 6f));
                pips[i] = DungeonUI.AddImage(light, DungeonUI.Pixel(), Color.white);
            }

            // The harvest's note, under the tab for a moment.
            RectTransform note = LookTestBuilder.UIRect(root, "Note", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, -26f), new Vector2(150f, 24f));
            DungeonUI.AddImage(note, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            LocalizedSuperText noteText = LookTestBuilder.Text(note, "Text", GardenLocKeys.Harvested, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, 0f));

            // Indoors only: the Decorate key's reminder, bottom left (the room never reaches the screen's bottom-left corner).
            RectTransform hint = LookTestBuilder.UIRect(root, "Decorate", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(5f, 3f), new Vector2(90f, 12f));
            LocalizedSuperText decorate = LookTestBuilder.Text(hint, "Text", SurfaceLocKeys.DecorateHint, TextStyle.Secondary, DungeonUI.k_Light, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<SurfaceClockView>().Configure(tab.gameObject, text, hint.gameObject, decorate, pips, note.gameObject, noteText);
            tab.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
            note.gameObject.SetActive(false);
        }

        /// <summary>The menu board's question: begin evening prep now? ("not yet" is the default).</summary>
        static void BuildPrepConfirm(Canvas ui)
        {
            Transform old = ui.transform.Find("PrepConfirm");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(ui, "PrepConfirm");
            RectTransform panel = DungeonUI.Panel(root, new Vector2(200f, 58f), Vector2.zero);
            DungeonUI.Line(panel, "Question", SurfaceLocKeys.PrepQuestion, 8f, 24f);
            Button yes = TavernScreens.SmallButton(panel, "Yes", SurfaceLocKeys.PrepYes, new Vector2(0.5f, 0f), new Vector2(-46f, 6f), 80f, out _);
            Button no = TavernScreens.SmallButton(panel, "No", SurfaceLocKeys.PrepNo, new Vector2(0.5f, 0f), new Vector2(46f, 6f), 80f, out _);
            UiFeedbackContent.Commit(yes);
            yes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = no, selectOnLeft = no };
            no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = yes, selectOnLeft = yes };
            root.gameObject.AddComponent<PrepConfirm>().Configure(panel.gameObject, yes, no);
            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// What to plant (4h Checkpoint B): the starter crops stacked, each with its seedling and days to grow, then "not now".
        /// Up and down move between them (wrapping); the first crop is selected.
        /// </summary>
        static void BuildGardenPanel(Canvas ui)
        {
            const int crops = 3;
            Transform old = ui.transform.Find("GardenPanel");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(ui, "GardenPanel");
            // The title's band (its line and rule) above the stacked choices: the first one's top sits 32 px under the panel's top.
            RectTransform panel = DungeonUI.Panel(root, new Vector2(170f, 130f), Vector2.zero);
            LocalizedSuperText title = DungeonUI.Title(panel, GardenLocKeys.PanelTitle);
            var buttons = new Button[crops];
            var labels = new LocalizedSuperText[crops];
            var icons = new Image[crops];
            for (int i = 0; i < crops; i++)
            {
                Button button = TavernScreens.SmallButton(panel, $"Crop {i + 1}", GardenLocKeys.PanelCrop, new Vector2(0.5f, 0f), new Vector2(0f, 82f - i * 19f), 130f, out LocalizedSuperText label);
                RectTransform labelRect = (RectTransform)label.transform;
                labelRect.offsetMin = new Vector2(14f, labelRect.offsetMin.y);
                RectTransform icon = LookTestBuilder.UIRect(button.transform, "Icon", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 2f), new Vector2(8f, 16f));
                icons[i] = DungeonUI.AddImage(icon, null, Color.white);
                icons[i].preserveAspect = true;
                buttons[i] = button;
                labels[i] = label;
                // A plain click: the Vigor spent gives the choice its tick and tap (SurfaceClockView), not a second one here.
            }
            Button cancel = TavernScreens.SmallButton(panel, "Cancel", GardenLocKeys.PanelCancel, new Vector2(0.5f, 0f), new Vector2(0f, 6f), 80f, out _);
            var order = new Selectable[] { buttons[0], buttons[1], buttons[2], cancel };
            for (int i = 0; i < order.Length; i++)
                order[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = order[(i + order.Length - 1) % order.Length],
                    selectOnDown = order[(i + 1) % order.Length],
                };
            root.gameObject.AddComponent<GardenPanel>().Configure(panel.gameObject, title, buttons, labels, icons, cancel);
            panel.gameObject.SetActive(false);
        }
    }
}
