using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Progression;
using UpgradeRules = Hearthdelve.Shared.Progression.Upgrades;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>One upgrade on the Night screen.</summary>
    [Serializable]
    public sealed class UpgradeRow
    {
        public GameObject root;
        public LocalizedSuperText name;
        public LocalizedSuperText effect;
        public Button buy;
        public LocalizedSuperText cost;
    }

    /// <summary>
    /// Night (GDD §3.1, day loop): how the day went (the delve, what came home, the evening, what was banked),
    /// the purse, the upgrades bought with banked gold (each showing what its next level does), and Sleep. The day
    /// was saved as night fell, and is again after each purchase; a short note says so.
    /// </summary>
    public sealed class NightScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Title;
        [SerializeField] LocalizedSuperText[] m_SummaryLabels = Array.Empty<LocalizedSuperText>();
        [SerializeField, Tooltip("Each summary line's value, beside its label.")] LocalizedSuperText[] m_Summary = Array.Empty<LocalizedSuperText>();
        [SerializeField] LocalizedSuperText m_Purse;
        [SerializeField] UpgradeRow[] m_Upgrades = Array.Empty<UpgradeRow>();
        [SerializeField] LocalizedSuperText m_Saved;
        [SerializeField] Button m_Sleep;
        [SerializeField, Min(0f), Tooltip("Seconds the saved note stays up.")] float m_SavedSeconds = 2.5f;
        [SerializeField, Min(0.01f), Tooltip("Seconds a bought row glows.")] float m_BoughtSeconds = 0.6f;
        [SerializeField] Color m_BoughtColour = new(1f, 0.85f, 0.4f);

        float[] m_Glow = Array.Empty<float>();
        Color m_RowColour;

        TavernDirector m_Director;
        GameFlow m_Flow;
        readonly List<TavernUpgradeDefinition> m_Defs = new();
        float m_SavedUntil;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<UpgradeRow> Upgrades => m_Upgrades;
        public IReadOnlyList<TavernUpgradeDefinition> Definitions => m_Defs;
        public Button SleepButton => m_Sleep;
        public bool SavedNoteShown => m_Saved != null && m_Saved.gameObject.activeSelf;

        public void Configure(GameObject root, LocalizedSuperText title, LocalizedSuperText[] summaryLabels, LocalizedSuperText[] summary, LocalizedSuperText purse,
            UpgradeRow[] upgrades, LocalizedSuperText saved, Button sleep)
        {
            m_SummaryLabels = summaryLabels;
            m_Root = root;
            m_Title = title;
            m_Summary = summary;
            m_Purse = purse;
            m_Upgrades = upgrades;
            m_Saved = saved;
            m_Sleep = sleep;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            m_Root.SetActive(false);
            if (m_Director == null || !m_Director.InDayLoop) return;
            m_Flow = m_Director.Flow;
            if (m_Flow.Database != null)
                foreach (TavernUpgradeDefinition upgrade in m_Flow.Database.upgrades)
                    if (upgrade != null && m_Defs.Count < m_Upgrades.Length) m_Defs.Add(upgrade);
            for (int i = 0; i < m_Upgrades.Length; i++)
            {
                int index = i;
                m_Upgrades[i].buy.onClick.AddListener(() => Buy(index));
            }
            m_Sleep.onClick.AddListener(() => m_Director.Sleep());
            m_Director.PhaseChanged += OnPhase;
            m_Flow.StateChanged += Refresh;
            OnPhase();
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= OnPhase;
            if (m_Flow != null) m_Flow.StateChanged -= Refresh;
        }

        public void Buy(int row)
        {
            if (row >= m_Defs.Count || !m_Director.BuyUpgrade(m_Defs[row])) return;
            // Bought: the row glows, a chime, a gentle pulse, and the saved note.
            UiFeedback.Play(UiMoment.Buy);
            if (row < m_Glow.Length) m_Glow[row] = m_BoughtSeconds;
            ShowSaved();
            // Keep the selection on something that still works (a maxed or unaffordable row's button goes away or greys out).
            if (EventSystem.current != null && !m_Upgrades[row].buy.interactable) EventSystem.current.SetSelectedGameObject(m_Sleep.gameObject);
        }

        void OnPhase()
        {
            bool shown = m_Director.Phase == TavernPhase.Night;
            bool was = m_Root.activeSelf;
            m_Root.SetActive(shown);
            if (!shown) return;
            Refresh();
            if (!was)
            {
                // Night falls: the day is saved (GameFlow did it on the way here).
                ShowSaved();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(m_Sleep.gameObject);
            }
        }

        void ShowSaved()
        {
            m_SavedUntil = Time.unscaledTime + m_SavedSeconds;
            m_Saved.gameObject.SetActive(true);
            m_Saved.Set(LoopLocKeys.NightSaved);
        }

        /// <summary>A row is glowing from a purchase (tests).</summary>
        public bool IsGlowing(int row) => row < m_Glow.Length && m_Glow[row] > 0f;

        void Update()
        {
            if (m_Saved != null && m_Saved.gameObject.activeSelf && Time.unscaledTime >= m_SavedUntil) m_Saved.gameObject.SetActive(false);
            if (m_Glow.Length != m_Upgrades.Length)
            {
                m_Glow = new float[m_Upgrades.Length];
                if (m_Upgrades.Length > 0 && m_Upgrades[0].root.TryGetComponent(out Image first)) m_RowColour = first.color;
            }
            for (int i = 0; i < m_Glow.Length; i++)
            {
                if (!m_Upgrades[i].root.TryGetComponent(out Image back)) continue;
                m_Glow[i] = Mathf.Max(0f, m_Glow[i] - Time.unscaledDeltaTime);
                back.color = Color.Lerp(m_RowColour, m_BoughtColour, m_Glow[i] / m_BoughtSeconds);
            }
        }

        void Refresh()
        {
            if (m_Flow == null || !m_Root.activeSelf) return;
            GameState state = m_Flow.State;
            DaySummary today = state.Today;
            m_Title.Set(LoopLocKeys.NightTitle, state.Day);

            string outcome = Loc.UI(today.Delve switch
            {
                DelveOutcome.Extracted => LoopLocKeys.SummaryExtracted,
                DelveOutcome.Died => LoopLocKeys.SummaryDied,
                _ => LoopLocKeys.SummarySkipped,
            });
            bool shut = m_Director.Report != null && m_Director.Report.StayedShut;
            string change = today.RenownChange > 0 ? $"+{today.RenownChange}" : today.RenownChange.ToString();
            // Every played day reaches Night through a delve. A Night with no delve on record was resumed from a save,
            // which keeps the purse and Renown but not the day's story: show only what's known, not zeros.
            bool recorded = today.Delve != DelveOutcome.None;
            var lines = new (string label, string value, object[] args)[]
            {
                // The day: the delve, what came home, the evening.
                (LoopLocKeys.SummaryDelve, TavernLocKeys.Plain, new object[] { outcome }),
                (LoopLocKeys.SummaryParts, LoopLocKeys.SummaryPartsValue, new object[] { today.PartsBroughtBack, today.PartsLost }),
                shut ? (LoopLocKeys.SummaryEvening, LoopLocKeys.SummaryShut, Array.Empty<object>())
                     : (LoopLocKeys.SummaryDishes, LoopLocKeys.SummaryDishesValue, new object[] { today.DishesServed, today.Walkouts }),
                // The money and standing: tonight's takings, the purse, Renown and today's change.
                (LoopLocKeys.NightBanked, TavernLocKeys.PrepValue, new object[] { today.Earned }),
                (LoopLocKeys.NightPurse, TavernLocKeys.PrepValue, new object[] { state.Gold }),
                recorded ? (LoopLocKeys.NightRenownToday, LoopLocKeys.NightRenownValue, new object[] { state.Renown, change })
                         : (LoopLocKeys.NightRenownToday, TavernLocKeys.Plain, new object[] { state.Renown }),
            };
            for (int i = 0; i < m_Summary.Length; i++)
            {
                // Unrecorded: the day (0-2) and tonight's takings (3) are unknown.
                bool has = i < lines.Length && (recorded || i >= 4);
                m_Summary[i].gameObject.SetActive(has);
                if (i < m_SummaryLabels.Length) m_SummaryLabels[i].gameObject.SetActive(has);
                if (!has) continue;
                if (i < m_SummaryLabels.Length) m_SummaryLabels[i].Set(lines[i].label);
                m_Summary[i].Set(lines[i].value, lines[i].args);
            }
            if (m_Purse != null) m_Purse.Set(LoopLocKeys.NightPurse, state.Gold, state.Renown);

            for (int i = 0; i < m_Upgrades.Length; i++)
            {
                UpgradeRow row = m_Upgrades[i];
                bool has = i < m_Defs.Count;
                row.root.SetActive(has);
                if (!has) continue;
                TavernUpgradeDefinition upgrade = m_Defs[i];
                int level = state.UpgradeLevel(upgrade.id);
                bool maxed = UpgradeRules.IsMaxed(upgrade, level);
                row.name.Set(LoopLocKeys.NightUpgradeLevel, Loc.Get(upgrade.displayName), level, upgrade.MaxLevel);
                if (maxed) row.effect.Set(LoopLocKeys.NightMaxed);
                else row.effect.Set(LoopLocKeys.NightNextTime, Effect(upgrade, level));
                row.buy.gameObject.SetActive(!maxed);
                if (!maxed)
                {
                    row.cost.Set(LoopLocKeys.NightBuyCost, UpgradeRules.NextCost(upgrade, level));
                    row.buy.interactable = UpgradeRules.CanBuy(upgrade, level, state.Gold);
                }
            }
        }

        /// <summary>What the next level adds: "+1 satchel slot", "+20 max Essence", "+1 seat".</summary>
        public static string Effect(TavernUpgradeDefinition upgrade, int level)
        {
            float amount = level < upgrade.MaxLevel ? upgrade.levels[level].amount : 0f;
            int rounded = Mathf.RoundToInt(amount);
            return Loc.UI(upgrade.kind switch
            {
                UpgradeKind.SatchelSlots => LoopLocKeys.UpgradeSatchel,
                UpgradeKind.MaxEssence => LoopLocKeys.UpgradeEssence,
                _ => LoopLocKeys.UpgradeSeats,
            }, rounded);
        }
    }
}
