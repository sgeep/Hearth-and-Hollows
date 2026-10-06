using System;
using System.Collections.Generic;
using System.IO;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using UnityEngine;

namespace Hearthdelve.Shared.Save
{
    /// <summary>
    /// Versioned JSON saves (CLAUDE.md): <see cref="GameState"/> ⇄ <see cref="SaveData"/> ⇄ JSON,
    /// with migrations from older versions. Unknown content ids are dropped (with a warning)
    /// rather than failing the load. Pure logic; file access is in <see cref="SaveStore"/>.
    /// </summary>
    public static class SaveSystem
    {
        public const int CurrentVersion = 7;

        /// <summary>Starting pieces added in version 7 (4f Checkpoint C); a version 6 save gets them once, in storage.</summary>
        public static readonly string[] StartersAddedInV7 = { "butcher_block" };

        /// <summary>The seat upgrade retired in 4f (D16): seating comes from placed tables and chairs.</summary>
        public const string RetiredSeatUpgrade = "tavern_seats";
        /// <summary>What each of its levels cost when it was retired: migrated saves get it back.</summary>
        static readonly int[] k_RetiredSeatUpgradeCosts = { 120, 220 };

        /// <summary>The Gold refunded for <paramref name="levels"/> bought levels of the retired seat upgrade.</summary>
        public static int SeatUpgradeRefund(int levels)
        {
            int gold = 0;
            for (int i = 0; i < levels && i < k_RetiredSeatUpgradeCosts.Length; i++) gold += k_RetiredSeatUpgradeCosts[i];
            return gold;
        }

        public static SaveData Capture(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var data = new SaveData
            {
                version = CurrentVersion,
                day = state.Day,
                phase = state.Phase.ToString(),
                gold = state.Gold,
                renown = state.Renown,
                meal = new MealData { kind = state.Meal.Kind.ToString(), amount = state.Meal.Amount, recipe = state.Meal.RecipeId },
            };
            foreach (var s in state.Storeroom.Stacks)
            {
                if (s.IsEmpty || !s.Item.IsValid) continue;
                data.storeroom.Add(new StackData
                {
                    ingredient = s.Item.Definition.id,
                    quality = s.Item.Quality.ToString(),
                    prep = s.Item.Prep.ToString(),
                    count = s.Count,
                    freshness = s.Freshness,
                });
            }
            foreach (var pair in state.UpgradeLevels)
                data.upgrades.Add(new UpgradeData { id = pair.Key, level = pair.Value });
            data.upgrades.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            foreach (var pair in state.BossClears)
                data.bosses.Add(new BossClearData { id = pair.Key, clears = pair.Value });
            data.bosses.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

            FurnitureState furniture = state.Furniture;
            data.furniture = new FurnitureSaveData { initialized = furniture.Initialized, nextUid = furniture.NextUid, tierAnnounced = furniture.AnnouncedTier };
            data.furniture.finishes.AddRange(furniture.OwnedFinishes);
            data.furniture.finishes.Sort(string.CompareOrdinal);
            data.furniture.newPieces.AddRange(furniture.NewIds);
            data.furniture.newPieces.Sort(string.CompareOrdinal);
            data.furniture.homecoming = furniture.PendingHomecoming ?? string.Empty;
            foreach (var pair in furniture.Owned)
                data.furniture.owned.Add(new OwnedPieceData { id = pair.Key, count = pair.Value });
            data.furniture.owned.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            var areas = new List<string>(furniture.AreaIds);
            areas.Sort(string.CompareOrdinal);
            foreach (string area in areas)
            {
                var saved = new AreaSaveData { id = area, floor = furniture.Finish(area, FinishKind.Floor), wall = furniture.Finish(area, FinishKind.Wall) };
                foreach (PlacedFurniture p in furniture.Layout(area))
                    saved.pieces.Add(new PieceData
                    {
                        uid = p.uid, def = p.definition, x = p.cell.x, y = p.cell.y, turns = p.turns, flip = p.flipped,
                        nx = p.nudge.x, ny = p.nudge.y, host = p.host, anchor = p.anchor, variant = p.variant ?? string.Empty,
                        palette = p.palette ?? string.Empty,
                    });
                data.furniture.areas.Add(saved);
            }
            return data;
        }

        /// <param name="ingredientById">Resolves an ingredient id (null if unknown).</param>
        /// <param name="upgradeExists">Whether an upgrade id is still in the game.</param>
        /// <param name="warnings">Receives a line per dropped entry.</param>
        /// <param name="furnitureExists">Whether a furniture id is still in the game (null: every id is kept).</param>
        /// <param name="startingFurniture">Granted when the save has no furniture yet (migrated from version 3).</param>
        public static GameState Restore(SaveData data, Func<string, IngredientDefinition> ingredientById,
            Func<string, bool> upgradeExists, List<string> warnings = null, Func<string, bool> furnitureExists = null,
            FurnitureStartingLayout startingFurniture = null, IEnumerable<BossTrophy> trophies = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            // Saves from before the v0.5 day order name daytime "Morning".
            DayPhase phase = DayCycle.Parse(data.phase);
            var state = new GameState(Math.Max(1, data.day), phase)
            {
                Gold = Math.Max(0, data.gold),
                Renown = data.renown,
            };

            var stacks = new List<IngredientStack>();
            foreach (var s in data.storeroom)
            {
                var def = s != null ? ingredientById(s.ingredient) : null;
                if (def == null)
                {
                    warnings?.Add($"Dropped unknown ingredient '{s?.ingredient}'.");
                    continue;
                }
                var quality = Enum.TryParse(s.quality, out Quality q) ? q : Quality.Standard;
                var prep = Enum.TryParse(s.prep, out PrepState ps) ? ps : PrepState.Raw;
                stacks.Add(new IngredientStack(new IngredientItem(def, quality, prep), s.count, s.freshness));
            }
            state.Storeroom.AddRange(stacks);

            foreach (var u in data.upgrades)
            {
                if (u == null || !upgradeExists(u.id))
                {
                    warnings?.Add($"Dropped unknown upgrade '{u?.id}'.");
                    continue;
                }
                state.SetUpgradeLevel(u.id, u.level);
            }

            // Kept even for a boss no longer in the game: the record is story state (4e).
            if (data.bosses != null)
                foreach (var b in data.bosses)
                    if (b != null && !string.IsNullOrEmpty(b.id) && b.clears > 0) state.SetBossClears(b.id, b.clears);

            if (data.meal != null && Enum.TryParse(data.meal.kind, out MealBuffKind kind))
                state.Meal = new MealBuff(kind, data.meal.amount, data.meal.recipe);

            RestoreFurniture(state.Furniture, data.furniture, furnitureExists ?? (_ => true), startingFurniture, warnings);
            // A boss beaten before its trophy existed (a 4e save) earns it now, once: the same rule as a fresh victory.
            TrophyRules.GrantEarned(state, trophies);
            return state;
        }

        static void RestoreFurniture(FurnitureState furniture, FurnitureSaveData data, Func<string, bool> exists,
            FurnitureStartingLayout startingFurniture, List<string> warnings)
        {
            if (data == null || !data.initialized)
            {
                furniture.GrantStarter(startingFurniture);
                return;
            }
            var finishAreas = new List<(string, string, string)>();
            var owned = new List<(string, int)>();
            foreach (OwnedPieceData o in data.owned)
            {
                if (o == null || !exists(o.id))
                {
                    warnings?.Add($"Dropped unknown furniture '{o?.id}'.");
                    continue;
                }
                owned.Add((o.id, o.count));
            }
            var areas = new List<(string, List<PlacedFurniture>)>();
            foreach (AreaSaveData a in data.areas)
            {
                if (a == null || string.IsNullOrEmpty(a.id)) continue;
                var pieces = new List<PlacedFurniture>();
                foreach (PieceData p in a.pieces)
                {
                    if (p == null || !exists(p.def))
                    {
                        warnings?.Add($"Dropped unknown placed furniture '{p?.def}' in {a.id}.");
                        continue;
                    }
                    pieces.Add(new PlacedFurniture
                    {
                        uid = p.uid, definition = p.def, cell = new Vector2Int(p.x, p.y), turns = p.turns, flipped = p.flip,
                        nudge = new Vector2Int(p.nx, p.ny), host = p.host, anchor = p.anchor, variant = p.variant ?? string.Empty,
                        palette = p.palette ?? string.Empty,
                    });
                }
                areas.Add((a.id, pieces));
                finishAreas.Add((a.id, a.floor, a.wall));
            }
            furniture.Restore(owned, areas, data.nextUid);
            furniture.RestoreFinishes(data.finishes ?? new List<string>(), finishAreas, data.tierAnnounced);
            var fresh = new List<string>();
            foreach (string id in data.newPieces ?? new List<string>())
                if (!string.IsNullOrEmpty(id) && exists(id)) fresh.Add(id);
            furniture.RestoreNew(fresh, !string.IsNullOrEmpty(data.homecoming) && exists(data.homecoming) ? data.homecoming : null);
            if (data.grantNewStarters) furniture.GrantNewStarters(startingFurniture, StartersAddedInV7);
            // Areas and starter finishes added since the save was made (the guest room) arrive with their starting furniture.
            furniture.GrantMissing(startingFurniture);
        }

        public static GameState Restore(SaveData data, GameDatabase database, List<string> warnings = null) =>
            Restore(data, database.Ingredient, id => database.Upgrade(id) != null, warnings, id => database.Furniture(id) != null,
                database.startingFurniture, database.bossTrophies);

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        /// <summary>Reads any known version, migrating older ones to the current format.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("The save file is empty.");
            int version = JsonUtility.FromJson<VersionProbe>(json).version;
            if (version > CurrentVersion) throw new NotSupportedException($"The save is from a newer version ({version}) of the game.");
            if (version < 1) throw new FormatException($"Unknown save version {version}.");
            // One step per version, so an old save goes through every change since.
            SaveData data = version == 1 ? MigrateV1(JsonUtility.FromJson<SaveDataV1>(json)) : JsonUtility.FromJson<SaveData>(json);
            if (data.version == 2) data = MigrateV2(data);
            if (data.version == 3) data = MigrateV3(data);
            if (data.version == 4) data = MigrateV4(data);
            if (data.version == 5) data = MigrateV5(data);
            if (data.version == 6) data = MigrateV6(data);
            return data;
        }

        /// <summary>
        /// v4 → v5 (after the 4f Checkpoint A playtest): pieces keep exactly where they stood. Barrels are now drawn a quarter
        /// tile west of their cell's middle, so each saved barrel gains the 2 pixels back in its nudge; and the row of glasses
        /// is a surface item, so a saved row standing on a low shelf goes onto that shelf's surface (otherwise to storage).
        /// </summary>
        /// <summary>
        /// v5 → v6 (4f Checkpoint B): new fields only, all empty in a version 5 save: pieces as drawn, areas with their
        /// starting finishes and areas new since (the guest room) granted on restore (<see cref="FurnitureState.GrantMissing"/>),
        /// and no tier announced yet, so tiers already reached are announced on the next Night.
        /// </summary>
        /// <summary>
        /// v6 → v7 (4f Checkpoint C): new fields only. Nothing is new in storage and no trophy waits; a boss already beaten
        /// earns its trophy as the save is restored (<see cref="TrophyRules.GrantEarned"/>).
        /// </summary>
        static SaveData MigrateV6(SaveData v6)
        {
            v6.version = 7;
            v6.furniture ??= new FurnitureSaveData();
            v6.furniture.newPieces ??= new List<string>();
            v6.furniture.homecoming = null;
            // Pieces in the starting layout since (the Butcher Block, 4f Checkpoint C) arrive in storage, once.
            v6.furniture.grantNewStarters = v6.furniture.initialized;
            return v6;
        }

        static SaveData MigrateV5(SaveData v5)
        {
            v5.version = 6;
            v5.furniture ??= new FurnitureSaveData();
            v5.furniture.finishes ??= new List<string>();
            v5.furniture.tierAnnounced = 0;
            return v5;
        }

        static SaveData MigrateV4(SaveData v4)
        {
            v4.version = 5;
            if (v4.furniture?.areas == null) return v4;
            foreach (AreaSaveData area in v4.furniture.areas)
            {
                if (area?.pieces == null) continue;
                foreach (PieceData p in area.pieces)
                    if (p != null && p.def == "cellar_barrel") p.nx = Math.Min(p.nx + 2, FurnitureGeometry.NudgeMax);
                for (int i = area.pieces.Count - 1; i >= 0; i--)
                {
                    PieceData glasses = area.pieces[i];
                    if (glasses == null || glasses.def != "shelf_glasses" || glasses.host >= 0) continue;
                    PieceData shelf = area.pieces.Find(p => p != null && p.def == "low_shelf" && p.x == glasses.x && p.y == glasses.y - 1 && p.nx == glasses.nx && p.ny == glasses.ny);
                    if (shelf != null)
                    {
                        glasses.host = shelf.uid;
                        glasses.anchor = 0;
                    }
                    else area.pieces.RemoveAt(i);
                }
            }
            return v4;
        }

        /// <summary>
        /// v3 → v4 (4f): furniture arrives. The save holds none yet (<see cref="FurnitureSaveData.initialized"/> false), so
        /// the starting furniture is granted as it loads. The retired seat upgrade (D16) is refunded: the Gold its levels
        /// cost goes back to the purse and the upgrade is dropped.
        /// </summary>
        static SaveData MigrateV3(SaveData v3)
        {
            v3.version = 4;
            v3.furniture = new FurnitureSaveData { initialized = false };
            v3.upgrades ??= new List<UpgradeData>();
            for (int i = v3.upgrades.Count - 1; i >= 0; i--)
            {
                UpgradeData u = v3.upgrades[i];
                if (u == null || u.id != RetiredSeatUpgrade) continue;
                v3.gold += SeatUpgradeRefund(u.level);
                v3.upgrades.RemoveAt(i);
            }
            return v3;
        }

        /// <summary>v2 → v3: the same, with no bosses defeated yet (4e).</summary>
        static SaveData MigrateV2(SaveData v2)
        {
            v2.version = 3;
            v2.bosses ??= new List<BossClearData>();
            return v2;
        }

        /// <summary>v1 → v2: parts gain prep state (Raw) and full freshness; renown, upgrades and a pending meal start empty; resume in the daytime.</summary>
        static SaveData MigrateV1(SaveDataV1 v1)
        {
            var data = new SaveData
            {
                version = 2,
                day = v1.day,
                phase = DayPhase.Daytime.ToString(),
                gold = v1.gold,
                renown = 0,
                meal = new MealData { kind = MealBuffKind.None.ToString() },
            };
            foreach (var s in v1.storeroom)
            {
                if (s == null) continue;
                data.storeroom.Add(new StackData
                {
                    ingredient = s.ingredient,
                    quality = ((Quality)s.quality).ToString(),
                    prep = PrepState.Raw.ToString(),
                    count = s.count,
                    freshness = Freshness.Max,
                });
            }
            return data;
        }
    }

    /// <summary>
    /// The single save slot on disk. Writes go to a temp file first so a crash can't leave half a save. On the web, each
    /// write and delete is flushed to the browser's storage (<see cref="WebStorage"/>).
    /// </summary>
    public sealed class SaveStore
    {
        public const string FileName = "save_slot_1.json";

        public SaveStore(string directory)
        {
            Directory = directory;
            FilePath = Path.Combine(directory, FileName);
        }

        public string Directory { get; }
        public string FilePath { get; }
        public bool Exists => File.Exists(FilePath);

        public void Write(string json)
        {
            System.IO.Directory.CreateDirectory(Directory);
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(temp, FilePath);
            WebStorage.Flush();
        }

        public string Read() => File.ReadAllText(FilePath);

        public void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            WebStorage.Flush();
        }
    }
}
