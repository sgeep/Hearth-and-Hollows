using Hearthdelve.Core.Services;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Hit-stop: MMFeedbacks' freeze frame (handled by the scene's MMTimeManager), skipped when the
    /// player has turned hit-stop off. The duration is set per attack from its data
    /// (<c>AttackData.hitStop</c>), so a heavy hit can freeze longer than a light one.
    /// </summary>
    [AddComponentMenu("")]
    [System.Serializable]
    [FeedbackPath("Hearthdelve/Hit Stop")]
    [FeedbackHelp("A freeze frame that respects the player's hit-stop setting. Needs an MMTimeManager in the scene.")]
    public class MMF_HitStop : MMF_FreezeFrame
    {
        public new static bool FeedbackTypeAuthorized = true;

        protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
        {
            if (!GameSettings.HitStopEnabled) return;
            base.CustomPlayFeedback(position, feedbacksIntensity);
        }
    }
}
