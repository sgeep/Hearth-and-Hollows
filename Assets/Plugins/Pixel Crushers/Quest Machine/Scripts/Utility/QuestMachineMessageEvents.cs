// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEngine.Events;
using System;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Provides a in-editor way to listen for messages and invoke events when they occur,
    /// and to send a message on demand, with awareness of Quest Machine IDs.
    /// </summary>
    [Serializable]
    [AddComponentMenu("")] // Use wrapper.
    public class QuestMachineMessageEvents : MonoBehaviour, IMessageHandler
    {

        [Serializable]
        public class MessageEvent
        {
            [Tooltip("(Optional) If set, only react to messages sent from sender having this Quest Machine ID.")]
            public StringField requiredSenderID;

            [Tooltip("(Optional) If set, only react to messages sent from this sender.")]
            public GameObject requiredSender;

            [Tooltip("(Optional) If set, only react to messages sent to target having this Quest Machine ID.")]
            public StringField requiredTargetID;

            [Tooltip("(Optional) If set, only react to messages sent to this target.")]
            public GameObject requiredTarget;

            [Tooltip("Listen for this message.")]
            public StringField message;

            [Tooltip("(Optional) If set, listen for this parameter with the message.")]
            public StringField parameter;

            public MessageArgsEvent onMessage = new MessageArgsEvent();
        }

        [Serializable]
        public class MessageToSend
        {
            [Tooltip("(Optional) If set, specify GameObject having this Quest Machine ID as the message target.")]
            public StringField targetID;

            [Tooltip("(Optional) If set, specify this GameObject as the message target.")]
            public GameObject target;

            [Tooltip("Send this message.")]
            public StringField message;

            [Tooltip("(Optional) If set, send this parameter with the message.")]
            public StringField parameter;
        }

        [SerializeField]
        private MessageEvent[] m_messagesToListenFor;

        [SerializeField]
        private MessageToSend[] m_messagesToSend;

        /// <summary>
        /// Listen for these messages and invoke an event when each message occurs.
        /// </summary>
        public MessageEvent[] messagesToListenFor
        {
            get { return m_messagesToListenFor; }
            set { m_messagesToListenFor = value; }
        }

        /// <summary>
        /// Send this message on demand by calling SendToMessageSystem().
        /// </summary>
        public MessageToSend[] messagesToSend
        {
            get { return m_messagesToSend; }
            set { m_messagesToSend = value; }
        }

        protected virtual void OnEnable()
        {
            for (int i = 0; i < messagesToListenFor.Length; i++)
            {
                var messageEvent = messagesToListenFor[i];
                MessageSystem.AddListener(this, messageEvent.message, messageEvent.parameter);
            }
        }

        protected virtual void OnDisable()
        {
            for (int i = 0; i < messagesToListenFor.Length; i++)
            {
                var messageEvent = messagesToListenFor[i];
                MessageSystem.RemoveListener(this, messageEvent.message, messageEvent.parameter);
            }
        }

        public virtual void OnMessage(MessageArgs messageArgs)
        {
            for (int i = 0; i < messagesToListenFor.Length; i++)
            {
                var messageEvent = messagesToListenFor[i];
                if (IsParticipantOk(messageEvent.requiredSenderID, messageEvent.requiredSender, messageArgs.sender) &&
                    IsParticipantOk(messageEvent.requiredTargetID, messageEvent.requiredTarget, messageArgs.target) &&
                    string.Equals(messageEvent.message, messageArgs.message) &&
                    (StringField.IsNullOrEmpty(messageEvent.parameter) || string.Equals(messageEvent.parameter, messageArgs.parameter)))
                {
                    messageEvent.onMessage.Invoke(messageArgs);
                }
            }
        }

        protected virtual bool IsParticipantOk(StringField requiredID, GameObject requiredParticipant, object actualParticipant)
        {
            if (requiredParticipant == null && StringField.IsNullOrEmpty(requiredID)) return true;
            if (actualParticipant == null) return false;
            return 
                StringField.Equals(QuestMachineMessages.GetID(actualParticipant), requiredID) ||
                (actualParticipant as GameObject == requiredParticipant) ||
                (actualParticipant as Transform == requiredParticipant.transform) ||
                ((actualParticipant is MonoBehaviour) && (actualParticipant as MonoBehaviour).gameObject == requiredParticipant) ||
                (actualParticipant.GetType() == typeof(string) && (string)actualParticipant == requiredParticipant.name) ||
                (actualParticipant.GetType() == typeof(StringField) && StringField.GetStringValue(actualParticipant as StringField) == requiredParticipant.name);
        }

        public virtual void SendToMessageSystem(int index)
        {
            if (messagesToSend == null) return;
            if (!(0 <= index && index < messagesToSend.Length)) return;
            var messageToSend = messagesToSend[index];
            var target = !StringField.IsNullOrEmpty(messageToSend.targetID) ? QuestMachineMessages.FindGameObjectWithID(messageToSend.targetID)
                : messageToSend.target;
            MessageSystem.SendMessageWithTarget(this, target, messageToSend.message, messageToSend.parameter);
        }
    }
}