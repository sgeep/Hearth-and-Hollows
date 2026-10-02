using System.IO;
using UnityEditor;

namespace Hearthdelve.Editor
{
    /// <summary>Where generated assets live. Keep in sync with the GDD §10.7 folder layout.</summary>
    public static class EditorPaths
    {
        public const string Root = "Assets/_Project";
        public const string Art = Root + "/Art";
        public const string Tiles = Art + "/Tiles";
        public const string Audio = Root + "/Audio/SFX";
        public const string Data = Root + "/Data";
        public const string Ingredients = Data + "/Ingredients";
        public const string Enemies = Data + "/Enemies";
        public const string Weapons = Data + "/Weapons";
        public const string Config = Data + "/Config";
        public const string Haptics = Data + "/Haptics";
        public const string Animations = Data + "/Animations";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";
        public const string LookTestDungeonScene = Scenes + "/LookTest_Dungeon.unity";
        public const string LookTestTavernScene = Scenes + "/LookTest_Tavern.unity";
        public const string Settings = Root + "/Settings";
        public const string InputActions = Settings + "/Hearthdelve.inputactions";
        public const string Localization = Root + "/Localization";

        /// <summary>Imported Minifantasy art: only what we use, one folder per pack (CLAUDE.md).</summary>
        public const string Minifantasy = "Assets/ThirdParty/Minifantasy";
        /// <summary>The raw packs, outside the repo.</summary>
        public const string MinifantasySource = "C:/Dev/Minifantasy";

        /// <summary>Creates a project folder (and parents) through the AssetDatabase so metas are generated.</summary>
        public static void Ensure(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
