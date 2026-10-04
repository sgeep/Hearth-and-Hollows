using Hearthdelve.Shared.Ingredients;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// What clearing a room gives (4d step 3). The door to the room shows the kind; what exactly (which ingredient, how
    /// much Gold) is rolled when the run is generated and revealed when the room is clear.
    /// <para>
    /// Extension point: later kinds (Delve Marks and weapons in 4e; furnishing discoveries in 4f;
    /// quest objects for villagers' errands) are new values here, with their payload in <see cref="ItemId"/> and
    /// <see cref="Amount"/>, a door sign, and a case in the room runner's grant. The graph, room clearing, the door
    /// previews and the run report don't change.
    /// </para>
    /// </summary>
    public enum RewardKind
    {
        None,
        /// <summary>A dungeon ingredient: a monster part of better quality than a kill usually gives.</summary>
        Ingredient,
        /// <summary>Unbanked run Gold: kept on extraction, lost on death.</summary>
        Gold,
        /// <summary>A run power, chosen one of three when the room is clear (step 4). The offer is drawn then, from the run's seed.</summary>
        Power,
    }

    /// <summary>A room's reward: its kind and payload.</summary>
    public readonly struct RoomReward
    {
        public readonly RewardKind Kind;
        /// <summary>What it is, by id: an ingredient (later a discovery or a quest object). Empty for Gold.</summary>
        public readonly string ItemId;
        public readonly Quality Quality;
        /// <summary>How many parts, or how much Gold.</summary>
        public readonly int Amount;

        public RoomReward(RewardKind kind, string itemId, Quality quality, int amount)
        {
            Kind = kind;
            ItemId = itemId ?? "";
            Quality = quality;
            Amount = amount;
        }

        public static RoomReward None => new(RewardKind.None, "", Quality.Standard, 0);
        public static RoomReward Gold(int amount) => new(RewardKind.Gold, "", Quality.Standard, amount);
        public static RoomReward Ingredient(string id, Quality quality, int count) => new(RewardKind.Ingredient, id, quality, count);
        public static RoomReward Power() => new(RewardKind.Power, "", Quality.Standard, 1);

        public override string ToString() => Kind switch
        {
            RewardKind.Gold => $"Gold {Amount}",
            RewardKind.Ingredient => $"{ItemId} {Quality} x{Amount}",
            RewardKind.Power => "power",
            _ => "-",
        };
    }
}
