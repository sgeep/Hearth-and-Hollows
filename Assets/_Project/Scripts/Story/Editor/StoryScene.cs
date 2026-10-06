using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Story.Presentation;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Typography;
using PixelCrushers.DialogueSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthdelve.Story.Editor
{
    /// <summary>
    /// The story's objects in Boot (4g), added in place (Boot is never rebuilt): the story host with the Dialogue Manager, Quest
    /// Machine (configuration and the player's journal) and the Love/Hate faction manager under it; Pixel Crushers' save component,
    /// used only as the serializer its savers call (it never writes a slot); and the dialogue box, which this rebuilds each run.
    /// </summary>
    public static class StoryScene
    {
        const string k_Story = "Story";
        const string k_DialogueCanvas = "Dialogue UI";
        const string k_Serializer = "Pixel Crushers Serializer";

        public static void UpdateBoot(StoryDatabase database)
        {
            var scene = EditorSceneManager.OpenScene(BootBuilder.BootScene, OpenSceneMode.Single);

            GameObject story = Root(k_Story);
            var host = Ensure<StoryHost>(story);
            Ensure<StoryDebugKeys>(story);

            HearthDialogueUI ui = BuildDialogueCanvas();

            GameObject manager = Child(story, "Dialogue Manager");
            var dialogue = Ensure<PixelCrushers.DialogueSystem.Wrappers.DialogueSystemController>(manager);
            dialogue.initialDatabase = database.dialogue;
            dialogue.dontDestroyOnLoad = false; // Boot is never unloaded.
            dialogue.allowOnlyOneInstance = true;
            dialogue.instantiateDatabase = true;
            dialogue.preloadResources = true;
            dialogue.dialogueTimeMode = DialogueTime.TimeMode.Realtime; // talks go on while the world is paused
            dialogue.debugLevel = DialogueDebug.DebugLevel.Warning;
            DisplaySettings display = dialogue.displaySettings;
            display.dialogueUI = ui.gameObject;
            display.subtitleSettings.showNPCSubtitlesDuringLine = true;
            display.subtitleSettings.showNPCSubtitlesWithResponses = true;
            display.subtitleSettings.showPCSubtitlesDuringLine = false; // the keeper's chosen line isn't repeated back
            display.subtitleSettings.continueButton = DisplaySettings.SubtitleSettings.ContinueButtonMode.Always;
            display.inputSettings.alwaysForceResponseMenu = true;
            display.cameraSettings.defaultSequence = "Delay({{end}})";

            GameObject quests = Child(story, "Quest Machine");
            var config = Ensure<PixelCrushers.QuestMachine.Wrappers.QuestMachineConfiguration>(quests);
            config.questDatabases = new System.Collections.Generic.List<PixelCrushers.QuestMachine.QuestDatabase> { database.quests };
            var journal = Ensure<PixelCrushers.QuestMachine.Wrappers.QuestJournal>(quests);
            journal.includeInSavedGameData = true;
            journal.saveQuestsToJson = true;

            // Builds before 4g Checkpoint A's playtest named this "Love/Hate" and, finding it by path, added one per run.
            foreach (Transform t in story.transform.Cast<Transform>().Where(t => t.name == "Love/Hate").ToList())
                Object.DestroyImmediate(t.gameObject);
            GameObject relationships = Child(story, "Relationships");
            var factions = Ensure<PixelCrushers.LoveHate.Wrappers.FactionManager>(relationships);
            factions.factionDatabase = database.factions;
            factions.allowOnlyOneFactionManager = true;

            GameObject serializer = Root(k_Serializer);
            Ensure<PixelCrushers.Wrappers.SaveSystem>(serializer);
            Ensure<PixelCrushers.Wrappers.JsonDataSerializer>(serializer);

            host.Configure(database, factions, journal);
            EditorUtility.SetDirty(host);
            EditorUtility.SetDirty(dialogue);
            EditorUtility.SetDirty(config);
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Hearthdelve] {BootBuilder.BootScene}: story objects updated in place.");
        }

        static GameObject Root(string name)
        {
            GameObject go = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Where(t => t.parent == null && t.name == name).Select(t => t.gameObject).FirstOrDefault();
            return go != null ? go : new GameObject(name);
        }

        /// <summary>The child called <paramref name="name"/> (compared by name: Transform.Find reads "/" as a path), created if missing.</summary>
        static GameObject Child(GameObject parent, string name)
        {
            foreach (Transform t in parent.transform)
                if (t.name == name) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();

        // ---------- The dialogue box ----------

        /// <summary>
        /// A box along the bottom of the 320×180 screen (312×74, three pixels up): the portrait at 2× in a one-pixel frame on the left,
        /// the speaker's name and four lines of text beside it, the ▼ in the bottom-right corner. The choices sit in their own panel
        /// above the box's right end, one 14-pixel button each, with a ▶ beside the chosen one.
        /// </summary>
        public const float BoxWidth = 312f, BoxHeight = 74f, BoxBottom = 3f;
        public const float TextLeft = 76f, TextWidth = 216f, BodyLines = 4f, MarkWidth = 12f;
        public const float ChoicesWidth = 262f, ChoiceLeft = 16f, ChoiceWidth = 241f, ChoiceHeight = 14f, ChoicePitch = 15f, ChoicePad = 4f;
        public const int ChoiceRows = 4;

        static HearthDialogueUI BuildDialogueCanvas()
        {
            GameObject old = Root(k_DialogueCanvas);
            if (old != null) Object.DestroyImmediate(old);
            var go = new GameObject(k_DialogueCanvas);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // over every scene's own UI, under the transition (1000)
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(LookTestBuilder.ReferenceWidth, LookTestBuilder.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            scaler.referencePixelsPerUnit = MinifantasySheets.PixelsPerUnit;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<Hearthdelve.UI.PixelCanvasScaler>();

            RectTransform root = DungeonUI.FullScreen(canvas, "Dialogue");
            var group = root.gameObject.AddComponent<CanvasGroup>();

            // The box.
            RectTransform box = LookTestBuilder.UIRect(root, "Box", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, BoxBottom), new Vector2(BoxWidth, BoxHeight));
            Image panel = DungeonUI.AddImage(box, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            panel.raycastTarget = true;
            var click = box.gameObject.AddComponent<DialogueBoxClick>();

            RectTransform frame = LookTestBuilder.UIRect(box, "PortraitFrame", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(66f, 66f));
            DungeonUI.AddImage(frame, DungeonUI.Pixel(), DungeonUI.k_Ink);
            RectTransform portraitRect = LookTestBuilder.UIRect(frame, "Portrait", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
            Image portrait = DungeonUI.AddImage(portraitRect, null, Color.white);
            portrait.preserveAspect = true;

            var topLeft = new Vector2(0f, 1f);
            LocalizedSuperText name = LookTestBuilder.Text(box, "Name", TavernLocKeys.Plain, TextStyle.Body, DungeonUI.k_Title, TextAnchor.UpperLeft,
                topLeft, topLeft, topLeft, new Vector2(TextLeft, -6f), new Vector2(TextWidth, SilverMetrics.LinePixels));
            LocalizedSuperText body = LookTestBuilder.Text(box, "Body", TavernLocKeys.Plain, TextStyle.Body, DungeonUI.k_Ink, TextAnchor.UpperLeft,
                topLeft, topLeft, topLeft, new Vector2(TextLeft, -20f), new Vector2(TextWidth, SilverMetrics.LinePixels * BodyLines));
            var bottomRight = new Vector2(1f, 0f);
            LocalizedSuperText more = LookTestBuilder.Text(box, "Continue", StoryLocKeys.Continue, TextStyle.Body, DungeonUI.k_Accent, TextAnchor.LowerRight,
                bottomRight, bottomRight, bottomRight, new Vector2(-6f, 6f), new Vector2(MarkWidth, SilverMetrics.LinePixels));

            // The choices.
            RectTransform choices = LookTestBuilder.UIRect(root, "Choices", new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                new Vector2(BoxWidth / 2f, BoxBottom + BoxHeight + 2f), new Vector2(ChoicesWidth, ChoicePad * 2f + ChoiceRows * ChoicePitch - 1f));
            DungeonUI.AddImage(choices, DungeonUI.UISprite("Panel"), Color.white, Image.Type.Sliced);
            var buttons = new Button[ChoiceRows];
            var labels = new LocalizedSuperText[ChoiceRows];
            var pointers = new GameObject[ChoiceRows];
            for (int i = 0; i < ChoiceRows; i++)
            {
                float top = -ChoicePad - i * ChoicePitch;
                RectTransform row = LookTestBuilder.UIRect(choices, $"Choice{i}", topLeft, topLeft, new Vector2(ChoiceLeft, top), new Vector2(ChoiceWidth, ChoiceHeight));
                buttons[i] = DungeonUI.ButtonFace(row);
                row.gameObject.AddComponent<SelectOnHover>();
                labels[i] = LookTestBuilder.Text(row, "Label", TavernLocKeys.Plain, TextStyle.Prompt, DungeonUI.k_Ink, TextAnchor.MiddleLeft,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(ChoiceWidth - 8f, SilverMetrics.LinePixels));
                LocalizedSuperText pointer = LookTestBuilder.Text(choices, $"Pointer{i}", StoryLocKeys.Pointer, TextStyle.Body, DungeonUI.k_Accent, TextAnchor.MiddleCenter,
                    topLeft, topLeft, topLeft, new Vector2(3f, top - 1f), new Vector2(MarkWidth, SilverMetrics.LinePixels));
                pointers[i] = pointer.gameObject;
                pointer.gameObject.SetActive(false);
            }
            choices.gameObject.SetActive(false);

            HearthDialogueUI ui = root.gameObject.AddComponent<HearthDialogueUI>();
            ui.Configure(group, portrait, frame.gameObject, name, body, (RectTransform)more.transform, click, choices, buttons, labels, pointers);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return ui;
        }
    }
}
