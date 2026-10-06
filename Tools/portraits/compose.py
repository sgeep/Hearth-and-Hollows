"""
Composes dialogue portraits from the Minifantasy Portrait Generator's own layers (4g).

Each character has a recipe, Tools/portraits/<id>.json: the race, skin and body height, and the generator's layers
(type, variant and colour), exactly the choices the generator app offers. This script stacks the layers in the app's
order and writes Tools/portraits/derived/<id>_portrait.png: one 32x32 frame each for the face at rest, the face
blinking, and the four talking mouths. The game shows them at 2x (PortraitDefinition).

It reproduces the app's rules (Minifantasy_Portrait_Generator_app_v1.0/app/portrait-generator.html):
  - layer order: bg, base, eyes, nose, hair, ears, brows, clothes, beard, mouth, moustache, hat;
  - file names: <race>/<race>_<skin>_base.png, <race>/<race>_<skin>_<type>_<variant>.png (ears, nose, mouth),
    <race>/<race>_eyes/<race>_eyes_<skin>_<variant>_<1|2>.png (2 = blinking),
    <race>/<race>_mouth/<race>_mouth_<skin>_talking_<1..4>.png, common/common_<type>[_<colour>]_<variant>.png;
  - talking cycles the face's own mouth then talking 1 to 4; every layer but the background moves up by the body height.
The raw packs stay outside the repo (C:/Dev/Minifantasy); only the composed frames are committed.

Usage:
  python Tools/portraits/compose.py            # every recipe -> Tools/portraits/derived/
  python Tools/portraits/compose.py --preview goblin grassland hat engineer,hood,beanie --out preview.png
"""
import json
import os
import sys

from PIL import Image

PACK = "C:/Dev/Minifantasy/Minifantasy_Portrait_Generator_Graphical_Assets_v1.0/Portrait_Generator/single_images"
HERE = os.path.dirname(os.path.abspath(__file__))
DERIVED = os.path.join(HERE, "derived")
ORDER = ["bg", "base", "eyes", "nose", "hair", "ears", "brows", "clothes", "beard", "mouth", "moustache", "hat"]
COMMON = {"bg", "beard", "brows", "clothes", "hair", "hat", "moustache"}
COLOURED = {"beard", "brows", "hair", "moustache", "clothes", "hat"}
SIZE = 32


def layer_path(race, skin, layer, blink=False, talking=0):
    kind, variant = layer["type"], layer.get("variant", "")
    if kind in COMMON:
        colour = "_" + layer["color"] if kind in COLOURED else ""
        return f"{PACK}/common/common_{kind}{colour}_{variant}.png"
    if kind == "base":
        return f"{PACK}/{race}/{race}_{skin}_base.png"
    if kind == "eyes":
        return f"{PACK}/{race}/{race}_eyes/{race}_eyes_{skin}_{variant}_{2 if blink else 1}.png"
    if kind == "mouth" and talking > 0:
        return f"{PACK}/{race}/{race}_mouth/{race}_mouth_{skin}_talking_{talking}.png"
    return f"{PACK}/{race}/{race}_{skin}_{kind}_{variant}.png"


def frame(recipe, blink=False, talking=0):
    race, skin, height = recipe["race"], recipe["skin"], int(recipe.get("height", 0))
    out = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    layers = sorted(recipe["layers"], key=lambda l: ORDER.index(l["type"]))
    for layer in layers:
        image = Image.open(layer_path(race, skin, layer, blink, talking)).convert("RGBA")
        dx, dy = layer.get("offset", [0, 0])
        lift = 0 if layer["type"] == "bg" else height
        piece = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        piece.paste(image, (dx, dy - lift))
        out.alpha_composite(piece)
    return out


def strip(recipe):
    """still, blink, talking 1-4: the frames PortraitDefinition reads."""
    frames = [frame(recipe), frame(recipe, blink=True)] + [frame(recipe, talking=t) for t in range(1, 5)]
    sheet = Image.new("RGBA", (SIZE * len(frames), SIZE), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        sheet.alpha_composite(f, (i * SIZE, 0))
    return sheet


def compose_all():
    os.makedirs(DERIVED, exist_ok=True)
    for name in sorted(os.listdir(HERE)):
        if not name.endswith(".json"):
            continue
        recipe = json.load(open(os.path.join(HERE, name), encoding="utf-8"))
        cid = os.path.splitext(name)[0]
        out = os.path.join(DERIVED, f"{cid}_portrait.png")
        strip(recipe).save(out)
        print("wrote", os.path.relpath(out, os.path.dirname(HERE)))


def preview(race, skin, kind, variants, out, base_recipe=None):
    """A contact sheet of one layer's variants over a base recipe, at 4x."""
    recipe = base_recipe or {"race": race, "skin": skin, "layers": [{"type": "base"}]}
    cells = []
    for v in variants:
        r = json.loads(json.dumps(recipe))
        r["layers"] = [l for l in r["layers"] if l["type"] != kind]
        layer = {"type": kind, "variant": v}
        if ":" in v:
            layer["variant"], layer["color"] = v.split(":")
        r["layers"].append(layer)
        cells.append(frame(r))
    sheet = Image.new("RGBA", (SIZE * len(cells), SIZE), (60, 100, 60, 255))
    for i, c in enumerate(cells):
        sheet.alpha_composite(c, (i * SIZE, 0))
    sheet.resize((sheet.width * 4, sheet.height * 4), Image.NEAREST).save(out)


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "--preview":
        _, _, race, skin, kind, variants, _, out, *rest = sys.argv
        base = json.load(open(rest[0], encoding="utf-8")) if rest else None
        preview(race, skin, kind, variants.split(","), out, base)
    else:
        compose_all()
