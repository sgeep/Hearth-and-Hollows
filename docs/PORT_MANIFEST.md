# Port Manifest

What happens to every file of the side-scroller prototype (tag `v0-sidescroller-prototype`, readable at `C:\Dev\Hearthdelve-v0`) in the top-down project.

- **Keep**: port as-is.
- **Adapt**: port with changes.
- **Drop**: replaced by TopDown Engine, MMFeedbacks or Super Text Mesh, or obsolete.

The **Ported** column shows whether the file is already in this project. "Adapt" files marked as ported were copied unchanged so the tests could pass first; their changes happen in the sub-milestone named in the reason. Files are copied together with their `.meta` files so asset references survive.

Totals for `Assets/_Project`: **94 Keep, 72 Adapt, 105 Drop** (271 files); 135 already ported.

Paths are relative to `Assets/_Project/`.


## Scripts: Core

| File | Status | Ported | Reason |
|---|---|---|---|
| `Events/CoreEvents.cs` | Adapt | yes | Ported as-is; `HitStopRequested` goes in 4b when MMFeedbacks freeze-frame replaces hit-stop |
| `Events/EventBus.cs` | Keep | yes | Event bus is engine-agnostic; TDE events get bridged onto it |
| `Hearthdelve.Core.asmdef` | Keep | yes | Assembly unchanged |
| `Input/InputMaps.cs` | Adapt | yes | Ported as-is; map names stay, but Dungeon/Tavern now feed TDE through an `InputSystemManager` subclass |
| `Layers.cs` | Adapt | yes | Ported as-is; layer names and indices must be reconciled with TDE's layers in 4a |
| `Minigames/IMinigame.cs` | Keep | yes | Minigame contract is unchanged |
| `Random/IRandom.cs` | Keep | yes | Seedable random used by pure logic and tests |
| `Services/GamePause.cs` | Adapt | yes | Ported as-is; hit-stop flag goes in 4b, and pause must cooperate with TDE's pause |
| `Services/GameSettings.cs` | Adapt | yes | Ported as-is; gains vibration on/off, intensity and reduced-intensity settings |
| `Services/HitStop.cs` | Drop | no | Replaced by MMFeedbacks freeze-frame inside the combined hit feedback |
| `Services/HitStopDriver.cs` | Drop | no | Replaced by MMFeedbacks freeze-frame inside the combined hit feedback |

## Scripts: Shared

| File | Status | Ported | Reason |
|---|---|---|---|
| `Economy/Economy.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Economy/EconomyConfig.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Game/DayCycle.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Game/DayRules.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Game/GameDatabase.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Game/GameFlow.cs` | Adapt | yes | Ported as-is; scene names and transitions change with the new scenes |
| `Game/GameState.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Hearthdelve.Shared.asmdef` | Keep | yes | Assembly unchanged |
| `Ingredients/IngredientDefinition.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Ingredients/IngredientEnums.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Ingredients/IngredientItem.cs` | Adapt | yes | Ported with one fix: `GetInstanceID` is an error on Unity 6.6 |
| `Inventory/FreshnessConfig.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Inventory/IngredientStack.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Inventory/Satchel.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Inventory/SatchelSettings.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Inventory/Storeroom.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Progression/TavernUpgradeDefinition.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Progression/Upgrades.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Recipes/MealBuffSettings.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Recipes/RecipeDefinition.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Recipes/RecipeMatcher.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Run/DeathPenalty.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Run/RunEvents.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Save/SaveData.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |
| `Save/SaveSystem.cs` | Keep | yes | Perspective-neutral game logic (economy, day loop, inventory, recipes, upgrades, saves) |

## Scripts: Dungeon

| File | Status | Ported | Reason |
|---|---|---|---|
| `Cameras/ScreenShaker.cs` | Drop | no | Replaced by MMFeedbacks camera shake / Cinemachine impulse |
| `Combat/AttackData.cs` | Adapt | yes | Ported as-is; hitbox offsets become 4-direction, frame data maps to TDE weapon timings |
| `Combat/ComboLogic.cs` | Keep | yes | Pure combo state machine; drives the TDE combo weapon timing |
| `Combat/Damage.cs` | Adapt | yes | Ported as-is; damage pipeline will sit behind TDE `Health`/`DamageOnTouch` in 4b |
| `Combat/MeleeHitbox.cs` | Drop | no | Replaced by TDE `MeleeWeapon` damage areas |
| `Combat/WeaponDefinition.cs` | Adapt | yes | Ported as-is; gains references to the TDE weapon prefab and Minifantasy weapon layers |
| `Debug/DungeonDebugOverlay.cs` | Adapt | no | Rebuilt in 4b against the TDE player (same debug keys) |
| `DungeonEvents.cs` | Keep | yes | Event payloads are perspective-neutral |
| `Enemies/AttackCycle.cs` | Keep | yes | Pure telegraph/attack/recover timing, reused by TDE AI actions |
| `Enemies/DummyBehaviour.cs` | Adapt | no | Behaviour rewritten as TDE AI actions/decisions; the rat is replaced (no rat art with an attack) |
| `Enemies/EnemyController.cs` | Drop | no | Replaced by TDE `Character` + `AIBrain` |
| `Enemies/EnemyDefinition.cs` | Adapt | yes | Ported as-is; gains TDE prefab and animation references; roster changes to fit the art |
| `Enemies/EnemyHealth.cs` | Adapt | no | Becomes a TDE `Health` subclass that reports kill context to the harvest rules |
| `Enemies/RatBehaviour.cs` | Adapt | no | Behaviour rewritten as TDE AI actions/decisions; the rat is replaced (no rat art with an attack) |
| `Enemies/ShroomBehaviour.cs` | Adapt | no | Behaviour rewritten as TDE AI actions/decisions; the rat is replaced (no rat art with an attack) |
| `Enemies/SlimeBehaviour.cs` | Adapt | no | Behaviour rewritten as TDE AI actions/decisions; the rat is replaced (no rat art with an attack) |
| `Enemies/SporeProjectile.cs` | Drop | no | Replaced by TDE `Projectile` |
| `Enemies/TelegraphIndicator.cs` | Adapt | no | Rebuilt as part of the enemy feedback pass (MMF_Player) |
| `Essence/EssenceConfig.cs` | Keep | yes | Tuning ScriptableObject unchanged |
| `Essence/EssenceMeter.cs` | Keep | yes | Pure Essence logic; backs the TDE `Health` subclass |
| `Harvest/HarvestRules.cs` | Keep | yes | Pure harvest rules and their tuning are unchanged |
| `Harvest/HarvestRulesConfig.cs` | Keep | yes | Pure harvest rules and their tuning are unchanged |
| `Harvest/HarvestSystem.cs` | Adapt | no | Listens to bridged TDE death events instead of `EnemyHealth` |
| `Harvest/IngredientPickup.cs` | Adapt | no | Rebuilt as a top-down pickup |
| `Harvest/PlayerPickupCollector.cs` | Adapt | no | Same satchel/swap logic on the TDE character |
| `Hearthdelve.Dungeon.asmdef` | Adapt | yes | Ported as-is; will add references to the TDE and MMTools assemblies |
| `Player/KinematicMover2D.cs` | Drop | no | Side-scroller movement; replaced by TDE `TopDownController2D` and abilities |
| `Player/MovementSettings.cs` | Drop | no | Side-scroller movement; replaced by TDE `TopDownController2D` and abilities |
| `Player/PlatformerMotor.cs` | Drop | no | Side-scroller movement; replaced by TDE `TopDownController2D` and abilities |
| `Player/PlayerController.cs` | Drop | no | Replaced by the TDE character, `InputSystemManager` subclass and animator |
| `Player/PlayerInputReader.cs` | Drop | no | Replaced by the TDE character, `InputSystemManager` subclass and animator |
| `Player/PlayerMovementConfig.cs` | Drop | no | Side-scroller movement; replaced by TDE `TopDownController2D` and abilities |
| `Player/PlayerVisuals.cs` | Drop | no | Replaced by the TDE character, `InputSystemManager` subclass and animator |
| `Player/PlayerVitals.cs` | Adapt | no | Becomes `EssenceHealth`, a TDE `Health` subclass backed by `EssenceMeter` |
| `Player/Timers.cs` | Keep | yes | Small pure timers used by combo and AI logic |
| `Run/DelveConfig.cs` | Keep | yes | Tuning ScriptableObject unchanged |
| `Run/DelveExit.cs` | Adapt | no | Same extraction rule on a top-down interactable |
| `Run/DelveRunController.cs` | Adapt | no | Same run/death/Lockbox flow, wired to TDE level and death events |

## Scripts: Tavern

| File | Status | Ported | Reason |
|---|---|---|---|
| `Customers/CustomerLogic.cs` | Keep | yes | Order, patience and preference logic is unchanged |
| `Customers/CustomerProfile.cs` | Keep | yes | Order, patience and preference logic is unchanged |
| `Customers/Preferences.cs` | Keep | yes | Order, patience and preference logic is unchanged |
| `Hearthdelve.Tavern.asmdef` | Adapt | yes | Ported as-is; will add references to the TDE and MMTools assemblies |
| `Minigames/ChopMinigame.cs` | Keep | yes | Panel minigames don't depend on perspective |
| `Minigames/GrillConfig.cs` | Keep | yes | Panel minigames don't depend on perspective |
| `Minigames/GrillMinigame.cs` | Keep | yes | Panel minigames don't depend on perspective |
| `Minigames/MinigameFactory.cs` | Keep | yes | Panel minigames don't depend on perspective |
| `Minigames/ServingConfig.cs` | Adapt | yes | Ported as-is; tuning fields follow the 2D serving rules in 4c |
| `Minigames/ServingMinigame.cs` | Adapt | yes | Ported as-is for now; 1D distance becomes 2D path length and real collisions in 4c |
| `Minigames/TapConfig.cs` | Keep | yes | Panel minigames don't depend on perspective |
| `Minigames/TapMinigame.cs` | Keep | yes | Panel minigames don't depend on perspective |
| `Scene/CustomerAgent.cs` | Adapt | no | Floor walkers become TDE characters following A* paths to tables |
| `Scene/StaffAgent.cs` | Adapt | no | Floor walkers become TDE characters following A* paths to tables |
| `Scene/Station.cs` | Adapt | no | Same service orchestration in a top-down room |
| `Scene/StewPotView.cs` | Adapt | no | Same service orchestration in a top-down room |
| `Scene/TavernContent.cs` | Keep | yes | Content list ScriptableObject unchanged |
| `Scene/TavernDebugOverlay.cs` | Adapt | no | Rebuilt in 4c (same debug keys) |
| `Scene/TavernDirector.cs` | Adapt | no | Same service orchestration in a top-down room |
| `Scene/TavernLayout.cs` | Adapt | no | Seat, door and queue x-positions become 2D points on the pathfinding grid |
| `Scene/TavernPlayer.cs` | Adapt | no | Station use, carrying and serving move onto the TDE character |
| `Service/ArrivalSchedule.cs` | Keep | yes | Service session, arrivals, stew pot and debug fill are unchanged |
| `Service/DebugStockFiller.cs` | Keep | yes | Service session, arrivals, stew pot and debug fill are unchanged |
| `Service/ServiceConfig.cs` | Adapt | yes | Ported as-is; walk speeds and reach become 2D values in 4c |
| `Service/ServiceSession.cs` | Keep | yes | Service session, arrivals, stew pot and debug fill are unchanged |
| `Service/ServiceSettings.cs` | Adapt | yes | Ported as-is; walk speeds and reach become 2D values in 4c |
| `Service/StewConfig.cs` | Keep | yes | Service session, arrivals, stew pot and debug fill are unchanged |
| `Service/StewPot.cs` | Keep | yes | Service session, arrivals, stew pot and debug fill are unchanged |
| `Staff/StaffCook.cs` | Keep | yes | Staff auto-resolve logic is unchanged |
| `Staff/StaffDefinition.cs` | Keep | yes | Staff auto-resolve logic is unchanged |
| `Staff/StaffPotCook.cs` | Keep | yes | Staff auto-resolve logic is unchanged |

## Scripts: UI

| File | Status | Ported | Reason |
|---|---|---|---|
| `Debug/GameFlowDebugOverlay.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Hearthdelve.UI.asmdef` | Adapt | yes | Ported as-is; will reference Super Text Mesh and uGUI instead of UI Toolkit |
| `Hud/DungeonHud.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Localization/Loc.cs` | Keep | yes | Localization lookup and key tables are UI-framework-neutral |
| `Localization/LoopLocKeys.cs` | Keep | yes | Localization lookup and key tables are UI-framework-neutral |
| `Localization/TavernLocKeys.cs` | Keep | yes | Localization lookup and key tables are UI-framework-neutral |
| `Screens/DeathScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Screens/MainMenuScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Screens/SlotPickerScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Screens/SlotView.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Screens/SwapPrompt.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/StationMinigamePanel.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/TavernHud.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/TavernMorningScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/TavernNightScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/TavernPrepScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/TavernResultsScreen.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |
| `Tavern/TavernUI.cs` | Drop | no | UI Toolkit screen; rebuilt in uGUI + Super Text Mesh in 4b/4c, using this as the spec |

## Editor tools

| File | Status | Ported | Reason |
|---|---|---|---|
| `Generation/ContentGenerator.cs` | Adapt | no | Same data-asset generation, with content re-themed to the Minifantasy roster |
| `Generation/GreyboxSceneBuilder.cs` | Drop | no | Built side-view scenes and prefabs; new top-down builders replace them |
| `Generation/LoopContentGenerator.cs` | Adapt | no | Same data-asset generation, with content re-themed to the Minifantasy roster |
| `Generation/LoopSceneBuilder.cs` | Drop | no | Built side-view scenes and prefabs; new top-down builders replace them |
| `Generation/Phase1Generator.cs` | Drop | no | Menu entry points for the old phases; Phase 4 gets its own |
| `Generation/Phase2Generator.cs` | Drop | no | Menu entry points for the old phases; Phase 4 gets its own |
| `Generation/Phase3Generator.cs` | Drop | no | Menu entry points for the old phases; Phase 4 gets its own |
| `Generation/PlaceholderArtGenerator.cs` | Drop | no | Placeholder art replaced by Minifantasy |
| `Generation/PrefabGenerator.cs` | Drop | no | Built side-view scenes and prefabs; new top-down builders replace them |
| `Generation/SceneKit.cs` | Adapt | no | Scene helpers reused; UI Toolkit helpers removed |
| `Generation/TavernArtGenerator.cs` | Drop | no | Placeholder art replaced by Minifantasy |
| `Generation/TavernContentGenerator.cs` | Adapt | no | Same data-asset generation, with content re-themed to the Minifantasy roster |
| `Generation/TavernSceneBuilder.cs` | Drop | no | Built side-view scenes and prefabs; new top-down builders replace them |
| `Hearthdelve.Editor.asmdef` | Adapt | no | Ported with the first editor tool; adds TDE and STM references |
| `Setup/EditorPaths.cs` | Keep | no | Path constants; extended with third-party art paths |
| `Setup/InputActionsBuilder.cs` | Adapt | no | Dungeon map changes for top-down (no jump or drop-through; adds aim and heavy attack) |
| `Setup/LocalizationBuilder.cs` | Keep | no | Still builds the string tables |
| `Setup/PixelArtImportPostprocessor.cs` | Adapt | no | Becomes the Minifantasy pipeline: point filter, no compression, 8 PPU, slicing |
| `Setup/ProjectConfigurator.cs` | Adapt | no | New settings: no gravity, Y-sort axis, 320x180 pixel-perfect, TDE layers |

## Tests: EditMode

| File | Status | Ported | Reason |
|---|---|---|---|
| `ChopAndStewTests.cs` | Keep | yes | Covers kept pure logic |
| `CombatTests.cs` | Adapt | yes | Damage tests kept; hit-stop tests retired with `HitStop` |
| `ComboLogicTests.cs` | Keep | yes | Covers kept pure logic |
| `CoreTests.cs` | Keep | yes | Covers kept pure logic |
| `DayLoopTests.cs` | Keep | yes | Covers kept pure logic |
| `EconomyTests.cs` | Keep | yes | Covers kept pure logic |
| `EssenceMeterTests.cs` | Keep | yes | Covers kept pure logic |
| `HarvestRulesTests.cs` | Keep | yes | Covers kept pure logic |
| `Hearthdelve.Tests.asmdef` | Keep | yes | Covers kept pure logic |
| `MotorTestUtil.cs` | Drop | no | Tests the dropped platformer motor |
| `PlatformerMotorTests.cs` | Drop | no | Tests the dropped platformer motor |
| `SatchelAndDeathTests.cs` | Keep | yes | Covers kept pure logic |
| `ScriptFileNameTests.cs` | Keep | yes | Covers kept pure logic |
| `StoreroomAndRecipeTests.cs` | Keep | yes | Covers kept pure logic |
| `TavernMinigameTests.cs` | Adapt | yes | Ported as-is; serving tests are rewritten for 2D in 4c |
| `TavernServiceTests.cs` | Keep | yes | Covers kept pure logic |

## Tests: PlayMode

| File | Status | Ported | Reason |
|---|---|---|---|
| `DayLoopSceneTests.cs` | Drop | no | Drives the old scenes and UI Toolkit; rewritten against the new scenes |
| `DungeonPlayModeTests.cs` | Drop | no | Drives the old scenes and UI Toolkit; rewritten against the new scenes |
| `GreyboxSceneTests.cs` | Drop | no | Drives the old scenes and UI Toolkit; rewritten against the new scenes |
| `Hearthdelve.Tests.PlayMode.asmdef` | Drop | no | Drives the old scenes and UI Toolkit; rewritten against the new scenes |
| `TavernSceneTests.cs` | Drop | no | Drives the old scenes and UI Toolkit; rewritten against the new scenes |

## Data assets

| File | Status | Ported | Reason |
|---|---|---|---|
| `Config/DelveConfig.asset` | Keep | yes | Tuning values carry over |
| `Config/EconomyConfig.asset` | Keep | yes | Tuning values carry over |
| `Config/EssenceConfig.asset` | Keep | yes | Tuning values carry over |
| `Config/FreshnessConfig.asset` | Keep | yes | Tuning values carry over |
| `Config/HarvestRulesConfig.asset` | Keep | yes | Tuning values carry over |
| `Config/PlayerMovementConfig.asset` | Drop | no | Tuning for the dropped platformer motor |
| `Customers/Customer_Adventurer.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Customers/Customer_Dwarf.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Customers/Customer_Villager.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Enemies/Enemy_CellarShroom.asset` | Adapt | yes | Ported as starting values; re-pointed at Minifantasy creatures and TDE prefabs |
| `Enemies/Enemy_GiantRat.asset` | Adapt | yes | Ported as starting values; no rat with an attack in the art, so it is replaced by Bat and Giant Spider |
| `Enemies/Enemy_GreenSlime.asset` | Adapt | yes | Ported as starting values; re-pointed at Minifantasy creatures and TDE prefabs |
| `Enemies/Enemy_TrainingDummy.asset` | Adapt | yes | Ported as starting values; no rat with an attack in the art, so it is replaced by Bat and Giant Spider |
| `GameDatabase.asset` | Keep | yes | Tuning and content lists carry over |
| `Ingredients/Ingredient_RatHaunch.asset` | Adapt | yes | Ported as starting values; re-themed to the rat's replacement (Bat / Giant Spider parts) |
| `Ingredients/Ingredient_RatLiver.asset` | Adapt | yes | Ported as starting values; re-themed to the rat's replacement (Bat / Giant Spider parts) |
| `Ingredients/Ingredient_ShroomCap.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Ingredients/Ingredient_SlimeCore.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Ingredients/Ingredient_SlimeGel.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Ingredients/Ingredient_SporeSac.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_CellarKebab.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_CellarStew.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_CoreTonic.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_Gelbrew.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_GrilledHaunch.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_OffalPottage.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Recipes/Recipe_ShroomSkewer.asset` | Adapt | yes | Ported as starting values; icons and names follow the Minifantasy roster |
| `Staff/Staff_Pip.asset` | Keep | yes | Tuning and content lists carry over |
| `Tavern/GrillConfig.asset` | Keep | yes | Tuning and content lists carry over |
| `Tavern/ServiceConfig.asset` | Keep | yes | Tuning and content lists carry over |
| `Tavern/ServingConfig.asset` | Adapt | yes | Ported as-is; tuning re-done for 2D serving in 4c |
| `Tavern/StewConfig.asset` | Keep | yes | Tuning and content lists carry over |
| `Tavern/TapConfig.asset` | Keep | yes | Tuning and content lists carry over |
| `Tavern/TavernContent.asset` | Keep | yes | Tuning and content lists carry over |
| `Upgrades/Upgrade_MaxEssence.asset` | Keep | yes | Tuning and content lists carry over |
| `Upgrades/Upgrade_SatchelSlots.asset` | Keep | yes | Tuning and content lists carry over |
| `Upgrades/Upgrade_TavernSeats.asset` | Keep | yes | Tuning and content lists carry over |
| `Weapons/Weapon_ButchersCleaver.asset` | Adapt | yes | Ported as starting values; the cleaver maps to the Minifantasy axe (slash) animations |

## Localization

| File | Status | Ported | Reason |
|---|---|---|---|
| `Locales/English (en).asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `LocalizationSettings.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `Tables/Content Shared Data.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `Tables/Content.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `Tables/Content_en.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `Tables/UI Shared Data.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `Tables/UI.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |
| `Tables/UI_en.asset` | Keep | yes | String tables carry over; new keys are added as UI is rebuilt |

## Input

| File | Status | Ported | Reason |
|---|---|---|---|
| `Hearthdelve.inputactions` | Adapt | yes | Ported as-is; Dungeon map changes for top-down |

## UI Toolkit assets

| File | Status | Ported | Reason |
|---|---|---|---|
| `DeathScreen.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `DungeonHud.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `Hearthdelve.uss` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `HearthdelvePanelSettings.asset` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `HearthdelveTheme.tss` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `MainMenu.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `SwapPrompt.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `Tavern.uss` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `TavernHud.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `TavernMinigame.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `TavernMorning.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `TavernNight.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `TavernPrep.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |
| `TavernResults.uxml` | Drop | no | UI Toolkit layout or style asset; replaced by uGUI prefabs |

## Prefabs

| File | Status | Ported | Reason |
|---|---|---|---|
| `Enemies/CellarShroom.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Enemies/GiantRat.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Enemies/GreenSlime.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Enemies/SporeProjectile.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Enemies/TrainingDummy.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Pickups/IngredientPickup.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Player/Player.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |
| `Tavern/CustomerAgent.prefab` | Drop | no | Side-view prefab; rebuilt on TDE |

## Scenes

| File | Status | Ported | Reason |
|---|---|---|---|
| `Boot.unity` | Drop | no | Side-view scene; new top-down scenes replace it |
| `CombatGreybox.unity` | Drop | no | Side-view scene; new top-down scenes replace it |
| `MainMenu.unity` | Drop | no | Side-view scene; new top-down scenes replace it |
| `TavernGreybox.unity` | Drop | no | Side-view scene; new top-down scenes replace it |

## Placeholder art

| File | Status | Ported | Reason |
|---|---|---|---|
| `Placeholders/PH_Bar.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Adventurer.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Dummy.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Dwarf.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Player.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Rat.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Shroom.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Slime.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Char_Villager.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_CleanKill.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Door.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Exclaim.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_ExclaimHeavy.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Marker.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Pickup.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Pip.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Plate.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_PotContents.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Slash.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Spore.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Station_Grill.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Station_Pass.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Station_StewPot.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Station_Tap.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Stool.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Table.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Tile_Floor.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Tile_OneWay.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Tile_Solid.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Tile_Wall.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/PH_Working.png` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/Tiles/Tile_OneWay.asset` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/Tiles/Tile_Solid.asset` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/Tiles/Tile_TavernFloor.asset` | Drop | no | Placeholder art replaced by Minifantasy |
| `Placeholders/Tiles/Tile_TavernWall.asset` | Drop | no | Placeholder art replaced by Minifantasy |

## Project files and docs

| File | Status | Reason |
|---|---|---|
| `CLAUDE.md` | Adapt | Edited in place for the pivot; every existing rule kept |
| `docs/GDD.md` | Adapt | Rewritten for top-down as v0.2, in Markdown; superseded design moved to an appendix |
| `docs/PROGRESS.md` | Keep | Phase 1-3 history kept; Phase 4 section added |
| `.gitignore` | Keep | Unity ignore rules unchanged (one line added for local Claude settings) |
| `.gitattributes` | Keep | Git LFS rules unchanged |
| `.vsconfig` | Keep | Editor tooling config |
| `Assets/AddressableAssetsData/` | Drop | Regenerated by the Localization package in the new project |
| `Assets/Settings/ and URP assets` | Drop | Replaced by the new project's URP 2D settings |
| `Packages/, ProjectSettings/` | Drop | Replaced by the Unity 6.6 project's |

## Tests

- The prototype had 237 project EditMode tests and 21 PlayMode tests.
- Retired with the code they covered: 24 platformer-motor tests and 4 hit-stop tests.
- **209 project EditMode tests are ported and pass** in batch mode on Unity 6000.6.4f1.
- The 21 PlayMode tests are not ported; new ones are written against the top-down scenes as each sub-milestone builds them.
