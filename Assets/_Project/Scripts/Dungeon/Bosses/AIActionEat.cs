using MoreMountains.Tools;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>TDE AI action (4e step 2): eats the part in reach; <see cref="AIDecisionDoneEating"/> leaves the state.</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Action Eat")]
    public sealed class AIActionEat : AIAction
    {
        public ScrapEater Eater;

        public override void OnEnterState()
        {
            base.OnEnterState();
            Eater?.BeginEating();
        }

        public override void OnExitState()
        {
            base.OnExitState();
            Eater?.StopEating();
        }

        public override void PerformAction() { }
    }
}
