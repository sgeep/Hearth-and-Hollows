using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The staff helper on the floor. At the Grill or Tap it runs <see cref="StaffCook"/>; on
    /// Serving it carries dishes from the pass using the Serving minigame with an auto-player.
    /// Either way the station resolves through <see cref="IMinigame"/> at reduced quality.
    /// </summary>
    public sealed class StaffAgent : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Body;
        [SerializeField] SpriteRenderer m_Plate;
        [SerializeField] SpriteRenderer m_WorkingIcon;

        TavernDirector m_Director;
        StaffDefinition m_Definition;
        StaffStation m_Station = StaffStation.None;
        StaffCook m_Cook;
        IRandom m_Random;
        ServingMinigame m_Serving;
        IMinigameAutoPlayer m_ServingPlayer;
        Ticket m_Carrying;
        CustomerLogic m_For;
        float m_Rest;
        bool m_Returning;

        public StaffStation Station => m_Station;
        public bool IsBusy => (m_Cook != null && m_Cook.IsBusy) || m_Carrying != null;

        public void Configure(SpriteRenderer body, SpriteRenderer plate, SpriteRenderer workingIcon)
        {
            m_Body = body;
            m_Plate = plate;
            m_WorkingIcon = workingIcon;
        }

        public bool Works(StationKind kind) =>
            (kind == StationKind.Grill && m_Station == StaffStation.Grill) ||
            (kind == StationKind.Tap && m_Station == StaffStation.Tap);

        public void Begin(StaffStation station, StaffDefinition definition, TavernDirector director, IRandom random)
        {
            m_Director = director;
            m_Definition = definition;
            m_Station = definition != null ? station : StaffStation.None;
            m_Random = random;
            gameObject.SetActive(m_Station != StaffStation.None);
            if (m_Station == StaffStation.None) return;

            if (m_Body != null) m_Body.color = definition.placeholderColor;
            if (m_Plate != null) m_Plate.enabled = false;
            var layout = director.Layout;
            switch (m_Station)
            {
                case StaffStation.Grill:
                    PlaceAt(layout.Grill.X + 0.35f);
                    m_Cook = new StaffCook(director.Session, director.Minigames, CookStation.Grill, definition.skill, definition.qualityCap, definition.restBetweenJobs, random);
                    break;
                case StaffStation.Tap:
                    PlaceAt(layout.Tap.X + 0.35f);
                    m_Cook = new StaffCook(director.Session, director.Minigames, CookStation.Tap, definition.skill, definition.qualityCap, definition.restBetweenJobs, random);
                    break;
                case StaffStation.Serving:
                    PlaceAt(layout.Pass.X);
                    break;
            }
        }

        public void StopWork()
        {
            m_Cook = null;
            m_Carrying = null;
            m_For = null;
            m_Serving = null;
            if (m_Plate != null) m_Plate.enabled = false;
            if (m_WorkingIcon != null) m_WorkingIcon.enabled = false;
        }

        void Update()
        {
            if (m_Director == null || m_Director.Phase != TavernPhase.Service || m_Director.Session == null) return;
            float dt = Time.deltaTime;

            if (m_Cook != null) m_Cook.Tick(dt);
            else if (m_Station == StaffStation.Serving) TickServing(dt);

            if (m_WorkingIcon != null) m_WorkingIcon.enabled = IsBusy;
        }

        void TickServing(float dt)
        {
            var session = m_Director.Session;
            var layout = m_Director.Layout;

            if (m_Carrying == null)
            {
                if (m_Returning)
                {
                    float speed = m_Director.PlayerSettings.walkSpeed;
                    PlaceAt(Mathf.MoveTowards(transform.position.x, layout.Pass.X, speed * dt));
                    m_Returning = Mathf.Abs(transform.position.x - layout.Pass.X) > 0.05f;
                    return;
                }
                if (m_Rest > 0f)
                {
                    m_Rest -= dt;
                    return;
                }
                var next = session.NextToServe(includeSpares: false);
                if (next == null || !session.StartDelivery(next, this)) return;
                m_Carrying = next;
                m_For = next.Customer;
                m_Serving = m_Director.Minigames.CreateServing(transform.position.x, layout.SeatX(next.Customer.Seat));
                m_ServingPlayer = MinigameFactory.CreateAutoPlayer(m_Serving, m_Definition.skill, m_Random);
                m_Serving.Begin();
                if (m_Plate != null)
                {
                    m_Plate.enabled = true;
                    m_Plate.color = next.Recipe.placeholderColor;
                }
            }

            if (m_Carrying.State != TicketState.Delivering)
            {
                FinishDelivery();
                return;
            }

            if (m_Serving == null || m_Carrying.Customer != m_For)
            {
                // The customer left (or the keeper served them another plate): take it back to the pass.
                m_Serving = null;
                float speed = m_Director.PlayerSettings.walkSpeed;
                PlaceAt(Mathf.MoveTowards(transform.position.x, layout.Pass.X, speed * dt));
                if (Mathf.Abs(transform.position.x - layout.Pass.X) > 0.05f) return;
                session.PutBack(m_Carrying);
                FinishDelivery();
                return;
            }

            m_Serving.Tick(dt, m_ServingPlayer.NextInput(dt));
            PlaceAt(m_Serving.Position);
            if (TavernPlayer.BumpedIntoSomeone(transform.position.x, m_Director)) m_Serving.RegisterBump();

            if (m_Serving.Dropped)
            {
                session.Dropped(m_Carrying);
                FinishDelivery();
            }
            else if (m_Serving.Arrived)
            {
                if (!session.Deliver(m_Carrying, m_For, Mathf.Min(m_Serving.Evaluate(), m_Definition.qualityCap)))
                    session.PutBack(m_Carrying);
                FinishDelivery();
            }
        }

        void FinishDelivery()
        {
            m_Carrying = null;
            m_For = null;
            m_Serving = null;
            m_ServingPlayer = null;
            m_Rest = m_Definition.restBetweenJobs;
            m_Returning = true;
            if (m_Plate != null) m_Plate.enabled = false;
        }

        void PlaceAt(float x)
        {
            var p = transform.position;
            p.x = x;
            transform.position = p;
        }
    }
}
