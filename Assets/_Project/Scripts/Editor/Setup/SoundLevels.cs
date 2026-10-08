using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-C's audio balance: how loud each approved clip really is (RMS over its sounding part, and its peak, in dBFS), so each
    /// family plays at its kind's level (<see cref="SoundBank.Family.Target"/>) whatever its library's mastering. Pure measurement;
    /// <see cref="SoundBank"/> turns it into each family's volume. <c>Hearthdelve → Report → Sound Levels</c> writes the table.
    /// </summary>
    public static class SoundLevels
    {
        /// <summary>RMS of the samples above a floor (silence at either end doesn't dilute it), and the peak, in dBFS. Null: unreadable.</summary>
        public static (float rms, float peak)? Measure(AudioClip clip)
        {
            if (clip == null) return null;
            if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            if (data.Length == 0 || !clip.GetData(data, 0)) return null;
            float peak = data.Max(Math.Abs);
            if (peak <= 0f) return null;
            float floor = peak * 0.05f;
            double sum = 0;
            int n = 0;
            foreach (float v in data)
                if (Math.Abs(v) >= floor)
                {
                    sum += v * v;
                    n++;
                }
            float rms = (float)Math.Sqrt(sum / Math.Max(1, n));
            return (20f * Mathf.Log10(rms), 20f * Mathf.Log10(peak));
        }

        /// <summary>A family's loudness: the mean of its clips' RMS (dBFS), and the loudest peak.</summary>
        public static (float rms, float peak)? Measure(SoundBank.Family family)
        {
            var levels = SoundBank.Clips(family).Select(Measure).Where(l => l.HasValue).Select(l => l.Value).ToList();
            if (levels.Count == 0) return null;
            return (levels.Average(l => l.rms), levels.Max(l => l.peak));
        }

        [MenuItem("Hearthdelve/Report/Sound Levels", priority = 41)]
        public static void WriteMenu()
        {
            var sb = new StringBuilder("| Family | Kind | RMS dBFS | Peak dBFS | Volume |\n|---|---|---:|---:|---:|\n");
            foreach (SoundBank.Family f in SoundBank.Families)
            {
                var m = Measure(f);
                sb.AppendLine(m.HasValue
                    ? $"| {f.Key} | {f.Target} | {m.Value.rms:0.0} | {m.Value.peak:0.0} | {SoundBank.Volume(f):0.00} |"
                    : $"| {f.Key} | {f.Target} | (unreadable) | | {SoundBank.Volume(f):0.00} |");
            }
            Directory.CreateDirectory("BatchLogs");
            File.WriteAllText("BatchLogs/sound_levels.md", sb.ToString());
            Debug.Log("[Hearthdelve] Sound levels → BatchLogs/sound_levels.md");
        }

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
    }
}
