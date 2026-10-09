using System.Collections;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Quests;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using Unity.Cinemachine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// Plays a generated delve room by room (4d). At the start it generates the run from a seed (<see cref="RunGenerator"/>);
    /// then one room is loaded at a time, at the origin: the player arrives at its P, the run's encounter is placed on
    /// its spawn points, the navigation grid and the camera's bounds (<see cref="RoomCameraBounds"/>) switch to it, and
    /// its gates drop while anything in it is alive. When the last enemy falls the gates of the exits the graph uses
    /// rise (any other exit stays shut); each leads to the room the graph says. Leaving fades out, unloads the room
    /// (and anything left lying in it) and loads the next. One way: there is no going back. A descent room's hole drops
    /// the player to the next floor; the arena's rope appears once it's clear; rope rooms end the run.
    /// </summary>
    public sealed class RoomRunner : MonoBehaviour
    {
        [SerializeField] RunSettings m_Settings;
        [SerializeField, Tooltip("0: a new seed every run. Any other number replays that run (for bug reports).")]
        int m_Seed;
        [SerializeField, Tooltip("Development only: start the run in the boss arena (the last floor), to practise the boss. Ignored in release builds and the day loop.")]
        bool m_StartInArena;
        [SerializeField, Tooltip("Rooms are loaded under this.")]
        Transform m_RoomRoot;
        [SerializeField] NavGrid m_Nav;
        [SerializeField] CinemachineCamera m_Camera;
        [SerializeField] RoomCameraBounds m_CameraBounds;
        [SerializeField, Min(0f), Tooltip("Seconds the screen takes to cover when leaving a room.")]
        float m_FadeOut = Hearthdelve.Shared.Game.PlaceFade.Seconds;
        [SerializeField, Min(0f), Tooltip("Seconds the screen takes to uncover in the new room.")]
        float m_FadeIn = Hearthdelve.Shared.Game.PlaceFade.Seconds;
        [SerializeField, Min(0f), Tooltip("Seconds the fall into a hole takes before the screen covers.")]
        float m_FallTime = 0.45f;
        [SerializeField, Tooltip("The gates dropping as a fight starts: sound and haptic together.")]
        MMF_Player m_SealFeedback;
        [SerializeField, Tooltip("The gates rising when the room is clear: sound and haptic together.")]
        MMF_Player m_ClearFeedback;
        [SerializeField, Tooltip("Dropping into a hole: sound and haptic together.")]
        MMF_Player m_FallFeedback;
        [Header("What each exit promises (the door previews)")]
        [SerializeField] Sprite m_MarkerOut;
        [SerializeField] Sprite m_MarkerDeeper;
        [SerializeField] Sprite m_MarkerArena;
        [SerializeField] Sprite m_MarkerGold;
        [SerializeField] Sprite m_MarkerIngredient;
        [SerializeField] Sprite m_MarkerPower;
        [SerializeField] Sprite m_MarkerCurio;

        RoomEncounter m_Encounter;
        int m_Entered = -1;
        bool m_Transitioning;

        /// <summary>Tests: the seed the next run uses (0 = none).</summary>
        public static int SeedOverride { get; set; }
        /// <summary>Tests: start the next run in the arena (as the development toggle does).</summary>
        public static bool StartInArenaOverride { get; set; }
        public static RoomRunner Active { get; private set; }

        public RunGraph Graph { get; private set; }
        public FloorGraph Floor { get; private set; }
        public FloorNode Node { get; private set; }
        public RunSettings Settings => m_Settings;
        public RoomInstance Current { get; private set; }
        public RoomEncounter Encounter => m_Encounter;
        public bool IsTransitioning => m_Transitioning;
        /// <summary>Rooms entered so far this run, counting the current one.</summary>
        public int RoomsEntered => m_Entered + 1;

        public void Configure(RunSettings settings, Transform roomRoot, NavGrid nav, CinemachineCamera camera, RoomCameraBounds cameraBounds,
            MMF_Player sealFeedback, MMF_Player clearFeedback, MMF_Player fallFeedback, Sprite markerOut, Sprite markerDeeper, Sprite markerArena,
            Sprite markerGold, Sprite markerIngredient, Sprite markerPower, Sprite markerCurio = null)
        {
            m_Settings = settings;
            m_RoomRoot = roomRoot;
            m_Nav = nav;
            m_Camera = camera;
            m_CameraBounds = cameraBounds;
            m_SealFeedback = sealFeedback;
            m_ClearFeedback = clearFeedback;
            m_FallFeedback = fallFeedback;
            m_MarkerOut = markerOut;
            m_MarkerDeeper = markerDeeper;
            m_MarkerArena = markerArena;
            m_MarkerGold = markerGold;
            m_MarkerIngredient = markerIngredient;
            m_MarkerPower = markerPower;
            m_MarkerCurio = markerCurio;
        }

        void OnEnable()
        {
            Active = this;
            EventBus<BossEncounterEnded>.Subscribe(OnBossEnded);
            EventBus<EnemyKilled>.Subscribe(OnEnemyKilled);
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
            EventBus<BossEncounterEnded>.Unsubscribe(OnBossEnded);
            EventBus<EnemyKilled>.Unsubscribe(OnEnemyKilled);
        }

        // The boss's reward (4e step 4): its Gold and its larder cache, where it fell.
        void OnBossEnded(BossEncounterEnded e)
        {
            if (!e.Defeated || Current == null) return;
            Hearthdelve.Dungeon.Bosses.BossDefinition boss = m_Settings != null ? m_Settings.tuning.boss : null;
            if (boss == null || boss.id != e.BossId) return;
            var encounter = FindAnyObjectByType<Hearthdelve.Dungeon.Bosses.BossEncounter>();
            Vector2 at = encounter != null ? (Vector2)encounter.transform.position : (Vector2)Current.BossPoint();
            GrantBossReward(boss, at);
            // Its fall fills the delver's Essence: the way home is theirs (4e playtest).
            Character player = Player;
            if (boss.essenceOnDefeat > 0f && player != null && player.TryGetComponent(out Hearthdelve.Dungeon.Essence.EssenceHealth essence) && essence.CurrentHealth > 0f)
                essence.Restore(essence.MaximumHealth * boss.essenceOnDefeat);
        }

        /// <summary>Drops a boss's Gold and larder cache around <paramref name="at"/>.</summary>
        public void GrantBossReward(Hearthdelve.Dungeon.Bosses.BossDefinition boss, Vector2 at)
        {
            if (boss.gold > 0 && m_Settings.goldPickup != null)
            {
                GoldPickup coin = Instantiate(m_Settings.goldPickup, at + new Vector2(0f, -1.2f), Quaternion.identity, Current.transform);
                coin.SetAmount(boss.gold);
            }
            if (HarvestSystem.Instance == null || boss.cache == null) return;
            int index = 0;
            foreach (Hearthdelve.Dungeon.Bosses.CacheEntry entry in boss.cache)
            {
                if (entry?.ingredient == null || entry.count <= 0) continue;
                var stack = new Hearthdelve.Shared.Inventory.IngredientStack(new Hearthdelve.Shared.Ingredients.IngredientItem(entry.ingredient, entry.quality), entry.count, 1f);
                Hearthdelve.Dungeon.Harvest.IngredientPickup pickup = HarvestSystem.Instance.Drop(stack, at, null);
                float angle = (200f + 140f * (boss.cache.Length > 1 ? index / (float)(boss.cache.Length - 1) : 0.5f)) * Mathf.Deg2Rad;
                pickup?.PopTo(at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.8f, 0.4f);
                index++;
            }
        }

        IEnumerator Start()
        {
            // TDE spawns the player during its own Start.
            while (Player == null) yield return null;
            if (m_Settings == null || m_Settings.rooms.Length == 0)
            {
                Debug.LogWarning("[Hearthdelve] The room runner has no run settings.");
                yield break;
            }
            int seed = SeedOverride != 0 ? SeedOverride : m_Seed != 0 ? m_Seed : new System.Random().Next(1, int.MaxValue);
            Graph = RunGenerator.Generate(seed, m_Settings.tuning, m_Settings.Catalog());
            Debug.Log($"[Hearthdelve] Delve seed {seed} (set it on the RoomRunner to replay this run).\n{Graph.Describe()}");
            FloorGraph first = Graph.Floors[0];
            FloorNode start = first.Start;
            // Development: straight to the arena, to practise the boss (never in the day loop or a release build).
            bool inDayLoop = Hearthdelve.Shared.Game.GameFlow.Instance != null && Hearthdelve.Shared.Game.GameFlow.Instance.InGame;
            if (StartInArenaOverride || (m_StartInArena && Debug.isDebugBuild && !inDayLoop))
            {
                FloorGraph last = Graph.Floors[^1];
                FloorNode arena = last.Nodes.FirstOrDefault(n => n.Kind == RoomKind.Arena);
                if (arena != null)
                {
                    first = last;
                    start = arena;
                }
            }
            Load(first, start);
            // The first room is safe ground (the owner's call, 4i-C playtest): the drain begins in the next room.
            if (Player != null && Player.TryGetComponent(out EssenceHealth startEssence)) startEssence.DrainPaused = start.PausesEssenceDrain;
            PublishEntered(0f);
            if (m_Encounter.IsSealed) Seal();
        }

        static Character Player =>
            LevelManager.HasInstance && LevelManager.Instance.Players != null && LevelManager.Instance.Players.Count > 0 ? LevelManager.Instance.Players[0] : null;

        void PublishEntered(float fade) =>
            EventBus<RoomEntered>.Publish(new RoomEntered(m_Entered, Node.RoomId, m_Encounter.IsSealed, fade, Floor.Floor, Graph.Seed));

        void Update()
        {
            if (m_Encounter == null || m_Encounter.IsCleared || Current == null) return;
            if (m_Encounter.Update(Current.LivingEnemies())) OnCleared();
        }

        void OnCleared()
        {
            GrantReward(Node.Reward);
            if (Node.Kind == RoomKind.Combat) PlaceQuestObjects();
            if (Node.Next.Any(id => Floor.Node(id).Kind == RoomKind.Arena)) LightCampfire(m_Settings != null ? m_Settings.tuning.campfire : null, CampfirePoint());
            OpenUsedExits(instant: false);
            Current.SetRevealed(true);
            m_ClearFeedback?.PlayFeedbacks(Player != null ? Player.transform.position : Vector3.zero);
            EventBus<RoomCleared>.Publish(new RoomCleared(m_Entered, Node.RoomId));
        }

        void Seal()
        {
            if (Current == null) return;
            Current.SetExitsOpen(false, instant: false);
            m_SealFeedback?.PlayFeedbacks(Player != null ? Player.transform.position : Vector3.zero);
        }

        /// <summary>Opens the exits the graph uses; any other exit of the room stays shut.</summary>
        void OpenUsedExits(bool instant)
        {
            foreach (RoomExit exit in Current.Exits)
                if (exit != null) exit.SetOpen(exit.Index < Node.Next.Count, instant);
        }

        /// <summary>What each exit promises, as a sign over its doorway: the way out, deeper, the arena, or the kind of reward a fight gives.</summary>
        Sprite MarkerFor(FloorNode target) => target.Kind switch
        {
            RoomKind.Extraction => m_MarkerOut,
            RoomKind.Descent => m_MarkerDeeper,
            RoomKind.Arena => m_MarkerArena,
            RoomKind.Combat => target.Reward.Kind switch
            {
                RewardKind.Gold => m_MarkerGold,
                RewardKind.Ingredient => m_MarkerIngredient,
                RewardKind.Power => m_MarkerPower,
                RewardKind.Curio => m_MarkerCurio,
                _ => null,
            },
            _ => null,
        };

        /// <summary>The room's reward, on the floor near its middle once it's clear.</summary>
        public void GrantReward(RoomReward reward)
        {
            if (reward.Kind == RewardKind.None || reward.Amount <= 0 || Current == null) return;
            Vector2 at = RewardPoint();
            switch (reward.Kind)
            {
                case RewardKind.Gold when m_Settings.goldPickup != null:
                    GoldPickup coin = Instantiate(m_Settings.goldPickup, at, Quaternion.identity, Current.transform);
                    coin.SetAmount(reward.Amount);
                    break;
                case RewardKind.Power when m_Settings.powerPickup != null:
                    // The offer is drawn when it's touched, from a seed fixed by the run and the room.
                    Hearthdelve.Dungeon.Powers.PowerPickup spark = Instantiate(m_Settings.powerPickup, at, Quaternion.identity, Current.transform);
                    spark.Setup(m_Settings.tuning.powers, unchecked(Graph.Seed * 31 + Floor.Floor * 7919 + Node.Id * 104729));
                    break;
                case RewardKind.Curio:
                    // Which piece depends on what's owned, so it's drawn now, from a seed fixed by the run and the room.
                    GrantCurio(CurioRules.RoomOrigin, at, new Hearthdelve.Core.Random.SeededRandom(unchecked(Graph.Seed * 37 + Floor.Floor * 7907 + Node.Id * 104723)));
                    break;
                case RewardKind.Ingredient when HarvestSystem.Instance != null:
                    Hearthdelve.Shared.Ingredients.IngredientDefinition ingredient = m_Settings.RewardIngredient(reward.ItemId);
                    if (ingredient == null) break;
                    HarvestSystem.Instance.Drop(new Hearthdelve.Shared.Inventory.IngredientStack(
                        new Hearthdelve.Shared.Ingredients.IngredientItem(ingredient, reward.Quality), reward.Amount, 1f), at, null);
                    break;
            }
        }

        // ---------- Quest objects (4g Checkpoint B) ----------

        int m_FightsFloor = -1, m_FightsCleared;

        /// <summary>
        /// A quest object the game wants turns up in the fight cleared the set number of rooms into its floor (reliable, whichever
        /// way the keeper goes; never a random drop), beside the room's reward, until it's brought home.
        /// </summary>
        void PlaceQuestObjects()
        {
            if (m_FightsFloor != Floor.Floor)
            {
                m_FightsFloor = Floor.Floor;
                m_FightsCleared = 0;
            }
            m_FightsCleared++;
            GameFlow flow = GameFlow.Instance;
            DelveRunController run = DelveRunController.Active;
            if (flow == null || !flow.InGame || flow.Database == null || run == null || m_Settings == null || m_Settings.questObjectPickup == null) return;
            foreach (QuestObjectDefinition definition in flow.Database.questObjects)
            {
                if (!QuestObjectRules.PlaceHere(definition, flow.State.QuestObjects, Floor.Floor, m_FightsCleared, run.Loot.Carries(definition.id))) continue;
                Vector2 at = RewardPoint() + new Vector2(1.5f, 0f);
                QuestObjectPickup pickup = Instantiate(m_Settings.questObjectPickup, at, Quaternion.identity, Current.transform);
                pickup.Set(definition);
            }
        }

        // ---------- Furnishing discoveries (4f Checkpoint C) ----------

        readonly System.Collections.Generic.List<string> m_CuriosOffered = new();
        Hearthdelve.Core.Random.SeededRandom m_CurioRandom;
        int m_CurioSeed;

        /// <summary>The run's curio rolls: seeded by the run, so a seed replays its drops.</summary>
        Hearthdelve.Core.Random.SeededRandom CurioRandom
        {
            get
            {
                int seed = Graph != null ? Graph.Seed : 0;
                if (m_CurioRandom == null || m_CurioSeed != seed)
                {
                    m_CurioSeed = seed;
                    m_CurioRandom = new Hearthdelve.Core.Random.SeededRandom(unchecked(seed * 131 + 977));
                    m_CuriosOffered.Clear();
                }
                return m_CurioRandom;
            }
        }

        /// <summary>A rare enemy drop: a kill by an enemy the pool knows, under the delve's cap.</summary>
        void OnEnemyKilled(EnemyKilled e)
        {
            CurioPool pool = m_Settings != null ? m_Settings.tuning.curios : null;
            DelveRunController run = DelveRunController.Active;
            string enemy = e.Definition != null ? e.Definition.id : null;
            if (pool == null || run == null || run.IsEnding || Current == null || string.IsNullOrEmpty(enemy)) return;
            if (!pool.entries.Any(entry => entry?.piece != null && entry.From(enemy))) return;
            if (!CurioRules.EnemyDrops(pool, run.Loot.CuriosDropped, CurioRandom.Value())) return;
            run.Loot.NoteEnemyDrop();
            GrantCurio(enemy, e.Position + new Vector2(0f, -0.9f), CurioRandom);
        }

        /// <summary>
        /// Puts a curio on the floor at <paramref name="at"/>: a piece from the pool for <paramref name="origin"/> (owned
        /// uniques and pieces already found on this delve left out, unowned pieces favoured), or run gold when nothing is
        /// left to give (D8).
        /// </summary>
        public void GrantCurio(string origin, Vector2 at, Hearthdelve.Core.Random.IRandom random)
        {
            CurioPool pool = m_Settings != null ? m_Settings.tuning.curios : null;
            if (pool == null || Current == null) return;
            _ = CurioRandom; // starts the run's record of what's been offered
            var flow = Hearthdelve.Shared.Game.GameFlow.Instance;
            System.Func<string, int> owned = id => flow != null && flow.State != null ? flow.State.Furniture.OwnedCount(id) : 0;
            Hearthdelve.Shared.Customization.FurnitureDefinition piece = CurioRules.Roll(pool.entries, origin, owned, m_CuriosOffered, random, pool.firstCopyWeight);
            if (piece == null)
            {
                int gold = CurioRules.FallbackGold(pool, random);
                if (gold > 0 && m_Settings.goldPickup != null)
                    Instantiate(m_Settings.goldPickup, at, Quaternion.identity, Current.transform).SetAmount(gold);
                return;
            }
            m_CuriosOffered.Add(piece.id);
            if (m_Settings.curioPickup == null) return;
            CurioPickup pickup = Instantiate(m_Settings.curioPickup, at, Quaternion.identity, Current.transform);
            pickup.SetPiece(piece.id);
        }

        /// <summary>Lights a campfire (4e playtest): before the boss's arena once that room is clear, and by each floor's hole down.</summary>
        void LightCampfire(CampfireSettings settings, Vector2 at)
        {
            if (settings == null || !settings.enabled || m_Settings == null || m_Settings.campfire == null || Current == null) return;
            Campfire fire = Instantiate(m_Settings.campfire, at, Quaternion.identity, Current.transform);
            fire.Setup(settings);
        }

        /// <summary>A ground spawn near the room's middle but apart from its reward.</summary>
        Vector2 CampfirePoint()
        {
            Vector2 reward = RewardPoint();
            Vector2 middle = (Vector2)Current.transform.position + (Vector2)Current.Size / 2f;
            Transform best = null;
            foreach (Transform point in Current.GroundSpawns)
            {
                if (point == null || Vector2.Distance(point.position, reward) < 3f) continue;
                if (best == null || Vector2.Distance(point.position, middle) < Vector2.Distance(best.position, middle)) best = point;
            }
            return best != null ? (Vector2)best.position : reward + new Vector2(3f, 0f);
        }

        /// <summary>The ground spawn point nearest the room's middle (spawn points are always reachable floor).</summary>
        Vector2 RewardPoint()
        {
            Vector2 middle = (Vector2)Current.transform.position + (Vector2)Current.Size / 2f;
            Transform best = null;
            foreach (Transform point in Current.GroundSpawns)
                if (point != null && (best == null || Vector2.Distance(point.position, middle) < Vector2.Distance(best.position, middle)))
                    best = point;
            return best != null ? best.position : Current.Arrival.position + Vector3.up * 3f;
        }

        /// <summary>Unloads the current room and loads <paramref name="node"/> with the player at its arrival.</summary>
        void Load(FloorGraph floor, FloorNode node)
        {
            if (Current != null)
            {
                foreach (RoomExit exit in Current.Exits)
                    if (exit != null) exit.Entered -= OnExitEntered;
                if (Current.Descent != null) Current.Descent.Entered -= OnDescent;
                // Off at once, so the new room's colliders are the only ones the grid sees this frame.
                Current.gameObject.SetActive(false);
                Destroy(Current.gameObject);
                ClearLeftovers();
            }

            Floor = floor;
            Node = node;
            m_Entered++;
            RoomDefinition definition = m_Settings.Room(node.RoomId);
            Current = Instantiate(definition.prefab, Vector3.zero, Quaternion.identity, m_RoomRoot);
            Current.name = definition.prefab.name;
            foreach (EncounterSpawn spawn in node.Encounter) Current.Spawn(spawn, m_Settings.Prefab(spawn.Kind));
            Current.SetRevealed(false);
            foreach (RoomExit exit in Current.Exits)
            {
                if (exit == null) continue;
                exit.Entered += OnExitEntered;
                bool used = exit.Index < node.Next.Count;
                exit.SetUnused(!used);
                exit.SetMarker(used ? MarkerFor(floor.Node(node.Next[exit.Index])) : null);
            }
            if (Current.Descent != null) Current.Descent.Entered += OnDescent;

            if (m_Nav != null) m_Nav.Configure(Current.TileBounds, LayerMask.GetMask(Layers.Obstacles));
            Character player = Player;
            if (player != null) PlaceAt(player, Current.Arrival.position);
            BindCamera(player);

            m_Encounter = new RoomEncounter(Current.LivingEnemies());
            // Arrive with the used gates up; they drop as the fight starts (after the fade).
            OpenUsedExits(instant: true);
            if (m_Encounter.IsCleared) Current.SetRevealed(true);
            // The hole down: a campfire by it for the delver going deeper (4e playtest). Beside the arrival, clear of the hole.
            if (Node.Kind == RoomKind.Descent && m_Settings != null)
                LightCampfire(m_Settings.tuning.floorCampfire, (Vector2)Current.Arrival.position + new Vector2(3.5f, 3f));
        }

        bool CanLeave => !m_Transitioning && (DelveRunController.Active == null || !DelveRunController.Active.IsEnding);

        void OnExitEntered(RoomExit exit)
        {
            if (!CanLeave || exit.Index >= Node.Next.Count) return;
            StartCoroutine(Transition(Floor, Floor.Node(Node.Next[exit.Index]), fall: false));
        }

        void OnDescent(FloorDescent hole)
        {
            int next = Floor.Floor;  // 1-based floor number = index of the next floor
            if (!CanLeave || next >= Graph.Floors.Count) return;
            StartCoroutine(Transition(Graph.Floors[next], Graph.Floors[next].Start, fall: true));
        }

        IEnumerator Transition(FloorGraph floor, FloorNode node, bool fall)
        {
            m_Transitioning = true;
            Character player = Player;
            InputMaps.ActivateUIOnly();
            player?.GetComponent<CharacterMovement>()?.SetMovement(Vector2.zero);
            var essence = player != null ? player.GetComponent<EssenceHealth>() : null;
            // Walking between rooms costs no Essence.
            if (essence != null) essence.DrainPaused = true;

            SpriteRenderer[] sprites = null;
            Vector3[] rest = null;
            if (fall && player != null)
            {
                // Presentation only: the player sinks into the hole and fades; the character stays put.
                m_FallFeedback?.PlayFeedbacks(player.transform.position);
                sprites = (player.CharacterModel != null ? player.CharacterModel : player.gameObject).GetComponentsInChildren<SpriteRenderer>();
                rest = new Vector3[sprites.Length];
                for (int i = 0; i < sprites.Length; i++) rest[i] = sprites[i].transform.localPosition;
                float started = Time.time;
                while (Time.time - started < m_FallTime)
                {
                    float t = (Time.time - started) / m_FallTime;
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        sprites[i].transform.localPosition = rest[i] + Vector3.down * (t * t * 0.8f);
                        Color color = sprites[i].color;
                        color.a = 1f - t;
                        sprites[i].color = color;
                    }
                    yield return null;
                }
            }

            EventBus<RoomTransitionStarted>.Publish(new RoomTransitionStarted(m_FadeOut));
            yield return new WaitForSecondsRealtime(m_FadeOut);
            if (sprites != null)
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == null) continue;
                    sprites[i].transform.localPosition = rest[i];
                    Color color = sprites[i].color;
                    color.a = 1f;
                    sprites[i].color = color;
                }
            Load(floor, node);
            PublishEntered(m_FadeIn);
            yield return new WaitForSecondsRealtime(m_FadeIn);

            // Walking on: the drain resumes, except at the rope up and the hole down (safe ground).
            if (essence != null) essence.DrainPaused = node.PausesEssenceDrain;
            InputMaps.Activate(InputMaps.Dungeon);
            m_Transitioning = false;
            if (m_Encounter.IsSealed) Seal();
        }

        static void PlaceAt(Character player, Vector2 position)
        {
            player.transform.position = position;
            if (player.TryGetComponent(out Rigidbody2D body))
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            player.GetComponent<CharacterMovement>()?.SetMovement(Vector2.zero);
            Physics2D.SyncTransforms();
        }

        /// <summary>The camera's bounds become the room, and it jumps to the player instead of sliding across.</summary>
        Transform m_HeldFollow;

        /// <summary>Points the camera at <paramref name="focus"/> (a boss's entrance) until <see cref="ReleaseCamera"/>.</summary>
        public void FocusCamera(Transform focus)
        {
            if (m_Camera == null || focus == null) return;
            if (m_HeldFollow == null) m_HeldFollow = m_Camera.Follow;
            m_Camera.Follow = focus;
        }

        public void ReleaseCamera()
        {
            if (m_Camera == null || m_HeldFollow == null) return;
            m_Camera.Follow = m_HeldFollow;
            m_HeldFollow = null;
        }

        /// <summary>What the camera follows now (tests).</summary>
        public Transform CameraFollow => m_Camera != null ? m_Camera.Follow : null;

        void BindCamera(Character player)
        {
            if (m_CameraBounds != null)
            {
                RectInt tiles = Current.TileBounds;
                m_CameraBounds.SetBounds(new Rect(tiles.x, tiles.y, tiles.width, tiles.height));
            }
            if (m_Camera == null || player == null) return;
            Transform target = m_Camera.Follow != null ? m_Camera.Follow : player.transform;
            Vector3 at = player.transform.position;
            m_Camera.ForceCameraPosition(new Vector3(at.x, at.y, m_Camera.transform.position.z), Quaternion.identity);
            if (target != player.transform) target.position = new Vector3(at.x, at.y, target.position.z);
        }

        /// <summary>Parts, Gold and power sparks left lying, and webs in flight, stay behind with the room.</summary>
        static void ClearLeftovers()
        {
            foreach (IngredientPickup pickup in FindObjectsByType<IngredientPickup>(FindObjectsSortMode.None)) Destroy(pickup.gameObject);
            foreach (WebProjectile web in FindObjectsByType<WebProjectile>(FindObjectsSortMode.None)) Destroy(web.gameObject);
            foreach (GoldPickup coin in FindObjectsByType<GoldPickup>(FindObjectsSortMode.None)) Destroy(coin.gameObject);
            foreach (Hearthdelve.Dungeon.Powers.PowerPickup spark in FindObjectsByType<Hearthdelve.Dungeon.Powers.PowerPickup>(FindObjectsSortMode.None)) Destroy(spark.gameObject);
        }
    }
}
