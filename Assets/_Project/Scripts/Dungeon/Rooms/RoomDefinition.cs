using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>What a room is for. The run generator chooses rooms by kind.</summary>
    public enum RoomKind
    {
        /// <summary>Where a run begins: no enemies.</summary>
        Start,
        /// <summary>A fight, with spawn points for the encounter the run chooses.</summary>
        Combat,
        /// <summary>The rope out: the run ends here, with everything carried.</summary>
        Extraction,
        /// <summary>The hole down to the next floor.</summary>
        Descent,
        /// <summary>The biome's boss arena. Its rope appears once it's clear.</summary>
        Arena,
    }

    /// <summary>
    /// An authored room (4d): its layout as rows of characters (<see cref="RoomLayout"/>) and the prefab the editor
    /// builds from it (<c>Hearthdelve → Generate → 4d Rooms</c>). The layout is the source of truth; the prefab is
    /// rebuilt from it.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Dungeon/Room", fileName = "Room_")]
    public sealed class RoomDefinition : ScriptableObject
    {
        public string id;
        public RoomKind kind = RoomKind.Combat;
        [Tooltip("The room, top row first. Legend in RoomLayout.")]
        [TextArea(3, 40)] public string[] layout;
        [Tooltip("Built from the layout by the room builder.")]
        public RoomInstance prefab;

        /// <summary>Parses and checks the layout.</summary>
        public RoomLayout Parse() => new(layout);
    }
}
