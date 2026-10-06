// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Unity UI implementation of QuestAlertUI.
    /// </summary>
    [AddComponentMenu("")] // Use wrapper.
    public class UnityUIQuestAlertUI : UnityUIBaseUI, IQuestAlertUI
    {

        #region Serialized Fields
        [SerializeField]
        private RectTransform m_contentContainer;

        [Header("UI Templates")]

        [SerializeField]
        private UnityUIContainerTemplate m_alertContainerTemplate;
        [SerializeField]
        private UnityUITextTemplate m_headingTemplate;
        [SerializeField]
        private UnityUITextTemplate[] m_subheadingTemplates;
        [SerializeField]
        private UnityUITextTemplate m_bodyTemplate;
        [SerializeField]
        private UnityUIIconListTemplate m_iconListTemplate;
        [SerializeField]
        private UnityUIButtonListTemplate m_buttonListTemplate;
        [Tooltip("Use container template even for single-element content such as a single string.")]
        [SerializeField]
        private bool m_alwaysUseContainerTemplate = false;

        [Header("Duration")]

        [Tooltip("Queue new alerts if an alert is currently showing. If your alert UI scrolls alerts, you probably want this to be UNticked.")]
        [SerializeField]
        private bool m_queueAlerts = false;

        [Tooltip("Minimum duration in seconds to show alerts.")]
        [SerializeField]
        private float m_minDisplayDuration = 5;

        [Tooltip("Duration to show alerts in characters per second.")]
        [SerializeField]
        private int m_charsPerSecDuration = 50;

        [Tooltip("When hiding after last content is done, leave last content visible during hide animation.")]
        [SerializeField]
        private bool m_leaveLastContentVisibleDuringHide = false;

        [SerializeField]
        private UnityEvent m_onShowAlert = new UnityEvent();

        #endregion

        #region Accessor Properties for Serialized Fields

        public RectTransform contentContainer
        {
            get { return m_contentContainer; }
            set { m_contentContainer = value; }
        }

        public UnityUIContainerTemplate alertContainerTemplate
        {
            get { return m_alertContainerTemplate; }
            set { m_alertContainerTemplate = value; }
        }
        public UnityUITextTemplate headingTemplate
        {
            get { return m_headingTemplate; }
            set { m_headingTemplate = value; }
        }
        public UnityUITextTemplate[] subheadingTemplates
        {
            get { return m_subheadingTemplates; }
            set { m_subheadingTemplates = value; }
        }
        public UnityUITextTemplate bodyTemplate
        {
            get { return m_bodyTemplate; }
            set { m_bodyTemplate = value; }
        }
        public UnityUIIconListTemplate iconListTemplate
        {
            get { return m_iconListTemplate; }
            set { m_iconListTemplate = value; }
        }
        public UnityUIButtonListTemplate buttonListTemplate
        {
            get { return m_buttonListTemplate; }
            set { m_buttonListTemplate = value; }
        }
        public bool alwaysUseContainerTemplate
        {
            get { return m_alwaysUseContainerTemplate; }
            set { m_alwaysUseContainerTemplate = value; }
        }

        /// <summary>
        /// Queue new alerts if an alert is already showing.
        /// </summary>
        public bool queueAlerts
        {
            get { return m_queueAlerts; }
            set { m_queueAlerts = value; }
        }

        /// <summary>
        /// Minimum duration in seconds to show alerts.
        /// </summary>
        public float minDisplayDuration
        {
            get { return m_minDisplayDuration; }
            set { m_minDisplayDuration = value; }
        }

        /// <summary>
        /// Duration to show alerts in characters per second.
        /// </summary>
        public int charsPerSecDuration
        {
            get { return m_charsPerSecDuration; }
            set { m_charsPerSecDuration = value; }
        }

        /// <summary>
        /// When hiding after last content is done, leave last content visible during hide animation.
        /// </summary>
        public bool leaveLastContentVisibleDuringHide
        {
            get { return m_leaveLastContentVisibleDuringHide; }
            set { m_leaveLastContentVisibleDuringHide = value; }
        }

        public UnityEvent onShowAlert { get { return m_onShowAlert; } }

        #endregion

        #region Runtime Properties

        protected UnityUIInstancedContentManager contentManager { get; set; }
        protected override RectTransform currentContentContainer { get { return contentContainer; } }
        protected override UnityUIInstancedContentManager currentContentManager { get { if (contentManager == null) contentManager = new UnityUIInstancedContentManager(); return contentManager; } }
        protected override UnityUITextTemplate currentHeadingTemplate { get { return headingTemplate; } }
        protected override UnityUITextTemplate[] currentSubheadingTemplates { get { return subheadingTemplates; } }
        protected override UnityUITextTemplate currentBodyTemplate { get { return bodyTemplate; } }
        protected override UnityUIIconListTemplate currentIconListTemplate { get { return iconListTemplate; } }
        protected override UnityUIButtonListTemplate currentButtonListTemplate { get { return buttonListTemplate; } }

        protected Coroutine timedDespawnCoroutine = null;
        protected Queue<QueuedContent> queuedAlerts = new Queue<QueuedContent>();

        protected struct QueuedContent
        {
            public string questID;
            public List<QuestContent> contents;
            public string message;

            public QueuedContent(string questID, List<QuestContent> contents)
            { this.questID = questID; this.contents = contents; this.message = null; }

            public QueuedContent(string message)
            { this.message = message; this.questID = null; this.contents = null; }
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            contentManager = new UnityUIInstancedContentManager();
            if (alertContainerTemplate != null) alertContainerTemplate.gameObject.SetActive(false);
            if (contentContainer == null && Debug.isDebugBuild) Debug.LogError("Quest Machine: Content Container is unassigned.", this);
            if (alertContainerTemplate == null && Debug.isDebugBuild) Debug.LogError("Quest Machine: Alert Container Template is unassigned.", this);
        }

        protected virtual void Start()
        {
            if (uiPanel != null) uiPanel.onClosed.AddListener(OnClosed);
        }

        protected virtual void OnClosed()
        {
            contentManager.instancedContent.ForEach(x => x.Despawn());
        }

        public override void Show()
        {
            currentContentManager.RetireInstances();
            base.Show();
            currentContentManager.DespawnRetiredInstances();
        }

        /// <summary>
        /// Shows a quest alert.
        /// </summary>
        /// <param name="questID">Quest ID.</param>
        /// <param name="contents">Complete quest alert content.</param>
        public virtual void ShowAlert(string questID, List<QuestContent> contents)
        {
            if (contents == null || contents.Count == 0) return;
            if (!isVisible) Show();

            // If an alert is already showing and queueAlerts is true, queue this one for later:
            if (queueAlerts && timedDespawnCoroutine != null)
            {
                queuedAlerts.Enqueue(new QueuedContent(questID, contents));
                return;
            }

            // Otherwise show it:
            UnityUIContentTemplate alertInstance;
            if (contents.Count == 1 && !alwaysUseContainerTemplate)
            {
                AddContent(contents[0]);
                alertInstance = (contentManager.instancedContent.Count == 0) ? null
                    : contentManager.instancedContent[contentManager.instancedContent.Count - 1];
            }
            else
            {
                var container = contentManager.AcquireContentInstance<UnityUIContainerTemplate>(alertContainerTemplate, null, null);
                container.gameObject.SetActive(true);
                alertInstance = container;
                contentManager.AddToContainer(container, currentContentContainer);
                var realContentManager = contentManager; // Don't add sub-contents to real content manager.
                contentManager = new UnityUIInstancedContentManager();
                for (int i = 0; i < contents.Count; i++)
                {
                    AddContent(contents[i]);
                    container.AddInstanceToContainer(contentManager.GetLastAdded());
                }
                contentManager = realContentManager;
            }
            if (alertInstance != null)
            {
                timedDespawnCoroutine = StartCoroutine(TimedDespawn(alertInstance, GetDisplayDuration(contents)));
            }
            onShowAlert.Invoke();
        }

        protected override void AddContent(QuestContent content)
        {
            base.AddContent(content);
        }

        /// <summary>
        /// Shows a quest alert.
        /// </summary>
        /// <param name="message">Alert to show.</param>
        public virtual void ShowAlert(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (!isVisible) Show();

            // If an alert is already showing and queueAlerts is true, queue this one for later:
            if (queueAlerts && timedDespawnCoroutine != null)
            {
                queuedAlerts.Enqueue(new QueuedContent(message));
                return;
            }

            if (alwaysUseContainerTemplate)
            {
                var container = contentManager.AcquireContentInstance<UnityUIContainerTemplate>(alertContainerTemplate, null, null);
                var alertInstance = container;
                contentManager.AddToContainer(container, currentContentContainer);
                var realContentManager = contentManager; // Don't add sub-contents to real content manager.
                contentManager = new UnityUIInstancedContentManager();
                AddBodyContent(message);
                container.AddInstanceToContainer(contentManager.GetLastAdded());
                contentManager = realContentManager;
                if (alertInstance != null)
                {
                    timedDespawnCoroutine = StartCoroutine(TimedDespawn(alertInstance, GetDisplayDuration(message)));
                }
                onShowAlert.Invoke();
            }
            else
            {
                // If useContainerTemplateForSimpleStrings is false, add body text directly to container
                // to save the instantiation of an extra GameObject:
                var alertInstance = contentManager.AcquireContentInstance<UnityUITextTemplate>(bodyTemplate, null, null);
                currentContentManager.AddToContainer(alertInstance, currentContentContainer);
                alertInstance.Assign(message);
                timedDespawnCoroutine = StartCoroutine(TimedDespawn(alertInstance, GetDisplayDuration(message)));
                onShowAlert.Invoke();
            }
        }

        /// <summary>
        /// Shows a quest alert.
        /// </summary>
        /// <param name="stringField">Alert to show.</param>
        public void ShowAlert(StringField stringField)
        {
            ShowAlert(StringField.GetStringValue(stringField));
        }

        protected virtual IEnumerator TimedDespawn(UnityUIContentTemplate instance, float duration)
        {
            if (instance != null)
            {
                yield return new WaitForSeconds(duration);
                if (leaveLastContentVisibleDuringHide &&
                    contentManager.instancedContent.Count <= 1 &&
                    queuedAlerts.Count == 0)
                {
                    Hide();
                }
                else
                {
                    contentManager.Remove(instance);
                    if (contentManager.instancedContent.Count == 0 && queuedAlerts.Count == 0)
                    {
                        Hide();
                    }
                }
            }
            timedDespawnCoroutine = null;

            // If there's a queued alert, show it:
            if (queuedAlerts.Count > 0)
            {
                var nextContents = queuedAlerts.Dequeue();
                if (string.IsNullOrEmpty(nextContents.message))
                {
                    ShowAlert(nextContents.questID, nextContents.contents);
                }
                else
                {
                    ShowAlert(nextContents.message);
                }
            }
        }

        protected float GetDisplayDuration(List<QuestContent> contents)
        {
            float duration = minDisplayDuration;
            if (contents != null)
            {
                for (int i = 0; i < contents.Count; i++)
                {
                    duration = Mathf.Max(duration, GetDisplayDuration(contents[i]));
                }
            }
            return duration;
        }

        protected float GetDisplayDuration(QuestContent content)
        {
            if (content is HeadingTextQuestContent)
            {
                return GetDisplayDuration((content as HeadingTextQuestContent).headingText);
            }
            else if (content is BodyTextQuestContent)
            {
                return GetDisplayDuration((content as BodyTextQuestContent).bodyText);
            }
            else if (content is IconQuestContent)
            {
                return GetDisplayDuration((content as IconQuestContent).caption);
            }
            else
            {
                return minDisplayDuration;
            }
        }

        protected float GetDisplayDuration(StringField stringField)
        {
            return GetDisplayDuration(StringField.GetStringValue(stringField));
        }

        protected virtual float GetDisplayDuration(string text)
        {
            return Mathf.Max(minDisplayDuration, string.IsNullOrEmpty(text) ? 0 : (float)text.Length / charsPerSecDuration);
        }
    }
}
