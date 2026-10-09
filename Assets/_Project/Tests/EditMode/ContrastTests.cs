using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hearthdelve.Editor;
using Hearthdelve.UI.Typography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthdelve.Tests.EditMode
{
    /// <summary>
    /// 4i-B's contrast check: every text in the game's scenes against the panel it sits on (the nearest image behind it: its sprite's
    /// average opaque colour, times the image's tint), as a WCAG contrast ratio. Silver at 1× is small, so the bar is AA's 4.5:1;
    /// text on a world-space backdrop (nothing behind it in the UI) isn't measured here.
    /// </summary>
    public class ContrastTests
    {
        public const float Minimum = 4.5f;
        /// <summary>Headings and display titles (2× and 3×) are large text: WCAG's 3:1.</summary>
        public const float LargeMinimum = 3f;

        /// <summary>
        /// Measured against a box, not the drawn pixels, these read wrongly, each checked by eye on a capture: the version sits on the
        /// menu's backdrop beside the panel (its box is wide); the catalogue's lower lines sit on the catalogue's own parchment, which
        /// covers the Decorate bar under it; "empty" shows only when the storeroom's slots are hidden.
        /// </summary>
        static readonly string[] k_Checked = { "UI/Menu/Version", "Content/Catalogue/", "Panel/StoreroomEmpty" };

        static readonly string[] k_Scenes = { BootBuilder.BootScene, BootBuilder.MainMenuScene, EditorPaths.TavernScene, EditorPaths.DungeonScene };
        static readonly Dictionary<Sprite, Color> s_Average = new();

        public static float Luminance(Color c)
        {
            static float Lin(float v) => v <= 0.03928f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            return 0.2126f * Lin(c.r) + 0.7152f * Lin(c.g) + 0.0722f * Lin(c.b);
        }

        public static float Ratio(Color a, Color b)
        {
            float la = Luminance(a), lb = Luminance(b);
            return (Mathf.Max(la, lb) + 0.05f) / (Mathf.Min(la, lb) + 0.05f);
        }

        /// <summary>The average colour of a sprite's opaque pixels, read from its source file (the imports aren't readable).</summary>
        static Color Average(Sprite sprite)
        {
            if (sprite == null) return Color.white;
            if (s_Average.TryGetValue(sprite, out Color c)) return c;
            string path = AssetDatabase.GetAssetPath(sprite.texture);
            var tex = new Texture2D(2, 2);
            c = Color.white;
            if (File.Exists(path) && tex.LoadImage(File.ReadAllBytes(path)))
            {
                // A sliced frame: only its face, inside the border (the text sits there, not on the frame).
                Rect r = sprite.rect;
                Vector4 border = sprite.border;
                if (border != Vector4.zero && r.width - border.x - border.z >= 1f && r.height - border.y - border.w >= 1f)
                    r = new Rect(r.x + border.x, r.y + border.y, r.width - border.x - border.z, r.height - border.y - border.w);
                Vector4 sum = Vector4.zero;
                int n = 0;
                for (int y = (int)r.y; y < (int)r.yMax; y++)
                for (int x = (int)r.x; x < (int)r.xMax; x++)
                {
                    Color p = tex.GetPixel(x, y);
                    if (p.a < 0.5f) continue;
                    sum += (Vector4)p;
                    n++;
                }
                if (n > 0) c = sum / n;
                c.a = 1f;
            }
            Object.DestroyImmediate(tex);
            return s_Average[sprite] = c;
        }

        /// <summary>
        /// What a text sits on: the topmost enabled, mostly opaque image drawn before it that covers its centre: at each level up,
        /// the earlier siblings (a button's face is its label's sibling), then the parent itself. Null: nothing in the UI.
        /// </summary>
        static Image Behind(Transform text)
        {
            Vector3 centre = Centre(text);
            for (Transform t = text; t.parent != null; t = t.parent)
            {
                Transform parent = t.parent;
                // Only within the text's own screen: the canvas's other children (the fade, the HUD, other screens) are never
                // shown with it in the way they're laid out in the scene file.
                if (parent.GetComponent<Canvas>() != null) return Opaque(parent.GetComponent<Image>()) ? parent.GetComponent<Image>() : null;
                for (int i = t.GetSiblingIndex() - 1; i >= 0; i--)
                {
                    Image covering = Covering(parent.GetChild(i), centre);
                    if (covering != null) return covering;
                }
                var image = parent.GetComponent<Image>();
                if (Opaque(image)) return image;
            }
            return null;
        }

        static bool Opaque(Image image) => image != null && image.enabled && image.gameObject.activeSelf && image.color.a >= 0.6f;

        /// <summary>The topmost opaque image in <paramref name="t"/>'s subtree whose rect covers <paramref name="point"/>.</summary>
        static Image Covering(Transform t, Vector3 point)
        {
            if (!t.gameObject.activeSelf) return null;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Image inner = Covering(t.GetChild(i), point);
                if (inner != null) return inner;
            }
            var image = t.GetComponent<Image>();
            return Opaque(image) && Contains((RectTransform)t, point) ? image : null;
        }

        static readonly Vector3[] s_Corners = new Vector3[4];

        static Vector3 Centre(Transform t)
        {
            ((RectTransform)t).GetWorldCorners(s_Corners);
            return (s_Corners[0] + s_Corners[2]) * 0.5f;
        }

        static bool Contains(RectTransform rect, Vector3 point)
        {
            rect.GetWorldCorners(s_Corners);
            return point.x >= s_Corners[0].x && point.x <= s_Corners[2].x && point.y >= s_Corners[0].y && point.y <= s_Corners[2].y;
        }

        static List<(string where, float ratio, Color text, Color panel)> Measure()
        {
            var found = new List<(string, float, Color, Color)>();
            foreach (string path in k_Scenes)
            {
                Scene scene = ProjectScan.Open(path);
                foreach (GameObject root in scene.GetRootGameObjects())
                foreach (SuperTextMesh text in root.GetComponentsInChildren<SuperTextMesh>(true))
                {
                    // The HUD's text is drawn light over the world, which this can't measure.
                    if (Name(text.transform).StartsWith("Hud/") || text.transform.root.name == "Hud") continue;
                    var styled = text.GetComponent<Hearthdelve.UI.Typography.StyledText>();
                    bool large = styled != null && (styled.Style == Hearthdelve.UI.Typography.TextStyle.Heading || styled.Style == Hearthdelve.UI.Typography.TextStyle.Display);
                    Image image = Behind(text.transform);
                    if (image == null) continue;
                    Color panel = Average(image.sprite) * image.color;
                    panel.a = 1f;
                    Color colour = text.color;
                    colour.a = 1f;
                    string where = $"{Path.GetFileNameWithoutExtension(path)}: {Name(text.transform)} [on {Name(image.transform)}]";
                    // Large text is measured against its own bar by scaling: a ratio of 3 counts as 4.5.
                    float ratio = Ratio(colour, panel) * (large ? Minimum / LargeMinimum : 1f);
                    found.Add((where, ratio, colour, panel));
                }
            }
            return found;
        }

        static string Name(Transform t)
        {
            var parts = new List<string>();
            for (int i = 0; t != null && i < 4; t = t.parent, i++) parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        [Test]
        public void EveryText_StandsOutFromItsPanel()
        {
            var low = Measure().Where(m => m.ratio < Minimum && !k_Checked.Any(m.where.Contains)).Select(m => $"{m.where}: {m.ratio:0.00}:1").ToList();
            Assert.That(low, Is.Empty, string.Join("\n", low));
        }

        /// <summary>The parchment panel, the lighter button face and the tan dish card, as measured.</summary>
        static readonly Color[] k_Faces = { new(0.906f, 0.737f, 0.471f), new(0.93f, 0.8f, 0.58f), new(0.82f, 0.66f, 0.46f) };

        [Test]
        public void EveryTextTone_ReachesAA_OnEveryParchmentFace_AndEveryOldToneIsMapped()
        {
            foreach (Color tone in new[] { UiPalette.Ink, UiPalette.Title, UiPalette.Label, UiPalette.Accent, UiPalette.Discovery, UiPalette.Note, UiPalette.Good, UiPalette.Warning })
            foreach (Color face in k_Faces)
                Assert.That(Ratio(tone, face), Is.GreaterThanOrEqualTo(Minimum), $"#{ColorUtility.ToHtmlStringRGB(tone)} on #{ColorUtility.ToHtmlStringRGB(face)}");
            foreach ((Color from, Color to) in UiPalette.Audited)
            {
                Assert.That(UiPalette.Audit(from), Is.EqualTo(to));
                Assert.That(UiPalette.Audit(to), Is.EqualTo(to), "audited tones stay as they are (the pass runs once in effect)");
            }
        }

        /// <summary>Machado et al. (2009), severity 1.</summary>
        static Color Simulate(Color c, bool protan)
        {
            float[,] m = protan
                ? new[,] { { 0.152f, 1.053f, -0.205f }, { 0.115f, 0.786f, 0.099f }, { -0.004f, -0.048f, 1.052f } }
                : new[,] { { 0.367f, 0.861f, -0.228f }, { 0.280f, 0.673f, 0.047f }, { -0.012f, 0.043f, 0.969f } };
            float R(int i) => Mathf.Clamp01(m[i, 0] * c.r + m[i, 1] * c.g + m[i, 2] * c.b);
            return new Color(R(0), R(1), R(2), c.a);
        }

        [Test]
        public void TheTelegraph_IsLighterThanTheFloor_NotOnlyRedder_UnderDeuteranopiaAndProtanopia()
        {
            var floor = new Color(0.22f, 0.19f, 0.18f);
            Color mark = Hearthdelve.Dungeon.Enemies.AttackTelegraphMarker.DefaultColour;
            foreach (bool protan in new[] { false, true })
            {
                Color f = Simulate(floor, protan), m = Simulate(mark, protan);
                Color seen = Color.Lerp(f, m, mark.a);
                Assert.That(Ratio(seen, f), Is.GreaterThanOrEqualTo(1.5f), protan ? "protanopia" : "deuteranopia");
            }
        }

        [Test, Explicit]
        public void WriteTheContrastReport()
        {
            var sb = new StringBuilder();
            foreach (var m in Measure().OrderBy(m => m.ratio))
                sb.AppendLine($"{m.ratio,6:0.00}  {m.where}  text #{ColorUtility.ToHtmlStringRGB(m.text)} on #{ColorUtility.ToHtmlStringRGB(m.panel)}");
            Directory.CreateDirectory("BatchLogs");
            File.WriteAllText("BatchLogs/contrast.txt", sb.ToString());
        }
    }
}
