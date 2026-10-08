using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A doorway between two surface areas (4h, H1): Tally Ho!'s front door and its outside face in Kariaston. Walking
    /// onto it fades briefly (the same quarter-second fade as the guest-room stairs), moves the keeper to the partner
    /// door's step and switches the area (camera, lights). Nothing loads: both sides are already there. The two halves
    /// find each other by id, since they live in different scenes. Open only while the keeper is free on foot in the
    /// daytime, and only when the other side is loaded.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SurfaceDoor : MonoBehaviour
    {
        public const string FrontInside = "tallyho.front.inside";
        public const string FrontOutside = "tallyho.front.outside";

        static readonly List<SurfaceDoor> s_All = new();

        [SerializeField] string m_Id = FrontInside;
        [SerializeField] string m_Partner = FrontOutside;
        [SerializeField, Tooltip("The area this side of the door is in.")]
        SurfaceArea m_Area;
        [SerializeField, Tooltip("Where someone coming through the partner door arrives, relative to this door (clear of its trigger).")]
        Vector2 m_ArrivalOffset = new(0f, 1.2f);
        [SerializeField, Min(0f)] float m_FadeSeconds = 0.18f;
        [SerializeField, Tooltip("The way through (world direction): pushing that way in the doorway goes through. Zero: stepping in does.")]
        Vector2 m_Through;
        Vector2 m_LastSeen;
        bool m_Tracked;

        bool m_Busy;

        public string Id => m_Id;
        public SurfaceArea Area => m_Area;
        public Vector2 Arrival => (Vector2)transform.position + m_ArrivalOffset;
        public bool Busy => m_Busy;
        public SurfaceDoor Partner => Find(m_Partner);

        public static SurfaceDoor Find(string id)
        {
            foreach (SurfaceDoor d in s_All)
                if (d.m_Id == id) return d;
            return null;
        }

        public void Configure(string id, string partner, SurfaceArea area, Vector2 arrivalOffset, float fadeSeconds = 0.18f, Vector2 through = default)
        {
            m_Through = through;
            m_Id = id;
            m_Partner = partner;
            m_Area = area;
            m_ArrivalOffset = arrivalOffset;
            m_FadeSeconds = fadeSeconds;
        }

        void OnEnable() => s_All.Add(this);
        void OnDisable() => s_All.Remove(this);

        /// <summary>The keeper may go through now: free daytime, on foot, the other side loaded, nothing else under way.</summary>
        public bool CanPass
        {
            get
            {
                SurfaceDoor partner = Partner;
                if (m_Busy || partner == null || partner.m_Busy) return false;
                if (GameFlow.Instance != null && GameFlow.Instance.IsLoading) return false;
                if (DecorateMode.Instance != null && DecorateMode.Instance.IsActive) return false;
                TavernDirector director = TavernDirector.Instance;
                return director == null || director.Phase == TavernPhase.Daytime;
            }
        }

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

        IEnumerator Go(Rigidbody2D player)
        {
            SurfaceDoor partner = Partner;
            m_Busy = partner.m_Busy = true;
            EventBus<AreaPassageStarted>.Publish(new AreaPassageStarted(m_FadeSeconds));
            yield return new WaitForSecondsRealtime(m_FadeSeconds);
            Arrive(player, partner);
            yield return new WaitForSecondsRealtime(m_FadeSeconds);
            m_Busy = partner.m_Busy = false;
        }

        /// <summary>Walks the keeper through at once (tests).</summary>
        public void Pass(Rigidbody2D player)
        {
            SurfaceDoor partner = Partner;
            if (player != null && partner != null) Arrive(player, partner);
        }

        static void Arrive(Rigidbody2D player, SurfaceDoor to)
        {
            Vector2 at = to.Arrival;
            player.position = at;
            player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
            player.linearVelocity = Vector2.zero;
            // Inside, the property area follows (decorating and the stairs use it); outside there is none.
            PropertyArea property = to.m_Area != null ? to.m_Area.GetComponent<PropertyArea>() : null;
            if (property != null) PropertyArea.Current = property;
            SurfaceArea.Enter(to.m_Area);
            EventBus<AreaPassageEnded>.Publish(new AreaPassageEnded(to.m_FadeSeconds, to.m_Area != null ? to.m_Area.Id : null));
            if (to.m_Area != null) EventBus<Hearthdelve.Shared.Game.KeeperEnteredArea>.Publish(new Hearthdelve.Shared.Game.KeeperEnteredArea(to.m_Area.Id));
        }
    }
}
