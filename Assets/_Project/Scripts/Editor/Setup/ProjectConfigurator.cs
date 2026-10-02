using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// One-time project settings for the top-down game: layers, no gravity, Y-sorting on the
    /// 2D renderer, scripting defines, player settings and the build scene list. Safe to re-run.
    /// </summary>
    public static class ProjectConfigurator
    {
        const string k_NiceVibrationsDefine = "MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED";

        [MenuItem("Hearthdelve/Setup/Configure Project Settings", priority = 0)]
        public static void ConfigureAll()
        {
            ConfigureLayers();
            ConfigurePhysics2D();
            ConfigureSorting();
            ConfigureDefines();
            ConfigurePlayer();
            RemoveMissingBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] Project settings configured.");
        }

        static SerializedObject LoadSettings(string path) =>
            new(AssetDatabase.LoadAllAssetsAtPath(path)[0]);

        /// <summary>Names our layers. TDE's layers keep TDE's indices; a clash is reported, not overwritten.</summary>
        public static void ConfigureLayers()
        {
            var tagManager = LoadSettings("ProjectSettings/TagManager.asset");
            var layers = tagManager.FindProperty("layers");
            foreach (var (name, index) in Layers.All)
            {
                var slot = layers.GetArrayElementAtIndex(index);
                if (slot.stringValue == name) continue;
                if (!string.IsNullOrEmpty(slot.stringValue))
                {
                    Debug.LogError($"[Hearthdelve] Layer {index} is '{slot.stringValue}', expected '{name}'. Left unchanged.");
                    continue;
                }
                slot.stringValue = name;
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Top-down: no gravity. Triggers stay visible to queries, as TDE expects.</summary>
        public static void ConfigurePhysics2D()
        {
            var physics = LoadSettings("ProjectSettings/Physics2DSettings.asset");
            physics.FindProperty("m_Gravity").vector2Value = Vector2.zero;
            physics.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Y-sorting: custom transparency sort axis (0, 1, 0) on every 2D renderer and in Graphics settings.</summary>
        public static void ConfigureSorting()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Renderer2DData", new[] { "Assets/Settings", EditorPaths.Root }))
            {
                var data = AssetDatabase.LoadAssetAtPath<Renderer2DData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                var so = new SerializedObject(data);
                so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
                so.FindProperty("m_TransparencySortAxis").vector3Value = Vector3.up;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
            }

            var graphics = LoadSettings("ProjectSettings/GraphicsSettings.asset");
            graphics.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            graphics.FindProperty("m_TransparencySortAxis").vector3Value = Vector3.up;
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Nice Vibrations only adds its define for the selected build target, so set it for
        /// every target we ship (docs/THIRD_PARTY.md). TDE's haptic feedbacks need it.
        /// </summary>
        public static void ConfigureDefines()
        {
            foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Standalone, NamedBuildTarget.WebGL })
            {
                var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';').Where(d => d.Length > 0).ToList();
                if (defines.Contains(k_NiceVibrationsDefine)) continue;
                defines.Add(k_NiceVibrationsDefine);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }

        public static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Hearthdelve";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
        }

        /// <summary>Drops build-list entries whose scene no longer exists (the removed TDE demos).</summary>
        public static void RemoveMissingBuildScenes()
        {
            EditorBuildSettingsScene[] kept = EditorBuildSettings.scenes.Where(s => File.Exists(s.path)).ToArray();
            if (kept.Length != EditorBuildSettings.scenes.Length) EditorBuildSettings.scenes = kept;
        }

        /// <summary>Puts these scenes first in the build list, in this order, keeping any others after them.</summary>
        public static void SetBuildOrder(params string[] firstScenes)
        {
            var ordered = new List<EditorBuildSettingsScene>(firstScenes.Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)));
            ordered.AddRange(EditorBuildSettings.scenes.Where(s => !firstScenes.Contains(s.path) && File.Exists(s.path)));
            EditorBuildSettings.scenes = ordered.ToArray();
        }
    }
}
