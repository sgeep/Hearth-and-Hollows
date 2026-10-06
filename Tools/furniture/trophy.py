"""The Larder Troll's tusks (4f, D22): a derived Minifantasy composite.

Two curved horns from Giant Bones (Desolate Desert add-on), cropped to their tips and recoloured to the troll's yellowed
ivory, mounted on the wooden plaque of the Castles and Strongholds antler head (the stag's head removed by mirroring the
plaque's clean left half). Rerun to rebuild: python Tools/furniture/trophy.py [out.png]
"""
import sys
from PIL import Image

ROOT = "C:/Dev/Minifantasy/"
CASTLE = ROOT + "Minifantasy_CastlesAndStrongholds_v.2.0/Minifantasy_CastlesAndStrongholds_Assets/Props/Props.png"
BONES = ROOT + "All_Exclusives_20261002/Addons/Desolate_Desert/Giant_Bones/Bones.png"
OUT = sys.argv[1] if len(sys.argv) > 1 else "Tools/furniture/derived/trophy_larder_troll.png"

# The bone's own ramp, dark to light, onto yellowed ivory.
IVORY = {
    (156, 120, 79): (128, 98, 52),
    (173, 135, 92): (163, 130, 70),
    (195, 156, 113): (199, 168, 102),
    (206, 175, 141): (222, 197, 132),
    (226, 195, 161): (238, 222, 166),
}


def plaque():
    """The antler head's plaque (8×9 px): its clean left edge mirrored over the stag's head, the middle filled with the
    plaque's own wood, and the original bottom rows (the gold tab)."""
    mount = Image.open(CASTLE).convert("RGBA").crop((12, 82, 26, 96))
    out = Image.new("RGBA", (8, 9), (0, 0, 0, 0))
    for y in range(9):
        src = y + 5
        if src >= 12:
            for x in range(8):
                out.putpixel((x, y), mount.getpixel((x, src)))
            continue
        for x in range(3):
            p = mount.getpixel((x, src))
            out.putpixel((x, y), p)
            out.putpixel((7 - x, y), p)
        inner = mount.getpixel((2, src))
        out.putpixel((3, y), inner)
        out.putpixel((4, y), inner)
    return out


def tusk():
    """A whole Giant Bones crescent at half size (its shape kept, the outline redrawn, the bone's ramp as ivory): the
    base at the bottom right, curving out and back to a point at the top right."""
    horn = Image.open(BONES).convert("RGBA").crop((73, 13, 96, 40))
    # A pixel of margin all round, for the redrawn outline.
    w, h = (horn.width + 1) // 2 + 2, (horn.height + 1) // 2 + 2
    shape = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for y in range(h - 2):
        for x in range(w - 2):
            block = [horn.getpixel((min(horn.width - 1, 2 * x + dx), min(horn.height - 1, 2 * y + dy))) for dx in (0, 1) for dy in (0, 1)]
            bone = [p for p in block if p[3] and p[:3] in IVORY]
            if bone:
                # The lightest of the block, so the curve's highlight survives the halving.
                shape.putpixel((x + 1, y + 1), max((IVORY[p[:3]] for p in bone), key=sum) + (255,))
    out = shape.copy()
    for y in range(h):
        for x in range(w):
            if shape.getpixel((x, y))[3]:
                continue
            if any(0 <= x + dx < w and 0 <= y + dy < h and shape.getpixel((x + dx, y + dy))[3]
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                out.putpixel((x, y), (0, 0, 0, 255))
    return out


def build():
    t = tusk()
    canvas = Image.new("RGBA", (2 * t.width - 2, t.height + 5), (0, 0, 0, 0))
    canvas.alpha_composite(t, (0, 0))
    canvas.alpha_composite(t.transpose(Image.FLIP_LEFT_RIGHT), (t.width - 2, 0))
    canvas.alpha_composite(plaque(), (canvas.width // 2 - 4, canvas.height - 9))
    return canvas


if __name__ == "__main__":
    import os
    os.makedirs(os.path.dirname(OUT) or ".", exist_ok=True)
    build().save(OUT)
    print("wrote", OUT)
