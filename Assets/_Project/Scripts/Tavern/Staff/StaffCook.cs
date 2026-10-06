using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using UnityEngine;

namespace Hearthdelve.Tavern.Staff
{
    /// <summary>
    /// A staff member working a cooking station: claims the next ticket, then plays the station's
    /// minigame through <see cref="IMinigame"/> with an auto-player of their skill (reduced quality).
    /// Serving is handled by the scene carrier using the same auto-player. Pure logic.
    /// </summary>
    public sealed class StaffCook
    {
        readonly ServiceSession m_Session;
        readonly MinigameFactory m_Factory;
        readonly CookStation m_Station;
        readonly float m_Skill;
        readonly float m_QualityCap;
        readonly float m_Rest;
        readonly IRandom m_Random;
        IMinigameAutoPlayer m_Player;
        float m_Resting;

        public StaffCook(ServiceSession session, MinigameFactory factory, CookStation station,
            float skill, float qualityCap, float restBetweenJobs, IRandom random)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            m_Station = station;
            m_Skill = skill;
            m_QualityCap = qualityCap;
            m_Rest = restBetweenJobs;
            m_Random = random;
        }

        public Ticket Current { get; private set; }
        public IMinigame Minigame { get; private set; }
        public bool IsBusy => Current != null;

        /// <summary>A dish came off the station, with its quality (0–1, the staff cap applied).</summary>
        public event Action<RecipeDefinition, float> Cooked;

        public void Tick(float deltaTime)
        {
            if (m_Session.IsOver) return;
            if (Current == null)
            {
                if (m_Resting > 0f)
                {
                    m_Resting -= deltaTime;
                    return;
                }
                var next = m_Session.NextToCook(m_Station);
                if (next == null || !m_Session.StartCooking(next, this)) return;
                Current = next;
                Minigame = m_Factory.CreateCook(m_Station);
                m_Player = MinigameFactory.CreateAutoPlayer(Minigame, m_Skill, m_Random);
                Minigame.Begin();
            }

            if (Current.State != TicketState.Cooking)
            {
                // The customer walked out mid-cook.
                Finish();
                return;
            }

            Minigame.Tick(deltaTime, m_Player.NextInput(deltaTime));
            if (!Minigame.IsComplete) return;

            float quality = Mathf.Min(Minigame.Evaluate(), m_QualityCap);
            RecipeDefinition dish = Current.Recipe;
            m_Session.FinishCooking(Current, quality);
            Finish();
            Cooked?.Invoke(dish, quality);
        }

        void Finish()
        {
            Current = null;
            Minigame = null;
            m_Player = null;
            m_Resting = m_Rest;
        }
    }
}
