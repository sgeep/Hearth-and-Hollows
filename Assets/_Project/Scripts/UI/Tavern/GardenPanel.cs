using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Garden;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// What to plant (4h Checkpoint B): an empty bed asks, and this offers the starter crops (seeds are free) with their days
    /// to grow, and "not now". Choosing plants the bed (Vigor is spent then, all or nothing). Holds the surface clock while
    /// it's open; Escape or B backs out with nothing spent.
    /// </summary>
    public sealed class GardenPanel : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Title;
        [SerializeField] Button[] m_Crops;
        [SerializeField] LocalizedSuperText[] m_CropLabels;
        [SerializeField] Image[] m_CropIcons;
        [SerializeField] Button m_Cancel;

        string m_Bed;
        CropDefinition[] m_Offered = new CropDefinition[0];

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public string Bed => m_Bed;
        public Button[] CropButtons => m_Crops;
        public Button Cancel => m_Cancel;
        /// <summary>The crops on offer, in the buttons' order.</summary>
        public CropDefinition[] Offered => m_Offered;

        public void Configure(GameObject root, LocalizedSuperText title, Button[] crops, LocalizedSuperText[] labels, Image[] icons, Button cancel)
        {
            m_Root = root;
            m_Title = title;
            m_Crops = crops;
            m_CropLabels = labels;
            m_CropIcons = icons;
            m_Cancel = cancel;
        }

        void Awake()
        {
            for (int i = 0; i < m_Crops.Length; i++)
            {
                int index = i;
                m_Crops[i].onClick.AddListener(() => Choose(index));
            }
            if (m_Cancel != null) m_Cancel.onClick.AddListener(Close);
            if (m_Root != null) m_Root.SetActive(false);
        }

        void OnEnable() => EventBus<GardenPlantAsked>.Subscribe(OnAsked);

        void OnDisable()
        {
            EventBus<GardenPlantAsked>.Unsubscribe(OnAsked);
            SurfacePause.Release(this);
        }

        void OnAsked(GardenPlantAsked e) => Open(e.BedId);

        /// <summary>Asks what to plant in <paramref name="bed"/>.</summary>
        public void Open(string bed)
        {
            GameFlow flow = GameFlow.Instance;
            if (IsOpen || m_Root == null || flow == null || flow.Database == null) return;
            m_Bed = bed;
            var crops = flow.Database.crops.FindAll(c => c != null);
            m_Offered = crops.GetRange(0, Mathf.Min(m_Crops.Length, crops.Count)).ToArray();
            m_Title.Set(GardenLocKeys.PanelTitle, flow.VigorSettings.Cost(VigorActivity.PlantBed));
            for (int i = 0; i < m_Crops.Length; i++)
            {
                bool shown = i < m_Offered.Length;
                m_Crops[i].gameObject.SetActive(shown);
                if (!shown) continue;
                m_CropLabels[i].Set(GardenLocKeys.PanelCrop, Loc.UI(m_Offered[i].nameKey), m_Offered[i].growthDays);
                if (m_CropIcons[i] != null)
                {
                    m_CropIcons[i].sprite = m_Offered[i].icon;
                    m_CropIcons[i].enabled = m_Offered[i].icon != null;
                }
            }
            m_Root.SetActive(true);
            SurfacePause.Hold(this);
            InputMaps.ActivateUIOnly();
            if (EventSystem.current != null && m_Offered.Length > 0) EventSystem.current.SetSelectedGameObject(m_Crops[0].gameObject);
        }

        /// <summary>Plants the crop on button <paramref name="index"/>.</summary>
        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= m_Offered.Length) return;
            CropDefinition crop = m_Offered[index];
            string bed = m_Bed;
            Close();
            GameFlow.Instance?.PlantBed(bed, crop);
        }

        /// <summary>Not now: back to the garden, nothing spent.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            SurfacePause.Release(this);
            TavernDirector.RestoreInput();
        }

        void Update()
        {
            if (!IsOpen) return;
            bool back = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                        || UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back) Close();
        }
    }
}
