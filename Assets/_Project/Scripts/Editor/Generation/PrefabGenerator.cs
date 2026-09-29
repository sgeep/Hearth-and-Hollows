using Hearthdelve.Core;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Player;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds the Phase 1 prefabs from code (player, three Cellars enemies, training dummy,
    /// spore projectile, ingredient pickup). Prefabs are generator-owned: re-running rebuilds
    /// them, so tune values in the data assets, not on the prefabs.
    /// </summary>
    public static class PrefabGenerator
    {
        public sealed class Prefabs
        {
            public GameObject Player, Rat, Slime, Shroom, Dummy;
            public IngredientPickup Pickup;
            public SporeProjectile Spore;
        }

        static string PrefabPath(string folder, string name) => $"{EditorPaths.Prefabs}/{folder}/{name}.prefab";

        public static Prefabs Generate(ContentGenerator.Content content)
        {
            EditorPaths.Ensure(EditorPaths.Prefabs + "/Player");
            EditorPaths.Ensure(EditorPaths.Prefabs + "/Enemies");
            EditorPaths.Ensure(EditorPaths.Prefabs + "/Pickups");

            var p = new Prefabs();
            p.Pickup = Save(BuildPickup(), PrefabPath("Pickups", "IngredientPickup")).GetComponent<IngredientPickup>();
            p.Spore = Save(BuildSpore(), PrefabPath("Enemies", "SporeProjectile")).GetComponent<SporeProjectile>();
            p.Player = Save(BuildPlayer(content), PrefabPath("Player", "Player"));
            p.Rat = Save(BuildEnemy<RatBehaviour>("GiantRat", content.Rat, PlaceholderArtGenerator.Rat, new Vector2(1.0f, 0.55f), withMarker: false), PrefabPath("Enemies", "GiantRat"));
            p.Slime = Save(BuildEnemy<SlimeBehaviour>("GreenSlime", content.Slime, PlaceholderArtGenerator.Slime, new Vector2(0.85f, 0.6f), withMarker: true), PrefabPath("Enemies", "GreenSlime"));

            var shroom = BuildEnemy<ShroomBehaviour>("CellarShroom", content.Shroom, PlaceholderArtGenerator.Shroom, new Vector2(0.65f, 1.0f), withMarker: false);
            shroom.GetComponent<ShroomBehaviour>().ConfigureProjectile(p.Spore);
            p.Shroom = Save(shroom, PrefabPath("Enemies", "CellarShroom"));

            p.Dummy = Save(BuildEnemy<DummyBehaviour>("TrainingDummy", content.Dummy, PlaceholderArtGenerator.Dummy, new Vector2(0.6f, 1.4f), withMarker: false), PrefabPath("Enemies", "TrainingDummy"));
            return p;
        }

        static GameObject Save(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static int Layer(string name) => LayerMask.NameToLayer(name);
        static LayerMask Mask(params string[] names) => LayerMask.GetMask(names);

        static GameObject BuildPlayer(ContentGenerator.Content content)
        {
            var go = new GameObject("Player") { layer = Layer(Layers.Player) };
            AddKinematicBody(go, new Vector2(0.6f, 1.4f));

            var mover = go.AddComponent<KinematicMover2D>();
            mover.Configure(Mask(Layers.Ground), Mask(Layers.OneWayPlatform));
            go.AddComponent<PlayerInputReader>();
            var controller = go.AddComponent<PlayerController>();
            controller.Configure(content.Movement, content.Cleaver, Mask(Layers.Enemy));
            var vitals = go.AddComponent<PlayerVitals>();
            vitals.Configure(content.Essence);
            go.AddComponent<PlayerPickupCollector>().Configure(Mask(Layers.Pickup));

            var bodySprite = AddSprite(go.transform, "Body", PlaceholderArtGenerator.Player, SortingLayers.Player, 0, new Color(0.95f, 0.82f, 0.62f), Vector3.zero);
            var slash = AddSprite(go.transform, "Slash", PlaceholderArtGenerator.Slash, SortingLayers.FX, 0, new Color(1f, 0.95f, 0.85f, 0.85f), Vector3.zero);
            slash.enabled = false;

            go.AddComponent<PlayerVisuals>().Configure(controller, vitals, bodySprite.transform, bodySprite, slash);
            return go;
        }

        static GameObject BuildEnemy<T>(string name, EnemyDefinition def, string sprite, Vector2 size, bool withMarker) where T : EnemyController
        {
            var go = new GameObject(name) { layer = Layer(Layers.Enemy) };
            AddKinematicBody(go, size);
            go.AddComponent<KinematicMover2D>().Configure(Mask(Layers.Ground), Mask(Layers.OneWayPlatform));
            go.AddComponent<EnemyHealth>();

            var bodySprite = AddSprite(go.transform, "Body", sprite, SortingLayers.Enemies, 0, def.placeholderColor, Vector3.zero);
            float top = bodySprite.sprite != null ? bodySprite.sprite.bounds.max.y : size.y;
            var icon = AddSprite(go.transform, "TelegraphIcon", PlaceholderArtGenerator.Exclaim, SortingLayers.FX, 0, Color.white, new Vector3(0f, top + 0.45f, 0f));
            SpriteRenderer marker = null;
            if (withMarker)
                marker = AddSprite(go.transform, "LandingMarker", PlaceholderArtGenerator.Marker, SortingLayers.FX, -1, Color.white, Vector3.zero);

            var telegraph = go.AddComponent<TelegraphIndicator>();
            telegraph.Configure(bodySprite, icon, marker);
            go.AddComponent<T>().Configure(def, telegraph, bodySprite, Mask(Layers.Player));
            return go;
        }

        static GameObject BuildPickup()
        {
            var go = new GameObject("IngredientPickup") { layer = Layer(Layers.Pickup) };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearDamping = 0.5f;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.36f, 0.36f);

            var sprite = AddSprite(go.transform, "Icon", PlaceholderArtGenerator.Pickup, SortingLayers.Pickups, 0, Color.white, Vector3.zero);
            var pip = AddSprite(go.transform, "QualityPip", PlaceholderArtGenerator.Pip, SortingLayers.Pickups, 1, Color.white, new Vector3(0.2f, 0.2f, 0f));
            go.AddComponent<IngredientPickup>().Configure(sprite, pip);
            return go;
        }

        static GameObject BuildSpore()
        {
            var go = new GameObject("SporeProjectile") { layer = Layer(Layers.Projectile) };
            AddSprite(go.transform, "Sprite", PlaceholderArtGenerator.Spore, SortingLayers.FX, 0, Color.white, Vector3.zero);
            go.AddComponent<SporeProjectile>().ConfigureTerrain(Mask(Layers.Ground));
            return go;
        }

        static BoxCollider2D AddKinematicBody(GameObject go, Vector2 size)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = new Vector2(0f, size.y * 0.5f);
            return box;
        }

        static SpriteRenderer AddSprite(Transform parent, string name, string spriteName, string sortingLayer, int order, Color color, Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArtGenerator.Load(spriteName);
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }
    }
}
