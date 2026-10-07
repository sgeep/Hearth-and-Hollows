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
        public static readonly Vector2Int[] TavernReserved = { new(15, 2), new(15, 3), new(25, 9) };
        /// <summary>The menu board and the storeroom shelves (area tiles): solid fixtures the layout check walks round.</summary>
        public static readonly Rect[] TavernFixtures = { new(15f, 2f, 1f, 1f), new(25f, 9f, 1f, 1f) };
                /// <summary>Phi's portrait on the upstairs room's back wall (area tiles): wall decor can't hang behind it.</summary>
        public static readonly Rect PortraitWall = new(7f, 10f, 3f, 3f);

        [MenuItem("Hearthdelve/Generate/Update Surface (4h)", priority = 4)]
        public static void UpdateSurface()
        {
            ClockConfig();
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
            tavern.SetFixtures(tavern.Fixtures.Where(f => !TavernFixtures.Contains(f)).Concat(TavernFixtures).ToArray());

            GameObject camera = GameObject.Find(TavernView.CameraName);
            if (camera.GetComponent<SurfaceCamera>() == null) camera.AddComponent<SurfaceCamera>();

            // The front door, inside: just above the threshold. Out into Kariaston, and back to a step inside it.
            var door = new GameObject("Front Door");
            door.transform.SetParent(surface, false);
            door.transform.position = new Vector2(TavernBuilder.DoorColumn + 0.5f, TavernBuilder.FloorBottom + 0.22f);
            var trigger = door.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(0.9f, 0.45f);
            door.AddComponent<SurfaceDoor>().Configure(SurfaceDoor.FrontInside, SurfaceDoor.FrontOutside, tavernSurface, new Vector2(0f, 1.3f));

            Transform places = new GameObject("Daytime Places").transform;
            places.SetParent(surface, false);
            // The menu board, standing just inside the door: the evening begins here, when the player says.
            Fixture(places, "Menu Board", Sprite(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "MenuBoard"), new Vector2(15.5f, 2.05f),
                TavernInteractableKind.MenuBoard, SurfaceLocKeys.MenuBoard, new Vector2(0f, 1.1f), solid: new Vector2(0.8f, 0.4f));
            // The storeroom shelves, in the barrels' corner of the kitchen.
            Fixture(places, "Storeroom Shelves", Sprite(KariastonSheets.TownsPack, KariastonSheets.TownsProps, "CupboardJars"), new Vector2(25.5f, 9.05f),
                TavernInteractableKind.Storeroom, SurfaceLocKeys.Storeroom, new Vector2(-1f, 0.4f), solid: new Vector2(0.9f, 0.5f));
            // Phi's portrait, upstairs, on the wall by the bed the keeper wakes in.
            Vector2 portrait = upstairs.Origin + new Vector2(PortraitWall.center.x, PortraitWall.yMin);
            DaytimeFixture look = Fixture(places, "Phi's Portrait", MinifantasyImporter.Sprites(MinifantasySheets.Portraits, "PhiFramed").Values.First(), portrait,
                TavernInteractableKind.Inspect, SurfaceLocKeys.LookPortrait, new Vector2(0f, -0.6f), solid: Vector2.zero, wall: true);
            look.Configure(SurfaceConversations.PhiPortrait);

            GameObject service = GameObject.Find("Service");
            if (service.GetComponent<FiveOClock>() == null) service.AddComponent<FiveOClock>();

            BuildClockFace(ui);
            BuildPrepConfirm(ui);
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

        /// <summary>The clock's face, top left, on a small parchment tab: only in the free daytime.</summary>
        static void BuildClockFace(Canvas ui)
        {
            Transform old = ui.transform.Find("SurfaceClock");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            RectTransform root = DungeonUI.FullScreen(ui, "SurfaceClock");
            root.SetAsFirstSibling();
            RectTransform tab = LookTestBuilder.UIRect(root, "Tab", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f, -3f), new Vector2(56f, 16f));
            DungeonUI.AddImage(tab, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            LocalizedSuperText text = LookTestBuilder.Text(tab, "Time", SurfaceLocKeys.Clock, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            // Under the tab, indoors only: the Decorate key's reminder.
            RectTransform hint = LookTestBuilder.UIRect(root, "Decorate", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -20f), new Vector2(90f, 12f));
            LocalizedSuperText decorate = LookTestBuilder.Text(hint, "Text", SurfaceLocKeys.DecorateHint, TextStyle.Secondary, DungeonUI.k_Light, TextAnchor.MiddleLeft,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            root.gameObject.AddComponent<SurfaceClockView>().Configure(tab.gameObject, text, hint.gameObject, decorate);
            tab.gameObject.SetActive(false);
            hint.gameObject.SetActive(false);
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
    }
}
