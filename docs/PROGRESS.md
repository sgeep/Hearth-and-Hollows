# Hearthdelve — Progress

_Last updated: 2026-09-29_

## Status

**Phase 1 (Combat Prototype): implemented and passing automated verification. Not yet playtested by a human.** Every Phase 1 "done" criterion is built and covered by tests. Feel tuning needs you in the editor.

| Phase 1 criterion | Where | Verified by |
|---|---|---|
| Run, jump (coyote time + jump buffer), dodge roll with i-frames, wall slide/jump, drop-through platforms | `PlatformerMotor`, `KinematicMover2D`, `PlayerController` | 24 EditMode motor tests; PlayMode landing/wall/one-way tests |
| Butcher's Cleaver 3-hit combo with hit-stop and screen shake | `ComboLogic`, `WeaponDefinition` (`Data/Weapons`), `HitStopDriver`, `ScreenShaker` | 7 EditMode combo tests; 4 hit-stop tests; PlayMode hit-kills-enemy test |
| Three enemies with distinct telegraphed attacks | `RatBehaviour` (crouch + flash + "!" → lunge), `SlimeBehaviour` (squash + landing marker → leap slam, super armor), `ShroomBehaviour` (swell + glow → arcing spore) | `AttackCycle` EditMode tests; scene smoke test |
| Kills drop ingredients through Harvest (clean kill / overkill / element → quality) | `HarvestRules`, `HarvestSystem`, `IngredientPickup` | 19 EditMode harvest tests; PlayMode kill-context test |
| Essence drains over time and on damage, forces exit at zero | `EssenceMeter`, `PlayerVitals`, `DelveRunController` | 10 EditMode Essence tests; PlayMode depletion + scene death-loop tests |
| Death screen: pick one slot (whole stack) to keep | `DeathScreen` (UI Toolkit), `DeathPenalty`, `PersistentStash` | 6 EditMode death-penalty tests; scene test picks slot 0 and verifies it is banked |
| Core logic has EditMode tests; clean batch compile | `Assets/_Project/Tests` | **107 project EditMode tests + 10 PlayMode tests, all passing; 0 compiler warnings** (the batch run reports one more EditMode test because the Addressables package adds a stub test) |

## Done

- **Setup:** Git LFS and ignore rules. Packages: Cinemachine 3.1.7, Localization 1.5.8, plus the URP / Input System / Test Framework versions that were already installed. Removed Visual Scripting, Multiplayer Center and Collab Proxy.
- **Assemblies:** `Core`, `Shared`, `Dungeon`, `Tavern` (empty), `UI`, `Editor`, `Tests`, `Tests.PlayMode`. Dungeon and Tavern never reference each other. UI talks to gameplay only through `EventBus` events in Core/Shared.
- **Project settings (via `Hearthdelve/Setup/Configure Project Settings`):**
  - Layers: Ground, OneWayPlatform, Player, Enemy, Pickup, Projectile.
  - Seven sorting layers.
  - Physics queries ignore triggers.
  - Fixed timestep of 1/60 s.
  - `Hearthdelve.inputactions` as the project-wide actions (Dungeon, Tavern, Minigame and UI maps).
- **Rendering:** URP 2D renderer, a global 2D light, and a Pixel Perfect Camera at 640×360 / 32 PPU with Cinemachine 3 (position composer, 2D confiner, pixel-perfect extension, impulse listener).
- **Localization:** English locale plus `UI` and `Content` string tables. All player-facing text uses table keys. The debug overlay is developer-only and not localized.
- **Content:** Butcher's Cleaver; Giant Rat, Green Slime, Cellar Shroom and a Training Dummy; six Cellars ingredients. All numbers live in ScriptableObjects under `Assets/_Project/Data`.
- **Greybox level:** `Assets/_Project/Scenes/CombatGreybox.unity`, laid out left to right:
  1. Start area with the training dummy.
  2. Wall-jump shaft.
  3. One-way platform tower.
  4. Arena with a slime and a rat.
  5. Shroom perch.

## Controls

| | Keyboard/Mouse | Gamepad |
|---|---|---|
| Move | A/D or arrows | Left stick / D-pad |
| Jump | Space | A / Cross |
| Attack (combo) | J or Left Mouse | X / Square |
| Dodge roll | Left Shift or L | B / Circle |
| Drop through platform | S + Space | Down + A |
| Swap when satchel full | E | D-pad Up |

**Debug** (editor/dev builds only):

| Key | Action |
|---|---|
| F1 | Show/hide the overlay |
| 1 / 2 / 3 / 4 | Weapon element: none / fire / ice / poison (to test Seared, Chilled and Inedible drops) |
| F2 | Toggle screen shake |
| F3 | Toggle hit-stop |
| F4 | Refill Essence |
| F5 | Set Essence to 3 (quick death-screen test) |
| F6 | God mode |
| F7 | Restart the level |

## Regenerating and verifying

- In the editor: **Hearthdelve → Generate → Phase 1 (All)**. It asks before overwriting the scene.
  - Data assets are only ever *created*; existing ones keep your tuning.
  - Prefabs are rebuilt every run.
  - Placeholder PNGs are kept if a file with the same name already exists.
- From the command line (close the editor first):

```
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -executeMethod Hearthdelve.Editor.Phase1Generator.RunBatch -logFile BatchLogs/generate.log
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults BatchLogs/editmode.xml -logFile BatchLogs/editmode.log
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults BatchLogs/playmode.xml -logFile BatchLogs/playmode.log
```

## Next

1. **You:** playtest the greybox and tune feel (see "Needs you in the editor" below). Report anything that feels off, and I'll adjust defaults or mechanics.
2. Phase 2 (Tavern prototype) planning. I'll propose a plan and wait for approval, per CLAUDE.md.

## Needs you in the editor

1. Open `Assets/_Project/Scenes/CombatGreybox.unity`. Set the Game view to a 16:9 resolution (1920×1080 scales 640×360 by exactly 3×). Press Play.
2. Tune while playing. Changes made in Play mode are kept after you exit.
   - `Data/Config/PlayerMovementConfig`, the Cleaver's combo frame data, and `HarvestRulesConfig` all apply live.
   - `EssenceConfig` and `DelveConfig` apply on the next level restart (F7).
3. Check the pixel-perfect camera. Grid snapping is **Upscale Render Texture** (most faithful to pixel art). If camera motion looks steppy, try **Pixel Snapping** on the Main Camera's Pixel Perfect Camera component and tell me which you prefer.
4. Before making a standalone build, build the Localization Addressables content once: *Window → Asset Management → Addressables → Groups → Build → New Build → Default Build Script*. The editor doesn't need this.

## Known issues / limitations

- **No human playtest yet.** Movement, combo and enemy numbers are first-pass guesses.
- **Air attacks** stop horizontal drift while the swing plays. That may feel sticky; easy to change once you've tried it.
- **Enemies** have no contact damage and don't block each other or the player; only their attack hitboxes hurt.
- **The Shroom** aims at where you stood when its telegraph started, so moving during the wind-up dodges it.
- **Harvest feed text** is built by joining two localized strings (e.g. "Clean kill!" + "Fine Rat Haunch ×2"). Some languages may need that as one formatted string later.
- **UI scale:** the UI uses a 1280×720 reference resolution so the default font stays readable. Pixel-art UI at 640×360 will come with a proper pixel font.
- **Lockbox storage:** `PersistentStash` (what you keep on death) is in memory only until the Phase 3 save system. After death the level restarts rather than returning to a tavern.
- **Deferred** (not in the Phase 1 criteria): ledge grab, double jump, harvest finisher move (the rules already support `IsFinisher`), freshness decay, extraction points, secondary weapon/skills, damage numbers, audio.
- **Localization lookups** are synchronous (`WaitForCompletion`). That's fine on PC and consoles but wouldn't work on WebGL.

## Decided (2026-09-29)

1. **Lockbox keeps the whole stack** in the chosen slot. (`DeathPenalty`; CLAUDE.md updated.)
2. **Stack size defaults to 3** per slot. Tune it in `Data/Config/DelveConfig`.
3. **Overkill tension stays.**
   - A green **check-mark icon** appears over a monster when your lightest hit would finish it without overkill *and* your weapon earns a Clean Kill bonus on its parts (e.g. the Cleaver on Rats, but not Slimes).
   - The overkill threshold scales with max HP, so the heavy hit only costs quality on small or nearly-dead monsters. It stays clean on tougher ones; this is covered by tests.
4. **Inedible drops stay.** The GDD now notes they'll get a later use (small sale value, poisons, or traps).
5. **The Slime stays uninterruptible.** Its wind-up now has its own tell: a red-orange **"!!"** icon instead of "!", a red-orange flash, and a visible tremble.

## Open design questions

1. **Stronghold defense events:** still undecided (not built; the Tavern scene stays combat-capable).
2. **Clean-kill cue scope:** the cue only shows when the weapon has a Clean Kill affinity for the monster's parts. Should monsters without affinity show a different cue for "finish without overkill"?
