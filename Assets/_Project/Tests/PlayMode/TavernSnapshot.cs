using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// The tavern room as it stands at runtime, as sorted text lines: every visible sprite (sprite, position, rotation,
    /// scale, sorting), every solid collider, the interactables with their use points and highlights, the seats, the
    /// door, queue and staff posts, the lights, the animated loops, the station views, and the baked walkable grid.
    /// Characters and UI are left out. 4f step 1 recorded it from the 4e scene (<c>Baselines/Tavern4e.txt</c>), so the
    /// data-driven tavern could be checked against the room it replaced, line by line; <c>TavernStarting.txt</c> is the
    /// starting room since the Checkpoint A playtest.
    /// </summary>
    public static class TavernSnapshot
    {
        static string F(float v) => (Mathf.Abs(v) < 0.00005f ? 0f : v).ToString("F4", CultureInfo.InvariantCulture);
        static string V(Vector2 v) => $"{F(v.x)},{F(v.y)}";
        static string C(Color c) => $"{F(c.r)},{F(c.g)},{F(c.b)},{F(c.a)}";

        static bool IsCharacterOrUi(Component c) =>
            c.GetComponentInParent<Character>(true) != null || c.GetComponentInParent<Canvas>(true) != null ||
            c.gameObject.layer == LayerMask.NameToLayer("UI") || OutsideTheTavern(c);

        /// <summary>The snapshot is of the tavern room: other areas of the property (the guest room, 4f step 6) stand far to its right.</summary>
        static bool OutsideTheTavern(Component c) => c.transform.position.x >= 40f;

        public static List<string> Take()
        {
            var lines = new List<string>();

            foreach (SpriteRenderer r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
            {
                if (IsCharacterOrUi(r)) continue;
                // Station highlights are hidden until targeted: recorded with their interactable below.
                if (r.GetComponentInParent<TavernInteractable>(true) is { } owner && r.transform.IsChildOf(owner.transform) && IsUnder(r.transform, owner.transform, "Highlight")) continue;
                bool visible = r.enabled && r.gameObject.activeInHierarchy;
                if (!visible) continue;
                Transform t = r.transform;
                SortingGroup group = r.GetComponentInParent<SortingGroup>();
                string grouped = group != null ? $"group {group.sortingLayerName}/{group.sortingOrder}@{V(group.transform.position)}" : "ungrouped";
                lines.Add($"sprite|{(r.sprite != null ? r.sprite.name : "none")}|{V(t.position)}|rot {F(t.eulerAngles.z)}|scale {V(t.lossyScale)}|flip {r.flipX},{r.flipY}" +
                          $"|{r.sortingLayerName}/{r.sortingOrder}|{C(r.color)}|{(r.sharedMaterial != null ? r.sharedMaterial.name : "none")}|{grouped}");
            }

            foreach (Collider2D col in Object.FindObjectsByType<Collider2D>())
            {
                if (IsCharacterOrUi(col) || !col.enabled) continue;
                Bounds b = col.bounds;
                lines.Add($"collider|{col.GetType().Name}|{V(b.min)}..{V(b.max)}|trigger {col.isTrigger}|layer {LayerMask.LayerToName(col.gameObject.layer)}");
            }

            foreach (TavernInteractable i in Object.FindObjectsByType<TavernInteractable>())
            {
                string uses = string.Join(";", i.UsePoints.Select(V));
                Transform highlight = i.transform.Find("Highlight");
                string corners = highlight == null ? "none" : string.Join(";", highlight.GetComponentsInChildren<SpriteRenderer>(true)
                    .Select(r => $"{r.name}@{V(r.transform.position)}/{r.sortingLayerName}/{r.sortingOrder}/{C(r.color)}/{(r.sharedMaterial != null ? r.sharedMaterial.name : "none")}")
                    .OrderBy(s => s));
                lines.Add($"interactable|{i.Kind}|{i.NameKey}|reach {F(i.Reach)}|use {uses}|highlight {corners}");
            }

            foreach (TavernSeat seat in Object.FindObjectsByType<TavernSeat>())
                lines.Add($"seat|{V(seat.SitPoint)}|approach {V(seat.ApproachPoint)}|{seat.Facing}");

            TavernLayout layout = Object.FindAnyObjectByType<TavernLayout>();
            if (layout != null)
            {
                lines.Add($"layout|door {V(layout.Door)}|seats {layout.SeatCount}");
                for (int q = 0; q < layout.QueueLength; q++) lines.Add($"queue|{q}|{V(layout.QueueSpot(q))}");
                foreach (StaffStation s in System.Enum.GetValues(typeof(StaffStation))) lines.Add($"post|{s}|{V(layout.PostFor(s))}");
                for (int s = 0; s < layout.SeatCount; s++)
                    lines.Add($"seat order|{s}|{V(layout.Seat(s).SitPoint)}");
            }

            foreach (Light2D light in Object.FindObjectsByType<Light2D>())
            {
                if (IsCharacterOrUi(light)) continue;
                lines.Add($"light|{light.lightType}|{V(light.transform.position)}|{C(light.color)}|{F(light.intensity)}|{F(light.pointLightInnerRadius)}..{F(light.pointLightOuterRadius)}|falloff {F(light.falloffIntensity)}");
            }

            foreach (SpriteLoop loop in Object.FindObjectsByType<SpriteLoop>())
            {
                if (IsCharacterOrUi(loop)) continue;
                lines.Add($"loop|{V(loop.transform.position)}|{string.Join(",", loop.Frames.Select(f => f != null ? f.name : "none"))}|{F(loop.FrameDuration)}");
            }

            foreach (PassView pass in Object.FindObjectsByType<PassView>())
                lines.Add($"pass view|{V(pass.transform.position)}|" + string.Join(";", pass.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(r => r.name.StartsWith("Plate")).Select(r => $"{V(r.transform.position)}/{r.sortingLayerName}/{r.sortingOrder}").OrderBy(x => x, System.StringComparer.Ordinal)));
            foreach (StewPotView pot in Object.FindObjectsByType<StewPotView>())
                lines.Add($"stew view|{V(pot.transform.position)}|" + string.Join(";", pot.GetComponentsInChildren<SpriteRenderer>(true)
                    .Select(r => $"{r.name}@{V(r.transform.position)}/{V(r.transform.lossyScale)}/{r.sortingLayerName}/{r.sortingOrder}/{C(r.color)}").OrderBy(x => x, System.StringComparer.Ordinal)));
            foreach (KitchenView kitchen in Object.FindObjectsByType<KitchenView>())
                lines.Add($"kitchen view|{V(kitchen.transform.position)}|idle {(kitchen.Idle != null ? kitchen.Idle.name : "none")}|working {kitchen.WorkingFrameCount}");

            NavGrid grid = NavGrid.Current;
            if (grid != null)
            {
                lines.Add($"grid|bounds {grid.Bounds}");
                for (int y = 0; y < grid.Map.Height; y++)
                {
                    var row = new char[grid.Map.Width];
                    for (int x = 0; x < grid.Map.Width; x++) row[x] = grid.Map.IsWalkable(new Hearthdelve.Core.Pathfinding.GridCell(x, y)) ? '.' : '#';
                    lines.Add($"grid|{y:D2}|{new string(row)}");
                }
            }

            lines.Sort(System.StringComparer.Ordinal);
            return lines;
        }

        static bool IsUnder(Transform t, Transform root, string name)
        {
            for (Transform p = t; p != null && p != root; p = p.parent)
                if (p.name == name) return true;
            return false;
        }
    }
}
