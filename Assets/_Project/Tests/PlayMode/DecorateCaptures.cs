using System.Collections;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Not a check: renders Decorate Mode at 320×180 to <c>BatchLogs/</c> (browsing, carrying a piece somewhere it won't
    /// go, the storage panel, the layout check) so its layout can be looked at. Explicit, so it only runs when asked for.
    /// </summary>
    [Explicit]
    public class DecorateCaptures : LookTestFixture
    {
        static DecorateMode Mode => DecorateMode.Instance;

        [UnityTest]
        public IEnumerator CaptureDecorateMode()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            Mode.Enter();
            Mode.SetCursor(new Vector2Int(9, 7));
            yield return null;
            yield return null;
            TavernEveningCaptures.Capture("BatchLogs/decorate_browse.png");

            Mode.SetCursor(new Vector2Int(26, 9));
            Mode.PickUp();
            Mode.SetCursor(new Vector2Int(4, 7));
            yield return null;
            yield return null;
            TavernEveningCaptures.Capture("BatchLogs/decorate_cant_go.png");
            Mode.SetCursor(new Vector2Int(12, 10));
            yield return null;
            TavernEveningCaptures.Capture("BatchLogs/decorate_carry.png");
            Mode.Place();

            Mode.SetCursor(new Vector2Int(20, 12));
            Mode.Store();
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            screen.OpenStorage();
            yield return null;
            yield return null;
            TavernEveningCaptures.Capture("BatchLogs/decorate_storage.png");
            screen.OpenCheck();
            yield return null;
            yield return null;
            TavernEveningCaptures.Capture("BatchLogs/decorate_check.png");
        }
    }
}
