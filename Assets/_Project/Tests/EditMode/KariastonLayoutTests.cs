using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Village;
using Hearthdelve.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// Kariaston, the Crossroads (2026-10-10): the ground tiles follow the mockup's own autotile rule, every shadow lies under its
    /// drawing, every tall or solid drawing in the scene is made solid, and the overheard pairs still stand within earshot.
    /// </summary>
    public class KariastonLayoutTests
    {
        static readonly Vector3Int[] k_Around =
        {
            new(0, 1, 0), new(0, -1, 0), new(1, 0, 0), new(-1, 0, 0), new(-1, 1, 0), new(1, 1, 0), new(-1, -1, 0), new(1, -1, 0),
        };

        [Test]
        public void EveryGroundRuleTile_DrawsWhatTheMockupsScriptDraws_InEveryNeighbourhood()
        {
            var go = new GameObject("Rule Test", typeof(Grid));
            try
            {
                var map = new GameObject("Map", typeof(Tilemap)).GetComponent<Tilemap>();
                map.transform.SetParent(go.transform, false);
                foreach (string name in KariastonGround.Autotiles.Keys)
                {
                    var tile = AssetDatabase.LoadAssetAtPath<RuleTile>($"{EditorPaths.Tiles}/{name}.asset");
                    Assert.That(tile, Is.Not.Null, $"{name} exists (run the Kariaston updater)");
                    bool pairs = KariastonGround.Autotiles[name].pairs;
                    for (int mask = 0; mask < 256; mask++)
                    {
                        map.ClearAllTiles();
                        map.SetTile(Vector3Int.zero, tile);
                        for (int i = 0; i < 8; i++)
                            if ((mask & (1 << i)) != 0) map.SetTile(k_Around[i], tile);
                        map.RefreshAllTiles();
                        bool Has(int i) => (mask & (1 << i)) != 0;
                        Vector2Int part = KariastonGround.Part(Has(0), Has(1), Has(2), Has(3), Has(4), Has(5), Has(6), Has(7), pairs);
                        Assert.That(map.GetSprite(Vector3Int.zero), Is.EqualTo(KariastonGround.PartSprite(name, part)), $"{name}, neighbours {mask}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EveryShadow_LiesExactlyUnderItsDrawing()
        {
            int checkedShadows = 0;
            var cuts = KariastonSheets.Shadows.ToDictionary(c => $"{c.file}_{c.name}");
            foreach (SpriteRenderer shadow in ProjectScan.All<SpriteRenderer>(KariastonBuilder.ScenePath)
                         .Where(r => r.name == KariastonBuilder.ShadowName && r.sprite != null && cuts.ContainsKey(r.sprite.name)))
            {
                SpriteRenderer art = shadow.transform.parent.GetComponent<SpriteRenderer>();
                Assert.That(art, Is.Not.Null, $"{shadow.transform.parent.name}'s shadow is under a drawing");
                Assert.That(shadow.transform.localPosition, Is.EqualTo(Vector3.zero), $"{art.name}'s shadow hasn't been moved off it");
                Assert.That(shadow.flipX, Is.EqualTo(art.flipX), $"{art.name}'s shadow flips with it");
                Assert.That(shadow.sortingLayerName, Is.EqualTo(SortingLayers.Floor), $"{art.name}'s shadow is on the ground");
                // The two sprites' corners, in their sheets' pixels, sit as far apart as the cut says (the shadow's pivot is the drawing's).
                KariastonSheets.ShadowCut cut = cuts[shadow.sprite.name];
                Vector2 artCorner = -art.sprite.pivot, shadowCorner = -shadow.sprite.pivot;
                var expected = new Vector2(cut.shadow.x - cut.art.x, (cut.art.y + cut.art.height) - (cut.shadow.y + cut.shadow.height));
                Assert.That(Vector2.Distance(shadowCorner - artCorner, expected), Is.LessThan(0.01f), $"{art.name}: its shadow's offset");
                checkedShadows++;
            }
            Assert.That(checkedShadows, Is.GreaterThan(150), "the village's drawings have their shadows");
        }

        [Test]
        public void EveryTallOrSolidDrawingInKariaston_IsMadeSolid()
        {
            DressingCollision dressing = ProjectScan.All<DressingCollision>(KariastonBuilder.ScenePath).Single();
            var solid = new HashSet<Sprite>(dressing.Solids.Select(s => s.sprite));
            var drawings = new HashSet<Sprite>(KariastonBuilder.TallDrawings.Concat(KariastonBuilder.SmallProps.Select(p => p.name))
                .Concat(KariastonSheets.ThinFence.Select(f => f.name)).Select(KariastonBuilder.Drawing));
            var missing = ProjectScan.All<SpriteRenderer>(KariastonBuilder.ScenePath)
                .Where(r => r.sprite != null && drawings.Contains(r.sprite) && !solid.Contains(r.sprite)).Select(r => r.name).ToList();
            Assert.That(missing, Is.Empty, "every tree, building, prop and fence in the village is solid by what it is");
            foreach (string tall in KariastonBuilder.TallDrawings)
                Assert.That(dressing.Solids.Count(s => s.sprite == KariastonBuilder.Drawing(tall)), Is.GreaterThan(0), $"{tall} hides nobody");
        }

        [Test]
        public void TheOverheardPairs_StandWithinEarshot_InTheirPlaces()
        {
            Vector2 PlaceOf(string character, string doing)
            {
                List<ScheduleBlock> blocks = VillageContent.Schedules.First(s => s.character == character).blocks();
                string anchor = (doing == null ? blocks.First() : blocks.First(b => b.activity == doing)).anchor;
                return VillageContent.KariastonAnchors.First(a => a.id == anchor).at;
            }
            foreach (AmbientMoment m in VillageContent.KariastonMoments())
            {
                float d = Vector2.Distance(PlaceOf(m.first, m.firstDoing), PlaceOf(m.second, m.secondDoing));
                Assert.That(d, Is.LessThanOrEqualTo(m.within), $"{m.conversation}: {d:0.0} tiles apart");
            }
        }
    }
}
