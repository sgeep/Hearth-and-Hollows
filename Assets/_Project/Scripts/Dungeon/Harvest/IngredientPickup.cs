using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>A harvested part lying in the world. Physics-driven so it pops out of the kill.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class IngredientPickup : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Renderer;
        [SerializeField] SpriteRenderer m_QualityPip;
        [SerializeField, Min(0), Tooltip("Can't be collected until this long after spawning.")]
        float m_PickupDelay = 0.35f;

        Rigidbody2D m_Body;
        float m_Age;

        public IngredientItem Item { get; private set; }
        public int Count { get; set; }
        public bool IsCollectable => m_Age >= m_PickupDelay && Count > 0 && !m_WaitingForPlayerToLeave;

        const float k_LeaveDistance = 2f;
        bool m_WaitingForPlayerToLeave;

        /// <summary>
        /// For a stack the player just discarded from the swap prompt: don't auto-collect it
        /// until they've stepped away, or it would jump straight back into the satchel.
        /// </summary>
        public void WaitForPlayerToLeave() => m_WaitingForPlayerToLeave = true;

        public void Configure(SpriteRenderer renderer, SpriteRenderer qualityPip)
        {
            m_Renderer = renderer;
            m_QualityPip = qualityPip;
        }

        void Awake() => m_Body = GetComponent<Rigidbody2D>();

        public void Initialize(IngredientItem item, int count, Vector2 velocity)
        {
            Item = item;
            Count = count;
            m_Age = 0f;
            m_Body.linearVelocity = velocity;

            var def = item.Definition;
            if (m_Renderer != null)
            {
                if (def.icon != null) m_Renderer.sprite = def.icon;
                m_Renderer.color = item.Prep == PrepState.Inedible ? Color.Lerp(def.placeholderColor, new Color(0.4f, 0.8f, 0.2f), 0.6f) : def.placeholderColor;
            }
            if (m_QualityPip != null) m_QualityPip.color = QualityColor(item.Quality);
            name = $"Pickup_{def.id}_{item.Quality}";
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            if (m_WaitingForPlayerToLeave)
            {
                var player = PlayerLocator.Player;
                if (player == null || Vector2.Distance(player.position, transform.position) > k_LeaveDistance)
                    m_WaitingForPlayerToLeave = false;
            }
        }

        /// <summary>Placeholder quality tint for the pip; the HUD also names the tier in text.</summary>
        public static Color QualityColor(Quality quality) => quality switch
        {
            Quality.Poor => new Color(0.55f, 0.45f, 0.4f),
            Quality.Standard => new Color(0.85f, 0.85f, 0.85f),
            Quality.Fine => new Color(0.35f, 0.7f, 1f),
            _ => new Color(1f, 0.8f, 0.2f),
        };
    }
}
