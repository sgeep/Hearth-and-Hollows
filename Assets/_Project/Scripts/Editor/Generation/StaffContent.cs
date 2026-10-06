using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The tavern's staff (4f Checkpoint C): Pip Marrowby, server and bookkeeper, and Gunta Ashbelly, the cook (stable ids
    /// <c>pip</c>, <c>gunta</c>; canonical, never renamed). Gunta is made once (her tuning is kept); both are on the
    /// tavern's roster. Also the Butcher Block's tuning asset.
    /// </summary>
    public static class StaffContent
    {
        const string k_Staff = EditorPaths.Data + "/Staff";
        const string k_TavernContent = EditorPaths.Data + "/Tavern/TavernContent.asset";
        public const string ButcherConfigPath = EditorPaths.Data + "/Tavern/ButcherConfig.asset";

        public static void Build()
        {
            var tavern = AssetDatabase.LoadAssetAtPath<TavernContent>(k_TavernContent);
            var pip = AssetDatabase.LoadAssetAtPath<StaffDefinition>($"{k_Staff}/Staff_Pip.asset");
            if (pip != null && pip.id != StaffIds.Pip)
            {
                pip.id = StaffIds.Pip;
                EditorUtility.SetDirty(pip);
            }
            // Gunta: a steady hand (she reaches her cap nearly every time), capped below a competent keeper's work (4f
            // Checkpoint D balance: at 0.85 she out-cooked most players, so handing her the grill was simply better).
            StaffDefinition gunta = LookTestContent.LoadOrCreate<StaffDefinition>($"{k_Staff}/Staff_Gunta.asset", created =>
            {
                created.skill = 0.75f;
                created.qualityCap = 0.75f;
                created.restBetweenJobs = 0.8f;
                created.placeholderColor = new Color(0.72f, 0.2f, 0.18f);
            });
            gunta.id = StaffIds.Boog;
            gunta.displayName = new LocalizedString(Loc.ContentTable, "staff.gunta");
            EditorUtility.SetDirty(gunta);

            ButcherConfig butcher = LookTestContent.LoadOrCreate<ButcherConfig>(ButcherConfigPath);
            if (tavern != null)
            {
                if (pip != null && !tavern.staff.Contains(pip)) tavern.staff.Insert(0, pip);
                if (!tavern.staff.Contains(gunta)) tavern.staff.Add(gunta);
                tavern.butcher = butcher;
                EditorUtility.SetDirty(tavern);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
