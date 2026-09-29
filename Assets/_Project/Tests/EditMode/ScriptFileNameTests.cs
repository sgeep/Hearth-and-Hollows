using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// Unity can only serialize a MonoBehaviour/ScriptableObject whose class lives in a file of
    /// the same name. Violations compile fine and even work when added from code, but prefabs
    /// and scenes silently save a broken script reference.
    /// </summary>
    public class ScriptFileNameTests
    {
        [Test]
        public void EverySerializableUnityType_HasAMatchingScriptFile()
        {
            var scriptedTypes = new HashSet<Type>(
                AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/_Project/Scripts" })
                    .Select(guid => AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid)))
                    .Where(script => script != null)
                    .Select(script => script.GetClass())
                    .Where(type => type != null));

            var missing = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("Hearthdelve.") && !a.GetName().Name.StartsWith("Hearthdelve.Tests"))
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsGenericType &&
                            (typeof(MonoBehaviour).IsAssignableFrom(t) || typeof(ScriptableObject).IsAssignableFrom(t)) &&
                            !typeof(UnityEditor.Editor).IsAssignableFrom(t) && !typeof(EditorWindow).IsAssignableFrom(t))
                .Where(t => !scriptedTypes.Contains(t))
                .Select(t => t.FullName)
                .ToList();

            Assert.That(missing, Is.Empty, "Move each of these into a .cs file named after the class:\n" + string.Join("\n", missing));
        }
    }
}
