using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>TDE AI decision: true when an <see cref="EnemyAttack"/> can start against the brain's target.</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Decision Enemy Attack Ready")]
    public sealed class AIDecisionEnemyAttackReady : AIDecision
    {
        public EnemyAttack Attack;

        public override bool Decide() => Attack != null && Attack.CanStart(_brain.Target);
    }
}
