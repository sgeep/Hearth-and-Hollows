using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Dungeon.Run
{
    /// <summary>
    /// How a delve ends (GDD §4.4). Extracting takes the whole satchel home. When Essence runs out the
    /// death animation plays, then the death screen asks which slot to keep (the Lockbox) and
    /// <see cref="DeathPenalty"/> applies: that whole stack goes home, the rest of the haul and any
    /// unspent run currency is lost. Either way gameplay pauses and the result screen shows what came
    /// home; continuing hands the report to <see cref="GameFlow"/> (the day loop, 4c) or, with no day
    /// loop running, restarts the floor.
    /// </summary>
    public sealed class DelveRunController : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Real seconds the death animation plays before the death screen opens.")]
        float m_DeathScreenDelay = 1.2f;

        bool m_Paused;

        /// <summary>The delve in this scene, if any.</summary>
        public static DelveRunController Active { get; private set; }
        public bool IsEnding { get; private set; }
        /// <summary>Delve Marks collected this run (none drop yet; 4e).</summary>
        public int RunCurrency { get; set; }
        /// <summary>What the run has found outside the satchel: its unbanked Gold (4d step 3).</summary>
        public RunLoot Loot { get; } = new();

        void PublishGold(int gold) => EventBus<RunGoldChanged>.Publish(new RunGoldChanged(gold));
        /// <summary>The report of the delve once it has ended.</summary>
        public DelveReport Report { get; private set; }

        void Awake() => Active = this;

        void OnEnable()
        {
            Loot.GoldChanged += PublishGold;
            EventBus<PlayerDefeated>.Subscribe(OnPlayerDefeated);
            EventBus<DebugSkipPhaseRequested>.Subscribe(OnDebugSkip);
        }

        void OnDisable()
        {
            Loot.GoldChanged -= PublishGold;
            EventBus<PlayerDefeated>.Unsubscribe(OnPlayerDefeated);
            EventBus<DebugSkipPhaseRequested>.Unsubscribe(OnDebugSkip);
        }

        // F8 in the day loop: leave as if through the exit, result screen and all.
        void OnDebugSkip(DebugSkipPhaseRequested _) => Extract();

        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (m_Paused) MenuPause.Pop();
        }

        static Satchel PlayerSatchel()
        {
            if (!LevelManager.HasInstance || LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0) return null;
            var carrier = LevelManager.Instance.Players[0].GetComponent<SatchelCarrier>();
            return carrier != null ? carrier.Satchel : null;
        }

        /// <summary>Leave through the exit with everything in the satchel. False if the delve is already ending.</summary>
        public bool Extract()
        {
            if (IsEnding) return false;
            IsEnding = true;
            Satchel satchel = PlayerSatchel();
            DelveReport report = satchel != null ? DelveReport.Extraction(satchel, Loot.Gold) : DelveReport.Empty;
            satchel?.Clear();
            End(report);
            return true;
        }

        void OnPlayerDefeated(PlayerDefeated defeated)
        {
            if (IsEnding) return;
            IsEnding = true;
            StartCoroutine(Defeat(defeated.Reason));
        }

        IEnumerator Defeat(DefeatReason reason)
        {
            yield return new WaitForSecondsRealtime(m_DeathScreenDelay);
            Pause();
            Satchel satchel = PlayerSatchel();
            // The death screen shows even with an empty satchel, so the player sees why the delve ended.
            if (satchel == null || EventBus<DeathScreenRequested>.HandlerCount == 0) FinishDefeat(DeathPenalty.KeepNothing);
            else EventBus<DeathScreenRequested>.Publish(new DeathScreenRequested(satchel, reason, FinishDefeat));
        }

        void FinishDefeat(int keepSlot)
        {
            Satchel satchel = PlayerSatchel() ?? new Satchel(1, 1);
            DeathPenaltyResult result = DeathPenalty.Resolve(satchel, keepSlot, RunCurrency);
            RunCurrency = 0;
            EventBus<DelveEnded>.Publish(new DelveEnded(result));
            End(DelveReport.Death(result, Loot.Gold));
        }

        void End(DelveReport report)
        {
            Report = report;
            Pause();
            bool backToTavern = GameFlow.Instance != null && GameFlow.Instance.InGame;
            if (EventBus<DelveResultRequested>.HandlerCount > 0) EventBus<DelveResultRequested>.Publish(new DelveResultRequested(report, backToTavern, Leave));
            else Leave();
        }

        void Pause()
        {
            if (m_Paused) return;
            m_Paused = true;
            MenuPause.Push();
            InputMaps.ActivateUIOnly();
        }

        void Leave()
        {
            if (m_Paused)
            {
                m_Paused = false;
                MenuPause.Pop();
            }
            InputMaps.Activate(InputMaps.Dungeon);
            GameFlow flow = GameFlow.Instance;
            if (flow != null && flow.InGame) flow.CompleteDelve(Report);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
