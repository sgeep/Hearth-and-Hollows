using System.Collections;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Animation;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Not a check: records the tavern room as text (<see cref="TavernSnapshot"/>) and as a 320×180 image of the empty
    /// room. Run by name to refresh a baseline; 4f step 1 used it to record the 4e room before furniture became data.
    /// </summary>
    [Explicit]
    public class TavernBaselineCaptures : LookTestFixture
    {
        public const string BaselineFolder = "Assets/_Project/Tests/PlayMode/Baselines";

        [UnityTest]
        public IEnumerator RecordTheRoom()
        {
            yield return Load("Tavern");
            yield return null;
            Directory.CreateDirectory(BaselineFolder);
            File.WriteAllLines(Path.Combine(BaselineFolder, "Tavern_current.txt"), TavernSnapshot.Take());
            CaptureEmptyRoom("BatchLogs/tavern_room_current.png");
        }

        /// <summary>The room alone: characters hidden, loops on their first frame, no UI.</summary>
        internal static void CaptureEmptyRoom(string output)
        {
            foreach (Character c in Object.FindObjectsByType<Character>())
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (SpriteLoop loop in Object.FindObjectsByType<SpriteLoop>())
            {
                loop.enabled = false;
                loop.GetComponent<SpriteRenderer>().sprite = loop.Frames[0];
            }
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>()) canvas.enabled = false;
            Camera camera = Camera.main;
            var target = new RenderTexture(320, 180, 24) { filterMode = FilterMode.Point };
            camera.targetTexture = target;
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllBytes(output, image.EncodeToPNG());
        }
    }
}
