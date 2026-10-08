using System;
using System.Linq;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.UI.Typography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-B's colour and contrast audit, applied in place to what the generators already made: every attack telegraph's mark to
    /// the audited colour (<see cref="AttackTelegraphMarker.DefaultColour"/>), and every serialized colour on texts, images and our
    /// UI components that was one of the old text tones to its audited one (<see cref="UiPalette.Audited"/>), in the prefabs and
    /// the game's scenes (the 4a look scenes stay as baselines). The generators make the new colours themselves. Running it again
    /// changes nothing.
    /// </summary>
    public static class AccessibilityUpdates
    {
        [MenuItem("Hearthdelve/Generate/Apply the Colour Audit", priority = 33)]
        public static void ApplyMenu() => Debug.Log($"[Hearthdelve] Colour audit: {Apply()} telegraph mark(s), {Recolour()} text colour(s) updated.");

        static readonly string[] k_Scenes =
        {
            BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene, EditorPaths.TestFloorScene,
        };

        /// <summary>The old text tones to the audited ones, in place; how many colours changed.</summary>
        public static int Recolour()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = Recolour(root);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed += n;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            foreach (string scenePath in k_Scenes.Where(System.IO.File.Exists))
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int n = scene.GetRootGameObjects().Sum(Recolour);
                if (n > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                changed += n;
            }
            AssetDatabase.SaveAssets();
            return changed;
        }

        static int Recolour(GameObject root)
        {
            int n = 0;
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                bool ours = c.GetType().Namespace != null && c.GetType().Namespace.StartsWith("Hearthdelve.UI");
                if (!(c is SuperTextMesh) && !(c is Image) && !ours) continue;
                var so = new SerializedObject(c);
                SerializedProperty p = so.GetIterator();
                bool changed = false;
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.Color) continue;
                    Color audited = UiPalette.Audit(p.colorValue);
                    if (audited == p.colorValue) continue;
                    p.colorValue = audited;
                    changed = true;
                    n++;
                }
                if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            }
            return n;
        }

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

        public static int Apply()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EditorPaths.Prefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponentInChildren<AttackTelegraphMarker>(true) == null) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = 0;
                    foreach (AttackTelegraphMarker marker in root.GetComponentsInChildren<AttackTelegraphMarker>(true))
                        if (marker.Colour != AttackTelegraphMarker.DefaultColour)
                        {
                            marker.Colour = AttackTelegraphMarker.DefaultColour;
                            n++;
                        }
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed += n;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return changed;
        }
    }
}
