using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// One-time project settings: layers, sorting layers, 2D physics, a 60 Hz fixed step,
    /// player settings, and removal of the template's sample assets. Safe to re-run.
    /// </summary>
    public static class ProjectConfigurator
    {
        [MenuItem("Hearthdelve/Setup/Configure Project Settings", priority = 0)]
        public static void ConfigureAll()
        {
            ConfigureLayers();
            ConfigureSortingLayers();
            ConfigurePhysics2D();
            ConfigureTime();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] Project settings configured.");
        }

        static SerializedObject LoadSettings(string path) =>
            new(AssetDatabase.LoadAllAssetsAtPath(path)[0]);

        public static void ConfigureLayers()
        {
            var tagManager = LoadSettings("ProjectSettings/TagManager.asset");
            var layers = tagManager.FindProperty("layers");
            foreach (var (name, index) in Layers.All)
            {
                var slot = layers.GetArrayElementAtIndex(index);
                if (slot.stringValue == name) continue;
                if (!string.IsNullOrEmpty(slot.stringValue))
                    Debug.LogWarning($"[Hearthdelve] Replacing layer {index} '{slot.stringValue}' with '{name}'.");
                slot.stringValue = name;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigureSortingLayers()
        {
            var tagManager = LoadSettings("ProjectSettings/TagManager.asset");
            var array = tagManager.FindProperty("m_SortingLayers");
            var existing = new HashSet<string>();
            var ids = new HashSet<long>();
            for (int i = 0; i < array.arraySize; i++)
            {
                var e = array.GetArrayElementAtIndex(i);
                existing.Add(e.FindPropertyRelative("name").stringValue);
                ids.Add(e.FindPropertyRelative("uniqueID").longValue);
            }

            var random = new System.Random(0x4ea27);
            foreach (var layerName in SortingLayers.Ordered.Where(n => !existing.Contains(n)))
            {
                array.InsertArrayElementAtIndex(array.arraySize);
                var e = array.GetArrayElementAtIndex(array.arraySize - 1);
                e.FindPropertyRelative("name").stringValue = layerName;
                long id;
                do id = random.Next(1, int.MaxValue); while (ids.Contains(id));
                ids.Add(id);
                e.FindPropertyRelative("uniqueID").longValue = id;
                var locked = e.FindPropertyRelative("locked");
                if (locked != null)
                {
                    if (locked.propertyType == SerializedPropertyType.Boolean) locked.boolValue = false;
                    else locked.intValue = 0;
                }
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigurePhysics2D()
        {
            // Hitbox and collision queries only consider solid colliders.
            var physics = LoadSettings("ProjectSettings/Physics2DSettings.asset");
            physics.FindProperty("m_QueriesHitTriggers").boolValue = false;
            physics.ApplyModifiedPropertiesWithoutUndo();

            // Characters are kinematic and collide via their own casts; only dynamic pickups
            // use the physics matrix. Keep it minimal so nothing pushes anything unexpectedly.
            int ground = LayerMask.NameToLayer(Layers.Ground);
            int oneWay = LayerMask.NameToLayer(Layers.OneWayPlatform);
            int player = LayerMask.NameToLayer(Layers.Player);
            int enemy = LayerMask.NameToLayer(Layers.Enemy);
            int pickup = LayerMask.NameToLayer(Layers.Pickup);
            int projectile = LayerMask.NameToLayer(Layers.Projectile);

            foreach (var a in new[] { player, enemy, pickup, projectile })
            foreach (var b in new[] { player, enemy, pickup, projectile })
                Physics2D.IgnoreLayerCollision(a, b, true);
            Physics2D.IgnoreLayerCollision(pickup, ground, false);
            Physics2D.IgnoreLayerCollision(pickup, oneWay, false);
        }

        public static void ConfigureTime()
        {
            // Unity 6 stores the fixed step as a rational: m_Count ticks at m_Rate ticks per second.
            var time = LoadSettings("ProjectSettings/TimeManager.asset");
            var step = time.FindProperty("Fixed Timestep");
            var count = step?.FindPropertyRelative("m_Count");
            var numerator = step?.FindPropertyRelative("m_Rate.m_Numerator");
            var denominator = step?.FindPropertyRelative("m_Rate.m_Denominator");
            if (count == null || numerator == null || denominator == null || denominator.longValue == 0)
            {
                Debug.LogWarning("[Hearthdelve] Couldn't find the fixed timestep setting; set it to 1/60 s in Project Settings > Time.");
                return;
            }
            long ticksPerSecond = numerator.longValue / denominator.longValue;
            count.longValue = ticksPerSecond / 60;
            time.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Hearthdelve";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
        }

        /// <summary>Deletes the Unity template's sample scene and default input actions.</summary>
        public static void RemoveTemplateAssets()
        {
            foreach (var path in new[] { "Assets/Scenes/SampleScene.unity", "Assets/InputSystem_Actions.inputactions" })
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
            if (AssetDatabase.IsValidFolder("Assets/Scenes") && AssetDatabase.FindAssets("", new[] { "Assets/Scenes" }).Length == 0)
                AssetDatabase.DeleteAsset("Assets/Scenes");
        }
    }
}
