using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Hud
{
    /// <summary>One line of the harvest feed: the part's icon and what happened to it.</summary>
    [Serializable]
    public sealed class HarvestFeedLine
    {
        public CanvasGroup group;
        public Image icon;
        public LocalizedSuperText text;
        [NonSerialized] public float shownAt = float.NegativeInfinity;
    }

    /// <summary>
    /// What a kill produced (GDD §4.3), as a short feed on the HUD: "Clean kill! Spider Leg ×2",
    /// "Overkill! …", "Spider Leg was destroyed", newest on top, each fading after a few seconds.
    /// Listens for <see cref="HarvestFeedback"/>.
    /// </summary>
    public sealed class HarvestFeed : MonoBehaviour
    {
        [SerializeField] HarvestFeedLine[] m_Lines = Array.Empty<HarvestFeedLine>();
        [SerializeField, Min(0.1f), Tooltip("Seconds a line stays fully visible.")]
        float m_Hold = 2.4f;
        [SerializeField, Min(0.01f), Tooltip("Seconds it takes to fade.")]
        float m_Fade = 0.5f;
        [SerializeField, Min(1f), Tooltip("Spacing between lines, in canvas pixels.")]
        float m_LineHeight = 9f;
        [SerializeField, Tooltip("A furnishing found (4f Checkpoint C): the chest beside its line.")]
        Sprite m_CurioIcon;
        [SerializeField, Tooltip("A found furnishing's line: its own color, unlike a harvest's.")]
        Color m_CurioColor = new(0.62f, 0.86f, 1f);

        Color m_TextColor = Color.white;

        public HarvestFeedLine[] Lines => m_Lines;

        public void Configure(HarvestFeedLine[] lines, Sprite curioIcon = null)
        {
            m_Lines = lines;
            m_CurioIcon = curioIcon;
        }

        void Awake()
        {
            foreach (HarvestFeedLine line in m_Lines) line.group.alpha = 0f;
            var first = m_Lines.Length > 0 ? m_Lines[0].text.GetComponent<SuperTextMesh>() : null;
            if (first != null) m_TextColor = first.color;
        }

        void OnEnable()
        {
            EventBus<HarvestFeedback>.Subscribe(Push);
            EventBus<CurioFound>.Subscribe(PushCurio);
        }

        void OnDisable()
        {
            EventBus<HarvestFeedback>.Unsubscribe(Push);
            EventBus<CurioFound>.Unsubscribe(PushCurio);
        }

        /// <summary>A furnishing found: "found: skull candle", with the chest, in the discovery color.</summary>
        void PushCurio(CurioFound found)
        {
            if (m_Lines.Length == 0 || string.IsNullOrEmpty(found.FurnitureId)) return;
            HarvestFeedLine line = NextLine();
            line.text.Set(LocKeys.HarvestCurio, LocKeys.FurnitureName(found.FurnitureId));
            Tint(line, m_CurioColor);
            line.icon.sprite = m_CurioIcon;
            line.icon.enabled = m_CurioIcon != null;
            line.shownAt = Time.time;
            line.group.alpha = 1f;
        }

        HarvestFeedLine NextLine()
        {
            // The oldest line is reused for the newest, which goes on top.
            HarvestFeedLine line = m_Lines[m_Lines.Length - 1];
            for (int i = m_Lines.Length - 1; i > 0; i--) m_Lines[i] = m_Lines[i - 1];
            m_Lines[0] = line;
            for (int i = 0; i < m_Lines.Length; i++)
                ((RectTransform)m_Lines[i].group.transform).anchoredPosition = new Vector2(0f, -i * m_LineHeight);
            return line;
        }

        static void Tint(HarvestFeedLine line, Color color)
        {
            var stm = line.text.GetComponent<SuperTextMesh>();
            if (stm != null && stm.color != color)
            {
                stm.color = color;
                stm.Rebuild();
            }
        }

        /// <summary>The key a harvest result is told with.</summary>
        public static string KeyFor(HarvestFlags flags) =>
            (flags & HarvestFlags.Destroyed) != 0 ? LocKeys.HarvestDestroyed
            : (flags & HarvestFlags.Finisher) != 0 ? LocKeys.HarvestGotFinisher
            : (flags & HarvestFlags.Overkill) != 0 ? LocKeys.HarvestGotOverkill
            : (flags & HarvestFlags.CleanKill) != 0 ? LocKeys.HarvestGotClean
            : LocKeys.HarvestGot;

        void Push(HarvestFeedback feedback)
        {
            if (m_Lines.Length == 0 || !feedback.Item.IsValid) return;
            HarvestFeedLine line = NextLine();
            Tint(line, m_TextColor);

            string name = Loc.ItemName(feedback.Item);
            string key = KeyFor(feedback.Flags);
            if (key == LocKeys.HarvestDestroyed) line.text.Set(key, name);
            else line.text.Set(key, name, feedback.Count);
            line.icon.sprite = feedback.Item.Definition.icon;
            line.icon.enabled = line.icon.sprite != null;
            line.shownAt = Time.time;
            line.group.alpha = 1f;
        }

        void Update()
        {
            foreach (HarvestFeedLine line in m_Lines)
            {
                float age = Time.time - line.shownAt;
                line.group.alpha = age <= m_Hold ? 1f : Mathf.Clamp01(1f - (age - m_Hold) / m_Fade);
            }
        }
    }
}
