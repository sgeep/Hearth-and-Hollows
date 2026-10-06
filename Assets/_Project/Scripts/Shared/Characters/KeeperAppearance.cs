using System.Collections.Generic;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using UnityEngine;

namespace Hearthdelve.Shared.Characters
{
    /// <summary>
    /// Bakes the keeper's look (4g Checkpoint B): the profile's body, its drawing recoloured to the chosen colourways through the
    /// furniture recolour (cached copies of the frames; <c>Sprite-Lit-Default</c> stays), as an animation set the keeper's
    /// animator plays. One set per body and palette, shared by the tavern, the Hollows and the creator's preview.
    /// </summary>
    public static class KeeperLook
    {
        static readonly Dictionary<string, SpriteAnimationSet> s_Sets = new();

        /// <param name="only">Bake just this animation (the creator's preview); the whole set otherwise.</param>
        public static SpriteAnimationSet Build(KeeperLooks looks, PlayerProfile profile, out SpriteAnimationSet shadow, CharacterAnim? only = null)
        {
            shadow = null;
            KeeperBody body = looks != null ? looks.Body(profile?.body) : null;
            if (body == null || body.animations == null) return null;
            shadow = body.shadow;
            string palette = KeeperRules.ForBody(profile?.palette, body);
            if (string.IsNullOrEmpty(palette) || looks.palettes == null) return body.animations;
            string key = body.id + "|" + palette + "|" + (only?.ToString() ?? "all");
            if (s_Sets.TryGetValue(key, out SpriteAnimationSet cached) && cached != null) return cached;
            SpriteAnimationSet set = ScriptableObject.CreateInstance<SpriteAnimationSet>();
            set.name = $"{body.animations.name} ({palette})";
            foreach (SpriteAnim anim in body.animations.animations)
            {
                if (anim == null || (only.HasValue && anim.action != only.Value)) continue;
                set.animations.Add(new SpriteAnim
                {
                    action = anim.action, frameDuration = anim.frameDuration, loop = anim.loop, mirrorForLeft = anim.mirrorForLeft,
                    walkForBack = anim.walkForBack,
                    frontRight = Recolour(anim.frontRight, body, palette, looks.palettes),
                    frontLeft = Recolour(anim.frontLeft, body, palette, looks.palettes),
                    backRight = Recolour(anim.backRight, body, palette, looks.palettes),
                    backLeft = Recolour(anim.backLeft, body, palette, looks.palettes),
                });
            }
            s_Sets[key] = set;
            return set;
        }

        static Sprite[] Recolour(Sprite[] frames, KeeperBody body, string palette, PaletteLibrary library)
        {
            if (frames == null) return null;
            var result = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++) result[i] = FurnitureRecolour.Apply(frames[i], body.channels, palette, library);
            return result;
        }
    }

    /// <summary>
    /// The keeper wearing the profile's look (4g Checkpoint B): on the keeper in the tavern and the Hollows, it swaps the
    /// animator's set for the baked one as the scene starts. Without a game (a scene played on its own) the keeper stays as built.
    /// </summary>
    [RequireComponent(typeof(CharacterSpriteAnimator))]
    public sealed class KeeperAppearance : MonoBehaviour
    {
        [SerializeField] KeeperLooks m_Looks;

        public KeeperLooks Looks => m_Looks;

        public void Configure(KeeperLooks looks) => m_Looks = looks;

        void Awake()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame) return;
            Apply(flow.State.Story.Player);
        }

        /// <summary>Wears <paramref name="profile"/>'s body and colours.</summary>
        public void Apply(PlayerProfile profile)
        {
            var animator = GetComponent<CharacterSpriteAnimator>();
            SpriteAnimationSet set = KeeperLook.Build(m_Looks, profile, out SpriteAnimationSet shadow);
            if (set == null || animator == null) return;
            animator.Configure(set, animator.Renderer, shadow != null ? shadow : animator.ShadowSet, animator.ShadowRenderer);
        }
    }
}
