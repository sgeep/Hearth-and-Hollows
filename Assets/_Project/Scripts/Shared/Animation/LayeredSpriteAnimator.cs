using System.Collections.Generic;
using Hearthdelve.Core.Animation;
using Hearthdelve.Core.Movement;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Shared.Animation
{
    /// <summary>
    /// Draws a layered NPC (A Myriad of NPCs: body, clothes, hair, …) as one character. Presentation
    /// only: it reads the character's movement and never drives gameplay. Every layer shows the same
    /// animation, facing and frame, chosen once per frame with the same rules as
    /// <see cref="CharacterSpriteAnimator"/> (<see cref="FacingLogic"/>, <see cref="SpriteAnimationMath"/>),
    /// so the layers can't drift apart. Kept separate so the combat animator stays simple (4c decision 3).
    /// </summary>
    public sealed class LayeredSpriteAnimator : MonoBehaviour
    {
        [SerializeField, Tooltip("Renderers, back to front (body first). A layer with no set is hidden.")]
        SpriteRenderer[] m_Layers = System.Array.Empty<SpriteRenderer>();
        [SerializeField] SpriteAnimationSet[] m_Sets = System.Array.Empty<SpriteAnimationSet>();
        [SerializeField] SpriteRenderer m_Shadow;
        [SerializeField] SpriteAnimationSet m_ShadowSet;
        [SerializeField] Facing4 m_InitialFacing = Facing4.FrontRight;

        TopDownController m_Controller;
        CharacterAnim m_Current = CharacterAnim.Idle;
        float m_Time;
        bool m_FacingLocked;

        public Facing4 Facing { get; private set; }
        public CharacterAnim Current => m_Current;
        /// <summary>The frame every layer is showing.</summary>
        public int Frame { get; private set; }
        public IReadOnlyList<SpriteRenderer> Layers => m_Layers;

        public void Configure(SpriteRenderer[] layers, SpriteRenderer shadow, SpriteAnimationSet shadowSet)
        {
            m_Layers = layers;
            m_Sets = new SpriteAnimationSet[layers.Length];
            m_Shadow = shadow;
            m_ShadowSet = shadowSet;
        }

        /// <summary>Sets what each layer wears (null hides a layer). Called once per character: appearance never changes with state.</summary>
        public void SetAppearance(IReadOnlyList<SpriteAnimationSet> sets)
        {
            m_Sets = new SpriteAnimationSet[m_Layers.Length];
            for (int i = 0; i < m_Layers.Length; i++)
            {
                m_Sets[i] = sets != null && i < sets.Count ? sets[i] : null;
                if (m_Layers[i] != null) m_Layers[i].enabled = m_Sets[i] != null;
            }
            Show();
        }

        /// <summary>Holds a facing (a seated customer faces the table) until <see cref="ReleaseFacing"/>.</summary>
        public void LockFacing(Facing4 facing)
        {
            Facing = facing;
            m_FacingLocked = true;
        }

        public void ReleaseFacing() => m_FacingLocked = false;

        void Awake()
        {
            Facing = m_InitialFacing;
            m_Controller = GetComponentInParent<TopDownController>();
        }

        void LateUpdate()
        {
            Vector2 movement = m_Controller != null ? (Vector2)m_Controller.CurrentMovement : Vector2.zero;
            bool moving = movement.sqrMagnitude > 0.01f;
            if (!m_FacingLocked && moving) Facing = FacingLogic.FromDirection(movement.x, movement.y, Facing);
            CharacterAnim wanted = moving ? CharacterAnim.Walk : CharacterAnim.Idle;
            if (wanted != m_Current)
            {
                m_Current = wanted;
                m_Time = 0f;
            }
            m_Time += Time.deltaTime;
            Show();
        }

        void Show()
        {
            // One clock for all layers: the frame comes from the first layer that has this animation.
            SpriteAnim lead = null;
            foreach (SpriteAnimationSet set in m_Sets)
                if (set != null && (lead = set.Find(m_Current)) != null) break;
            if (lead == null) return;
            Frame = SpriteAnimationMath.FrameAt(m_Time, lead.For(Facing).Length, lead.frameDuration, lead.loop);
            for (int i = 0; i < m_Layers.Length; i++) Apply(m_Sets[i], m_Layers[i]);
            Apply(m_ShadowSet, m_Shadow);
        }

        void Apply(SpriteAnimationSet set, SpriteRenderer target)
        {
            if (set == null || target == null) return;
            SpriteAnim anim = set.Find(m_Current);
            Sprite[] frames = anim?.For(Facing);
            if (frames == null || frames.Length == 0) return;
            target.sprite = frames[Mathf.Min(Frame, frames.Length - 1)];
        }
    }
}
