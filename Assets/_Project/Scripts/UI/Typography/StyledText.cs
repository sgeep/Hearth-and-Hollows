using UnityEngine;

namespace Hearthdelve.UI.Typography
{
    /// <summary>
    /// Which <see cref="TextStyle"/> a text is. The builders set it, and the type scale is baked from it into the text's Super Text
    /// Mesh settings (<see cref="TypeScale.Apply(SuperTextMesh, TextStyle)"/>), so sizes live in one asset, never in prefabs.
    /// </summary>
    [RequireComponent(typeof(SuperTextMesh))]
    public sealed class StyledText : MonoBehaviour
    {
        [SerializeField] TextStyle m_Style = TextStyle.Body;

        public TextStyle Style
        {
            get => m_Style;
            set => m_Style = value;
        }
    }
}
