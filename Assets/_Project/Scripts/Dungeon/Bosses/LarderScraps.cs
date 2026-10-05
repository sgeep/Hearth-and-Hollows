using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>
    /// The troll's stolen larder (4e step 2): its slams shake parts loose (at most every
    /// <see cref="FeedingSettings.dropEverySeconds"/>, and not while the floor already holds
    /// <see cref="FeedingSettings.maxOnFloor"/>); in its frenzy each charge spills one where it started. They're ordinary
    /// parts: the troll goes for them (<see cref="ScrapEater"/>), and the player can take them first.
    /// </summary>
    public sealed class LarderScraps : MonoBehaviour
    {
        [SerializeField] BossDefinition m_Boss;
        [SerializeField, Min(0.5f), Tooltip("How far parts pop from the slam, in tiles.")]
        float m_Scatter = 2.5f;

        EnemyAttack m_Slam;
        EnemyAttack m_Charge;
        BossFrenzy m_Frenzy;
        float m_LastDrop = float.NegativeInfinity;

        /// <summary>Parts shaken loose so far (tests).</summary>
        public int Dropped { get; private set; }

        public void Configure(BossDefinition boss) => m_Boss = boss;

        void Awake()
        {
            EnemyAttack[] attacks = GetComponents<EnemyAttack>();
            m_Slam = attacks.Length > 0 ? attacks[0] : null;
            m_Charge = attacks.Length > 1 ? attacks[1] : null;
            m_Frenzy = GetComponent<BossFrenzy>();
            if (m_Slam != null) m_Slam.PhaseChanged += OnSlam;
            if (m_Charge != null) m_Charge.PhaseChanged += OnCharge;
        }

        void Start()
        {
            // The first slam of the fight doesn't wait the full interval.
            if (m_Boss != null) m_LastDrop = Time.time - m_Boss.feeding.dropEverySeconds * 0.5f;
        }

        void OnDestroy()
        {
            if (m_Slam != null) m_Slam.PhaseChanged -= OnSlam;
            if (m_Charge != null) m_Charge.PhaseChanged -= OnCharge;
        }

        static int OnFloor() => FindObjectsByType<IngredientPickup>(FindObjectsSortMode.None).Length;

        void OnSlam(EnemyAttackPhase phase)
        {
            if (phase != EnemyAttackPhase.Active || m_Boss == null || !m_Boss.feeding.enabled) return;
            FeedingSettings s = m_Boss.feeding;
            if (!FeedingRules.MayDrop(Time.time - m_LastDrop, s.dropEverySeconds, OnFloor(), s.maxOnFloor)) return;
            m_LastDrop = Time.time;
            Vector2 impact = (Vector2)transform.position + m_Slam.Direction * m_Slam.Settings.hitboxOffset.x;
            for (int i = 0; i < s.partsPerDrop; i++) Drop(impact);
        }

        void OnCharge(EnemyAttackPhase phase)
        {
            if (phase != EnemyAttackPhase.Active || m_Boss == null || !m_Boss.feeding.enabled) return;
            if (m_Frenzy == null || !m_Frenzy.IsFrenzied || !m_Boss.frenzy.chargesSpillScraps) return;
            if (OnFloor() >= m_Boss.feeding.maxOnFloor) return;
            Drop(transform.position);
        }

        /// <summary>Pops one part out from <paramref name="from"/> onto open floor.</summary>
        public IngredientPickup Drop(Vector2 from)
        {
            IngredientDefinition[] pool = m_Boss != null ? m_Boss.feeding.scraps : null;
            if (pool == null || pool.Length == 0 || HarvestSystem.Instance == null) return null;
            IngredientDefinition part = pool[Random.Range(0, pool.Length)];
            if (part == null) return null;
            IngredientPickup pickup = HarvestSystem.Instance.Drop(new IngredientStack(new IngredientItem(part, Quality.Standard), 1, 1f), from, null);
            if (pickup == null) return null;
            pickup.PopTo(Landing(from), 0.35f);
            Dropped++;
            return pickup;
        }

        Vector2 Landing(Vector2 from)
        {
            int obstacles = LayerMask.GetMask(Hearthdelve.Core.Layers.Obstacles);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 spot = from + Random.insideUnitCircle.normalized * Random.Range(m_Scatter * 0.5f, m_Scatter);
                if (Physics2D.OverlapCircle(spot, 0.35f, obstacles) == null && Physics2D.Linecast(from, spot, obstacles).collider == null) return spot;
            }
            return from;
        }
    }
}
