# The itch.io page (for you to publish)

Everything to paste, in itch's order (*Dashboard → Create new project*). Nothing here is published until you press Save with the visibility below.

## Settings

| Field | Value |
|---|---|
| Title | Hearth & Hollows |
| Project URL | `hearth-and-hollows` (or anything you like: the page is private) |
| Short description or tagline | Run a tavern in a strange village by day; delve into the Hollows by night for what goes in the pot. |
| Classification | Games |
| Kind of project | HTML (you upload the web build; the Windows zip goes alongside as a download) |
| Release status | Prototype |
| Pricing | No payments |
| Uploads | `HearthAndHollows-<version>-web.zip`: tick **This file will be played in the browser**. `HearthAndHollows-<version>-windows.zip`: tick **Windows** |
| Embed options | Viewport dimensions **1280 × 720**; tick **Fullscreen button**; leave **Mobile friendly** off; leave **Automatically start on page load** off (a click to start lets the sound play); **SharedArrayBuffer support** off |
| Frame options | leave **Enable scrollbars** off |
| Details: genre | Simulation |
| Tags | cozy, pixel-art, roguelite, cooking, top-down, fantasy, life-simulation, tavern |
| AI generation disclosure | Answer as is true for the project (the code was written with an AI assistant; the art, music and sounds are licensed packs by human artists, credited in the game) |
| App store links, community | none |
| Visibility & access | **Restricted**, then set a **password** under *Visibility*, and *Save* |

The web build: zip the *contents* of `Builds/WebRelease` (`index.html` at the top of the zip, not inside a folder). It's Brotli-compressed with the decompression fallback, so it plays on itch as it is (itch doesn't send Brotli headers; the fallback decompresses in the page).

The Windows build: zip `Builds/Windows` **without** the `HearthAndHollows_BackUpThisFolder_ButDontShipItWithYourGame` folder (IL2CPP's debugging symbols, 1.5 GB, never shipped). `version.txt` sits beside the exe.

## Page text

> **Hearth & Hollows** · playtest build, version `<version>`
>
> You've inherited Tally Ho!, the tavern in the odd little village of Kariaston, from someone who vanished into the Hollows beneath it. By day, walk the village, tend your garden, shop at Musashi's cart and get to know your neighbours. In the evening, cook and serve. At night, go down into the Hollows and bring back what the surface can't grow.
>
> **This is an early playtest.** Thank you for trying it! Please play at least two in-game days (a day ends when you sleep), then fill in the short questionnaire: **[the form link]**. About five minutes.
>
> **How to play** (and the controls, known issues and how to start over): **[the tester page link]**
>
> **Play in the browser** above (Chrome, Edge or Firefox on a computer), or **download for Windows** below (unzip anywhere and run `HearthAndHollows.exe`; Windows may warn about an unknown publisher: *More info → Run anyway*).
>
> Keyboard and mouse or a controller (Xbox or PlayStation) both work. Your progress saves itself at safe moments: when you sleep, after the evening, and when a delve ends.
>
> Art: Minifantasy by Krishna Palacio. Music: HeatleyBros. Sounds: Leohpaz, Kenney, OwlishMedia. Font: Silver by Poppy Works. The full credits are in the game's menu.

## Before sending the link

1. Open the page logged out (a private window), enter the password, and start the web build: the menu shows the version.
2. Download the Windows zip and check it unzips with `HearthAndHollows.exe`, `HearthAndHollows_Data`, `GameAssembly.dll`, `UnityPlayer.dll` and `version.txt`, and nothing called *BackUpThisFolder*.
3. Send each tester the page link, the password and the form link.
