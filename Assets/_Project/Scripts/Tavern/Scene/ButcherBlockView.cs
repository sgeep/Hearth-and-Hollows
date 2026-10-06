using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The Butcher Block at work (4f Checkpoint C): the knife chops and the meat moves while the keeper or Gunta is breaking
    /// a part down; at rest otherwise. Presentation only.
    /// </summary>
    public sealed class ButcherBlockView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Renderer;
        [SerializeField] Sprite m_Idle;
        [SerializeField] Sprite[] m_Working = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] float m_FrameSeconds = 0.1f;

        float m_Time;

        public bool IsWorking { get; private set; }

        public void Configure(SpriteRenderer renderer, Sprite idle, Sprite[] working)
        {
            m_Renderer = renderer;
            m_Idle = idle;
            m_Working = working;
        }

        void LateUpdate()
        {
            KeeperWork keeper = KeeperWork.Instance;
            TavernDirector director = TavernDirector.Instance;
            IsWorking = keeper != null && keeper.ButcheringPart.IsValid || director != null && director.CookButchering;
            if (m_Renderer == null) return;
            if (!IsWorking || m_Working.Length == 0)
            {
                m_Renderer.sprite = m_Idle;
                m_Time = 0f;
                return;
            }
            m_Time += Time.deltaTime;
            m_Renderer.sprite = m_Working[(int)(m_Time / m_FrameSeconds) % m_Working.Length];
        }
    }
}
