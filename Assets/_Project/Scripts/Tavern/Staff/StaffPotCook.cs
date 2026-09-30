using System;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using UnityEngine;

namespace Hearthdelve.Tavern.Staff
{
    /// <summary>
    /// A staff member working the stew pot: whenever it's empty and a stew can be made, they put a
    /// batch on and chop it through <see cref="ChopMinigame"/> with an auto-player of their skill
    /// (their quality cap caps the chop score, so staff batches make fewer helpings). Pure logic.
    /// </summary>
    public sealed class StaffPotCook
    {
        readonly ServiceSession m_Session;
        readonly MinigameFactory m_Factory;
        readonly float m_Skill;
        readonly float m_QualityCap;
        readonly float m_Rest;
        readonly IRandom m_Random;
        IMinigameAutoPlayer m_Player;
        float m_Resting;

        public StaffPotCook(ServiceSession session, MinigameFactory factory, float skill, float qualityCap, float restBetweenJobs, IRandom random)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Factory = factory ?? throw new ArgumentNullException(nameof(factory));
            m_Skill = skill;
            m_QualityCap = qualityCap;
            m_Rest = restBetweenJobs;
            m_Random = random ?? new SeededRandom();
        }

        public ChopMinigame Minigame { get; private set; }
        public bool IsBusy => Minigame != null;

        public void Tick(float deltaTime)
        {
            if (m_Session.IsOver) return;
            if (Minigame == null)
            {
                if (m_Resting > 0f)
                {
                    m_Resting -= deltaTime;
                    return;
                }
                var recipe = m_Session.NextBatch();
                if (recipe == null || !m_Session.StartBatch(recipe, this)) return;
                Minigame = m_Factory.CreateChop(m_Session.Pot.ChopItems.Count, m_Random);
                m_Player = MinigameFactory.CreateAutoPlayer(Minigame, m_Skill, m_Random);
                Minigame.Begin();
            }

            if (m_Session.Pot.State != PotState.Chopping || m_Session.Pot.ClaimedBy != this)
            {
                Finish();
                return;
            }

            Minigame.Tick(deltaTime, m_Player.NextInput(deltaTime));
            if (!Minigame.IsComplete) return;
            m_Session.FinishChopping(this, Mathf.Min(Minigame.Evaluate(), m_QualityCap));
            Finish();
        }

        void Finish()
        {
            Minigame = null;
            m_Player = null;
            m_Resting = m_Rest;
        }
    }
}
