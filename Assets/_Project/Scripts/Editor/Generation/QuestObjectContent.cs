using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Haptics;
using Hearthdelve.Dungeon.Rooms;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Quests;
using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Quest objects (4g Checkpoint B): Boog's bomb (made once: its placement, policy and reward are then tuned on the asset) and the
    /// pickup that shows any quest object on the floor of the Hollows (rebuilt every run). Run by the dungeon run updater.
    /// </summary>
    public static class QuestObjectContent
    {
        public const string BoogsBomb = "boogs_bomb";
        public const string BoogsBombQuest = "boogs_bomb";
        const string Folder = EditorPaths.Data + "/Quests";
        public const string BombPath = Folder + "/QuestObject_" + BoogsBomb + ".asset";
        public const string PickupPrefab = EditorPaths.Prefabs + "/Dungeon/QuestObjectPickup.prefab";

        /// <summary>The objects' names (UI table, <c>quest_object.&lt;id&gt;</c>).</summary>
        public static readonly (string key, string english)[] English =
        {
            ($"quest_object.{BoogsBomb}", "Boog's bomb"),
        };

        /// <summary>Its frames: the Goblin Sapper's bomb, the fuse sputtering (row 0 of its sheet).</summary>
        public static Sprite[] BombFrames() => MinifantasyImporter.Row(MinifantasySheets.GoblinSapper, "GoblinSapperBomb", 0, 10);

        public static void Build(RunSettings settings)
        {
            EditorPaths.Ensure(Folder);
            QuestObjectDefinition bomb = StoryLoadOrCreate<QuestObjectDefinition>(BombPath, d =>
            {
                d.floor = 1;
                d.afterRoomsCleared = 2;
                d.lostOnDeath = true;
                d.offeredAgain = true;
                d.rewardGold = 60;
                d.frameSeconds = 0.1f;
            });
            bomb.id = BoogsBomb;
            bomb.questId = BoogsBombQuest;
            bomb.frames = BombFrames();
            EditorUtility.SetDirty(bomb);

            GameDatabase database = CurioContent.Database();
            database.questObjects ??= new List<QuestObjectDefinition>();
            database.questObjects.RemoveAll(q => q == null || q.id == bomb.id);
            database.questObjects.Add(bomb);
            EditorUtility.SetDirty(database);

            if (settings != null)
            {
                settings.questObjectPickup = BuildPickup(bomb.frames.Length > 0 ? bomb.frames[0] : null);
                EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
        }

        static T StoryLoadOrCreate<T>(string path, System.Action<T> created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            created?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>
        /// The pickup: the object's own frames (unlit, like the other pickups, so it reads on a dim floor), rocking as if it might go
        /// off, with a flickering orange glow from the fuse; a find's chime and the discovery haptic.
        /// </summary>
        static QuestObjectPickup BuildPickup(Sprite first)
        {
            var root = new GameObject("QuestObjectPickup") { layer = LayerMask.NameToLayer(Layers.Pickup) };
            var trigger = root.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.5f;
            trigger.offset = new Vector2(0f, 0.2f);
            SpriteRenderer sprite = LookTestContent.AddSprite(root.transform, "Object", first, SortingLayers.YSorted, 0, new Vector3(0f, 0f, 0f));
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (unlit != null) sprite.sharedMaterial = unlit;
            Light2D glow = LookTestBuilder.Light("Glow", new Vector3(0f, 0.3f, 0f), Light2D.LightType.Point);
            glow.transform.SetParent(root.transform, false);
            glow.color = new Color(1f, 0.55f, 0.2f);
            glow.intensity = 1.1f;
            glow.pointLightInnerRadius = 0.2f;
            glow.pointLightOuterRadius = 2f;
            MMF_Player feedback = LookTestContent.Feedback(root.transform, "Feedback_Found", null, 0f, LookTestContent.Sfx("PH_Discovery"),
                LookTestContent.Pattern(HapticIds.DiscoveryFound));
            root.AddComponent<QuestObjectPickup>().Configure(sprite, glow, feedback);
            return LookTestContent.SavePrefab(root, PickupPrefab).GetComponent<QuestObjectPickup>();
        }
    }
}
