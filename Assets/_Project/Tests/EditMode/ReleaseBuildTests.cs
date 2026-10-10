using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Game;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using NUnit.Framework;
using UnityEditor;

namespace Hearthdelve.Tests
{
    /// <summary>4i-D: the builds are what decision D7 says, the version is stamped in one shape, and both menus show it.</summary>
    public class ReleaseBuildTests
    {
        [Test]
        public void TheWebRelease_IsBrotli_WithTheFallback_AndNoDevelopmentCode()
        {
            var s = ReleaseBuilds.For(ReleaseBuilds.Kind.WebRelease);
            Assert.That((s.Target, s.IsDevelopment), Is.EqualTo((BuildTarget.WebGL, false)));
            Assert.That(s.Compression, Is.EqualTo(WebGLCompressionFormat.Brotli));
            Assert.That(s.DecompressionFallback, "for hosts that don't send Brotli's headers");
            Assert.That(s.Location, Is.Not.EqualTo(ReleaseBuilds.For(ReleaseBuilds.Kind.WebDevelopment).Location), "the release never overwrites the owner's development build");
        }

        [Test]
        public void TheDevelopmentWebBuild_StaysAvailable()
        {
            var s = ReleaseBuilds.For(ReleaseBuilds.Kind.WebDevelopment);
            Assert.That((s.Target, s.IsDevelopment, s.Compression), Is.EqualTo((BuildTarget.WebGL, true, WebGLCompressionFormat.Disabled)));
        }

        [Test]
        public void Windows_IsIL2CPPForTesters_AndMonoInternally()
        {
            var testers = ReleaseBuilds.For(ReleaseBuilds.Kind.Windows);
            var internal_ = ReleaseBuilds.For(ReleaseBuilds.Kind.WindowsInternal);
            Assert.That((testers.Target, testers.Backend, testers.IsDevelopment), Is.EqualTo((BuildTarget.StandaloneWindows64, ScriptingImplementation.IL2CPP, false)));
            Assert.That((internal_.Target, internal_.Backend), Is.EqualTo((BuildTarget.StandaloneWindows64, ScriptingImplementation.Mono2x)));
            Assert.That(testers.Location, Does.EndWith(ReleaseBuilds.ExeName));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Hearthdelve"), "the Product Name sets the save folder (D7): it stays");
        }

        [Test]
        public void TheVersion_IsTheMilestone_TheBuildAndTheCommit()
        {
            Assert.That(VersionStamp.Format("0.4i-d", 388, "4fafd6c9e1", false), Is.EqualTo("0.4i-d.388+4fafd6c9"));
            Assert.That(VersionStamp.Format("0.4i-d", 388, "4fafd6c9", true), Is.EqualTo("0.4i-d.388+4fafd6c9-dirty"));
            Assert.That(VersionStamp.IsStamped("0.4i-d.388+4fafd6c9"));
            Assert.That(VersionStamp.IsStamped("0.4i-d.388+4fafd6c9-dirty"));
            Assert.That(VersionStamp.IsStamped("0.4i-d"), Is.False, "the editor's milestone isn't a build's stamp");
            Assert.That(ReleaseBuilds.CurrentVersion(), Does.StartWith(VersionStamp.Milestone + "."), "git gives this checkout's stamp");
            Assert.That(VersionStamp.IsStamped(ReleaseBuilds.CurrentVersion()), ReleaseBuilds.CurrentVersion());
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo(VersionStamp.Milestone), "a build puts the project's version back");
        }

        [Test]
        public void BothMenus_ShowTheVersion()
        {
            var main = ProjectScan.All<MainMenuScreen>(BootBuilder.MainMenuScene).Single();
            var mainText = ProjectScan.All<LocalizedSuperText>(BootBuilder.MainMenuScene).Single(t => t.name == "Version");
            Assert.That(mainText.Key, Is.EqualTo(MenuLocKeys.Version));
            Assert.That(main, Is.Not.Null);
            PauseMenu pause = ProjectScan.All<PauseMenu>(BootBuilder.BootScene).Single();
            Assert.That(pause.Version, Is.Not.Null, "the pause menu shows it too");
            Assert.That(pause.Version.Key, Is.EqualTo(MenuLocKeys.Version));
            // Wide enough for a stamped version on one line.
            float width = ((UnityEngine.RectTransform)pause.Version.transform).rect.width;
            Assert.That(width, Is.GreaterThanOrEqualTo(150f));
            Assert.That(((UnityEngine.RectTransform)mainText.transform).rect.width, Is.GreaterThanOrEqualTo(150f));
        }
    }
}
