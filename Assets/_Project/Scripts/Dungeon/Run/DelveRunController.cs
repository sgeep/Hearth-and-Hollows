using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Services;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Dungeon.Run
{
    /// <summary>
    /// Owns the state of one delve (the satchel) and its ending: on defeat it freezes play,
    /// asks the UI for the Lockbox pick, applies <see cref="DeathPenalty"/>, and restarts.
    /// (Phase 3 will send the player to the tavern instead of restarting the level.)
    /// </summary>
    public sealed class DelveRunController : MonoBehaviour
    {
        [SerializeField] DelveConfig m_Config;

        bool m_Ending;

        public static DelveRunController Active { get; private set; }
        public Satchel Satchel { get; private set; }
        /// <summary>Delve Marks collected this run (none drop yet in Phase 1).</summary>
        public int RunCurrency { get; set; }

        public void Configure(DelveConfig config) => m_Config = config;

        void Awake()
        {
            Active = this;
            var settings = m_Config != null ? m_Config.satchel : SatchelSettings.Default;
            Satchel = new Satchel(settings.capacity, settings.maxStack);
        }

        void OnEnable() => EventBus<PlayerDefeated>.Subscribe(OnPlayerDefeated);
        void OnDisable() => EventBus<PlayerDefeated>.Unsubscribe(OnPlayerDefeated);

        void Start()
        {
            InputMaps.Activate(InputMaps.Dungeon);
            EventBus<SatchelBound>.Publish(new SatchelBound(Satchel));
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
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
            if (result.Kept.HasValue) PersistentStash.Deposit(result.Kept.Value);
            RunCurrency = 0;
            EventBus<DelveEnded>.Publish(new DelveEnded(result));

            GamePause.PopMenuPause();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
