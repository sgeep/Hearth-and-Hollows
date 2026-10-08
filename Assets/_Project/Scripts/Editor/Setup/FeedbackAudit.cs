using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-C's feedback audit, from what's actually built: every <c>MMF_Player</c> in the prefabs and the game's scenes, with the
    /// channels it plays (sound, and whether that sound is still a placeholder; haptics; screen shake; flash; motion such as scale,
    /// position or rotation; hit-stop; anything else), written to <c>BatchLogs/feedback_audit.md</c>. The audit table in the
    /// docs maps these players to the player's actions and marks the gaps.
    /// </summary>
    public static class FeedbackAudit
    {
        static readonly string[] k_Scenes = { BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene };

        public sealed class Row
        {
            public string Where, Player;
            public readonly List<string> Sounds = new();
            public bool Placeholder, Haptic, Shake, Flash, Motion, HitStop;
            public readonly SortedSet<string> Other = new();
        }

        [MenuItem("Hearthdelve/Report/Feedback Audit", priority = 40)]
        public static void WriteMenu() => Debug.Log($"[Hearthdelve] Feedback audit: {Write().Count} players → BatchLogs/feedback_audit.md");

        public static void WriteBatch()
        {
            try
            {
                WriteMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static List<Row> Collect()
        {
            var rows = new List<Row>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) Add(prefab, Path.GetFileNameWithoutExtension(path), rows);
            }
            foreach (string path in k_Scenes.Where(File.Exists))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects()) Add(root, Path.GetFileNameWithoutExtension(path), rows);
            }
            return rows;
        }

        static void Add(GameObject root, string where, List<Row> rows)
        {
            foreach (MMF_Player player in root.GetComponentsInChildren<MMF_Player>(true))
            {
                var row = new Row { Where = where, Player = Name(player.transform) };
                if (player.FeedbacksList != null)
                    foreach (MMF_Feedback f in player.FeedbacksList)
                    {
                        string type = f.GetType().Name;
                        if (f is MMF_Sound s)
                        {
                            string clip = s.Sfx != null ? s.Sfx.name : s.RandomSfx != null && s.RandomSfx.Length > 0 && s.RandomSfx[0] != null ? s.RandomSfx[0].name : "(none)";
                            row.Sounds.Add(clip);
                            if (clip.StartsWith("PH_")) row.Placeholder = true;
                        }
                        else if (type.Contains("Haptic")) row.Haptic = true;
                        else if (type.Contains("Shake") || type.Contains("CinemachineImpulse")) row.Shake = true;
                        else if (type.Contains("Flash") || type.Contains("Flicker") || type.Contains("Color")) row.Flash = true;
                        else if (type.Contains("Scale") || type.Contains("Position") || type.Contains("Rotation") || type.Contains("Squash") || type.Contains("Wiggle")) row.Motion = true;
                        else if (type.Contains("HitStop") || type.Contains("FreezeFrame") || type.Contains("TimescaleModifier")) row.HitStop = true;
                        else row.Other.Add(type.Replace("MMF_", ""));
                    }
                rows.Add(row);
            }
        }

        static string Name(Transform t)
        {
            var parts = new List<string>();
            for (int i = 0; t != null && i < 3; t = t.parent, i++) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        public static List<Row> Write()
        {
            List<Row> rows = Collect();
            var sb = new StringBuilder("| Where | Player | Sound | Haptic | Shake | Flash | Motion | Hit-stop | Other |\n|---|---|---|---|---|---|---|---|---|\n");
            static string Y(bool b) => b ? "yes" : "";
            foreach (Row r in rows.OrderBy(r => r.Where).ThenBy(r => r.Player))
                sb.AppendLine($"| {r.Where} | {r.Player} | {string.Join(", ", r.Sounds.Distinct())}{(r.Placeholder ? " (PH)" : "")} | {Y(r.Haptic)} | {Y(r.Shake)} | {Y(r.Flash)} | {Y(r.Motion)} | {Y(r.HitStop)} | {string.Join(", ", r.Other)} |");
            Directory.CreateDirectory("BatchLogs");
            File.WriteAllText("BatchLogs/feedback_audit.md", sb.ToString());
            return rows;
        }
    }
}
