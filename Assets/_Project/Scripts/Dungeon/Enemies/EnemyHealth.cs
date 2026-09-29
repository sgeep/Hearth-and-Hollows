using System;
using Hearthdelve.Dungeon.Combat;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>Enemy hit receiver. Runs damage through <see cref="DamageCalculator"/> and reports kills.</summary>
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        public float Max { get; private set; } = 1f;
        public float Current { get; private set; } = 1f;
        public bool IsDead { get; private set; }
        public Team Team => Team.Enemy;

        public event Action<DamageInfo, DamageResult> Damaged;
        public event Action<DamageInfo, DamageResult> Died;

        public void Initialize(float maxHealth)
        {
            Max = Mathf.Max(1f, maxHealth);
            Current = Max;
            IsDead = false;
        }

        public void RefillToMax() => Current = Max;

        public bool ReceiveHit(in DamageInfo hit)
        {
            if (IsDead || !isActiveAndEnabled) return false;
            var result = DamageCalculator.Apply(Current, hit.Amount);
            Current = result.RemainingHealth;
            if (result.Killed)
            {
                IsDead = true;
                Died?.Invoke(hit, result);
            }
            else
            {
                Damaged?.Invoke(hit, result);
            }
            return true;
        }
    }
}
