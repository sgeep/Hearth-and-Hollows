// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Body text UI content.
    /// </summary>
    public class BodyTextQuestContent : QuestContent
    {

        [Tooltip("Text to show in regular body text style.")]
        [StringFieldTextArea]
        [SerializeField]
        private StringField m_bodyText;

        [SerializeField]
        [Tooltip("Specify a color for the text.")]
        private bool m_setColor = false;

        [SerializeField]
        [Tooltip("Color to set the text if Set Color is ticked.")]
        [ShowIf("m_setColor")]
        private Color m_color = Color.white;

        /// <summary>
        /// If true, set a color for the text.
        /// </summary>
        public bool setColor
        {
            get { return m_setColor; }
            set { m_setColor = value; }
        }

        /// <summary>
        /// Color to set the text if setColor is true.
        /// </summary>
        public Color color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        /// <summary>
        /// Text to show in regular body text style.
        /// </summary>
        public StringField bodyText
        {
            get { return m_bodyText; }
            set { m_bodyText = value; }
        }

        public override StringField originalText
        {
            get { return bodyText; }
            set { bodyText = value; }
       }

        public override string GetEditorName()
        {
            return (bodyText == null) ? "Body Text" : "Text: " + bodyText;
        }

        public override void SetDefaultTextTable(TextTable textTable)
        {
            if (m_bodyText == null) m_bodyText = new StringField();
            m_bodyText.SetDefaultTextTable(textTable);
        }

    }

}
