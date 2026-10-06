// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Quest condition that becomes true when another quest reaches a specified state.
    /// </summary>
    [Serializable]
    public class QuestStateQuestCondition : QuestCondition, IMessageHandler
    {

        [Tooltip("Quest to monitor. If set, this takes priority over Required Quest ID. If neither is set, uses this quest.")]
        [SerializeField]
        private Quest m_requiredQuest;

        [Tooltip("ID of quest to monitor. Used if Required Quest is unassigned. If neither is set, uses this quest.")]
        [SerializeField]
        private StringField m_requiredQuestID;

        [Tooltip("Quest must not be the required state.")]
        [SerializeField]
        private bool m_isNot = false;

        [Tooltip("Required quest state.")]
        [SerializeField]
        private QuestState m_requiredState;

        /// <summary>
        /// Quest to monitor. If set, this takes priority over Required Quest ID. If neither is set, uses this quest.
        /// </summary>
        public Quest requiredQuest
        {
            get { return m_requiredQuest; }
            set { m_requiredQuest = value; }
        }

        /// <summary>
        /// ID of quest to monitor. Used if Required Quest is unassigned. If neither is set, uses this quest.
        /// </summary>
        public StringField requiredQuestID
        {
            get { return (StringField.IsNullOrEmpty(m_requiredQuestID) && quest != null) ? quest.id : m_requiredQuestID; }
            set { m_requiredQuestID = value; }
        }

        /// <summary>
        /// If true, quest must not be the required state.
        /// </summary>
        public bool isNot
        {
            get { return m_isNot; }
            set { m_isNot = value; }
        }

        public StringField questIDToCheck
        {
            get { return (requiredQuest != null) ? requiredQuest.id : requiredQuestID; }
        }

        /// <summary>
        /// State that monitored quest must be in.
        /// </summary>
        public QuestState requiredState
        {
            get { return m_requiredState; }
            set { m_requiredState = value; }
        }

        public override string GetEditorName()
        {
            return (requiredQuest != null)
                ? ("Quest State: " + requiredQuest.id + (isNot ? " != " : " == ") + requiredState)
                : StringField.IsNullOrEmpty(m_requiredQuestID) ? 
                    ("Quest State: " + (isNot ? "!= " : "== ") + requiredState) : 
                    ("Quest State: " + m_requiredQuestID.value + (isNot ? " != " : " == ") + requiredState);
        }

        public override void StartChecking(System.Action trueAction)
        {
            base.StartChecking(trueAction);
            if (IsConditionTrue(QuestMachine.GetQuestState(questIDToCheck)))
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
            if (!isChecking || messageArgs.values == null || messageArgs.values.Length < 2 || 
                (requiredQuest == null && requiredQuestID == null)) return;
            var questID = messageArgs.parameter;
            var isThisQuest = StringField.Equals(questIDToCheck, questID);
            if (!isThisQuest) return;
            var questNodeID = QuestMachineMessages.ArgToString(messageArgs.values[0]);
            if (!string.IsNullOrEmpty(questNodeID)) return;
            var stateValue = messageArgs.values[1];
            var state = (stateValue != null && stateValue.GetType() == typeof(QuestState)) ? (QuestState)stateValue : QuestState.WaitingToStart;
            if (IsConditionTrue(state)) SetTrue();
        }

        protected bool IsConditionTrue(QuestState state)
        {
            return isNot ? state != requiredState
                : state == requiredState;
        }

        public override bool Recheck()
        {
            alreadyTrue = IsConditionTrue(QuestMachine.GetQuestState(questIDToCheck));
            return alreadyTrue;
        }

    }

}
