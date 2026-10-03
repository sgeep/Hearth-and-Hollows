using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Engine;
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

        [UnityTest]
        public IEnumerator Follow_KeepsThePlayerOnOnePixel_WhileWalkingDiagonallyAndDodging([Values(DungeonScene, TavernScene)] string scene)
        {
            yield return Load(scene);
            FreezeEnemies();
            // After loading: TDE's GameManager applies its own frame-rate setting in Start.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            Camera camera = Camera.main;
            var brain = camera.GetComponent<CinemachineBrain>();
            var composer = Object.FindAnyObjectByType<CinemachinePositionComposer>();
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            Assert.That(brain.UpdateMethod, Is.EqualTo(CinemachineBrain.UpdateMethods.LateUpdate), "the player is an interpolated rigidbody that moves every rendered frame");
            Assert.That(composer.Damping, Is.EqualTo(Vector3.zero), "a damped follow makes the player and the camera round to different pixels");
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
            var sampler = new GameObject("FrameSampler").AddComponent<FrameSampler>();
            sampler.OnFrame = () =>
            {
                Vector3 snapped = pixelPerfect.RoundToPixel(camera.transform.position);
                Vector3 player = Player.transform.position;
                cameraX.Add(Mathf.RoundToInt(snapped.x * k_PixelsPerUnit));
                cameraY.Add(Mathf.RoundToInt(snapped.y * k_PixelsPerUnit));
                playerOnScreenX.Add(Mathf.RoundToInt((player.x - snapped.x) * k_PixelsPerUnit));
                playerOnScreenY.Add(Mathf.RoundToInt((player.y - snapped.y) * k_PixelsPerUnit));
                playerX.Add(Mathf.RoundToInt(player.x * k_PixelsPerUnit));
                playerY.Add(Mathf.RoundToInt(player.y * k_PixelsPerUnit));
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
            TestContext.WriteLine($"{scene}: camera reversals {Reversals(cameraX)}/{Reversals(cameraY)}, player reversals {Reversals(playerX)}/{Reversals(playerY)}, frames {cameraX.Count}");
        }
    }
}
