using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthdelve.UI.Debugging
{
    /// <summary>
    /// 4a look-test helper: F2 cycles the pixel-perfect reference resolution so character
    /// scale can be compared live, and F3 swaps between the dungeon room and the tavern
    /// corner. It also restarts the room a moment after the player dies (the real death and
    /// Lockbox flow is 4b). Not part of the game; removed once the resolution is decided.
    /// </summary>
    public sealed class LookTestOverlay : MonoBehaviour
    {
        static readonly Vector2Int[] k_Resolutions = { new(320, 180), new(240, 135), new(160, 90) };
        static int s_Index;

        [SerializeField] PixelPerfectCamera m_Camera;
        [SerializeField] CanvasScaler m_Scaler;
        [SerializeField] LocalizedSuperText m_Label;
        [SerializeField, Tooltip("The scene F3 switches to.")]
        string m_OtherScene;

        public Vector2Int Resolution => k_Resolutions[s_Index];

        public void Configure(PixelPerfectCamera camera, CanvasScaler scaler, LocalizedSuperText label, string otherScene)
        {
            m_Camera = camera;
            m_Scaler = scaler;
            m_Label = label;
            m_OtherScene = otherScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Index = 0;

        void Start() => Apply();

        void OnEnable() => EventBus<CharacterDied>.Subscribe(OnCharacterDied);
        void OnDisable() => EventBus<CharacterDied>.Unsubscribe(OnCharacterDied);

        void OnCharacterDied(CharacterDied e)
        {
            if (e.IsPlayer) StartCoroutine(Restart());
        }

        IEnumerator Restart()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f2Key.wasPressedThisFrame) Cycle();
            if (keyboard.f3Key.wasPressedThisFrame && !string.IsNullOrEmpty(m_OtherScene) && Application.CanStreamedLevelBeLoaded(m_OtherScene))
                SceneManager.LoadScene(m_OtherScene);
        }

        public void Cycle()
        {
            s_Index = (s_Index + 1) % k_Resolutions.Length;
            Apply();
        }

        void Apply()
        {
            Vector2Int resolution = Resolution;
            if (m_Camera != null)
            {
                m_Camera.refResolutionX = resolution.x;
                m_Camera.refResolutionY = resolution.y;
            }
            // The HUD keeps its 320×180 layout; only the world's pixel size changes.
            if (m_Label != null) m_Label.Set(LocKeys.LookTestResolution, resolution.x, resolution.y);
        }
    }
}
