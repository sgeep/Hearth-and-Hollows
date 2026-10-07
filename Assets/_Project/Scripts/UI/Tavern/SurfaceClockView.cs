using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// The surface clock's face (4h): "2:40 pm" in a corner, only in the free daytime, moving in the clock's display steps. Quiet
    /// on purpose: it tells the world's time, it isn't a countdown. Never shown in the Hollows.
    /// </summary>
    public sealed class SurfaceClockView : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Text;
        [SerializeField, Tooltip("The Decorate key's reminder, under the clock while indoors.")] GameObject m_DecorateRoot;
        [SerializeField] LocalizedSuperText m_Decorate;
        bool m_DecorateShown;

        int m_Shown = -1;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        /// <summary>The minute on the face now.</summary>
        public int ShownMinute => m_Shown;

        public void Configure(GameObject root, LocalizedSuperText text, GameObject decorateRoot = null, LocalizedSuperText decorate = null)
        {
            m_Root = root;
            m_Text = text;
            m_DecorateRoot = decorateRoot;
            m_Decorate = decorate;
        }

        void OnEnable() => EventBus<SurfaceTimeChanged>.Subscribe(OnTime);
        void OnDisable() => EventBus<SurfaceTimeChanged>.Unsubscribe(OnTime);

        void OnTime(SurfaceTimeChanged e) => Show(e.Minute);

        void LateUpdate()
        {
            TavernDirector director = TavernDirector.Instance;
            bool shown = director != null && director.Phase == TavernPhase.Daytime && GameFlow.Instance != null && GameFlow.Instance.InGame;
            if (m_Root != null && m_Root.activeSelf != shown) m_Root.SetActive(shown);
            if (shown && m_Shown != SurfaceTime.ShownMinute) Show(SurfaceTime.ShownMinute);
            // Decorating is a key away indoors (4h): say which, quietly.
            bool decorate = shown && SurfaceArea.Current != null && SurfaceArea.Current.Indoors && (DecorateMode.Instance == null || !DecorateMode.Instance.IsActive);
            if (m_DecorateRoot != null && m_DecorateRoot.activeSelf != decorate) m_DecorateRoot.SetActive(decorate);
            if (decorate && !m_DecorateShown && m_Decorate != null)
                m_Decorate.Set(SurfaceLocKeys.DecorateHint, Hearthdelve.UI.Screens.InputHints.Binding(Hearthdelve.Core.Input.InputMaps.Tavern, Hearthdelve.Core.Input.TavernActions.Decorate));
            m_DecorateShown = decorate;
        }

        void Show(int minute)
        {
            if (m_Text == null || minute == m_Shown) return;
            m_Shown = minute;
            (int hour, int minutes, bool pm) = SurfaceClock.Face(minute);
            m_Text.Set(SurfaceLocKeys.Clock, hour, minutes.ToString("00"), Loc.UI(pm ? SurfaceLocKeys.Pm : SurfaceLocKeys.Am));
        }
    }
}
