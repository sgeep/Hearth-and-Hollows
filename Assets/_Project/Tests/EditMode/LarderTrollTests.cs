using System.Linq;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Rooms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>4e step 1: the Larder Troll's rules and data, and the arena hosting it.</summary>
    public class LarderTrollTests
    {
        static BossDefinition Boss => AssetDatabase.LoadAssetAtPath<BossDefinition>("Assets/_Project/Data/Dungeon/Bosses/Boss_LarderTroll.asset");
        static EnemyDefinition Troll => AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_LarderTroll.asset");
        static RunSettings Settings => AssetDatabase.LoadAssetAtPath<RunSettings>("Assets/_Project/Data/Dungeon/RunSettings.asset");

        [Test]
        public void AChargeIsBlocked_WhenItCoversFarLessThanItsSpeedShould()
        {
            Assert.That(ChargeRules.IsBlocked(expected: 1f, moved: 0.1f), "stopped by a pillar");
            Assert.That(ChargeRules.IsBlocked(expected: 1f, moved: 0.9f), Is.False, "running freely");
            Assert.That(ChargeRules.IsBlocked(expected: 0f, moved: 0f), Is.False, "not charging at all");
        }

        [Test]
        public void Feeding_HealsAShare_SpoilsWithEnoughDamage_AndDropsOnlyWhenRoomAndTimeAllow()
        {
            Assert.That(FeedingRules.Heal(500f, 900f, 0.08f), Is.EqualTo(72f).Within(1e-3f));
            Assert.That(FeedingRules.Heal(880f, 900f, 0.08f), Is.EqualTo(20f).Within(1e-3f), "never past its maximum");
            Assert.That(FeedingRules.Spoiled(600f, 555f, 40f), "hit hard enough while eating");
            Assert.That(FeedingRules.Spoiled(600f, 580f, 40f), Is.False, "a light hit doesn't stop it");
            Assert.That(FeedingRules.MayDrop(15f, 14f, onFloor: 1, maxOnFloor: 4));
            Assert.That(FeedingRules.MayDrop(5f, 14f, 1, 4), Is.False, "too soon");
            Assert.That(FeedingRules.MayDrop(30f, 14f, 4, 4), Is.False, "the floor is full");
        }

        [Test]
        public void TheFinisher_TakesALowFreshlyHitEnemy_Only()
        {
            var s = Hearthdelve.Dungeon.Harvest.FinisherSettings.Default;
            Assert.That(Hearthdelve.Dungeon.Harvest.FinisherRules.Eligible(10f, 34f, 0.5f, s), "low (35% or 20 health) and just hit");
            Assert.That(Hearthdelve.Dungeon.Harvest.FinisherRules.Eligible(18f, 34f, 0.5f, s), "20 health counts as low for small enemies");
            Assert.That(Hearthdelve.Dungeon.Harvest.FinisherRules.Eligible(30f, 34f, 0.5f, s), Is.False, "not low");
            Assert.That(Hearthdelve.Dungeon.Harvest.FinisherRules.Eligible(10f, 34f, 2f, s), Is.False, "the moment passed");
            Assert.That(Hearthdelve.Dungeon.Harvest.FinisherRules.Eligible(0f, 34f, 0.5f, s), Is.False, "already dead");
            Assert.That(Hearthdelve.Dungeon.Harvest.FinisherRules.IsLow(300f, 900f, s), Is.True, "a big enemy's 35%");
        }

        [Test]
        public void ACooldown_CanBeSkipped_ButNothingElse()
        {
            var cycle = new AttackCycle(0.1f, 0.1f, 0.1f, 5f);
            Assert.That(cycle.TryStart());
            cycle.SkipCooldown();
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Telegraph), "only a cooldown is skipped");
            cycle.Tick(0.11f);
            cycle.Tick(0.11f);
            cycle.Tick(0.11f);
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Cooldown));
            cycle.SkipCooldown();
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Ready), "the frenzy's second slam");
        }

        [Test]
        public void TheTroll_Feeds_AndFrenzies()
        {
            BossDefinition boss = Boss;
            Assert.That(boss.feeding.enabled);
            Assert.That(boss.feeding.scraps, Is.Not.Empty.And.All.Not.Null, "existing Cellars parts, no boss-only ingredient");
            Assert.That(boss.feeding.eatSeconds, Is.GreaterThanOrEqualTo(1f), "a window to spoil the meal");
            Assert.That(boss.frenzy.enabled);
            Assert.That(boss.frenzy.atHealth, Is.InRange(0.3f, 0.7f));
            foreach (System.Type part in new[] { typeof(ScrapEater), typeof(LarderScraps), typeof(BossFrenzy) })
                Assert.That(boss.prefab.GetComponent(part), Is.Not.Null, part.Name);
        }

        [Test]
        public void AnEncounterCanPauseDrain_ButHitsStillCost()
        {
            var meter = new EssenceMeter(new EssenceSettings { baseMax = 100f, drainPerSecond = 0.5f, damageMultiplier = 1f, lowThreshold = 0.25f },
                EssenceModifiers.None) { EncounterDrainMultiplier = 0f };
            meter.Tick(10f);
            Assert.That(meter.Current, Is.EqualTo(100f), "no passive drain during the boss");
            meter.TakeDamage(18f);
            Assert.That(meter.Current, Is.EqualTo(82f), "the slam still costs");
            meter.EncounterDrainMultiplier = 1f;
            meter.Tick(10f);
            Assert.That(meter.Current, Is.EqualTo(77f).Within(1e-4f), "drain back after the fight");
        }

        [Test]
        public void TheTroll_IsABoss_WithTheSlamAndTheCharge()
        {
            BossDefinition boss = Boss;
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.id, Is.EqualTo("larder_troll"), "stable: saves and 4f's trophy key on it");
            Assert.That(boss.drainMultiplierWhileActive, Is.Zero, "passive drain pauses during the fight");
            Assert.That(boss.prefab, Is.Not.Null);
            Assert.That(boss.prefab.GetComponent<BossEncounter>(), Is.Not.Null);
            Assert.That(boss.prefab.GetComponent<ChargeStun>(), Is.Not.Null);
            Assert.That(boss.prefab.GetComponents<AttackTelegraphMarker>(), Has.Length.EqualTo(2), "both telegraphs mark the floor");

            EnemyDefinition troll = Troll;
            Assert.That(troll.staggerMultiplier, Is.Zero, "hits don't interrupt it: its stun is earned");
            Assert.That(troll.knockbackMultiplier, Is.Zero);
            Assert.That(troll.attack.kind, Is.EqualTo(EnemyAttackKind.Bite), "the slam");
            Assert.That(troll.attack.telegraph, Is.GreaterThanOrEqualTo(0.7f), "a slam you can read");
            EnemyAttackSettings charge = troll.otherAttacks.Single();
            Assert.That(charge.kind, Is.EqualTo(EnemyAttackKind.Swoop));
            Assert.That(charge.stunOnBlock, Is.GreaterThan(1.5f), "running into a wall is the big opening");
            Assert.That(charge.minRange, Is.GreaterThan(troll.attack.maxRange), "slam up close, charge at range");
        }

        [Test]
        public void TheArena_HoldsTheTroll_NotTheStandInFight()
        {
            RunSettings settings = Settings;
            Assert.That(settings.tuning.boss, Is.SameAs(Boss));
            Assert.That(settings.Prefab(EnemyKind.Boss), Is.SameAs(Boss.prefab));
            foreach (int seed in Enumerable.Range(1, 50))
            {
                RunGraph run = RunGenerator.Generate(seed, settings.tuning, settings.Catalog());
                FloorNode arena = run.Floors[^1].Nodes.Single(n => n.Kind == RoomKind.Arena);
                Assert.That(arena.Encounter.Select(e => e.Kind), Is.EqualTo(new[] { EnemyKind.Boss }), $"seed {seed}");
            }
        }

        [Test]
        public void Feeding_GivesUp_OnAPartItStopsGettingCloserTo()
        {
            float best = 6f, stuck = 0f;
            Assert.That(FeedingRules.Stalled(ref best, ref stuck, 5.5f, 0.5f, 0.25f, 2f), Is.False, "closer: progress");
            Assert.That(best, Is.EqualTo(5.5f));
            Assert.That(FeedingRules.Stalled(ref best, ref stuck, 5.4f, 1f, 0.25f, 2f), Is.False, "jittering in place is not progress");
            Assert.That(stuck, Is.EqualTo(1f));
            Assert.That(FeedingRules.Stalled(ref best, ref stuck, 5.6f, 1f, 0.25f, 2f), Is.True, "two seconds without progress: give up");
        }

        [Test]
        public void Campfire_GivesItsShareOverTime_NeverMoreThanLeftOrMissing()
        {
            // 50 Essence over 2.5 s: 20 a second.
            Assert.That(Hearthdelve.Dungeon.Rooms.CampfireRules.Warm(50f, 50f, 2.5f, 0.5f, 100f), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(Hearthdelve.Dungeon.Rooms.CampfireRules.Warm(4f, 50f, 2.5f, 0.5f, 100f), Is.EqualTo(4f).Within(1e-4f), "what it has left");
            Assert.That(Hearthdelve.Dungeon.Rooms.CampfireRules.Warm(50f, 50f, 2.5f, 0.5f, 3f), Is.EqualTo(3f).Within(1e-4f), "what's missing, the rest kept");
            Assert.That(Hearthdelve.Dungeon.Rooms.CampfireRules.Warm(0f, 50f, 2.5f, 0.5f, 30f), Is.Zero, "spent");
            Assert.That(Hearthdelve.Dungeon.Rooms.CampfireRules.Warm(20f, 50f, 2.5f, 0.5f, 0f), Is.Zero, "full");
        }
    }
}
