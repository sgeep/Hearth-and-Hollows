using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>The player's satchel for this delve. Lives on the player character.</summary>
    public sealed class SatchelCarrier : MonoBehaviour
    {
        [SerializeField] DelveConfig m_Config;

        public Satchel Satchel { get; private set; }

        public void Configure(DelveConfig config) => m_Config = config;

        void Awake()
        {
            SatchelSettings settings = m_Config != null ? m_Config.satchel : SatchelSettings.Default;
            DelveLoadout loadout = GameFlow.Instance != null ? GameFlow.Instance.Loadout : DelveLoadout.None;
            Satchel = new Satchel(settings.capacity + loadout.ExtraSatchelSlots, settings.maxStack);
        }

        void Start() => EventBus<SatchelBound>.Publish(new SatchelBound(Satchel));
    }
}
