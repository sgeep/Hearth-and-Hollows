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
        bool m_HeldActive;
        CharacterAnim m_Held;
        bool m_OnceActive;
        CharacterAnim m_Once;

        /// <summary>
        /// How it's moving, for a character that isn't a TDE character (4h Checkpoint C: a villager walking a path): set each frame
        /// by its mover; null reads the TDE controller as before.
        /// </summary>
        public Vector2? Movement { get; set; }

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

        /// <summary>A different shadow (a figure with its own: a named villager at dinner, 4h Checkpoint D).</summary>
        public void SetShadow(SpriteAnimationSet shadow)
        {
            m_ShadowSet = shadow;
            Show();
        }

        /// <summary>Holds a facing (a seated customer faces the table) until <see cref="ReleaseFacing"/>.</summary>
        public void LockFacing(Facing4 facing)
        {
            Facing = facing;
            m_FacingLocked = true;
        }

        public void ReleaseFacing() => m_FacingLocked = false;

        /// <summary>Loops an action instead of standing idle (while not moving) until <see cref="Release"/>; ignored if no layer has it.</summary>
        public void Hold(CharacterAnim action)
        {
            m_HeldActive = HasAnim(action);
            m_Held = action;
        }

        public void Release() => m_HeldActive = false;

        /// <summary>Plays an action once (while not moving), then goes back to what it was doing; ignored if no layer has it.</summary>
        public void PlayOnce(CharacterAnim action)
        {
            if (!HasAnim(action)) return;
            m_OnceActive = true;
            m_Once = action;
            m_Current = action;
            m_Time = 0f;
        }

        bool HasAnim(CharacterAnim action)
        {
            foreach (SpriteAnimationSet set in m_Sets)
                if (set != null && set.Find(action) != null) return true;
            return false;
        }

        void Awake()
        {
            Facing = m_InitialFacing;
            m_Controller = GetComponentInParent<TopDownController>();
        }

        void LateUpdate()
        {
            Vector2 movement = Movement ?? (m_Controller != null ? (Vector2)m_Controller.CurrentMovement : Vector2.zero);
            bool moving = movement.sqrMagnitude > 0.01f;
            if (!m_FacingLocked && moving) Facing = FacingLogic.FromDirection(movement.x, movement.y, Facing);
            if (m_OnceActive && (moving || OnceFinished())) m_OnceActive = false;
            CharacterAnim wanted = moving ? CharacterAnim.Walk : m_OnceActive ? m_Once : m_HeldActive ? m_Held : CharacterAnim.Idle;
            if (wanted != m_Current)
            {
                m_Current = wanted;
                m_Time = 0f;
            }
            m_Time += Time.deltaTime;
            Show();
        }

        bool OnceFinished()
        {
            foreach (SpriteAnimationSet set in m_Sets)
            {
                SpriteAnim anim = set != null ? set.Find(m_Once) : null;
                if (anim != null) return SpriteAnimationMath.IsFinished(m_Time, anim.For(Facing).Length, anim.frameDuration, false);
            }
            return true;
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
