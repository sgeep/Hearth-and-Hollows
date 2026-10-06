using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>
    /// A click anywhere on the dialogue box moves it on. Not a Button: it never takes the keyboard or gamepad focus, so Submit
    /// can't reach it twice.
    /// </summary>
    public sealed class DialogueBoxClick : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) Clicked?.Invoke();
        }
    }
}
