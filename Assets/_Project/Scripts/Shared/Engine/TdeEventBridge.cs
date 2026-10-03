using Hearthdelve.Core.Events;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// The one place TopDown Engine events enter our code (CLAUDE.md, TDE boundary): damage,
    /// death, revive and level start are republished on the <see cref="EventBus{T}"/> as
    /// engine-neutral events. One per scene.
    /// </summary>
    public sealed class TdeEventBridge : MonoBehaviour,
        MMEventListener<MMDamageTakenEvent>,
        MMEventListener<MMLifeCycleEvent>,
        MMEventListener<TopDownEngineEvent>
    {
        // TDE raises the damage event just before the death it causes, so the last hit is
        // enough to work out the overkill of a kill.
        Health m_LastDamaged;
        float m_LastOverkill;

        void OnEnable()
        {
            this.MMEventStartListening<MMDamageTakenEvent>();
            this.MMEventStartListening<MMLifeCycleEvent>();
            this.MMEventStartListening<TopDownEngineEvent>();
        }

        void OnDisable()
        {
            this.MMEventStopListening<MMDamageTakenEvent>();
            this.MMEventStopListening<MMLifeCycleEvent>();
            this.MMEventStopListening<TopDownEngineEvent>();
        }

        public void OnMMEvent(MMDamageTakenEvent e)
        {
            if (e.AffectedHealth == null) return;
            m_LastDamaged = e.AffectedHealth;
            m_LastOverkill = Mathf.Max(0f, e.DamageCaused - e.PreviousHealth);
            EventBus<CharacterDamaged>.Publish(new CharacterDamaged(
                Root(e.AffectedHealth), e.Instigator, IsPlayer(e.AffectedHealth),
                e.DamageCaused, e.PreviousHealth, Mathf.Max(0f, e.CurrentHealth)));
        }

        public void OnMMEvent(MMLifeCycleEvent e)
        {
            Health health = e.AffectedHealth;
            if (health == null) return;
            GameObject target = Root(health);
            bool isPlayer = IsPlayer(health);

            if (e.MMLifeCycleEventType == MMLifeCycleEventTypes.Death)
            {
                float overkill = health == m_LastDamaged ? m_LastOverkill : 0f;
                m_LastDamaged = null;
                EventBus<CharacterDied>.Publish(new CharacterDied(target, isPlayer, target.transform.position, health.MaximumHealth, overkill));
            }
            else
            {
                EventBus<CharacterRevived>.Publish(new CharacterRevived(target, isPlayer));
            }
        }

        public void OnMMEvent(TopDownEngineEvent e)
        {
            if (e.EventType == TopDownEngineEventTypes.LevelStart) EventBus<LevelStarted>.Publish(new LevelStarted());
        }

        static GameObject Root(Health health)
        {
            var character = health.GetComponentInParent<Character>();
            return character != null ? character.gameObject : health.gameObject;
        }

        static bool IsPlayer(Health health)
        {
            var character = health.GetComponentInParent<Character>();
            return character != null && character.CharacterType == Character.CharacterTypes.Player;
        }
    }
}
