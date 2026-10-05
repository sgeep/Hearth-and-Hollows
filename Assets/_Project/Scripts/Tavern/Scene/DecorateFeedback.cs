using System;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>Decorate Mode's authored moments: each one combined feedback (sound and a named haptic pattern together).</summary>
    [Serializable]
    public sealed class DecorateMoments
    {
        public MMF_Player enter;
        public MMF_Player leave;
        [Tooltip("A piece lifts.")] public MMF_Player pickUp;
        [Tooltip("A piece settles: the satisfying one.")] public MMF_Player place;
        public MMF_Player turn;
        public MMF_Player flip;
        [Tooltip("It won't go there (the ghost says why).")] public MMF_Player invalid;
        public MMF_Player store;
        public MMF_Player fromStorage;
        public MMF_Player undo;
        public MMF_Player putBack;
        [Tooltip("A piece bought from the catalogue: coins and a firm pulse.")] public MMF_Player buy;
        public MMF_Player sell;
        [Tooltip("A new colourway or palette: a soft brush.")] public MMF_Player restyle;
        [Tooltip("A wall or floor finish laid.")] public MMF_Player finish;
        [Tooltip("Off to another room.")] public MMF_Player area;
    }

    /// <summary>
    /// Makes decorating felt (CLAUDE.md, Game feel): a lift when a piece comes up, a settling thud when it goes down, a
    /// click for a turn, a short buzz when it won't fit. Restrained: rearranging a room is many small actions, so only
    /// placing is firm. Haptics respect the player's settings and do nothing where unsupported (the haptic service).
    /// </summary>
    public sealed class DecorateFeedback : MonoBehaviour
    {
        [SerializeField] DecorateMoments m_Moments = new();

        DecorateMode m_Mode;

        /// <summary>The last moment played (tests).</summary>
        public DecorateMoment? LastMoment { get; private set; }

        public void Configure(DecorateMoments moments) => m_Moments = moments;

        void Start()
        {
            m_Mode = GetComponentInParent<DecorateMode>() ?? DecorateMode.Instance;
            if (m_Mode != null) m_Mode.MomentPlayed += Play;
        }

        void OnDestroy()
        {
            if (m_Mode != null) m_Mode.MomentPlayed -= Play;
        }

        void Play(DecorateMoment moment)
        {
            LastMoment = moment;
            MMF_Player player = moment switch
            {
                DecorateMoment.Enter => m_Moments.enter,
                DecorateMoment.Leave => m_Moments.leave,
                DecorateMoment.PickUp => m_Moments.pickUp,
                DecorateMoment.Place => m_Moments.place,
                DecorateMoment.Turn => m_Moments.turn,
                DecorateMoment.Flip => m_Moments.flip,
                DecorateMoment.Invalid => m_Moments.invalid,
                DecorateMoment.Store => m_Moments.store,
                DecorateMoment.FromStorage => m_Moments.fromStorage,
                DecorateMoment.Undo => m_Moments.undo,
                DecorateMoment.PutBack => m_Moments.putBack,
                DecorateMoment.Buy => m_Moments.buy,
                DecorateMoment.Sell => m_Moments.sell,
                DecorateMoment.Restyle => m_Moments.restyle,
                DecorateMoment.Finish => m_Moments.finish,
                DecorateMoment.Area => m_Moments.area,
                _ => null,
            };
            if (player != null) player.PlayFeedbacks();
        }
    }
}
