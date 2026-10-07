# 4g plan: story, quests, character creation and relationship reactivity

> **Status: approved 2026-10-06, with the owner's decisions below. Checkpoint A (Steps 1–3) built and signed off 2026-10-06. Checkpoint B (Steps 4–6) built and signed off 2026-10-06 (the owner playtested it and verified the node-editor workflow). Checkpoint C (Steps 7–8) built 2026-10-06, waiting for the owner's final 4g playtest and sign-off.** 4f is complete (signed off 2026-10-06; tag `milestone-4f`).

## Approval (2026-10-06): the owner's decisions

- **Canon:** Kariaston, Tally Ho!, Boog (the goblin cook, stable id `gunta`), Orik (the dwarf server, stable id `pip`; he replaced Pip Marrowby after Checkpoint A was built), the Hollows. Gunta, the Sunken Flagon and Brackenford are not reintroduced.
- **Tag:** `milestone-4f` (annotated, on `f7812b3`); later milestones `milestone-4g`, `milestone-4h`…
- **D1:** the Dialogue System's editor and database are authoritative for conversations; no text-file importer in 4g (read-only exports, review dumps and localization reports may come later).
- **Packages:** all three were purchased and **already imported by the owner**; nothing is downloaded or reimported, only missing official support components are added (§2's import order is superseded; see "As built").
- **Love/Hate:** approved, with Affinity and Respect only (no Trust yet); social stand-ins approved; Respect by the smallest clean extension of Love/Hate's evaluation; memories on a deterministic game-day clock.
- **Quest Machine localization:** Unity Localization stays authoritative; quest text holds our keys.
- **D5 replaced:** the first quest is **Boog's Bomb** (his favorite bomb, lost in the Hollows, wanted back for "research"), not Tamsin's strongbox. Step 6.
- **D6:** quest objects are lost on death before extraction and obtainable again while the quest is active; never in the Satchel or the Lockbox; kept on extraction; a modest per-object policy.
- **D4:** no keeper portrait in 4g. **D7:** old saves skip the opening through explicit story state; new games play it.

## As built: Checkpoint A (2026-10-06)

Where the build differs from §§2–14 below (each for a reason found in the packages' own code):

- **No Pixel Crushers save recorder.** Each adapter records its middleware directly (the Dialogue System's `PersistentDataManager`, Quest Machine's journal `RecordData`); Pixel Crushers' `SaveSystem` component exists in Boot only because Quest Machine's journal serializes through it. Nothing writes a slot.
- **Relationships are saved in our own shape** (`RelationshipData`: changed values and memories by stable id and trait name), not Love/Hate's `SerializeToString`, which is positional (adding Trust later would misread old saves) and writes floats in the player's locale.
- **No support packages installed.** The `HH_` Lua functions stand in for the Dialogue System ↔ Love/Hate and ↔ Quest Machine bridges; our dialogue UI implements `IDialogueUI` directly (the STM support package lands outside `Plugins`, where our assemblies can't reference it); our builder writes the Dialogue table with the bridge's key convention (each entry's `Guid`, `<guid>_MenuText`).
- **Respect alignment** uses Love/Hate's own formula over only the traits a deed shows, and gives respect only above indifference (`RelationshipRules`): over all traits, a judge's unrelated values counted against a deed and a neutral judge respected it as much as Boog.
- **The memory clock** is Pixel Crushers' `GameTime` in manual mode, set to the game day; each deed's memory lasts its `memoryDays` (0: for good). The Dialogue System keeps its own real-time clock.
- **The proof quest** is `proof_trophy_wall` ("something with teeth": Boog wants a trophy over the bar), completed by the same `TrophyDisplayed` fact as the deed; temporary, replaced by Boog's Bomb in Step 6.
- **Talking:** staff carry a `Person` interactable (E during service, while they stand still with their hands free); F1 (debug builds) talks to Boog in any tavern phase, Shift+F1 brings the first boss trophy home again.

## As built: Checkpoint C (2026-10-06)

**Deeds** (each from an existing fact, only when remarkable; `RelationshipRules.Qualifies`):

| Deed | From | Learned by | Shows (craft, nerve, warmth) | Memory |
|---|---|---|---|---|
| `displayed_trophy` | a boss trophy hung (Checkpoint A) | staff | 0, 80, 0 | for good |
| `returned_boogs_bomb` | Boog's conversation (Checkpoint B) | Boog | 0, 70, 30 | for good |
| `felled_larder_troll` | `BossFirstCleared` (the troll; fires once ever) | staff | 50, 70, 0 | for good |
| `kept_a_wish` | a special request met at quality ≥ 0.9 | staff | 40, 0, 60 | 7 days |
| `fine_butchery` | the keeper's own cut at score ≥ 0.9 (never Boog's) | Boog | 60, 60, 0 | 3 days |

**Two readings.** Boog values nerve 90, craft 60, warmth 10; Orik warmth 80, craft 40, nerve −30. So the troll earns Boog's respect loudly (85% of the deed's) and Orik's quietly (45%); a kept wish matters more to Orik; a grotesque trophy alone earns Orik's liking but not his respect; the block is Boog's business.

**Repeats** fade 1, ½, ¼, ⅒, then nothing: Love/Hate's acclimatization curve on the stand-ins, used for Respect too (read from the count before this sighting). A forgotten memory makes the deed fresh again.

**Conversations.** Each character opens a hub (`Boog/Hub`, `Orik/Hub`, seeded once): critical story and quest (her return, arrival day) → one-time callbacks (Boog: the troll, combined with his bomb if he remembers it; his bomb, the next time; a wish kept, heard over the stove; a clean cut, and if he respects the keeper enough, "you could work my stove. don't. but you could." Orik: the troll, moved from "risks" to "resolved", and Phi, who went after it twice and wouldn't say why; a wish kept, and if he respects the keeper enough, "Phi ran it that way") → the everyday Talk conversations. A callback marks a Dialogue System variable and is never repeated; the variables are saved with the dialogue.

**Author edits** (once, only where the entry was exactly as written): the tusks remarks and Orik's incident-book line are said once (they repeated forever above the everyday branches, which hid the bomb offer if the tusks went up first); "about your bomb..." stays after the opening (her shelf line); Orik's homecoming no longer says "1 parts". The owner's arrival line ("ahh, you must be …!") is kept.

**Presentation.** The tavern's day panels (morning, prep, results, night) step aside while someone's talking; Boog's bomb has the target arrow and a stronger, swelling glow. No quest journal: Boog's reminder and the arrow were enough in the Checkpoint B playtest.

**Save:** still version 9 (nothing new in the schema: deeds and callbacks live in the relationship and dialogue blocks).

## As built: Checkpoint B (2026-10-06)

**The builder boundary.** Conversations are seeded once and logged (`Data/Story/DialogueSeeds.txt`); a logged title is never written again, so an edit, a rename or a deletion in the node editor stands. Checkpoint A's two proof conversations predate the log: each was replaced by its Checkpoint B version only because it was exactly as Checkpoint A wrote it (compared line by line, with speakers, conditions, scripts and links); one changed by hand would have been kept. The tooling still gives new entries a `Guid` and copies the English into the Dialogue table. C# decides only when a conversation plays (`OpeningRules`); every branch, condition and script is in the graph.

**Step 4, the keeper.** Four bodies, each complete (idle, walk, attack, charged attack, hurt, the roll, a death): the Human Townsfolk ("townsfolk"), the Human Amazon ("warrior", so the name doesn't promise a class), the Yellow Beard dwarf ("dwarf") and the Wild Orc ("orc"). Three colour channels each (skin, hair or beard, clothes); no "trim" channel: the bodies' remaining colours are belts, boots and outlines, and recolouring them read as noise. Ramps are sampled from Minifantasy's own drawings: five skins (the top four shades of A Myriad of NPCs' skin layers, plus the dwarf's own), two orc greens (the orcs' own and the goblins'; every orc in the packs shares one green), nine hair colours and twelve clothes colours. Each body's drawn colours are themselves a choice, so "as drawn" is exact. Names up to 16 letters (any script Silver draws, spaces, apostrophes, hyphens); empty is Bram. Controls: up and down choose a row, left and right change it, A or Enter on the name opens the letters (or type), Begin starts the game. The look is baked once per body and palette into cached sprites (`KeeperLook`), worn by the keeper in the tavern and the Hollows (`KeeperAppearance`).

**Step 5, the opening** (`OpeningStage`: Arrival → FirstDelve → Homecoming → FirstEvening → Complete, saved by name):
1. **Arrival** (day 1, a new `TavernPhase.Arrival`: the keeper walks the room). `Act1/Arrival`: Orik meets the keeper by name; Phi went into the Hollows nine days ago and her letter leaves Tally Ho! to the keeper; Boog; the empty storeroom; the hatch; Orik won't go down. Then the cellar hatch (the Dungeon pack's ladder hole, used like a station; shown only on arrival day) leads to the first delve.
2. **The first delve** teaches by prompts at the foot of the screen, each once, when it first matters: moving and aiming, attack and roll at the first sealed room, Essence at the first hit, the satchel at the first harvest, the finisher when a foe can first be finished, the rope home when it first opens. Nothing about clean-kill maths, overkill, the Butcher Block or powers.
3. **Homecoming** (that night): `Act1/Homecoming`, Boog delighted at what came up (or glad anyway), Orik counting it and remembering Phi coming up the same hatch.
4. **The first evening** (day 2): `Act1/FirstEvening` at Prep (the board, the stations, the pass, being paid; nothing else), then `Act1/FirstTakings` at Results, which runs into Boog's question: Boog/Bomb.

**Step 6, Boog's Bomb.** `QuestObjectDefinition` `boogs_bomb` (the Goblin Sapper's bomb, its fuse sputtering; a flickering orange glow); Quest Machine quest `boogs_bomb` (find → return → success, each step a fact; no failure node). It lies beside the reward in the second fight cleared on the Cellars' first floor while the quest wants it and it isn't carried. Picked up: "found: Boog's bomb!" in the harvest feed. Extraction brings it home (persistent); a death loses it (the death screen says so; never in the Lockbox) and the quest goes on; it lies there again on a later delve. Boog's conversation (`Boog/Bomb`) offers it (accept, ask, or "not now": declining never closes it; Boog/Talk keeps "about your bomb..." until she's back), reminds, and on her return runs one script: the deed `returned_boogs_bomb` (for Boog alone: nerve and a little warmth; Affinity and Respect rise, since he values nerve; remembered for good), then the handover (60 gold, from Hearth & Hollows code; saved). Orik has a line about the incident book afterwards.

**Characters as written (first pass, for your review before more writing):**
- **Boog**: a goblin cook who treats alarming things as reasonable ("the smell's mine. i'm keeping it"; "people do miss it, and then they fall in. that works too"). Competent and sincere: the bomb's "research" turns out to be that she was the first thing he made that went off when he meant her to, and Tamsin let him keep her. His respect means something: "you went all the way down for her. i won't forget it, keeper."
- **Orik**: dry, exact, a ledger man. A dwarf who won't go below the cellar ("my family went down for three hundred years. somebody had to come up and count what they left"). Wants Tally Ho! in the black and keeps "the incident book" on Boog. Quietly loyal to Tamsin, who hired him "for a week" eleven years ago and never said the week was over. Irritations: lateness, vagueness, Boog's receipts. What he respects: people who come back, and pay what they owe. Funny by understatement ("in the red. a cheerful sort of red. we've had worse reds").
- **The bomb** is "her", with no name yet. Candidates if you want one: **Bertha**, **Old Faithful**, **Sweetpea**, **Mabel**, **Kaboomrah**.

**Typewriter.** One asset (`Data/Story/DialogueSettings.asset`) holds the reveal speed (45 characters a second) and the ▼'s bob. Confirm while revealing shows the whole line and stops; the next confirm moves on (Submit, E, A, a click on the box). Choices that appear after a line can't be taken by the press that moved on: they unlock only once every confirm (Enter, Space, E, A, the mouse button) has been let go.

**Save v9:** `story.openingStage`, `story.creationComplete`, `story.seenHints`, `story.player.palette`, `questObjects`. v8 → v9 marks the opening complete, the keeper made and every prompt seen (Checkpoint A's new games had the opening "not complete"; they still skip it). A saved quest the game no longer has (Checkpoint A's proof) is dropped from the journal on load, with a warning.

## 0. Corrections to the brief, from the repository

- **Names.** The brief's Step 5 says "the Sunken Flagon" and "Gunta". Since 2026-10-06 they are **Tally Ho!** and **Boog** (GDD Decided 35). Orik was unchanged. Old Tamsin was replaced by **Old Phi** (Phi'rai) in the closeout patch (GDD Decided 51); the as-built sections use the current names, while the original proposals below keep the names they were written with.
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
- **Conditions:** branch on `HH_*` in entry conditions. Conversation-local variables (Lua) are fine for dialogue-only flags such as "Orik told the cellar joke".

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
  - Boog: Craft +80, Nerve +40, Warmth −10. Orik: Warmth +70, Craft +30, Nerve −20 (Orik worries).
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
| `met_request` / `missed_request` | `CustomerRequestCompleted` / `CustomerRequestFailed` | the patron (later) / tavern | Orik | 10 / 0 / 70 | per-patron targets once patrons are named (4h) |
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
2. **Dusk, Kariaston.** A letter from **Old Tamsin**: the deed to Tally Ho!, "if I'm not back by spring". The tavern is dusty. Orik is already at the books ("She said you'd come. She also said you'd be taller.").
3. **The kitchen.** Boog, with his lit fuse: "No meat, no menu." The cellar door leads to the Hollows.
4. **The first delve** (the existing new-game delve), framed as Boog's errand. Contextual prompts teach movement, the cleaver, harvesting, the rope, and safe ground.
5. **Home.** Boog's first lesson: cook your haul as the delve meal (the grill tutorial). Orik walks you through Prep, the first service (pass, carrying, Results) and Orik's ledger.
6. **Night.** Orik shows the upgrades. Boog notices a carving on a recovered part that matches one over Tamsin's door. That's the Act I hook.
7. **Day 2.** The market (Orik). The first real quest starts (below).

Onboarding is diegetic. Each teaching beat is a short conversation or bark, gated by facts and shown once (Dialogue System variables).

**Representative quest** (Step 6): **"Tamsin's strongbox"**.
- **Given by:** Orik, the morning of day 2.
- **The errand:** Tamsin's strongbox is locked, and her key went down with her. Orik has heard a strange glint was seen on the Cellars' second floor.
- **Finding it:** a quest-aware room on floor 2 offers **Tamsin's key**, a quest object.
- **Bringing it home:** `QuestObjectBroughtHome` advances the quest. Open the strongbox with Orik, which leads to a **choice**:
  - read Tamsin's notes yourself (Nerve, Boog approves);
  - or give Orik the ledger inside (Warmth, Orik approves).

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
| D5 | The representative quest: "Tamsin's strongbox" (Orik) instead of a villager errand | **Yes** (villagers are 4h) | low |
| D6 | Quest objects on death: lost but offered again | **Yes** | medium |
| D7 | Old saves (v7) on Continue: start the story at Act I's next beat, or offer the opening | **Skip the opening; Orik gives the strongbox quest the next morning** | low |
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
   - `CharacterDefinition` for player, Orik, Boog and Tamsin; `ICharacterDirectory`.
   - Two Portrait Generator portraits (Orik, Boog) through the pipeline.
   - The faction database (Affinity, Respect, Craft, Nerve, Warmth; `pip`, `gunta`, `player`, `tavern`, `village`).
   - `RelationshipRules` with the deed vocabulary; the Respect extension; the custom time mode.

**Checkpoint A proof** (temporary writing allowed):
1. Hang the Larder Troll's tusks (an existing gameplay action).
2. `TrophyDisplayed` is published, and `RelationshipRules` maps it to `displayed_trophy` (player → tavern), learned by Boog and Orik.
3. Love/Hate evaluates it: Boog's Affinity and Respect rise (Respect more, through Nerve and Craft alignment); Orik's Affinity rises a little. Both remember the deed.
4. Talking to Boog branches on `HH_Respect("gunta") >= N` and `HH_Remembers("gunta","displayed_trophy")` to a line about the tusks.
5. Save, quit to the menu, Continue: same values, same memory, same branch.
6. A **PlayMode test** runs the whole chain, and the web build is checked by hand.

If Love/Hate can't do this cleanly, you choose before Checkpoint B: keep it with our extension, or replace it behind the same `DeedAdapter` with a small Hearthdelve model. The content wouldn't change, because it only calls `HH_*`.

**Checkpoint B: a new game has a story** (Steps 4–6).

4. **Character creation** (§9): the screen, the four bodies, palette channels, name entry, saved and shown.
5. **The Act I opening and onboarding** (§12): the conversations, diegetic teaching, first-time beats gated by facts.
6. **The quest framework and the strongbox quest:** quest objects (§10) and the room reward, the journal and objective HUD, the outcome choice, persistence. Tests cover extraction, death and Continue.

**Checkpoint C: characters remember, and closeout** (Steps 7–8).

7. **The relationship and reactivity slice:** the authored deed set (§7), Boog's and Orik's preferences and memories, a handful of reactive lines and barks (the tusks, a clean cut, a bad night, the troll), and how relationship state shows (a Prep or Night line; no meters).
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
3. **Portraits:** compose Orik, Boog and Tamsin in the Portrait Generator app; export the sheets and share codes (I'll give exact steps then).
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
