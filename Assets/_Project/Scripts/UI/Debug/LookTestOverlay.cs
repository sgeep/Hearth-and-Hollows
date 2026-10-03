using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Engine;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthdelve.UI.Debugging
{
    /// <summary>
    /// Look-test helper: F2 cycles the pixel-perfect reference resolution so character scale can
    /// be compared live, F3 swaps scenes, and F4 cycles the scroll mode (<see cref="ScrollMode"/>)
    /// so the scrolling options can be compared before one is chosen. It also restarts the room a
    /// moment after the player dies (the real death and Lockbox flow is 4b). Not part of the game.
    /// </summary>
    public sealed class LookTestOverlay : MonoBehaviour
    {
        /// <summary>How the view scrolls. Smooth is the locked default (CLAUDE.md, Camera); the others are kept for comparison.</summary>
        public enum ScrollMode
        {
            /// <summary>Everything on the art-pixel grid; the world moves in whole art pixels.</summary>
            PixelPerfect,
            /// <summary>The same low-res pipeline at twice the resolution: positions snap to half art pixels.</summary>
            HalfPixel,
            /// <summary>Rendered at screen resolution: the camera and characters move in screen pixels.</summary>
            Smooth,
        }

        static readonly Vector2Int[] k_Resolutions = { new(320, 180), new(240, 135), new(160, 90) };
        static readonly string[] k_ModeLabels = { LocKeys.LookTestResolutionPixel, LocKeys.LookTestResolutionHalf, LocKeys.LookTestResolution };
        static int s_Index;
        static ScrollMode s_Mode = ScrollMode.Smooth;

        [SerializeField] PixelPerfectCamera m_Camera;
        [SerializeField] CanvasScaler m_Scaler;
        [SerializeField] LocalizedSuperText m_Label;
        [SerializeField, Tooltip("The scene F3 switches to.")]
        string m_OtherScene;

        int m_BasePixelsPerUnit;
        PixelSnappedPresentation m_Presentation;

        public Vector2Int Resolution => k_Resolutions[s_Index];
        public static ScrollMode Mode => s_Mode;

        public void Configure(PixelPerfectCamera camera, CanvasScaler scaler, LocalizedSuperText label, string otherScene)
        {
            m_Camera = camera;
            m_Scaler = scaler;
            m_Label = label;
            m_OtherScene = otherScene;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Index = 0;
            s_Mode = ScrollMode.Smooth;
        }

        void Start()
        {
            m_BasePixelsPerUnit = m_Camera != null ? m_Camera.assetsPPU : 8;
            Apply();
        }

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
            if (m_Presentation == null)
            {
                // TDE spawns the player after this overlay starts, and again on a restart.
                m_Presentation = FindAnyObjectByType<PixelSnappedPresentation>();
                if (m_Presentation != null) Apply();
            }
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f2Key.wasPressedThisFrame) Cycle();
            if (keyboard.f4Key.wasPressedThisFrame) SetScrollMode((ScrollMode)(((int)s_Mode + 1) % 3));
            if (keyboard.f3Key.wasPressedThisFrame && !string.IsNullOrEmpty(m_OtherScene) && Application.CanStreamedLevelBeLoaded(m_OtherScene))
                SceneManager.LoadScene(m_OtherScene);
        }

        public void Cycle()
        {
            s_Index = (s_Index + 1) % k_Resolutions.Length;
            Apply();
        }

        public void SetScrollMode(ScrollMode mode)
        {
            s_Mode = mode;
            Apply();
        }

        void Apply()
        {
            Vector2Int resolution = Resolution;
            // Half-pixel mode doubles the low-res target and the grid together, so the view covers the same area.
            int scale = s_Mode == ScrollMode.HalfPixel ? 2 : 1;
            int basePpu = m_BasePixelsPerUnit > 0 ? m_BasePixelsPerUnit : 8;
            if (m_Camera != null)
            {
                m_Camera.assetsPPU = basePpu * scale;
                m_Camera.refResolutionX = resolution.x * scale;
                m_Camera.refResolutionY = resolution.y * scale;
                m_Camera.gridSnapping = s_Mode == ScrollMode.Smooth
                    ? PixelPerfectCamera.GridSnapping.None
                    : PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            }
            if (m_Presentation != null)
            {
                m_Presentation.PixelsPerUnit = basePpu * scale;
                m_Presentation.Snapping = s_Mode != ScrollMode.Smooth;
            }
            // The HUD keeps its 320x180 layout; only the world's pixel size changes.
            if (m_Label != null) m_Label.Set(k_ModeLabels[(int)s_Mode], resolution.x, resolution.y);
        }
    }
}
