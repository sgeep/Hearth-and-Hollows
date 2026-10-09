using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Settings
{
    public enum ShakeLevel
    {
        Off,
        Low,
        Full,
    }

    public enum TextSpeed
    {
        Slow,
        Normal,
        Instant,
    }

    /// <summary>
    /// The player's options (4i-B, decision D4): the player's, not the keeper's. Kept in their own file (<see cref="OptionsStore"/>),
    /// never in <c>SaveData</c>, so they survive relaunching, Continue and New Game. Plain data with defaults; versioned so a later
    /// field can be added without losing the rest.
    /// </summary>
    [Serializable]
    public sealed class PlayerOptions
    {
        /// <summary>2 (4i-C playtest): the music slider's 100% became what 25% was, so an older file's music is scaled to match.</summary>
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;

        // ---------- Audio (0–1, in steps of 5%) ----------
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float effectsVolume = 1f;

        // ---------- Feel ----------
        public ShakeLevel shake = ShakeLevel.Full;
        public bool flashes = true;
        public bool hitStop = true;
        public bool vibration = true;
        public float vibrationIntensity = 1f;
        public bool reducedVibration;

        // ---------- Display (desktop; the web has only fullscreen) ----------
        public bool fullscreen = true;
        /// <summary>The window's size as a whole multiple of 320×180 (desktop, windowed). 0: not chosen yet (the largest that fits).</summary>
        public int windowScale;

        // ---------- Accessibility ----------
        public TextSpeed textSpeed = TextSpeed.Normal;
        /// <summary>Wider target bands and a gentler pace at the grill, the tap and the chopping board (the keeper's own cooking only).</summary>
        public bool relaxedTiming;
        /// <summary>Customers wait longer before giving up.</summary>
        public bool patientCustomers;

        public PlayerOptions Clone() => (PlayerOptions)MemberwiseClone();
    }

    /// <summary>What the options mean in play (4i-B; pure, EditMode-tested).</summary>
    public static class OptionsRules
    {
        /// <summary>Volume sliders move in 5% steps.</summary>
        public const float VolumeStep = 0.05f;

        /// <summary>Relaxed timing: the target bands this much wider (about their centres).</summary>
        public const float RelaxedBandScale = 1.6f;
        /// <summary>Relaxed timing: the meters and needles this much slower.</summary>
        public const float RelaxedPace = 0.8f;
        /// <summary>Patient customers wait this much longer.</summary>
        public const float PatientScale = 1.6f;

        /// <summary>A volume moved by <paramref name="steps"/> steps of 5%, clamped and kept on the grid.</summary>
        public static float StepVolume(float volume, int steps) => Mathf.Clamp01(Mathf.Round(volume / VolumeStep + steps) * VolumeStep);

        /// <summary>A 0–1 volume as the mixer's decibels: 0 is silence (−80 dB), 1 is 0 dB.</summary>
        public static float ToDecibels(float volume) => volume <= 0.0001f ? -80f : Mathf.Max(-80f, 20f * Mathf.Log10(volume));

        /// <summary>How hard screens shake.</summary>
        public static float ShakeScale(ShakeLevel level) => level switch
        {
            ShakeLevel.Off => 0f,
            ShakeLevel.Low => 0.4f,
            _ => 1f,
        };

        /// <summary>How fast dialogue is written out, as a multiple of the authored speed; instant is infinite.</summary>
        public static float TextSpeedScale(TextSpeed speed) => speed switch
        {
            TextSpeed.Slow => 0.55f,
            TextSpeed.Instant => float.PositiveInfinity,
            _ => 1f,
        };

        public static float Patience(bool patient) => patient ? PatientScale : 1f;

        /// <summary>The window sizes on offer: whole multiples of 320×180 that fit the display (always at least 1×).</summary>
        public static List<int> WindowScales(int displayWidth, int displayHeight)
        {
            var scales = new List<int>();
            for (int k = 1; k <= 12; k++)
                if (320 * k <= displayWidth && 180 * k <= displayHeight) scales.Add(k);
            if (scales.Count == 0) scales.Add(1);
            return scales;
        }

        /// <summary>The window scale to use: the chosen one if it still fits, else the largest that does.</summary>
        public static int WindowScale(int chosen, int displayWidth, int displayHeight)
        {
            List<int> scales = WindowScales(displayWidth, displayHeight);
            return scales.Contains(chosen) ? chosen : scales[^1];
        }

        /// <summary>
        /// Version 2: the music's 100% is a quarter of what it was (the owner's call after the 4i-C playtest), so an older file's
        /// music slider is multiplied by this to sound the same (25% then is 100% now), up to the top.
        /// </summary>
        public const float MusicRescaleFromVersion1 = 4f;

        /// <summary>Anything out of range (an edited or older file) brought back into range, and an older file brought up to date.</summary>
        public static PlayerOptions Sanitized(PlayerOptions o)
        {
            o ??= new PlayerOptions();
            if (o.version < 2) o.musicVolume = Mathf.Clamp01(o.musicVolume * MusicRescaleFromVersion1);
            o.masterVolume = StepVolume(o.masterVolume, 0);
            o.musicVolume = StepVolume(o.musicVolume, 0);
            o.effectsVolume = StepVolume(o.effectsVolume, 0);
            o.vibrationIntensity = Mathf.Clamp(StepVolume(o.vibrationIntensity, 0), 0.1f, 1f);
            if (!Enum.IsDefined(typeof(ShakeLevel), o.shake)) o.shake = ShakeLevel.Full;
            if (!Enum.IsDefined(typeof(TextSpeed), o.textSpeed)) o.textSpeed = TextSpeed.Normal;
            o.windowScale = Mathf.Clamp(o.windowScale, 0, 12);
            o.version = PlayerOptions.CurrentVersion;
            return o;
        }
    }
}
