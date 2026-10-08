using System;
using System.IO;
using Hearthdelve.Core.Services;
using Hearthdelve.Shared.Save;
using UnityEngine;

namespace Hearthdelve.Shared.Settings
{
    /// <summary>
    /// The options file (4i-B, D4): <c>options.json</c> beside the save (in IndexedDB on the web, flushed after each write), never part
    /// of <c>SaveData</c>. A missing, damaged or unreadable file simply gives the defaults; nothing here throws on the way in.
    /// </summary>
    public sealed class OptionsStore
    {
        public const string FileName = "options.json";

        public OptionsStore(string directory)
        {
            Directory = directory;
            FilePath = Path.Combine(directory, FileName);
        }

        public string Directory { get; }
        public string FilePath { get; }

        public PlayerOptions Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return new PlayerOptions();
                string json = File.ReadAllText(FilePath);
                if (!SaveSystem.LooksWhole(json)) return new PlayerOptions();
                return OptionsRules.Sanitized(JsonUtility.FromJson<PlayerOptions>(json));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hearthdelve] The options can't be read ({e.Message}); using the defaults.");
                return new PlayerOptions();
            }
        }

        public void Save(PlayerOptions options)
        {
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(options, prettyPrint: true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
                WebStorage.Flush();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hearthdelve] The options couldn't be saved: {e.Message}");
            }
        }
    }

    /// <summary>
    /// The options in play (4i-B): loaded before the first scene, applied at once (feel to <see cref="GameSettings"/>, the window on
    /// desktop), and changed only through <see cref="Change"/>, which saves and tells the rest (the mixer, the dialogue box, the
    /// tavern) through <see cref="Changed"/>. Nothing about them is in the game's save.
    /// </summary>
    public static class GameOptions
    {
        static PlayerOptions s_Current = new();
        static OptionsStore s_Store;

        /// <summary>Tests: options read and written here instead of the player's folder.</summary>
        public static string DirectoryOverride { get; set; }

        public static PlayerOptions Current => s_Current;

        /// <summary>The options changed (and are saved).</summary>
        public static event Action Changed;

        static OptionsStore Store => s_Store ??= new OptionsStore(DirectoryOverride ?? Application.persistentDataPath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Current = new PlayerOptions();
            s_Store = null;
            Changed = null;
        }

        /// <summary>Before the first scene: the options from their file, applied.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void LoadAtStart() => Reload(applyDisplay: true);

        /// <summary>Reads the options file again (tests, and the start of play).</summary>
        public static void Reload(bool applyDisplay = false)
        {
            s_Store = null;
            s_Current = Store.Load();
            Apply(applyDisplay);
            Changed?.Invoke();
        }

        /// <summary>Changes the options: <paramref name="change"/> edits a copy, which is checked, saved and applied.</summary>
        public static void Change(Action<PlayerOptions> change, bool display = false)
        {
            PlayerOptions next = s_Current.Clone();
            change?.Invoke(next);
            s_Current = OptionsRules.Sanitized(next);
            Store.Save(s_Current);
            Apply(display);
            Changed?.Invoke();
        }

        /// <summary>Back to the defaults (saved).</summary>
        public static void ResetToDefaults() => Change(o =>
        {
            PlayerOptions d = new();
            o.masterVolume = d.masterVolume;
            o.musicVolume = d.musicVolume;
            o.effectsVolume = d.effectsVolume;
            o.shake = d.shake;
            o.flashes = d.flashes;
            o.hitStop = d.hitStop;
            o.vibration = d.vibration;
            o.vibrationIntensity = d.vibrationIntensity;
            o.reducedVibration = d.reducedVibration;
            o.textSpeed = d.textSpeed;
            o.relaxedTiming = d.relaxedTiming;
            o.patientCustomers = d.patientCustomers;
        });

        static void Apply(bool display)
        {
            PlayerOptions o = s_Current;
            GameSettings.ScreenShakeScale = OptionsRules.ShakeScale(o.shake);
            GameSettings.FlashEnabled = o.flashes;
            GameSettings.HitStopEnabled = o.hitStop;
            GameSettings.VibrationEnabled = o.vibration;
            GameSettings.VibrationIntensity = o.vibrationIntensity;
            GameSettings.ReducedVibration = o.reducedVibration;
            if (display) ApplyDisplay(o);
        }

        /// <summary>Desktop only: fullscreen, or a window of whole multiples of 320×180. (The web's fullscreen is the page's, asked for by a click.)</summary>
        static void ApplyDisplay(PlayerOptions o)
        {
            if (Application.isEditor || Application.platform == RuntimePlatform.WebGLPlayer) return;
            Resolution display = Screen.currentResolution;
            if (o.fullscreen) Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
            else
            {
                int k = OptionsRules.WindowScale(o.windowScale, display.width, display.height);
                Screen.SetResolution(320 * k, 180 * k, FullScreenMode.Windowed);
            }
        }
    }
}
