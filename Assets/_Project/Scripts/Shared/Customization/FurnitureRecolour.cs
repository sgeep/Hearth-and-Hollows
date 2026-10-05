using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Customization
{
    /// <summary>
    /// Bakes recoloured copies of furniture drawings (D11): the drawn pixels remapped to the chosen palette ramps, in a
    /// new point-filtered texture with the same pivot and pixels per unit. Every piece with the same drawing and choices
    /// shares one copy, so the lit sprite material, lighting, sorting and batching stay exactly as they are (no palette
    /// shader). The source sheets are imported readable. The mapping itself is <see cref="FurniturePalette"/>'s.
    /// </summary>
    public static class FurnitureRecolour
    {
        static readonly Dictionary<string, Sprite> s_Cache = new();
        static readonly HashSet<Texture2D> s_Warned = new();

        /// <summary>How many recoloured drawings exist (tests).</summary>
        public static int CacheSize => s_Cache.Count;

        /// <summary>The drawing as the piece's palette colours it (the drawing itself without choices for its channels).</summary>
        public static Sprite Apply(Sprite sprite, FurnitureDefinition definition, string palette, PaletteLibrary library) =>
            definition == null ? sprite : Apply(sprite, definition.paletteChannels, palette, library);

        /// <summary>A drawing recoloured through these channels (furniture, or a finish's tiles).</summary>
        public static Sprite Apply(Sprite sprite, IReadOnlyList<PaletteChannel> paletteChannels, string palette, PaletteLibrary library)
        {
            if (sprite == null || paletteChannels == null || library == null || string.IsNullOrEmpty(palette) || paletteChannels.Count == 0)
                return sprite;
            List<(Color32[] from, Color32[] to)> channels = FurniturePalette.Channels(paletteChannels, palette, library.Ramp);
            if (channels.Count == 0) return sprite;
            string applied = FurniturePalette.Restrict(palette, paletteChannels);
            string key = FurniturePalette.CacheKey($"{sprite.texture.GetEntityId()}:{sprite.name}:{sprite.rect}", applied);
            if (s_Cache.TryGetValue(key, out Sprite baked) && baked != null) return baked;

            Texture2D source = sprite.texture;
            if (!source.isReadable)
            {
                if (s_Warned.Add(source)) Debug.LogWarning($"[Hearthdelve] '{source.name}' isn't readable, so its pieces can't be recoloured.");
                return sprite;
            }
            Rect r = sprite.rect;
            int x = Mathf.RoundToInt(r.x), y = Mathf.RoundToInt(r.y), w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
            Color[] read = source.GetPixels(x, y, w, h);
            var pixels = new Color32[read.Length];
            for (int i = 0; i < read.Length; i++) pixels[i] = read[i];
            Color32[] remapped = FurniturePalette.Remap(pixels, FurniturePalette.Mapping(channels));

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = $"{sprite.name} ({applied})",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(remapped);
            texture.Apply(false, false);
            baked = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(sprite.pivot.x / w, sprite.pivot.y / h), sprite.pixelsPerUnit, 0,
                SpriteMeshType.FullRect);
            baked.name = texture.name;
            s_Cache[key] = baked;
            return baked;
        }

        public static Sprite[] Apply(Sprite[] frames, FurnitureDefinition definition, string palette, PaletteLibrary library)
        {
            if (frames == null || frames.Length == 0 || string.IsNullOrEmpty(palette)) return frames;
            var result = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++) result[i] = Apply(frames[i], definition, palette, library);
            return result;
        }
    }
}
