using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Engine;
using Hearthdelve.UI.Debugging;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.World;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Regression for the "room shakes while moving" bug. The Pixel Perfect Camera snaps the view to
    /// the 8 PPU art grid; when the follow camera trailed the player by a damped, fractional amount
    /// (or updated out of step with the interpolated player), the player and the camera rounded to
    /// different pixels on alternate frames and the whole room appeared to shake. Locked here: the
    /// follow keeps the player on the same screen pixel while it moves, and the snapped camera only
    /// ever steps forwards.
    /// </summary>
    public class CameraFollowTests : LookTestFixture
    {
        const float k_PixelsPerUnit = 8f;

        /// <summary>Records each frame once Cinemachine has placed the camera (its brain runs in LateUpdate at order 0).</summary>
        [DefaultExecutionOrder(32000)]
        sealed class FrameSampler : MonoBehaviour
        {
            public Action OnFrame;
            void LateUpdate() => OnFrame?.Invoke();
        }

        static int Reversals(IReadOnlyList<int> values)
        {
            int count = 0, last = 0;
            for (int i = 1; i < values.Count; i++)
            {
                int step = Math.Sign(values[i] - values[i - 1]);
                if (step == 0) continue;
                if (last != 0 && step != last) count++;
                last = step;
            }
            return count;
        }

        /// <summary>
        /// Frames where the snapped camera stepped on both axes together, and on one axis only, counting
        /// only steady diagonal movement: the player moved on both axes, in the same direction as on the
        /// frame before. (Sliding along a wall is one-axis movement; neither it nor a turn is a zig-zag.)
        /// A diagonal walk can start with the two axes at different sub-pixel phases, so before its first
        /// step on both axes there may be one step on a single axis to bring them into line; that step is
        /// counted as an alignment, not a zig-zag. Any single-axis step after the axes are in line is.
        /// </summary>
        static (int both, int single, int alignments, int runs) DiagonalSteps(IReadOnlyList<int> x, IReadOnlyList<int> y, IReadOnlyList<Vector2> moved)
        {
            int both = 0, single = 0, alignments = 0, runs = 0;
            bool inRun = false, aligned = false;
            for (int i = 2; i < x.Count; i++)
            {
                if (!SteadyDiagonal(moved[i], moved[i - 1]))
                {
                    inRun = false;
                    continue;
                }
                if (!inRun)
                {
                    inRun = true;
                    aligned = false;
                    runs++;
                }
                bool dx = x[i] != x[i - 1], dy = y[i] != y[i - 1];
                if (dx && dy)
                {
                    both++;
                    aligned = true;
                }
                else if (dx || dy)
                {
                    if (aligned) single++;
                    else alignments++;
                }
            }
            return (both, single, alignments, runs);
        }

        static bool SteadyDiagonal(Vector2 now, Vector2 before) =>
            Mathf.Abs(now.x) >= 1e-4f && Mathf.Abs(now.y) >= 1e-4f &&
            Mathf.Abs(before.x) >= 1e-4f && Mathf.Abs(before.y) >= 1e-4f &&
            Vector2.Angle(now, before) < 5f;

        [UnityTest]
        public IEnumerator SpeechBubble_StaysLockedToTheSpeaker_WhileThePlayerWalks()
        {
            yield return Load(TavernScene);
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables to preload");
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            Camera camera = Camera.main;
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            var bubble = Object.FindAnyObjectByType<SpeechBubble>();
            var bubbleRect = (RectTransform)bubble.transform;
            Transform cook = GameObject.Find("Cook").transform;
            Teleport(Player, (Vector2)cook.position + new Vector2(-2f, -3.2f));
            yield return new WaitForSeconds(0.3f);

            Assert.That(pixelPerfect.gridSnapping, Is.EqualTo(PixelPerfectCamera.GridSnapping.None), "smooth scrolling");
            // Where the world draws the cook, in art pixels from the centre of the view, against where
            // the bubble is. While the camera scrolls, the difference must not change.
            var drift = new List<Vector2>();
            var sampler = new GameObject("FrameSampler").AddComponent<FrameSampler>();
            sampler.OnFrame = () =>
            {
                if (!bubble.IsVisible) return;
                Vector2 cookPx = (Vector2)(cook.position - camera.transform.position) * k_PixelsPerUnit;
                // Bubble position converted back to art pixels from the centre of the view.
                Vector2 canvasSize = ((RectTransform)bubbleRect.root).rect.size;
                float viewHeight = 2f * camera.orthographicSize * k_PixelsPerUnit;
                float viewWidth = viewHeight * camera.aspect;
                Vector2 bubblePx = new((bubbleRect.anchoredPosition.x / canvasSize.x - 0.5f) * viewWidth,
                                       (bubbleRect.anchoredPosition.y / canvasSize.y - 0.5f) * viewHeight);
                drift.Add(bubblePx - cookPx);
            };
            try
            {
                Hold(Key.D, Key.W);
                yield return new WaitForSeconds(0.45f);
                Hold(Key.D);
                yield return new WaitForSeconds(0.35f);
                ReleaseKeys();
                yield return new WaitForSeconds(0.1f);
            }
            finally
            {
                Object.Destroy(sampler.gameObject);
                Application.targetFrameRate = -1;
            }

            Assert.That(drift.Count, Is.GreaterThan(20), "the bubble was visible while the player walked");
            float spread = drift.Max(d => Vector2.Distance(d, drift[0]));
            TestContext.WriteLine($"bubble offset from the cook {drift[0]} art px, spread {spread:F3}");
            Assert.That(spread, Is.LessThan(0.05f), $"the bubble moved against the cook by {spread:F3} art px");
        }

        /// <summary>
        /// Each look-test scroll mode (F4) keeps the player fixed on screen while the world scrolls,
        /// and the finer modes move the world on more frames. The default is restored afterwards.
        /// </summary>
        [UnityTest]
        public IEnumerator ScrollModes_KeepThePlayerFixedOnScreen_WhileWalkingDiagonally([Values] LookTestOverlay.ScrollMode mode)
        {
            yield return Load(DungeonScene);
            FreezeEnemies();
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            var overlay = Object.FindAnyObjectByType<LookTestOverlay>();
            yield return null;
            overlay.SetScrollMode(mode);
            Camera camera = Camera.main;
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            var presentation = Player.GetComponent<PixelSnappedPresentation>();
            Teleport(Player, new Vector2(-8f, -4f));
            yield return new WaitForSeconds(0.3f);

            bool snapping = mode != LookTestOverlay.ScrollMode.Smooth;
            var offsets = new List<Vector2>();
            var drawnFrom = new List<Vector2>();
            var sampler = new GameObject("FrameSampler").AddComponent<FrameSampler>();
            sampler.OnFrame = () =>
            {
                // The upscale pipeline draws from the camera rounded to its grid. (RoundToPixel would do it,
                // but its grid only updates when the camera renders, which batch-mode tests never do.)
                float grid = pixelPerfect.assetsPPU;
                Vector2 position = camera.transform.position;
                Vector2 view = snapping ? new Vector2(Mathf.Round(position.x * grid), Mathf.Round(position.y * grid)) / grid : position;
                drawnFrom.Add(view);
                offsets.Add(presentation.DisplayPosition - view);
            };
            try
            {
                Hold(Key.D, Key.W);
                yield return new WaitForSeconds(0.2f);
                offsets.Clear();
                drawnFrom.Clear();
                yield return new WaitForSeconds(0.6f);
            }
            finally
            {
                Object.Destroy(sampler.gameObject);
                ReleaseKeys();
                overlay.SetScrollMode(LookTestOverlay.ScrollMode.Smooth);
                Application.targetFrameRate = -1;
            }

            Assert.That(pixelPerfect.assetsPPU, Is.EqualTo(8), "the default is restored");
            Assert.That(pixelPerfect.gridSnapping, Is.EqualTo(PixelPerfectCamera.GridSnapping.None), "the default is restored");
            Assert.That(offsets.Count, Is.GreaterThan(20));
            float spread = offsets.Max(o => Vector2.Distance(o, offsets[0]));
            int jumpAt = offsets.FindIndex(o => Vector2.Distance(o, offsets[0]) >= 1e-3f);
            Assert.That(spread, Is.LessThan(1e-3f), $"{mode}: the player moved on screen by {spread * 8f:F3} art px, first at sample {jumpAt} of {offsets.Count}; " +
                                                     $"offsets in art px: {string.Join(" ", offsets.Select(o => (Vector2Int.RoundToInt(o * 16f))).Distinct())} (sixteenths of a tile)");

            int moved = 0;
            for (int i = 1; i < drawnFrom.Count; i++)
                if (drawnFrom[i] != drawnFrom[i - 1]) moved++;
            float share = moved / (float)(drawnFrom.Count - 1);
            TestContext.WriteLine($"{mode}: the world moved on {share:P0} of frames while walking diagonally");
            Assert.That(share, Is.GreaterThan(mode switch
            {
                LookTestOverlay.ScrollMode.PixelPerfect => 0.4f,
                LookTestOverlay.ScrollMode.HalfPixel => 0.8f,
                _ => 0.95f,
            }), $"{mode}: the world moved on only {share:P0} of frames");
        }

        [UnityTest]
        public IEnumerator Follow_KeepsThePlayerOnOnePixel_WhileWalkingDiagonallyAndDodging([Values(DungeonScene, TavernScene)] string scene)
        {
            yield return Load(scene);
            FreezeEnemies();
            // After loading: TDE's GameManager applies its own frame-rate setting in Start.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            // The pixel-perfect comparison mode (F4): its stepping must stay clean if it is ever chosen.
            var overlay = Object.FindAnyObjectByType<LookTestOverlay>();
            yield return null;
            overlay.SetScrollMode(LookTestOverlay.ScrollMode.PixelPerfect);

            Camera camera = Camera.main;
            var brain = camera.GetComponent<CinemachineBrain>();
            var composer = Object.FindAnyObjectByType<CinemachinePositionComposer>();
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            Assert.That(brain.UpdateMethod, Is.EqualTo(CinemachineBrain.UpdateMethods.LateUpdate), "the player is an interpolated rigidbody that moves every rendered frame");
            Assert.That(composer.Damping, Is.EqualTo(Vector3.zero), "a damped follow makes the player and the camera round to different pixels");
            var presentation = Player.GetComponent<PixelSnappedPresentation>();
            Assert.That(presentation, Is.Not.Null, "the player is drawn on the art-pixel grid");
            Assert.That(Object.FindAnyObjectByType<CinemachineCamera>().Follow, Is.SameAs(presentation.Anchor), "the camera follows the drawn position");
            Assert.That(Object.FindObjectsByType<PixelPerfectCamera>().Length, Is.EqualTo(1), "one pixel-perfect solution");
            Assert.That(pixelPerfect.assetsPPU, Is.EqualTo(8));

            Teleport(Player, scene == DungeonScene ? new Vector2(-8f, -4f) : (Vector2)Player.transform.position + new Vector2(-2.5f, -0.5f));
            yield return new WaitForSeconds(0.5f);

            var cameraX = new List<int>();
            var cameraY = new List<int>();
            var playerOnScreenX = new List<int>();
            var playerOnScreenY = new List<int>();
            var playerX = new List<int>();
            var playerY = new List<int>();
            var moved = new List<Vector2>();
            Vector2 lastReal = Player.transform.position;
            var sampler = new GameObject("FrameSampler").AddComponent<FrameSampler>();
            sampler.OnFrame = () =>
            {
                // The upscale pipeline draws from the camera rounded to its grid (RoundToPixel's grid
                // only updates when the camera renders, which batch-mode tests never do).
                Vector3 position = camera.transform.position;
                var snapped = new Vector3(Mathf.Round(position.x * k_PixelsPerUnit), Mathf.Round(position.y * k_PixelsPerUnit)) / k_PixelsPerUnit;
                // Where the player is drawn: its pixel-snapped display position.
                Vector3 player = Player.GetComponent<PixelSnappedPresentation>().DisplayPosition;
                cameraX.Add(Mathf.RoundToInt(snapped.x * k_PixelsPerUnit));
                cameraY.Add(Mathf.RoundToInt(snapped.y * k_PixelsPerUnit));
                playerOnScreenX.Add(Mathf.RoundToInt((player.x - snapped.x) * k_PixelsPerUnit));
                playerOnScreenY.Add(Mathf.RoundToInt((player.y - snapped.y) * k_PixelsPerUnit));
                playerX.Add(Mathf.RoundToInt(player.x * k_PixelsPerUnit));
                playerY.Add(Mathf.RoundToInt(player.y * k_PixelsPerUnit));
                Vector2 real = Player.transform.position;
                moved.Add(real - lastReal);
                lastReal = real;
            };

            var shake = Object.FindAnyObjectByType<ScreenShakeListener>();
            float shakeBefore = shake.LastForce;
            Vector3 start = Player.transform.position;
            try
            {
                Hold(Key.D, Key.W);
                yield return new WaitForSeconds(0.6f);
                Hold(Key.D, Key.W, Key.Space);
                yield return new WaitForSeconds(0.1f);
                Hold(Key.D, Key.W);
                yield return new WaitForSeconds(0.5f);
                ReleaseKeys();
                yield return new WaitForSeconds(0.2f);
            }
            finally
            {
                Object.Destroy(sampler.gameObject);
                overlay.SetScrollMode(LookTestOverlay.ScrollMode.Smooth);
                Application.targetFrameRate = -1;
            }

            Vector3 travelled = Player.transform.position - start;
            Assert.That(travelled.x, Is.GreaterThan(2f), "the player walked and dodged to the right");
            Assert.That(travelled.y, Is.GreaterThan(1f), "and up, at the same time");
            Assert.That(cameraX.Count, Is.GreaterThan(30));
            Assert.That(shake.LastForce, Is.EqualTo(shakeBefore), "no screen shake fires during plain movement");

            // The jitter itself: the player must stay on one screen pixel while the camera follows it.
            Assert.That(playerOnScreenX.Distinct().Count(), Is.EqualTo(1), $"{scene}: the player changed screen pixel on X: {string.Join(",", playerOnScreenX.Distinct())}");
            Assert.That(playerOnScreenY.Distinct().Count(), Is.EqualTo(1), $"{scene}: the player changed screen pixel on Y: {string.Join(",", playerOnScreenY.Distinct())}");
            // The camera only steps back when the player itself does (a collision push-out), never on its own.
            Assert.That(Reversals(cameraX), Is.LessThanOrEqualTo(Reversals(playerX)), $"{scene}: the snapped camera stepped back on X more often than the player");
            Assert.That(Reversals(cameraY), Is.LessThanOrEqualTo(Reversals(playerY)), $"{scene}: the snapped camera stepped back on Y more often than the player");
            // Diagonal movement: both axes should step on the same frames, not in a zig-zag.
            var (both, single, alignments, runs) = DiagonalSteps(cameraX, cameraY, moved);
            for (int i = 2; i < cameraX.Count; i++)
            {
                bool dx = cameraX[i] != cameraX[i - 1], dy = cameraY[i] != cameraY[i - 1];
                if (dx != dy && SteadyDiagonal(moved[i], moved[i - 1]))
                    TestContext.WriteLine($"{scene} single-axis step at frame {i}: player moved ({moved[i].x * k_PixelsPerUnit:F3}, {moved[i].y * k_PixelsPerUnit:F3}) px, previous ({moved[i - 1].x * k_PixelsPerUnit:F3}, {moved[i - 1].y * k_PixelsPerUnit:F3}), camera step ({cameraX[i] - cameraX[i - 1]}, {cameraY[i] - cameraY[i - 1]})");
            }
            Assert.That(both, Is.GreaterThan(15), $"{scene}: the walk included steady diagonal movement");
            Assert.That(single, Is.Zero, $"{scene}: the camera stepped on one axis at a time during steady diagonal movement ({single} single, {both} both): a zig-zag");
            Assert.That(alignments, Is.LessThanOrEqualTo(runs), $"{scene}: more than one alignment step per diagonal run ({alignments} in {runs} runs)");
            TestContext.WriteLine($"{scene}: camera reversals {Reversals(cameraX)}/{Reversals(cameraY)}, player reversals {Reversals(playerX)}/{Reversals(playerY)}, " +
                                  $"frames {cameraX.Count}, camera steps on both axes {both}, on one axis {single}, alignment steps {alignments} in {runs} diagonal runs");
        }
    }
}
