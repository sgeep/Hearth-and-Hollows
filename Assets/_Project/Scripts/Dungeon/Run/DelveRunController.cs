using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Services;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Dungeon.Run
{
    /// <summary>
    /// Owns one delve: the satchel (sized by upgrades), its freshness loss, and how the delve
    /// ends. At the exit the whole satchel goes home; on defeat the UI picks the Lockbox stack
    /// and only that goes home. Either way the haul is handed to <see cref="GameFlow"/>, which
    /// moves the day on to Evening. Played on its own (no GameFlow), the level restarts instead.
    /// </summary>
    public sealed class DelveRunController : MonoBehaviour
    {
        [SerializeField] DelveConfig m_Config;

        bool m_Ending;

        public static DelveRunController Active { get; private set; }
        public Satchel Satchel { get; private set; }
        /// <summary>Delve Marks collected this run (none drop yet).</summary>
        public int RunCurrency { get; set; }
        public bool IsEnding => m_Ending;

        FreshnessSettings Freshness => m_Config != null && m_Config.freshness != null ? m_Config.freshness.freshness : FreshnessSettings.Default;

        public void Configure(DelveConfig config) => m_Config = config;

        void Awake()
        {
            Active = this;
            var settings = m_Config != null ? m_Config.satchel : SatchelSettings.Default;
            var loadout = GameFlow.Instance != null ? GameFlow.Instance.Loadout : DelveLoadout.None;
            Satchel = new Satchel(settings.capacity + Mathf.Max(0, loadout.ExtraSatchelSlots), settings.maxStack);
        }

        void OnEnable()
        {
            EventBus<PlayerDefeated>.Subscribe(OnPlayerDefeated);
            EventBus<DebugSkipPhaseRequested>.Subscribe(OnDebugSkip);
        }

        void OnDisable()
        {
            EventBus<PlayerDefeated>.Unsubscribe(OnPlayerDefeated);
            EventBus<DebugSkipPhaseRequested>.Unsubscribe(OnDebugSkip);
        }

        void Start()
        {
            InputMaps.Activate(InputMaps.Dungeon);
            EventBus<SatchelBound>.Publish(new SatchelBound(Satchel));
        }

        void Update()
        {
            // Scaled time: menus and hit-stop don't age the haul.
            if (!m_Ending) Satchel.Decay(Freshness, Freshness.dungeonLossPerMinute / 60f * Time.deltaTime);
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        void OnDebugSkip(DebugSkipPhaseRequested _) => Extract();

        /// <summary>Leave through the exit with everything in the satchel.</summary>
        public void Extract()
        {
            if (m_Ending) return;
            m_Ending = true;
            var report = DelveReport.Extraction(Satchel);
            Satchel.Clear();
            Leave(report);
        }

        void OnPlayerDefeated(PlayerDefeated evt)
        {
            if (m_Ending) return;
            m_Ending = true;

            GamePause.PushMenuPause();
            InputMaps.ActivateUIOnly();

            // The death screen also shows with an empty satchel, so the player sees why the delve ended.
            if (EventBus<DeathScreenRequested>.HandlerCount == 0)
            {
                FinishDelve(DeathPenalty.KeepNothing);
                return;
            }
            EventBus<DeathScreenRequested>.Publish(new DeathScreenRequested(Satchel, evt.Reason, FinishDelve));
        }

        void FinishDelve(int keepSlot)
        {
            var result = DeathPenalty.Resolve(Satchel, keepSlot, RunCurrency);
            RunCurrency = 0;
            EventBus<DelveEnded>.Publish(new DelveEnded(result));
            GamePause.PopMenuPause();
            Leave(DelveReport.Death(result));
        }

        static void Leave(DelveReport report)
        {
            var flow = GameFlow.Instance;
            if (flow != null && flow.InGame) flow.CompleteDelve(report);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // standalone greybox: restart
        }
    }
}
