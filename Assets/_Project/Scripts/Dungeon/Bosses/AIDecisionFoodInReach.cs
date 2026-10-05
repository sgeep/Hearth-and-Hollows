using MoreMountains.Tools;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>TDE AI decision (4e step 2): the part it's walking to is close enough to eat.</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Decision Food In Reach")]
    public sealed class AIDecisionFoodInReach : AIDecision
    {
        public ScrapEater Eater;

        public override bool Decide() => Eater != null && Eater.InReach;
    }
}
