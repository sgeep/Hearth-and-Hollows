using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// One drawn layer of a piece: a sprite (or an animation loop), where its pivot sits in footprint space, and how it
    /// sorts. Layers keep their own sorting so a piece can, like the kitchen, put its shadow on the ground layer.
    /// </summary>
    [Serializable]
    public sealed class FurnitureArt
    {
        [Tooltip("The GameObject's name (station views and tests find layers by it).")]
        public string name = "Art";
        public Sprite sprite;
        [Tooltip("An idle animation loop (the fire under the stew pot). Empty: the sprite stands still.")]
        public Sprite[] frames = Array.Empty<Sprite>();
        [Min(0.01f)] public float frameSeconds = 0.12f;
        [Tooltip("Frames shown while the station works (the kitchen at the Grill).")]
        public Sprite[] workingFrames = Array.Empty<Sprite>();
        [Tooltip("The sprite's pivot, in tiles from the footprint's bottom-left corner (unrotated).")]
        public Vector2 position;
        public string sortingLayer = "Characters";
        public int order;
    }

    /// <summary>Where a customer sits, where they step from, and the way they face (towards the table).</summary>
    [Serializable]
    public struct FurnitureSeat
    {
        [Tooltip("The seat's point (the chair's sort point), in footprint space.")]
        public Vector2 position;
        [Tooltip("The walkable spot a customer steps onto the chair from, in footprint space.")]
        public Vector2 approach;
        [Tooltip("The way the sitter faces: (1,0) east, (-1,0) west, (0,1) north, (0,-1) south.")]
        public Vector2Int facing;
    }

    /// <summary>A light the piece carries (a fire, a lamp): moving the piece moves its light.</summary>
    [Serializable]
    public sealed class FurnitureLight
    {
        public Vector2 position;
        public Color color = Color.white;
        [Min(0f)] public float intensity = 1f;
        [Min(0f)] public float innerRadius;
        [Min(0f)] public float outerRadius = 3f;
        [Range(0f, 1f)] public float falloff = 0.6f;
    }

    /// <summary>
    /// A piece as it stands in one orientation: its whole-tile footprint, pixel-exact bodies, art, seats, use points,
    /// surfaces, lights and station overlays, all in footprint space (tiles from the footprint's bottom-left corner).
    /// <see cref="FurnitureGeometry"/> places, rotates and flips all of it together.
    /// </summary>
    [Serializable]
    public sealed class FurnitureFacing
    {
        [Tooltip("For authored facings: the quarter turns (counter-clockwise from facing the camera) this drawing shows. 0 otherwise.")]
        [Range(0, 3)] public int turns;
        [Tooltip("Footprint in whole tiles: what snaps, and what no other blocking piece may overlap (D1).")]
        public Vector2Int size = Vector2Int.one;
        public List<FurnitureArt> art = new();
        [Tooltip("Sort the layers as one (a SortingGroup) at the group point: pieces with overlays drawn on top.")]
        public bool grouped;
        public Vector2 groupPoint;
        [Tooltip("What physically blocks, pixel-exact, in footprint space: the walkable grid bakes these (as in 4e).")]
        public List<Rect> bodies = new();
        public List<FurnitureSeat> seats = new();
        [Tooltip("Where the piece's interaction (and its highlight) is anchored.")]
        public Vector2 interactPoint;
        [Tooltip("Where people stand to use it; the first is the main one.")]
        public List<Vector2> usePoints = new();
        [Tooltip("Where staff stand when working it, if not the first use point (the server by the pass).")]
        public bool hasStaffPost;
        public Vector2 staffPost;
        [Tooltip("The gold corners shown while it's the player's target.")]
        public Rect highlight;
        [Tooltip("Where Surface items can sit (D4).")]
        public List<Vector2> surfaces = new();
        public List<FurnitureLight> lights = new();
        [Tooltip("Display slots (plates waiting on the pass).")]
        public List<Vector2> slots = new();
        [Tooltip("Where a station's status overlay goes (the stew pot's simmer bar).")]
        public Vector2 statusPoint;
    }
}
