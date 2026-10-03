using Hearthdelve.Shared.Animation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>TDE AI action: stays still and asleep (the bat hanging); wakes with its wake animation on leaving the state.</summary>
    [AddComponentMenu("Hearthdelve/AI/AI Action Sleep")]
    public sealed class AIActionSleep : AIAction
    {
        CharacterMovement m_Movement;
        CharacterSpriteAnimator m_Animator;

        public bool IsAsleep { get; private set; }

        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            var character = GetComponentInParent<Character>();
            m_Movement = character != null ? character.FindAbility<CharacterMovement>() : null;
            m_Animator = character != null ? character.GetComponentInChildren<CharacterSpriteAnimator>() : null;
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            IsAsleep = true;
            m_Animator?.Hold(CharacterAnim.Sleep);
        }

        public override void PerformAction() => m_Movement?.SetMovement(Vector2.zero);

        public override void OnExitState()
        {
            base.OnExitState();
            IsAsleep = false;
            m_Animator?.Release();
            m_Animator?.PlayOneShot(CharacterAnim.Wake);
        }
    }
}
