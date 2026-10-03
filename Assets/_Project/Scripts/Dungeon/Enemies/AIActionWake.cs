using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// TDE AI action: wakes up in place. A hanging bat lets go of its wall (<see cref="EnemyPerch.Detach"/>)
    /// and plays its unfold animation; the brain moves on to the chase once the state's time is up.
    /// </summary>
    [AddComponentMenu("Hearthdelve/AI/AI Action Wake")]
    public sealed class AIActionWake : AIAction
    {
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;
        EnemyPerch m_Perch;

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            var character = GetComponentInParent<Character>();
            m_Movement = character != null ? character.FindAbility<CharacterMovement>() : null;
            m_Animator = character != null ? character.GetComponentInChildren<CharacterSpriteAnimator>() : null;
            m_Perch = character != null ? character.GetComponent<EnemyPerch>() : null;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            if (m_Perch != null && m_Perch.IsPerched) m_Perch.Detach();
            else if (m_Perch == null) m_Animator?.PlayOneShot(CharacterAnim.Wake);
        }

        public override void PerformAction() => m_Movement?.SetMovement(Vector2.zero);
    }
}
