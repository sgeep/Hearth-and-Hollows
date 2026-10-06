using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>One butcherable part in the storeroom: what it is, how many, and who cuts it.</summary>
    [Serializable]
    public sealed class ButcherRow
    {
        public GameObject root;
        public Image icon;
        public LocalizedSuperText name;
        public LocalizedSuperText count;
        public LocalizedSuperText yield;
        public Button yourself;
        public Button staff;
        public LocalizedSuperText staffLabel;
    }

    /// <summary>
    /// The Butcher Block at Prep (4f Checkpoint C, D17: mise en place): the parts in the storeroom that can be broken down,
    /// each cut by the keeper (the minigame at the block) or by Gunta (she walks over and does it, steadily). Choosing is an
    /// investment, never a requirement: whole-part dishes stay on the menu.
    /// </summary>
    public sealed class ButcherPanel : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Intro;
        [SerializeField] ButcherRow[] m_Rows = Array.Empty<ButcherRow>();
        [SerializeField] LocalizedSuperText m_Message;
        [SerializeField] Button m_Done;

        readonly List<IngredientStack> m_Parts = new();
        TavernDirector m_Director;
        bool m_ReopenAfterCut;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<ButcherRow> Rows => m_Rows;
        public IReadOnlyList<IngredientStack> Parts => m_Parts;
        public string MessageText => m_Message != null && m_Message.gameObject.activeSelf ? m_Message.GetComponent<SuperTextMesh>()?.text : null;

        /// <summary>Raised when it closes (the Prep screen takes the selection back).</summary>
        public event Action Closed;

        public void Configure(GameObject root, LocalizedSuperText intro, ButcherRow[] rows, LocalizedSuperText message, Button done)
        {
            m_Root = root;
            m_Intro = intro;
            m_Rows = rows;
            m_Message = message;
            m_Done = done;
        }

        void Awake()
        {
            for (int i = 0; i < m_Rows.Length; i++)
            {
                int index = i;
                m_Rows[i].yourself.onClick.AddListener(() => CutYourself(index));
                m_Rows[i].staff.onClick.AddListener(() => LetStaff(index));
            }
            if (m_Done != null) m_Done.onClick.AddListener(Close);
            if (m_Root != null) m_Root.SetActive(false);
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (KeeperWork.Instance != null) KeeperWork.Instance.Butchered += OnButchered;
        }

        void OnDestroy()
        {
            if (KeeperWork.Instance != null) KeeperWork.Instance.Butchered -= OnButchered;
        }

        public void Open()
        {
            m_Director ??= TavernDirector.Instance;
            if (m_Root == null || m_Director == null) return;
            m_Root.SetActive(true);
            SetMessage(null);
            Fill();
            SelectFirst();
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            Closed?.Invoke();
        }

        void Update()
        {
            if (m_ReopenAfterCut && KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook == null)
            {
                // Back from the block: the panel again, with what the cut gave.
                m_ReopenAfterCut = false;
                m_Root.SetActive(true);
                Fill();
                SelectFirst();
            }
            if (!IsOpen) return;
            bool back = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                        || UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back) Close();
            else if (m_Director != null && m_Director.Cook != null && !m_Director.Cook.HasTask && m_WaitingOnStaff)
            {
                m_WaitingOnStaff = false;
                Fill();
            }
        }

        bool m_WaitingOnStaff;

        /// <summary>The keeper takes the part to the block (the panel steps aside for the cut).</summary>
        public bool CutYourself(int index)
        {
            if (!IsOpen || index >= m_Parts.Count || KeeperWork.Instance == null) return false;
            if (!KeeperWork.Instance.Butcher(m_Parts[index].Item)) return false;
            m_Root.SetActive(false);
            m_ReopenAfterCut = true;
            return true;
        }

        /// <summary>Gunta takes it: she walks over and cuts it while the list stays open.</summary>
        public bool LetStaff(int index)
        {
            if (!IsOpen || index >= m_Parts.Count || m_Director == null) return false;
            var staff = m_Director.CookMember;
            if (!m_Director.CookCanButcher)
            {
                SetMessage(Loc.UI(TavernLocKeys.ButcherBusy, staff != null ? Loc.Get(staff.displayName) : string.Empty));
                return false;
            }
            if (!m_Director.LetCookButcher(m_Parts[index].Item, OnButchered)) return false;
            m_WaitingOnStaff = true;
            SetMessage(Loc.UI(TavernLocKeys.ButcherWorking, Loc.Get(staff.displayName)));
            Fill();
            return true;
        }

        void OnButchered(IngredientStack cuts)
        {
            if (cuts.IsEmpty) return;
            SetMessage(Loc.UI(TavernLocKeys.ButcherResult, cuts.Count, Loc.Get(cuts.Item.Definition.displayName)));
            if (IsOpen) Fill();
        }

        void Fill()
        {
            m_Parts.Clear();
            KeeperWork keeper = KeeperWork.Instance;
            if (m_Director != null)
                m_Parts.AddRange(m_Director.Storeroom.Stacks.Where(s => !s.IsEmpty && s.Item.IsValid && s.Item.Definition.Butcherable)
                    .OrderBy(s => s.Item.Definition.id).ThenByDescending(s => s.Item.Quality));
            bool block = keeper != null && keeper.ButcherBlock != null;
            var staff = m_Director != null ? m_Director.CookMember : null;
            string staffName = staff != null ? Loc.Get(staff.displayName) : string.Empty;
            if (m_Intro != null)
                m_Intro.Set(!block ? TavernLocKeys.ButcherNoBlock : m_Parts.Count == 0 ? TavernLocKeys.ButcherNone : TavernLocKeys.ButcherIntro, staffName);
            bool staffFree = m_Director != null && m_Director.CookCanButcher;
            for (int i = 0; i < m_Rows.Length; i++)
            {
                ButcherRow row = m_Rows[i];
                bool has = block && i < m_Parts.Count;
                row.root.SetActive(has);
                if (!has) continue;
                IngredientStack stack = m_Parts[i];
                row.icon.sprite = stack.Item.Definition.icon;
                row.icon.enabled = row.icon.sprite != null;
                row.name.Set(TavernLocKeys.Plain, Loc.ItemName(stack.Item));
                row.count?.Set(TavernLocKeys.Plain, $"×{stack.Count}");
                row.yield.Set(TavernLocKeys.ButcherYield, stack.Item.Definition.butchering.maxCuts);
                row.staff.gameObject.SetActive(staff != null);
                if (staff != null) row.staffLabel.Set(TavernLocKeys.ButcherStaff, staffName);
                row.staff.interactable = staffFree;
            }
        }

        void SetMessage(string text)
        {
            if (m_Message == null) return;
            m_Message.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) m_Message.Set(TavernLocKeys.Plain, text);
        }

        void SelectFirst()
        {
            if (EventSystem.current == null) return;
            ButcherRow first = m_Rows.FirstOrDefault(r => r.root.activeSelf);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(first != null ? first.yourself.gameObject : m_Done != null ? m_Done.gameObject : null);
        }
    }
}
