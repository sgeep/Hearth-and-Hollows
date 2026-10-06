// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Quest condition that becomes true when another quest's node reaches a specified state.
    /// </summary>
    [Serializable]
    public class QuestNodeStateQuestCondition : QuestCondition, IMessageHandler
    {

        [Tooltip("Quest to monitor. If set, this takes priority over Required Quest ID. If neither is set, uses this quest.")]
        [SerializeField]
        private Quest m_requiredQuest;

        [Tooltip("ID of quest to monitor. Used if Required Quest is unassigned. If neither is set, uses this quest.")]
        [SerializeField]
        private StringField m_requiredQuestID;

        [Tooltip("ID of quest node to monitor. Leave blank to monitor this quest node. If neither is set, uses this quest.")]
        [SerializeField]
        private StringField m_requiredQuestNodeID;

        [Tooltip("Quest must not be the required state.")]
        [SerializeField]
        private bool m_isNot = false;

        [Tooltip("Required quest node state.")]
        [SerializeField]
        private QuestNodeState m_requiredState;

        /// <summary>
        /// Quest to monitor. If set, this takes priority over Required Quest ID. If neither is set, uses this quest.
        /// </summary>
        public Quest requiredQuest
        {
            get { return m_requiredQuest; }
            set { m_requiredQuest = value; }
        }
        /// <summary>
        /// ID of quest node to monitor. Leave blank to monitor this quest node. If neither is set, uses this quest.
        /// </summary>
        public StringField requiredQuestID
        {
            get { return (StringField.IsNullOrEmpty(m_requiredQuestID) && quest != null) ? quest.id : m_requiredQuestID; }
            set { m_requiredQuestID = value; }
        }

        /// <summary>
        /// ID of quest node to monitor. Leave blank for main quest state.
        /// </summary>
        public StringField requiredQuestNodeID
        {
            get { return (StringField.IsNullOrEmpty(m_requiredQuestNodeID) && questNode != null) ? questNode.id : m_requiredQuestNodeID; }
            set { m_requiredQuestNodeID = value; }
        }

        /// <summary>
        /// If true, quest node must not be the required state.
        /// </summary>
        public bool isNot
        {
            get { return m_isNot; }
            set { m_isNot = value; }
        }

        /// <summary>
        /// State that monitored quest node must be in.
        /// </summary>
        public QuestNodeState requiredState
        {
            get { return m_requiredState; }
            set { m_requiredState = value; }
        }

        public StringField questIDToCheck
        {
            get { return (requiredQuest != null) ? requiredQuest.id : requiredQuestID; }
        }

        public override string GetEditorName()
        {
            if (requiredQuest != null)
            {
                if (!StringField.IsNullOrEmpty(requiredQuestNodeID))
                {
                    return "Quest Node State: Quest '" + requiredQuest.id + "' Node '" + requiredQuestNodeID + "' " + (isNot ? "!= " : "== ") + requiredState;
                }
                else
                {
                    return "Quest Node State: Quest '" + requiredQuest.id + "' Node (unspecified) " + (isNot ? "!= " : "== ") + requiredState;
                }
            }
            else if (!StringField.IsNullOrEmpty(m_requiredQuestID))
            {
                if (!StringField.IsNullOrEmpty(requiredQuestNodeID))
                {
                    return "Quest Node State: Quest '" + m_requiredQuestID + "' Node '" + requiredQuestNodeID + "' " + (isNot ? "!= " : "== ") + requiredState;
                }
                else
                {
                    return "Quest Node State: Quest '" + m_requiredQuestID + "' Node (unspecified) " + (isNot ? "!= " : "== ") + requiredState;
                }
            }
            else
            {
                if (!StringField.IsNullOrEmpty(requiredQuestNodeID))
                {
                    return "Quest Node State: Quest Node '" + requiredQuestNodeID + "' " + (isNot ? "!= " : "== ") + requiredState;
                }
                else
                {
                    return "Quest Node State: Quest Node (unspecified) " + (isNot ? "!= " : "== ") + requiredState;
                }
            }
        }

        public override void StartChecking(System.Action trueAction)
        {
            base.StartChecking(trueAction);
            if (requiredQuestID == null) return;
            if (IsConditionTrue(QuestMachine.GetQuestNodeState(questIDToCheck, requiredQuestNodeID)))
            {
                SetTrue();
            }
            else
            {
                MessageSystem.AddListener(this, QuestMachineMessages.QuestStateChangedMessage, questIDToCheck.value);
            }
        }

        public override void StopChecking()
        {
            base.StopChecking();
            MessageSystem.RemoveListener(this);
        }

        void IMessageHandler.OnMessage(MessageArgs messageArgs)
        {
            if (!isChecking) return;
            if (messageArgs.values == null || messageArgs.values.Length < 2 || requiredQuestID == null) return;
            var questID = messageArgs.parameter;
            var isThisQuest = StringField.Equals(questIDToCheck, questID);
            if (!isThisQuest) return;
            var questNodeID = QuestMachineMessages.ArgToString(messageArgs.values[0]);
            if (!string.Equals(questNodeID, requiredQuestNodeID.value)) return;
            var stateValue = messageArgs.values[1];
            var state = (stateValue != null && stateValue.GetType() == typeof(QuestNodeState)) ? (QuestNodeState)stateValue : QuestNodeState.Inactive;
            if (IsConditionTrue(state)) SetTrue();
        }

        protected bool IsConditionTrue(QuestNodeState state)
        {
            return isNot ? state != requiredState
                : state == requiredState;
        }

        public override bool Recheck()
        {
            alreadyTrue = IsConditionTrue(QuestMachine.GetQuestNodeState(questIDToCheck, requiredQuestNodeID));
            return alreadyTrue;
        }

    }

}
