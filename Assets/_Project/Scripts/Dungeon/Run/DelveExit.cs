using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Dungeon.Run
{
    /// <summary>
    /// The way back up (GDD §4.4 extraction): stand at it and press Interact to end the delve
    /// with the whole satchel. Shows a prompt through <see cref="DelveExitHint"/>.
    /// </summary>
    public sealed class DelveExit : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Horizontal reach from the exit's centre.")]
        float m_Reach = 1f;
        [SerializeField, Min(0.1f), Tooltip("Vertical reach from the exit's base.")]
        float m_Height = 2f;
        [SerializeField] SpriteRenderer m_Highlight;

        PlayerController m_Player;
        PlayerVitals m_Vitals;
        InputAction m_Interact;
        bool m_InReach;

        public bool PlayerInReach => m_InReach;

        public void Configure(SpriteRenderer highlight) => m_Highlight = highlight;

        void Start()
        {
            m_Player = FindFirstObjectByType<PlayerController>();
            m_Vitals = m_Player != null ? m_Player.GetComponent<PlayerVitals>() : null;
            m_Interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            SetInReach(false);
        }

        void OnDisable()
        {
            if (m_InReach) EventBus<DelveExitHint>.Publish(new DelveExitHint(false));
            m_InReach = false;
        }

        void Update()
        {
            var run = DelveRunController.Active;
            bool inReach = m_Player != null && run != null && !run.IsEnding && (m_Vitals == null || !m_Vitals.IsDefeated) && IsInReach(m_Player.transform.position);
            SetInReach(inReach);
            if (inReach && m_Interact != null && m_Interact.WasPressedThisFrame()) run.Extract();
        }

        bool IsInReach(Vector3 p)
        {
            var e = transform.position;
            return Mathf.Abs(p.x - e.x) <= m_Reach && p.y >= e.y - 0.5f && p.y <= e.y + m_Height;
        }

        void SetInReach(bool inReach)
        {
            if (m_Highlight != null) m_Highlight.enabled = inReach;
            if (inReach == m_InReach) return;
            m_InReach = inReach;
            EventBus<DelveExitHint>.Publish(new DelveExitHint(inReach));
        }
    }
}
