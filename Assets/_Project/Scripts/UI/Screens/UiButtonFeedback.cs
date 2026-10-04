using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>Plays the menu click (or, for a commitment, the click and a light tap) when its button is pressed.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class UiButtonFeedback : MonoBehaviour
    {
        [SerializeField, Tooltip("A step forward in the day: also a very light tap.")] bool m_Commit;

        public bool Commit
        {
            get => m_Commit;
            set => m_Commit = value;
        }

        void Awake() => GetComponent<Button>().onClick.AddListener(() => UiFeedback.Play(m_Commit ? UiMoment.Commit : UiMoment.Confirm));
    }
}
