using MoreMountains.Tools;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>TDE AI decision (4e step 2): the meal is over (eaten, spoiled, or the part was taken).</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Decision Done Eating")]
    public sealed class AIDecisionDoneEating : AIDecision
    {
        public ScrapEater Eater;

        public override bool Decide() => Eater == null || !Eater.IsEating;
    }
}
