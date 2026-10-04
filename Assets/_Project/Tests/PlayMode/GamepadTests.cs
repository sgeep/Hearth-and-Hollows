using System.Collections;
using System.Linq;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// With a (virtual) gamepad: the keeper plays the walk while moving (4c step 4 playtest: they glided in
    /// the idle), and a released stick springing back past centre doesn't step a menu selection back.
    /// </summary>
    public class GamepadTests : LookTestFixture
    {
        Gamepad m_Pad;

        [SetUp]
        public void AddPad() => m_Pad = InputSystem.AddDevice<Gamepad>();

        [TearDown]
        public void RemovePad()
        {
            if (m_Pad != null) InputSystem.RemoveDevice(m_Pad);
        }

        IEnumerator Stick(Vector2 value, int frames)
        {
            InputSystem.QueueStateEvent(m_Pad, new GamepadState { leftStick = value });
            for (int i = 0; i < frames; i++) yield return null;
        }

        /// <summary>Holds the stick for a time (batch-mode frames are very short).</summary>
        IEnumerator HoldStick(Vector2 value, float seconds)
        {
            InputSystem.QueueStateEvent(m_Pad, new GamepadState { leftStick = value });
            yield return new WaitForSeconds(seconds);
        }

        [UnityTest]
        public IEnumerator TheKeeper_WalksWithTheWalkAnimation_AndIdlesWhenStopped()
        {
            yield return Load("Tavern");
            TavernDirector.Instance.OpenDebugEvening();
            TavernDirector.Instance.ArrivalsPaused = true;
            yield return null;
            Character keeper = Object.FindAnyObjectByType<TavernInteractor>().GetComponent<Character>();
            var animator = keeper.GetComponentInChildren<CharacterSpriteAnimator>();
            Assert.That(keeper.GetComponent<TopDownController>(), Is.InstanceOf<FloorController2D>());

            Vector3 start = keeper.transform.position;
            yield return HoldStick(new Vector2(0f, -1f), 0.25f);
            Assert.That(keeper.transform.position.y, Is.LessThan(start.y - 0.2f), "the keeper moved");
            Assert.That(keeper.MovementState.CurrentState, Is.EqualTo(CharacterStates.MovementStates.Walking));
            Assert.That(animator.Current, Is.EqualTo(CharacterAnim.Walk), "the walk plays while moving");

            yield return HoldStick(Vector2.zero, 0.2f);
            Assert.That(keeper.MovementState.CurrentState, Is.EqualTo(CharacterStates.MovementStates.Idle));
            Assert.That(animator.Current, Is.EqualTo(CharacterAnim.Idle));
        }

        [UnityTest]
        public IEnumerator EveryCharacterPrefab_UsesTheFloorController()
        {
            yield return Load("Tavern");
            foreach (TopDownController controller in Object.FindObjectsByType<TopDownController>(FindObjectsInactive.Include))
                Assert.That(controller, Is.InstanceOf<FloorController2D>(), controller.name);
        }

        [UnityTest]
        public IEnumerator AReleasedStick_SpringingBackPastCentre_DoesNotStepTheMenuBack()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            yield return null;
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            GameObject first = prep.Cards[0].button.gameObject;
            EventSystem.current.SetSelectedGameObject(first);
            yield return null;

            yield return Stick(new Vector2(0f, -1f), 3);
            GameObject moved = EventSystem.current.currentSelectedGameObject;
            Assert.That(moved, Is.Not.SameAs(first), "down moves the selection");
            // Let go: the stick overshoots the other way for a moment, then rests.
            yield return Stick(new Vector2(0f, 0.35f), 2);
            yield return Stick(new Vector2(0.05f, -0.03f), 2);
            yield return Stick(Vector2.zero, 2);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(moved), "the spring-back isn't a press up");

            yield return Stick(new Vector2(0f, 1f), 3);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(first), "a real push up still moves");
        }
    }
}
