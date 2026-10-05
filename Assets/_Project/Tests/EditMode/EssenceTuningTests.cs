using System.Linq;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Run;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>
    /// Guards the Essence budget's intent (4d playtest, 2026-10-05): at base progression drain alone carries a delver
    /// through the first floor but not a full Cellars run; gear (the max-Essence upgrades and a good delve meal) is what
    /// makes a full run reachable; and no single run power makes drain irrelevant. The bounds are wide on purpose: the
    /// values are tuned in play.
    /// </summary>
    public class EssenceTuningTests
    {
        static EssenceSettings Essence => AssetDatabase.LoadAssetAtPath<EssenceConfig>("Assets/_Project/Data/Config/EssenceConfig.asset").essence;
        static RunTuning Run => AssetDatabase.LoadAssetAtPath<RunSettings>("Assets/_Project/Data/Dungeon/RunSettings.asset").tuning;

        /// <summary>About a full Cellars run, and its first floor, in competent play (the 4d playtest).</summary>
        const float k_FullRunSeconds = 360f;
        const float k_FirstFloorSeconds = 110f;

        static float BestGear()
        {
            var essence = AssetDatabase.LoadAssetAtPath<TavernUpgradeDefinition>("Assets/_Project/Data/Upgrades/Upgrade_MaxEssence.asset");
            float upgrades = essence.levels.Sum(l => l.amount);
            float meal = AssetDatabase.FindAssets("t:RecipeDefinition")
                .Select(g => AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null && r.mealBuff.kind == MealBuffKind.MaxEssence).Max(r => r.mealBuff.amount);
            return upgrades + meal;
        }

        [Test]
        public void AtBase_DrainCarriesYouThroughTheFirstFloor_ButNotAFullRun()
        {
            EssenceSettings e = Essence;
            float lifetime = e.baseMax / e.drainPerSecond;
            Assert.That(lifetime, Is.GreaterThan(k_FirstFloorSeconds * 1.4f), "the first floor, with room for a few hits");
            Assert.That(lifetime, Is.LessThan(k_FullRunSeconds), "a full run needs gear");
        }

        [Test]
        public void Gear_MakesAFullRunReachable()
        {
            EssenceSettings e = Essence;
            float geared = (e.baseMax + BestGear()) / e.drainPerSecond;
            Assert.That(geared, Is.GreaterThan(k_FullRunSeconds * 0.95f), "fully upgraded with a good meal, the drain of a full run is about covered; powers and clean play do the rest");
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
                Assert.That(worth, Is.LessThan(passiveCost * 0.5f), $"{p.id} would make drain an afterthought even if taken in the first room");
            }
        }
    }
}
