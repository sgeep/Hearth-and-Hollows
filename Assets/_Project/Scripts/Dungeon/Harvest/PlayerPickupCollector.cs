using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// Auto-collects parts the player touches. When the satchel can't take a part, shows a hint;
    /// pressing Interact opens the swap prompt (game paused) to discard a slot for it.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerPickupCollector : MonoBehaviour
    {
        [SerializeField] LayerMask m_PickupMask;
        [SerializeField] Vector2 m_Reach = new(1.1f, 1.6f);

        PlayerController m_Controller;
        readonly List<Collider2D> m_Overlaps = new(8);
        IngredientPickup m_BlockedPickup;
        bool m_HintVisible;
        bool m_Prompting;
        bool m_InteractLatched;

        public void Configure(LayerMask pickupMask) => m_PickupMask = pickupMask;

        void Awake()
        {
            m_Controller = GetComponent<PlayerController>();
        }

        void Update()
        {
            // Interact is read here (not via the controller's latched frame) so the prompt opens immediately.
            var interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            if (interact != null && interact.WasPressedThisFrame()) m_InteractLatched = true;
        }

        void FixedUpdate()
        {
            var run = DelveRunController.Active;
            if (run == null || m_Prompting) return;
            var satchel = run.Satchel;

            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(m_PickupMask);
            Vector2 center = m_Controller.Mover.Position + Vector2.up * (m_Reach.y * 0.5f);
            int n = Physics2D.OverlapBox(center, m_Reach, 0f, filter, m_Overlaps);

            IngredientPickup blocked = null;
            for (int i = 0; i < n; i++)
            {
                var pickup = m_Overlaps[i].GetComponentInParent<IngredientPickup>();
                if (pickup == null || !pickup.IsCollectable) continue;

                int remainder = satchel.Add(pickup.Item, pickup.Count, pickup.Freshness);
                if (remainder == 0) Destroy(pickup.gameObject);
                else
                {
                    pickup.Count = remainder;
                    blocked ??= pickup;
                }
            }

            m_BlockedPickup = blocked;
            SetHint(blocked != null);

            if (m_InteractLatched && m_BlockedPickup != null) OpenSwapPrompt(m_BlockedPickup);
            m_InteractLatched = false;
        }

        void OpenSwapPrompt(IngredientPickup pickup)
        {
            var satchel = DelveRunController.Active.Satchel;
            if (EventBus<SwapPromptRequested>.HandlerCount == 0) return; // no UI loaded

            m_Prompting = true;
            SetHint(false);
            GamePause.PushMenuPause();
            InputMaps.ActivateUIOnly();

            EventBus<SwapPromptRequested>.Publish(new SwapPromptRequested(satchel, pickup.Stack, slot =>
            {
                m_Prompting = false;
                GamePause.PopMenuPause();
                InputMaps.Activate(InputMaps.Dungeon);
                if (slot < 0 || pickup == null) return;

                var discarded = satchel.ReplaceAt(slot, pickup.Stack, out int placed);
                pickup.Count -= placed;
                if (pickup.Count <= 0) Destroy(pickup.gameObject);

                // The discarded stack drops at the player's feet so they can change their mind.
                if (!discarded.IsEmpty && HarvestSystem.Instance != null)
                {
                    var dropped = HarvestSystem.Instance.Spawn(discarded,
                        m_Controller.Mover.Position + Vector2.up * 0.5f, new Vector2(-m_Controller.Facing * 3f, 5f));
                    if (dropped != null) dropped.WaitForPlayerToLeave();
                }
            }));
        }

        void SetHint(bool visible)
        {
            if (m_HintVisible == visible) return;
            m_HintVisible = visible;
            EventBus<SatchelFullHint>.Publish(new SatchelFullHint(visible));
        }

        void OnDisable() => SetHint(false);
    }
}
