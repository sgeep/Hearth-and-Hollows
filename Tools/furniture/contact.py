"""
Renders the furniture catalogue (Assets/_Project/Data/Furniture/Catalog/catalog.json) as contact sheets, one per
category, straight from the raw Minifantasy sheets: every piece, each of its facings across, each of its variants down,
with its id, tier and price. Also checks each rect: empty rects, and drawings that run past a rect's edge (a clipped
piece), are listed as warnings.

usage: python Tools/furniture/contact.py [scale]
Writes BatchLogs/catalog/contact_<category>.png and prints the warnings. Reads the raw packs only.
"""
import json
import os
import sys

from PIL import Image, ImageDraw

RAW = "C:/Dev/Minifantasy"
ROOT = os.path.join(os.path.dirname(__file__), "..", "..")
CATALOG = os.path.join(ROOT, "Assets/_Project/Data/Furniture/Catalog/catalog.json")
OUT = os.path.join(ROOT, "BatchLogs", "catalog")
BG = (74, 66, 58, 255)
LABEL = (255, 236, 180)


def load():
    with open(CATALOG, encoding="utf-8") as f:
        return json.load(f)


def variants_of(piece, sets):
    if piece.get("variants"):
        return piece["variants"]
    if piece.get("variantSet"):
        return sets[piece["variantSet"]]
    return [{"id": "", "name": "", "dx": 0, "dy": 0}]


def rect_for(facing, variant, index):
    """The rect a facing uses in a variant: an explicit override, else the facing's rect moved by the variant's offset."""
    for o in facing.get("variantRects", []):
        if o["variant"] == variant.get("id"):
            return o["rect"]
    x, y, w, h = facing["rect"]
    if index == 0:
        return [x, y, w, h]
    return [x + variant.get("dx", 0), y + variant.get("dy", 0), w, h]


def check(image, rect, where, warnings):
    x, y, w, h = rect
    if x < 0 or y < 0 or x + w > image.width or y + h > image.height:
        warnings.append(f"{where}: rect {rect} is outside the sheet ({image.width}x{image.height})")
        return
    crop = image.crop((x, y, x + w, y + h))
    if crop.getbbox() is None:
        warnings.append(f"{where}: rect {rect} is empty")
        return
    a = image.getchannel("A").load()
    edges = []
    for xx in range(x, x + w):
        if y > 0 and a[xx, y] and a[xx, y - 1]: edges.append("top")
        if y + h < image.height and a[xx, y + h - 1] and a[xx, y + h]: edges.append("bottom")
    for yy in range(y, y + h):
        if x > 0 and a[x, yy] and a[x - 1, yy]: edges.append("left")
        if x + w < image.width and a[x + w - 1, yy] and a[x + w, yy]: edges.append("right")
    if edges:
        warnings.append(f"{where}: drawing continues past the {', '.join(sorted(set(edges)))} edge of {rect}")


def main():
    scale = int(sys.argv[1]) if len(sys.argv) > 1 else 3
    data = load()
    sheets = {s["key"]: Image.open(os.path.join(RAW, s["source"])).convert("RGBA") for s in data["sheets"]}
    sets = {s["id"]: s["variants"] for s in data["variantSets"]}
    warnings = []
    ids = set()
    by_category = {}
    for p in data["pieces"]:
        if p["id"] in ids:
            warnings.append(f"{p['id']}: duplicate id")
        ids.add(p["id"])
        by_category.setdefault(p["category"], []).append(p)
        turns = [f.get("turns", 0) for f in p["facings"]]
        if len(turns) != len(set(turns)):
            warnings.append(f"{p['id']}: two facings share a turn")
    os.makedirs(OUT, exist_ok=True)
    for category, pieces in by_category.items():
        rows = []
        for p in pieces:
            vs = variants_of(p, sets)
            cells = []
            for vi, v in enumerate(vs):
                sheet = sheets[v.get("sheet") or p["sheet"]]
                line = []
                for f in p["facings"]:
                    r = rect_for(f, v, vi)
                    check(sheet, r, f"{p['id']}/{v.get('id') or 'default'}/turn {f.get('turns', 0)}", warnings)
                    line.append(sheet.crop((r[0], r[1], r[0] + r[2], r[1] + r[3])))
                cells.append(line)
            rows.append((p, vs, cells))
        width = 0
        height = 0
        for p, vs, cells in rows:
            w = 180 + max(sum(im.width * scale + 6 for im in line) for line in cells)
            h = 14 + sum(max(im.height for im in line) * scale + 4 for line in cells)
            width, height = max(width, w), height + h + 6
        sheet_img = Image.new("RGBA", (width, height), (40, 36, 34, 255))
        d = ImageDraw.Draw(sheet_img)
        y = 4
        for p, vs, cells in rows:
            d.text((4, y), f"{p['id']}  t{p['tier']}  {p['price']}g  {p.get('layer', 'Standing')}  {p.get('function', '')}", fill=LABEL)
            y += 14
            for (v, line) in zip(vs, cells):
                d.text((12, y), v.get("name") or "-", fill=(200, 200, 200))
                x = 180
                lh = max(im.height for im in line) * scale
                for im in line:
                    bg = Image.new("RGBA", im.size, BG)
                    bg.alpha_composite(im)
                    big = bg.resize((im.width * scale, im.height * scale), Image.NEAREST)
                    sheet_img.paste(big, (x, y))
                    x += big.width + 6
                y += lh + 4
            y += 6
        sheet_img.save(os.path.join(OUT, f"contact_{category}.png"))
        print(f"{category}: {len(pieces)} pieces, {sum(len(variants_of(p, sets)) * len(p['facings']) for p in pieces)} drawings")
    print(f"{len(data['pieces'])} pieces")
    for w in warnings:
        print("WARNING", w)


if __name__ == "__main__":
    main()
