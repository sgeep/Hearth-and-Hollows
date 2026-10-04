using System;
using System.Collections.Generic;
using System.IO;
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
        public const int CurrentVersion = 2;

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
            return data;
        }

        /// <param name="ingredientById">Resolves an ingredient id (null if unknown).</param>
        /// <param name="upgradeExists">Whether an upgrade id is still in the game.</param>
        /// <param name="warnings">Receives a line per dropped entry.</param>
        public static GameState Restore(SaveData data, Func<string, IngredientDefinition> ingredientById,
            Func<string, bool> upgradeExists, List<string> warnings = null)
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

            if (data.meal != null && Enum.TryParse(data.meal.kind, out MealBuffKind kind))
                state.Meal = new MealBuff(kind, data.meal.amount, data.meal.recipe);
            return state;
        }

        public static GameState Restore(SaveData data, GameDatabase database, List<string> warnings = null) =>
            Restore(data, database.Ingredient, id => database.Upgrade(id) != null, warnings);

        public static string ToJson(SaveData data) => JsonUtility.ToJson(data, prettyPrint: true);

        /// <summary>Reads any known version, migrating older ones to the current format.</summary>
        public static SaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("The save file is empty.");
            int version = JsonUtility.FromJson<VersionProbe>(json).version;
            switch (version)
            {
                case CurrentVersion:
                    return JsonUtility.FromJson<SaveData>(json);
                case 1:
                    return MigrateV1(JsonUtility.FromJson<SaveDataV1>(json));
                default:
                    if (version > CurrentVersion) throw new NotSupportedException($"The save is from a newer version ({version}) of the game.");
                    throw new FormatException($"Unknown save version {version}.");
            }
        }

        /// <summary>v1 → v2: parts gain prep state (Raw) and full freshness; renown, upgrades and a pending meal start empty; resume in the daytime.</summary>
        static SaveData MigrateV1(SaveDataV1 v1)
        {
            var data = new SaveData
            {
                version = CurrentVersion,
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
