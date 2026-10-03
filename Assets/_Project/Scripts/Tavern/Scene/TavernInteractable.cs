using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Something in the tavern the player can use with Interact: a station, the pass, a seat or the
    /// door. It only says where to stand, how near, and whether it can be used now; what using it
    /// does belongs to the service (it listens for <see cref="TavernInteracted"/> or <see cref="Used"/>).
    /// While it's the player's target its highlight shows (presentation only).
    /// </summary>
    public sealed class TavernInteractable : MonoBehaviour
    {
        static readonly List<TavernInteractable> s_All = new();

        [SerializeField] TavernInteractableKind m_Kind;
        [SerializeField, Tooltip("Localization key (UI table) of the name shown in the hint.")]
        string m_NameKey;
        [SerializeField, Tooltip("Where the player stands to use it, relative to this object (the main spot: staff work from here).")]
        Vector2 m_UseOffset;
        [SerializeField, Tooltip("Other spots it can be used from (behind the stove, for example), relative to this object.")]
        Vector2[] m_OtherUseOffsets = System.Array.Empty<Vector2>();
        [SerializeField, Min(0.1f), Tooltip("How near the player's feet must be to the use point, in tiles.")]
        float m_Reach = 1.1f;
        [SerializeField, Tooltip("Shown while this is the player's target.")]
        GameObject m_Highlight;
        [SerializeField] bool m_Available = true;

        /// <summary>Every interactable in the loaded scenes.</summary>
        public static IReadOnlyList<TavernInteractable> All => s_All;

        public TavernInteractableKind Kind => m_Kind;
        public string NameKey => m_NameKey;
        public float Reach => m_Reach;
        /// <summary>The main spot to use it from (staff work from here).</summary>
        public Vector2 UsePoint => (Vector2)transform.position + m_UseOffset;

        /// <summary>Every spot it can be used from, the main one first.</summary>
        public IEnumerable<Vector2> UsePoints
        {
            get
            {
                yield return UsePoint;
                foreach (Vector2 offset in m_OtherUseOffsets) yield return (Vector2)transform.position + offset;
            }
        }
        public bool IsAvailable => m_Available && isActiveAndEnabled;
        public bool IsHighlighted => m_Highlight != null && m_Highlight.activeSelf;

        /// <summary>
        /// What the hint says while this is the target. Set by the service (the station's state decides);
        /// unset, it's a plain "use" hint with the name.
        /// </summary>
        public Func<TavernHint> Describe { get; set; }

        public TavernHint Hint => Describe != null ? Describe() : TavernHint.Use(m_NameKey);

        /// <summary>Raised when the player uses it.</summary>
        public event Action<TavernInteractable> Used;

        public void Configure(TavernInteractableKind kind, string nameKey, Vector2 useOffset, float reach, GameObject highlight, Vector2[] otherUseOffsets = null)
        {
            m_OtherUseOffsets = otherUseOffsets ?? System.Array.Empty<Vector2>();
            m_Kind = kind;
            m_NameKey = nameKey;
            m_UseOffset = useOffset;
            m_Reach = reach;
            m_Highlight = highlight;
        }

        /// <summary>Whether it can be used now (a seat with no one to serve, for example, can't).</summary>
        public void SetAvailable(bool available) => m_Available = available;

        public void SetHighlighted(bool on)
        {
            if (m_Highlight != null && m_Highlight.activeSelf != on) m_Highlight.SetActive(on);
        }

        public void Use()
        {
            Used?.Invoke(this);
            EventBus<TavernInteracted>.Publish(new TavernInteracted(m_Kind, this));
        }

        void Awake() => SetHighlighted(false);
        void OnEnable() => s_All.Add(this);

        void OnDisable()
        {
            s_All.Remove(this);
            SetHighlighted(false);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            foreach (Vector2 point in UsePoints) Gizmos.DrawWireSphere(point, m_Reach);
        }
    }
}
