using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Hud
{
    /// <summary>The run's powers (4d step 4): their icons in a row under the Essence bar, in the order they were taken.</summary>
    public sealed class RunPowersHud : MonoBehaviour
    {
        [SerializeField] Image[] m_Icons = Array.Empty<Image>();

        public int Shown { get; private set; }

        public void Configure(Image[] icons) => m_Icons = icons;

        void Awake()
        {
            foreach (Image icon in m_Icons) icon.gameObject.SetActive(false);
        }

        void OnEnable() => EventBus<RunPowerTaken>.Subscribe(OnTaken);
        void OnDisable() => EventBus<RunPowerTaken>.Unsubscribe(OnTaken);

        void OnTaken(RunPowerTaken e)
        {
            Shown = 0;
            for (int i = 0; i < m_Icons.Length; i++)
            {
                bool shown = e.All != null && i < e.All.Count && e.All[i] != null;
                m_Icons[i].gameObject.SetActive(shown);
                if (!shown) continue;
                m_Icons[i].sprite = e.All[i].icon;
                Shown++;
            }
        }
    }
}
