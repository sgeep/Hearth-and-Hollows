using System.Collections;
using Hearthdelve.Core.Events;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>Walking from one area of the property to another began: the screen fades out (4f step 6).</summary>
    public readonly struct AreaPassageStarted : IEvent
    {
        public readonly float FadeSeconds;
        public AreaPassageStarted(float fadeSeconds) => FadeSeconds = fadeSeconds;
    }

    /// <summary>The keeper is in the next area: the screen fades back in.</summary>
    public readonly struct AreaPassageEnded : IEvent
    {
        public readonly float FadeSeconds;
        public readonly string Area;
        public AreaPassageEnded(float fadeSeconds, string area)
        {
            FadeSeconds = fadeSeconds;
            Area = area;
        }
    }

    /// <summary>
    /// A fixed way between two areas of the property (4f step 6; plan §8): the slim stairs in the tavern's back-right
    /// corner up to the guest room, and the guest room's door back down.
    /// Walking onto it fades the screen, moves the keeper to the other area's arrival point and the camera onto that area,
    /// and fades back. It works whenever the keeper walks (Prep and service until the village milestone); when the
    /// evening ends with the keeper still upstairs, they're brought back down. Not while decorating, which switches areas itself.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AreaPassage : MonoBehaviour
    {
        [SerializeField] PropertyArea m_To;
        [SerializeField, Tooltip("The area this way leads out of.")] PropertyArea m_From;
        [SerializeField, Min(0f)] float m_FadeSeconds = Hearthdelve.Shared.Game.PlaceFade.Seconds;
        [SerializeField, Tooltip("The way through (world direction): pushing that way in the doorway goes through. Zero: stepping in does.")]
        Vector2 m_Through;

        bool m_Busy;
        Vector2 m_LastSeen;
        bool m_Tracked;

        public PropertyArea To => m_To;
        public PropertyArea From => m_From;
        public bool Busy => m_Busy;

        public void Configure(PropertyArea from, PropertyArea to, float fadeSeconds = Hearthdelve.Shared.Game.PlaceFade.Seconds, Vector2 through = default)
        {
            m_Through = through;
            m_From = from;
            m_To = to;
            m_FadeSeconds = fadeSeconds;
        }

        /// <summary>The parts of the day when the keeper is about the property on foot.</summary>
        static bool OnFoot => TavernDirector.Instance == null || TavernDirector.Instance.Phase is TavernPhase.Service or TavernPhase.Prep or TavernPhase.Arrival or TavernPhase.Daytime;

        public bool CanPass => m_To != null && !m_Busy && OnFoot && (DecorateMode.Instance == null || !DecorateMode.Instance.IsActive);

        void OnTriggerEnter2D(Collider2D other) => Consider(other, entering: true);

        void OnTriggerStay2D(Collider2D other) => Consider(other, entering: false);

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) m_Tracked = false;
        }

        /// <summary>In the doorway and pushing the way through (<see cref="Doorway"/>): through it goes.</summary>
        void Consider(Collider2D other, bool entering)
        {
            if (other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player")) return;
            bool going = Doorway.GoingThrough(other.attachedRigidbody, m_Through, entering, ref m_LastSeen, ref m_Tracked);
            if (going && CanPass) StartCoroutine(Go(other.attachedRigidbody));
        }

        void Update()
        {
            // The evening ends while the keeper is upstairs: down they come (the results and the night are the tavern's).
            if (m_Busy || m_From == null || m_To == null || PropertyArea.Current != m_From || m_From.Kind == Shared.Customization.AreaKind.Tavern || OnFoot) return;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && player.TryGetComponent(out Rigidbody2D body)) Arrive(body);
        }

        /// <summary>Walks the keeper through at once (tests, and the return when service starts).</summary>
        public void Pass(Rigidbody2D player)
        {
            if (player != null && m_To != null) Arrive(player);
        }

        IEnumerator Go(Rigidbody2D player)
        {
            m_Busy = true;
            EventBus<AreaPassageStarted>.Publish(new AreaPassageStarted(m_FadeSeconds));
            yield return new WaitForSecondsRealtime(m_FadeSeconds);
            Arrive(player);
            yield return new WaitForSecondsRealtime(m_FadeSeconds);
            m_Busy = false;
        }

        void Arrive(Rigidbody2D player)
        {
            player.position = m_To.Arrival;
            player.transform.position = new Vector3(m_To.Arrival.x, m_To.Arrival.y, player.transform.position.z);
            player.linearVelocity = Vector2.zero;
            PropertyArea.Current = m_To;
            TavernView.Show(m_To);
            EventBus<AreaPassageEnded>.Publish(new AreaPassageEnded(m_FadeSeconds, m_To.Id));
            EventBus<Hearthdelve.Shared.Game.KeeperEnteredArea>.Publish(new Hearthdelve.Shared.Game.KeeperEnteredArea(m_To.Id));
        }
    }
}
