using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Not a check: renders the evening's screens (Prep, the service HUD with orders, Results) at 320×180 to
    /// <c>BatchLogs/</c> so their layout can be looked at. Explicit, so it only runs when asked for by name.
    /// </summary>
    [Explicit]
    public class TavernEveningCaptures : LookTestFixture
    {
        TavernDirector Director => TavernDirector.Instance;

        [TearDown]
        public void RestoreTime() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator CaptureTheEveningScreens()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            yield return null;
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            foreach (int card in new[] { 0, 2, 4 }) prep.Toggle(card);
            yield return null;
            Capture("BatchLogs/tavern_prep.png");

            Director.AssignStaff(StaffStation.Serving);
            Director.OpenService();
            Director.ArrivalsPaused = true;
            for (int i = 0; i < 4; i++) Director.SpawnCustomer();
            Time.timeScale = 4f;
            yield return WaitUntil(() => Director.Session.Tickets.Count >= 3, 40f, "a few orders");
            Time.timeScale = 1f;
            Ticket first = Director.Session.Tickets[0];
            Director.Session.StartCooking(first, this);
            yield return null;
            Capture("BatchLogs/tavern_hud.png");

            Director.EndServiceNow();
            Object.FindAnyObjectByType<EveningResultsScreen>().DoneButton.onClick.Invoke();
            yield return null;
            Capture("BatchLogs/tavern_results.png");
        }

        /// <summary>Renders the main camera and every screen-space canvas (each over the world, in its own order) at 320×180.</summary>
        internal static void Capture(string output)
        {
            Camera camera = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var saved = canvases.Select(c => (c, c.sortingLayerName, c.sortingOrder)).ToArray();
            foreach (Canvas canvas in canvases)
            {
                int order = canvas.sortingOrder;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                canvas.sortingLayerName = Hearthdelve.Core.SortingLayers.Above;
                canvas.sortingOrder = 100 + order;
            }
            var target = new RenderTexture(320, 180, 24) { filterMode = FilterMode.Point };
            camera.targetTexture = target;
            foreach (var pixel in Object.FindObjectsByType<Hearthdelve.UI.PixelCanvasScaler>()) pixel.Apply();
            foreach (var scaler in Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>())
                typeof(UnityEngine.UI.CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(scaler, null);
            Canvas.ForceUpdateCanvases();
            // The first render lets the Pixel Perfect Camera fit the target.
            camera.Render();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(320, 180, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
            image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
            File.WriteAllBytes(output, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            target.Release();
            foreach (var (canvas, layer, order) in saved)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingLayerName = layer;
                canvas.sortingOrder = order;
            }
        }
    }
}
