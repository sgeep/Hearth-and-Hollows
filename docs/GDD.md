<!doctype html><html><head><meta charset=utf8><meta name=viewport content="width=device-width,initial-scale=1,viewport-fit=cover"><style>:root{box-sizing:border-box;padding-top:env(safe-area-inset-top,0px);padding-bottom:env(safe-area-inset-bottom,0px)}html{scroll-padding-top:env(safe-area-inset-top,0px)}</style><style>:root{color-scheme:light dark;--md-bg:#fff;--md-text:rgba(0,0,0,.8);--md-muted:rgba(0,0,0,.6);--md-fill:rgba(0,0,0,.04);--md-fill-strong:rgba(0,0,0,.06);--md-rule:rgba(0,0,0,.1);--md-rule-strong:rgba(0,0,0,.16);--md-link:hsl(210 100% 45%)}@media (prefers-color-scheme:dark){:root:where(:not([data-theme="light"])){--md-bg:#0d0d0d;--md-text:rgba(255,255,255,.85);--md-muted:rgba(255,255,255,.6);--md-fill:rgba(255,255,255,.06);--md-fill-strong:rgba(255,255,255,.09);--md-rule:rgba(255,255,255,.14);--md-rule-strong:rgba(255,255,255,.22);--md-link:hsl(210 100% 72%)}}:root[data-theme="dark"]{color-scheme:dark;--md-bg:#0d0d0d;--md-text:rgba(255,255,255,.85);--md-muted:rgba(255,255,255,.6);--md-fill:rgba(255,255,255,.06);--md-fill-strong:rgba(255,255,255,.09);--md-rule:rgba(255,255,255,.14);--md-rule-strong:rgba(255,255,255,.22);--md-link:hsl(210 100% 72%)}:root[data-theme="light"]{color-scheme:light}@media print{:root,:root[data-theme="dark"]{color-scheme:light;--md-bg:#fff;--md-text:rgba(0,0,0,.8);--md-muted:rgba(0,0,0,.6);--md-fill:rgba(0,0,0,.04);--md-fill-strong:rgba(0,0,0,.06);--md-rule:rgba(0,0,0,.1);--md-rule-strong:rgba(0,0,0,.16);--md-link:hsl(210 100% 45%)}}body{background:var(--md-bg);color:var(--md-text);max-width:720px;margin:0 auto;padding:32px;display:flex;flex-direction:column;gap:10px;font:14px/1.55 -apple-system,BlinkMacSystemFont,'SF Pro','Segoe UI',sans-serif;overflow-wrap:break-word}body>:first-child{margin-top:0}h1,h2,h3,h4,h5,h6{margin:6px 0 0;line-height:1.25;font-weight:600;text-wrap:balance}h1{font-size:1.35em}h2{font-size:1.15em;color:var(--md-muted)}h3,h4,h5,h6{font-size:1em}p,ul,ol,blockquote,table,pre,hr{margin:0}strong{font-weight:600}a{color:var(--md-link);text-decoration:none}a:hover{text-decoration:underline}ul,ol{display:flex;flex-direction:column;gap:6px;padding-left:22px}ul{list-style:disc}ol{list-style:decimal}:is(li,td,th)>*+:is(p,ul,ol,blockquote){margin-top:6px}blockquote{display:flex;flex-direction:column;gap:10px;border-left:2px solid var(--md-rule);padding-left:10px;color:var(--md-muted)}:not(pre)>code{background:var(--md-fill);padding:1px 3px;border-radius:4px;font:.92em 'SF Mono',ui-monospace,Menlo,Consolas,monospace}a>code{background:none;color:inherit}pre{background:var(--md-fill);padding:10px 12px;border-radius:6px;overflow-x:auto;font:12px/1.5 'SF Mono',ui-monospace,Menlo,Consolas,monospace;margin-block:4px}pre code{background:none;padding:0;font:inherit}table{width:100%;border-collapse:separate;border-spacing:2px;font:inherit}th,td{padding:6px 8px;border-radius:3px;text-align:left;vertical-align:top}th{background:var(--md-fill-strong);font-weight:600}td{background:var(--md-fill)}:is(th,td) :not(pre)>code{background:transparent}hr{border:0;border-top:1px solid var(--md-rule-strong);margin-block:10px}img{max-width:100%;height:auto;border-radius:4px}</style>
</head><body>
<h1>HEARTHDELVE — Project Design Document</h1>
<p><em>Working title. Version 0.1 (first draft). Engine: Unity 6.3 LTS.</em></p>
<hr>
<h2>1. Overview</h2>
<h3>1.1 Elevator Pitch</h3>
<p>You are the keeper of a small tavern built atop the mouth of an ancient dungeon. By day you descend into <strong>The Dungeons</strong>, carving through monsters in fast, side‑scrolling hack‑and‑slash runs, harvesting their parts. By night you cook those parts into meals and pour brews for a growing crowd of patrons. The coin you earn buys better gear so you can delve deeper for rarer ingredients. As the dungeons begin to spill onto the surface, your tavern becomes a sanctuary, then a stronghold, and finally the rallying point of a world looking for a champion.</p>
<h3>1.2 Genre and Inspirations</h3>
<p>Hybrid: side‑scrolling action roguelite + restaurant/tavern management sim.</p>
<table>
<thead>
<tr>
<th>Inspiration</th>
<th>What we take from it</th>
</tr>
</thead>
<tbody>
<tr>
<td><em>Dead Cells</em></td>
<td>Fluid 2D melee combat, weapon variety, procedurally stitched levels, run‑based structure with persistent unlocks</td>
</tr>
<tr>
<td><em>Dave the Diver</em></td>
<td>Day/night split between exploration and restaurant service, minigame‑driven cooking, charming NPC cast</td>
</tr>
<tr>
<td><em>Delicious in Dungeon</em></td>
<td>Monsters as food, the ecology and &quot;cookability&quot; of creatures, how you kill something affecting how it tastes</td>
</tr>
<tr>
<td><em>Warcraft / Lord of the Rings</em></td>
<td>Classic high‑fantasy world: humans, dwarves, elves, orcs, ancient evils, kingdoms under threat</td>
</tr>
</tbody>
</table>
<h3>1.3 Design Pillars</h3>
<ol>
<li><strong>Every kill is a harvest.</strong> Combat is not only about survival; <em>how</em> you fight determines what you bring home.</li>
<li><strong>Two halves, one loop.</strong> The dungeon and the tavern feed each other constantly. Neither half should feel like a detour from the &quot;real&quot; game.</li>
<li><strong>A home that grows with you.</strong> The tavern visibly transforms from a quiet inn into a fortified stronghold full of people you saved.</li>
<li><strong>Cozy on the surface, dread below.</strong> The warmth of the tavern contrasts with the growing menace of the depths.</li>
</ol>
<h3>1.4 Target Platform and Audience</h3>
<ul>
<li><strong>Primary:</strong> PC (Steam). <strong>Secondary:</strong> Nintendo Switch 2, PlayStation 5, Xbox Series (post‑launch consideration).</li>
<li><strong>Input:</strong> Controller‑first design, full keyboard and mouse support.</li>
<li><strong>Audience:</strong> Players who enjoy action roguelites and cozy management games; fans of <em>Dave the Diver</em>, <em>Dead Cells</em>, <em>Hades</em>, <em>Potion Craft</em>, <em>Stardew Valley</em>.</li>
<li><strong>Rating target:</strong> Teen (fantasy violence, mild monster gore played for comedy).</li>
</ul>
<hr>
<h2>2. World and Story</h2>
<h3>2.1 Setting</h3>
<p>The world of <strong>Aldmere</strong> is a traditional high‑fantasy continent: human kingdoms, dwarven holds carved into mountains, elven forests, orcish clans of the steppes, and wild borderlands between them. Ages ago a civilization delved too deep and sealed what it found beneath the earth. Those seals are failing.</p>
<p><strong>The Dungeons</strong> are not ordinary caves. They are living, shifting underworlds that rearrange themselves (justifying procedural layouts). Each one grows outward and upward over time, and monsters from their depths are beginning to emerge onto the surface.</p>
<h3>2.2 The Tavern</h3>
<p><strong>The Sunken Flagon</strong> sits in the frontier village of <strong>Brackenford</strong>, built directly over a dungeon entrance that locals treated as a curiosity. Adventurers used to stop in for a drink before exploring the shallow floors. The player inherits the tavern at the start of the game (see Act I).</p>
<h3>2.3 The Protagonist</h3>
<p>A retired (or reluctant) adventurer who has taken over the tavern. Recommended approach: a customizable protagonist with a fixed voice/personality, or a named character with light customization (appearance, name). Default name for this document: <strong>Bram Holloway</strong>.</p>
<h3>2.4 Story Arc</h3>
<p>The story unfolds in four acts, advanced by reaching dungeon depths and by tavern milestones (renown, sanctuary capacity).</p>
<p><strong>Act I — The Inn (Biomes 1–2).</strong> Bram inherits the Sunken Flagon from a mentor who vanished in the dungeon. Business is slow. A wandering dwarf cook teaches Bram that monster meat, prepared right, is delicious. The first customers are adventurers and curious villagers. Hooks: the mentor's disappearance, strange carvings on the dungeon walls.</p>
<p><strong>Act II — The Sanctuary (Biomes 3–4).</strong> Travelers bring news: other dungeons have opened across Aldmere. Monsters raid nearby farms. Refugees begin arriving at the tavern looking for food and safety. Bram expands the inn into a sanctuary with rooms, a wall, and space for newcomers. Some refugees have skills and join the tavern's workforce. The player learns the dungeons are connected beneath the world.</p>
<p><strong>Act III — The Stronghold (Biomes 5–6).</strong> A neighboring kingdom falls. The tavern becomes one of the last safe places on the frontier. Soldiers, a disgraced knight, an elven scout and an orc warband arrive, uneasy allies. The tavern is fortified. Patrons now watch Bram's delves with hope; their morale becomes a mechanical force (see Section 6.4). Bram discovers what happened to his mentor.</p>
<p><strong>Act IV — The Champion (Biome 7 and the Heart).</strong> The source of the dungeons is revealed at the deepest point beneath Brackenford. The whole stronghold rallies. A final descent culminates in a boss fight, with the people Bram fed and sheltered providing direct support. Post‑game: endless/ascension mode and &quot;legendary&quot; ingredients.</p>
<h3>2.5 Key Characters (Draft)</h3>
<table>
<thead>
<tr>
<th>Character</th>
<th>Role</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Bram Holloway</strong></td>
<td>Protagonist, tavern keeper and delver</td>
</tr>
<tr>
<td><strong>Gundra Ashbelly</strong> (dwarf)</td>
<td>Head cook and mentor for cooking mechanics; gruff, obsessed with flavor</td>
</tr>
<tr>
<td><strong>Pip Marrowby</strong> (halfling)</td>
<td>Server and bookkeeper; runs the floor during service</td>
</tr>
<tr>
<td><strong>Old Tamsin</strong></td>
<td>Former owner/mentor, missing in the dungeon; central mystery</td>
</tr>
<tr>
<td><strong>Ser Aldric Vane</strong></td>
<td>Disgraced knight who arrives in Act II; unlocks weapon training</td>
</tr>
<tr>
<td><strong>Sylvaris</strong> (elf)</td>
<td>Herbalist and scout; unlocks herb garden and brewing depth</td>
</tr>
<tr>
<td><strong>Grukka Stonejaw</strong> (orc)</td>
<td>Warband chief; blacksmith and fortification builder</td>
</tr>
<tr>
<td><strong>The Warden Below</strong></td>
<td>The intelligence behind the dungeons; antagonist</td>
</tr>
</tbody>
</table>
<hr>
<h2>3. Core Gameplay Loop</h2>
<h3>3.1 The Day Cycle</h3>
<p>Each in‑game day is divided into four phases:</p>
<ol>
<li><strong>Morning — Prep (Tavern hub).</strong> Check stock, set the day's menu, eat a buff meal, choose gear, accept customer requests (e.g. &quot;bring me cave troll liver&quot;).</li>
<li><strong>Day — The Delve (Dungeon).</strong> A roguelite run. Fight, harvest, and choose when to return. Deeper = rarer ingredients and more risk.</li>
<li><strong>Evening — Service (Tavern).</strong> Cook and serve using minigames. Earn gold, tips, and renown.</li>
<li><strong>Night — Upgrade (Tavern hub).</strong> Spend earnings on equipment, tavern expansions, recipes, and staff. Story scenes play here. Save point.</li>
</ol>
<h3>3.2 Loop Diagram</h3>
<pre class="mermaid">flowchart LR
    A[Morning Prep] --&gt; B[Delve into the Dungeon]
    B --&gt; C[Evening Service]
    C --&gt; D[Night Upgrades &amp; Story]
    D --&gt; A
    B -- monster parts --&gt; C
    C -- gold &amp; renown --&gt; D
    D -- gear, buffs, unlocks --&gt; B
</pre>
<h3>3.3 How the Two Halves Feed Each Other</h3>
<table>
<thead>
<tr>
<th>From Dungeon to Tavern</th>
<th>From Tavern to Dungeon</th>
</tr>
</thead>
<tbody>
<tr>
<td>Monster parts are ingredients</td>
<td>Gold buys weapons, armor, and tools</td>
</tr>
<tr>
<td>Harvest quality affects dish quality</td>
<td>Pre‑delve meals grant run buffs</td>
</tr>
<tr>
<td>Rare parts unlock new recipes</td>
<td>Customer requests point you at specific monsters</td>
</tr>
<tr>
<td>Found recipe scraps and lore</td>
<td>Refugee staff unlock new dungeon abilities</td>
</tr>
<tr>
<td>Rescued NPCs join the tavern</td>
<td>Stronghold morale grants in‑dungeon &quot;Cheer&quot;</td>
</tr>
</tbody>
</table>
<hr>
<h2>4. Dungeon Gameplay</h2>
<h3>4.1 Combat Feel</h3>
<p>Target feel is <em>Dead Cells</em>: responsive, fast, readable, with heavy hit‑stop and satisfying animation canceling.</p>
<ul>
<li><strong>Movement:</strong> run, jump, double jump (unlockable), dodge roll with i‑frames, wall slide/jump, drop‑through platforms, ledge grab.</li>
<li><strong>Attacks:</strong> primary weapon (combo chains), secondary weapon or shield, two skill slots (tools/throwables), and a special &quot;Kitchen Arts&quot; meter attack.</li>
<li><strong>Feedback:</strong> hit‑stop, screen shake (subtle, toggleable), damage numbers (toggleable), clear enemy telegraphs.</li>
</ul>
<h3>4.2 Weapons as Kitchen Tools</h3>
<p>A signature flavor hook: many weapons are culinary. This also ties weapon choice to harvesting.</p>
<table>
<thead>
<tr>
<th>Weapon Type</th>
<th>Example</th>
<th>Harvest Specialty</th>
</tr>
</thead>
<tbody>
<tr>
<td>Cleaver</td>
<td>Butcher's Cleaver</td>
<td>Clean cuts, bonus to meat quality</td>
</tr>
<tr>
<td>Filleting Blade</td>
<td>Eel‑Tooth Knife</td>
<td>Fast combos, perfect for fish/serpent parts</td>
</tr>
<tr>
<td>Tenderizer</td>
<td>Troll‑Mallet</td>
<td>Stagger damage, softens tough meats (bonus to stews)</td>
</tr>
<tr>
<td>Skewer Spear</td>
<td>Rotisserie Pike</td>
<td>Reach, pins enemies; &quot;spit‑roast&quot; fire variant</td>
</tr>
<tr>
<td>Frying Pan</td>
<td>Iron Skillet</td>
<td>Parry/block weapon; counter hits sear enemies</td>
</tr>
<tr>
<td>Traditional</td>
<td>Swords, axes, bows, staves</td>
<td>Standard harvest; wider combat variety</td>
</tr>
</tbody>
</table>
<p>Weapons have rarity tiers (Common → Fine → Masterwork → Legendary) and random affixes per run, <em>Dead Cells</em> style. Permanent unlocks add weapons to the drop pool.</p>
<h3>4.3 The Harvest System</h3>
<p>The heart of the fantasy. How a monster dies influences what it drops.</p>
<ul>
<li><strong>Clean Kill:</strong> finishing with a matching tool type or a finisher move yields higher quality parts.</li>
<li><strong>Overkill:</strong> excessive damage (big explosions, over‑hits) damages parts, lowering quality or destroying some.</li>
<li><strong>Elemental Kills:</strong> fire‑killed monsters may drop &quot;Seared&quot; parts (pre‑cooked, faster to prepare but some recipes need raw). Ice‑killed monsters drop &quot;Chilled&quot; parts that stay fresh longer. Poison kills make parts inedible.</li>
<li><strong>Harvest Finisher:</strong> when an enemy is low, a prompt allows a quick finisher that guarantees a premium part at the cost of a moment of vulnerability. Risk/reward.</li>
</ul>
<h3>4.4 Inventory, Freshness, and Extraction</h3>
<ul>
<li><strong>The Satchel:</strong> limited carry slots for ingredients, upgradeable in the tavern. Forces choices about what to keep.</li>
<li><strong>Freshness:</strong> parts decay over time in the dungeon (measured in rooms cleared or real time, to be tuned). Salt, ice runes, and preservation jars extend freshness.</li>
<li><strong>Extraction:</strong> the player can return via exit points at the end of each biome (a lift or rope back to the tavern). Leaving early keeps everything; continuing deeper risks it.</li>
<li><strong>Death:</strong> on death, the player keeps a portion of the haul (e.g. items in a protected &quot;Lockbox&quot; slot plus a percentage of the rest), loses the remainder, and loses the day's unspent run currency. Permanent unlocks are never lost. This keeps death painful but not punishing enough to stall the tavern economy.</li>
</ul>
<h3>4.5 Field Cooking (Optional Mechanic)</h3>
<p>At campfire rooms, the player can cook a quick meal from carried parts for a mid‑run heal or buff. This sacrifices ingredients that could be sold, creating a meaningful choice, and echoes the <em>Delicious in Dungeon</em> spirit.</p>
<h3>4.6 Run Structure and Biomes</h3>
<p>Levels are assembled from hand‑authored rooms stitched together procedurally (see Section 10.5). Each biome has 3–5 floors plus a boss.</p>
<table>
<thead>
<tr>
<th>#</th>
<th>Biome</th>
<th>Theme</th>
<th>Signature Ingredients</th>
</tr>
</thead>
<tbody>
<tr>
<td>1</td>
<td>The Cellars</td>
<td>Flooded old cellars and tunnels</td>
<td>Giant rats, slimes, cave mushrooms</td>
</tr>
<tr>
<td>2</td>
<td>Fungal Warrens</td>
<td>Glowing fungal forest</td>
<td>Myconids, spore beetles, walking truffles</td>
</tr>
<tr>
<td>3</td>
<td>Goblin Sprawl</td>
<td>Goblin shanty‑town and mines</td>
<td>Boar‑riders, cave boars, stolen spices</td>
</tr>
<tr>
<td>4</td>
<td>Drowned Halls</td>
<td>Sunken dwarven ruins</td>
<td>Giant eels, crab knights, kelp horrors</td>
</tr>
<tr>
<td>5</td>
<td>Ember Forge</td>
<td>Volcanic dwarven forge</td>
<td>Salamanders, fire drakes, magma snails</td>
</tr>
<tr>
<td>6</td>
<td>Frostvault</td>
<td>Frozen crypts</td>
<td>Ice trolls, wyrm eggs, frost wraiths</td>
</tr>
<tr>
<td>7</td>
<td>The Rootdeep</td>
<td>Living, pulsing underworld</td>
<td>Aberrations, dragon cuts, legendary parts</td>
</tr>
<tr>
<td>—</td>
<td>The Heart</td>
<td>Final area</td>
<td>Final boss</td>
</tr>
</tbody>
</table>
<p>Branching paths between biomes (as in <em>Dead Cells</em>) let players choose which ingredients to target on a given run.</p>
<h3>4.7 Enemies and Bosses</h3>
<p>Each enemy has a <strong>combat profile</strong> (behavior, attacks, telegraphs) and a <strong>harvest profile</strong> (parts, preferred kill method, freshness rate). Bosses drop signature ingredients that unlock &quot;Legendary Dishes&quot; and progress the story.</p>
<p>Example bosses: <em>The Cellar King</em> (giant rat monarch), <em>Grandmother Spore</em> (myconid matriarch), <em>Chieftain Gutgrin</em> (goblin warlord on a war boar), <em>The Leviathan Eel</em>, <em>Forge‑Drake Cindermaw</em>, <em>The Frost Troll Queen</em>, <em>The Warden Below</em> (final).</p>
<hr>
<h2>5. Ingredients and Recipes</h2>
<h3>5.1 Ingredient Properties</h3>
<p>Every ingredient is data‑driven (ScriptableObject) with:</p>
<ul>
<li><strong>Category:</strong> Meat, Offal, Fish, Fungus, Plant, Egg, Spice, Liquid, Magical.</li>
<li><strong>Flavor Tags:</strong> Savory, Sweet, Spicy, Sour, Bitter, Umami, Earthy, Arcane.</li>
<li><strong>Quality:</strong> Poor / Standard / Fine / Premium (from the Harvest system).</li>
<li><strong>Freshness:</strong> 0–100%, decays over time; affects dish score.</li>
<li><strong>Rarity:</strong> Common → Legendary; affects price.</li>
<li><strong>Special Effects:</strong> some ingredients carry buffs (e.g. Fire Drake Heart grants fire resistance when eaten).</li>
</ul>
<h3>5.2 Example Ingredient Table</h3>
<table>
<thead>
<tr>
<th>Monster</th>
<th>Part</th>
<th>Category</th>
<th>Flavor</th>
<th>Notes</th>
</tr>
</thead>
<tbody>
<tr>
<td>Giant Rat</td>
<td>Haunch</td>
<td>Meat</td>
<td>Savory</td>
<td>Staple early meat</td>
</tr>
<tr>
<td>Green Slime</td>
<td>Gel</td>
<td>Liquid</td>
<td>Sweet</td>
<td>Used in jellies and drinks</td>
</tr>
<tr>
<td>Myconid</td>
<td>Cap</td>
<td>Fungus</td>
<td>Earthy, Umami</td>
<td>Great in stews</td>
</tr>
<tr>
<td>Cave Boar</td>
<td>Belly</td>
<td>Meat</td>
<td>Savory</td>
<td>Premium when killed with Cleaver</td>
</tr>
<tr>
<td>Giant Eel</td>
<td>Fillet</td>
<td>Fish</td>
<td>Umami</td>
<td>Spoils fast; needs chilling</td>
</tr>
<tr>
<td>Salamander</td>
<td>Tail</td>
<td>Meat</td>
<td>Spicy</td>
<td>Arrives &quot;Seared&quot; if fire‑killed</td>
</tr>
<tr>
<td>Ice Troll</td>
<td>Liver</td>
<td>Offal</td>
<td>Bitter</td>
<td>Grants frost resistance</td>
</tr>
<tr>
<td>Fire Drake</td>
<td>Heart</td>
<td>Magical</td>
<td>Spicy, Arcane</td>
<td>Legendary dish ingredient</td>
</tr>
</tbody>
</table>
<h3>5.3 Recipes</h3>
<ul>
<li>Recipes are discovered through NPCs, recipe scraps found in the dungeon, customer hints, and experimentation.</li>
<li>Each recipe has required ingredient slots (by category or specific item) and optional slots that add flavor tags and bonuses.</li>
<li><strong>Experimentation:</strong> combining ingredients freely at the &quot;Test Kitchen&quot; can discover new recipes. Failed experiments produce funny &quot;Questionable Stew&quot;.</li>
<li>Dish score = base recipe value × ingredient quality × freshness × minigame performance.</li>
</ul>
<hr>
<h2>6. Tavern Gameplay</h2>
<h3>6.1 Service Phase</h3>
<p>During evening service the camera shows the tavern floor and kitchen in a side view. Customers enter, sit, and order from the menu the player set that morning. The player moves between stations to cook, pour, and serve, with staff helping as they're unlocked. Service lasts a fixed in‑game time (e.g. 5–8 real minutes at launch, tuned in playtesting).</p>
<h3>6.2 Minigames</h3>
<p>Each station is a short, skill‑based minigame. Staff can auto‑complete stations at reduced quality so the player can focus on others.</p>
<table>
<thead>
<tr>
<th>Station</th>
<th>Minigame</th>
<th>Skill</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Butcher Block</strong></td>
<td>Follow cut lines on a monster part; accuracy sets portion count</td>
<td>Precision</td>
</tr>
<tr>
<td><strong>Grill / Pan</strong></td>
<td>Flip at the right moment; watch a doneness meter</td>
<td>Timing</td>
</tr>
<tr>
<td><strong>Stew Pot</strong></td>
<td>Add ingredients in order, stir to keep temperature in a band</td>
<td>Rhythm/management</td>
</tr>
<tr>
<td><strong>Oven</strong></td>
<td>Set heat and pull at the right time while multitasking</td>
<td>Timing</td>
</tr>
<tr>
<td><strong>Tap &amp; Brew</strong></td>
<td>Pour ale/mead to the line with correct foam; mix cocktails and potions</td>
<td>Precision</td>
</tr>
<tr>
<td><strong>Plating</strong></td>
<td>Arrange garnish quickly for presentation bonus</td>
<td>Speed</td>
</tr>
<tr>
<td><strong>Serving</strong></td>
<td>Carry plates across a busy floor, avoid collisions</td>
<td>Movement</td>
</tr>
<tr>
<td><strong>Bouncer</strong></td>
<td>Rowdy customers occasionally brawl; quick combat‑lite minigame to throw them out</td>
<td>Reflex</td>
</tr>
</tbody>
</table>
<p>Additional minigames can be introduced over time (fermentation, bread proofing, spice grinding) to keep service fresh through the campaign.</p>
<h3>6.3 Customers</h3>
<ul>
<li><strong>Types:</strong> villagers, adventurers, dwarves, elves, orcs, merchants, nobles, refugees, and eventually soldiers and heroes.</li>
<li><strong>Preferences:</strong> each race/type has favorite flavor tags and categories (e.g. dwarves love savory and strong ale; elves prefer herbs and fungus; orcs demand big meat portions).</li>
<li><strong>Patience:</strong> a timer; slow service lowers tips and reviews.</li>
<li><strong>Special Guests:</strong> named characters with unique requests that drive story, unlock recipes, or give quests.</li>
<li><strong>Reviews and Renown:</strong> satisfied customers raise the tavern's Renown, which attracts better‑paying clientele and unlocks story beats.</li>
</ul>
<h3>6.4 The Growing Stronghold</h3>
<p>The tavern evolves across the acts. Each stage adds visual changes, new rooms, and mechanics.</p>
<table>
<thead>
<tr>
<th>Stage</th>
<th>Name</th>
<th>Adds</th>
</tr>
</thead>
<tbody>
<tr>
<td>1</td>
<td>The Inn</td>
<td>Kitchen, bar, small dining room</td>
</tr>
<tr>
<td>2</td>
<td>The Sanctuary</td>
<td>Guest rooms, refugee quarters, herb garden, storeroom</td>
</tr>
<tr>
<td>3</td>
<td>The Stronghold</td>
<td>Walls, watchtower, forge, training yard, brewery, great hall</td>
</tr>
<tr>
<td>4</td>
<td>The Bastion</td>
<td>War room, shrine, feast hall for the finale</td>
</tr>
</tbody>
</table>
<p><strong>Residents:</strong> refugees who move in can be assigned roles (cook, server, gardener, smith, guard). Each resident has a small personal questline.</p>
<p><strong>Morale and Cheer:</strong> the stronghold has a Morale value driven by food quality, housing, and story events. High morale grants <strong>Cheer</strong> in the dungeon: temporary buffs, extra revives, or crowd &quot;chants&quot; that power up the Kitchen Arts meter. This makes the story theme of people rallying behind you a real mechanic.</p>
<p><strong>Defense Events (optional, later scope):</strong> occasionally monsters breach the surface and attack the stronghold. The player defends in a short side‑scrolling combat encounter with residents helping.</p>
<hr>
<h2>7. Progression and Economy</h2>
<h3>7.1 Currencies</h3>
<table>
<thead>
<tr>
<th>Currency</th>
<th>Earned From</th>
<th>Spent On</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Gold</strong></td>
<td>Service, selling surplus ingredients</td>
<td>Gear, tavern upgrades, recipes, staff wages</td>
</tr>
<tr>
<td><strong>Renown</strong></td>
<td>Customer satisfaction, story</td>
<td>Unlocks tiers of customers, story progress (not spent)</td>
</tr>
<tr>
<td><strong>Delve Marks</strong></td>
<td>Found in dungeon runs (lost on death if unspent)</td>
<td>Permanent combat unlocks at the &quot;Delver's Board&quot;</td>
</tr>
<tr>
<td><strong>Relics</strong></td>
<td>Bosses, secrets</td>
<td>Major permanent abilities (double jump, dash, etc.)</td>
</tr>
</tbody>
</table>
<h3>7.2 Upgrade Tracks</h3>
<ul>
<li><strong>Combat:</strong> weapon blueprints (added to drop pools), armor, satchel size, preservation tools, health flasks.</li>
<li><strong>Traversal:</strong> metroidvania‑style relics that open shortcuts and hidden rooms.</li>
<li><strong>Tavern:</strong> stations, furniture, seating capacity, decor (decor raises customer satisfaction), room expansions.</li>
<li><strong>Staff:</strong> hire and train residents; staff skill levels affect auto‑complete quality.</li>
</ul>
<h3>7.3 Economy Balance Goals</h3>
<ul>
<li>A good delve should fund roughly one meaningful upgrade.</li>
<li>Selling raw ingredients should be viable but noticeably worse than cooking them.</li>
<li>Staff wages and refugee upkeep create light pressure without becoming a punishing survival mechanic.</li>
</ul>
<hr>
<h2>8. Art and Audio Direction</h2>
<h3>8.1 Visual Style</h3>
<p>Options to decide early (see Open Questions):</p>
<ul>
<li><strong>Option A — High‑res hand‑painted 2D</strong> with skeletal animation (Unity 2D Animation package or Spine). Scales well for many enemy variants.</li>
<li><strong>Option B — Detailed pixel art</strong> in the spirit of <em>Dead Cells</em> and <em>Dave the Diver</em>'s 2D characters. Strong genre fit, but animation‑heavy.</li>
</ul>
<p>In either case: warm, saturated tavern palettes (amber candlelight, wood, hearth) contrasted with cool, eerie dungeon palettes (teal, violet, bioluminescence). Food should look genuinely appetizing, with close‑up &quot;dish reveal&quot; art for each recipe.</p>
<h3>8.2 UI</h3>
<p>Rustic fantasy UI: parchment menus, wooden signage, chalkboard menu board. Readable during fast combat, with a minimal HUD in the dungeon.</p>
<h3>8.3 Audio</h3>
<ul>
<li><strong>Tavern:</strong> folk instrumentation (fiddle, lute, accordion, bodhrán); music gains layers as the tavern grows and more residents join in.</li>
<li><strong>Dungeon:</strong> darker, percussive, biome‑specific themes that intensify in combat.</li>
<li><strong>SFX:</strong> chunky, satisfying combat impacts; sizzling, chopping, pouring, and crowd chatter in the tavern.</li>
<li><strong>Voice:</strong> grunts and barks (&quot;Hmm!&quot;, &quot;Aye!&quot;) rather than full voice acting, for scope.</li>
</ul>
<hr>
<h2>9. Controls (Default Controller Layout)</h2>
<table>
<thead>
<tr>
<th>Action</th>
<th>Dungeon</th>
<th>Tavern</th>
</tr>
</thead>
<tbody>
<tr>
<td>Left Stick</td>
<td>Move</td>
<td>Move between stations</td>
</tr>
<tr>
<td>A / Cross</td>
<td>Jump</td>
<td>Interact / confirm</td>
</tr>
<tr>
<td>X / Square</td>
<td>Primary attack</td>
<td>Minigame action</td>
</tr>
<tr>
<td>Y / Triangle</td>
<td>Secondary attack</td>
<td>Minigame alt action</td>
</tr>
<tr>
<td>B / Circle</td>
<td>Dodge roll</td>
<td>Cancel / back</td>
</tr>
<tr>
<td>LB / RB</td>
<td>Skills 1 and 2</td>
<td>Cycle orders</td>
</tr>
<tr>
<td>RT</td>
<td>Kitchen Arts special</td>
<td>Speed up (hold)</td>
</tr>
<tr>
<td>LT</td>
<td>Harvest finisher</td>
<td>—</td>
</tr>
<tr>
<td>Start</td>
<td>Pause menu</td>
<td>Pause menu</td>
</tr>
</tbody>
</table>
<p>All controls remappable via the Unity Input System.</p>
<hr>
<h2>10. Technical Design (Unity 6.3 LTS)</h2>
<h3>10.1 Engine Configuration</h3>
<ul>
<li><strong>Render Pipeline:</strong> Universal Render Pipeline (URP) with the 2D Renderer, 2D lights and shadow casters for torchlit dungeons and a warm tavern.</li>
<li><strong>Input:</strong> Input System package with separate action maps (<code>Dungeon</code>, <code>Tavern</code>, <code>UI</code>, <code>Minigame</code>) and runtime rebinding.</li>
<li><strong>Camera:</strong> Cinemachine for follow cameras, confiner bounds per room, and impulse‑based screen shake.</li>
<li><strong>UI:</strong> UI Toolkit for menus and HUD; uGUI where world‑space UI is easier (customer speech bubbles, order tickets).</li>
<li><strong>Animation:</strong> 2D Animation package (or Spine runtime) for skeletal characters; Animator or a code‑driven state machine for combat.</li>
<li><strong>Physics:</strong> Physics 2D with a custom kinematic character controller (not Rigidbody‑driven) for tight platforming.</li>
<li><strong>Content Loading:</strong> Addressables for biome assets, room prefabs, and localization.</li>
<li><strong>Localization:</strong> Unity Localization package from day one.</li>
</ul>
<p><em>Note: confirm package versions against the Unity 6.3 LTS package manifest at project setup.</em></p>
<h3>10.2 Scene Structure</h3>
<ul>
<li><code>Boot</code> — initializes services (save, audio, input, localization) and persists.</li>
<li><code>MainMenu</code></li>
<li><code>Tavern</code> — hub scene for Prep, Service, and Night phases.</li>
<li><code>Dungeon</code> — single scene into which biome rooms are loaded procedurally.</li>
<li><code>Cutscene</code> scenes as needed (or Timeline sequences inside Tavern).</li>
</ul>
<p>Additive scene loading keeps the persistent <code>Boot</code> services alive.</p>
<h3>10.3 Architecture Overview</h3>
<ul>
<li><strong>Data‑driven design with ScriptableObjects:</strong> <code>IngredientDefinition</code>, <code>RecipeDefinition</code>, <code>EnemyDefinition</code>, <code>WeaponDefinition</code>, <code>CustomerProfile</code>, <code>BiomeDefinition</code>, <code>RoomDefinition</code>, <code>TavernUpgradeDefinition</code>.</li>
<li><strong>Game State Machine:</strong> a top‑level <code>GameFlowManager</code> drives phases (Prep → Delve → Service → Night) and scene transitions.</li>
<li><strong>Event channels:</strong> ScriptableObject‑based event channels (or a lightweight event bus) to decouple systems, e.g. <code>OnEnemyKilled</code> → Harvest system → Inventory.</li>
<li><strong>Character controller:</strong> state machine (Idle, Run, Jump, Fall, Dodge, Attack, Hurt, Dead) with frame‑data‑driven attacks (startup, active, recovery frames, cancel windows).</li>
<li><strong>Combat:</strong> hitbox/hurtbox components, damage pipeline with modifiers (element, crit, overkill), and hit‑stop via a time‑scale service.</li>
<li><strong>Minigames:</strong> each station implements an <code>IMinigame</code> interface (Begin, Tick, Evaluate → score 0–1), making it easy to add new minigames and to let staff auto‑resolve.</li>
<li><strong>Customer AI:</strong> simple state machine (Enter, Queue, Seat, Order, Wait, Eat, Pay, Leave) with a patience timer and preference scoring.</li>
</ul>
<h3>10.4 Key Systems</h3>
<table>
<thead>
<tr>
<th>System</th>
<th>Responsibility</th>
</tr>
</thead>
<tbody>
<tr>
<td><code>HarvestSystem</code></td>
<td>Determines drops from kill context (weapon type, element, overkill)</td>
</tr>
<tr>
<td><code>InventorySystem</code></td>
<td>Satchel, storeroom, freshness decay, preservation modifiers</td>
</tr>
<tr>
<td><code>RecipeSystem</code></td>
<td>Recipe matching, experimentation, dish scoring</td>
</tr>
<tr>
<td><code>ServiceSystem</code></td>
<td>Customer spawning, orders, timers, payment, reviews</td>
</tr>
<tr>
<td><code>EconomySystem</code></td>
<td>Currencies, prices, wages</td>
</tr>
<tr>
<td><code>ProgressionSystem</code></td>
<td>Unlocks, relics, tavern stages, story flags</td>
</tr>
<tr>
<td><code>StoryManager</code></td>
<td>Act progression, dialogue triggers (e.g. Yarn Spinner or Ink integration)</td>
</tr>
<tr>
<td><code>SaveSystem</code></td>
<td>JSON serialization of persistent state; autosave at Night phase</td>
</tr>
<tr>
<td><code>LevelGenerator</code></td>
<td>Builds dungeon floors from room graphs</td>
</tr>
</tbody>
</table>
<h3>10.5 Procedural Level Generation</h3>
<p>Approach similar to <em>Dead Cells</em>: designer‑authored <strong>room prefabs</strong> stitched together by a <strong>graph‑based generator</strong>.</p>
<ol>
<li>Each biome defines a floor template graph (entrance, combat rooms, treasure, campfire, shop, secret, exit, boss).</li>
<li>The generator picks room prefabs matching each node's type and required door connections.</li>
<li>Rooms are placed on a grid with connection validation to avoid overlap.</li>
<li>Enemies and loot spawn from weighted tables per biome and depth.</li>
<li>Seeds are stored for debugging and potential daily‑challenge modes.</li>
</ol>
<h3>10.6 Save Data</h3>
<p>Persistent: tavern stage and upgrades, unlocked weapons/relics/recipes, storeroom inventory, currencies, residents, story flags, settings. Run state is saved only at biome transitions to prevent save‑scumming (optionally allow a &quot;suspend run&quot; save).</p>
<h3>10.7 Suggested Project Folder Structure</h3>
<pre><code>Assets/
  _Project/
    Art/            (Characters, Enemies, Environments, UI, VFX)
    Audio/          (Music, SFX)
    Data/           (Ingredients, Recipes, Enemies, Weapons, Biomes, Customers)
    Prefabs/        (Player, Enemies, Rooms, Tavern, UI)
    Scenes/
    Scripts/
      Core/         (GameFlow, Save, Events, Services)
      Dungeon/      (Player, Combat, Enemies, Harvest, LevelGen)
      Tavern/       (Service, Customers, Minigames, Stations, Upgrades)
      Shared/       (Inventory, Economy, Progression, Story)
      UI/
    Settings/       (Input actions, URP assets, Addressables)
    Localization/
</code></pre>
<hr>
<h2>11. Scope and Milestones</h2>
<h3>11.1 Recommended Development Phases</h3>
<table>
<thead>
<tr>
<th>Phase</th>
<th>Goal</th>
<th>Contents</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>1. Prototype: Combat</strong></td>
<td>Prove the dungeon feels good</td>
<td>Player controller, one weapon, 3 enemies, greybox rooms, harvest drops</td>
</tr>
<tr>
<td><strong>2. Prototype: Tavern</strong></td>
<td>Prove service is fun</td>
<td>3 minigames (grill, pour, serve), 3 customer types, 5 recipes</td>
</tr>
<tr>
<td><strong>3. Loop Prototype</strong></td>
<td>Prove the halves connect</td>
<td>Full day cycle, inventory carryover, gold, 3 upgrades</td>
</tr>
<tr>
<td><strong>4. Vertical Slice</strong></td>
<td>Represent final quality</td>
<td>Biome 1 fully arted + boss, Stage 1 tavern polished, Act I opening story</td>
</tr>
<tr>
<td><strong>5. Production</strong></td>
<td>Content build‑out</td>
<td>Biomes 2–7, all minigames, stronghold stages, full story</td>
</tr>
<tr>
<td><strong>6. Polish and Launch</strong></td>
<td>Ship</td>
<td>Balance, accessibility, localization, performance, platform certification</td>
</tr>
</tbody>
</table>
<h3>11.2 Scope Warning</h3>
<p>Two full games in one is ambitious, especially for a small team. Recommended guardrails: keep minigames short and reusable; build a strong, small vertical slice before expanding; consider Early Access with 3–4 biomes and the first two tavern stages, adding later acts in updates.</p>
<hr>
<h2>12. Accessibility</h2>
<ul>
<li>Remappable controls, hold/toggle options.</li>
<li>Adjustable combat speed and &quot;assist mode&quot; (damage taken, freshness decay, customer patience).</li>
<li>Minigame assist: wider timing windows or auto‑complete.</li>
<li>Colorblind‑safe telegraphs and freshness indicators (use shapes/icons, not color alone).</li>
<li>Screen shake and flash intensity sliders; text size options.</li>
</ul>
<hr>
<h2>13. Open Questions</h2>
<ol>
<li><strong>Art style:</strong> pixel art</li>
<li><strong>Protagonist:</strong> customizable</li>
<li><strong>Time pressure:</strong> No calendar deadline, but like Dave the Diver can only delve into dungeon with enough "essence" which depletes over time or when you take damage. It can be upgraded eventually.</li>
<li><strong>Death penalty tuning:</strong> lose everything except 1 item you choose to keep</li>
<li><strong>Stronghold defense events:</strong> core feature or post‑launch?</li>
<li><strong>Co‑op:</strong> No. </li>
<li><strong>Dialogue tooling:</strong> Yarn Spinner</li>
<li><strong>Monetization:</strong> premium only (recommended for this genre) with possible paid expansions.</li>
</ol>
<hr>
<h2>14. Appendix: Glossary</h2>
<ul>
<li><strong>Delve:</strong> a single roguelite run into the dungeon.</li>
<li><strong>Haul:</strong> ingredients carried back from a delve.</li>
<li><strong>Harvest Finisher:</strong> a special kill move that guarantees a premium part.</li>
<li><strong>Kitchen Arts:</strong> the player's special meter attack.</li>
<li><strong>Cheer:</strong> in‑dungeon buffs granted by stronghold morale.</li>
<li><strong>Renown:</strong> the tavern's reputation, driving customer tiers and story.</li>
<li><strong>Satchel / Lockbox:</strong> carry inventory / protected slot kept on death.</li>
</ul>


<!--claude-mermaid-runtime-begin:3477-->
<style>.mermaid-diagram{margin-block:4px}.mermaid-diagram svg{display:block;margin:0 auto;max-width:100%;height:auto}</style>
<script src="/_runtime/mermaid-11.16.1.min.js"></script>
<script>(function(){
var CFG={"palettes":{"light":{"surface":"#f4efe4","text":"#42392e","line":"#8a7f6d","border":"#7a6c52","bg":"#fffdf8"},"dark":{"surface":"#262b34","text":"#f2f3f5","line":"#a8adb8","border":"#9aa4b8","bg":"#1f232b"}}};
if(typeof mermaid==='undefined')return;
var pres=Array.prototype.slice.call(document.querySelectorAll('pre.mermaid')).filter(function(p){if(p.hasAttribute('data-claude-mermaid-claimed'))return false;p.setAttribute('data-claude-mermaid-claimed','1');return true;});
if(!pres.length)return;
var mq=window.matchMedia?window.matchMedia('(prefers-color-scheme: dark)'):null;
var root=document.documentElement;
var items=pres.map(function(pre){
var mount=document.createElement('div');mount.className='mermaid-diagram';
return {pre:pre,mount:mount,src:pre.textContent||''};
});
var seq=0;
var renderGen=0;
var lastKey='';
function pageBg(fallback){
var els=[document.body,document.documentElement];
for(var i=0;i<els.length;i++){
var c=els[i]&&getComputedStyle(els[i]).backgroundColor;
if(c&&c!=='transparent'&&c!=='rgba(0, 0, 0, 0)')return c;
}
return fallback;
}
function render(){
var theme=root.getAttribute('data-theme');
var dark=theme==='dark'||(!!(mq&&mq.matches)&&theme!=='light');
var pal=dark?CFG.palettes.dark:CFG.palettes.light;
var bg=pageBg(pal.bg);
var key=(dark?'d':'l')+'|'+bg;
if(key===lastKey)return;
lastKey=key;
var gen=++renderGen;
var font=getComputedStyle(document.body).fontFamily||'sans-serif';
var nat={useMaxWidth:false};
mermaid.initialize({
startOnLoad:false,securityLevel:'strict',theme:'base',
flowchart:nat,sequence:nat,er:nat,state:nat,class:nat,pie:nat,
gantt:nat,journey:nat,timeline:nat,gitGraph:nat,mindmap:nat,xyChart:nat,
quadrantChart:nat,sankey:nat,c4:nat,requirement:nat,block:nat,
packet:nat,kanban:nat,architecture:nat,radar:nat,
themeVariables:{background:bg,mainBkg:pal.surface,primaryColor:pal.surface,
primaryTextColor:pal.text,lineColor:pal.line,primaryBorderColor:pal.border,
nodeBorder:pal.border,clusterBorder:pal.border,edgeLabelBackground:bg,
clusterBkg:'rgba(127,127,127,0.07)',titleColor:pal.text,
darkMode:dark,rowOdd:bg,rowEven:'rgba(127,127,127,0.07)',
attributeBackgroundColorOdd:bg,attributeBackgroundColorEven:'rgba(127,127,127,0.07)',
fontSize:'16px',fontFamily:font},
themeCSS:'.node rect, .node circle, .node polygon, .node path, .cluster rect { stroke-width: 2px; }'
});
items.forEach(function(it){
var id='claude-mermaid-'+seq++;
mermaid.render(id,it.src).then(function(r){
if(gen!==renderGen)return;
var prev=it.pre.previousElementSibling;
if(prev&&prev.className==='mermaid-diagram'&&prev!==it.mount)return;
it.mount.innerHTML=r.svg;
if(!it.mount.parentNode)it.pre.parentNode.insertBefore(it.mount,it.pre);
it.pre.style.display='none';
},function(){
var scratch=document.getElementById(id);
if(scratch)scratch.parentNode.removeChild(scratch);
scratch=document.getElementById('d'+id);
if(scratch)scratch.parentNode.removeChild(scratch);
if(gen!==renderGen)return;
if(it.mount.parentNode)it.mount.parentNode.removeChild(it.mount);
it.pre.style.display='';
});
});
}
render();
if(mq&&mq.addEventListener)mq.addEventListener('change',render);
if(typeof MutationObserver!=='undefined')new MutationObserver(render).observe(root,{attributes:true,attributeFilter:['data-theme']});
})();</script>
<!--claude-mermaid-runtime-end-->
</body></html>