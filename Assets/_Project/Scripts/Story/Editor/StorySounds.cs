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
        static void Register()
        {
            SoundSwap.EquipScene = Equip;
            SoundSwap.RelevelScene = Relevel;
        }

        public static int Relevel(GameObject root)
        {
            int n = 0;
            float volume = SoundBank.Volume(SoundBank.Get("Blip")), low = SoundBank.Volume(SoundBank.Get("Blip.Low"));
            AudioClip[] high = SoundBank.Clips(SoundBank.Get("Blip")), lowClips = SoundBank.Clips(SoundBank.Get("Blip.Low"));
            foreach (DialogueBlips blips in root.GetComponentsInChildren<DialogueBlips>(true))
            {
                // Round 2: the synth blips (both registers, no jitter, the 0.079 s gap) replace round 1's plucks.
                if (!System.Linq.Enumerable.SequenceEqual(blips.HighClips ?? new AudioClip[0], high)
                    || !System.Linq.Enumerable.SequenceEqual(blips.LowClips ?? new AudioClip[0], lowClips))
                {
                    blips.Configure(high, lowClips);
                    EditorUtility.SetDirty(blips);
                    n++;
                }
                if (Mathf.Approximately(blips.Volume, volume) && Mathf.Approximately(blips.LowVolume, low)) continue;
                blips.Volume = volume;
                blips.LowVolume = low;
                EditorUtility.SetDirty(blips);
                n++;
            }
            return n;
        }

        public static int Equip(GameObject root)
        {
            int n = 0;
            foreach (HearthDialogueUI box in root.GetComponentsInChildren<HearthDialogueUI>(true))
            {
                if (box.GetComponentInChildren<DialogueBlips>(true) != null) continue;
                var go = new GameObject("Blips");
                go.transform.SetParent(box.transform, false);
                SoundSwap.EffectsSource(go);
                go.AddComponent<DialogueBlips>().Configure(SoundBank.Clips(SoundBank.Get("Blip")), SoundBank.Clips(SoundBank.Get("Blip.Low")));
                EditorUtility.SetDirty(box);
                n++;
            }
            return n;
        }
    }
}
