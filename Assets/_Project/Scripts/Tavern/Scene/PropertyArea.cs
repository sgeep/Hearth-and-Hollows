using System.Collections.Generic;
using Hearthdelve.Shared.Customization;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// One decoratable area of the property (4f plan §2, §8): its id (layouts and saves key on it), its kind (which
    /// checks apply), where it sits in the world, the floor its standing pieces may use, the wall band its wall pieces
    /// hang on, and the tiles kept clear (the entrance and the tile inside it). The room's shell is fixed (D5);
    /// <see cref="AreaFurniture"/> builds what stands in it.
    /// </summary>
    public sealed class PropertyArea : MonoBehaviour
    {
        public const string TavernId = "tavern";

        [SerializeField] string m_Id = TavernId;
        [SerializeField] AreaKind m_Kind = AreaKind.Tavern;
        [SerializeField, Tooltip("The area's (0, 0) cell in world tiles.")]
        Vector2 m_Origin;
        [SerializeField, Tooltip("The whole area in cells: the walkable grid's extent.")]
        RectInt m_Bounds = new(0, 0, 28, 17);
        [SerializeField, Tooltip("Cells standing and floor pieces may use, in area cells.")]
        RectInt m_Floor = new(1, 2, 26, 12);
        [SerializeField, Tooltip("Cells wall pieces hang on, in area cells.")]
        RectInt m_WallBand = new(1, 14, 26, 3);
        [SerializeField, Tooltip("Cells nothing may be placed on (the entrance, the tile inside it), in area cells.")]
        Vector2Int[] m_Reserved = System.Array.Empty<Vector2Int>();

        static readonly List<PropertyArea> s_All = new();

        public static IReadOnlyList<PropertyArea> All => s_All;

        public string Id => m_Id;
        public AreaKind Kind => m_Kind;
        public Vector2 Origin => m_Origin;
        public RectInt Bounds => m_Bounds;
        public RectInt Floor => m_Floor;
        public RectInt WallBand => m_WallBand;
        public IReadOnlyList<Vector2Int> Reserved => m_Reserved;

        public void Configure(string id, AreaKind kind, Vector2 origin, RectInt bounds, RectInt floor, RectInt wallBand, Vector2Int[] reserved)
        {
            m_Id = id;
            m_Kind = kind;
            m_Origin = origin;
            m_Bounds = bounds;
            m_Floor = floor;
            m_WallBand = wallBand;
            m_Reserved = reserved ?? System.Array.Empty<Vector2Int>();
        }

        void OnEnable() => s_All.Add(this);
        void OnDisable() => s_All.Remove(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f);
            Gizmos.DrawWireCube(m_Origin + m_Floor.center, new Vector3(m_Floor.width, m_Floor.height, 0f));
            Gizmos.color = new Color(1f, 0.8f, 0.3f);
            Gizmos.DrawWireCube(m_Origin + m_WallBand.center, new Vector3(m_WallBand.width, m_WallBand.height, 0f));
            Gizmos.color = Color.red;
            foreach (Vector2Int cell in m_Reserved) Gizmos.DrawWireCube(m_Origin + cell + Vector2.one * 0.5f, Vector3.one * 0.9f);
        }
    }
}
