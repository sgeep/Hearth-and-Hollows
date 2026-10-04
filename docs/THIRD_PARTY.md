# Third-Party Code and Assets

Rules for working with these are in `CLAUDE.md` ("Third-party code"). The repo stays **private**: vendor code and Minifantasy files must not be redistributed.

## Engine and packages

- **Unity 6000.6.4f1** (moving to 6.7 LTS when released).
- Package changes made by the TopDown Engine import (2026-10-02):
  - Added: `com.unity.2d.pixel-perfect` 6.0.0, `com.unity.ai.navigation` 2.0.14, `com.unity.mathematics` 1.4.0, `com.unity.nuget.newtonsoft-json` 3.2.2.
  - Updated: `com.unity.cinemachine` 3.1.7 → 6.6.0, `com.unity.inputsystem` 1.19.0 → 1.20.0, URP and render-pipelines core 17.5.0 → 17.6.0, `com.unity.ugui` 2.5.0 → 2.6.0, `com.unity.2d.tilemap.extras` → 9.0.1.
- Added by us: `com.unity.localization` **1.5.13** (1.5.8 does not compile on Unity 6.6; it pulls in Addressables 2.11.2).
- Removed by us: `com.unity.learn.iet-framework` (template tutorial), `com.unity.visualscripting`, `com.unity.collab-proxy`.

## Vendors

| Vendor | Version | Folder | License | What we use |
|---|---|---|---|---|
| TopDown Engine (More Mountains) | 5.0 | `Assets/TopDownEngine` | Unity Asset Store EULA, plus `Assets/TopDownEngine/license.txt` | Character controller, abilities, combat, enemy AI, camera, rooms |
| MMFeedbacks / MMTools / MMInterface / InventoryEngine | Bundled with TDE 5.0 | `Assets/TopDownEngine/ThirdParty/MoreMountains` | Same as TDE | All game-feel feedbacks. This is the **only** copy of MMFeedbacks/MMTools. |
| Nice Vibrations (Lofelt) | 4.1.2, Lofelt Studio SDK 1.3.4 | `Assets/Feel/NiceVibrations` | Unity Asset Store EULA (obtained as part of Feel), plus `3RD-PARTY-LICENSES.md` in its folder | Gamepad rumble |
| Feel (More Mountains) | 6.1 | not imported | Unity Asset Store EULA | **Licensed, but only its `NiceVibrations` folder is imported.** Never import Feel's `MMFeedbacks`, `MMTools` or demo folders. |
| Super Text Mesh (Kai Clavier) | version not stated in the package | `Assets/Clavian/SuperTextMesh` | Unity Asset Store EULA, plus `3rdPartyComponentLicense.txt` | All player-facing text (uGUI and world space), Ultra shader under URP |
| Silver font (Poppy Works) | font version field "Version 1.0", copyright "2019 Poppy Works" (the release date of our copy isn't recorded) | `Assets/_Project/Fonts/silver/Silver.ttf` | CC BY 4.0, with a budget condition (see license notes) | All player-facing text (the only game font) |
| Minifantasy (Krishna Palacio) | per pack; imported so far: Creatures 3.3, Dungeon 2.3, A Myriad of NPCs 1.0, UI Overhaul 1.0, Crafting And Professions II 1.0, Dwarven Kingdom 1.0, and the Tavern Indoor, Loot Icons, Giant Spider and Hole Entrances And Ropes exclusives (see `docs/ASSET_MAP.md`) | raw: `C:\Dev\Minifantasy` (outside the repo); imported: `Assets/ThirdParty/Minifantasy/<Pack>/` | Each pack's `CommercialLicense.txt` | All art |

### Licensed or planned, not imported

Listed so nobody mistakes them for missing installs. Versions and license notes are added to the table above when each is actually imported.

| Asset | Status | When |
|---|---|---|
| Dialogue System for Unity (Pixel Crushers) | Licensed, not imported | 4g. Replaces Yarn Spinner, which is not used. |
| Quest Machine (Pixel Crushers) | Licensed, not imported | 4g |
| Love/Hate (Pixel Crushers) | Planned, not purchased | Decided when 4g is planned |
| Minifantasy Portrait Generator (Krishna Palacio) | Planned, not imported | 4g; recorded in `docs/ASSET_MAP.md` when inspected |

### License notes

- **TopDown Engine:** code and visual assets may be used in our game; nothing may be redistributed. Its music is demo-only and must not be reused. The license does not grant reuse of its sound effects either, so we don't ship any TDE audio.
- **Minifantasy:** assets may be used and edited in a commercial game. They must not be redistributed or resold as assets. We must **credit Krishna Palacio** in the game's credits and **send him a link to the project on completion**.
- **Silver (font):** verified 2026-10-04 from the official page, https://poppyworks.itch.io/silver (the font's own name table gives only the copyright, the designers and http://poppy.works/; no license file came with it). Terms: Creative Commons **Attribution 4.0 International (CC BY 4.0)**, so commercial use and adaptation are allowed with attribution to Poppy Works (credit line in `docs/CREDITS.md`). The page adds: *if the production's budget exceeds $100,000 USD in total spend or earnings, contact hello@poppy.works to license the font.* That threshold is an open item for the project owner before release (PROGRESS.md, open questions); it doesn't affect development.
- **Super Text Mesh:** two sample fonts (Itim, Walibi) carry their own licenses; we removed the samples and don't use those fonts.

## Our additions inside vendor folders

Vendor code is never modified. These are the only files we added inside a vendor folder:

- `Assets/Clavian/SuperTextMesh/Clavian.SuperTextMesh.asmdef`
- `Assets/Clavian/SuperTextMesh/Scripts/Editor/Clavian.SuperTextMesh.Editor.asmdef`

Super Text Mesh ships without assembly definitions, so it would compile into `Assembly-CSharp`, which our `Hearthdelve.*` assemblies cannot reference. After an STM upgrade, check that both files are still present.

## Removed vendor folders

Removed before the first commit to keep Git LFS usage small (Assets went from 488 MB to 92 MB; 69 MB is in LFS). **After upgrading a vendor, remove these again.**

| Folder | Why |
|---|---|
| `Assets/TopDownEngine/Demos` | Demo scenes and art we won't ship |
| `Assets/TopDownEngine/Common/ScriptsInputSystem/MinimalScene3D_InputSystem*.unity`, `Lighting`, `Prefabs` | Input System demo scenes that reference removed demo assets (the scripts and input actions stay) |
| `…/MoreMountains/MMFeedbacks/Demos` | Demos |
| `…/MoreMountains/MMTools/Demos` | Demos |
| `…/MoreMountains/InventoryEngine/Demos` | Demos (the engine itself stays; TDE references it) |
| `Assets/Clavian/SuperTextMesh/Sample` | Samples (documentation stays) |
| `Assets/Feel/NiceVibrations/Demo` | Demo with its own assembly |
| `Assets/Feel/NiceVibrations/OlderVersions` | Packages of old API versions |
| `Assets/Feel/NiceVibrations/Plugins/iOS`, `Plugins/Android` | Mobile-only native plugins (the Windows/macOS editor plugins stay) |
| `Assets/Feel/NiceVibrations/HapticSamples` | Sample haptic clips; re-add individual clips only if we use them |
| `Assets/Welcome` | Unity template tutorial |

TDE's `Koala2D` and `Minimal2D` demos were copied to `C:\Dev\TDE-Reference` (outside the repo) for reading.

### Known dangling references after the trim

These kept vendor files point at removed demo assets. None is used by our game; build our own instead of using them.

- TDE `Common/Prefabs/GUI/UICamera.prefab` (inventory and mobile-control sprites) and `Common/Animations/StartScreenKoala.anim`.
- MMTools `MMDebugMenu` prefabs, `MMDebugOnScreenConsole`, `MMFloatingTextMeshPro` and `MMSaveLoadTestScene` (Lato demo fonts).
- MMTools additive loading-screen scenes (a Nice Vibrations demo font).
- STM `Resources/STMFonts/itim`, `walibi` and `Resources/STMQuads/bubble`, `wood` (sample fonts and textures).

## Nice Vibrations notes

- TDE's MMFeedbacks already contains the haptic feedbacks (`MMF_NVClip`, `MMF_NVContinuous`, `MMF_NVControl`, `MMF_NVEmphasis`, `MMF_NVPreset`). They compile only when the scripting define `MOREMOUNTAINS_NICEVIBRATIONS_INSTALLED` is set; Nice Vibrations sets it for the **selected build target only**. *Hearthdelve → Setup → Configure Project Settings* sets it for both Standalone and Web.
- To re-import from the Feel package: tick only `Feel/NiceVibrations` and `Feel/readme.txt`; untick `FeelDemos*`, `MMFeedbacks` and `MMTools`.
- Our `HapticService` mixes the named patterns itself and hands Nice Vibrations a short rumble each frame (`GamepadRumbler.Load` / `Play`), so the motors switch off by themselves if updates stop. Nice Vibrations' own pattern playback uses `System.Timers`, which never fire in a web build; the service does not rely on it and uses a no-op output on the web.
- Off mobile, rumble goes through the Input System's `Gamepad.SetMotorSpeeds` (low and high motor). Controller support therefore follows the Input System; see `docs/PROGRESS.md` for the test results.
