using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Animation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>
    /// A figure drawn in layers sorts as one, by its feet (the owner's 4i-B note: Musashi, without a sorting group, drew over the
    /// keeper standing in front of him, because his layers' orders beat the keeper's before position was compared).
    /// </summary>
    public class FigureSortingTests
    {
        [Test]
        public void EveryLayeredFigure_InTheVillageAndTheTavern_SortsAsOne()
        {
            var loose = new List<string>();
            foreach (string path in new[] { KariastonBuilder.ScenePath, EditorPaths.TavernScene })
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (LayeredSpriteAnimator figure in root.GetComponentsInChildren<LayeredSpriteAnimator>(true))
                        if (figure.GetComponentInParent<SortingGroup>(true) == null)
                            loose.Add($"{scene.name}: {figure.transform.parent?.name}/{figure.name}");
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            Assert.That(loose, Is.Empty, string.Join("\n", loose));
        }
    }
}
