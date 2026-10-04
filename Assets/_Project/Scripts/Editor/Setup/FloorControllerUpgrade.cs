using System;
using System.Linq;
using Hearthdelve.Shared.Engine;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Converts our character prefabs from TDE's <see cref="TopDownController2D"/> to <see cref="FloorController2D"/>
    /// in place: only the component's script changes, so its values and every reference to it are kept. The
    /// generators add <see cref="FloorController2D"/> themselves; this is for prefabs built before it existed.
    /// </summary>
    public static class FloorControllerUpgrade
    {
        [MenuItem("Hearthdelve/Setup/Upgrade Character Controllers", priority = 40)]
        public static void Upgrade()
        {
            MonoScript script = AssetDatabase.FindAssets("FloorController2D t:MonoScript")
                .Select(g => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(g)))
                .First(s => s != null && s.GetClass() == typeof(FloorController2D));
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool any = false;
                    foreach (TopDownController2D controller in root.GetComponentsInChildren<TopDownController2D>(true))
                    {
                        if (controller.GetType() != typeof(TopDownController2D)) continue;
                        var serialized = new SerializedObject(controller);
                        serialized.FindProperty("m_Script").objectReferenceValue = script;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        any = true;
                    }
                    if (any)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed++;
                        Debug.Log($"[Hearthdelve] Floor controller: {path}");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Debug.Log($"[Hearthdelve] Floor controllers upgraded in {changed} prefabs.");
        }

        public static void UpgradeBatch()
        {
            try
            {
                Upgrade();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
