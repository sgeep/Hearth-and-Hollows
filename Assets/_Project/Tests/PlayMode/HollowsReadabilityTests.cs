using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Core.Presentation;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Game;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 2026-10-07 (the owner's playtests): at some angles the Essence bar covered the doors' reward signs. The HUD's top-left block
    /// is spoken for, and a sign under any spoken-for area steps down just clear of it, then home again.
    /// </summary>
    public class HollowsReadabilityTests : LookTestFixture
    {
        string m_SaveDir;
        static GameFlow Flow => GameFlow.Instance;

        [SetUp]
        public void UseTempSaves()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
        }

        [TearDown]
        public void ClearSaves()
        {
            GameFlow.SaveDirectoryOverride = null;
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        static Rect OnScreen(Bounds b)
        {
            Camera camera = Camera.main;
            Vector2 min = camera.WorldToScreenPoint(b.min), max = camera.WorldToScreenPoint(b.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        [UnityTest]
        public IEnumerator ADoorsSign_StepsOutFromUnderTheHud_AndBackHome()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            Flow.QuickNewGame();
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the delve");
            yield return new WaitForSecondsRealtime(0.5f);

            ScreenReservation essence = Object.FindObjectsByType<ScreenReservation>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "Essence");
            Assert.That(essence, Is.Not.Null, "the Essence bar is spoken for");
            Assert.That(essence.Area(), Is.Not.Null);

            RoomExit exit = Object.FindObjectsByType<RoomExit>(FindObjectsSortMode.None).FirstOrDefault(e => e.Marker != null);
            Assume.That(exit, Is.Not.Null, "the first room shows a reward sign");
            yield return null;
            Vector3 home = exit.MarkerPosition;
            SpriteRenderer sign = exit.GetComponentsInChildren<SpriteRenderer>().First(r => r.enabled && r.sprite == exit.Marker);
            Rect covered = OnScreen(sign.bounds);
            // Speak for the very spot the sign is drawn (as the bar does when the camera puts a door under it).
            var owner = new object();
            Rect area = Rect.MinMaxRect(covered.xMin - 4f, covered.yMin - 2f, covered.xMax + 4f, covered.yMax + 4f);
            ScreenReservations.Add(owner, () => area);
            yield return null;
            yield return null;
            Rect now = OnScreen(sign.bounds);
            Assert.That(now.yMax, Is.LessThanOrEqualTo(area.yMin + 0.5f), "the sign is clear of the area, below it");
            Assert.That(exit.MarkerPosition.x, Is.EqualTo(home.x).Within(1e-4f), "still over its own door");
            ScreenReservations.Remove(owner);
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(exit.MarkerPosition, home), Is.LessThan(1e-3f), "home again");
        }
    }
}
