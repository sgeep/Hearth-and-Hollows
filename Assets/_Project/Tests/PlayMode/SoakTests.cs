using System.Collections;
using System.Linq;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using Hearthdelve.Village;
using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4i-D's soak: five real days from a new game (the real delve, night and sleep each time; the evenings kept shut, the delves
    /// empty), checking what accumulates: the save every morning, the garden from planting to harvest, Vigor refilled, one of
    /// each villager where their schedule says, Gimp's night exactly once, the music's cues, and a save, quit and Continue at the
    /// end that comes back to the same game.
    /// </summary>
    public class SoakTests : BootFixture
    {
        static readonly string[] k_People = { CharacterIds.Maximo, CharacterIds.Kaloren, CharacterIds.Grim, CharacterIds.Ogrin, CharacterIds.Bart, CharacterIds.Musashi };

        static GardenBed Bed(string id) => GardenBed.Find(id);
        static GardenPanel Panel => Object.FindAnyObjectByType<GardenPanel>(FindObjectsInactive.Include);

        static IEnumerator Plant(string bed, string crop)
        {
            Bed(bed).Interactable.Use();
            yield return Frames(2);
            Assert.That(Panel.IsOpen, $"{bed} asks what to plant");
            Panel.CropButtons[Panel.Offered.ToList().FindIndex(c => c.id == crop)].onClick.Invoke();
            yield return Frames(2);
        }

        static IEnumerator Use(string bed)
        {
            Bed(bed).Interactable.Use();
            yield return Frames(2);
        }

        /// <summary>The morning's checks, every day.</summary>
        IEnumerator Morning(int day)
        {
            Assert.That(Flow.State.Day, Is.EqualTo(day));
            Assert.That(Flow.PeekSave().day, Is.EqualTo(day), $"day {day}: saved on waking");
            Assert.That(Flow.State.Vigor.Current, Is.EqualTo(Flow.VigorSettings.maxVigor), $"day {day}: Vigor full");
            // Gimp's night, if this is the morning for it, plays to its end.
            yield return Frames(3);
            if (NightVisitor.Instance != null && NightVisitor.Instance.Playing)
            {
                m_GimpNights++;
                yield return WaitUntil(() => DialogueManager.IsConversationActive || !NightVisitor.Instance.Playing, 5f, "Gimp's conversation");
                if (DialogueManager.IsConversationActive) DialogueManager.StopConversation();
                yield return WaitUntil(() => !NightVisitor.Instance.Playing, 10f, "the morning after Gimp's night");
            }
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.None), $"day {day}: Tally Ho! is quiet by day");
            SurfaceDoor.Find(SurfaceDoor.FrontInside).Pass(Keeper);
            yield return Frames(3);
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.Day), $"day {day}: the day's tune outdoors");
            VillagePresence.Instance.Refresh();
            yield return Frames(2);
            foreach (string id in k_People)
                Assert.That(Villager.All.Count(v => v.CharacterId == id && v.Shown), Is.EqualTo(1), $"day {day}: one {id}");
            SurfaceDoor.Find(SurfaceDoor.FrontOutside).Pass(Keeper);
            yield return Frames(2);
        }

        int m_GimpNights;

        [UnityTest, Category("Slow"), Timeout(600000)]
        public IEnumerator FiveDays_FromANewGame_SavesGrowsSchedulesGimpOnceAndTheMusic_ThenContinue()
        {
            m_GimpNights = 0;
            yield return RealDaytime();
            yield return Morning(2);
            // Herbs (two days) and barley (four), planted and tended day by day.
            yield return Plant(GardenConfig.Bed1, "herbs");
            yield return Plant(GardenConfig.Bed2, "barley");
            string herbsId = Flow.Database.Crop("herbs").produce.id, barleyId = Flow.Database.Crop("barley").produce.id;
            int herbsBefore = Flow.State.Storeroom.CountMatching(i => i.Definition.id == herbsId);
            int barleyBefore = Flow.State.Storeroom.CountMatching(i => i.Definition.id == barleyId);

            for (int day = 3; day <= 6; day++)
            {
                yield return NextDay();
                yield return Morning(day);
                CropDefinition herbs = Flow.Database.Crop("herbs"), barley = Flow.Database.Crop("barley");
                BedState bed1 = Flow.State.Garden.Bed(GardenConfig.Bed1), bed2 = Flow.State.Garden.Bed(GardenConfig.Bed2);
                if (day == 4)
                {
                    Assert.That(GardenRules.IsReady(bed1, herbs), "herbs ready after two nights");
                    yield return Use(GardenConfig.Bed1);
                    Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == herbsId), Is.GreaterThan(herbsBefore), "herbs into the storeroom");
                }
                if (day < 6)
                {
                    Assert.That(GardenRules.IsReady(bed2, barley), Is.False, $"day {day}: barley still growing");
                    yield return Use(GardenConfig.Bed2);   // tend it
                }
                else
                {
                    Assert.That(GardenRules.IsReady(bed2, barley), "barley ready after four nights");
                    yield return Use(GardenConfig.Bed2);
                    Assert.That(Flow.State.Storeroom.CountMatching(i => i.Definition.id == barleyId), Is.GreaterThan(barleyBefore), "barley harvested");
                }
            }
            Assert.That(m_GimpNights, Is.EqualTo(1), "Gimp's night, once (day 3)");

            // The evening's music, once.
            SurfacePause.Release(ClockHold);
            Flow.StartEvening();
            yield return WaitUntil(() => IsIn(TavernPhase.Prep), 30f, "the evening");
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.None), "Prep is quiet");
            Director.FillStoreroom();
            Director.OpenDebugEvening();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Service, 10f, "service");
            yield return Frames(3);
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.Service));
            Director.EndServiceNow();
            yield return WaitUntil(() => Director.Phase == TavernPhase.Results, 10f, "the results");
            Director.FinishEvening();
            yield return WaitUntil(() => InDungeonNow, 30f, "the delve");
            Assert.That(MusicDirector.Instance.Current, Is.EqualTo(MusicCue.Cellars));
            yield return EmptyDelveToTheNight();

            // Quit at night and Continue: the same game.
            int storeroom = Flow.State.Storeroom.TotalCount;
            yield return SaveQuitAndContinue(() => IsIn(TavernPhase.Night), "the night");
            Assert.That((Flow.State.Day, Flow.State.Storeroom.TotalCount), Is.EqualTo((6, storeroom)));
            Assert.That(Flow.State.Story.SeenHints, Has.Member(CommunityRules.GimpIntro));
            yield return SleepToTheMorning();
            yield return Frames(3);
            Assert.That(NightVisitor.Instance.Playing, Is.False, "and never again");
            Assert.That(Flow.State.Day, Is.EqualTo(7));
        }
    }
}
