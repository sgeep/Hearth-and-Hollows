using Hearthdelve.Shared.Story;
using UnityEngine;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// A tavern panel (the morning, prep, results and night screens) fades away while someone is talking and comes back when they
    /// stop (4g Checkpoint C): the dialogue box would otherwise cover half of it, and a conversation is better had over the room
    /// than over a ledger. The panel can't be used meanwhile (the conversation has the input anyway).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class StepAsideWhileTalking : MonoBehaviour
    {
        [SerializeField, Min(0.01f), Tooltip("Seconds to fade out or back in (real time: talking pauses the world).")]
        float m_Fade = 0.2f;

        CanvasGroup m_Group;

        public bool SteppedAside => m_Group != null && m_Group.alpha < 0.5f;

        void Awake() => m_Group = GetComponent<CanvasGroup>();

        void OnDisable()
        {
            if (m_Group == null) return;
            m_Group.alpha = 1f;
            m_Group.blocksRaycasts = true;
        }

        void Update()
        {
            bool talking = StoryServices.Conversations != null && StoryServices.Conversations.IsTalking;
            float target = talking ? 0f : 1f;
            m_Group.alpha = Mathf.MoveTowards(m_Group.alpha, target, Time.unscaledDeltaTime / m_Fade);
            m_Group.blocksRaycasts = !talking;
        }
    }
}
