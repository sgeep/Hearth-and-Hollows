// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Sets a quest state.
    /// </summary>
    public class SetQuestStateQuestAction : QuestAction
    {

        [Tooltip("Quest to set. If assigned, this takes priority over Quest ID. If neither is set, uses this quest.")]
        [SerializeField]
        private Quest m_questToSet;

        [Tooltip("ID of quest. Used if Quest To Set is unassigned. Leave both blank to set this quest's state.")]
        [SerializeField]
        private StringField m_questID;

        [Tooltip("New quest state.")]
        [SerializeField]
        private QuestState m_state;

        [Tooltip("Set all quest nodes to equivalent state.")]
        [SerializeField]
        private bool m_setQuestNodesToSame = false;

        /// <summary>
        /// Quest to set. If assigned, this takes priority over Quest ID. If neither is set, uses this quest.
        /// </summary>
        public Quest questToSet
        {
            get { return m_questToSet; } 
            set { m_questToSet = value; }
        }

        /// <summary>
        /// ID of quest. Used if Quest To Set is unassigned. Leave both blank to set this quest's state.
        /// </summary>
        public StringField questID
        {
            get { return (StringField.IsNullOrEmpty(m_questID) && quest != null) ? quest.id : m_questID; }
            set { m_questID = value; }
        }

        public QuestState state
        {
            get { return m_state; }
            set { m_state = value; }
        }

        public bool setQuestNodesToSame
        {
            get { return m_setQuestNodesToSame; }
            set { m_setQuestNodesToSame = value; }
        }

        public StringField questIDToSet
        {
            get { return (questToSet != null) ? questToSet.id : !StringField.IsNullOrEmpty(questID) ? questID : StringField.empty; }
        }

        public override string GetEditorName()
        {
            return (questToSet != null) 
                ? ("Set Quest State: Quest '" + questToSet.id + "' to " + state)
                : !StringField.IsNullOrEmpty(m_questID) 
                    ? ("Set Quest State: Quest '" + m_questID + "' to " + state)
                    : ("Set Quest State: " + state);
        }

        public override void Execute()
        {
            var useThisQuest = questToSet != null && StringField.IsNullOrEmpty(questID) && quest != null;
            if (useThisQuest)
            {
                quest.SetState(state);
            }
            else if (QuestMachine.GetQuestState(questIDToSet) != state)
            {
                QuestMachine.SetQuestState(questIDToSet, state);
            }
            if (setQuestNodesToSame)
            {
                var questToSet = useThisQuest ? quest : QuestMachine.GetQuestInstance(questIDToSet);
                if (questToSet != null)
                {
                    var stateToSet = GetEquivalentQuestNodeState(state);
                    for (int i = 0; i < questToSet.nodeList.Count; i++)
                    {
                        questToSet.nodeList[i].SetStateRaw(stateToSet);
                    }
                }
            }
        }

        private QuestNodeState GetEquivalentQuestNodeState(QuestState state)
        {
            switch (state)
            {
                default:
                case QuestState.WaitingToStart:
                case QuestState.Disabled:
                case QuestState.Abandoned:
                    return QuestNodeState.Inactive;
                case QuestState.Active:
                    return QuestNodeState.Active;
                case QuestState.Successful:
                case QuestState.Failed:
                    return QuestNodeState.True;
            }
        }
    }

}
