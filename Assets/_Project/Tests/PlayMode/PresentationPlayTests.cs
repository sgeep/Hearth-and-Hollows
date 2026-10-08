using System;
using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Settings;
using Hearthdelve.Shared.Surface;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4i-C, presentation in the game: the credits from the main menu and the pause menu (they scroll, close, and the HeatleyBros
    /// link asks to open its address); Options lines clicked at a fast human pace through a mouse each step once (the 4i-B web note);
    /// Options driven by a gamepad.
    /// </summary>
    public class PresentationPlayTests : LookTestFixture
    {
        string m_SaveDir, m_OptionsDir;
        Mouse m_Mouse;
        Gamepad m_Pad;

        static GameFlow Flow => GameFlow.Instance;
        static PauseMenu Pause => PauseMenu.Instance;

        [SetUp]
        public void UseTempFiles()
        {
            m_SaveDir = Path.Combine(Path.GetTempPath(), "HearthdelveTests_" + Guid.NewGuid().ToString("N"));
            m_OptionsDir = Path.Combine(Path.GetTempPath(), "HearthdelveOptions_" + Guid.NewGuid().ToString("N"));
            GameFlow.SaveDirectoryOverride = m_SaveDir;
            GameOptions.DirectoryOverride = m_OptionsDir;
            GameOptions.Reload();
        }

        [TearDown]
        public void ClearFiles()
        {
            if (m_Mouse != null) InputSystem.RemoveDevice(m_Mouse);
            if (m_Pad != null) InputSystem.RemoveDevice(m_Pad);
            m_Mouse = null;
            m_Pad = null;
            UrlService.Opener = Application.OpenURL;
            if (Pause != null && Pause.IsOpen) Pause.Close();
            GameFlow.SaveDirectoryOverride = null;
            GameOptions.DirectoryOverride = null;
            GameOptions.Reload();
            SurfacePause.Clear();
            MusicHolds.Clear();
            Time.timeScale = 1f;
            MenuPause.Clear();
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
            if (Directory.Exists(m_OptionsDir)) Directory.Delete(m_OptionsDir, true);
        }

        static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        static IEnumerator Revealed()
        {
            int settled = 0;
            float started = Time.realtimeSinceStartup;
            while (settled < 5)
            {
                bool clear = !Flow.IsLoading && (Flow.Transition == null || !Flow.Transition.IsCovering);
                settled = clear ? settled + 1 : 0;
                Assert.That(Time.realtimeSinceStartup - started, Is.LessThan(8f), "the scene revealed");
                yield return null;
            }
        }

        IEnumerator Menu()
        {
            yield return SceneManager.LoadSceneAsync(GameScenes.Boot, LoadSceneMode.Single);
            yield return WaitUntil(() => Flow != null && !Flow.IsLoading && Object.FindAnyObjectByType<MainMenuScreen>() != null, 20f, "the main menu");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the tables");
            yield return Revealed();
            yield return Frames(3);
        }

        // ---------- the credits ----------

        [UnityTest]
        public IEnumerator TheCredits_OpenFromTheMenu_Scroll_OpenTheLink_AndClose()
        {
            string opened = null;
            UrlService.Opener = url => opened = url;
            yield return Menu();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            Assert.That(menu.CreditsButton, Is.Not.Null);
            Assert.That(menu.CreditsButton.transform.GetSiblingIndex(), Is.GreaterThan(menu.ControlsButton.transform.GetSiblingIndex()), "after Controls");
            menu.CreditsButton.onClick.Invoke();
            yield return null;
            CreditsScreen credits = menu.Credits;
            Assert.That(credits.IsOpen, Is.True);
            Assert.That(menu.NewGameButton.gameObject.activeInHierarchy, Is.False, "the credits cover the menu");
            Assert.That(credits.MaxScroll, Is.GreaterThan(0f), "longer than the panel: it scrolls");
            Assert.That(credits.Scroll, Is.EqualTo(0f));

            // Every line shows real text from the Credits table (never a missing key).
            foreach (LocalizedSuperText text in credits.GetComponentsInChildren<LocalizedSuperText>(true))
            {
                string shown = text.GetComponent<SuperTextMesh>().text;
                Assert.That(shown, Does.Not.StartWith("#"), $"{text.name}: {shown}");
                Assert.That(shown, Is.Not.Empty, text.name);
            }
            Assert.That(credits.GetComponentsInChildren<SuperTextMesh>(true).Any(t => t.text.Contains("HeatleyBros")), Is.True);

            yield return new WaitForSecondsRealtime(2.5f);
            Assert.That(credits.Scroll, Is.GreaterThan(0f), "they drift up on their own");
            credits.ScrollBy(10000f);
            Assert.That(credits.Scroll, Is.EqualTo(credits.MaxScroll), "and stop at the end");

            credits.LinkButton.onClick.Invoke();
            Assert.That(opened, Is.EqualTo(CreditsLocKeys.HeatleyBrosUrl), "the link asks for HeatleyBros' channel");
            Assert.That(opened, Does.StartWith("https://"));

            credits.Close();
            Assert.That(credits.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TheCredits_OpenFromThePauseMenu()
        {
            yield return Menu();
            Flow.QuickNewGame();
            Flow.MarkHintSeen(Hearthdelve.Shared.Village.CommunityRules.GimpIntro);
            yield return WaitUntil(() => !Flow.IsLoading && Flow.LoadedScene == GameScenes.Dungeon, 30f, "the first delve");
            yield return Revealed();
            Pause.Open();
            yield return null;
            Assert.That(Pause.CreditsButton, Is.Not.Null);
            Pause.CreditsButton.onClick.Invoke();
            yield return null;
            Assert.That(Pause.Credits.IsOpen, Is.True);
            Assert.That(MenuPause.IsPaused, Is.True, "still paused under the credits");
            Pause.Close();
            yield return null;
            Assert.That(Pause.Credits.IsOpen, Is.False, "closing the pause menu closes the credits");
        }

        // ---------- Options at a human pace ----------

        IEnumerator Click(Vector2 at, int framesDown, int framesUp)
        {
            InputSystem.QueueStateEvent(m_Mouse, new MouseState { position = at, buttons = 1 });
            yield return Frames(framesDown);
            InputSystem.QueueStateEvent(m_Mouse, new MouseState { position = at, buttons = 0 });
            yield return Frames(framesUp);
        }

        static Vector2 LeftHalf(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 point = Vector3.Lerp(corners[0], corners[2], 0.5f);
            point.x = Mathf.Lerp(corners[0].x, corners[2].x, 0.2f);
            return RectTransformUtility.WorldToScreenPoint(cam, point);
        }

        /// <summary>
        /// The 4i-B web note: five fast automated clicks moved a volume two steps. The UI reads the mouse once a frame, so clicks the
        /// browser driver sent within one frame counted once; a person's quickest clicks are frames apart, and each counts.
        /// </summary>
        [UnityTest]
        public IEnumerator ClicksOnAnOptionsLine_AtAFastHumanPace_EachStepOnce()
        {
            yield return Menu();
            m_Mouse = InputSystem.AddDevice<Mouse>();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.OptionsButton.onClick.Invoke();
            yield return Frames(2);
            OptionRow music = menu.Options.Rows[1];
            Vector2 at = LeftHalf((RectTransform)music.transform);
            // About eight clicks a second at 60 frames a second: one frame down, six up.
            for (int i = 0; i < 4; i++) yield return Click(at, 1, 6);
            Assert.That(GameOptions.Current.musicVolume, Is.EqualTo(0.8f).Within(1e-4f), "four clicks, four steps");
        }

        // ---------- Options on a gamepad ----------

        IEnumerator PadPress(GamepadButton button)
        {
            var state = new GamepadState();
            state = state.WithButton(button);
            InputSystem.QueueStateEvent(m_Pad, state);
            yield return Frames(2);
            InputSystem.QueueStateEvent(m_Pad, new GamepadState());
            yield return Frames(4);
        }

        [UnityTest]
        public IEnumerator Options_OnAGamepad_TabsWithTheShoulders_StepsWithTheDpad_AndBGoesBack()
        {
            yield return Menu();
            m_Pad = InputSystem.AddDevice<Gamepad>();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            menu.OptionsButton.onClick.Invoke();
            yield return Frames(2);
            OptionsScreen options = menu.Options;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.Rows[0].gameObject), "the first line is selected");

            yield return PadPress(GamepadButton.DpadLeft);
            Assert.That(GameOptions.Current.masterVolume, Is.EqualTo(0.95f).Within(1e-4f), "d-pad left lowers the line");
            yield return PadPress(GamepadButton.DpadDown);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.Rows[1].gameObject), "d-pad down moves to the next line");

            yield return PadPress(GamepadButton.RightShoulder);
            Assert.That(options.Current, Is.EqualTo(OptionsScreen.Tab.Feel), "RB: the next tab");
            yield return PadPress(GamepadButton.LeftShoulder);
            Assert.That(options.Current, Is.EqualTo(OptionsScreen.Tab.Audio), "LB: back a tab");

            yield return PadPress(GamepadButton.East);
            Assert.That(options.IsOpen, Is.False, "B goes back");
            Assert.That(menu.OptionsButton.gameObject.activeInHierarchy, Is.True, "to the menu");
        }
    }
}
