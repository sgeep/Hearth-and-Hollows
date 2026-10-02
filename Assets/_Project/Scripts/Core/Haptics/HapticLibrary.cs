using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Core.Haptics
{
    /// <summary>Ids of the starting haptic vocabulary (GDD §9A).</summary>
    public static class HapticIds
    {
        public const string TapLight = "Tap.Light";
        public const string TapFirm = "Tap.Firm";
        public const string HitHeavy = "Hit.Heavy";
        public const string HitTaken = "Hit.Taken";
        public const string KillClean = "Kill.Clean";
        public const string FinisherHarvest = "Finisher.Harvest";
        public const string PulseSuccess = "Pulse.Success";
        public const string BuzzFailure = "Buzz.Failure";
        public const string CueThreshold = "Cue.Threshold";
        public const string BumpSoft = "Bump.Soft";
        public const string BumpHard = "Bump.Hard";
        public const string CutRagged = "Cut.Ragged";
        public const string HeartbeatWarning = "Heartbeat.Warning";
        public const string BossTelegraph = "Boss.Telegraph";
        public const string BossPhaseChange = "Boss.PhaseChange";
    }

    /// <summary>Every haptic pattern in the game, looked up by id.</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Haptics/Library", fileName = "HapticLibrary")]
    public sealed class HapticLibrary : ScriptableObject
    {
        public List<HapticPattern> patterns = new();

        public HapticPattern Find(string id)
        {
            foreach (var p in patterns) if (p != null && p.id == id) return p;
            return null;
        }
    }
}
