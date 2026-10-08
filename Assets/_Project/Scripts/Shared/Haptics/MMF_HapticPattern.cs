using Hearthdelve.Core.Events;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Core.Services;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Shared.Haptics
{
    /// <summary>
    /// Plays a named haptic pattern from an MMF_Player, so a moment's visuals, sound and
    /// haptics are authored together in one feedback (CLAUDE.md, Game feel).
    /// </summary>
    [AddComponentMenu("")]
    [System.Serializable]
    [FeedbackPath("Hearthdelve/Haptic Pattern")]
    [FeedbackHelp("Plays a named haptic pattern through the HapticService. Respects the player's vibration settings and does nothing where rumble is unsupported.")]
    public class MMF_HapticPattern : MMF_Feedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.HapticsColor;
        public override bool EvaluateRequiresSetup() => Pattern == null;
        public override string RequiredTargetText => Pattern != null ? Pattern.id : "";
#endif
        [MMFInspectorGroup("Haptic Pattern", true, 23, true)]
        public HapticPattern Pattern;
        [Range(0f, 1f)] public float Scale = 1f;
        [Tooltip("Multiply the strength by the intensity the feedback is played with.")]
        public bool UseFeedbackIntensity;

        public override float FeedbackDuration => Pattern != null ? Pattern.Duration : 0f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized || Pattern == null) return;
            HapticService.Play(Pattern, UseFeedbackIntensity ? Scale * feedbacksIntensity : Scale);
        }
    }

    /// <summary>Asks for a screen shake over the event bus; the camera applies the shake setting.</summary>
    [AddComponentMenu("")]
    [System.Serializable]
    [FeedbackPath("Hearthdelve/Screen Shake")]
    [FeedbackHelp("Publishes ScreenShakeRequested. The camera's shake listener scales it by the player's screen shake setting.")]
    public class MMF_ScreenShake : MMF_Feedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.CameraColor;
#endif
        [MMFInspectorGroup("Screen Shake", true, 24)]
        [Min(0f)] public float Force = 0.3f;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized) return;
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(Force * feedbacksIntensity));
        }
    }

    /// <summary>
    /// Flashes a sprite to a colour and back. Skipped when the player turns flashes off. Several flashes can share one sprite (an
    /// enemy's hit and its other moments, 2026-10-08): the sprite's resting colour is recorded once, when no flash is showing on
    /// it, and put back when the last one ends, so one flash can never mistake another's tint for the sprite's own colour and
    /// leave it stuck (a slime stayed dark). Anything that changes a sprite's lasting colour while flashes may play on it goes
    /// through <see cref="SetResting"/>.
    /// </summary>
    [AddComponentMenu("")]
    [System.Serializable]
    [FeedbackPath("Hearthdelve/Sprite Flash")]
    [FeedbackHelp("Tints a SpriteRenderer for a moment. Respects the player's flash setting.")]
    public class MMF_SpriteFlash : MMF_Feedback
    {
        public static bool FeedbackTypeAuthorized = true;
#if UNITY_EDITOR
        public override Color FeedbackColor => MMFeedbacksInspectorColors.RendererColor;
        public override bool EvaluateRequiresSetup() => Target == null;
        public override string RequiredTargetText => Target != null ? Target.name : "";
#endif
        [MMFInspectorGroup("Sprite Flash", true, 25, true)]
        public SpriteRenderer Target;
        public Color FlashColor = new(1f, 0.35f, 0.35f, 1f);
        [Min(0f)] public float Duration = 0.12f;

        sealed class Resting
        {
            public Color Color;
            public int Showing;
        }

        static readonly System.Collections.Generic.Dictionary<SpriteRenderer, Resting> s_Resting = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Resting.Clear();

        /// <summary>
        /// Sets a sprite's lasting colour: at once if no flash is showing on it, otherwise when the last flash ends.
        /// </summary>
        public static void SetResting(SpriteRenderer target, Color color)
        {
            if (target == null) return;
            if (s_Resting.TryGetValue(target, out Resting rest) && rest.Showing > 0) rest.Color = color;
            else target.color = color;
        }

        /// <summary>How many flashes are showing on a sprite now (tests).</summary>
        public static int ShowingOn(SpriteRenderer target) => target != null && s_Resting.TryGetValue(target, out Resting rest) ? rest.Showing : 0;

        Coroutine m_Routine;
        SpriteRenderer m_Showing;

        public override float FeedbackDuration => Duration;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!Active || !FeedbackTypeAuthorized || Target == null || !GameSettings.FlashEnabled) return;
            if (m_Routine != null)
            {
                Owner.StopCoroutine(m_Routine);
                m_Routine = null;
                End();
            }
            if (!s_Resting.TryGetValue(Target, out Resting rest))
                s_Resting[Target] = rest = new Resting { Color = Target.color };
            rest.Showing++;
            m_Showing = Target;
            Target.color = FlashColor;
            m_Routine = Owner.StartCoroutine(Flash());
        }

        System.Collections.IEnumerator Flash()
        {
            yield return WaitFor(Duration);
            m_Routine = null;
            End();
        }

        /// <summary>This flash is over: the last one on the sprite puts its resting colour back.</summary>
        void End()
        {
            SpriteRenderer target = m_Showing;
            m_Showing = null;
            if (target == null || !s_Resting.TryGetValue(target, out Resting rest)) return;
            if (--rest.Showing > 0) return;
            s_Resting.Remove(target);
            target.color = rest.Color;
        }

        protected override void CustomStopFeedback(Vector3 position, float feedbacksIntensity = 1)
        {
            if (m_Routine == null) return;
            Owner.StopCoroutine(m_Routine);
            m_Routine = null;
            End();
        }
    }
}
