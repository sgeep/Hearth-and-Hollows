using UnityEngine;

namespace Hearthdelve.UI.Localization
{
    /// <summary>
    /// Preloads the string tables when a scene starts, without blocking. Needed on the web,
    /// where synchronous Localization lookups don't work; harmless everywhere else.
    /// </summary>
    public sealed class LocalizationBoot : MonoBehaviour
    {
        void Start()
        {
            if (!Loc.IsReady) StartCoroutine(Loc.Preload());
        }
    }
}
