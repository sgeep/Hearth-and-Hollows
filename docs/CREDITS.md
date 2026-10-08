# Credits

The list of credits the shipped game must show, kept up to date as assets are added. License details are in `docs/THIRD_PARTY.md`. The game shows them on its Credits screen (4i-C; the **Credits** string table, authored in `CreditsLocKeys`); `CreditsTests` checks every name below is on it.

## Required by license

- **Art:** Minifantasy by **Krishna Palacio**. Required credit. On completion, send Krishna Palacio a link to the game. Packs in use (4f Checkpoint B adds the furniture catalogue's; Checkpoint C adds Sacrifice Altars, Piles of Loot and Stuff, Desolate Desert (Giant Bones, for the Larder Troll's tusks), Farm and the farm icons; 4h Checkpoint A adds Forgotten Plains, Plants & Foliage, Towns II's building samples, Caravans and Wagons, Town Monuments, Animated Well and the Travelling Merchant's cart; Checkpoint B adds Farm's crops and action icons and the More Veggies add-on; Checkpoint C adds the Knight Jousting add-on (the Knight on foot), the Miner, Snowball Wars Revamped (a child) and more of A Myriad of NPCs' layers, and portraits from the Portrait Generator): Creatures, Dungeon, Towns, Towns II, Tavern Indoor, Shop Indoor, Plant Pots, Church, Medieval City, Castles and Strongholds, Dwarven Kingdom, Elven Kingdom, Haunted House, Lunar New Year Festival, Astronomical Observatory, Wizard Tower, Chests, Ships and Docks, Crafting and Professions I and II, A Myriad of NPCs, UI Overhaul, User Interface, Adventurer's Campsite, Giant Spider, Ancient Troll, Gladiator Arena, icon packs.
- **Font:** Silver by **Poppy Works** (Wolfgang Wozniak), with major contributions from Itou Hiro (PixelMplus), leedheo (DOSGothic) and ぶち. Licensed CC BY 4.0, which requires attribution to Poppy Works and an indication of changes; suggested credit line: "Silver font by Poppy Works (poppyworks.itch.io/silver), CC BY 4.0; punctuation adapted for Hearth & Hollows". Poppy Works also asks to be told by email when a game uses the font (courtesy, not a license condition).
- Minifantasy Portrait Generator (graphical assets by Krishna Palacio; app by Pixel_Pincher): the dialogue portraits are composed from its layers (4g). Its commercial license allows use and editing in a game; it falls under the Krishna Palacio credit above.

- **Music:** by **HeatleyBros** (*HeatleyBros V*: "Quirkii", "Continue", "Coastal Market", "Otherworld"; added 2026-10-07), under the **HeatleyBros Attribution License** (https://heatleybros.com/index.php/heatleybros-attribution-license/): games are allowed, commercial use included, if the game credits them **in-game** (credits, a menu or end credits) **with a working link**, song-specific or to the channel; non-exclusive and revocable; the raw tracks may never be redistributed on their own; no use for AI training or generation. **In-game since 4i-C:** the Credits screen (main menu and pause menu) carries the credit and a working link to their channel, https://www.youtube.com/c/heatleybros (`CreditsLocKeys.HeatleyBrosUrl`; opened with `Application.OpenURL`, also in the web build). The WAV sources stay outside the repo in `C:\Dev\Music\Hearthdelve`; only the four used are imported (`Assets/_Project/Audio/Music`).

## Tools and middleware (courtesy credits)

- TopDown Engine, MMFeedbacks and Nice Vibrations by More Mountains.
- Super Text Mesh by Kai Clavier.
- Dialogue System for Unity, Quest Machine and Love/Hate by Pixel Crushers (imported in 4g; Unity Asset Store EULA, which requires no credit; credited by courtesy).
- Made with Unity.

## To add as they come in

- Sound effects (none yet; all sound effects are placeholders). Music: HeatleyBros, above.
- A decorative title font, if one is chosen in 4h.
