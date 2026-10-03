using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Animation;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class HeavyChargeTests
    {
        static readonly float[] k_Times = { 0f, 0.45f, 0.9f };

        [TestCase(0f, 0)]
        [TestCase(0.2f, 0)]
        [TestCase(0.449f, 0)]
        [TestCase(0.45f, 1)]
        [TestCase(0.89f, 1)]
        [TestCase(0.9f, 2)]
        [TestCase(5f, 2)]
        public void Release_GivesTheLastStepReached(float held, int step)
        {
            Assert.That(HeavyCharge.StepAt(held, k_Times), Is.EqualTo(step));
        }

        [Test]
        public void TdeChargeDurations_GiveTheSameStep_AtEveryHoldTime()
        {
            float[] durations = HeavyCharge.StepDurations(k_Times);
            Assert.That(durations[0], Is.EqualTo(0.45f).Within(1e-5f));
            Assert.That(durations[1], Is.EqualTo(0.45f).Within(1e-5f));
            for (float held = 0f; held < 2f; held += 0.01f)
                Assert.That(HeavyCharge.TdeStepAt(held, durations), Is.EqualTo(HeavyCharge.StepAt(held, k_Times)), $"held {held:F2}s");
        }

        [Test]
        public void NoSteps_GivesNoStep()
        {
            Assert.That(HeavyCharge.StepAt(1f, new float[0]), Is.EqualTo(-1));
            Assert.That(HeavyCharge.StepDurations(null), Is.Empty);
        }

        [Test]
        public void Validate_WantsATapFirst_AndRisingTimes()
        {
            Assert.That(HeavyCharge.Validate(k_Times), Is.Empty);
            Assert.That(HeavyCharge.Validate(new[] { 0.1f, 0.5f }), Is.Not.Empty, "the first step must be a tap");
            Assert.That(HeavyCharge.Validate(new[] { 0f, 0.5f, 0.5f }), Is.Not.Empty, "times must rise");
            Assert.That(HeavyCharge.Validate(new float[0]), Is.Not.Empty);
        }

        [Test]
        public void TheCleaversHeavy_IsValid_AndHitsHarderTheLongerItCharges()
        {
            var cleaver = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/_Project/Data/Weapons/Weapon_ButchersCleaver.asset");
            Assert.That(cleaver, Is.Not.Null);
            List<float> times = cleaver.heavy.Select(s => s.chargeTime).ToList();
            Assert.That(HeavyCharge.Validate(times), Is.Empty);
            for (int i = 1; i < cleaver.heavy.Count; i++)
            {
                AttackData before = cleaver.heavy[i - 1].attack, after = cleaver.heavy[i].attack;
                Assert.That(after.damage, Is.GreaterThan(before.damage), $"step {i} damage");
                Assert.That(after.hitStop, Is.GreaterThanOrEqualTo(before.hitStop), $"step {i} hit-stop");
                Assert.That(after.knockbackForce, Is.GreaterThanOrEqualTo(before.knockbackForce), $"step {i} knockback");
            }
            Assert.That(cleaver.heavy[^1].attack.damage, Is.GreaterThan(cleaver.combo.Max(a => a.damage)), "a full charge outhits any light hit");
        }
    }

    public class TelegraphTimingTests
    {
        [Test]
        public void FramesBeforeTheRelease_FillTheTelegraph()
        {
            // 7 frames at 0.1 s, landing on frame 4, over a 0.5 s telegraph.
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0f, 0.5f, 4, 7, 0.1f), Is.EqualTo(0));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.13f, 0.5f, 4, 7, 0.1f), Is.EqualTo(1));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.49f, 0.5f, 4, 7, 0.1f), Is.EqualTo(3));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.5f, 0.5f, 4, 7, 0.1f), Is.EqualTo(4), "lands as the telegraph ends");
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.65f, 0.5f, 4, 7, 0.1f), Is.EqualTo(5));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(5f, 0.5f, 4, 7, 0.1f), Is.EqualTo(6), "holds the last frame");
        }

        [Test]
        public void ALongerTelegraph_StretchesTheWindUp_NotTheAttack()
        {
            for (float t = 0f; t < 1.2f; t += 0.05f)
                Assert.That(SpriteAnimationMath.TelegraphedFrame(t, 1.2f, 9, 14, 0.1f), Is.LessThan(9), $"{t:F2}s is still winding up");
            Assert.That(SpriteAnimationMath.TelegraphedFrame(1.2f, 1.2f, 9, 14, 0.1f), Is.EqualTo(9));
        }

        [Test]
        public void ReleaseOnTheFirstFrame_HoldsItThroughTheTelegraph()
        {
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.3f, 0.6f, 0, 4, 0.1f), Is.EqualTo(0));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.59f, 0.6f, 1, 4, 0.1f), Is.EqualTo(0));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.7f, 0.6f, 1, 4, 0.1f), Is.EqualTo(2));
            Assert.That(SpriteAnimationMath.TelegraphedFrame(0.2f, 0.6f, 99, 1, 0.1f), Is.EqualTo(0), "single frame");
        }
    }

    public class StaggerRulesTests
    {
        [Test]
        public void SuperArmour_OnlyCounts_WhileAttacking()
        {
            Assert.That(StaggerRules.IgnoresHit(superArmorWhileAttacking: true, attacking: true));
            Assert.That(StaggerRules.IgnoresHit(true, false), Is.False);
            Assert.That(StaggerRules.IgnoresHit(false, true), Is.False);
        }

        [Test]
        public void Knockback_IsAShortSlide_ThatSlowsToAStop()
        {
            Assert.That(StaggerRules.KnockbackSpeed(8f, 1f, 0f), Is.EqualTo(8f));
            Assert.That(StaggerRules.KnockbackSpeed(8f, 1.5f, 0f), Is.EqualTo(12f), "the enemy's knockback multiplier");
            Assert.That(StaggerRules.KnockbackSpeed(8f, 1f, StaggerRules.KnockbackDuration / 2f), Is.EqualTo(4f).Within(1e-4f));
            Assert.That(StaggerRules.KnockbackSpeed(8f, 1f, StaggerRules.KnockbackDuration), Is.Zero);
            Assert.That(StaggerRules.KnockbackSpeed(8f, 0f, 0f), Is.Zero, "immune");
            // The slide's distance is the area under that line.
            float distance = 0f;
            for (float t = 0f; t < 1f; t += 0.0001f) distance += StaggerRules.KnockbackSpeed(8f, 1f, t) * 0.0001f;
            Assert.That(distance, Is.EqualTo(StaggerRules.KnockbackDistance(8f, 1f)).Within(0.01f));
        }

        [Test]
        public void Hits_ExtendTheStagger_ButNeverStackOrShortenIt()
        {
            float until = StaggerRules.StaggerUntil(0f, 10f, 0.4f, 1f);
            Assert.That(until, Is.EqualTo(10.4f).Within(1e-5f));
            Assert.That(StaggerRules.StaggerUntil(until, 10.1f, 0.1f, 1f), Is.EqualTo(10.4f).Within(1e-5f), "a weaker hit doesn't shorten it");
            Assert.That(StaggerRules.StaggerUntil(until, 10.3f, 0.4f, 1f), Is.EqualTo(10.7f).Within(1e-5f), "a later hit extends it from now, not from the end");
            Assert.That(StaggerRules.StaggerUntil(0f, 10f, 0.4f, 0.5f), Is.EqualTo(10.2f).Within(1e-5f), "the enemy's multiplier");
            Assert.That(StaggerRules.StaggerUntil(0f, 10f, -1f, 1f), Is.EqualTo(10f));
        }
    }

    /// <summary>The 4b roster's data: three enemies, each attack readable and wired to its prefab.</summary>
    public class EnemyContentTests
    {
        static readonly (string definition, string prefab)[] k_Roster =
        {
            ("Assets/_Project/Data/Enemies/Enemy_GreenSlime.asset", "Assets/_Project/Prefabs/Enemies/GreenSlime.prefab"),
            ("Assets/_Project/Data/Enemies/Enemy_Bat.asset", "Assets/_Project/Prefabs/Enemies/Bat.prefab"),
            ("Assets/_Project/Data/Enemies/Enemy_GiantSpider.asset", "Assets/_Project/Prefabs/Enemies/GiantSpider.prefab"),
        };

        [Test]
        public void TheOldPrototypeEnemies_WereRenamed_NotLeftBehind()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_GiantRat.asset"), Is.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_CellarShroom.asset"), Is.Null);
        }

        [Test]
        public void EveryAttack_IsTelegraphedReadably_AndWiredToItsPrefab()
        {
            var ids = new HashSet<string>();
            foreach (var (definitionPath, prefabPath) in k_Roster)
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.That(definition, Is.Not.Null, definitionPath);
                Assert.That(prefab, Is.Not.Null, prefabPath);
                Assert.That(ids.Add(definition.id), definition.id);
                Assert.That(prefab.GetComponent<EnemyIdentity>().Definition, Is.SameAs(definition), prefabPath);

                SpriteAnimationSet set = prefab.GetComponentInChildren<CharacterSpriteAnimator>(true) is { } animator
                    ? (SpriteAnimationSet)new SerializedObject(animator).FindProperty("m_Set").objectReferenceValue
                    : null;
                Assert.That(set, Is.Not.Null, $"{prefabPath} animation set");

                List<EnemyAttackSettings> attacks = definition.AllAttacks.ToList();
                EnemyAttack[] components = prefab.GetComponents<EnemyAttack>();
                Assert.That(components.Length, Is.EqualTo(attacks.Count), $"{definition.id}: one EnemyAttack per attack");
                for (int i = 0; i < attacks.Count; i++)
                {
                    EnemyAttackSettings a = attacks[i];
                    string name = $"{definition.id}/{a.debugName}";
                    Assert.That(a.telegraph, Is.GreaterThanOrEqualTo(0.4f), $"{name}: a telegraph the player can read");
                    Assert.That(a.active, Is.GreaterThan(0f), name);
                    Assert.That(a.maxRange, Is.GreaterThan(a.minRange), name);
                    Assert.That(a.damage, Is.GreaterThan(0f), name);
                    SpriteAnim anim = set.Find(a.animation);
                    Assert.That(anim, Is.Not.Null, $"{name}: the {a.animation} animation exists");
                    Assert.That(a.releaseFrame, Is.LessThan(anim.frontRight.Length), $"{name}: the release frame is in the animation");

                    var wiring = new SerializedObject(components[i]);
                    Assert.That(wiring.FindProperty("m_AttackIndex").intValue, Is.EqualTo(i), name);
                    Assert.That(wiring.FindProperty("m_Alert").objectReferenceValue, Is.Not.Null, $"{name}: the telegraph alert");
                    if (a.kind == EnemyAttackKind.Spit)
                        Assert.That(wiring.FindProperty("m_Projectile").objectReferenceValue, Is.Not.Null, $"{name}: the projectile");
                    else
                    {
                        var hitbox = (GameObject)wiring.FindProperty("m_Hitbox").objectReferenceValue;
                        Assert.That(hitbox, Is.Not.Null, $"{name}: the hitbox");
                        Assert.That(hitbox.activeSelf, Is.False, $"{name}: the hitbox is off until the attack is active (no contact damage)");
                    }
                }
                Assert.That(prefab.GetComponent<HitReaction>(), Is.Not.Null, $"{definition.id} reacts to hits");
            }
        }

        [Test]
        public void TheRoster_CoversTheThreeBehaviours()
        {
            var bat = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(k_Roster[1].definition);
            var spider = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(k_Roster[2].definition);
            var slime = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(k_Roster[0].definition);
            Assert.That(slime.attack.kind, Is.EqualTo(EnemyAttackKind.Leap));
            Assert.That(slime.superArmorWhileAttacking, "the slime's leap can't be interrupted");
            Assert.That(bat.attack.kind, Is.EqualTo(EnemyAttackKind.Swoop));
            Assert.That(bat.startsAsleep);
            var batPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_Roster[1].prefab);
            var perch = batPrefab.GetComponent<EnemyPerch>();
            Assert.That(perch, Is.Not.Null, "a sleeping bat hangs from a wall");
            Assert.That(new SerializedObject(perch).FindProperty("m_Shadow").objectReferenceValue, Is.Not.Null, "and hides its ground shadow while hanging");
            Assert.That(spider.AllAttacks.Select(a => a.kind), Is.EquivalentTo(new[] { EnemyAttackKind.Bite, EnemyAttackKind.Spit }));
            Assert.That(spider.keepDistance.x, Is.GreaterThan(spider.attack.maxRange), "the spider backs off past its bite range");
        }
    }
}
