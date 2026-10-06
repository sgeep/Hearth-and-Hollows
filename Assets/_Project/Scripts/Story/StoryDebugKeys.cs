using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Story
{
    /// <summary>
    /// Story playtest keys (4g Checkpoint A), in the editor and development builds only, in Boot: F1 talks to Boog in the tavern
    /// whatever the phase (in play you walk up to him during service and press Interact); Shift+F1 brings the first boss trophy
    /// home again (back in storage, waiting for its homecoming in Decorate Mode), so the tusks proof can be repeated on a save
    /// that already hung them.
    /// </summary>
    public sealed class StoryDebugKeys : MonoBehaviour
    {
        void Awake()
        {
            if (!Debug.isDebugBuild) enabled = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            GameFlow flow = GameFlow.Instance;
            if (keyboard == null || flow == null || !flow.InGame || flow.IsLoading || !keyboard.f1Key.wasPressedThisFrame) return;
            if (keyboard.shiftKey.isPressed)
            {
                string trophy = flow.DebugTrophyHomecoming();
                Debug.Log(trophy != null
                    ? $"[Hearthdelve] {trophy} is home again: open Decorate Mode to hang it."
                    : "[Hearthdelve] No boss trophy to bring home.");
                return;
            }
            IConversationService talk = StoryServices.Conversations;
            if (flow.LoadedScene == GameScenes.Tavern && talk != null && !talk.IsTalking) talk.Talk(CharacterIds.Boog);
        }
    }
}
