using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>The tavern's authored moments, each one combined feedback (sound, visuals and a named haptic pattern together).</summary>
    [Serializable]
    public sealed class TavernMoments
    {
        [Header("Grill")] public MMF_Player flip;
        public MMF_Player perfectFlip;
        public MMF_Player burned;
        [Header("Tap")] public MMF_Player lineReached;
        public MMF_Player cleanPour;
        public MMF_Player pourDone;
        public MMF_Player overflow;
        [Header("Chop and stew")] public MMF_Player cleanCut;
        public MMF_Player raggedCut;
        public MMF_Player chopDone;
        public MMF_Player stewReady;
        [Header("Butcher Block (4f Checkpoint C)")] public MMF_Player butcherStroke;
        public MMF_Player butcherClean;
        public MMF_Player butcherRagged;
        public MMF_Player butcherDone;
        [Header("Serving")] public MMF_Player pickUp;
        public MMF_Player putBack;
        public MMF_Player softBump;
        public MMF_Player hardBump;
        public MMF_Player spillWarning;
        public MMF_Player dropped;
        public MMF_Player served;
        [Header("The room (no haptics)")] public MMF_Player paid;
        public MMF_Player walkout;
    }

    /// <summary>
    /// Makes the tavern's work felt (GDD §6.5, §9A): the keeper's cooking and serving play their combined feedbacks,
    /// and the grill's heat and the tap's pour run continuous rumble and sound, all read from the minigames' and the
    /// session's own state (<see cref="TavernFeedbackRules"/>), never timed separately. Strong on the moments that matter
    /// (a perfect flip, a clean pour, a dropped plate), restrained on repetition; payments and walkouts are heard and
    /// seen but don't vibrate. Only the keeper's own work rumbles: Orik's cooking is quiet. Haptics respect the player's
    /// settings and do nothing where unsupported (the haptic service).
    /// </summary>
    public sealed class TavernFeedback : MonoBehaviour
    {
        public const string GrillChannel = "tavern.grill";
        public const string PourChannel = "tavern.pour";
        public const string ButcherChannel = "tavern.butcher";

        [SerializeField] TavernFeedbackConfig m_Config;
        [SerializeField] TavernMoments m_Moments = new();
        [SerializeField, Tooltip("Looping sizzle at the grill; louder as it nears burning.")] AudioSource m_Sizzle;
        [SerializeField, Tooltip("Looping pour at the tap.")] AudioSource m_Pour;

        IMinigame m_Game;
        bool m_Resolved;
        float m_LastFill;
        float m_PlateSpill;
        PotState m_LastPot = PotState.Empty;
        int m_LastPaid, m_LastWalkouts;
        ServiceSession m_Session;

        TavernFeedbackSettings Settings => m_Config != null ? m_Config.feedback : TavernFeedbackSettings.Default;

        /// <summary>The last moment played (tests and debugging).</summary>
        public string LastMoment { get; private set; }
        /// <summary>Raised with each moment's name as it plays (the UI's visual side listens: the station panel's flash).</summary>
        public event Action<string> MomentPlayed;
        public float GrillWarning { get; private set; }
        public (float low, float high) PourLevel { get; private set; }
        /// <summary>The Butcher Block's drag rumble now (0: not cutting).</summary>
        public float ButcherDrag { get; private set; }

        public void Configure(TavernFeedbackConfig config, TavernMoments moments, AudioSource sizzle, AudioSource pour)
        {
            m_Config = config;
            m_Moments = moments;
            m_Sizzle = sizzle;
            m_Pour = pour;
        }

        void OnEnable()
        {
            EventBus<KeeperPlate>.Subscribe(OnPlate);
            EventBus<ServingBumped>.Subscribe(OnBumped);
        }

        void OnDisable()
        {
            EventBus<KeeperPlate>.Unsubscribe(OnPlate);
            EventBus<ServingBumped>.Unsubscribe(OnBumped);
            Release(m_Game);
            m_Game = null;
            StopContinuous();
        }

        void Update()
        {
            ObserveCooking();
            ObserveRoom();
        }

        // ---------- Cooking ----------

        void ObserveCooking()
        {
            IMinigame current = KeeperWork.Instance != null ? KeeperWork.Instance.ActiveCook : null;
            if (m_Game != null && !m_Resolved && m_Game.IsComplete) Resolve(m_Game);
            if (!ReferenceEquals(current, m_Game))
            {
                Release(m_Game);
                m_Game = current;
                m_Resolved = false;
                m_LastFill = 0f;
                Watch(m_Game);
            }

            TavernFeedbackSettings s = Settings;
            GrillWarning = 0f;
            PourLevel = (0f, 0f);
            if (m_Game is GrillMinigame grill && !grill.IsComplete && !grill.IsPausing)
                GrillWarning = TavernFeedbackRules.GrillWarning(grill.Meter, grill.Settings, s);
            if (m_Game is TapMinigame tap && !tap.IsComplete)
            {
                PourLevel = TavernFeedbackRules.Pour(tap.IsPouring, tap.Total, tap.Settings, s);
                if (TavernFeedbackRules.ReachedLine(m_LastFill, tap.Total, tap.Settings)) Play(m_Moments.lineReached, nameof(TavernMoments.lineReached));
                m_LastFill = tap.Total;
            }
            // The Butcher Block: a smooth, light drag while the knife follows the line; a rough grind off it.
            ButcherDrag = m_Game is ButcherMinigame butcher && !butcher.IsComplete && butcher.Stroke >= 0 ? (butcher.OnTrack ? 0.15f : 0.45f) : 0f;
            HapticService.SetContinuous(ButcherChannel, ButcherDrag, ButcherDrag > 0.3f ? 0.1f : ButcherDrag);
            HapticService.SetContinuous(GrillChannel, GrillWarning * 0.6f, GrillWarning);
            HapticService.SetContinuous(PourChannel, PourLevel.low, PourLevel.high);
            // The sound follows the same values as the rumble.
            SetLoop(m_Sizzle, m_Game is GrillMinigame g && !g.IsComplete && !g.IsPausing, 0.25f + 0.75f * GrillWarning);
            SetLoop(m_Pour, PourLevel.low > 0f, 0.4f + 0.6f * PourLevel.high);
        }

        void Watch(IMinigame game)
        {
            if (game is GrillMinigame grill) grill.SideFinished += OnSideFinished;
            else if (game is ChopMinigame chop) chop.Cut += OnCut;
            else if (game is ButcherMinigame butcher)
            {
                butcher.StrokeStarted += OnStroke;
                butcher.StrokeFinished += OnStrokeFinished;
            }
        }

        void Release(IMinigame game)
        {
            if (game is GrillMinigame grill) grill.SideFinished -= OnSideFinished;
            else if (game is ChopMinigame chop) chop.Cut -= OnCut;
            else if (game is ButcherMinigame butcher)
            {
                butcher.StrokeStarted -= OnStroke;
                butcher.StrokeFinished -= OnStrokeFinished;
            }
        }

        void OnStroke(int line) => Play(m_Moments.butcherStroke, nameof(TavernMoments.butcherStroke));

        /// <summary>A stroke through: a crisp, solid cleave when it followed the line, a dull ragged hack when it didn't.</summary>
        void OnStrokeFinished(int line, float score)
        {
            bool clean = m_Game is ButcherMinigame butcher && score >= butcher.Settings.cleanStroke;
            if (clean) Play(m_Moments.butcherClean, nameof(TavernMoments.butcherClean));
            else Play(m_Moments.butcherRagged, nameof(TavernMoments.butcherRagged));
        }

        void OnSideFinished(int side, float score)
        {
            // A side scores zero only by burning (or by flipping raw, which the meter shows plainly).
            bool burned = m_Game is GrillMinigame grill && grill.Meter >= GrillMinigame.BurnAt;
            switch (TavernFeedbackRules.Flip(score, burned, Settings))
            {
                case FlipFeel.Burned: Play(m_Moments.burned, nameof(TavernMoments.burned)); break;
                case FlipFeel.Perfect: Play(m_Moments.perfectFlip, nameof(TavernMoments.perfectFlip)); break;
                default: Play(m_Moments.flip, nameof(TavernMoments.flip)); break;
            }
        }

        void OnCut(float score)
        {
            if (TavernFeedbackRules.Cut(score, Settings) == CutFeel.Clean) Play(m_Moments.cleanCut, nameof(TavernMoments.cleanCut));
            else Play(m_Moments.raggedCut, nameof(TavernMoments.raggedCut));
        }

        /// <summary>The game finished (not abandoned): its completion moment. Grill sides already spoke as they finished.</summary>
        void Resolve(IMinigame game)
        {
            m_Resolved = true;
            if (game is TapMinigame tap)
            {
                switch (TavernFeedbackRules.Pour(tap.Overflowed, tap.Evaluate(), Settings))
                {
                    case PourResult.Overflow: Play(m_Moments.overflow, nameof(TavernMoments.overflow)); break;
                    case PourResult.Clean: Play(m_Moments.cleanPour, nameof(TavernMoments.cleanPour)); break;
                    default: Play(m_Moments.pourDone, nameof(TavernMoments.pourDone)); break;
                }
            }
            else if (game is ChopMinigame chop)
            {
                Play(m_Moments.chopDone, nameof(TavernMoments.chopDone), TavernFeedbackRules.GoodChop(chop.Evaluate(), Settings) ? 1f : 0.5f);
            }
            else if (game is ButcherMinigame butcher)
            {
                // The cuts sliding apart: fuller the more there are.
                Play(m_Moments.butcherDone, nameof(TavernMoments.butcherDone), Mathf.Lerp(0.4f, 1f, butcher.Evaluate()));
            }
        }

        // ---------- The room ----------

        void ObserveRoom()
        {
            ServiceSession session = TavernDirector.Instance != null ? TavernDirector.Instance.Session : null;
            if (!ReferenceEquals(session, m_Session))
            {
                m_Session = session;
                m_LastPot = session != null ? session.Pot.State : PotState.Empty;
                m_LastPaid = session != null ? session.Ledger.DishesServed : 0;
                m_LastWalkouts = session != null ? session.Ledger.Walkouts : 0;
            }
            if (session == null) return;

            // A pot coming ready is worth one cue, not one per helping.
            PotState pot = session.Pot.State;
            if (pot == PotState.Ready && m_LastPot != PotState.Ready) Play(m_Moments.stewReady, nameof(TavernMoments.stewReady));
            m_LastPot = pot;

            ServiceLedger ledger = session.Ledger;
            if (ledger.DishesServed > m_LastPaid) Play(m_Moments.paid, nameof(TavernMoments.paid));
            if (ledger.Walkouts > m_LastWalkouts) Play(m_Moments.walkout, nameof(TavernMoments.walkout));
            m_LastPaid = ledger.DishesServed;
            m_LastWalkouts = ledger.Walkouts;
        }

        void OnPlate(KeeperPlate plate)
        {
            switch (plate.Moment)
            {
                case PlateMoment.PickedUp:
                    m_PlateSpill = 0f;
                    Play(m_Moments.pickUp, nameof(TavernMoments.pickUp));
                    break;
                case PlateMoment.PutBack: Play(m_Moments.putBack, nameof(TavernMoments.putBack)); break;
                case PlateMoment.Served: Play(m_Moments.served, nameof(TavernMoments.served)); break;
                case PlateMoment.Dropped: Play(m_Moments.dropped, nameof(TavernMoments.dropped)); break;
            }
        }

        // The keeper's bumps only (Orik's are Orik's); a drop is the dropped moment, not a bump.
        void OnBumped(ServingBumped bump)
        {
            if (!bump.ByPlayer || bump.Dropped) return;
            TavernFeedbackSettings s = Settings;
            var (feel, intensity) = TavernFeedbackRules.Bump(bump.Strength, s);
            if (TavernFeedbackRules.SpillWarning(m_PlateSpill, bump.Spill, s)) Play(m_Moments.spillWarning, nameof(TavernMoments.spillWarning));
            else if (feel == BumpFeel.Hard) Play(m_Moments.hardBump, nameof(TavernMoments.hardBump), intensity);
            else Play(m_Moments.softBump, nameof(TavernMoments.softBump), intensity);
            m_PlateSpill = bump.Spill;
        }

        // ---------- Playing ----------

        void Play(MMF_Player moment, string name, float intensity = 1f)
        {
            LastMoment = name;
            MomentPlayed?.Invoke(name);
            if (moment == null) return;
            Transform keeper = KeeperWork.Instance != null ? KeeperWork.Instance.transform : transform;
            moment.PlayFeedbacks(keeper.position, intensity);
        }

        static void SetLoop(AudioSource source, bool on, float volume)
        {
            if (source == null) return;
            if (on)
            {
                source.volume = Mathf.Clamp01(volume);
                if (!source.isPlaying) source.Play();
            }
            else if (source.isPlaying) source.Stop();
        }

        void StopContinuous()
        {
            HapticService.SetContinuous(GrillChannel, 0f, 0f);
            HapticService.SetContinuous(PourChannel, 0f, 0f);
            HapticService.SetContinuous(ButcherChannel, 0f, 0f);
            SetLoop(m_Sizzle, false, 0f);
            SetLoop(m_Pour, false, 0f);
        }
    }
}
