using System.Collections.Generic;
using PixelCrushers;
using PixelCrushers.QuestMachine;
using UnityEngine;

namespace Hearthdelve.Story.Quests
{
    /// <summary>
    /// Hearth &amp; Hollows' only door to Quest Machine (4g). Quest Machine owns quest state, objectives and completion; this gives
    /// quests to the player's journal (on the persistent story host, so it survives every scene change), tells quests what
    /// happened in play as Quest Machine messages (<see cref="FactMessage"/>, from the gameplay facts, never from Quest Machine
    /// reading our objects), and records the journal into the game's save. Rewards are always granted by Hearth &amp; Hollows code,
    /// never by a quest action: Quest Machine doesn't re-run actions on load.
    /// </summary>
    public sealed class QuestAdapter
    {
        /// <summary>The message every gameplay fact is sent as; its parameter is the fact's name, its value the fact's id.</summary>
        public const string FactMessage = "HH Fact";

        readonly QuestJournal m_Journal;

        public QuestAdapter(QuestJournal journal)
        {
            m_Journal = journal;
            if (m_Journal == null) return;
            m_Journal.includeInSavedGameData = true;
            // JSON survives later edits to a quest's nodes better than Quest Machine's binary form.
            m_Journal.saveQuestsToJson = true;
        }

        public QuestJournal Journal => m_Journal;

        /// <summary>Gives the player a quest from the quest database, on behalf of <paramref name="giverId"/> (a character id). False if it's unknown or already in the journal.</summary>
        public bool Give(string questId, string giverId)
        {
            if (m_Journal == null || string.IsNullOrEmpty(questId) || m_Journal.FindQuest(questId) != null) return false;
            Quest asset = QuestMachine.GetQuestAsset(questId);
            if (asset == null)
            {
                Debug.LogWarning($"[Hearthdelve] No quest '{questId}' in the quest database.");
                return false;
            }
            Quest quest = asset.Clone();
            quest.questGiverID = new StringField(giverId ?? string.Empty);
            quest = m_Journal.AddQuest(quest);
            if (quest == null) return false;
            quest.SetState(QuestState.Active);
            return true;
        }

        /// <summary>The quest's state, by Quest Machine's names in lower case ("active", "successful"); "unassigned" if the player hasn't got it.</summary>
        public string State(string questId)
        {
            Quest quest = m_Journal != null && !string.IsNullOrEmpty(questId) ? m_Journal.FindQuest(questId) : null;
            return quest == null ? "unassigned" : Name(quest.GetState());
        }

        /// <summary>The names conversations compare against (authored lower case, never converted).</summary>
        public static string Name(QuestState state) => state switch
        {
            QuestState.WaitingToStart => "waiting",
            QuestState.Active => "active",
            QuestState.Successful => "successful",
            QuestState.Failed => "failed",
            QuestState.Abandoned => "abandoned",
            _ => "disabled",
        };

        public bool Has(string questId) => m_Journal != null && !string.IsNullOrEmpty(questId) && m_Journal.FindQuest(questId) != null;

        /// <summary>A gameplay fact, as a Quest Machine message for any quest node listening for it.</summary>
        public void Fact(string fact, string id) => MessageSystem.SendMessage(this, FactMessage, fact, id ?? string.Empty);

        /// <summary>The journal as Quest Machine records it (its JSON), for the game's save.</summary>
        public string Record() => m_Journal != null ? m_Journal.RecordData() : string.Empty;

        public void Apply(string data, List<string> warnings = null)
        {
            Clear();
            if (m_Journal != null && !string.IsNullOrEmpty(data)) m_Journal.ApplyData(WithoutRetired(data, warnings));
        }

        /// <summary>
        /// The journal's data without quests the game no longer has (4g Checkpoint A's proof quest, retired in Checkpoint B): Quest
        /// Machine would report each as an error. A retired quest has nothing to hand on; it's dropped with a warning.
        /// </summary>
        static string WithoutRetired(string data, List<string> warnings)
        {
            QuestListContainer.SaveData saved;
            try
            {
                saved = SaveSystem.Deserialize<QuestListContainer.SaveData>(data);
            }
            catch (System.Exception)
            {
                return data;
            }
            if (saved == null) return data;
            bool dropped = false;
            for (int i = saved.staticQuestIds.Count - 1; i >= 0; i--)
            {
                string id = saved.staticQuestIds[i];
                if (string.IsNullOrEmpty(id) || QuestMachine.GetQuestAsset(id) != null) continue;
                saved.staticQuestIds.RemoveAt(i);
                if (i < saved.staticQuestJsonData.Count) saved.staticQuestJsonData.RemoveAt(i);
                if (i < saved.staticQuestData.Count) saved.staticQuestData.RemoveAt(i);
                warnings?.Add($"the quest '{id}' is no longer in the game; dropped from the journal");
                dropped = true;
            }
            return dropped ? SaveSystem.Serialize(saved) : data;
        }

        /// <summary>An empty journal: a new game, or the menu.</summary>
        public void Clear()
        {
            if (m_Journal == null) return;
            foreach (Quest quest in new List<Quest>(m_Journal.questList))
                if (quest != null) m_Journal.DeleteQuest(quest);
            // Deleting marks a database quest "deleted"; a fresh journal has deleted nothing.
            m_Journal.deletedStaticQuests.Clear();
        }
    }
}
