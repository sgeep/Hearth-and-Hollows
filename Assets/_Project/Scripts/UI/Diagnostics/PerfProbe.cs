using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;

namespace Hearthdelve.UI.Diagnostics
{
    /// <summary>
    /// 4i-D's performance pass, in any build (release included) but only when asked: <c>?perf</c> in the web build's address,
    /// <c>-perf</c> on the Windows command line. It drives a scripted day the way the tests do (a new game, its delve, the night,
    /// sleep, the daytime indoors and out, a busy evening, the troll fight) and records the time to the menu, each change of
    /// scenes, the frame times in each stretch and the memory, then reports: on the web, posted to the page's own server
    /// (<c>perf-report</c>) and logged; on Windows, a file beside the log. It never runs otherwise, and saves nowhere real (it
    /// plays in a game of its own, written over by the next New Game).
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        readonly StringBuilder m_Report = new();
        readonly List<float> m_Frames = new();

        static bool Requested =>
            Environment.GetCommandLineArgs().Any(a => a.Equals("-perf", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(Application.absoluteURL) && Application.absoluteURL.Contains("perf"));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartIfAsked()
        {
            if (!Requested) return;
            // Its own save folder, set before Boot's GameFlow opens the store: the probe's new game never touches the player's save.
            GameFlow.SaveDirectoryOverride = Path.Combine(Application.persistentDataPath, "perf");
            var go = new GameObject("PerfProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<PerfProbe>();
        }

        /// <summary>How many days to play (<c>-perfDays=3</c>, or <c>perfdays=3</c> in the address): memory over several days.</summary>
        static int Days
        {
            get
            {
                string arg = Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith("-perfDays=", StringComparison.OrdinalIgnoreCase));
                string url = Application.absoluteURL ?? string.Empty;
                int at = url.IndexOf("perfdays=", StringComparison.OrdinalIgnoreCase);
                string value = arg != null ? arg.Substring("-perfDays=".Length) : at >= 0 ? new string(url.Substring(at + "perfdays=".Length).TakeWhile(char.IsDigit).ToArray()) : "1";
                return int.TryParse(value, out int n) ? Mathf.Clamp(n, 1, 10) : 1;
            }
        }

        static bool Shots => Environment.GetCommandLineArgs().Any(a => a.Equals("-perfShots", StringComparison.OrdinalIgnoreCase));

        /// <summary>With <c>-perfShots</c>: a picture of the stage (the tester kit's screenshots come from the release build).</summary>
        IEnumerator Shot(string name)
        {
            if (!Shots) yield break;
            yield return new WaitForEndOfFrame();
            string folder = Path.Combine(Application.persistentDataPath, "shots");
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, name + ".png"));
            yield return null;
        }

        /// <summary>Plays a conversation through (Space on a virtual keyboard), as a player would.</summary>
        IEnumerator TalkThrough()
        {
            var keys = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("ProbeKeyboard");
            float until = Time.realtimeSinceStartup + 30f;
            while (Hearthdelve.Shared.Story.StoryServices.Conversations != null && Hearthdelve.Shared.Story.StoryServices.Conversations.IsTalking && Time.realtimeSinceStartup < until)
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Space));
                yield return null;
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keys, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                yield return new WaitForSecondsRealtime(0.25f);
            }
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keys);
        }

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;

        void Line(string text)
        {
            m_Report.AppendLine(text);
            Debug.Log("[Perf] " + text);
        }

        static string Memory()
        {
            long reserved = Profiler.GetTotalReservedMemoryLong(), allocated = Profiler.GetTotalAllocatedMemoryLong(), mono = Profiler.GetMonoUsedSizeLong();
            var music = Hearthdelve.Shared.Audio.MusicDirector.Instance;
            string tracks = music == null || music.Config == null ? "none" : string.Join(" ", music.Config.tracks
                .Where(t => t.clip != null && t.clip.loadState == AudioDataLoadState.Loaded).Select(t => t.clip.name));
            return $"memory: reserved {reserved / 1048576} MB, allocated {allocated / 1048576} MB, managed {mono / 1048576} MB, gc {GC.GetTotalMemory(false) / 1048576} MB; music loaded: {(tracks.Length == 0 ? "none" : tracks)}";
        }

        IEnumerator Until(Func<bool> done, float limit)
        {
            float until = Time.realtimeSinceStartup + limit;
            while (!done() && Time.realtimeSinceStartup < until) yield return null;
        }

        /// <summary>One change of scenes: from the call until it's loaded and uncovered.</summary>
        IEnumerator Swap(string name, Action begin, Func<bool> arrived)
        {
            float start = Time.realtimeSinceStartup;
            begin();
            yield return null;
            yield return Until(() => !Flow.IsLoading && arrived() && (Flow.Transition == null || !Flow.Transition.IsCovering), 60f);
            Line($"swap {name}: {Time.realtimeSinceStartup - start:0.00} s {(arrived() ? "" : "(TIMED OUT)")}");
        }

        /// <summary>Frame times over a stretch of play: mean, 95th percentile, worst, and the share over a 60 fps frame.</summary>
        IEnumerator Stretch(string name, float seconds)
        {
            m_Frames.Clear();
            float until = Time.realtimeSinceStartup + seconds;
            yield return null;
            while (Time.realtimeSinceStartup < until)
            {
                m_Frames.Add(Time.unscaledDeltaTime * 1000f);
                yield return null;
            }
            if (m_Frames.Count == 0)
            {
                Line($"frames {name}: none (the page was hidden or stalled); {Memory()}");
                yield break;
            }
            var sorted = m_Frames.OrderBy(f => f).ToList();
            float p95 = sorted[Mathf.Clamp((int)(sorted.Count * 0.95f), 0, sorted.Count - 1)];
            float slow = sorted.Count(f => f > 17.5f) * 100f / sorted.Count;
            Line($"frames {name}: {sorted.Count} frames, mean {sorted.Average():0.0} ms, p95 {p95:0.0} ms, worst {sorted[^1]:0.0} ms, over 17.5 ms {slow:0}%; {Memory()}");
        }

        IEnumerator Start()
        {
            Line($"Hearth & Hollows {Application.version}, {Application.platform}, {SystemInfo.graphicsDeviceName}, {SystemInfo.processorType}, " +
                 $"{SystemInfo.systemMemorySize} MB, vSync {QualitySettings.vSyncCount}, {Screen.width}x{Screen.height}");
            yield return Until(() => Flow != null && !Flow.IsLoading && FindAnyObjectByType<MainMenuScreen>() != null && Loc.IsReady
                                     && (Flow.Transition == null || !Flow.Transition.IsCovering), 120f);
            Line($"startup to the menu: {Time.realtimeSinceStartup:0.00} s; {Memory()}");
            yield return Stretch("the menu", 3f);
            yield return Shot("1_menu");

            yield return Swap("menu to the first delve", () => Flow.QuickNewGame(), () => Flow.LoadedScene == GameScenes.Dungeon);
            yield return Stretch("the first room", 5f);
            yield return Shot("2_hollows");
            yield return Swap("delve to the night", () => Flow.CompleteDelve(DelveReport.Empty), () => Director != null && Director.Phase == TavernPhase.Night);
            yield return Stretch("the night", 3f);
            if (Shots && Hearthdelve.Shared.Story.StoryServices.Conversations != null && Hearthdelve.Shared.Story.StoryServices.Conversations.Talk("pip"))
            {
                yield return new WaitForSecondsRealtime(2.5f);
                yield return Shot("3_talking");
                yield return TalkThrough();
            }
            int days = Days;
            for (int day = 1; day <= days; day++)
            {
                if (day > 1)
                {
                    Line($"--- day {day + 1} ---");
                    yield return Swap("the arena to the night", () => Flow.CompleteDelve(DelveReport.Empty), () => Director != null && Director.Phase == TavernPhase.Night);
                }
                yield return Swap("night to the day (Tally Ho! and Kariaston)", () => Flow.Sleep(), () => Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston));
                yield return Stretch("the day, upstairs", 5f);
                if (day == 1) yield return Shot("4_upstairs");
                GameObject keeper = GameObject.FindGameObjectWithTag("Player");
                if (keeper != null && keeper.TryGetComponent(out Rigidbody2D body))
                {
                    SurfaceDoor.Find(SurfaceDoor.FrontInside)?.Pass(body);
                    yield return null;
                    yield return Stretch("the day, in Kariaston", 8f);
                    if (day == 1) yield return Shot("5_kariaston");
                    // Decorate Mode holds its own tune over the day's (the music's known memory risk on the web).
                    SurfaceDoor.Find(SurfaceDoor.FrontOutside)?.Pass(body);
                    yield return null;
                    if (DecorateMode.Instance != null)
                    {
                        DecorateMode.Instance.Enter();
                        yield return Stretch("Decorate Mode", 5f);
                        if (day == 1) yield return Shot("6_decorate");
                        DecorateMode.Instance.Leave();
                    }
                }
                yield return Swap("day to the evening", () => Flow.StartEvening(), () => Director != null && Director.Phase == TavernPhase.Prep);
                Director.FillStoreroom();
                Director.OpenDebugEvening();
                yield return Until(() => Director.Phase == TavernPhase.Service, 10f);
                Director.ArrivalsPaused = true;
                for (int i = 0; i < 8; i++)
                {
                    Director.SpawnCustomer();
                    yield return new WaitForSecondsRealtime(0.5f);
                }
                yield return Stretch("a busy service (eight customers)", 10f);
                if (day == 1) yield return Shot("7_service");
                Director.EndServiceNow();
                yield return Until(() => Director.Phase == TavernPhase.Results, 15f);
                PerfOptions.StartInArena = true;
                yield return Swap("the evening to the arena", () => Director.FinishEvening(), () => Flow.LoadedScene == GameScenes.Dungeon);
                yield return Stretch("the troll fight", 10f);
                if (day == 1) yield return Shot("8_troll");
                PerfOptions.StartInArena = false;
            }
            Line($"done at {Time.realtimeSinceStartup:0.0} s; {Memory()}");
            yield return Report();
        }

        IEnumerator Report()
        {
            string text = m_Report.ToString();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                using var post = new UnityWebRequest("perf-report", "POST") { uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(text)), downloadHandler = new DownloadHandlerBuffer() };
                yield return post.SendWebRequest();
                Debug.Log($"[Perf] report posted: {post.result}");
            }
            else
            {
                string path = Path.Combine(Application.persistentDataPath, $"perf_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, text);
                Debug.Log($"[Perf] report written to {path}");
                if (Environment.GetCommandLineArgs().Contains("-perfQuit")) Application.Quit();
            }
        }
    }
}
