using System;
using System.Collections.Generic;
using Hearthdelve.Dungeon.Combat;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Ingredients;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Creates the Phase 1 data assets (configs, Butcher's Cleaver, Cellars ingredients and
    /// enemies). Existing assets are left alone so inspector tuning is never overwritten;
    /// delete an asset to regenerate it with the defaults below.
    /// </summary>
    public static class ContentGenerator
    {
        public sealed class Content
        {
            public PlayerMovementConfig Movement;
            public EssenceConfig Essence;
            public HarvestRulesConfig HarvestRules;
            public DelveConfig Delve;
            public WeaponDefinition Cleaver;
            public EnemyDefinition Rat, Slime, Shroom, Dummy;
            public readonly Dictionary<string, IngredientDefinition> Ingredients = new();
        }

        [MenuItem("Hearthdelve/Generate/Phase 1 Data Assets", priority = 110)]
        static void GenerateMenu()
        {
            Generate();
            LocalizationBuilder.Build();
        }

        public static Content Generate()
        {
            foreach (var folder in new[] { EditorPaths.Ingredients, EditorPaths.Enemies, EditorPaths.Weapons, EditorPaths.Config })
                EditorPaths.Ensure(folder);

            var c = new Content
            {
                Movement = LoadOrCreate<PlayerMovementConfig>($"{EditorPaths.Config}/PlayerMovementConfig.asset", _ => { }),
                Essence = LoadOrCreate<EssenceConfig>($"{EditorPaths.Config}/EssenceConfig.asset", _ => { }),
                HarvestRules = LoadOrCreate<HarvestRulesConfig>($"{EditorPaths.Config}/HarvestRulesConfig.asset", _ => { }),
                Delve = LoadOrCreate<DelveConfig>($"{EditorPaths.Config}/DelveConfig.asset", _ => { }),
            };

            Ingredient(c, "rat_haunch", "Rat Haunch", IngredientCategory.Meat, FlavorTags.Savory, Rarity.Common, 6, new Color(0.78f, 0.45f, 0.38f));
            Ingredient(c, "rat_liver", "Rat Liver", IngredientCategory.Offal, FlavorTags.Bitter | FlavorTags.Savory, Rarity.Common, 5, new Color(0.55f, 0.2f, 0.28f));
            Ingredient(c, "slime_gel", "Slime Gel", IngredientCategory.Liquid, FlavorTags.Sweet, Rarity.Common, 4, new Color(0.5f, 0.9f, 0.45f));
            Ingredient(c, "slime_core", "Slime Core", IngredientCategory.Magical, FlavorTags.Sweet | FlavorTags.Arcane, Rarity.Uncommon, 14, new Color(0.3f, 0.95f, 0.75f));
            Ingredient(c, "shroom_cap", "Shroom Cap", IngredientCategory.Fungus, FlavorTags.Earthy | FlavorTags.Umami, Rarity.Common, 5, new Color(0.72f, 0.55f, 0.82f));
            Ingredient(c, "spore_sac", "Spore Sac", IngredientCategory.Spice, FlavorTags.Spicy | FlavorTags.Earthy, Rarity.Uncommon, 9, new Color(0.88f, 0.8f, 0.4f));

            c.Cleaver = LoadOrCreate<WeaponDefinition>($"{EditorPaths.Weapons}/Weapon_ButchersCleaver.asset", w =>
            {
                w.id = "butchers_cleaver";
                w.weaponType = WeaponType.Cleaver;
                w.element = Element.None;
                w.cleanKillCategories = IngredientCategory.Meat | IngredientCategory.Offal;
                w.inputBuffer = 0.15f;
                w.comboLinkWindow = 0.2f;
                w.combo = new List<AttackData>
                {
                    new()
                    {
                        debugName = "Chop", startupFrames = 5, activeFrames = 3, recoveryFrames = 14, cancelFrame = 11,
                        damage = 8f, hitboxOffset = new Vector2(0.85f, 0.75f), hitboxSize = new Vector2(1.4f, 1.0f),
                        knockback = new Vector2(3f, 1.5f), staggerTime = 0.25f,
                        lungeSpeed = 2.5f, lungeStartFrame = 2, lungeFrames = 4, hitStop = 0.05f, screenShake = 0.08f,
                    },
                    new()
                    {
                        debugName = "Backhand", startupFrames = 5, activeFrames = 3, recoveryFrames = 14, cancelFrame = 11,
                        damage = 8f, hitboxOffset = new Vector2(0.85f, 0.8f), hitboxSize = new Vector2(1.5f, 1.1f),
                        knockback = new Vector2(3.5f, 1.5f), staggerTime = 0.25f,
                        lungeSpeed = 2.5f, lungeStartFrame = 2, lungeFrames = 4, hitStop = 0.05f, screenShake = 0.08f,
                    },
                    new()
                    {
                        debugName = "Cleave", startupFrames = 10, activeFrames = 4, recoveryFrames = 22, cancelFrame = 28,
                        damage = 20f, hitboxOffset = new Vector2(1.0f, 0.7f), hitboxSize = new Vector2(1.8f, 1.4f),
                        knockback = new Vector2(8f, 4f), staggerTime = 0.5f,
                        lungeSpeed = 4f, lungeStartFrame = 4, lungeFrames = 6, hitStop = 0.12f, screenShake = 0.25f,
                    },
                };
            });
            c.Cleaver.displayName = LocalizationBuilder.ContentString("weapon.butchers_cleaver", "Butcher's Cleaver");
            EditorUtility.SetDirty(c.Cleaver);

            c.Rat = Enemy(c, "giant_rat", "Giant Rat", e =>
            {
                e.maxHealth = 22f; e.moveSpeed = 3.2f; e.aggroRange = 7f; e.attackRange = 3f;
                e.placeholderColor = new Color(0.62f, 0.5f, 0.42f);
                e.attack = new EnemyAttackSettings
                {
                    telegraph = 0.5f, active = 0.25f, recovery = 0.5f, cooldown = 1.0f, damage = 10f,
                    hitboxOffset = new Vector2(0.55f, 0.3f), hitboxSize = new Vector2(0.8f, 0.6f),
                    knockback = new Vector2(6f, 5f), lungeSpeed = 12f,
                };
                e.harvest = new List<HarvestPart>
                {
                    new() { ingredient = c.Ingredients["rat_haunch"], baseQuality = Quality.Standard, dropChance = 1f, minCount = 1, maxCount = 2 },
                    new() { ingredient = c.Ingredients["rat_liver"], baseQuality = Quality.Standard, dropChance = 0.35f, minCount = 1, maxCount = 1 },
                };
            });

            c.Slime = Enemy(c, "green_slime", "Green Slime", e =>
            {
                e.maxHealth = 34f; e.moveSpeed = 1.6f; e.aggroRange = 8f; e.attackRange = 4.5f;
                e.knockbackMultiplier = 0.4f; e.superArmorWhileAttacking = true;
                e.placeholderColor = new Color(0.45f, 0.85f, 0.4f);
                e.attack = new EnemyAttackSettings
                {
                    telegraph = 0.7f, active = 0.8f, recovery = 0.6f, cooldown = 1.6f, damage = 14f,
                    hitboxOffset = new Vector2(0f, 0.3f), hitboxSize = new Vector2(2.4f, 0.8f),
                    knockback = new Vector2(7f, 6f), leapHeight = 2.5f, leapMaxDistance = 5f,
                };
                e.harvest = new List<HarvestPart>
                {
                    new() { ingredient = c.Ingredients["slime_gel"], baseQuality = Quality.Standard, dropChance = 1f, minCount = 1, maxCount = 3 },
                    new() { ingredient = c.Ingredients["slime_core"], baseQuality = Quality.Fine, dropChance = 0.25f, minCount = 1, maxCount = 1 },
                };
            });

            c.Shroom = Enemy(c, "cellar_shroom", "Cellar Shroom", e =>
            {
                e.maxHealth = 18f; e.moveSpeed = 0f; e.aggroRange = 9f; e.attackRange = 8f; e.knockbackMultiplier = 0f;
                e.placeholderColor = new Color(0.7f, 0.5f, 0.8f);
                e.attack = new EnemyAttackSettings
                {
                    telegraph = 0.8f, active = 0.1f, recovery = 0.4f, cooldown = 1.8f, damage = 9f,
                    projectileFlightTime = 0.8f, projectileSpawnOffset = new Vector2(0.3f, 1.0f),
                };
                e.harvest = new List<HarvestPart>
                {
                    new() { ingredient = c.Ingredients["shroom_cap"], baseQuality = Quality.Standard, dropChance = 1f, minCount = 1, maxCount = 2 },
                    new() { ingredient = c.Ingredients["spore_sac"], baseQuality = Quality.Fine, dropChance = 0.4f, minCount = 1, maxCount = 1 },
                };
            });

            c.Dummy = Enemy(c, "training_dummy", "Training Dummy", e =>
            {
                e.maxHealth = 500f; e.moveSpeed = 0f; e.aggroRange = 0f; e.attackRange = 0f;
                e.knockbackMultiplier = 0f; e.regenerates = true;
                e.placeholderColor = new Color(0.82f, 0.7f, 0.5f);
            });

            AssetDatabase.SaveAssets();
            return c;
        }

        static void Ingredient(Content c, string id, string english, IngredientCategory category, FlavorTags flavors, Rarity rarity, int value, Color color)
        {
            var def = LoadOrCreate<IngredientDefinition>($"{EditorPaths.Ingredients}/Ingredient_{Pascal(id)}.asset", d =>
            {
                d.id = id;
                d.category = category;
                d.flavors = flavors;
                d.rarity = rarity;
                d.baseValue = value;
                d.placeholderColor = color;
            });
            def.displayName = LocalizationBuilder.ContentString($"ingredient.{id}", english);
            EditorUtility.SetDirty(def);
            c.Ingredients[id] = def;
        }

        static EnemyDefinition Enemy(Content c, string id, string english, Action<EnemyDefinition> init)
        {
            var def = LoadOrCreate<EnemyDefinition>($"{EditorPaths.Enemies}/Enemy_{Pascal(id)}.asset", d =>
            {
                d.id = id;
                init(d);
            });
            def.displayName = LocalizationBuilder.ContentString($"enemy.{id}", english);
            EditorUtility.SetDirty(def);
            return def;
        }

        public static T LoadOrCreate<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static string Pascal(string id)
        {
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0) parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            return string.Concat(parts);
        }
    }
}
