using UnityEngine;

namespace Hearthdelve.Shared.Quests
{
    /// <summary>
    /// A quest object (4g Checkpoint B, D6): something a quest sends the keeper into the Hollows for. Never an ingredient: it takes
    /// no satchel slot and can't be kept in the Lockbox. Its policy is data, so later objects can differ; Boog's bomb is lost with
    /// a death and turns up again on a later delve while the quest wants it. Its name is the UI table's <c>quest_object.&lt;id&gt;</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Quest Object", fileName = "QuestObject_")]
    public sealed class QuestObjectDefinition : ScriptableObject
    {
        [Tooltip("Stable id: saves, facts, dialogue (HH_HasQuestObject) and the quest's messages use it.")]
        public string id;
        [Tooltip("Its animation on the floor of the Hollows.")]
        public Sprite[] frames = System.Array.Empty<Sprite>();
        [Min(0.02f)] public float frameSeconds = 0.1f;
        [Tooltip("The Quest Machine quest that wants it (its id): giving the quest wants the object.")]
        public string questId;

        [Header("Where it turns up")]
        [Min(1), Tooltip("The floor of the Cellars it's found on.")]
        public int floor = 1;
        [Min(1), Tooltip("It lies in the room cleared this many fights into that floor: reliable, never a random drop.")]
        public int afterRoomsCleared = 2;

        [Header("Policy")]
        [Tooltip("Lost if the keeper dies before extraction (otherwise it comes home anyway).")]
        public bool lostOnDeath = true;
        [Tooltip("Turns up again on a later delve while the quest still wants it.")]
        public bool offeredAgain = true;

        [Header("Delivered")]
        [Min(0), Tooltip("Gold given when it's handed over.")]
        public int rewardGold = 60;
    }
}
