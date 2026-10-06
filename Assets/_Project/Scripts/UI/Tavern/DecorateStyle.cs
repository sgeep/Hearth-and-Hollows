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
    /// <summary>One line of the colour panel: what it sets, its swatches, and the choice's name.</summary>
    [Serializable]
    public sealed class StyleRow
    {
        public GameObject root;
        public LocalizedSuperText label;
        public LocalizedSuperText value;
        public Image[] swatches = Array.Empty<Image>();
        public Button[] buttons = Array.Empty<Button>();
        public Image marker;
        /// <summary>The row's band: soft gold behind the chosen row, as in the catalogue.</summary>
        public Image background;
    }

    /// <summary>
    /// The colour panel (4f step 5; D11): one tool for a piece's looks, whether Minifantasy drew them (its colourways, the
    /// "style" row) or they're its own colours remapped to a palette ramp (a row per channel: wood, cushion…). Schemes set
    /// several channels at once; "use the last colours" copies the last look given; "colour every … here" gives every copy
    /// in the room this look. Changes show at once and can be undone. Controller first: up and down choose a row, left and
    /// right a swatch; the mouse clicks swatches. Presentation over <see cref="DecorateMode"/>.
    /// </summary>
    public sealed class DecorateStyle : MonoBehaviour
    {
        enum Kind
        {
            Variant,
            Channel,
            Presets,
            Copy,
            ApplyAll,
        }

        sealed class Line
        {
            public Kind Kind;
            public string Channel;
            public readonly List<(string id, string nameKey, Color swatch)> Options = new();
            public int Current;
        }

        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Title;
        [SerializeField] LocalizedSuperText m_Controls;
        [SerializeField] LocalizedSuperText m_Nothing;
        [SerializeField] StyleRow[] m_Rows = Array.Empty<StyleRow>();
        [SerializeField] Button m_Copy, m_ApplyAll;
        [SerializeField] LocalizedSuperText m_CopyLabel, m_ApplyAllLabel;
        [SerializeField, Tooltip("The band behind the chosen row (the catalogue's selection colour); its text stays ink.")]
        Color m_RowSelected = new(0.95f, 0.8f, 0.45f, 0.55f);
        [SerializeField, Tooltip("A chosen button's tint.")] Color m_ButtonSelected = new(1f, 0.82f, 0.45f);
        [SerializeField] Color m_Ink = new(0.24f, 0.16f, 0.12f);
        [SerializeField, Min(0.05f)] float m_RepeatDelay = 0.3f;
        [SerializeField, Min(0.02f)] float m_RepeatRate = 0.1f;

        readonly List<Line> m_Lines = new();
        int m_Focused;
        int m_OpenedFrame = -1;
        Vector2Int m_Held;
        float m_HeldFor, m_NextRepeat;
        bool m_Gamepad;
        DirectionReader m_MoveReader;
        InputAction m_Move, m_Select, m_Cancel, m_Style, m_Click;
        FurnitureDefinition m_Piece;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public int LineCount => m_Lines.Count;
        public IReadOnlyList<StyleRow> Rows => m_Rows;
        public int Focused => m_Focused;
        public string TitleText => m_Title != null ? m_Title.GetComponent<SuperTextMesh>()?.text : null;

        static DecorateMode Mode => DecorateMode.Instance;

        public void Configure(GameObject root, LocalizedSuperText title, LocalizedSuperText controls, LocalizedSuperText nothing, StyleRow[] rows,
            Button copy, LocalizedSuperText copyLabel, Button applyAll, LocalizedSuperText applyAllLabel)
        {
            m_Root = root;
            m_Title = title;
            m_Controls = controls;
            m_Nothing = nothing;
            m_Rows = rows;
            m_Copy = copy;
            m_CopyLabel = copyLabel;
            m_ApplyAll = applyAll;
            m_ApplyAllLabel = applyAllLabel;
        }

        void OnDestroy() => m_MoveReader?.Dispose();

        void Start()
        {
            for (int r = 0; r < m_Rows.Length; r++)
            for (int s = 0; s < m_Rows[r].buttons.Length; s++)
            {
                int row = r, swatch = s;
                m_Rows[r].buttons[s].onClick.AddListener(() =>
                {
                    m_Focused = row;
                    Choose(row, swatch);
                });
            }
            m_Copy.onClick.AddListener(Copy);
            m_ApplyAll.onClick.AddListener(ApplyAll);
            if (!IsOpen) m_Root.SetActive(false);
        }

        /// <summary>Opens for the piece carried or under the cursor.</summary>
        public void Open()
        {
            DecorateMode mode = Mode;
            PlacedFurniture target = mode != null && mode.IsActive ? mode.StyleTarget : null;
            if (target == null) return;
            m_Piece = mode.Area.Definition(target.definition);
            if (m_Piece == null) return;
            FindActions();
            m_Root.SetActive(true);
            mode.PanelOpen = true;
            m_OpenedFrame = Time.frameCount;
            m_Focused = 0;
            Fill();
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            if (Mode != null) Mode.PanelOpen = false;
        }

        void FindActions()
        {
            InputAction A(string name) => InputMaps.Find(InputMaps.Decorate, name);
            m_Move = A(DecorateActions.Move);
            m_MoveReader?.Dispose();
            m_MoveReader = new DirectionReader(m_Move);
            m_Select = A(DecorateActions.Select);
            m_Cancel = A(DecorateActions.Cancel);
            m_Style = A(DecorateActions.Style);
            m_Click = A(DecorateActions.Click);
        }

        static bool Pressed(InputAction a) => a != null && a.WasPressedThisFrame();

        void Update()
        {
            if (!IsOpen) return;
            DecorateMode mode = Mode;
            if (mode == null || !mode.IsActive || mode.StyleTarget == null)
            {
                Close();
                return;
            }
            if (Time.frameCount == m_OpenedFrame)
            {
                m_MoveReader?.Clear();
                return;
            }
            if (Pressed(m_Cancel) || Pressed(m_Style))
            {
                Close();
                return;
            }
            if (Pressed(m_Select) && m_Lines.Count > 0)
            {
                if (m_Lines[m_Focused].Kind == Kind.Copy) Copy();
                else if (m_Lines[m_Focused].Kind == Kind.ApplyAll) ApplyAll();
            }
            Vector2 move = m_MoveReader != null ? m_MoveReader.Read() : Vector2.zero;
            var step = new Vector2Int(Mathf.Abs(move.x) > 0.5f ? (int)Mathf.Sign(move.x) : 0, Mathf.Abs(move.y) > 0.5f ? (int)Mathf.Sign(move.y) : 0);
            if (step.x != 0 && step.y != 0) step.y = 0;
            if (step == Vector2Int.zero)
            {
                m_Held = Vector2Int.zero;
                return;
            }
            bool fire;
            if (step != m_Held)
            {
                m_Held = step;
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
            if (step.y != 0) Focus(m_Focused - step.y);
            else Step(step.x);
        }

        void LateUpdate()
        {
            if (!IsOpen) return;
            bool pad = Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                                                    Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f);
            bool keys = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (pad && !m_Gamepad || keys && m_Gamepad)
            {
                m_Gamepad = !m_Gamepad;
                FillControls();
            }
        }

        public void Focus(int line)
        {
            if (m_Lines.Count == 0) return;
            m_Focused = (line + m_Lines.Count) % m_Lines.Count;
            Paint();
        }

        /// <summary>Left or right on the focused line: the next swatch or scheme, shown at once.</summary>
        public void Step(int by)
        {
            if (m_Lines.Count == 0) return;
            Line line = m_Lines[m_Focused];
            if (line.Options.Count == 0) return;
            int next = line.Current < 0 ? (by > 0 ? 0 : line.Options.Count - 1) : (line.Current + by + line.Options.Count) % line.Options.Count;
            Choose(m_Focused, next);
        }

        /// <summary>Picks option <paramref name="option"/> of line <paramref name="index"/> (a swatch clicked, or stepped to).</summary>
        public void Choose(int index, int option)
        {
            DecorateMode mode = Mode;
            PlacedFurniture piece = mode?.StyleTarget;
            if (piece == null || index >= m_Lines.Count) return;
            Line line = m_Lines[index];
            if (option < 0 || option >= line.Options.Count) return;
            string variant = piece.variant, palette = piece.palette;
            switch (line.Kind)
            {
                case Kind.Variant:
                    variant = line.Options[option].id;
                    break;
                case Kind.Channel:
                    palette = FurniturePalette.With(palette, line.Channel, line.Options[option].id);
                    break;
                case Kind.Presets:
                    PalettePreset preset = mode.Area.Palettes.presets.Find(p => p.id == line.Options[option].id);
                    if (preset != null)
                        foreach (PalettePick pick in preset.picks)
                            palette = FurniturePalette.With(palette, pick.kind, pick.ramp);
                    break;
                default:
                    return;
            }
            mode.SetLook(variant, palette);
            Fill();
        }

        public void Copy()
        {
            if (Mode == null || !Mode.CopyLastLook()) return;
            Fill();
        }

        public void ApplyAll()
        {
            if (Mode == null) return;
            Mode.ApplyLookToAll();
            Fill();
        }

        // ------------------------------------------------------------------ filling

        void Fill()
        {
            DecorateMode mode = Mode;
            PlacedFurniture piece = mode.StyleTarget;
            if (piece == null) return;
            m_Piece = mode.Area.Definition(piece.definition);
            m_Lines.Clear();
            PaletteLibrary library = mode.Area.Palettes;
            if (m_Piece.variants.Count > 1)
            {
                var line = new Line { Kind = Kind.Variant, Current = m_Piece.VariantIndex(piece.variant) };
                foreach (FurnitureVariant v in m_Piece.variants) line.Options.Add((v.id, v.nameKey, v.swatch));
                m_Lines.Add(line);
            }
            Dictionary<string, string> picks = FurniturePalette.Parse(piece.palette);
            if (library != null)
            {
                foreach (PaletteChannel channel in m_Piece.paletteChannels)
                {
                    var line = new Line { Kind = Kind.Channel, Channel = channel.kind };
                    Color32[] drawn = FurniturePalette.ByBrightness(channel.source);
                    line.Options.Add((null, DecorateLocKeys.StyleAsDrawn, drawn.Length > 0 ? (Color)drawn[drawn.Length / 2] : Color.white));
                    foreach (PaletteRamp ramp in library.For(channel.kind)) line.Options.Add((ramp.id, ramp.nameKey, ramp.Swatch));
                    picks.TryGetValue(channel.kind, out string chosen);
                    line.Current = Mathf.Max(0, line.Options.FindIndex(o => o.id == chosen));
                    m_Lines.Add(line);
                }
                List<PalettePreset> presets = library.PresetsFor(m_Piece.paletteChannels);
                if (presets.Count > 0)
                {
                    var line = new Line { Kind = Kind.Presets, Current = -1 };
                    foreach (PalettePreset p in presets) line.Options.Add((p.id, p.nameKey, Color.white));
                    line.Current = presets.FindIndex(p => p.picks.TrueForAll(pick => !m_Piece.paletteChannels.Exists(c => c.kind == pick.kind) ||
                                                                                       picks.TryGetValue(pick.kind, out string r) && r == pick.ramp));
                    m_Lines.Add(line);
                }
            }
            if (m_Piece.HasLooks)
            {
                m_Lines.Add(new Line { Kind = Kind.Copy });
                m_Lines.Add(new Line { Kind = Kind.ApplyAll });
            }
            m_Focused = Mathf.Clamp(m_Focused, 0, Mathf.Max(0, m_Lines.Count - 1));
            m_Title.Set(DecorateLocKeys.StyleTitle, DecorateScreen.PieceName(m_Piece.id));
            m_Nothing.gameObject.SetActive(!m_Piece.HasLooks);
            if (!m_Piece.HasLooks) m_Nothing.Set(DecorateLocKeys.StyleNothing, DecorateScreen.PieceName(m_Piece.id));
            Paint();
            FillControls();
        }

        /// <summary>The rows from the lines: swatches for colourways and channels, a name for schemes, buttons for the actions.</summary>
        void Paint()
        {
            int row = 0;
            m_Copy.gameObject.SetActive(false);
            m_ApplyAll.gameObject.SetActive(false);
            for (int i = 0; i < m_Lines.Count; i++)
            {
                Line line = m_Lines[i];
                bool focused = i == m_Focused;
                if (line.Kind == Kind.Copy)
                {
                    m_Copy.gameObject.SetActive(true);
                    m_CopyLabel.Set(DecorateLocKeys.StyleCopy);
                    SetColor(m_CopyLabel, m_Ink);
                    Tint(m_Copy, focused);
                    m_Copy.interactable = Mode.LastLook != null;
                    continue;
                }
                if (line.Kind == Kind.ApplyAll)
                {
                    m_ApplyAll.gameObject.SetActive(true);
                    m_ApplyAllLabel.Set(DecorateLocKeys.StyleApplyAll, DecorateScreen.PieceName(m_Piece.id));
                    SetColor(m_ApplyAllLabel, m_Ink);
                    Tint(m_ApplyAll, focused);
                    continue;
                }
                if (row >= m_Rows.Length) continue;
                StyleRow r = m_Rows[row++];
                r.root.SetActive(true);
                string label = line.Kind switch
                {
                    Kind.Variant => DecorateLocKeys.StyleVariant,
                    Kind.Presets => DecorateLocKeys.StylePresets,
                    _ => DecorateLocKeys.Channel(line.Channel),
                };
                string chosen = line.Current >= 0 && line.Current < line.Options.Count && line.Options[line.Current].nameKey != null
                    ? Loc.UI(line.Options[line.Current].nameKey) : string.Empty;
                r.label.Set(label);
                r.value.gameObject.SetActive(chosen.Length > 0);
                if (chosen.Length > 0) r.value.Set(TavernLocKeys.Plain, chosen);
                SetColor(r.label, m_Ink);
                if (r.background != null) r.background.color = focused ? m_RowSelected : Color.clear;
                bool swatches = line.Kind != Kind.Presets;
                for (int s = 0; s < r.swatches.Length; s++)
                {
                    bool has = swatches && s < line.Options.Count;
                    r.buttons[s].gameObject.SetActive(has);
                    if (has) r.swatches[s].color = line.Options[s].swatch;
                }
                bool marked = swatches && line.Current >= 0 && line.Current < r.swatches.Length;
                r.marker.gameObject.SetActive(marked);
                if (marked)
                {
                    var at = (RectTransform)r.buttons[line.Current].transform;
                    r.marker.rectTransform.anchoredPosition = at.anchoredPosition + new Vector2(0f, at.sizeDelta.y * 0.5f + 1f);
                }
            }
            for (; row < m_Rows.Length; row++) m_Rows[row].root.SetActive(false);
        }

        void FillControls()
        {
            string Prompt(string action)
            {
                InputAction a = InputMaps.Find(InputMaps.Decorate, action);
                if (a == null) return string.Empty;
                string shown = a.GetBindingDisplayString(InputBinding.MaskByGroup(m_Gamepad ? "Gamepad" : "Keyboard&Mouse"));
                return string.IsNullOrEmpty(shown) ? a.GetBindingDisplayString() : shown.Split('|')[0].Trim();
            }
            m_Controls.Set(DecorateLocKeys.StyleControls, Prompt(DecorateActions.Move), Prompt(DecorateActions.Select), Prompt(DecorateActions.Cancel));
        }

        readonly Dictionary<Button, Color> m_ButtonColors = new();

        /// <summary>A chosen button takes the selection tint; otherwise it keeps its own color.</summary>
        void Tint(Button button, bool focused)
        {
            if (button.targetGraphic == null) return;
            if (!m_ButtonColors.TryGetValue(button, out Color own)) m_ButtonColors[button] = own = button.targetGraphic.color;
            button.targetGraphic.color = focused ? m_ButtonSelected : own;
        }

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
