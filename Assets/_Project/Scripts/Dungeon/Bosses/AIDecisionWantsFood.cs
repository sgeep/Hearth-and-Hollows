using MoreMountains.Tools;
using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>TDE AI decision (4e step 2): true while the <see cref="ScrapEater"/> wants a part on the floor.</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Decision Wants Food")]
    public sealed class AIDecisionWantsFood : AIDecision
    {
        public ScrapEater Eater;
        [Tooltip("Inverted: true when it no longer wants food (the part was taken).")]
        public bool Invert;

        public override bool Decide()
        {
            bool wants = Eater != null && Eater.WantsFood();
            return Invert ? !wants : wants;
        }
    }
}
