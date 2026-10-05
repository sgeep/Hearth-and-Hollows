using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using MoreMountains.Feedbacks;
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

            GameObject prefab = DungeonContent.BuildEnemy("LarderTroll", PrefabPath, enemy, set, shadow, new Vector2(1.3f, 0.7f), new Vector2(0f, 0.3f), 2.4f, null,
                root => AddBossParts(root, boss));
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
            // The entrance: a roar, a shake and the boss rumble.
            root.AddComponent<BossEncounter>().Configure(boss, LookTestContent.Feedback(root.transform, "Feedback_Entrance", null, 0.5f,
                LookTestContent.Sfx("PH_TrollRoar"), LookTestContent.Pattern(HapticIds.BossTelegraph)));
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

        /// <summary>The troll's data: lumbering, unshakeable, two readable patterns. New assets only; edits are kept.</summary>
        static void Defaults(EnemyDefinition d)
        {
            d.id = Id;
            d.maxHealth = 500f;
            d.moveSpeed = 2.4f;
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
                debugName = "Slam", kind = EnemyAttackKind.Bite, minRange = 0f, maxRange = 2.4f,
                telegraph = 0.95f, active = 0.18f, recovery = 1.0f, cooldown = 1.2f, damage = 18f,
                hitboxOffset = new Vector2(0.7f, 0.3f), hitboxSize = new Vector2(3f, 2.4f),
                animation = CharacterAnim.Attack, releaseFrame = 4,
            };
            d.otherAttacks = new List<EnemyAttackSettings>
            {
                new()
                {
                    // A lumbering run along the marked line: dodge aside, and bait it into a pillar or wall to stun it.
                    debugName = "Charge", kind = EnemyAttackKind.Swoop, minRange = 4f, maxRange = 16f,
                    telegraph = 0.9f, active = 1.6f, recovery = 0.7f, cooldown = 2.2f, damage = 16f,
                    hitboxOffset = new Vector2(0f, 0.3f), hitboxSize = new Vector2(1.6f, 1.3f),
                    animation = CharacterAnim.Walk, releaseFrame = 2, travelDistance = 13f, stunOnBlock = 2.4f,
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
