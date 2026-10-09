using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Localization;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    /// <summary>The harvest feed's words for each kind of harvest (moved from PlayMode's HudTests by the test review: it needs no scene).</summary>
    public class HarvestFeedTests
    {
        [Test]
        public void HarvestFeed_PicksTheRightWords()
        {
            Assert.That(HarvestFeed.KeyFor(HarvestFlags.None), Is.EqualTo(LocKeys.HarvestGot));
            Assert.That(HarvestFeed.KeyFor(HarvestFlags.CleanKill), Is.EqualTo(LocKeys.HarvestGotClean));
            Assert.That(HarvestFeed.KeyFor(HarvestFlags.Overkill | HarvestFlags.CleanKill), Is.EqualTo(LocKeys.HarvestGotOverkill));
            Assert.That(HarvestFeed.KeyFor(HarvestFlags.Destroyed | HarvestFlags.Overkill), Is.EqualTo(LocKeys.HarvestDestroyed));
        }
    }
}
