# 4i-D: your checklist (the release builds)

Play the **release** builds: the web build from the itch page once it's up (or `Builds/WebRelease` served locally), and the Windows build from `Builds/Windows`. Note the version in the menu's corner on each.

## A. Checking the IL2CPP build on a clean machine

A clean machine is one that has never had Unity, Visual Studio or this project on it: a friend's PC, an old laptop, or **Windows Sandbox** on this PC (Windows Pro: *Turn Windows features on or off → Windows Sandbox*; it starts empty every time and throws everything away when closed).

1. Copy `HearthAndHollows-<version>-windows.zip` onto it and unzip it anywhere (the Desktop is fine).
2. Check the folder holds `HearthAndHollows.exe`, `HearthAndHollows_Data`, `GameAssembly.dll`, `UnityPlayer.dll`, `D3D12` and `version.txt`, and **no** folder called `…BackUpThisFolder_ButDontShipItWithYourGame`.
3. Run `HearthAndHollows.exe`. If Windows SmartScreen warns about an unknown publisher: *More info → Run anyway* (expected: the build isn't code-signed; testers will see this too, and the itch text tells them).
4. The menu should appear within about five seconds, with music silent (the menu is quiet by design) and the version in the bottom-right corner matching `version.txt`.
5. **New Game**, make a keeper, play arrival day down the hatch into the first delve: the Hollows' music starts, hits are heard.
6. Pause (Esc), **quit to the menu**, **Continue**: you're back at the night's delve from its start (by design).
7. **Options:** set the window to 1280×720, then fullscreen, then back; move the music slider. Quit the game completely and run it again: the options are as you left them.
8. Plug in a controller: the prompts switch to its buttons on the first press.
9. Your files are in `%USERPROFILE%\AppData\LocalLow\sagaphy\Hearthdelve\` (`save_slot_1.json`, `options.json`). If anything goes wrong, send me `Player.log` from that folder.
10. Optional, a minute: in a command prompt in the game's folder run `HearthAndHollows.exe -perf -perfQuit`. It plays a scripted day by itself in a save folder of its own (yours is untouched) and writes `perf_<date>.txt` into the folder in step 9. Send it to me: it's that machine's frame times, load times and memory.

## B. Every real-controller check, in one place

Carried from 4i-A, 4i-B and 4i-C, with 4i-D's builds. On the **Windows** build (the web build has no rumble, by design: haptics do nothing in browsers).

1. **The main menu, the creator and the pause menu** with the d-pad and stick: everything reachable, A chooses, B backs out. In the creator: change body, colors and name (the on-screen letters) with the pad alone.
2. **Prompts:** switch between keyboard and controller mid-game (garden, stations, doors): the prompts follow the last device you touched, without flicker.
3. **Menus with nothing selected:** at night with "sleep" and "decorate", click empty space with the mouse, then push the stick or d-pad: the first push lands on the top choice, the next moves. The same in a conversation's choices and in the creator.
4. **Options:** LB/RB change tabs, the d-pad steps each line, B backs out; open it from the main menu and from the pause menu.
5. **Conversations:** A moves on and chooses; hold A through a long line: it hurries but never skips a choice.
6. **Rumble:** strength at 10%, 50% and 100%, then reduced vibration on: each sample distinctly weaker; a hit in the Hollows matches what Options showed. Vibration off: nothing rumbles.
7. **The Hollows:** right stick aims (and the keeper faces it), X light combo, Y heavy (hold to charge), B dodge, LT the Harvest Finisher, A the rope and swapping a part. With the right stick centred, attacks go the way you walk.
8. **The tavern's stations:** the grill, the tap, chopping and the Butcher Block on the pad (left stick to tilt or move the knife, A to act, B to step away).
9. **Decorate Mode** on the pad: View to enter, A pick up and place, X/Y turn and flip, hold LB to place to the pixel, RT put away, LT undo, Start the catalog, R3/L3 colors and the other room.
10. **The controls page** (pause → controls): each page names the controller's buttons correctly.

## C. The release builds themselves

1. **Web, from the itch page** (once you've published it, logged out, with the password): it loads, the menu shows the version, and sound starts after your first click. Play the opening and a day.
2. **Web memory:** play three or four days in one tab. Chrome's task manager (*Shift+Esc*) should show the game's tab settling around 850 MB and staying there (it used to climb about 200 MB a day; fixed in 4i-D).
3. **The final 4h checklist, the judgement half** (the mechanical half passes in the release builds, see `PLAN_4I.md`, As built: 4i-D): does Gimp's night surprise, amuse and unsettle, abrasive but not a joke, and does it say something about Phi? Ask Gimp about Maximo at the bar. Do the overheard lines make people feel like old neighbours, never chatter? Does dinner feel like the village's evening? Ogrin's window after five on several evenings.
4. **The version** is readable in the menu's and the pause menu's corner, on one line.

## D. The external playtest

1. Publish the itch page (`itch-page.md`) as Restricted, with a password.
2. Make the form from `questionnaire.md` (Google Forms or Tally) and put its link in the itch text and the tester page.
3. Send 3–5 friends or family the page link, the password, the form link, and the tester page (`how-to-play.html`).
4. When the answers are in, send them to me: I'll triage them into blocking fixes (in before closeout) and Phase 5.
