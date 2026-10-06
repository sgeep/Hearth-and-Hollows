using System;
using System.Collections.Generic;
using System.IO;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Shared.Ingredients;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Data assets and prefabs for the 4a look test: haptic patterns, placeholder sounds,
    /// sprite animation sets, tiles, and the player, slime, pickup and NPC prefabs.
    /// Data assets are only created (tuning is kept); animation sets, tiles and prefabs are
    /// rebuilt every run.
    /// </summary>
    public static class LookTestContent
    {
        public const string PlayerPrefab = EditorPaths.Prefabs + "/Player/Player.prefab";
        public const string TavernPlayerPrefab = EditorPaths.Prefabs + "/Player/PlayerTavern.prefab";
        public const string CleaverPrefab = EditorPaths.Prefabs + "/Weapons/ButchersCleaver.prefab";
        public const string SlimePrefab = EditorPaths.Prefabs + "/Enemies/GreenSlime.prefab";
        public const string PickupPrefab = EditorPaths.Prefabs + "/Pickups/IngredientPickup.prefab";
        public const string CookPrefab = EditorPaths.Prefabs + "/Tavern/Cook.prefab";
        public const string LibraryPath = EditorPaths.Haptics + "/HapticLibrary.asset";
        public const string MoveConfigPath = EditorPaths.Config + "/PlayerMoveConfig.asset";

        // ------------------------------------------------------------------ helpers

        /// <summary>Loads the asset, or creates it with <paramref name="init"/>. Existing assets are left alone.</summary>
        public static T LoadOrCreate<T>(string path, Action<T> init = null) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EditorPaths.Ensure(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            init?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Loads or creates the asset and applies <paramref name="apply"/> every run (generated content).</summary>
        public static T CreateOrUpdate<T>(string path, Action<T> apply) where T : ScriptableObject
        {
            T asset = LoadOrCreate<T>(path);
            apply(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        public static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogError($"[Hearthdelve] Missing asset: {path}");
            return asset;
        }

        /// <summary>The URP 2D lit sprite material, so sprites respond to 2D lights.</summary>
        public static Material LitSpriteMaterial =>
            GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.default2DMaterial : null;

        public static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, string sortingLayer, int order, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            if (LitSpriteMaterial != null) renderer.sharedMaterial = LitSpriteMaterial;
            return renderer;
        }

        internal static GameObject SavePrefab(GameObject root, string path)
        {
            EditorPaths.Ensure(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ haptics

        static readonly (string id, HapticKey[] keys)[] k_Patterns =
        {
            (HapticIds.TapLight, Keys((0f, 0f, 0.5f), (0.03f, 0f, 0.5f), (0.05f, 0f, 0f))),
            (HapticIds.TapFirm, Keys((0f, 0.6f, 0.6f), (0.05f, 0.6f, 0.6f), (0.09f, 0f, 0f))),
            (HapticIds.HitHeavy, Keys((0f, 1f, 0.4f), (0.08f, 0.9f, 0.3f), (0.22f, 0f, 0f))),
            (HapticIds.HitTaken, Keys((0f, 0.9f, 0.2f), (0.06f, 0.5f, 0f), (0.2f, 0f, 0f))),
            (HapticIds.KillClean, Keys((0f, 0.6f, 0.6f), (0.05f, 0.6f, 0.6f), (0.08f, 0f, 0f), (0.14f, 0f, 0f), (0.15f, 0f, 0.5f), (0.22f, 0f, 0.9f), (0.24f, 0f, 0f))),
            (HapticIds.FinisherHarvest, Keys((0f, 0.1f, 0f), (0.35f, 0.6f, 0f), (0.36f, 0f, 0f), (0.48f, 0f, 0f), (0.49f, 1f, 0.8f), (0.57f, 1f, 0.8f), (0.58f, 0f, 0f), (0.66f, 0f, 0f), (0.67f, 1f, 0.8f), (0.77f, 1f, 0.8f), (0.8f, 0f, 0f))),
            (HapticIds.PulseSuccess, Keys((0f, 0f, 0.4f), (0.05f, 0f, 0.4f), (0.06f, 0f, 0f), (0.12f, 0f, 0f), (0.13f, 0f, 0.8f), (0.2f, 0f, 0.8f), (0.22f, 0f, 0f))),
            (HapticIds.BuzzFailure, Keys((0f, 0.7f, 0f), (0.05f, 0.3f, 0f), (0.1f, 0.7f, 0f), (0.15f, 0.3f, 0f), (0.2f, 0.7f, 0f), (0.25f, 0.3f, 0f), (0.3f, 0.6f, 0f), (0.36f, 0f, 0f))),
            (HapticIds.CueThreshold, Keys((0f, 0f, 0.7f), (0.03f, 0f, 0.7f), (0.04f, 0f, 0f))),
            (HapticIds.BumpSoft, Keys((0f, 0.35f, 0f), (0.05f, 0.35f, 0f), (0.08f, 0f, 0f))),
            (HapticIds.BumpHard, Keys((0f, 0.8f, 0.2f), (0.07f, 0.8f, 0.2f), (0.12f, 0f, 0f))),
            (HapticIds.CutRagged, Keys((0f, 0.5f, 0f), (0.04f, 0.5f, 0f), (0.05f, 0f, 0f), (0.09f, 0f, 0f), (0.1f, 0.3f, 0f), (0.16f, 0.3f, 0f), (0.17f, 0f, 0f))),
            (HapticIds.HeartbeatWarning, Keys((0f, 0.6f, 0f), (0.06f, 0.6f, 0f), (0.07f, 0f, 0f), (0.17f, 0f, 0f), (0.18f, 0.4f, 0f), (0.24f, 0.4f, 0f), (0.26f, 0f, 0f))),
            (HapticIds.BossTelegraph, Keys((0f, 0f, 0f), (0.6f, 0.7f, 0.1f), (0.7f, 0f, 0f))),
            (HapticIds.BossPhaseChange, Keys((0f, 0.3f, 0.1f), (0.5f, 1f, 0.6f), (0.6f, 1f, 0.6f), (1.2f, 0.2f, 0f), (1.3f, 0f, 0f))),
            // 4f Checkpoint C.
            (HapticIds.DiscoveryFound, Keys((0f, 0f, 0.6f), (0.03f, 0f, 0.6f), (0.04f, 0f, 0f), (0.09f, 0f, 0f), (0.1f, 0f, 0.8f), (0.13f, 0f, 0.8f), (0.14f, 0f, 0f), (0.2f, 0f, 0f), (0.21f, 0.2f, 1f), (0.3f, 0.2f, 1f), (0.34f, 0f, 0f))),
            (HapticIds.CutClean, Keys((0f, 0.8f, 0.9f), (0.04f, 0.8f, 0.9f), (0.06f, 0f, 0f))),
            (HapticIds.Homecoming, Keys((0f, 0.5f, 0f), (0.15f, 0.9f, 0.2f), (0.3f, 0.6f, 0.1f), (0.6f, 0.3f, 0f), (0.8f, 0f, 0f))),
        };

        static HapticKey[] Keys(params (float time, float low, float high)[] keys)
        {
            var result = new HapticKey[keys.Length];
            for (int i = 0; i < keys.Length; i++) result[i] = new HapticKey(keys[i].time, keys[i].low, keys[i].high);
            return result;
        }

        /// <summary>The starting haptic vocabulary (GDD §9A) and the library that lists it.</summary>
        public static HapticLibrary BuildHaptics()
        {
            var patterns = new List<HapticPattern>();
            foreach (var (id, keys) in k_Patterns)
            {
                patterns.Add(LoadOrCreate<HapticPattern>($"{EditorPaths.Haptics}/Haptic_{id.Replace('.', '_')}.asset", p =>
                {
                    p.id = id;
                    p.keys = keys;
                }));
            }
            return CreateOrUpdate<HapticLibrary>(LibraryPath, library =>
            {
                foreach (HapticPattern pattern in patterns)
                    if (!library.patterns.Contains(pattern)) library.patterns.Add(pattern);
                library.patterns.RemoveAll(p => p == null);
            });
        }

        public static HapticPattern Pattern(string id) => Load<HapticPattern>($"{EditorPaths.Haptics}/Haptic_{id.Replace('.', '_')}.asset");

        // ------------------------------------------------------------------ placeholder sound

        /// <summary>Generated placeholder SFX (no audio source exists yet). Named PH_ so they are easy to replace.</summary>
        public static void BuildPlaceholderAudio()
        {
            EditorPaths.Ensure(EditorPaths.Audio);
            WriteWav("PH_Hit", 0.12f, (t, n) => Noise(n) * Mathf.Exp(-t * 38f) * 0.8f + Mathf.Sin(t * 2f * Mathf.PI * 140f) * Mathf.Exp(-t * 30f) * 0.5f);
            WriteWav("PH_Hurt", 0.2f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (220f - 500f * t)) * Mathf.Exp(-t * 14f) * 0.7f);
            WriteWav("PH_Pickup", 0.14f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.06f ? 880f : 1320f)) * Mathf.Exp(-t * 16f) * 0.5f);
            // An enemy winding up: a rising two-note warning.
            WriteWav("PH_Telegraph", 0.22f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.1f ? 520f : 780f)) * (1f - t / 0.22f) * 0.45f);
            // Low Essence: two soft low thumps.
            WriteWav("PH_Heartbeat", 0.35f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * 55f) * (Mathf.Exp(-t * 30f) + (t > 0.16f ? Mathf.Exp(-(t - 0.16f) * 30f) * 0.7f : 0f)) * 0.8f);
            // Climbing out: a rising chime.
            WriteWav("PH_Climb", 0.5f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (440f + 880f * t)) * (1f - t / 0.5f) * 0.4f);
            // The heavy spin: a filtered noise sweep.
            WriteWav("PH_Whoosh", 0.3f, (t, n) => Noise(n) * Mathf.Sin(t / 0.3f * Mathf.PI) * 0.5f);
            // A heavy landing: lower and longer than PH_Hit.
            WriteWav("PH_HitHeavy", 0.22f, (t, n) => Noise(n) * Mathf.Exp(-t * 22f) * 0.7f + Mathf.Sin(t * 2f * Mathf.PI * (90f - 120f * t)) * Mathf.Exp(-t * 12f) * 0.8f);
            // A clean kill: a bright two-note ring.
            WriteWav("PH_KillClean", 0.3f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.1f ? 660f : 990f)) * Mathf.Exp(-t * 9f) * 0.45f);
            // An overkill or a destroyed part: a dull, muffled thud.
            WriteWav("PH_Thud", 0.18f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * 70f) * Mathf.Exp(-t * 20f) * 0.8f + Noise(n) * Mathf.Exp(-t * 60f) * 0.2f);
            // The satchel refusing a part: a low buzz.
            WriteWav("PH_SatchelFull", 0.25f, (t, n) => Mathf.Sign(Mathf.Sin(t * 2f * Mathf.PI * 110f)) * (1f - t / 0.25f) * 0.25f);
            // The dodge roll: a short airy swish.
            WriteWav("PH_Dodge", 0.14f, (t, n) => Noise(n) * Mathf.Sin(t / 0.14f * Mathf.PI) * 0.35f);
            // The heavy reaching a stronger level: a short high tick.
            WriteWav("PH_ChargeTick", 0.06f, (t, n) => Mathf.Sin(t * 2f * Mathf.PI * 1500f) * Mathf.Exp(-t * 60f) * 0.45f);
            // The Harvest Finisher (4e): a heavy chop, then a bright ring (a clean harvest).
            WriteWav("PH_Finisher", 0.45f, (t, n) => (t < 0.08f ? Noise(n) * 0.8f + Mathf.Sin(t * 2f * Mathf.PI * 90f) * 0.6f
                : Mathf.Sin(t * 2f * Mathf.PI * 880f) * 0.4f + Mathf.Sin(t * 2f * Mathf.PI * 1320f) * 0.2f) * Mathf.Exp(-t * 6f));
            UnityEditor.AssetDatabase.ImportAsset($"{EditorPaths.Audio}/PH_Finisher.wav");
        }

        internal static float Noise(int n)
        {
            unchecked
            {
                uint x = (uint)n * 747796405u + 2891336453u;
                x = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
                return ((x >> 22) ^ x) / (float)uint.MaxValue * 2f - 1f;
            }
        }

        internal static void WriteWav(string name, float seconds, Func<float, int, float> wave)
        {
            string path = $"{EditorPaths.Audio}/{name}.wav";
            if (File.Exists(path)) return;
            const int rate = 22050;
            int samples = Mathf.CeilToInt(seconds * rate);
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + samples * 2);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(rate);
                writer.Write(rate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                    writer.Write((short)(Mathf.Clamp(wave(i / (float)rate, i), -1f, 1f) * short.MaxValue));
            }
            AssetDatabase.ImportAsset(path);
        }

        internal static AudioClip Sfx(string name) => Load<AudioClip>($"{EditorPaths.Audio}/{name}.wav");

        // ------------------------------------------------------------------ animation sets

        internal static SpriteAnim Mirrored(SpriteAnim anim)
        {
            anim.mirrorForLeft = true;
            anim.walkForBack = true;
            anim.frontLeft = anim.backLeft = System.Array.Empty<Sprite>();
            return anim;
        }

        /// <summary>
        /// A one-row sheet drawn facing front-right, mirrored for the left facings: the charged attack's stages, so the
        /// charge turns with the aim while it's held (Checkpoint A playtest; the back facings keep the front pose).
        /// </summary>
        internal static SpriteAnim MirrorLeft(SpriteAnim anim)
        {
            anim.mirrorForLeft = true;
            return anim;
        }

        internal static SpriteAnim Anim(CharacterAnim action, string pack, string file, int frames, int rows, float frameDuration, bool loop)
        {
            var anim = new SpriteAnim { action = action, frameDuration = frameDuration, loop = loop };
            // Minifantasy rows: front-right, front-left, back-right, back-left. One-row sheets serve every facing.
            anim.frontRight = MinifantasyImporter.Row(pack, file, 0, frames);
            if (rows >= 4)
            {
                anim.frontLeft = MinifantasyImporter.Row(pack, file, 1, frames);
                anim.backRight = MinifantasyImporter.Row(pack, file, 2, frames);
                anim.backLeft = MinifantasyImporter.Row(pack, file, 3, frames);
            }
            return anim;
        }

        /// <summary>One row of a sheet used for every facing (rows that aren't facings, like the charged attack's stages).</summary>
        internal static SpriteAnim AnimRow(CharacterAnim action, string pack, string file, int frames, int row, float frameDuration, bool loop) =>
            new() { action = action, frameDuration = frameDuration, loop = loop, frontRight = MinifantasyImporter.Row(pack, file, row, frames) };

        internal static SpriteAnimationSet Set(string name, params SpriteAnim[] animations) =>
            CreateOrUpdate<SpriteAnimationSet>($"{EditorPaths.Animations}/{name}.asset", set => set.animations = new List<SpriteAnim>(animations));

        /// <summary>Rebuilds only the animation sets (their assets are updated in place, so references hold).</summary>
        [MenuItem("Hearthdelve/Generate/Animation Sets", priority = 30)]
        public static void RebuildAnimationSets()
        {
            BuildAnimationSets(out _, out _, out _, out _, out _);
            AssetDatabase.SaveAssets();
        }

        public static void RebuildAnimationSetsBatch()
        {
            try
            {
                RebuildAnimationSets();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildAnimationSets(out SpriteAnimationSet human, out SpriteAnimationSet humanShadow,
            out SpriteAnimationSet slime, out SpriteAnimationSet slimeShadow, out SpriteAnimationSet cook)
        {
            const string c = MinifantasySheets.Creatures;
            // Frame durations come from each pack's _AnimationInfo.txt: 200 ms idle and walk, 100 ms the rest.
            human = Set("Anim_HumanTownsfolk",
                Anim(CharacterAnim.Idle, c, "HumanTownsfolkIdle", 16, 4, 0.2f, true),
                Anim(CharacterAnim.Walk, c, "HumanTownsfolkWalk", 4, 4, 0.2f, true),
                Anim(CharacterAnim.Attack, c, "HumanTownsfolkAttack", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Hurt, c, "HumanTownsfolkDmg", 4, 4, 0.1f, false),
                // One row in the pack (front-right): mirrored for dodges to the left, and the walk's back frames for
                // dodges away from the camera (4d playtest).
                Mirrored(Anim(CharacterAnim.Dodge, c, "HumanTownsfolkJump", 4, 1, 0.1f, false)),
                Anim(CharacterAnim.Die, c, "HumanTownsfolkSpinDie", 12, 1, 0.1f, false),
                // ChargedAttack's rows are stages, not facings: wind-up, charged loop, the spin. Drawn facing front-right
                // only: mirrored when the aim is to the left.
                MirrorLeft(AnimRow(CharacterAnim.Charge, c, "HumanTownsfolkChargedAttack", 6, 0, 0.1f, false)),
                MirrorLeft(AnimRow(CharacterAnim.ChargeHold, c, "HumanTownsfolkChargedAttack", 6, 1, 0.1f, true)),
                MirrorLeft(AnimRow(CharacterAnim.HeavyAttack, c, "HumanTownsfolkChargedAttack", 6, 2, 0.1f, false)));
            humanShadow = Set("Anim_HumanTownsfolk_Shadow",
                Anim(CharacterAnim.Idle, c, "ShadowHumanoidIdle", 16, 4, 0.2f, true),
                Anim(CharacterAnim.Walk, c, "ShadowHumanoidWalk", 4, 4, 0.2f, true),
                Anim(CharacterAnim.Attack, c, "ShadowHumanoidAttack", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Hurt, c, "ShadowHumanoidDmg", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Dodge, c, "ShadowHumanoidJump", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Die, c, "ShadowHumanoidSpinDie", 12, 1, 0.1f, false),
                MirrorLeft(AnimRow(CharacterAnim.Charge, c, "ShadowHumanoidChargedAttack", 6, 0, 0.1f, false)),
                MirrorLeft(AnimRow(CharacterAnim.ChargeHold, c, "ShadowHumanoidChargedAttack", 6, 1, 0.1f, true)),
                MirrorLeft(AnimRow(CharacterAnim.HeavyAttack, c, "ShadowHumanoidChargedAttack", 6, 2, 0.1f, false)));
            slime = Set("Anim_GreenSlime",
                Anim(CharacterAnim.Idle, c, "SlimeGreenIdle", 8, 4, 0.2f, true),
                Anim(CharacterAnim.Walk, c, "SlimeGreenJumpAttack", 4, 4, 0.2f, true),
                // The leap: the same jump, faster.
                Anim(CharacterAnim.Attack, c, "SlimeGreenJumpAttack", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Hurt, c, "SlimeGreenDmg", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Die, c, "SlimeGreenDie", 9, 1, 0.1f, false));
            slimeShadow = Set("Anim_GreenSlime_Shadow",
                Anim(CharacterAnim.Idle, c, "ShadowSlimeIdle", 8, 4, 0.2f, true),
                Anim(CharacterAnim.Walk, c, "ShadowSlimeJump", 4, 4, 0.2f, true),
                Anim(CharacterAnim.Attack, c, "ShadowSlimeJump", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Hurt, c, "ShadowSlimeDmg", 4, 4, 0.1f, false),
                Anim(CharacterAnim.Die, c, "ShadowSlimeDie", 9, 1, 0.1f, false));
            cook = Set("Anim_Cook",
                Anim(CharacterAnim.Idle, MinifantasySheets.MyriadOfNPCs, "CookerIdle", 16, 4, 0.2f, true));
        }

        // ------------------------------------------------------------------ tiles

        /// <summary>A Tile asset for one cell of the dungeon tileset (row 0 at the top).</summary>
        public static Tile DungeonTile(int column, int row, bool solid)
        {
            Sprite sprite = MinifantasyImporter.Cell(MinifantasySheets.Dungeon, "Tileset", column, row);
            return CreateOrUpdate<Tile>($"{EditorPaths.Tiles}/Dungeon_{column}_{row}.asset", tile =>
            {
                tile.sprite = sprite;
                tile.colliderType = solid ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            });
        }

        // ------------------------------------------------------------------ feedbacks

        /// <summary>One MMF_Player holding a moment's visuals, sound and haptics together (CLAUDE.md, Game feel).</summary>
        internal static MMF_Player Feedback(Transform parent, string name, SpriteRenderer flashTarget, float shake, AudioClip sound, HapticPattern haptic)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var player = go.AddComponent<MMF_Player>();
            player.FeedbacksList ??= new List<MMF_Feedback>();
            if (flashTarget != null) player.AddFeedback(new MMF_SpriteFlash { Label = "Flash", Target = flashTarget });
            if (shake > 0f) player.AddFeedback(new MMF_ScreenShake { Label = "Screen Shake", Force = shake });
            if (sound != null) player.AddFeedback(new MMF_Sound { Label = "Sound (placeholder)", Sfx = sound, PlayMethod = MMF_Sound.PlayMethods.Cached });
            if (haptic != null) player.AddFeedback(new MMF_HapticPattern { Label = $"Haptic {haptic.id}", Pattern = haptic });
            return player;
        }

        // ------------------------------------------------------------------ characters

        internal static GameObject CharacterRoot(string name, string layer, Vector2 colliderSize, Vector2 colliderOffset)
        {
            var root = new GameObject(name) { layer = LayerMask.NameToLayer(layer) };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.useAutoMass = false;
            body.mass = 1f;
            body.linearDamping = 1f;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            var group = root.AddComponent<SortingGroup>();
            group.sortingLayerName = SortingLayers.YSorted;

            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;
            collider.offset = colliderOffset;

            // Grounded everywhere: our floors have no Ground layer (FloorController2D).
            var controller = root.AddComponent<Hearthdelve.Shared.Engine.FloorController2D>();
            controller.GroundLayerMask = LayerMask.GetMask(Layers.Ground);
            controller.ObstaclesLayerMask = LayerMask.GetMask(Layers.Obstacles);
            return root;
        }

        internal static CharacterSpriteAnimator AddModel(GameObject root, SpriteAnimationSet set, SpriteAnimationSet shadowSet, out SpriteRenderer body)
        {
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            SpriteRenderer shadow = AddSprite(model.transform, "Shadow", shadowSet != null ? shadowSet.Find(CharacterAnim.Idle)?.frontRight[0] : null, SortingLayers.YSorted, 0, Vector3.zero);
            shadow.gameObject.SetActive(shadowSet != null);
            body = AddSprite(model.transform, "Body", set.Find(CharacterAnim.Idle).frontRight[0], SortingLayers.YSorted, 1, Vector3.zero);
            var animator = model.AddComponent<CharacterSpriteAnimator>();
            animator.Configure(set, body, shadowSet, shadowSet != null ? shadow : null);
            return animator;
        }

        public static GameObject BuildCleaver(WeaponDefinition definition)
        {
            var root = new GameObject("ButchersCleaver");
            int attacks = Mathf.Max(1, definition != null ? definition.combo.Count : 3);
            for (int i = 0; i < attacks; i++)
            {
                var attack = root.AddComponent<CombatMeleeWeapon>();
                attack.WeaponName = $"Cleaver {i + 1}";
                attack.TriggerMode = Weapon.TriggerModes.SemiAuto;
                attack.DamageAreaShape = MeleeWeapon.MeleeDamageAreaShapes.Rectangle;
                attack.TargetLayerMask = LayerMask.GetMask(Layers.Enemies);
                attack.InvincibilityDuration = 0.1f;
                attack.HitDamageableFeedback = HitFeedback(root.transform, $"Feedback_Hit_{i + 1}", Sfx("PH_Hit"), Pattern(HapticIds.TapFirm));
            }
            var aim = root.AddComponent<WeaponAim2D>();
            aim.AimControl = WeaponAim.AimControls.Mouse;
            // The system cursor stays visible (4e playtest): TDE hides it for a reticle, and we draw none.
            aim.ReplaceMousePointer = false;
            // Melee goes exactly where the player aims: TDE's default eases the weapon round at one turn per second.
            aim.WeaponRotationSpeed = 0f;
            root.AddComponent<ComboWeapon>();
            root.AddComponent<ComboWeaponTuning>().Configure(definition);
            GameObject prefab = SavePrefab(root, CleaverPrefab);
            return prefab;
        }

        /// <summary>
        /// A landed hit's one combined feedback, played by the weapon: hit-stop, screen shake, sound and haptic.
        /// Hit-stop duration and shake force are set per attack from AttackData. (The enemy only flashes.)
        /// </summary>
        internal static MMF_Player HitFeedback(Transform parent, string name, AudioClip sound, HapticPattern haptic)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var player = go.AddComponent<MMF_Player>();
            player.FeedbacksList ??= new List<MMF_Feedback>();
            player.AddFeedback(new MMF_HitStop { Label = "Hit Stop", FreezeFrameDuration = 0.06f });
            player.AddFeedback(new MMF_ScreenShake { Label = "Screen Shake", Force = 0.15f });
            if (sound != null) player.AddFeedback(new MMF_Sound { Label = "Sound (placeholder)", Sfx = sound, PlayMethod = MMF_Sound.PlayMethods.Cached });
            if (haptic != null) player.AddFeedback(new MMF_HapticPattern { Label = $"Haptic {haptic.id}", Pattern = haptic });
            return player;
        }

        public static GameObject BuildPlayer(bool dungeon, SpriteAnimationSet set, SpriteAnimationSet shadowSet, PlayerMoveConfig moveConfig,
            EssenceConfig essenceConfig, DelveConfig delveConfig, GameObject cleaver, GameObject heavy = null)
        {
            GameObject root = CharacterRoot(dungeon ? "Player" : "PlayerTavern", Layers.Player, new Vector2(0.7f, 0.45f), new Vector2(0f, 0.2f));
            root.tag = "Player";
            AddModel(root, set, shadowSet, out SpriteRenderer body);

            var character = root.AddComponent<Character>();
            character.CharacterType = Character.CharacterTypes.Player;
            character.CharacterDimension = Character.CharacterDimensions.Type2D;
            character.PlayerID = "Player1";
            character.CharacterModel = body.transform.parent.gameObject;

            root.AddComponent<CharacterOrientation2D>();
            root.AddComponent<CharacterMovement>();
            var dash = root.AddComponent<CharacterDash2D>();
            dash.DashMode = CharacterDash2D.DashModes.MainMovement;
            // Sound only: the roll is frequent, and a buzz on every one would numb the hits.
            dash.AbilityStartFeedbacks = Feedback(root.transform, "Feedback_Dodge", null, 0f, Sfx("PH_Dodge"), null);
            root.AddComponent<PlayerTuning>().Configure(moveConfig);
            // Drawn on the art-pixel grid, with the camera following the drawn position (CLAUDE.md, Camera and pixel-perfect).
            root.AddComponent<PixelSnappedPresentation>();

            if (dungeon)
            {
                var attachment = new GameObject("WeaponAttachment");
                attachment.transform.SetParent(root.transform, false);
                attachment.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                var handle = root.AddComponent<CharacterHandleWeapon>();
                handle.WeaponAttachment = attachment.transform;
                handle.InitialWeapon = cleaver.GetComponent<MeleeWeapon>();
                handle.CanPickupWeapons = false;
                if (heavy != null)
                {
                    // Added after the light handle, so GetComponent<CharacterHandleWeapon> still finds the light one.
                    var heavyAttachment = new GameObject("HeavyAttachment");
                    heavyAttachment.transform.SetParent(root.transform, false);
                    heavyAttachment.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                    var heavyHandle = root.AddComponent<CharacterHandleSecondaryWeapon>();
                    heavyHandle.WeaponAttachment = heavyAttachment.transform;
                    heavyHandle.InitialWeapon = heavy.GetComponent<ChargeWeapon>();
                    heavyHandle.CanPickupWeapons = false;
                    root.AddComponent<PlayerAttackGate>();
                }
                root.AddComponent<AimControlSwitcher>();
                // The character faces where it aims: the mouse, or the right stick (4e playtest).
                root.GetComponentInChildren<CharacterSpriteAnimator>().FaceAim = true;

                var health = root.AddComponent<EssenceHealth>();
                health.Configure(essenceConfig);
                health.DestroyOnDeath = false;
                health.DelayBeforeDestruction = 1.2f;
                health.DamageMMFeedbacks = Feedback(root.transform, "Feedback_HitTaken", body, 0.6f, Sfx("PH_Hurt"), Pattern(HapticIds.HitTaken));
                character.CharacterHealth = health;
                // Low Essence: a heartbeat, sound and haptic in one feedback, faster as Essence falls.
                root.AddComponent<LowEssenceWarning>().Configure(Feedback(root.transform, "Feedback_Heartbeat", null, 0f, Sfx("PH_Heartbeat"), Pattern(HapticIds.HeartbeatWarning)));
                var carrier = root.AddComponent<SatchelCarrier>();
                carrier.Configure(delveConfig);
                // The Harvest Finisher (4e step 3): a freeze, a shake, its sound and the Finisher.Harvest rumble together.
                MMF_Player finish = Feedback(root.transform, "Feedback_Finisher", null, 0.4f, Sfx("PH_Finisher"), Pattern(HapticIds.FinisherHarvest));
                finish.AddFeedback(new MMF_HitStop { Label = "Hit Stop", FreezeFrameDuration = 0.14f });
                // The dodge roll passes through enemies (4e playtest).
                root.AddComponent<Hearthdelve.Dungeon.Player.DodgeThroughEnemies>().Configure(LayerMask.GetMask(Layers.Enemies));
                root.AddComponent<PlayerFinisher>().Configure(
                    AssetDatabase.LoadAssetAtPath<HarvestRulesConfig>($"{EditorPaths.Config}/HarvestRulesConfig.asset"), finish);
                carrier.ConfigureFeedback(Feedback(root.transform, "Feedback_SatchelFull", null, 0f, Sfx("PH_SatchelFull"), Pattern(HapticIds.BuzzFailure)));
            }
            else
            {
                // The tavern has no Essence: the same character, simply unable to be hurt.
                var health = root.AddComponent<Health>();
                health.ImmuneToDamage = true;
                character.CharacterHealth = health;
                // Uses stations, the pass and seats (4c).
                root.AddComponent<Hearthdelve.Tavern.Scene.TavernInteractor>();
                // The keeper looks toward the mouse or along the right stick, as in the Hollows (4e playtest).
                root.AddComponent<PlayerLook>();
                root.GetComponentInChildren<CharacterSpriteAnimator>().FaceAim = true;
            }
            return SavePrefab(root, dungeon ? PlayerPrefab : TavernPlayerPrefab);
        }

        public static GameObject BuildPickup()
        {
            var root = new GameObject("IngredientPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            trigger.offset = new Vector2(0f, 0.3f);
            SpriteRenderer icon = AddSprite(root.transform, "Icon", null, SortingLayers.YSorted, 0, new Vector3(0f, 0.1f, 0f));
            // Unlit, like the enemy alert: a dark part must still read on a dim floor (CLAUDE.md, Lighting).
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (unlit != null) icon.sharedMaterial = unlit;
            MMF_Player picked = Feedback(root.transform, "Feedback_PickedUp", null, 0f, Sfx("PH_Pickup"), Pattern(HapticIds.TapLight));
            root.AddComponent<IngredientPickup>().Configure(icon, picked);
            return SavePrefab(root, PickupPrefab);
        }

        public static GameObject BuildCook(SpriteAnimationSet set)
        {
            var root = new GameObject("Cook");
            root.AddComponent<SortingGroup>().sortingLayerName = SortingLayers.YSorted;
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            SpriteRenderer body = AddSprite(model.transform, "Body", set.Find(CharacterAnim.Idle).frontRight[0], SortingLayers.YSorted, 1, Vector3.zero);
            model.AddComponent<CharacterSpriteAnimator>().Configure(set, body, null, null);
            return SavePrefab(root, CookPrefab);
        }

        /// <summary>Gives the slime's parts their Minifantasy loot icons (they had none).</summary>
        public static void AssignIngredientIcons()
        {
            SetIcon("Ingredient_SlimeGel", 5, 11);
            SetIcon("Ingredient_SlimeCore", 7, 11);
        }

        static void SetIcon(string asset, int column, int row)
        {
            var ingredient = AssetDatabase.LoadAssetAtPath<IngredientDefinition>($"{EditorPaths.Ingredients}/{asset}.asset");
            Sprite icon = MinifantasyImporter.Cell(MinifantasySheets.LootIcons, "LootIcons", column, row);
            if (ingredient == null || icon == null || ingredient.icon == icon) return;
            ingredient.icon = icon;
            EditorUtility.SetDirty(ingredient);
        }
    }
}
