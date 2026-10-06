using System;
using System.Collections.Generic;

namespace Hearthdelve.Shared.Quests
{
    /// <summary>Where a quest object is (saved by name).</summary>
    public enum QuestObjectStatus
    {
        /// <summary>No quest wants it.</summary>
        None,
        /// <summary>A quest wants it: it turns up in the Hollows until it's brought home.</summary>
        Wanted,
        /// <summary>Brought home (extracted): persistent quest progress.</summary>
        Home,
        /// <summary>Handed over: the quest's part is done.</summary>
        Delivered,
    }

    /// <summary>The quest objects in play and where each is (4g Checkpoint B). Part of <c>GameState</c>, saved.</summary>
    public sealed class QuestObjectLedger
    {
        readonly Dictionary<string, QuestObjectStatus> m_Status = new();

        public IReadOnlyDictionary<string, QuestObjectStatus> All => m_Status;

        public QuestObjectStatus Status(string id) => id != null && m_Status.TryGetValue(id, out QuestObjectStatus s) ? s : QuestObjectStatus.None;

        public bool IsWanted(string id) => Status(id) == QuestObjectStatus.Wanted;
        public bool IsHome(string id) => Status(id) == QuestObjectStatus.Home;

        /// <summary>A quest now wants it (no change once it's home or delivered).</summary>
        public bool Want(string id)
        {
            if (string.IsNullOrEmpty(id) || Status(id) is QuestObjectStatus.Home or QuestObjectStatus.Delivered or QuestObjectStatus.Wanted) return false;
            m_Status[id] = QuestObjectStatus.Wanted;
            return true;
        }

        /// <summary>Extracted with it: home for good.</summary>
        public bool BringHome(string id)
        {
            if (Status(id) != QuestObjectStatus.Wanted) return false;
            m_Status[id] = QuestObjectStatus.Home;
            return true;
        }

        /// <summary>Handed over to whoever wanted it.</summary>
        public bool Deliver(string id)
        {
            if (Status(id) != QuestObjectStatus.Home) return false;
            m_Status[id] = QuestObjectStatus.Delivered;
            return true;
        }

        /// <summary>Restoring a save.</summary>
        public void Set(string id, QuestObjectStatus status)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (status == QuestObjectStatus.None) m_Status.Remove(id);
            else m_Status[id] = status;
        }
    }

    /// <summary>The quest-object rules (pure, EditMode-tested).</summary>
    public static class QuestObjectRules
    {
        /// <summary>
        /// Whether a cleared room gets the object: the quest wants it, it isn't already carried this delve, this is its floor, and
        /// this is the fight cleared the right number of rooms into the floor (reliable, whichever way the keeper goes).
        /// </summary>
        public static bool PlaceHere(QuestObjectDefinition definition, QuestObjectLedger ledger, int floor, int fightsClearedOnFloor, bool carried) =>
            definition != null && ledger != null && !carried && ledger.IsWanted(definition.id) && floor == definition.floor &&
            fightsClearedOnFloor == definition.afterRoomsCleared;

        /// <summary>
        /// The delve ended: objects carried come home on extraction; on a death they're lost (if their policy says so, else they
        /// come home anyway). A lost object stays wanted, so it turns up on a later delve. Returns those brought home.
        /// </summary>
        public static List<string> EndDelve(QuestObjectLedger ledger, IEnumerable<string> carried, bool extracted, Func<string, QuestObjectDefinition> definition)
        {
            var home = new List<string>();
            if (ledger == null || carried == null) return home;
            foreach (string id in carried)
            {
                QuestObjectDefinition d = definition?.Invoke(id);
                bool kept = extracted || (d != null && !d.lostOnDeath);
                if (kept && ledger.BringHome(id)) home.Add(id);
            }
            return home;
        }
    }
}
