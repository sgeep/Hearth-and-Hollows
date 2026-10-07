using System.Collections.Generic;
using Hearthdelve.Core.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>2026-10-07: world signs step down out of the HUD's areas (the Essence bar covered door signs at some angles).</summary>
    public class ScreenReservationTests
    {
        static readonly Rect Bar = Rect.MinMaxRect(10f, 160f, 120f, 176f);

        [Test]
        public void ASignUnderTheBar_DropsJustClearOfIt()
        {
            var sign = Rect.MinMaxRect(40f, 150f, 60f, 170f);
            float drop = ScreenReservations.DropToClear(sign, new List<Rect> { Bar }, margin: 2f);
            Assert.That(drop, Is.EqualTo(170f - (160f - 2f)).Within(1e-4f));
        }

        [Test]
        public void ASignBesideOrBelowTheBar_StaysPut()
        {
            Assert.That(ScreenReservations.DropToClear(Rect.MinMaxRect(130f, 150f, 150f, 170f), new List<Rect> { Bar }), Is.Zero, "beside");
            Assert.That(ScreenReservations.DropToClear(Rect.MinMaxRect(40f, 120f, 60f, 140f), new List<Rect> { Bar }), Is.Zero, "below");
        }

        [Test]
        public void DroppingOntoAnotherArea_ClearsThatOneToo()
        {
            var gold = Rect.MinMaxRect(10f, 140f, 70f, 156f);
            var sign = Rect.MinMaxRect(40f, 150f, 60f, 170f);
            float drop = ScreenReservations.DropToClear(sign, new List<Rect> { Bar, gold }, margin: 2f);
            Assert.That(170f - drop, Is.LessThanOrEqualTo(140f - 2f + 1e-4f), "under the gold too");
        }

        [Test]
        public void Registrations_ComeAndGo()
        {
            object owner = new object();
            ScreenReservations.Add(owner, () => Bar);
            Assert.That(ScreenReservations.Current(), Has.Member(Bar));
            ScreenReservations.Remove(owner);
            Assert.That(ScreenReservations.Current(), Has.No.Member(Bar));
        }
    }
}
