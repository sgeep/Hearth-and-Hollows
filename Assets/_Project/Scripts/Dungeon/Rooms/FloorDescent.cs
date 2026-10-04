using System;
using Hearthdelve.Dungeon.Harvest;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// The hole down to the next floor (4d): step into it and you drop. The room runner plays the fall and loads the
    /// next floor; this only notices the player.
    /// </summary>
    public sealed class FloorDescent : MonoBehaviour
    {
        bool m_Raised;

        /// <summary>The player stepped into the hole.</summary>
        public event Action<FloorDescent> Entered;

        void OnTriggerEnter2D(Collider2D other) => Enter(other);
        void OnTriggerStay2D(Collider2D other) => Enter(other);

        void Enter(Collider2D other)
        {
            if (m_Raised || !other.TryGetComponent(out SatchelCarrier _)) return;
            m_Raised = true;
            Entered?.Invoke(this);
        }
    }
}
