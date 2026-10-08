using Hearthdelve.Editor;
using Hearthdelve.Shared.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>
    /// The keeper stands still when idle (the owner's call, 2026-10-08): no head turn of its own (Minifantasy's idle frames
    /// 8–10 hold frame 7), but the blinks stay, and the loop keeps its length.
    /// </summary>
    public class KeeperIdleTests
    {
        static readonly string[] k_Sets =
        {
            "Anim_HumanTownsfolk", "Anim_HumanTownsfolk_Shadow", "Anim_KeeperAmazon", "Anim_KeeperWildOrc", "Anim_KeeperDwarf",
            "Anim_KeeperDwarf_Shadow",
        };

        [Test]
        public void EveryKeeperBody_Idles_WithoutTurningItsHead_AndStillBlinks([ValueSource(nameof(k_Sets))] string name)
        {
            var set = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>($"{EditorPaths.Animations}/{name}.asset");
            Assert.That(set, Is.Not.Null, name);
            SpriteAnim idle = set.Find(CharacterAnim.Idle);
            Assert.That(idle, Is.Not.Null, $"{name} has an idle");
            foreach (Sprite[] frames in new[] { idle.frontRight, idle.frontLeft, idle.backRight, idle.backLeft })
            {
                if (frames == null || frames.Length == 0) continue;
                Assert.That(frames.Length, Is.EqualTo(16), $"{name}: the loop keeps its length (and its blinks' timing)");
                for (int i = 8; i <= 10; i++) Assert.That(frames[i], Is.SameAs(frames[7]), $"{name}: frame {i} holds frame 7 (no head turn)");
            }
            if (!name.Contains("Shadow"))
                Assert.That(idle.frontRight[3], Is.Not.SameAs(idle.frontRight[0]), $"{name}: the blink is kept");
        }
    }
}
