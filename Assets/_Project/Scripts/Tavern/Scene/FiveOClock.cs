using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Five o'clock (4h Checkpoint A): the first time the keeper is inside Tally Ho! once the village has wound down, Orik says
    /// so (<see cref="SurfaceConversations.OrikFive"/>), once in the game (recorded with the onboarding prompts, so no save
    /// change). It changes nothing: the evening still begins only when the player chooses.
    /// </summary>
    public sealed class FiveOClock : MonoBehaviour
    {
        public const string SeenId = "beat:orik_five";

        void Update()
        {
            GameFlow flow = GameFlow.Instance;
            TavernDirector director = TavernDirector.Instance;
            if (flow == null || !flow.InGame || director == null || director.Phase != TavernPhase.Daytime) return;
            if (SurfaceTime.Band != SurfaceBand.Evening || flow.State.Story.SeenHints.Contains(SeenId)) return;
            SurfaceArea here = SurfaceArea.Current;
            if (here == null || !here.Indoors || MenuPause.IsPaused || flow.IsLoading) return;
            if (KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook != null) return;
            if (DecorateMode.Instance != null && DecorateMode.Instance.IsActive) return;
            IConversationService talk = StoryServices.Conversations;
            if (talk == null || talk.IsTalking || !talk.HasConversation(SurfaceConversations.OrikFive)) return;
            if (talk.Play(SurfaceConversations.OrikFive)) flow.MarkHintSeen(SeenId);
        }
    }
}
