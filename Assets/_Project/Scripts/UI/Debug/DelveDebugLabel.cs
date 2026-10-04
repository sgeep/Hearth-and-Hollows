using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Debugging
{
    /// <summary>
    /// A small debug line in the 4d run scene: the run's seed (set it on the RoomRunner to replay the same run), the floor
    /// and how many rooms in. For testing and bug reports, not part of the game.
    /// </summary>
    [RequireComponent(typeof(LocalizedSuperText))]
    public sealed class DelveDebugLabel : MonoBehaviour
    {
        LocalizedSuperText m_Text;

        void Awake() => m_Text = GetComponent<LocalizedSuperText>();
        void OnEnable() => EventBus<RoomEntered>.Subscribe(OnEntered);
        void OnDisable() => EventBus<RoomEntered>.Unsubscribe(OnEntered);

        void OnEntered(RoomEntered e) => m_Text.Set(LocKeys.DelveDebug, e.Seed, e.Floor, e.Index + 1);
    }
}
