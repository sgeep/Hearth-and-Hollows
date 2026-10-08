using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core.Movement;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Audio;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Village
{
    /// <summary>
    /// Gimp, in the night (4h Checkpoint D, the owner's approved first meeting): the first morning after a delve of the keeper's
    /// own (<see cref="CommunityRules.GimpIntroDue"/>), the keeper wakes in the dark to a half-elf climbing out of the hatch in their
    /// floor, as he has for years by his arrangement with Phi. It plays as the new day begins, after sleep has already grown the
    /// garden, refilled Vigor and saved the morning, so none of that is touched: the room goes dark, the clock holds, the music
    /// is silent, his conversation (<see cref="Conversation"/>, the node editor's) plays, he goes back down, and the light comes up
    /// on an ordinary morning. Once per save (<see cref="CommunityRules.GimpIntro"/> in the story's seen beats), saved at the end.
    /// </summary>
    public sealed class NightVisitor : MonoBehaviour
    {
        public const string Conversation = "Gimp/Intruder";

        [SerializeField] GameObject m_Figure;
        [SerializeField] LayeredSpriteAnimator m_Animator;
        [SerializeField] Light2D m_Lantern;
        [SerializeField, Tooltip("The hatch (world): where he climbs out and goes back down.")] Vector2 m_Hatch;
        [SerializeField, Tooltip("Where he stands to talk (world), a step off the hatch.")] Vector2 m_Stand;
        [SerializeField, Range(0f, 1f), Tooltip("The room's light while it's night (a share of the day's).")] float m_Dark = 0.3f;
        [SerializeField] Color m_NightTint = new(0.55f, 0.6f, 0.95f);
        [SerializeField, Min(0f)] float m_DawnSeconds = 1.4f;

        TavernDirector m_Director;
        /// <summary>Each lit room light: what the room itself set it to, and what we last wrote over it.</summary>
        readonly List<(Light2D light, float baseIntensity, Color baseColor, float wroteIntensity, Color wroteColor)> m_Lights = new();
        /// <summary>How dark it is now: 1 night, 0 day.</summary>
        float m_Night;

        /// <summary>The scene is playing (tests).</summary>
        public bool Playing { get; private set; }

        public static NightVisitor Instance { get; private set; }

        public void Configure(GameObject figure, LayeredSpriteAnimator animator, Light2D lantern, Vector2 hatch, Vector2 stand)
        {
            m_Figure = figure;
            m_Animator = animator;
            m_Lantern = lantern;
            m_Hatch = hatch;
            m_Stand = stand;
        }

        void Awake()
        {
            Instance = this;
            if (m_Figure != null) m_Figure.SetActive(false);
            if (m_Lantern != null) m_Lantern.enabled = false;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director != null) m_Director.Woke += OnWoke;
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.Woke -= OnWoke;
            if (Instance == this) Instance = null;
            SurfacePause.Release(this);
            Hearthdelve.Shared.Engine.PauseRules.Unblock(this);
            MusicHolds.Release(this);
        }

        /// <summary>Whether this morning is his (the rule, read from the game).</summary>
        public static bool Due()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame) return false;
            GameState s = flow.State;
            SurfaceClockSettings clock = SurfaceTime.CurrentSettings;
            return CommunityRules.GimpIntroDue(s.Story.OpeningComplete, s.Day, s.Story.SeenHints.Contains(CommunityRules.GimpIntro),
                s.Surface.WholeMinute, clock.dayStartMinute);
        }

        void OnWoke()
        {
            if (!Playing && Due() && StoryServices.Conversations != null && StoryServices.Conversations.HasConversation(Conversation))
                StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            Playing = true;
            SurfacePause.Hold(this);
            // 4i-A: no pause menu in the middle of the scene (it plays out, then the morning is ordinary).
            Hearthdelve.Shared.Engine.PauseRules.Block(this);
            MusicHolds.Hold(this, MusicCue.Silence);
            Night(true);
            if (m_Lantern != null) m_Lantern.enabled = true;
            // He's already up, a step off the hatch, looking at the bed.
            if (m_Figure != null)
            {
                m_Figure.transform.position = m_Stand;
                m_Figure.SetActive(true);
                FaceKeeper();
            }
            yield return new WaitForSecondsRealtime(0.9f);
            IConversationService talk = StoryServices.Conversations;
            if (talk != null && talk.Play(Conversation))
            {
                while (talk.IsTalking) yield return null;
                GameFlow.Instance?.MarkHintSeen(CommunityRules.GimpIntro);
            }
            // Back down the way he came.
            if (m_Figure != null)
            {
                if (m_Animator != null) m_Animator.ReleaseFacing();
                Vector2 from = m_Figure.transform.position;
                for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
                {
                    m_Figure.transform.position = Vector2.Lerp(from, m_Hatch, t / 0.6f);
                    if (m_Animator != null) m_Animator.Movement = (m_Hatch - from).normalized;
                    yield return null;
                }
                if (m_Animator != null) m_Animator.Movement = Vector2.zero;
                m_Figure.SetActive(false);
            }
            if (m_Lantern != null) m_Lantern.enabled = false;
            // The light comes up on an ordinary morning.
            for (float t = 0f; t < m_DawnSeconds; t += Time.unscaledDeltaTime)
            {
                Dawn(t / m_DawnSeconds);
                yield return null;
            }
            Night(false);
            SurfacePause.Release(this);
            Hearthdelve.Shared.Engine.PauseRules.Unblock(this);
            MusicHolds.Release(this);
            GameFlow flow = GameFlow.Instance;
            if (flow != null && flow.InGame && flow.State.Story.SeenHints.Contains(CommunityRules.GimpIntro)) flow.Save();
            Playing = false;
        }

        void FaceKeeper()
        {
            GameObject keeper = GameObject.FindWithTag("Player");
            if (keeper == null || m_Animator == null) return;
            Vector2 to = (Vector2)keeper.transform.position - m_Stand;
            m_Animator.LockFacing(FacingLogic.FromDirection(to.x, to.y, Facing4.FrontLeft, 0f));
        }

        /// <summary>Darkens the keeper's room (its own lights), or puts them back exactly as the room has them now.</summary>
        void Night(bool dark)
        {
            if (dark)
            {
                m_Lights.Clear();
                SurfaceArea room = SurfaceArea.Find(SurfaceArea.GuestRoomId);
                if (room != null)
                    foreach (Light2D light in room.Lights)
                        if (light != null) m_Lights.Add((light, light.intensity, light.color, float.NaN, default));
                m_Night = 1f;
                ApplyNight();
                return;
            }
            m_Night = 0f;
            ApplyNight();
            m_Lights.Clear();
        }

        void Dawn(float k) => m_Night = 1f - Mathf.Clamp01(k);

        /// <summary>
        /// The night over the room's own light: if the room changed a light since we last wrote it (its mood blending to morning),
        /// that's the new base; the light shows the base darkened by how much night there is.
        /// </summary>
        void ApplyNight()
        {
            for (int i = 0; i < m_Lights.Count; i++)
            {
                var (light, baseIntensity, baseColor, wroteIntensity, wroteColor) = m_Lights[i];
                if (light == null) continue;
                if (!Mathf.Approximately(light.intensity, wroteIntensity) || light.color != wroteColor)
                {
                    baseIntensity = light.intensity;
                    baseColor = light.color;
                }
                float intensity = Mathf.Lerp(baseIntensity, baseIntensity * m_Dark, m_Night);
                Color color = Color.Lerp(baseColor, baseColor * m_NightTint, m_Night);
                light.intensity = intensity;
                light.color = color;
                m_Lights[i] = (light, baseIntensity, baseColor, intensity, color);
            }
        }

        void LateUpdate()
        {
            if (Playing && m_Lights.Count > 0) ApplyNight();
        }
    }
}
