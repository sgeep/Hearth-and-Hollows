using Hearthdelve.Core.Input;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>Draws the Grill or Tap minigame the player is playing, from its live state.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class StationMinigamePanel : MonoBehaviour
    {
        VisualElement m_Grill, m_GrillBand, m_GrillNeedle;
        Label m_GrillTitle, m_GrillSide, m_GrillPrompt;
        VisualElement m_Tap, m_Glass, m_Liquid, m_Foam, m_Line, m_FoamZone;
        Label m_TapTitle, m_TapPrompt;

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
            m_Grill.style.display = DisplayStyle.None;
            m_Tap.style.display = DisplayStyle.None;
        }

        void Update()
        {
            var player = TavernDirector.Instance != null ? TavernDirector.Instance.Player : null;
            var game = player != null ? player.ActiveCook : null;
            var grill = game as GrillMinigame;
            var tap = game as TapMinigame;
            m_Grill.style.display = grill != null ? DisplayStyle.Flex : DisplayStyle.None;
            m_Tap.style.display = tap != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (grill != null) DrawGrill(grill);
            if (tap != null) DrawTap(tap);
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
