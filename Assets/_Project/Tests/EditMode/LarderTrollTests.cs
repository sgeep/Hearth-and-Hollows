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
    }
}
