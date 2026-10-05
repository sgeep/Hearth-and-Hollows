using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The short black fade between the property's areas (4f step 6): out as the keeper takes the stairs or the guest
    /// room's door (<see cref="AreaPassageStarted"/>), back in once they're through (<see cref="AreaPassageEnded"/>).
    /// Presentation only.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class AreaFadeView : MonoBehaviour
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
            EventBus<AreaPassageStarted>.Subscribe(OnStarted);
            EventBus<AreaPassageEnded>.Subscribe(OnEnded);
        }

        void OnDisable()
        {
            EventBus<AreaPassageStarted>.Unsubscribe(OnStarted);
            EventBus<AreaPassageEnded>.Unsubscribe(OnEnded);
        }

        void OnStarted(AreaPassageStarted e) => FadeTo(1f, e.FadeSeconds);
        void OnEnded(AreaPassageEnded e) => FadeTo(0f, e.FadeSeconds);

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
