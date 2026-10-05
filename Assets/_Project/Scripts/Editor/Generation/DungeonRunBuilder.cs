using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.UI.Debugging;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using MoreMountains.Feedbacks;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The 4d delve scene (<c>Dungeon</c>): one scene into which rooms are loaded one at a time (GDD §10.2). It holds
    /// what lasts across rooms (the player, managers, harvest, the delve controller, the navigation grid, the camera
    /// kept inside the room, the HUD, the fade between rooms and a seed line); <see cref="RoomRunner"/> generates the run
    /// from <see cref="RunSettings"/> and loads its rooms. <c>Dungeon_TestFloor</c> stays the standalone combat test bed.
    /// </summary>
    public static class DungeonRunBuilder
    {
        [MenuItem("Hearthdelve/Generate/4d Dungeon (Rooms)", priority = 4)]
        public static void GenerateMenu() => Generate(rebuildSceneApproved: false);

        /// <summary>Batch entry point: <c>-executeMethod Hearthdelve.Editor.DungeonRunBuilder.RunBatch [-rebuildScene]</c>.</summary>
        public static void RunBatch()
        {
            try
            {
                Generate(Environment.GetCommandLineArgs().Contains("-rebuildScene"));
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public const string SettingsPath = EditorPaths.Data + "/Dungeon/RunSettings.asset";
        public const string GoldPickupPrefab = EditorPaths.Prefabs + "/Dungeon/GoldPickup.prefab";
        public const string PowerPickupPrefab = EditorPaths.Prefabs + "/Dungeon/PowerPickup.prefab";
        const string k_PowersFolder = EditorPaths.Data + "/Dungeon/Powers";

        /// <summary>
        /// Rebuilds the rooms and the run settings (keeping their tuning). The scene is created if it doesn't exist, rebuilt
        /// only with approval, and otherwise brought up to date in place.
        /// </summary>
        public static void Generate(bool rebuildSceneApproved)
        {
            LookTestBuilder.Content content = LookTestBuilder.BuildContent();
            // Rebuilding the shared prefabs recreates the tavern player without what the tavern adds to it.
            TavernBuilder.AddCarryViewToPlayer();
            BuildSounds();
            Hearthdelve.Dungeon.Bosses.BossDefinition boss = BossContent.BuildLarderTroll();
            Dictionary<string, RoomDefinition> rooms = RoomContent.Build(content);
            RunSettings settings = BuildSettings(content, rooms);
            // The arena's encounter (4e): the Larder Troll replaces 4d's stand-in fight. Set once; a later edit is kept.
            if (settings.tuning.boss == null)
            {
                settings.tuning.boss = boss;
                EditorUtility.SetDirty(settings);
            }
            bool exists = System.IO.File.Exists(EditorPaths.DungeonScene);
            if (!exists || rebuildSceneApproved && LookTestBuilder.MayWrite(EditorPaths.DungeonScene, true)) BuildScene(content, settings);
            else UpdateRun(settings);
            AddToBuild(EditorPaths.DungeonScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearthdelve] 4d dungeon generated.");
        }

        /// <summary>The run's rooms, enemies and Gold pickup are refreshed every time; its tuning is kept (it's for editing).</summary>
        static RunSettings BuildSettings(LookTestBuilder.Content content, Dictionary<string, RoomDefinition> rooms)
        {
            RunSettings settings = LookTestContent.LoadOrCreate<RunSettings>(SettingsPath);
            settings.rooms = RoomContent.Ids.Select(id => rooms[id]).ToArray();
            settings.slime = content.Slime;
            settings.bat = content.Bat;
            settings.spider = content.Spider;
            settings.goldPickup = BuildGoldPickup();
            settings.powerPickup = BuildPowerPickup();
            // Step 4's powers: the assets are refreshed (art) but their amounts are kept; the list is filled once.
            Hearthdelve.Shared.Run.RunPowerDefinition[] powers = BuildPowers();
            if (settings.tuning.powers == null || settings.tuning.powers.Length == 0) settings.tuning.powers = powers;
            // Step 3's rewards, once: settings made before step 3 have no ingredient list, and their floors took the reward
            // fields' plain defaults rather than the per-floor ones. Later edits are kept.
            if (settings.tuning.ingredientRewards == null || settings.tuning.ingredientRewards.Length == 0)
            {
                settings.tuning.ingredientRewards = DefaultIngredientRewards();
                FloorTuning[] defaults = RunTuning.Defaults();
                for (int f = 0; f < settings.tuning.floors.Length && f < defaults.Length; f++)
                {
                    FloorTuning floor = settings.tuning.floors[f], d = defaults[f];
                    floor.goldWeight = d.goldWeight;
                    floor.ingredientWeight = d.ingredientWeight;
                    floor.minGold = d.minGold;
                    floor.maxGold = d.maxGold;
                    floor.minParts = d.minParts;
                    floor.maxParts = d.maxParts;
                    floor.fineChance = d.fineChance;
                }
            }
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>
        /// The dungeon's own ingredients as room rewards: the Cellars' monster parts, the slime core and the venom sac only
        /// from the second floor down. (Shroom cap and spore sac wait for the Mushroom People and their icons.)
        /// </summary>
        static IngredientRewardOption[] DefaultIngredientRewards()
        {
            IngredientRewardOption Option(string asset, float weight, int fromFloor) => new()
            {
                ingredient = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_{asset}.asset"),
                weight = weight,
                fromFloor = fromFloor,
            };
            return new[]
            {
                Option("SlimeGel", 1f, 1), Option("BatWing", 1f, 1), Option("SpiderLeg", 1f, 1),
                Option("SlimeCore", 0.7f, 2), Option("VenomSac", 0.7f, 2),
            }.Where(o => o.ingredient != null).ToArray();
        }

        /// <summary>
        /// The run powers (4d step 4), one asset each: a small nudge to existing tuning, with a True Heroes skill icon.
        /// New assets get these amounts; existing ones keep theirs (they're for tuning).
        /// </summary>
        static Hearthdelve.Shared.Run.RunPowerDefinition[] BuildPowers()
        {
            EditorPaths.Ensure(k_PowersFolder);
            var list = new List<Hearthdelve.Shared.Run.RunPowerDefinition>();
            void Power(string id, string asset, Hearthdelve.Shared.Run.RunPowerEffect effect, float amount, string icon)
            {
                var power = LookTestContent.LoadOrCreate<Hearthdelve.Shared.Run.RunPowerDefinition>($"{k_PowersFolder}/Power_{asset}.asset");
                if (string.IsNullOrEmpty(power.id))
                {
                    power.id = id;
                    power.effect = effect;
                    power.amount = amount;
                }
                power.icon = MinifantasyImporter.Sprite(MinifantasySheets.SkillIcons, "SkillIcons", icon);
                EditorUtility.SetDirty(power);
                list.Add(power);
            }
            Power("deep_reserves", "DeepReserves", Hearthdelve.Shared.Run.RunPowerEffect.MaxEssence, 25f, "BardBallad");
            Power("slow_burn", "SlowBurn", Hearthdelve.Shared.Run.RunPowerEffect.SlowerDrain, 0.3f, "ClericDivineFire");
            Power("thick_hide", "ThickHide", Hearthdelve.Shared.Run.RunPowerEffect.LighterHits, 0.3f, "BardDefense");
            Power("keen_edge", "KeenEdge", Hearthdelve.Shared.Run.RunPowerEffect.LightDamage, 0.25f, "BardMelee");
            Power("heavy_hand", "HeavyHand", Hearthdelve.Shared.Run.RunPowerEffect.HeavyDamage, 0.4f, "PaladinHolyHammer");
            Power("light_feet", "LightFeet", Hearthdelve.Shared.Run.RunPowerEffect.FasterDodge, 0.4f, "RogueDodge");
            Power("second_wind", "SecondWind", Hearthdelve.Shared.Run.RunPowerEffect.EssenceOnClear, 8f, "ClericHealingWords");
            Power("butchers_eye", "ButchersEye", Hearthdelve.Shared.Run.RunPowerEffect.GentleKills, 0.5f, "RogueAttack");
            AssetDatabase.SaveAssets();
            return list.ToArray();
        }

        /// <summary>A power room's reward: a bobbing spark with a small warm glow (unlit, so it reads anywhere), offering three powers.</summary>
        static Hearthdelve.Dungeon.Powers.PowerPickup BuildPowerPickup()
        {
            var root = new GameObject("PowerPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
            trigger.offset = new Vector2(0f, 0.3f);
            SpriteRenderer spark = LookTestContent.AddSprite(root.transform, "Spark", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Lightning"),
                SortingLayers.YSorted, 0, new Vector3(0f, 0.3f, 0f));
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (unlit != null) spark.sharedMaterial = unlit;
            Light2D glow = LookTestBuilder.Light("Glow", new Vector3(0f, 0.4f, 0f), Light2D.LightType.Point);
            glow.transform.SetParent(root.transform, false);
            glow.color = new Color(1f, 0.82f, 0.4f);
            glow.intensity = 0.9f;
            glow.pointLightInnerRadius = 0.3f;
            glow.pointLightOuterRadius = 2.5f;
            MMF_Player feedback = LookTestContent.Feedback(root.transform, "Feedback_Power", null, 0f, LookTestContent.Sfx("PH_PowerUp"), LookTestContent.Pattern(HapticIds.PulseSuccess));
            root.AddComponent<Hearthdelve.Dungeon.Powers.PowerPickup>().Configure(spark.transform, feedback);
            return LookTestContent.SavePrefab(root, PowerPickupPrefab).GetComponent<Hearthdelve.Dungeon.Powers.PowerPickup>();
        }

        /// <summary>A room's Gold reward: a bobbing coin (unlit, so it reads on a dim floor, like the parts) that adds to the run's Gold.</summary>
        static GoldPickup BuildGoldPickup()
        {
            var root = new GameObject("GoldPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;
            trigger.offset = new Vector2(0f, 0.25f);
            SpriteRenderer coin = LookTestContent.AddSprite(root.transform, "Coin", MinifantasyImporter.Sprite(MinifantasySheets.MiscellanyIcons, "Miscellany", "GoldCoin"),
                SortingLayers.YSorted, 0, new Vector3(0f, 0.25f, 0f));
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (unlit != null) coin.sharedMaterial = unlit;
            MMF_Player feedback = LookTestContent.Feedback(root.transform, "Feedback_Gold", null, 0f, LookTestContent.Sfx("PH_Coin"), LookTestContent.Pattern(HapticIds.TapLight));
            root.AddComponent<GoldPickup>().Configure(coin.transform, feedback);
            return LookTestContent.SavePrefab(root, GoldPickupPrefab).GetComponent<GoldPickup>();
        }

        /// <summary>
        /// Brings the existing scene up to the generated run in place (4d step 2): the room runner gets the run settings,
        /// the exit signs and the fall's feedback, and the canvas gets the seed line.
        /// </summary>
        [MenuItem("Hearthdelve/Generate/4d Update Dungeon Run", priority = 25)]
        public static void UpdateRunMenu() => UpdateRun(AssetDatabase.LoadAssetAtPath<RunSettings>(SettingsPath));

        public static void UpdateRunBatch()
        {
            try
            {
                UpdateRunMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void UpdateRun(RunSettings settings)
        {
            Scene scene = EditorSceneManager.OpenScene(EditorPaths.DungeonScene, OpenSceneMode.Single);
            // The HUD and the result screen (step 3: run Gold) are rebuilt with the run.
            DungeonUI.RebuildScreens(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "UI"));
            var runner = UnityEngine.Object.FindAnyObjectByType<RoomRunner>(FindObjectsInactive.Include);
            ConfigureRunner(runner, settings);
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "UI");
            BuildDebugLabel(canvas);
            BuildRoomFade(canvas);
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Dungeon run updated.");
        }

        /// <summary>Points the runner at the scene's pieces and the run settings; its feedbacks are rebuilt.</summary>
        static void ConfigureRunner(RoomRunner runner, RunSettings settings)
        {
            Transform rooms = runner.transform;
            foreach (string name in new[] { "Feedback_Seal", "Feedback_Clear", "Feedback_Fall" })
            {
                Transform old = rooms.Find(name);
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            MMF_Player seal = LookTestContent.Feedback(rooms, "Feedback_Seal", null, 0.1f, LookTestContent.Sfx("PH_GateSlam"), LookTestContent.Pattern(HapticIds.BumpSoft));
            MMF_Player clear = LookTestContent.Feedback(rooms, "Feedback_Clear", null, 0f, LookTestContent.Sfx("PH_GateRise"), LookTestContent.Pattern(HapticIds.PulseSuccess));
            MMF_Player fall = LookTestContent.Feedback(rooms, "Feedback_Fall", null, 0f, LookTestContent.Sfx("PH_Whoosh"), LookTestContent.Pattern(HapticIds.TapFirm));
            GameObject follow = GameObject.Find("Follow Camera");
            Transform roomRoot = rooms.Find("Room");
            if (roomRoot == null)
            {
                roomRoot = new GameObject("Room").transform;
                roomRoot.SetParent(rooms, false);
            }
            runner.Configure(settings, roomRoot, UnityEngine.Object.FindAnyObjectByType<NavGrid>(), follow.GetComponent<CinemachineCamera>(),
                follow.GetComponent<RoomCameraBounds>(), seal, clear, fall,
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "ArrowUp"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "ArrowDown"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Swords"),
                MinifantasyImporter.Sprite(MinifantasySheets.MiscellanyIcons, "Miscellany", "GoldCoin"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Food"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Lightning"));
            EditorUtility.SetDirty(runner);
        }

        /// <summary>The seed line (debug): bottom right, where the HUD has nothing, quiet, under the room fade.</summary>
        static void BuildDebugLabel(Canvas canvas)
        {
            Transform old = canvas.transform.Find("DelveDebug");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var corner = new Vector2(1f, 0f);
            LocalizedSuperText text = LookTestBuilder.Text(canvas.transform, "DelveDebug", LocKeys.DelveDebug, 6f, new Color(0.6f, 0.62f, 0.7f), TextAnchor.LowerRight,
                corner, corner, corner, new Vector2(-4f, 3f), new Vector2(200f, GameFonts.LinePixels));
            text.gameObject.AddComponent<DelveDebugLabel>();
        }

        /// <summary>The scene's UI, in place: the HUD and screens are rebuilt, the room fade added, and text gets the game font.</summary>
        [MenuItem("Hearthdelve/Generate/4d Update Dungeon UI", priority = 24)]
        public static void UpdateDungeonUI()
        {
            MinifantasyImporter.ImportAll();
            LocalizationBuilder.Build();
            Scene scene = EditorSceneManager.OpenScene(EditorPaths.DungeonScene, OpenSceneMode.Single);
            Canvas canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == "UI");
            DungeonUI.RebuildScreens(canvas);
            BuildRoomFade(canvas);
            GameFonts.ApplyToOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Hearthdelve] Dungeon UI updated.");
        }

        public static void UpdateDungeonUIBatch()
        {
            try
            {
                UpdateDungeonUI();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>The gates: an iron slam as they drop, a rattle as they rise.</summary>
        static void BuildSounds()
        {
            static float Sin(float t, float hz) => Mathf.Sin(t * 2f * Mathf.PI * hz);
            LookTestContent.WriteWav("PH_GateSlam", 0.35f, (t, n) =>
                (LookTestContent.Noise(n) * 0.5f + Sin(t, 70f - 40f * t) * 0.9f) * Mathf.Exp(-t * 14f) * 0.8f);
            LookTestContent.WriteWav("PH_GateRise", 0.5f, (t, n) =>
                (LookTestContent.Noise(n) * 0.35f + Sin(t, 900f) * 0.15f) * (0.6f + 0.4f * Sin(t, 22f)) * Mathf.Sin(t / 0.5f * Mathf.PI) * 0.6f);
            // A power taken: a rising shimmer.
            LookTestContent.WriteWav("PH_PowerUp", 0.6f, (t, n) =>
                (Sin(t, 520f + 700f * t) * 0.5f + Sin(t, 1040f + 1400f * t) * 0.25f) * Mathf.Sin(t / 0.6f * Mathf.PI) * 0.35f);
            // The Larder Troll (4e): a low roar, the slam's thud, and a heavier thud for running into a wall.
            LookTestContent.WriteWav("PH_TrollRoar", 0.9f, (t, n) =>
                (LookTestContent.Noise(n) * 0.35f + Sin(t, 70f + 25f * Mathf.Sin(t * 9f)) * 0.8f) * Mathf.Sin(t / 0.9f * Mathf.PI) * 0.7f);
            LookTestContent.WriteWav("PH_TrollSlam", 0.4f, (t, n) =>
                (LookTestContent.Noise(n) * 0.6f + Sin(t, 55f - 25f * t) * 0.9f) * Mathf.Exp(-t * 9f) * 0.9f);
            LookTestContent.WriteWav("PH_TrollThud", 0.55f, (t, n) =>
                (LookTestContent.Noise(n) * 0.7f + Sin(t, 45f - 20f * t) * 1f) * Mathf.Exp(-t * 6f) * 0.95f);
            foreach (string name in new[] { "PH_GateSlam", "PH_GateRise", "PH_Whoosh", "PH_Coin", "PH_PowerUp", "PH_TrollRoar", "PH_TrollSlam", "PH_TrollThud" })
                AssetDatabase.ImportAsset($"{EditorPaths.Audio}/{name}.wav");
        }

        static void BuildScene(LookTestBuilder.Content content, RunSettings settings)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // The player spawns here and is moved to the first room's arrival point.
            GameObject managers = LookTestBuilder.Managers(HearthdelveInputManager.GameplayMap.Dungeon, content.Library, content.Player, new Vector2(20f, 3f));
            HarvestSystem harvest = managers.AddComponent<HarvestSystem>();
            harvest.Configure(content.HarvestRules, content.Cleaver, content.Pickup.GetComponent<IngredientPickup>(), content.Freshness);
            harvest.Scatter = 1.1f;
            TestFloorBuilder.HarvestFeedbacks(harvest);
            var nav = managers.AddComponent<NavGrid>();
            nav.Configure(new RectInt(0, 0, RoomLayout.MinWidth, RoomLayout.MinHeight), LayerMask.GetMask(Layers.Obstacles));
            managers.AddComponent<DelveRunController>();

            LookTestBuilder.Cameras(new Color(0.05f, 0.05f, 0.07f));
            var follow = GameObject.Find("Follow Camera");
            // Kept inside the room, with no easing at its edge, like the follow itself (CLAUDE.md, Camera and scrolling).
            follow.AddComponent<RoomCameraBounds>();
            LookTestBuilder.Light("Global Light 2D", Vector3.zero, Light2D.LightType.Global);

            var rooms = new GameObject("Rooms");
            new GameObject("Room").transform.SetParent(rooms.transform, false);
            ConfigureRunner(rooms.AddComponent<RoomRunner>(), settings);

            Canvas canvas = LookTestBuilder.Canvas(content.Actions, out _);
            DungeonUI.RebuildScreens(canvas);
            BuildDebugLabel(canvas);
            BuildRoomFade(canvas);

            LookTestBuilder.ApplyLighting(dungeon: true);
            EditorPaths.Ensure(EditorPaths.Scenes);
            EditorSceneManager.SaveScene(scene, EditorPaths.DungeonScene);
        }

        /// <summary>The black cover between rooms, over everything else on the canvas.</summary>
        static void BuildRoomFade(Canvas canvas)
        {
            Transform old = canvas.transform.Find("RoomFade");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            RectTransform rect = DungeonUI.FullScreen(canvas, "RoomFade");
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            rect.gameObject.AddComponent<CanvasGroup>();
            rect.gameObject.AddComponent<RoomFadeView>();
            rect.SetAsLastSibling();
        }

        /// <summary>Adds the scene to the build list if it isn't there, keeping the existing order (Boot stays first).</summary>
        static void AddToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
