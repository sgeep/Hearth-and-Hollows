using System;
using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Customization;
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
        List<PlacedFurniture> m_Entered;
        FurnitureLayout m_Layout;
        PlacedFurniture m_From;
        Vector2Int m_Grab;
        Vector2 m_HeldDirection;
        float m_HeldFor, m_NextRepeat;
        Vector2 m_LastPointer;
        Transform m_Ghost;
        InputAction m_Move, m_Point, m_Select, m_Click, m_Cancel, m_Turn, m_Flip, m_Store, m_Undoing, m_Cycle, m_Storage, m_Check, m_Wheel;

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

        /// <summary>Raised when anything shown changes (cursor, carried piece, layout).</summary>
        public event Action Changed;
        public event Action<DecorateMoment> MomentPlayed;
        /// <summary>The player asked for the storage panel or the check (the UI opens them).</summary>
        public event Action StorageRequested, CheckRequested;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static TavernDirector Director => TavernDirector.Instance;

        /// <summary>Decorating is for the quiet parts of the day: the daytime, Prep and the night (never during service).</summary>
        public bool CanEnter => !IsActive && AreaFurniture.Tavern != null && (Director == null ||
            Director.Phase is TavernPhase.Daytime or TavernPhase.Prep or TavernPhase.Night) && (KeeperWork.Instance == null || KeeperWork.Instance.ActiveCook == null);

        public void Enter() => Enter(AreaFurniture.Tavern);

        public void Enter(AreaFurniture area)
        {
            if (IsActive || area == null) return;
            if (Director != null && Director.IsServing) return;
            Area = area;
            m_Entered = new List<PlacedFurniture>();
            foreach (PlacedFurniture p in area.CurrentLayout()) m_Entered.Add(p.Clone());
            m_Layout = new FurnitureLayout(area.Shape(), area.Definition, m_Entered);
            m_Undo.Clear();
            Carried = null;
            m_From = null;
            PanelOpen = false;
            AreaShape shape = m_Layout.Shape;
            Cursor = new Vector2Int(shape.Floor.xMin + shape.Floor.width / 2, shape.Floor.yMin + shape.Floor.height / 2);
            FindActions();
            IsActive = true;
            InputMaps.Activate(InputMaps.Decorate);
            m_LastPointer = m_Point != null ? m_Point.ReadValue<Vector2>() : Vector2.zero;
            Refresh();
            MomentPlayed?.Invoke(DecorateMoment.Enter);
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
            InputMaps.ActivateUIOnly();
            MomentPlayed?.Invoke(DecorateMoment.Leave);
            Changed?.Invoke();
        }

        void FindActions()
        {
            InputAction A(string name) => InputMaps.Find(InputMaps.Decorate, name);
            m_Move = A(DecorateActions.Move);
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
        }

        static bool Pressed(InputAction a) => a != null && a.WasPressedThisFrame();

        void Update()
        {
            if (!IsActive || PanelOpen) return;
            ReadCursor();
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
        }

        /// <summary>The stick, d-pad or keys move a tile at a time (repeating while held); the mouse puts the cursor under it.</summary>
        void ReadCursor()
        {
            Vector2 dir = m_Move != null ? m_Move.ReadValue<Vector2>() : Vector2.zero;
            var step = new Vector2Int(Mathf.Abs(dir.x) > 0.5f ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.5f ? (int)Mathf.Sign(dir.y) : 0);
            if (step != Vector2Int.zero)
            {
                if (dir != m_HeldDirection && m_HeldFor == 0f)
                {
                    MoveCursor(step);
                    m_NextRepeat = m_RepeatDelay;
                }
                m_HeldFor += Time.unscaledDeltaTime;
                if (m_HeldFor >= m_NextRepeat)
                {
                    MoveCursor(step);
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
            SetCursor(m_Layout.Shape.CellOf(world));
        }

        public void MoveCursor(Vector2Int step) => SetCursor(Cursor + step);

        public void SetCursor(Vector2Int cell)
        {
            RectInt b = m_Layout.Shape.Bounds;
            cell = new Vector2Int(Mathf.Clamp(cell.x, b.xMin, b.xMax - 1), Mathf.Clamp(cell.y, b.yMin, b.yMax - 1));
            if (cell == Cursor) return;
            Cursor = cell;
            HoverIndex = 0;
            if (Carried != null) Carried.cell = Cursor - m_Grab;
            Refresh();
        }

        /// <summary>The pieces under the cursor, the one picked first first.</summary>
        public List<PlacedFurniture> Hovered() => IsActive ? m_Layout.At(Cursor) : new List<PlacedFurniture>();

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
            List<PlacedFurniture> removed = m_Layout.Remove(piece.uid);
            // Surface items on it go to storage with their host (they'd have nothing to stand on).
            m_From = piece.Clone();
            Carried = piece;
            // D1: standing pieces move on whole tiles (putting it back restores its exact place).
            FurnitureDefinition d = m_Layout.Definition(piece.definition);
            if (d != null && d.BlocksMovement) Carried.nudge = Vector2Int.zero;
            m_Grab = Cursor - piece.cell;
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.PickUp);
            if (removed.Count > 1) MomentPlayed?.Invoke(DecorateMoment.Store);
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
            Carried = null;
            m_From = null;
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.Place);
        }

        /// <summary>The carried piece goes back where it was (or to storage, if it came from there).</summary>
        public void PutBack()
        {
            if (Carried == null) return;
            if (m_From != null) m_Layout.Add(m_From);
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
            m_Grab = Vector2Int.zero;
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
            m_Undo.Clear();
            m_Layout.Restore(m_Entered);
            Rebuild();
            MomentPlayed?.Invoke(DecorateMoment.Undo);
        }

        /// <summary>Moves the cursor to a piece (the check's "show me").</summary>
        public void Focus(int uid)
        {
            PlacedFurniture piece = m_Layout?.Find(uid);
            ResolvedFurniture r = piece != null ? m_Layout.Resolve(piece) : null;
            if (r != null) SetCursor(r.Footprint.min);
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
                ResolvedFurniture r = m_Layout.Resolve(Carried);
                if (r != null)
                {
                    Color tile = CarriedCheck.IsValid ? m_Fits : m_Blocked;
                    for (int x = r.Footprint.xMin; x < r.Footprint.xMax; x++)
                    for (int y = r.Footprint.yMin; y < r.Footprint.yMax; y++)
                        Tile(look, new Vector2(x, y) + m_Layout.Shape.Origin, tile);
                    foreach (PlacedArt art in r.Art)
                    {
                        var go = new GameObject(art.Art.name);
                        go.transform.SetParent(m_Ghost, false);
                        go.transform.position = art.Position;
                        go.transform.rotation = Quaternion.Euler(0f, 0f, art.Degrees);
                        var sprite = go.AddComponent<SpriteRenderer>();
                        sprite.sprite = art.Art.frames != null && art.Art.frames.Length > 0 ? art.Art.frames[0] : art.Art.sprite;
                        sprite.flipX = art.FlipX;
                        sprite.sortingLayerName = SortingLayers.Above;
                        sprite.sortingOrder = 10 + art.Art.order;
                        sprite.color = new Color(1f, 1f, 1f, m_GhostAlpha);
                        if (look != null && look.overlayMaterial != null) sprite.sharedMaterial = look.overlayMaterial;
                    }
                }
                return;
            }

            PlacedFurniture hovered = HoveredPiece;
            ResolvedFurniture h = hovered != null ? m_Layout.Resolve(hovered) : null;
            RectInt frame = h != null ? h.Footprint : new RectInt(Cursor, Vector2Int.one);
            Corners(look, new Rect(frame.position + m_Layout.Shape.Origin, frame.size));
        }

        void Tile(FurniturePresentation look, Vector2 cell, Color color)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(m_Ghost, false);
            go.transform.position = cell + new Vector2(0.5f, 0.5f);
            go.transform.localScale = new Vector3(8f, 8f, 1f);
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = look != null ? look.pixel : null;
            sprite.sortingLayerName = SortingLayers.Above;
            sprite.sortingOrder = 9;
            sprite.color = color;
            if (look != null && look.overlayMaterial != null) sprite.sharedMaterial = look.overlayMaterial;
        }

        void Corners(FurniturePresentation look, Rect frame)
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
                renderer.color = look.highlightColor;
                if (look.overlayMaterial != null) renderer.sharedMaterial = look.overlayMaterial;
            }
        }
    }
}
