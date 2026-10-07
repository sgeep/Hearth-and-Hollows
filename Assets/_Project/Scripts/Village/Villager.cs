using System;
using System.Collections.Generic;
using Hearthdelve.Core.Animation;
using Hearthdelve.Core.Movement;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>How someone looks doing one of their activities (4h Checkpoint C): a held action, a flourish now and then, a face.</summary>
    [Serializable]
    public sealed class ActivityLook
    {
        public string activity;
        [Tooltip("Loop this while standing (Idle: just stand).")] public CharacterAnim hold = CharacterAnim.Idle;
        [Tooltip("Play this once every so often (Idle: never).")] public CharacterAnim flourish = CharacterAnim.Idle;
        [Tooltip("Show this over their head every so often (none: never).")] public Sprite emote;
        [Min(0f), Tooltip("Seconds between flourishes or faces (0: never).")] public float every;
    }

    /// <summary>
    /// A named villager in the world (4h Checkpoint C; Musashi since 2026-10-07): one per character per scene that can host them,
    /// shown, walked and hidden by <see cref="VillagePresence"/> from the character's schedule. Thin: their look is the layered
    /// NPC animator, their walking a grid path at a stroll, and talking to them is the keeper's Interact (the story decides what's
    /// said: the character's hub). They never block the keeper while walking, and only stand solid when the keeper isn't in
    /// their way. Nothing about them is saved.
    /// </summary>
    public sealed class Villager : MonoBehaviour
    {
        static readonly List<Villager> s_All = new();

        [SerializeField, Tooltip("The character's stable id (CharacterIds).")] string m_CharacterId;
        [SerializeField, Tooltip("Their name in the Content table, for the 'talk to' prompt.")] string m_NameKey;
        [SerializeField] TavernInteractable m_Talk;
        [SerializeField, Tooltip("The surface area this copy of them lives in (kariaston, tavern).")] string m_Area = "kariaston";
        [SerializeField, Tooltip("Their drawing (hidden when they're not here, or indoors).")] GameObject m_Model;
        [SerializeField] LayeredSpriteAnimator m_Animator;
        [SerializeField, Tooltip("Solid while they stand (never while they walk).")] Collider2D m_Feet;
        [SerializeField] NpcEmote m_Emote;
        [SerializeField, Tooltip("The grid they walk on (none: the floor's own, NavGrid.Current).")] NavGrid m_Grid;
        [SerializeField] List<ActivityLook> m_Looks = new();
        [SerializeField, Tooltip("Not about until the village's presence places them (a copy in Tally Ho! for a visit).")] bool m_StartHidden;

        readonly List<Vector2> m_Path = new();
        int m_Next;
        float m_Walked;
        bool m_HideAtEnd;
        bool m_TalkFacing;
        float m_UntilLook;
        Transform m_Keeper;

        public static IReadOnlyList<Villager> All => s_All;
        public string CharacterId => m_CharacterId;
        public TavernInteractable Talk => m_Talk;
        public string Area => m_Area;
        public LayeredSpriteAnimator Animator => m_Animator;
        public NpcEmote Emote => m_Emote;

        /// <summary>Here (in this scene), whether seen or behind a window.</summary>
        public bool Shown { get; private set; } = true;
        /// <summary>Where they are or are walking to.</summary>
        public ScheduleAnchor At { get; private set; }
        public string Activity { get; private set; } = string.Empty;
        public bool Walking => m_Path.Count > 0;
        /// <summary>Behind a window (shown, unseen; talked to there).</summary>
        public bool Indoors => Shown && At != null && At.Window && !Walking;

        /// <summary>They walked to a place and are now there (not when simply placed): presentation listens (Kaloren's herbs).</summary>
        public static event Action<Villager> Arrived;

        /// <summary>The copy of them that is here now (or any copy, when none is).</summary>
        public static Villager Find(string characterId)
        {
            Villager any = null;
            foreach (Villager v in s_All)
            {
                if (v.m_CharacterId != characterId) continue;
                if (v.Shown) return v;
                any ??= v;
            }
            return any;
        }

        public void Configure(string characterId, string nameKey, TavernInteractable talk)
        {
            m_CharacterId = characterId;
            m_NameKey = nameKey;
            m_Talk = talk;
        }

        public void ConfigurePresence(string area, GameObject model, LayeredSpriteAnimator animator, Collider2D feet, NpcEmote emote, NavGrid grid, List<ActivityLook> looks,
            bool startHidden = false)
        {
            m_StartHidden = startHidden;
            if (startHidden)
            {
                if (model != null) model.SetActive(false);
                if (feet != null) feet.enabled = false;
            }
            m_Area = area;
            m_Model = model;
            m_Animator = animator;
            m_Feet = feet;
            m_Emote = emote;
            m_Grid = grid;
            m_Looks = looks ?? new List<ActivityLook>();
        }

        void Awake()
        {
            if (m_StartHidden) Shown = false;
        }

        void OnEnable()
        {
            s_All.Add(this);
            if (m_Talk == null) return;
            m_Talk.Describe = () => new TavernHint(TavernHintKind.Talk, m_NameKey);
            m_Talk.Used += OnUsed;
        }

        void OnDisable()
        {
            s_All.Remove(this);
            if (m_Talk == null) return;
            m_Talk.Describe = null;
            m_Talk.Used -= OnUsed;
        }

        void OnUsed(TavernInteractable _)
        {
            if (StoryServices.Conversations?.Talk(m_CharacterId) != true) return;
            // Turn to the keeper while they talk (a window has no one to turn).
            Transform keeper = Keeper();
            if (m_Animator == null || keeper == null || Indoors) return;
            Vector2 to = keeper.position - transform.position;
            m_Animator.LockFacing(FacingLogic.FromDirection(to.x, to.y, m_Animator.Facing, 0f));
            m_TalkFacing = true;
        }

        Transform Keeper()
        {
            if (m_Keeper == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                m_Keeper = player != null ? player.transform : null;
            }
            return m_Keeper;
        }

        // ---------- presence (VillagePresence decides; these carry it out) ----------

        /// <summary>Here at once, standing at <paramref name="anchor"/> (a load, or a change the keeper can't see).</summary>
        public void Place(ScheduleAnchor anchor, string activity)
        {
            m_Path.Clear();
            m_HideAtEnd = false;
            SetAt(anchor, activity);
            Shown = true;
            if (anchor != null) transform.position = anchor.Spot;
            Settle();
        }

        /// <summary>Walks from where they are to <paramref name="anchor"/>; placed there if no path is found.</summary>
        public void WalkTo(ScheduleAnchor anchor, string activity)
        {
            if (anchor == null || !Shown || !PlanPath(transform.position, anchor))
            {
                Place(anchor, activity);
                return;
            }
            m_HideAtEnd = false;
            SetAt(anchor, activity);
            StartWalking();
        }

        /// <summary>Appears at <paramref name="from"/> (their scene's door) and walks in to <paramref name="anchor"/>.</summary>
        public void Enter(Vector2 from, ScheduleAnchor anchor, string activity)
        {
            Shown = true;
            transform.position = from;
            WalkTo(anchor, activity);
        }

        /// <summary>Walks out to <paramref name="exit"/> (their scene's door) and is gone; gone at once with no exit or no path.</summary>
        public void Leave(Vector2? exit)
        {
            if (!Shown) return;
            SetAt(null, string.Empty);
            if (exit == null || !PlanPath(transform.position, null, exit.Value))
            {
                Hide();
                return;
            }
            m_HideAtEnd = true;
            StartWalking();
        }

        public void Hide()
        {
            m_Path.Clear();
            m_HideAtEnd = false;
            SetAt(null, string.Empty);
            Shown = false;
            Settle();
        }

        /// <summary>The keeper stands where they would stand solid: they stay walk-through until the keeper moves off.</summary>
        bool KeeperInTheWay()
        {
            Transform keeper = Keeper();
            return keeper != null && Vector2.Distance(keeper.position, transform.position) < 0.85f;
        }

        void SetAt(ScheduleAnchor anchor, string activity)
        {
            if (At != null && At != anchor) At.SetOccupied(false);
            At = anchor;
            Activity = activity ?? string.Empty;
            m_UntilLook = 2f;
        }

        void StartWalking()
        {
            m_Next = 0;
            m_Walked = 0f;
            if (m_Animator != null)
            {
                m_Animator.ReleaseFacing();
                m_Animator.Release();
            }
            Settle();
        }

        /// <summary>Shows or hides the drawing, the solid feet and the window glow for where they are and what they're doing.</summary>
        void Settle()
        {
            bool indoors = Indoors;
            bool seen = Shown && !indoors;
            if (m_Model != null && m_Model.activeSelf != seen) m_Model.SetActive(seen);
            if (At != null) At.SetOccupied(Shown && At.Window && !Walking);
            if (m_Feet != null) m_Feet.enabled = seen && !Walking && !KeeperInTheWay();
            if (m_Talk != null)
            {
                // Behind a window, they're talked to at the window.
                m_Talk.transform.position = indoors && At != null ? (Vector3)At.Spot : transform.position;
            }
            if (m_Animator == null) return;
            m_Animator.Movement = Vector2.zero;
            if (!seen || Walking) return;
            if (At != null) m_Animator.LockFacing(At.Facing);
            ActivityLook look = Look();
            if (look != null && look.hold != CharacterAnim.Idle) m_Animator.Hold(look.hold);
            else m_Animator.Release();
        }

        ActivityLook Look()
        {
            foreach (ActivityLook look in m_Looks)
                if (look != null && look.activity == Activity) return look;
            return null;
        }

        // ---------- walking ----------

        bool PlanPath(Vector2 from, ScheduleAnchor anchor, Vector2? point = null)
        {
            m_Path.Clear();
            NavGrid grid = m_Grid != null ? m_Grid : NavGrid.Current;
            Vector2 approach = anchor != null ? anchor.Approach : point ?? from;
            Vector2 end = anchor != null ? anchor.Spot : approach;
            if (grid == null) return false;
            GridMap map = grid.Map;
            GridSpace space = grid.Space;
            if (!map.TryFindNearestWalkable(space.ToCell(from), 3, out GridCell start) || !map.TryFindNearestWalkable(space.ToCell(approach), 4, out GridCell goal))
                return false;
            var cells = new List<GridCell>();
            if (!GridPathfinder.TryFindPath(map, start, goal, cells)) return false;
            // Cell centres, pruned to the corners a straight walk needs, then the exact spot (and the seat beyond its approach).
            var points = new List<Vector2> { from };
            foreach (GridCell c in cells) points.Add(space.CellCentre(c));
            points.Add(approach);
            Prune(points, map, space);
            for (int i = 1; i < points.Count; i++) m_Path.Add(points[i]);
            if ((end - approach).sqrMagnitude > 0.0001f) m_Path.Add(end);
            return true;
        }

        /// <summary>Drops waypoints a straight line can skip (every cell it crosses walkable).</summary>
        static void Prune(List<Vector2> points, GridMap map, GridSpace space)
        {
            int i = 0;
            while (i < points.Count - 2)
            {
                if (Clear(points[i], points[i + 2], map, space)) points.RemoveAt(i + 1);
                else i++;
            }
        }

        static bool Clear(Vector2 a, Vector2 b, GridMap map, GridSpace space)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / 0.25f);
            for (int s = 1; s < steps; s++)
            {
                Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                GridCell c = space.ToCell(p);
                if (!map.IsWalkable(c)) return false;
            }
            return true;
        }

        void Update()
        {
            if (Walking) Step();
            else if (Shown) Stand();
            UpdateTalk();
        }

        void Step()
        {
            VillageLifeSettings settings = VillageLife.Settings;
            m_Walked += Time.deltaTime;
            if (m_Walked > settings.longestWalkSeconds)
            {
                Finish(snap: true);
                return;
            }
            Vector2 at = transform.position;
            Vector2 target = m_Path[m_Next];
            float step = settings.walkSpeed * Time.deltaTime;
            Vector2 to = target - at;
            if (to.magnitude <= step)
            {
                transform.position = target;
                if (++m_Next >= m_Path.Count) Finish(snap: false);
                return;
            }
            Vector2 dir = to.normalized;
            transform.position = at + dir * step;
            if (m_Animator != null) m_Animator.Movement = dir;
        }

        void Finish(bool snap)
        {
            Vector2 end = m_Path.Count > 0 ? m_Path[m_Path.Count - 1] : (Vector2)transform.position;
            m_Path.Clear();
            if (snap) transform.position = end;
            if (m_HideAtEnd)
            {
                Hide();
                return;
            }
            Settle();
            Arrived?.Invoke(this);
        }

        void Stand()
        {
            // Solid once the keeper isn't standing where they stand.
            if (m_Feet != null && !m_Feet.enabled && m_Model != null && m_Model.activeSelf && !KeeperInTheWay()) m_Feet.enabled = true;
            ActivityLook look = Look();
            m_UntilLook -= Time.deltaTime;
            if (look == null || look.every <= 0f || m_UntilLook > 0f) return;
            m_UntilLook = look.every;
            if (Indoors || (m_Model != null && !m_Model.activeSelf))
            {
                // Behind a window: the face shows over the window.
                if (look.emote != null && m_Emote != null) m_Emote.Show(look.emote);
                return;
            }
            if (look.flourish != CharacterAnim.Idle && m_Animator != null) m_Animator.PlayOnce(look.flourish);
            if (look.emote != null && m_Emote != null) m_Emote.Show(look.emote);
        }

        void UpdateTalk()
        {
            IConversationService talk = StoryServices.Conversations;
            if (m_TalkFacing && (talk == null || !talk.IsTalking))
            {
                m_TalkFacing = false;
                if (m_Animator != null && At != null && !Walking) m_Animator.LockFacing(At.Facing);
            }
            if (m_Talk == null) return;
            bool daytime = TavernDirector.Instance != null && TavernDirector.Instance.Phase == TavernPhase.Daytime;
            bool available = daytime && Shown && !Walking && talk != null && !talk.IsTalking && talk.CanTalk(m_CharacterId);
            if (available != m_Talk.IsAvailable) m_Talk.SetAvailable(available);
        }

        /// <summary>Shows something over their head for a moment (Kaloren's herbs changing hands).</summary>
        public void ShowOverhead(Sprite sprite, float seconds = 0f)
        {
            if (m_Emote != null) m_Emote.Show(sprite, seconds);
        }
    }
}
