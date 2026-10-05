using System;
using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Staff;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Builds an area's placed furniture in the scene (4f step 1: furniture as data): each piece's art, pixel-exact
    /// bodies on the obstacle layer, lights, interaction and station overlays, from its definition and placement
    /// (turned and mirrored by <see cref="FurnitureGeometry"/>). In the tavern it then gives the service its seats (the
    /// usable seats, in placement order) and staff posts, gives the keeper the stations, and tells the walkable grid
    /// the layout changed. The layout comes from the game's state, or the starting layout when the scene is played on
    /// its own. Runs before the tavern's director wakes.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class AreaFurniture : MonoBehaviour
    {
        const string k_RootName = "Placed Furniture";

        [SerializeField] PropertyArea m_Area;
        [SerializeField, Tooltip("Definitions and the starting layout (when played without a game running).")]
        GameDatabase m_Database;
        [SerializeField] FurniturePresentation m_Presentation;
        [SerializeField, Tooltip("Serving reach, for the seats.")]
        TavernContent m_Content;

        readonly List<ResolvedFurniture> m_Resolved = new();
        readonly List<TavernSeat> m_Seats = new();
        Transform m_Root;

        static readonly List<AreaFurniture> s_All = new();

        public static IReadOnlyList<AreaFurniture> All => s_All;

        public PropertyArea Area => m_Area;
        /// <summary>The pieces as built, in placement order.</summary>
        public IReadOnlyList<ResolvedFurniture> Pieces => m_Resolved;
        public IReadOnlyList<TavernSeat> Seats => m_Seats;
        public FurniturePresentation Presentation => m_Presentation;

        /// <summary>Raised after every build.</summary>
        public event Action Built;

        public void Configure(PropertyArea area, GameDatabase database, FurniturePresentation presentation, TavernContent content)
        {
            m_Area = area;
            m_Database = database;
            m_Presentation = presentation;
            m_Content = content;
        }

        static GameFlow Flow => GameFlow.Instance != null && GameFlow.Instance.InGame ? GameFlow.Instance : null;
        GameDatabase Database => Flow != null && Flow.Database != null ? Flow.Database : m_Database;

        public FurnitureDefinition Definition(string id) => Database != null ? Database.Furniture(id) : null;

        /// <summary>The area's layout now: the game's, or the starting layout played on its own.</summary>
        public IReadOnlyList<PlacedFurniture> CurrentLayout()
        {
            GameFlow flow = Flow;
            if (flow != null)
            {
                if (!flow.State.Furniture.Initialized) flow.State.Furniture.GrantStarter(Database != null ? Database.startingFurniture : null);
                return flow.State.Furniture.Layout(m_Area.Id);
            }
            return m_Database != null && m_Database.startingFurniture != null ? m_Database.startingFurniture.Layout(m_Area.Id) : Array.Empty<PlacedFurniture>();
        }

        void OnEnable() => s_All.Add(this);
        void OnDisable() => s_All.Remove(this);

        void Awake()
        {
            if (m_Area == null) m_Area = GetComponent<PropertyArea>();
            Build(CurrentLayout());
        }

        /// <summary>Resolves a placement in this area (null if its definition is unknown or it can't stand that way).</summary>
        public ResolvedFurniture Resolve(PlacedFurniture placement) =>
            FurnitureGeometry.Resolve(Definition(placement?.definition), placement, m_Area.Origin);

        /// <summary>Builds the pieces (replacing any built before) and hands the tavern its seats, posts and stations.</summary>
        public void Build(IReadOnlyList<PlacedFurniture> placements)
        {
            ClearBuilt();
            m_Root = new GameObject(k_RootName).transform;
            m_Root.SetParent(transform, false);

            foreach (PlacedFurniture placement in placements)
            {
                ResolvedFurniture resolved = Resolve(placement);
                if (resolved == null)
                {
                    Debug.LogWarning($"[Hearthdelve] Furniture '{placement?.definition}' can't be placed in {m_Area.Id} (unknown, or not at that turn or mirror).");
                    continue;
                }
                m_Resolved.Add(resolved);
            }

            var stations = new Dictionary<StationKind, TavernInteractable>();
            TavernInteractable pass = null;
            var posts = new Dictionary<StaffStation, Vector2>();
            foreach (ResolvedFurniture piece in m_Resolved)
            {
                TavernInteractable use = BuildPiece(piece);
                if (use == null) continue;
                if (piece.Definition.function == FurnitureFunction.Pass && pass == null)
                {
                    pass = use;
                    posts[StaffStation.Serving] = piece.StaffPost;
                }
                else if (piece.Definition.function == FurnitureFunction.Station && !stations.ContainsKey(piece.Definition.station))
                {
                    stations[piece.Definition.station] = use;
                    StaffStation job = StaffJob(piece.Definition.station);
                    if (job != StaffStation.None) posts[job] = piece.StaffPost;
                }
            }

            var seatUses = new List<TavernInteractable>();
            var seatRoot = new GameObject("Seats").transform;
            seatRoot.SetParent(m_Root, false);
            int n = 0;
            foreach (var (_, seat) in FurnitureRules.UsableSeats(m_Resolved))
            {
                TavernSeat built = BuildSeat(seatRoot, seat, ++n);
                m_Seats.Add(built);
                seatUses.Add(built.GetComponent<TavernInteractable>());
            }

            if (m_Area.Kind == AreaKind.Tavern)
            {
                TavernLayout layout = FindInScene<TavernLayout>();
                if (layout != null) layout.SetFurniture(m_Seats, posts);
                KeeperWork keeper = FindInScene<KeeperWork>();
                if (keeper != null)
                    keeper.Configure(stations.GetValueOrDefault(StationKind.Grill), stations.GetValueOrDefault(StationKind.Tap),
                        stations.GetValueOrDefault(StationKind.StewPot), pass, seatUses.ToArray());
            }

            EventBus<NavigationLayoutChanged>.Publish(new NavigationLayoutChanged());
            Built?.Invoke();
        }

        T FindInScene<T>() where T : Component
        {
            foreach (T found in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
                if (found.gameObject.scene == gameObject.scene) return found;
            return null;
        }

        void ClearBuilt()
        {
            m_Resolved.Clear();
            m_Seats.Clear();
            Transform old = transform.Find(k_RootName);
            while (old != null)
            {
                // Out of the way at once (interactables unregister, colliders stop counting), destroyed at the frame's end.
                old.gameObject.SetActive(false);
                old.SetParent(null, false);
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
                old = transform.Find(k_RootName);
            }
        }

        static StaffStation StaffJob(StationKind station) => station switch
        {
            StationKind.Grill => StaffStation.Grill,
            StationKind.Tap => StaffStation.Tap,
            StationKind.StewPot => StaffStation.StewPot,
            _ => StaffStation.None,
        };

        static TavernInteractableKind? InteractableKind(FurnitureDefinition definition) => definition.function switch
        {
            FurnitureFunction.Pass => TavernInteractableKind.Pass,
            FurnitureFunction.Station => definition.station switch
            {
                StationKind.Grill => TavernInteractableKind.Grill,
                StationKind.Tap => TavernInteractableKind.Tap,
                StationKind.StewPot => TavernInteractableKind.StewPot,
                _ => null,
            },
            _ => null,
        };

        // ------------------------------------------------------------------ pieces

        /// <summary>Builds one piece; returns its interaction if it has one.</summary>
        TavernInteractable BuildPiece(ResolvedFurniture piece)
        {
            Vector2 origin = m_Area.Origin + piece.Placement.cell + (Vector2)piece.Placement.nudge / FurnitureGeometry.PixelsPerTile;
            var root = new GameObject($"{piece.Definition.id}#{piece.Placement.uid}");
            root.transform.SetParent(m_Root, false);
            root.transform.position = origin;
            root.AddComponent<FurnitureView>().Configure(piece);

            if (piece.Definition.BlocksMovement && piece.Bodies.Count > 0)
            {
                root.layer = LayerMask.NameToLayer(Layers.Obstacles);
                foreach (Rect body in piece.Bodies)
                {
                    var box = root.AddComponent<BoxCollider2D>();
                    box.size = body.size;
                    box.offset = body.center - origin;
                }
            }

            Transform art = root.transform;
            if (piece.Grouped)
            {
                art = new GameObject("Group").transform;
                art.SetParent(root.transform, false);
                art.position = piece.GroupPoint;
                SortingGroup group = art.gameObject.AddComponent<SortingGroup>();
                group.sortingLayerName = SortingLayers.YSorted;
            }

            var renderers = new List<(PlacedArt, SpriteRenderer)>();
            foreach (PlacedArt placed in piece.Art)
            {
                FurnitureArt layer = placed.Art;
                var go = new GameObject(string.IsNullOrEmpty(layer.name) ? "Art" : layer.name);
                go.transform.SetParent(art, false);
                go.transform.position = placed.Position;
                go.transform.rotation = Quaternion.Euler(0f, 0f, placed.Degrees);
                var renderer = go.AddComponent<SpriteRenderer>();
                bool animated = layer.frames != null && layer.frames.Length > 0;
                renderer.sprite = animated ? layer.frames[0] : layer.sprite;
                renderer.flipX = placed.FlipX;
                renderer.sortingLayerName = layer.sortingLayer;
                renderer.sortingOrder = layer.order;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                if (m_Presentation != null && m_Presentation.litMaterial != null) renderer.sharedMaterial = m_Presentation.litMaterial;
                if (animated) go.AddComponent<SpriteLoop>().Configure(layer.frames, layer.frameSeconds);
                renderers.Add((placed, renderer));
            }

            foreach (PlacedLight placed in piece.Lights) BuildLight(root.transform, placed);

            TavernInteractable use = null;
            TavernInteractableKind? kind = InteractableKind(piece.Definition);
            if (kind.HasValue && piece.UsePoints.Count > 0)
            {
                var useObject = new GameObject("Use").transform;
                useObject.SetParent(art, false);
                useObject.position = piece.InteractPoint;
                use = Interactable(useObject, kind.Value, piece.Definition.useNameKey, piece.UsePoints, piece.Definition.reach,
                    RelativeTo(piece.Highlight, piece.InteractPoint));
            }

            Dress(piece, art, renderers);
            return use;
        }

        static Rect RelativeTo(Rect r, Vector2 to) => new(r.position - to, r.size);

        void BuildLight(Transform parent, PlacedLight placed)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.position = placed.Position;
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = placed.Light.color;
            light.intensity = placed.Light.intensity;
            light.pointLightInnerRadius = placed.Light.innerRadius;
            light.pointLightOuterRadius = placed.Light.outerRadius;
            light.falloffIntensity = placed.Light.falloff;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            light.targetSortingLayers = ids;
        }

        TavernInteractable Interactable(Transform at, TavernInteractableKind kind, string nameKey, IReadOnlyList<Vector2> usePoints, float reach, Rect frame)
        {
            Vector2 anchor = at.position;
            var others = new Vector2[Mathf.Max(0, usePoints.Count - 1)];
            for (int i = 1; i < usePoints.Count; i++) others[i - 1] = usePoints[i] - anchor;
            GameObject highlight = Highlight(at, frame);
            var interactable = at.gameObject.AddComponent<TavernInteractable>();
            interactable.Configure(kind, nameKey, usePoints[0] - anchor, reach, highlight, others);
            highlight.SetActive(false);
            return interactable;
        }

        /// <summary>Gold corners around <paramref name="frame"/> (relative to <paramref name="at"/>) and a bobbing marker above.</summary>
        GameObject Highlight(Transform at, Rect frame)
        {
            var highlight = new GameObject("Highlight").transform;
            highlight.SetParent(at, false);
            if (m_Presentation == null) return highlight.gameObject;
            Rect outer = new(frame.xMin - 0.125f, frame.yMin - 0.125f, frame.width + 0.25f, frame.height + 0.25f);
            Overlay(highlight, "CornerTL", m_Presentation.cornerTopLeft, new Vector2(outer.xMin, outer.yMax), 0);
            Overlay(highlight, "CornerTR", m_Presentation.cornerTopRight, new Vector2(outer.xMax, outer.yMax), 0);
            Overlay(highlight, "CornerBL", m_Presentation.cornerBottomLeft, new Vector2(outer.xMin, outer.yMin), 0);
            Overlay(highlight, "CornerBR", m_Presentation.cornerBottomRight, new Vector2(outer.xMax, outer.yMin), 0);
            SpriteRenderer marker = Overlay(highlight, "Marker", m_Presentation.marker, new Vector2(frame.center.x, frame.yMax + 0.25f), 1);
            marker.gameObject.AddComponent<SpriteBob>();
            return highlight.gameObject;
        }

        SpriteRenderer Overlay(Transform parent, string name, Sprite sprite, Vector2 local, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = SortingLayers.Above;
            renderer.sortingOrder = order;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.color = m_Presentation.highlightColor;
            if (m_Presentation.overlayMaterial != null) renderer.sharedMaterial = m_Presentation.overlayMaterial;
            return renderer;
        }

        TavernSeat BuildSeat(Transform parent, PlacedSeat seat, int number)
        {
            var go = new GameObject($"Seat{number}");
            go.transform.SetParent(parent, false);
            go.transform.position = seat.Position;
            TavernSeat built = go.AddComponent<TavernSeat>();
            built.Configure(seat.Approach - seat.Position, TavernSeat.FacingFor(seat.Facing));
            float reach = (m_Content != null ? m_Content.serving.serving.arriveDistance : 1.2f) + 0.2f;
            Rect frame = m_Presentation != null ? m_Presentation.seatHighlight : new Rect(-0.5f, -0.1f, 1f, 1.85f);
            Interactable(go.transform, TavernInteractableKind.Seat, null, new[] { seat.Position }, reach, frame);
            return built;
        }

        // ------------------------------------------------------------------ station overlays

        /// <summary>What a working piece shows: plates on the pass, the stew pot's simmer bar and helpings, the kitchen at work.</summary>
        void Dress(ResolvedFurniture piece, Transform art, List<(PlacedArt placed, SpriteRenderer renderer)> renderers)
        {
            FurnitureDefinition d = piece.Definition;
            if (d.function == FurnitureFunction.Pass && piece.Slots.Count > 0)
            {
                var plates = new GameObject("Plates").transform;
                plates.SetParent(art, false);
                var slots = new SpriteRenderer[piece.Slots.Count];
                for (int i = 0; i < slots.Length; i++)
                {
                    var go = new GameObject($"Plate{i + 1}");
                    go.transform.SetParent(plates, false);
                    go.transform.position = piece.Slots[i];
                    slots[i] = go.AddComponent<SpriteRenderer>();
                    slots[i].sortingLayerName = SortingLayers.YSorted;
                    slots[i].sortingOrder = 1;
                    slots[i].spriteSortPoint = SpriteSortPoint.Pivot;
                    if (m_Presentation != null && m_Presentation.litMaterial != null) slots[i].sharedMaterial = m_Presentation.litMaterial;
                }
                art.gameObject.AddComponent<PassView>().Configure(slots);
            }
            if (d.function == FurnitureFunction.Station && d.station == StationKind.StewPot) DressStewPot(piece, art);
            if (d.function == FurnitureFunction.Station && d.station == StationKind.Grill)
                foreach (var (placed, renderer) in renderers)
                    if (placed.Art.workingFrames != null && placed.Art.workingFrames.Length > 0)
                        renderer.gameObject.AddComponent<KitchenView>().Configure(renderer, renderer.sprite, placed.Art.workingFrames);
        }

        void DressStewPot(ResolvedFurniture piece, Transform art)
        {
            Sprite pixel = m_Presentation != null ? m_Presentation.pixel : null;
            var status = new GameObject("Status").transform;
            status.SetParent(art, false);
            status.position = piece.StatusPoint;
            // Not "Bar": that's the bar's name.
            var bar = new GameObject("Progress").transform;
            bar.SetParent(status, false);
            SpriteRenderer back = Overlay(bar, "Back", pixel, Vector3.zero, 0);
            back.transform.localScale = new Vector3(12f, 3f, 1f);
            back.color = new Color(0.08f, 0.06f, 0.06f);
            var anchor = new GameObject("Anchor").transform;
            anchor.SetParent(bar, false);
            anchor.localPosition = new Vector3(-0.625f, 0f, 0f);
            SpriteRenderer fill = Overlay(anchor, "Fill", pixel, new Vector2(0.0625f, 0f), 1);
            fill.color = new Color(1f, 0.66f, 0.3f);
            var pips = new SpriteRenderer[5];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = Overlay(status, $"Pip{i + 1}", pixel, new Vector2((i - 2) * 0.375f, -0.375f), 1);
                pips[i].transform.localScale = new Vector3(2f, 2f, 1f);
                pips[i].color = Color.white;
            }
            art.gameObject.AddComponent<StewPotView>().Configure(bar.gameObject, anchor, pips);
        }
    }
}
