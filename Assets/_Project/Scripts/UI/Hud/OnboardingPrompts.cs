using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Run;
using Hearthdelve.Shared.Story;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// The first delve's prompts (4g Checkpoint B): a short line at the foot of the screen, each shown once in a game when it first
    /// matters (moving and aiming as the delve begins, attack and dodge when a fight seals the room, Essence when it's first lost to
    /// a hit, the satchel at the first harvest, the finisher when a foe can first be finished, the way home when it first opens).
    /// Optimisation (clean-kill maths, overkill, the Butcher Block, powers) is left to be discovered. A game made before the opening,
    /// or a quick new game, has seen them all.
    /// </summary>
    public sealed class OnboardingPrompts : MonoBehaviour
    {
        [SerializeField] CanvasGroup m_Group;
        [SerializeField] LocalizedSuperText m_Text;
        [SerializeField, Min(0.5f), Tooltip("Seconds a prompt stays up (a newer one replaces it sooner).")] float m_Hold = 6f;
        [SerializeField, Min(0.01f)] float m_Fade = 0.4f;

        readonly Queue<string> m_Queue = new();
        string m_Showing;
        float m_ShownAt = float.NegativeInfinity;

        public string Showing => m_Group != null && m_Group.alpha > 0f ? m_Showing : null;
        public string ShownText => m_Text != null ? m_Text.GetComponent<SuperTextMesh>().text : null;

        public void Configure(CanvasGroup group, LocalizedSuperText text)
        {
            m_Group = group;
            m_Text = text;
        }

        void Awake()
        {
            if (m_Group != null) m_Group.alpha = 0f;
        }

        void OnEnable()
        {
            EventBus<RoomEntered>.Subscribe(OnRoomEntered);
            EventBus<EssenceChanged>.Subscribe(OnEssence);
            EventBus<HarvestFeedback>.Subscribe(OnHarvest);
            EventBus<FinisherAvailable>.Subscribe(OnFinisher);
            EventBus<DelveExitHint>.Subscribe(OnExit);
        }

        void OnDisable()
        {
            EventBus<RoomEntered>.Unsubscribe(OnRoomEntered);
            EventBus<EssenceChanged>.Unsubscribe(OnEssence);
            EventBus<HarvestFeedback>.Unsubscribe(OnHarvest);
            EventBus<FinisherAvailable>.Unsubscribe(OnFinisher);
            EventBus<DelveExitHint>.Unsubscribe(OnExit);
        }

        void OnRoomEntered(RoomEntered e)
        {
            if (e.Index == 0) Want(OnboardingHints.Move);
            if (e.Sealed) Want(OnboardingHints.Fight);
        }

        void OnEssence(EssenceChanged e)
        {
            if (e.FromDamage) Want(OnboardingHints.Essence);
        }

        void OnHarvest(HarvestFeedback e)
        {
            if (e.Count > 0) Want(OnboardingHints.Harvest);
        }

        void OnFinisher(FinisherAvailable e) => Want(OnboardingHints.Finisher);

        void OnExit(DelveExitHint e)
        {
            if (e.Visible) Want(OnboardingHints.Extract);
        }

        /// <summary>Queues a prompt if this game hasn't shown it (and marks it shown).</summary>
        void Want(string id)
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame || flow.State.Story.SeenHints.Contains(id) || m_Queue.Contains(id)) return;
            flow.MarkHintSeen(id);
            m_Queue.Enqueue(id);
        }

        void Update()
        {
            if (m_Group == null) return;
            float age = Time.unscaledTime - m_ShownAt;
            // The next waits only a moment once one is up (they come in quick succession at a fight's start).
            if (m_Queue.Count > 0 && (m_Showing == null || age > Mathf.Min(m_Hold, 2.5f))) Show(m_Queue.Dequeue());
            age = Time.unscaledTime - m_ShownAt;
            m_Group.alpha = m_Showing == null ? 0f : age < m_Hold ? 1f : Mathf.Clamp01(1f - (age - m_Hold) / m_Fade);
            if (m_Showing != null && age > m_Hold + m_Fade) m_Showing = null;
        }

        void Show(string id)
        {
            m_Showing = id;
            m_ShownAt = Time.unscaledTime;
            bool pad = UsingGamepad();
            string Dungeon(string action) => InputHints.Binding(InputMaps.Dungeon, action);
            switch (id)
            {
                case OnboardingHints.Move:
                    // Written out: the move bindings are composites ("W/A/S/D | Up Arrow/…"), too long for a prompt.
                    m_Text.Set(pad ? OnboardingLocKeys.MovePad : OnboardingLocKeys.Move);
                    break;
                case OnboardingHints.Fight: m_Text.Set(OnboardingLocKeys.Fight, Dungeon(DungeonActions.Attack), Dungeon(DungeonActions.Dodge)); break;
                case OnboardingHints.Essence: m_Text.Set(OnboardingLocKeys.Essence); break;
                case OnboardingHints.Harvest: m_Text.Set(OnboardingLocKeys.Harvest); break;
                case OnboardingHints.Finisher: m_Text.Set(OnboardingLocKeys.Finisher, Dungeon(DungeonActions.Finisher)); break;
                case OnboardingHints.Extract: m_Text.Set(OnboardingLocKeys.Extract); break;
            }
        }

        /// <summary>The device used last: a gamepad's labels if it moved more recently than the keyboard or mouse.</summary>
        static bool UsingGamepad()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null) return false;
            double keys = Keyboard.current != null ? Keyboard.current.lastUpdateTime : 0d;
            double mouse = Mouse.current != null ? Mouse.current.lastUpdateTime : 0d;
            return pad.lastUpdateTime > keys && pad.lastUpdateTime > mouse;
        }
    }
}
