using Hearthdelve.Core.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI
{
    /// <summary>
    /// Scales a screen-space canvas by whole pixels (<see cref="PixelScale"/>), like the Pixel Perfect Camera zooms the
    /// world: one canvas unit is one game pixel, and always a whole number of screen pixels, so the pixel font and the UI sprites
    /// stay crisp at any window size. Replaces the scaler's stretch-to-height mode.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class PixelCanvasScaler : MonoBehaviour
    {
        CanvasScaler m_Scaler;
        Canvas m_Canvas;

        /// <summary>The scale in use.</summary>
        public int Scale { get; private set; } = 1;

        void OnEnable()
        {
            m_Scaler = GetComponent<CanvasScaler>();
            m_Canvas = GetComponent<Canvas>();
            Apply();
        }

        void Update() => Apply();

        /// <summary>Sets the scale for the size the canvas draws at (the screen, or its camera's target).</summary>
        public void Apply()
        {
            if (m_Scaler == null) m_Scaler = GetComponent<CanvasScaler>();
            if (m_Canvas == null) m_Canvas = GetComponent<Canvas>();
            if (m_Scaler == null) return;
            int width = Screen.width, height = Screen.height;
            if (m_Canvas != null && m_Canvas.renderMode == RenderMode.ScreenSpaceCamera && m_Canvas.worldCamera != null)
            {
                width = m_Canvas.worldCamera.pixelWidth;
                height = m_Canvas.worldCamera.pixelHeight;
            }
            Scale = PixelScale.For(width, height);
            if (m_Scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize) m_Scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            if (!Mathf.Approximately(m_Scaler.scaleFactor, Scale)) m_Scaler.scaleFactor = Scale;
        }
    }
}
