using System.Linq;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Run;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>
    /// 4d step 5: guards the Essence budget against drifting back to the old mismatch (drain alone lasting ~3 minutes
    /// against a 6-9 minute run). The bounds are wide on purpose: the values are tuned in play, these only catch a
    /// budget that makes a full run impossible, or one so generous that hits stop mattering.
    /// </summary>
    public class EssenceTuningTests
    {
        static EssenceSettings Essence => AssetDatabase.LoadAssetAtPath<EssenceConfig>("Assets/_Project/Data/Config/EssenceConfig.asset").essence;
        static RunTuning Run => AssetDatabase.LoadAssetAtPath<RunSettings>("Assets/_Project/Data/Dungeon/RunSettings.asset").tuning;

        /// <summary>About a full Cellars run of competent play (4d step 5 estimate; see PROGRESS.md).</summary>
        const float k_FullRunSeconds = 400f;

        [Test]
        public void DrainAlone_OutlastsACompetentFullRun_ButNotByAWide_Margin()
        {
            EssenceSettings e = Essence;
            float lifetime = e.baseMax / e.drainPerSecond;
            Assert.That(lifetime, Is.GreaterThan(k_FullRunSeconds * 1.3f), "a full run is possible at base progression");
            Assert.That(lifetime, Is.LessThan(k_FullRunSeconds * 2.2f), "drain still presses: lingering costs");
            float passiveCost = k_FullRunSeconds * e.drainPerSecond;
            Assert.That(e.baseMax - passiveCost, Is.InRange(20f, 60f), "what's left for hits on a full run: a few, not many");
        }

        [Test]
        public void BaseEssence_IsNotSimplyHuge()
        {
            Assert.That(Essence.baseMax, Is.LessThanOrEqualTo(120f));
            Assert.That(Essence.damageMultiplier, Is.GreaterThanOrEqualTo(0.75f), "hits still cost real Essence");
        }

        [Test]
        public void NoSinglePower_CoversTheWholeRunsDrain()
        {
            EssenceSettings e = Essence;
            float passiveCost = k_FullRunSeconds * e.drainPerSecond;
            const int fightsPerRun = 10;
            foreach (RunPowerDefinition p in Run.powers)
            {
                float worth = p.effect switch
                {
                    RunPowerEffect.MaxEssence => p.amount,
                    RunPowerEffect.SlowerDrain => passiveCost * p.amount,
                    RunPowerEffect.EssenceOnClear => p.amount * fightsPerRun,
                    _ => 0f,
                };
                Assert.That(worth, Is.LessThan(passiveCost), $"{p.id} would make drain irrelevant even if taken in the first room");
            }
        }
    }
}
