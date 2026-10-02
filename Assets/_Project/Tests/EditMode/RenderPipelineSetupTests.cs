using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// Guards the locked rendering setup (CLAUDE.md): URP with the 2D Renderer is the project's
    /// pipeline. A missing or dangling pipeline reference makes Unity fall back to the Built-in
    /// pipeline without any error, and builds then strip every URP-tagged shader.
    /// </summary>
    public class RenderPipelineSetupTests
    {
        [Test]
        public void DefaultPipeline_IsTheProjectsUrp2DAsset()
        {
            var expected = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            Assert.That(expected, Is.Not.Null, "Assets/Settings/UniversalRP.asset is missing");
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(expected),
                "Graphics Settings must use the URP 2D asset (Hearthdelve > Setup > Configure Project Settings)");
        }

        [Test]
        public void NoQualityLevel_OverridesThePipeline()
        {
            for (int i = 0; i < QualitySettings.count; i++)
            {
                RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i);
                Assert.That(asset == null || asset == GraphicsSettings.defaultRenderPipeline, $"quality level {i} uses a different pipeline");
            }
        }

        [Test]
        public void Urp2DRenderer_SortsByY()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/Settings/Renderer2D.asset");
            Assert.That(renderer, Is.Not.Null);
            var so = new SerializedObject(renderer);
            Assert.That(so.FindProperty("m_TransparencySortMode").intValue, Is.EqualTo((int)TransparencySortMode.CustomAxis));
            Assert.That(so.FindProperty("m_TransparencySortAxis").vector3Value, Is.EqualTo(Vector3.up));
        }
    }
}
