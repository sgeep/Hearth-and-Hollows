"""
A framed portrait to hang on a wall (4h Checkpoint A): Phi's portrait in Tally Ho!.

Takes a composed portrait's still frame (Tools/portraits/derived/<id>_portrait.png, from compose.py), crops it to the face
so it fits the tavern's three-tile wall band, and draws a frame round it in the colours of Minifantasy's own picture frame
(the Castles and Strongholds props' noble portrait, the catalogue's castle_portrait). Writes
Tools/portraits/derived/<id>_framed.png, read by the game through a derived: source. Recorded in docs/ASSET_MAP.md.

Usage:
  python Tools/portraits/framed.py phi
"""
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
DERIVED = os.path.join(HERE, "derived")
CASTLE_PROPS = "C:/Dev/Minifantasy/Minifantasy_CastlesAndStrongholds_v.2.0/Minifantasy_CastlesAndStrongholds_Assets/Props/Props.png"
# The noble portrait's frame in the castle props (x, y, width, height): its colours, outermost first.
FRAME_SOURCE = (11, 34, 10, 12)
# The face, inside the 32x32 portrait: (left, top, right, bottom).
CROP = (7, 7, 25, 27)


def frame_colours():
    """The frame's outline, wood and highlight, taken from the edge of Minifantasy's framed portrait."""
    props = Image.open(CASTLE_PROPS).convert("RGBA")
    x, y, w, h = FRAME_SOURCE
    picture = props.crop((x, y, x + w, y + h))
    outline = picture.getpixel((0, h // 2))
    wood = picture.getpixel((1, h // 2))
    light = picture.getpixel((w // 2, 1))
    return outline, wood, light


def framed(portrait_id):
    still = Image.open(os.path.join(DERIVED, f"{portrait_id}_portrait.png")).convert("RGBA").crop((0, 0, 32, 32))
    face = still.crop(CROP)
    outline, wood, light = frame_colours()
    w, h = face.width + 4, face.height + 4
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for px in range(w):
        for py in range(h):
            edge = min(px, py, w - 1 - px, h - 1 - py)
            if edge == 0:
                out.putpixel((px, py), outline)
            elif edge == 1:
                out.putpixel((px, py), light if py == 1 else wood)
    out.paste(face, (2, 2))
    path = os.path.join(DERIVED, f"{portrait_id}_framed.png")
    out.save(path)
    print(f"wrote {os.path.relpath(path, os.path.dirname(HERE))} ({w}x{h})")


if __name__ == "__main__":
    for portrait_id in sys.argv[1:] or ["phi"]:
        framed(portrait_id)
