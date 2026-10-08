using Hearthdelve.Editor;
using Hearthdelve.Story.Presentation;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Story.Editor
{
    /// <summary>4i-C: the dialogue box's blips, added by <see cref="SoundSwap"/> in place (this assembly can see the box).</summary>
    public static class StorySounds
    {
        [InitializeOnLoadMethod]
        static void Register() => SoundSwap.EquipScene = Equip;

        public static int Equip(GameObject root)
        {
            int n = 0;
            foreach (HearthDialogueUI box in root.GetComponentsInChildren<HearthDialogueUI>(true))
            {
                if (box.GetComponentInChildren<DialogueBlips>(true) != null) continue;
                var go = new GameObject("Blips");
                go.transform.SetParent(box.transform, false);
                SoundSwap.EffectsSource(go);
                go.AddComponent<DialogueBlips>().Configure(SoundBank.Clips(SoundBank.Get("Blip")));
                EditorUtility.SetDirty(box);
                n++;
            }
            return n;
        }
    }
}
