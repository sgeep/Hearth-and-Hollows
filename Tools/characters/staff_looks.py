"""The staff's derived looks: Orik's (2026-10-06), and the retired Pip's and Gunta's (4f Checkpoint C), rebuilt by rerunning this script.

Minifantasy has no dressed halfling or cook: the Creatures pack's base humanoids are plain templates. So:

* **Gunta Ashbelly** is the Creatures pack's yellow-bearded dwarf with its palette remapped: the grey helmet becomes a
  white cook's cap, the orange beard a deep auburn, and the brown-red clothes a bright cook's red (an apron's red).
* **Orik** (2026-10-06; he replaced Pip, stable id `pip`) is the same yellow-bearded dwarf with his ginger beard kept as drawn:
  the grey belt becomes brown leather and the brown-red clothes Pip's green.
* (Retired) **Gunta** and **Pip**: Gunta is the cook Boog now (the Goblin Sapper); their sheets stay as history.
* **Pip Marrowby** (retired) was the Creatures pack's base halfling, coloured by where each pixel sits in the figure (the template
  has one cream ramp throughout): the top of the head brown curls (the whole head from behind), the face left as it is,
  the torso a green waistcoat, the legs brown breeches. The outline and shading ramps are kept.

Usage: python Tools/characters/staff_looks.py  (writes Tools/characters/derived/*.png)
"""
import os
from PIL import Image

ROOT = "C:/Dev/Minifantasy/Minifantasy_Creatures_v3.3_Commercial_Version/Minifantasy_Creatures_Assets/Base_Humanoids/"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "derived")
FRAME = 32

# Gunta: the yellow-bearded dwarf's own colours onto her cook's colours.
GUNTA = {
    # beard and hair: orange -> deep auburn
    (162, 92, 15): (92, 34, 22),
    (194, 113, 23): (122, 48, 30),
    (221, 138, 47): (156, 70, 42),
    # helmet -> white cook's cap
    (74, 78, 80): (196, 192, 184),
    (92, 98, 100): (226, 222, 212),
    (129, 135, 138): (246, 244, 236),
    # clothes -> a cook's red
    (121, 46, 13): (138, 28, 30),
    (156, 56, 32): (184, 44, 42),
}

# Orik: the yellow-bearded dwarf, ginger hair and beard as drawn, a leather belt and green clothes (the server's colours).
ORIK = {
    # belt -> brown leather
    (74, 78, 80): (78, 50, 30),
    (92, 98, 100): (104, 68, 40),
    (129, 135, 138): (138, 94, 56),
    # clothes -> green
    (121, 46, 13): (48, 90, 54),
    (156, 56, 32): (76, 128, 78),
}

# Pip: the halfling template's cream ramp (dark to light), and what it becomes in each part of the figure.
CREAM = [(194, 184, 127), (207, 195, 135), (223, 207, 144), (238, 219, 154), (253, 229, 170)]
HAIR = [(70, 42, 22), (84, 52, 28), (102, 64, 34), (122, 78, 42), (140, 92, 52)]
WAISTCOAT = [(38, 74, 44), (48, 90, 54), (60, 108, 64), (76, 128, 78), (94, 148, 92)]
BREECHES = [(70, 48, 32), (84, 58, 38), (100, 70, 46), (116, 82, 54), (132, 96, 64)]


def ramp(colour, target):
    return target[CREAM.index(colour)] + (255,) if colour in CREAM else None


def remap(path, table):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a and (r, g, b) in table:
                px[x, y] = table[(r, g, b)] + (a,)
    return im


def gunta(path):
    return remap(path, GUNTA)


def orik(path):
    return remap(path, ORIK)


def pip(path):
    im = Image.open(path).convert("RGBA")
    px = im.load()
    for fy in range(0, im.height, FRAME):
        # Rows 0-1 of the sheet face the camera; rows 2-3 face away (the head is all hair from behind).
        facing_away = (fy // FRAME) >= 2
        for fx in range(0, im.width, FRAME):
            box = im.crop((fx, fy, fx + FRAME, fy + FRAME)).getbbox()
            if box is None:
                continue
            top, bottom = fy + box[1], fy + box[3]
            # Inside the outline: the head's first row, the face's rows, then the torso and the legs at the bottom.
            for y in range(top, bottom):
                row = y - top
                legs = y == bottom - 1
                torso = y == bottom - 2
                for x in range(fx, fx + FRAME):
                    r, g, b, a = px[x, y]
                    if not a or (r, g, b) not in CREAM:
                        continue
                    if legs:
                        target = BREECHES
                    elif torso:
                        target = WAISTCOAT
                    elif row <= 1 or facing_away:
                        target = HAIR
                    else:
                        continue  # the face
                    px[x, y] = ramp((r, g, b), target)
    return im


def main():
    os.makedirs(OUT, exist_ok=True)
    jobs = [
        ("OrikIdle", orik, "Dwarf/Dwarf_Yellow_Beard/YellowBeardIdle.png"),
        ("OrikWalk", orik, "Dwarf/Dwarf_Yellow_Beard/YellowBearWalk.png"),
        ("GuntaIdle", gunta, "Dwarf/Dwarf_Yellow_Beard/YellowBeardIdle.png"),
        ("GuntaWalk", gunta, "Dwarf/Dwarf_Yellow_Beard/YellowBearWalk.png"),
        ("PipIdle", pip, "Halfling/HalflingIdle.png"),
        ("PipWalk", pip, "Halfling/HalflingWalk.png"),
    ]
    for name, recolour, source in jobs:
        out = os.path.join(OUT, name + ".png")
        recolour(ROOT + source).save(out)
        print("wrote", out)


if __name__ == "__main__":
    main()
