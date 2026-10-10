using System;
using System.IO;
using System.Linq;
using System.Text;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Village;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Kariaston's walking map, for checking a hand edit (2026-10-10): opens the village, makes its dressing solid as the game does,
    /// bakes the villagers' grid, and writes BatchLogs/kariaston_walk.txt (# blocked, . walkable and reachable from Tally Ho!'s door,
    /// o walkable but cut off, letters the schedule anchors) with a line per anchor. Never saves the scene.
    /// </summary>
    public static class KariastonDiagnostics
    {
        /// <summary>Batch: <c>-executeMethod Hearthdelve.Editor.KariastonDiagnostics.WalkMapBatch</c>.</summary>
        public static void WalkMapBatch()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(KariastonBuilder.ScenePath, OpenSceneMode.Single);
                UnityEngine.Object.FindAnyObjectByType<DressingCollision>().Apply();
                Physics2D.SyncTransforms();
                NavGrid grid = UnityEngine.Object.FindAnyObjectByType<NavGrid>();
                grid.Bake();
                GridMap map = grid.Map;
                GridSpace space = grid.Space;
                RectInt b = grid.Bounds;
                SurfaceDoor door = UnityEngine.Object.FindObjectsByType<SurfaceDoor>(FindObjectsSortMode.None).First();
                GridCell start = space.ToCell(door.Arrival);
                var reach = new bool[b.width, b.height];
                var queue = new System.Collections.Generic.Queue<GridCell>();
                if (map.IsWalkable(start)) { reach[start.X, start.Y] = true; queue.Enqueue(start); }
                while (queue.Count > 0)
                {
                    GridCell c = queue.Dequeue();
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        var n = new GridCell(c.X + dx, c.Y + dy);
                        if (n.X < 0 || n.Y < 0 || n.X >= b.width || n.Y >= b.height || reach[n.X, n.Y] || !map.IsWalkable(n)) continue;
                        reach[n.X, n.Y] = true;
                        queue.Enqueue(n);
                    }
                }
                ScheduleAnchor[] anchors = UnityEngine.Object.FindObjectsByType<ScheduleAnchor>(FindObjectsSortMode.None).OrderBy(a => a.Id).ToArray();
                var rows = new char[b.height][];
                for (int y = 0; y < b.height; y++)
                {
                    rows[y] = new char[b.width];
                    for (int x = 0; x < b.width; x++)
                        rows[y][x] = !map.IsWalkable(new GridCell(x, y)) ? '#' : reach[x, y] ? '.' : 'o';
                }
                var report = new StringBuilder();
                for (int i = 0; i < anchors.Length; i++)
                {
                    GridCell c = space.ToCell(anchors[i].Spot);
                    bool inside = c.X >= 0 && c.Y >= 0 && c.X < b.width && c.Y < b.height;
                    bool ok = inside && map.IsWalkable(c) && reach[c.X, c.Y];
                    char mark = (char)('A' + i);
                    if (inside) rows[c.Y][c.X] = mark;
                    Vector2 local = anchors[i].Spot - (Vector2)b.position;
                    report.AppendLine($"{mark} {anchors[i].Id,-16} ({local.x:0.##}, {local.y:0.##}) {(ok ? "ok" : anchors[i].Window ? "window (indoors)" : "BLOCKED OR CUT OFF")}");
                }
                var text = new StringBuilder();
                text.AppendLine($"Kariaston walking map (village cells, y up; the door's arrival at {start.X},{start.Y})");
                for (int y = b.height - 1; y >= 0; y--) text.AppendLine($"{y,2} {new string(rows[y])}");
                text.AppendLine("   " + string.Concat(Enumerable.Range(0, b.width).Select(x => (char)('0' + x % 10))));
                text.Append(report);
                Directory.CreateDirectory("BatchLogs");
                File.WriteAllText("BatchLogs/kariaston_walk.txt", text.ToString());
                Debug.Log("[Hearthdelve] BatchLogs/kariaston_walk.txt written.\n" + report);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }
    }
}
