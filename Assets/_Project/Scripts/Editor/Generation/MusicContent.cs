using System;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Audio;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The background music (2026-10-07): the owner's HeatleyBros tracks (WAV, kept outside the repo in C:\Dev\Music\Hearthdelve),
    /// copied in under clean names (only the ones used), imported as Vorbis, compressed in memory, loaded in the background and
    /// not preloaded (the director loads only what plays), and listed in the music config. Licence: docs/CREDITS.md.
    /// </summary>
    public static class MusicContent
    {
        public const string SourceFolder = "C:/Dev/Music/Hearthdelve";
        public const string Folder = EditorPaths.Root + "/Audio/Music";
        public const string ConfigPath = EditorPaths.Config + "/MusicConfig.asset";

        /// <summary>Cue, the clean name in the repo, and the end of the downloaded file's name.</summary>
        static readonly (MusicCue cue, string name, string source)[] k_Tracks =
        {
            (MusicCue.Decorate, "Continue", "Continue-.wav"),
            (MusicCue.Day, "Quirkii", "Quirkii.wav"),
            (MusicCue.Service, "CoastalMarket", "Coastal Market.wav"),
            (MusicCue.Cellars, "Otherworld", "Otherworld.wav"),
        };

        const float k_Quality = 0.5f;   // Vorbis quality: plenty for background music, and a modest web build

        public static MusicConfig Build()
        {
            EditorPaths.Ensure(Folder);
            foreach (var (_, name, source) in k_Tracks)
            {
                string target = $"{Folder}/{name}.wav";
                if (!File.Exists(target))
                {
                    string from = Directory.Exists(SourceFolder) ? Directory.GetFiles(SourceFolder, "*.wav").FirstOrDefault(f => f.EndsWith(source, StringComparison.OrdinalIgnoreCase)) : null;
                    if (from == null) throw new InvalidOperationException($"No '{source}' in {SourceFolder} (the music is kept outside the repo).");
                    File.Copy(from, target);
                    AssetDatabase.ImportAsset(target);
                }
                Configure(target);
            }
            MusicConfig config = LookTestContent.CreateOrUpdate<MusicConfig>(ConfigPath, c =>
            {
                foreach (var (cue, name, _) in k_Tracks)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.wav");
                    MusicTrack track = c.tracks.FirstOrDefault(t => t.cue == cue);
                    if (track == null) c.tracks.Add(track = new MusicTrack { cue = cue });
                    track.clip = clip;
                }
            });
            AssetDatabase.SaveAssets();
            return config;
        }

        static void Configure(string path)
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            bool changed = s.loadType != AudioClipLoadType.CompressedInMemory || s.compressionFormat != AudioCompressionFormat.Vorbis
                || !Mathf.Approximately(s.quality, k_Quality) || s.preloadAudioData || !importer.loadInBackground;
            if (!changed) return;
            s.loadType = AudioClipLoadType.CompressedInMemory;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = k_Quality;
            s.preloadAudioData = false;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = s;
            importer.loadInBackground = true;
            importer.forceToMono = false;
            importer.SaveAndReimport();
        }
    }
}
