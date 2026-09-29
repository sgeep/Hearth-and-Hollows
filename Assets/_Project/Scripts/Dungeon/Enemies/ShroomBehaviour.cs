using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// Cellar Shroom — stationary. Telegraph: swells and glows; attack: lobs a spore blob in
    /// an arc at where the player stood when the telegraph began. Dodge or step aside.
    /// </summary>
    public sealed class ShroomBehaviour : EnemyController
    {
        [SerializeField] SporeProjectile m_ProjectilePrefab;
        Vector2 m_AimPoint;

        public void ConfigureProjectile(SporeProjectile prefab) => m_ProjectilePrefab = prefab;

        protected override void OnTelegraphStart(Transform player)
        {
            m_AimPoint = player != null ? (Vector2)player.position : Mover.Position + new Vector2(m_Facing * 3f, 0f);
            FaceTowards(m_AimPoint.x - Mover.Position.x);
            SetSwell(1.25f);
        }

        protected override void OnActiveStart(Transform player)
        {
            SetSwell(0.9f);
            if (m_ProjectilePrefab == null) return;
            var origin = Mover.Position + new Vector2(Attack.projectileSpawnOffset.x * m_Facing, Attack.projectileSpawnOffset.y);
            var spore = Instantiate(m_ProjectilePrefab, origin, Quaternion.identity);
            spore.Launch(origin, m_AimPoint + Vector2.up * 0.5f, Attack.projectileFlightTime, Attack.damage, m_PlayerMask, gameObject);
        }

        protected override void OnAttackOver() => SetSwell(1f);
        protected override void OnInterrupted() => SetSwell(1f);

        void SetSwell(float s)
        {
            if (m_Body == null) return;
            float sign = Mathf.Sign(m_Body.transform.localScale.x);
            m_Body.transform.localScale = new Vector3(s * sign, s, 1f);
        }
    }
}
