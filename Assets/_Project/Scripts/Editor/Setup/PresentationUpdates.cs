using System;
using System.Linq;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Game;
using Hearthdelve.Tavern.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-C's presentation fixes, applied in place to what the generators already made: every change of place inside the world
    /// (doors, stairs, the Hollows' rooms) on the one shared timing (<see cref="PlaceFade"/>). New objects take it from their
    /// defaults. Running it again changes nothing.
    /// </summary>
    public static class PresentationUpdates
    {
        static readonly string[] k_Scenes = { EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene, EditorPaths.TestFloorScene };

        [MenuItem("Hearthdelve/Generate/Apply the Presentation Fixes", priority = 34)]
        public static void ApplyMenu() => Debug.Log($"[Hearthdelve] Presentation fixes: {UnifyFades()} fade(s) set to {PlaceFade.Seconds} s.");

        public static void ApplyBatch()
        {
            try
            {
                ApplyMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>The fields that time a change of place, by component.</summary>
        public static readonly (Type type, string[] fields)[] Fades =
        {
            (typeof(AreaPassage), new[] { "m_FadeSeconds" }),
            (typeof(SurfaceDoor), new[] { "m_FadeSeconds" }),
            (typeof(RoomRunner), new[] { "m_FadeOut", "m_FadeIn" }),
        };

        public static int UnifyFades()
        {
            int changed = 0;
            foreach (string path in k_Scenes.Where(System.IO.File.Exists))
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int n = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (var (type, fields) in Fades)
                foreach (Component c in root.GetComponentsInChildren(type, true))
                {
                    var so = new SerializedObject(c);
                    foreach (string field in fields)
                    {
                        SerializedProperty p = so.FindProperty(field);
                        if (p == null || Mathf.Approximately(p.floatValue, PlaceFade.Seconds)) continue;
                        p.floatValue = PlaceFade.Seconds;
                        n++;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (n > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                changed += n;
            }
            return changed;
        }
    }
}
