using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Village
{
    /// <summary>
    /// The day rules a schedule can ask about (4h Checkpoint C): each a pure function of the day and the world seed
    /// (<see cref="VillageDays"/>), so the village is the same on every load of the same day. Kept to what the cast needs.
    /// </summary>
    public enum DayRule
    {
        /// <summary>Kaloren brings Ogrin his herbs (locked canon: once every three days).</summary>
        HerbDay,
        /// <summary>Ogrin is well enough to be out (seeded; the day after the herbs leans well, and nothing cures him).</summary>
        OgrinWell,
        /// <summary>Maximo keeps an evening vigil at Karias's memorial (seeded, now and then).</summary>
        MaximoVigil,
        /// <summary>4h Checkpoint D: Gimp comes up to see Boog (seeded and irregular: never two days running).</summary>
        GimpVisit,
        /// <summary>4h Checkpoint D: a small light at Ogrin's window this evening (seeded, now and then; unexplained).</summary>
        GlimmerEvening,
    }

    public enum ScheduleConditionKind
    {
        /// <summary>A <see cref="DayRule"/> holds today.</summary>
        Day,
        /// <summary>The game day is at least <see cref="ScheduleCondition.number"/>.</summary>
        DayAtLeast,
        /// <summary>The opening (arrival day) is over.</summary>
        OpeningComplete,
        /// <summary>A quest object (<see cref="ScheduleCondition.id"/>) is in a status (<see cref="ScheduleCondition.status"/>: wanted, home, delivered, none).</summary>
        QuestObject,
        /// <summary>4h Checkpoint D: a one-time story beat (<see cref="ScheduleCondition.id"/>, e.g. beat:gimp_intro) has happened.</summary>
        Beat,
    }

    /// <summary>One condition on a block. A block's conditions combine with AND; <see cref="negate"/> turns one round.</summary>
    [Serializable]
    public struct ScheduleCondition
    {
        public ScheduleConditionKind kind;
        public DayRule rule;
        [Tooltip("DayAtLeast: the day.")] public int number;
        [Tooltip("QuestObject: the object's id.")] public string id;
        [Tooltip("QuestObject: wanted, home, delivered or none.")] public string status;
        public bool negate;

        public static ScheduleCondition On(DayRule rule) => new() { kind = ScheduleConditionKind.Day, rule = rule };
        public static ScheduleCondition NotOn(DayRule rule) => new() { kind = ScheduleConditionKind.Day, rule = rule, negate = true };
        public static ScheduleCondition FromDay(int day) => new() { kind = ScheduleConditionKind.DayAtLeast, number = day };
        public static ScheduleCondition After(string beat) => new() { kind = ScheduleConditionKind.Beat, id = beat };
    }

    /// <summary>
    /// One broad beat of someone's day (4h Checkpoint C): from a minute (inclusive) to a minute (exclusive), at an anchor, doing
    /// something. The first block whose time and conditions match wins, so a special day's block goes before the ordinary one.
    /// </summary>
    [Serializable]
    public sealed class ScheduleBlock
    {
        [Tooltip("Minutes of the day (8:00 = 480).")] public int from;
        [Tooltip("Minutes of the day, exclusive (24:00 = 1440: the rest of the day, past the 5 PM stop).")] public int to = 24 * 60;
        [Tooltip("A ScheduleAnchor's id in Kariaston or Tally Ho! (empty: indoors, not seen).")] public string anchor;
        [Tooltip("What they're doing there (a word the presentation and conversations read: HH_Doing).")] public string activity;
        public List<ScheduleCondition> conditions = new();

        public ScheduleBlock() { }

        public ScheduleBlock(int from, int to, string anchor, string activity, params ScheduleCondition[] conditions)
        {
            this.from = from;
            this.to = to;
            this.anchor = anchor;
            this.activity = activity;
            this.conditions = new List<ScheduleCondition>(conditions);
        }

        public bool Covers(int minute) => minute >= from && minute < to;

        public override string ToString() => $"{from / 60:00}:{from % 60:00}-{to / 60:00}:{to % 60:00} {anchor} ({activity})";
    }

    /// <summary>
    /// Where one character spends the surface day (4h Checkpoint C): a few broad authored beats, never a simulation. Positions are
    /// derived from the day, the minute, the story and the world seed (<see cref="ScheduleRules"/>), so nothing here is saved.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Village/Schedule", fileName = "Schedule")]
    public sealed class ScheduleDefinition : ScriptableObject
    {
        [Tooltip("The character's stable id (CharacterIds).")] public string character;
        [Tooltip("In priority order: the first block whose time and conditions match is where they are.")]
        public List<ScheduleBlock> blocks = new();
    }
}
