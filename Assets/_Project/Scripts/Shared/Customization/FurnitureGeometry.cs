using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// Turns and mirrors footprint-space geometry (tiles from the footprint's bottom-left corner). The mirror comes
    /// first (x → width − x), then whole quarter turns counter-clockwise, each keeping the result in its own box:
    /// (x, y) → (height − y, x). Points, rectangles and directions all go through the same transform, so a turned
    /// piece's footprint, bodies, seats, use points, surfaces and lights stay together. Pure logic.
    /// </summary>
    public readonly struct FurnitureTransform
    {
        public readonly Vector2Int Size;
        public readonly int Turns;
        public readonly bool Flip;

        public FurnitureTransform(Vector2Int size, int turns, bool flip)
        {
            Size = size;
            Turns = ((turns % 4) + 4) % 4;
            Flip = flip;
        }

        public static FurnitureTransform Identity(Vector2Int size) => new(size, 0, false);

        /// <summary>The footprint's size once turned.</summary>
        public Vector2Int TurnedSize => Turns % 2 == 0 ? Size : new Vector2Int(Size.y, Size.x);

        /// <summary>How far a sprite is rotated, in degrees (counter-clockwise).</summary>
        public float Degrees => 90f * Turns;

        public Vector2 Point(Vector2 p)
        {
            float w = Size.x, h = Size.y;
            if (Flip) p.x = w - p.x;
            for (int i = 0; i < Turns; i++)
            {
                p = new Vector2(h - p.y, p.x);
                (w, h) = (h, w);
            }
            return p;
        }

        public Vector2Int Direction(Vector2Int d)
        {
            if (Flip) d.x = -d.x;
            for (int i = 0; i < Turns; i++) d = new Vector2Int(-d.y, d.x);
            return d;
        }

        public Rect Rect(Rect r)
        {
            Vector2 a = Point(r.min), b = Point(r.max);
            return UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }

    /// <summary>One art layer as placed in the room: where its pivot is, how it's turned and mirrored.</summary>
    public readonly struct PlacedArt
    {
        public readonly FurnitureArt Art;
        public readonly Vector2 Position;
        public readonly float Degrees;
        public readonly bool FlipX;

        public PlacedArt(FurnitureArt art, Vector2 position, float degrees, bool flipX)
        {
            Art = art;
            Position = position;
            Degrees = degrees;
            FlipX = flipX;
        }
    }

    /// <summary>A seat as placed: its point, where to step from, and the way the sitter faces.</summary>
    public readonly struct PlacedSeat
    {
        public readonly Vector2 Position;
        public readonly Vector2 Approach;
        public readonly Vector2Int Facing;

        public PlacedSeat(Vector2 position, Vector2 approach, Vector2Int facing)
        {
            Position = position;
            Approach = approach;
            Facing = facing;
        }
    }

    /// <summary>A light as placed.</summary>
    public readonly struct PlacedLight
    {
        public readonly FurnitureLight Light;
        public readonly Vector2 Position;

        public PlacedLight(FurnitureLight light, Vector2 position)
        {
            Light = light;
            Position = position;
        }
    }

    /// <summary>
    /// Everything about a placed piece in area coordinates (tiles; the area's origin added): the footprint cells, the
    /// bodies, art, seats, use points, staff post, highlight, surfaces, lights, display slots and status point.
    /// </summary>
    public sealed class ResolvedFurniture
    {
        public FurnitureDefinition Definition;
        public PlacedFurniture Placement;
        public FurnitureTransform Transform;
        /// <summary>Footprint cells in the area's grid (no area origin: cell coordinates).</summary>
        public RectInt Footprint;
        public readonly List<Rect> Bodies = new();
        public readonly List<PlacedArt> Art = new();
        public bool Grouped;
        public Vector2 GroupPoint;
        public readonly List<PlacedSeat> Seats = new();
        public Vector2 InteractPoint;
        public readonly List<Vector2> UsePoints = new();
        public Vector2 StaffPost;
        public Rect Highlight;
        public readonly List<Vector2> Surfaces = new();
        public readonly List<PlacedLight> Lights = new();
        public readonly List<Vector2> Slots = new();
        public Vector2 StatusPoint;
    }

    /// <summary>
    /// Resolves a placement against its definition: picks the facing for its turns, applies flip and rotation, and
    /// moves everything to the cell plus its nudge (decision D1: whole-tile footprints; the nudge carries the exact
    /// pixel position, up to half a tile, for decor and for the 4e starting layout). Pure logic, EditMode-tested.
    /// </summary>
    public static class FurnitureGeometry
    {
        public const int PixelsPerTile = 8;
        /// <summary>Nudge range per axis, in art pixels: under half a tile either way.</summary>
        public const int NudgeMin = -4, NudgeMax = 3;

        public static bool IsValidNudge(Vector2Int nudge) =>
            nudge.x >= NudgeMin && nudge.x <= NudgeMax && nudge.y >= NudgeMin && nudge.y <= NudgeMax;

        /// <summary>
        /// Null when the piece can't stand at the placement's turns, or flipped when it can't flip. A Surface item stands at
        /// <paramref name="surfaceAnchor"/> (its host's anchor, in the world; null: it can't stand at all), and its art is
        /// placed relative to it.
        /// </summary>
        public static ResolvedFurniture Resolve(FurnitureDefinition definition, PlacedFurniture placement, Vector2 areaOrigin = default,
            Vector2? surfaceAnchor = null)
        {
            if (definition == null || placement == null) return null;
            if (!definition.TryGetFacing(placement.turns, out FurnitureFacing facing, out int rotate)) return null;
            if (placement.flipped && !definition.flippable) return null;
            bool onSurface = definition.layer == FurnitureLayer.Surface;
            if (onSurface && !surfaceAnchor.HasValue) return null;

            var t = new FurnitureTransform(facing.size, rotate, placement.flipped);
            Vector2 offset = onSurface ? surfaceAnchor.Value : areaOrigin + (Vector2)placement.cell + (Vector2)placement.nudge / PixelsPerTile;
            Vector2 P(Vector2 local) => offset + t.Point(local);
            Rect R(Rect local)
            {
                Rect r = t.Rect(local);
                r.position += offset;
                return r;
            }

            var resolved = new ResolvedFurniture
            {
                Definition = definition,
                Placement = placement,
                Transform = t,
                Footprint = new RectInt(onSurface ? Vector2Int.FloorToInt(offset - areaOrigin) : placement.cell, t.TurnedSize),
                InteractPoint = P(facing.interactPoint),
                Highlight = R(facing.highlight),
                StatusPoint = P(facing.statusPoint),
            };
            foreach (Rect body in facing.bodies) resolved.Bodies.Add(R(body));
            foreach (FurnitureArt art in facing.art)
                if (art != null) resolved.Art.Add(new PlacedArt(art, P(art.position), t.Degrees, t.Flip));
            foreach (FurnitureSeat seat in facing.seats) resolved.Seats.Add(new PlacedSeat(P(seat.position), P(seat.approach), t.Direction(seat.facing)));
            foreach (Vector2 use in facing.usePoints) resolved.UsePoints.Add(P(use));
            resolved.StaffPost = facing.hasStaffPost ? P(facing.staffPost) : resolved.UsePoints.Count > 0 ? resolved.UsePoints[0] : resolved.InteractPoint;
            foreach (Vector2 surface in facing.surfaces) resolved.Surfaces.Add(P(surface));
            foreach (FurnitureLight light in facing.lights)
                if (light != null) resolved.Lights.Add(new PlacedLight(light, P(light.position)));
            foreach (Vector2 slot in facing.slots) resolved.Slots.Add(P(slot));

            // A turned or mirrored drawing sorts as one at the bottom of what it now covers, so characters in front
            // still draw in front (a pivot turned to the top would sort it as if it stood further back).
            resolved.Grouped = facing.grouped || t.Turns != 0 || t.Flip;
            resolved.GroupPoint = facing.grouped && t.Turns == 0 && !t.Flip ? P(facing.groupPoint) : SortPoint(resolved, offset);
            return resolved;
        }

        /// <summary>
        /// What the piece covers as drawn, in the world: the union of its art layers' sprite rectangles (turned and mirrored
        /// as placed). Decorate Mode picks and frames pieces by it, so a chair drawn a quarter tile off its cell is picked
        /// where it's seen.
        /// </summary>
        public static Rect ArtBounds(ResolvedFurniture r)
        {
            bool any = false;
            Rect union = default;
            foreach (PlacedArt art in r.Art)
            {
                Sprite sprite = art.Art.frames != null && art.Art.frames.Length > 0 ? art.Art.frames[0] : art.Art.sprite;
                if (sprite == null) continue;
                Bounds b = sprite.bounds;
                float x0 = b.min.x, x1 = b.max.x;
                if (art.FlipX) (x0, x1) = (-x1, -x0);
                Vector2 Turn(Vector2 p)
                {
                    for (int i = 0; i < Mathf.RoundToInt(art.Degrees / 90f) % 4; i++) p = new Vector2(-p.y, p.x);
                    return p;
                }
                Vector2 a = Turn(new Vector2(x0, b.min.y)), c = Turn(new Vector2(x1, b.max.y));
                Rect rect = Rect.MinMaxRect(Mathf.Min(a.x, c.x), Mathf.Min(a.y, c.y), Mathf.Max(a.x, c.x), Mathf.Max(a.y, c.y));
                rect.position += art.Position;
                union = any ? Rect.MinMaxRect(Mathf.Min(union.xMin, rect.xMin), Mathf.Min(union.yMin, rect.yMin),
                    Mathf.Max(union.xMax, rect.xMax), Mathf.Max(union.yMax, rect.yMax)) : rect;
                any = true;
            }
            if (any) return union;
            return new Rect(r.Footprint.position, r.Footprint.size);
        }

        /// <summary>The bottom-centre of a turned piece: centred on its footprint, at the lowest of its bodies (or the footprint's bottom, with none).</summary>
        static Vector2 SortPoint(ResolvedFurniture r, Vector2 offset)
        {
            float y = offset.y;
            if (r.Bodies.Count > 0)
            {
                y = float.MaxValue;
                foreach (Rect b in r.Bodies) y = Mathf.Min(y, b.yMin);
            }
            return new Vector2(offset.x + r.Footprint.width / 2f, y);
        }
    }
}
