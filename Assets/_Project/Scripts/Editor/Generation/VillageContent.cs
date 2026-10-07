using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Movement;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Village;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Kariaston's people (4h Checkpoint C): their figures' animation sets, their schedules (a few broad beats each, made once
    /// and then tuned on the assets: a rerun never undoes a tuning), the village's tuning, the named places schedules send them
    /// to, and the villager objects the Kariaston and Tally Ho! updaters build.
    /// </summary>
    public static class VillageContent
    {
        public const string Folder = EditorPaths.Data + "/Village";
        const string k_Sets = EditorPaths.Animations + "/Village";
        const string k_DatabasePath = EditorPaths.Data + "/GameDatabase.asset";
        const string k_UnlitSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        // ---------- the day's broad beats (4-minute day: about 77 real seconds each) ----------

        public const int Morning = 8 * 60, Midday = 11 * 60, Afternoon = 14 * 60, Evening = 17 * 60, Night = 24 * 60;
        /// <summary>Kaloren's herb visit, mid-morning on a herb day.</summary>
        public const int HerbsFrom = 9 * 60 + 30;

        // ---------- anchors (Kariaston's in village tiles; Tally Ho!'s in the tavern's world) ----------

        public const string MemorialSquare = "square.memorial", MaximoPorch = "maximo.porch", TallyWatch = "tally.watch", TavernTable = "tavern.table";
        public const string KalorenTower = "kaloren.tower", SquareBench = "square.bench", CottageDoor = "cottage.door";
        public const string GrimYard = "grim.yard", MarketSide = "market.side", PondEast = "pond.east";
        public const string OgrinYard = "ogrin.yard", OgrinWindow = "ogrin.window", PondWest = "pond.west", GreenListen = "green.listen";
        public const string BartWagon = "bart.wagon", MarketFront = "market.front", Green = "green", MarketCart = "market.cart";
        public const string TavernKitchen = "tavern.kitchen", TavernBar = "tavern.bar";

        /// <summary>Kariaston's anchors: id, where they stand (village tiles), which way they face, behind a window.</summary>
        public static readonly (string id, Vector2 at, Facing4 facing, bool window)[] KariastonAnchors =
        {
            (MemorialSquare, new Vector2(34.4f, 17.7f), Facing4.BackRight, false),      // before Karias's memorial, south of it
            (MaximoPorch, new Vector2(15.4f, 11.2f), Facing4.FrontLeft, false),         // by his own porch
            (TallyWatch, new Vector2(38.8f, 25.6f), Facing4.BackLeft, false),           // looking up at Tally Ho!, never closer
            (KalorenTower, new Vector2(63.9f, 11.0f), Facing4.FrontLeft, false),        // at his tower's door
            (SquareBench, new Vector2(40.5f, 20.4f), Facing4.FrontRight, false),        // by the square's east bench
            (CottageDoor, new Vector2(52.3f, 11.5f), Facing4.BackLeft, false),          // at Grim and Ogrin's door, with the herbs
            (GrimYard, new Vector2(50.0f, 11.5f), Facing4.FrontRight, false),           // in front of his cottage
            (MarketSide, new Vector2(42.9f, 10.6f), Facing4.BackLeft, false),           // below the market cart's right corner
            (PondEast, new Vector2(19.7f, 25.4f), Facing4.FrontLeft, false),            // the pond's east bank
            (OgrinYard, new Vector2(48.2f, 11.8f), Facing4.FrontRight, false),          // beside the cottage
            (OgrinWindow, new Vector2(53.0f, 12.85f), Facing4.FrontRight, true),        // in bed, at the cottage's window
            (PondWest, new Vector2(16.6f, 23.4f), Facing4.BackLeft, false),             // the pond's south bank, drawing maps
            (GreenListen, new Vector2(26.4f, 17.3f), Facing4.BackRight, false),         // on the green, listening
            (Green, new Vector2(27.9f, 18.3f), Facing4.FrontLeft, false),               // on the green, playing
            (BartWagon, new Vector2(21.4f, 14.6f), Facing4.FrontRight, false),          // by his painted wagon
            (MarketFront, new Vector2(34.9f, 11.2f), Facing4.FrontRight, false),        // beside Musashi, gossiping (clear of the stall)
            (MarketCart, new Vector2(36.9f, 12.5f), Facing4.FrontRight, false),         // Musashi's spot (KariastonBuilder.MusashiSpot)
        };

        /// <summary>Tally Ho!'s: Maximo's lunch, at the seat nearest the middle of the room (whatever the furniture is today).</summary>
        public static readonly Vector2 TavernTableNear = new(12f, 8f);

        // ---------- schedules ----------

        static ScheduleBlock B(int from, int to, string anchor, string activity, params ScheduleCondition[] conditions) => new(from, to, anchor, activity, conditions);

        /// <summary>First drafts of everyone's day: two to four places each, the special day's block before the ordinary one.</summary>
        public static readonly (string character, Func<List<ScheduleBlock>> blocks)[] Schedules =
        {
            (CharacterIds.Maximo, () => new()
            {
                B(Morning, Midday, MemorialSquare, "proclaim"),
                B(Midday, Afternoon, TavernTable, "lunch"),
                B(Afternoon, Evening, TallyWatch, "watch"),
                B(Evening, Night, MemorialSquare, "vigil", ScheduleCondition.On(DayRule.MaximoVigil)),
                B(Evening, Night, MaximoPorch, "home"),
            }),
            (CharacterIds.Kaloren, () => new()
            {
                B(HerbsFrom, Midday, CottageDoor, HerbVisit.Activity, ScheduleCondition.On(DayRule.HerbDay)),
                B(Morning, Midday, KalorenTower, "tower"),
                B(Midday, Afternoon, SquareBench, "reading"),
                B(Afternoon, Night, KalorenTower, "tower"),
            }),
            (CharacterIds.Grim, () => new()
            {
                B(Morning, Midday, GrimYard, "chores"),
                B(Midday, Afternoon, MarketSide, "errand"),
                B(Afternoon, Evening, PondEast, "pond", ScheduleCondition.On(DayRule.OgrinWell)),
                B(Afternoon, Night, GrimYard, "home"),
            }),
            (CharacterIds.Ogrin, () => new()
            {
                B(Morning, Night, OgrinWindow, "bed", ScheduleCondition.NotOn(DayRule.OgrinWell)),
                B(Morning, Midday, OgrinYard, "yard"),
                B(Midday, Afternoon, PondWest, "maps"),
                B(Afternoon, Evening, GreenListen, "listening"),
                B(Evening, Night, OgrinWindow, "home"),
            }),
            (CharacterIds.Bart, () => new()
            {
                B(Morning, Midday, BartWagon, "tuning"),
                B(Midday, Afternoon, MarketFront, "gossip"),
                B(Afternoon, Evening, Green, "playing"),
                B(Evening, Night, BartWagon, "home"),
            }),
            (CharacterIds.Musashi, () => new()
            {
                B(Morning, Night, MarketCart, "trading"),
            }),
            // Boog and Orik keep their tavern posts (StaffAgent places them): their schedules say so, for the village's sake.
            (CharacterIds.Boog, () => new() { B(Morning, Night, TavernKitchen, "kitchen") }),
            (CharacterIds.Orik, () => new() { B(Morning, Night, TavernBar, "ledger") }),
        };

        /// <summary>The sheets the cast needs beyond the village's own (Kaloren's and Bart's layers, the emotes with Bart's note).</summary>
        static IEnumerable<Sheet> CastSheets()
        {
            var files = new HashSet<string> { "Emotions", "NpcShadowIdle", "NpcShadowWalk" };
            foreach (string anim in new[] { "Idle", "Walk" })
            {
                foreach (var (category, _, kind, variants) in MinifantasySheets.CastNpcLayers)
                foreach (string variant in variants)
                    files.Add(MinifantasySheets.NpcFile(anim, category, kind, variant));
                foreach (var (category, kind, variant) in new[] { ("Body", "Human", "whiteskin"), ("Hair", "Long", "white"), ("Beard", "LongBeard", "white"), ("Top", "Doublet", "red") })
                    files.Add(MinifantasySheets.NpcFile(anim, category, kind, variant));
            }
            return MinifantasySheets.All.Where(s => files.Contains(s.File));
        }

        public static void Build()
        {
            MinifantasyImporter.Import(CastSheets());
            EditorPaths.Ensure(Folder);
            var database = AssetDatabase.LoadAssetAtPath<GameDatabase>(k_DatabasePath) ?? throw new InvalidOperationException($"No game database at {k_DatabasePath}.");
            foreach (var (character, blocks) in Schedules)
            {
                string path = $"{Folder}/Schedule_{character}.asset";
                bool fresh = AssetDatabase.LoadAssetAtPath<ScheduleDefinition>(path) == null;
                ScheduleDefinition schedule = LookTestContent.CreateOrUpdate<ScheduleDefinition>(path, s =>
                {
                    s.character = character;
                    if (fresh || s.blocks == null || s.blocks.Count == 0) s.blocks = blocks();
                });
                if (!database.schedules.Contains(schedule)) database.schedules.Add(schedule);
            }
            database.schedules.RemoveAll(s => s == null);
            database.villageLife = LookTestContent.CreateOrUpdate<VillageLifeConfig>(EditorPaths.Config + "/VillageLife.asset", c =>
            {
                if (c.settings.herbEveryDays <= 0) c.settings = VillageLifeSettings.Default;
            });
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        // ---------- figures ----------

        /// <summary>A person's look: their layers back to front, and their shadow.</summary>
        public sealed class Figure
        {
            public SpriteAnimationSet[] Layers;
            public SpriteAnimationSet Shadow;
        }

        static SpriteAnimationSet Set(string name, params SpriteAnim[] anims)
        {
            EditorPaths.Ensure(k_Sets);
            return LookTestContent.CreateOrUpdate<SpriteAnimationSet>($"{k_Sets}/{name}.asset", s => s.animations = anims.ToList());
        }

        static SpriteAnim Anim(CharacterAnim action, string pack, string file, int frames, float seconds, bool loop = true) =>
            LookTestContent.Anim(action, pack, file, frames, 4, seconds, loop);

        static SpriteAnimationSet Npc(string category, string kind, string variant)
        {
            string name = $"{category}_{kind}_{variant}";
            return Set($"Npc_{name}",
                Anim(CharacterAnim.Idle, MinifantasySheets.MyriadOfNPCs, MinifantasySheets.NpcFile("Idle", category, kind, variant), 16, 0.2f),
                Anim(CharacterAnim.Walk, MinifantasySheets.MyriadOfNPCs, MinifantasySheets.NpcFile("Walk", category, kind, variant), 4, 0.15f));
        }

        static SpriteAnimationSet NpcShadow() => Set("Npc_Shadow",
            Anim(CharacterAnim.Idle, MinifantasySheets.MyriadOfNPCs, "NpcShadowIdle", 16, 0.2f),
            Anim(CharacterAnim.Walk, MinifantasySheets.MyriadOfNPCs, "NpcShadowWalk", 4, 0.15f));

        /// <summary>Everyone's look (built from the imported sheets; rerun freely).</summary>
        public static Dictionary<string, Figure> Figures()
        {
            string knight = KariastonSheets.KnightPack, miner = KariastonSheets.MinerPack, child = KariastonSheets.SnowballPack;
            SpriteAnimationSet ChildLayer(string part) => Set($"Ogrin_{(part.Length == 0 ? "Body" : part)}",
                Anim(CharacterAnim.Idle, child, $"Child{part}Idle", 16, 0.2f),
                Anim(CharacterAnim.Walk, child, $"Child{part}Walk", 4, 0.2f),
                Anim(CharacterAnim.Gather, child, $"Child{part}Gather", 6, 0.2f));
            return new Dictionary<string, Figure>
            {
                // The blue Knight on foot (locked): helmeted, always in his old armour; his sword raised to Karias now and then.
                [CharacterIds.Maximo] = new()
                {
                    Layers = new[]
                    {
                        Set("Maximo", Anim(CharacterAnim.Idle, knight, "KnightIdle", 16, 0.2f), Anim(CharacterAnim.Walk, knight, "KnightWalk", 4, 0.2f),
                            Anim(CharacterAnim.Attack, knight, "KnightAttack", 4, 0.12f, loop: false)),
                    },
                    Shadow = Set("Maximo_Shadow", Anim(CharacterAnim.Idle, knight, "KnightIdleShadow", 16, 0.2f), Anim(CharacterAnim.Walk, knight, "KnightWalkShadow", 4, 0.2f),
                        Anim(CharacterAnim.Attack, knight, "KnightAttackShadow", 4, 0.12f, loop: false)),
                },
                // The Miner: a dwarf in a lamp helmet with a pick (a delver who stopped going down), at work in his yard.
                [CharacterIds.Grim] = new()
                {
                    Layers = new[]
                    {
                        Set("Grim", Anim(CharacterAnim.Idle, miner, "MinerIdle", 16, 0.2f), Anim(CharacterAnim.Walk, miner, "MinerWalk", 4, 0.2f),
                            Anim(CharacterAnim.Attack, miner, "MinerAttack", 6, 0.12f, loop: false)),
                    },
                    Shadow = Set("Grim_Shadow", Anim(CharacterAnim.Idle, miner, "MinerIdleShadow", 16, 0.2f), Anim(CharacterAnim.Walk, miner, "MinerWalkShadow", 4, 0.2f),
                        Anim(CharacterAnim.Attack, miner, "MinerAttackShadow", 6, 0.12f, loop: false)),
                },
                // A Snowball Wars child (8 px against the keeper's 10): a red jumper and boots, crouching to draw his maps.
                [CharacterIds.Ogrin] = new()
                {
                    Layers = new[] { ChildLayer(""), ChildLayer("Boots"), ChildLayer("Jumper") },
                    Shadow = Set("Ogrin_Shadow", Anim(CharacterAnim.Idle, child, "ChildIdleShadow", 16, 0.2f), Anim(CharacterAnim.Walk, child, "ChildWalkShadow", 4, 0.2f),
                        Anim(CharacterAnim.Gather, child, "ChildGatherShadow", 6, 0.2f)),
                },
                // A Myriad old man: white hair and beard, a purple robe and long hat, and white gloves (in summer).
                [CharacterIds.Kaloren] = new()
                {
                    Layers = new[]
                    {
                        Npc("Body", "Human", "whiteskin"), Npc("Toga", "Toga", "purple"), Npc("Gloves", "Gloves", "white"),
                        Npc("Hair", "Long", "white"), Npc("Beard", "LongBeard", "white"), Npc("Hat", "LongHat", "purple"),
                    },
                    Shadow = NpcShadow(),
                },
                // A Myriad orc in a red doublet, boots and a cowboy hat (the Wise Orc is an armoured warlord with two swords at game scale).
                [CharacterIds.Bart] = new()
                {
                    Layers = new[]
                    {
                        Npc("Body", "Orc", "greenskin"), Npc("Trousers", "Trousers", "brownleather"), Npc("Top", "Doublet", "red"),
                        Npc("Shoes", "Shoes", "brownleather"), Npc("Hat", "CowboyHat", "brownleather"),
                    },
                    Shadow = NpcShadow(),
                },
            };
        }

        /// <summary>How each looks at what they do: a held action, a flourish, a face now and then.</summary>
        public static List<ActivityLook> Looks(string character)
        {
            Sprite Face(string name) => MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", name);
            ActivityLook L(string activity, CharacterAnim hold = CharacterAnim.Idle, CharacterAnim flourish = CharacterAnim.Idle, string face = null, float every = 0f) =>
                new() { activity = activity, hold = hold, flourish = flourish, emote = face != null ? Face(face) : null, every = every };
            return character switch
            {
                CharacterIds.Maximo => new() { L("proclaim", flourish: CharacterAnim.Attack, every: 7f), L("lunch", face: "Content", every: 14f), L("vigil") },
                CharacterIds.Kaloren => new() { L("reading", face: "Thinking", every: 12f), L(HerbVisit.Activity) },
                CharacterIds.Grim => new() { L("chores", flourish: CharacterAnim.Attack, every: 6f), L("errand", face: "Happy", every: 16f) },
                CharacterIds.Ogrin => new() { L("maps", hold: CharacterAnim.Gather), L("listening", face: "Heart", every: 11f), L("bed", face: "Thinking", every: 14f) },
                CharacterIds.Bart => new() { L("playing", face: "Note", every: 2.5f), L("tuning", face: "Note", every: 9f), L("gossip", face: "Happy", every: 10f) },
                _ => new(),
            };
        }

        // ---------- the objects ----------

        /// <summary>
        /// A villager in a scene (their copy there): the layered figure in a sorting group, solid feet (on Default: the keeper bumps
        /// them, the navigation grid doesn't), a face over the head, the talk interactable, and the <see cref="Villager"/>.
        /// </summary>
        public static Villager BuildVillager(Transform parent, string objectName, string characterId, string nameKey, string area, Figure figure, NavGrid grid, Vector2 at,
            bool startHidden = false)
        {
            var root = new GameObject(objectName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = at;
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            SortingGroup group = model.AddComponent<SortingGroup>();
            group.sortingLayerName = SortingLayers.YSorted;
            SpriteRenderer Layer(string name, int order)
            {
                SpriteRenderer r = LookTestContent.AddSprite(model.transform, name, null, SortingLayers.YSorted, order, Vector3.zero);
                r.spriteSortPoint = SpriteSortPoint.Pivot;
                return r;
            }
            SpriteRenderer shadow = Layer("Shadow", 0);
            SpriteRenderer[] layers = figure.Layers.Select((_, i) => Layer($"Layer {i}", i + 1)).ToArray();
            var look = model.AddComponent<LayeredSpriteAnimator>();
            look.Configure(layers, shadow, figure.Shadow);
            look.SetAppearance(figure.Layers);
            Collider2D feet = Feet(root.transform);
            NpcEmote emote = Emote(root.transform, characterId == CharacterIds.Ogrin ? 1.6f : 2.1f);
            TavernInteractable talk = Talk(root.transform);
            Villager villager = root.AddComponent<Villager>();
            villager.Configure(characterId, nameKey, talk);
            villager.ConfigurePresence(area, model, look, feet, emote, grid, Looks(characterId), startHidden);
            return villager;
        }

        public static Collider2D Feet(Transform root)
        {
            var go = new GameObject("Feet") { layer = LayerMask.NameToLayer("Default") };
            go.transform.SetParent(root, false);
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.6f, 0.3f);
            box.offset = new Vector2(0f, 0.15f);
            return box;
        }

        public static NpcEmote Emote(Transform root, float height)
        {
            var emoteRoot = new GameObject("Emote").transform;
            emoteRoot.SetParent(root, false);
            emoteRoot.localPosition = new Vector3(0f, height, 0f);
            SpriteRenderer face = LookTestContent.AddSprite(emoteRoot, "Face", null, SortingLayers.Above, 6, Vector3.zero);
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            if (unlit != null) face.sharedMaterial = unlit;
            NpcEmote emote = emoteRoot.gameObject.AddComponent<NpcEmote>();
            emote.Configure(face);
            return emote;
        }

        public static TavernInteractable Talk(Transform root)
        {
            var talk = new GameObject("Talk");
            talk.transform.SetParent(root, false);
            var interactable = talk.AddComponent<TavernInteractable>();
            interactable.Configure(TavernInteractableKind.Person, null, new Vector2(0f, -0.8f), 1f, null, new[] { new Vector2(-0.9f, 0.1f), new Vector2(0.9f, 0.1f), new Vector2(0f, 0.9f) });
            return interactable;
        }

        /// <summary>A named place in a scene.</summary>
        public static ScheduleAnchor Anchor(Transform parent, string id, string area, Vector2 localAt, Facing4 facing, bool window = false, GameObject occupied = null, bool tavernSeat = false)
        {
            var go = new GameObject($"Anchor {id}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localAt;
            ScheduleAnchor anchor = go.AddComponent<ScheduleAnchor>();
            anchor.Configure(id, area, facing, window, occupied, tavernSeat);
            return anchor;
        }
    }
}
