using System;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>Where a piece goes, and whether it blocks (GDD §6.6; 4f plan §2).</summary>
    public enum FurnitureLayer
    {
        /// <summary>Under everything, never blocks (rugs).</summary>
        Floor,
        /// <summary>Stands on the floor and blocks its bodies; Y-sorted with characters.</summary>
        Standing,
        /// <summary>Hangs on the back wall's band; never blocks.</summary>
        Wall,
        /// <summary>Sits on another piece's surface anchor (candles on a table); never blocks.</summary>
        Surface,
    }

    /// <summary>
    /// How a piece turns (decision D2). Pieces that would look wrong turned (most wall-bound pieces, art with strong
    /// baked light or perspective) use <see cref="None"/>.
    /// </summary>
    public enum RotationMode
    {
        /// <summary>Never turns.</summary>
        None,
        /// <summary>Each quarter turn Minifantasy drew is its own facing, with its own art and geometry; undrawn turns are skipped.</summary>
        AuthoredFacings,
        /// <summary>One drawing, really rotated to 0°, 90°, 180° or 270°, with its whole geometry.</summary>
        QuarterTurnSprite,
    }

    /// <summary>What a piece does besides being looked at.</summary>
    public enum FurnitureFunction
    {
        None,
        /// <summary>Has seat anchors; a seat counts when it faces a table.</summary>
        Seat,
        /// <summary>Something seats face.</summary>
        Table,
        /// <summary>A cooking station (<see cref="StationKind"/>).</summary>
        Station,
        /// <summary>Where finished plates wait.</summary>
        Pass,
    }

    public enum StationKind
    {
        None,
        Grill,
        Tap,
        StewPot,
        ButcherBlock,
    }

    public enum FurnitureCategory
    {
        Seating,
        Tables,
        BarAndStorage,
        Lighting,
        WallDecor,
        FloorDecor,
        Plants,
        Curios,
        Bedroom,
        Stations,
    }

    /// <summary>Where copies of a piece can come from.</summary>
    [Flags]
    public enum FurnitureSource
    {
        None = 0,
        Starter = 1 << 0,
        Bought = 1 << 1,
        Discovery = 1 << 2,
        Boss = 1 << 3,
        Story = 1 << 4,
    }

    /// <summary>
    /// The collection a piece belongs to (4f plan §10): what a catalogue page groups by, and which Renown tier usually opens
    /// it (D14). A look, not a rule: a dwarven stool goes anywhere.
    /// </summary>
    public enum FurnitureTheme
    {
        Tavern,
        Village,
        Dwarven,
        Elven,
        Castle,
        Haunted,
        Curio,
    }

    /// <summary>An area-wide finish (D5): the whole floor, or the whole back wall.</summary>
    public enum FinishKind
    {
        Floor,
        Wall,
    }

    /// <summary>The kind of property area: it decides which layout checks apply (4f plan §6, §8).</summary>
    public enum AreaKind
    {
        Tavern,
        GuestRoom,
    }
}
