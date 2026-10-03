using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>TDE AI decision: true once an <see cref="EnemyAttack"/> is no longer under way (finished or interrupted).</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Decision Enemy Attack Done")]
    public sealed class AIDecisionEnemyAttackDone : AIDecision
    {
        public EnemyAttack Attack;

        public override bool Decide() => Attack == null || !Attack.IsAttacking;
    }
}
