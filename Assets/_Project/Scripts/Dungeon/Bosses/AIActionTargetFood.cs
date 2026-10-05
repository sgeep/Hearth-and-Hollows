using MoreMountains.Tools;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>TDE AI action (4e step 2): makes the part the <see cref="ScrapEater"/> wants the brain's target, so the path action walks to it.</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Action Target Food")]
    public sealed class AIActionTargetFood : AIAction
    {
        public ScrapEater Eater;

        public override void PerformAction()
        {
            if (Eater != null && !Eater.FoodGone) _brain.Target = Eater.Food.transform;
        }
    }
}
