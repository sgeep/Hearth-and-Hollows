"""Silver, adapted for Hearthdelve: plain punctuation.

Silver (Poppy Works, CC BY 4.0) draws its period, middle dot, comma, colon and semicolon as 3x3 plus signs. This
redraws those five glyphs with single-pixel dots, like Silver's own "!" and "?", and narrows their advances to match.
Nothing else in the font changes. Idempotent: run it again and the file is unchanged.

    python Tools/fonts/silver_plain_punctuation.py Assets/_Project/Fonts/silver/Silver.ttf

The adaptation is recorded in docs/THIRD_PARTY.md and docs/CREDITS.md (CC BY asks that changes be indicated).
Silver's pixel grid is 100 font units; y = 0 is the baseline.
"""
import struct
import sys

PX = 100

# codepoint: (advance, [pixel rectangles (x0, y0, x1, y1) in pixels]); the left bearing is the leftmost pixel.
GLYPHS = {
    0x2E: (3, [(1, 0, 2, 1)]),                    # .  one pixel on the baseline
    0xB7: (3, [(1, 3, 2, 4)]),                    # ·  one pixel, raised
    0x2C: (4, [(2, 0, 3, 1), (1, -1, 2, 0)]),     # ,  a dot and a tail down-left
    0x3A: (2, [(0, 1, 1, 2), (0, 5, 1, 6)]),      # :  two dots, centred on the x-height
    0x3B: (4, [(2, 0, 3, 1), (1, -1, 2, 0), (2, 5, 3, 6)]),  # ;  a comma and a dot level with the colon's top
}


def tables(data):
    count = struct.unpack(">H", data[4:6])[0]
    out = {}
    for i in range(count):
        tag, _, offset, length = struct.unpack(">4sIII", data[12 + 16 * i:28 + 16 * i])
        out[tag.decode("latin-1")] = data[offset:offset + length]
    order = [struct.unpack(">4sIII", data[12 + 16 * i:28 + 16 * i])[0].decode("latin-1") for i in range(count)]
    return order, out


def glyph_id(cmap, codepoint):
    count = struct.unpack(">H", cmap[2:4])[0]
    for i in range(count):
        _, _, offset = struct.unpack(">HHI", cmap[4 + 8 * i:12 + 8 * i])
        if struct.unpack(">H", cmap[offset:offset + 2])[0] != 12:
            continue
        groups = struct.unpack(">I", cmap[offset + 12:offset + 16])[0]
        for g in range(groups):
            start, end, first = struct.unpack(">III", cmap[offset + 16 + 12 * g:offset + 28 + 12 * g])
            if start <= codepoint <= end:
                return first + codepoint - start
    raise KeyError(hex(codepoint))


def simple_glyph(rects):
    """Clockwise squares (TrueType outer contours), one contour per rectangle, on-curve points, 16-bit deltas."""
    points, ends = [], []
    for x0, y0, x1, y1 in rects:
        points += [(x0 * PX, y0 * PX), (x0 * PX, y1 * PX), (x1 * PX, y1 * PX), (x1 * PX, y0 * PX)]
        ends.append(len(points) - 1)
    xs, ys = [p[0] for p in points], [p[1] for p in points]
    out = struct.pack(">hhhhh", len(rects), min(xs), min(ys), max(xs), max(ys))
    out += b"".join(struct.pack(">H", e) for e in ends) + struct.pack(">H", 0)  # no instructions
    out += bytes([0x01] * len(points))  # on curve, x and y as signed 16-bit deltas
    px = py = 0
    for x, _ in points:
        out += struct.pack(">h", x - px); px = x
    for _, y in points:
        out += struct.pack(">h", y - py); py = y
    return out + b"\0" * (-len(out) % 4)


def checksum(block):
    block += b"\0" * (-len(block) % 4)
    return sum(struct.unpack(">%dI" % (len(block) // 4), block)) & 0xFFFFFFFF


def patch(path):
    data = open(path, "rb").read()
    order, t = tables(data)
    assert struct.unpack(">h", t["head"][50:52])[0] == 1, "expects long loca offsets"
    glyph_count = struct.unpack(">H", t["maxp"][4:6])[0]
    metrics = struct.unpack(">H", t["hhea"][34:36])[0]
    loca = list(struct.unpack(">%dI" % (glyph_count + 1), t["loca"]))
    records = [t["glyf"][loca[g]:loca[g + 1]] for g in range(glyph_count)]
    hmtx = bytearray(t["hmtx"])

    for codepoint, (advance, rects) in GLYPHS.items():
        g = glyph_id(t["cmap"], codepoint)
        assert g < metrics, "the glyph has its own metrics"
        records[g] = simple_glyph(rects)
        struct.pack_into(">Hh", hmtx, 4 * g, advance * PX, min(r[0] for r in rects) * PX)

    glyf, loca = b"", []
    for record in records:
        loca.append(len(glyf)); glyf += record
    loca.append(len(glyf))
    t["glyf"], t["loca"], t["hmtx"] = glyf, struct.pack(">%dI" % len(loca), *loca), bytes(hmtx)
    head = bytearray(t["head"]); struct.pack_into(">I", head, 8, 0); t["head"] = bytes(head)

    count = len(order)
    header = data[:12]
    directory, body = b"", b""
    offset = 12 + 16 * count
    for tag in order:
        block = t[tag]
        directory += struct.pack(">4sIII", tag.encode("latin-1"), checksum(block), offset + len(body), len(block))
        body += block + b"\0" * (-len(block) % 4)
    font = bytearray(header + directory + body)
    head_offset = 12 + 16 * count + sum(len(t[x]) + (-len(t[x]) % 4) for x in order[:order.index("head")])
    struct.pack_into(">I", font, head_offset + 8, (0xB1B0AFBA - checksum(bytes(font))) & 0xFFFFFFFF)
    open(path, "wb").write(bytes(font))


if __name__ == "__main__":
    patch(sys.argv[1])
