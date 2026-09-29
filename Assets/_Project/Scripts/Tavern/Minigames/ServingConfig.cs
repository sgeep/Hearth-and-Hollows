using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    [CreateAssetMenu(menuName = "Hearthdelve/Config/Serving", fileName = "ServingConfig")]
    public sealed class ServingConfig : ScriptableObject
    {
        [Header("Design target: 5–10 s per delivery to a mid-floor table at default tuning (≈6 s for 10 tiles)")]
        public ServingSettings serving = ServingSettings.Default;
    }
}
