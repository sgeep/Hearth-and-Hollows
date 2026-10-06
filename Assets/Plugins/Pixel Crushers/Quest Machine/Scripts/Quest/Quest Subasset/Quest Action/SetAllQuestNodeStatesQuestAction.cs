// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Sets all quest node states.
    /// </summary>
    public class SetAllQuestNodeStatesQuestAction : QuestAction
    {

        [Tooltip("Quest to set. If assigned, this takes priority over Quest ID. If neither is set, uses this quest.")]
        [SerializeField]
        private Quest m_questToSet;

        [Tooltip("ID of quest. Used if Quest To Set is unassigned. Leave both blank to set this quest's state.")]
        [SerializeField]
        private StringField m_questID;

        [Tooltip("New state to set ALL quest nodes.")]
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
                return "Set All Quest Node States: Quest '" + questToSet.id + "' nodes to " + state;
            }
            else if (!StringField.IsNullOrEmpty(questID))
            {
                return "Set All Quest Node States: Quest '" + questID + "' to " + state;
            }
            else
            {
                return "Set All Quest Node States: " + state;
            }
        }

        public override void Execute()
        {
            var targetQuest = (questToSet != null)
                ? QuestMachine.GetQuestInstance(questToSet.id)
                : !StringField.IsNullOrEmpty(questID)
                    ? QuestMachine.GetQuestInstance(questID)
                    : quest;
            if (targetQuest == null)
            {
                if (QuestMachine.debug) Debug.LogWarning($"Quest Machine: {GetType().Name}: Can't find quest to set all states to {state}");
                return;
            }
            for (int i = targetQuest.nodeList.Count - 1; i >= 0; i--)
            {
                var node = targetQuest.nodeList[i];
                if (QuestMachine.GetQuestNodeState(targetQuest.id, node.id) != state)
                {
                    QuestMachine.SetQuestNodeState(targetQuest.id, node.id, state);
                }
            }
        }

    }

}
