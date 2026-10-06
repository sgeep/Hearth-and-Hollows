using System;
using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>The moments of decorating that get feedback (4f step 2): each plays its combined sound and haptic.</summary>
    public enum DecorateMoment
    {
        Enter,
        Leave,
        PickUp,
        Place,
        Turn,
        Flip,
        Invalid,
        Store,
        FromStorage,
        Undo,
        PutBack,
        /// <summary>A piece bought from the catalogue (4f step 4).</summary>
        Buy,
        Sell,
        /// <summary>A new colourway or palette (4f step 5).</summary>
        Restyle,
        /// <summary>A floor or wall finish laid.</summary>
        Finish,
        /// <summary>A boss trophy goes up for the first time (4f Checkpoint C): the bigger moment.</summary>
        Homecoming,
        /// <summary>Off to another area of the property (4f step 6).</summary>
        Area,
    }

    /// <summary>
    /// Decorate Mode (4f step 2; GDD §6.6; 4f plan §3): a tile cursor over the room, controller first, with the mouse
    /// pointing at tiles. Pick a piece up, carry it (its ghost shows where it would stand, green or red with the reason),
    /// turn it (D2), mirror it (D3), put it down, or put it away in storage (D12); take pieces out of storage; undo, or
    /// put it all back. Never during service. Leaving keeps the layout (saved in the day loop) and rebuilds the room, the
    /// walkable grid and the service's seats and posts. Rules are <see cref="FurnitureLayout"/>'s; the check of what
    /// service needs is <see cref="LayoutCheck"/>'s and only keeps the doors shut, it never stops you decorating (D13).
    /// </summary>
    public sealed class DecorateMode : MonoBehaviour
    {
        [SerializeField, Min(0.05f), Tooltip("Seconds a held direction waits before repeating.")] float m_RepeatDelay = 0.28f;
        [SerializeField, Min(0.02f), Tooltip("Seconds between repeats while held.")] float m_RepeatRate = 0.09f;
        [SerializeField, Range(0f, 1f), Tooltip("How see-through the carried piece's ghost is.")] float m_GhostAlpha = 0.8f;
        [SerializeField] Color m_Fits = new(0.45f, 1f, 0.5f, 0.35f);
        [SerializeField] Color m_Blocked = new(1f, 0.35f, 0.3f, 0.4f);

        readonly Stack<List<PlacedFurniture>> m_Undo = new();
        /// <summary>Surface items riding on the carried piece: they come back down with it.</summary>
        readonly List<PlacedFurniture> m_Riders = new();
        bool m_Pointing;
        bool m_WasFree;
        Vector2 m_PointerWorld;
        /// <summary>Where the carried piece was grabbed, from its origin (world tiles): it follows the mouse from there.</summary>
        Vector2 m_GrabWorld;
        /// <summary>Where a carried surface item is being held (its ghost stands there when no surface is in reach).</summary>
        Vector2 m_SurfacePoint;
        List<PlacedFurniture> m_Entered;
        FurnitureLayout m_Layout;
        PlacedFurniture m_From;
        Vector2Int m_Grab;
        Vector2 m_HeldDirection;
        float m_HeldFor, m_NextRepeat;
        Vector2 m_LastPointer;
        Transform m_Ghost;
        DirectionReader m_MoveReader;
        InputAction m_Move, m_Point, m_Select, m_Click, m_Cancel, m_Turn, m_Flip, m_Store, m_Undoing, m_Cycle, m_Storage, m_Check, m_Wheel, m_Free, m_Style, m_AreaKey;
        /// <summary>Each area's finishes as they were on entering (put it all back restores them).</summary>
        readonly Dictionary<string, (string floor, string wall)> m_EnteredFinishes = new();

        public static DecorateMode Instance { get; private set; }

        public bool IsActive { get; private set; }
        public AreaFurniture Area { get; private set; }
        public FurnitureLayout Layout => m_Layout;
        public Vector2Int Cursor { get; private set; }
        /// <summary>The piece being carried (not in the layout while carried), or null.</summary>
        public PlacedFurniture Carried { get; private set; }
        public PlacementCheck CarriedCheck { get; private set; }
        /// <summary>Which of the pieces under the cursor is picked first (cycled with Cycle).</summary>
        public int HoverIndex { get; private set; }
        public LayoutReport Report { get; private set; } = LayoutReport.Clear;
        /// <summary>A panel (storage or the check) has the controls; the room ignores them.</summary>
        public bool PanelOpen { get; set; }
        public bool CanUndo => m_Undo.Count > 0;
        /// <summary>
        /// Free placement (the owner's request after the Checkpoint A playtest, amending D1): while the free key is held,
        /// the carried piece goes where the mouse puts it, to the art pixel, and the arrows or d-pad move it a pixel at a
        /// time; otherwise standing pieces snap to whole tiles and decor to quarter tiles. It's stored as the piece's cell
        /// and nudge, so the rules, the walkable grid and the saves are the same either way.
        /// </summary>
        public bool FreeMode => ForceFree || (m_Free != null && m_Free.IsPressed());
        /// <summary>Tests: free placement without holding the key.</summary>
        public bool ForceFree { get; set; }
        /// <summary>The mouse is what's pointing (it picks by what's drawn under it); otherwise the tile cursor.</summary>
        public bool Pointing => m_Pointing;

        /// <summary>Raised when anything shown changes (cursor, carried piece, layout).</summary>
        public event Action Changed;
        public event Action<DecorateMoment> MomentPlayed;
        /// <summary>The player asked for the storage panel or the check (the UI opens them).</summary>
        public event Action StorageRequested, CheckRequested;
        /// <summary>The player asked for the colour panel (the UI opens it for <see cref="StyleTarget"/>).</summary>
        public event Action StyleRequested;

        /// <summary>
        /// Developer sandbox (4f step 3; F8 with debug keys): the whole catalogue is open and free, so every piece can be
        /// tried in the room. Never on in a normal game.
        /// </summary>
        public static bool Sandbox { get; set; }

        /// <summary>The last look given to a piece (copy colours): its definition, colourway and palette.</summary>
        public (string definition, string variant, string palette)? LastLook { get; private set; }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            m_MoveReader?.Dispose();
            if (Instance == this) Instance = null;
        }

        static TavernDirector Director => TavernDirector.Instance;

        /// <summary>Decorating is for the quiet parts of the day: the daytime, Prep and the night (never during service).</summary>
        public bool CanEnter => !IsActive && AreaFurniture.Tavern != null && (Director == null ||
            Director.Phase is TavernPhase.Daytime or TavernPhase.Prep or TavernPhase.Night) && (KeeperWork.Instance == null || KeeperWork.Instance.ActiveCook == null);

        /// <summary>Decorates the area the keeper is in (the tavern, or the guest room when they've gone upstairs).</summary>
        public void Enter()
        {
            PropertyArea here = PropertyArea.Current;
            Enter(here != null ? AreaFurniture.Find(here.Id) ?? AreaFurniture.Tavern : AreaFurniture.Tavern);
        }

        public void Enter(AreaFurniture area)
        {
            if (IsActive || area == null) return;
            if (Director != null && Director.IsServing) return;
            m_EnteredFinishes.Clear();
            Begin(area);
            FindActions();
            IsActive = true;
            InputMaps.Activate(InputMaps.Decorate);
            m_LastPointer = m_Point != null ? m_Point.ReadValue<Vector2>() : Vector2.zero;
            Refresh();
            MomentPlayed?.Invoke(DecorateMoment.Enter);
            OfferHomecoming();
        }

        /// <summary>The trophy on the cursor waiting for its first place on the wall (null once it's up, or put away).</summary>
        public string HomecomingPiece { get; private set; }

        /// <summary>
        /// A boss trophy earned and not yet hung (plan §16): offered once, already on the cursor, the first time Decorate Mode
        /// opens after it was earned. Put away instead, it waits in storage like any other piece.
        /// </summary>
        void OfferHomecoming()
        {
            string pending = Area?.State.PendingHomecoming;
            if (string.IsNullOrEmpty(pending)) return;
            Area.State.PendingHomecoming = null;
            if (TakeFromStorage(pending)) HomecomingPiece = pending;
            Changed?.Invoke();
        }

        /// <summary>Starts working on an area: its layout copied, the cursor in the middle of its floor, the camera on it.</summary>
        void Begin(AreaFurniture area)
        {
            Area = area;
            m_Entered = new List<PlacedFurniture>();
            foreach (PlacedFurniture p in area.CurrentLayout()) m_Entered.Add(p.Clone());
            m_Layout = new FurnitureLayout(area.Shape(), area.Definition, m_Entered);
            m_Undo.Clear();
            Carried = null;
            m_From = null;
            m_Riders.Clear();
            m_Pointing = false;
            PanelOpen = false;
            AreaShape shape = m_Layout.Shape;
            Cursor = new Vector2Int(shape.Floor.xMin + shape.Floor.width / 2, shape.Floor.yMin + shape.Floor.height / 2);
            string id = area.Area.Id;
            if (!m_EnteredFinishes.ContainsKey(id))
                m_EnteredFinishes[id] = (area.State.Finish(id, FinishKind.Floor), area.State.Finish(id, FinishKind.Wall));
            TavernView.Show(area.Area);
        }

        /// <summary>
        /// Goes to the property's next area without walking (4f step 6): this one's layout is kept, the next one's opened, and
        /// the camera moves there. Undo starts afresh in each.
        /// </summary>
        public void SwitchArea()
        {
            if (!IsActive) return;
            var areas = new List<AreaFurniture>(AreaFurniture.All);
            if (areas.Count < 2) return;
            areas.Sort((a, b) => a.Area.Kind != b.Area.Kind ? a.Area.Kind.CompareTo(b.Area.Kind) : string.CompareOrdinal(a.Area.Id, b.Area.Id));
            if (Carried != null) PutBack();
            Area.Commit(m_Layout.Pieces);
            AreaFurniture next = areas[(areas.IndexOf(Area) + 1) % areas.Count];
            ClearGhost();
            Begin(next);
            m_Pointing = false;
            Refresh();
            MomentPlayed?.Invoke(DecorateMoment.Area);
        }

        /// <summary>Done: a carried piece goes back where it came from, the layout is kept and the room rebuilt.</summary>
        public void Leave()
        {
            if (!IsActive) return;
            if (Carried != null) PutBack();
            Area.Commit(m_Layout.Pieces);
            IsActive = false;
            PanelOpen = false;
            ClearGhost();
            // Back to where the keeper is.
            TavernView.Show(PropertyArea.Current);
            InputMaps.ActivateUIOnly();
            MomentPlayed?.Invoke(DecorateMoment.Leave);
            Changed?.Invoke();
        }

        void FindActions()
        {
            InputAction A(string name) => InputMaps.Find(InputMaps.Decorate, name);
            m_Move = A(DecorateActions.Move);
            m_MoveReader?.Dispose();
            m_MoveReader = new DirectionReader(m_Move);
            m_Point = A(DecorateActions.Point);
            m_Select = A(DecorateActions.Select);
            m_Click = A(DecorateActions.Click);
            m_Cancel = A(DecorateActions.Cancel);
            m_Turn = A(DecorateActions.Turn);
            m_Flip = A(DecorateActions.Flip);
            m_Store = A(DecorateActions.Store);
            m_Undoing = A(DecorateActions.Undo);
            m_Cycle = A(DecorateActions.Cycle);
            m_Storage = A(DecorateActions.Storage);
            m_Check = A(DecorateActions.Check);
            m_Wheel = A(DecorateActions.Wheel);
            m_Free = A(DecorateActions.Free);
            m_Style = A(DecorateActions.Style);
            m_AreaKey = A(DecorateActions.Area);
        }

        static bool Pressed(InputAction a) => a != null && a.WasPressedThisFrame();

        void Update()
        {
            if (!IsActive || PanelOpen)
            {
                // A panel has the controls: its taps aren't the cursor's.
                m_MoveReader?.Clear();
                return;
            }
            ReadCursor();
            // Holding or letting go of the free key re-places the carried piece under the mouse at once.
            bool free = FreeMode;
            if (free != m_WasFree && Carried != null)
            {
                if (m_Pointing) FollowPointer();
                else Refresh();
            }
            m_WasFree = free;
            if (Pressed(m_Select)) PickOrPlace();
            else if (Pressed(m_Click)) PickOrPlace();
            if (Pressed(m_Cancel))
            {
                if (Carried != null) PutBack();
                else Leave();
                return;
            }
            if (Pressed(m_Turn)) Turn();
            if (m_Wheel != null && Mathf.Abs(m_Wheel.ReadValue<Vector2>().y) > 0.1f && Carried != null) Turn();
            if (Pressed(m_Flip)) Flip();
            if (Pressed(m_Store)) Store();
            if (Pressed(m_Undoing)) Undo();
            if (Pressed(m_Cycle)) CycleHover();
            if (Pressed(m_Storage)) StorageRequested?.Invoke();
            if (Pressed(m_Check)) CheckRequested?.Invoke();
            if (Pressed(m_Style)) StyleRequested?.Invoke();
            if (Pressed(m_AreaKey)) SwitchArea();
        }

        /// <summary>The stick, d-pad or keys move a tile at a time (repeating while held); the mouse puts the cursor under it.</summary>
        void ReadCursor()
        {
            Vector2 dir = m_MoveReader != null ? m_MoveReader.Read() : Vector2.zero;
            var step = new Vector2Int(Mathf.Abs(dir.x) > 0.5f ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.5f ? (int)Mathf.Sign(dir.y) : 0);
            if (step != Vector2Int.zero)
            {
                m_Pointing = false;
                if (dir != m_HeldDirection && m_HeldFor == 0f)
                {
                    Step(step);
                    m_NextRepeat = m_RepeatDelay;
                }
                m_HeldFor += Time.unscaledDeltaTime;
                if (m_HeldFor >= m_NextRepeat)
                {
                    Step(step);
                    m_NextRepeat += m_RepeatRate;
                }
                m_HeldDirection = dir;
            }
            else
            {
                m_HeldFor = 0f;
                m_HeldDirection = Vector2.zero;
            }

            if (m_Point == null || Camera.main == null) return;
            Vector2 pointer = m_Point.ReadValue<Vector2>();
            if ((pointer - m_LastPointer).sqrMagnitude < 1f) return;
            m_LastPointer = pointer;
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -Camera.main.transform.position.z));
            Point(world);
        }

        /// <summary>A key or d-pad step: a tile, or a pixel for the carried piece in free mode.</summary>
        void Step(Vector2Int step)
        {
            if (Carried != null && FreeMode) NudgeCarried(step);
            else MoveCursor(step);
        }

        public void MoveCursor(Vector2Int step)
        {
            m_Pointing = false;
            SetCursor(Cursor + step);
        }

        /// <summary>The mouse at <paramref name="world"/>: hover by what's drawn there, or carry the held piece along.</summary>
        public void Point(Vector2 world)
        {
            m_Pointing = true;
            m_PointerWorld = world;
            if (Carried != null)
            {
                FollowPointer();
                return;
            }
            RectInt b = m_Layout.Shape.Bounds;
            Vector2Int cell = m_Layout.Shape.CellOf(world);
            Cursor = new Vector2Int(Mathf.Clamp(cell.x, b.xMin, b.xMax - 1), Mathf.Clamp(cell.y, b.yMin, b.yMax - 1));
            Refresh();
        }

        Vector2 Origin(PlacedFurniture p) => m_Layout.Shape.Origin + p.cell + (Vector2)p.nudge / FurnitureGeometry.PixelsPerTile;

        bool CarriedIsSurface => Carried != null && m_Layout.Definition(Carried.definition)?.layer == FurnitureLayer.Surface;

        /// <summary>
        /// Puts the carried piece's origin at <paramref name="world"/>: snapped to whole tiles for standing pieces (D1), to
        /// quarter tiles for decor, or to the art pixel in free mode; stored as a cell and a nudge of under half a tile.
        /// </summary>
        void SetCarriedOrigin(Vector2 world, bool free)
        {
            FurnitureDefinition d = m_Layout.Definition(Carried.definition);
            Vector2 local = world - m_Layout.Shape.Origin;
            float step = free ? 1f / FurnitureGeometry.PixelsPerTile : d != null && d.BlocksMovement ? 1f : 0.25f;
            local = new Vector2(Mathf.Round(local.x / step) * step, Mathf.Round(local.y / step) * step);
            Vector2Int cell = Vector2Int.FloorToInt(local + new Vector2(0.5f, 0.5f));
            Vector2 rest = (local - cell) * FurnitureGeometry.PixelsPerTile;
            Carried.cell = cell;
            Carried.nudge = new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(rest.x), FurnitureGeometry.NudgeMin, FurnitureGeometry.NudgeMax),
                Mathf.Clamp(Mathf.RoundToInt(rest.y), FurnitureGeometry.NudgeMin, FurnitureGeometry.NudgeMax));
        }

        void FollowPointer()
        {
            if (CarriedIsSurface) TargetSurface(m_PointerWorld);
            else SetCarriedOrigin(m_PointerWorld - m_GrabWorld, FreeMode);
            Cursor = m_Layout.Shape.CellOf(m_PointerWorld);
            Refresh();
        }

        /// <summary>Free mode with keys or the d-pad: the carried piece moves a pixel.</summary>
        void NudgeCarried(Vector2Int step)
        {
            if (CarriedIsSurface) return;
            SetCarriedOrigin(Origin(Carried) + (Vector2)step / FurnitureGeometry.PixelsPerTile, true);
            Cursor = Carried.cell + m_Grab;
            Refresh();
        }

        /// <summary>A carried surface item goes on the nearest free table or shelf anchor within a tile, if there is one.</summary>
        void TargetSurface(Vector2 world)
        {
            m_SurfacePoint = world;
            (int host, int anchor) = m_Layout.NearestSurface(world);
            Carried.host = host;
            Carried.anchor = anchor;
        }

        public void SetCursor(Vector2Int cell)
        {
            RectInt b = m_Layout.Shape.Bounds;
            cell = new Vector2Int(Mathf.Clamp(cell.x, b.xMin, b.xMax - 1), Mathf.Clamp(cell.y, b.yMin, b.yMax - 1));
            bool wasPointing = m_Pointing;
            m_Pointing = false;
            if (cell == Cursor && !wasPointing) return;
            Cursor = cell;
            HoverIndex = 0;
            if (Carried != null)
            {
                if (CarriedIsSurface) TargetSurface(m_Layout.Shape.Origin + Cursor + new Vector2(0.5f, 0.5f));
                else
                {
                    FurnitureDefinition d = m_Layout.Definition(Carried.definition);
                    Carried.cell = Cursor - m_Grab;
                    if (d != null && d.BlocksMovement) Carried.nudge = Vector2Int.zero;
                }
            }
            Refresh();
        }

        /// <summary>The pieces under the cursor (by what's drawn under the mouse, or what stands on the tile), the one picked first first.</summary>
        public List<PlacedFurniture> Hovered() =>
            !IsActive ? new List<PlacedFurniture>() : m_Pointing ? m_Layout.AtPoint(m_PointerWorld) : m_Layout.At(Cursor);

        public PlacedFurniture HoveredPiece
        {
            get
            {
                List<PlacedFurniture> here = Hovered();
                return here.Count == 0 ? null : here[HoverIndex % here.Count];
            }
        }

        public void CycleHover()
        {
            if (Carried != null) return;
            HoverIndex++;
            Refresh();
        }

        // ------------------------------------------------------------------ the actions

        public void PickOrPlace()
        {
            if (Carried != null) Place();
            else PickUp();
        }

        public void PickUp()
        {
            PlacedFurniture piece = HoveredPiece;
            if (piece == null || Carried != null) return;
            PushUndo();
            ResolvedFurniture before = m_Layout.Resolve(piece);
            Vector2 grabbedAt = m_Pointing ? m_PointerWorld : before != null ? FurnitureGeometry.ArtBounds(before).center : Origin(piece);
            List<PlacedFurniture> removed = m_Layout.Remove(piece.uid);
            // Surface items on it ride along and come back down with it.
            m_Riders.Clear();
            for (int i = 1; i < removed.Count; i++) m_Riders.Add(removed[i]);
            m_From = piece.Clone();
            Carried = piece;
            m_Grab = Cursor - piece.cell;
            m_GrabWorld = grabbedAt - Origin(piece);
            m_SurfacePoint = grabbedAt;
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.PickUp);
        }

        public void Place()
        {
            if (Carried == null) return;
            CarriedCheck = m_Layout.Check(Carried);
            if (!CarriedCheck.IsValid)
            {
                MomentPlayed?.Invoke(DecorateMoment.Invalid);
                Changed?.Invoke();
                return;
            }
            m_Layout.Add(Carried);
            // Riders come down with their host; any its new facing has no anchor for go to storage.
            foreach (PlacedFurniture rider in m_Riders)
            {
                m_Layout.Add(rider);
                if (m_Layout.Resolve(rider) == null) m_Layout.Remove(rider.uid);
            }
            m_Riders.Clear();
            string placed = Carried.definition;
            Carried = null;
            m_From = null;
            Rebuild();
            // Facts for later reactions (4g): what went where, and a trophy's first time up.
            string areaId = Area.Area.Id;
            EventBus<FurniturePlaced>.Publish(new FurniturePlaced(placed, areaId));
            if (placed == HomecomingPiece)
            {
                HomecomingPiece = null;
                EventBus<TrophyDisplayed>.Publish(new TrophyDisplayed(placed, areaId));
                MomentPlayed?.Invoke(DecorateMoment.Homecoming);
                return;
            }
            MomentPlayed?.Invoke(DecorateMoment.Place);
        }

        /// <summary>The carried piece goes back where it was (or to storage, if it came from there).</summary>
        public void PutBack()
        {
            if (Carried == null) return;
            if (m_From != null)
            {
                m_Layout.Add(m_From);
                foreach (PlacedFurniture rider in m_Riders) m_Layout.Add(rider);
            }
            m_Riders.Clear();
            Carried = null;
            m_From = null;
            if (m_Undo.Count > 0) m_Undo.Pop();
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.PutBack);
        }

        /// <summary>Turns the carried piece, or the hovered one in place if it fits that way (D2).</summary>
        public void Turn()
        {
            PlacedFurniture piece = Carried ?? HoveredPiece;
            if (piece == null) return;
            FurnitureDefinition d = m_Layout.Definition(piece.definition);
            if (d == null) return;
            int next = d.NextTurns(piece.turns);
            if (next == piece.turns)
            {
                MomentPlayed?.Invoke(DecorateMoment.Invalid);
                return;
            }
            if (Carried != null)
            {
                Carried.turns = next;
                Carried.nudge = Vector2Int.zero;
                ClampGrab();
                Refresh();
            }
            else if (!Adjust(piece, p => { p.turns = next; p.nudge = Vector2Int.zero; })) return;
            MomentPlayed?.Invoke(DecorateMoment.Turn);
        }

        /// <summary>Mirrors the carried or hovered piece, where its art allows (D3).</summary>
        public void Flip()
        {
            PlacedFurniture piece = Carried ?? HoveredPiece;
            if (piece == null) return;
            FurnitureDefinition d = m_Layout.Definition(piece.definition);
            if (d == null || !d.flippable)
            {
                MomentPlayed?.Invoke(DecorateMoment.Invalid);
                return;
            }
            if (Carried != null)
            {
                Carried.flipped = !Carried.flipped;
                Refresh();
            }
            else if (!Adjust(piece, p => p.flipped = !p.flipped)) return;
            MomentPlayed?.Invoke(DecorateMoment.Flip);
        }

        /// <summary>Changes a placed piece in place if it still fits; otherwise nothing changes and the reason shows.</summary>
        bool Adjust(PlacedFurniture piece, Action<PlacedFurniture> change)
        {
            PlacedFurniture changed = piece.Clone();
            change(changed);
            CarriedCheck = m_Layout.Check(changed);
            if (!CarriedCheck.IsValid)
            {
                MomentPlayed?.Invoke(DecorateMoment.Invalid);
                Changed?.Invoke();
                return false;
            }
            PushUndo();
            change(piece);
            Rebuild();
            return true;
        }

        /// <summary>After a turn, keep the cursor over the carried piece's footprint.</summary>
        void ClampGrab()
        {
            ResolvedFurniture r = m_Layout.Resolve(Carried);
            if (r == null) return;
            m_Grab = new Vector2Int(Mathf.Clamp(m_Grab.x, 0, r.Footprint.width - 1), Mathf.Clamp(m_Grab.y, 0, r.Footprint.height - 1));
            Carried.cell = Cursor - m_Grab;
        }

        /// <summary>The carried piece, or the hovered one, goes to storage (D12: never destroyed).</summary>
        public void Store()
        {
            if (Carried != null)
            {
                Carried = null;
                m_From = null;
                m_Riders.Clear();
                Rebuild();
                MomentPlayed?.Invoke(DecorateMoment.Store);
                return;
            }
            PlacedFurniture piece = HoveredPiece;
            if (piece == null) return;
            PushUndo();
            m_Layout.Remove(piece.uid);
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.Store);
        }

        /// <summary>What's in storage now, counting this area's working layout: (definition, copies) in catalogue order.</summary>
        public List<(FurnitureDefinition definition, int count)> Storage()
        {
            var result = new List<(FurnitureDefinition, int)>();
            if (!IsActive) return result;
            FurnitureState state = Area.State;
            foreach (var pair in state.Owned)
            {
                FurnitureDefinition d = Area.Definition(pair.Key);
                if (d == null) continue;
                int placedElsewhere = state.PlacedCount(pair.Key) - Count(state.Layout(Area.Area.Id), pair.Key);
                int placedHere = Count(m_Layout.Pieces, pair.Key) + (Carried != null && Carried.definition == pair.Key ? 1 : 0);
                int free = pair.Value - placedElsewhere - placedHere;
                if (free > 0) result.Add((d, free));
            }
            result.Sort((a, b) => string.CompareOrdinal(a.Item1.id, b.Item1.id));
            return result;
        }

        static int Count(IReadOnlyList<PlacedFurniture> pieces, string id)
        {
            int n = 0;
            foreach (PlacedFurniture p in pieces)
                if (p.definition == id) n++;
            return n;
        }

        /// <summary>Takes a copy out of storage onto the cursor.</summary>
        public bool TakeFromStorage(string definition)
        {
            if (!IsActive || Carried != null) return false;
            FurnitureDefinition d = Area.Definition(definition);
            if (d == null || !Storage().Exists(s => s.definition == d)) return false;
            int turns = 0;
            foreach (int t in d.AllowedTurns())
            {
                turns = t;
                break;
            }
            Carried = new PlacedFurniture { uid = Area.State.TakeUid(), definition = definition, cell = Cursor, turns = turns };
            m_From = null;
            m_Riders.Clear();
            m_Grab = Vector2Int.zero;
            // Held by the middle of what's drawn.
            ResolvedFurniture fresh = m_Layout.Resolve(Carried);
            m_GrabWorld = fresh != null ? FurnitureGeometry.ArtBounds(fresh).center - Origin(Carried) : new Vector2(0.5f, 0.5f);
            if (d.layer == FurnitureLayer.Surface) TargetSurface(m_Pointing ? m_PointerWorld : m_Layout.Shape.Origin + Cursor + new Vector2(0.5f, 0.5f));
            else if (m_Pointing) SetCarriedOrigin(m_PointerWorld - m_GrabWorld, FreeMode);
            PushUndo();
            Refresh();
            MomentPlayed?.Invoke(DecorateMoment.FromStorage);
            return true;
        }

        public void Undo()
        {
            if (m_Undo.Count == 0) return;
            Carried = null;
            m_From = null;
            m_Riders.Clear();
            m_Layout.Restore(m_Undo.Pop());
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.Undo);
        }

        /// <summary>Every change since entering is undone.</summary>
        public void PutAllBack()
        {
            if (!IsActive) return;
            Carried = null;
            m_From = null;
            m_Riders.Clear();
            m_Undo.Clear();
            m_Layout.Restore(m_Entered);
            if (m_EnteredFinishes.TryGetValue(Area.Area.Id, out var finishes))
            {
                Area.State.SetFinish(Area.Area.Id, FinishKind.Floor, finishes.floor);
                Area.State.SetFinish(Area.Area.Id, FinishKind.Wall, finishes.wall);
            }
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.Undo);
        }

        // ------------------------------------------------------------------ the catalogue (4f step 4)

        public GameState Game => Area != null ? Area.Game : null;
        public int[] Thresholds => Area != null && Area.Content != null ? Area.Content.CatalogThresholds() : new[] { 0, 25, 60, 100 };

        /// <summary>Every piece in the game, in catalogue order (category, tier, price, name).</summary>
        public List<FurnitureDefinition> Catalogue()
        {
            var all = new List<FurnitureDefinition>();
            if (Area?.Content == null) return all;
            foreach (FurnitureDefinition d in Area.Content.furniture)
                if (d != null) all.Add(d);
            all.Sort((a, b) => a.category != b.category ? a.category.CompareTo(b.category)
                : a.catalogTier != b.catalogTier ? a.catalogTier.CompareTo(b.catalogTier)
                : a.price != b.price ? a.price.CompareTo(b.price) : string.CompareOrdinal(a.id, b.id));
            return all;
        }

        /// <summary>Copies of a piece in storage now (counting this area's working layout and what's carried).</summary>
        public int Stored(string definition)
        {
            foreach (var (d, count) in Storage())
                if (d.id == definition) return count;
            return 0;
        }

        /// <summary>Copies placed anywhere, counting this area's working layout.</summary>
        public int Placed(string definition)
        {
            if (!IsActive) return 0;
            FurnitureState state = Area.State;
            return state.PlacedCount(definition) - Count(state.Layout(Area.Area.Id), definition) + Count(m_Layout.Pieces, definition)
                   + (Carried != null && Carried.definition == definition ? 1 : 0);
        }

        public PurchaseProblem CanBuy(FurnitureDefinition piece)
        {
            if (piece == null || Game == null) return PurchaseProblem.NotForSale;
            if (Sandbox) return piece.unique && Area.State.OwnedCount(piece.id) > 0 ? PurchaseProblem.AlreadyOwned : PurchaseProblem.None;
            return FurnitureShop.CanBuy(piece, Game, Thresholds);
        }

        /// <summary>Buys a copy (D7), into storage, or straight onto the cursor (<paramref name="place"/>). Delivery is immediate (4f).</summary>
        public bool Buy(string definition, bool place = false)
        {
            FurnitureDefinition d = Area?.Definition(definition);
            if (!IsActive || d == null || (place && Carried != null)) return false;
            if (CanBuy(d) != PurchaseProblem.None)
            {
                MomentPlayed?.Invoke(DecorateMoment.Invalid);
                return false;
            }
            if (Sandbox) Area.State.AddOwnedCopies(d.id, 1);
            else FurnitureShop.Buy(Game, d, Thresholds);
            GameFlow.Instance?.FurnitureChanged();
            MomentPlayed?.Invoke(DecorateMoment.Buy);
            if (place) TakeFromStorage(d.id);
            else Refresh();
            return true;
        }

        /// <summary>Sells one copy from storage for its sell-back price (D12: bought pieces only).</summary>
        public bool Sell(string definition)
        {
            FurnitureDefinition d = Area?.Definition(definition);
            if (!IsActive || d == null || !FurnitureShop.Sell(Game, d, Stored(definition)))
            {
                MomentPlayed?.Invoke(DecorateMoment.Invalid);
                return false;
            }
            GameFlow.Instance?.FurnitureChanged();
            MomentPlayed?.Invoke(DecorateMoment.Sell);
            Refresh();
            return true;
        }

        public string Finish(FinishKind kind) => IsActive ? Area.State.Finish(Area.Area.Id, kind) : null;

        public PurchaseProblem CanBuy(FinishDefinition finish)
        {
            if (finish == null || Game == null) return PurchaseProblem.NotForSale;
            if (Sandbox) return Area.State.OwnsFinish(finish.id) ? PurchaseProblem.AlreadyOwned : PurchaseProblem.None;
            return FurnitureShop.CanBuy(finish, Game, Thresholds);
        }

        /// <summary>Lays a finish over this area's whole floor or wall (D5), buying it first if it isn't owned yet.</summary>
        public bool UseFinish(FinishDefinition finish)
        {
            if (!IsActive || finish == null) return false;
            FurnitureState state = Area.State;
            if (!state.OwnsFinish(finish.id))
            {
                if (CanBuy(finish) != PurchaseProblem.None)
                {
                    MomentPlayed?.Invoke(DecorateMoment.Invalid);
                    return false;
                }
                if (Sandbox) state.OwnFinish(finish.id);
                else FurnitureShop.Buy(Game, finish, Thresholds);
                MomentPlayed?.Invoke(DecorateMoment.Buy);
            }
            state.SetFinish(Area.Area.Id, finish.kind, finish.id);
            Area.ApplyFinishes();
            GameFlow.Instance?.FurnitureChanged();
            MomentPlayed?.Invoke(DecorateMoment.Finish);
            Refresh();
            return true;
        }

        // ------------------------------------------------------------------ looks (4f step 5)

        /// <summary>The piece the colour panel works on: the one carried, or under the cursor.</summary>
        public PlacedFurniture StyleTarget => Carried ?? HoveredPiece;

        /// <summary>Gives the carried or hovered piece a colourway and palette (D11). Undoable; nothing about where it stands changes.</summary>
        public bool SetLook(string variant, string palette)
        {
            PlacedFurniture piece = StyleTarget;
            FurnitureDefinition d = piece != null ? m_Layout.Definition(piece.definition) : null;
            if (d == null || !d.HasLooks) return false;
            variant ??= string.Empty;
            palette = FurniturePalette.Restrict(palette, d.paletteChannels);
            if (piece.variant == variant && piece.palette == palette) return false;
            if (Carried != null)
            {
                Carried.variant = variant;
                Carried.palette = palette;
                Refresh();
            }
            else
            {
                PushUndo();
                piece.variant = variant;
                piece.palette = palette;
                Rebuild();
            }
            LastLook = (d.id, variant, palette);
            MomentPlayed?.Invoke(DecorateMoment.Restyle);
            return true;
        }

        /// <summary>
        /// Copies the last look given to a piece onto this one: the colourway when it's the same piece, the palette ramps
        /// for whichever channels it shares.
        /// </summary>
        public bool CopyLastLook()
        {
            PlacedFurniture piece = StyleTarget;
            if (piece == null || LastLook == null) return false;
            var (definition, variant, palette) = LastLook.Value;
            return SetLook(definition == piece.definition ? variant : piece.variant, string.IsNullOrEmpty(palette) ? piece.palette : palette);
        }

        /// <summary>Gives every copy of the target's piece in this area the target's look. Returns how many changed.</summary>
        public int ApplyLookToAll()
        {
            PlacedFurniture piece = StyleTarget;
            if (piece == null) return 0;
            int changed = 0;
            List<PlacedFurniture> before = m_Layout.Snapshot();
            foreach (PlacedFurniture p in m_Layout.Pieces)
            {
                if (p == piece || p.definition != piece.definition || (p.variant == piece.variant && p.palette == piece.palette)) continue;
                p.variant = piece.variant;
                p.palette = piece.palette;
                changed++;
            }
            if (changed == 0) return 0;
            m_Undo.Push(before);
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.Restyle);
            return changed;
        }

        /// <summary>Moves the cursor to a piece (the check's "show me").</summary>
        public void Focus(int uid)
        {
            PlacedFurniture piece = m_Layout?.Find(uid);
            ResolvedFurniture r = piece != null ? m_Layout.Resolve(piece) : null;
            if (r == null) return;
            m_Pointing = false;
            SetCursor(r.Footprint.min);
        }

        void PushUndo() => m_Undo.Push(m_Layout.Snapshot());

        /// <summary>The room is rebuilt from the working layout (the carried piece isn't in it), and the check rerun.</summary>
        void Rebuild()
        {
            Area.Build(m_Layout.Pieces);
            Refresh();
        }

        void Refresh()
        {
            Report = LayoutCheck.For(m_Layout.Shape, m_Layout.ResolveAll());
            CarriedCheck = Carried != null ? m_Layout.Check(Carried) : new PlacementCheck(PlacementProblem.None);
            DrawGhost();
            Changed?.Invoke();
        }

        // ------------------------------------------------------------------ the ghost and the cursor

        void ClearGhost()
        {
            if (m_Ghost == null) return;
            // Out of sight at once (it's destroyed at the frame's end).
            m_Ghost.gameObject.SetActive(false);
            m_Ghost.SetParent(null, false);
            Destroy(m_Ghost.gameObject);
            m_Ghost = null;
        }

        /// <summary>The cursor's corners round the hovered piece (or the tile), and the carried piece drawn where it would stand.</summary>
        void DrawGhost()
        {
            ClearGhost();
            if (!IsActive) return;
            FurniturePresentation look = Area.Presentation;
            m_Ghost = new GameObject("Decorate Ghost").transform;
            m_Ghost.SetParent(transform, false);

            if (Carried != null)
            {
                bool surface = CarriedIsSurface;
                ResolvedFurniture r = m_Layout.Resolve(Carried) ??
                                      (surface ? FurnitureGeometry.Resolve(m_Layout.Definition(Carried.definition), Carried, m_Layout.Shape.Origin, m_SurfacePoint) : null);
                if (r != null)
                {
                    // Snapping: green or red corners round the drawing (not the floor tiles, which a chair or barrel is
                    // drawn a quarter tile off). Free placement: no outline, the piece itself turns red where it can't go
                    // (Checkpoint A playtest).
                    bool fits = CarriedCheck.IsValid;
                    Color tile = fits ? m_Fits : m_Blocked;
                    if (!FreeMode) Corners(look, FurnitureGeometry.ArtBounds(r), new Color(tile.r, tile.g, tile.b, 1f));
                    foreach (PlacedArt art in r.Art)
                    {
                        var go = new GameObject(art.Art.name);
                        go.transform.SetParent(m_Ghost, false);
                        go.transform.position = art.Position;
                        go.transform.rotation = Quaternion.Euler(0f, 0f, art.Degrees);
                        var sprite = go.AddComponent<SpriteRenderer>();
                        sprite.sprite = Area.Drawing(art, r);
                        sprite.flipX = art.FlipX;
                        sprite.sortingLayerName = SortingLayers.Above;
                        sprite.sortingOrder = 10 + art.Art.order;
                        sprite.color = fits ? new Color(1f, 1f, 1f, m_GhostAlpha) : new Color(1f, 0.45f, 0.4f, m_GhostAlpha);
                        if (look != null && look.overlayMaterial != null) sprite.sharedMaterial = look.overlayMaterial;
                    }
                }
                return;
            }

            PlacedFurniture hovered = HoveredPiece;
            ResolvedFurniture h = hovered != null ? m_Layout.Resolve(hovered) : null;
            // Round what's drawn (a chair sits a quarter tile off its cell), or the tile under the cursor.
            Corners(look, h != null ? FurnitureGeometry.ArtBounds(h) : new Rect(Cursor + m_Layout.Shape.Origin, Vector2.one), look != null ? look.highlightColor : Color.white);
        }

        void Corners(FurniturePresentation look, Rect frame, Color color)
        {
            if (look == null) return;
            Rect outer = new(frame.xMin - 0.125f, frame.yMin - 0.125f, frame.width + 0.25f, frame.height + 0.25f);
            foreach (var (sprite, at) in new[]
                     {
                         (look.cornerTopLeft, new Vector2(outer.xMin, outer.yMax)), (look.cornerTopRight, new Vector2(outer.xMax, outer.yMax)),
                         (look.cornerBottomLeft, new Vector2(outer.xMin, outer.yMin)), (look.cornerBottomRight, new Vector2(outer.xMax, outer.yMin)),
                     })
            {
                var go = new GameObject("Corner");
                go.transform.SetParent(m_Ghost, false);
                go.transform.position = at;
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerName = SortingLayers.Above;
                renderer.sortingOrder = 11;
                renderer.color = color;
                if (look.overlayMaterial != null) renderer.sharedMaterial = look.overlayMaterial;
            }
        }
    }
}
