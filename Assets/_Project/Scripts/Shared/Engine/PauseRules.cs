using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// When the pause menu may open (4i-A, A2; the decision is pure, EditMode-tested). The rule the game follows everywhere:
    /// <list type="bullet">
    /// <item><b>Start</b> (controller) or <b>Esc</b> (keyboard) opens the pause menu while the keeper is free to move (the day, arrival
    /// day, service, the Hollows) and on the evening's and night's own screens (Prep, the results, the night, the delve's result).</item>
    /// <item>Anything else that's open owns <b>Esc / B</b> (a station, a panel, a question, Decorate Mode, a conversation): it backs out
    /// first, and the pause menu waits until it has. Start in Decorate Mode stays its catalog.</item>
    /// <item>Never during a load or a scene the story is playing (Gimp's night).</item>
    /// </list>
    /// </summary>
    public static class PauseRules
    {
        static readonly HashSet<object> s_Blocks = new();

        /// <summary>Something (a story scene) has the pause menu held off until it's done.</summary>
        public static bool IsBlocked => s_Blocks.Count > 0;

        public static void Block(object key)
        {
            if (key != null) s_Blocks.Add(key);
        }

        public static void Unblock(object key)
        {
            if (key != null) s_Blocks.Remove(key);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Blocks.Clear();

        /// <param name="inGame">A game is being played (not the main menu).</param>
        /// <param name="loading">Scenes are changing, or the transition covers the screen.</param>
        /// <param name="talking">A conversation is up.</param>
        /// <param name="blocked">A story scene holds it off (<see cref="IsBlocked"/>).</param>
        /// <param name="onFoot">The keeper's own controls are live (the tavern's or the Hollows' map).</param>
        /// <param name="menuPaused">Another menu has paused the game (the swap prompt, a power, the death screen).</param>
        /// <param name="onPhaseScreen">One of the screens that are the moment itself shows: Prep, the results, the night, the delve's result.</param>
        public static bool CanOpen(bool inGame, bool loading, bool talking, bool blocked, bool onFoot, bool menuPaused, bool onPhaseScreen) =>
            inGame && !loading && !talking && !blocked && ((onFoot && !menuPaused) || onPhaseScreen);

        /// <summary>4i-B: an Esc or Start pressed while a scene fades is kept this long, and opens the menu if it can by then.</summary>
        public const float QueueSeconds = 2.5f;

        /// <summary>Whether a press made during a fade should be kept: only then, and only in a game (any other refusal is meant).</summary>
        public static bool Queues(bool inGame, bool loading) => inGame && loading;

        /// <summary>Whether a press kept at <paramref name="pressedAt"/> still counts at <paramref name="now"/>.</summary>
        public static bool StillQueued(float pressedAt, float now) => pressedAt >= 0f && now - pressedAt <= QueueSeconds;
    }
}
