using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Game;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4i-D's builds (decision D7), each from the one menu (<c>Hearthdelve/Build</c>) and its own batch method: the Web release
    /// (Brotli, the decompression fallback for hosts without its headers, no development code), the Web development build (the
    /// owner's, uncompressed, as before), the Windows build for testers (IL2CPP) and the internal Windows build (Mono, quick).
    /// Every build stamps the version from git (<see cref="VersionStamp"/>) for its own length and puts the project's settings
    /// back afterwards, so building never changes what's committed.
    /// </summary>
    public static class ReleaseBuilds
    {
        public const string WebReleaseFolder = "Builds/WebRelease";
        public const string WebDevelopmentFolder = BuildTools.WebBuildFolder;
        public const string WindowsFolder = "Builds/Windows";
        public const string WindowsInternalFolder = "Builds/WindowsMono";
        /// <summary>The player's file name (the save folder comes from the Product Name, which stays "Hearthdelve": decision D7).</summary>
        public const string ExeName = "HearthAndHollows.exe";

        public enum Kind { WebRelease, WebDevelopment, Windows, WindowsInternal }

        /// <summary>What each build is, as data (the build-script test reads these).</summary>
        public readonly struct Settings
        {
            public readonly BuildTarget Target;
            public readonly string Location;
            public readonly BuildOptions Options;
            public readonly WebGLCompressionFormat Compression;
            public readonly bool DecompressionFallback;
            public readonly ScriptingImplementation Backend;

            public Settings(BuildTarget target, string location, BuildOptions options, WebGLCompressionFormat compression, bool fallback, ScriptingImplementation backend)
            {
                Target = target;
                Location = location;
                Options = options;
                Compression = compression;
                DecompressionFallback = fallback;
                Backend = backend;
            }

            public bool IsDevelopment => (Options & BuildOptions.Development) != 0;
        }

        public static Settings For(Kind kind) => kind switch
        {
            Kind.WebRelease => new Settings(BuildTarget.WebGL, WebReleaseFolder, BuildOptions.None, WebGLCompressionFormat.Brotli, true, ScriptingImplementation.IL2CPP),
            Kind.WebDevelopment => new Settings(BuildTarget.WebGL, WebDevelopmentFolder, BuildOptions.Development, WebGLCompressionFormat.Disabled, true, ScriptingImplementation.IL2CPP),
            Kind.Windows => new Settings(BuildTarget.StandaloneWindows64, Path.Combine(WindowsFolder, ExeName), BuildOptions.None, default, false, ScriptingImplementation.IL2CPP),
            _ => new Settings(BuildTarget.StandaloneWindows64, Path.Combine(WindowsInternalFolder, ExeName), BuildOptions.None, default, false, ScriptingImplementation.Mono2x),
        };

        // ---------- the one menu ----------

        [MenuItem("Hearthdelve/Build/Web (release, Brotli)", priority = 200)]
        public static void WebReleaseMenu() => Build(Kind.WebRelease);

        [MenuItem("Hearthdelve/Build/Web (development)", priority = 201)]
        public static void WebDevelopmentMenu() => Build(Kind.WebDevelopment);

        [MenuItem("Hearthdelve/Build/Windows (IL2CPP, for testers)", priority = 210)]
        public static void WindowsMenu() => Build(Kind.Windows);

        [MenuItem("Hearthdelve/Build/Windows (Mono, internal)", priority = 211)]
        public static void WindowsInternalMenu() => Build(Kind.WindowsInternal);

        // ---------- the batch methods (-executeMethod Hearthdelve.Editor.ReleaseBuilds.…) ----------

        public static void WebReleaseBatch() => EditorApplication.Exit(Build(Kind.WebRelease) ? 0 : 1);
        public static void WebDevelopmentBatch() => EditorApplication.Exit(Build(Kind.WebDevelopment) ? 0 : 1);
        public static void WindowsBatch() => EditorApplication.Exit(Build(Kind.Windows) ? 0 : 1);
        public static void WindowsInternalBatch() => EditorApplication.Exit(Build(Kind.WindowsInternal) ? 0 : 1);

        // ---------- the version ----------

        /// <summary>The version this build carries, from git (the commit count on HEAD, its short hash, and whether the tree is dirty).</summary>
        public static string CurrentVersion()
        {
            string count = Git("rev-list --count HEAD"), sha = Git("rev-parse --short=8 HEAD"), status = Git("status --porcelain --untracked-files=no");
            int build = int.TryParse(count, out int n) ? n : 0;
            return VersionStamp.Format(VersionStamp.Milestone, build, sha, !string.IsNullOrWhiteSpace(status));
        }

        static string Git(string args)
        {
            try
            {
                using var git = Process.Start(new ProcessStartInfo("git", args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                });
                string output = git.StandardOutput.ReadToEnd();
                git.WaitForExit(10000);
                return output.Trim();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hearthdelve] git {args}: {e.Message}");
                return string.Empty;
            }
        }

        // ---------- building ----------

        public static bool Build(Kind kind)
        {
            Settings s = For(kind);
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(s.Target);
            if (!BuildPipeline.IsBuildTargetSupported(group, s.Target))
            {
                Debug.LogError($"[Hearthdelve] {kind}: the {s.Target} build module isn't installed for this editor (Unity Hub > Installs > Add modules).");
                return false;
            }
            NamedBuildTarget named = NamedBuildTarget.FromBuildTargetGroup(group);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(named, s.Target))
            {
                Debug.LogError($"[Hearthdelve] {kind}: could not switch the active build target to {s.Target}.");
                return false;
            }

            // What a build changes, put back after it.
            string version = PlayerSettings.bundleVersion;
            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(named);
            WebGLCompressionFormat compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback;
            try
            {
                string stamped = CurrentVersion();
                PlayerSettings.bundleVersion = stamped;
                PlayerSettings.SetScriptingBackend(named, s.Backend);
                if (s.Target == BuildTarget.WebGL)
                {
                    PlayerSettings.WebGL.compressionFormat = s.Compression;
                    PlayerSettings.WebGL.decompressionFallback = s.DecompressionFallback;
                    // 4i-A: the game's try/catch must work in the browser (with "None", even a caught exception stops the page).
                    PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
                }

                // Localization stores its string tables in Addressables; the player needs them built for this target.
                AddressableAssetSettings.BuildPlayerContent(out var content);
                if (!string.IsNullOrEmpty(content.Error))
                {
                    Debug.LogError($"[Hearthdelve] {kind}: Addressables content build failed: {content.Error}");
                    return false;
                }

                string folder = s.Target == BuildTarget.WebGL ? s.Location : Path.GetDirectoryName(s.Location);
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                string[] scenes = EditorBuildSettings.scenes.Where(e => e.enabled && File.Exists(e.path)).Select(e => e.path).ToArray();
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = s.Location, target = s.Target, options = s.Options });
                bool ok = report.summary.result == BuildResult.Succeeded;
                if (ok) File.WriteAllText(Path.Combine(folder, "version.txt"), stamped + Environment.NewLine);
                Debug.Log($"[Hearthdelve] {kind} build {report.summary.result} ({stamped}): {report.summary.totalErrors} errors, " +
                          $"{report.summary.totalSize / (1024 * 1024)} MB at {folder}, {report.summary.totalTime.TotalMinutes:0.0} min.");
                return ok;
            }
            finally
            {
                PlayerSettings.bundleVersion = version;
                PlayerSettings.SetScriptingBackend(named, backend);
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
