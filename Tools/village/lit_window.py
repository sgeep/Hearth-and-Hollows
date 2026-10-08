"""Lit windows for Kariaston's buildings (2026-10-08).

Towns II draws its windows dark. A lit window is the building's own window with its glass recoloured to the warm flame
colours of Medieval City's lamp post (Minifantasy's own "lit" colours, never free tinting), saved as a building-sized
overlay: transparent except the lit glass, so it sits on the building with the same pivot and lines up exactly.

    python Tools/village/lit_window.py

Reads the raw pack from C:/Dev/Minifantasy (never copied whole into the repo); writes Tools/village/derived/*.png, which
the importer reads through a "derived:" source (KariastonSheets). Recorded in docs/ASSET_MAP.md.
"""
import os
from PIL import Image

MINIFANTASY = os.environ.get("MINIFANTASY", "C:/Dev/Minifantasy")
BUILDINGS = os.path.join(MINIFANTASY, "Minifantasy_Towns2_v1.5/Minifantasy_Towns2_Assets/Buildings/_Mix_And_Match_Samples/Minifantasy_TownsIIMoreBuildingSamples.png")
LAMP = os.path.join(MINIFANTASY, "Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Props/Props.png")
OUT = os.path.join(os.path.dirname(__file__), "derived")

# The lamp post's flame (Props.png, LampPost cell at 112,98): its orange and its pale yellow.
LAMP_ORANGE = (112 + 3, 98 + 7)
LAMP_YELLOW = (112 + 4, 98 + 7)

# Each lit window: the building's cell on the sheet, and the window's glass box inside it (x, y, w, h; top-left origin).
WINDOWS = {
    # Grim and Ogrin's cottage: the ground floor's right-hand window, Ogrin's (he's talked to through it).
    "BrownCottageWindowLit": ((319, 160, 58, 64), (43, 55, 4, 4)),
}


def main():
    sheet = Image.open(BUILDINGS).convert("RGBA")
    lamp = Image.open(LAMP).convert("RGBA")
    orange = lamp.getpixel(LAMP_ORANGE)
    yellow = lamp.getpixel(LAMP_YELLOW)
    assert orange[:3] == (0xFF, 0xA7, 0x00) and yellow[:3] == (0xFF, 0xE1, 0x85), "the lamp's flame moved on the sheet"
    os.makedirs(OUT, exist_ok=True)
    for name, ((bx, by, bw, bh), (gx, gy, gw, gh)) in WINDOWS.items():
        building = sheet.crop((bx, by, bx + bw, by + bh))
        out = Image.new("RGBA", building.size, (0, 0, 0, 0))
        # The glass's own shades, darkest first: the darkest become the flame's pale core, the rest its orange.
        shades = sorted({building.getpixel((x, y))[:3] for x in range(gx, gx + gw) for y in range(gy, gy + gh)}, key=sum)
        core = shades[0]
        for x in range(gx, gx + gw):
            for y in range(gy, gy + gh):
                rgb = building.getpixel((x, y))[:3]
                out.putpixel((x, y), yellow if rgb == core else orange)
        path = os.path.join(OUT, f"{name}.png")
        out.save(path)
        print(path, building.size, "glass shades", ["%02x%02x%02x" % s for s in shades])


if __name__ == "__main__":
    main()
