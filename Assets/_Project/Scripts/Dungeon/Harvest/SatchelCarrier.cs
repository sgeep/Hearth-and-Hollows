using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// The player's satchel for this delve (GDD §4.4). Lives on the player character. Parts the player
    /// touches go in as far as they fit, and everything carried loses freshness over time. When a part
    /// doesn't fit, a hint shows; pressing Interact opens the swap prompt (gameplay paused, UI input
    /// only) to drop a slot for it. The dropped stack falls at the player's feet, and isn't picked up
    /// again until the player has stepped off it, so they can change their mind.
    /// </summary>
    public sealed class SatchelCarrier : MonoBehaviour
    {
        [SerializeField] DelveConfig m_Config;
        [SerializeField, Tooltip("When a part first won't fit: a buzz and the Buzz.Failure haptic.")]
        MMF_Player m_FullFeedback;

        IngredientPickup m_Blocked;
        bool m_HintVisible;

        public Satchel Satchel { get; private set; }
        /// <summary>True while the swap prompt is open.</summary>
        public bool IsPrompting { get; private set; }
        /// <summary>The part on the ground that didn't fit, if the player is standing on one.</summary>
        public IngredientPickup Blocked => m_Blocked;

        public void Configure(DelveConfig config) => m_Config = config;

        public void ConfigureFeedback(MMF_Player satchelFull) => m_FullFeedback = satchelFull;

        FreshnessSettings Freshness => m_Config != null && m_Config.freshness != null ? m_Config.freshness.freshness : FreshnessSettings.Default;

        void Awake()
        {
            SatchelSettings settings = m_Config != null ? m_Config.satchel : SatchelSettings.Default;
            DelveLoadout loadout = GameFlow.Instance != null ? GameFlow.Instance.Loadout : DelveLoadout.None;
            Satchel = new Satchel(settings.capacity + loadout.ExtraSatchelSlots, settings.maxStack);
        }

        void Start() => EventBus<SatchelBound>.Publish(new SatchelBound(Satchel));

        void OnDisable()
        {
            SetHint(false);
            if (IsPrompting) Close();
        }

        void Update()
        {
            FreshnessSettings freshness = Freshness;
            Satchel.Decay(freshness, freshness.dungeonLossPerMinute * Time.deltaTime / 60f);

            SetHint(m_Blocked != null && !IsPrompting);
            if (IsPrompting || m_Blocked == null) return;
            var interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            if (interact != null && interact.WasPressedThisFrame()) OpenSwapPrompt();
        }

        /// <summary>A pickup the player is touching offers its parts. Returns how many went in.</summary>
        public int Offer(IngredientPickup pickup)
        {
            if (IsPrompting || pickup == null) return 0;
            int left = Satchel.Add(pickup.Item, pickup.Count, pickup.Freshness);
            int taken = pickup.Count - left;
            // The hint follows in Update: this runs inside a physics callback.
            if (left > 0) m_Blocked = pickup;
            else if (m_Blocked == pickup)
            {
                m_Blocked = null;
            }
            return taken;
        }

        /// <summary>The player stepped off a pickup.</summary>
        public void Withdraw(IngredientPickup pickup)
        {
            if (m_Blocked == pickup) m_Blocked = null;
        }

        void OpenSwapPrompt()
        {
            // No UI in the scene: nothing to ask with.
            if (EventBus<SwapPromptRequested>.HandlerCount == 0) return;
            IngredientPickup pickup = m_Blocked;
            IsPrompting = true;
            SetHint(false);
            MenuPause.Push();
            InputMaps.ActivateUIOnly();
            EventBus<SwapPromptRequested>.Publish(new SwapPromptRequested(Satchel, pickup.Stack, slot =>
            {
                Close();
                if (slot < 0 || pickup == null || pickup.Count <= 0) return;
                IngredientStack discarded = Satchel.ReplaceAt(slot, pickup.Stack, out int placed);
                pickup.Take(placed);
                if (m_Blocked == pickup && pickup.Count <= 0) m_Blocked = null;
                if (!discarded.IsEmpty && HarvestSystem.Instance != null)
                    HarvestSystem.Instance.Drop(discarded, transform.position, this);
            }));
        }

        void Close()
        {
            IsPrompting = false;
            MenuPause.Pop();
            InputMaps.Activate(InputMaps.Dungeon);
        }

        void SetHint(bool visible)
        {
            if (m_HintVisible == visible) return;
            m_HintVisible = visible;
            // Once when the satchel first refuses a part, not every frame the player stands on it.
            if (visible) m_FullFeedback?.PlayFeedbacks(transform.position);
            EventBus<SatchelFullHint>.Publish(new SatchelFullHint(visible));
        }
    }
}
