using System.Linq;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.UI.Screens;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The menus' feedback (4c step 6): a <see cref="UiFeedback"/> on each canvas, kept quiet (clicks and ticks; only
    /// commitments and purchases are felt, and lightly).
    /// </summary>
    public static class UiFeedbackContent
    {
        /// <summary>Puts a fresh UiFeedback on <paramref name="canvas"/> (replacing an old one).</summary>
        public static UiFeedback Ensure(Canvas canvas)
        {
            TavernFeedbackContent.BuildSounds();
            Transform old = canvas.transform.Find("UiFeedback");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("UiFeedback", typeof(RectTransform)).transform;
            root.SetParent(canvas.transform, false);
            var feedback = root.gameObject.AddComponent<UiFeedback>();
            feedback.Configure(
                Moment(root, "Feedback_Confirm", "PH_UiConfirm", null, 0f),
                Moment(root, "Feedback_Commit", "PH_UiConfirm", HapticIds.TapLight, 0.35f),
                Moment(root, "Feedback_Buy", "PH_UiBuy", HapticIds.PulseSuccess, 0.6f),
                Moment(root, "Feedback_Tick", "PH_UiTick", null, 0f),
                Moment(root, "Feedback_Takings", "PH_UiChime", null, 0f));
            return feedback;
        }

        /// <summary>Marks a button as a step forward in the day: its click comes with a very light tap.</summary>
        public static void Commit(Button button)
        {
            UiButtonFeedback feedback = button.GetComponent<UiButtonFeedback>() ?? button.gameObject.AddComponent<UiButtonFeedback>();
            feedback.Commit = true;
        }

        static MMF_Player Moment(Transform parent, string name, string sound, string haptic, float hapticScale)
        {
            MMF_Player player = LookTestContent.Feedback(parent, name, null, 0f, LookTestContent.Sfx(sound), haptic != null ? LookTestContent.Pattern(haptic) : null);
            foreach (MMF_HapticPattern pattern in player.FeedbacksList.OfType<MMF_HapticPattern>()) pattern.Scale = hapticScale;
            return player;
        }
    }
}
