using System;
using System.Collections;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using Unity.Cinemachine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// Plays a delve room by room (4d). One room is loaded at a time, at the origin: the player arrives at its P,
    /// the navigation grid and the camera's bounds (<see cref="RoomCameraBounds"/>) switch to it, and its gates drop while anything in it is alive.
    /// When the last enemy falls the gates rise; stepping into an open doorway fades out, unloads the room (and
    /// anything left lying in it) and loads the next. One way: there is no going back.
    /// Step 1 walks a fixed route; the floor graph (step 2) will choose the next room from the exit taken.
    /// </summary>
    public sealed class RoomRunner : MonoBehaviour
    {
        [SerializeField, Tooltip("The rooms in order (step 1's test route).")]
        RoomDefinition[] m_Route = Array.Empty<RoomDefinition>();
        [SerializeField, Tooltip("Rooms are loaded under this.")]
        Transform m_RoomRoot;
        [SerializeField] NavGrid m_Nav;
        [SerializeField] CinemachineCamera m_Camera;
        [SerializeField] RoomCameraBounds m_CameraBounds;
        [SerializeField, Min(0f), Tooltip("Seconds the screen takes to cover when leaving a room.")]
        float m_FadeOut = 0.25f;
        [SerializeField, Min(0f), Tooltip("Seconds the screen takes to uncover in the new room.")]
        float m_FadeIn = 0.3f;
        [SerializeField, Tooltip("The gates dropping as a fight starts: sound and haptic together.")]
        MMF_Player m_SealFeedback;
        [SerializeField, Tooltip("The gates rising when the room is clear: sound and haptic together.")]
        MMF_Player m_ClearFeedback;

        RoomEncounter m_Encounter;
        int m_Entered = -1;
        int m_RouteIndex;
        bool m_Transitioning;

        public static RoomRunner Active { get; private set; }
        public RoomInstance Current { get; private set; }
        public RoomDefinition CurrentDefinition => m_RouteIndex < m_Route.Length ? m_Route[m_RouteIndex] : null;
        public RoomEncounter Encounter => m_Encounter;
        public bool IsTransitioning => m_Transitioning;
        /// <summary>Rooms entered so far this run, counting the current one.</summary>
        public int RoomsEntered => m_Entered + 1;

        public void Configure(RoomDefinition[] route, Transform roomRoot, NavGrid nav, CinemachineCamera camera, RoomCameraBounds cameraBounds,
            MMF_Player sealFeedback, MMF_Player clearFeedback)
        {
            m_Route = route;
            m_RoomRoot = roomRoot;
            m_Nav = nav;
            m_Camera = camera;
            m_CameraBounds = cameraBounds;
            m_SealFeedback = sealFeedback;
            m_ClearFeedback = clearFeedback;
        }

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        IEnumerator Start()
        {
            // TDE spawns the player during its own Start.
            while (Player == null) yield return null;
            if (m_Route.Length == 0)
            {
                Debug.LogWarning("[Hearthdelve] The room runner has no rooms.");
                yield break;
            }
            Load(0);
            EventBus<RoomEntered>.Publish(new RoomEntered(m_Entered, CurrentDefinition.id, m_Encounter.IsSealed, 0f));
            if (m_Encounter.IsSealed) Seal();
        }

        static Character Player =>
            LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0 ? LevelManager.Instance.Players[0] : null;

        void Update()
        {
            if (m_Encounter == null || m_Encounter.IsCleared || Current == null) return;
            if (m_Encounter.Update(Current.LivingEnemies()))
            {
                Current.SetExitsOpen(true, instant: false);
                m_ClearFeedback?.PlayFeedbacks(Player != null ? Player.transform.position : Vector3.zero);
                EventBus<RoomCleared>.Publish(new RoomCleared(m_Entered, CurrentDefinition.id));
            }
        }

        void Seal()
        {
            if (Current == null) return;
            Current.SetExitsOpen(false, instant: false);
            m_SealFeedback?.PlayFeedbacks(Player != null ? Player.transform.position : Vector3.zero);
        }

        /// <summary>Unloads the current room and loads route room <paramref name="index"/> with the player at its arrival.</summary>
        void Load(int index)
        {
            if (Current != null)
            {
                foreach (RoomExit exit in Current.Exits)
                    if (exit != null) exit.Entered -= OnExitEntered;
                // Off at once, so the new room's colliders are the only ones the grid sees this frame.
                Current.gameObject.SetActive(false);
                Destroy(Current.gameObject);
                ClearLeftovers();
            }

            m_RouteIndex = index;
            m_Entered++;
            RoomDefinition definition = m_Route[index];
            Current = Instantiate(definition.prefab, Vector3.zero, Quaternion.identity, m_RoomRoot);
            Current.name = definition.prefab.name;
            foreach (RoomExit exit in Current.Exits)
                if (exit != null) exit.Entered += OnExitEntered;

            if (m_Nav != null) m_Nav.Configure(Current.TileBounds, LayerMask.GetMask(Layers.Obstacles));
            Character player = Player;
            if (player != null) PlaceAt(player, Current.Arrival.position);
            BindCamera(player);

            m_Encounter = new RoomEncounter(Current.LivingEnemies());
            // Arrive with the gates up; they drop as the fight starts (after the fade).
            Current.SetExitsOpen(true, instant: true);
        }

        void OnExitEntered(RoomExit exit)
        {
            if (m_Transitioning || DelveRunController.Active != null && DelveRunController.Active.IsEnding) return;
            if (m_RouteIndex + 1 >= m_Route.Length) return;
            StartCoroutine(Transition(m_RouteIndex + 1));
        }

        IEnumerator Transition(int next)
        {
            m_Transitioning = true;
            Character player = Player;
            InputMaps.ActivateUIOnly();
            player?.GetComponent<CharacterMovement>()?.SetMovement(Vector2.zero);
            var essence = player != null ? player.GetComponent<EssenceHealth>() : null;
            // Walking between rooms costs no Essence.
            if (essence != null) essence.DrainPaused = true;

            EventBus<RoomTransitionStarted>.Publish(new RoomTransitionStarted(m_FadeOut));
            yield return new WaitForSecondsRealtime(m_FadeOut);
            Load(next);
            EventBus<RoomEntered>.Publish(new RoomEntered(m_Entered, CurrentDefinition.id, m_Encounter.IsSealed, m_FadeIn));
            yield return new WaitForSecondsRealtime(m_FadeIn);

            if (essence != null) essence.DrainPaused = false;
            InputMaps.Activate(InputMaps.Dungeon);
            m_Transitioning = false;
            if (m_Encounter.IsSealed) Seal();
        }

        static void PlaceAt(Character player, Vector2 position)
        {
            player.transform.position = position;
            if (player.TryGetComponent(out Rigidbody2D body))
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            player.GetComponent<CharacterMovement>()?.SetMovement(Vector2.zero);
            Physics2D.SyncTransforms();
        }

        /// <summary>The camera's bounds become the room, and it jumps to the player instead of sliding across.</summary>
        void BindCamera(Character player)
        {
            if (m_CameraBounds != null)
            {
                RectInt tiles = Current.TileBounds;
                m_CameraBounds.SetBounds(new Rect(tiles.x, tiles.y, tiles.width, tiles.height));
            }
            if (m_Camera == null || player == null) return;
            Transform target = m_Camera.Follow != null ? m_Camera.Follow : player.transform;
            Vector3 at = player.transform.position;
            m_Camera.ForceCameraPosition(new Vector3(at.x, at.y, m_Camera.transform.position.z), Quaternion.identity);
            if (target != player.transform) target.position = new Vector3(at.x, at.y, target.position.z);
        }

        /// <summary>Parts left lying and webs in flight stay behind with the room.</summary>
        static void ClearLeftovers()
        {
            foreach (IngredientPickup pickup in FindObjectsByType<IngredientPickup>(FindObjectsSortMode.None)) Destroy(pickup.gameObject);
            foreach (WebProjectile web in FindObjectsByType<WebProjectile>(FindObjectsSortMode.None)) Destroy(web.gameObject);
        }
    }
}
