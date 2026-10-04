using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// Everything a generated delve is made from (4d): the run's tuning, the authored rooms it chooses among, and the
    /// enemies it places. Tuning is kept when the rooms are rebuilt.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Dungeon/Run Settings", fileName = "RunSettings")]
    public sealed class RunSettings : ScriptableObject
    {
        public RunTuning tuning = new();
        [Tooltip("The rooms a run can use (rebuilt by Hearthdelve → Generate → 4d Dungeon).")]
        public RoomDefinition[] rooms = Array.Empty<RoomDefinition>();
        public GameObject slime;
        public GameObject bat;
        public GameObject spider;
        [Tooltip("A room's Gold reward, on the floor once it's clear.")]
        public GoldPickup goldPickup;
        [Tooltip("A power room's reward: touched, it offers three run powers.")]
        public Hearthdelve.Dungeon.Powers.PowerPickup powerPickup;

        /// <summary>A reward ingredient by id (from the tuning's options).</summary>
        public Hearthdelve.Shared.Ingredients.IngredientDefinition RewardIngredient(string id) =>
            tuning.ingredientRewards?.FirstOrDefault(o => o != null && o.ingredient != null && o.ingredient.id == id)?.ingredient;

        public RoomDefinition Room(string id) => rooms.FirstOrDefault(r => r != null && r.id == id);

        public GameObject Prefab(EnemyKind kind) => kind switch
        {
            EnemyKind.Slime => slime,
            EnemyKind.Bat => bat,
            _ => spider,
        };

        /// <summary>What the generator needs to know about each room, read from its layout.</summary>
        public List<RoomCatalogEntry> Catalog()
        {
            var entries = new List<RoomCatalogEntry>();
            foreach (RoomDefinition room in rooms)
            {
                if (room == null) continue;
                RoomLayout layout = room.Parse();
                entries.Add(new RoomCatalogEntry(room.id, room.kind, layout.Exits.Count, layout.GroundSpawns.Count, layout.PerchSpawns.Count));
            }
            return entries;
        }
    }
}
