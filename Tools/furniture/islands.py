"""
Finds the separate drawings ("islands") on a Minifantasy prop sheet and writes a numbered contact sheet, so catalogue
entries (Assets/_Project/Data/Furniture/catalog.json) can name rectangles by looking rather than measuring by hand.

usage: python Tools/furniture/islands.py <sheet path under C:/Dev/Minifantasy> <out name> [gap] [scale]

  gap    pixels of transparency that still join two parts into one island (default 1: touching or one pixel apart)
  scale  contact sheet zoom (default 4)

Writes BatchLogs/catalog/<out name>_islands.png (each island boxed and numbered) and <out name>_islands.json
([{"n": 1, "x": .., "y": .., "w": .., "h": ..}], x and y from the image's top-left, as the catalogue uses).
Reads the raw packs only; nothing is copied into the repository.
"""
import json
import os
import sys

from PIL import Image, ImageDraw

RAW = "C:/Dev/Minifantasy"
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "BatchLogs", "catalog")


def islands(image, gap):
    w, h = image.size
    alpha = image.getchannel("A").load()
    seen = [[False] * w for _ in range(h)]
    found = []
    for y in range(h):
        for x in range(w):
            if seen[y][x] or alpha[x, y] == 0:
                continue
            stack = [(x, y)]
            seen[y][x] = True
            x0 = x1 = x
            y0 = y1 = y
            while stack:
                cx, cy = stack.pop()
                x0, x1, y0, y1 = min(x0, cx), max(x1, cx), min(y0, cy), max(y1, cy)
                for dy in range(-gap, gap + 1):
                    for dx in range(-gap, gap + 1):
                        nx, ny = cx + dx, cy + dy
                        if 0 <= nx < w and 0 <= ny < h and not seen[ny][nx] and alpha[nx, ny] != 0:
                            seen[ny][nx] = True
                            stack.append((nx, ny))
            found.append((x0, y0, x1 - x0 + 1, y1 - y0 + 1))
    found.sort(key=lambda r: (r[1] // 8, r[0]))
    return found


def main():
    rel, name = sys.argv[1], sys.argv[2]
    gap = int(sys.argv[3]) if len(sys.argv) > 3 else 1
    scale = int(sys.argv[4]) if len(sys.argv) > 4 else 4
    image = Image.open(os.path.join(RAW, rel)).convert("RGBA")
    rects = islands(image, gap)
    os.makedirs(OUT, exist_ok=True)

    bg = Image.new("RGBA", image.size, (90, 90, 98, 255))
    bg.alpha_composite(image)
    big = bg.resize((image.width * scale, image.height * scale), Image.NEAREST).convert("RGB")
    draw = ImageDraw.Draw(big)
    for i, (x, y, w, h) in enumerate(rects, 1):
        draw.rectangle([x * scale, y * scale, (x + w) * scale - 1, (y + h) * scale - 1], outline=(255, 220, 60))
        draw.text((x * scale + 1, y * scale + 1), str(i), fill=(255, 255, 255))
    big.save(os.path.join(OUT, f"{name}_islands.png"))
    with open(os.path.join(OUT, f"{name}_islands.json"), "w") as f:
        json.dump([{"n": i, "x": x, "y": y, "w": w, "h": h} for i, (x, y, w, h) in enumerate(rects, 1)], f, indent=0)
    print(f"{name}: {len(rects)} islands, {image.width}x{image.height}")


if __name__ == "__main__":
    main()
