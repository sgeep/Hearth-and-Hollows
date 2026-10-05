"""
Crops a region of a raw Minifantasy sheet, enlarged with a pixel grid every 8 px and coordinates every 16 px, for
measuring catalogue rects. usage: python Tools/furniture/zoom.py <sheet under C:/Dev/Minifantasy> x y w h out.png [scale]
"""
import sys
from PIL import Image, ImageDraw

rel, x, y, w, h, out = sys.argv[1], *map(int, sys.argv[2:6]), sys.argv[6]
scale = int(sys.argv[7]) if len(sys.argv) > 7 else 6
im = Image.open("C:/Dev/Minifantasy/" + rel).convert("RGBA").crop((x, y, x + w, y + h))
bg = Image.new("RGBA", im.size, (90, 90, 98, 255))
bg.alpha_composite(im)
big = bg.resize((w * scale, h * scale), Image.NEAREST).convert("RGB")
d = ImageDraw.Draw(big)
for gx in range(0, w + 1):
    if (x + gx) % 8 == 0:
        d.line([(gx * scale, 0), (gx * scale, h * scale)], fill=(70, 70, 76) if (x + gx) % 16 else (40, 40, 46))
        if (x + gx) % 16 == 0:
            d.text((gx * scale + 1, 0), str(x + gx), fill=(255, 255, 140))
for gy in range(0, h + 1):
    if (y + gy) % 8 == 0:
        d.line([(0, gy * scale), (w * scale, gy * scale)], fill=(70, 70, 76) if (y + gy) % 16 else (40, 40, 46))
        if (y + gy) % 16 == 0:
            d.text((0, gy * scale + 1), str(y + gy), fill=(140, 255, 255))
big.save(out)
