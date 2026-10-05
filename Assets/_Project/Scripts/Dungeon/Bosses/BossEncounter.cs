using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    public enum BossEncounterState
    {
        /// <summary>Spawned with the room, waiting for the player to arrive and the gates to seal.</summary>
        Waiting,
        /// <summary>The entrance: the boss is shown and named, and doesn't act yet.</summary>
        Entrance,
        Fighting,
        Defeated,
        /// <summary>Ended without a victory (the player died, or the room went away).</summary>
        Ended,
    }

    /// <summary>
    /// A boss's encounter (4e): waits until the player is in the sealed arena, plays a short entrance, then wakes the
    /// boss's brain. While it fights, the encounter's drain rule applies to the player's Essence
    /// (<see cref="BossDefinition.drainMultiplierWhileActive"/>, paused for the Cellars) and the HUD's boss bar follows
    /// its health. Victory, the player's death or the room unloading end it and restore the drain.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class BossEncounter : MonoBehaviour
    {
        [SerializeField] BossDefinition m_Boss;
        [SerializeField, Tooltip("The entrance: a roar, a shake and a heavy rumble.")]
        MMF_Player m_EntranceFeedback;

        Health m_Health;
        AIBrain m_Brain;
        CharacterSpriteAnimator m_Animator;
        EssenceHealth m_PlayerEssence;
        float m_EntranceLeft;
        float m_LastHealth;

        public BossDefinition Boss => m_Boss;
        public BossEncounterState State { get; private set; } = BossEncounterState.Waiting;

        public void Configure(BossDefinition boss, MMF_Player entrance)
        {
            m_Boss = boss;
            m_EntranceFeedback = entrance;
        }

        void Awake()
        {
            m_Health = GetComponent<Health>();
            m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();
        }

        void Start()
        {
            Character character = GetComponent<Character>();
            m_Brain = character != null ? character.CharacterBrain : GetComponent<AIBrain>();
            if (m_Brain != null) m_Brain.BrainActive = false;
            // Found at its meal: it eats until the entrance is over.
            m_Animator?.Hold(CharacterAnim.Eat);
        }

        void OnEnable() => EventBus<PlayerDefeated>.Subscribe(OnPlayerDefeated);

        void OnDisable()
        {
            EventBus<PlayerDefeated>.Unsubscribe(OnPlayerDefeated);
            End(defeated: false);
        }

        void OnPlayerDefeated(PlayerDefeated _) => End(defeated: false);

        void Update()
        {
            switch (State)
            {
                case BossEncounterState.Waiting:
                    // The arena seals once the player is in and the fade is over.
                    RoomRunner runner = RoomRunner.Active;
                    if (runner == null || runner.IsTransitioning || runner.Encounter == null || !runner.Encounter.IsSealed) return;
                    BeginEntrance();
                    break;
                case BossEncounterState.Entrance:
                    m_EntranceLeft -= Time.deltaTime;
                    if (m_EntranceLeft <= 0f) BeginFight();
                    break;
                case BossEncounterState.Fighting:
                    if (m_Health.CurrentHealth != m_LastHealth)
                    {
                        m_LastHealth = m_Health.CurrentHealth;
                        EventBus<BossHealthChanged>.Publish(new BossHealthChanged(Mathf.Max(0f, m_LastHealth), m_Health.MaximumHealth));
                    }
                    if (m_Health.CurrentHealth <= 0f) End(defeated: true);
                    break;
            }
        }

        void BeginEntrance()
        {
            State = BossEncounterState.Entrance;
            m_EntranceLeft = m_Boss != null ? m_Boss.entranceSeconds : 0f;
            m_LastHealth = m_Health.CurrentHealth;
            // The fight's rules start with the entrance: the gates have sealed and the boss is here.
            Character player = LevelManager.HasInstance && LevelManager.Instance.Players.Count > 0 ? LevelManager.Instance.Players[0] : null;
            m_PlayerEssence = player != null ? player.GetComponent<EssenceHealth>() : null;
            m_PlayerEssence?.SetEncounterDrain(m_Boss != null ? m_Boss.drainMultiplierWhileActive : 1f);
            m_EntranceFeedback?.PlayFeedbacks(transform.position);
            EventBus<BossEncounterStarted>.Publish(new BossEncounterStarted(m_Boss != null ? m_Boss.id : name, m_LastHealth, m_Health.MaximumHealth));
        }

        void BeginFight()
        {
            State = BossEncounterState.Fighting;
            m_Animator?.Release();
            if (m_Brain != null) m_Brain.BrainActive = true;
        }

        void End(bool defeated)
        {
            if (State is BossEncounterState.Defeated or BossEncounterState.Ended) return;
            bool started = State != BossEncounterState.Waiting;
            State = defeated ? BossEncounterState.Defeated : BossEncounterState.Ended;
            m_PlayerEssence?.SetEncounterDrain(1f);
            if (started) EventBus<BossEncounterEnded>.Publish(new BossEncounterEnded(m_Boss != null ? m_Boss.id : name, defeated));
        }
    }
}
