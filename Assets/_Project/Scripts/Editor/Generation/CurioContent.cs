using System.Collections.Generic;
using Hearthdelve.Dungeon.Bosses;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Run;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The Cellars' furnishing discoveries and the Larder Troll's trophy (4f Checkpoint C; plan §14, §16). The pool asset is
    /// filled once (later tuning is kept); the boss's trophy is linked every run. Pieces come from the catalogue
    /// (<c>catalog.json</c>), where discovery-only pieces have no shop source.
    /// </summary>
    public static class CurioContent
    {
        public const string PoolPath = EditorPaths.Data + "/Dungeon/CurioPool_Cellars.asset";
        public const string BossPath = EditorPaths.Data + "/Dungeon/Bosses/Boss_LarderTroll.asset";
        public const string TrophyId = "trophy_larder_troll";

        const string k_Slime = "green_slime", k_Bat = "bat", k_Spider = "giant_spider";

        /// <summary>
        /// Who gives what. Spiders: webs and the spider brazier; slimes: the slime jars; bats: the tattered banner.
        /// The rest only in curio rooms' caches: the cellar's leftovers and stranger finds.
        /// </summary>
        static readonly (string id, float weight, string[] droppedBy, bool rooms)[] k_Cellars =
        {
            ("cobweb", 1f, new[] { k_Spider }, true),
            ("cobweb_great", 0.6f, new[] { k_Spider }, true),
            ("spider_brazier", 0.5f, new[] { k_Spider }, true),
            ("slime_jars", 1f, new[] { k_Slime }, true),
            ("tattered_banner", 1f, new[] { k_Bat }, true),
            ("skull_candle", 1f, new string[0], true),
            ("iron_cage", 0.8f, new string[0], true),
            ("cellar_stores", 0.8f, new string[0], true),
            ("junk_heap", 0.5f, new string[0], true),
            ("mimic_chest", 0.4f, new string[0], true),
        };

        public static CurioPool BuildCellarPool(GameDatabase database)
        {
            CurioPool pool = LookTestContent.LoadOrCreate<CurioPool>(PoolPath);
            if (pool.entries.Count == 0 && database != null)
            {
                foreach (var (id, weight, droppedBy, rooms) in k_Cellars)
                {
                    FurnitureDefinition piece = database.Furniture(id);
                    if (piece == null)
                    {
                        Debug.LogWarning($"[Hearthdelve] Curio pool: no furniture '{id}' in the catalogue.");
                        continue;
                    }
                    pool.entries.Add(new CurioEntry { piece = piece, weight = weight, droppedBy = droppedBy, inRoomCaches = rooms });
                }
                EditorUtility.SetDirty(pool);
            }
            return pool;
        }

        /// <summary>The Larder Troll's trophy: on its boss data (for the record) and in the database's trophy list.</summary>
        public static void LinkTrophies(GameDatabase database)
        {
            if (database == null) return;
            var boss = AssetDatabase.LoadAssetAtPath<BossDefinition>(BossPath);
            FurnitureDefinition trophy = database.Furniture(TrophyId);
            if (boss == null || trophy == null)
            {
                Debug.LogWarning("[Hearthdelve] The Larder Troll's trophy couldn't be linked (boss or trophy missing).");
                return;
            }
            if (boss.trophyId != TrophyId)
            {
                boss.trophyId = TrophyId;
                EditorUtility.SetDirty(boss);
            }
            database.bossTrophies ??= new List<BossTrophy>();
            database.bossTrophies.RemoveAll(t => t == null || t.bossId == boss.id);
            database.bossTrophies.Add(new BossTrophy { bossId = boss.id, trophy = trophy });
            EditorUtility.SetDirty(database);
        }

        public static GameDatabase Database() => AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset");
    }
}
