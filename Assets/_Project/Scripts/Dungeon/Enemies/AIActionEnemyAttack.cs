using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// TDE AI action: starts an <see cref="EnemyAttack"/> against the brain's target when the state is
    /// entered. The attack runs itself; <see cref="AIDecisionEnemyAttackDone"/> leaves the state.
    /// </summary>
    [AddComponentMenu("Hearthdelve/AI/AI Action Enemy Attack")]
    public sealed class AIActionEnemyAttack : AIAction
    {
        public EnemyAttack Attack;

        public override void OnEnterState()
        {
            base.OnEnterState();
            if (Attack != null) Attack.Begin(_brain.Target);
        }

        public override void PerformAction() { }
    }
}
