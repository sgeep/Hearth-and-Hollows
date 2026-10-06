using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>The mouse moves the focus to the choice it points at, so the pointer, the highlight and Submit always agree.</summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            var selectable = GetComponent<Selectable>();
            if (selectable != null && selectable.interactable && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
