using System;
using System.Collections.Generic;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// The property's furniture (in <c>GameState</c>, saved): owned copies of each piece (D7: copies, not unlimited),
    /// each area's layout, and so what's in storage (D12: property-wide, unlimited; removing never destroys). Pure data
    /// with its bookkeeping rules; placement rules are in <c>FurnitureLayout</c>.
    /// </summary>
    public sealed class FurnitureState
    {
        readonly Dictionary<string, int> m_Owned = new();
        readonly Dictionary<string, List<PlacedFurniture>> m_Areas = new();
        readonly Dictionary<string, string> m_Floors = new();
        readonly Dictionary<string, string> m_Walls = new();
        readonly HashSet<string> m_OwnedFinishes = new();

        /// <summary>False until the starting furniture is granted (a new game, or an old save meeting furniture for the first time).</summary>
        public bool Initialized { get; private set; }
        /// <summary>The next unused piece uid.</summary>
        public int NextUid { get; private set; } = 1;

        public IReadOnlyDictionary<string, int> Owned => m_Owned;
        public IEnumerable<string> AreaIds => m_Areas.Keys;
        public IEnumerable<string> OwnedFinishes => m_OwnedFinishes;
        /// <summary>The highest catalogue tier the Night screen has announced (D14): each opening is announced once.</summary>
        public int AnnouncedTier { get; set; }

        public bool OwnsFinish(string id) => !string.IsNullOrEmpty(id) && m_OwnedFinishes.Contains(id);
        public void OwnFinish(string id)
        {
            if (!string.IsNullOrEmpty(id)) m_OwnedFinishes.Add(id);
        }

        /// <summary>An area's floor or wall finish (null: never set, so the room as built).</summary>
        public string Finish(string area, FinishKind kind) =>
            area != null && (kind == FinishKind.Floor ? m_Floors : m_Walls).TryGetValue(area, out string id) ? id : null;

        public void SetFinish(string area, FinishKind kind, string id)
        {
            if (string.IsNullOrEmpty(area)) return;
            Dictionary<string, string> finishes = kind == FinishKind.Floor ? m_Floors : m_Walls;
            if (string.IsNullOrEmpty(id)) finishes.Remove(area);
            else finishes[area] = id;
        }

        public int OwnedCount(string definition) => definition != null && m_Owned.TryGetValue(definition, out int n) ? n : 0;

        public int PlacedCount(string definition)
        {
            int n = 0;
            foreach (List<PlacedFurniture> pieces in m_Areas.Values)
                foreach (PlacedFurniture p in pieces)
                    if (p.definition == definition) n++;
            return n;
        }

        /// <summary>Owned but not placed anywhere.</summary>
        public int InStorage(string definition) => Math.Max(0, OwnedCount(definition) - PlacedCount(definition));

        public IReadOnlyList<PlacedFurniture> Layout(string area) =>
            area != null && m_Areas.TryGetValue(area, out List<PlacedFurniture> pieces) ? pieces : Array.Empty<PlacedFurniture>();

        /// <summary>Grants the starting furniture: each area's layout (owned as placed) and the extra copies in storage.</summary>
        public void GrantStarter(FurnitureStartingLayout start)
        {
            m_Owned.Clear();
            m_Areas.Clear();
            m_Floors.Clear();
            m_Walls.Clear();
            m_OwnedFinishes.Clear();
            AnnouncedTier = 0;
            NextUid = 1;
            if (start != null)
            {
                foreach (AreaLayoutData area in start.areas)
                {
                    if (area == null || string.IsNullOrEmpty(area.area)) continue;
                    var pieces = new List<PlacedFurniture>();
                    foreach (PlacedFurniture p in area.pieces)
                    {
                        if (p == null || string.IsNullOrEmpty(p.definition)) continue;
                        PlacedFurniture copy = p.Clone();
                        pieces.Add(copy);
                        AddOwned(copy.definition, 1);
                        NextUid = Math.Max(NextUid, copy.uid + 1);
                    }
                    m_Areas[area.area] = pieces;
                    SetFinish(area.area, FinishKind.Floor, area.floor);
                    SetFinish(area.area, FinishKind.Wall, area.wall);
                }
                foreach (OwnedFurnitureData extra in start.storage)
                    if (extra != null) AddOwned(extra.definition, extra.count);
                foreach (string finish in start.finishes) OwnFinish(finish);
            }
            Initialized = true;
        }

        /// <summary>
        /// Brings a save up to the property as it now is (any version): an area the save has never seen (the guest room,
        /// for a save from Checkpoint A) arrives with its starting furniture, owned and placed (uids renumbered to stay
        /// unique), and its starting finishes; the starter finishes are owned. Running it again changes nothing.
        /// </summary>
        public void GrantMissing(FurnitureStartingLayout start)
        {
            if (start == null) return;
            foreach (AreaLayoutData area in start.areas)
            {
                if (area == null || string.IsNullOrEmpty(area.area)) continue;
                if (!m_Areas.ContainsKey(area.area))
                {
                    var uids = new Dictionary<int, int>();
                    var pieces = new List<PlacedFurniture>();
                    foreach (PlacedFurniture p in area.pieces)
                    {
                        if (p == null || string.IsNullOrEmpty(p.definition)) continue;
                        PlacedFurniture copy = p.Clone();
                        uids[p.uid] = copy.uid = TakeUid();
                        pieces.Add(copy);
                        AddOwned(copy.definition, 1);
                    }
                    foreach (PlacedFurniture p in pieces)
                        if (p.host >= 0) p.host = uids.TryGetValue(p.host, out int h) ? h : -1;
                    m_Areas[area.area] = pieces;
                }
                if (Finish(area.area, FinishKind.Floor) == null) SetFinish(area.area, FinishKind.Floor, area.floor);
                if (Finish(area.area, FinishKind.Wall) == null) SetFinish(area.area, FinishKind.Wall, area.wall);
            }
            foreach (string finish in start.finishes) OwnFinish(finish);
        }

        /// <summary>Restores the finishes and the announced tier (SaveSystem; after <see cref="Restore"/>).</summary>
        public void RestoreFinishes(IEnumerable<string> owned, IEnumerable<(string area, string floor, string wall)> areas, int announcedTier)
        {
            m_OwnedFinishes.Clear();
            m_Floors.Clear();
            m_Walls.Clear();
            foreach (string id in owned) OwnFinish(id);
            foreach (var (area, floor, wall) in areas)
            {
                SetFinish(area, FinishKind.Floor, floor);
                SetFinish(area, FinishKind.Wall, wall);
            }
            AnnouncedTier = Math.Max(0, announcedTier);
        }

        /// <summary>Restores saved state (SaveSystem). Pieces whose definitions are owned fewer times than placed gain the missing copies.</summary>
        public void Restore(IEnumerable<(string definition, int count)> owned, IEnumerable<(string area, List<PlacedFurniture> pieces)> areas, int nextUid)
        {
            m_Owned.Clear();
            m_Areas.Clear();
            foreach (var (definition, count) in owned) AddOwned(definition, count);
            NextUid = Math.Max(1, nextUid);
            foreach (var (area, pieces) in areas)
            {
                if (string.IsNullOrEmpty(area)) continue;
                m_Areas[area] = pieces;
                foreach (PlacedFurniture p in pieces) NextUid = Math.Max(NextUid, p.uid + 1);
            }
            foreach (string id in new List<string>(PlacedIds()))
                if (PlacedCount(id) > OwnedCount(id)) m_Owned[id] = PlacedCount(id);
            Initialized = true;
        }

        IEnumerable<string> PlacedIds()
        {
            var seen = new HashSet<string>();
            foreach (List<PlacedFurniture> pieces in m_Areas.Values)
                foreach (PlacedFurniture p in pieces)
                    if (seen.Add(p.definition)) yield return p.definition;
        }

        /// <summary>Replaces an area's layout (Decorate Mode commits here). Every piece must be owned.</summary>
        public void SetLayout(string area, IEnumerable<PlacedFurniture> pieces)
        {
            if (string.IsNullOrEmpty(area)) throw new ArgumentException("An area needs an id.", nameof(area));
            var list = new List<PlacedFurniture>(pieces);
            m_Areas[area] = list;
            foreach (PlacedFurniture p in list) NextUid = Math.Max(NextUid, p.uid + 1);
        }

        public int TakeUid() => NextUid++;

        /// <summary>Gives (or, negative, takes away) owned copies outside the shop: the developer sandbox, later discoveries.</summary>
        public void AddOwnedCopies(string definition, int count) => AddOwned(definition, count);

        internal void AddOwned(string definition, int count)
        {
            if (string.IsNullOrEmpty(definition) || count == 0) return;
            int n = Math.Max(0, OwnedCount(definition) + count);
            if (n == 0) m_Owned.Remove(definition);
            else m_Owned[definition] = n;
        }
    }
}
