# 4g plan: story, quests, character creation and relationship reactivity

> **Status: proposed 2026-10-06, awaiting the owner's approval.** Nothing of 4g is built and no Pixel Crushers package is imported. 4f is complete (signed off 2026-10-06).

## 0. Corrections to the brief, from the repository

- **Names.** The brief's Step 5 says "the Sunken Flagon" and "Gunta". Since 2026-10-06 they are **Tally Ho!** and **Boog** (GDD Decided 35). Pip and Old Tamsin are unchanged. This plan uses the current names.
- **Love/Hate's standing in the docs.** CLAUDE.md and GDD §2.7 say Love/Hate is "planned but not purchased or imported", and GDD §11.1 says 4g decides whether it joins. The brief decides it joins 4g. Once you confirm it's purchased, the docs change in Step 1.
- **Protagonist art (affects Step 4).** The locked rule is "a pre-clothed body with palette swaps; all bodies share one animation set". Of the Creatures pack's humanoids, only four are **clothed and fully animated** in the player's exact sheet layout. The base Human, Elf, Dwarf, Halfling, Orc and Goblin are **unclothed** (an outline and five skin tones, no hair or clothes), so they can't be the protagonist as they are. See §9.

## 1. What 4f left us (the baseline)

- **Facts** (`Shared/Game/GameFacts.cs`, plus `BossFirstCleared` in `GameFlow`). These are stable-id structs published on `EventBus<T>`:
  - `CurioBroughtHome`, `FurniturePlaced`, `TrophyDisplayed`;
  - `MarketPurchase`, `PartButchered` (score, by whom), `StaffWorkDone`;
  - `CustomerRequestIssued`, `CustomerRequestCompleted`, `CustomerRequestFailed` (patron profile id, visit id);
  - `DishServed` (dish, patron, quality, gold, tip, request), `ServiceCompleted`;
  - `BossDefeated`, `BossFirstCleared`.

  Nothing listens to them yet. One gap: `DishServed` doesn't carry how well the dish matched the patron's taste, which "served a favorite" needs (§7).
- **EventBus.** `Core/Events/EventBus<T>` is static and typed, with `Subscribe`/`Publish`. Gameplay publishes and never knows who listens.
- **Saves.** `Shared/Save/SaveSystem` is static, version **7**, using `JsonUtility` over `SaveData`, migrations that grant once, and `WebStorage.Flush` for the web. `GameFlow.Save()` is the only writer, at the day's safe points.
- **Characters today:**
  - **Staff:** `StaffDefinition` (id `pip`, `gunta` for Boog; a localized name, skill, cap) on a prefab with `NpcLook` and `CharacterSpriteAnimator`.
  - **Patrons:** three archetype `CustomerProfile`s (villager, adventurer, dwarf) with traits and layered appearance pools. Each visit gets a transient `VisitId`. There are no named villagers yet; those come in 4h.
  - **The player:** the Human Townsfolk with the full combat animation set. There's no customization yet.
- **Text.**
  - Unity Localization 1.5.x tables `UI` and `Content`, written by `LocalizationBuilder` from the `*LocKeys.English` sources.
  - Every UI text is uGUI + Super Text Mesh, styled from the type scale (`TypeScale`, `StyledText`: Display 3×, Heading 2×, Body, Secondary and Prompt 1×).
  - `TypographyTests` check every fixed string's fit, and `TextOverlapTests` check for overlaps.
- **Input.** Action maps are Dungeon, Tavern, Minigame, UI and Decorate. Menus use the UI map; Decorate and minigames don't go through TDE.
- **Assemblies:**
  - `Core` (pure);
  - `Shared` (Core, Localization, TDE);
  - `Dungeon` and `Tavern` (Core, Shared, TDE), which never reference each other;
  - `UI` (Core, Shared, Tavern, STM, Localization);
  - `Editor`, `Tests`.
- **Stale docs to correct when 4g starts:**
  - CLAUDE.md and GDD §2.7 and §11.1 on Love/Hate's status.
  - GDD §2.4: Acts II–IV still await revision, and Act I's "wandering cook" was updated.
  - GDD §6.4: Morale and Cheer are "to confirm".
  - `THIRD_PARTY.md`'s "licensed, not imported" rows.
  - **Yarn Spinner** appears only as "replaced by/not used" history notes (CLAUDE.md, GDD v0.3 note, `THIRD_PARTY.md`); there's no live Yarn reference anywhere in code or docs. The leftover is to make the wording past-tense where a sentence still reads as a plan.

## 2. Packages: versions, integration, order, exclusions

**Versions** (checked 2026-10-06 on the Unity Asset Store and Pixel Crushers' release notes):

| Product | Version | Released | Unity | Notes |
|---|---|---|---|---|
| Dialogue System for Unity | **2.2.74** | 2026-09-12 | release notes: "Updated for Unity 6.6" | native Super Text Mesh UI support; a Unity Localization bridge; v2.2.74 "exports all localized content into Localization Package locale tables" |
| Quest Machine | **1.2.74** | 2026-09-12 | 6000.0+ and 2022.3+ | quest text uses Pixel Crushers Text Tables; saves through the Pixel Crushers Save System |
| Love/Hate | **1.10.74.1** | 2026-09-12 | 6000.0+ and 2022.3+ | serialization strings and savers |

The project runs Unity **6000.6.4f1**. All three share **Pixel Crushers Common** (`Plugins/Pixel Crushers/Common`); one copy, upgraded together. We'll use those exact versions, record them in `THIRD_PARTY.md`, and not upgrade mid-milestone.

**Integration packages** (inside the products; no extra purchase):
1. Dialogue System's **assembly definitions**: `Plugins/Pixel Crushers/Dialogue System/Scripts/DialogueSystemAssemblyDefinitions.unitypackage`, giving `PixelCrushers`, `PixelCrushersEditor`, `DialogueSystem` and `DialogueSystemEditor`. **Required**, because our code is in asmdefs and can't reference Plugins code compiled into the default firstpass assembly.
2. Quest Machine's and Love/Hate's assembly definition packages, from each product's `Scripts` folder (names to confirm on import).
3. **Dialogue System ↔ Quest Machine:** first `Plugins/Pixel Crushers/Common/Third Party Support/Dialogue System Support.unitypackage` if present, then `Plugins/Pixel Crushers/Quest Machine/Third Party Support/Dialogue System Support.unitypackage` (the order Quest Machine's manual gives). We use it only if its bridge earns its place; quests can also be driven entirely through our adapter (§6).
4. **Dialogue System ↔ Love/Hate:** `Plugins/Pixel Crushers/LoveHate/Third Party Support/Dialogue System Support.unitypackage`, plus its `LoveHateDialogueSystemAssemblyDefinitions.unitypackage`. Gives `LoveHateLua` and the persistent faction savers.
5. **Quest Machine ↔ Love/Hate:** the package in Quest Machine's `Third Party Support`. **Not imported** unless a quest needs Love/Hate conditions directly; our adapters already join them.
6. **Dialogue System ↔ Unity Localization:** the Localization Package support (the **Dialogue System Localization Package Bridge** and the *DS To Loc* window), with an asmdef referencing `Unity.Localization`.
7. **Super Text Mesh:** native in 2.2.74 (Super Text Mesh Subtitle Panel, Menu Panel, Response Button and Bark UI, Localize Super Text Mesh). It's enabled by a define symbol from the Welcome Window, and the `DialogueSystem` asmdef needs a reference to our `Clavian.SuperTextMesh` asmdef. That's a configuration change in a vendor asmdef, recorded in `THIRD_PARTY.md` as the STM asmdefs are.

**Import order:**
1. Dialogue System (with Common).
2. Its asmdef package.
3. Quest Machine (Common: keep the newest copy).
4. Its asmdefs.
5. Love/Hate.
6. Its asmdefs.
7. The integration packages above (Common's first, then the product's).
8. Define symbols: `USE_PHYSICS2D` (all three), plus the STM and Input System symbols from the Welcome Window.

Compile after each import. Batch-compile and run the full suites after the last one.

**Excluded**, never imported or deleted on import: every `Demo`, `Example`, `Examples` and `Prefabs/Demo*` folder; `Templates` other than the minimal faction database and deed template; Quest Machine's procedural generation samples; the third-party support packages for products we don't own (Adventure Creator, ORK, Emerald, Opsive, PlayMaker and the rest); Dialogue System's TextMesh Pro, Timeline and Cinemachine sequencer samples. The `Skills` folders (AI-agent notes) are harmless and kept out of builds. Each deletion is listed in `THIRD_PARTY.md`.

## 3. Architecture

```
 Dungeon / Tavern / Shared gameplay
        │ publish stable-id facts (EventBus<T>, Shared/Game/GameFacts.cs)
        ▼
 ┌────────────────────── Hearthdelve.Story (new; the only assembly that references Pixel Crushers) ──────────────────────┐
 │  FactRouter ──► DeedAdapter ──► Love/Hate (FactionManager + one FactionMember per tracked character)                  │
 │      │               └─ RelationshipRules (pure, in Shared): which deed, who learns of it, the Respect change              │
 │      ├──► QuestAdapter ──► Quest Machine (QuestJournal on the persistent Story host; MessageSystem messages)               │
 │      └──► (later) DialogueVariables ──► Dialogue System Lua                                                                │
 │  StoryLuaFunctions: HH_* functions registered with Dialogue System (read Hearthdelve state and relationships)          │
 │  StorySaveParticipant: Pixel Crushers state ⇄ one string in SaveData (v8)                                              │
 │  CharacterDirectory: character ids ⇄ Dialogue System actors, Love/Hate factions, portraits                             │
 └────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
        ▲ queries through Shared interfaces (IStoryQueries) — gameplay never sees Pixel Crushers
 Hearthdelve.Story.Presentation (new): the dialogue UI (uGUI + STM, type scale), portraits, choices, barks, the journal
```

**Rules** (to add to CLAUDE.md):
- Only `Hearthdelve.Story` and `Hearthdelve.Story.Presentation` reference `PixelCrushers*`, `DialogueSystem`, `QuestMachine` and `LoveHate`.
- Dungeon, Tavern, Shared and Core never do. Their only link to story is through facts they publish and the `Shared` interfaces they may query.
- Dialogue and quest content calls our `HH_*` Lua functions, never Love/Hate's GameObject-name functions directly. The content then doesn't depend on Love/Hate's object names, and Love/Hate could be replaced behind the adapter.

**Assemblies:**
- **`Hearthdelve.Story`** (new). References Core, Shared, Unity.Localization, `PixelCrushers`, `DialogueSystem`, `QuestMachine` and `LoveHate`.
- **`Hearthdelve.Story.Presentation`** (new). References Story, UI (the type scale and canvas scaler), STM, Input System and `DialogueSystem`.
- **`Hearthdelve.Shared`** (modified). Gains pure character and relationship abstractions; no Pixel Crushers.
- **`Hearthdelve.Story.Editor`** (new). Builders: the story host prefab, database scaffolding, the portrait importer, validation.
- **`Hearthdelve.Tests`.** EditMode references Shared and Story; PlayMode references everything.

**Important classes:**

| Where | Class / interface | Job |
|---|---|---|
| Shared/Characters | `CharacterDefinition` (SO), `CharacterKind` {Player, Staff, Villager, Visitor, Resident, Story}, `ICharacterDirectory` | one character abstraction for everyone (§8) |
| Shared/Story | `IStoryQueries` | what gameplay may ask: `HasQuestObject(id)`, `QuestState(id)`, `Relationship(characterId)`; read-only |
| Shared/Story | `DeedDefinition` (SO), `RelationshipRules` (pure) | facts → deeds, witnesses, the Respect change (§7) |
| Shared/Save | `SaveData.story` (v8), `IExternalStateParticipant` | middleware state as opaque strings (§5) |
| Shared/Quests | `QuestObjectDefinition` (SO), `QuestObjects` in `GameState` | quest objects as a reward kind, owned by Hearthdelve (§10) |
| Story | `StoryHost` (DontDestroyOnLoad, in Boot) | the Dialogue Manager, Quest Machine config, Love/Hate `FactionManager`, the player's `QuestJournal`, the Pixel Crushers SaveSystem (recorder only) |
| Story | `FactRouter`, `DeedAdapter`, `QuestAdapter`, `StoryLuaFunctions`, `StorySaveParticipant`, `CharacterDirectory`, `ConversationStarter` | the adapters |
| Story.Presentation | `HearthDialogueUI` (a `StandardDialogueUI` built from STM subtitle/menu panels), `PortraitView`, `ResponseList`, `BarkView`, `JournalScreen` | presentation |

## 4. Dialogue architecture

- **One Dialogue System database** asset, `Data/Story/HearthDialogue.asset`.
  - **Actors** are created from `CharacterDefinition`s by an editor builder, keyed by character id (`pip`, `gunta`→Boog, `tamsin`, `player`), so names and portraits come from our data.
  - **Conversations** are authored in the Dialogue System editor. **Decision D1** (§13) is whether we author there, or write conversation files that a builder turns into the database.
- **Localization.**
  - English is written in the database. The *DS To Loc* window exports it into a new **`Dialogue`** String Table Collection, with stable GUIDs.
  - At runtime the **Localization Package Bridge** reads the active locale.
  - `TextStyleTests` and `TypographyTests` extend to the Dialogue table: glyph coverage, American spelling, lower-case style, and fit in the dialogue box.
- **Presentation** (Step 2). A Hearth & Hollows dialogue box at 320×180:
  - a 2× portrait (64 px) on the speaker's side;
  - the speaker's name (Body, accent ink) and the line (Body, three lines of 12 px, typewriter on STM);
  - responses as Prompt rows, navigable with keys, stick or mouse;
  - "continue" on E / A / click;
  - barks as small bubbles over characters.

  Input goes through the UI map. Opening a conversation pauses the relevant gameplay maps (`MenuPause`), as menus do now.
- **What dialogue can read**, through `StoryLuaFunctions` (registered with `Lua.RegisterFunction`; names to fix in Step 1):
  - game state: `HH_Day()`, `HH_Gold()`, `HH_Renown()`, `HH_TimesDefeated(boss)`, `HH_Owns(furniture)`, `HH_Displayed(furniture)`, `HH_HasQuestObject(id)`;
  - relationships: `HH_Affinity(char)`, `HH_Respect(char)`, `HH_Remembers(char, deed)`;
  - the player: `HH_PlayerName()`.

  Each reads Hearthdelve state or the relationship adapter at the moment of asking. Nothing is copied into Lua variables, so nothing goes stale.
- **What dialogue can do:** `HH_Deed(deed, target)` (a chosen outcome counts as a deed), `HH_GiveQuest(id)` and `HH_SetQuestNode(...)` through the quest adapter, `HH_GrantQuestObject(id)`. Every effect on game state goes through Hearthdelve code that writes `GameState`.
- **Conditions:** branch on `HH_*` in entry conditions. Conversation-local variables (Lua) are fine for dialogue-only flags such as "Pip told the cellar joke".

## 5. Save architecture (version 8)

- **Format.** `SaveData` gains a `story` block (JsonUtility-friendly):
  - `middleware`: one string, the Pixel Crushers `SavedGameData` serialized by its `JsonDataSerializer`. It covers the Dialogue System's Lua state (`DialogueSystemSaver`), Quest Machine's journal (its saver, **JSON mode**, which survives later quest-node edits) and Love/Hate's faction manager and members (its savers).
  - `player`: name, body id, palette choices.
  - `questObjects`: Hearthdelve-owned (§10).
  - `persistentCharacters`: empty in 4g; Visitors promoted in Phase 5 go here.
- **How it flows.**
  - **Saving:** `GameFlow.Save` → `SaveSystem.Capture(state)` → each registered `IExternalStateParticipant.Capture()` → `StorySaveParticipant` calls the Pixel Crushers `SaveSystem.RecordSavedGameData()` and serializes it.
  - **Loading:** after `Restore`, once Boot's `StoryHost` exists, `ApplySavedGameData(deserialized)`.
  - **Pixel Crushers' own storage is never used:** no slots, no PlayerPrefs, no disk storer, no autosave. Their Save System is only a recorder of savers.
  - Every Pixel Crushers saver lives on the persistent `StoryHost`, so scene changes need no re-apply.
- **Migration v7 → v8.** Add an empty `story` block. A missing block means a fresh story: Act I not started. An old save Continues into the world without its opening; **decision D7** is whether to offer the opening then.
- **Rule kept.** `SaveSystem` stays the only authoritative save. Quest Machine doesn't re-run actions on load, so **rewards are always granted by Hearthdelve code into `GameState`**, never by a quest action.

## 6. Quest architecture

- Quests are Quest Machine assets in `Data/Story/Quests`, listed in one Quest Database on the `StoryHost`'s Quest Machine configuration. The player's `QuestJournal` sits on the `StoryHost`, not on the player prefab, so it survives scene changes.
- **Gameplay → quest progress.** `QuestAdapter` turns facts into Quest Machine messages (`MessageSystem.SendMessage("Fact", "<factName>:<id>")`, e.g. `QuestObjectFound:tamsin_key`), and quest node conditions listen for them. Quest counters come only from messages, never from Quest Machine reading our objects.
- **Quest → gameplay.** Node actions publish Hearthdelve requests through `QuestAdapter` (a UnityEvent → `HH` action) that Hearthdelve code carries out: grant a quest object, grant gold or furniture, unlock a recipe.
- **Text.** Quest Machine's Text Tables aren't used. Quest titles and objectives hold **our Localization keys**, resolved by our `JournalScreen` and objective HUD (uGUI + STM). Quest Machine's own UIs aren't used.
- **Service orders stay tavern systems** (locked): special requests are never quests.

## 7. Relationship architecture (Love/Hate)

**Minimal data model:**
- **Relationship traits:** `Affinity` (built in) and **`Respect`**. `Trust` only if writing proves it's different.
- **Personality traits:** three, which describe what a character values and also tag deeds:
  - **Craft:** good cooking and good work.
  - **Nerve:** daring in the Hollows.
  - **Warmth:** looking after people.
  - Boog: Craft +80, Nerve +40, Warmth −10. Pip: Warmth +70, Craft +30, Nerve −20 (Pip worries).
- **Factions:**
  - one per tracked character (`pip`, `gunta`, later villagers), with the player as `player`;
  - two "place" factions, **`tavern`** (Tally Ho!) and **`village`** (Kariaston), as deed targets. Love/Hate's affinity change scales with how much the judge likes the target, so "something good for Tally Ho!" pleases those who love Tally Ho!.
- **Not used:** PAD-driven behaviour (it runs but drives nothing), emotion models, gossip and greeting triggers, auras, impressionability (0), vision (`CanSee` always true: our rules decide who learns of a deed).

**Love/Hate is scene-based, so we use a scene-independent host.** It evaluates deeds through `FactionMember` components and scene witnesses, but our deeds happen across scenes: the trophy is hung in Decorate, and the troll falls in the Dungeon, where Boog isn't. So each tracked character gets a **"social" FactionMember on the persistent `StoryHost`**, separate from their visible NPC GameObject. `DeedAdapter` commits a deed to exactly the members our rules say learn of it: present at service, told the next morning, everyone for a boss.

**Respect isn't moved by Love/Hate's default evaluation**, which changes only Affinity, emotional state and memory. Each `DeedDefinition` therefore carries a `respect` value. After Love/Hate's evaluation, `DeedAdapter` applies `respect × (1 + alignment(character traits, deed traits))`, using Love/Hate's own `Traits.Alignment`, through its relationship-trait API. If that proves awkward, the fallback is our own `EvaluateRumor` delegate. This is the honest test of whether Love/Hate earns its place. It gives us affinity evaluation shaped by personality, repeat-deed acclimatization, memory, serialization and Dialogue System access. We add one trait.

**Memories expire by default**: short- and long-term durations in game-time seconds, plus a cap. We'll set Love/Hate's time mode to **custom** and advance it by our day clock, so durations are measured in days. Milestone deeds (trophy, boss) get durations longer than the slice. `HH_Remembers(char, deed)` uses Love/Hate's `KnowsDeed`.

**Deed vocabulary** (recommended; facts on the left, all in `Data/Story/Deeds`):

| Deed | From | Target | Who learns | Craft / Nerve / Warmth | Notes |
|---|---|---|---|---|---|
| `displayed_trophy` | `TrophyDisplayed` | tavern | staff, now | 20 / 80 / 0 | Checkpoint A's proof; Boog respects it |
| `defeated_boss` | `BossDefeated` (came home) | village | everyone, next morning | 0 / 90 / 20 | first clear stronger (`BossFirstCleared`) |
| `served_exceptional_dish` | `DishServed` quality ≥ 0.9 | tavern | cooking staff present | 90 / 0 / 10 | capped per evening (acclimatization) |
| `cut_cleanly` | `PartButchered` by the keeper, score ≥ 0.85 | tavern | Boog | 80 / 0 / 0 | |
| `met_request` / `missed_request` | `CustomerRequestCompleted` / `CustomerRequestFailed` | the patron (later) / tavern | Pip | 10 / 0 / 70 | per-patron targets once patrons are named (4h) |
| `neglected_service` | `ServiceCompleted` with walkouts ≥ 3 | tavern | staff | −30 / 0 / −60 | negative impact |
| `kept_a_promise` | a quest completed | the giver | the giver | 0 / 20 / 60 | from `QuestAdapter` |
| `chose:<outcome>` | a dialogue choice | the speaker | the speaker | authored | from `HH_Deed` |

`served_favorite_food` and `served_disliked_food` wait for named patrons (4h). They need `DishServed` to carry the taste match, a one-field addition to the fact.

**Fact → deed without gameplay knowing.** `RelationshipRules` (pure, Shared, EditMode-tested) maps a fact to zero or more `(deed, actor, target, learners)`. `DeedAdapter` (Story) is the only thing that calls Love/Hate.

## 8. Character architecture

- One **`CharacterDefinition`** (ScriptableObject) for every person who can speak or be remembered:
  - `id` (stable; `pip`, `gunta`, `tamsin`, `player`);
  - `kind`;
  - `displayName` (LocalizedString);
  - `portrait` (`PortraitDefinition`);
  - `look` (an animation set or appearance pool);
  - `relationship` (faction name, personality trait values, or a preset);
  - `persistent` (bool).

  `StaffDefinition` gains a `character` reference. `CustomerProfile` gains an optional `character` for named patrons (4h).
- **The three populations:**
  - **Named villagers and story characters** are authored, persistent, have Love/Hate factions, and are Dialogue System actors.
  - **Transient Visitors** are runtime `CharacterRef`s, `visitor/<day>/<visitId>`, built from an archetype profile. They're never saved and have no faction of their own. Lines come from generic archetype conversations and barks with `[var=VisitorName]`-style fields.
  - **Persistent Visitors and residents** (Phase 5): promoting a Visitor writes a `PersistentCharacterRecord` (seed, appearance, generated name) into `story.persistentCharacters` and creates a Love/Hate faction at runtime. Love/Hate's faction database supports runtime factions, and its manager serialization saves them.

  `ICharacterDirectory` resolves any id the same way, so dialogue, quests and relationships take one kind of reference.
- **The protagonist** has `CharacterDefinition` id `player`, filled at runtime from the save: name, body, palette. "Bram Holloway" is only the default name.

## 9. Character creation scope (Step 4)

**What the art supports.** These four bodies are clothed and fully animated in the player's exact layout (idle, walk, attack, charged attack, damage, jump and a death):
- **Human Townsfolk** (the current player; 15 colours);
- **Human Amazon** (11);
- **Dwarf Yellow Beard** (13);
- **Wild Orc** (12).

The other base humanoids are unclothed and are out.

**Options:**
1. **Name.** Up to 16 characters, with an on-screen letter grid for gamepad and typing on keyboard. Default "Bram".
2. **Body:** one of the four.
3. **Palette:** skin, hair (or beard), outfit and trim. Each choice is a ramp taken from Minifantasy's own palettes; skin ramps come from the six base races' skin tones, including orc and goblin greens. A `Tools/characters/protagonist_channels.py` script assigns each body's colours to channels once; at runtime the 4f recolour path remaps them into cached textures, staying on `Sprite-Lit-Default`.

It's saved in `story.player`, reflected by the player's look in both scenes, and read by dialogue through `HH_PlayerName()`.

**Writing rule:** dialogue addresses the player by name or as "you", never by gendered pronouns (no pronoun choice needed).

**Not in scope:** a protagonist portrait (decision D4: none in 4g; the player speaks through response choices), separate hair or clothing layers, body shapes beyond the four.

## 10. Quest objects

- A new reward kind: a `QuestObjectDefinition` (id, localized name, icon, `lostOnDeath`). It's offered by a quest-aware room reward or a node action.
- It's **never a satchel item** (locked), shown in the delve HUD like curios, and kept in `GameState.QuestObjects` (saved).
- **Extraction:** kept.
- **Death:** **lost, and the quest offers it again** on a later delve, so a story item is never permanently lost and the delve's risk still means something (decision D6).
- Publishes `QuestObjectFound`, `QuestObjectBroughtHome` and `QuestObjectLost` facts for `QuestAdapter`.

## 11. Portrait pipeline

1. **Compose.** The owner composes each portrait in the **Minifantasy Portrait Generator app** (`C:\Dev\Minifantasy\Minifantasy_Portrait_Generator_app_v1.0`): race, components, colours.
2. **Export and record.** Export **"blinking + talking" sprite sheets** at scale 1 (32×32 frames), and copy the app's **share code** into a recipe file.
   - The sheets go in `Tools/portraits/derived/<characterId>/`, the recipe in `Tools/portraits/<characterId>.txt`.
   - The recipe means the portrait can be edited later from the same starting point.
3. **Import.** A `derived:` import (as the troll's tusks) brings them into `Assets/ThirdParty/Minifantasy/Portraits/`, and a `PortraitDefinition` holds idle, blink and talk frames.
4. **Display.** The dialogue UI shows the portrait at 2× (64 px), blinking on a timer and talking while the typewriter runs.
5. **Record.** `docs/ASSET_MAP.md` records each portrait's recipe and credits Krishna Palacio.

Raw packs stay outside the repo. Only exported frames are committed.

**Fallback**, if the app's sheet export doesn't match these needs: assemble from `single_images` layers with a Tools script reading the recipe.

## 12. Story content

**Act I opening** (Step 5; a sketch for you to rewrite):
1. **New game.** Character creation, then a title card.
2. **Dusk, Kariaston.** A letter from **Old Tamsin**: the deed to Tally Ho!, "if I'm not back by spring". The tavern is dusty. Pip is already at the books ("She said you'd come. She also said you'd be taller.").
3. **The kitchen.** Boog, with his lit fuse: "No meat, no menu." The cellar door leads to the Hollows.
4. **The first delve** (the existing new-game delve), framed as Boog's errand. Contextual prompts teach movement, the cleaver, harvesting, the rope, and safe ground.
5. **Home.** Boog's first lesson: cook your haul as the delve meal (the grill tutorial). Pip walks you through Prep, the first service (pass, carrying, Results) and Pip's ledger.
6. **Night.** Pip shows the upgrades. Boog notices a carving on a recovered part that matches one over Tamsin's door. That's the Act I hook.
7. **Day 2.** The market (Pip). The first real quest starts (below).

Onboarding is diegetic. Each teaching beat is a short conversation or bark, gated by facts and shown once (Dialogue System variables).

**Representative quest** (Step 6): **"Tamsin's strongbox"**.
- **Given by:** Pip, the morning of day 2.
- **The errand:** Tamsin's strongbox is locked, and her key went down with her. Pip has heard a strange glint was seen on the Cellars' second floor.
- **Finding it:** a quest-aware room on floor 2 offers **Tamsin's key**, a quest object.
- **Bringing it home:** `QuestObjectBroughtHome` advances the quest. Open the strongbox with Pip, which leads to a **choice**:
  - read Tamsin's notes yourself (Nerve, Boog approves);
  - or give Pip the ledger inside (Warmth, Pip approves).

  The choice is a `chose:` deed, and the outcome persists: quest state, the deed and a memory.
- **If you die:** the key is lost and the room offers it again.
- **Rewards:** a recipe hint and a curio, granted by Hearthdelve code.

This replaces the roadmap's "a villager's errand": there are no villagers until 4h. Decision D5.

## 13. Unresolved decisions (yours)

| # | Decision | Recommendation | Cost to reverse |
|---|---|---|---|
| D1 | Author conversations in the Dialogue System editor, or in text files a builder imports | **The Dialogue System editor** (made for branching and conditions), with the *DS To Loc* export as the localization source. Text-file import (Dialogue System supports Ink, Twine, Articy and CSV) if you'd rather write outside Unity | **high**: it shapes every conversation |
| D2 | Respect: our per-deed extension, or our own `EvaluateRumor` | **The per-deed extension** (smallest, transparent) | low |
| D3 | Love/Hate time: custom mode advanced by days | **Yes** (memories age in days) | medium |
| D4 | A protagonist portrait | **None in 4g** | low |
| D5 | The representative quest: "Tamsin's strongbox" (Pip) instead of a villager errand | **Yes** (villagers are 4h) | low |
| D6 | Quest objects on death: lost but offered again | **Yes** | medium |
| D7 | Old saves (v7) on Continue: start the story at Act I's next beat, or offer the opening | **Skip the opening; Pip gives the strongbox quest the next morning** | low |
| D8 | Protagonist bodies: the four dressed humanoids | **Yes**; import the three new sheet sets | medium |
| D9 | Unity Localization through the bridge (Dialogue table) | **Yes** (our rule: every string in tables) | high |
| D10 | Tag 4f as `milestone-4f` and tag later milestones the same way | **Yes** (§17) | low |

## 14. Steps and checkpoints

**Checkpoint A: dialogue and social technology** (Steps 1–3). *Does Love/Hate earn its place?*

1. **Pixel Crushers integration.**
   - Import (§2); create `Hearthdelve.Story` and the `StoryHost` in Boot.
   - Build `StorySaveParticipant` and save v8 (`SaveData.story`, the v7 → v8 migration).
   - Build `FactRouter`, a skeleton `DeedAdapter` and `QuestAdapter`, and the `StoryLuaFunctions` registry.
   - Content: one test conversation, one test quest, one test deed.
   - A web build checking Lua, saves, stripping (`link.xml` for Pixel Crushers saver and data types if IL2CPP strips them) and size.
2. **Dialogue presentation.**
   - `HearthDialogueUI`: STM panels, the type scale, 2× portraits, a typewriter, a response list, barks.
   - Keyboard, mouse and gamepad; the Localization bridge and the `Dialogue` table.
   - 320×180 fit tests; captures.
3. **Character, portrait and relationship adapters.**
   - `CharacterDefinition` for player, Pip, Boog and Tamsin; `ICharacterDirectory`.
   - Two Portrait Generator portraits (Pip, Boog) through the pipeline.
   - The faction database (Affinity, Respect, Craft, Nerve, Warmth; `pip`, `gunta`, `player`, `tavern`, `village`).
   - `RelationshipRules` with the deed vocabulary; the Respect extension; the custom time mode.

**Checkpoint A proof** (temporary writing allowed):
1. Hang the Larder Troll's tusks (an existing gameplay action).
2. `TrophyDisplayed` is published, and `RelationshipRules` maps it to `displayed_trophy` (player → tavern), learned by Boog and Pip.
3. Love/Hate evaluates it: Boog's Affinity and Respect rise (Respect more, through Nerve and Craft alignment); Pip's Affinity rises a little. Both remember the deed.
4. Talking to Boog branches on `HH_Respect("gunta") >= N` and `HH_Remembers("gunta","displayed_trophy")` to a line about the tusks.
5. Save, quit to the menu, Continue: same values, same memory, same branch.
6. A **PlayMode test** runs the whole chain, and the web build is checked by hand.

If Love/Hate can't do this cleanly, you choose before Checkpoint B: keep it with our extension, or replace it behind the same `DeedAdapter` with a small Hearthdelve model. The content wouldn't change, because it only calls `HH_*`.

**Checkpoint B: a new game has a story** (Steps 4–6).

4. **Character creation** (§9): the screen, the four bodies, palette channels, name entry, saved and shown.
5. **The Act I opening and onboarding** (§12): the conversations, diegetic teaching, first-time beats gated by facts.
6. **The quest framework and the strongbox quest:** quest objects (§10) and the room reward, the journal and objective HUD, the outcome choice, persistence. Tests cover extraction, death and Continue.

**Checkpoint C: characters remember, and closeout** (Steps 7–8).

7. **The relationship and reactivity slice:** the authored deed set (§7), Boog's and Pip's preferences and memories, a handful of reactive lines and barks (the tusks, a clean cut, a bad night, the troll), and how relationship state shows (a Prep or Night line; no meters).
8. **Act I vertical-slice content and closeout:** remaining Act I writing, polish, localization checks, the full regression, the web build, docs (CLAUDE.md rules, the GDD's story sections, `THIRD_PARTY.md`, `CREDITS.md`, `ASSET_MAP.md`), and the sign-off report.

## 15. Tests

**EditMode:**
- `RelationshipRules`: every fact → deed mapping, learners, Respect from alignment, acclimatization caps.
- Deed definitions are complete and valid.
- Save v8 round trip and the v7 → v8 migration; `IExternalStateParticipant` ordering.
- `ICharacterDirectory`: all three populations; transient Visitors never saved.
- Quest object rules: extraction keeps; death loses and offers again; never in the satchel.
- Character creation: bodies have the full animation set, palette channels are complete, names are valid.
- Dialogue table: glyph coverage, American spelling, lower-case style, box fit.
- Love/Hate behind the adapter, in its own suite.

**PlayMode:**
- A Dialogue System conversation runs in our UI with keyboard and gamepad navigation, and `HH_*` conditions branch correctly.
- Quest start → object → return → outcome, through Continue.
- **The Checkpoint A proof**, end to end.
- Character creation is reflected in the delve and the tavern.
- Onboarding beats fire once.
- `TextOverlapTests` extended to the dialogue box, journal and creation screens.

**Web:** at each checkpoint, a fresh build and a browser smoke test of each new path, with the console watched. The owner's browser save is backed up and restored, as now.

## 16. Manual Unity and editor actions you'll need

1. **Confirm Love/Hate is purchased.** In **Package Manager → My Assets**, **download** (don't import) Dialogue System 2.2.74, Quest Machine 1.2.74 and Love/Hate 1.10.74.1. I'll import them from the cache in batch mode in the order above, unticking or deleting demos. Or import them yourself in the editor with Demo and Example unticked.
2. **The Welcome Windows:** tick `USE_PHYSICS2D`, Input System and Super Text Mesh support (I'll set the define symbols in batch and confirm with you).
3. **Portraits:** compose Pip, Boog and Tamsin in the Portrait Generator app; export the sheets and share codes (I'll give exact steps then).
4. **Playtests** at each checkpoint: dialogue feel, the creation screen, the opening.

## 17. Milestone tag

There's no convention yet: the only tag is `v0-sidescroller-prototype`. Proposed: **`milestone-4f`**, annotated "4f complete: Tavern Stage 1 content and the customization foundation (signed off 2026-10-06)", on `f7812b3` (the sign-off docs, after code commit `64ddf4d`). Later milestones would be `milestone-4g`, `milestone-4h`, and so on. Optionally, earlier milestones could be backfilled as `milestone-4a`…`4e` on their sign-off commits. **Not created until you approve the name.**

## 18. The first implementation sequence after approval

1. Create the `milestone-4f` tag and push it (if approved).
2. You download the three packages. I import Dialogue System and its asmdefs, compile and run the full suites; then the same for Quest Machine, then Love/Hate; then the integration packages and define symbols. Each step is its own commit, with `THIRD_PARTY.md` updated.
3. `Hearthdelve.Story` and `Story.Presentation` asmdefs, and a `StoryHost` in Boot (an in-place updater).
4. Save v8 with `StorySaveParticipant`; round-trip and migration tests.
5. `StoryLuaFunctions` with three functions and the test conversation in a placeholder UI; then the test quest; then the test deed.
6. A web build checkpoint for Step 1.
7. Step 2 (presentation), then Step 3 (characters, portraits, relationships), then the Checkpoint A proof and playtest.
