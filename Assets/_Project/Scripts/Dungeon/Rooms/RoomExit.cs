using System;
using System.Collections;
using Hearthdelve.Dungeon.Harvest;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// A gated doorway in a room's north wall (4d). Sealed, a portcullis bars it and a collider blocks it; open, the
    /// bars drop away and stepping into the doorway takes the player on (<see cref="Entered"/>). The gate is the
    /// Gladiator Arena animated gate's barred interior: frames 0–3 open it, 4–7 close it.
    /// </summary>
    public sealed class RoomExit : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Gate;
        [SerializeField, Tooltip("Closed, opening…, open, closing…, closed (8 frames).")]
        Sprite[] m_Frames = Array.Empty<Sprite>();
        [SerializeField, Tooltip("Blocks the doorway while sealed.")]
        Collider2D m_Block;
        [SerializeField, Min(0.01f), Tooltip("Seconds per frame of the gate's animation.")]
        float m_FrameTime = 0.07f;
        [SerializeField, Tooltip("Which exit of the room this is, left to right.")]
        int m_Index;
        [SerializeField, Tooltip("TEMPORARY (4d step 2): what lies beyond (out, deeper, the arena), until step 3's reward previews.")]
        SpriteRenderer m_Marker;
        [SerializeField, Tooltip("The doorway bricked up with the wall's own tiles, for an exit this run doesn't use.")]
        GameObject m_Bricked;

        Coroutine m_Animation;
        bool m_Raised;

        /// <summary>Which exit of the room this is, left to right.</summary>
        public int Index => m_Index;
        public bool IsOpen { get; private set; }

        /// <summary>The player stepped into the open doorway.</summary>
        public event Action<RoomExit> Entered;

        public void Configure(int index, SpriteRenderer gate, Sprite[] frames, Collider2D block, SpriteRenderer marker, GameObject bricked)
        {
            m_Index = index;
            m_Gate = gate;
            m_Frames = frames;
            m_Block = block;
            m_Marker = marker;
            m_Bricked = bricked;
        }

        /// <summary>True when this run doesn't use the exit: it's bricked up and never opens.</summary>
        public bool IsUnused { get; private set; }

        /// <summary>
        /// An exit the run doesn't use is bricked up with the wall's tiles (no gate, no sign), so it reads as wall, not as a
        /// gate that stays shut (the step 2 playtest).
        /// </summary>
        public void SetUnused(bool unused)
        {
            IsUnused = unused;
            if (m_Bricked != null) m_Bricked.SetActive(unused);
            if (m_Gate != null) m_Gate.enabled = !unused;
            if (!unused) return;
            SetMarker(null);
            SetOpen(false, instant: true);
        }

        /// <summary>The sign over the doorway (none for an ordinary fight).</summary>
        public void SetMarker(Sprite sprite)
        {
            if (m_Marker == null) return;
            m_Marker.sprite = sprite;
            m_Marker.enabled = sprite != null;
        }

        /// <summary>The marker currently shown, if any.</summary>
        public Sprite Marker => m_Marker != null && m_Marker.enabled ? m_Marker.sprite : null;

        /// <summary>Opens or seals the doorway. The collider changes at once; the gate animates unless <paramref name="instant"/>.</summary>
        public void SetOpen(bool open, bool instant)
        {
            if (IsUnused) open = false;
            IsOpen = open;
            m_Raised = false;
            if (m_Block != null) m_Block.enabled = !open;
            if (m_Animation != null) StopCoroutine(m_Animation);
            m_Animation = null;
            if (m_Frames.Length < 8 || m_Gate == null) return;
            if (instant || !isActiveAndEnabled) m_Gate.sprite = m_Frames[open ? 3 : 0];
            else m_Animation = StartCoroutine(Animate(open ? 0 : 4, open ? 3 : 7));
        }

        IEnumerator Animate(int first, int last)
        {
            for (int frame = first; frame <= last; frame++)
            {
                m_Gate.sprite = m_Frames[frame];
                yield return new WaitForSeconds(m_FrameTime);
            }
            m_Animation = null;
        }

        void OnTriggerEnter2D(Collider2D other) => Enter(other);
        void OnTriggerStay2D(Collider2D other) => Enter(other);

        void Enter(Collider2D other)
        {
            if (!IsOpen || m_Raised || !other.TryGetComponent(out SatchelCarrier _)) return;
            m_Raised = true;
            Entered?.Invoke(this);
        }
    }
}
