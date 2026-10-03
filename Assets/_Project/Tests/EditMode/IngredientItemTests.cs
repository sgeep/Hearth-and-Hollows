using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Save;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// <see cref="IngredientItem"/>'s hash is runtime identity only: it follows the definition's
    /// object identity, so it differs between sessions and must never be saved or used as a
    /// content id. Saves store <see cref="IngredientDefinition.id"/> instead.
    /// </summary>
    public class IngredientItemTests : IngredientFixture
    {
        [Test]
        public void EqualItems_HaveEqualHashes()
        {
            var a = new IngredientItem(Haunch, Quality.Fine, PrepState.Seared);
            var b = new IngredientItem(Haunch, Quality.Fine, PrepState.Seared);
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void Hash_IsStable_ForTheLifeOfTheDefinition()
        {
            var item = new IngredientItem(Haunch, Quality.Standard);
            int first = item.GetHashCode();
            Haunch.id = "renamed";
            Haunch.name = "Renamed";
            Assert.That(item.GetHashCode(), Is.EqualTo(first));
        }

        [Test]
        public void Items_DifferingInDefinitionQualityOrPrep_AreNotEqual()
        {
            var item = new IngredientItem(Haunch, Quality.Fine);
            Assert.That(item, Is.Not.EqualTo(new IngredientItem(Gel, Quality.Fine)));
            Assert.That(item, Is.Not.EqualTo(new IngredientItem(Haunch, Quality.Standard)));
            Assert.That(item, Is.Not.EqualTo(new IngredientItem(Haunch, Quality.Fine, PrepState.Chilled)));
        }

        [Test]
        public void Equality_IsByDefinitionReference_NotById()
        {
            var twin = ScriptableObject.CreateInstance<IngredientDefinition>();
            twin.id = Haunch.id;
            try
            {
                Assert.That(new IngredientItem(twin, Quality.Fine), Is.Not.EqualTo(new IngredientItem(Haunch, Quality.Fine)));
            }
            finally
            {
                Object.DestroyImmediate(twin);
            }
        }

        [Test]
        public void Items_WorkAsDictionaryAndSetKeys()
        {
            var counts = new Dictionary<IngredientItem, int>
            {
                [new IngredientItem(Haunch, Quality.Fine)] = 2,
                [new IngredientItem(Haunch, Quality.Standard)] = 1,
                [new IngredientItem(Gel, Quality.Fine)] = 5,
            };
            counts[new IngredientItem(Haunch, Quality.Fine)] += 3;

            Assert.That(counts.Count, Is.EqualTo(3));
            Assert.That(counts[new IngredientItem(Haunch, Quality.Fine)], Is.EqualTo(5));

            var set = new HashSet<IngredientItem>
            {
                new IngredientItem(Gel, Quality.Poor),
                new IngredientItem(Gel, Quality.Poor),
                new IngredientItem(Gel, Quality.Poor, PrepState.Chilled),
            };
            Assert.That(set.Count, Is.EqualTo(2));
        }

        [Test]
        public void DefaultItem_HashesWithoutADefinition()
        {
            Assert.DoesNotThrow(() => default(IngredientItem).GetHashCode());
            Assert.That(default(IngredientItem), Is.EqualTo(new IngredientItem(null, default)));
        }

        [Test]
        public void Save_RoundTripsById_IntoADifferentDefinitionInstance()
        {
            var item = new IngredientItem(Haunch, Quality.Fine, PrepState.Chilled);
            var state = new GameState();
            state.Storeroom.AddRange(new[] { new IngredientStack(item, 2, 0.5f) });

            string json = SaveSystem.ToJson(SaveSystem.Capture(state));

            // A later session loads a new object for the same asset: another identity and hash, same id.
            var reloaded = ScriptableObject.CreateInstance<IngredientDefinition>();
            reloaded.id = Haunch.id;
            try
            {
                SaveData data = SaveSystem.FromJson(json);
                Assert.That(data.storeroom[0].ingredient, Is.EqualTo(Haunch.id));

                GameState restored = SaveSystem.Restore(data, id => id == reloaded.id ? reloaded : null, _ => true);
                IngredientStack stack = restored.Storeroom.Stacks[0];
                Assert.That(stack.Item, Is.EqualTo(new IngredientItem(reloaded, Quality.Fine, PrepState.Chilled)));
                Assert.That(stack.Count, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(reloaded);
            }
        }
    }
}
