using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The game's one audio mixer (4i-B), made by code rather than by hand: Master, with Music and Effects under it, and an exposed
    /// volume for each (<see cref="MasterVolume"/>, <see cref="MusicVolume"/>, <see cref="EffectsVolume"/>). Unity has no public
    /// API for building a mixer, so this uses its own mixer editor's (internal) one by reflection; it is created once, then only
    /// checked. Every sound is routed to Music or Effects by the generators (<see cref="Effects"/>), never by hand.
    /// </summary>
    public static class AudioMixerBuilder
    {
        public const string MixerPath = EditorPaths.Root + "/Audio/GameMixer.mixer";
        public const string MasterVolume = "MasterVolume", MusicVolume = "MusicVolume", EffectsVolume = "EffectsVolume";
        public const string MusicGroup = "Music", EffectsGroup = "Effects";

        static readonly Assembly k_Editor = typeof(UnityEditor.Editor).Assembly;
        const BindingFlags k_All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>The mixer, made if it isn't there yet.</summary>
        public static AudioMixer Mixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer != null) return mixer;
            EditorPaths.Ensure(EditorPaths.Root + "/Audio");
            Type controllerType = k_Editor.GetType("UnityEditor.Audio.AudioMixerController", true);
            object controller = controllerType.GetMethod("CreateMixerControllerAtPath", k_All).Invoke(null, new object[] { MixerPath });
            object master = controllerType.GetProperty("masterGroup", k_All).GetValue(controller);
            object music = NewGroup(controllerType, controller, master, MusicGroup);
            object effects = NewGroup(controllerType, controller, master, EffectsGroup);

            // Expose each group's volume under a name the game sets.
            Type exposedType = k_Editor.GetType("UnityEditor.Audio.ExposedAudioParameter", true);
            Array exposed = Array.CreateInstance(exposedType, 3);
            int i = 0;
            foreach (var (group, name) in new[] { (master, MasterVolume), (music, MusicVolume), (effects, EffectsVolume) })
            {
                object guid = group.GetType().GetMethod("GetGUIDForVolume", k_All).Invoke(group, null);
                object parameter = Activator.CreateInstance(exposedType);
                exposedType.GetField("guid").SetValue(parameter, guid);
                exposedType.GetField("name").SetValue(parameter, name);
                exposed.SetValue(parameter, i++);
            }
            controllerType.GetProperty("exposedParameters", k_All).SetValue(controller, exposed);
            controllerType.GetMethod("OnChangedExposedParameter", k_All)?.Invoke(controller, null);
            EditorUtility.SetDirty((UnityEngine.Object)controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath);
            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null) throw new InvalidOperationException("The audio mixer couldn't be created.");
            Debug.Log($"[Hearthdelve] Created {MixerPath}.");
            return mixer;
        }

        static object NewGroup(Type controllerType, object controller, object parent, string name)
        {
            object group = controllerType.GetMethod("CreateNewGroup", k_All).Invoke(controller, new object[] { name, false });
            controllerType.GetMethod("AddChildToParent", k_All).Invoke(controller, new[] { group, parent });
            // (The mixer window's group view is layout only; it's filled in when the mixer is first opened in the editor.)
            return group;
        }

        public static AudioMixerGroup Group(string name) => Mixer().FindMatchingGroups(name).FirstOrDefault(g => g.name == name);
        public static AudioMixerGroup Music => Group(MusicGroup);
        public static AudioMixerGroup Effects => Group(EffectsGroup);

        [MenuItem("Hearthdelve/Generate/Audio Mixer", priority = 31)]
        public static void BuildMenu() => Debug.Log($"[Hearthdelve] Mixer: {AssetDatabase.GetAssetPath(Mixer())}, groups {string.Join(", ", Mixer().FindMatchingGroups(string.Empty).Select(g => g.name))}.");

        public static void BuildBatch()
        {
            try
            {
                BuildMenu();
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
