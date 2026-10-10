# Kariaston layout mockups (2026-10-10)

Reference only: these scripts draw still mockups of Kariaston straight from the raw Minifantasy packs. The game never uses them, and they don't touch Unity.

The owner chose **layout 2, Crossroads** (`layout2_crossroads.png`) as the direction for the village. The other four layouts are kept for comparison.

## Running

Python 3 with Pillow and NumPy. The packs are read from `C:\Dev\Minifantasy`; set `MINIFANTASY_ROOT` to use another path.

    python Tools/village/mockup/layout2_crossroads.py [out.png]

The output is drawn at 3x. One tile is 8 px, and coordinates in the layout scripts are tiles, origin top-left with y down. (Unity's y points up and Kariaston's origin is (200, 0), so convert when porting.)

## What's where

- `kar.py`, the engine. `P` gives the source sheet paths. `S` is the sprite catalogue: sheet, rect (x, y, w, h in pixels from the top-left), pivot in px above the art's base, and a shadow sheet. `AT` lists the autotiles by sheet and the 3x3 block's origin cell, with inner corners in the two rows below (NW, NE, NW+SE / SW, SE, NE+SW).
- `common.py` and `opt1.py` hold the shared helpers: garden beds, the thin farm fence, the memorial, plots, dressing.
- In each `layoutN_*.py`, `m.put(key, x, y)` places a sprite with its foot at tile (x, y). `m.layer(name, mask)` lays an autotiled ground region.

## The look the mockups use

- Roads and the square: Medieval City `Tileset/Tileset.png`. Dark dirt on grass is cells (14,138). Cobble in dirt is (18,138), and denser cobble is (22,138).
- Shadows: Plants & Foliage `Plains_And_Forests/_Shadow.png` and Towns II `MoreBuildingSamplesShadows.png` share their art's coordinates.
- Grass patches: `More_Grass_Variations` and `Tall_Grass_Tileset`. Flowers and tufts come from Plants & Foliage `Forgotten_Plains.png`, in clusters.
- Garden: Farm crops in Medieval City soil, inside the Farm tileset's thin fence (cells 50–54 × 15–19).
