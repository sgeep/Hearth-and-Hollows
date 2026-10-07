using System;
using System.Collections.Generic;
using System.Globalization;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Story;
using PixelCrushers;
using PixelCrushers.LoveHate;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Story.Relationships
{
    /// <summary>What one character made of one deed (debug and tests).</summary>
    public readonly struct DeedReaction
    {
        public readonly string Judge;
        public readonly string Deed;
        public readonly float Affinity;
        public readonly float Respect;
        public readonly bool Remembered;

        public DeedReaction(string judge, string deed, float affinity, float respect, bool remembered)
        {
            Judge = judge;
            Deed = deed;
            Affinity = affinity;
            Respect = respect;
            Remembered = remembered;
        }
    }

    /// <summary>
    /// Hearth &amp; Hollows' only door to Love/Hate (4g). It keeps one <b>social stand-in</b> per tracked character: a Love/Hate
    /// faction member on the persistent story host, keyed by the character's stable id and independent of any visible NPC, so a
    /// deed done in Decorate Mode or in the Hollows reaches Boog whether or not he's in the loaded scene. It commits deeds to the
    /// characters <see cref="RelationshipRules.Learners"/> names (no sight or witnesses), lets Love/Hate evaluate Affinity and keep
    /// the memory, and adds Respect through Love/Hate's own evaluation hook. Memories age by the game's day, never real time.
    /// </summary>
    public sealed class RelationshipAdapter
    {
        readonly FactionManager m_Manager;
        readonly Transform m_Root;
        readonly CharacterDirectory m_Cast;
        readonly Dictionary<string, DeedDefinition> m_Deeds = new();
        readonly Dictionary<string, FactionMember> m_Members = new();
        FactionMember m_Player;
        int m_Day = 1;

        public RelationshipAdapter(FactionManager manager, Transform root, CharacterDirectory cast, IEnumerable<DeedDefinition> deeds)
        {
            m_Manager = manager;
            m_Root = root;
            m_Cast = cast;
            if (deeds != null)
                foreach (DeedDefinition d in deeds)
                    if (d != null && !string.IsNullOrEmpty(d.id)) m_Deeds[d.id] = d;
        }

        /// <summary>Raised for each character who evaluates a deed.</summary>
        public event Action<DeedReaction> Reacted;

        public int Day => m_Day;
        public IEnumerable<string> StandIns => m_Members.Keys;
        public FactionDatabase Database => m_Manager != null ? m_Manager.factionDatabase : null;

        /// <summary>Creates the stand-ins: the player (the actor of every deed) and each tracked character.</summary>
        public void Initialize()
        {
            m_Player = StandIn(CharacterIds.Player, null);
            foreach (CharacterDefinition c in m_Cast.Tracked())
                if (c.kind != CharacterKind.Player && !m_Members.ContainsKey(c.id))
                    m_Members[c.id] = StandIn(c.id, c);
            SetDay(m_Day);
        }

        FactionMember StandIn(string id, CharacterDefinition character)
        {
            var go = new GameObject($"Social: {id}");
            go.transform.SetParent(m_Root, false);
            var member = go.AddComponent<PixelCrushers.LoveHate.Wrappers.FactionMember>();
            member.factionManager = m_Manager;
            member.factionDatabase = m_Manager.factionDatabase;
            member.factionID = m_Manager.factionDatabase.GetFactionID(id);
            if (member.factionID < 0) Debug.LogWarning($"[Hearthdelve] No Love/Hate faction for '{id}'.");
            // Only what 4g uses: affinity, respect and memory. No emotions, impressions or rumors.
            member.impressionability = 0f;
            member.arousalImportance = 0f;
            member.deedImpactThreshold = 0f;
            member.maxMemories = 50;
            // Repeats fade fast (4g Checkpoint C): Love/Hate's acclimatization, shaped by the game's own rule.
            member.acclimatizationCurve = RepeatCurve();
            if (character != null) member.EvaluateRumor = (rumor, source) => Evaluate(member, character, rumor, source);
            return member;
        }

        /// <summary><see cref="RelationshipRules.RepeatFactor"/> as Love/Hate's curve (by how many times the deed is remembered).</summary>
        public static AnimationCurve RepeatCurve()
        {
            var keys = new Keyframe[5];
            for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(i, RelationshipRules.RepeatFactor(i));
            var curve = new AnimationCurve(keys) { preWrapMode = WrapMode.Clamp, postWrapMode = WrapMode.Clamp };
            for (int i = 0; i < keys.Length; i++)
            {
                LinearKey(curve, i);
            }
            return curve;
        }

        /// <summary>Straight lines between the keys (no overshoot between whole counts, which are the only ones evaluated anyway).</summary>
        static void LinearKey(AnimationCurve curve, int i)
        {
            Keyframe k = curve[i];
            float inTangent = i > 0 ? (curve[i].value - curve[i - 1].value) / (curve[i].time - curve[i - 1].time) : 0f;
            float outTangent = i < curve.length - 1 ? (curve[i + 1].value - curve[i].value) / (curve[i + 1].time - curve[i].time) : 0f;
            k.inTangent = inTangent;
            k.outTangent = outTangent;
            curve.MoveKey(i, k);
        }

        /// <summary>
        /// Love/Hate's own evaluation (Affinity, repetition, memory), plus Respect: the deed's respect weighted by how well the
        /// judge's values match what it shows and by the same acclimatization Love/Hate applied (plan §7, the smallest extension).
        /// The memory then lasts the deed's days on the game's day clock.
        /// </summary>
        Rumor Evaluate(FactionMember member, CharacterDefinition judge, Rumor rumor, FactionMember source)
        {
            float affinityBefore = member.GetAffinity(rumor.actorFactionID);
            // A repeat is one they already remember (Love/Hate's own test). Its pooled rumors keep a stale count, so a first
            // sighting's count is set here rather than trusted.
            Rumor remembered = member.longTermMemory.Find(r => r.actorFactionID == rumor.actorFactionID && r.targetFactionID == rumor.targetFactionID && r.tag == rumor.tag);
            // Read before Love/Hate's evaluation, which counts this sighting into the memory it updates (4g Checkpoint C).
            int seenBefore = remembered != null ? remembered.count : 0;
            Rumor result = member.DefaultEvaluateRumor(rumor, source);
            if (result == null || !m_Deeds.TryGetValue(rumor.tag, out DeedDefinition deed)) return result;
            if (remembered == null) result.count = 1;
            float acclimatization = remembered == null ? 1f : member.acclimatizationCurve.Evaluate(seenBefore);
            float respect = RelationshipRules.RespectChange(deed.respect, RelationshipRules.Alignment(judge.values, deed.shows), acclimatization);
            if (!Mathf.Approximately(respect, 0f))
                Database.ModifyPersonalRelationshipTrait(member.factionID, rumor.actorFactionID, RespectTrait, respect);
            if (result.memorable)
            {
                result.longTermExpiration = RelationshipRules.MemoryExpires(m_Day, deed.memoryDays);
                result.shortTermExpiration = Mathf.Min(result.longTermExpiration, m_Day + 1.5f);
            }
            Reacted?.Invoke(new DeedReaction(judge.id, deed.id, member.GetAffinity(rumor.actorFactionID) - affinityBefore, respect, result.memorable));
            return result;
        }

        int RespectTrait => Database.GetRelationshipTraitID(StoryFactions.Respect);

        /// <summary>
        /// The game day memories are measured against. Pixel Crushers' shared clock runs in manual mode on it: pausing, menus or an
        /// idle window never age a memory. (The Dialogue System keeps its own real-time clock for text and sequences.)
        /// </summary>
        public void SetDay(int day)
        {
            m_Day = Math.Max(1, day);
            GameTime.mode = GameTimeMode.Manual;
            GameTime.time = m_Day;
        }

        // ---------- Deeds ----------

        public DeedDefinition Deed(string id) => id != null && m_Deeds.TryGetValue(id, out DeedDefinition d) ? d : null;

        /// <summary>Makes a deed known (a quest's or a conversation's own deed, added at runtime; tests).</summary>
        public void Add(DeedDefinition deed)
        {
            if (deed != null && !string.IsNullOrEmpty(deed.id)) m_Deeds[deed.id] = deed;
        }

        /// <summary>The player did <paramref name="deed"/>: each of <paramref name="learners"/> evaluates it now, wherever they are.</summary>
        public void Commit(DeedDefinition deed, IEnumerable<string> learners)
        {
            if (deed == null || Database == null || m_Player == null) return;
            int target = Database.GetFactionID(RelationshipRules.TargetFaction(deed));
            Deed committed = PixelCrushers.LoveHate.Deed.GetNew(deed.id, m_Player.factionID, target, deed.impact, 0f, 0f, deed.shows.ToArray());
            try
            {
                foreach (string id in learners)
                    if (id != null && m_Members.TryGetValue(id, out FactionMember member)) member.WitnessDeed(committed, m_Player, false);
            }
            finally
            {
                PixelCrushers.LoveHate.Deed.Release(committed);
            }
        }

        // ---------- Queries (dialogue asks these through HH_ functions) ----------

        public float Affinity(string characterId) => Trait(characterId, CharacterIds.Player, StoryFactions.Affinity);
        public float Respect(string characterId) => Trait(characterId, CharacterIds.Player, StoryFactions.Respect);

        public float Trait(string judge, string subject, string trait)
        {
            FactionDatabase db = Database;
            if (db == null) return 0f;
            int j = db.GetFactionID(judge), s = db.GetFactionID(subject), t = db.GetRelationshipTraitID(trait);
            return j < 0 || s < 0 || t < 0 ? 0f : db.GetRelationshipTrait(j, s, t);
        }

        /// <summary>Whether <paramref name="characterId"/> remembers the player doing <paramref name="deedId"/> (forgotten after its days).</summary>
        public bool Remembers(string characterId, string deedId)
        {
            if (characterId == null || !m_Members.TryGetValue(characterId, out FactionMember member) || m_Player == null) return false;
            foreach (Rumor r in member.longTermMemory)
                if (r.tag == deedId && r.actorFactionID == m_Player.factionID && !RelationshipRules.IsForgotten(r.longTermExpiration, m_Day)) return true;
            return false;
        }

        /// <summary>How many times they've seen the player do it (Love/Hate's repeat count; 0: not remembered).</summary>
        public int TimesSeen(string characterId, string deedId)
        {
            if (characterId == null || !m_Members.TryGetValue(characterId, out FactionMember member) || m_Player == null) return 0;
            foreach (Rumor r in member.longTermMemory)
                if (r.tag == deedId && r.actorFactionID == m_Player.factionID) return r.count;
            return 0;
        }

        // ---------- Saving ----------

        /// <summary>
        /// The relationships in Hearth &amp; Hollows' own shape: every value that moved from where its character starts, and every
        /// memory, by stable ids and trait names. (Love/Hate's own string is positional and locale-formatted: not a save format.)
        /// </summary>
        public RelationshipData Record()
        {
            var data = new RelationshipData();
            FactionDatabase db = Database;
            if (db == null) return data;
            foreach (CharacterDefinition c in m_Cast.Tracked())
            foreach ((string subject, string trait, float start) in Starts(c))
            {
                float value = Trait(c.id, subject, trait);
                if (Mathf.Abs(value - start) > 0.0001f) data.values.Add(new RelationshipValueData { judge = c.id, subject = subject, trait = trait, value = value });
            }
            foreach (KeyValuePair<string, FactionMember> pair in m_Members)
            foreach (Rumor r in pair.Value.longTermMemory)
            {
                Faction actor = db.GetFaction(r.actorFactionID), target = db.GetFaction(r.targetFactionID);
                if (actor == null || target == null) continue;
                data.memories.Add(new SocialMemoryData
                {
                    judge = pair.Key, deed = r.tag, actor = actor.name, target = target.name, count = r.count, impact = r.impact,
                    pleasure = r.pleasure, expires = r.longTermExpiration,
                });
            }
            return data;
        }

        /// <summary>Where a character's relationships start (their definition), for comparing and for a fresh game.</summary>
        static IEnumerable<(string subject, string trait, float start)> Starts(CharacterDefinition c)
        {
            yield return (CharacterIds.Player, StoryFactions.Affinity, c.affinityToPlayer);
            yield return (CharacterIds.Player, StoryFactions.Respect, c.respectForPlayer);
            yield return (StoryFactions.Tavern, StoryFactions.Affinity, c.affinityToTavern);
            yield return (StoryFactions.Village, StoryFactions.Affinity, c.affinityToVillage);
        }

        /// <summary>Puts saved relationships back over a fresh start. Unknown characters, traits or deeds are dropped with a warning.</summary>
        public void Apply(RelationshipData data, List<string> warnings = null)
        {
            Clear();
            FactionDatabase db = Database;
            if (data == null || db == null) return;
            foreach (RelationshipValueData v in data.values ?? new List<RelationshipValueData>())
            {
                int j = db.GetFactionID(v?.judge), s = db.GetFactionID(v?.subject), t = db.GetRelationshipTraitID(v?.trait);
                if (v == null || j < 0 || s < 0 || t < 0)
                {
                    warnings?.Add($"Dropped relationship {v?.judge}→{v?.subject} {v?.trait}.");
                    continue;
                }
                db.SetPersonalRelationshipTrait(j, s, t, v.value);
            }
            foreach (SocialMemoryData m in data.memories ?? new List<SocialMemoryData>())
            {
                int actor = db.GetFactionID(m?.actor), target = db.GetFactionID(m?.target);
                if (m == null || !m_Members.TryGetValue(m.judge ?? string.Empty, out FactionMember member) || actor < 0 || target < 0 || string.IsNullOrEmpty(m.deed))
                {
                    warnings?.Add($"Dropped a memory of {m?.deed} held by {m?.judge}.");
                    continue;
                }
                Rumor rumor = Rumor.GetNew();
                rumor.deedGuid = Guid.NewGuid();
                rumor.tag = m.deed;
                rumor.actorFactionID = actor;
                rumor.targetFactionID = target;
                rumor.impact = m.impact;
                rumor.traits = m_Deeds.TryGetValue(m.deed, out DeedDefinition deed) ? deed.shows.ToArray() : new float[SocialTraits.Count];
                rumor.count = Math.Max(1, m.count);
                rumor.confidence = 100f;
                rumor.pleasure = m.pleasure;
                rumor.memorable = true;
                rumor.longTermExpiration = m.expires;
                rumor.shortTermExpiration = 0f;
                member.longTermMemory.Add(rumor);
            }
        }

        /// <summary>Everyone back where they start, with nothing remembered (a new game, the menu, or before a load).</summary>
        public void Clear()
        {
            if (m_Manager == null) return;
            m_Manager.ResetAll();
            // ResetAll gives the manager a fresh copy of the database; the stand-ins follow it.
            foreach (FactionMember member in AllMembers())
            {
                member.factionDatabase = m_Manager.factionDatabase;
                member.SwitchFaction(member.factionID);
                foreach (Rumor r in member.longTermMemory) Rumor.Release(r);
                member.longTermMemory.Clear();
                member.shortTermMemory.Clear();
            }
            ApplyStarts();
        }

        /// <summary>Each character's starting relationships from their definition (the database asset is built from the same values).</summary>
        void ApplyStarts()
        {
            FactionDatabase db = Database;
            foreach (CharacterDefinition c in m_Cast.Tracked())
            foreach ((string subject, string trait, float start) in Starts(c))
            {
                int j = db.GetFactionID(c.id), s = db.GetFactionID(subject), t = db.GetRelationshipTraitID(trait);
                if (j >= 0 && s >= 0 && t >= 0) db.SetPersonalRelationshipTrait(j, s, t, start);
            }
        }

        IEnumerable<FactionMember> AllMembers()
        {
            if (m_Player != null) yield return m_Player;
            foreach (FactionMember m in m_Members.Values) yield return m;
        }

        /// <summary>A readable dump for the debug log (invariant culture).</summary>
        public string Describe(string characterId) => string.Format(CultureInfo.InvariantCulture, "{0}: affinity {1:0.#}, respect {2:0.#}",
            characterId, Affinity(characterId), Respect(characterId));

        public void Destroy()
        {
            foreach (FactionMember m in AllMembers())
                if (m != null) Object.Destroy(m.gameObject);
            m_Members.Clear();
            m_Player = null;
        }
    }
}
