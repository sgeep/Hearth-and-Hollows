using System;
using System.Text.RegularExpressions;

namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// The game's version as builds stamp it (4i-D): the milestone, then the build's number (commits on main) and the commit it was
    /// built from, so a tester's report names exactly what they played: <c>0.4i-d.388+4fafd6c9</c>, or <c>…-dirty</c> when the
    /// build had uncommitted changes in it. The builds set it as the player's version (<c>Application.version</c>), which the
    /// main menu and the pause menu show.
    /// </summary>
    public static class VersionStamp
    {
        /// <summary>The milestone being built (the version until a build stamps the rest).</summary>
        public const string Milestone = "0.4i-d";

        static readonly Regex k_Pattern = new(@"^\d+\.\d+[a-z](-[a-z])?\.\d+\+[0-9a-f]{7,40}(-dirty)?$");

        public static string Format(string milestone, int build, string commit, bool dirty)
        {
            if (string.IsNullOrWhiteSpace(milestone)) throw new ArgumentException("A milestone is needed.", nameof(milestone));
            if (build < 0) throw new ArgumentOutOfRangeException(nameof(build));
            string sha = string.IsNullOrWhiteSpace(commit) ? "unknown" : commit.Trim();
            if (sha.Length > 8) sha = sha.Substring(0, 8);
            return $"{milestone}.{build}+{sha}{(dirty ? "-dirty" : string.Empty)}";
        }

        /// <summary>
        /// What the menus show: the milestone and the build (<c>0.4i-d.392</c>), which name one commit on main; the commit's hash
        /// stays in the build's <c>version.txt</c> and the log (the menu's corner has room for about eighty pixels).
        /// </summary>
        public static string Short(string version)
        {
            if (string.IsNullOrEmpty(version)) return string.Empty;
            int plus = version.IndexOf('+');
            return plus < 0 ? version : version.Substring(0, plus);
        }

        /// <summary>A stamped version's shape (the build-script test checks the builds' strings against it).</summary>
        public static bool IsStamped(string version) => !string.IsNullOrEmpty(version) && k_Pattern.IsMatch(version);
    }
}
