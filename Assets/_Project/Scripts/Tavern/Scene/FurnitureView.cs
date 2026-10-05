using Hearthdelve.Shared.Customization;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>A placed piece in the scene: which placement it shows. Made by <see cref="AreaFurniture"/>; presentation only.</summary>
    public sealed class FurnitureView : MonoBehaviour
    {
        [SerializeField] string m_Definition;
        [SerializeField] int m_Uid;

        public string Definition => m_Definition;
        public int Uid => m_Uid;
        /// <summary>The resolved geometry it was built from (runtime only).</summary>
        public ResolvedFurniture Resolved { get; private set; }

        public void Configure(ResolvedFurniture resolved)
        {
            Resolved = resolved;
            m_Definition = resolved.Definition.id;
            m_Uid = resolved.Placement.uid;
        }
    }
}
