using System.Collections.Generic;
using Hearthdelve.Core.Input;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>Draws the Grill, Tap, or Chop minigame the player is playing, from its live state.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StationMinigamePanel : MonoBehaviour
    {
        VisualElement m_Grill, m_GrillBand, m_GrillNeedle;
        Label m_GrillTitle, m_GrillSide, m_GrillPrompt;
        VisualElement m_Tap, m_Glass, m_Liquid, m_Foam, m_Line, m_FoamZone;
        Label m_TapTitle, m_TapPrompt;
        VisualElement m_Chop, m_ChopBoard, m_ChopFood, m_ChopMarks, m_ChopKnife, m_ChopTimer;
        Label m_ChopTitle, m_ChopProgress, m_ChopPrompt;
        readonly List<VisualElement> m_ChopCuts = new();
        ChopMinigame m_DrawnChop;
        int m_DrawnItem = -1;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Grill = root.Q("grill");
            m_GrillBand = root.Q("grill-band");
            m_GrillNeedle = root.Q("grill-needle");
            m_GrillTitle = root.Q<Label>("grill-title");
            m_GrillSide = root.Q<Label>("grill-side");
            m_GrillPrompt = root.Q<Label>("grill-prompt");
            m_Tap = root.Q("tap");
            m_Glass = root.Q("tap-glass");
            m_Liquid = root.Q("tap-liquid");
            m_Foam = root.Q("tap-foam");
            m_Line = root.Q("tap-line");
            m_FoamZone = root.Q("tap-foam-zone");
            m_TapTitle = root.Q<Label>("tap-title");
            m_TapPrompt = root.Q<Label>("tap-prompt");
            m_Chop = root.Q("chop");
            m_ChopBoard = root.Q("chop-board");
            m_ChopFood = root.Q("chop-food");
            m_ChopMarks = root.Q("chop-marks");
            m_ChopKnife = root.Q("chop-knife");
            m_ChopTimer = root.Q("chop-timer");
            m_ChopTitle = root.Q<Label>("chop-title");
            m_ChopProgress = root.Q<Label>("chop-progress");
            m_ChopPrompt = root.Q<Label>("chop-prompt");
            m_Grill.style.display = DisplayStyle.None;
            m_Tap.style.display = DisplayStyle.None;
            m_Chop.style.display = DisplayStyle.None;
        }

        void Update()
        {
            var player = TavernDirector.Instance != null ? TavernDirector.Instance.Player : null;
            var game = player != null ? player.ActiveCook : null;
            var grill = game as GrillMinigame;
            var tap = game as TapMinigame;
            var chop = game as ChopMinigame;
            m_Grill.style.display = grill != null ? DisplayStyle.Flex : DisplayStyle.None;
            m_Tap.style.display = tap != null ? DisplayStyle.Flex : DisplayStyle.None;
            m_Chop.style.display = chop != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (grill != null) DrawGrill(grill);
            if (tap != null) DrawTap(tap);
            if (chop != null) DrawChop(chop);
        }

        void DrawChop(ChopMinigame c)
        {
            var s = c.Settings;
            // The board spans the same share of the screen the player's mouse is mapped onto.
            m_ChopBoard.style.left = Length.Percent(s.boardLeft * 100f);
            m_ChopBoard.style.width = Length.Percent(s.boardWidth * 100f);

            var pot = TavernDirector.Instance.Session.Pot;
            var item = c.Item < pot.ChopItems.Count ? pot.ChopItems[c.Item] : default;
            m_ChopTitle.text = Loc.UI(TavernLocKeys.ChopTitle, item.IsValid ? Loc.Get(item.Definition.displayName) : string.Empty);
            m_ChopProgress.text = Loc.UI(TavernLocKeys.ChopProgress, c.Item + 1, c.ItemCount);
            m_ChopPrompt.text = Loc.UI(TavernLocKeys.ChopPrompt,
                TavernUI.Binding(InputMaps.Minigame, MinigameActions.Aim),
                TavernUI.Binding(InputMaps.Minigame, MinigameActions.Action));
            m_ChopFood.style.backgroundColor = item.IsValid ? item.Definition.placeholderColor : Color.gray;

            var lines = c.LinesOf(c.Item);
            if (c != m_DrawnChop || c.Item != m_DrawnItem)
            {
                m_DrawnChop = c;
                m_DrawnItem = c.Item;
                m_ChopMarks.Clear();
                m_ChopCuts.Clear();
                foreach (float x in lines)
                {
                    var line = new VisualElement { pickingMode = PickingMode.Ignore };
                    line.AddToClassList("hd-chop__line");
                    line.style.left = Length.Percent(x * 100f);
                    m_ChopMarks.Add(line);
                    var cut = new VisualElement { pickingMode = PickingMode.Ignore };
                    cut.AddToClassList("hd-chop__cut");
                    m_ChopMarks.Add(cut);
                    m_ChopCuts.Add(cut);
                }
            }

            for (int i = 0; i < m_ChopCuts.Count; i++)
            {
                bool cut = c.IsCut(c.Item, i);
                m_ChopCuts[i].style.display = cut ? DisplayStyle.Flex : DisplayStyle.None;
                if (!cut) continue;
                m_ChopCuts[i].style.left = Length.Percent(c.CutPosition(c.Item, i) * 100f);
                m_ChopCuts[i].style.backgroundColor = Color.Lerp(new Color(0.85f, 0.25f, 0.2f), new Color(0.3f, 0.85f, 0.35f), c.CutScore(c.Item, i));
            }
            m_ChopKnife.style.left = Length.Percent(c.Knife * 100f);
            m_ChopTimer.style.width = Length.Percent(Mathf.Clamp01(c.ItemTimeLeft / Mathf.Max(0.01f, s.itemTimeLimit)) * 100f);
        }

        void DrawGrill(GrillMinigame g)
        {
            var s = g.Settings;
            m_GrillTitle.text = Loc.UI(TavernLocKeys.StationGrill);
            m_GrillSide.text = Loc.UI(TavernLocKeys.GrillSide, Mathf.Min(g.Side + 1, g.SideCount), g.SideCount);
            m_GrillPrompt.text = Loc.UI(TavernLocKeys.GrillPrompt, TavernUI.Binding(InputMaps.Minigame, MinigameActions.Action));
            m_GrillBand.style.left = Length.Percent(s.bandMin * 100f);
            m_GrillBand.style.width = Length.Percent((s.bandMax - s.bandMin) * 100f);
            m_GrillNeedle.style.left = Length.Percent(Mathf.Clamp01(g.IsPausing ? 0f : g.Meter) * 100f);
        }

        void DrawTap(TapMinigame t)
        {
            var s = t.Settings;
            m_TapTitle.text = Loc.UI(TavernLocKeys.StationTap);
            m_TapPrompt.text = Loc.UI(TavernLocKeys.TapPrompt,
                TavernUI.Binding(InputMaps.Minigame, MinigameActions.Action),
                TavernUI.Binding(InputMaps.Minigame, MinigameActions.Aim));
            m_Liquid.style.height = Length.Percent(Mathf.Clamp01(t.Liquid) * 100f);
            m_Foam.style.bottom = Length.Percent(Mathf.Clamp01(t.Liquid) * 100f);
            m_Foam.style.height = Length.Percent(Mathf.Clamp01(t.Foam) * 100f);
            m_Line.style.bottom = Length.Percent(s.fillLine * 100f);
            // Where the beer/foam boundary should land for a head inside the foam band.
            m_FoamZone.style.bottom = Length.Percent(s.fillLine * (1f - s.foamBandMax) * 100f);
            m_FoamZone.style.height = Length.Percent(s.fillLine * (s.foamBandMax - s.foamBandMin) * 100f);
            m_Glass.style.rotate = new Rotate(new Angle((0.5f - t.Tilt) * 50f, AngleUnit.Degree));
        }
    }
}
