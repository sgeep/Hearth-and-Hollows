using Hearthdelve.Shared.Inventory;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Screens
{
    /// <summary>Builds the visual for one satchel slot (icon swatch, name, quality, count).</summary>
    public static class SlotView
    {
        public const string SlotClass = "hd-slot";
        public const string EmptyClass = "hd-slot--empty";
        public const string SelectedClass = "hd-slot--selected";

        /// <summary>Fills <paramref name="root"/> with the slot's contents. Pass compact for the HUD.</summary>
        public static void Populate(VisualElement root, SatchelSlot slot, bool compact)
        {
            root.Clear();
            root.AddToClassList(SlotClass);
            root.EnableInClassList(EmptyClass, slot.IsEmpty);

            var swatch = new VisualElement();
            swatch.AddToClassList("hd-slot__swatch");
            root.Add(swatch);

            if (slot.IsEmpty)
            {
                if (!compact) root.Add(MakeLabel(Loc.UI(LocKeys.SlotEmpty), "hd-slot__name"));
                return;
            }

            var def = slot.Item.Definition;
            if (def.icon != null) swatch.style.backgroundImage = new StyleBackground(def.icon);
            else swatch.style.backgroundColor = def.placeholderColor;

            // Quality is shown as text (initial letter in compact mode), never by colour alone.
            var qualityText = Loc.Quality(slot.Item.Quality);
            var quality = MakeLabel(compact ? qualityText.Substring(0, 1) : qualityText, "hd-slot__quality");
            quality.AddToClassList($"hd-quality--{slot.Item.Quality.ToString().ToLowerInvariant()}");
            root.Add(quality);

            if (!compact)
            {
                root.Add(MakeLabel(Loc.Get(def.displayName), "hd-slot__name"));
                string prep = Loc.Prep(slot.Item.Prep);
                if (!string.IsNullOrEmpty(prep)) root.Add(MakeLabel(prep, "hd-slot__prep"));
            }
            if (slot.Count > 1 || !compact) root.Add(MakeLabel(Loc.UI(LocKeys.SlotCount, slot.Count), "hd-slot__count"));
        }

        static Label MakeLabel(string text, string cls)
        {
            var label = new Label(text);
            label.AddToClassList(cls);
            label.pickingMode = PickingMode.Ignore;
            return label;
        }
    }
}
