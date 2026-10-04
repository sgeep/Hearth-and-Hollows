using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The power room's choice (4d step 4): three cards, each a power's icon, name and what it does. Choosing one keeps
    /// it for the rest of the delve; Cancel backs out and leaves the spark on the floor. Keyboard, controller and mouse
    /// all work. Gameplay pauses it and switches input; this only shows the choice and answers through the request.
    /// </summary>
    public sealed class RunPowerScreen : MonoBehaviour
    {
        [Serializable]
        public sealed class Card
        {
            public Button button;
            public Image icon;
            public LocalizedSuperText name;
            public LocalizedSuperText description;
        }

        [SerializeField] GameObject m_Panel;
        [SerializeField] Card[] m_Cards = Array.Empty<Card>();

        IReadOnlyList<RunPowerDefinition> m_Options = Array.Empty<RunPowerDefinition>();
        Action<RunPowerDefinition> m_OnChosen;

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;
        public IReadOnlyList<RunPowerDefinition> Options => m_Options;
        public Card[] Cards => m_Cards;

        public void Configure(GameObject panel, Card[] cards)
        {
            m_Panel = panel;
            m_Cards = cards;
        }

        void Awake()
        {
            for (int i = 0; i < m_Cards.Length; i++)
            {
                int index = i;
                m_Cards[i].button.onClick.AddListener(() => Choose(index));
            }
            if (m_Panel != null) m_Panel.SetActive(false);
        }

        void OnEnable() => EventBus<RunPowerOfferRequested>.Subscribe(Open);
        void OnDisable() => EventBus<RunPowerOfferRequested>.Unsubscribe(Open);

        void Open(RunPowerOfferRequested request)
        {
            m_Options = request.Options;
            m_OnChosen = request.OnChosen;
            Card first = null;
            for (int i = 0; i < m_Cards.Length; i++)
            {
                Card card = m_Cards[i];
                bool shown = i < m_Options.Count;
                card.button.gameObject.SetActive(shown);
                if (!shown) continue;
                first ??= card;
                RunPowerDefinition power = m_Options[i];
                card.icon.sprite = power.icon;
                card.icon.enabled = power.icon != null;
                card.name.Set(LocKeys.PowerName(power.id));
                card.description.Set(LocKeys.PowerDescription(power.id), RunPowers.ShownAmount(power));
            }
            m_Panel.SetActive(true);
            if (EventSystem.current != null && first != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(first.button.gameObject);
            }
        }

        void Update()
        {
            if (!IsOpen) return;
            InputAction cancel = InputMaps.Find(InputMaps.UI, "Cancel");
            if (cancel != null && cancel.WasPressedThisFrame()) Choose(-1);
        }

        /// <summary>Answers the offer: a card's index, or -1 to leave the spark for now.</summary>
        public void Choose(int index)
        {
            if (!IsOpen) return;
            m_Panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            RunPowerDefinition chosen = index >= 0 && index < m_Options.Count ? m_Options[index] : null;
            Action<RunPowerDefinition> answer = m_OnChosen;
            m_OnChosen = null;
            answer?.Invoke(chosen);
        }
    }
}
