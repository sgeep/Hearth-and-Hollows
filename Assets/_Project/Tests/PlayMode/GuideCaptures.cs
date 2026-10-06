using System.Collections;
using System.Linq;
using Hearthdelve.Core.Movement;
using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Hud;
using Hearthdelve.UI.Screens;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Not a check: renders the moments the player's guide shows at 320×180 into <c>BatchLogs/guide/</c>: each cooking
    /// minigame mid-play, carrying a plate, each monster's warning, a clean kill, low Essence and the Lockbox.
    /// Explicit, so it only runs when asked for by name.
    /// </summary>
    [Explicit]
    public class GuideCaptures : LookTestFixture
    {
        const string k_Out = "BatchLogs/guide";
        static readonly Vector2 k_Arena = new(20.5f, 2.3f);
        TavernDirector Director => TavernDirector.Instance;
        KeeperWork Keeper => KeeperWork.Instance;

        [TearDown]
        public void Restore()
        {
            Time.timeScale = 1f;
            MenuPause.Clear();
        }

        static void Shot(string name)
        {
            // The test floor's developer readouts (resolution, scroll mode, the controls line) aren't part of the game.
            foreach (Hearthdelve.UI.Debugging.LookTestOverlay overlay in Object.FindObjectsByType<Hearthdelve.UI.Debugging.LookTestOverlay>())
                foreach (string child in new[] { "Resolution", "Hint" })
                    overlay.transform.Find(child)?.gameObject.SetActive(false);
            TavernEveningCaptures.Capture($"{k_Out}/{name}.png");
        }

        // ------------------------------------------------------------------ the tavern

        IEnumerator Open(string recipeId)
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            Director.ArrivalsPaused = true;
            RecipeDefinition recipe = Director.Content.recipes.First(r => r.id == recipeId);
            Director.SetMenu(new[] { recipe });
            Director.AssignStaff(StaffStation.None);
            Director.OpenService();
            Director.ArrivalsPaused = true;
        }

        IEnumerator Order(System.Action<CustomerAgent> got)
        {
            CustomerProfile profile = Object.Instantiate(Director.Content.customers[0]);
            profile.traits.orderPatience = 1000f;
            CustomerAgent customer = Director.SpawnCustomer(profile);
            yield return WaitUntil(() => customer.Logic.State == CustomerState.WaitingForFood, 20f, "the customer to order");
            got(customer);
        }

        IEnumerator UseAt(TavernInteractable target)
        {
            Teleport(Player, target.Kind == TavernInteractableKind.Pass ? target.UsePoint + Vector2.down : target.UsePoint);
            var interactor = Player.GetComponent<TavernInteractor>();
            yield return WaitUntil(() => interactor.Target == target, 2f, $"{target.Kind} to be the target");
            Hold(Key.E);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
        }

        static TavernInteractable Station(TavernInteractableKind kind) => Object.FindObjectsByType<TavernInteractable>().First(s => s.Kind == kind);

        [UnityTest]
        public IEnumerator CaptureTheTavern()
        {
            // The grill: the doneness needle in the gold band, the moment to flip.
            yield return Open("cellar_kebab");
            yield return Order(_ => { });
            yield return UseAt(Station(TavernInteractableKind.Grill));
            yield return new WaitForSeconds(2.9f);
            Shot("grill");

            // Carrying the plate: the dish over the keeper's head, the customer's gold ring.
            Keeper.FinishCook(1f);
            yield return null;
            yield return UseAt(Station(TavernInteractableKind.Pass));
            CustomerAgent seated = Object.FindObjectsByType<CustomerAgent>().First(c => c.Logic.State == CustomerState.WaitingForFood);
            Teleport(Player, (Vector2)seated.transform.position + new Vector2(0f, -1.1f));
            yield return new WaitForSeconds(0.3f);
            Shot("serving");

            // The tap: pouring, with the head building.
            yield return Open("core_tonic");
            yield return Order(_ => { });
            yield return UseAt(Station(TavernInteractableKind.Tap));
            yield return new WaitForSeconds(0.3f);
            Hold(Key.Space);
            yield return new WaitForSeconds(3.6f);
            Shot("tap");
            ReleaseKeys();

            // Chopping for the stew pot: the guide lines and the knife.
            yield return Open("cellar_stew");
            yield return Order(_ => { });
            yield return UseAt(Station(TavernInteractableKind.StewPot));
            yield return new WaitForSeconds(0.9f);
            Hold(Key.Space);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return new WaitForSeconds(0.3f);
            Shot("chop");
        }

        // ------------------------------------------------------------------ the Hollows

        IEnumerator Arena(string enemyId, Vector2 offset, System.Action<EnemyIdentity> got)
        {
            yield return Load("Dungeon_TestFloor");
            EnemyIdentity keep = null;
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
            {
                if (keep == null && enemy.Definition.id == enemyId) keep = enemy;
                else Object.Destroy(enemy.gameObject);
            }
            keep.GetComponent<EnemyPerch>()?.Detach();
            keep.GetComponent<Character>().CharacterBrain.BrainActive = false;
            keep.GetComponent<CharacterMovement>().SetMovement(Vector2.zero);
            Teleport(Player, k_Arena);
            Teleport(keep, k_Arena + offset);
            Player.GetComponent<AimControlSwitcher>().enabled = false;
            var handle = Player.GetComponent<CharacterHandleWeapon>();
            handle.WeaponAimComponent.AimControl = WeaponAim.AimControls.Script;
            handle.WeaponAimComponent.SetCurrentAim(offset.normalized);
            yield return new WaitForFixedUpdate();
            yield return null;
            got(keep);
        }

        [UnityTest]
        public IEnumerator CaptureTheMonsters()
        {
            foreach (var (id, kind, distance) in new[]
                     {
                         ("green_slime", EnemyAttackKind.Leap, 2.4f), ("bat", EnemyAttackKind.Swoop, 2.4f),
                         ("giant_spider", EnemyAttackKind.Spit, 4.5f), ("giant_spider", EnemyAttackKind.Bite, 1.4f),
                     })
            {
                EnemyIdentity enemy = null;
                yield return Arena(id, new Vector2(distance, 0.4f), e => enemy = e);
                EnemyAttack attack = enemy.GetComponents<EnemyAttack>().First(a => a.Settings.kind == kind);
                attack.Begin(Player.transform);
                yield return new WaitForSeconds(attack.Settings.telegraph * 0.6f);
                Shot($"warn_{id}_{kind.ToString().ToLowerInvariant()}");
            }
        }

        [UnityTest]
        public IEnumerator CaptureACleanKill()
        {
            EnemyIdentity bat = null;
            yield return Arena("bat", new Vector2(1.1f, 0f), e => bat = e);
            // One light chop (8) left to finish it, with little to spare: a clean kill.
            bat.GetComponent<Health>().SetHealth(6f);
            yield return Click();
            yield return new WaitForSeconds(1.3f);
            Shot("clean_kill");
        }

        [UnityTest]
        public IEnumerator CaptureLowEssenceAndTheLockbox()
        {
            yield return Load("Dungeon_TestFloor");
            foreach (EnemyIdentity enemy in Object.FindObjectsByType<EnemyIdentity>())
                enemy.GetComponent<Character>().CharacterBrain.BrainActive = false;
            var essence = Player.GetComponent<EssenceHealth>();
            Satchel satchel = Player.GetComponent<SatchelCarrier>().Satchel;
            IngredientDefinition wing = Object.FindObjectsByType<EnemyIdentity>().First(e => e.Definition.id == "bat").Definition.harvest[0].ingredient;
            IngredientDefinition leg = Object.FindObjectsByType<EnemyIdentity>().First(e => e.Definition.id == "giant_spider").Definition.harvest[0].ingredient;
            satchel.Clear();
            satchel.Add(new IngredientItem(wing, Quality.Fine), 2);
            satchel.Add(new IngredientItem(leg, Quality.Standard), 3);
            satchel.Add(new IngredientItem(leg, Quality.Premium), 1);
            while (essence.Essence.Normalized > 0.15f) essence.Essence.TakeDamage(0.5f);
            yield return new WaitForSeconds(1.2f);
            Shot("low_essence");

            var death = Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
            essence.Damage(essence.CurrentHealth + 50f, Player.gameObject, 0f, 0f, Vector3.zero);
            yield return WaitUntil(() => death.IsOpen, 4f, "the death screen");
            yield return null;
            Hold(Key.D);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
            yield return null;
            Shot("lockbox");
        }
    }
}
