using Hearthdelve.Core.Events;
using MoreMountains.TopDownEngine;
using Unity.Cinemachine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>Points the Cinemachine camera at the player once the level has spawned them.</summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class PlayerCameraTarget : MonoBehaviour
    {
        CinemachineCamera m_Camera;

        void Awake() => m_Camera = GetComponent<CinemachineCamera>();
        void OnEnable() => EventBus<LevelStarted>.Subscribe(OnLevelStarted);
        void OnDisable() => EventBus<LevelStarted>.Unsubscribe(OnLevelStarted);

        void Start() => Bind();
        void OnLevelStarted(LevelStarted _) => Bind();

        void Bind()
        {
            if (!LevelManager.HasInstance || LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0) return;
            Character player = LevelManager.Instance.Players[0];
            if (player == null) return;
            m_Camera.Follow = player.transform;
            m_Camera.ForceCameraPosition(new Vector3(player.transform.position.x, player.transform.position.y, transform.position.z), Quaternion.identity);
        }
    }
}
