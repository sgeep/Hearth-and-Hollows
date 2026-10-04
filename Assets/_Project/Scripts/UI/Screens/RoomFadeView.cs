using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The black cover between rooms (4d): it fades in as the run leaves a room (<see cref="RoomTransitionStarted"/>)
    /// and out once the next room is loaded (<see cref="RoomEntered"/>). Presentation only; the room runner times
    /// the transition.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class RoomFadeView : MonoBehaviour
    {
        CanvasGroup m_Group;
        Coroutine m_Fade;

        public float Alpha => m_Group != null ? m_Group.alpha : 0f;

        void Awake()
        {
            m_Group = GetComponent<CanvasGroup>();
            m_Group.alpha = 0f;
            m_Group.blocksRaycasts = false;
            m_Group.interactable = false;
        }

        void OnEnable()
        {
            EventBus<RoomTransitionStarted>.Subscribe(OnLeaving);
            EventBus<RoomEntered>.Subscribe(OnEntered);
        }

        void OnDisable()
        {
            EventBus<RoomTransitionStarted>.Unsubscribe(OnLeaving);
            EventBus<RoomEntered>.Unsubscribe(OnEntered);
        }

        void OnLeaving(RoomTransitionStarted e) => FadeTo(1f, e.FadeSeconds);
        void OnEntered(RoomEntered e) => FadeTo(0f, e.FadeSeconds);

        void FadeTo(float target, float seconds)
        {
            if (m_Fade != null) StopCoroutine(m_Fade);
            m_Fade = null;
            if (seconds <= 0f || !isActiveAndEnabled) m_Group.alpha = target;
            else m_Fade = StartCoroutine(Fade(target, seconds));
        }

        IEnumerator Fade(float target, float seconds)
        {
            float from = m_Group.alpha, started = Time.unscaledTime;
            while (Time.unscaledTime - started < seconds)
            {
                m_Group.alpha = Mathf.Lerp(from, target, (Time.unscaledTime - started) / seconds);
                yield return null;
            }
            m_Group.alpha = target;
            m_Fade = null;
        }
    }
}
