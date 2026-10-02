using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// Links a TDE enemy character to its <see cref="EnemyDefinition"/>, and applies the
    /// definition's stats to the TDE components so balance stays in the data asset.
    /// </summary>
    public sealed class EnemyIdentity : MonoBehaviour
    {
        [SerializeField] EnemyDefinition m_Definition;

        public EnemyDefinition Definition => m_Definition;

        public void Configure(EnemyDefinition definition) => m_Definition = definition;

        void Awake()
        {
            if (m_Definition == null) return;

            if (TryGetComponent(out Health health))
            {
                health.InitialHealth = m_Definition.maxHealth;
                health.MaximumHealth = m_Definition.maxHealth;
                health.InitializeCurrentHealth();
            }
            if (TryGetComponent(out CharacterMovement movement)) movement.WalkSpeed = m_Definition.moveSpeed;

            var touch = GetComponentInChildren<DamageOnTouch>();
            if (touch != null)
            {
                touch.MinDamageCaused = m_Definition.attack.damage;
                touch.MaxDamageCaused = m_Definition.attack.damage;
            }
        }
    }
}
