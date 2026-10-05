using System;
using System.Collections.Generic;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// Decorate Mode's screen (4f step 2): the room stays in view, with a strip at the top (what you're doing, and
    /// whether the doors could open), a line naming the piece under the cursor or carried (and why it won't go where
    /// it's held), the controls for the device in use at the bottom, and two panels: storage (take a piece out) and
    /// the layout check (each problem in words, put it all back, done). Reads <see cref="DecorateMode"/>; presentation only.
    /// </summary>
    public sealed class DecorateScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Status;
        [SerializeField] LocalizedSuperText m_Piece;
        [SerializeField] LocalizedSuperText m_Controls;
        [SerializeField] GameObject m_Storage;
        [SerializeField] Button[] m_StorageRows = Array.Empty<Button>();
        [SerializeField] LocalizedSuperText[] m_StorageLabels = Array.Empty<LocalizedSuperText>();
        [SerializeField] LocalizedSuperText m_StorageEmpty;
        [SerializeField] GameObject m_Check;
        [SerializeField] LocalizedSuperText[] m_Issues = Array.Empty<LocalizedSuperText>();
        [SerializeField] Button m_PutAllBack;
        [SerializeField] Button m_Done;
        [SerializeField] Color m_Ready = new(0.55f, 0.9f, 0.5f);
        [SerializeField] Color m_Problem = new(1f, 0.55f, 0.35f);
        [SerializeField] Color m_Plain = new(0.95f, 0.92f, 0.85f);
        [SerializeField] Color m_Warning = new(1f, 0.85f, 0.45f);

        DecorateMode m_Mode;
        readonly List<string> m_StorageIds = new();
        bool m_Gamepad;
        InputAction m_UiCancel;

        public static bool IsDecorating => DecorateMode.Instance != null && DecorateMode.Instance.IsActive;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public bool StorageOpen => m_Storage != null && m_Storage.activeSelf;
        public bool CheckOpen => m_Check != null && m_Check.activeSelf;
        public IReadOnlyList<Button> StorageRows => m_StorageRows;
        public string PieceText => m_Piece != null ? m_Piece.GetComponent<SuperTextMesh>()?.text : null;
        public string StatusText => m_Status != null ? m_Status.GetComponent<SuperTextMesh>()?.text : null;

        public void Configure(GameObject root, LocalizedSuperText status, LocalizedSuperText piece, LocalizedSuperText controls, GameObject storage,
            Button[] storageRows, LocalizedSuperText[] storageLabels, LocalizedSuperText storageEmpty, GameObject check, LocalizedSuperText[] issues,
            Button putAllBack, Button done)
        {
            m_Root = root;
            m_Status = status;
            m_Piece = piece;
            m_Controls = controls;
            m_Storage = storage;
            m_StorageRows = storageRows;
            m_StorageLabels = storageLabels;
            m_StorageEmpty = storageEmpty;
            m_Check = check;
            m_Issues = issues;
            m_PutAllBack = putAllBack;
            m_Done = done;
        }

        void Start()
        {
            m_Root.SetActive(false);
            m_Storage.SetActive(false);
            m_Check.SetActive(false);
            m_UiCancel = InputMaps.Find(InputMaps.UI, "Cancel");
            for (int i = 0; i < m_StorageRows.Length; i++)
            {
                int row = i;
                m_StorageRows[i].onClick.AddListener(() => TakeOut(row));
            }
            m_PutAllBack.onClick.AddListener(() =>
            {
                m_Mode?.PutAllBack();
                ClosePanels();
            });
            m_Done.onClick.AddListener(() =>
            {
                ClosePanels();
                m_Mode?.Leave();
            });
        }

        DecorateMode Mode()
        {
            if (m_Mode != null) return m_Mode;
            m_Mode = DecorateMode.Instance;
            if (m_Mode == null) return null;
            m_Mode.Changed += Refresh;
            m_Mode.StorageRequested += OpenStorage;
            m_Mode.CheckRequested += OpenCheck;
            return m_Mode;
        }

        void OnDestroy()
        {
            if (m_Mode == null) return;
            m_Mode.Changed -= Refresh;
            m_Mode.StorageRequested -= OpenStorage;
            m_Mode.CheckRequested -= OpenCheck;
        }

        void LateUpdate()
        {
            DecorateMode mode = Mode();
            bool shown = mode != null && mode.IsActive;
            if (shown != m_Root.activeSelf)
            {
                m_Root.SetActive(shown);
                if (shown)
                {
                    // Nothing selected underneath (the daytime or night panel's buttons): A and E belong to the room now.
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                    Refresh();
                }
                else ClosePanels();
            }
            if (!shown) return;
            if ((StorageOpen || CheckOpen) && m_UiCancel != null && m_UiCancel.WasPressedThisFrame())
            {
                ClosePanels();
                return;
            }
            bool gamepad = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
            bool keyboard = (Keyboard.current != null && Keyboard.current.wasUpdatedThisFrame) || (Mouse.current != null && Mouse.current.wasUpdatedThisFrame);
            if (gamepad && !m_Gamepad || keyboard && m_Gamepad)
            {
                m_Gamepad = gamepad;
                RefreshControls();
            }
        }

        void Refresh()
        {
            DecorateMode mode = Mode();
            if (mode == null || !mode.IsActive || m_Root == null || !m_Root.activeSelf) return;
            LayoutReport report = mode.Report;
            int blocking = 0, warnings = 0;
            foreach (LayoutIssue issue in report.Issues)
                if (issue.Blocking) blocking++;
                else warnings++;
            if (blocking > 0) m_Status.Set(blocking == 1 ? DecorateLocKeys.StatusProblem : DecorateLocKeys.StatusProblems, blocking);
            else if (warnings > 0) m_Status.Set(DecorateLocKeys.StatusWarnings, warnings);
            else m_Status.Set(DecorateLocKeys.StatusReady);
            SetColor(m_Status, blocking > 0 ? m_Problem : warnings > 0 ? m_Warning : m_Ready);

            if (mode.Carried != null)
            {
                string name = PieceName(mode.Carried.definition);
                if (mode.CarriedCheck.IsValid)
                {
                    m_Piece.Set(DecorateLocKeys.Carrying, name);
                    SetColor(m_Piece, m_Plain);
                }
                else
                {
                    m_Piece.Set(DecorateLocKeys.CantGo, name, Loc.UI(ProblemKey(mode.CarriedCheck.Problem)));
                    SetColor(m_Piece, m_Problem);
                }
            }
            else
            {
                List<PlacedFurniture> here = mode.Hovered();
                PlacedFurniture piece = mode.HoveredPiece;
                if (piece == null) m_Piece.Set(DecorateLocKeys.Empty);
                else if (here.Count > 1) m_Piece.Set(DecorateLocKeys.HoverMore, PieceName(piece.definition), here.Count - 1);
                else m_Piece.Set(DecorateLocKeys.Hover, PieceName(piece.definition));
                SetColor(m_Piece, m_Plain);
            }
            RefreshControls();
            if (CheckOpen) FillCheck();
            if (StorageOpen) FillStorage();
        }

        void RefreshControls()
        {
            DecorateMode mode = Mode();
            if (mode == null || m_Controls == null) return;
            string group = m_Gamepad ? "Gamepad" : "Keyboard&Mouse";
            string B(string action)
            {
                InputAction a = InputMaps.Find(InputMaps.Decorate, action);
                if (a == null) return string.Empty;
                string shown = a.GetBindingDisplayString(InputBinding.MaskByGroup(group));
                return string.IsNullOrEmpty(shown) ? a.GetBindingDisplayString() : shown.Split('|')[0].Trim();
            }
            if (mode.Carried != null)
                m_Controls.Set(DecorateLocKeys.CarryControls, B(DecorateActions.Select), B(DecorateActions.Turn), B(DecorateActions.Flip),
                    B(DecorateActions.Store), B(DecorateActions.Cancel));
            else
                m_Controls.Set(DecorateLocKeys.Controls, B(DecorateActions.Select), B(DecorateActions.Turn), B(DecorateActions.Flip), B(DecorateActions.Store),
                    B(DecorateActions.Undo), B(DecorateActions.Storage), B(DecorateActions.Check), B(DecorateActions.Cancel));
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

        public static string PieceName(string definition) => Loc.UI(DecorateLocKeys.Furniture(definition));

        public static string ProblemKey(PlacementProblem problem) => problem switch
        {
            PlacementProblem.OutsideTheRoom => DecorateLocKeys.ProblemOutside,
            PlacementProblem.NotOnTheWall => DecorateLocKeys.ProblemWall,
            PlacementProblem.NotAgainstTheBackWall => DecorateLocKeys.ProblemBackWall,
            PlacementProblem.Overlaps => DecorateLocKeys.ProblemOverlaps,
            PlacementProblem.BlocksTheEntrance => DecorateLocKeys.ProblemEntrance,
            PlacementProblem.NeedsASurface => DecorateLocKeys.ProblemSurface,
            _ => DecorateLocKeys.ProblemTurn,
        };

        static string StationName(StationKind station) => Loc.UI(station switch
        {
            StationKind.Grill => DecorateLocKeys.StationGrill,
            StationKind.Tap => DecorateLocKeys.StationTap,
            StationKind.StewPot => DecorateLocKeys.StationStewPot,
            _ => DecorateLocKeys.StationButcherBlock,
        });

        /// <summary>A layout problem in words ("the Grill can't be reached").</summary>
        public static string IssueText(LayoutIssue issue) => issue.Kind switch
        {
            LayoutIssueKind.EntranceBlocked => Loc.UI(DecorateLocKeys.IssueEntrance),
            LayoutIssueKind.StationMissing => Loc.UI(DecorateLocKeys.IssueStationMissing, StationName(issue.Station)),
            LayoutIssueKind.StationUnreachable => Loc.UI(DecorateLocKeys.IssueStationUnreachable, StationName(issue.Station)),
            LayoutIssueKind.PassMissing => Loc.UI(DecorateLocKeys.IssuePassMissing),
            LayoutIssueKind.PassUnreachable => Loc.UI(DecorateLocKeys.IssuePassUnreachable),
            LayoutIssueKind.NoSeats => Loc.UI(DecorateLocKeys.IssueNoSeats),
            LayoutIssueKind.SeatsUnreachable => issue.Count == 1 ? Loc.UI(DecorateLocKeys.IssueSeatUnreachable) : Loc.UI(DecorateLocKeys.IssueSeatsUnreachable, issue.Count),
            LayoutIssueKind.StaffCantReach => Loc.UI(DecorateLocKeys.IssueStaff),
            _ => Loc.UI(DecorateLocKeys.IssueQueue),
        };

        // ------------------------------------------------------------------ panels

        public void OpenStorage()
        {
            DecorateMode mode = Mode();
            if (mode == null || !mode.IsActive || mode.Carried != null) return;
            m_Check.SetActive(false);
            m_Storage.SetActive(true);
            mode.PanelOpen = true;
            FillStorage();
            Button first = Array.Find(m_StorageRows, b => b.gameObject.activeSelf);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first != null ? first.gameObject : null);
        }

        void FillStorage()
        {
            List<(FurnitureDefinition definition, int count)> stored = m_Mode.Storage();
            m_StorageIds.Clear();
            for (int i = 0; i < m_StorageRows.Length; i++)
            {
                bool has = i < stored.Count;
                m_StorageRows[i].gameObject.SetActive(has);
                if (!has) continue;
                m_StorageIds.Add(stored[i].definition.id);
                m_StorageLabels[i].Set(DecorateLocKeys.StorageRow, PieceName(stored[i].definition.id), stored[i].count);
            }
            m_StorageEmpty.gameObject.SetActive(stored.Count == 0);
        }

        void TakeOut(int row)
        {
            if (row >= m_StorageIds.Count) return;
            string id = m_StorageIds[row];
            ClosePanels();
            m_Mode?.TakeFromStorage(id);
        }

        public void OpenCheck()
        {
            DecorateMode mode = Mode();
            if (mode == null || !mode.IsActive) return;
            m_Storage.SetActive(false);
            m_Check.SetActive(true);
            mode.PanelOpen = true;
            FillCheck();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(m_Done.gameObject);
        }

        void FillCheck()
        {
            List<LayoutIssue> issues = m_Mode.Report.Issues;
            for (int i = 0; i < m_Issues.Length; i++)
            {
                bool has = i < issues.Count || (i == 0 && issues.Count == 0);
                m_Issues[i].gameObject.SetActive(has);
                if (!has) continue;
                if (issues.Count == 0)
                {
                    m_Issues[i].Set(DecorateLocKeys.CheckClear);
                    SetColor(m_Issues[i], new Color(0.25f, 0.45f, 0.2f));
                    continue;
                }
                m_Issues[i].Set(TavernLocKeys.Plain, IssueText(issues[i]));
                SetColor(m_Issues[i], issues[i].Blocking ? new Color(0.6f, 0.15f, 0.1f) : new Color(0.45f, 0.3f, 0.2f));
            }
        }

        void ClosePanels()
        {
            if (m_Storage != null) m_Storage.SetActive(false);
            if (m_Check != null) m_Check.SetActive(false);
            if (m_Mode != null) m_Mode.PanelOpen = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
