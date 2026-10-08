# 4i plan: a playtest-ready vertical slice

> **Status: proposed 2026-10-08, waiting for the owner's review.** Nothing in this plan is built. 4h is complete (signed off 2026-10-08, tag `milestone-4h`). This plan replaces the old one-line 4i entry ("settings, accessibility per GDD §12, audio system, web build"), keeps its intent and widens it to the owner's objective below.

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

## 3. Decisions for the owner before 4i-A

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

## 4. Phase 5 roadmap proposal (for discussion; nothing locked)

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
