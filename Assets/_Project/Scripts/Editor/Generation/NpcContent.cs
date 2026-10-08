using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Animation;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Tavern NPCs (4c step 2): one animation set per A Myriad of NPCs layer (idle and walk), an
    /// appearance pool per customer profile, and the customer and Orik prefabs. Customers are TDE
    /// characters on the Npcs layer, walking on the grid through the thin pathfinding AI action and
    /// drawn by <see cref="LayeredSpriteAnimator"/>.
    /// </summary>
    public static class NpcContent
    {
        public const string CustomerPrefab = EditorPaths.Prefabs + "/Tavern/Customer.prefab";
        public const string PipPrefab = EditorPaths.Prefabs + "/Tavern/Pip.prefab";
        public const string GuntaPrefab = EditorPaths.Prefabs + "/Tavern/Gunta.prefab";
        const string k_Npcs = EditorPaths.Animations + "/Npc";
        const string k_UnlitSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        /// <summary>Back to front: the order <see cref="NpcAppearancePool.Layers"/> returns.</summary>
        static readonly string[] k_LayerNames = { "Body", "Trousers", "Top", "Beard", "Head", "Extra" };   // Extra: a named villager's sixth layer (Kaloren, 4h Checkpoint D)

        public sealed class Built
        {
            public GameObject Customer;
            public GameObject Pip;
            public GameObject Gunta;
        }

        public static Built Build()
        {
            var sets = BuildLayerSets();
            SpriteAnimationSet shadow = Set("NpcShadow", "NpcShadowIdle", "NpcShadowWalk");
            BuildPools(sets);
            AssetDatabase.SaveAssets();
            // 4f Checkpoint C: Orik's halfling look and Gunta's, replacing the Butcher stand-in.
            return new Built
            {
                Customer = BuildCustomer(shadow),
                Pip = BuildStaff("Pip", PipPrefab, StaffSet("Orik"), shadow, 3.2f, Vector2.zero),
                // The cook (stable id gunta) is Boog: the Goblin Sapper, with its own shadow.
                Gunta = BuildStaff("Gunta", GuntaPrefab, SapperSet("Boog", "GoblinSapperIdle", "GoblinSapperRun"),
                    SapperSet("BoogShadow", "GoblinSapperIdleShadow", "GoblinSapperRunShadow"), 2.8f, new Vector2(1.25f, 0f)),
            };
        }

        /// <summary>A member of staff's idle and walk (the derived sheets, in the Creatures pack's layout).</summary>
        static SpriteAnimationSet StaffSet(string who)
        {
            EditorPaths.Ensure(k_Npcs);
            return LookTestContent.CreateOrUpdate<SpriteAnimationSet>($"{k_Npcs}/{who}.asset", set => set.animations = new List<SpriteAnim>
            {
                LookTestContent.Anim(CharacterAnim.Idle, MinifantasySheets.Staff, $"{who}Idle", 16, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Walk, MinifantasySheets.Staff, $"{who}Walk", 4, 4, 0.15f, true),
            });
        }

        /// <summary>Boog's idle and walk: the Goblin Sapper's 20-frame idle and 10-frame run, 100 ms a frame, in the four facings.</summary>
        static SpriteAnimationSet SapperSet(string name, string idleFile, string runFile)
        {
            EditorPaths.Ensure(k_Npcs);
            return LookTestContent.CreateOrUpdate<SpriteAnimationSet>($"{k_Npcs}/{name}.asset", set => set.animations = new List<SpriteAnim>
            {
                LookTestContent.Anim(CharacterAnim.Idle, MinifantasySheets.GoblinSapper, idleFile, 20, 4, 0.1f, true),
                LookTestContent.Anim(CharacterAnim.Walk, MinifantasySheets.GoblinSapper, runFile, 10, 4, 0.1f, true),
            });
        }

        /// <summary>An idle and walk set from two imported sheets (<paramref name="idleFile"/>, <paramref name="walkFile"/>).</summary>
        static SpriteAnimationSet Set(string name, string idleFile, string walkFile)
        {
            EditorPaths.Ensure(k_Npcs);
            return LookTestContent.CreateOrUpdate<SpriteAnimationSet>($"{k_Npcs}/{name}.asset", set => set.animations = new List<SpriteAnim>
            {
                LookTestContent.Anim(CharacterAnim.Idle, MinifantasySheets.MyriadOfNPCs, idleFile, 16, 4, 0.2f, true),
                LookTestContent.Anim(CharacterAnim.Walk, MinifantasySheets.MyriadOfNPCs, walkFile, 4, 4, 0.15f, true),
            });
        }

        /// <summary>Every imported layer variant as an animation set, by "category_kind_variant".</summary>
        static Dictionary<string, SpriteAnimationSet> BuildLayerSets()
        {
            var sets = new Dictionary<string, SpriteAnimationSet>();
            foreach (var (category, _, kind, variants) in MinifantasySheets.NpcLayers)
            foreach (string variant in variants)
                sets[$"{category}_{kind}_{variant}"] = Set($"{category}_{kind}_{variant}",
                    MinifantasySheets.NpcFile("Idle", category, kind, variant), MinifantasySheets.NpcFile("Walk", category, kind, variant));
            return sets;
        }

        static List<SpriteAnimationSet> Pick(Dictionary<string, SpriteAnimationSet> sets, params string[] prefixes) =>
            sets.Where(p => prefixes.Any(prefix => p.Key.StartsWith(prefix))).OrderBy(p => p.Key).Select(p => p.Value).ToList();

        /// <summary>What each kind of customer can look like. Villagers wear shirts; adventurers jackets and hoods; dwarves beards.</summary>
        static void BuildPools(Dictionary<string, SpriteAnimationSet> sets)
        {
            var pools = new (string profile, System.Action<NpcAppearancePool> fill)[]
            {
                ("villager", pool =>
                {
                    pool.bodies = Pick(sets, "Body_Human");
                    pool.tops = Pick(sets, "Top_Shirt", "Top_Doublet_red");
                    pool.trousers = Pick(sets, "Trousers");
                    pool.heads = Pick(sets, "Hair_Short", "Hair_PonyTail", "Hair_Long");
                    pool.beards = Pick(sets, "Beard");
                    pool.beardChance = 0.15f;
                }),
                ("adventurer", pool =>
                {
                    pool.bodies = Pick(sets, "Body_Human", "Body_Elf");
                    pool.tops = Pick(sets, "Top_Jacket", "Top_Doublet_purple", "Top_Doublet_turquoise");
                    pool.trousers = Pick(sets, "Trousers_Trousers_black", "Trousers_Trousers_grey");
                    pool.heads = Pick(sets, "Hat", "Hair_PonyTail", "Hair_Short");
                    pool.beards = Pick(sets, "Beard");
                    pool.beardChance = 0.1f;
                }),
                // No dwarf body exists in the pack (docs/ASSET_MAP.md): dwarves are stocky-dressed humans with long beards.
                ("dwarf", pool =>
                {
                    pool.bodies = Pick(sets, "Body_Human_paleskin", "Body_Human_whiteskin", "Body_Human_brownskin");
                    pool.tops = Pick(sets, "Top_Doublet_red", "Top_Shirt_orange", "Top_Shirt_yellow");
                    pool.trousers = Pick(sets, "Trousers_Trousers_black", "Trousers_Trousers_grey");
                    pool.heads = Pick(sets, "Hair_Bold", "Hair_Short");
                    pool.beards = Pick(sets, "Beard");
                    pool.beardChance = 1f;
                }),
            };
            foreach (var (id, fill) in pools)
            {
                NpcAppearancePool pool = LookTestContent.CreateOrUpdate<NpcAppearancePool>($"{EditorPaths.Data}/Customers/Appearance_{id}.asset", fill);
                foreach (string guid in AssetDatabase.FindAssets("t:CustomerProfile", new[] { $"{EditorPaths.Data}/Customers" }))
                {
                    var profile = AssetDatabase.LoadAssetAtPath<CustomerProfile>(AssetDatabase.GUIDToAssetPath(guid));
                    if (profile == null || profile.id != id) continue;
                    profile.appearance = pool;
                    EditorUtility.SetDirty(profile);
                }
            }
        }

        /// <summary>A TDE character on the Npcs layer that walks on the grid toward its brain's target.</summary>
        static GameObject Walker(string name)
        {
            GameObject root = LookTestContent.CharacterRoot(name, Layers.Npcs, new Vector2(0.6f, 0.4f), new Vector2(0f, 0.2f));
            var character = root.AddComponent<Character>();
            character.CharacterType = Character.CharacterTypes.AI;
            character.CharacterDimension = Character.CharacterDimensions.Type2D;
            // Start and stop at once: TDE's easing let them drift past a spot and turn back (Orik "vibrating" at the
            // pass, step 2 playtest), and kept them looking like they walk for a moment after stopping.
            var movement = root.AddComponent<CharacterMovement>();
            movement.Acceleration = 0f;
            movement.Deceleration = 0f;
            var walk = root.AddComponent<AIActionPathfindToTarget2D>();
            // The agents stop the walk themselves on arrival; this only keeps the action from chasing the last few pixels.
            walk.StopDistance = 0.1f;
            var brain = root.AddComponent<AIBrain>();
            brain.States = new List<AIState> { new() { StateName = "Walk", Actions = new AIActionsList { walk }, Transitions = new AITransitionsList() } };
            return root;
        }

        static GameObject BuildCustomer(SpriteAnimationSet shadowSet)
        {
            GameObject root = Walker("Customer");
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            root.GetComponent<Character>().CharacterModel = model;
            SpriteRenderer shadow = LookTestContent.AddSprite(model.transform, "Shadow", shadowSet.Find(CharacterAnim.Idle).frontRight[0],
                SortingLayers.YSorted, 0, Vector3.zero);
            var layers = new SpriteRenderer[k_LayerNames.Length];
            for (int i = 0; i < layers.Length; i++)
                layers[i] = LookTestContent.AddSprite(model.transform, k_LayerNames[i], null, SortingLayers.YSorted, i + 1, Vector3.zero);
            var look = model.AddComponent<LayeredSpriteAnimator>();
            look.Configure(layers, shadow, shadowSet);

            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            // Patience: an 8-pixel bar over the head, green to red, shortening from the right.
            var patience = new GameObject("Patience").transform;
            patience.SetParent(root.transform, false);
            patience.localPosition = new Vector3(0f, 1.375f, 0f);
            SpriteRenderer back = LookTestContent.AddSprite(patience, "Back", DungeonUI.Pixel(), SortingLayers.Above, 0, Vector3.zero);
            back.transform.localScale = new Vector3(10f, 3f, 1f);
            back.color = new Color(0.08f, 0.06f, 0.06f);
            var anchor = new GameObject("Anchor").transform;
            anchor.SetParent(patience, false);
            anchor.localPosition = new Vector3(-0.5f, 0f, 0f);
            SpriteRenderer fill = LookTestContent.AddSprite(anchor, "Fill", DungeonUI.Pixel(), SortingLayers.Above, 1, new Vector3(0.0625f, 0f, 0f));
            if (unlit != null)
            {
                back.sharedMaterial = unlit;
                fill.sharedMaterial = unlit;
            }

            // A speech bubble for a face or the dish they ordered.
            var bubble = new GameObject("Bubble").transform;
            bubble.SetParent(root.transform, false);
            bubble.localPosition = new Vector3(0f, 2.125f, 0f);
            var bubbleSprites = new List<SpriteRenderer>
            {
                LookTestContent.AddSprite(bubble, "Body", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Bubble", "Body"), SortingLayers.Above, 2, Vector3.zero),
                LookTestContent.AddSprite(bubble, "Tail", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Bubble", "Tail"), SortingLayers.Above, 2, new Vector3(0f, -0.625f, 0f)),
            };
            SpriteRenderer icon = LookTestContent.AddSprite(bubble, "Icon", null, SortingLayers.Above, 3, Vector3.zero);
            bubbleSprites.Add(icon);
            // A special request's sparkle on the bubble's top-right corner (4f Checkpoint D).
            SpriteRenderer requestMark = LookTestContent.AddSprite(bubble, "RequestMark", MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Icons", "Sparkle"),
                SortingLayers.Above, 4, new Vector3(0.625f, 0.5f, 0f));
            requestMark.enabled = false;
            bubbleSprites.Add(requestMark);
            if (unlit != null) foreach (SpriteRenderer r in bubbleSprites) r.sharedMaterial = unlit;
            bubble.gameObject.SetActive(false);

            root.AddComponent<CustomerAgent>().Configure(look, back, fill, bubble.gameObject, icon,
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Thinking"), MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Angry"));
            root.GetComponent<CustomerAgent>().ConfigureEmotes(MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Surprised"));
            root.GetComponent<CustomerAgent>().ConfigureRequests(requestMark, MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Heart"),
                MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Frown"));
            return LookTestContent.SavePrefab(root, CustomerPrefab);
        }

        /// <summary>
        /// A member of staff (Orik, Gunta): a walker with their look, a plate over the head when carrying, and a face for their
        /// wordless reactions (4f Checkpoint C).
        /// </summary>
        static GameObject BuildStaff(string name, string path, SpriteAnimationSet set, SpriteAnimationSet shadowSet, float walkSpeed, Vector2 restOffset)
        {
            GameObject root = Walker(name);
            var model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            root.GetComponent<Character>().CharacterModel = model;
            SpriteRenderer shadow = LookTestContent.AddSprite(model.transform, "Shadow", null, SortingLayers.YSorted, 0, Vector3.zero);
            SpriteRenderer body = LookTestContent.AddSprite(model.transform, "Body", null, SortingLayers.YSorted, 1, Vector3.zero);
            var look = model.AddComponent<LayeredSpriteAnimator>();
            look.Configure(new[] { body }, shadow, shadowSet);
            look.SetAppearance(new[] { set });
            root.GetComponent<CharacterMovement>().WalkSpeed = walkSpeed;
            StaffAgent agent = root.AddComponent<StaffAgent>();
            TavernStationContent.AddCarryView(root);
            // The emote sits where a carried plate would, a little higher: a face over the head.
            var emoteRoot = new GameObject("Emote").transform;
            emoteRoot.SetParent(root.transform, false);
            emoteRoot.localPosition = new Vector3(0f, 2.25f, 0f);
            SpriteRenderer face = LookTestContent.AddSprite(emoteRoot, "Face", null, SortingLayers.Above, 6, Vector3.zero);
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
            if (unlit != null) face.sharedMaterial = unlit;
            NpcEmote emote = emoteRoot.gameObject.AddComponent<NpcEmote>();
            emote.Configure(face);
            agent.ConfigureLook(emote, restOffset, new StaffFaces
            {
                happy = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Happy"),
                content = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Content"),
                frown = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Frown"),
                surprised = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Surprised"),
                sweat = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Sweat"),
                heart = MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Emotions", "Heart"),
            });
            return LookTestContent.SavePrefab(root, path);
        }
    }
}
