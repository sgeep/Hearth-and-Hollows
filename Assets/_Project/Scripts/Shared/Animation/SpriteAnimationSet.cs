using System;
using System.Collections.Generic;
using Hearthdelve.Core.Movement;
using UnityEngine;

namespace Hearthdelve.Shared.Animation
{
    public enum CharacterAnim
    {
        Idle,
        Walk,
        Attack,
        Dodge,
        Hurt,
        Die,
        // Added in 4b; appended so existing assets keep their values.
        /// <summary>Heavy attack wind-up while the button is held.</summary>
        Charge,
        /// <summary>Heavy attack fully wound up, looping until release.</summary>
        ChargeHold,
        /// <summary>The heavy attack itself (the charged spin).</summary>
        HeavyAttack,
        /// <summary>A ranged attack (the spider's web shot).</summary>
        Shoot,
        /// <summary>Asleep (the bat hanging).</summary>
        Sleep,
        /// <summary>Waking up.</summary>
        Wake,
    }

    /// <summary>One action's frames, for each of the four drawn facings.</summary>
    [Serializable]
    public sealed class SpriteAnim
    {
        public CharacterAnim action;
        [Min(0.01f)] public float frameDuration = 0.1f;
        public bool loop;
        public Sprite[] frontRight = Array.Empty<Sprite>();
        public Sprite[] frontLeft = Array.Empty<Sprite>();
        public Sprite[] backRight = Array.Empty<Sprite>();
        public Sprite[] backLeft = Array.Empty<Sprite>();
        [Tooltip("A single-row sheet drawn facing front-right: mirror it for the left facings instead of reusing it as drawn " +
                 "(the Townsfolk's jump, used for the dodge, has no other rows).")]
        public bool mirrorForLeft;
        [Tooltip("A sheet drawn only facing the front: for the back facings, show the walk's back frames instead, so the head " +
                 "doesn't turn to the camera (the Townsfolk's jump, used for the dodge).")]
        public bool walkForBack;

        /// <summary>Whether the sheet has its own frames for a facing (one-row sheets only have front-right).</summary>
        public bool HasOwn(Facing4 facing)
        {
            Sprite[] own = facing switch
            {
                Facing4.FrontLeft => frontLeft,
                Facing4.BackRight => backRight,
                Facing4.BackLeft => backLeft,
                _ => frontRight,
            };
            return own != null && own.Length > 0;
        }

        /// <summary>
        /// Frames for a facing, and whether to draw them mirrored: a sheet missing a left facing's frames, with
        /// <see cref="mirrorForLeft"/>, shows its front-right frames flipped.
        /// </summary>
        public Sprite[] For(Facing4 facing, out bool mirrored)
        {
            Sprite[] own = facing switch
            {
                Facing4.FrontLeft => frontLeft,
                Facing4.BackLeft => backLeft,
                _ => null,
            };
            mirrored = mirrorForLeft && (facing == Facing4.FrontLeft || facing == Facing4.BackLeft) && (own == null || own.Length == 0);
            return mirrored ? frontRight : For(facing);
        }

        /// <summary>Frames for a facing. Sheets with a single row (deaths) reuse it for every facing.</summary>
        public Sprite[] For(Facing4 facing)
        {
            Sprite[] frames = facing switch
            {
                Facing4.FrontLeft => frontLeft,
                Facing4.BackRight => backRight,
                Facing4.BackLeft => backLeft,
                _ => frontRight,
            };
            return frames != null && frames.Length > 0 ? frames : frontRight;
        }
    }

    /// <summary>
    /// A character's sprite-sheet animations, generated from the Minifantasy sheets and their
    /// frame-duration guides. All protagonist bodies share one set's layout.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Sprite Animation Set", fileName = "Anim_")]
    public sealed class SpriteAnimationSet : ScriptableObject
    {
        public List<SpriteAnim> animations = new();

        public SpriteAnim Find(CharacterAnim action)
        {
            foreach (var a in animations) if (a != null && a.action == action) return a;
            return null;
        }
    }
}
