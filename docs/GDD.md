# HEARTH & HOLLOWS — Project Design Document

*Working title: **Hearth & Hollows** (renamed from Hearthdelve on 2026-10-05). Version 0.5, with 4f as built recorded 2026-10-06 (Section 11.1, Decided 28–34). The Kariaston cast added 2026-10-07 (Section 2.10, Decided 51). Version 0.5 (title and terminology pass, 2026-10-05: Hearth & Hollows, the Hollows, delves; village life-sim direction: daily loop, Kariaston's villagers and Visitors, the Inn, farming, ranching and fishing, the surface and dungeon ingredient model; Stronghold and defense direction dropped, 2026-10-04; learning priorities, tavern customization, decor rewards, worker customization, 2026-10-04; design philosophy, tavern immersion, quests and relationships in v0.3, 2026-10-03; top-down pivot in v0.2, 2026-10-02). Engine: Unity 6.6, moving to 6.7 LTS on release.*

> **About this version.** Version 0.1 described a side-scrolling game in the style of Dead Cells. On 2026-10-02 the game pivoted to top-down. Sections rewritten for the pivot are marked **(rewritten in v0.2)**; the text they replace is kept in [Appendix B](#appendix-b-superseded-v01-side-scroller-design) rather than deleted. Sections and lines added in v0.3 are marked **(added in v0.3)** or *(v0.3)*, those added in v0.4 **(added in v0.4)** or *(v0.4)*, and those added or rewritten in v0.5 **(added in v0.5)**, **(rewritten in v0.5)** or *(v0.5)*; they record design direction and do not widen any milestone's approved scope (the roadmap in Section 11.1 says what each milestone builds). Sections without a mark are unchanged from v0.1. Version 0.5 moves the game's identity toward a fantasy life sim centred on the tavern; the Stronghold direction and the old day order it replaces are kept in [Appendix C](#appendix-c-superseded-v04-stronghold-direction-and-day-order). Where this document and `CLAUDE.md` disagree, `CLAUDE.md` wins.
>
> **Names (2026-10-05).** The game's working title is **Hearth & Hollows** (it was Hearthdelve) and the underground world is **the Hollows** (it was "the Dungeons"). Current text uses the new names without per-line marks; appendices and historical notes keep the old ones. "Dungeon" remains the technical and genre term: the dungeon gameplay layer, its code, scenes and assets, and the repository, project and code name Hearthdelve, are unchanged (Section 14).

---

## 1. Overview

### 1.1 Elevator Pitch (rewritten in v0.5)

**Experience target:** *Live in a strange fantasy village, grow and gather ingredients, run and personalize your tavern and inn, build relationships with villagers and visitors, and descend into the Hollows at night for things the surface world cannot provide.*

You inherit Tally Ho!, a tavern and inn in the small frontier village of Kariaston, built over a way down into **the Hollows**, the ancient underworld beneath the village. Your days are your own: tend a garden plot and a few animals, fish, shop, decorate the tavern and its guest rooms, run errands and get to know the odd, warm people who live here (the kind of village where an elderly skeleton might keep bees and nobody finds that strange). In the evening you open the doors, cook and serve: some of the faces at the tables are your neighbours, others are Visitors passing through, and a few of those Visitors may take a room upstairs and, if you help them, settle into one of the village's empty plots. When the tavern closes, you delve into the Hollows, fighting room by room through top-down, hack-and-slash runs for monster parts, rare and magical ingredients, strange discoveries and things your neighbours asked you to find. Then you come home, sleep, and the next day begins.

Delving into the Hollows is still a major pillar, but it is one part of a broader daily life rather than the whole game with a tavern between runs. *(The v0.2 pitch, built around a home that grows into a fortified Stronghold, is in Appendix C.)*

**The title** *(added 2026-10-05)*. *Hearth & Hollows* names the game's two halves and the contrast between them: **warmth and community above; mystery and danger below.** The **Hearth** is home: the tavern and the Inn, cooking, the village and its people, relationships, farming, ranching and fishing, and a property the player makes their own. The **Hollows** are what lies beneath Kariaston: danger, monsters, mystery, rare ingredients, quests, strange discoveries, the rewards of the deep, and the ancient forces stirring below. The game is a life sim centred on the hearth, with the Hollows as one of its major pillars, not an action roguelite with a home attached.

### 1.2 Genre and Inspirations (rewritten in v0.5)

Hybrid: a top-down fantasy life sim centred on owning and running a tavern and inn, with a top-down action roguelite dungeon, the Hollows, beneath it.

| Inspiration | What we take from it |
|---|---|
| *Stardew Valley* *(v0.5)* | The overall shape: a free daytime in a small village whose residents you come to know, growing and gathering your own ingredients, a day that ends in sleep. Not a template: its mechanics (crops, seasons, the clock, gifting) are not copied automatically (Section 13) |
| *Cult of the Lamb* | Short top-down combat runs feeding a home that grows; residents with personalities. *(v0.5: no longer the overall shape; the home is a tavern, inn and village, not a cult compound or fortress)* |
| *Hades* | Combat feel: 8-direction movement, dodge with i-frames, light combo plus a heavy/charged attack; room-by-room runs where you pick the next room by its reward |
| *Moonlighter* | A shopkeeper who delves; what you carry out of the dungeon is what you sell. *(v0.5: the order is now tavern in the evening, the delve at night)* |
| *Dave the Diver* | Minigame-driven cooking and service, a limited "oxygen" resource (our Essence), charming NPC cast |
| *Delicious in Dungeon* | Monsters as food, the ecology and "cookability" of creatures, how you kill something affecting how it tastes |
| *Warcraft / Lord of the Rings* | Classic high-fantasy world: humans, dwarves, elves, orcs, ancient evils, kingdoms under threat |

### 1.3 Design Pillars

1. **Every kill is a harvest.** Combat is not only about survival; *how* you fight determines what you bring home.
2. **The surface and the depths, one life.** *(rewritten in v0.5)* Village life, the tavern and the Hollows feed each other. The surface provides dependable ingredients, people and a home; the Hollows provide what the surface can't: monster and magical ingredients, discoveries and stories. No part should feel like a detour from the "real" game.
3. **A home and a community that grow with you.** *(rewritten in v0.5)* Tally Ho! grows into a larger tavern, inn and home inside a village whose people know Bram, some of whom are there because of him. *(It no longer grows into a fortified Stronghold; Appendix C.)*
4. **Cozy on the surface, dread below.** The warmth of the village and the tavern, warm, funny and a little strange, contrasts with the menace of the depths.
5. **You can feel it.** *(added in v0.2)* Every important moment lands through visuals, sound and haptics together.
6. **Your tavern, your hands.** *(added in v0.3)* Tavern immersion: running Tally Ho! should feel physical and present. You walk the room, work the stations, carry the plates and watch strange monster parts become recognizable dishes. Immersion serves the fun and is never an excuse for busywork (Section 6.5).
7. **A home you made.** *(added in v0.4)* Tally Ho! increasingly becomes a place the player personally created: they choose how it looks and how it works, and fill it with things they bought, earned and dragged up from the Hollows. "This is my tavern. I chose how it looks, I earned the strange things inside it, and the room itself tells the story of what I've done" (Section 6.6).

### 1.4 Target Platform and Audience

- **Primary:** PC (Steam), plus a web build kept working throughout development. **Secondary:** Nintendo Switch 2, PlayStation 5, Xbox Series (post-launch consideration).
- **Input:** Controller-first design, full keyboard and mouse support.
- **Audience:** Players who enjoy cozy life sims, management games and action roguelites; fans of *Stardew Valley*, *Dave the Diver*, *Cult of the Lamb*, *Hades*, *Moonlighter*, *Potion Craft*.
- **Rating target:** Teen (fantasy violence, mild monster gore played for comedy).

### 1.5 Design Philosophy (added in v0.3)

The goal is not systems that work correctly; it is the experience the player has. A system can be bug-free and balanced on paper and still fail if it isn't fun, understandable or satisfying in play. The final test is always the experience in play.

**Questions for a non-trivial design decision**

- What experience is this supposed to create for the player, and which parts of it are essential?
- Does the mechanic actually create that experience in play?
- Is it fun, understandable and satisfying, rather than merely technically correct?
- Does it create curiosity, meaningful choices, challenge, mastery or surprise?
- Does it support good pacing and flow?
- Is the complexity producing interesting decisions, or merely more work?
- Does the player get clear and satisfying feedback?
- Does it reinforce Hearth & Hollows' theme, world and other systems?
- Can the idea be tested cheaply before we commit to a large build?

**Lenses as perspectives, not rules.** Jesse Schell's *The Art of Game Design: A Book of Lenses* is the project's recurring vocabulary for questioning a design. Schell presents each lens as a different way of looking at a design, and that is how we use them: pick the lenses that reveal something about the problem in front of us. No feature has to satisfy every lens, and a lens is never a box to tick. When an important design choice is proposed, it states the player experience it targets and, where useful, the lens or principle behind it; but the argument has to stand on its own, and "Schell says so" is not a reason. (The book is reference material only and is not kept in the repository.)

Where each lens most often matters in Hearth & Hollows:

| Lens | Where it tends to bite |
|---|---|
| Essential Experience | The start of every feature: the satisfying kill-and-harvest, the busy evening service, a rare dish carried out to the table, a neighbour you know walking in to eat |
| Fun | Whether a moment is enjoyable, not only correct: hits, minigames, serving |
| Curiosity | Rooms behind doors, unfamiliar monster parts, Test Kitchen experiments, Old Phi's disappearance |
| Problem Solving | Planning a delve around what the menu needs; routing service through a busy room |
| Elemental Tetrad | Checking that mechanics, story, aesthetics and technology pull the same way (a dish's preparation, its icon, its sound and its lore) |
| Flow | Minigame length and difficulty; service pacing; the rhythm of fights and choices in a delve |
| Challenge | Enemy telegraphs and fairness; timing windows; patience under load |
| Meaningful Choices | Room rewards, satchel space, the Lockbox, the menu, which part to cook and which to sell |
| Reward | Harvest quality, dish results, Renown, story beats, relationship moments |
| Simplicity/Complexity | Preferring emergent complexity (simple rules that interact) over innate complexity (more rules); see "Meaningful complexity" below |
| Elegance | One system serving several purposes, as Essence is both the delve timer and the health pool |
| Balance | The economy, the risk of going deeper, staff quality against the player's; tuning lives in ScriptableObjects so it can move |
| Visible Progress | The tavern and inn growing and changing; preparation depth rising with rarer dishes; familiar faces returning; empty plots becoming neighbours' homes |
| Feedback | Every action answering the player in visuals, sound and haptics (Section 9A) |
| Juiciness | Hits, flips, pours and plating that feel good simply to do |
| Interest Curve | The shape of a delve, an evening and an act, with peaks for bosses and signature dishes |
| Character / Character Web | Boog, Orik, Ser Aldric, Sylvaris, Grukka and the regulars, and how they relate to Bram and each other (Section 2.7) |
| World | Aldmere, Kariaston and the Hollows' ecology: why monsters are edible, who lives in the village, why Visitors come |
| Playtesting | The final judge (below) |
| Technology | Choosing tools that serve the experience, and not letting a tool's shape dictate the design |

**Playtesting over theory.** Hearth & Hollows keeps its prototype-and-playtest approach: every sub-milestone step is playtested before the next. A theoretically elegant design that feels bad in play is not a successful design. For uncertain or expensive ideas:

1. identify the desired experience;
2. identify the smallest version that can test it;
3. build only that much;
4. playtest it;
5. analyze specifically what felt good or bad, and why;
6. iterate.

Player feedback is interpreted, not blindly obeyed. What players actually experience and do matters more than the solution they suggest.

**Meaningful complexity over system count.** Hearth & Hollows is not judged by how many mechanics it has. It already combines action combat, roguelite runs, harvesting, inventory and freshness, cooking minigames, tavern service, economy and upgrades, quests, relationships and story, and the v0.5 direction adds village life, farming, ranching, fishing and the Inn, so a smaller number of systems that interact richly beats many isolated ones. Before adding a rule or subsystem, ask:

- Does it create new decisions?
- Does it interact with existing systems?
- Does it serve more than one purpose?
- Does it strengthen the essential experience?
- Could an existing system do the same thing more elegantly?

Nothing is added only because another RPG or management game has it.

**Immersion and convenience.** Tavern immersion is a pillar, but immersion is a preference, not permission to create tedium. When the two conflict, neither extreme wins automatically; the question is what experience the interaction actually produces. Section 6.5 has the details.

**Learning priorities** *(added in v0.4)*. Hearth & Hollows is meant to become a finished, coherent game, and it is also a deliberate way for its designer to practise the parts of game development they most enjoy:

1. interactive dialogue writing;
2. building and tavern customization;
3. cooking minigames;
4. funny, strange, memorable NPC interactions;
5. persistent NPC relationships and reactivity;
6. top-down action combat.

*(v0.5)* The village life-sim direction strengthens all six: daytime gives dialogue, relationships and funny NPC moments a place to live outside service, the Inn extends customization, farming and fishing feed the kitchen, and the Hollows keep combat purposeful by sending the player down for things people actually want.

This shapes where complexity and content budget go. In these areas the goal is **not always the smallest number of systems or pieces of content**: depth, iteration, experimentation and variety have value of their own. A large furnishing catalog, several distinct cooking minigames and elaborate premium recipes, substantial dialogue and reactivity for important characters, and lots of amusing contextual NPC moments are all welcome. When one of these pillars has two viable options, the smaller one is not chosen automatically for being smaller: prefer the option that makes the more useful and enjoyable design experiment while staying maintainable.

The other rules still hold. "Meaningful complexity over system count" governs everything outside these pillars, and inside them every step, rule and piece of content still has to earn its place (no busywork, no stages that only add time, no feature because another game has it). Unrelated technical systems get the minimum that serves the game. Combat should be responsive, readable and satisfying, with interesting enemies and bosses worth fighting, but it is one pillar among six: it does not grow into a combat-engineering project that crowds out customization, cooking, dialogue, NPC interactions or relationships. The Hollows are as much a source of ingredients, stories, discoveries and objects for the player's home as they are a fight.

---

## 2. World and Story

### 2.1 Setting

The world of **Aldmere** is a traditional high-fantasy continent: human kingdoms, dwarven holds carved into mountains, elven forests, orcish clans of the steppes, and wild borderlands between them. ~~Ages ago a civilization delved too deep and sealed what it found beneath the earth. Those seals are failing.~~ *(Superseded 2026-10-06 by the founding canon below.)*

*(2026-10-06, locked; Decided 54)* **The sealing and the founding.** Long ago a source of evil opened beneath this region; an enormous force poured out of it into the surface world and caused a great war. **Karias**, a great wizard and once **Maximo**'s apprentice, gave his life to seal it, and Maximo was among those who performed the sealing. The seal held, but not perfectly: small remnants of what lies beyond still seep through, and those remnants are what people now call **the Hollows**. Afterwards Maximo founded **Kariaston**, named it for Karias, and vowed to watch over the Hollows for as long as he lives. People gathered around that watch: glory-seekers, fortune-seekers, people who supply delvers, ordinary settlers and, in time, a few friendly monsters and stranger neighbours, and all of them needed somewhere to drink. Kariaston exists because of the Hollows; they are the centre of its history. The game reveals this through people, places and partial stories, never as a cosmology lecture, and the full nature of the source and the seal is for the story revision (Section 2.9).

**The Hollows** are the underground world beneath Kariaston, and they are not ordinary caves. They are a living, shifting underworld that rearranges itself (justifying procedural layouts), reaching down through distinct regions from the old cellars under the village to the Heart at the bottom (Section 4.6). The Hollows grow outward and upward over time, and monsters from their depths are beginning to emerge onto the surface *(2026-10-06: read as the seal's imperfection, what still seeps through)*. Villagers have their own stories about the Hollows, and some may have their own superstitions or slang for the different depths.

*(2026-10-05)* The world's three main places are **Kariaston** (the village and its community), **Tally Ho!** (the player's tavern and inn) and **the Hollows** (the world below).

### 2.2 The Tavern

**Tally Ho!** sits in the frontier village of **Kariaston**, built directly over an entrance to the Hollows. ~~that locals treated as a curiosity~~ *(2026-10-06: Kariaston was founded as a watch over the Hollows, so its people know exactly what the hatch leads to; Section 2.1.)* Adventurers used to stop in for a drink before exploring the upper Hollows. The player inherits the tavern at the start of the game (see Act I).

*(v0.5)* Tally Ho! is a tavern **and an inn**: guest rooms are part of the property and grow with it (Section 6.8). Kariaston is no longer only a name on the sign: it is a small, persistent village around the tavern that the player lives in (Section 2.8).

### 2.3 The Protagonist

A retired (or reluctant) adventurer who has taken over the tavern. The protagonist is customizable (name, body, colours) with a fixed voice and personality. Default name for this document: **Bram Holloway**.

*(v0.2)* Customization is limited by the art: the player picks a body and recolours skin, hair and outfit through palette swaps. Layered outfits are not possible, because Minifantasy has no clothing or hair layers for attack animations.

### 2.4 Story Arc

> **(v0.5) Awaiting story revision.** The four acts below were written for the Sanctuary-to-Stronghold direction, which v0.5 drops. They are kept unchanged so no story material is silently lost, but Acts II–IV conflict with the village life-sim direction in places (refugees and a wall, a fortified Stronghold, a world war footing, "the whole stronghold rallies"). The conflicts are listed in Section 2.9; revising the acts is a story decision for the owner. Act I fits the new direction as written.

The story unfolds in four acts, advanced by reaching new depths of the Hollows and by tavern milestones (renown, sanctuary capacity).

**Act I — The Inn (Biomes 1–2).** Bram inherits Tally Ho! from a mentor who vanished in the Hollows. Business is slow. A wandering goblin cook teaches Bram that monster meat, prepared right, is delicious. The first customers are adventurers and curious villagers. Hooks: the mentor's disappearance, strange carvings on the walls of the Hollows.

**Act II — The Sanctuary (Biomes 3–4).** Travelers bring news: other openings into the deep have appeared across Aldmere. Monsters raid nearby farms. Refugees begin arriving at the tavern looking for food and safety. Bram expands the inn into a sanctuary with rooms, a wall, and space for newcomers. Some refugees have skills and join the tavern's workforce. The player learns these openings all lead down into the Hollows, which run beneath the world.

**Act III — The Stronghold (Biomes 5–6).** A neighboring kingdom falls. The tavern becomes one of the last safe places on the frontier. Soldiers, a disgraced knight, an elven scout and an orc warband arrive, uneasy allies. The tavern is fortified. Patrons now watch Bram's delves with hope; their morale becomes a mechanical force (see Section 6.4). Bram discovers what happened to his mentor.

**Act IV — The Champion (Biome 7 and the Heart).** The source of what stirs in the Hollows is revealed at their deepest point beneath Kariaston. The whole stronghold rallies. A final descent culminates in a boss fight, with the people Bram fed and sheltered providing direct support. Post-game: endless/ascension mode and "legendary" ingredients.

### 2.5 Key Characters (Draft)

| Character | Role |
|---|---|
| **Bram Holloway** | Protagonist, tavern keeper and delver |
| **Boog** (goblin) | Head cook and mentor for cooking mechanics; gruff, obsessed with flavor |
| **Orik** (dwarf) | Server, bartender and bookkeeper; runs the floor during service. Phi's old friend: ran Tally Ho! in her absence, left when the Hollows grew too dangerous, and was sought out and rehired when she returned *(renamed 2026-10-06: formerly Pip Marrowby, a halfling; history 2026-10-07)* |
| **Old Phi** (Phi'rai, a drow) | Former proprietor of Tally Ho! and the keeper's mentor, vanished in the Hollows; central mystery. Once an adventurer of the **Fortunate Five**, she helped rebuild Tally Ho!, left for many years, and came back decades later to settle down and run it *(renamed 2026-10-07: formerly Old Tamsin; Section 2.10)* |
| **Ser Aldric Vane** | Disgraced knight who arrives in Act II; unlocks weapon training |
| **Sylvaris** (elf) | Herbalist and scout; unlocks herb garden and brewing depth *(v0.5: a natural fit for farming; Section 2.9)* |
| **Grukka Stonejaw** (orc) | Warband chief; blacksmith and fortification builder *(v0.5: the fortification role no longer has a home; Section 2.9)* |
| **The Warden Below** | The intelligence behind the Hollows; antagonist *(2026-10-06: to be reconciled with the sealing canon, Section 2.1: perhaps what lies beyond the seal)* |
| **Maximo** (human) | *(2026-10-07; history locked 2026-10-06)* The founder and mayor of Kariaston and its watchman over the Hollows: helped seal the source of evil his apprentice Karias died sealing, named the village for him, and will not (probably cannot) go back below; a theatrical, heroic, Don Quixote-like eccentric (Section 2.10) |
| **Kaloren Frosthand** (lich) | *(2026-10-07)* A kind wizard and villager, secretly a lich made in the Hollows; his phylactery is still below. Brings Ogrin herbs every third day (Section 2.10) |
| **Grim** (dwarf) and **Ogrin** | *(2026-10-07; history locked 2026-10-06)* Grim, a former delver and the market's keeper, found Ogrin as an infant in the Hollows and raised him; Ogrin looks human, aged impossibly fast, then stopped, and is chronically ill (Section 2.10) |
| **Bart** (orc) | *(2026-10-07)* A villager and bard (Section 2.10) |
| **Gimp** (half-elf) | *(2026-10-07; locked 2026-10-06)* A Hollower: a nomadic, abrasive hunter and ranger who loves rifles and explosives, likes almost no one but Boog (and Phi), and comes up to Tally Ho! when he feels like it (Section 2.10) |
| **Glimmer** (fey spirit) | *(2026-10-07)* A Hollower: a whimsical spirit, once a guardian of the Hollows' seal, who has forgotten her past (Section 2.10) |

*(v0.3)* Boog, Orik, Ser Aldric, Sylvaris, Grukka and other important characters are the obvious candidates for personal questlines (Section 2.6) and persistent relationship state (Section 2.7), and they speak with Portrait Generator portraits (Section 8.1). Their quest trees and relationship progressions are not designed yet.

*(v0.4)* **Canonical characters keep their identities.** Authored story characters (Orik, Boog, Grukka Stonejaw, Sylvaris, Ser Aldric Vane and others) are not renameable, because their names are part of the story; they may still allow visual customization where it fits. Full naming and appearance customization belongs to hired and recruited workers (Section 6.7). Making a named character renameable would be an explicit story decision.

### 2.6 Quests and Objectives (added in v0.3)

A real, persistent quest and objective system is a required feature. **Quest Machine** (Pixel Crushers) owns quest and objective state. Quests include:

- main-story objectives;
- personal questlines for important NPCs;
- villager and resident questlines, including the requests that move a Visitor toward settling in the village (Sections 2.8, 6A.5); *(v0.5: these replace the refugee questlines of the Stronghold direction)*
- meaningful requests from patrons and customers, including requests for particular monster parts or ingredients ("bring me cave troll liver");
- exploration and discovery objectives, including villagers' errands that send Bram into the Hollows to retrieve something, find a rare ingredient, investigate a place, defeat a creature or bring back a strange object *(v0.5)*;
- onboarding and tutorial objectives, where they help;
- multi-stage objectives and their rewards.

**Ordinary service orders are not quests.** A customer ordering a kebab during service belongs to the tavern service systems and lives and ends within that evening. A quest is an objective that persists, or matters, beyond a single ordinary order.

**How the pieces fit.** Quest Machine owns the quest state. Dialogue System presents the conversations that offer, discuss and complete quests, and can query or advance quest state through Hearthdelve-owned adapters. Hearthdelve's gameplay systems publish gameplay events (a part harvested, a dish served, a boss defeated, a crop harvested, a Visitor settled), and the adapters turn those into objective progress. Gameplay code never calls Quest Machine directly (Section 10.3).

Quests should strengthen the essential experience: they point a delve at a particular monster, give a rare dish someone to be cooked for, and deepen the people in the tavern. They are not lists of chores.

### 2.7 Relationships and Recurring Characters (added in v0.3)

Persistent relationships with selected named NPCs and recurring patrons are a required feature, whatever the middleware. The backend is **Love/Hate** (Pixel Crushers; approved and imported in 4g, 2026-10-06), behind a Hearth & Hollows relationship adapter: Affinity and Respect only for now (no Trust, no emotion simulation, no rumor networks), with deeds that gameplay facts commit to the characters who learn of them, and memories that age by the game's days.

Three different things, kept separate:

| Measure | What it describes | Drives |
|---|---|---|
| **Renown** | The reputation of Tally Ho! as a tavern | Customer tiers, story progress (Section 7.1) |
| **Morale** | *(v0.5, reinterpreted)* The state of the village community as a whole | **Cheer** in the Hollows (Section 6.4) |
| **Disposition** | What one named character, or a relevant faction, thinks of Bram | That character's dialogue, quests, help and reactions |

**What characters remember.** Selected characters remember and react to meaningful things, such as: conversations; completing or failing their requests; favours; being served something they love or dislike in the tavern; gifts and food, where appropriate; Bram finding ingredients that matter to them; helping people they care about; major story choices; helping Visitors and residents; tavern and inn improvements or failures; accomplishments in the Hollows; events they witness in the tavern or the village; changes in the village, such as who has moved in; and repeated good or bad interactions.

*(v0.5)* **Relationships matter more under the village direction.** The persistent villagers (Section 2.8) are the main relationship cast alongside the canonical characters, and Visitors who become Inn guests or resident candidates can develop relationships too. Renown (the tavern), Morale (the village community) and disposition (one character or faction) stay separate. **Love/Hate** remains the planned individual and faction relationship system unless a later technical review changes that. Whether romance exists, and how far relationships go, is open (Section 13).

**What relationships can change.** Dialogue and barks, personal quests, gifts and rewards, willingness to help, special services or discounts where appropriate, story reactions, how patrons and residents behave, and optional content.

**Scope.** Hearth & Hollows is not a dating sim or a large social sim. The goal is that important characters feel as if they know Bram, remember what has happened and live in the same world. Relationship state is used selectively, where it creates meaningful character moments.

**Contextual reactivity** *(v0.4)*. Funny, strange and memorable NPC interactions are a learning priority (Section 1.5), so dialogue and relationship work is designed for reactivity, not only linear conversations. Recurring characters should be able to remember earlier conversations, have preferences, disagree, develop running jokes, surprise the player, react to other residents, and comment on the world they share with Bram, including the tavern itself: a patron noticing the absurd monster trophy placed beside their favourite table, a resident who hates the new rug, someone recognizing a boss trophy (Section 6.6). Hearthdelve publishes the facts (what is placed where, what was just bought or found) for the adapters to expose as dialogue conditions and variables; the dialogue decides what is funny about them. Whether decor affects disposition mechanically is open (Section 13).

**Recurring patrons.** The tavern should gradually feel less like a room of disposable customer entities and more like a place with familiar faces. Some patrons return, develop preferences, recognize Bram, react to the tavern's changes and to other residents or events, remember notable service, offer or take part in quests, and change their disposition over time. Most customers stay lightweight and procedurally generated; persistent relationship state is kept for the characters whose continuity creates value. Familiar faces are part of tavern immersion (Section 6.5). *(v0.5)* The familiar faces are now mostly **named villagers** who come to the tavern in the evening, plus the few Visitors promoted to persistent identities (Sections 2.8, 6.3).

### 2.8 Kariaston and Its People (added in v0.5)

Long-term design direction. Nothing here is in the current milestone's scope; the roadmap (Section 11.1) says when village life is first built.

**The village.** Kariaston is a small, persistent village around Tally Ho!. It is small enough that players learn who lives there: a place where people know each other, and come to know Bram. Its exact size, layout and buildings are not designed yet (Section 13).

**Who lives there.** The population is mostly human, but classic fantasy peoples are normal neighbours: dwarves, elves, orcs, goblins, halflings, skeletons, liches and other fitting folk. A skeleton, a goblin, a lich or an orc can simply be a member of the community, with a job, a garden and opinions about the new rug in the tavern, rather than an enemy archetype that happens to be friendly. The tone is a **cozy fantasy community with odd people and occasional absurdity**, contrasted against the Hollows, a dangerous ancient underworld: warm, funny and strange on the surface; dangerous below (pillar 4).

**Three kinds of people.** Identity is tiered so that continuity is spent where it creates value, and so that the save doesn't grow with every stranger who ever ordered a stew.

| Tier | Who | Persists |
|---|---|---|
| **Named villagers** | A fixed, authored cast of residents, including canonical story characters who live in Kariaston | Always |
| **Visitors** | Generated outsiders who come to the tavern; not members of the village | Mostly not: a Visitor lasts the evening (or their stay) and is then forgotten |
| **Promoted Visitors** | A Visitor who has become relevant through the Inn or the path toward settling: an Inn guest the player has got to know, a resident candidate | Yes, from the moment of promotion: their generated identity becomes a saved, persistent identity |
| **Recruited residents** | A promoted Visitor who has moved into one of the village's empty plots (Section 6A.5) | Permanently, as a villager |
| **Hollowers** *(2026-10-07)* | Authored people who live in the Hollows and are not enemies, met below or coming up to Tally Ho! (Section 2.10) | Always, like named villagers |

The path is: **ordinary transient Visitor → potentially interesting Visitor → persistent guest or resident candidate → resident.** The exact promotion rules are open (Section 13).

**Named villagers** are persistent authored characters. They can have homes, schedules, relationships with Bram and each other (friendships and rivalries), dialogue, quests, preferences, and reactions to world events, to the player's tavern and inn, and to other villagers. They also take part in tavern life: during evening service some of them are chosen from the available population and come in to eat and drink (Section 6.3), with the same identity, memories and relationship state as in the village. The number of permanent villagers is open (Section 13).

**Visitors** are generated outsiders: travellers, adventurers, merchants, pilgrims and stranger things. They may have a generated name, race, appearance, food and drink preferences, light personality traits, a reason for travelling and other reusable characteristics. Most are temporary. They provide variety, strangers to meet, potential Inn guests and potential future residents. How deep Visitor generation goes is open (Section 13).

**Recruited residents** become persistent villagers: their generated identity is permanent, and they take part in schedules, dialogue, relationships, tavern visits, quests and interactions with other villagers. This is where generated characters become meaningful rather than disposable, so they must not rely on procedural dialogue alone. They use the same dialogue architecture as everyone else (Section 10.3): authored modular dialogue, conditions on traits, race and background, contextual barks, relationship state and event-driven responses. How much bespoke writing a recruited resident gets is decided when the system is designed.

**Reactivity and comedy.** Meaningful, funny and strange NPC interactions are a learning priority (Section 1.5), and the village gives them room. Characters should be able to react to food, relationships, other villagers, strange Visitors, the tavern's decorations, trophies from the Hollows, monster ingredients, quests, events and who has moved into town. Hearthdelve publishes the facts; the dialogue decides what is funny about them (Section 2.7).

### 2.9 Story Conflicts from the v0.5 Direction (added in v0.5)

The village direction leaves parts of the story written for the Stronghold direction without a home. Nothing has been deleted: these are for the owner to revise.

1. **Act II, the Sanctuary.** Refugees arriving for food and safety, and the inn expanding into "a sanctuary with rooms, a wall, and space for newcomers". Guest rooms survive as the Inn (Section 6.8) and newcomers can become Visitors and residents (Section 6A.5), but the wall, and refugees as the main way people arrive, belong to the old direction.
2. **Act III, the Stronghold.** A neighbouring kingdom falls, the tavern becomes "one of the last safe places on the frontier", soldiers and an orc warband arrive, and "the tavern is fortified". The fortification and war-footing premise is gone; the act needs a new shape.
3. **Act IV, the Champion.** "The whole stronghold rallies" and the people Bram sheltered support the final descent. The idea of a community rallying behind Bram survives (it is what Morale and Cheer express, Section 6.4) but the stronghold framing does not, and the elevator pitch's "rallying point of a world looking for a champion" is a larger, more martial scale than a village life sim.
4. **Story gating.** Acts advance by depth in the Hollows and by "tavern milestones (renown, sanctuary capacity)". Sanctuary capacity no longer exists; village and inn milestones (residents settled, Inn rooms, relationships) are candidates.
5. **The surface threat.** Monsters raiding farms and the Hollows' creatures spilling onto the surface gave the Stronghold its purpose. Some surface stakes may still be useful, but escalating surface danger pulls against "cozy on the surface, dread below" (pillar 4). Decide how much of the menace reaches the village.
6. **Grukka Stonejaw** is "warband chief; blacksmith and fortification builder" and arrives with a warband in Act III. The blacksmith survives; the fortification role and the warband arrival need revision.
7. **Ser Aldric Vane** arrives in Act II (weapon training). Compatible, but his arrival was framed by the sanctuary.
8. **Sylvaris** unlocks the herb garden; farming (Section 6A.2) may make Sylvaris its natural mentor, or a garden may now come earlier than Sylvaris.
9. **Canonical characters and the village.** It is undecided which canonical characters are Kariaston villagers from the start and which arrive later, and whether a late arrival uses one of the three empty plots (which would reduce the player's influence over who settles there).
10. **Rescued NPCs and refugee staff.** "Rescued NPCs join the tavern" and "refugee staff unlock new dungeon abilities" (Section 3.3) relied on refugees; people rescued in the Hollows could instead become Visitors or resident candidates.
11. **Customer types.** "Refugees, and eventually soldiers and heroes" as customer tiers (Section 6.3) came from the war arc.
12. **How far the Hollows reach** *(2026-10-05)*. The Hollows are the world beneath Kariaston, but Act II's news of "other dungeons" opening across Aldmere, "connected beneath the world", is now worded as other openings that all lead into the Hollows. Whether the Hollows run beneath all of Aldmere or are local to Kariaston, and whether those other openings stay in the story at all, is for the story revision.

### 2.10 The Kariaston Cast (added 2026-10-07)

The owner's call on 2026-10-07 (Decided 51): seven people join the cast, five who live in Kariaston and two who live in the Hollows. Their names, kinds and core concepts are the owner's; everything in *Proposed* below is a first reading for the owner to correct, and nothing here widens any milestone (`docs/PLAN_4H.md` says what 4h builds). *(2026-10-06: the owner's approval of the 4h plan locked further canon for Maximo, Karias, Grim, Ogrin, Kaloren and Gimp, marked **Locked** below; where it conflicts with a campaign note or an earlier proposal, the locked text wins.)* Several names and roots come from the owner's own D&D campaigns (Astral Gauntlet and *Campaign 3*). Only what serves these characters is carried over; the campaigns' wider lore (the Silver Flame, the Blight, the Mournland, its kingdoms and wars) is not part of Aldmere.

**Experience target:** *Kariaston is a village of odd, kind people with old wounds, and the Hollows have touched almost all of them: the keeper keeps meeting the cost of what lies below in the faces at the bar.* (Character web and Story Machine lenses: each new person is tied to the Hollows, to Boog or Orik, or to each other, so they create stories together instead of standing alone.)

**Hollowers** are a new kind of person: someone who **lives in the Hollows and is not an enemy**. They are met below or come up to Tally Ho! (by the cellar hatch: the tavern is built over a way down), and some may follow the *Hollows encounter → recurring visitor → Inn guest → possible resident* path (Decided 50). They are authored characters, never generated, and never arbitrary enemies made friendly.

#### Villagers

**Kaloren Frosthand** (wizard, secretly a lich; he).
- *Owner's concept:* became a lich in the Hollows, somehow regained his memories and left; his phylactery is still down there. Despite this he is a nice, kind person.
- *Proposed:* a gentle, courteous scholar who is slightly too cold to the touch, wears gloves in summer, and never eats at the tavern (he orders, and admires the plate). The comedy is in near misses: he forgets to breathe in conversation, knows a little too much about how the Cellars were built. The warmth is real: he chose to come back up and be a neighbour.
- **Locked (2026-10-06):** he brings Ogrin herbs that ease his symptoms, **once every three days**; they relieve, never cure. In 4h this is a small routine the player may witness (tower → the cottage → on with his day). Not connected to his lichdom.
- *Hooks:* the phylactery is the obvious long quest (a quest object deep in the Hollows; whether the keeper returns it, hides it, or something else is the owner's story decision). He is living proof that the Hollows can change a person and that the change can be survived, which matters to anyone wondering what happened to Old Phi.

**Maximo** (an elderly human, the founder and mayor of Kariaston; he).
- **Locked (2026-10-06, Decided 54):** Maximo helped seal the source of evil beneath this region; his former apprentice **Karias**, a great wizard, gave his life in the sealing. Maximo then founded Kariaston, named it for Karias, and vowed to watch over the Hollows for as long as he lives. He will not go back into the Hollows, and probably **cannot**: the reason is a **future story decision**, not hinted at yet. He keeps the Don Quixote personality (theatrical, heroic, eccentric, romantic about adventure, funny), and the contrast is the point: under the speeches is a man who really did help save the world. **World figure (locked): the blue Knight on foot** (Minifantasy *Knight Jousting*); no crown.
- *Superseded by the lock:* Karias dying on an ordinary expedition, the village possibly renamed rather than founded, and the campaign timeline below where it conflicts.
- *From the campaign notes (Campaign 3), kept as personality DNA only:* a tanner's son (Tannerman's Tannery) raised on the adventure stories of Rayford Goodfallow; imagines himself the hero of the tale; grand, sincere speeches ("my words of boundless wisdom"); rousing and very rude songs; mentored Karias, "an eager young wizard who held his staff like a wish come true". The campaign's chronology (adventuring only from his late fifties) does not apply.
- *Proposed:* proclamations, ceremonies and a heroic song for every small civic event, genuine courage about everything except the hatch at Tally Ho!. A delver living under his village is the thing he most admires and most fears; he may treat the keeper as a fellow watchman, which is both funny and sad.
- *Hooks:* what is left of Karias, and of the seal, below; why Maximo cannot go back.

**Grim and Ogrin** (a dwarf, Grim, he; and Ogrin, he).
- **Locked (2026-10-06, Decided 55):** Grim is a dwarf and a **former delver**. On a delve into the Hollows he found a human-looking infant, alive, beside two dead adults he assumes were the parents, and brought him home to Kariaston and raised him: Ogrin. Ogrin aged impossibly fast (to about ten in a year or two), then the ageing stopped abruptly; since then he has been chronically ill, with bouts of severe exhaustion. Grim doesn't know what Ogrin is, why he aged so, or what happened below; he loves him deeply. Ogrin calls him **Grim**; a later beat where Ogrin first calls him **Dad** is reserved and never spent casually. Ogrin is central to Glimmer's questline (below).
- **Locked (2026-10-06, the 4h plan, H7):** Grim runs the market stall as his ordinary livelihood, one part of who he is besides Ogrin. *(Superseded: "spends everything he earns on Ogrin's remedies".)*
- *Proposed:* Grim is gruff, fair and practical, has a dry warmth he hides and a delver's unromantic knowledge of the Hollows. Ogrin is bright, curious and opinionated: he draws maps of the Hollows from what he overhears, collects the keeper's stories, loves Bart's songs and Boog's explosions from a distance, hates onion broth and being called brave or fragile; good days at the stall, bad days at his window. The relationship with Bram grows through care, not deeds of nerve.
- *Hooks:* what Ogrin is and what happened below; Glimmer.

**Bart** (an orc bard; he).
- *Owner's concept:* an orc bard.
- *Proposed:* plays in Tally Ho! some evenings, which gives the room music, a regular face and a running joke: his repertoire versus Maximo's songs (the mayor considers himself a fellow artist; Bart considers him a fan). A bard is also the village's news, gossip and rumor-carrier, a natural voice for barks about the keeper's deeds without adding a rumor system.
- *Hooks:* whether he can play during service (a live-music decor or Renown effect is a later question, not a rule).

**Orik** (a dwarf with a ginger beard; he). Already in the game (Decided 43): the server, bookkeeper and, in the owner's words, bartender of Tally Ho!.
- *Owner's history (2026-10-07, Decided 53):* an old friend of Phi's. She hired him, and he helped run Tally Ho! while she was away. When the Hollows became too dangerous he eventually left; when Phi came back she sought him out and hired him again.
- *As built (4g closeout, 2026-10-06):* his old "hired me for a week, eleven years ago" line was replaced at the owner's request by three short lines heard only if the keeper asks (the rebuilding and the Fortunate Five, his years keeping it open alone, her coming home to find him). His refusal to go below ("my family went down for three hundred years... i came up. i'm staying up") now has a second root: he once ran the place over a hole that got too dangerous, and walked away.
- *Proposed:* this makes Orik the keeper of Phi's history: he knew her before and after, kept the books through her absence, and is the person most shaken (and least willing to show it) that she hasn't come back. Whether he knew the Fortunate Five is open.

#### Hollowers

**Gimp** (a half-elf, lives in and around the Hollows; he).
- **Locked (2026-10-06, Decided 56):** a **half-elf** hunter and ranger; nomadic, abrasive, something of a nutjob; dislikes most people and hates cities; loves guns, rifles above all. The exceptions are **Boog** (their shared enthusiasm for dangerous explosives) and **Phi** (he knew and liked her, which, since he likes almost no one, quietly says something about her). He starts **standoffish toward Bram** and does not warm up because Bram is the protagonist. He lives in or around the Hollows but is extremely nomadic: below for a while, in the forest or up a tree, gone for stretches, out of the area entirely, back to see Boog. **World figure (locked): `soldier_headband`** (Minifantasy *Modern Soldiers*, All Exclusives › Creatures); his portrait is a half-elf treatment, not a human recipe.
- *From the campaign notes, as energy only:* a scout who ranged ahead and turned up later, carried gunpowder, once came back wearing a crow cape, had a heart to heart with Boog.
- *Proposed (4h):* his visits feel irregular ("Gimp shows up when Gimp shows up"); he climbs out of the cellar hatch, sits at the bar with Boog arguing blast radii, treats the keeper as an unwanted complication, and leaves the way he came. The smallest test of *encounter → recurring visitor*.
- *Hooks:* he may know where things were lost below; later errands that need something blown open; what he knew of Phi.

**Glimmer** (a fey spirit; she).
- *Owner's concept:* came from the Hollows. She was a guardian meant to seal the Hollows, failed, and has forgotten her past. Whimsical, in the manner of Syl from *The Stormlight Archive*. She eventually has a questline in which she heals and fuses with Ogrin.
- *Proposed:* a small light with opinions: curious about everything, easily distracted, delighted by words she has just learned, and suddenly, briefly ancient when something in the Hollows reminds her of what she was. She fits Section 2.1's sealing: she was a guardian meant to help seal or guard against what lies beyond, and failed *(how that relates to Maximo and Karias's sealing, which held, is open: Section 2.10 question 8)*. Her questline (Phase 5 or later): the keeper helps her remember; the bond with Ogrin heals him and makes her whole, at a cost the story decides.
- *Reading taken (to confirm):* "heal and fuse with Ogrin" is read as one act, a bond in which Glimmer heals Ogrin and is herself restored by it, much as Syl and Kaladin's bond restores Syl.

#### Old Phi (renamed 2026-10-07)

**Old Phi** (Phi'rai, a drow; she) replaces Old Tamsin as the former proprietor of Tally Ho!, the mentor who vanished in the Hollows and the Act I mystery.
- *Owner's concept:* an adventurer who helped rebuild Tally Ho!, then left for many years, and returned decades later to settle down and run the tavern. Usually called "Old Phi". She would bring up stories of her old adventuring party, **the Fortunate Five**.
- *As built:* nine days before the keeper arrives she went down into the Hollows, "a week at most"; her letter leaves Tally Ho! to the keeper; Orik's three lines tell their history if asked; she let Boog keep his bomb; she went after the Larder Troll twice and wouldn't say why. No stable id ever named her (there was no character asset); 4h's plan gives her the story id `phi` for things said about her.
- *Proposed:* since she's missing, her stories reach the player through others: Orik's ledger and his memory of her, Boog's kitchen lore, and things she left in Tally Ho! (a curio from the Five, a letter, a carving). A drow is long-lived, so "decades" sits easily: she rebuilt Tally Ho! young, by drow reckoning, and came back still able to delve.
- **Approved (2026-10-06):** a framed portrait of Phi'rai hangs in Tally Ho! (a Portrait Generator drow), making her present in the room without explaining her disappearance.
- *Questions:* who the other four of the Fortunate Five were, and whether any of them are in Kariaston now (Kaloren, ageless as a lich, would be the natural candidate; Maximo only if his late-life adventuring overlapped hers), or whether one of them is what she went down after.

#### Through-lines (proposed, for the owner)

- **The Hollows take memory.** Kaloren lost his and got it back; Glimmer lost hers. If that is a property of the Hollows rather than a coincidence, it is a quiet thread for Phi's disappearance and for the Warden Below.
- **The Hollows are the village's centre** *(locked 2026-10-06)*. Every major character has their own relationship with them: Maximo helped seal the evil and founded the village to guard it; Karias died sealing it; Phi went back after retiring; Grim went in and brought Ogrin out; Ogrin seems changed by something connected to it; Kaloren was transformed by it and left part of himself below; Gimp chooses to live around it; Bram keeps choosing to go down.
- **Everyone has lost someone below.** Maximo lost Karias, Grim may lose Ogrin, Kaloren lost himself, Glimmer lost her purpose, Orik's family went down for three hundred years, Tally Ho! lost Phi. The keeper is the one who goes down and comes back.
- **Pairs.** Boog and Gimp (explosives), Maximo and Bart (songs), Grim and Ogrin (family), Ogrin and Glimmer (the bond), Kaloren and Maximo (both survivors of the Hollows, one who went back and one who never will). These are the first pairs for "react to other residents" (Section 2.7).

#### Questions for the owner (also in Section 13, Open 12)

1. ~~**When Kariaston got its name.**~~ *Answered 2026-10-06:* Maximo founded it after the sealing (Decided 54). **Still open:** how long ago the sealing was, and so Maximo's age. Orik's built line ("my family went down for three hundred years") implies at least three centuries, which makes Maximo, a human, extraordinarily old: tied to the future decision about why he can't go back.
2. **Karias: elf or half-elf.** The campaign notes say half-elf; the brief says elf. These docs say elf until told otherwise.
3. **A secret lich among neighbourly liches.** Section 2.8 makes skeletons and liches ordinary neighbours, so Kaloren's secret can't simply be "I'm a lich". Recommended: what he hides is where and how he became one (the Hollows made him) and that his phylactery is still below; whether other villagers know is the owner's call.
4. **Glimmer and the Warden Below.** A failed guardian of the seal and an antagonist called "the Warden" overlap in role and name. Are they the same order, enemies, or is "Warden" due for a rename?
5. **Names that sit close together.** **Bart** and **Bram** (the keeper's default name) differ by two letters; **Grim** and **Gimp** by one; **Orik** and **Ogrin** share a shape. All are kept as given; flagged only because they will sit side by side in dialogue.
6. ~~**Gimp's kind.**~~ *Answered 2026-10-06:* a half-elf (Decided 56).
7. **Firearms.** In the campaign Boog carries a pistol and Gimp buys bullets and gunpowder. *(2026-10-06: Gimp now loves guns, rifles above all, and his figure carries a rifle.)* Recommended: rifles exist as rare personal property of odd people like Gimp (seen and talked about), never a player weapon in this phase. Needed before 4h Checkpoint D.
8. **Glimmer and the sealing.** Glimmer was a guardian meant to help seal or protect against the Hollows, and failed; Maximo's sealing held. Same event, an earlier attempt, or a different guard? (Also question 4.)

---

## 3. Core Gameplay Loop (rewritten in v0.5)

### 3.1 The Day (rewritten in v0.5)

**Wake → free daytime → evening prep → tavern service → nighttime delve → return and sleep → next day.**

1. **Wake.** A new day in Tally Ho!.
2. **Free daytime (the village and the property).** The player is free to spend the day as they like. Long term this includes decorating and rearranging the tavern, decorating and managing the Inn, farming, ranching, fishing, talking to villagers, relationship moments, quests and errands, shopping, exploring the village, managing ingredients and resources, and preparing for the evening or the night's delve (Section 6A). Daytime is **not** a menu leading straight into the delve: it becomes a free-roaming life-sim part of the day.
3. **Evening: the tavern.** The player decides to prepare and open the tavern. The established flow stays: **Prep → open → cook and serve → Results → close.** The physical service gameplay remains central (Sections 6.1, 6.5). The crowd is a mix of named villagers and Visitors (Section 6.3).
4. **Night: the delve.** After the tavern closes, the player may delve into the Hollows (Section 4). There is **no separate time-of-night limit:** Essence remains the delve's only health pool and its only time pressure (Section 4.4).
5. **Return and sleep.** Coming back from the delve, by extraction, death or Essence running out, ends the day and leads to sleep and the next morning. The day's autosave happens on sleeping.

For now the sequence is fixed as **tavern → delve → sleep**. Whether the player may eventually skip the nightly delve and simply sleep (the life-sim side may make some days long) is an **open question**, not decided (Section 13).

**What moved from the old day.** The v0.4 day ran Morning (a prep menu) → Delve → Evening service → Night (upgrades, story, save); it is kept in Appendix C. Under the new day:

- *Accepting requests* happens in the daytime, by talking to villagers (and in the evening, from people in the tavern).
- *Setting the menu* happens at evening Prep, as it already does in the 4c build.
- *Spending* (gear, upgrades, furnishings) moves to the daytime (shops, the tavern's own ledger; *(4e plan, 2026-10-05)* a "Delver's Board" may return as a diegetic village quest and request board, never as a Delve Marks upgrade tree) and to whenever the tavern is closed; where the old Night upgrade screen's functions end up is decided at the GameFlow integration step.
- *The pre-delve meal* (the breakfast buff in the 4c build) no longer sits right before the delve. Whether it becomes a late meal after service, before descending, or something else, is decided at the GameFlow integration step.
- *Story scenes* can happen at any point of the day where they fit, most naturally in the daytime village and in the tavern.

**Consequences worth designing for.**

- **The Hollows answer tomorrow.** The haul comes home at night and is cooked the next evening, so freshness now matters across a whole day in the storeroom (overnight loss already exists since 4c). Preservation, and which parts keep, become part of planning; the freshness tuning will need revisiting.
- **Day one doesn't need a delve.** Surface ingredients (Section 5.5) let the first evening open before the first delve.
- **The day's length.** A full daytime, a service and a delve in one sitting may be long. This is the reason the skip question and the daytime time model (Section 13) stay open until they are prototyped.

**What is built today.** Since 4d step 5, `GameFlow` runs the new order (daytime → evening → the night's delve → night → sleep), with a daytime placeholder (the old morning panel: storeroom, the delve meal, open for the evening) standing in for free daytime until the village milestone (Section 11.1). *(Updated 2026-10-05; until 4d step 5 the build ran the old order, Morning → Delve → Evening → Night → Sleep.)*

### 3.2 Loop Diagram (rewritten in v0.5)

```mermaid
flowchart LR
    W[Wake] --> D[Free daytime: village, farm, Inn, decorating, people]
    D --> E[Evening: prep, open, cook and serve, results]
    E --> N[Night: delve into the Hollows]
    N --> S[Return and sleep]
    S --> W
    D -- surface ingredients, requests, furnishings placed --> E
    E -- Gold, Renown, relationships --> D
    N -- monster and rare ingredients, discoveries, quest objects --> D
    D -- gear, upgrades, quests that point the delve --> N
```

### 3.3 How the Surface and the Depths Feed Each Other (rewritten in v0.5)

| From the Hollows to the surface | From the surface to the Hollows |
|---|---|
| Monster, rare and magical ingredients for special dishes | Gold buys weapons, armour and tools |
| Harvest quality affects dish quality | Meals grant run buffs |
| Rare parts unlock new recipes | Villagers' quests and customers' requests point a delve at a monster, place or object |
| Found recipe scraps and lore | Relationships and residents unlock help, services and new delving abilities *(replaces "refugee staff")* |
| Furnishing discoveries and boss trophies for the tavern and inn | Village Morale grants "Cheer" in the Hollows |
| Quest objects that move villagers' stories on | Surface ingredients keep the everyday menu running, so the Hollows can be about the unusual |
| People met in the Hollows may come to the tavern as Visitors *(open; Section 2.9)* | |

### 3.4 Daytime Time Model (added in v0.5)

How daytime passes is **open** and expensive to reverse, so it is prototyped before it is chosen (Section 13): a continuously ticking clock in the style of *Stardew Valley*, player-controlled phase transitions (the day lasts until the player chooses to go and open the tavern), or something between. The choice shapes NPC schedules, farming, how long a day feels, and how much pressure the daytime carries; the tavern and the delve do not depend on it.

---

## 4. Dungeon Gameplay: Delving the Hollows

*(2026-10-05)* This section describes the dungeon gameplay layer: delves into the Hollows, one region (biome) at a time.

### 4.1 Combat Feel (rewritten in v0.2)

Target feel is *Hades* and *Cult of the Lamb*: responsive, fast, readable top-down melee with strong hit feedback.

- **Movement:** 8-direction run; dodge roll with i-frames. No jumping.
- **Facing:** the art has four diagonal facings (front-right, front-left, back-right, back-left). Movement is 8-directional; the sprite shows the nearest facing.
- **Aim:** by movement direction on gamepad; by mouse on keyboard and mouse.
- **Attacks:** a light combo (three hits), a heavy/charged attack (hold to charge), two skill slots (tools/throwables), the **Harvest Finisher**, and the **Kitchen Arts** special (meter attack).
- **Feedback:** each hit plays one combined feedback: flash, a short freeze-frame, camera shake (subtle, adjustable), sound and a haptic pattern. Damage numbers are optional. Enemy attacks are clearly telegraphed.
- **Health:** Essence is the only health pool (Section 4.4).

### 4.2 Weapons as Kitchen Tools (rewritten in v0.2)

A signature flavor hook: many weapons are culinary, which ties weapon choice to harvesting. Weapon types now follow the attack animations Minifantasy provides (slash, thrust, swing, two-handed, ranged, guard, each with a charged version where available).

| Weapon Type | Example | Minifantasy animation | Harvest Specialty |
|---|---|---|---|
| Cleaver | Butcher's Cleaver | Slash (axe) | Clean cuts, bonus to meat quality |
| Filleting Blade | Eel-Tooth Knife | Slash (dagger) | Fast combos, perfect for fish/serpent parts |
| Skewer Spear | Rotisserie Pike | Thrust (spear, pitchfork) | Reach, pins enemies; "spit-roast" fire variant |
| Tenderizer | Troll-Mallet | Two-handed (waraxe) | Stagger damage, softens tough meats (bonus to stews). No mallet art exists; the waraxe stands in or is recoloured. |
| Frying Pan | Iron Skillet | Guard (buckler) | Parry/block weapon; counter hits sear enemies. No pan art exists; needs a small edit of the buckler. |
| Traditional | Sword, longsword, flail, whip, bow, slingshot | Slash, two-handed, swing, ranged | Standard harvest; wider combat variety |

~~Weapons have rarity tiers (Common → Fine → Masterwork → Legendary) and random affixes per run.~~ *(4e plan, 2026-10-05: randomized rarity and affixes are removed from the roadmap; run powers already give per-run variety, and long-term progression lives in the tavern, village and relationships. New weapons wait for the protagonist body decision in 4g, because the Weapons pack's attack animations come as base bodies with weapon layers.)* Permanent unlocks add weapons to the drop pool. Elemental variants use the effect layers from *Magic Weapons And Effects*.

### 4.3 The Harvest System

The heart of the fantasy. How a monster dies influences what it drops. Unchanged by the pivot.

- **Clean Kill:** finishing with a matching tool type or a finisher move yields higher quality parts.
- **Overkill:** excessive damage (big explosions, over-hits) damages parts, lowering quality or destroying some.
- **Elemental Kills:** fire-killed monsters may drop "Seared" parts (pre-cooked, faster to prepare but some recipes need raw). Ice-killed monsters drop "Chilled" parts that stay fresh longer. Poison kills make parts inedible. Inedible parts still drop and can be carried; they will get a use later (a small sale value, poisons, or traps).
- **Harvest Finisher:** when an enemy is low, a prompt allows a quick finisher that guarantees a premium part at the cost of a moment of vulnerability. Risk/reward.

### 4.4 Essence, Inventory, Freshness, and Extraction

- **Essence:** delves are limited by Essence, which drains over time in the Hollows and drops when the player takes damage. At zero Essence the player is forced out (treated as a death). It is the only health pool. Max Essence and drain rate are upgradeable in the tavern. *(v0.5)* The delve happens at night, after service, and there is no separate time-of-night limit: Essence is the only time pressure.
- **The Satchel:** limited carry slots for ingredients (6 by default, stacks of up to 3), upgradeable in the tavern. Forces choices about what to keep. When it is full, picking up a part opens a swap prompt.
- **Freshness:** parts decay over time during a delve. Salt, ice runes, and preservation jars extend freshness.
- **Extraction:** the player can return via exit points (a rope or lift back to the tavern). Leaving early keeps everything; continuing deeper risks it.
- **Death:** the player loses the entire haul except one satchel slot they choose to keep (the Lockbox, the whole stack in it), and loses the day's unspent run currency. Permanent unlocks are never lost.

### 4.5 Field Cooking (Optional Mechanic; deferred in the 4e plan, 2026-10-05: a second cooking context competing with the tavern; if it returns, it needs one specific purpose)

At campfire rooms, the player can cook a quick meal from carried parts to restore Essence or grant a buff. This sacrifices ingredients that could be sold, creating a meaningful choice, and echoes the *Delicious in Dungeon* spirit.

### 4.6 Run Structure and Biomes (rewritten in v0.2)

**Runs are room by room.**

1. Enter a room; the doors lock.
2. Clear the room.
3. The doors unlock. Each door shows the **reward** of the room behind it.
4. Choose the next room by its reward.

Room rewards:

- **Ingredients** (a guaranteed part, or a room with a particular monster)
- **Gold**
- ~~**Delve Marks**~~ *(removed in the 4e plan, 2026-10-05)*
- **A weapon**
- **A run power-up**, chosen from three

*(v0.4)* The run's reward model must not assume these are the only kinds. Persistent **customization discoveries** (Section 6.6) are a planned future reward kind, from enemy drops and possibly from rooms, and must plug in later without rewriting the run reward system. *(v0.5)* So are **quest objects** (things villagers asked Bram to bring back, Section 4.8). Ingredient rewards must not assume monster parts will be the player's only cooking ingredients: the surface supplies the everyday ones (Section 5.5), so the ingredient rewards of the Hollows lean toward the unusual, the rare and the magical.

Floors are generated from a **room graph** (Section 10.5). Each biome has 3 floors plus a boss arena. Special rooms: campfire (field cooking), shop, extraction point.

**Biomes and their Minifantasy packs.** *(2026-10-05)* The biomes are regions of the one Hollows at increasing depth, not separate dungeons: the Cellars lie just beneath Kariaston and the Heart at the bottom. Only Biome 1 has been checked against the catalog in detail. The rest are provisional: the packs exist in our library, but their sheets have not been inspected yet. `docs/ASSET_MAP.md` holds the verified mapping.

| # | Biome | Theme | Environment packs | Creature candidates | Boss candidate |
|---|---|---|---|---|---|
| 1 | The Cellars | Old cellars and tunnels | Dungeon, More Dungeons, Dungeon Traps | Green Slime, Bat, Giant Spider, Skeleton, Mushroom People; Slime Cube as elite | **The Larder Troll** (Ancient Troll art; chosen 2026-10-05, replacing the Mother Slime candidate): a troll that raided the tavern's stores and eats dropped parts to recover |
| 2 | Fungal Warrens | Glowing fungal caves | Deep Caves, Glowing Mushrooms, Giant Mushrooms | Mushroom People, Blue Slime, Giant Snail, Necrofungus risen corpses | Open (no fungal boss found yet) |
| 3 | Goblin Sprawl | Goblin shanty-town and mines | Deep Caves, Old Mine Addon, Gold And Rock Nodes | Goblin, Goblin Raider, Goblin Sapper, Warg, Trasgo | Goblin King |
| 4 | Drowned Halls | Sunken dwarven ruins | Dwarven Kingdom, Shallow Water, Cenote | Frogfolk, Naga, Water Elemental, Octopurr | Kraken |
| 5 | Ember Forge | Volcanic dwarven forge | Lava Forge, Dungeon Lava Pit, Volcano | Magma Hound, Magma Golem, Fire Elemental, Imp, Burning Skull | Dragon or Balrog |
| 6 | Frostvault | Frozen crypts | Icy Wilderness, Ice Dungeon (More Dungeons) | Yeti, Wraith, Spectre, Skeleton, Evil Snowman | TBD (the Ancient Troll became the Cellars' Larder Troll, 2026-10-05; a Lich remains a candidate) |
| 7 | The Rootdeep | Living, pulsing underworld | Lost Civilization, The Void, Chamber Of Secrets | Tree Spirits, Beholder, Alien Bio Horror, Shoggoth's Avatar | The King In Yellow |
| — | The Heart | Final area, the deepest point of the Hollows | To be chosen | — | Demon Lord (as The Warden Below) |

Changes from v0.1 forced by the art: there is no rat with an attack, so the Giant Rat and the Cellar King are replaced in Biome 1; the Leviathan Eel becomes the Kraken; other v0.1 monsters without art (boar-riders, crab knights, salamanders, ice trolls) are replaced by the candidates above.

Branching between biomes lets players choose which ingredients to target on a given run.

### 4.7 Enemies and Bosses

Each enemy has a **combat profile** (behavior, attacks, telegraphs) and a **harvest profile** (parts, preferred kill method, freshness rate). Bosses drop signature ingredients that unlock "Legendary Dishes" and progress the story. *(v0.4)* Enemies may later also carry a **decor drop profile** (a small chance of customization discoveries, by enemy and biome), and bosses can award rare or unique furnishings (Section 6.6). Boss candidates per biome are in Section 4.6. Enemies are chosen from creatures that have idle, move, attack, damage and death animations.

### 4.8 The Role of the Hollows (added in v0.5)

The Hollows remain a major pillar: they are **the source of what the village cannot provide.** Their rewards can ultimately include monster ingredients, rare ingredients, magical materials, unique furnishing and customization discoveries (Section 6.6), boss trophies, quest objects, weapons and combat progression, run power-ups and other unusual discoveries.

**People send you down.** Villagers' quests frequently point into the Hollows: retrieve something, find a rare ingredient, investigate a location, defeat a creature, bring back a strange object. This ties dialogue and relationships directly to combat and exploration: the delve is often *for someone*.

**It is one part of the day.** The delve happens at night, after the tavern closes, and returning ends the day (Section 3.1). The 4d room graph, extraction routes, floor transitions, arena, room loading and encounters are all compatible with this; only where the delve sits in the day changes.

**It doesn't carry the whole kitchen.** Surface activities supply dependable everyday ingredients (Section 5.5). The Hollows are what make special dishes, strange furnishings and stories possible; delving should never feel like a chore the player does to restock onions, and farming should never make it unnecessary.

---

## 5. Ingredients and Recipes

### 5.1 Ingredient Properties

Every ingredient is data-driven (ScriptableObject) with:

- **Category:** Meat, Offal, Fish, Fungus, Plant, Egg, Spice, Liquid, Magical. *(v0.5: grains, fruit, dairy and other ordinary ingredients from farming and ranching may need categories when those are designed.)*
- **Source** *(v0.5)*: where it comes from (the Hollows, the farm, the ranch, fishing, shops and trades, villagers). Sources overlap; Section 5.5.
- **Flavor Tags:** Savory, Sweet, Spicy, Sour, Bitter, Umami, Earthy, Arcane.
- **Quality:** Poor / Standard / Fine / Premium (from the Harvest system).
- **Freshness:** 0–100%, decays over time; affects dish score. Tracked per stack: when two stacks of the same part merge, freshness becomes the count-weighted average. Kitchens use the least-fresh stock first (on a tie, the lower quality first). Freshness is designed to also drop in the storeroom overnight, slowed by preservation upgrades (salt, ice runes, jars).
- **Rarity:** Common → Legendary; affects price.
- **Special Effects:** some ingredients carry buffs (e.g. a dragon heart grants fire resistance when eaten).

### 5.2 Example Ingredient Table

*(v0.2)* Ingredients follow the monster roster in Section 4.6. Icons come from the Minifantasy *Body Part Icons*, *Loot Icons* and food icon sets.

| Monster | Part | Category | Flavor | Notes |
|---|---|---|---|---|
| Green Slime | Gel | Liquid | Sweet | Used in jellies and drinks |
| Green Slime | Core | Magical | Arcane | Tonic ingredient |
| Bat | Wing | Meat | Savory | Staple early meat (replaces Rat Haunch) |
| Giant Spider | Leg | Meat | Savory, Umami | Premium when killed with a Cleaver |
| Giant Spider | Venom Sac | Offal | Bitter | Replaces Rat Liver |
| Mushroom People | Cap | Fungus | Earthy, Umami | Great in stews |
| Mushroom People | Spore Sac | Spice | Earthy | Seasoning |
| Dragon | Heart | Magical | Spicy, Arcane | Legendary dish ingredient |

### 5.3 Recipes

- Recipes are discovered through NPCs, recipe scraps found in the Hollows, customer hints, and experimentation.
- *(v0.5)* Recipes may combine surface ingredients and ingredients from the Hollows: everyday dishes can be made entirely from surface ingredients, while special dishes need something from below (Section 5.5).
- Each recipe has required ingredient slots (by category or specific item) and optional slots that add flavor tags and bonuses.
- **Experimentation:** combining ingredients freely at the "Test Kitchen" can discover new recipes. Failed experiments produce funny "Questionable Stew".
- Dish score = base recipe value × ingredient quality × freshness × minigame performance. *(v0.3)* For a dish with several stages, minigame performance combines the results of its stages; exactly how is decided with the first multi-stage dish.
- *(v0.2)* Dish art comes from the Minifantasy food icon sets (*More Food Recipes* and others); dishes are named to fit the icons available.

### 5.4 Preparation Depth (added in v0.3)

Long-term design direction, not the scope of any current milestone.

Preparation complexity is one way to communicate progression and value, and to make rare monster parts feel precious when they come back from the Hollows.

| Dish tier | Preparation |
|---|---|
| Everyday / basic | Quick: usually one station |
| Better dishes | One additional meaningful ingredient or process |
| Rare / signature | Several distinct, satisfying stages |

The shapes the design should allow include `raw monster part → preparation → cooking → finishing/serving`, and dishes whose ingredients are each prepared differently before being combined. Possible stages include butchering or trimming a monster part, chopping, grilling, simmering or stewing, pouring or adding a component, combining prepared ingredients, and a final timing or finishing step. These are examples, not a crafting tree to design now.

**Every step must earn its existence.** A stage has to contribute at least one of: tactile fun, player skill, a meaningful choice, anticipation, risk and reward, stronger sensory feedback, a higher perceived value for the finished dish, story or worldbuilding, or interesting service logistics. No step exists only to make a recipe take longer. A rare dish feels special because the player performed an interesting process, not because they clicked through more menus.

**Reward in proportion.** The time and attention a premium dish takes is paid back through its value, the customer's response, Renown, relationships, special effects or other meaningful outcomes.

**Pacing.** Stages must not pile up until service pacing collapses. Staff, upgrades and mastery can take over familiar stages over time (Section 6.5).

**A depth priority** *(v0.4)*. Cooking minigames are a learning priority (Section 1.5). "Keep scope small" is not by itself an argument against more distinct preparation interactions, premium multi-stage dishes, ingredient-specific preparation, unusual monster-food mechanics or richer cooking feedback; each still has to be fun and earn its complexity, and more stages must make valuable food more interesting to prepare, never merely longer.

**Data.** Today each recipe names one cooking station (`RecipeDefinition.station`), and the Stew Pot already has two steps (chop, then simmer). That is the Stage 1 shape, not a limit: when the first multi-stage dish is designed, recipe data gains its stages, and how intermediate results are held (on the pass, carried, or inside one panel) is decided then.

### 5.5 Where Ingredients Come From (added in v0.5)

The ingredient economy distinguishes **surface sources** from **the Hollows**. The intended relationship: **surface activities provide dependable ingredients; the Hollows provide unusual ingredients and discoveries that make special dishes and stories possible.**

| | Surface and village | The Hollows |
|---|---|---|
| **Sources** | Farming, ranching, fishing, shops and trades, villagers | Harvesting monsters, rooms and rewards, bosses, quest locations |
| **Provides** | Many reliable, common ingredients: vegetables, herbs, grains, fruit, eggs, milk, fish, and other ordinary cooking ingredients | Monster ingredients, rare ingredients, magical ingredients, unusual ingredients, quest materials, rare customization objects, unique discoveries |
| **Feels like** | Planning, care, routine, attachment | Risk, discovery, surprise, stories to tell |
| **In the kitchen** | Keeps the everyday menu running | Makes special, signature and legendary dishes possible |

Two guardrails, held together:

- **Farming does not replace the Hollows.** If the surface can supply everything worth cooking, delving loses its purpose.
- **Not every basic recipe needs a dangerous ingredient from the Hollows.** If every stew needs a monster part, the delve becomes a grocery run and the surface loses its purpose.

Overlaps are fine where they create decisions (a farmed mushroom against a far better mushroom from the Hollows; a fish caught in the village pond against a cave eel). Exact recipes, prices and balances are not set; the Biome 1 recipe rework in 4f is still the next recipe work.

---

## 6. Tavern Gameplay

### 6.1 Service Phase (rewritten in v0.2)

The tavern is a **top-down room the player walks around**. Customers enter, path to a free table, sit, and order from the menu the player set at evening Prep. The player moves between stations to cook and pour, and **carries plates through the room** to the tables, avoiding people on the way. Staff help as they're unlocked. Service lasts a fixed time (currently 2.5 minutes, tuned in playtesting).

The cooking minigames stay as **screen panels** that open over the room when the player uses a station.

### 6.2 Minigames

Each station is a short, skill-based minigame. Staff can auto-complete stations at reduced quality so the player can focus on others. *(v0.3)* Most dishes use one station; rare and signature dishes may pass through several (Section 5.4).

| Station | Minigame | Skill |
|---|---|---|
| **Butcher Block** | Follow cut lines on a monster part; accuracy sets portion count | Precision |
| **Grill / Pan** | Flip at the right moment; watch a doneness meter | Timing |
| **Stew Pot** | Chop ingredients; the pot simmers on its own | Precision/management |
| **Oven** | Set heat and pull at the right time while multitasking | Timing |
| **Tap & Brew** | Pour ale/mead to the line with correct foam; mix cocktails and potions | Precision |
| **Plating** | Arrange garnish quickly for presentation bonus | Speed |
| **Serving** | Carry plates through the room; collisions fill a spill meter | Movement |
| **Bouncer** | Rowdy customers occasionally brawl; quick combat-lite minigame to throw them out | Reflex |

Additional minigames can be introduced over time (fermentation, bread proofing, spice grinding) to keep service fresh through the campaign. Each minigame's haptics are described in Section 9A.

### 6.3 Customers (rewritten in v0.5)

The long-term crowd is a mix of two populations, which makes service feel different from a fully procedural restaurant simulator.

| Population | Who | What they bring |
|---|---|---|
| **Named villagers** | Persistent authored residents, chosen each evening from the village population who are free to come (Section 2.8) | Recognition: the player knows them, their preferences may be known, dialogue and relationship context matter, and events can happen |
| **Visitors** | Generated outsiders (Section 2.8) | Variety, strangers, potential Inn guests (Section 6.8) and potential future residents (Section 6A.5) |

When a named villager walks in, the player recognizes them; what they order, what they say and how they react can draw on their relationship with Bram, their quests and what happened in the village that day. A villager's identity persists between village life and tavern visits. How villagers are chosen for an evening (schedules, mood, relationships, events) is designed with village life.

- **Types:** villagers and Visitors of many peoples: humans, dwarves, elves, orcs, goblins, halflings, skeletons and stranger folk; adventurers, merchants, pilgrims and travellers among the Visitors.
- **Preferences:** each race/type has favorite flavor tags and categories (e.g. dwarves love savory and strong ale; elves prefer herbs and fungus; orcs demand big meat portions). Named villagers also have personal preferences, which the player can learn.
- **Patience:** a timer; slow service lowers tips and reviews.
- **Special Guests:** named characters with unique requests that drive story, unlock recipes, or give quests. *(v0.3)* Requests that persist beyond one evening are quests (Section 2.6).
- *(v0.3)* **Recurring patrons:** some customers are named regulars who return, remember and change over time (Section 2.7). *(v0.5)* Under the village direction these are mainly the named villagers, plus promoted Visitors.
- **Reviews and Renown:** satisfied customers raise the tavern's Renown, which attracts better-paying clientele and unlocks story beats.
- *(v0.2)* Customers are built from the layered *A Myriad Of NPCs* characters, which gives a large variety of bodies, outfits and hair. *(v0.5)* This suits Visitor generation; named villagers may need their own authored looks.
- *(v0.5)* **Today's build:** the 4c customer system is entirely generated customers, which are effectively Visitors. It is not changed until village life is built; the evolution above is direction.

### 6.4 The Growing Property (rewritten in v0.5)

Tally Ho! grows from a small tavern into a larger, personalized **tavern, inn and home inside the village**. It expands, gains rooms, becomes more successful, houses more people and becomes the village's social centre. It does **not** grow into a fortress: the Stronghold direction and its stage table are dropped and kept in Appendix C.

Growth areas, none of them locked as stages or tied to particular acts yet:

| Area | Adds |
|---|---|
| The tavern | Kitchen, bar and dining room (Stage 1), then more seating, stations, a larger hall |
| The Inn | Guest rooms to unlock, furnish and offer to Visitors (Section 6.8) |
| The player's own space | Bram's quarters, possibly decorated (Section 6.6) |
| Storage and work rooms | Storeroom, cellar, brewing and preparation space |
| The grounds | Garden and farm plots, animal pens, perhaps a fishing spot (Section 6A) |

- **Areas** unlock through story and upgrades.
- **Furniture and decor** are placed freely inside unlocked areas *(v0.4: see Section 6.6; whether decor carries gameplay bonuses such as customer satisfaction is open)*.
- **No freeform construction** (placing walls and rooms) for now, but nothing should be designed in a way that rules it out later.
- *(v0.4)* Every area uses **the same customization architecture** as the Stage 1 tavern (Section 6.6). This is not a city-builder.
- Art: *Tavern Indoor*, *Towns*, *Towns 2*, *Crafting And Professions I/II* (kitchen, preparation table and other workbenches), *Farm*, *Builders*. *(v0.5: Castles And Strongholds no longer has an obvious use; village packs still need inspecting, `docs/ASSET_MAP.md`.)*

**Residents and staff.** *(v0.5, revised)* The people who live in the village are named villagers and recruited residents (Section 2.8); the people who work in the tavern are staff (Section 6.7). Whether recruited villagers can become employees, and whether Visitors are a natural recruitment source for staff, are open cross-system questions (Section 13). Selected residents carry personal questlines (Section 2.6) and relationship state (Section 2.7). *(The v0.2 refugee residents with roles such as gardener, smith and guard are in Appendix C.)*

**Morale and Cheer** *(v0.5, reinterpreted; to confirm)*. Morale is the state of the **village community** as a whole, driven by things like how well the village eats, helping villagers, settling newcomers and story events. High Morale grants **Cheer** in the Hollows: temporary buffs, extra revives, or the village's encouragement powering up the Kitchen Arts meter. The idea that people who know and care about Bram rally behind him survives the change of direction; what replaces "the stronghold" is the village. Morale stays separate from the tavern's Renown and from any one character's disposition (Section 2.7). **Flag:** with persistent relationships now central, a separate Morale value may overlap with the sum of the villagers' dispositions; whether Morale stays its own measure or is derived from the community's relationships is open (Section 13).

**Defense events: dropped** *(v0.5)*. Monsters breaching the surface to attack the tavern, and any tower-defense or Stronghold-defense mode, are no longer part of the design. (The Bouncer minigame, Section 6.2, is a service moment and is unaffected.)

### 6.5 Tavern Immersion (added in v0.3)

Pillar 6. One of the most important parts of Hearth & Hollows is the feeling of actually running this fantasy tavern and preparing strange monster cuisine. When two otherwise viable designs are on the table, the one that gives a stronger sense of presence, physicality and immersion in the tavern is generally preferred.

**Physical and grounded.** The important actions should feel physical: walking to stations, carrying plates, pouring drinks, cooking, preparing ingredients, dealing with patrons, managing a busy room, and seeing ingredients become recognizable finished dishes. Animation, audio, haptics, movement, station interactions, visual feedback and NPC behaviour all reinforce it. (An early example from 4c: the stove and cauldron can be worked from behind, like a cook at a range.)

**Immersion is not maximum manual labour.** The goal is the essential experience of preparing and serving food, not a literal simulation of every mundane action. When immersion and convenience conflict, ask what experience the interaction actually produces, and avoid:

- repetitive busywork;
- needless menu navigation;
- waiting with nothing meaningful to do;
- repeating low-skill actions the player has already mastered;
- realism that doesn't make a better fantasy;
- so many preparation stages that service pacing collapses.

If the more immersive option is clearly more repetitive, confusing, slow or frustrating, it is not chosen silently for realism; the tradeoff is flagged.

**Progression can lift repetition without removing the fantasy.** Staff, upgrades or mastery may eventually automate or simplify parts of familiar work while the player keeps personally doing the most interesting or valuable steps, so the tavern grows more capable without turning late-game play into the same chores forever. Staff auto-completing stations (Section 6.2) is the first form of this; the full progression is not designed yet.

**Feedback sells the fantasy.** Tactile feedback is part of the immersion. Chopping, flipping, sizzling near the burn threshold, pouring, reaching the right fill level, spilling, plating, serving and finishing a high-quality dish should all feel responsive and satisfying, through the combined visuals, sound and haptics of Section 9A. Feedback carries information and emotion at once: the player should often know they did well because they saw, heard and felt it, not only because a score appeared afterwards. In a multi-stage premium dish each stage builds anticipation, so finishing it feels proportionally rewarding.

**Familiar faces.** Recurring patrons and residents who remember Bram (Section 2.7) make the room feel lived in. *(v0.5)* Most of them are neighbours: named villagers who come in for the evening (Section 6.3).

### 6.6 Customization: the Tavern and Inn as the Player's Creation (added in v0.4, revised in v0.5)

Long-term design direction (pillar 7, and a learning priority in Section 1.5). The 4f milestone builds its foundation and proves one small dungeon-to-tavern reward loop in Biome 1 (Section 11.1); Phase 5 grows it.

**The essential experience.** "This is my tavern. I chose how it looks, I earned the strange things inside it, and the room itself tells the story of what I've done." *(v0.5)* And, for the Inn: "I decorated this room, someone interesting stayed here, and now I know them." Customization should create ownership, expression, visible progress, discovery, anticipation, meaningful choices, reward and storytelling, and give the cast something to react to (Section 2.7). It is not merely a level editor. The Stage 1 layout is only a starting arrangement.

**Decorate Mode.** Over time, and where the art and technology allow, the player can move, remove and add furniture; rearrange tables, chairs, counters and bar pieces; move functional stations; position decorative props; swap variants; rotate or flip where supported; recolour compatible pieces; buy furnishings with Gold; unlock new collections; find unusual furnishings in the Hollows; and keep the whole layout persistently. Controller-first, like the rest of the game.

**Data-driven furniture.** Customization is built on reusable furniture definitions and placed instances, not layouts baked into the Tavern scene, and never duplicate scenes per layout. A definition may describe a stable id, display name, sprites and variants, category, footprint, collision, navigation blocking, wall or floor placement, orientation and flip support, functional type and interaction, Gold price, rarity, unlock source, biome or theme tags, palette channels, and whether owning duplicates is meaningful. A placed instance may store the furniture id, position, orientation, variant, palette choices and any instance state. These are guidelines, not class names. Registering many furnishings should be tooling and data, not bespoke code per chair, barrel or rug.

**Functional furniture is decor too.** Where practical the Grill, Tap, Stew Pot, serving pass, tables, chairs, counters and later stations use the same placement architecture, so players change how their tavern *works*, not only where the paintings hang. Moving a functional piece keeps its station and service behaviour. Whether every functional station can move is open (Section 13).

**Freedom with understandable validation.** The tavern's walkable grid is rebuilt when the layout changes (already supported since 4c). Placement is generous; instead of many arbitrary restrictions, the game checks the layout against real service needs before the doors open and names the actual problem: "the Grill can't be reached", "the entrance is blocked", "2 seats can't be reached", "Orik can't reach the serving pass". The checks test gameplay constraints, not resemblance to the authored layout. Exact UX is open.

**Where furnishings come from.**

| Source | Role |
|---|---|
| **Gold** | Ordinary and common furnishings: serve customers → earn Gold → improve and personalize the tavern |
| **Discoveries from the Hollows** | Things that can't simply be bought: a second, emotional reward axis for delving beyond power and ingredients |
| **Bosses** | Rare or unique pieces that remember a victory |
| **Story, quests, relationships** | Special furnishings tied to events and characters (an NPC's storyline ending with an object of theirs) |

**Decor as dungeon loot.** Enemies can occasionally drop customization discoveries ("Oh! It dropped something new for my tavern"): furniture, decorative objects, wall decorations, rugs, lighting, bar and kitchen pieces, trophies, banners, monster-themed decor, palette or material unlocks, unusual variants and biome-specific pieces. Normal enemies have low chances of common or uncommon rewards; enemy type and biome shape the pool (dungeon inhabitants drop things from their own environment and culture). Not one or two scripted trophies: a real part of the reward ecosystem. Probabilities and tables are not set yet.

**Boss rewards.** Biome bosses can award rare or unique furnishings (a trophy, a distinctive piece of furniture, rare lighting, a banner, a statue, a unique bar or kitchen piece, a palette or material), so that "I beat that thing, and now part of my tavern tells that story." A guaranteed first-clear unique reward from major bosses, rather than pure chance, is the leading idea (not locked).

**Discoveries, extraction and death (open).** Discoveries should look and feel like dungeon loot but **never take Satchel slots**: the six-slot Satchel's job is ingredient pressure, not general inventory. The leading idea is a separate lightweight **curio** channel: the enemy drops the discovery visibly, the player picks it up, it belongs to the current run, extracting unlocks it permanently, and dying may lose it. That keeps extraction tension without stealing ingredient space. It is to be compared with simpler alternatives when the system is designed, and boss trophies may need different rules (losing a unique first-clear trophy may feel bad).

**Duplicates (open).** With a large catalog, random drops must not turn into frustration. Options to weigh, without adding a currency casually: exclude already-unlocked permanent discoveries from the roll, reroll duplicates, convert them to Gold, or allow duplicates only where owning several copies is useful (chairs, tables, barrels, candles, yes; a palette unlock or a unique boss trophy, no). Furniture data distinguishes the two.

**Recolouring.** More expressive than a few tint buttons, while keeping the Minifantasy look coherent: an authored **palette-swap / recolour-channel** approach where the art permits (channels such as wood, metal, cloth, upholstery, trim, banner, accent), with possible tools such as curated palettes, presets, copying colours from another object, apply-to-set, saved schemes, material or style variants, and eyedropper-like workflows. No unrestricted full-sprite RGB tinting that makes the art look broken. Prototype on a small group of real Minifantasy sprites before scaling; the technical approach is open.

**Content at scale.** As many suitable building, furniture and decoration options from the Minifantasy collection as reasonably practical; quantity and variety are wanted here. Candidates are found through the asset catalog CSVs, raw packs stay outside the repository, and only selected, player-usable sprites are imported, but "only selected" does not mean small: a large curated set is right when it is actually usable.

**Growing with the home** *(rewritten in v0.5)*. Decoration spans the tavern's dining and service spaces, the kitchen and service layout, the Inn's guest rooms (Section 6.8), possibly the player's own quarters, and later other unlockable parts of the property (Section 6.4). It grows into a larger personalized tavern, inn and home within the village, not a Stronghold. One architecture serves every area: guest rooms are areas with their own layouts in the same placement, validation, ownership and save systems, never a second decorating system. Dungeon furnishing drops remain as desirable as before. *(The v0.4 Sanctuary-and-Stronghold progression of this paragraph is in Appendix C.)*

### 6.7 Staff and Worker Customization (added in v0.4)

*(v0.5)* **A possible cross-system interaction, not locked:** the Visitor system (Section 2.8) and recruited residents may become a natural source of staff, so a Visitor the player met in the tavern could end up working in it. The rule is open (Section 13).

Character customization and attachment are worth practising, so future hired or recruited workers can carry real personalization: a player-entered name, appearance, clothing and clothing colours, accessories, job-related looks and other light touches. Canonical story characters keep their names (Section 2.5) and may allow visual customization where appropriate. Like the protagonist (Section 2.3), worker appearance is limited by the art's layers (A Myriad Of NPCs layers bodies, clothing and hair for idle, walk, damage and death).

### 6.8 The Inn (added in v0.5)

Long-term design direction; nothing here is in the current milestone's scope.

Tally Ho! eventually contains an **Inn**: a guest-room area alongside the tavern. The essential experience is: **"I decorated this room, someone interesting stayed here, and now I know them."**

- The player can unlock guest rooms, furnish and decorate them, customize their appearance, and invite or offer rooms to suitable Visitors.
- Guest rooms reuse **the same furniture, placement and customization architecture** as the tavern (Section 6.6): a guest room is another area with its own layout, saved the same way. There is no second, unrelated decorating system.
- Room furnishing and style should eventually be able to affect attractiveness, Visitor interest, reactions, quests and other inn interactions. **No numerical hotel-management systems are locked:** occupancy, pricing, room ratings and similar rules are open (Section 13) and are prototyped against the essential experience, which is about people, not yield.
- The Inn is the main bridge from transient Visitor to persistent character: a Visitor who stays is a candidate for promotion to a persistent identity (Section 2.8) and, later, for settling in the village (Section 6A.5).

---

## 6A. Village Life (added in v0.5)

Long-term design direction. None of these activities is built in 4d or any current milestone; Section 11.1 says when each is first prototyped. The aim throughout is the essential experience each activity contributes, not a copy of another game's systems: nothing is added only because *Stardew Valley* has it.

### 6A.1 Daytime

When the player wakes, the day is theirs (Section 3.1): around the tavern and inn (decorating, rearranging, managing guest rooms, sorting ingredients and the storeroom), on the grounds (farming, ranching), around the village (talking to villagers, relationship moments, quests and errands, shopping, exploring, fishing), and preparing for the evening or the night's delve. Daytime is where most dialogue, relationship and NPC comedy happens outside service. How time passes in the daytime is open (Section 3.4).

### 6A.2 Farming

A future major daytime activity whose essential purpose is: **the player can grow part of the tavern's food supply themselves.** Farming mainly provides reliable ingredients: vegetables, herbs, grains, fruit, mushrooms where they fit, and other ordinary cooking ingredients. It should eventually interact with recipes, the tavern menu, the economy, villagers' requests and relationships. The crop, growth-time and season system, and the farm's size, are open (Section 13); no *Stardew* mechanic is copied automatically.

### 6A.3 Ranching

Small-scale animal keeping: another daytime ingredient source, kept to the cozy fantasy tone. Outputs can include eggs, milk, wool where useful, fantasy equivalents and other animal products. Ranching exists for ingredient production, player attachment to the animals, and another life-sim activity. It is not a large livestock simulation; how deep animal simulation goes is open (Section 13).

### 6A.4 Fishing

A daytime activity and ingredient source. Fish support cooking, villagers' requests, collection and discovery, and rare catches. Fishing should eventually get an interactive minigame in the spirit of the cooking stations' tactile feel (Section 9A); the minigame itself is open (Section 13).

### 6A.5 Visitors Becoming Villagers

The village initially has about **three empty residential plots** (a current design target, not necessarily permanent). Some Visitors who stay at the Inn may eventually become candidates to settle in one. A possible loop:

**Visitor comes to the tavern → the player gets to know them → the Visitor stays at the Inn → their requests and the relationship develop → the player helps them → the Visitor becomes willing to move into an empty plot.**

This gives the player influence over part of the village's population. The three plots are **fixed building and resident sites**, not a freeform city-building system, which keeps the feature about characters rather than settlement simulation. Once settled, a resident becomes a persistent villager (Section 2.8). Eligibility, recruitment rules and how a plot is built on are open (Section 13).

---

## 7. Progression and Economy

### 7.1 Currencies

| Currency | Earned From | Spent On |
|---|---|---|
| **Gold** | Service, selling surplus ingredients, gold rooms, *(v0.5)* surplus produce and errands where they fit | Gear, tavern upgrades, recipes, staff wages, *(v0.4)* furnishings, *(v0.5)* seeds, animals, supplies and village shops |
| **Renown** | Customer satisfaction, story | Unlocks tiers of customers, story progress (not spent). *(First tangible use in 4f; story and dialogue conditions possibly in 4g.)* |
| ~~**Delve Marks**~~ | *(removed in the 4e plan, 2026-10-05: a second permanent currency beside Gold and tavern upgrades)* | |
| ~~**Relics**~~ | *(removed for now in the 4e plan, 2026-10-05: run powers and upgrades cover abilities, boss trophies (4f) give bosses their permanent reward)* | |

### 7.2 Upgrade Tracks

- **Combat:** weapon blueprints (added to drop pools), armor, satchel size, preservation tools, Essence Tonics *(deferred in the 4e plan: revisited only if the run needs another recovery tool after the boss playtest)*.
- **Run power-ups:** *(v0.2)* temporary boons chosen one-of-three in power-up rooms; they last for the run.
- ~~**Relics:**~~ permanent abilities that open shortcuts and hidden rooms. *(v0.2: no longer platforming abilities such as double jump.)* *(Removed for now in the 4e plan, 2026-10-05.)*
- **Tavern:** stations, furniture, seating capacity, decor, new areas. *(v0.4)* Furnishings are bought with Gold or discovered (Section 6.6).
- **Property and life** *(v0.5)*: Inn rooms, farming, ranching and fishing capability (tools, plots, animals, gear). Not designed yet.
- **Staff:** hire and train residents; staff skill levels affect auto-complete quality.

### 7.3 Economy Balance Goals

- A good delve should fund roughly one meaningful upgrade.
- Selling raw ingredients should be viable but noticeably worse than cooking them.
- Staff wages and running the property create light pressure without becoming a punishing survival mechanic. *(v0.5: "refugee upkeep" belonged to the Stronghold direction.)*
- *(v0.5)* Surface ingredients and those from the Hollows both pay their way (Section 5.5): surface food keeps everyday service viable; ingredients from the Hollows are worth noticeably more.

### 7.4 Progression Philosophy (added in v0.5)

The player should increasingly feel: **"This is my tavern, my inn, my village community, my menu, and the people here know me."**

Progress should be visible through property customization, farming, ranching and fishing capability, recipes, villagers, recruited residents, relationships, Inn rooms, discoveries from the Hollows, equipment and story. Progression should **not** be primarily a sequence of numeric stat upgrades: upgrades that change numbers (Max Essence, drain rate, satchel size) still exist, but the progress the player notices and remembers is people, places and things.

---

## 8. Art and Audio Direction

### 8.1 Visual Style (rewritten in v0.2)

All art is **Minifantasy** by Krishna Palacio: tiny top-down pixel art on an 8×8 grid.

- **Resolution:** 320×180 reference at 8 pixels per unit; 1 world unit = 1 tile = 8 px. Pixel Perfect Camera with integer zoom and smooth scrolling (the view is not snapped to the art-pixel grid, so the camera and characters move in screen pixels). Resolution locked after the 4a look test; smooth scrolling chosen in 4b (2026-10-02).
- **Characters:** 32×32 frames with a body of about 8×8, four diagonal facings.
- **Sorting:** sprites sort by Y position, with pivots at the feet.
- **Palette and lighting:** warm, saturated tavern (amber candlelight, wood, hearth) against the cool, eerie Hollows (teal, violet, bioluminescence). Sprites are lit with URP 2D lights: this is the visual baseline. Each environment has an ambient light plus local lights (hearth and candles in the tavern; torches, and later bioluminescence, in the Hollows), always keeping characters, enemies and pickups readable. *(v0.5)* The daytime village adds a third look (open-air daylight, still warm), which the lighting baseline will need when village life is built.
- **Food** should look appetizing even at this scale; dishes use the Minifantasy food icons, shown enlarged in menus and results.
- **Content adapts to the art:** monsters, ingredients, dishes, stations, NPCs and bosses are chosen from what Minifantasy contains (`docs/ASSET_MAP.md`). *(v0.5)* So are the village, villagers' peoples, crops, animals and fish; those packs have not been inspected yet.
- **Known gaps:** no rat with an attack, no mallet or frying pan weapon, no plate-carrying overlay, and no audio. *(v0.3)* The body font is Silver (Section 8.2).
- *(v0.3)* **Portraits:** important NPCs get dialogue portraits made with the **Minifantasy Portrait Generator** by Krishna Palacio, so they belong with the rest of the art. The usual Minifantasy rules apply: raw files stay outside the repo, the catalog is searched first, only what is used is imported, and the workflow and choices are recorded in `docs/ASSET_MAP.md`. Nothing is imported yet (4g).

### 8.2 UI (rewritten in v0.2)

Rustic fantasy UI built with uGUI, **Super Text Mesh** for all text, and Minifantasy UI sprites (*User Interface*, *UI Overhaul*: panels, speech bubbles, emotion icons, controller glyphs). Readable during fast combat, with a minimal HUD during a delve. All text is localized.

*(v0.3)* **Text:** the body font is **Silver**, a pixel font drawn at the game's own pixel size so it sits with the Minifantasy art, with wide language coverage (our copy has plain single-pixel punctuation, *v0.4*); a decorative title font may follow in the menus and polish milestone (4i). English is written in a lower-case style ("open the doors", "cellar stew", "last orders!"), with proper nouns, resource names (Essence, Renown) and control labels capitalised; gold is the exception and stays lower case (2026-10-05); the style lives in the written strings, and other languages follow their own conventions.

*(v0.3)* Dialogue (4g) is presented in uGUI + Super Text Mesh through Dialogue System. Portraits are data-driven: character and NPC data reference a portrait, and the presenter reads it from there, never from a portrait hard-coded into a particular dialogue screen.

### 8.3 Audio

- **Tavern:** folk instrumentation (fiddle, lute, accordion, bodhrán); music gains layers as the tavern grows and more residents join in.
- *(v0.5)* **Village:** a gentler daytime theme in the same folk palette.
- **Dungeon:** darker, percussive, biome-specific themes that intensify in combat.
- **SFX:** chunky, satisfying combat impacts; sizzling, chopping, pouring, and crowd chatter in the tavern.
- **Voice:** grunts and barks ("Hmm!", "Aye!") rather than full voice acting, for scope.
- *(v0.2)* No audio source exists yet. Generated placeholder sounds are used until real SFX and music are sourced.

---

## 9. Controls (rewritten in v0.2)

| Action | Gamepad | Keyboard and mouse |
|---|---|---|
| Move | Left stick | W A S D |
| Aim | Movement direction | Mouse |
| Light attack (combo) | X / Square | Left mouse |
| Heavy / charged attack (hold) | Y / Triangle | Right mouse |
| Dodge roll | B / Circle | Space |
| Interact / pick up / use station / serve | A / Cross | E |
| Skills 1 and 2 | LB / RB | 1 / 2 |
| Kitchen Arts special *(deferred, 4e plan)* | RT | Q |
| Harvest Finisher | LT | F |
| Pause | Start | Esc |

In the tavern the same character controls apply (move, interact); attacks are disabled. Minigames use their own actions (minigame action on X / left mouse, alternate on Y, cancel on B / Esc).

All controls are remappable via the Unity Input System.

## 9A. Haptics (new in v0.2)

Haptics are a core part of game feel, designed in from the start.

**Principles**

- **A vocabulary of named patterns.** Gameplay triggers named patterns, never raw motor values. Patterns are data (ScriptableObjects).
- **Authored with visuals and sound.** Each important moment has one combined feedback containing all three.
- **Pure, tested mappings.** Any mapping from a gameplay value to intensity (e.g. pour speed → rumble strength) is plain logic with unit tests.
- **Player control.** Vibration on/off and an intensity slider apply globally, plus a reduced-intensity accessibility option. Vibration defaults to on when a supported controller is connected.
- **Graceful degradation.** Where rumble is unsupported (no controller, web builds, some controllers), haptics do nothing and nothing else changes.

**Starting vocabulary** (a gamepad has a low motor for heavy thuds and a high motor for light buzz)

| Pattern | Shape | Used for |
|---|---|---|
| `Tap.Light` | High, very short | Grill flip, UI confirm, pickup |
| `Tap.Firm` | Both, short | Light hit landed, clean cut |
| `Hit.Heavy` | Low-dominant, medium, quick decay | Heavy/charged hit, enemy slam |
| `Hit.Taken` | Low, sharp, short tail | Player damaged |
| `Kill.Clean` | Firm tap, then a rising high tick | Clean-kill cue |
| `Finisher.Harvest` | Low build-up, pause, strong double pulse | Harvest Finisher |
| `Pulse.Success` | Two rising high pulses | Perfect flip, perfect pour |
| `Buzz.Failure` | Rough low buzz | Overflow, burnt, dropped plate |
| `Cue.Threshold` | Single crisp high tick | Foam reaches the line |
| `Bump.Soft` / `Bump.Hard` | Low, short; strength by spill meter | Serving collisions |
| `Cut.Ragged` | Two uneven low ticks | Butcher Block miss |
| `Heartbeat.Warning` | Low double-beat, looping, rate rises | Low Essence |
| `Boss.Telegraph` / `Boss.PhaseChange` | Slow low swell / long rumble with a peak | Boss attacks and phases |
| `Rumble.Continuous` | Level set each frame, 0–1 | Pour speed, grill nearing burn |

**Cooking minigames**

- **Grill:** a tap on each flip; a rising rumble as doneness nears the burn zone; a success pulse for a perfect flip.
- **Tap:** a continuous rumble that scales with pour speed; a distinct cue as foam reaches the line; a failure buzz on overflow.
- **Serving:** a bump pulse on collisions, intensifying as the spill meter fills.
- **Butcher Block:** a cue for each cut, with clean cuts feeling different from ragged ones.

**Dungeon**

- Light hits versus heavy hits.
- A distinct, satisfying clean-kill cue.
- The Harvest Finisher.
- Damage taken.
- A warning heartbeat at low Essence.
- Boss attacks and phase changes.

**Platform support** (expected; to be tested on hardware): Xbox controllers on PC; DualShock 4 and DualSense over USB; no rumble for Switch Pro on PC unless remapped by Steam Input; no rumble in web builds.

---

## 10. Technical Design (rewritten in v0.2)

Unity 6.6 now, moving to 6.7 LTS when it is released and staying there through launch.

### 10.1 Engine Configuration and Third-Party Assets

- **Render Pipeline:** URP with the 2D Renderer and 2D lights; sprites and tilemaps use the lit sprite material. Pixel Perfect Camera at 320×180, 8 PPU, smooth scrolling (no grid snapping). Custom transparency sort axis (0, 1, 0).
- **TopDown Engine 5.0 (TDE):** character controller, abilities (movement, dash, weapons), combat, enemy AI, camera and rooms. It replaces the custom kinematic controller.
- **MMFeedbacks / MMTools** (bundled with TDE): all game feel. There is only one copy; Feel's copies are never imported.
- **Nice Vibrations** (from Feel): haptics.
- **Super Text Mesh (STM):** all player-facing text, in uGUI and world space, with its Ultra shader under URP.
- **Input:** Input System with separate action maps (`Dungeon`, `Tavern`, `UI`, `Minigame`) and runtime rebinding. TDE reads input through a subclass of its `InputSystemManager` that maps our Dungeon and Tavern maps onto TDE's buttons.
- **Camera:** Cinemachine 6.6 (TDE's Cinemachine 3 code path), room confiners, impulse-based shake.
- **UI:** uGUI + STM + Minifantasy UI sprites. UI Toolkit is no longer used.
- **Animation:** sprite-sheet animation through our own **SpriteSet** path, not Mecanim. Each character has a `SpriteAnimationSet` asset (per action: frames for the four drawn facings, a frame duration and a loop flag), generated from the Minifantasy sheets and their frame-duration guides. `CharacterSpriteAnimator` picks the action and facing from the TDE character's state and shows the frame. We deliberately don't use Animator Controllers or AnimationClips: four facings per action would mean 24 or more states per character, and TDE's animator parameters go unused. The animator is **presentation only**: TDE and our gameplay code stay authoritative for attack timing, damage, the dodge and its i-frames, and death, and the animator only reflects that state. It never drives gameplay, and gameplay never waits on an animation.
- **Physics:** Physics 2D with no gravity.
- **Pathfinding:** our own grid A* (TDE has none for 2D).
- **Content Loading:** Addressables for biome assets and room prefabs when room loading is built; until then only Localization uses it.
- **Localization:** Unity Localization package; every player-facing string comes from a string table.
- **Story, dialogue and quests:** Dialogue System for Unity 2.2.74, Quest Machine 1.2.74 and Love/Hate 1.10.74.1 (Pixel Crushers; imported in 4g), presented through uGUI + Super Text Mesh, with Minifantasy Portrait Generator portraits; Love/Hate for relationships. *(Locked 2026-10-03, replacing Yarn Spinner entirely; there is no second dialogue system.)*

Versions, licenses and vendor rules are in `docs/THIRD_PARTY.md` and `CLAUDE.md`.

### 10.2 Scene Structure

- `Boot` — initializes services (save, audio, input, localization, haptics) and persists.
- `MainMenu`
- `Tavern` — hub scene for Prep, Service, and Night phases. *(v0.5: the tavern and its Inn areas.)*
- *(v0.5, future)* The village: how it is split into scenes (one scene, or areas loaded additively) is decided when village life is planned.
- `Dungeon` — single scene into which biome rooms are loaded.
- `Cutscene` scenes as needed (or Timeline sequences inside Tavern).

Additive scene loading keeps the persistent `Boot` services alive.

### 10.3 Architecture Overview

- **Data-driven design with ScriptableObjects:** `IngredientDefinition`, `RecipeDefinition`, `EnemyDefinition`, `WeaponDefinition`, `CustomerProfile`, `BiomeDefinition`, `RoomDefinition`, `TavernUpgradeDefinition`, plus haptic patterns. *(v0.4)* Furniture definitions join them in 4f (Section 6.6).
- *(v0.4)* **Rewards are open-ended.** Run and room rewards, enemy drops and boss rewards are designed so a new reward kind (persistent customization discoveries) can be added without rewriting them: rewards are not assumed to be only ingredients, Gold, Delve Marks, weapons or run power.
- **Pure logic in plain C#** with EditMode tests: Essence, harvest rules, inventory, freshness, recipes, economy, service session, customer order and patience logic, staff, game flow, saving, pathfinding, haptic intensity mapping. TDE-dependent behaviour gets PlayMode tests.
- **Game flow:** `GameFlow` drives phases and scene transitions. *(v0.5)* Today it runs the 4c order (Morning → Delve → Evening service → Night → Sleep); the target order is Daytime → Evening (Prep, Service, Results) → Delve → Sleep (Section 3.1), adopted at the GameFlow integration step. The daytime's time model is open (Section 3.4).
- **Event bus:** a lightweight event bus decouples systems. TDE and MoreMountains events are **bridged onto our bus at the boundary** rather than used throughout our code.
- **Characters:** the player, enemies, customers and staff are TDE characters. Our behaviour is added through subclasses, composition and TDE abilities in our own assemblies; vendor code is never modified.
- **Essence and health:** Essence is the only health pool, implemented as a subclass of TDE's `Health` backed by `EssenceMeter`.
- **Combat:** TDE weapons and damage areas, with our damage pipeline (element, overkill) and harvest rules reading the kill context.
- **Minigames:** each station implements `IMinigame` (Begin, Tick, Evaluate → score 0–1), so staff can auto-resolve any station.
- **Customer AI:** a state machine (Enter, Queue, Seat, Order, Wait, Eat, Pay, Leave) with a patience timer and preference scoring; movement follows A* paths to tables.
- **Feedbacks:** one `MMF_Player` per important moment, containing visuals, sound and a named haptic pattern. All intensities respect the player's settings.
- *(v0.3)* **Story middleware boundary.** The Pixel Crushers packages must not become a second gameplay architecture.
  - **Dialogue System:** conversations, branching dialogue, dialogue conditions and story variables, contextual barks, dialogue presentation.
  - **Quest Machine:** persistent quest and objective state.
  - **Love/Hate** (4g): Affinity and Respect of selected characters, deed evaluation and memories.
  - **Hearthdelve:** inventory, ingredients, recipes, combat, harvesting, Essence, economy, Renown, Morale, progression, day flow, tavern service, upgrades and all other core gameplay, plus the authoritative save. *(v0.5)* This includes village life: villager schedules and presence, Visitor generation and promotion, the Inn, resident plots, farming, ranching and fishing.

  They are reached only through Hearthdelve-owned adapters and bridges at the event bus boundary, the same way TDE events are. Gameplay systems publish events; adapters turn them into quest progress, dialogue variables and relationship changes, and expose game state to dialogue conditions. Dungeon, Tavern, combat and inventory code never call Pixel Crushers APIs, pure logic never depends on Pixel Crushers packages, and Dungeon and Tavern still never reference each other. Which assembly holds the adapters is decided in the 4g plan.

### 10.4 Key Systems

| System | Responsibility |
|---|---|
| `HarvestSystem` | Determines drops from kill context (weapon type, element, overkill) |
| `InventorySystem` | Satchel, storeroom, freshness decay, preservation modifiers |
| `RecipeSystem` | Recipe matching, experimentation, dish scoring |
| `ServiceSystem` | Customer spawning, orders, timers, payment, reviews |
| `EconomySystem` | Currencies, prices, wages |
| `ProgressionSystem` | Unlocks, relics, tavern stages, story flags |
| `StoryManager` | Act progression; the Hearthdelve-owned bridge between gameplay events and Dialogue System for Unity and Quest Machine (4g) |
| Relationships *(4g)* | Affinity, Respect and memories of selected characters behind `RelationshipAdapter` (Love/Hate), with a social stand-in per tracked character in Boot |
| `SaveSystem` | Versioned JSON of persistent state; autosave at Night phase *(v0.5: moving to sleep at the end of the day)*. The only authoritative save, including middleware state through adapters |
| `LevelGenerator` | Builds dungeon floors from room graphs |
| `HapticService` | Plays named haptic patterns, applies settings, checks device support |
| `GridPathfinder` | A* on the room's tile grid for customers and enemies |
| Customization *(planned, 4f)* | Furniture definitions, placed layouts per area (tavern areas and, later, Inn guest rooms), Decorate Mode, layout validation against service needs, palettes; owned and unlocked furnishings |
| Village life *(planned, v0.5)* | Named villagers and their presence and schedules; Visitor generation, promotion to persistent identities and settlement; the Inn's guests; farming, ranching and fishing. Split into systems when each is designed |

### 10.5 Procedural Level Generation

Designer-authored **room prefabs** chosen by a **graph-based generator**, played one room at a time.

1. Each biome defines a floor template graph (entrance, combat rooms, reward rooms, campfire, shop, extraction, boss).
2. The generator picks a room prefab for each node by type and door layout, and assigns each room a reward.
3. Each exit door shows the reward of the room it leads to.
4. Enemies and loot spawn from weighted tables per biome and depth.
5. Seeds are stored for debugging and potential daily-challenge modes.

### 10.6 Save Data

Persistent: tavern and property upgrades, placed furniture *(v0.4: each area's layout as placed instances with their variants and palettes, plus owned and unlocked furnishings)*, unlocked weapons/relics/recipes, storeroom inventory, currencies, residents, story flags, settings. *(v0.5)* Later: the named villagers' state, promoted Visitors' generated identities (from the moment of promotion), recruited residents, Inn rooms, and farm, ranch and fishing state. Ordinary transient Visitors are **not** saved. The day's autosave moves to sleeping (Section 3.1). Run state is saved only at biome transitions to prevent save-scumming (optionally allow a "suspend run" save).

*(v0.3)* `SaveSystem` stays the authoritative save. When the Pixel Crushers systems arrive, their persistent state joins its save/load lifecycle through adapters: Dialogue System story state as required, Quest Machine quest and objective state, and Love/Hate relationship state when added. An adapter may use the middleware's own serialization internally, but the data lives in Hearthdelve's save file and follows its versioning; there is no separate player-save path.

### 10.7 Project Folder Structure

```
Assets/
  _Project/
    Art/            (our own edits and placeholders)
    Audio/          (Music, SFX)
    Data/           (Ingredients, Recipes, Enemies, Weapons, Biomes, Customers, Haptics)
    Dialogue/       (Dialogue System and Quest Machine databases, 4g)
    Prefabs/        (Player, Enemies, Rooms, Tavern, UI)
    Scenes/
    Scripts/
      Core/         (Events, Services, Input, Pathfinding)
      Dungeon/      (Combat, Enemies, Harvest, Essence, Run)
      Tavern/       (Service, Customers, Minigames, Staff)
      Shared/       (Game flow, Inventory, Economy, Progression, Save)
      UI/
      Editor/
    Settings/       (Input actions)
    Localization/
    Tests/
  ThirdParty/
    Minifantasy/    (imported packs, only what we need)
  TopDownEngine/    (vendor, default folder)
  Clavian/          (Super Text Mesh, default folder)
  Feel/             (Nice Vibrations only, default folder)
```

---

## 11. Scope and Milestones

### 11.1 Development Phases

| Phase | Goal | Contents |
|---|---|---|
| **1. Prototype: Combat** | Prove the dungeon feels good | Done as a side-scroller (see tag `v0-sidescroller-prototype`) |
| **2. Prototype: Tavern** | Prove service is fun | Done as a side-scroller |
| **3. Loop Prototype** | Prove the halves connect | Done as a side-scroller |
| **4. Vertical Slice** | Represent final quality, top-down; *(v0.5)* prove the new identity in miniature | Biome 1 fully arted + boss, Stage 1 tavern polished with its customization foundation, Act I opening story; *(v0.5, approved 2026-10-05)* one full day of the new loop: a free daytime in a small part of Kariaston with a few named villagers and one small farm plot, an evening service where villagers and Visitors eat, a Biome 1 delve at night, then sleep |
| **5. Production** | *(v0.5, approved 2026-10-05)* Prove the remaining life-sim systems, then build out content | First the Inn's guests and Visitor promotion, settling residents on the three plots, fishing, ranching and farming depth, each prototyped against its essential experience; then Biomes 2–7, all minigames, the growing property, the revised full story, more villagers and relationship content, Love/Hate if not earlier; *(v0.4)* the large furnishing catalog, many enemy- and biome-specific drop pools, more boss trophies and rewards, rarity tuning, customization of the whole property (tavern, Inn, own quarters), advanced customization content, worker customization |
| **6. Polish and Launch** | Ship | Balance, accessibility, localization, performance, platform certification |

**Phase 4 sub-milestones** *(v0.2; revised in v0.5, approved 2026-10-05)*. Each is planned, approved, built and playtested separately; the web build works at the end of each. The v0.5 revision keeps 4d, 4e, 4f and 4g, inserts a village and daytime slice as 4h, and moves menus and polish to 4i. The slice proves that the new identity works (a day with people, a garden, a tavern and a delve) without building ranching, fishing, Inn guests or resident recruitment, which go to Phase 5.

- **4a Integration and look test:** project swap, port manifest, logic and tests ported, Nice Vibrations, Minifantasy import pipeline, one dungeon room and one tavern corner with real art, `docs/ASSET_MAP.md`.
- **4b Dungeon migration:** Phase 1 and 3 dungeon gameplay rebuilt on TDE, with the dungeon haptics.
- **4c Tavern and UI migration:** top-down tavern, customer pathing, 2D serving, Grill/Tap/Serving with haptics, all UI in uGUI + STM.
- **4d Biome 1 runs:** room-by-room structure, room rewards, run power-ups, 3 floors plus a boss arena. *(v0.4)* The reward architecture leaves room for future persistent reward kinds (customization discoveries) without building any. *(v0.5)* Scope unchanged and the completed step 1 and 2 work stands. Step 3's rewards are designed knowing a larger ingredient ecosystem exists: ingredient rewards don't assume monster parts are the only ingredients, and the reward model stays open to dungeon ingredients, quest objects, customization discoveries and future weapons and currencies (ingredient and Gold rewards remain the prototype's focus). No farming, fishing or ranching in 4d. Step 5 (day-loop integration) is where the run joins `GameFlow`; **proposed:** it adopts the new order there (evening service → delve → sleep, with the existing Morning panel standing in for the daytime until 4h), so the integration isn't done twice. How the Night upgrade screen and the breakfast buff are rehomed is decided when step 5 is planned. *(4d step 5, 2026-10-04: adopted. The old morning panel is the daytime placeholder; the breakfast became the **delve meal**, cooked in the daytime and kept until that night's delve; the Night screen keeps the upgrades, after the delve.)* *(4d playtest, 2026-10-05: a new game opens with the first night's delve, since the storeroom starts empty; base Essence is tuned so a full run of the Cellars needs some gear.)* *(Complete, approved 2026-10-05.)*
- **4e Combat depth and boss:** *(revised in the 4e plan, 2026-10-05)* **the Larder Troll** (the Cellars boss), the **Harvest Finisher** (a small version, and an optional finishing moment on the defeated boss), the boss's rewards (Gold, a Premium larder cache, a persistent first-clear record, a hook for 4f's trophy), and tuning the cleaver and the Cellars' enemies for it. Deferred: more weapons (until 4g's protagonist decision), Kitchen Arts, field cooking, Essence Tonics. Removed: weapon rarity and affixes, Delve Marks, relics. *(Originally: Harvest Finisher, Kitchen Arts, 3–4 more weapons with rarity and affixes, Essence Tonics, field cooking, Delve Marks and the Delver's Board, one relic, the Biome 1 boss.)* *(v0.4)* Boss rewards are designed so a unique boss furnishing can plug in later. *(v0.5)* Unchanged: the roadmap review found no reason to move it. *(Complete, signed off 2026-10-05; final values)* The Larder Troll: 900 health, a slam (26) and a charge (24) with floor telegraphs and a 1.6 s wall stun; it eats any ingredient lying in the arena (including parts swapped out of the satchel) for 8% of its health, spoiled by 40 damage; a frenzy at half health (roar, swell, hit-stop, shake, rumble and "rages!" on the bar besides the tint); the full ~1.6 s reveal until first beaten (the save's boss-clear record), then a ~0.85 s intro. Passive drain paused in the fight; a full Essence refill when it falls; 120 run Gold and a Premium larder cache; the result screen says it was felled. Campfires (each with a prompt): a quarter of max Essence by each floor's hole down and before the arena. Harvest Finisher: about 25% health or 15 or less, within 1.2 s of a hit, 1.8 tiles; the troll's optional 3 s finishing window. The cleaver's heavy 26 / 40. Aim is independent of movement (mouse, or the right stick), the dodge rolls through enemies with a 0.5 s cooldown.
- **4f Tavern Stage 1 content:** Butcher Block, all Biome 1 recipes, customer requests, Orik and Boog, and *(v0.4)* **a real customization foundation**: Decorate Mode (move, add and remove furnishings, functional furniture where feasible), persistent layouts, Gold purchases, nav rebuild and service-layout validation, the furniture definition and data pipeline, a substantial curated catalog from Minifantasy (enough that players make visibly different taverns, not a token handful), one proven recolouring workflow, and controller-first decorating UX. Not every possible furnishing: the pipeline and a substantial first collection, with more added through Phase 5. Once that foundation exists, 4f also proves **one small end-to-end reward loop in Biome 1**: fight → a furnishing discovery drops → pick it up → extract → it is permanently owned → place it through Decorate Mode. That means a small real furnishing drop pool on suitable Biome 1 enemies, at least one rare or unique furnishing from the Biome 1 boss, persistent ownership and unlock state, visible pickup and reward feedback, and extraction and death behaviour under whichever rule is approved when the feature is designed. It is an integration slice, not the production loot catalog: it proves that finding strange things in the Hollows and bringing them home is fun. *(v0.5, approved 2026-10-05)* The customization foundation is built for **several areas from the start** and proves it with **one small guest room** as a second decoratable area with its own saved layout (no guests or Inn rules yet), so the Inn never needs a second decorating system. The Biome 1 recipe rework follows the surface and dungeon ingredient model (Section 5.5): a few everyday surface staples, bought until farming exists, alongside the dungeon's parts. *(4e sign-off, 2026-10-05)* **Renown gets its first tangible gameplay use here**, ideally a small tavern or customer-content unlock (until now it is only earned, saved and shown). *(4f complete, approved and signed off 2026-10-06.)* Four checkpoints (`docs/PLAN_4F.md` is the approved plan as a historical record; `docs/PROGRESS.md` records what was built and every deviation). **A:** furniture as data in placed layouts, Decorate Mode (whole tiles, the free-placement key for pixel nudges, real quarter turns, flips), nav rebuild and service validation. **B:** the catalog (about a hundred pieces from Minifantasy, generated from `catalog.json`), Gold purchases into property-wide storage, four Renown tiers (0, 25, 60, 100; Renown is earned, never spent), palette-ramp recolouring and area finishes, and the guest room (a second property area up the corner stairs). **C:** furnishing discoveries (a curio channel, kept on extraction, lost on death), the Larder Troll's tusks (granted with the victory, never lost, with a homecoming in Decorate Mode), the Kariaston market (`SupplySource`) and five surface staples, a 13-dish Cellars menu in three tiers, mushroom forage, the Butcher Block (a Prep-time precision cut whose score sets the yield), Boog and Orik. **D:** special customer requests, a whole-loop balance pass with a balance report tool, a clean trophy spot on the back wall, and the event facts 4g will consume. Save version 7.
- **4g Story, quests and character creation:** Dialogue System for Unity and Quest Machine integration; uGUI + Super Text Mesh dialogue presentation; Minifantasy Portrait Generator NPC portraits; character creation; the Act I opening; onboarding and tutorial flow; the first story quests and objectives; one representative NPC quest integration; save/load of dialogue and quest state; *(4g plan, approved 2026-10-06)* **Love/Hate** in 4g for relationship reactivity (Affinity and Respect). *(v0.5, approved 2026-10-05; revised in the 4g plan)* The representative NPC quest returns a **quest object** (the first non-ingredient, non-Gold reward kind in real use): **Boog's Bomb**, Boog's favorite bomb gone missing somewhere in the Hollows (villagers arrive in 4h), and the dialogue adapters are built knowing villagers, Visitors and generated residents will use them. Act I is written for the village direction. *(4e sign-off)* 4g may also use Renown in story and dialogue conditions. *(2026-10-07, proposed; folded into 4h at 4g's sign-off, 2026-10-06)* A **Checkpoint D** would have brought the new cast (Section 2.10) into 4g in the smallest way that needs no village: every new person as a `CharacterDefinition` with values and a generated faction; **Gimp**, the first Hollower, coming up the hatch on some nights after Boog's Bomb to talk explosives with Boog; one **Glimmer** glimpse in the Cellars; and Kariaston's people named in Boog's and Orik's talk (`docs/PLAN_4G.md`, "Proposed: Checkpoint D").
- **4h Village and daytime slice** *(v0.5, new; approved 2026-10-05)*: a small part of Kariaston and the tavern's grounds, walkable in the daytime, replacing the Morning panel; a prototype of the daytime time model (a ticking clock or player-controlled phases, chosen by playtesting both cheaply); 3–4 named villagers with homes, simple presence or schedules, dialogue and relationship hooks; *(2026-10-07, proposed)* the four households are **Maximo**, **Kaloren Frosthand**, **Grim and Ogrin**, and **Bart** (Section 2.10); named villagers chosen into evening service alongside Visitors (today's generated customers become the Visitors); one small farm plot with a handful of crops feeding the storeroom (the smallest test of "I grew part of tonight's menu"); the full new day loop working through `GameFlow`. Not in 4h: ranching, fishing, Inn guests, Visitor promotion, resident recruitment. *(4h plan approved 2026-10-06 for Checkpoint A: `docs/PLAN_4H.md`; four checkpoints: A Tally Ho! and Kariaston walkable with the soft clock and 5 PM wind-down, B Vigor and a three-crop garden, C the cast with routines, D the community, Gimp and villager patrons.)*
- **4i Menus, options and polish** *(was 4h)*: settings (screen shake, flash and vibration intensity), accessibility per Section 12, audio system, web build.

**Phase 5's first steps** *(v0.5, approved 2026-10-05; order to be set when Phase 5 is planned)*:

- **The Inn and Visitors:** Visitors taking guest rooms, promotion to persistent identities, guest rooms that matter to who stays (qualitatively first), the first resident-candidate requests.
- **Settling residents:** the three plots end to end with one candidate (Visitor → guest → requests → settles), and generated residents speaking through authored modular dialogue.
- **Fishing** with its minigame.
- **Ranching**, small-scale.
- **Farming depth:** the crop set, and whether seasons exist (decided then).

Then content build-out as in the table above.

### 11.2 Scope Warning

Two full games in one is ambitious, especially for a small team. Recommended guardrails: keep minigames short and reusable; build a strong, small vertical slice before expanding; consider Early Access with 3–4 biomes and the first two tavern stages, adding later acts in updates. *(v0.5)* The life-sim direction makes this warning stronger, not weaker: a village, farming, ranching, fishing, an Inn and resident recruitment are each sizeable. They are proved one at a time with the smallest version that tests their essential experience, and an Early Access cut would be something like 3–4 biomes, the village with its first residents, the Inn and farming. *(v0.4)* The learning priorities (Section 1.5) deliberately spend more of the budget on customization, cooking, dialogue, NPC interactions and relationships; the guardrails apply most strictly everywhere else.

---

## 12. Accessibility

- Remappable controls, hold/toggle options.
- Adjustable combat speed and "assist mode" (damage taken, freshness decay, customer patience).
- Minigame assist: wider timing windows or auto-complete.
- Colorblind-safe telegraphs and freshness indicators (use shapes/icons, not color alone).
- Screen shake and flash intensity sliders; text size options.
- *(v0.2)* Vibration on/off, a vibration intensity slider, and a reduced-intensity option. No gameplay information is conveyed by haptics alone.

---

## 13. Decisions and Open Questions

**Decided**

1. **Art style:** pixel art, Minifantasy (8×8, top-down).
2. **Perspective:** top-down *(v0.2)*.
3. **Protagonist:** customizable through body choice and palette swaps.
4. **Time pressure:** no calendar deadline. Delves are limited by Essence, which depletes over time and when the player takes damage, and can be upgraded.
5. **Death penalty:** lose everything except one satchel slot the player chooses to keep.
6. **Co-op:** no.
7. **Dialogue and quest tooling:** Dialogue System for Unity and Quest Machine, with Minifantasy Portrait Generator portraits (locked 2026-10-03; replaces Yarn Spinner entirely), and Love/Hate for relationships (decided with the 4g plan, 2026-10-06; entry 37).
8. **Monetization:** premium only, with possible paid expansions.
9. **Tavern immersion is a design pillar** (Section 6.5), without equating immersion with manual labour *(2026-10-03)*.
10. **Persistent quests are required,** owned by Quest Machine; ordinary service orders are not quests (Section 2.6) *(2026-10-03)*.
11. **Persistent relationships with selected characters are required,** kept separate from Renown and Morale (Section 2.7) *(2026-10-03)*.
12. **`SaveSystem` is the only authoritative save;** middleware state joins it through adapters (Section 10.6) *(2026-10-03)*.
13. **Learning priorities** shape the content and complexity budget (Section 1.5) *(2026-10-04)*.
14. **Tavern customization is a major pillar** (pillar 7, Section 6.6): data-driven furniture and layouts, functional furniture included where practical, a large Minifantasy catalog, Gold purchases plus discoveries from the Hollows, bosses and story, one architecture for the whole property *(2026-10-04; "from the inn to the Stronghold" revised in v0.5)*.
15. **Discoveries never use Satchel slots** (Section 6.6) *(2026-10-04)*.
16. **Canonical story characters are not renameable;** hired and recruited workers carry full naming and appearance customization (Sections 2.5, 6.7) *(2026-10-04)*.
17. **Font:** Silver, adapted with plain punctuation (Section 8.2) *(2026-10-04)*.

*Recorded in v0.5 (2026-10-04); the direction was accepted then and its roadmap approved on 2026-10-05:*

18. **Identity:** a fantasy life sim centred on owning and running a tavern and inn in a strange village, with delving into the Hollows as a major pillar inside that daily life (Section 1.1).
19. **The day:** wake → free daytime → evening prep → tavern service → nighttime delve → return and sleep. Tavern before the delve for now (Section 3.1).
20. **The Stronghold direction is dropped:** no fortified Stronghold as the late-game home, no tower defense and no defense events (Section 6.4; Appendix C).
21. **Kariaston is a small persistent village** with a fixed authored cast of named villagers; tavern customers are named villagers plus generated Visitors; most Visitors are transient and only relevant ones are promoted to persistent identities (Sections 2.8, 6.3).
22. **The Inn reuses the customization architecture;** no second decorating system (Section 6.8).
23. **About three fixed residential plots** that settled Visitors can move into; fixed sites, not city-building (a current target, Section 6A.5).
24. **Surface ingredients and the Hollows:** the surface provides dependable ingredients, the Hollows unusual ones and discoveries; neither replaces the other (Section 5.5).
25. **Essence is the delve's only time pressure;** there is no time-of-night limit (Section 4.4).

*Recorded 2026-10-05:*

26. **Customization rules for 4f** *(2026-10-05)*: the Section 13 Open 9 questions are settled for 4f in `docs/PLAN_4F.md` §22: whole-tile snapping for blocking furniture (quarter tiles for decor); explicit rotation modes including real quarter-turn rotation; per-piece flipping; area-wide floor and wall finishes with fixed structure; movable stations (the Grill wall-bound); owned copies; curios kept on extraction and lost on death, boss trophies never lost; palette-channel recolouring baked into textures; no stat bonuses in 4f. Decor's relationship effects and renaming canonical characters stay open.
27. **Boog** *(2026-10-05; renamed 2026-10-06)*: the head cook, a goblin (he; drawn from Minifantasy's Goblin Sapper). Formerly Gunta Ashbelly, a dwarf, and before that Gundra Ashbelly; the stable id stays `gunta`.

*Recorded 2026-10-06 (the owner's approval of the 4h plan and its canon):*

54. **The sealing and the founding** *(locked)*: a source of evil opened beneath this region and its army caused a great war; Karias, Maximo's former apprentice and a great wizard, gave his life to seal it, with Maximo among those who sealed it; the seal is imperfect and what seeps through is the Hollows; Maximo founded Kariaston as a watch and named it for Karias; people gathered round the watch, and Tally Ho! with them. Maximo will not, and probably cannot, return below (the reason is a future story decision). Revealed in play through people and places, not cosmology (Section 2.1).
55. **Grim and Ogrin** *(locked)*: Grim, a dwarf and former delver, found the infant Ogrin beside two dead adults in the Hollows and raised him; Ogrin aged to about ten in a year or two, stopped, and has been chronically ill since; Grim doesn't know what he is. Ogrin calls him Grim; "Dad" is a reserved later beat. Grim runs the market stall as his livelihood.
56. **Gimp** *(locked)*: a half-elf hunter and ranger, nomadic and abrasive, who likes almost no one but Boog and Phi, starts standoffish toward Bram, loves rifles and explosives, and lives in and around the Hollows; world figure `soldier_headband`.
57. **Kaloren and Ogrin** *(locked)*: Kaloren brings Ogrin symptom-easing herbs once every three days, a routine that happens whether or not the keeper sees it; not tied to his lichdom.
58. **Maximo's figure** *(locked)*: the blue Knight on foot (no crown); his humour is his personality.
59. **The 4h plan** *(approved)*: Kariaston its own scene beside the tavern in the daytime, doors a near-instant fade (H1, locked); four checkpoints; Bram's room upstairs; four fixed garden beds as a deliberate farming model (a grid migration kept possible); optional tending, nothing dies; free starter seeds; Vigor and crop values as test values; Grim's stall; Bart the first Visitor who stayed; Gimp's irregular visits; Glimmer only a light; 0–2 villager patrons in Checkpoint D; generated-once, hand-owned village tilemaps; save version 10 in Checkpoint B; `Hearthdelve.Village` (`docs/PLAN_4H.md` §0).
60. **Phi's portrait** *(approved)*: a framed Portrait Generator drow portrait of Phi'rai hangs in Tally Ho!.

*Recorded 2026-10-07 (the owner's cast):*

53. **Orik's history** *(the owner's call, 2026-10-07)*: Orik is an old friend of Phi's. She hired him and he helped run Tally Ho! in her absence; when the Hollows became too dangerous he eventually left; Phi sought him out and hired him again (the 4g closeout patch replaced his "hired me for a week, eleven years ago" line with three short lines heard only if the keeper asks: the rebuilding, his years keeping it open alone, her finding him). His feelings about her disappearance carry a lifelong friend's weight; he stays dry, exact, practical and unwilling to go meaningfully into the Hollows.
52. **Old Phi** *(the owner's call, 2026-10-07)*: Tally Ho!'s former proprietor and the keeper's missing mentor is **Old Phi** (Phi'rai, a drow; she), replacing Old Tamsin: an adventurer of **the Fortunate Five** who helped rebuild Tally Ho!, left for many years and came back decades later to run it. No stable id, save field, quest or dialogue variable ever named her (there was no character asset), so nothing migrates; the game's nine lines were edited in the 4g closeout patch (Orik calls her Phi, Boog Old Phi; the keeper asks for Phi'rai). Her history comes out gradually, through Orik, objects and later conversations, never as an opening exposition dump; **the Fortunate Five** are history, texture, occasional anecdotes and future hooks, and the owner's tabletop campaign is not imported wholesale.
51. **The Kariaston cast** *(the owner's call, 2026-10-07)*: new villagers **Kaloren Frosthand** (a kind wizard, secretly a lich made in the Hollows, his phylactery still below), **Maximo** (the elderly, Quixote-like mayor; Kariaston is named for his friend and sidekick **Karias**, a young elf wizard he mentored, killed in the Hollows; Maximo has never gone back), **Grim and Ogrin** (a dwarf and the sick orphan human boy he looks after) and **Bart** (an orc bard); **Orik** confirmed as the dwarf bartender who replaced Pip (entry 43). New **Hollowers**, people who live in the Hollows and aren't enemies: **Gimp** (comes up to the tavern to talk explosives with Boog) and **Glimmer** (a whimsical fey spirit, once a guardian meant to seal the Hollows, who failed and forgot her past; a later questline heals and fuses her with Ogrin). Names, kinds and concepts are the owner's; the details in Section 2.10 are proposed. All are canonical characters (not renameable, Decided 16). Questions: Open 12.

*Recorded 2026-10-06 (4g Checkpoint C and the closeout, complete; playtested by the owner):*

48. **Relationship reactivity in 4g** *(Step 7)*: a handful of remarkable deeds (the troll's first fall, Boog's bomb returned, a special request met very well, a fine cut at the block, the trophy over the bar), read differently by Boog (nerve, craft) and Orik (warmth, craft); repeats fade to nothing; callbacks are said once, in a priority order that never hides the story or a quest. No meters, hearts or numbers: whether the full game wants them is decided later.
49. **Tally Ho! as a home and social space** *(for 4h)*: the daytime tavern is a place to be (wander, talk, see regulars and Visitors, decorate, go out into a walkable Kariaston) without the day advancing, not a chain of management screens.
50. **Friendly monsters** *(direction for 4h / Phase 5)*: Kariaston's population is mixed, including selected friendly monsters; some authored creatures met in the Hollows may go *encounter → recurring visitor → Inn guest → possible resident*. Never arbitrary enemies. A first candidate in owned art: the Mushroom People (Creatures; idle, jump, hurt and die, no attack, which is why they never became an enemy).

*Recorded 2026-10-06 (4g Checkpoint B as built; signed off 2026-10-06):*

44. **The keeper** *(4g Step 4)*: made at New Game: a name (up to 16 letters) and one of four complete Minifantasy bodies (townsfolk, warrior, dwarf, orc), with Minifantasy colourways for skin, hair and clothes; no pronoun choice (dialogue says "you" or the name). Saved in the keeper's profile; a legacy keeper is Bram, the townsfolk.
45. **The Act I opening** *(4g Step 5)*: arrival day at Tally Ho! (Orik, Boog, Phi missing below, the empty storeroom), down the cellar hatch to a first delve taught by one-time prompts, the homecoming that night, then the first evening (the board, cooking, serving, the takings), ending on Boog's question. Explicit stages, saved; an old save is past them.
46. **Boog's Bomb as built** *(4g Step 6)*: the bomb lies in the second fight cleared on the Cellars' first floor while the quest wants it; lost with a death and found again; brought home by extraction; handed over in Boog's conversation for 60 gold and a deed done for Boog alone (`returned_boogs_bomb`). Declining never closes it. She has no name yet (candidates in `PLAN_4G.md`).
47. **Dialogue tooling boundary** *(4g Checkpoint B)*: the story tooling seeds each conversation once and never rewrites it; the node editor owns it from then on.

*Recorded 2026-10-06 (after 4g Checkpoint A was built):*

43. **Orik** *(the owner's call, 2026-10-06)*: the server and bookkeeper is **Orik**, a dwarf (he) with a ginger beard, replacing Pip Marrowby, a halfling. His look is the Creatures pack's yellow-bearded dwarf (ginger hair and beard as drawn, a brown leather belt, green clothes); his portrait a ginger-bearded dwarf from the Portrait Generator. The stable id stays `pip`, as Boog's stays `gunta`; his role, values and starting feelings are unchanged.

*Recorded 2026-10-06 (the 4g plan's decisions, approved by the owner):*

37. **Love/Hate in 4g** *(2026-10-06)*: the relationship backend, behind the Hearth & Hollows relationship adapter: **Affinity** (Love/Hate's evaluation) and **Respect** (added through Love/Hate's own evaluation hook: a deed's respect, weighted by how well the judge's values match what it shows and by repetition). No Trust yet, no emotion simulation, no rumor networks, no automatic reactions to everything. Each tracked character has a **social stand-in** in Boot, keyed by their stable id, so a deed done anywhere reaches them. Memories age by the game's days, never real time.
38. **Dialogue authoring** *(D1, 2026-10-06)*: the Dialogue System's own editor and database are where conversations are written; no custom text-file importer. Unity Localization stays authoritative for what the player reads (the Dialogue string table, keyed by each line's Guid); quest text is keyed the same way, never in Quest Machine's text tables.
39. **The first quest** *(2026-10-06)*: **Boog's Bomb**. Boog's favorite bomb has gone missing somewhere in the Hollows, and he desperately needs it back for "research". Boog is an enthusiastic, unpredictable, sincere enthusiast of explosives, oddly sentimental about this bomb, never malicious or grim: the comedy is that he finds this perfectly normal. Built in Step 6 (Checkpoint B).
40. **Quest objects** *(2026-10-06)*: never in the Satchel or the Lockbox; kept on extraction and then persistent quest progress; lost on a death before extraction, and offered again on a later delve while the quest is active, so dying never fails the quest by itself. A modest per-object policy, not a general quest-item framework.
41. **No keeper portrait in 4g** *(D4)*: characters speak with portraits; the keeper answers through choices. The keeper's name, look and creation choices still reach gameplay and dialogue.
42. **Old saves skip the opening** *(D7)*: a save from before 4g has its opening marked done (explicit story state, never inferred), keeps Bram's legacy name and look, and is never sent through character creation or onboarding; a new game plays the opening.

*Recorded 2026-10-06 (names, after the 4f type pass):*

35. **Names** *(the owner's call, 2026-10-06)*: the tavern and inn is **Tally Ho!** (with its exclamation mark, written as a name: "decorating Tally Ho!", "Tally Ho!'s guest room"), replacing the Sunken Flagon; the village is **Kariaston**, replacing Brackenford; the head cook is **Boog**, a goblin (entry 27). Stable ids keep their old names (`gunta`, `brackenford_ale`, `Supply_BrackenfordMarket`), like the Hollows' `Dungeon` code.
36. **Safe ground in the Hollows** *(2026-10-06)*: Essence doesn't drain in the rope room (climbing out) or the hole room (going deeper), so the choice at a floor's end isn't rushed; walking between rooms already cost nothing. Damage still applies.

*Recorded 2026-10-06 (4f as built):*

28. **Special customer requests (4f)**: now and then a patron's order is one they particularly want tonight. It is the same order through ordinary service, never a quest: only a dish on today's menu that can be made at that moment, about one or two a night (capped, seeded, tuned in `ServiceConfig`), met for a thank-you tip and a little Renown, missed for a frown and nothing worse. Requests last only for the evening and are never saved. Persistent and story quests are Quest Machine's, from 4g.
29. **Renown pacing (4f)**: each patron's Renown is added up over the evening and the total rounded, so competent service earns some and strong service clearly more; the catalog tiers stay at 0, 25, 60 and 100 (about a week of strong nights to the last).
30. **Surface food is the safety net (4f)**: the market's staples make a modest profit even when played weakly, while a delve night at the same skill earns well over twice as much; signature dishes are the best nights but limited by their cuts and rare parts. Checked with the balance report (Hearthdelve → Balance → Evening Report), not final balance.
31. **The Butcher Block (4f)**: a Prep activity, not a service stage; a weak cut is about as good as cooking the part whole, a clean one roughly two and a half to three times better as the signature dish.
32. **Staff defaults (4f)**: Boog starts each game off duty and is given a station at Prep, so the player meets the cooking first; his steady work is capped below a competent keeper's (0.75), Orik serves by default.
33. **Gameplay facts for 4g (4f)**: the tavern and the Hollows publish what happened (a curio brought home, a piece placed, a trophy displayed, a market purchase, a part butchered, staff work, a dish served, a service completed, a boss defeated, special requests issued, met or missed) as stable-id events on the `EventBus`. 4g's Dialogue System, Quest Machine and Love/Hate adapters listen there; gameplay never calls them.
34. **Save version 7 (4f)**: discoveries marked new and a trophy awaiting its homecoming are saved; older saves migrate in place, gain only what didn't exist when they were made (the guest room, the Butcher Block, a trophy already earned), and never twice.

**Open**

1. ~~**Stronghold defense events:** core feature or post-launch?~~ Dropped with the Stronghold direction (Decided 20).
2. **Biome 2 boss:** no fungal boss found in the art yet.
3. **Audio source:** where SFX and music come from.
4. ~~**Font:** a pixel font for Super Text Mesh.~~ Decided: Silver (Decided 17).
5. **Freeform construction:** not planned, but kept possible.
6. **Patron requests before quests exist** *(v0.3)*: 4f lists "customer requests", but requests for parts or ingredients that persist beyond an evening are now quests (Section 2.6), and Quest Machine arrives in 4g. See `docs/PROGRESS.md`.
7. ~~**Love/Hate in 4g or later**~~ *(v0.3; resolved 2026-10-06: in 4g, Decided 37).*
8. **The multi-stage dish model** *(v0.3)*: how stages are represented and scored, and how intermediate results are held. Decided with the first multi-stage dish (Section 5.4).
9. **Customization decisions** *(v0.4; most settled for 4f on 2026-10-05, Decided 26)*, each to be prototyped or brought back to the owner, not decided silently (Section 6.6):
   - free placement or grid placement;
   - whether walls, floors and doors are editable;
   - whether every functional station can move;
   - furniture ownership and quantity rules;
   - duplicate-drop behaviour;
   - how decor discoveries survive extraction and death (the curio channel or a simpler alternative);
   - whether boss trophies can be lost;
   - the palette and recolouring technique (shader or authored variants);
   - whether furniture carries gameplay stat bonuses;
   - what relationship effects tavern decor has;
   - whether any canonical character becomes renameable (a story decision).
10. **Village life decisions** *(v0.5)*, expensive to reverse and to be prototyped later, not decided silently:
    - whether daytime uses a continuously ticking clock or player-controlled phase transitions (Section 3.4);
    - whether the nightly delve is always part of the day or can be skipped to go straight to sleep (Section 3.1);
    - the crop, growth and season system, and farm size;
    - how deep ranch animal simulation goes;
    - the fishing minigame;
    - the village's exact size and layout, and the number of permanent villagers;
    - how deep procedural Visitor generation goes;
    - which Visitors are eligible to become residents, and the promotion rules from transient Visitor to persistent guest or candidate;
    - Inn occupancy rules (and whether any numerical hotel systems exist);
    - how the three village plots are built on;
    - whether recruited villagers or Visitors can become tavern staff (Section 6.7);
    - relationship and romance scope;
    - seasons and weather, and how many days make a season or year if they exist.
11. **Questions the v0.5 direction raised** *(v0.5)*:
    - **Morale's purpose:** whether village Morale stays a separate measure or is derived from the villagers' relationships (Section 6.4);
    - where the old Night upgrade screen's functions and the pre-delve breakfast buff go in the new day (Section 3.1; decided when 4d step 5 is planned);
    - freshness tuning now that parts from the Hollows wait a day before service (Section 3.1);
    - the story revision for Acts II–IV and the canonical cast (Section 2.9);
    - whether people met or rescued in the Hollows can become Visitors (Section 3.3).
12. **The new cast** *(2026-10-07; Section 2.10 has the detail)*:
    - ~~when Kariaston got its name~~ (founded by Maximo, Decided 54); still open: how long ago the sealing was, and Maximo's age;
    - Karias: an elf (the brief) or a half-elf (the campaign notes); the docs say elf;
    - what Kaloren's secret is, given that liches are ordinary neighbours (recommended: how and where he became one, and the phylactery below), and who knows it;
    - how Glimmer, a failed guardian of the seal, relates to the Warden Below;
    - the reading of Glimmer's questline (a bond that heals Ogrin and restores her);
    - names that sit close together: Bart and Bram (the keeper's default), Grim and Gimp, Orik and Ogrin;
    - ~~Gimp's kind~~ (half-elf, Decided 56);
    - firearms (Gimp loves rifles: recommended as rare personal property, never a player weapon; before 4h Checkpoint D);
    - how Glimmer's failed guardianship relates to Maximo's sealing;
    - why Maximo cannot return to the Hollows (a future story decision).

---

## 14. Appendix A: Glossary

- **Hearth & Hollows:** the game's working title since 2026-10-05 (formerly Hearthdelve). Hearthdelve remains the repository, Unity project and code name.
- **The Hollows:** the underground world beneath Kariaston, made of regions (biomes) at increasing depth from the Cellars to the Heart; the in-world proper name, always "the Hollows" *(2026-10-05; formerly "the Dungeons")*.
- **Dungeon:** the technical and genre term for the gameplay layer set in the Hollows (the `Dungeon` assembly, scenes, input map and classes); not an in-world place name *(2026-10-05)*.
- **Delve:** a single roguelite run into the Hollows; *(v0.5)* it happens at night, after service. To **delve**; a **delver** is someone who does.
- **Kariaston:** the village above the Hollows; **Tally Ho!:** the player's tavern and inn.
- **Essence:** the delve timer and the player's only health pool.
- **Haul:** ingredients carried back from a delve.
- **Harvest Finisher:** a special kill move that guarantees a premium part.
- **Kitchen Arts:** the player's special meter attack *(deferred in the 4e plan)*.
- **Cheer:** buffs during a delve, granted by the village's Morale *(v0.5; was stronghold morale)*.
- **Renown:** the tavern's reputation, driving customer tiers and story.
- **Morale:** the state of the village community; it produces Cheer *(v0.5, reinterpreted from the Sanctuary/Stronghold community; Section 6.4)*.
- **Disposition:** what one named character or faction thinks of Bram (Section 2.7).
- **Quest:** an objective that persists or matters beyond a single ordinary order, owned by Quest Machine.
- **Recurring patron:** a named customer who returns and remembers.
- **Named villager** *(v0.5)*: a persistent, authored resident of Kariaston (Section 2.8).
- **Visitor** *(v0.5)*: a generated outsider who comes to the tavern; usually transient (Section 2.8).
- **Promoted Visitor** *(v0.5)*: a Visitor saved as a persistent identity because they became relevant through the Inn or as a resident candidate.
- **Resident** *(v0.5)*: a villager; a **recruited resident** is a former Visitor who settled in one of the empty plots (Section 6A.5).
- **Hollower** *(2026-10-07)*: an authored person who lives in the Hollows and is not an enemy, such as Gimp or Glimmer (Section 2.10).
- **Inn** *(v0.5)*: Tally Ho!'s guest rooms (Section 6.8).
- **Preparation stage:** one step of a multi-stage dish (Section 5.4).
- **Satchel / Lockbox:** carry inventory / the one slot kept on death.
- **Run power-up:** a temporary boon chosen from three, lasting one run.
- **Haptic pattern:** a named vibration design triggered by gameplay.

---

## Appendix B: Superseded v0.1 side-scroller design

Kept for reference. None of this describes the current game. The playable prototype built from it is at the tag `v0-sidescroller-prototype`.

### B.1 Elevator pitch (v0.1)

"…By day you descend into The Dungeons, carving through monsters in fast, **side-scrolling** hack-and-slash runs, harvesting their parts…"

### B.2 Genre and inspirations (v0.1)

Hybrid: side-scrolling action roguelite + restaurant/tavern management sim. *Dead Cells* was the main combat inspiration: fluid 2D melee combat, weapon variety, procedurally stitched levels, run-based structure with persistent unlocks. Target audience listed fans of *Dead Cells*.

### B.3 Combat feel (v0.1, Section 4.1)

Target feel was *Dead Cells*: responsive, fast, readable, with heavy hit-stop and satisfying animation canceling.

- **Movement:** run, jump, double jump (unlockable), dodge roll with i-frames, wall slide/jump, drop-through platforms, ledge grab.
- **Attacks:** primary weapon (combo chains), secondary weapon or shield, two skill slots (tools/throwables), and a special "Kitchen Arts" meter attack.
- **Feedback:** hit-stop, screen shake (subtle, toggleable), damage numbers (toggleable), clear enemy telegraphs.

### B.4 Weapons (v0.1, Section 4.2)

Cleaver (Butcher's Cleaver), Filleting Blade (Eel-Tooth Knife), Tenderizer (Troll-Mallet), Skewer Spear (Rotisserie Pike), Frying Pan (Iron Skillet), Traditional (swords, axes, bows, staves). Random affixes per run, "*Dead Cells* style".

### B.5 Death (v0.1, Section 4.4)

"On death, the player keeps a portion of the haul (e.g. items in a protected 'Lockbox' slot plus a percentage of the rest)…" Replaced before the pivot by the one-slot Lockbox rule.

### B.6 Run structure and biomes (v0.1, Section 4.6)

Levels were assembled from hand-authored rooms stitched together procedurally into continuous side-scrolling levels; each biome had 3–5 floors plus a boss, with branching paths between biomes as in *Dead Cells*.

| # | Biome | Theme | Signature Ingredients |
|---|---|---|---|
| 1 | The Cellars | Flooded old cellars and tunnels | Giant rats, slimes, cave mushrooms |
| 2 | Fungal Warrens | Glowing fungal forest | Myconids, spore beetles, walking truffles |
| 3 | Goblin Sprawl | Goblin shanty-town and mines | Boar-riders, cave boars, stolen spices |
| 4 | Drowned Halls | Sunken dwarven ruins | Giant eels, crab knights, kelp horrors |
| 5 | Ember Forge | Volcanic dwarven forge | Salamanders, fire drakes, magma snails |
| 6 | Frostvault | Frozen crypts | Ice trolls, wyrm eggs, frost wraiths |
| 7 | The Rootdeep | Living, pulsing underworld | Aberrations, dragon cuts, legendary parts |

Example bosses: *The Cellar King* (giant rat monarch), *Grandmother Spore* (myconid matriarch), *Chieftain Gutgrin* (goblin warlord on a war boar), *The Leviathan Eel*, *Forge-Drake Cindermaw*, *The Frost Troll Queen*, *The Warden Below* (final).

### B.7 Example ingredients (v0.1, Section 5.2)

Giant Rat Haunch (Meat, Savory), Green Slime Gel (Liquid, Sweet), Myconid Cap (Fungus, Earthy/Umami), Cave Boar Belly (Meat, Savory), Giant Eel Fillet (Fish, Umami), Salamander Tail (Meat, Spicy), Ice Troll Liver (Offal, Bitter), Fire Drake Heart (Magical, Spicy/Arcane).

### B.8 Service phase and defense events (v0.1, Sections 6.1 and 6.4)

"During evening service the camera shows the tavern floor and kitchen in a **side view**…" The player moved between stations along one axis. Defense events were described as "a short side-scrolling combat encounter".

### B.9 Visual style (v0.1, Section 8.1)

Two options were open: high-res hand-painted 2D with skeletal animation, or detailed pixel art in the spirit of *Dead Cells* and *Dave the Diver*. The prototype used pixel-art placeholders at 640×360 and 32 PPU. UI was planned in UI Toolkit.

### B.10 Controls (v0.1, Section 9)

| Action | Dungeon | Tavern |
|---|---|---|
| Left Stick | Move | Move between stations |
| A / Cross | Jump | Interact / confirm |
| X / Square | Primary attack | Minigame action |
| Y / Triangle | Secondary attack | Minigame alt action |
| B / Circle | Dodge roll | Cancel / back |
| LB / RB | Skills 1 and 2 | Cycle orders |
| RT | Kitchen Arts special | Speed up (hold) |
| LT | Harvest finisher | — |
| Start | Pause menu | Pause menu |

### B.11 Technical design (v0.1, Section 10)

Unity 6.3 LTS. UI Toolkit for menus and HUD, with uGUI only for world-space UI. 2D Animation package (or Spine) for skeletal characters. Physics 2D with a **custom kinematic character controller** for tight platforming. Character state machine: Idle, Run, Jump, Fall, Dodge, Attack, Hurt, Dead, with frame-data-driven attacks. Hitbox/hurtbox components and hit-stop via a time-scale service. Level generation "similar to *Dead Cells*": room prefabs placed on a grid with connection validation to avoid overlap. Relics granted traversal abilities (double jump, dash).


---

## Appendix C: Superseded v0.4 Stronghold direction and day order

Kept for reference (v0.5, 2026-10-04). None of this describes the current design. The story acts that depended on it are still in Section 2.4, awaiting revision (Section 2.9).

### C.1 Elevator pitch (v0.2)

"You are the keeper of a small inn built atop the mouth of an ancient dungeon. By day you descend into **The Dungeons**, fighting room by room through top-down, hack-and-slash runs and harvesting the monsters you kill. By night you cook those parts into meals and pour brews for a growing crowd of patrons. The coin you earn buys better gear so you can delve deeper for rarer ingredients. As the dungeons begin to spill onto the surface, your inn grows the way a cult grows in *Cult of the Lamb*: from a quiet inn into a sanctuary, then a stronghold, and finally the rallying point of a world looking for a champion."

Genre (v0.2): "Hybrid: top-down action roguelite + tavern management sim." *Cult of the Lamb* was "the overall shape: short top-down combat runs feeding a home base that grows, with residents who have roles and moods"; *Moonlighter* was "dungeon by day, shop by night".

Pillars (v0.1–v0.4): "2. **Two halves, one loop.** The dungeon and the tavern feed each other constantly. Neither half should feel like a detour from the 'real' game." "3. **A home that grows with you.** The tavern visibly transforms from a quiet inn into a fortified stronghold full of people you saved."

### C.2 Core loop (v0.1–v0.4, Section 3)

#### 3.1 The Day Cycle

Each in-game day is divided into four phases:

1. **Morning — Prep (Tavern hub).** Check stock, set the day's menu, eat a buff meal, choose gear, accept requests from patrons and residents (e.g. "bring me cave troll liver"; these are quests, Section 2.6).
2. **Day — The Delve (Dungeon).** A roguelite run. Fight, harvest, and choose when to return. Deeper = rarer ingredients and more risk.
3. **Evening — Service (Tavern).** Cook and serve using minigames. Earn gold, tips, and renown.
4. **Night — Upgrade (Tavern hub).** Spend earnings on equipment, tavern expansions, recipes, and staff. Story scenes play here. Save point.

#### 3.2 Loop Diagram

```mermaid
flowchart LR
    A[Morning Prep] --> B[Delve into the Dungeon]
    B --> C[Evening Service]
    C --> D[Night Upgrades & Story]
    D --> A
    B -- monster parts --> C
    C -- gold & renown --> D
    D -- gear, buffs, unlocks --> B
```

#### 3.3 How the Two Halves Feed Each Other

| From Dungeon to Tavern | From Tavern to Dungeon |
|---|---|
| Monster parts are ingredients | Gold buys weapons, armor, and tools |
| Harvest quality affects dish quality | Pre-delve meals grant run buffs |
| Rare parts unlock new recipes | Customer requests point you at specific monsters |
| Found recipe scraps and lore | Refugee staff unlock new dungeon abilities |
| Rescued NPCs join the tavern | Stronghold morale grants in-dungeon "Cheer" |

### C.3 The Growing Stronghold (v0.2–v0.4, Section 6.4)

The inn plays the role the cult plays in *Cult of the Lamb*. It grows across the acts from a small inn into a stronghold.

| Stage | Name | Adds |
|---|---|---|
| 1 | The Inn | Kitchen, bar, small dining room |
| 2 | The Sanctuary | Guest rooms, refugee quarters, herb garden, storeroom |
| 3 | The Stronghold | Walls, watchtower, forge, training yard, brewery, great hall |
| 4 | The Bastion | War room, shrine, feast hall for the finale |

- **Areas** unlock through story and upgrades.
- **Furniture and decor** are placed freely inside unlocked areas *(v0.4: see Section 6.6; whether decor carries gameplay bonuses such as customer satisfaction is open)*.
- **No freeform construction** (placing walls and rooms) for now, but nothing should be designed in a way that rules it out later.
- *(v0.4)* Every stage uses **the same customization architecture** as the Stage 1 inn (Section 6.6): the home the player starts decorating early is the stronghold they later defend and return to. This is not a city-builder.
- Art: *Tavern Indoor*, *Towns*, *Towns 2*, *Crafting And Professions I/II* (kitchen, preparation table and other workbenches), *Farm*, *Castles And Strongholds*, *Builders*.

**Residents:** refugees who move in can be assigned roles (cook, server, gardener, smith, guard). Each resident has a small personal questline (Quest Machine, Section 2.6); selected residents also carry relationship state (Section 2.7).

**Morale and Cheer:** the stronghold has a Morale value driven by food quality, housing, and story events. High morale grants **Cheer** in the dungeon: temporary buffs, extra revives, or crowd "chants" that power up the Kitchen Arts meter. This makes the story theme of people rallying behind you a real mechanic. *(v0.3)* Morale is the community's state; it is separate from the tavern's Renown and from any one character's disposition (Section 2.7).

**Defense Events (undecided):** occasionally monsters breach the surface and attack the stronghold, and the player defends with residents helping. Not built yet. Because the tavern now uses the same top-down character as the dungeon, adding them later is cheap.

### C.4 Customization growing into the Stronghold (v0.4, Section 6.6)

"**Growing with the home.** The same architecture later serves the Sanctuary and the Stronghold (Section 6.4): early game, personalize Tally Ho!; Act II, the inn grows into a Sanctuary with new areas and furnishing possibilities; Act III, the Stronghold's larger customizable spaces; Act IV, a home that visibly reflects everything the player survived and collected. No separate building system per stage."
