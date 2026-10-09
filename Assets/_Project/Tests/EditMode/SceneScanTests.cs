using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Engine;
using Hearthdelve.UI.Localization;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// Checks of the game's real scenes that only the 4a look-test scenes had (the test review, 2026-10-09): every localized text
    /// names a key its table has, and the lighting follows CLAUDE.md (lit sprites, one enabled global light per sorting layer, also
    /// with Tally Ho! and Kariaston loaded together). Through <see cref="ProjectScan"/>, so no scene is opened twice.
    /// </summary>
    public class SceneScanTests
    {
        const string LitShader = "Universal Render Pipeline/2D/Sprite-Lit-Default";

        /// <summary>
        /// What's drawn unlit on purpose, so it reads in the dark (CLAUDE.md: lighting never costs readability; each is commented so
        /// in its generator): an enemy's "!" telegraph and the Harvest Finisher's drumstick (DungeonContent), the troll's slam and
        /// charge marks (BossContent), the pickups a room leaves (parts, gold, a power's spark, a curio's chest and glint, a quest
        /// object and its marker; DungeonRunBuilder) and a lit window's glow (Kariaston). Anything else unlit is a mistake.
        /// </summary>
        static readonly HashSet<string> k_ReadInTheDark = new()
        {
            "Alert", "FinisherPrompt", "SlamMark", "ChargeMark", "Icon", "Coin", "Spark", "Chest", "Glint", "Object", "Marker", "Lit Window",
        };

        /// <summary>The places the player walks: the world scenes with sprites and lights.</summary>
        static readonly string[] k_World = { EditorPaths.TavernScene, KariastonBuilder.ScenePath, EditorPaths.DungeonScene };

        static string Name(Transform t)
        {
            var parts = new List<string>();
            for (; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        /// <summary>
        /// Every character (the keeper, staff, customers, villagers, enemies) moves on the floor controller, in every prefab and
        /// every game scene (moved from PlayMode's GamepadTests, which loaded the tavern to see the ones there at load).
        /// </summary>
        [Test]
        public void EveryCharacter_UsesTheFloorController()
        {
            var wrong = new List<string>();
            int seen = 0;
            foreach (GameObject prefab in ProjectScan.Prefabs)
            foreach (TopDownController controller in prefab.GetComponentsInChildren<TopDownController>(true))
            {
                seen++;
                if (controller is not FloorController2D) wrong.Add($"{prefab.name}/{controller.name}: {controller.GetType().Name}");
            }
            foreach (string path in ProjectScan.GameScenes)
            foreach (TopDownController controller in ProjectScan.All<TopDownController>(path))
            {
                seen++;
                if (controller is not FloorController2D) wrong.Add($"{Path.GetFileNameWithoutExtension(path)}: {Name(controller.transform)}: {controller.GetType().Name}");
            }
            Assert.That(wrong, Is.Empty, string.Join("\n", wrong));
            Assert.That(seen, Is.GreaterThanOrEqualTo(4), "the keeper (both), staff and customers at least");
        }

        [Test]
        public void EveryLocalizedText_InTheGamesScenes_NamesAKeyItsTableHas()
        {
            var missing = new List<string>();
            int named = 0;
            foreach (string path in ProjectScan.GameScenes)
            foreach (LocalizedSuperText text in ProjectScan.All<LocalizedSuperText>(path))
            {
                if (string.IsNullOrEmpty(text.Key)) continue;
                named++;
                if (!ProjectScan.English(text.Table).ContainsKey(text.Key))
                    missing.Add($"{Path.GetFileNameWithoutExtension(path)}: {Name(text.transform)} wants \"{text.Key}\" from {text.Table}");
            }
            Assert.That(missing, Is.Empty, string.Join("\n", missing));
            Assert.That(named, Is.GreaterThan(300), "the scenes' texts were found");
        }

        /// <summary>
        /// Sprites and tilemaps are lit (the exceptions read in the dark: an enemy's "!" telegraph, the Harvest Finisher's prompt,
        /// and markers on the Above layer): in Tally Ho! and Kariaston, and in every prefab (the Hollows' rooms, enemies and the
        /// keeper are prefabs; the Dungeon scene holds none). Each world scene has its ambient light.
        /// </summary>
        [Test]
        public void TheWorld_DrawsLitSprites_AndEachPlaceHasItsAmbientLight()
        {
            var unlit = new List<string>();
            void Check(string where, IEnumerable<Renderer> found)
            {
                foreach (Renderer r in found.Where(r => r is SpriteRenderer || r is TilemapRenderer)
                             .Where(r => !k_ReadInTheDark.Contains(r.name) && r.sortingLayerName != Hearthdelve.Core.SortingLayers.Above))
                {
                    string shader = r.sharedMaterial != null ? r.sharedMaterial.shader.name : "none";
                    if (shader != LitShader) unlit.Add($"{where}: {Name(r.transform)} uses {shader}");
                }
            }
            foreach (string path in new[] { EditorPaths.TavernScene, KariastonBuilder.ScenePath })
            {
                Renderer[] renderers = ProjectScan.All<Renderer>(path).ToArray();
                Assert.That(renderers.Any(r => r is SpriteRenderer), Path.GetFileNameWithoutExtension(path));
                Check(Path.GetFileNameWithoutExtension(path), renderers);
            }
            foreach (GameObject prefab in ProjectScan.Prefabs) Check($"prefab {prefab.name}", prefab.GetComponentsInChildren<Renderer>(true));
            foreach (string path in k_World)
                Assert.That(ProjectScan.All<Light2D>(path).Count(l => l.lightType == Light2D.LightType.Global), Is.GreaterThanOrEqualTo(1),
                    $"{Path.GetFileNameWithoutExtension(path)}: an ambient light");
            Assert.That(unlit, Is.Empty, string.Join("\n", unlit.Take(40)));
        }

        /// <summary>
        /// URP's 2D renderer allows one enabled global light per sorting layer: in each scene, and with Tally Ho! and Kariaston loaded
        /// together in the daytime (the place not shown has its global light authored disabled).
        /// </summary>
        [Test]
        public void OneEnabledGlobalLight_PerSortingLayer_InEachScene_AndInTheDaytimesPair()
        {
            var problems = new List<string>();
            void Check(string what, IEnumerable<Light2D> lights)
            {
                var enabled = lights.Where(l => l.lightType == Light2D.LightType.Global && l.enabled && l.gameObject.activeInHierarchy).ToList();
                foreach (SortingLayer layer in SortingLayer.layers)
                {
                    var on = enabled.Where(l => l.targetSortingLayers.Contains(layer.id)).ToList();
                    if (on.Count > 1) problems.Add($"{what}, layer {layer.name}: {string.Join(", ", on.Select(l => Name(l.transform)))}");
                }
            }
            foreach (string path in ProjectScan.GameScenes) Check(Path.GetFileNameWithoutExtension(path), ProjectScan.All<Light2D>(path));
            Check("Tavern + Kariaston", ProjectScan.All<Light2D>(EditorPaths.TavernScene).Concat(ProjectScan.All<Light2D>(KariastonBuilder.ScenePath)));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }
    }
}
