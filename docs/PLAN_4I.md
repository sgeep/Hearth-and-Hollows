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

## As built: 4i-C, "Presentation and polish" (2026-10-08; waiting for the owner's listening and look playtest)

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
| Dodge roll | yes | — | the roll | complete (no rumble by design since 4b: frequent, it would numb the hits) |
| Hurt | yes | yes | shake, flash | complete |
| An enemy telegraphs; dies (clean or overkill) | yes | yes | flash, mark, death | complete |
| Pick up an ingredient, gold, a curio, a power, a quest object | yes | yes | feed, HUD | complete |
| Satchel full; rooms seal and clear; fall; climb out; campfire; low Essence | yes | yes | prompts, gates, the fade | complete |
| The Larder Troll: entrance, slam, stun, spoil, frenzy, gulp, defeat | yes | yes (not the gulp) | shake, flash | complete |
| Menus: buttons, Options steps, the credits' link | yes | commit and buy | selection | complete |

The gaps are filled after the owner approves the sounds (they need the new sounds and, for footsteps, doors and blips, small new hooks through the generators). The dodge row was first marked a gap, then corrected: its missing rumble is a 4b decision.

### Changes from the plan (so far)

- **The delve's controls line is retired in the day loop**, not moved: onboarding and the controls page cover it, and it named both devices.
- **"Decide Prep's music"** was settled by the owner on 2026-10-08 (Prep is quiet); nothing to decide in 4i-C.
- **The pause menu's "about"** is a Credits button, the same screen as the main menu's.
- **The bat wing icon stays** a stand-in (no Minifantasy source at icon size; new drawing is out of 4i-C's scope).
- **Selective polish** touched only the keeper's room: the menu backdrop and Kariaston's square already read as finished in captures.

### Sound (after the owner's listening, 2026-10-08)

**The owner's choice:** every recommendation from the listening list, except family 15 (stairs, the hatch, the rope), which is option B (Kenney's plank impacts).

**Licences.** All four libraries are CC0 1.0, confirmed on the publishers' pages before import. Notes are kept in `C:\Dev\Music\SFX\_catalog\licences`, and the sounds are recorded in `docs/CREDITS.md` (a courtesy credit, also on the Credits screen) and `docs/THIRD_PARTY.md`.

**Import.** 137 files in `Assets/_Project/Audio/SFX/Library/{Kenney,OwlishMedia,Derived}`, mono, Vorbis. Short effects decompress on load; the pour loop stays compressed in memory. A test checks that nothing beyond the approved list is imported. Eighteen files are edits of approved files, made as copies by `Tools/audio/derive.py`:
- the tap's 1.6-second seamless pour loop;
- a half-second watering burst;
- sixteen of OwlishMedia's impacts (fruit, scrapes, crockery), peak-normalised to −1 dBFS. As recorded they peak near −17 dBFS, some 10 dB under the Kenney packs, too quiet to reach their level at full volume.

**The swap.** `SoundBank` (Editor) holds the families. Each placeholder maps to its family, or a feedback's name does where one placeholder served two moments: the interface tick as Vigor spent, the chime as a harvest. The generators ask it when they build (`LookTestContent.Feedback`, `TavernFeedbackContent`, `UiFeedbackContent`, `StoryScene`). `SoundSwap` (*Hearthdelve → Generate → Swap In the Approved Sounds*) applies it in place; it's idempotent, and a second run changes nothing. Repeated sounds play random variations, never the same footstep twice running, with a little pitch spread.

**The feedback audit's gaps, filled:**
- **Footsteps** (`Footsteps`, on both keeper prefabs) fall on the walk's two footfalls. Kariaston's paths, Tally Ho!'s boards and the Hollows' stone each have their own set and level.
- **Doors and stairs** (`PassageSounds`, beside the tavern's feedback): a door closing for the front door, a plank for the stairs, played as the screen covers. The passage event now says which it was.
- **Dialogue blips** (`DialogueBlips`, on the dialogue box): every third letter as a line is written, never closer than 50 ms. Each speaker has their own pitch (`CharacterDefinition.voicePitch`): Orik 0.85, Grim 0.82, Boog 1.18, Ogrin 1.25, Maximo 0.95, Kaloren 0.78, Bart 0.9, Gimp 1.05; everyone else 1. Silent for a look and the narration.
- **Other gaps:** the conversation opening is heard; the cleaver's swing (`WeaponUsedMMFeedback`, hit or miss); watering a bed (a new `Water` moment; planting is soil); market purchases (the buy moment). The dodge keeps no rumble: the audit flagged it, but 4b's feedback pass left it out on purpose (it's frequent and would numb the hits), and that stands.

### The sounds by family

All CC0; credit isn't required (credited by courtesy). K is Kenney (RPG Audio, Impact Sounds, Interface Sounds), O is OwlishMedia's Sound Effects Pack, and D is an edit by `Tools/audio/derive.py` of the O file named.

| # | Family | Plays | Files | Source |
|---|---|---|---|---|
| 1 | Footsteps, Kariaston | the keeper outdoors | `footstep00–09` | K RPG Audio |
| 2 | Footsteps, Tally Ho! | the keeper indoors | `footstep_wood_000–004` | K Impact |
| 3 | Footsteps, the Hollows | the keeper in the delve | `footstep_concrete_000–004` | K Impact |
| 4 | Dodge | the roll | `cloth1–4` | K RPG Audio |
| 5 | Swing | every cleaver swing | `drawKnife1–3` | K RPG Audio |
| 6 | Hit | light hits | `impactPunch_medium_000–004` | K Impact |
| 7 | Heavy hit | heavy hits, the finisher | `impactPunch_heavy_000–004` | K Impact |
| 8 | Hurt | the keeper hit | `impactSoft_heavy_000–004` | K Impact |
| 9 | Telegraph | enemies winding up | `impactBell_heavy_004` | K Impact |
| 10 | Enemy death | clean kills, overkills | `fruit1–3` | D (O Impacts, normalised) |
| 11 | Pickups | ingredients | `handleSmallLeather`, `…2` | K RPG Audio |
| 12 | Satchel full | a pickup refused | `error_004`, `error_008`, `error_001` | K Interface |
| 13 | Coins | gold picked up, paid, sold | `handleCoins`, `…2` | K RPG Audio |
| 14 | Doors | Tally Ho!'s front door | `doorClose_1–4` | K RPG Audio |
| 15 | Stairs, hatch, rope | the stairs, climbing out | `impactPlank_medium_000–004` | K Impact (**option B**) |
| 16 | Room gates | slam / rise | `impactMining_000–004` / `scrape1–2` | K Impact / D (O, normalised) |
| 17 | Grill | flips | `flip` | O Impacts |
| 18 | Tap | the pour loop / the clink and line | `TapPourLoop` / `impactGlass_light_000–004` | D (O Water) / K Impact |
| 19 | Chopping and butchery | the board / the knife / butchery | `impactWood_light_*` / `chop`, `knifeSlice`, `…2` / `impactWood_medium_*` | K Impact / K RPG Audio / K Impact |
| 20 | Plates | pick up, set down, serve / a crash | `impactPlate_light_*` / `clamour1–11` | K Impact / D (O, normalised) |
| 21 | Stew pot | the stew's ready | `metalPot1–3` | K RPG Audio |
| 22 | Interface confirm, tick | buttons, the conversation opening / ticks | `select_001`, `002`, `007` / `tick_002` | K Interface |
| 23 | Buy, chime, discovery | upgrades and purchases / the takings / curios, powers, homecomings | `confirmation_001` / `confirmation_002`, `004` / `maximize_004–006` | K Interface |
| 24 | Back, no | an invalid placement / a walkout | `back_001–004` / `error_005` | K Interface |
| 25 | Decorating | lift / place / store / turn / undo | `cloth3` / `impactWood_medium_*` / `bookPlace1–2` / `switch_002` / `minimize_007` | K RPG Audio, Impact, Interface |
| 26 | Garden | plant / tend / harvest | `impactSoft_medium_*` / `GardenWater` / `fruit1–2` | K Impact / D (O Water) / D (O, normalised) |
| 27 | Dialogue blips | lines being written | `pluck_001–002` | K Interface |
| 28 | The Larder Troll | slam, stun, fall / gulp, spoil | `impactMining_*` / `gulp1–2` | K Impact / O Impacts |

### Placeholders still left

Twelve sounds have no approved replacement.
- **Gaps no library fills:** the troll's roar (`PH_TrollRoar`), the grill's sizzle (`PH_SizzleLoop`), the campfire (`PH_Campfire`, `PH_CampfireLow`), low Essence's heartbeat (`PH_Heartbeat`), and the air swoosh of falling into a hole and of Decorate's area change (`PH_Whoosh`).
- **Not in the first-hour list** (candidates exist in the libraries; a short second listening round would cover them): Decorate's finish and restyle brush (`PH_Brush`), plate bumps (`PH_Bump`), the grill's burn (`PH_Burn`), the charge-up tick (`PH_ChargeTick`), the tap's spill warning (`PH_SpillWarn`) and overflow (`PH_Splash`).

`SoundTests` checks that exactly these twelve are left.

### The audio balance

Measured, not guessed (*Hearthdelve → Report → Sound Levels*, `SoundLevels`). Each family plays at its kind's target loudness (RMS of its sounding part), whatever its library's mastering; a quiet clip plays at full:

| Kind | Target |
|---|---|
| Impacts | −16 dBFS |
| Actions | −20 dBFS |
| Interface and blips | −24 dBFS |
| Footsteps | −28 dBFS |

Three families keep a deliberate trim: the swing (0.7: it plays on every attack), doors (0.8) and stairs (0.7).

**Music** (the HeatleyBros WAVs measured: Continue −16.5, Coastal Market −13.0, Quirkii −12.3, Otherworld −10.2 dBFS RMS). The tracks are evened to about −19.5 dBFS as played:
- Coastal Market 0.85 and Otherworld 0.6 (it was about 4 dB above the rest, and above the Hollows' hits under their quarter-down);
- Quirkii keeps the owner's 0.75; Continue stays at 1 (already quieter).

**Ducking:** the music steps down 3 dB (`MusicConfig.duckUnderDialogue` 0.7, over 0.4 s) while a conversation is open, so the line and its blips come forward. Set it to 1 to turn it off.

**Mixer groups:** left at 0 dB under the player's sliders, and the Hollows' level is unchanged (the owner's 0.75).

The feedback audit's **gap** rows above are now filled, except where the placeholders above remain.

### Sound, round 2: Leohpaz (2026-10-09)

The owner bought Leohpaz packs to replace the 4i-C sounds they disliked and to fill the placeholders left. The workflow was the same: catalog, a listening list (`_catalog\LISTENING_4iC2.md`, two or three candidates per family against the current sound), the owner's picks ("your picks"), then import and swap.

**The packs.** Nine packs, each in its own folder in `C:\Dev\Music\Leohpaz SFX` beside the zip it came from. `_catalog\verify_leohpaz.py` found every folder identical to its zip. `catalog.py` reads that folder as a second root and takes pack membership from the folders (the zips aren't counted), and `SoundBank` has one more path helper (`L()`).

**Licence.** Commercial use is allowed; the pack may not be sold or redistributed; credit is optional, so Leohpaz is credited by courtesy (the Credits screen, `CREDITS.md`, `THIRD_PARTY.md`).
- Five packs carry it in `Licensing.txt`: Farm, Inventory, Humanoids Grunts, Retro Dialogue, Crafting and Professions II.
- The other four zips hold only sounds, and their licence is on their itch.io pages, read 2026-10-09: Retro RPG 90 Battle, Retro Player 90 Movement, Minifantasy Dungeon Audio, Forgotten Plains Audio.
- All of it is recorded in `_catalog\licences\Leohpaz.txt`.

**A correction.** While four packs were briefly loose and mixed at the top of the folder, I reported Forgotten Plains' "campfire loop" as missing. It never existed in that pack: its 37 sounds are rustling, fruit drops, footsteps, landings, water, waterfall, ambience loops and hits on wood and brick. The campfire loops on its store page are Patreon exclusives. The claim came from a premise in the brief, which I reported as a fact about the files without checking the pack's contents; it's removed everywhere. I also guessed which loose files belonged to which pack from their numbering. The folders show that guess was wrong (ATB is in RPG Battle; Attack, Hit and Dash evade are in Player Movement), but nothing had been imported from those files, so nothing in the game changed.

**What round 2 changed** (75 Leohpaz files, plus three edits by `Tools/audio/derive.py`):

| # | Family | Now | Was | Pack |
|---|---|---|---|---|
| 1 | Footsteps, Kariaston | `Step_dirt_1–3` | Kenney `footstep00–09` | Minifantasy Farm |
| 2 | Footsteps, Tally Ho! | `10–12_Step_wood_01–03` | Kenney `footstep_wood_*` | Retro Player 90 Movement |
| 3 | Footsteps, the Hollows | `Step_stone_1–3` | Kenney `footstep_concrete_*` | Minifantasy Farm |
| 4 | Dodge | `65–67_Dash_evade_01–03` | Kenney `cloth1–4` | Retro Player 90 Movement |
| 5 | Swing | `27_sword_miss_1–3` | Kenney `drawKnife1–3` | Minifantasy Dungeon Audio |
| 6 | Hit | `14–18_Impact_flesh_01–05` | Kenney `impactPunch_medium_*` | Retro RPG 90 Battle |
| 7 | Heavy hit, finisher | `09–13_Impact_01–05` | Kenney `impactPunch_heavy_*` | Retro RPG 90 Battle |
| 8 | Hurt | `11_human_damage_1–3` | Kenney `impactSoft_heavy_*` | Minifantasy Dungeon Audio |
| 10 | Enemy death | `69_Enemy_death_01`, `70_…_02`, `72_…_04` | OwlishMedia fruit (normalised) | Retro RPG 90 Battle |
| 11 | Pickups | `Item_Pick` | Kenney `handleSmallLeather*` | Inventory |
| 12 | Satchel full | `Bag_Full` | Kenney `error_*` | Inventory |
| 13 | Coins | `Coins`, with Kenney's `handleCoins`, `…2` | Kenney only | Inventory |
| 15 | The rope (climbing out) | `40–42_Cling_climb_01–03` | Kenney planks (the stairs keep them) | Retro Player 90 Movement |
| 16 | Gates: slam / rise | `16_Hit_on_brick_1–2` / `19_Slide_01` | Kenney mining / OwlishMedia scrapes | Forgotten Plains Audio / Retro Player 90 Movement |
| 17 | Grill flip | `EggFlip` (the first flip of `Flipping_Eggs_single`, 0.6 s) | OwlishMedia `flip` | Crafting and Professions II (an edit) |
| — | Grill sizzle (was `PH_SizzleLoop`) | `Loop_with_eggs` (loop) | placeholder | Crafting and Professions II |
| 19 | Chopping / butchery done | `Food_Preparation_Cut_1–2` / `Carving_Butcher` | Kenney wood impacts | Crafting and Professions II |
| 26 | Garden: plant, tend, harvest | `Seeds_1–4`, `Watering_1–2`, `Harvest_1–2` | Kenney soil / water edit / fruit | Minifantasy Farm |
| 27 | Dialogue blips | `Triangular_High`, `Triangular_Low` | Kenney `pluck_*` | Retro Dialogue (only the allowed synth blips; tested) |
| 28 | The troll's voice | `Troll_Attack_1–5` on his slam, `Troll_Damage_1–5` on his stun, `Troll_Death_1–2` on his fall, each a second sound over his impact | — | Humanoids Grunts |
| — | The troll's roar (was `PH_TrollRoar`) | `Troll_Wind_up_1–3` | placeholder | Humanoids Grunts |
| — | The fall into a hole (was `PH_Whoosh`) | `Fall1`, `Fall2` (the first half second of `43/44_Falling_Loop`) | placeholder | Retro Player 90 Movement (an edit) |
| — | Decorate's area change (was `PH_Whoosh`) | `Drop_Whoosh` | placeholder | Inventory |
| — | The charge ticks (was `PH_ChargeTick`) | `65_ATB_1` (level 2), `66_ATB_2` (level 3) | placeholder | Retro RPG 90 Battle |
| — | The tap's overflow (was `PH_Splash`) | `13–15_Step_water_01–03` | placeholder | Retro Player 90 Movement |

The telegraph, doors, the stairs, the tap's pour and clink, plates, the crash, the stew, the knife, Decorate's other sounds and every interface sound stay Kenney and OwlishMedia, as the picks said.

**Ambience for later** (not 4i-C): Forgotten Plains' crickets, cicada, birds, wind and water loops are noted in the catalog as candidates for village and night ambience.

**The two fixes** (approved with the picks, confirmed 2026-10-09):
- **Blips:**
  - in the quiet tier (−28 dBFS);
  - no random pitch per blip (round 1's ±5% was close to a semitone, the likely "out of tune");
  - one blip every 0.079 s at most, Leohpaz's advice;
  - one shared waveform (Triangular), its Low file for the deep voices (`CharacterDefinition.lowVoice`: Orik, Grim, Kaloren);
  - every voice a whole number of semitones (`SoundSwap.Voices`): Orik −3, Grim −5, Boog +3, Ogrin +4, Maximo −1, Kaloren −4, Bart −2, Gimp +1.
- **Footsteps:** their own tier at −34 dBFS, 6 dB under the blips (`SoundBank.TargetDb`).

**Waiting for the owner** (`_catalog\LISTENING_4iC3.md`): the campfire (no fire sound in any library; two stand-ins offered) and grass footsteps beside dirt for Kariaston's paths. Forgotten Plains' walking sounds turned out to be byte-identical to the Farm pack's.

**Placeholders still left (7):**
- the campfire and its low state, waiting for the owner's choice;
- no source found: low Essence's heartbeat, Decorate's finish and restyle brush, plate bumps, the grill's burn (Crafting II's "Fail" is a musical cue), and the tap's spill warning.

### Closing 4i-C (2026-10-09)

- **Menus on a controller** (the owner's note: at night, with only "sleep" and "decorate", the stick and d-pad did nothing). uGUI moves the selection only from something already selected, and about 16 screens set a selection when they open but never restore it, so a mouse click on empty space or a screen opening with the focus elsewhere left nothing to move from. `MenuFocus` (UI; made at start-up, in no scene) covers them all. When only the menus' controls are live and the stick, the d-pad or the arrows are used with nothing usable selected, it selects the topmost menu's first usable control. It runs after every screen's own logic, so the keeper creator's keyboard and the dialogue box keep their own focus. It never acts on Submit, the mouse, a conversation or play. (Its first version also reacted to Submit; the full run caught that breaking conversations, which advance on Submit with nothing selected on purpose.) A PlayMode test reproduces the night and checks it never acts on foot.
- **Why the village tests timed out late in a full run** (investigated, not only given more time). Two causes:
  - **Seed luck.** These tests walk in-game days until a seeded day comes round (Glimmer's light, Ogrin in bed, Gimp's visit), and a new game's world seed is random, so they walked one to four days. Tests can now pin a new game's seed (`GameFlow.NewGameSeedOverride`, unset in play), chosen through the same pure day rules, so the day they need comes at once.
  - **A real bug: game time was capped at 0.03 s a frame.** A diagnostic logged at the start of each village test showed `Time.maximumDeltaTime` at 0.03 instead of the project's 0.33. TDE's `MMTimeManager` scales the cap and the physics step with the time scale, from the values it finds when it starts. Each scene's manager starts afresh, so one starting during another's hit-stop or pause took the reduced values as normal, and they compounded across scene loads. With the cap at 0.03 s, any frame slower than 30 ms runs the whole game in slow motion, in play on a slow machine or during a web hitch. The physics step drifted the same way. `TimeBaseline` (Shared; made at start-up, vendor code untouched) restores the project's own steps whenever time runs at its normal scale. A hit-stop or a pause still scales them while it lasts. `TimeBaselineTests` covers the rule.
  - **Measured in the final full run:** the guard undid 29 drifted steps, frames late in the run took 0.4 ms (so it wasn't slow frames under the cap, as I first thought), and the late village tests ran at their solo speed. The slowest PlayMode test is now 51 s, down from 176–184 s, and nothing comes near a limit. The cap and the physics step weren't measured apart, so which drift did the damage isn't separated. The 10-minute limits stay only as a guard.
- **Credits clipping** (found in the web smoke test): Super Text Mesh draws its own meshes, which the credits' `RectMask2D` doesn't clip, so entries scrolled out over the title and the footer. Each entry now shows only while it's wholly inside the window; the credits test checks that no shown entry is outside it.
- **Mixer balance:** groups stay at 0 dB under the player's sliders. Every family is levelled to its kind (`Hearthdelve → Report → Sound Levels`), and the music keeps its per-track levels and dialogue duck.
- **Web smoke test** (development build 0.4i-c, the owner's browser storage backed up first and restored exactly):
  - the menu, with Options, Controls and Credits, and the version shown;
  - Credits scroll by keys, and Enter on HeatleyBros' link opened youtube.com/c/heatleybros in a new tab;
  - Continue loaded the save; the pause menu has Credits;
  - sound reaches the speakers: the page's audio context is running, and a tap on its output caught the interface confirm (peak 0.0055 over about 60 ms, silence either side);
  - memory: the game's Chrome renderer held about 640 MB, with a peak working set of about 1.1 GB across three reloads in the same process;
  - not driven: walking, fights and service (the browser driver's key presses are taps, not held keys).

### After the owner's 4i-C playtest (2026-10-09)

- **Music:** what the music slider at 25% gave is the new 100% (`MusicConfig.volume` 0.5625 → 0.140625); the slider scales down from there. An options file from before (version 1) has its music slider multiplied by four once (25% then is 100% now, up to the top), so a player who had turned it down hears the same; `PlayerOptions` is version 2.
- **Sound effects a quarter down everywhere** (`MusicConfig.effects` 0.75, on the mixer's Effects volume under the player's slider); the Hollows' own quarter-down stays on top (0.5625 there). Footsteps stay 6 dB under the blips (both are effects).
- **Decorate Mode keeps its generated placeholders** (the owner liked them better): every Decorate moment, lifting, placing, turning, storing, undoing, the invalid buzz, buying, selling, restyling, the area change and the trophy's homecoming, plays the sound it was built with (`SoundBank.DecoratePlaceholders`, `SoundSwap.KeepDecoratePlaceholders`, also run by the builder). The five Kenney furniture sounds and Leohpaz's area whoosh are no longer imported.
- **The delve's first room is safe ground:** Essence doesn't drain there; the drain begins when the keeper goes through its doors and the next room loads (`FloorNode.PausesEssenceDrain` now includes the run's one `Start` room; the runner applies it as the delve opens). Later floors begin in a fight and drain as before.
- **Sound picks:** enemy death 69 only; dodge 65 only; the upgrade pickup Kenney `maximize_006` only (its own family, `PowerUp`; discoveries keep 4–6); a full satchel Retro Dialogue's `Window\Window_Close_2` (the owner's own exception to the Retro Dialogue rule; the blips stay the four synth waveforms, and a test allows nothing else from the pack); the campfire OwlishMedia's crumpled paper (A), burning low the same, half as loud; Kariaston's footsteps dirt and grass mixed (B, Farm's `Step_grass_1–3`, the same files as Forgotten Plains').
- **Found while doing it: round 2's footsteps had never reached the keeper.** The keeper's `Footsteps` still listed round 1's Kenney steps, which round 2's import had removed, so every step was silent; the tests counted steps, not sounds. The sound pass now keeps the steps' clips in step with the bank, a step with no clip isn't counted, and two tests guard it: the keeper's steps are the bank's, and no sound in any prefab, scene or asset points at a missing file (`SoundTests.NoSound_PointsAtAMissingFile`).
- **Orik's cap (`Pip_OnServing…`, 0.861 against 0.85):** not a broken rule but a test measuring the wrong thing. The cap is on staff's own work (`StaffDefinition.qualityCap`: Orik's serve, Boog's and Orik's cooking, each `Mathf.Min`'d before it's used). The test compared the *dish's* quality with it, and a dish is the keeper's cooking × the ingredients × a serving factor of 0.6 + 0.4 × the serve. With the keeper's perfect cook and Orik serving at his cap, the factor is 0.94, so the dish comes out about 0.86–0.94 with these ingredients: correctly above 0.85, and still below the keeper's own perfect serve of it. Whether it failed depended on how cleanly Orik walked that run. The test now checks Orik's serve against his cap (from `StaffWorkDone`), that the dish is exactly the keeper's cooking with that serve, and that it's worse than the keeper's own perfect serve.
- **`CameraFollowTests`** remembers the frame rate and VSync before each test and restores them after (they turned VSync off for every later test in the run).

### After the second 4i-C playtest (2026-10-09)

- **Footsteps a third down** on Kariaston's paths and the Hollows' stone (a 0.67 trim on those families; Tally Ho!'s boards unchanged); **the dodge a quarter down** (0.75 trim). Both through the bank and the sound pass.
- **Quirkii 15% down** (its own level 0.75 → 0.6375).
- **Slimes hurt after dying** (any enemy could): an enemy killed during its telegraph still reached its attack's active phase, because the attack's timer kept running, and TDE leaves a child hitbox's collider on at death. Dying now ends the attack at once (`EnemyAttack` listens for `Health.OnDeath`; a dead enemy's attack never starts its active phase). Test: `ASlimeKilledMidTelegraph_NeverHurtsWhileItDies`. A web already in flight still flies.
- **The harvest note bled into its frame:** its text box was 138 pixels, and "3 fine onion into the storeroom" needs about 150, so it wrapped onto a second line. The typography test counts a `{0}` as one digit, so the crop's name was never measured. The note is 200 wide now, and a test measures every crop's real line (`TheHarvestNote_FitsEveryCropsLine_OnOneLine`).
- **The kills, the owner's picks:** a clean kill is Leohpaz's "77_flesh_02" alone (no layered tone), trimmed to its real length by `derive.py` (0.67 s → 0.5 s, where its tail falls under −60 dBFS; `CleanKill.wav`); an overkill is Kenney's "impactPunch_heavy_000". Two families, `CleanKill` and `Overkill` (`PH_KillClean` and `PH_Thud`, and the two feedbacks by name), each one file with a ±5% pitch spread, levelled as impacts. The shared `EnemyDeath` family and its Leohpaz file are gone (nothing else used them; the Larder Troll has his own). A plain kill still plays only its hit.
- **Orik running to customers empty-handed:** when a guest paid and left, he remembered their table to tidy and went as soon as no plate was waiting on the pass. Meanwhile a new customer had often sat at that table and ordered, so the moment the keeper picked up the plate, he set off for the new customer's table with nothing. He now tidies only while no plate is coming (nothing ordered, cooking or on the pass; `ServiceSession.PlatesComing`), and forgets a table someone has sat at again (`SeatTaken`). Otherwise he waits by the pass for the next plate.

### Listening and look checklist (4i-C)

1. **Headphones, then speakers:** a full day. Wake in your room (does it feel lived in?), walk the boards, go downstairs (stairs), out the front door (the door), along Kariaston's paths (footsteps change with the ground), plant, tend (water) and harvest, buy at the market.
2. Talk to Orik, Boog and Musashi: the box opening, the blips at each speaker's pitch (Orik deeper, Boog higher), the music stepping back while they talk and returning after.
3. **An evening:** butchery, chopping, the grill's flips, the tap's pour, plates, the stew, coins, a walkout if one happens, the takings. Does anything stand out as too loud or too quiet?
4. **A fight in the Hollows:** footsteps on stone, swings and hits, a telegraph, an enemy dying, pickups, gates, the troll if you reach him. Do the music and the hits sit well together under the Hollows' quarter-down?
5. Decorate: lift, place, turn, store, undo, an invalid spot.
6. Note any sound that still feels like a placeholder (the twelve left are listed above), and any you'd swap.
7. **The Credits** from the main menu and the pause menu: does it read well? In the **web build**, click HeatleyBros' link: does their channel open in a new tab?
8. The look: the fades between places (all the same now), the death screen with an empty satchel, the delve without the controls line.
9. **Round 2's sounds:** footsteps on all three grounds (quieter now), the dodge, swings, hits, heavy hits and hurt, enemy deaths, the gates, the rope, the fall into a hole, the charge ticks, the troll's voice and roar, the grill's flip and sizzle, chopping, the garden, the tap's overflow. Do the blips (Triangular, low register for Orik, Grim and Kaloren) sound in tune and quiet enough?
10. **On a controller** (carried from 4i-A and 4i-B): the menus, pause and controls pages, and prompts switching without flicker; Options with the d-pad, LB/RB and B; rumble at 10%, 50% and 100% strength, then reduced vibration (each sample distinctly weaker, a hit in the Hollows matching).
11. **Menus on a controller, the fix:** at night with "sleep" and "decorate", click empty space with the mouse, then push the stick or d-pad: the first push lands on the top choice, the next moves. Check the same in a conversation's choices and the keeper creator (they keep their own focus).
12. **Game speed on a hitch:** in the web build, after several delves and scene changes, does anything ever feel in slow motion? (`TimeBaseline` should have ended that.)
13. **The credits' edges:** entries now appear and leave the window whole instead of sliding under its edge. Does the pop read fine, or should they fade at the edges?

After the playtest's changes (2026-10-09):

14. **Walk everywhere:** footsteps should now be heard at all (they were silent since round 2), on the boards, on Kariaston's paths (dirt and grass mixed) and on the Hollows' stone. Too loud or quiet under the new effects level?
15. **The levels:** music at 100% (where your 25% was; your saved options were moved to 100% to match), effects a quarter down. The door in particular.
16. **Decorate Mode** with its old placeholders, every moment.
17. **The delve's first room:** Essence holds still until you go through its doors.
18. **The picks:** an enemy dying (69), the dodge (65), an upgrade pickup (6), a full satchel (Window_Close_2), the campfire as you warm by it and as it burns low (the crumpled paper).
19. **Second playtest's changes:** footsteps outdoors and in the Hollows a third quieter, the dodge a quarter quieter, Quirkii 15% quieter; slimes never hurting while they die; the harvest note on one line inside its frame; Orik staying by the pass while food is coming, and never walking to a customer without a plate; a clean kill (the flesh) and an overkill (the heavy punch), each a little different every time.
