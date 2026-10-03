using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Builds and checks run from menus or the command line: the web build (kept working at
    /// the end of every sub-milestone) and still captures of the look-test scenes.
    /// </summary>
    public static class BuildTools
    {
        public const string WebBuildFolder = "Builds/Web";

        [MenuItem("Hearthdelve/Build/Web (development)", priority = 200)]
        public static void BuildWebMenu() => BuildWeb();

        /// <summary>Batch entry point: <c>-executeMethod Hearthdelve.Editor.BuildTools.BuildWebBatch</c>.</summary>
        public static void BuildWebBatch() => EditorApplication.Exit(BuildWeb() ? 0 : 1);

        public static bool BuildWeb()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[Hearthdelve] The Web build module is not installed for this editor. Add it in Unity Hub (Installs > Add modules > Web Build Support).");
                return false;
            }
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[Hearthdelve] Could not switch the active build target to Web.");
                return false;
            }

            // Localization stores its string tables in Addressables; the player needs them built.
            AddressableAssetSettings.BuildPlayerContent(out var content);
            if (!string.IsNullOrEmpty(content.Error))
            {
                Debug.LogError($"[Hearthdelve] Addressables content build failed: {content.Error}");
                return false;
            }

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WebBuildFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development,
            });
            bool ok = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[Hearthdelve] Web build {report.summary.result}: {report.summary.totalErrors} errors, {report.summary.totalSize / (1024 * 1024)} MB at {WebBuildFolder}.");
            return ok;
        }

        /// <summary>
        /// Batch entry point (run without -nographics): renders each look-test scene's camera
        /// at the reference resolution into BatchLogs/, with the player placed at the spawn
        /// point, so the composition can be checked without opening the editor.
        /// </summary>
        public static void CaptureLookTestBatch()
        {
            try
            {
                Capture(EditorPaths.LookTestDungeonScene, "BatchLogs/looktest_dungeon.png");
                Capture(EditorPaths.LookTestTavernScene, "BatchLogs/looktest_tavern.png");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Batch entry point (run without -nographics): the 4b test floor, once at the reference
        /// resolution at the spawn and once as an overview of the whole floor at 8 px per tile.
        /// </summary>
        public static void CaptureTestFloorBatch()
        {
            try
            {
                Capture(EditorPaths.TestFloorScene, "BatchLogs/testfloor_spawn.png");
                Capture(EditorPaths.TestFloorScene, "BatchLogs/testfloor_overview.png",
                    new Vector2(TestFloorBuilder.Width, TestFloorBuilder.Height) * 0.5f, TestFloorBuilder.Width * 8, TestFloorBuilder.Height * 8);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Batch entry point (run without -nographics): the test floor with the swap prompt open over
        /// sample satchel contents, so its layout can be checked without the editor.
        /// </summary>
        public static void CaptureSwapPromptBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(EditorPaths.TestFloorScene, OpenSceneMode.Single);
                var screen = UnityEngine.Object.FindAnyObjectByType<Hearthdelve.UI.Screens.SwapPromptScreen>(FindObjectsInactive.Include);
                var leg = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>(EditorPaths.Ingredients + "/Ingredient_SpiderLeg.asset");
                var wing = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>(EditorPaths.Ingredients + "/Ingredient_BatWing.asset");
                var sac = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>(EditorPaths.Ingredients + "/Ingredient_VenomSac.asset");
                var gel = AssetDatabase.LoadAssetAtPath<Hearthdelve.Shared.Ingredients.IngredientDefinition>(EditorPaths.Ingredients + "/Ingredient_SlimeGel.asset");
                var satchel = new Hearthdelve.Shared.Inventory.Satchel(6, 3);
                satchel.Add(new Hearthdelve.Shared.Ingredients.IngredientItem(wing, Hearthdelve.Shared.Ingredients.Quality.Standard), 2, 0.9f);
                satchel.Add(new Hearthdelve.Shared.Ingredients.IngredientItem(leg, Hearthdelve.Shared.Ingredients.Quality.Fine), 3, 0.6f);
                satchel.Add(new Hearthdelve.Shared.Ingredients.IngredientItem(gel, Hearthdelve.Shared.Ingredients.Quality.Poor), 1, 0.3f);
                satchel.Add(new Hearthdelve.Shared.Ingredients.IngredientItem(sac, Hearthdelve.Shared.Ingredients.Quality.Premium), 1, 1f);
                satchel.Add(new Hearthdelve.Shared.Ingredients.IngredientItem(wing, Hearthdelve.Shared.Ingredients.Quality.Fine), 1, 0.8f);
                satchel.Add(new Hearthdelve.Shared.Ingredients.IngredientItem(leg, Hearthdelve.Shared.Ingredients.Quality.Standard), 2, 0.95f);
                var incoming = new Hearthdelve.Shared.Inventory.IngredientStack(new Hearthdelve.Shared.Ingredients.IngredientItem(sac, Hearthdelve.Shared.Ingredients.Quality.Fine), 1, 1f);
                typeof(Hearthdelve.UI.Screens.SwapPromptScreen).GetMethod("Open", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(screen, new object[] { new Hearthdelve.Shared.Run.SwapPromptRequested(satchel, incoming, _ => { }) });
                screen.Slots[1].OnSelect(null);
                Canvas canvas = screen.GetComponentInParent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1f;
                canvas.sortingLayerName = Hearthdelve.Core.SortingLayers.Above;
                canvas.sortingOrder = 100;
                foreach (var text in canvas.GetComponentsInChildren<SuperTextMesh>(true)) text.Rebuild();
                Capture(EditorPaths.TestFloorScene, "BatchLogs/swap_prompt.png", reopen: false);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Renders the scene's camera to a PNG: at the spawn point by default, or over a given area at 8 px per tile.</summary>
        static void Capture(string scenePath, string output, Vector2? centre = null, int width = 0, int height = 0, bool reopen = true)
        {
            if (reopen) EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var level = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
            GameObject player = null;
            if (level != null && level.PlayerPrefabs.Length > 0 && level.InitialSpawnPoint != null)
                player = UnityEngine.Object.Instantiate(level.PlayerPrefabs[0].gameObject, level.InitialSpawnPoint.transform.position, Quaternion.identity);

            // Local 2D lights build their mesh in LateUpdate, which a batch-mode capture never runs.
            foreach (var light in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>())
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                typeof(UnityEngine.Rendering.Universal.Light2D).GetMethod("UpdateMesh", flags)?.Invoke(light, new object[] { true });
                typeof(UnityEngine.Rendering.Universal.Light2D).GetMethod("UpdateBoundingSphere", flags)?.Invoke(light, null);
            }

            Camera camera = Camera.main;
            if (width <= 0)
            {
                width = LookTestBuilder.ReferenceWidth;
                height = LookTestBuilder.ReferenceHeight;
                if (player != null) camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
            }
            else
            {
                // An overview: the Pixel Perfect Camera would force the reference view size.
                var pixelPerfect = camera.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
                if (pixelPerfect != null) pixelPerfect.enabled = false;
                camera.orthographicSize = height / 2f / MinifantasySheets.PixelsPerUnit;
            }
            if (centre.HasValue) camera.transform.position = new Vector3(centre.Value.x, centre.Value.y, -10f);
            var target = new RenderTexture(width, height, 24) { filterMode = FilterMode.Point };
            camera.targetTexture = target;
            // Canvases drawn through the camera lay out for its target, not the batch "screen".
            foreach (var scaler in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>())
                typeof(UnityEngine.UI.CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(scaler, null);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
            File.WriteAllBytes(output, image.EncodeToPNG());

            RenderTexture.active = null;
            camera.targetTexture = null;
            target.Release();
            if (player != null) UnityEngine.Object.DestroyImmediate(player);
            Debug.Log($"[Hearthdelve] Captured {scenePath} to {output}.");
        }
    }
}
