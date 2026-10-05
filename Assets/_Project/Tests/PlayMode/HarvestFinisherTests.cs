using System.Collections;
using System.Linq;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Ingredients;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4e step 3, the Harvest Finisher, in the arena (the troll frozen): an ordinary enemy that's low and freshly hit can
    /// be finished, for Premium parts; the moment passes; and the troll, brought down by lethal damage, can be finished for
    /// a Premium harvest, or left to fall with its ordinary one, but never finished early.
    /// </summary>
    public class HarvestFinisherTests : LookTestFixture
    {
        static RoomRunner Runner => RoomRunner.Active;

        [SetUp]
        public void StartInTheArena()
        {
            RoomRunner.SeedOverride = 20261004;
            RoomRunner.StartInArenaOverride = true;
        }

        [TearDown]
        public void Release()
        {
            RoomRunner.SeedOverride = 0;
            RoomRunner.StartInArenaOverride = false;
        }

        BossEncounter Encounter => Object.FindAnyObjectByType<BossEncounter>();
        PlayerFinisher Finisher => Player.GetComponent<PlayerFinisher>();

        IEnumerator Fighting()
        {
            yield return Load("Dungeon");
            yield return WaitUntil(() => Runner != null && Runner.Current != null && !Runner.IsTransitioning, 10f, "the arena");
            Player.GetComponent<EssenceHealth>().GodMode = true;
            yield return WaitUntil(() => Encounter != null && Encounter.State == BossEncounterState.Fighting, 6f, "the fight");
            FreezeEnemies();
        }

        /// <summary>A slime beside the player, at <paramref name="health"/>, just hit.</summary>
        GameObject LowSlime(float health)
        {
            GameObject slime = Object.Instantiate(Runner.Settings.slime, (Vector2)Player.transform.position + new Vector2(1f, 0f), Quaternion.identity);
            slime.GetComponent<MoreMountains.Tools.AIBrain>().BrainActive = false;
            slime.GetComponent<Health>().SetHealth(health);
            slime.GetComponent<HitReaction>().ReceiveHit(new HitContext(null, null, false, Player.transform.position));
            return slime;
        }

        static IngredientPickup[] Drops() => Object.FindObjectsByType<IngredientPickup>();

        [UnityTest]
        public IEnumerator ALowFreshlyHitEnemy_CanBeFinished_ForPremiumParts()
        {
            yield return Fighting();
            Teleport(Player, (Vector2)Runner.Current.transform.position + new Vector2(8f, 6f));
            yield return null;
            GameObject slime = LowSlime(8f);
            var target = slime.GetComponent<FinisherTarget>();
            yield return null;
            Assert.That(target.IsEligible, "low and freshly hit");
            Assert.That(slime.transform.Find("FinisherPrompt").gameObject.activeSelf, "the drumstick says so");
            Assert.That(Finisher.TryFinish(), "the finisher starts");
            yield return null;
            Assert.That(Finisher.IsFinishing, "committed");
            Assert.That(Player.GetComponent<CharacterDash2D>().AbilityPermitted, Is.False, "no dodging out of it");
            yield return WaitUntil(() => slime == null || slime.GetComponent<Health>().CurrentHealth <= 0f, 2f, "the blow");
            Assert.That(target.FinishingBlow);
            yield return WaitUntil(() => !Finisher.IsFinishing, 2f, "the finisher to end");
            Assert.That(Player.GetComponent<CharacterDash2D>().AbilityPermitted, "free again");
            yield return new WaitForSeconds(0.6f);
            IngredientPickup[] parts = Drops();
            Assert.That(parts, Is.Not.Empty, "the slime's parts");
            Assert.That(parts.All(p => p.Item.Quality == Quality.Premium), "a finisher's harvest is Premium");
        }

        [UnityTest]
        public IEnumerator TheMoment_Passes_AndAHealthyEnemyIsNeverEligible()
        {
            yield return Fighting();
            Teleport(Player, (Vector2)Runner.Current.transform.position + new Vector2(8f, 6f));
            yield return null;
            GameObject low = LowSlime(8f);
            GameObject healthy = LowSlime(34f);
            yield return null;
            Assert.That(low.GetComponent<FinisherTarget>().IsEligible);
            Assert.That(healthy.GetComponent<FinisherTarget>().IsEligible, Is.False, "not low");
            yield return new WaitForSeconds(1.4f);
            Assert.That(low.GetComponent<FinisherTarget>().IsEligible, Is.False, "the moment passed");
            Assert.That(Finisher.TryFinish(), Is.False);
        }

        [UnityTest]
        public IEnumerator TheTroll_IsBroughtDown_ByLethalDamage_AndCanBeFinished()
        {
            yield return Fighting();
            var health = (BossHealth)Encounter.GetComponent<Health>();
            var target = Encounter.GetComponent<FinisherTarget>();
            // Low, but not brought down: no early execution.
            health.SetHealth(health.MaximumHealth * 0.1f);
            var frenzy = Encounter.GetComponent<BossFrenzy>();
            yield return null;
            yield return WaitUntil(() => !frenzy.IsRoaring, 3f, "the frenzy roar (low health) to pass");
            FreezeEnemies();
            Assert.That(target.IsEligible, Is.False, "it can't be finished before lethal damage");
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            Assert.That(health.IsDowned, "brought down");
            Assert.That(health.CurrentHealth, Is.GreaterThan(0f));
            Assert.That(Encounter.State, Is.EqualTo(BossEncounterState.Fighting), "the fight isn't over yet");
            health.Damage(500f, Player.gameObject, 0f, 0f, Vector3.zero);
            Assert.That(health.IsDowned, "down, it can't be hurt: only finished, or left");
            Assert.That(target.IsEligible);
            Teleport(Player, (Vector2)Encounter.transform.position + new Vector2(0f, -1.5f));
            yield return new WaitForFixedUpdate();
            Assert.That(Finisher.TryFinish(), "the finishing moment");
            yield return WaitUntil(() => Encounter == null || Encounter.State == BossEncounterState.Defeated, 2f, "the troll to fall");
            Assert.That(health.WasFinished);
            yield return new WaitForSeconds(0.6f);
            Assert.That(Drops().Where(p => p.Item.Definition.id is "slime_core" or "venom_sac").All(p => p.Item.Quality == Quality.Premium) && Drops().Length > 0,
                "the troll's harvest, Premium to the finisher");
        }

        [UnityTest]
        public IEnumerator TheTrollsFall_LeavesItsGold_AndAPremiumLarderCache_AndIsRecordedForTheDelve()
        {
            yield return Fighting();
            var health = (BossHealth)Encounter.GetComponent<Health>();
            BossDefinition boss = Encounter.Boss;
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            health.FinishOff(Player.gameObject, finisher: false);
            yield return new WaitForSeconds(0.7f);
            GoldPickup gold = Runner.Current.GetComponentInChildren<GoldPickup>();
            Assert.That(gold, Is.Not.Null, "its Gold");
            Assert.That(gold.Amount, Is.EqualTo(boss.gold).And.GreaterThanOrEqualTo(100), "a substantial sum");
            foreach (CacheEntry entry in boss.cache)
                Assert.That(Drops().Any(p => p.Item.Definition == entry.ingredient && p.Item.Quality == Quality.Premium && p.Count == entry.count),
                    $"the cache's {entry.ingredient.id}");
            Assert.That(boss.cache.Select(c => c.ingredient.id), Is.EquivalentTo(new[] { "slime_core", "venom_sac" }), "the deeper Cellars' parts");
            Assert.That(Hearthdelve.Dungeon.Run.DelveRunController.Active.Loot.BossesDefeated, Is.EqualTo(new[] { "larder_troll" }));
            Assert.That(boss.trophyId, Is.Not.Null, "4f's hook is there, empty until then");
        }

        [UnityTest]
        public IEnumerator LeftDown_TheTrollFallsOnItsOwn_WithItsOrdinaryHarvest()
        {
            yield return Fighting();
            var health = (BossHealth)Encounter.GetComponent<Health>();
            health.Damage(health.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return null;
            Assert.That(health.IsDowned);
            yield return WaitUntil(() => Encounter == null || Encounter.State == BossEncounterState.Defeated, 5f, "the moment to pass");
            Assert.That(health.WasFinished, Is.False);
            yield return new WaitForSeconds(0.6f);
            // Its own harvest is a single core; the larder cache (step 4) is a Premium pair, set aside here.
            IngredientPickup core = Drops().FirstOrDefault(p => p.Item.Definition.id == "slime_core" && p.Count == 1);
            Assert.That(core, Is.Not.Null);
            // The ordinary harvest: Fine, unless the weapon's clean kill lifts it (the cleaver's Offal); the finisher makes all of it Premium.
            Assert.That(core.Item.Quality, Is.EqualTo(Quality.Fine), "the slime core, without the finisher");
        }
    }
}
