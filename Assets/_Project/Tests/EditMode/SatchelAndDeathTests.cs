using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests
{
    public abstract class IngredientFixture
    {
        protected IngredientDefinition Haunch;
        protected IngredientDefinition Gel;
        readonly List<Object> m_Created = new();

        [SetUp]
        public void CreateDefinitions()
        {
            Haunch = Make("rat_haunch");
            Gel = Make("slime_gel");
        }

        [TearDown]
        public void DestroyDefinitions()
        {
            foreach (var o in m_Created) Object.DestroyImmediate(o);
            m_Created.Clear();
        }

        IngredientDefinition Make(string id)
        {
            var def = ScriptableObject.CreateInstance<IngredientDefinition>();
            def.id = id;
            m_Created.Add(def);
            return def;
        }
    }

    public class SatchelTests : IngredientFixture
    {
        [Test]
        public void IdenticalItems_Stack_InOneSlot()
        {
            var s = new Satchel(6, 5);
            var item = new IngredientItem(Haunch, Quality.Fine);
            Assert.That(s.Add(item, 3), Is.EqualTo(0));
            Assert.That(s.Add(item, 1), Is.EqualTo(0));
            Assert.That(s.Slots[0].Count, Is.EqualTo(4));
            Assert.That(s.Slots[1].IsEmpty);
        }

        [Test]
        public void DifferentQualityOrPrep_DoNotStack()
        {
            var s = new Satchel(6, 5);
            s.Add(new IngredientItem(Haunch, Quality.Fine));
            s.Add(new IngredientItem(Haunch, Quality.Standard));
            s.Add(new IngredientItem(Haunch, Quality.Fine, PrepState.Seared));
            Assert.That(s.Slots[0].Count, Is.EqualTo(1));
            Assert.That(s.Slots[1].Count, Is.EqualTo(1));
            Assert.That(s.Slots[2].Count, Is.EqualTo(1));
        }

        [Test]
        public void StackOverflow_SpillsIntoNextEmptySlot()
        {
            var s = new Satchel(6, 5);
            var item = new IngredientItem(Haunch, Quality.Standard);
            s.Add(item, 7);
            Assert.That(s.Slots[0].Count, Is.EqualTo(5));
            Assert.That(s.Slots[1].Count, Is.EqualTo(2));
        }

        [Test]
        public void TopsUpExistingStack_BeforeUsingEmptySlot()
        {
            var s = new Satchel(3, 5);
            var a = new IngredientItem(Haunch, Quality.Standard);
            s.Add(a, 2);
            s.Add(new IngredientItem(Gel, Quality.Standard));
            s.Add(a, 2);
            Assert.That(s.Slots[0].Count, Is.EqualTo(4));
            Assert.That(s.Slots[2].IsEmpty);
        }

        [Test]
        public void Full_ReturnsRemainder_AndCanAcceptIsFalse()
        {
            var s = new Satchel(2, 1);
            s.Add(new IngredientItem(Haunch, Quality.Standard));
            s.Add(new IngredientItem(Gel, Quality.Standard));
            var newItem = new IngredientItem(Haunch, Quality.Premium);
            Assert.That(s.CanAccept(newItem), Is.False);
            Assert.That(s.Add(newItem, 2), Is.EqualTo(2));
        }

        [Test]
        public void Full_ButMatchingStackHasRoom_Accepts()
        {
            var s = new Satchel(1, 5);
            var item = new IngredientItem(Haunch, Quality.Standard);
            s.Add(item, 2);
            Assert.That(s.CanAccept(item));
            Assert.That(s.SpaceFor(item), Is.EqualTo(3));
        }

        [Test]
        public void ReplaceAt_SwapsSlot_ReturnsDiscarded()
        {
            var s = new Satchel(2, 5);
            var old = new IngredientItem(Gel, Quality.Poor);
            s.Add(old, 3);
            var incoming = new IngredientItem(Haunch, Quality.Premium);

            var discarded = s.ReplaceAt(0, new IngredientStack(incoming, 2, 0.4f), out int placed);

            Assert.That(discarded.Item, Is.EqualTo(old));
            Assert.That(discarded.Count, Is.EqualTo(3));
            Assert.That(placed, Is.EqualTo(2));
            Assert.That(s.Slots[0].Item, Is.EqualTo(incoming));
        }

        [Test]
        public void ReplaceAt_KeepsIncomingFreshness()
        {
            var s = new Satchel(1, 5);
            s.ReplaceAt(0, new IngredientStack(new IngredientItem(Haunch, Quality.Fine), 2, 0.4f), out _);
            Assert.That(s.Slots[0].Freshness, Is.EqualTo(0.4f).Within(1e-5f));
        }

        [Test]
        public void Merging_UsesCountWeightedFreshness()
        {
            var s = new Satchel(6, 5);
            var item = new IngredientItem(Haunch, Quality.Standard);
            s.Add(item, 1, 1.0f);
            s.Add(item, 3, 0.2f);
            // (1×1.0 + 3×0.2) / 4 = 0.4
            Assert.That(s.Slots[0].Count, Is.EqualTo(4));
            Assert.That(s.Slots[0].Freshness, Is.EqualTo(0.4f).Within(1e-5f));
        }

        [Test]
        public void NewStack_DefaultsToFullyFresh()
        {
            var s = new Satchel(6, 5);
            s.Add(new IngredientItem(Haunch, Quality.Standard));
            Assert.That(s.Slots[0].Freshness, Is.EqualTo(1f));
        }

        [Test]
        public void ReplaceAt_ClampsToMaxStack()
        {
            var s = new Satchel(1, 5);
            s.ReplaceAt(0, new IngredientStack(new IngredientItem(Haunch, Quality.Standard), 9), out int placed);
            Assert.That(placed, Is.EqualTo(5));
        }

        [Test]
        public void Changed_FiresOnMutation_NotOnNoop()
        {
            var s = new Satchel(1, 1);
            int changes = 0;
            s.Changed += () => changes++;
            s.Add(new IngredientItem(Haunch, Quality.Standard));
            s.Add(new IngredientItem(Gel, Quality.Standard)); // full: no-op
            s.RemoveAt(0);
            s.RemoveAt(0);                                      // already empty: no-op
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void InvalidArguments_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Satchel(0, 5));
            var s = new Satchel(2, 5);
            Assert.Throws<ArgumentException>(() => s.Add(default));
            Assert.Throws<ArgumentOutOfRangeException>(() => s.RemoveAt(2));
        }
    }

    public class DeathPenaltyTests : IngredientFixture
    {
        Satchel Filled()
        {
            var s = new Satchel(6, 5);
            s.Add(new IngredientItem(Haunch, Quality.Premium), 3);
            s.Add(new IngredientItem(Gel, Quality.Standard), 2);
            return s;
        }

        [Test]
        public void KeepsWholeStack_FromChosenSlot_LosesRest()
        {
            var s = Filled();
            var result = DeathPenalty.Resolve(s, keepSlotIndex: 0, runCurrency: 40);

            Assert.That(result.KeptSomething);
            Assert.That(result.Kept.Item, Is.EqualTo(new IngredientItem(Haunch, Quality.Premium)));
            Assert.That(result.Kept.Count, Is.EqualTo(3), "the whole stack is kept");
            Assert.That(result.ItemsLost, Is.EqualTo(2));
            Assert.That(s.IsEmpty, "the satchel is emptied");
        }

        [Test]
        public void RunCurrency_IsAlwaysLost()
        {
            var result = DeathPenalty.Resolve(Filled(), 1, runCurrency: 125);
            Assert.That(result.RunCurrencyLost, Is.EqualTo(125));
        }

        [Test]
        public void KeepNothing_LosesEverything()
        {
            var s = Filled();
            var result = DeathPenalty.Resolve(s, DeathPenalty.KeepNothing, 0);
            Assert.That(result.KeptSomething, Is.False);
            Assert.That(result.ItemsLost, Is.EqualTo(5));
            Assert.That(s.IsEmpty);
        }

        [Test]
        public void ChoosingEmptySlot_KeepsNothing()
        {
            var result = DeathPenalty.Resolve(Filled(), 4, 0);
            Assert.That(result.KeptSomething, Is.False);
        }

        [Test]
        public void EmptySatchel_KeepsNothing_LosesNothing()
        {
            var result = DeathPenalty.Resolve(new Satchel(6, 5), DeathPenalty.KeepNothing, 0);
            Assert.That(result.ItemsLost, Is.EqualTo(0));
        }

        [Test]
        public void InvalidSlot_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DeathPenalty.Resolve(Filled(), 6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => DeathPenalty.Resolve(Filled(), -2, 0));
        }
    }
}
