using System;
using System.IO;
using System.Linq;
using Hearthdelve.Editor;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.DialogueEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Hearthdelve.Story.Editor
{
    /// <summary>
    /// Opens a conversation in the Dialogue System's node editor and saves a picture of it (4g Checkpoint B's acceptance check:
    /// the story's conversations are ordinary graphs, edited there). With <c>-proveEdits</c> it also edits one line and one link
    /// in the database, runs the story updater, checks both survived, and puts them back. Run from the menu, or in the editor
    /// (not batch mode: it needs a window) with <c>-executeMethod Hearthdelve.Story.Editor.DialogueGraphCapture.CaptureAndQuit</c>.
    /// </summary>
    public static class DialogueGraphCapture
    {
        public const string Title = "Boog/Bomb";
        public static string OutputFolder => "BatchLogs/story";

        [MenuItem("Hearthdelve/Story/Capture Boog's Bomb Graph", priority = 41)]
        public static void CaptureMenu() => Begin(quit: false);

        public static void CaptureAndQuit() => Begin(quit: true);

        /// <summary>Batch: the edit-survival check alone (no window needed). Exits 0 if the edits survived, 1 if not.</summary>
        public static void ProveEditsBatch()
        {
            try
            {
                string report = ProveEdits(AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue));
                Debug.Log("[Hearthdelve] " + report);
                EditorApplication.Exit(report.StartsWith("Edits survived") ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void Begin(bool quit)
        {
            var db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue);
            Conversation conversation = db != null ? db.GetConversation(Title) : null;
            if (conversation == null)
            {
                Debug.LogError($"[Hearthdelve] No '{Title}' conversation.");
                if (quit) EditorApplication.Exit(1);
                return;
            }
            string report = Environment.GetCommandLineArgs().Contains("-proveEdits") ? ProveEdits(db) : null;
            DialogueEditorWindow.OpenDialogueEntry(db, conversation.id, 0);
            var window = EditorWindow.GetWindow<DialogueEditorWindow>();
            window.position = new Rect(20, 40, 1880, 980);
            window.Focus();
            int frames = 0;
            void Tick()
            {
                window.Repaint();
                if (++frames < 240) return;
                EditorApplication.update -= Tick;
                Directory.CreateDirectory(OutputFolder);
                Rect r = window.position;
                Color[] pixels = InternalEditorUtility.ReadScreenPixel(r.position, (int)r.width, (int)r.height);
                var texture = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGB24, false);
                texture.SetPixels(pixels);
                texture.Apply();
                string path = Path.Combine(OutputFolder, "DialogueEditor_BoogBomb.png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                Debug.Log($"[Hearthdelve] Dialogue graph captured: {path}{(report != null ? "\n" + report : string.Empty)}");
                if (quit) EditorApplication.Exit(0);
            }
            EditorApplication.update += Tick;
        }

        /// <summary>
        /// Edits a line, a condition and a link of Boog/Bomb as an author would in the node editor, runs the story updater, and checks
        /// each survived; then restores them. Returns what it found.
        /// </summary>
        static string ProveEdits(DialogueDatabase db)
        {
            Conversation c = db.GetConversation(Title);
            DialogueEntry line = c.dialogueEntries.First(e => e.DialogueText != null && e.DialogueText.StartsWith("i lost something"));
            DialogueEntry reminder = c.dialogueEntries.First(e => e.DialogueText != null && e.DialogueText.StartsWith("any sign of her"));
            string oldText = line.DialogueText, oldCondition = reminder.conditionsString;
            Link removed = line.outgoingLinks[0];
            int links = line.outgoingLinks.Count, entries = c.dialogueEntries.Count;

            line.DialogueText = "i misplaced my favorite bomb. it happens.";
            reminder.conditionsString = oldCondition + " and true";
            line.outgoingLinks.RemoveAt(0);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            StoryBuilder.Update();

            db = AssetDatabase.LoadAssetAtPath<DialogueDatabase>(StoryPaths.Dialogue);
            c = db.GetConversation(Title);
            line = c.dialogueEntries.First(e => e.id == line.id);
            reminder = c.dialogueEntries.First(e => e.id == reminder.id);
            bool kept = line.DialogueText == "i misplaced my favorite bomb. it happens." && reminder.conditionsString == oldCondition + " and true" &&
                        line.outgoingLinks.Count == links - 1 && c.dialogueEntries.Count == entries && db.conversations.Count(x => x.Title == Title) == 1;

            line.DialogueText = oldText;
            reminder.conditionsString = oldCondition;
            line.outgoingLinks.Insert(0, removed);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            StoryDialogue.FillTable(db);
            return kept
                ? "Edits survived the story updater: a line, a condition and a link, as edited (then restored)."
                : "EDITS DID NOT SURVIVE the story updater.";
        }
    }
}
