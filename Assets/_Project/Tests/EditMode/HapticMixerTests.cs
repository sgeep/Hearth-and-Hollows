using System.Linq;
using Hearthdelve.Core.Animation;
using Hearthdelve.Core.Haptics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class HapticCurveTests
    {
        static readonly HapticKey[] k_Ramp = { new(0f, 0f, 1f), new(0.1f, 1f, 0f) };

        [Test]
        public void Sample_InterpolatesBetweenKeys()
        {
            HapticSample mid = HapticCurve.Sample(k_Ramp, 0.05f);
            Assert.That(mid.Low, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(mid.High, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Sample_IsSilent_BeforeStart_AndFromTheLastKey()
        {
            Assert.That(HapticCurve.Sample(k_Ramp, -0.01f).IsSilent);
            Assert.That(HapticCurve.Sample(k_Ramp, 0.1f).IsSilent);
            Assert.That(HapticCurve.Sample(k_Ramp, 5f).IsSilent);
        }

        [Test]
        public void EmptyPattern_IsSilent_WithZeroDuration()
        {
            Assert.That(HapticCurve.Sample(null, 0f).IsSilent);
            Assert.That(HapticCurve.Sample(new HapticKey[0], 0f).IsSilent);
            Assert.That(HapticCurve.Duration(null), Is.EqualTo(0f));
        }

        [Test]
        public void Duration_IsTheLastKeyTime()
        {
            Assert.That(HapticCurve.Duration(k_Ramp), Is.EqualTo(0.1f));
        }
    }

    public class HapticMixerTests
    {
        static readonly HapticSettings k_On = new(true, 1f, false, 0.4f);
        static readonly HapticKey[] k_Tap = { new(0f, 0.6f, 0.2f), new(0.05f, 0.6f, 0.2f), new(0.1f, 0f, 0f) };

        [Test]
        public void OneShot_Plays_ThenEnds()
        {
            var mixer = new HapticMixer();
            mixer.Play(k_Tap);
            HapticSample first = mixer.Tick(0.02f, k_On);
            Assert.That(first.Low, Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(first.High, Is.EqualTo(0.2f).Within(1e-4f));

            for (int i = 0; i < 10; i++) mixer.Tick(0.02f, k_On);
            Assert.That(mixer.IsIdle);
            Assert.That(mixer.Tick(0.02f, k_On).IsSilent);
        }

        [Test]
        public void Scale_MultipliesTheAuthoredStrength()
        {
            var mixer = new HapticMixer();
            mixer.Play(k_Tap, 0.5f);
            Assert.That(mixer.Tick(0.01f, k_On).Low, Is.EqualTo(0.3f).Within(1e-4f));
        }

        [Test]
        public void Layers_TakeTheStrongest_NotTheSum()
        {
            var mixer = new HapticMixer();
            mixer.SetContinuous("pour", 0.5f, 0.1f);
            mixer.Play(k_Tap);
            HapticSample sample = mixer.Tick(0.01f, k_On);
            Assert.That(sample.Low, Is.EqualTo(0.6f).Within(1e-4f));
            Assert.That(sample.High, Is.EqualTo(0.2f).Within(1e-4f));
        }

        [Test]
        public void Continuous_Holds_UntilSetToZero()
        {
            var mixer = new HapticMixer();
            mixer.SetContinuous("pour", 0.4f, 0f);
            for (int i = 0; i < 5; i++) Assert.That(mixer.Tick(0.1f, k_On).Low, Is.EqualTo(0.4f).Within(1e-4f));
            mixer.SetContinuous("pour", 0f, 0f);
            Assert.That(mixer.Tick(0.1f, k_On).IsSilent);
            Assert.That(mixer.IsIdle);
        }

        [Test]
        public void Settings_Off_SilencesEverything_ButPatternsStillFinish()
        {
            var mixer = new HapticMixer();
            mixer.Play(k_Tap);
            mixer.SetContinuous("pour", 1f, 1f);
            Assert.That(mixer.Tick(0.01f, new HapticSettings(false, 1f, false, 0.4f)).IsSilent);
            mixer.SetContinuous("pour", 0f, 0f);
            for (int i = 0; i < 20; i++) mixer.Tick(0.01f, new HapticSettings(false, 1f, false, 0.4f));
            Assert.That(mixer.IsIdle);
        }

        [Test]
        public void Settings_IntensityAndReducedCap_Apply()
        {
            var mixer = new HapticMixer();
            mixer.SetContinuous("grill", 1f, 1f);
            Assert.That(mixer.Tick(0.01f, new HapticSettings(true, 0.5f, false, 0.4f)).Low, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(mixer.Tick(0.01f, new HapticSettings(true, 1f, true, 0.4f)).High, Is.EqualTo(0.4f).Within(1e-4f));
        }

        [Test]
        public void StopAll_ClearsVoicesAndChannels()
        {
            var mixer = new HapticMixer();
            mixer.Play(k_Tap);
            mixer.SetContinuous("pour", 1f, 1f);
            mixer.StopAll();
            Assert.That(mixer.IsIdle);
            Assert.That(mixer.Tick(0.01f, k_On).IsSilent);
        }

        [Test]
        public void EmptyOrZeroScalePatterns_AreIgnored()
        {
            var mixer = new HapticMixer();
            mixer.Play(null);
            mixer.Play(new HapticKey[0]);
            mixer.Play(k_Tap, 0f);
            Assert.That(mixer.ActiveVoices, Is.EqualTo(0));
        }

        [Test]
        public void NullOutput_IsUnavailable_AndSafeToCall()
        {
            IHapticOutput output = new NullHapticOutput();
            Assert.That(output.IsAvailable, Is.False);
            Assert.DoesNotThrow(() =>
            {
                output.SetMotors(1f, 1f);
                output.Stop();
            });
        }
    }

    public class HapticLibraryAssetTests
    {
        [Test]
        public void Library_HoldsTheWholeStartingVocabulary_WithWellFormedPatterns()
        {
            var library = AssetDatabase.LoadAssetAtPath<HapticLibrary>("Assets/_Project/Data/Haptics/HapticLibrary.asset");
            Assert.That(library, Is.Not.Null, "Run Hearthdelve > Generate > 4a Look Test (All).");

            string[] ids = typeof(HapticIds).GetFields().Select(f => (string)f.GetRawConstantValue()).ToArray();
            Assert.That(ids.Length, Is.EqualTo(18));
            foreach (string id in ids)
            {
                HapticPattern pattern = library.Find(id);
                Assert.That(pattern, Is.Not.Null, id);
                Assert.That(pattern.Duration, Is.GreaterThan(0f), id);
                Assert.That(pattern.Duration, Is.LessThanOrEqualTo(1.5f), id);
                for (int i = 1; i < pattern.keys.Length; i++)
                    Assert.That(pattern.keys[i].time, Is.GreaterThanOrEqualTo(pattern.keys[i - 1].time), $"{id}: keys must be in time order");
                Assert.That(pattern.keys.All(k => k.low >= 0f && k.low <= 1f && k.high >= 0f && k.high <= 1f), id);
            }
        }
    }

    public class SpriteAnimationMathTests
    {
        [TestCase(0f, 0)]
        [TestCase(0.19f, 0)]
        [TestCase(0.2f, 1)]
        [TestCase(0.79f, 3)]
        [TestCase(0.8f, 0)]
        [TestCase(1.0f, 1)]
        public void Looping_WrapsAround(float time, int expected)
        {
            Assert.That(SpriteAnimationMath.FrameAt(time, 4, 0.2f, true), Is.EqualTo(expected));
        }

        [Test]
        public void OneShot_HoldsItsLastFrame_AndReportsFinished()
        {
            Assert.That(SpriteAnimationMath.FrameAt(0.35f, 4, 0.1f, false), Is.EqualTo(3));
            Assert.That(SpriteAnimationMath.FrameAt(9f, 4, 0.1f, false), Is.EqualTo(3));
            Assert.That(SpriteAnimationMath.IsFinished(0.35f, 4, 0.1f, false), Is.False);
            Assert.That(SpriteAnimationMath.IsFinished(0.4f, 4, 0.1f, false), Is.True);
            Assert.That(SpriteAnimationMath.IsFinished(9f, 4, 0.1f, true), Is.False, "loops never finish");
        }

        [Test]
        public void DegenerateInput_ShowsTheFirstFrame()
        {
            Assert.That(SpriteAnimationMath.FrameAt(1f, 0, 0.1f, true), Is.EqualTo(0));
            Assert.That(SpriteAnimationMath.FrameAt(1f, 1, 0.1f, true), Is.EqualTo(0));
            Assert.That(SpriteAnimationMath.FrameAt(1f, 4, 0f, true), Is.EqualTo(0));
            Assert.That(SpriteAnimationMath.FrameAt(-1f, 4, 0.1f, true), Is.EqualTo(0));
        }
    }

    public class MinifantasyImportTests
    {
        [Test]
        public void EveryImportedTexture_IsPointFiltered_Uncompressed_At8PixelsPerUnit()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ThirdParty/Minifantasy" });
            Assert.That(guids, Is.Not.Empty, "Run Hearthdelve > Art > Import Minifantasy.");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(8f), path);
                Assert.That(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Any(), path + " has no sprites");
            }
        }
    }
}
