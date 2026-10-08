using System.Collections;
using Hearthdelve.Shared.Haptics;
using MoreMountains.Feedbacks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 2026-10-08: two flashes on one sprite (an enemy's hit and its other moments) overlapping must leave it its own colour; a
    /// slime stayed dark when the later flash took the earlier one's tint for the sprite's colour.
    /// </summary>
    public class SpriteFlashTests
    {
        GameObject m_Root;

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null) Object.Destroy(m_Root);
        }

        MMF_Player Player(SpriteRenderer target, Color color, float duration)
        {
            var go = new GameObject("Feedback");
            go.transform.SetParent(m_Root.transform, false);
            var player = go.AddComponent<MMF_Player>();
            player.InitializationMode = MMFeedbacks.InitializationModes.Script;
            player.AddFeedback(new MMF_SpriteFlash { Label = "Flash", Target = target, FlashColor = color, Duration = duration });
            player.Initialization();
            return player;
        }

        [UnityTest]
        public IEnumerator OverlappingFlashes_LeaveTheSpriteItsOwnColour_WhicheverEndsLast()
        {
            m_Root = new GameObject("Slime");
            var body = m_Root.AddComponent<SpriteRenderer>();
            Color green = new(0.6f, 1f, 0.6f, 1f);
            body.color = green;
            MMF_Player dark = Player(body, new Color(0.2f, 0.2f, 0.2f, 1f), 0.3f);
            MMF_Player hit = Player(body, new Color(1f, 0.35f, 0.35f, 1f), 0.1f);

            // The long dark flash, then a short hit flash inside it: the hit ends first and must not restore its "original" (dark).
            dark.PlayFeedbacks();
            yield return new WaitForSeconds(0.05f);
            hit.PlayFeedbacks();
            yield return new WaitForSeconds(0.5f);
            Assert.That(body.color, Is.EqualTo(green), "back to its own colour after both");
            Assert.That(MMF_SpriteFlash.ShowingOn(body), Is.Zero);

            // The other way round: the hit, then the dark flash outlasting it.
            hit.PlayFeedbacks();
            yield return new WaitForSeconds(0.05f);
            dark.PlayFeedbacks();
            yield return new WaitForSeconds(0.5f);
            Assert.That(body.color, Is.EqualTo(green), "back to its own colour after both, the other order");

            // The same flash again before it ends.
            hit.PlayFeedbacks();
            hit.PlayFeedbacks();
            yield return new WaitForSeconds(0.3f);
            Assert.That(body.color, Is.EqualTo(green), "a replayed flash restores too");

            // A lasting colour set while a flash shows (the troll's frenzy) is the one it returns to.
            Color frenzy = new(1f, 0.6f, 0.5f, 1f);
            dark.PlayFeedbacks();
            MMF_SpriteFlash.SetResting(body, frenzy);
            Assert.That(body.color, Is.Not.EqualTo(frenzy), "the flash still shows");
            yield return new WaitForSeconds(0.5f);
            Assert.That(body.color, Is.EqualTo(frenzy), "the new lasting colour after the flash");
        }
    }
}
