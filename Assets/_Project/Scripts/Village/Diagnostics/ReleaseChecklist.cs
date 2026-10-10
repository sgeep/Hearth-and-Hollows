using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Networking;

namespace Hearthdelve.Village.Diagnostics
{
    /// <summary>
    /// 4i-D: the final 4h playtest checklist's mechanical half, run inside a release build (<c>?checklist</c> on the web,
    /// <c>-checklist</c> on Windows), so what the editor's tests prove is seen to hold in the shipped player too (IL2CPP, stripping,
    /// the web). The checklist's judgements (does Gimp's night unsettle, does dinner feel like the village's) stay the owner's. It
    /// plays a quick new game in a save folder of its own, presses keys on a virtual keyboard to play conversations through, and
    /// reports PASS or FAIL per item (posted to the page's server on the web, a file beside the log on Windows).
    /// </summary>
    public sealed class ReleaseChecklist : MonoBehaviour
    {
        readonly StringBuilder m_Report = new();
        readonly object m_Hold = new();
        Keyboard m_Keys;
        int m_Passed, m_Failed;

        static bool Requested =>
            Environment.GetCommandLineArgs().Any(a => a.Equals("-checklist", StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(Application.absoluteURL) && Application.absoluteURL.Contains("checklist"));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartIfAsked()
        {
            if (!Requested) return;
            GameFlow.SaveDirectoryOverride = Path.Combine(Application.persistentDataPath, "checklist");
            var go = new GameObject("ReleaseChecklist");
            DontDestroyOnLoad(go);
            go.AddComponent<ReleaseChecklist>();
        }

        static GameFlow Flow => GameFlow.Instance;
        static TavernDirector Director => TavernDirector.Instance;
        static Rigidbody2D Keeper => GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
        static readonly Vector2 k_Origin = new(200f, 0f);

        void Line(string text)
        {
            m_Report.AppendLine(text);
            Debug.Log("[Checklist] " + text);
        }

        void Check(string item, bool ok, string detail = "")
        {
            if (ok) m_Passed++;
            else m_Failed++;
            Line($"{(ok ? "PASS" : "FAIL")}  {item}{(string.IsNullOrEmpty(detail) ? "" : $" ({detail})")}");
        }

        static IEnumerator Until(Func<bool> done, float limit)
        {
            float until = Time.realtimeSinceStartup + limit;
            while (!done() && Time.realtimeSinceStartup < until) yield return null;
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static bool Settled => !Flow.IsLoading && (Flow.Transition == null || !Flow.Transition.IsCovering);
        static bool InDaytime => Settled && Director != null && Director.Phase == TavernPhase.Daytime && Flow.IsLoaded(GameScenes.Kariaston);

        /// <summary>Plays the conversation through as a player would: Space, until it ends (a choice takes its first answer).</summary>
        IEnumerator TalkThrough(float limit = 40f)
        {
            float until = Time.realtimeSinceStartup + limit;
            while (StoryServices.Conversations != null && StoryServices.Conversations.IsTalking && Time.realtimeSinceStartup < until)
            {
                InputSystem.QueueStateEvent(m_Keys, new KeyboardState(Key.Space));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(m_Keys, new KeyboardState());
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        IEnumerator At(int minute)
        {
            Flow.State.Surface.Restore(minute);
            yield return Frames(2);
            VillagePresence.Instance?.Refresh();
            yield return Frames(2);
        }

        static Villager Shown(string id) => Villager.All.FirstOrDefault(v => v.CharacterId == id && v.Shown);

        static void KeeperAt(Vector2 world)
        {
            Rigidbody2D body = Keeper;
            body.position = world;
            body.transform.position = new Vector3(world.x, world.y, body.transform.position.z);
            Physics2D.SyncTransforms();
        }

        static void Outside()
        {
            if (SurfaceArea.Current == null || SurfaceArea.Current.Id != SurfaceArea.KariastonId) SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
        }

        static void Inside()
        {
            if (SurfaceArea.Current != null && SurfaceArea.Current.Id == SurfaceArea.KariastonId) SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            SurfaceArea main = SurfaceArea.Find(SurfaceArea.TavernId);
            PropertyArea.Current = main.GetComponent<PropertyArea>();
            SurfaceArea.Enter(main);
        }

        /// <summary>The evening (dinner served to whoever comes: who are the familiar faces?), the delve, the night and sleep.</summary>
        IEnumerator Evening(int day)
        {
            SurfacePause.Release(m_Hold);
            Flow.StartEvening();
            yield return Until(() => Settled && Director != null && Director.Phase == TavernPhase.Prep, 30f);
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            yield return Until(() => Director.Phase == TavernPhase.Service, 10f);
            var faces = Director.FamiliarFaces.Select(f => f.character).ToList();
            m_Faces += faces.Count;
            Line($"day {day}'s dinner: familiar faces planned tonight: {(faces.Count == 0 ? "none" : string.Join(", ", faces))}");
            Director.EndServiceNow();
            yield return Until(() => Director.Phase == TavernPhase.Results, 15f);
            Director.FinishEvening();
            yield return Until(() => Settled && Flow.LoadedScene == GameScenes.Dungeon, 30f);
            Flow.CompleteDelve(DelveReport.Empty);
            yield return Until(() => Settled && Director != null && Director.Phase == TavernPhase.Night, 30f);
            Flow.Sleep();
            yield return Until(() => InDaytime, 30f);
            SurfacePause.Hold(m_Hold);
            yield return Frames(3);
        }

        int m_Faces;

        IEnumerator Start()
        {
            m_Keys = InputSystem.AddDevice<Keyboard>("ChecklistKeyboard");
            // Launched from a script, the window may not have focus, and the Input System ignores devices without it (the PlayMode
            // tests set the same).
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Line($"Hearth & Hollows {Application.version}, {Application.platform}: the final 4h checklist's mechanical half");
            yield return Until(() => Flow != null && Settled && Flow.LoadedScene == GameScenes.MainMenu, 120f);
            VillageLifeSettings settings = VillageLife.Settings;
            GameFlow.NewGameSeedOverride = Enumerable.Range(1, 100000).First(s => VillageDays.GimpVisit(s, 4, settings) && !VillageDays.GimpVisit(s, 3, settings));
            Flow.QuickNewGame();
            GameFlow.NewGameSeedOverride = null;
            yield return Until(() => Settled && Flow.LoadedScene == GameScenes.Dungeon, 30f);
            Flow.CompleteDelve(DelveReport.Empty);
            yield return Until(() => Settled && Director != null && Director.Phase == TavernPhase.Night, 30f);
            Flow.Sleep();
            yield return Until(() => InDaytime, 30f);
            SurfacePause.Hold(m_Hold);
            yield return Frames(3);
            Line($"day {Flow.State.Day}, world seed {Flow.State.WorldSeed}");

            // 6. Decorate Tally Ho! while Maximo's at lunch: the room holds still, and he finds his seat after.
            Inside();
            KeeperAt(new Vector2(14f, 3f));
            yield return At(10 * 60 + 50);
            yield return At(11 * 60);
            yield return Until(() => Villager.All.Any(v => v.CharacterId == CharacterIds.Maximo && v.Shown && v.Area == PropertyArea.TavernId && v.Walking), 25f);
            Villager maximo = Villager.All.FirstOrDefault(v => v.CharacterId == CharacterIds.Maximo && v.Shown && v.Area == PropertyArea.TavernId);
            if (maximo == null) Check("6. Maximo comes in for lunch, and Decorate Mode holds the room still", false, "he never came in");
            else
            {
                DecorateMode.Instance.Enter();
                yield return Frames(2);
                Vector3 held = maximo.transform.position;
                yield return new WaitForSecondsRealtime(1f);
                bool still = Vector3.Distance(maximo.transform.position, held) < 0.01f;
                DecorateMode.Instance.Leave();
                yield return Until(() => !maximo.Walking, 30f);
                bool seated = Vector2.Distance(maximo.transform.position, ScheduleAnchor.Find("tavern.table").Spot) < 0.05f;
                Check("6. Maximo comes in for lunch, and Decorate Mode holds the room still", still && seated, $"held {still}, at his seat after {seated}");
            }

            // 3. Near the market at midday: an exchange is overheard. Overheard moments play only while the day goes on, so the
            // clock runs here, slowed right down (the PlayMode test does the same).
            Outside();
            KeeperAt(k_Origin + new Vector2(38f, 9f));
            yield return At(12 * 60);
            Villager bart = Shown(CharacterIds.Bart);
            yield return Until(() => (bart = Shown(CharacterIds.Bart)) != null && !bart.Walking, 40f);
            SurfaceClockSettings normal = SurfaceTime.CurrentSettings, slow = normal;
            slow.realSecondsPerGameMinute = 1000f;
            SurfaceTime.SettingsOverride = slow;
            SurfacePause.Release(m_Hold);
            yield return Until(() => StoryServices.Barks != null && StoryServices.Barks.IsBarking, 45f);
            Check("3. Near the market at midday, the village is overheard", StoryServices.Barks != null && StoryServices.Barks.IsBarking);
            StoryServices.Barks?.Stop();
            SurfacePause.Hold(m_Hold);
            SurfaceTime.SettingsOverride = null;
            yield return Evening(2);

            // 1. Day 3's morning: Gimp's night in the keeper's room (it plays, and the morning follows).
            yield return Until(() => NightVisitor.Instance != null && NightVisitor.Instance.Playing, 5f);
            bool gimpNight = NightVisitor.Instance != null && NightVisitor.Instance.Playing;
            yield return Until(() => StoryServices.Conversations.IsTalking || !NightVisitor.Instance.Playing, 8f);
            bool talked = StoryServices.Conversations.IsTalking;
            yield return TalkThrough();
            yield return Until(() => !NightVisitor.Instance.Playing, 15f);
            Check("1. Day 3: Gimp comes up the hatch, talks, and the morning follows", gimpNight && talked && !NightVisitor.Instance.Playing && Flow.State.Day == 3,
                $"played {gimpNight}, talked {talked}");

            // 7. Save and Continue after Gimp's night: it never replays.
            Flow.Save();
            Flow.QuitToMenu();
            yield return Until(() => Settled && Flow.LoadedScene == GameScenes.MainMenu, 30f);
            bool continued = Flow.Continue();
            yield return Until(() => InDaytime, 30f);
            SurfacePause.Hold(m_Hold);
            yield return new WaitForSecondsRealtime(2f);
            Check("7. Save and Continue after Gimp's night: it never replays", continued && NightVisitor.Instance != null && !NightVisitor.Instance.Playing
                                                                        && !StoryServices.Conversations.IsTalking);
            yield return Evening(3);

            // 2. Day 4, one of his afternoons: Gimp comes down the stairs to Boog's table, and talks.
            Inside();
            KeeperAt(new Vector2(22f, 8f));
            yield return At(13 * 60 + 50);
            yield return At(14 * 60);
            Villager gimp = null;
            yield return Until(() => (gimp = Shown(CharacterIds.Gimp)) != null, 10f);
            yield return Until(() => gimp != null && !gimp.Walking, 40f);
            bool atTable = gimp != null && !gimp.Walking && gimp.Activity == "boog";
            bool talks = false;
            if (gimp != null && gimp.Talk.IsAvailable)
            {
                gimp.Talk.Use();
                yield return Frames(3);
                talks = StoryServices.Conversations.IsTalking;
                yield return TalkThrough();
            }
            Check("2. Day 4: Gimp at the bar with Boog on his afternoon, and he talks", atTable && talks, $"at Boog's table {atTable}, talks {talks}");

            // 5. By Ogrin's window after five: lit exactly while he's behind it.
            Outside();
            KeeperAt(k_Origin + new Vector2(50f, 9f));
            yield return At(17 * 60);
            yield return Until(() => Villager.All.Where(v => v.CharacterId == CharacterIds.Ogrin && v.Shown).All(v => !v.Walking), 40f);
            yield return new WaitForSecondsRealtime(1.6f);
            bool indoors = Villager.All.Any(v => v.CharacterId == CharacterIds.Ogrin && v.Shown && v.Indoors);
            bool lit = LitWindow.IsLitAt("ogrin.window");
            Check("5. After five, Ogrin's window is lit exactly while he's home", lit == indoors, $"home {indoors}, lit {lit}");
            yield return Evening(4);

            // 4. Familiar faces at dinner: planned on some of the evenings (none to two a night, seeded).
            Check("4. Named villagers come to dinner on some evenings", m_Faces > 0, $"{m_Faces} over three evenings");
            Line($"done: {m_Passed} passed, {m_Failed} failed");
            InputSystem.RemoveDevice(m_Keys);
            yield return Report();
        }

        IEnumerator Report()
        {
            string text = m_Report.ToString();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                using var post = new UnityWebRequest("perf-report?checklist", "POST") { uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(text)), downloadHandler = new DownloadHandlerBuffer() };
                yield return post.SendWebRequest();
            }
            else
            {
                string path = Path.Combine(Application.persistentDataPath, $"checklist_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(path, text);
                Debug.Log($"[Checklist] written to {path}");
                if (Environment.GetCommandLineArgs().Contains("-checklistQuit")) Application.Quit();
            }
        }
    }
}
