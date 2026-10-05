using System;
using System.Collections.Generic;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>One line of the catalogue's list.</summary>
    [Serializable]
    public sealed class CatalogueRow
    {
        public Button button;
        public Image background;
        public LocalizedSuperText name;
        public LocalizedSuperText info;
    }

    /// <summary>
    /// The furniture catalogue (4f step 4; plan §12): pages for what's in storage, each category, and the room's walls and
    /// floors. Each piece shows its drawing, name, price, how many are owned, placed and stored, whether its tier is open
    /// (and the Renown it needs), and where copies come from. A places one (from storage, or buys one onto the cursor), X
    /// buys one into storage, Y sells one back (D12); walls and floors are bought once and laid over the whole room (D5).
    /// Controller first, with the Decorate controls; the mouse clicks rows and actions. Presentation over
    /// <see cref="DecorateMode"/>, which does the buying.
    /// </summary>
    public sealed class DecorateCatalogue : MonoBehaviour
    {
        enum Page
        {
            Storage,
            Category,
            Room,
        }

        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Tab;
        [SerializeField] LocalizedSuperText m_Purse;
        [SerializeField] LocalizedSuperText m_Empty;
        [SerializeField] LocalizedSuperText m_Controls;
        [SerializeField] CatalogueRow[] m_Rows = Array.Empty<CatalogueRow>();
        [SerializeField] Image m_Icon;
        [SerializeField] LocalizedSuperText m_PieceName;
        [SerializeField] LocalizedSuperText m_Description;
        [SerializeField] LocalizedSuperText m_Tier;
        [SerializeField] LocalizedSuperText m_Counts;
        [SerializeField] LocalizedSuperText m_Source;
        [SerializeField] LocalizedSuperText m_Message;
        [SerializeField] Image[] m_Swatches = Array.Empty<Image>();
        [SerializeField] Button m_Primary, m_Secondary, m_Tertiary;
        [SerializeField] LocalizedSuperText m_PrimaryLabel, m_SecondaryLabel, m_TertiaryLabel;
        [SerializeField] Color m_RowSelected = new(0.95f, 0.8f, 0.45f, 0.55f);
        [SerializeField] Color m_RowPlain = new(0f, 0f, 0f, 0f);
        [SerializeField] Color m_Ink = new(0.24f, 0.16f, 0.12f);
        [SerializeField] Color m_Locked = new(0.5f, 0.45f, 0.4f);
        [SerializeField] Color m_Short = new(0.62f, 0.2f, 0.12f);
        [SerializeField, Min(0.05f)] float m_RepeatDelay = 0.3f;
        [SerializeField, Min(0.02f)] float m_RepeatRate = 0.08f;

        readonly List<(Page page, FurnitureCategory category)> m_Tabs = new();
        readonly List<FurnitureDefinition> m_Pieces = new();
        readonly List<FinishDefinition> m_Finishes = new();
        int m_TabIndex, m_Selected, m_Scroll;
        float m_HeldFor, m_NextRepeat;
        int m_HeldStep;
        int m_OpenedFrame = -1;
        bool m_Gamepad;
        DirectionReader m_MoveReader;
        InputAction m_Move, m_Select, m_Cancel, m_Buy, m_Sell, m_Next, m_Previous, m_Close, m_Wheel;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public int Count => CurrentPage == Page.Room ? m_Finishes.Count : m_Pieces.Count;
        public int Selected => m_Selected;
        public string TabText => Text(m_Tab);
        public string MessageText => Text(m_Message);
        public IReadOnlyList<CatalogueRow> Rows => m_Rows;
        public FurnitureDefinition SelectedPiece => CurrentPage != Page.Room && m_Selected < m_Pieces.Count ? m_Pieces[m_Selected] : null;
        public FinishDefinition SelectedFinish => CurrentPage == Page.Room && m_Selected < m_Finishes.Count ? m_Finishes[m_Selected] : null;

        Page CurrentPage => m_Tabs.Count > 0 ? m_Tabs[m_TabIndex].page : Page.Storage;
        static DecorateMode Mode => DecorateMode.Instance;

        public void Configure(GameObject root, LocalizedSuperText tab, LocalizedSuperText purse, LocalizedSuperText empty, LocalizedSuperText controls,
            CatalogueRow[] rows, Image icon, LocalizedSuperText name, LocalizedSuperText description, LocalizedSuperText tier, LocalizedSuperText counts,
            LocalizedSuperText source, LocalizedSuperText message, Image[] swatches, Button primary, LocalizedSuperText primaryLabel, Button secondary,
            LocalizedSuperText secondaryLabel, Button tertiary, LocalizedSuperText tertiaryLabel)
        {
            m_Root = root;
            m_Tab = tab;
            m_Purse = purse;
            m_Empty = empty;
            m_Controls = controls;
            m_Rows = rows;
            m_Icon = icon;
            m_PieceName = name;
            m_Description = description;
            m_Tier = tier;
            m_Counts = counts;
            m_Source = source;
            m_Message = message;
            m_Swatches = swatches;
            m_Primary = primary;
            m_PrimaryLabel = primaryLabel;
            m_Secondary = secondary;
            m_SecondaryLabel = secondaryLabel;
            m_Tertiary = tertiary;
            m_TertiaryLabel = tertiaryLabel;
        }

        void OnDestroy() => m_MoveReader?.Dispose();

        void Start()
        {
            for (int i = 0; i < m_Rows.Length; i++)
            {
                int row = i;
                m_Rows[i].button.onClick.AddListener(() =>
                {
                    if (m_Scroll + row == m_Selected) Primary();
                    else Select(m_Scroll + row);
                });
            }
            m_Primary.onClick.AddListener(Primary);
            m_Secondary.onClick.AddListener(Secondary);
            m_Tertiary.onClick.AddListener(Tertiary);
            if (!IsOpen) m_Root.SetActive(false);
        }

        // ------------------------------------------------------------------ opening

        /// <summary>Opens on what's in storage (where the storage panel used to be).</summary>
        public void OpenStorage() => Open(Page.Storage, default);

        public void OpenCategory(FurnitureCategory category) => Open(Page.Category, category);

        public void OpenRoom() => Open(Page.Room, default);

        void Open(Page page, FurnitureCategory category)
        {
            DecorateMode mode = Mode;
            if (mode == null || !mode.IsActive || mode.Carried != null) return;
            BuildTabs();
            m_TabIndex = Mathf.Max(0, m_Tabs.FindIndex(t => t.page == page && (page != Page.Category || t.category == category)));
            FindActions();
            m_Root.SetActive(true);
            mode.PanelOpen = true;
            // The press that opened it isn't also a press inside it.
            m_OpenedFrame = Time.frameCount;
            SetMessage(null);
            Fill(0);
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            if (Mode != null) Mode.PanelOpen = false;
        }

        void BuildTabs()
        {
            m_Tabs.Clear();
            m_Tabs.Add((Page.Storage, default));
            var categories = new HashSet<FurnitureCategory>();
            foreach (FurnitureDefinition d in Mode.Catalogue()) categories.Add(d.category);
            foreach (FurnitureCategory c in Enum.GetValues(typeof(FurnitureCategory)))
                if (categories.Contains(c)) m_Tabs.Add((Page.Category, c));
            m_Tabs.Add((Page.Room, default));
        }

        void FindActions()
        {
            InputAction A(string name) => InputMaps.Find(InputMaps.Decorate, name);
            m_Move = A(DecorateActions.Move);
            m_MoveReader?.Dispose();
            m_MoveReader = new DirectionReader(m_Move);
            m_Select = A(DecorateActions.Select);
            m_Cancel = A(DecorateActions.Cancel);
            m_Buy = A(DecorateActions.Turn);
            m_Sell = A(DecorateActions.Flip);
            m_Next = A(DecorateActions.Cycle);
            m_Previous = A(DecorateActions.Free);
            m_Close = A(DecorateActions.Storage);
            m_Wheel = A(DecorateActions.Wheel);
        }

        // ------------------------------------------------------------------ input

        static bool Pressed(InputAction a) => a != null && a.WasPressedThisFrame();

        void Update()
        {
            if (!IsOpen) return;
            DecorateMode mode = Mode;
            if (mode == null || !mode.IsActive)
            {
                Close();
                return;
            }
            if (Time.frameCount == m_OpenedFrame)
            {
                m_MoveReader?.Clear();
                return;
            }
            if (Pressed(m_Cancel) || Pressed(m_Close))
            {
                Close();
                return;
            }
            if (Pressed(m_Next)) Tab(+1);
            if (Pressed(m_Previous)) Tab(-1);
            if (Pressed(m_Select)) Primary();
            if (Pressed(m_Buy)) Secondary();
            if (Pressed(m_Sell)) Tertiary();
            float wheel = m_Wheel != null ? m_Wheel.ReadValue<Vector2>().y : 0f;
            if (Mathf.Abs(wheel) > 0.1f) Select(m_Selected - (int)Mathf.Sign(wheel));

            Vector2 move = m_MoveReader != null ? m_MoveReader.Read() : Vector2.zero;
            int step = Mathf.Abs(move.y) > 0.5f ? (move.y > 0f ? -1 : 1) : 0;
            int across = Mathf.Abs(move.x) > 0.5f && Mathf.Abs(move.x) > Mathf.Abs(move.y) ? (move.x > 0f ? 1 : -1) : 0;
            int held = step != 0 ? step : across * 100;
            if (held == 0)
            {
                m_HeldStep = 0;
                return;
            }
            bool fire;
            if (held != m_HeldStep)
            {
                m_HeldStep = held;
                m_HeldFor = 0f;
                m_NextRepeat = m_RepeatDelay;
                fire = true;
            }
            else
            {
                m_HeldFor += Time.unscaledDeltaTime;
                fire = m_HeldFor >= m_NextRepeat;
                if (fire) m_NextRepeat += m_RepeatRate;
            }
            if (!fire) return;
            if (step != 0) Select(m_Selected + step);
            else Tab(across);
        }

        void LateUpdate()
        {
            if (!IsOpen) return;
            bool pad = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame && (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f || Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f);
            bool keys = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (pad && !m_Gamepad || keys && m_Gamepad)
            {
                m_Gamepad = !m_Gamepad;
                Fill(m_Selected);
            }
        }

        public void Tab(int step)
        {
            if (m_Tabs.Count == 0) return;
            m_TabIndex = (m_TabIndex + step + m_Tabs.Count) % m_Tabs.Count;
            SetMessage(null);
            Fill(0);
        }

        public void Select(int index)
        {
            if (Count == 0) return;
            m_Selected = Mathf.Clamp(index, 0, Count - 1);
            if (m_Selected < m_Scroll) m_Scroll = m_Selected;
            if (m_Selected >= m_Scroll + m_Rows.Length) m_Scroll = m_Selected - m_Rows.Length + 1;
            FillRows();
            FillDetail();
        }

        // ------------------------------------------------------------------ actions

        /// <summary>A: place one (from storage, or bought onto the cursor), or lay a finish.</summary>
        public void Primary()
        {
            DecorateMode mode = Mode;
            if (mode == null) return;
            if (CurrentPage == Page.Room)
            {
                FinishDefinition finish = SelectedFinish;
                if (finish == null) return;
                PurchaseProblem problem = mode.Area.State.OwnsFinish(finish.id) ? PurchaseProblem.None : mode.CanBuy(finish);
                if (problem != PurchaseProblem.None) SetMessage(ProblemText(problem, finish.catalogTier));
                else mode.UseFinish(finish);
                Fill(m_Selected);
                return;
            }
            FurnitureDefinition piece = SelectedPiece;
            if (piece == null) return;
            if (mode.Stored(piece.id) > 0)
            {
                Close();
                mode.TakeFromStorage(piece.id);
                return;
            }
            PurchaseProblem cant = mode.CanBuy(piece);
            if (cant != PurchaseProblem.None)
            {
                SetMessage(ProblemText(cant, piece.catalogTier));
                mode.Buy(piece.id);
                return;
            }
            Close();
            mode.Buy(piece.id, place: true);
        }

        /// <summary>X: buy one into storage.</summary>
        public void Secondary()
        {
            DecorateMode mode = Mode;
            FurnitureDefinition piece = SelectedPiece;
            if (mode == null || piece == null) return;
            PurchaseProblem cant = mode.CanBuy(piece);
            if (cant != PurchaseProblem.None) SetMessage(ProblemText(cant, piece.catalogTier));
            else SetMessage(Loc.UI(DecorateLocKeys.Bought, DecorateScreen.PieceName(piece.id)));
            mode.Buy(piece.id);
            Fill(m_Selected);
        }

        /// <summary>Y: sell one from storage.</summary>
        public void Tertiary()
        {
            DecorateMode mode = Mode;
            FurnitureDefinition piece = SelectedPiece;
            if (mode == null || piece == null) return;
            if (mode.Sell(piece.id)) SetMessage(Loc.UI(DecorateLocKeys.Sold, DecorateScreen.PieceName(piece.id)));
            Fill(m_Selected);
        }

        string ProblemText(PurchaseProblem problem, int tier) => problem switch
        {
            PurchaseProblem.Locked => Loc.UI(DecorateLocKeys.Locked, FurnitureShop.RenownFor(tier, Mode.Thresholds)),
            PurchaseProblem.NotEnoughGold => Loc.UI(DecorateLocKeys.CantAfford),
            PurchaseProblem.AlreadyOwned => Loc.UI(DecorateLocKeys.AlreadyOwned),
            _ => Loc.UI(DecorateLocKeys.RowNotSold),
        };

        // ------------------------------------------------------------------ filling

        void Fill(int select)
        {
            DecorateMode mode = Mode;
            if (mode == null || !mode.IsActive) return;
            m_Pieces.Clear();
            m_Finishes.Clear();
            var (page, category) = m_Tabs[m_TabIndex];
            switch (page)
            {
                case Page.Storage:
                    foreach (var (d, _) in mode.Storage()) m_Pieces.Add(d);
                    m_Tab.Set(DecorateLocKeys.TabStorage);
                    break;
                case Page.Category:
                    foreach (FurnitureDefinition d in mode.Catalogue())
                        if (d.category == category && (d.ForSale || mode.Area.State.OwnedCount(d.id) > 0)) m_Pieces.Add(d);
                    m_Tab.Set(DecorateLocKeys.Category(category));
                    break;
                default:
                    foreach (FinishDefinition f in mode.Area.Content.finishes)
                        if (f != null) m_Finishes.Add(f);
                    m_Finishes.Sort((a, b) => a.kind != b.kind ? a.kind.CompareTo(b.kind) : a.catalogTier != b.catalogTier ? a.catalogTier.CompareTo(b.catalogTier) : a.price.CompareTo(b.price));
                    m_Tab.Set(DecorateLocKeys.TabRoom);
                    break;
            }
            m_Purse.Set(DecorateLocKeys.CatalogPurse, mode.Game.Gold, mode.Game.Renown);
            m_Empty.gameObject.SetActive(Count == 0);
            m_Selected = Mathf.Clamp(select, 0, Mathf.Max(0, Count - 1));
            m_Scroll = Mathf.Clamp(m_Scroll, 0, Mathf.Max(0, Count - m_Rows.Length));
            if (m_Selected < m_Scroll || m_Selected >= m_Scroll + m_Rows.Length) m_Scroll = Mathf.Max(0, m_Selected - m_Rows.Length + 1);
            if (m_Selected == 0) m_Scroll = 0;
            FillRows();
            FillDetail();
            FillControls();
        }

        void FillRows()
        {
            DecorateMode mode = Mode;
            for (int i = 0; i < m_Rows.Length; i++)
            {
                CatalogueRow row = m_Rows[i];
                int index = m_Scroll + i;
                bool has = index < Count;
                row.button.gameObject.SetActive(has);
                if (!has) continue;
                row.background.color = index == m_Selected ? m_RowSelected : m_RowPlain;
                Color ink = m_Ink;
                if (CurrentPage == Page.Room)
                {
                    FinishDefinition f = m_Finishes[index];
                    // Floors first, then walls (the detail says which); just the name, so it fits the row.
                    row.name.Set(TavernLocKeys.Plain, Loc.UI(f.nameKey));
                    bool owned = mode.Area.State.OwnsFinish(f.id);
                    if (mode.Finish(f.kind) == f.id) row.info.Set(DecorateLocKeys.RowInUse);
                    else if (owned) row.info.Set(DecorateLocKeys.RowOwned);
                    else if (!DecorateMode.Sandbox && !FurnitureShop.IsOpen(f.catalogTier, mode.Game.Renown, mode.Thresholds))
                    {
                        row.info.Set(DecorateLocKeys.RowLocked, FurnitureShop.RenownFor(f.catalogTier, mode.Thresholds));
                        ink = m_Locked;
                    }
                    else
                    {
                        row.info.Set(DecorateLocKeys.RowPrice, f.price);
                        if (mode.CanBuy(f) == PurchaseProblem.NotEnoughGold) ink = m_Short;
                    }
                }
                else
                {
                    FurnitureDefinition d = m_Pieces[index];
                    row.name.Set(TavernLocKeys.Plain, DecorateScreen.PieceName(d.id));
                    int stored = mode.Stored(d.id);
                    PurchaseProblem problem = mode.CanBuy(d);
                    if (stored > 0) row.info.Set(DecorateLocKeys.RowStored, stored);
                    else if (problem == PurchaseProblem.Locked)
                    {
                        row.info.Set(DecorateLocKeys.RowLocked, FurnitureShop.RenownFor(d.catalogTier, mode.Thresholds));
                        ink = m_Locked;
                    }
                    else if (!d.ForSale) row.info.Set(DecorateLocKeys.RowNotSold);
                    else
                    {
                        row.info.Set(DecorateLocKeys.RowPrice, d.price);
                        if (problem == PurchaseProblem.NotEnoughGold) ink = m_Short;
                    }
                }
                SetColor(row.name, ink);
                SetColor(row.info, ink);
            }
        }

        void FillDetail()
        {
            DecorateMode mode = Mode;
            foreach (Image s in m_Swatches) s.gameObject.SetActive(false);
            m_Primary.gameObject.SetActive(false);
            m_Secondary.gameObject.SetActive(false);
            m_Tertiary.gameObject.SetActive(false);
            bool any = Count > 0;
            foreach (Component c in new Component[] { m_Icon, m_PieceName, m_Description, m_Tier, m_Counts, m_Source }) c.gameObject.SetActive(any);
            if (!any) return;

            if (CurrentPage == Page.Room)
            {
                FinishDefinition f = SelectedFinish;
                SetIcon(f.swatch != null ? f.swatch : null);
                m_PieceName.Set(TavernLocKeys.Plain, Loc.UI(f.nameKey));
                m_Description.Set(f.kind == FinishKind.Floor ? DecorateLocKeys.FloorKind : DecorateLocKeys.WallKind);
                SetTier(f.catalogTier, f.price);
                bool owned = mode.Area.State.OwnsFinish(f.id);
                m_Counts.gameObject.SetActive(owned);
                if (owned) m_Counts.Set(DecorateLocKeys.RowOwned);
                m_Source.Set((f.sources & FurnitureSource.Starter) != 0 ? DecorateLocKeys.SourceStarter : DecorateLocKeys.SourceBought);
                if (mode.Finish(f.kind) != f.id) ShowAction(m_Primary, m_PrimaryLabel, owned ? DecorateLocKeys.ActionUse : DecorateLocKeys.ActionBuyPlace, Prompt(DecorateActions.Select), f.price);
                return;
            }

            FurnitureDefinition d = SelectedPiece;
            SetIcon(d.Icon());
            m_PieceName.Set(TavernLocKeys.Plain, DecorateScreen.PieceName(d.id));
            m_Description.gameObject.SetActive(!string.IsNullOrEmpty(d.descriptionKey));
            if (!string.IsNullOrEmpty(d.descriptionKey)) m_Description.Set(d.descriptionKey);
            SetTier(d.catalogTier, d.ForSale ? d.price : -1);
            int stored = mode.Stored(d.id), placed = mode.Placed(d.id);
            m_Counts.Set(DecorateLocKeys.Counts, stored + placed, placed, stored);
            string source = SourceText(d);
            m_Source.gameObject.SetActive(source.Length > 0);
            if (source.Length > 0) m_Source.Set(TavernLocKeys.Plain, source);

            int looks = d.variants.Count;
            for (int i = 0; i < m_Swatches.Length && i < looks; i++)
            {
                m_Swatches[i].gameObject.SetActive(true);
                m_Swatches[i].color = d.variants[i].swatch;
            }

            PurchaseProblem problem = mode.CanBuy(d);
            if (stored > 0) ShowAction(m_Primary, m_PrimaryLabel, DecorateLocKeys.ActionPlace, Prompt(DecorateActions.Select));
            else if (problem is PurchaseProblem.None or PurchaseProblem.NotEnoughGold)
                ShowAction(m_Primary, m_PrimaryLabel, DecorateLocKeys.ActionBuyPlace, Prompt(DecorateActions.Select), DecorateMode.Sandbox ? 0 : d.price);
            if (d.ForSale && problem is PurchaseProblem.None or PurchaseProblem.NotEnoughGold)
                ShowAction(m_Secondary, m_SecondaryLabel, DecorateLocKeys.ActionBuy, Prompt(DecorateActions.Turn), DecorateMode.Sandbox ? 0 : d.price);
            if (FurnitureShop.CanSell(d, stored)) ShowAction(m_Tertiary, m_TertiaryLabel, DecorateLocKeys.ActionSell, Prompt(DecorateActions.Flip), d.SellPrice);
        }

        /// <summary>The price and the tier: who makes it, or the Renown it needs (D14). A price below zero: not sold.</summary>
        void SetTier(int tier, int price)
        {
            DecorateMode mode = Mode;
            CatalogSettings settings = mode.Area.Content.catalog;
            string tierName = settings != null && tier < settings.tiers.Count && settings.tiers[tier] != null ? Loc.UI(settings.tiers[tier].nameKey) : string.Empty;
            string cost = price < 0 ? Loc.UI(DecorateLocKeys.RowNotSold) : Loc.UI(DecorateLocKeys.RowPrice, price);
            if (!DecorateMode.Sandbox && !FurnitureShop.IsOpen(tier, mode.Game.Renown, mode.Thresholds))
            {
                m_Tier.Set(DecorateLocKeys.NeedsRenown, FurnitureShop.RenownFor(tier, mode.Thresholds), tierName, cost);
                SetColor(m_Tier, m_Short);
            }
            else
            {
                m_Tier.Set(DecorateLocKeys.FromTier, tierName, cost);
                SetColor(m_Tier, m_Ink);
            }
        }

        static string SourceText(FurnitureDefinition d)
        {
            var parts = new List<string>();
            // Bought in Brackenford is the usual: only the notable origins are named (the swatches show the colourways).
            if ((d.sources & FurnitureSource.Starter) != 0) parts.Add(Loc.UI(DecorateLocKeys.SourceStarter));
            else if (!d.ForSale && (d.sources & FurnitureSource.Bought) != 0) parts.Add(Loc.UI(DecorateLocKeys.SourceBought));
            if ((d.sources & FurnitureSource.Discovery) != 0) parts.Add(Loc.UI(DecorateLocKeys.SourceDiscovery));
            if ((d.sources & FurnitureSource.Boss) != 0) parts.Add(Loc.UI(DecorateLocKeys.SourceBoss));
            if ((d.sources & FurnitureSource.Story) != 0) parts.Add(Loc.UI(DecorateLocKeys.SourceStory));
            if (d.unique) parts.Add(Loc.UI(DecorateLocKeys.Unique));
            return string.Join(" · ", parts);
        }

        void SetIcon(Sprite sprite)
        {
            m_Icon.sprite = sprite;
            m_Icon.enabled = sprite != null;
            if (sprite == null) return;
            // Drawn in whole pixels (as the canvas scales): small pieces at double size, large ones at half, to fit the 40-pixel box.
            Vector2 size = sprite.rect.size;
            if (size.x > 40f || size.y > 40f) size *= 0.5f;
            else if (size.x <= 20f && size.y <= 20f) size *= 2f;
            m_Icon.rectTransform.sizeDelta = size;
        }

        void ShowAction(Button button, LocalizedSuperText label, string key, params object[] args)
        {
            button.gameObject.SetActive(true);
            label.Set(key, args);
        }

        void FillControls() => m_Controls.Set(DecorateLocKeys.CatalogControls, Prompt(DecorateActions.Cycle), Prompt(DecorateActions.Cancel));

        string Prompt(string action)
        {
            InputAction a = InputMaps.Find(InputMaps.Decorate, action);
            if (a == null) return string.Empty;
            string shown = a.GetBindingDisplayString(InputBinding.MaskByGroup(m_Gamepad ? "Gamepad" : "Keyboard&Mouse"));
            return string.IsNullOrEmpty(shown) ? a.GetBindingDisplayString() : shown.Split('|')[0].Trim();
        }

        void SetMessage(string text)
        {
            if (m_Message == null) return;
            m_Message.gameObject.SetActive(!string.IsNullOrEmpty(text));
            if (!string.IsNullOrEmpty(text)) m_Message.Set(TavernLocKeys.Plain, text);
        }

        static string Text(LocalizedSuperText text) => text != null ? text.GetComponent<SuperTextMesh>()?.text : null;

        static void SetColor(LocalizedSuperText text, Color color)
        {
            var stm = text.GetComponent<SuperTextMesh>();
            if (stm != null && stm.color != color)
            {
                stm.color = color;
                stm.Rebuild();
            }
        }
    }
}
