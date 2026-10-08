# 4i plan: a playtest-ready vertical slice

> **Status: approved 2026-10-08 (the four checkpoints, with decisions D1–D10 in §5). 4i-A is built and waiting for the owner's playtest** (*As built: 4i-A*, at the end); 4i-B, C and D wait for their own approval. 4h is complete (signed off 2026-10-08, tag `milestone-4h`). This plan replaces the old one-line 4i entry ("settings, accessibility per GDD §12, audio system, web build"), keeps its intent and widens it to the owner's objective below.

**Objective (the owner's, 2026-10-08):** *an unfamiliar player can launch Hearth & Hollows, understand what to do, play several complete days, and enjoy the experience without developer assistance.*

**The experience to protect:** the slice already plays well for someone who knows it. 4i is about the first hour for someone who doesn't: they should feel welcomed into a strange, warm place, never lost, never punished by the interface, and they should leave wanting another day. 4i adds no new gameplay system; everything here is the frame around the game (menus, settings, guidance, sound, builds) plus polish of what exists.

Guiding lenses: *the player's first five minutes* (Schell's Lens of the Toy and of Curiosity: they should want to poke things before they're told why), *clarity before depth* (every confusion found in a playtest is a bug), *small and finished over large and half-done*.

---

## 1. Audit (2026-10-08, from the repository)

Status key: **Complete** (good enough for an outside playtest), **Partial** (exists, with gaps named), **Missing**, **Improve** (works, but below the bar for strangers).

| Area | Status | What exists | Gaps |
|---|---|---|---|
| **Main menu, New Game / Continue** | Partial | `MainMenuScreen`: title, Continue with "day N, phase", New Game with an overwrite confirmation, the creator. Playing `MainMenu` alone loads Boot. | No Options, Credits or Quit (desktop); a plain parchment panel on black (no art, no version label); the Continue detail shows the raw `day {0}, {1}` for a moment while localization loads (seen 2026-10-08). |
| **First-run onboarding** | Partial | The Act I opening (Orik and Boog, the hatch); six delve hints (move, fight, Essence, harvest, finisher, extract) as one-time `SeenHints`; station panels state their controls; garden prompts; the "Tab: decorate" reminder; Orik's five-o'clock line; the Field Guide (an external page). | Nothing orients the **first free day** (what can I do, where's the market, what is Vigor, how does the evening start: the menu board is easy to miss); no in-game controls reference; no reminder of what the evening and the delve are for after the opening ends. |
| **Character creation** | Complete | Four bodies, Minifantasy colour ramps, a 16-letter name with gamepad letters and keyboard typing, live preview, back and begin. | Only polish (it shares the menu's plain look). |
| **Saving and loading** | Partial | Versioned JSON (v10), migrations from v1, temp-file writes, the web's IndexedDB sync, autosaves at the safe boundaries, a newer-version guard. | An unreadable or newer save just hides Continue (a warning in the log only), and New Game then replaces it **without** the confirmation (the menu thinks there's no save); no backup of the last good save; nothing tells the player when the game saves, or that quitting mid-delve loses that delve. |
| **Options and settings** | Missing (UI) | `GameSettings` (Core) holds screen-shake scale, hit-stop, flashes, vibration on/off, intensity and the reduced-vibration cap, and the feel code already honours them. | No screen, nothing persisted (every value resets each launch), no volume, display or text options. |
| **Audio and music controls** | Partial | `MusicDirector` (Boot): cues by `MusicRules`, holds, crossfades, a per-track level, the music volume on `MusicConfig`; the Hollows' effects a quarter down through `AudioListener.volume`; a fallback listener (2026-10-08). | No player volume controls; no `AudioMixer` (effects are many MMFeedbacks/TDE sources with no common group, which is why the Hollows' level rides on the listener); **all 61 sound effects are generated placeholders** (`PH_*.wav`). |
| **Controller and keyboard/mouse** | Partial | Every map bound for both; aim by mouse or right stick; Decorate Mode controller-first; TDE through `HearthdelveInputManager`. | **No pause menu** (Esc/Start are bound and reach TDE's `PauseButton`, but nothing opens a menu); no remapping; button hints are words ("press E" / "press A"), not glyphs, and pick the device by `wasUpdatedThisFrame`, so they can show the wrong device. |
| **Haptics** | Complete (core) | Named patterns, intensity mapping as pure logic with tests, the settings scale and cap, a graceful no-op on the web. | Only the missing settings UI; a desktop release build without a gamepad is untested. |
| **Accessibility and readability** | Partial | Silver on a strict type scale (`TypographyTests`, `TextStyleTests`); telegraphs are shapes (the red "!", floor marks and lines), not colour alone; enemies hurt only with telegraphed attacks. | No settings for any of GDD §12: shake/flash sliders exist in code only; no assist options; no text speed or instant text; no colour audit of freshness and quality indicators; text size is constrained (Silver is pixel-exact only at whole multiples of 19 on the 320×180 grid, and every layout is built for 1×). |
| **UI consistency and pixel-perfect presentation** | Improve | `PixelCanvasScaler`, the type scale, styled text everywhere, Minifantasy UI sprites. | **Station panels' controls lines overflow their box** ("W/A/S/D \| Up Arrow/Left Arrow/…" runs under the frame: Field Guide screenshots, 2026-10-08); the delve's controls line overlaps the satchel for a few seconds; the death screen's lone button sits right of centre with an empty satchel; two placeholder icons (bat wing) and two missing (shroom cap, spore sac). |
| **Credits and attribution** | Partial | `docs/CREDITS.md` and `docs/THIRD_PARTY.md` are complete. | **No in-game credits.** HeatleyBros' licence requires an in-game credit with a working link before release; Minifantasy (Krishna Palacio) and Silver (Poppy Works, CC BY 4.0) need credits too. |
| **PC and Web builds** | Partial | `BuildTools.BuildWeb` (menu and batch): a **development** build, compression **off** (153 MB), decompression fallback on. Debug keys (F2–F4, F8, F9) are already off outside development builds. | No release Web build (compressed); **no PC build path at all** (no menu, no batch method, no tested Windows player); no version number in the game; itch uploads are by hand. |
| **Performance, memory, load times** | Unknown | VSync on; the web build has run smoothly in smoke tests; a dev-only run log. | Never measured: startup, scene-set loads, frame time in a busy service or the troll fight, the web's memory peak. **A known memory risk:** Chrome decodes each music track whole, to 32-bit floats (about 65 MB per 2½-minute stereo track); the director unloads stopped tracks, but the day's tune stays decoded while Decorate Mode plays its own (about 130 MB at once). |
| **Known bugs and presentation problems** | — | Recorded in `docs/PROGRESS.md` (*Known issues*). | Relevant to strangers: the overflowing station hints; the menu's `{0}` flash; the delve controls overlap; the death-screen button; the bark bubble's position not separately browser-checked (owner's D playtest passed); a once-seen flaky tavern test. |
| **Placeholder content and feedback** | Improve | Every interaction has a combined `MMF_Player` feedback (visual, sound, haptic). | The sound half of it is placeholder everywhere; no keeper portrait (by design in 4g); the main menu has no art. |
| **External playtest readiness** | Missing | The Field Guide (a private artifact), the owner's own playtests. | No release build, version label, tester instructions, feedback route, known-issues note or "how to reset your save". |

---

## 2. Checkpoints

Four checkpoints, each built, then stopped for the owner's playtest. Within a checkpoint, steps continue unless one of the usual stop reasons comes up.

### 4i-A: First impressions

**Purpose.** From launch to the end of the second day, a newcomer knows what to do next and never fights the interface.

**Subjective acceptance.** Watching someone new (or playing as if new): the menu looks like a game, not a debug panel; nobody asks "how do I pause / quit / start the evening / what's this bar?"; the first free day feels like an invitation, not a void; nothing about saving surprises them.

**Systems and assets.** `MainMenuScreen`, `BootBuilder` (menu builder) and a new in-place menu updater (the scene is never overwritten), `GameFlow` (quit-to-menu), `SaveStore`/`SaveSystem` (backup, unreadable-save path), `OpeningRules` and the hint/beat mechanism (`SeenHints`), `InputHints`, the Dialogue database (new first-day lines, seeded once), Localization tables (**Menu**, **Hints**).

**Tasks.**
1. **Main menu look:** a backdrop (decision D1), the title at Display size, a version label (from the build, §4i-D), and buttons Continue / New Game / Options / Credits / Quit (Quit on desktop only). Fix the `{0}` flash (show nothing until the table is ready).
2. **Pause menu** on Esc / Start in the tavern, Kariaston, the Hollows and Decorate Mode (Decorate's own Esc stays "cancel", so Start/the menu key opens it there; decided in the build, playtested): Resume, Options, Controls, Quit to menu (and Quit game on desktop). It holds `MenuPause` and the surface clock. Quitting mid-delve asks first: "this delve will be lost" (decision D2).
3. **Controls reference** (inside the pause menu): one page per context (the day, the evening's stations, the Hollows, Decorate Mode), for the device in use.
4. **Device-aware hints:** track the last device the player actually used (`InputSystem.onActionChange`) instead of `wasUpdatedThisFrame`, so prompts switch cleanly between "E" and "A"; one shared source for every hint.
5. **First free day orientation** (decision D3, recommended diegetic): on the first free morning, Orik (or Boog) gives two or three practical lines (the market is open till five, the garden out back, the menu board when you're ready); one-time prompts the first time the keeper reaches the garden (what Vigor is and that sleep refills it), the menu board (the evening starts here, whenever you like) and the market. Each is a `SeenHints` beat, so it never repeats, and skippable by simply doing the thing.
6. **Saves the player can trust:** keep a copy of the last good save (`save_slot_1.bak.json`) before each write; on an unreadable save, the menu says so plainly and offers the backup or a new game (with the confirmation); a newer-version save says it's from a newer version; a small "saved" mark (an icon, a second) when the game autosaves.
7. Station panels' controls lines fit their boxes (shortened text, two lines where needed; `TypographyTests` extended to cover them).

**Tests.** EditMode: save backup and recovery (`SaveStore` with a corrupt file, a newer file, a missing backup), the unreadable-save menu state, hint device selection as pure logic, the first-day beats' conditions. PlayMode: the pause menu opens and closes in each context and holds the clock; quit to menu and Continue round trip; quit mid-delve asks; a corrupt save shows the message and New Game still confirms; the first free day's lines play once. `TypographyTests` cover every new string and the station hint lines.

**Web/PC.** Web smoke (backing up and restoring the owner's save): menu → Continue; pause → options → back; quit to menu → Continue. PC: deferred to 4i-D (no PC build yet).

**Your tasks.** Play a **new game** as if you'd never seen it, through the second day; note every moment you'd have asked a question. Check the pause menu on a controller.

**Risks.** The first-day lines can tip into tutorial chatter (keep them few, and in character); Esc's meaning in Decorate Mode; the menu backdrop's cost if it's a live scene (D1).

**Not in 4i-A.** Options' contents (4i-B), credits' contents (4i-C), a quest log or journal, a full tutorial, new mechanics.

**Gate.** Stop for the owner's playtest of the first two days from a fresh save.

### 4i-B: Settings and accessibility

**Purpose.** The player can make the game comfortable: hear it the way they like, turn down what bothers them, play on their device, and get help with timing if they need it.

**Subjective acceptance.** Every option does exactly what it says, immediately and audibly or visibly; settings survive a restart; nothing in the options menu breaks the pixel look; someone sensitive to shake and flashes can play comfortably; someone who struggles with the timing minigames can still enjoy service.

**Systems and assets.** A new `SettingsStore` (Core; a small JSON file beside the save on desktop, the same IndexedDB sync on the web; decision D4), `GameSettings` (load and save), a new `AudioMixer` asset (Master, Music, Effects, UI) and routing of MMFeedbacks/TDE sounds and our own sources into it, `MusicDirector` (Hollows' effects level through the mixer instead of the listener), display settings on desktop, `DialogueSettings` (text speed), the minigames' tuning assets (assist multipliers).

**Tasks.**
1. **Options screen** (from the main menu and the pause menu), tabbed: Audio, Feel, Display, Accessibility, Controls (read-only reference in 4i).
2. **Audio:** Master, Music, Effects (and UI) sliders through the mixer; the Hollows' quarter-down becomes a mixer parameter; music volume and per-track levels stay authored on `MusicConfig` underneath the player's slider.
3. **Feel:** screen shake (off / low / full), flashes on/off, hit-stop on/off; vibration on/off, intensity, reduced vibration (all already honoured by the code).
4. **Display (desktop):** fullscreen or windowed; window size in whole multiples of 320×180; VSync stays on. Web: a fullscreen button in the pause menu.
5. **Readability:** dialogue text speed (slow / normal / instant) and hold-to-fast-forward; a contrast check of every text against its panel. Text size: see decision D6 (the honest options are limited).
6. **Assist** (decision D6): one **relaxed timing** option that widens the grill's, tap's and chopping's perfect bands and slows their needles a little (multipliers on the minigame configs, pure logic); a **patient customers** option (longer patience). Optional and named plainly; never a score penalty in 4i.
7. **Colour audit:** freshness, quality dots, Essence low state, telegraphs, request hearts, checked with a deuteranopia/protanopia simulation; fix anything that relies on hue alone with a shape or an icon.
8. **Settings persist** across launches and are applied before the first frame the player sees.

**Tests.** EditMode: settings round trip and defaults; migration of an absent file; assist multipliers on the minigame rules; slider-to-decibel mapping; feel settings still scale shake, flash, hit-stop and haptics (extend `HapticTests` and friends). PlayMode: options open from both menus; a volume change reaches the mixer; the Hollows' effects level holds under the player's sliders; relaxed timing widens the grill's band in a real station; settings survive a scene change.

**Web/PC.** Web smoke: change music and effects volume, reload, still applied (backing up and restoring the owner's save). PC: options on a Windows build (from 4i-D's build path if it lands earlier; it may move into this checkpoint).

**Your tasks.** Try each option with sound on, on keyboard and controller; rumble on a real controller at each intensity; play one evening on relaxed timing.

**Risks.** Routing every MMFeedbacks and TDE sound into the mixer touches many generated prefabs (through the updaters; a broad but mechanical change); assist tuning changes the balance report (run *Evening Report*, keep `RenownAndBalanceTests` green; assisted play is excluded from the balance targets).

**Not in 4i-B.** Control remapping (decision D6), languages beyond English, combat speed, a full "assist mode" for the Hollows (damage taken, Essence drain), colour-blind filters.

**Gate.** Stop for the owner's playtest of the options.

### 4i-C: Presentation and polish

**Purpose.** The slice sounds and looks finished in the places a stranger touches most, and the credits are honest and complete.

**Subjective acceptance.** Nothing sounds like a placeholder in the first hour; the music and effects sit well together; transitions feel deliberate; every screen looks like the same game; the credits read well and the HeatleyBros link works.

**Systems and assets.** Sound effects (decision D5) replacing `PH_*.wav` through the existing `MMF_Player`s (no new feedback system); the mixer from 4i-B; a **Credits** screen (main menu, and pause menu → about); UI sprites; transitions (`TransitionScreen`, `AreaFadeView`); `docs/CREDITS.md`.

**Tasks.**
1. **Sound effects, prioritised by how often they're heard:** footsteps (surface and Hollows), the cleaver's swings and hits, enemy telegraphs and deaths, pickups, doors and stairs, the grill, tap, chopping and plating, coins, UI clicks, the garden's actions, dialogue blips. About 25 sounds cover most of the first hour; the rest follow if the source allows.
2. **Audio balance pass** with the real effects: per-group levels, the Hollows' level, the music's per-track levels, ducking under dialogue if it helps; decide Prep's music (the open question).
3. **Credits screen:** scrolling, at the type scale, localized; HeatleyBros with a working link (`Application.OpenURL` on a click, which also works in the web build); Minifantasy (Krishna Palacio) and the Portrait Generator; Silver (Poppy Works); More Mountains, Super Text Mesh, Pixel Crushers by courtesy; made with Unity. Its text lives in the **Credits** table and is checked against `docs/CREDITS.md`.
4. **Presentation fixes:** the delve controls line and the satchel; the death screen's centring; the bat wing icon and the shroom cap and spore sac icons (Minifantasy first, catalog CSVs); consistent fades between every pair of places.
5. **Feedback audit:** list every player action and confirm each has its combined feedback; fill gaps (talking, buying, the menu board, the garden's harvest are likely candidates).
6. **Selective visual polish** only where a newcomer will look: the menu backdrop finished, Tally Ho!'s first view, Kariaston's square. No new art pipelines.

**Tests.** EditMode: every credit present (the table against the CREDITS document's required entries); every `MMF_Player` with a sound references a non-placeholder clip (a placeholder report, not a failing test, until the source covers them all); localization coverage of the Credits table; `TextStyleTests` on the credits. PlayMode: the credits open, scroll and close; the link button calls the URL service (mocked).

**Web/PC.** Web smoke with sound: the first minutes of a day, an evening, a fight; the credits' link opens in a new tab. Check the web build's memory peak with real effects.

**Your tasks.** Listen to a full day with headphones and with speakers; approve the sound choices (or swap them); click the HeatleyBros link in the web build.

**Risks.** The sound source (D5) is the biggest unknown in 4i; licences must allow commercial use and be recorded in `docs/CREDITS.md` and `docs/THIRD_PARTY.md` before import. Polish expands to fill time: hold to the list.

**Not in 4i-C.** New music, new art packs beyond icons and the menu, new animations, a decorative title font unless the owner asks (CLAUDE.md allows one here), a keeper portrait (decision D8).

**Gate.** Stop for the owner's listening and look playtest.

### 4i-D: Playtest-ready vertical slice

**Purpose.** A stranger can download or open the game, play several days without help, and send useful feedback; 4i closes and `milestone-4i` is tagged.

**Subjective acceptance.** External testers finish at least two full days without asking how; their feedback is about the game, not about bugs or confusion; the owner is comfortable putting the build in front of people.

**Systems and assets.** `BuildTools` (release Web and Windows builds, batch and menu), version stamping, project settings (compression), performance measurement, save compatibility checks, a tester kit (docs and a page).

**Tasks.**
1. **Release builds:** a non-development Web build with Brotli (or gzip for hosts without the headers; decision D7) and the decompression fallback; a Windows build (decision D7: IL2CPP or Mono); both from one menu and one batch method each; the version (for example `0.4i.<build>`) stamped from git and shown in the menu and the pause menu; development builds stay available for the owner.
2. **Performance pass:** measure and record startup to menu, menu to day, each content-set swap, a busy service, the troll fight, on the web (Chrome, plus Firefox once) and Windows; set budgets (proposed: 60 fps on a mid laptop, swaps under 2 s on desktop and under 4 s on the web); fix the worst offender. **Music memory:** stop holding a decoded paused track during Decorate Mode on the web, or stream on desktop; record the peak.
3. **Save compatibility:** a 4h save (v10) continues in 4i untouched; v7–v9 still migrate; settings live outside the save, so no version bump is needed unless 4i-A's backup format requires one (it shouldn't).
4. **Full regression:** EditMode and PlayMode; a scripted five-day soak (debug skips plus real days) checking saves, garden growth, schedules, Gimp's night once, music cues; the 4h playtest checklist again on the release build; the bark bubble's position in the browser.
5. **Tester kit:** a short how-to-play page (a refreshed Field Guide, current screenshots), known issues, how to reset a save, what kind of feedback helps and where to send it (decision D9), the controls summary; an itch.io page set to restricted (if that's the host) prepared for the owner to publish.
6. **External playtest** run by the owner (decision D9: who, how many, how); Claude turns the feedback into a triaged list; blocking fixes go in before closeout, the rest to Phase 5.
7. **Closeout:** docs, roadmap, `milestone-4i`.

**Tests.** EditMode and PlayMode suites; a build-script test (release options, version string format); the soak as an explicit PlayMode test; save fixtures from v7 to v10 loading (extend the migration tests with a real 4h save if the owner shares one).

**Web/PC.** Both release builds, smoke-tested from a clean browser profile and a clean Windows user folder (no save), then with a migrated save.

**Your tasks.** Upload the builds (itch or elsewhere); run the external playtest; play the release build yourself on a controller.

**Risks.** IL2CPP build times and platform quirks; compressed web builds need correct server headers (the fallback covers hosts that don't); testers' feedback can expand scope: anything that isn't blocking goes to Phase 5.

**Not in 4i-D.** Mac or Linux builds, Steam, localization into other languages, achievements, analytics or telemetry, new content.

**Gate.** Stop for the owner's review of the external playtest and the sign-off.

---

## 3. Decisions for the owner before 4i-A (answered 2026-10-08: see §5)

1. **D1: the main menu's backdrop.** (a) A live view: the Kariaston scene behind the menu, a slow camera pan over the square at dusk (most immersive; adds a scene load before the menu, so a slower start, more on the web), or (b) a still composed from the same art at 320×180 (fast, cheap, no movement). *Recommendation: (b) for 4i, keeping (a) as a possible later upgrade.*
2. **D2: quitting.** From the pause menu: in the daytime and evening the game saves and returns to the menu; mid-delve, quitting loses that delve (as today) after a confirmation. *Recommendation: as described; no mid-delve saves.*
3. **D3: first-day guidance.** (a) Diegetic: Orik and Boog say a few practical things on the first free day, and one-time prompts at the garden, the menu board and the market; or (b) a small "things to do today" list on the HUD for the first days. *Recommendation: (a); (b) only if the playtest shows people still lost.*
4. **D4: where settings live.** A small settings file beside the save (and in IndexedDB on the web), separate from `SaveSystem`'s game save: settings are the player's, not the keeper's, and survive New Game. *Recommendation: yes; `SaveSystem` stays the only authoritative game save.*
5. **D5: sound effects.** Who makes them, and with what budget: a purchased pack (for example a fantasy or retro SFX pack with a commercial licence), free CC0 sources, or commissioned. *This decides 4i-C's size more than anything else.*
6. **D6: accessibility scope for 4i.** *Recommended in:* shake, flash and hit-stop, vibration, volumes, dialogue text speed, relaxed timing for the minigames, patient customers, a colour audit. *Recommended deferred to Phase 6:* control remapping, a combat assist (damage and drain), larger text (Silver at 2× would need every layout rebuilt; the one realistic option is a 2× dialogue box only, which could be a 4i stretch if you want it).
7. **D7: builds.** Windows only for PC in 4i (yes?); IL2CPP or Mono (*recommendation: Mono for 4i's faster iteration, IL2CPP before any public release*); Brotli with the decompression fallback for the web (*recommended*), unless the host serves gzip only. The Unity Product Name stays "Hearthdelve" (it sets the save folder).
8. **D8: a keeper portrait** in dialogue (none since 4g). *Recommendation: leave it out of 4i.*
9. **D9: the external playtest.** How many testers (*suggestion: three to five*), who (friends, a small Discord, an itch restricted page), and where feedback goes (a short form, a shared doc, or messages to you).
10. **Phase 5 wording.** The roadmap item "Tavern, Sanctuary and Stronghold expansion" conflicts with the Stronghold direction dropped on 2026-10-04 (CLAUDE.md, GDD §6.4 and Appendix C). Is this (a) the property growing as a tavern, inn and home under those names, (b) a revival of the Sanctuary and Stronghold progression (story and building), or (c) something else? Recorded as open until you say.

---

## 4. Phase 5 roadmap proposal (for discussion; the approved baseline is in §5)

**Approved direction (2026-10-07 and 2026-10-08):** a proper fantasy calendar; recurring village festivals and community events; villager birthdays; eventual seasons; fishing and further optional daytime activities; the remaining Hollows biomes; tavern and property expansion (the Sanctuary and Stronghold wording is decision 10, above); the remaining story acts. Already approved earlier (GDD §11.1): the Inn and Visitors, settling residents on the three plots, ranching, farming depth.

**Principles carried forward.** The calendar creates things to look forward to, never punishing deadlines; story-critical events stay reachable (a missed festival comes round again, a story beat waits for the keeper); calendar lengths, season counts, birthdays and festival dates are not decided until the calendar milestone is planned; each life-sim system is proved with its smallest version first (GDD §11.2); content milestones alternate with system milestones so the game stays playable and fun at every tag.

**Proposed order.**

| # | Milestone | Why here |
|---|---|---|
| 5a | **Story revision (design only)** for Acts II–IV in the village direction (GDD §2.9's conflict list), and decision 10 | Every later milestone hangs off it (biomes, the property, festivals' meaning); a document, no code |
| 5b | **The calendar and the first festival:** a calendar layered on the existing game day (names, a date on the HUD, schedule conditions by date), one festival end to end, the cast's birthdays as small authored moments | Gives every day an identity and something to anticipate; schedules already key off the game day, so this layers rather than rewrites (CLAUDE.md, 4h) |
| 5c | **The Hollows, Biome 2** (new enemies, ingredients, a boss, recipes) | Keeps the combat and cooking pillars moving between life-sim systems |
| 5d | **The Inn and Visitors:** guests in the rooms, Visitor promotion, daytime Visitors (deferred from 4h) | Already first in GDD §11.1's order; reuses the furniture architecture |
| 5e | **Fishing** and one more optional daytime activity (foraging is the cheapest candidate) | Optional daytime variety; a new minigame (a learning priority) |
| 5f | **Settling residents** on the three plots | Needs the Inn and Visitors first |
| 5g | **Seasons** (if the calendar has proved itself): seasonal crops, world dressing, seasonal festivals | Only after 5b shows the calendar works; seasons touch farming, art and schedules at once |
| 5h | **Property expansion** (per decision 10), **ranching**, **farming depth** | Larger systems once the village is full of people |
| 5i+ | **Biomes 3–7, Acts II–IV**, more villagers and relationships, the growing catalog | Content build-out, in alternating slices |

Open for the Phase 5 plan: whether fishing comes before the Inn (more immediate fun) or after (more structural value); whether Biome 2 comes before the calendar (combat momentum) or after (the village keeps its momentum from 4h).

---

## 5. The owner's decisions (approved 2026-10-08)

The four-checkpoint structure is approved; each checkpoint is built, then stopped for the owner's playtest and separate approval. These supersede the questions in §3.

- **D1, menu backdrop:** a still composed from owned Minifantasy game art at 320×180 (Tally Ho! or Kariaston, warm); no live village behind the menu; no new art bought or commissioned without asking.
- **D2, quitting:** save-and-quit from the pause menu with warnings and safe resumption. The free day keeps its progress. Mid-delve quitting asks, abandons the delve (the haul and run rewards never bank), never touches banked gold, unlocks or the tavern, and adds no mid-delve saving. **Every phase audited** (below); no half-done state is serialized to make a button work; anything not resumable returns to a known checkpoint and says what will be lost. Save version 10 stays unless a bump is genuinely needed.
- **D3, first-day guidance:** Orik and Boog, in their own voices, through the Dialogue System, brief: the village outside, Musashi's market, the garden, Vigor limiting strenuous work (not exploring), and the menu board starting Prep whenever the keeper likes. A few one-time contextual prompts; no checklist; the soft clock, optional Prep and free exploring preserved.
- **D4, settings:** a separate persistent settings file (the player's, not the save's): survives relaunch, loading, New Game and scene changes; IndexedDB on the web; not in `SaveData` v10. Mostly 4i-B.
- **D5, sound effects:** licensed libraries and CC0 first. Four libraries are in `C:\Dev\Music\SFX` (Kenney RPG Audio, Kenney Impact Sounds, Kenney Interface Sounds, an OwlishMedia general library); inspect the real folders and licences; nothing more bought or downloaded without approval. A reusable catalog process kept outside the repo beside the libraries (discovery; file, pack, path, format, rate, length, channels; licence; purpose; matches to the placeholders; missing categories; what needs listening or editing; approval before import). Only approved, game-ready clips are imported. Sonniss GameAudioGDC bundles are not present; they may join the catalog later. About 25 high-impact sound families for 4i-C (listed in the owner's brief); variations for repeated sounds; crisp, tactile, slightly stylized. The full pass is 4i-C; 4i-A may use a few Kenney interface sounds if trivial.
- **D6, accessibility:** 4i-B has the master/music/effects volumes, screen shake, flashes, hit-stop, vibration on/off, intensity and reduced vibration, dialogue text speed (with instant), relaxed cooking timing, patient customers, and a colour audit; all optional, clear, no score penalty in this milestone. **Deferred and recorded:** full control remapping, full UI/text scaling, combat assists, advanced accessibility modes, extensive layout rework.
- **D7, builds:** Windows and Web. Mono is fine for internal Windows builds; an IL2CPP Windows build validated on an independent machine before any external test. Web gets a release configuration with Brotli and the decompression fallback, keeping a development option. No Mac or Linux in 4i.
- **D8:** no keeper dialogue portrait in 4i.
- **D9, external playtest (4i-D):** 3–5 fresh players through a restricted itch.io page or similar; a short updated Field Guide, controls, instructions, known issues, a short questionnaire, save-reset steps and the version; no analytics. The owner publishes and invites.
- **D10, Phase 5 story direction:** Tally Ho! and Kariaston grow naturally as an inn, a property and a village. The formal Sanctuary → Stronghold transformation is **not** revived (no fortress, defense system or construction mechanic); the story may still bring escalating threats from the Hollows, refugees and new arrivals, more people settling, more rooms and property, village improvements and greater consequences of delving. Acts II–IV are revised in 5a. Recorded in the GDD (§11.1, §13 Open 13 resolved).

**Phase 5 provisional sequence** (a planning baseline, not permission to build): 5a story revision (design only) → 5b the fantasy calendar and first festival (calendar, birthdays, recurring festivals, one authored community event) → 5c Hollows Biome 2 → 5d the Inn and Visitors → 5e fishing and foraging → 5f settling residents → 5g seasons → 5h property expansion, ranching and farming depth → 5i onward: biomes, story acts, content. Not locked: month and year lengths, weekday names, birthday and festival dates, season lengths. Calendar events create anticipation, never punishing deadlines; story-critical progress is never permanently missable because of a date.

---

## As built: 4i-A, "First impressions" (2026-10-08; signed off by the owner 2026-10-08)

**Signed off by the owner (2026-10-08)** after playing the whole checklist: the menu, the first morning, the hints, pausing in each part of the day, quit and Continue from each moment, switching between keyboard and controller, and 320×180, "all works as described". (The sign-off's note on whether a real controller was used was left blank; a controller check stays on 4i-B's list.) Carried into 4i-B: the menu's save message strip must read clearly over the backdrop (part of the colour and contrast audit), and an Esc pressed while a scene fades in should be queued rather than dropped. Not tagged: `milestone-4i` waits for 4i-D.

### Quitting, phase by phase (D2)

The rule lives in one pure place (`Shared/Game/QuitRules`, EditMode-tested) and the pause menu follows it. Only the moments the save already supports are resume points; nothing new is serialized. **Save version stays 10.**

| Moment | Saveable there? | Quit to menu does | Continue loads | Lost | Asks first? | Could anything duplicate? |
|---|---|---|---|---|---|---|
| **Free day** (Tally Ho!, Kariaston, any minute) | Yes (v10 keeps the minute, Vigor, the garden) | Saves, then the menu | The same minute of the same day | Nothing | No | No: the save is one snapshot; purchases and garden work already saved as they happened |
| **Arrival day** (the opening) | No: its beats aren't resume points | The menu; the last save stands | Arrival day from its start (the new game's own save) | Arrival day's progress (the welcome replays) | **Yes** | No: arrival day gives no rewards |
| **Evening prep** | No | The menu; the last save stands | The last save: normally the day as left when the evening began (`StartEvening` saves it) | Prep choices and butchery | **Yes** | No |
| **Service** | No (customers, orders and stations aren't state) | As above | As above | Tonight's service so far: takings, dishes, requests and the evening's relationship deeds | **Yes** | No: nothing was banked |
| **Results** | Banked exactly as "close up" banks them (`GameFlow.BankEvening`, the same `DayRules.CompleteService`) | Banks, saves (phase Delve), then the menu | The night's delve | Nothing | No | No: banked once and the evening scene is left; a test checks the gold arrives once |
| **The delve** (running) | No mid-delve saves | The menu; the last save stands | The night's delve from its start (saved when the evening closed) | The haul, the delve's gold, powers, curios and quest objects carried; banked gold, unlocks and Tally Ho! untouched | **Yes** | No; as since 4d, the delve can be tried again from its start |
| **The delve's result** | Brought home exactly as its button would (`CompleteDelve(report, goHome: false)`) | Brings home, saves (phase Night), then the menu | The night | Nothing | No | No: applied once; the dungeon is left |
| **Night** | Yes (saved when the delve ended and after each purchase) | Saves, then the menu | The night | Nothing | No | No |
| Conversations, story scenes (Gimp's night), transitions | — | The pause menu isn't offered | — | — | — | — |
| Stations, panels, questions, Decorate Mode | — | Esc / B backs out of them first; then pause | — | — | — | — |

Quit game (desktop) does exactly the same, then closes the application; on the web it isn't offered (the browser closes the page).

### What was built

- **Main menu (A1):** a still of Kariaston and Tally Ho! behind the title (rendered once from the village scene at 320×180 with the keeper and HUD out of frame, a little warmth and a vignette: `Art/Menu/MenuBackdrop.png`, from the explicit `CaptureMenuBackdrops`), Continue / New Game / Controls / Quit (Quit on desktop only), the version in the corner (`Application.version`, now `0.4i-a`), and the `day {0}, {1}` flash fixed (the line is invisible until its text is set). Options and Credits are not shown until 4i-B and 4i-C build them. Controller navigation is the layout's own, wrapping in the pause menu.
- **Pause menu (A2):** in Boot on its own canvas over everything (`PauseMenu`, `Menus` canvas built by `FirstImpressionsUI`). Esc or Start opens it when `PauseRules.CanOpen` allows; Esc, B or Start closes it. While open: `MenuPause` (time stops), the surface clock holds, the keeper's controls are off; closing restores exactly the maps that were on. The rule: on foot (the day, arrival day, service, the Hollows) and on the evening's and night's own screens (Prep, the results, the night, the delve's result); never during a load, a conversation, a story scene (`PauseRules.Block`, used by Gimp's night), or while a station, panel, question or Decorate Mode owns Esc. An Esc that just closed a panel never also opens the menu (it must have been pausable the frame before).
- **Controls reference (A3):** five pages (the day, the evening, the Hollows, decorating, menus and talking), both keyboard and controller columns on every page with the device in use lit; left/right turn the page; from the main menu and the pause menu (which opens on the page for where the keeper is). Hand-written from the actions asset in players' words (`MenuLocKeys.Pages`).
- **Device-aware prompts (A4):** `Core/Input/InputDevices` follows the last *meaningful* input (a key or button, or a stick or trigger past half way; mouse movement and stick drift never switch it; pure rule `InputDeviceRules`). Every prompt reads it through `InputHints`; gamepad names are the ones players know (A, B, X, Y, LB, RT, Start, View). Station prompts now show one binding each ("Space", "W/A/S/D") and update if the player switches device mid-station.
- **First free day (A5, D3):** `Act1/FirstMorning` (seeded once; the node editor owns it), Orik and Boog, seven short lines, played once as the keeper first comes downstairs on day 2 (an opening beat, `OpeningRules.Downstairs`, from a new `KeeperEnteredArea` fact; only at the opening's `FirstEvening` stage, so saves past the opening never get it). Three one-time prompts (`FirstDayPrompts`, `SurfacePrompts` over the interaction hint, seven seconds): the garden (Vigor), the market, the menu board (Prep whenever the keeper chooses). Each once per game, in the daytime after arrival day; existing saves see each once too.
- **Safer saves (A6):** each write keeps the save it replaces as `save_slot_1.backup.json` if that one reads cleanly; an unreadable one is set aside as `save_slot_1.unreadable.json` and never becomes the backup; a save from a newer version is never loaded or touched. The menu says plainly when the save can't be read (and Continue then loads the backup, "the backup, day N"), or that it's from a newer version (no Continue). New Game asks before replacing any save file, readable or not. A brief "saved" mark in the corner on every save (`SaveIndicator`, `GameSaved` fact). The web build's IndexedDB flush covers the backup and the set-aside file (same directory, same flush).
- **UI clarity (A7):** the station controls lines fit (compact bindings; the tap's line now "pour · tilt the glass").

### Deviations and notes

- **Decorate Mode has no pause menu:** its own keys own Esc (cancel) and Start (the catalog); leaving Decorate Mode (Tab / View) and pausing works. Recorded as the rule.
- **No pause on the death screen, the swap prompt or a power choice:** those are the moment's own choices (they already pause the game).
- **No new audio or art beyond the backdrop:** the Kenney interface sounds wait for 4i-C's catalog so they're chosen with everything else.
- The version is set by hand (`0.4i-a`); stamping from git is 4i-D.
- Two English strings changed: the tap prompt ("tilt the glass") and the controls header ("keyboard"). `TextStyleTests` now also covers 4h's surface and garden strings and 4i-A's, and "Vigor" joins the resource names that keep a capital.
- Gamepad names assume the Xbox layout (as every prompt already did).
- **Found in the web smoke test and fixed:** the Web player had exception support "None" since the project's first commit, so *any* exception, even one caught, stopped the page behind a blocking alert (an unreadable save froze the browser; this also affected the old menu's own catch). `BuildTools` now builds with "explicitly thrown exceptions only", and the save check recognises the usual damage (empty, cut off, not a save, a bad version) without throwing at all (`SaveSystem.LooksWhole`).

### Tests

EditMode `FirstImpressionsTests` (quit plan per phase, pause rules, device rule, first-morning beat and prompts once, backup and set-aside, newer-version and unreadable saves, every controls page and message fitting). PlayMode `FirstImpressionsPlayTests` (pause in the day holds the clock and controls and gives them back; pause waits for conversations, Decorate Mode and story scenes, and is there at Prep; quitting from the day, Prep, service, results, the delve, the delve's result, the night and arrival day, with what Continue loads and that nothing is banked twice; the first morning once on coming downstairs and never after the opening; the three prompts once each; the main menu's controls and version; an unreadable save with its backup; a newer-version save; the saved mark; prompts per device). Explicit captures: `CaptureFirstImpressions`, `CaptureMenuBackdrops`.

### Playtest checklist (4i-A)

1. Launch: is the menu clear at a glance? Try it on keyboard and controller.
2. New Game through arrival day; on day 2 come downstairs: does Orik and Boog's morning feel natural and short?
3. Walk to the garden, the market and the menu board: do the prompts help without nagging?
4. Pause in the day, in service, in the Hollows, at night; open the controls on each device; resume.
5. Quit from the day and Continue (same minute?); quit during Prep or service (is the warning clear?); quit mid-delve (clear?).
6. Switch between keyboard and controller: do the prompts follow without flickering?
7. Does everything look at home at 320×180?

## As built: 4i-B, "Settings and accessibility" (2026-10-08; signed off by the owner 2026-10-08)

**Signed off by the owner (2026-10-08)** after the checklist, except the controller items (no real controller available): "everything works as described". Two notes from the playtest were fixed before sign-off (commit 5d20b427): the web fullscreen line didn't change until the tab was reopened (the browser switches a moment after it's asked; the line now shows the choice at once and follows the browser), and Musashi drew over the keeper standing in front of him (his figure had no sorting group; `FigureSortingTests` now checks every layered figure). The sign-off's notes on relaxed timing and fast clicks were left blank. The owner has a controller since (2026-10-08): the controller checks (4i-A's, and 4i-B's items 1 and 4) are carried onto 4i-C's playtest checklist. Carried into 4i-C: fast clicks on an Options line. Not tagged: `milestone-4i` waits for 4i-D.

Approved as written in §4i-B with D4 and D6 (2026-10-08), with the owner's details: Options on the main menu between New Game and Controls and in the pause menu; tabs Audio, Feel, Display, Accessibility, Controls; 5% volume steps heard at once; a sample rumble on vibration changes; a separate options file that survives New Game; the Hollows' quarter-down on the mixer; routing only through generators and updaters. Carried from 4i-A: the menu's message band and Esc during a fade. **Save version stays 10.** Not tagged.

### What was built

- **Options (`OptionsScreen`, `OptionRow`, built by `FirstImpressionsUI.BuildOptions`):** one screen shared by the main menu (between New Game and Controls) and the pause menu (under Resume). Five tabs; Q / E or LB / RB change tab, up / down choose a line, left / right (stick, d-pad, arrows) or a click on either half change it, Esc / B goes back. Every change is saved and applied at once. The Controls tab is 4i-A's controls page.
- **The options file (D4; `Shared/Settings`: `PlayerOptions`, `OptionsRules`, `OptionsStore`, `GameOptions`):** `options.json` beside the save (IndexedDB on the web, flushed after each write), never in `SaveData`. It's read before the first scene loads (`RuntimeInitializeOnLoadMethod`, BeforeSceneLoad) and applied before the first frame. A missing, damaged or out-of-range file gives the defaults or is brought back into range, and never throws. New Game, Continue and relaunching never touch it.
- **The mixer (`Audio/GameMixer.mixer`, made by `AudioMixerBuilder` through Unity's own mixer editor API, since there's no public one):** Master with Music and Effects under it, each an exposed volume. `AudioMixerHub` in Boot sets them from the options, in decibels (0% is −80 dB). **The Hollows' quarter-down is now a mixer parameter:** Effects is multiplied by `effectsInHollows` while the delve runs. `AudioListener.volume` is no longer touched, and the music no longer divides it back out.
- **Routing:** the generators route every new sound to Effects (`LookTestContent`'s feedback sounds, `TavernFeedbackContent`'s loops), and the music director's sources go to Music. A one-time in-place pass (`AudioRouting`, *Hearthdelve → Generate → Route Sounds to the Mixer*) routed the 54 sounds that already existed in the prefabs and the game's scenes, leaving the 4a look scenes as baselines. The hub also sends any unrouted sound in a newly loaded scene to Effects as a safety net. Tests check that nothing in the prefabs or the game's scenes is unrouted.
- **Feel:** shake off / low (40%) / full, flashes, hit-stop, vibration on/off, its strength (10–100%, dimmed while vibration is off), and reduced vibration. All of them go to `GameSettings`, which the feedback code already honoured. Changing any vibration line fires a sample rumble (`Tap.Firm`).
- **Display:** desktop has fullscreen (the display's own resolution, borderless) or a window in whole multiples of 320×180 that fit the display (largest by default). The web has only fullscreen (the page's, requested by the click or key). VSync stays on.
- **Accessibility:** text speed slow (0.55×) / normal / instant, through the dialogue box's reveal (instant writes the line out whole). **Hold to hurry:** holding E, Enter, Space, A or the mouse in a conversation writes lines out 4× faster and moves on after a short pause, but never past a choice (choices stay locked while it's held). **Relaxed cooking timing** (off by default; `Tavern/Minigames/AssistRules`, pure): the keeper's grill, tap, chopping board and Butcher Block get targets 1.6× wider about their centres and a 0.8× pace (meters and knives slower, time limits longer). Scoring is unchanged, so there's no penalty. Staff, the balance report and everyone else use the ordinary factory (`TavernDirector.KeeperMinigames` versus `Minigames`). **Patient customers** (off by default): seat and food patience × 1.6 when a customer is created.
- **Esc during a fade (carried from 4i-A):** an Esc or Start pressed while a scene changes or the transition covers the screen is kept for 2.5 seconds and opens the pause menu as soon as it may (`PauseRules.Queues`, `StillQueued`). A press refused for any other reason (a panel, a conversation, a story scene) is still meant as a refusal.
- **The menu's message (carried from 4i-A):** a full-width band at 90% opacity under the title, so "the save can't be read" reads over any part of the still.
- **Colour and contrast audit:**
  - Quality (a pip count), freshness (a bar's length), Essence low (a pulse and a different sprite) and requests (a heart or frown icon) already carry their meaning in shape or size as well as hue.
  - **The Larder Troll's telegraph** relied on hue: under a protanopia simulation the old deep red was barely lighter than the cellar floor (1.07:1). It's now a lighter orange-red at a little more opacity (1.6–1.8:1 above the floor under every simulation, still read as danger).
  - **Text:** a new check measures every text in the game's scenes against what it sits on. Five secondary tones (labels, gold accents, notes, discovery blue, the "saved" green, warnings, titles) were 2.6–4.4:1 on parchment. They're darkened, keeping their hue, to at least 4.5:1 on all three parchment faces (`UI/Typography/UiPalette`). The generators read the palette, and a one-time in-place pass recoloured the existing 196 (`AccessibilityUpdates`, *Hearthdelve → Generate → Apply the Colour Audit*).

### Changes from the plan

- **No UI slider and no UI mixer group.** Interface sounds play through the same feedback players as everything else, so they're routed to Effects; the owner's rule was a UI slider only if UI sounds have their own group.
- **Settings live in `Shared`, not `Core`** (`Shared/Settings`), beside `SaveSystem`'s `WebStorage` flush and the existing save-damage check they reuse. The file is `options.json`.
- **Relaxed timing also covers the Butcher Block**, the keeper's fourth timing minigame; the plan named only the grill, tap and chopping.
- **Web fullscreen is a line in Options' Display tab**, as the owner specified, not a separate pause-menu button.
- **No "reset to defaults" button.** `GameOptions.ResetToDefaults` exists for tests and later; with only five tabs it wasn't worth a line yet.
- **The contrast check is a test (`ContrastTests`)**, at WCAG AA (4.5:1; 3:1 for 2× and 3× headings). It measures boxes, not drawn pixels, so three readings were checked by eye on captures and are listed in the test with their reasons. HUD text over the world isn't measured.
- **Windows build:** checking Options on a Windows build needs 4i-D's build path, so it's left for 4i-D as instructed. Options was checked in the editor's play mode and on the web build.

### The options and their defaults

| Tab | Option | Values | Default |
|---|---|---|---|
| Audio | master volume | 0–100% in 5% steps | 100% |
| Audio | music | 0–100% in 5% steps (under `MusicConfig`'s authored levels) | 100% |
| Audio | sound effects | 0–100% in 5% steps (a quarter down in the Hollows, on top) | 100% |
| Feel | screen shake | off / low / full | full |
| Feel | flashes | on / off | on |
| Feel | pause on big hits (hit-stop) | on / off | on |
| Feel | vibration | on / off | on |
| Feel | vibration strength | 10–100% in 5% steps | 100% |
| Feel | reduced vibration | on / off (caps rumble at 40%) | off |
| Display (desktop) | fullscreen | on / off | on |
| Display (desktop) | window size | 320×180 × 1 … the largest that fits | the largest that fits |
| Display (web) | fullscreen | on / off (the browser's) | off |
| Accessibility | text speed | slow / normal / instant | normal |
| Accessibility | relaxed cooking timing | on / off | off |
| Accessibility | patient customers | on / off | off |
| Controls | the 4i-A controls reference | — | — |

### Balance

*Hearthdelve → Balance → Evening Report* rerun after the assist multipliers: unchanged, because the report and every staff member use the ordinary minigames. Assisted play stays out of the balance targets. `RenownAndBalanceTests` pass.

### Tests

- **EditMode `OptionsTests`:**
  - volume steps and decibels;
  - the defaults;
  - window sizes;
  - text speed;
  - out-of-range values brought back;
  - the file round trip;
  - a missing or damaged file giving the defaults without throwing;
  - the options never in `SaveData` (still v10);
  - feel reaching `GameSettings`;
  - the mixer's groups and parameters;
  - every sound in the prefabs and the game's scenes routed, and Boot's hub;
  - no code lowering the listener;
  - Options' place in both menus;
  - relaxed timing's widening and pace, scoring unchanged, and staff untouched;
  - patient customers;
  - the Esc queue.
- **EditMode `ContrastTests`:**
  - every text in the game's scenes against its panel;
  - every palette tone on every parchment face;
  - the telegraph under the deuteranopia and protanopia simulations.
- **PlayMode `OptionsPlayTests`:**
  - from the main menu, every tab's lines;
  - a volume heard on the mixer at once and saved;
  - vibration strength dimmed with vibration off;
  - options surviving New Game;
  - the Hollows lowering effects on the mixer, never the listener, with the music on its own group;
  - from the pause menu, relaxed timing reaching only the keeper's stations;
  - patient customers in a real service;
  - instant and normal text speed in the first-morning conversation.

### Playtest checklist (4i-B)

1. Open Options from the main menu and from the pause menu, on keyboard and on controller. Is every line clear, and does left / right feel right?
2. Audio: move each slider with sound on. Is the change heard at once (a click on Effects, the tune on Music)? Go down into the Hollows: are effects still a little quieter there, under your own level?
3. Feel: shake off, low and full in the Hollows; flashes off; hit-stop off.
4. **Vibration on a real controller:** strength at 10%, 50% and 100%, then reduced vibration on. Is each sample rumble distinctly weaker, and does a hit in the Hollows match?
5. Display (desktop, in the editor's player or later the Windows build): fullscreen and each window size. Is every pixel crisp? On the web: fullscreen on and off.
6. Text speed slow, normal and instant in a conversation; hold E or A through one. Does it hurry without skipping a choice?
7. **Play one evening on relaxed cooking timing:** the grill, the tap, chopping and the Butcher Block. Does it help without feeling like the game plays itself? Then one with patient customers.
8. Change some options, quit, relaunch (and reload the web page): still as you left them? Start a New Game: still there?
9. Press Esc as a door fade or a scene change begins: the pause menu opens when it's done.
10. The menu with an unreadable save: does the message read clearly? Are the softer text colours (labels, gold, notes) still pleasant and in keeping?

## As built: 4i-C, "Presentation and polish" (2026-10-08; in progress: the non-audio work is built; sound waits for the owner's listening)

Approved as written in §4i-C with D5 (2026-10-08). Order: the sound catalog first, then a listening list for the owner; meanwhile the non-audio work; after approval, import, swap, fill the feedback gaps and balance.

### Sound catalog (D5)

Outside the repo, beside the libraries: `C:\Dev\Music\SFX\_catalog` (`README.md` for the process; `catalog.py` → `catalog.csv` and `packs.csv`; `candidates_4ic.py` → `LISTENING_4iC.md`). 442 sounds (22 minutes) in four packs, all CC0: Kenney RPG Audio (licence file present), Kenney Impact Sounds and Kenney Interface Sounds (published CC0; their licence files aren't in the folder), OwlishMedia's Sound Effects Pack (listed CC0 on OpenGameArt; no licence file). The three Kenney packs are flattened into one folder; the script tells them apart by name. 28 first-hour families have two or three candidates each (264 files), a proposed pick and the gaps no library fills (an air swoosh, a sizzle and fire, creature voices and the troll's roar, a hurt grunt, anything magical, a heartbeat). Nothing is imported until the owner approves.

### What's built so far

- **Credits** (`CreditsScreen`, built by `FirstImpressionsUI.BuildCredits`): from the main menu (after Controls) and the pause menu (after Controls). A parchment panel; its lines come from a new **Credits** string table (authored in `CreditsLocKeys`), at the type scale, drifting up slowly after a moment and scrolled by up/down, the stick or the wheel; HeatleyBros' link is the one selectable line (Enter / A / a click) and opens `https://www.youtube.com/c/heatleybros` through `UrlService` (`Application.OpenURL`; mocked in tests). Esc / B goes back. `CreditsTests` checks every credit `docs/CREDITS.md` names is on it and that the link is the same in both.
- **One timing for every change of place** (`Shared/Game/PlaceFade`, 0.2 s out and in): Tally Ho!'s front door (was 0.18 s), its stairs (0.25 s) and the Hollows' rooms (0.25 s out, 0.3 s in). Scene changes keep the captioned transition (0.35 s, a 0.7 s hold, 0.45 s) as the day moving on. Existing objects set in place (`PresentationUpdates`); a test checks every one.
- **The delve's controls line is gone from the day loop** (it overlapped the satchel and named both devices): the first delve's onboarding teaches the controls for the device in use (4g) and the pause menu has them all (4i-A). Run on its own, the test floor keeps it.
- **The death screen's lone button** stands in the middle when there's nothing to keep.
- **Icons:** shroom cap and spore sac have had icons since 4f (the known issue was stale). The bat wing keeps the vampire's cape: Minifantasy has no wing icon, and the bat's own drawings (a 6×11 folded wing, 11×4 spread) can't become an 8×8 icon without new drawing.
- **Notes name one device** (the 4i-A rule): Options' footer and the credits' footer follow the keyboard or the controller, which also put Options' note back on one line.
- **The keeper's room** (selective polish: the first thing seen each morning): a new game's room adds a wardrobe, a bookcase, a washstand, a lit candle stand and violets on the chest, all from the catalogue and all movable. Saves keep the room their keeper made.
- **Fast clicks on an Options line (carried from 4i-B): harmless.** Unity's UI reads the mouse once a frame, so clicks the browser driver sent within one frame counted once. A PlayMode test clicks through a simulated mouse at about eight clicks a second (one frame down, six up): four clicks, four steps.

### Feedback audit

From `Hearthdelve → Report → Feedback Audit` (117 feedback players in the prefabs and game scenes) and the code that plays the shared interface moments. "Visual" includes the station panels', bubbles' and HUD's own animation, which isn't in an `MMF_Player`. Every sound is still a placeholder until the swap.

| Player action | Sound | Haptic | Visual | Status |
|---|---|---|---|---|
| Walk (Kariaston, Tally Ho!, the Hollows) | — | — | walk animation | **gap: footsteps** (families 1–3) |
| Go through a door or up the stairs | — | — | the fade | **gap: door and stairs sounds** (14–15) |
| Talk to someone: the box opens | — | — | box, portrait | **gap: an opening sound** |
| A line writes out / advance | tick on advance | — | letters, the continue mark | **gap: dialogue blips** (27) |
| Plant, tend (spend Vigor) | tick | light tap | Vigor pips pop | partial: **an action sound** (26) |
| Harvest | chime | yes | note, pips | complete |
| Buy at the market | button click | — | the stock changes | **gap: coins and the buy moment** |
| Begin the evening (menu board) | commit | light tap | the question | complete |
| Decorate: pick up, place, store, sell, restyle, buy | yes | yes | selection, ghost | complete |
| Decorate: turn, flip, undo, enter, leave | yes | — | the piece | complete (light actions, no rumble by design) |
| Butchery: stroke, clean, ragged, done | yes | yes | cut lines | complete |
| Chopping: clean, ragged, done | yes | yes | cuts | complete |
| Grill: flip, perfect flip, burn, sizzle | yes | yes | meter, needle | complete (sizzle has no real source: gap) |
| Tap: pour, line, spill warning, overflow, done | yes | yes | glass, foam | complete |
| Plates: pick up, put back, serve, drop, bumps | yes | yes | carried plate | complete |
| Stew ready; a customer pays or walks out | yes | — | bubbles | complete (not the keeper's own actions) |
| Results counting, the takings; the night's upgrades | yes | takings no, buy yes | counting | complete |
| Attack swing (a miss) | — | — | swing animation | **gap: a swing sound** (5) |
| Hit, heavy hit, finisher, charge levels | yes | yes | shake, hit-stop, the enemy flashes | complete |
| Dodge roll | yes | — | the roll | partial: **a light rumble** |
| Hurt | yes | yes | shake, flash | complete |
| An enemy telegraphs; dies (clean or overkill) | yes | yes | flash, mark, death | complete |
| Pick up an ingredient, gold, a curio, a power, a quest object | yes | yes | feed, HUD | complete |
| Satchel full; rooms seal and clear; fall; climb out; campfire; low Essence | yes | yes | prompts, gates, the fade | complete |
| The Larder Troll: entrance, slam, stun, spoil, frenzy, gulp, defeat | yes | yes (not the gulp) | shake, flash | complete |
| Menus: buttons, Options steps, the credits' link | yes | commit and buy | selection | complete |

The gaps are filled after the owner approves the sounds (they need the new sounds and, for footsteps, doors and blips, small new hooks through the generators).

### Changes from the plan (so far)

- **The delve's controls line is retired in the day loop**, not moved: onboarding and the controls page cover it, and it named both devices.
- **"Decide Prep's music"** was settled by the owner on 2026-10-08 (Prep is quiet); nothing to decide in 4i-C.
- **The pause menu's "about"** is a Credits button, the same screen as the main menu's.
- **The bat wing icon stays** a stand-in (no Minifantasy source at icon size; new drawing is out of 4i-C's scope).
- **Selective polish** touched only the keeper's room: the menu backdrop and Kariaston's square already read as finished in captures.
