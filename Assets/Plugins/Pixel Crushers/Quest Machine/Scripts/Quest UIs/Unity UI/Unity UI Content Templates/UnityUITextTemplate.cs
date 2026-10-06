// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Unity UI template for text.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class UnityUITextTemplate : UnityUIContentTemplate
    {

        [Tooltip("Text UI element.")]
        [SerializeField]
        private UITextField m_text;

        /// <summary>
        /// Text UI element.
        /// </summary>
        public UITextField text
        {
            get { return m_text; }
            set { m_text = value; }
        }

        protected Color originalColor { get; set; }

        public virtual void Awake()
        {
            if (UITextField.IsNull(text) && Debug.isDebugBuild) Debug.LogError("Quest Machine: UI Text is unassigned.", this);
            originalColor = text.color;
        }

        /// <summary>
        /// Assigns a text string to the UI element.
        /// </summary>
        public void Assign(string text)
        {
            if (gameObject == null) return;
            name = text;
            this.text.text = text;
            this.text.color = originalColor;
        }

        /// <summary>
        /// Assigns a text string to the UI element and sets its color.
        /// </summary>
        public void Assign(string text, Color color)
        {
            if (gameObject == null) return;
            Assign(text);
            this.text.color = color;
        }

        /// <summary>
        /// Assigns a text string to the UI element and, if setColor is true, sets its color.
        /// </summary>
        public void Assign(string text, bool setColor, Color color)
        {
            if (setColor)
            {
                Assign(text, color);
            }
            else
            {
                Assign(text);
            }
        }

        public bool Matches(string text)
        {
            if (gameObject == null) return false;
            return this.text.text == text;
        }

    }
}
