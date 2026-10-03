using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// Shows and hides a hint by fading its CanvasGroup, never by deactivating it. Hints follow
    /// gameplay that can flicker every physics step (standing at the edge of a trigger), and
    /// deactivating Super Text Mesh makes it destroy and rebuild its mesh each time.
    /// </summary>
    public static class HintVisibility
    {
        public static void Init(GameObject hint)
        {
            if (hint == null) return;
            hint.SetActive(true);
            Set(hint, false);
        }

        public static void Set(GameObject hint, bool visible)
        {
            if (hint == null) return;
            if (!hint.TryGetComponent(out CanvasGroup group)) group = hint.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public static bool IsShown(GameObject hint) =>
            hint != null && hint.activeInHierarchy && hint.TryGetComponent(out CanvasGroup group) && group.alpha > 0f;
    }
}
