using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using System.Linq;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The Cellars' boss, the Larder Troll (4e): its animation sets (Minifantasy's Ancient Troll), its enemy data (two
    /// patterns in step 1: the ground slam and the lumbering charge), its boss definition, and its prefab: an enemy like
    /// the others plus the boss encounter, the charge's stun and the ground marks for both telegraphs. Existing tuning
    /// in the data assets is kept.
    /// </summary>
    public static class BossContent
    {
        public const string EnemyPath = EditorPaths.Enemies + "/Enemy_LarderTroll.asset";
        public const string BossPath = EditorPaths.Data + "/Dungeon/Bosses/Boss_LarderTroll.asset";
        public const string PrefabPath = EditorPaths.Prefabs + "/Enemies/LarderTroll.prefab";
        public const string Id = "larder_troll";

        public static BossDefinition BuildLarderTroll()
        {
            BuildAnimationSets(out SpriteAnimationSet set, out SpriteAnimationSet shadow);
            EnemyDefinition enemy = LookTestContent.LoadOrCreate<EnemyDefinition>(EnemyPath, Defaults);
            // What it carried (step 3): a couple of the deeper Cellars' parts, Fine, or Premium to the finisher. Set once.
            if (enemy.harvest == null || enemy.harvest.Count == 0)
            {
                var core = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_SlimeCore.asset");
                var venom = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_VenomSac.asset");
                enemy.harvest = new List<Hearthdelve.Dungeon.Harvest.HarvestPart>
                {
                    new() { ingredient = core, baseQuality = Hearthdelve.Shared.Ingredients.Quality.Fine, dropChance = 1f, minCount = 1, maxCount = 1 },
                    new() { ingredient = venom, baseQuality = Hearthdelve.Shared.Ingredients.Quality.Fine, dropChance = 1f, minCount = 1, maxCount = 1 },
                };
                EditorUtility.SetDirty(enemy);
            }
            if (enemy.displayName == null || enemy.displayName.IsEmpty)
            {
                enemy.displayName = LocalizationBuilder.ContentString("enemy.larder_troll", "the Larder Troll");
                EditorUtility.SetDirty(enemy);
            }
            EditorPaths.Ensure(System.IO.Path.GetDirectoryName(BossPath)?.Replace('\\', '/'));
            BossDefinition boss = LookTestContent.LoadOrCreate<BossDefinition>(BossPath, b =>
            {
                b.id = Id;
                b.drainMultiplierWhileActive = 0f;
                b.entranceSeconds = 1.6f;
            });
            // What its slams shake loose (step 2): the Cellars' everyday parts, as ordinary parts. Set once; edits are kept.
            if (boss.feeding.scraps == null || boss.feeding.scraps.Length == 0)
            {
                boss.feeding.scraps = new[] { "SlimeGel", "SpiderLeg", "BatWing" }
                    .Select(a => AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_{a}.asset"))
                    .Where(i => i != null).ToArray();
                EditorUtility.SetDirty(boss);
            }

            GameObject prefab = DungeonContent.BuildEnemy("LarderTroll", PrefabPath, enemy, set, shadow, new Vector2(1.3f, 0.7f), new Vector2(0f, 0.3f), 2.4f, null,
                root => AddBossParts(root, boss), boss: true);
            boss.prefab = prefab;
            EditorUtility.SetDirty(boss);
            AssetDatabase.SaveAssets();
            return boss;
        }

        static void AddBossParts(GameObject root, BossDefinition boss)
        {
            SpriteRenderer body = root.GetComponentInChildren<CharacterSpriteAnimator>().GetComponent<SpriteRenderer>();
            EnemyAttack[] attacks = root.GetComponents<EnemyAttack>();
            // The slam lands with a thud and a shake (felt lightly even on a miss: the floor shakes).
            attacks[0].ConfigureImpact(LookTestContent.Feedback(root.transform, "Feedback_Slam", null, 0.45f,
                LookTestContent.Sfx("PH_TrollSlam"), LookTestContent.Pattern(HapticIds.BumpSoft)));
            // The charge into a wall: a harder thud, a bigger shake, a hard bump; then it stands dazed.
            root.AddComponent<ChargeStun>().Configure(1, LookTestContent.Feedback(root.transform, "Feedback_Stun", body, 0.6f,
                LookTestContent.Sfx("PH_TrollThud"), LookTestContent.Pattern(HapticIds.BumpHard)));
            root.AddComponent<AttackTelegraphMarker>().Configure(0, AttackTelegraphMarker.Shape.Area, Mark(root, "SlamMark"));
            var line = root.AddComponent<AttackTelegraphMarker>();
            line.Configure(1, AttackTelegraphMarker.Shape.Line, Mark(root, "ChargeMark"));
            // The entrance: a roar, a shake and the boss rumble. The defeat: slow motion, the fall, a big shake, a heavy rumble.
            MMF_Player defeat = LookTestContent.Feedback(root.transform, "Feedback_Defeat", null, 0.8f,
                LookTestContent.Sfx("PH_TrollFall"), LookTestContent.Pattern(HapticIds.HitHeavy));
            defeat.AddFeedback(new MMF_TimescaleModifier { Label = "Slow motion", Mode = MMF_TimescaleModifier.Modes.Shake, TimeScale = 0.3f, TimeScaleDuration = 0.8f });
            root.AddComponent<BossEncounter>().Configure(boss, LookTestContent.Feedback(root.transform, "Feedback_Entrance", null, 0.5f,
                LookTestContent.Sfx("PH_TrollRoar"), LookTestContent.Pattern(HapticIds.BossTelegraph)), defeat);

            // Step 2: the frenzy (a roar, a big shake, the phase-change rumble), its stolen larder, and its appetite.
            root.AddComponent<BossFrenzy>().Configure(boss, LookTestContent.Feedback(root.transform, "Feedback_Frenzy", body, 0.7f,
                LookTestContent.Sfx("PH_TrollRoar"), LookTestContent.Pattern(HapticIds.BossPhaseChange)));
            root.AddComponent<LarderScraps>().Configure(boss);
            var eater = root.AddComponent<ScrapEater>();
            eater.Configure(boss,
                LookTestContent.Feedback(root.transform, "Feedback_Gulp", body, 0f, LookTestContent.Sfx("PH_TrollGulp"), null),
                LookTestContent.Feedback(root.transform, "Feedback_Spoil", null, 0.2f, LookTestContent.Sfx("PH_TrollSpoil"), LookTestContent.Pattern(HapticIds.TapFirm)));
            AddFeeding(root, eater);
        }

        /// <summary>
        /// The brain's appetite (step 2): from the chase, after the slam (so it still answers a player in reach) and before
        /// the charge, it goes for a part on the floor (Feed: target the part, walk to it), then eats it (Eat), then returns
        /// to the chase. Taking the part sends it back to the chase at once.
        /// </summary>
        static void AddFeeding(GameObject root, ScrapEater eater)
        {
            AIBrain brain = root.GetComponent<AIBrain>();
            AIState chase = brain.States.First(s => s.StateName == "Chase");
            var wants = root.AddComponent<AIDecisionWantsFood>();
            wants.Eater = eater;
            chase.Transitions.Insert(Mathf.Min(1, chase.Transitions.Count), new AITransition { Decision = wants, TrueState = "Feed", FalseState = "" });

            var target = root.AddComponent<AIActionTargetFood>();
            target.Eater = eater;
            var walk = root.AddComponent<AIActionPathfindToTarget2D>();
            walk.StopDistance = 0.4f;
            var reach = root.AddComponent<AIDecisionFoodInReach>();
            reach.Eater = eater;
            var gone = root.AddComponent<AIDecisionWantsFood>();
            gone.Eater = eater;
            gone.Invert = true;
            brain.States.Add(new AIState
            {
                StateName = "Feed",
                Actions = new AIActionsList { target, walk },
                Transitions = new AITransitionsList
                {
                    new AITransition { Decision = reach, TrueState = "Eat", FalseState = "" },
                    new AITransition { Decision = gone, TrueState = "Chase", FalseState = "" },
                },
            });

            var eat = root.AddComponent<AIActionEat>();
            eat.Eater = eater;
            var done = root.AddComponent<AIDecisionDoneEating>();
            done.Eater = eater;
            brain.States.Add(new AIState
            {
                StateName = "Eat",
                Actions = new AIActionsList { eat },
                Transitions = new AITransitionsList { new AITransition { Decision = done, TrueState = "Chase", FalseState = "" } },
            });
        }

        /// <summary>A tinted pixel on the floor (unlit, so it reads in the dark), placed and scaled by the marker.</summary>
        static SpriteRenderer Mark(GameObject root, string name)
        {
            SpriteRenderer mark = LookTestContent.AddSprite(root.transform, name, DungeonUI.Pixel(), SortingLayers.Floor, 20, Vector3.zero);
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (unlit != null) mark.sharedMaterial = unlit;
            // A pixel is an eighth of a tile at 8 PPU: scale 8 makes a 1×1 tile mark; the marker scales from there.
            mark.drawMode = SpriteDrawMode.Simple;
            mark.gameObject.SetActive(false);
            return mark;
        }

        /// <summary>
        /// The troll's data: unshakeable, quick for its size, two readable patterns with short openings (made much harder after the
        /// step 1 playtest). New assets only; edits are kept.
        /// </summary>
        static void Defaults(EnemyDefinition d)
        {
            d.id = Id;
            d.maxHealth = 900f;
            d.moveSpeed = 3.8f;
            d.aggroRange = 40f;
            d.knockbackMultiplier = 0f;
            d.staggerMultiplier = 0f;
            d.superArmorWhileAttacking = true;
            d.keepDistance = Vector2.zero;
            d.placeholderColor = new Color(0.45f, 0.5f, 0.2f);
            d.attack = new EnemyAttackSettings
            {
                // Arms up over a long wind-up, then both fists into the floor in front: step out of the mark, then punish
                // the long recovery.
                debugName = "Slam", kind = EnemyAttackKind.Bite, minRange = 0f, maxRange = 3f,
                telegraph = 0.7f, active = 0.18f, recovery = 0.55f, cooldown = 0.5f, damage = 26f,
                hitboxOffset = new Vector2(0.8f, 0.3f), hitboxSize = new Vector2(3.6f, 3f),
                animation = CharacterAnim.Attack, releaseFrame = 4,
            };
            d.otherAttacks = new List<EnemyAttackSettings>
            {
                new()
                {
                    // A lumbering run along the marked line: dodge aside, and bait it into a pillar or wall to stun it.
                    debugName = "Charge", kind = EnemyAttackKind.Swoop, minRange = 3.5f, maxRange = 18f,
                    telegraph = 0.6f, active = 1.6f, recovery = 0.4f, cooldown = 1f, damage = 24f,
                    hitboxOffset = new Vector2(0f, 0.3f), hitboxSize = new Vector2(2f, 1.6f),
                    animation = CharacterAnim.Walk, releaseFrame = 2, travelDistance = 16f, stunOnBlock = 1.6f,
                },
            };
        }

        /// <summary>The Ancient Troll's sheets: idle has two front rows; its back facings borrow the walk's first frame.</summary>
        static void BuildAnimationSets(out SpriteAnimationSet set, out SpriteAnimationSet shadow)
        {
            const string t = MinifantasySheets.AncientTroll;
            set = LookTestContent.Set("Anim_LarderTroll",
                Idle(t, "AncientTrollIdle", "AncientTrollWalk"),
                LookTestContent.Anim(CharacterAnim.Walk, t, "AncientTrollWalk", 6, 4, 0.12f, true),
                LookTestContent.Anim(CharacterAnim.Attack, t, "AncientTrollAttack", 7, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, t, "AncientTrollDmg", 4, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Die, t, "AncientTrollDie", 21, 1, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Eat, t, "AncientTrollEat", 11, 1, 0.1f, true));
            shadow = LookTestContent.Set("Anim_LarderTroll_Shadow",
                Idle(t, "AncientTrollIdleShadow", "AncientTrollWalkShadow"),
                LookTestContent.Anim(CharacterAnim.Walk, t, "AncientTrollWalkShadow", 6, 4, 0.12f, true),
                LookTestContent.Anim(CharacterAnim.Attack, t, "AncientTrollAttackShadow", 7, 4, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Hurt, t, "AncientTrollDmgShadow", 4, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Die, t, "AncientTrollDieShadow", 21, 1, 0.1f, false),
                LookTestContent.Anim(CharacterAnim.Eat, t, "AncientTrollEatShadow", 11, 1, 0.1f, true));
        }

        static SpriteAnim Idle(string pack, string idle, string walk)
        {
            SpriteAnim anim = LookTestContent.AnimRow(CharacterAnim.Idle, pack, idle, 18, 0, 0.15f, true);
            anim.frontLeft = LookTestContent.AnimRow(CharacterAnim.Idle, pack, idle, 18, 1, 0.15f, true).frontRight;
            SpriteAnim walking = LookTestContent.Anim(CharacterAnim.Walk, pack, walk, 6, 4, 0.12f, true);
            anim.backRight = new[] { walking.backRight[0] };
            anim.backLeft = new[] { walking.backLeft[0] };
            return anim;
        }
    }
}
