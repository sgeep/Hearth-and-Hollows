using System;
using System.Linq;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The tavern's feedback pass (4c step 6): placeholder sounds (generated, named PH_ until real audio is sourced), the
    /// feedback tuning asset, and the <see cref="TavernFeedback"/> object with one combined feedback per moment (sound,
    /// visuals and a named haptic pattern in the same MMF player). Rebuilt in place by the tavern updater.
    /// </summary>
    public static class TavernFeedbackContent
    {
        const string k_ConfigPath = EditorPaths.Data + "/Tavern/TavernFeedbackConfig.asset";

        static float Sin(float t, float hz) => Mathf.Sin(t * 2f * Mathf.PI * hz);

        /// <summary>The tavern's placeholder sounds (written once; delete one to regenerate it).</summary>
        public static void BuildSounds()
        {
            EditorPaths.Ensure(EditorPaths.Audio);
            Func<int, float> noise = LookTestContent.Noise;
            // The Butcher Block (4f Checkpoint C): a soft bite as the knife goes in; a deep, clean cleave (a thock through
            // to the wood); the cuts sliding apart (three soft slaps).
            LookTestContent.WriteWav("PH_KnifeIn", 0.1f, (t, n) => noise(n) * Mathf.Exp(-t * 40f) * 0.3f + Sin(t, 300f) * Mathf.Exp(-t * 60f) * 0.2f);
            LookTestContent.WriteWav("PH_Cleave", 0.28f, (t, n) => noise(n) * Mathf.Exp(-t * 30f) * 0.5f + Sin(t, 120f - 40f * t) * Mathf.Exp(-t * 14f) * 0.8f);
            LookTestContent.WriteWav("PH_ButcherDone", 0.6f, (t, n) =>
            {
                float local = t % 0.16f;
                return t < 0.48f ? (noise(n) * 0.35f + Sin(t, 180f) * 0.4f) * Mathf.Exp(-local * 30f) * 0.7f : 0f;
            });
            // Grill: a short hiss when the meat turns; brighter for a perfect flip; a harsh hiss and thump when it burns.
            LookTestContent.WriteWav("PH_Flip", 0.16f, (t, n) => noise(n) * Mathf.Exp(-t * 22f) * 0.45f + Sin(t, 180f) * Mathf.Exp(-t * 40f) * 0.3f);
            LookTestContent.WriteWav("PH_FlipPerfect", 0.32f, (t, n) => noise(n) * Mathf.Exp(-t * 22f) * 0.35f + Sin(t, t < 0.1f ? 880f : 1320f) * Mathf.Exp(-t * 8f) * 0.35f);
            LookTestContent.WriteWav("PH_Burn", 0.4f, (t, n) => noise(n) * Mathf.Exp(-t * 5f) * 0.55f + Sin(t, 70f) * Mathf.Exp(-t * 10f) * 0.5f);
            // Loops: the sizzle on the grill and the stream from the tap (noise, so they loop without a seam).
            LookTestContent.WriteWav("PH_SizzleLoop", 1f, (t, n) => noise(n) * (0.22f + 0.06f * Mathf.Sin(t * 2f * Mathf.PI * 7f)));
            LookTestContent.WriteWav("PH_PourLoop", 1f, (t, n) => (noise(n) * 0.5f + noise(n + 1) * 0.5f) * 0.25f + Sin(t, 220f) * 0.03f);
            // Tap: a glass ting at the line; a clink when it's done; a splash when it overflows.
            LookTestContent.WriteWav("PH_LineTing", 0.12f, (t, n) => Sin(t, 1760f) * Mathf.Exp(-t * 30f) * 0.4f);
            LookTestContent.WriteWav("PH_Clink", 0.3f, (t, n) => (Sin(t, 1400f) + Sin(t, 2100f) * 0.5f) * Mathf.Exp(-t * 12f) * 0.35f);
            LookTestContent.WriteWav("PH_Splash", 0.35f, (t, n) => noise(n) * Mathf.Sin(t / 0.35f * Mathf.PI) * 0.5f);
            // Chop: a clean wooden thock, a dull ragged double thud, a little flourish when the board is done.
            LookTestContent.WriteWav("PH_Chop", 0.1f, (t, n) => (Sin(t, 420f) * 0.6f + noise(n) * 0.4f) * Mathf.Exp(-t * 45f) * 0.7f);
            LookTestContent.WriteWav("PH_ChopRagged", 0.2f, (t, n) => (Sin(t, 150f) * Mathf.Exp(-t * 30f) + (t > 0.07f ? Sin(t, 130f) * Mathf.Exp(-(t - 0.07f) * 30f) : 0f)) * 0.5f);
            LookTestContent.WriteWav("PH_ChopDone", 0.3f, (t, n) => Sin(t, t < 0.12f ? 660f : 880f) * Mathf.Exp(-t * 8f) * 0.35f);
            // The stew pot ready: a bubble and a soft bell.
            LookTestContent.WriteWav("PH_StewReady", 0.45f, (t, n) => Sin(t, 90f + 60f * Mathf.Sin(t * 30f)) * Mathf.Exp(-t * 14f) * 0.4f + Sin(t, 990f) * Mathf.Exp(-t * 6f) * 0.2f);
            // Plates: up and down on the pass, a soft and a firm bump, a wobble near falling, a crash, a warm serve.
            LookTestContent.WriteWav("PH_PlateUp", 0.12f, (t, n) => Sin(t, 1250f) * Mathf.Exp(-t * 35f) * 0.3f);
            LookTestContent.WriteWav("PH_PlateDown", 0.14f, (t, n) => Sin(t, 900f) * Mathf.Exp(-t * 30f) * 0.25f);
            LookTestContent.WriteWav("PH_Bump", 0.12f, (t, n) => Sin(t, 110f) * Mathf.Exp(-t * 35f) * 0.6f + noise(n) * Mathf.Exp(-t * 80f) * 0.15f);
            LookTestContent.WriteWav("PH_SpillWarn", 0.3f, (t, n) => Sin(t, 700f + 120f * Mathf.Sin(t * 50f)) * Mathf.Exp(-t * 7f) * 0.3f);
            LookTestContent.WriteWav("PH_Crash", 0.5f, (t, n) => noise(n) * Mathf.Exp(-t * 7f) * 0.7f + Sin(t, 1100f) * Mathf.Exp(-t * 20f) * 0.2f);
            LookTestContent.WriteWav("PH_Serve", 0.36f, (t, n) => Sin(t, t < 0.1f ? 523f : t < 0.2f ? 659f : 784f) * Mathf.Exp(-(t % 0.1f) * 10f) * 0.3f);
            // The room: a coin for a payment, a sour fall for a walkout.
            LookTestContent.WriteWav("PH_Coin", 0.22f, (t, n) => (Sin(t, 1900f) + Sin(t, 2500f) * 0.6f) * Mathf.Exp(-t * 14f) * 0.25f);
            LookTestContent.WriteWav("PH_Walkout", 0.4f, (t, n) => Sin(t, 330f - 220f * t) * Mathf.Exp(-t * 6f) * 0.3f);
            // Menus: a click, a purchase chime, a count tick, the takings chime.
            LookTestContent.WriteWav("PH_UiConfirm", 0.06f, (t, n) => Sin(t, 1000f) * Mathf.Exp(-t * 70f) * 0.35f);
            LookTestContent.WriteWav("PH_UiBuy", 0.4f, (t, n) => Sin(t, t < 0.12f ? 660f : 990f) * Mathf.Exp(-t * 7f) * 0.35f);
            LookTestContent.WriteWav("PH_UiTick", 0.04f, (t, n) => Sin(t, 1500f) * Mathf.Exp(-t * 90f) * 0.25f);
            LookTestContent.WriteWav("PH_UiChime", 0.5f, (t, n) => (Sin(t, 784f) + Sin(t, 1175f) * 0.6f) * Mathf.Exp(-t * 5f) * 0.3f);
            // Decorating (4f): a soft lift, a wooden settle, a turning click, a dull "no", a shelf thunk, a short rewind.
            LookTestContent.WriteWav("PH_FurnitureLift", 0.14f, (t, n) => Sin(t, 300f + 900f * t) * Mathf.Exp(-t * 25f) * 0.25f + noise(n) * Mathf.Exp(-t * 60f) * 0.08f);
            LookTestContent.WriteWav("PH_FurniturePlace", 0.22f, (t, n) => (Sin(t, 120f) * 0.7f + noise(n) * 0.3f) * Mathf.Exp(-t * 28f) * 0.7f);
            LookTestContent.WriteWav("PH_FurnitureTurn", 0.06f, (t, n) => Sin(t, 1300f) * Mathf.Exp(-t * 80f) * 0.25f);
            LookTestContent.WriteWav("PH_FurnitureNo", 0.18f, (t, n) => (Sin(t, 160f) + Sin(t, 170f)) * Mathf.Exp(-t * 18f) * 0.25f);
            LookTestContent.WriteWav("PH_FurnitureStore", 0.2f, (t, n) => (Sin(t, 220f - 120f * t) * 0.6f + noise(n) * 0.2f) * Mathf.Exp(-t * 20f) * 0.5f);
            LookTestContent.WriteWav("PH_FurnitureUndo", 0.12f, (t, n) => Sin(t, 1200f - 3000f * t) * Mathf.Exp(-t * 25f) * 0.25f);
        }

        static HapticPattern Pattern(string id) => LookTestContent.Pattern(id);
        static AudioClip Sfx(string name) => LookTestContent.Sfx(name);

        /// <summary>A moment: its sound and haptic together, with a shake for the big ones; <paramref name="scaled"/> haptics follow the moment's intensity.</summary>
        static MMF_Player Moment(Transform parent, string name, string sound, string haptic, float shake = 0f, bool scaled = false)
        {
            MMF_Player player = LookTestContent.Feedback(parent, name, null, shake, Sfx(sound), haptic != null ? Pattern(haptic) : null);
            if (scaled)
                foreach (MMF_HapticPattern pattern in player.FeedbacksList.OfType<MMF_HapticPattern>())
                    pattern.UseFeedbackIntensity = true;
            return player;
        }

        static AudioSource Loop(Transform parent, string name, string sound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = Sfx(sound);
            // 4i-C: the approved loop, where there is one (the tap's pour); the sizzle stays a placeholder.
            if (SoundBank.ByPlaceholder.TryGetValue(sound, out string family))
            {
                AudioClip approved = System.Linq.Enumerable.FirstOrDefault(SoundBank.Clips(SoundBank.Get(family)));
                if (approved != null) source.clip = approved;
            }
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.outputAudioMixerGroup = AudioMixerBuilder.Effects;
            return source;
        }

        /// <summary>
        /// Decorate Mode's feedback under <paramref name="parent"/> (4f step 2): placing is the firm one; lifting, putting away
        /// and taking out are light; a turn is only heard; "it won't go there" buzzes. Entering and leaving use the UI's sounds.
        /// </summary>
        public static DecorateFeedback BuildDecorate(Transform parent)
        {
            BuildSounds();
            // Restyling: a short soft brush of noise (4f step 5).
            LookTestContent.WriteWav("PH_Brush", 0.2f, (t, n) => LookTestContent.Noise(n) * Mathf.Sin(t / 0.2f * Mathf.PI) * 0.22f);
            // A trophy's homecoming (4f Checkpoint C): a heavy wooden thud, then a bright, warm chord.
            LookTestContent.WriteWav("PH_Homecoming", 1.2f, (t, n) =>
                (LookTestContent.Noise(n) * 0.5f + Sin(t, 70f - 30f * t) * 0.9f) * Mathf.Exp(-t * 14f) * 0.8f
                + (t > 0.18f ? (Sin(t, 523f) + Sin(t, 659f) * 0.8f + Sin(t, 784f) * 0.6f) * Mathf.Exp(-(t - 0.18f) * 3f) * 0.16f : 0f));
            var root = new GameObject("Feedback").transform;
            root.SetParent(parent, false);
            var moments = new DecorateMoments
            {
                enter = Moment(root, "Feedback_Enter", "PH_UiConfirm", null),
                leave = Moment(root, "Feedback_Leave", "PH_UiChime", null),
                pickUp = Moment(root, "Feedback_PickUp", "PH_FurnitureLift", HapticIds.TapLight),
                place = Moment(root, "Feedback_Place", "PH_FurniturePlace", HapticIds.TapFirm),
                turn = Moment(root, "Feedback_Turn", "PH_FurnitureTurn", null),
                flip = Moment(root, "Feedback_Flip", "PH_FurnitureTurn", null),
                invalid = Moment(root, "Feedback_Invalid", "PH_FurnitureNo", HapticIds.BuzzFailure),
                store = Moment(root, "Feedback_Store", "PH_FurnitureStore", HapticIds.TapLight),
                fromStorage = Moment(root, "Feedback_FromStorage", "PH_FurnitureLift", HapticIds.TapLight),
                undo = Moment(root, "Feedback_Undo", "PH_FurnitureUndo", null),
                putBack = Moment(root, "Feedback_PutBack", "PH_FurniturePlace", HapticIds.TapLight),
                buy = Moment(root, "Feedback_Buy", "PH_UiBuy", HapticIds.PulseSuccess),
                sell = Moment(root, "Feedback_Sell", "PH_Coin", HapticIds.TapLight),
                restyle = Moment(root, "Feedback_Restyle", "PH_Brush", HapticIds.TapLight),
                finish = Moment(root, "Feedback_Finish", "PH_Brush", HapticIds.TapFirm),
                area = Moment(root, "Feedback_Area", "PH_Whoosh", null),
                homecoming = Moment(root, "Feedback_Homecoming", "PH_Homecoming", HapticIds.Homecoming),
            };
            var feedback = root.gameObject.AddComponent<DecorateFeedback>();
            feedback.Configure(moments);
            return feedback;
        }

        /// <summary>Builds (or rebuilds) the tavern's feedback object under <paramref name="parent"/>.</summary>
        public static TavernFeedback Build(Transform parent)
        {
            BuildSounds();
            TavernFeedbackConfig config = LookTestContent.LoadOrCreate<TavernFeedbackConfig>(k_ConfigPath, null);
            Transform old = parent.Find("Feedback");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Feedback").transform;
            root.SetParent(parent, false);

            var moments = new TavernMoments
            {
                flip = Moment(root, "Feedback_Flip", "PH_Flip", HapticIds.TapLight),
                perfectFlip = Moment(root, "Feedback_PerfectFlip", "PH_FlipPerfect", HapticIds.PulseSuccess),
                burned = Moment(root, "Feedback_Burned", "PH_Burn", HapticIds.BuzzFailure, 0.1f),
                lineReached = Moment(root, "Feedback_LineReached", "PH_LineTing", HapticIds.CueThreshold),
                cleanPour = Moment(root, "Feedback_CleanPour", "PH_Clink", HapticIds.PulseSuccess),
                pourDone = Moment(root, "Feedback_PourDone", "PH_Clink", HapticIds.TapLight),
                overflow = Moment(root, "Feedback_Overflow", "PH_Splash", HapticIds.BuzzFailure),
                cleanCut = Moment(root, "Feedback_CleanCut", "PH_Chop", HapticIds.TapFirm),
                raggedCut = Moment(root, "Feedback_RaggedCut", "PH_ChopRagged", HapticIds.CutRagged),
                chopDone = Moment(root, "Feedback_ChopDone", "PH_ChopDone", HapticIds.PulseSuccess, scaled: true),
                // The Butcher Block (4f Checkpoint C): the knife biting in, a clean cleave or a ragged hack, the cuts sliding apart.
                butcherStroke = Moment(root, "Feedback_ButcherStroke", "PH_KnifeIn", HapticIds.TapLight),
                butcherClean = Moment(root, "Feedback_ButcherClean", "PH_Cleave", HapticIds.CutClean),
                butcherRagged = Moment(root, "Feedback_ButcherRagged", "PH_ChopRagged", HapticIds.CutRagged),
                butcherDone = Moment(root, "Feedback_ButcherDone", "PH_ButcherDone", HapticIds.PulseSuccess, scaled: true),
                stewReady = Moment(root, "Feedback_StewReady", "PH_StewReady", null),
                pickUp = Moment(root, "Feedback_PickUp", "PH_PlateUp", HapticIds.TapLight),
                putBack = Moment(root, "Feedback_PutBack", "PH_PlateDown", HapticIds.TapLight),
                softBump = Moment(root, "Feedback_SoftBump", "PH_Bump", HapticIds.BumpSoft, scaled: true),
                hardBump = Moment(root, "Feedback_HardBump", "PH_Bump", HapticIds.BumpHard, 0.05f, scaled: true),
                spillWarning = Moment(root, "Feedback_SpillWarning", "PH_SpillWarn", HapticIds.BumpHard),
                dropped = Moment(root, "Feedback_Dropped", "PH_Crash", HapticIds.BuzzFailure, 0.15f),
                served = Moment(root, "Feedback_Served", "PH_Serve", HapticIds.TapFirm),
                paid = Moment(root, "Feedback_Paid", "PH_Coin", null),
                walkout = Moment(root, "Feedback_Walkout", "PH_Walkout", null),
            };
            var feedback = root.gameObject.AddComponent<TavernFeedback>();
            feedback.Configure(config, moments, Loop(root, "SizzleLoop", "PH_SizzleLoop"), Loop(root, "PourLoop", "PH_PourLoop"));
            // 4i-C: doors and stairs heard.
            SoundSwap.EquipPassages(root);
            return feedback;
        }
    }
}
