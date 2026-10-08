using System;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// One line of the Options screen (4i-B): its name, then "‹ value ›". Up and down move between lines (uGUI navigation); left and
    /// right change it (a controller's stick or d-pad, the arrow keys); a click on its left half lowers it, on its right half raises
    /// it. A line that doesn't apply here (the window size while fullscreen) is shown dimmed and can't be changed.
    /// </summary>
    public sealed class OptionRow : Selectable, IPointerClickHandler
    {
        [SerializeField] LocalizedSuperText m_Label;
        [SerializeField] LocalizedSuperText m_Value;
        [SerializeField] GameObject m_Arrows;

        Func<string> m_Show;
        Action<int> m_Change;
        Func<bool> m_Usable;

        public LocalizedSuperText Label => m_Label;
        public string ValueText { get; private set; }

        public void Configure(LocalizedSuperText label, LocalizedSuperText value, GameObject arrows)
        {
            m_Label = label;
            m_Value = value;
            m_Arrows = arrows;
        }

        /// <summary>What this line is now: its name, how to show its value (already localized), how it changes, and when it applies.</summary>
        public void Bind(string labelKey, Func<string> show, Action<int> change, Func<bool> usable = null)
        {
            m_Show = show;
            m_Change = change;
            m_Usable = usable;
            m_Label?.Set(labelKey);
            Refresh();
        }

        public bool Usable => m_Usable == null || m_Usable();

        public void Refresh()
        {
            ValueText = m_Show != null ? m_Show() : string.Empty;
            m_Value?.Set(TavernLocKeys.Plain, ValueText);
            if (m_Arrows != null) m_Arrows.SetActive(Usable);
            var group = GetComponent<CanvasGroup>();
            if (group != null) group.alpha = Usable ? 1f : 0.45f;
        }

        /// <summary>Changes the value by <paramref name="direction"/> (−1 or +1), if it applies.</summary>
        public void Step(int direction)
        {
            if (!Usable || m_Change == null || direction == 0) return;
            m_Change(direction);
            UiFeedback.Play(UiMoment.Tick);
            Refresh();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left) Step(-1);
            else if (eventData.moveDir == MoveDirection.Right) Step(1);
            else base.OnMove(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Step(local.x < rect.rect.center.x + rect.rect.width * 0.25f ? -1 : 1);
        }
    }
}
