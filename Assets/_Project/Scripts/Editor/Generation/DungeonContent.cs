using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Haptics;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4b dungeon content: the Biome 1 enemies (green slime, bat, giant spider) with their telegraphed
    /// attacks and TDE brains, the cleaver's heavy (charged) attack, and the spider's web. Data assets
    /// get new fields filled in once (<see cref="EnemyDefinition.schema"/>), so tuning is kept;
    /// animation sets and prefabs are rebuilt every run.
    /// </summary>
    public static class DungeonContent
    {
        public const string HeavyPrefab = EditorPaths.Prefabs + "/Weapons/ButchersCleaverHeavy.prefab";
        public const string BatPrefab = EditorPaths.Prefabs + "/Enemies/Bat.prefab";
        public const string SpiderPrefab = EditorPaths.Prefabs + "/Enemies/GiantSpider.prefab";
        public const string WebPrefab = EditorPaths.Prefabs + "/Enemies/GiantSpiderWeb.prefab";
        public const string RopeExitPrefab = EditorPaths.Prefabs + "/Dungeon/RopeExit.prefab";
        public const string SlimeDefinitionPath = EditorPaths.Enemies + "/Enemy_GreenSlime.asset";
        public const string BatDefinitionPath = EditorPaths.Enemies + "/Enemy_Bat.asset";
        public const string SpiderDefinitionPath = EditorPaths.Enemies + "/Enemy_GiantSpider.asset";
        const string k_UnlitSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        const int k_Schema = 1;

        public sealed class Enemies
        {
            public GameObject Slime, Bat, Spider;
        }

        // ------------------------------------------------------------------ data

        /// <summary>Fills in the 4b data: the cleaver's heavy steps and the three enemies' attacks. Existing tuning is kept.</summary>
        public static void BuildData(WeaponDefinition cleaver)
        {
            if (cleaver != null && cleaver.heavy.Count == 0)
            {
                cleaver.heavy = new List<HeavyChargeStep>
                {
                    Heavy(0f, "Heavy tap", 14f, 2.2f, 7f, 0.35f, 0.07f),
                    Heavy(0.45f, "Heavy charged", 26f, 2.6f, 9f, 0.5f, 0.09f),
                    Heavy(0.9f, "Heavy full", 40f, 3.0f, 11f, 0.7f, 0.12f),
                };
                EditorUtility.SetDirty(cleaver);
            }
            Migrate(Load(SlimeDefinitionPath, null), Slime);
            Migrate(Load(BatDefinitionPath, "Enemy_GiantRat"), Bat);
            Migrate(Load(SpiderDefinitionPath, "Enemy_CellarShroom"), Spider);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Stand-off distances for the slime and the bat (schema 3, after the step 4 playtest): closing
        /// right in, they pushed the player around, and the bat was always too close to swoop.
        /// </summary>
        public static void BuildStandOff()
        {
            foreach (var (path, standOff) in new[] { (SlimeDefinitionPath, new Vector2(0f, 1.2f)), (BatDefinitionPath, new Vector2(1.4f, 2.6f)) })
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (definition == null || definition.schema >= 3) continue;
                if (definition.keepDistance == Vector2.zero) definition.keepDistance = standOff;
                definition.schema = 3;
                EditorUtility.SetDirty(definition);
            }
            AssetDatabase.SaveAssets();
        }

        static HeavyChargeStep Heavy(float chargeTime, string name, float damage, float diameter, float knockback, float stagger, float hitStop) => new()
        {
            chargeTime = chargeTime,
            attack = new AttackData
            {
                debugName = name, damage = damage, startupFrames = 6, activeFrames = 12, recoveryFrames = 18, cancelFrame = 30,
                hitboxOffset = Vector2.zero, hitboxSize = new Vector2(diameter, diameter), knockbackForce = knockback,
                staggerTime = stagger, hitStop = hitStop, screenShake = 0.25f,
            },
        };

        /// <summary>
        /// Loads an enemy definition, or renames the prototype's asset it replaces (keeping its GUID and
        /// every reference to it), or creates it.
        /// </summary>
        static EnemyDefinition Load(string path, string replaces)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (existing != null) return existing;
            string old = replaces != null ? $"{EditorPaths.Enemies}/{replaces}.asset" : null;
            if (old != null && File.Exists(old))
            {
                string error = AssetDatabase.RenameAsset(old, Path.GetFileNameWithoutExtension(path));
                if (!string.IsNullOrEmpty(error)) Debug.LogError($"[Hearthdelve] Could not rename {old}: {error}");
                AssetDatabase.SaveAssets();
                existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                if (existing != null) return existing;
            }
            return LookTestContent.LoadOrCreate<EnemyDefinition>(path);
        }

        static void Migrate(EnemyDefinition definition, System.Action<EnemyDefinition> apply)
        {
            if (definition == null || definition.schema >= k_Schema) return;
            apply(definition);
            definition.schema = k_Schema;
            EditorUtility.SetDirty(definition);
        }

        static void Slime(EnemyDefinition d)
        {
            d.aggroRange = 8f;
            d.knockbackMultiplier = 1f;
            d.staggerMultiplier = 1f;
            // The prototype's decision: the slime's leap can't be interrupted.
            d.superArmorWhileAttacking = true;
            d.keepDistance = Vector2.zero;
            d.flutter = 0f;
            d.startsAsleep = false;
            d.attack = new EnemyAttackSettings
            {
                debugName = "Leap", kind = EnemyAttackKind.Leap, minRange = 0f, maxRange = 3.2f,
                telegraph = 0.6f, active = 0.35f, recovery = 0.5f, cooldown = 1.5f,
                damage = 10f, hitboxOffset = new Vector2(0f, 0.2f), hitboxSize = new Vector2(0.8f, 0.6f),
                animation = CharacterAnim.Attack, releaseFrame = 1, travelDistance = 3.5f, arcHeight = 0.8f,
            };
            d.otherAttacks = new List<EnemyAttackSettings>();
        }

        static void Bat(EnemyDefinition d)
        {
            d.id = "bat";
            d.maxHealth = 14f;
            d.moveSpeed = 2.6f;
            d.aggroRange = 9f;
            d.knockbackMultiplier = 1.3f;
            d.staggerMultiplier = 1.2f;
            d.superArmorWhileAttacking = false;
            d.keepDistance = Vector2.zero;
            d.flutter = 0.55f;
            d.startsAsleep = true;
            d.wakeRange = 4f;
            d.attack = new EnemyAttackSettings
            {
                debugName = "Swoop", kind = EnemyAttackKind.Swoop, minRange = 1f, maxRange = 3.5f,
                telegraph = 0.5f, active = 0.3f, recovery = 0.45f, cooldown = 1.6f,
                damage = 8f, hitboxOffset = new Vector2(0f, 0.3f), hitboxSize = new Vector2(0.7f, 0.6f),
                animation = CharacterAnim.Attack, releaseFrame = 2, travelDistance = 4f,
            };
            d.otherAttacks = new List<EnemyAttackSettings>();
            d.placeholderColor = new Color(0.45f, 0.3f, 0.45f);
        }

        static void Spider(EnemyDefinition d)
        {
            d.id = "giant_spider";
            d.maxHealth = 36f;
            d.moveSpeed = 1.7f;
            d.aggroRange = 9f;
            d.knockbackMultiplier = 0.6f;
            d.staggerMultiplier = 0.6f;
            d.superArmorWhileAttacking = false;
            // Keeps its distance and spits; bites when the player closes in.
            d.keepDistance = new Vector2(3f, 5.5f);
            d.flutter = 0f;
            d.startsAsleep = false;
            d.attack = new EnemyAttackSettings
            {
                debugName = "Bite", kind = EnemyAttackKind.Bite, minRange = 0f, maxRange = 1.8f,
                telegraph = 0.5f, active = 0.15f, recovery = 0.4f, cooldown = 1.1f,
                damage = 14f, hitboxOffset = new Vector2(1f, 0.3f), hitboxSize = new Vector2(1.1f, 0.9f),
                animation = CharacterAnim.Attack, releaseFrame = 4,
            };
            d.otherAttacks = new List<EnemyAttackSettings>
            {
                new()
                {
                    debugName = "Web", kind = EnemyAttackKind.Spit, minRange = 2.5f, maxRange = 7f,
                    telegraph = 0.8f, active = 0.1f, recovery = 0.5f, cooldown = 2.4f,
                    damage = 9f, animation = CharacterAnim.Shoot, releaseFrame = 9,
                    projectileSpeed = 6.5f, projectileRange = 9f, projectileSpawnOffset = new Vector2(0.6f, 0.3f),
                },
            };
            d.placeholderColor = new Color(0.2f, 0.2f, 0.3f);
        }

        // ------------------------------------------------------------------ animation

        public static void BuildAnimationSets(out SpriteAnimationSet bat, out SpriteAnimationSet batShadow,
            out SpriteAnimationSet spider, out SpriteAnimationSet spiderShadow)
        {
            const string c = MinifantasySheets.Creatures;
            const string s = MinifantasySheets.GiantSpider;
            // The bat pack's _AnimationInfo.txt: 200 ms idle and fly, 100 ms the rest. BatSleep's rows: asleep, waking, falling asleep.
            bat = LookTestContent.Set("Anim_Bat",
                LookTestContent.Anim(CharacterAnim.Idle, c, "BatFlyIdle", 2, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Walk, c, "BatFlyIdle", 2, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Attack, c, "BatAttack", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, c, "BatDmg", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Die, c, "BatDie", 9, 1, 0.1f, false),
                LookTestContent.AnimRow(CharacterAnim.Sleep, c, "BatSleep", 8, 0, 0.2f, true),
                LookTestContent.AnimRow(CharacterAnim.Wake, c, "BatSleep", 5, 1, 0.1f, false));
            batShadow = LookTestContent.Set("Anim_Bat_Shadow",
                LookTestContent.Anim(CharacterAnim.Idle, c, "ShadowBatFly", 2, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Walk, c, "ShadowBatFly", 2, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Attack, c, "ShadowBatAttack", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, c, "ShadowBatDmg", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Die, c, "ShadowBatDie", 9, 1, 0.1f, false),
                LookTestContent.AnimRow(CharacterAnim.Sleep, c, "ShadowBatSleep", 8, 0, 0.2f, true),
                LookTestContent.AnimRow(CharacterAnim.Wake, c, "ShadowBatSleep", 5, 1, 0.1f, false));
            // The spider pack has no timing notes; 100 ms throughout reads right.
            spider = LookTestContent.Set("Anim_GiantSpider",
                LookTestContent.Anim(CharacterAnim.Idle, s, "GiantSpiderIdle", 17, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Walk, s, "GiantSpiderWalk", 6, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Attack, s, "GiantSpiderAttack", 7, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Shoot, s, "GiantSpiderShotWebDiagonal", 14, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, s, "GiantSpiderDmg", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Die, s, "GiantSpiderDie", 33, 1, 0.1f, false));
            spiderShadow = LookTestContent.Set("Anim_GiantSpider_Shadow",
                LookTestContent.Anim(CharacterAnim.Idle, s, "GiantSpiderIdleShadow", 17, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Walk, s, "GiantSpiderWalkShadow", 6, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Attack, s, "GiantSpiderAttackShadow", 7, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Shoot, s, "GiantSpiderWebShotShadow", 14, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, s, "GiantSpiderDmgShadow", 4, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Die, s, "GiantSpiderDieShadow", 33, 1, 0.1f, false));
        }

        // ------------------------------------------------------------------ weapons

        /// <summary>The cleaver's heavy: a TDE charge weapon with one spin attack per charge step.</summary>
        public static GameObject BuildHeavy(WeaponDefinition definition)
        {
            var root = new GameObject("ButchersCleaverHeavy");
            // First, so TDE (which takes a weapon prefab's first Weapon) equips the charge weapon.
            var charge = root.AddComponent<ChargeWeapon>();
            charge.WeaponName = "Cleaver Heavy";
            int steps = Mathf.Max(1, definition != null ? definition.heavy.Count : 3);
            for (int i = 0; i < steps; i++)
            {
                var step = root.AddComponent<CombatMeleeWeapon>();
                step.WeaponName = $"Cleaver Heavy {i + 1}";
                step.TriggerMode = Weapon.TriggerModes.SemiAuto;
                step.DamageAreaShape = MeleeWeapon.MeleeDamageAreaShapes.Circle;
                step.TargetLayerMask = LayerMask.GetMask(Layers.Enemies);
                step.InvincibilityDuration = 0.1f;
                step.HitDamageableFeedback = LookTestContent.HitFeedback(root.transform, $"Feedback_Hit_Heavy_{i + 1}", LookTestContent.Sfx("PH_HitHeavy"), LookTestContent.Pattern(HapticIds.HitHeavy));
            }
            // A tick each time the charge reaches a stronger level, felt more strongly at the top level.
            var levels = new MMF_Player[Mathf.Max(0, steps - 1)];
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i] = LookTestContent.Feedback(root.transform, $"Feedback_ChargeLevel_{i + 2}", null, 0f, LookTestContent.Sfx("PH_ChargeTick"), LookTestContent.Pattern(HapticIds.CueThreshold));
                levels[i].GetFeedbackOfType<MMF_HapticPattern>().Scale = i == levels.Length - 1 ? 1f : 0.6f;
            }
            var tuning = root.AddComponent<HeavyWeaponTuning>();
            tuning.Configure(definition);
            tuning.ConfigureLevelFeedbacks(levels);
            return LookTestContent.SavePrefab(root, HeavyPrefab);
        }

        /// <summary>The spider's web: a kinematic trigger that hurts the player and stops at walls.</summary>
        public static GameObject BuildWeb()
        {
            var root = new GameObject("GiantSpiderWeb") { layer = LayerMask.NameToLayer(Layers.Enemies) };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.15f;
            Sprite[] sprites = new[] { "E", "NE", "N", "NW", "W", "SW", "S", "SE" }
                .Select(d => MinifantasyImporter.Sprite(MinifantasySheets.GiantSpider, "GiantSpiderWeb", d)).ToArray();
            SpriteRenderer renderer = LookTestContent.AddSprite(root.transform, "Sprite", sprites[0], SortingLayers.YSorted, 2, Vector3.zero);
            var damage = root.AddComponent<DamageOnTouch>();
            damage.TargetLayerMask = LayerMask.GetMask(Layers.Player);
            damage.Owner = root;
            damage.InvincibilityDuration = 0.5f;
            damage.DamageCausedKnockbackType = DamageOnTouch.KnockbackStyles.NoKnockback;
            root.AddComponent<WebProjectile>().Configure(sprites, renderer, damage);
            return LookTestContent.SavePrefab(root, WebPrefab);
        }

        // ------------------------------------------------------------------ the way out

        /// <summary>
        /// The rope back to the tavern: hanging from a hole in the ceiling, its coil on the floor at the
        /// pivot. Standing in the trigger around the coil shows the hint; Interact climbs out.
        /// </summary>
        public static GameObject BuildRopeExit()
        {
            var root = new GameObject("RopeExit") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            var trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.4f, 1f);
            trigger.offset = new Vector2(0f, 0.3f);
            LookTestContent.AddSprite(root.transform, "Shadow", MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "RopesShadows", "Hanging"), SortingLayers.YSorted, -1, Vector3.zero);
            LookTestContent.AddSprite(root.transform, "Rope", MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Ropes", "Hanging"), SortingLayers.YSorted, 0, Vector3.zero);
            MMF_Player climb = LookTestContent.Feedback(root.transform, "Feedback_Climb", null, 0f, LookTestContent.Sfx("PH_Climb"), LookTestContent.Pattern(HapticIds.PulseSuccess));
            root.AddComponent<Hearthdelve.Dungeon.Run.DelveExit>().Configure(climb);
            return LookTestContent.SavePrefab(root, RopeExitPrefab);
        }

        // ------------------------------------------------------------------ enemies

        public static Enemies BuildEnemies(SpriteAnimationSet slime, SpriteAnimationSet slimeShadow)
        {
            BuildAnimationSets(out var bat, out var batShadow, out var spider, out var spiderShadow);
            GameObject web = BuildWeb();
            return new Enemies
            {
                Slime = BuildEnemy("GreenSlime", LookTestContent.SlimePrefab, AssetDatabase.LoadAssetAtPath<EnemyDefinition>(SlimeDefinitionPath),
                    slime, slimeShadow, new Vector2(0.8f, 0.5f), new Vector2(0f, 0.2f), 1.1f, null),
                Bat = BuildEnemy("Bat", BatPrefab, AssetDatabase.LoadAssetAtPath<EnemyDefinition>(BatDefinitionPath),
                    bat, batShadow, new Vector2(0.6f, 0.4f), new Vector2(0f, 0.2f), 1.6f, null),
                // The spider's legs spread wider than its body; the collider is the body, so it fits through doorways.
                Spider = BuildEnemy("GiantSpider", SpiderPrefab, AssetDatabase.LoadAssetAtPath<EnemyDefinition>(SpiderDefinitionPath),
                    spider, spiderShadow, new Vector2(0.9f, 0.5f), new Vector2(0f, 0.2f), 1.4f, web.GetComponent<WebProjectile>()),
            };
        }

        internal static GameObject BuildEnemy(string name, string path, EnemyDefinition definition, SpriteAnimationSet set, SpriteAnimationSet shadowSet,
            Vector2 colliderSize, Vector2 colliderOffset, float alertHeight, WebProjectile web, System.Action<GameObject> extras = null, bool boss = false)
        {
            GameObject root = LookTestContent.CharacterRoot(name, Layers.Enemies, colliderSize, colliderOffset);
            LookTestContent.AddModel(root, set, shadowSet, out SpriteRenderer body);

            var character = root.AddComponent<Character>();
            character.CharacterType = Character.CharacterTypes.AI;
            character.CharacterDimension = Character.CharacterDimensions.Type2D;
            character.CharacterModel = body.transform.parent.gameObject;
            root.AddComponent<CharacterMovement>();

            // A boss is brought down by lethal damage, for its optional finishing moment (4e step 3).
            Health health = boss ? root.AddComponent<Hearthdelve.Dungeon.Bosses.BossHealth>() : root.AddComponent<Health>();
            var rules = AssetDatabase.LoadAssetAtPath<Hearthdelve.Dungeon.Harvest.HarvestRulesConfig>($"{EditorPaths.Config}/HarvestRulesConfig.asset");
            if (health is Hearthdelve.Dungeon.Bosses.BossHealth bossHealth) bossHealth.ConfigureDowned(rules);
            health.DestroyOnDeath = true;
            SpriteAnim die = set.Find(CharacterAnim.Die);
            health.DelayBeforeDestruction = die != null ? die.frameDuration * die.frontRight.Length : 0.9f;
            // The enemy's side of a hit is only its flash: the weapon's feedback carries the hit-stop, shake, sound
            // and haptic, so a light and a heavy hit feel different and nothing plays twice.
            health.DamageMMFeedbacks = LookTestContent.Feedback(root.transform, "Feedback_Hit", body, 0f, null, null);
            character.CharacterHealth = health;
            root.AddComponent<EnemyIdentity>().Configure(definition);

            // The telegraph: a red "!" over the head, unlit so it reads in the dark, plus a flash and a warning sound.
            Sprite alertSprite = MinifantasyImporter.Sprite(MinifantasySheets.UserInterface, "GuiEmoticons", "AlertRed");
            SpriteRenderer alert = LookTestContent.AddSprite(root.transform, "Alert", alertSprite, SortingLayers.YSorted, 5, new Vector3(0f, alertHeight, 0f));
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            if (unlit != null) alert.sharedMaterial = unlit;
            alert.gameObject.SetActive(false);
            MMF_Player telegraph = LookTestContent.Feedback(root.transform, "Feedback_Telegraph", body, 0f, LookTestContent.Sfx("PH_Telegraph"), null);
            // Felt only when the attack is aimed at the player and near (EnemyAttack decides), and lightly.
            MMF_Player cue = LookTestContent.Feedback(root.transform, "Feedback_TelegraphCue", null, 0f, null, LookTestContent.Pattern(HapticIds.CueThreshold));
            cue.GetFeedbackOfType<MMF_HapticPattern>().Scale = 0.5f;

            var attacks = new List<EnemyAttack>();
            List<EnemyAttackSettings> settings = definition != null ? definition.AllAttacks.ToList() : new List<EnemyAttackSettings> { new() };
            for (int i = 0; i < settings.Count; i++)
            {
                GameObject hitbox = settings[i].kind == EnemyAttackKind.Spit ? null : Hitbox(root, $"Hitbox_{settings[i].debugName}");
                var attack = root.AddComponent<EnemyAttack>();
                attack.Configure(i, hitbox, alert.gameObject, telegraph, settings[i].kind == EnemyAttackKind.Spit ? web : null, body.transform, cue);
                attacks.Add(attack);
            }
            if (definition != null && definition.startsAsleep)
            {
                // Shown hanging in the editor too, so level layouts read as they will in play.
                GameObject shadow = body.transform.parent.Find("Shadow")?.gameObject;
                SpriteAnim sleep = set.Find(CharacterAnim.Sleep);
                if (sleep != null && sleep.frontRight.Length > 0) body.sprite = sleep.frontRight[0];
                if (shadow != null) shadow.SetActive(false);
                root.AddComponent<EnemyPerch>().Configure(shadow);
            }
            root.AddComponent<HitReaction>();
            // The Harvest Finisher (4e step 3): a drumstick over its head while it can be finished (unlit, to read in the dark).
            SpriteRenderer prompt = LookTestContent.AddSprite(root.transform, "FinisherPrompt",
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Food"), SortingLayers.YSorted, 6, new Vector3(0f, alertHeight + 0.15f, 0f));
            if (unlit != null) prompt.sharedMaterial = unlit;
            prompt.gameObject.SetActive(false);
            root.AddComponent<Hearthdelve.Dungeon.Harvest.FinisherTarget>().Configure(rules, prompt.gameObject);
            character.CharacterBrain = Brain(root, definition, attacks, settings.Select(a => a.debugName).ToList());
            // A boss's own parts (4e): its encounter, its stun, its ground marks.
            extras?.Invoke(root);
            return LookTestContent.SavePrefab(root, path);
        }

        /// <summary>How long the wake animation plays, so the bat unfolds fully before it flies.</summary>
        static float WakeDuration(GameObject root)
        {
            var animator = root.GetComponentInChildren<CharacterSpriteAnimator>();
            var set = animator != null ? (SpriteAnimationSet)new SerializedObject(animator).FindProperty("m_Set").objectReferenceValue : null;
            SpriteAnim wake = set != null ? set.Find(CharacterAnim.Wake) : null;
            return wake != null ? wake.frameDuration * wake.frontRight.Length : 0.5f;
        }

        static GameObject Hitbox(GameObject root, string name)
        {
            var hitbox = new GameObject(name) { layer = root.layer };
            hitbox.transform.SetParent(root.transform, false);
            hitbox.AddComponent<BoxCollider2D>().isTrigger = true;
            var damage = hitbox.AddComponent<DamageOnTouch>();
            damage.TargetLayerMask = LayerMask.GetMask(Layers.Player);
            damage.Owner = root;
            damage.InvincibilityDuration = 0.5f;
            hitbox.SetActive(false);
            return hitbox;
        }

        /// <summary>
        /// The TDE brain: Idle (or Sleep) → Chase (grid pathfinding) → one state per attack, back to
        /// Chase when it's done. Attacks start only when their range and line are right.
        /// </summary>
        static AIBrain Brain(GameObject root, EnemyDefinition definition, List<EnemyAttack> attacks, List<string> names)
        {
            bool asleep = definition != null && definition.startsAsleep;
            var idle = asleep ? (AIAction)root.AddComponent<AIActionSleep>() : root.AddComponent<AIActionDoNothing>();
            var detect = root.AddComponent<AIDecisionDetectTargetRadius2D>();
            detect.Radius = definition == null ? 8f : asleep ? definition.wakeRange : definition.aggroRange;
            detect.TargetLayer = LayerMask.GetMask(Layers.Player);
            detect.ObstacleDetection = false;
            var startTransitions = new AITransitionsList { new AITransition { Decision = detect, TrueState = asleep ? "Wake" : "Chase", FalseState = "" } };
            if (asleep)
            {
                // A hit wakes it too.
                startTransitions.Add(new AITransition { Decision = root.AddComponent<AIDecisionHit>(), TrueState = "Wake", FalseState = "" });
            }

            var target = root.AddComponent<AIActionSetPlayerAsTarget>();
            var chase = root.AddComponent<AIActionPathfindToTarget2D>();
            if (definition != null)
            {
                chase.KeepDistance = definition.keepDistance;
                chase.Flutter = definition.flutter;
            }
            var chaseTransitions = new AITransitionsList();
            var states = new List<AIState>
            {
                new() { StateName = asleep ? "Sleep" : "Idle", Actions = new AIActionsList { idle }, Transitions = startTransitions },
                new() { StateName = "Chase", Actions = new AIActionsList { target, chase }, Transitions = chaseTransitions },
            };
            if (asleep)
            {
                // Waking: let go of the wall and unfold in place, then give chase.
                var wake = root.AddComponent<AIActionWake>();
                var awake = root.AddComponent<AIDecisionTimeInState>();
                awake.AfterTimeMin = awake.AfterTimeMax = WakeDuration(root);
                states.Add(new AIState
                {
                    StateName = "Wake",
                    Actions = new AIActionsList { wake },
                    Transitions = new AITransitionsList { new AITransition { Decision = awake, TrueState = "Chase", FalseState = "" } },
                });
            }
            for (int i = 0; i < attacks.Count; i++)
            {
                string stateName = $"Attack {names[i]}";
                var ready = root.AddComponent<AIDecisionEnemyAttackReady>();
                ready.Attack = attacks[i];
                chaseTransitions.Add(new AITransition { Decision = ready, TrueState = stateName, FalseState = "" });
                var act = root.AddComponent<AIActionEnemyAttack>();
                act.Attack = attacks[i];
                var done = root.AddComponent<AIDecisionEnemyAttackDone>();
                done.Attack = attacks[i];
                states.Add(new AIState
                {
                    StateName = stateName,
                    Actions = new AIActionsList { act },
                    Transitions = new AITransitionsList { new AITransition { Decision = done, TrueState = "Chase", FalseState = "" } },
                });
            }
            var brain = root.AddComponent<AIBrain>();
            brain.States = states;
            return brain;
        }
    }
}
