using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>
    /// Puts the village's people where their schedules say (4h Checkpoint C), in Kariaston and in Tally Ho! alike. Each character
    /// may have a copy in each scene that can host them (Maximo has one in Tally Ho! for lunch); the copy in the scene of their
    /// current anchor is the one shown, and only one is ever shown. On a load everyone is simply there. When the clock moves
    /// them, they walk if the keeper can see either end, and otherwise are just there; crossing between Kariaston and Tally Ho!,
    /// they walk out of one door and in at the other, the second copy waiting for the first to be gone. Nothing is saved:
    /// positions come from the day, the clock, the story and the world seed (<see cref="VillageLife"/>). Outside the free
    /// daytime, nobody is about.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class VillagePresence : MonoBehaviour
    {
        readonly Dictionary<string, ScheduleBlock> m_Last = new();
        readonly Dictionary<string, List<Villager>> m_Copies = new();
        int m_Day = -1;
        Transform m_Keeper;

        public static VillagePresence Instance { get; private set; }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        static bool Daytime => TavernDirector.Instance != null && TavernDirector.Instance.Phase == TavernPhase.Daytime && VillageLife.World() != null;

        void LateUpdate() => Refresh();

        /// <summary>Brings everyone up to date with the clock now (tests call it after moving the clock).</summary>
        public void Refresh()
        {
            Group();
            if (!Daytime)
            {
                foreach (List<Villager> copies in m_Copies.Values)
                foreach (Villager v in copies)
                    if (v.Shown) v.Hide();
                m_Last.Clear();
                return;
            }
            int day = GameFlow.Instance.State.Day;
            if (day != m_Day)
            {
                // A new day (or a load): everyone simply where they are now.
                m_Day = day;
                m_Last.Clear();
            }
            foreach (KeyValuePair<string, List<Villager>> pair in m_Copies) Resolve(pair.Key, pair.Value);
        }

        /// <summary>Forgets what everyone was doing: the next refresh places them all directly (a load, Continue).</summary>
        public void Forget()
        {
            m_Last.Clear();
            m_Day = -1;
        }

        void Group()
        {
            m_Copies.Clear();
            foreach (Villager v in Villager.All)
            {
                if (v == null || string.IsNullOrEmpty(v.CharacterId)) continue;
                if (!m_Copies.TryGetValue(v.CharacterId, out List<Villager> list)) m_Copies[v.CharacterId] = list = new List<Villager>();
                list.Add(v);
            }
        }

        void Resolve(string character, List<Villager> copies)
        {
            ScheduleBlock block = VillageLife.Now(character);
            ScheduleAnchor anchor = block != null ? ScheduleAnchor.Find(block.anchor) : null;
            bool first = !m_Last.TryGetValue(character, out ScheduleBlock last);
            m_Last[character] = block;
            Villager host = null;
            if (anchor != null)
                foreach (Villager v in copies)
                    if (v.gameObject.scene == anchor.gameObject.scene) host = v;

            // Every other copy goes: out of its door if the keeper is watching, otherwise at once.
            bool othersHere = false;
            foreach (Villager v in copies)
            {
                if (v == host || !v.Shown) continue;
                if (first || !Sees(v.Area, v.transform.position)) v.Hide();
                else if (v.At != null || !v.Walking) v.Leave(Door(v));   // (already on the way out: let them go)
                othersHere |= v.Shown;
            }
            if (host == null) return;
            string activity = block.activity;
            if (othersHere) return;   // wait for them to walk out

            if (!host.Shown)
            {
                Vector2? door = Door(host);
                bool seen = !first && door != null && (Sees(host.Area, door.Value) || Sees(host.Area, anchor.Spot));
                if (seen) host.Enter(door.Value, anchor, activity);
                else host.Place(anchor, activity);
                return;
            }
            if (host.At == anchor && host.Activity == activity) return;
            if (host.At == anchor)
            {
                host.Place(anchor, activity);
                return;
            }
            if (first || host.At == null || !(Sees(host.Area, host.transform.position) || Sees(host.Area, anchor.Spot))) host.Place(anchor, activity);
            else host.WalkTo(anchor, activity);
        }

        /// <summary>The keeper is in that area and close enough to see the spot.</summary>
        bool Sees(string area, Vector2 spot)
        {
            SurfaceArea current = SurfaceArea.Current;
            if (current == null || current.Id != area) return false;
            if (m_Keeper == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                m_Keeper = player != null ? player.transform : null;
            }
            return m_Keeper != null && Vector2.Distance(m_Keeper.position, spot) <= VillageLife.Settings.seenWithin;
        }

        /// <summary>Their scene's way in and out: its Tally Ho! front door, on its own side.</summary>
        static Vector2? Door(Villager v)
        {
            foreach (string id in new[] { SurfaceDoor.FrontOutside, SurfaceDoor.FrontInside })
            {
                SurfaceDoor door = SurfaceDoor.Find(id);
                if (door != null && door.gameObject.scene == v.gameObject.scene) return door.Arrival;
            }
            return null;
        }
    }
}
