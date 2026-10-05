using System.Collections;
using System.Linq;
using Hearthdelve.Core.Movement;
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

        /// <summary>
        /// Flicking the stick and letting go: it springs back past centre for a frame or two, the other way. That
        /// must not turn the keeper round (the step 5 playtest: a flick down-left could end facing up-right).
        /// </summary>
        [UnityTest]
        public IEnumerator AFlickReleased_SpringingBackPastCentre_KeepsTheFacing()
        {
            yield return Load("Tavern");
            TavernDirector.Instance.OpenDebugEvening();
            TavernDirector.Instance.ArrivalsPaused = true;
            yield return null;
            Character keeper = Object.FindAnyObjectByType<TavernInteractor>().GetComponent<Character>();
            var animator = keeper.GetComponentInChildren<CharacterSpriteAnimator>();

            yield return HoldStick(new Vector2(-0.75f, -0.66f), 0.2f);
            Assert.That(animator.Facing, Is.EqualTo(Facing4.FrontLeft), "facing the flick");
            // Let go: an overshoot up-right for two frames, then rest.
            yield return Stick(new Vector2(0.3f, 0.28f), 2);
            yield return Stick(new Vector2(0.06f, 0.04f), 2);
            yield return HoldStick(Vector2.zero, 0.2f);
            Assert.That(animator.Facing, Is.EqualTo(Facing4.FrontLeft), "the spring-back doesn't turn them round");

            // A real turn still turns them, at once.
            yield return HoldStick(new Vector2(0.7f, 0.7f), 0.1f);
            Assert.That(animator.Facing, Is.EqualTo(Facing4.BackRight), "pushing the other way turns them");
        }

        /// <summary>4e playtest: in the Hollows the right stick aims and turns the character; centred, aim follows the walk.</summary>
        [UnityTest]
        public IEnumerator TheRightStick_AimsAndTurnsTheDelver_AndCentred_AimFollowsTheWalk()
        {
            yield return Load("Dungeon_TestFloor");
            foreach (var enemy in Object.FindObjectsByType<Hearthdelve.Dungeon.Enemies.EnemyIdentity>()) Object.Destroy(enemy.gameObject);
            var handle = Player.GetComponent<CharacterHandleWeapon>();
            var animator = Player.GetComponentInChildren<CharacterSpriteAnimator>();
            Vector3 start = Player.transform.position;

            InputSystem.QueueStateEvent(m_Pad, new GamepadState { rightStick = new Vector2(-1f, 0.1f) });
            yield return new WaitForSeconds(0.15f);
            Assert.That(handle.WeaponAimComponent.AimControl, Is.EqualTo(WeaponAim.AimControls.SecondaryThenPrimaryMovement));
            Assert.That(handle.WeaponAimComponent.CurrentAim.x, Is.LessThan(-0.9f), "aims left");
            Assert.That(animator.Facing, Is.EqualTo(Facing4.BackLeft).Or.EqualTo(Facing4.FrontLeft), "and faces left");
            Assert.That(Vector2.Distance(Player.transform.position, start), Is.LessThan(0.05f), "without moving");

            // Walking right while aiming left: faces the aim.
            InputSystem.QueueStateEvent(m_Pad, new GamepadState { leftStick = new Vector2(1f, 0f), rightStick = new Vector2(-1f, 0.1f) });
            yield return new WaitForSeconds(0.2f);
            Assert.That(animator.Facing, Is.EqualTo(Facing4.BackLeft).Or.EqualTo(Facing4.FrontLeft), "walking right, still facing the aim");

            // The right stick let go: aim follows the walk.
            InputSystem.QueueStateEvent(m_Pad, new GamepadState { leftStick = new Vector2(0f, 1f) });
            yield return new WaitForSeconds(0.2f);
            Assert.That(handle.WeaponAimComponent.CurrentAim.y, Is.GreaterThan(0.9f), "aims up, the way it walks");
            InputSystem.QueueStateEvent(m_Pad, new GamepadState());
            yield return null;
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
