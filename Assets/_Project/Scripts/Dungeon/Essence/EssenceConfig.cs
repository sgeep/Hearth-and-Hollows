using UnityEngine;

namespace Hearthdelve.Dungeon.Essence
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Essence", fileName = "EssenceConfig")]
    public sealed class EssenceConfig : ScriptableObject
    {
        public EssenceSettings essence = EssenceSettings.Default;

        [Header("Getting Hit")]
        [Min(0), Tooltip("Invulnerability after taking a hit, in seconds.")]
        public float postHitInvulnerability = 0.6f;
        [Min(0), Tooltip("Control is locked this long while knocked back.")]
        public float hitStunTime = 0.2f;
        public Vector2 hitKnockback = new(6f, 7f);
        [Min(0)] public float hitStop = 0.08f;
        [Min(0)] public float hitScreenShake = 0.35f;

        [Header("Low Essence Warning")]
        [Min(0.05f), Tooltip("Seconds between heartbeats just below the low threshold.")]
        public float lowWarningSlowInterval = 1.1f;
        [Min(0.05f), Tooltip("Seconds between heartbeats as Essence nears zero.")]
        public float lowWarningFastInterval = 0.45f;
    }
}
