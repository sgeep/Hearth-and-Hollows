// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Sets a quest node state.
    /// </summary>
    public class SetQuestNodeStateQuestAction : QuestAction
    {

        [Tooltip("Quest to set. If assigned, this takes priority over Quest ID. If neither is set, uses this quest.")]
        [SerializeField]
        private Quest m_questToSet;

        [Tooltip("ID of quest. Used if Quest To Set is unassigned. Leave both blank to set this quest's state.")]
        [SerializeField]
        private StringField m_questID;

        [Tooltip("ID of quest node. Leave blank to set this quest node's state.")]
        [SerializeField]
        private StringField m_questNodeID;

        [Tooltip("New quest node state.")]
        [SerializeField]
        private QuestNodeState m_state;

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

        /// <summary>
        /// ID of quest node. Leave blank to set this quest node's state.
        /// </summary>
        public StringField questNodeID
        {
            get { return (StringField.IsNullOrEmpty(m_questNodeID) && questNode != null) ? questNode.id : m_questNodeID; }
            set { m_questNodeID = value; }
        }

        public QuestNodeState state
        {
            get { return m_state; }
            set { m_state = value; }
        }

        public StringField questIDToSet
        {
            get { return (questToSet != null) ? questToSet.id : !StringField.IsNullOrEmpty(questID) ? questID : StringField.empty; }
        }

        public override string GetEditorName()
        {
            if (questToSet != null)
            {
                if (!StringField.IsNullOrEmpty(questNodeID))
                {
                    return "Set Quest Node State: Quest '" + questToSet.id + "' Node '" + questNodeID + "' to " + state;
                }
                else
                {
                    return "Set Quest Node State: Quest '" + questToSet.id + "' Node (unspecified) to " + state;
                }
            }
            else if (!StringField.IsNullOrEmpty(m_questID))
            {
                if (!StringField.IsNullOrEmpty(questNodeID))
                {
                    return "Set Quest Node State: Quest '" + m_questID + "' Node '" + questNodeID + "' to " + state;
                }
                else
                {
                    return "Set Quest Node State: Quest '" + m_questID + "' Node (unspecified) to " + state;
                }
            }
            else
            {
                if (!StringField.IsNullOrEmpty(questID))
                {
                    return "Set Quest Node State: '" + questNodeID + "' to " + state;
                }
                else
                {
                    return "Set Quest Node State: " + state;
                }
            }
        }

        public override void Execute()
        {
            if (QuestMachine.GetQuestNodeState(questIDToSet, questNodeID) != state)
            {
                QuestMachine.SetQuestNodeState(questIDToSet, questNodeID, state);
            }
        }

    }

}
