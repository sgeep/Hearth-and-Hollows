"""Exports the Crossroads layout for the game's one-time relayout of Kariaston (2026-10-10).

Runs layout2_crossroads.py unchanged (so every seeded tree, bush, flower and grass tile is exactly the chosen image's), then
applies the fixes the game needs on top (docs/ASSET_MAP.md, "Kariaston: the Crossroads"):
  1. four garden beds, the middle of the plot (the game's four, garden_1..4), not eight; the mockup's crops are dropped
     (in the game the beds show their real crops);
  2. fences use their own right-hand posts and bottom runs (the script used the left post and the top run everywhere);
  3. whole tuft drawings (two of the script's tuft rects cropped them);
  4. the well's whole frame (24x32: the script, like the game before, cut 24x24 and lost the stone base);
  5. roads continue past the map's edge (no end cap drawn at the border).
Every drawing then gets its Minifantasy shadow, cut by the shadow's own extent (the script cropped shadows to the art's
rectangle and drew none for props).

Writes Tools/village/crossroads_layout.json (read once by KariastonBuilder.RelayoutBatch) and
Tools/village/mockup/layout2_crossroads_built.png (the village as built, at 3x).

    python Tools/village/mockup/export_crossroads.py
"""
import os, sys, json, runpy
import numpy as np
from PIL import Image
from collections import deque

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
os.chdir(HERE)
import kar

M, A = kar.M, kar.A
kar.P.update({
    'towersh': A + '_Miscellany/Wizard_Tower/Exterior/Wizard_Tower_Exterior_shadows.png',
    'wagonsh': A + 'Medieval_Carnival/Caravans_And_Wagons/Tileset/CaravansAndWagonsShadows.png',
    'wellsh': A + 'Towns_I_II/Animated_Well/_Shadows/WellStaticFramesShadow.png',
    'monsh': A + 'Towns_I_II/Town_Monuments/Shadows.png',
    'cartsh': M + 'All_Exclusives_20261002/Creatures/Travelling_Merchant/Merchant_On_Cart/_Shadows/Shop_Open_Idle_Shadow.png',
    'cartclosed': M + 'All_Exclusives_20261002/Creatures/Travelling_Merchant/Merchant_On_Cart/Idle_Shop_Closed_No_Merchant.png',
    'cartclosedsh': M + 'All_Exclusives_20261002/Creatures/Travelling_Merchant/Merchant_On_Cart/_Shadows/Shop_Closed_Idle_Shadow.png',
    'mpropsh': M + 'Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Props/Shadows.png',
    'fpropsh': M + 'Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Props/Minifantasy_FarmPropsShadows.png',
    'tpropsh': M + 'Minifantasy_Towns_v3.0/Minifantasy_Towns_Assets/Props/Minifantasy_TownsPropsShadows.png',
    'ftilesh': M + 'Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Tileset/Minifantasy_FarmTilesetShadowLayer.png',
})
SHADOW_OF = {'bld': 'bldsh', 'fp': 'folsh', 'tower': 'towersh', 'wagons': 'wagonsh', 'well': 'wellsh', 'mon': 'monsh',
             'cart': 'cartsh', 'mprops': 'mpropsh', 'fprops': 'fpropsh', 'tprops': 'tpropsh', 'ftiles': 'ftilesh'}

# The game's name for each drawing the layout uses: (sheet, rect) -> name (registered in KariastonSheets).
NAMES = {
    ('bld', (9, 264, 118, 105)): 'ThatchedHall', ('bld', (13, 28, 110, 93)): 'BlueRoofHall', ('bld', (319, 160, 58, 64)): 'BrownCottage',
    ('tower', (0, 0, 64, 136)): 'TowerExterior', ('wagons', (302, 229, 28, 61)): 'PaintedWagon', ('cart', (0, 0, 64, 64)): 'CartOpen',
    ('mon', (40, 16, 16, 27)): 'Pedestal', ('mon', (8, 201, 15, 17)): 'Figure', ('mon', (296, 244, 16, 12)): 'Plaque',
    ('well', (0, 0, 24, 32)): 'Well',
    ('fp', (16, 124, 118, 72)): 'TreeLarge', ('fp', (157, 134, 71, 59)): 'TreeMedium', ('fp', (243, 144, 54, 46)): 'TreeSmall',
    ('fp', (43, 49, 26, 15)): 'Bush0', ('fp', (250, 48, 28, 16)): 'Bush2', ('fp', (459, 47, 26, 17)): 'Bush3',
    ('fp', (355, 48, 26, 16)): 'BushRed', ('fp', (11, 50, 18, 12)): 'Shrub0', ('fp', (218, 48, 20, 14)): 'ShrubPurple',
    ('mprops', (24, 148, 16, 9)): 'BenchLong', ('mprops', (112, 98, 9, 30)): 'LampPost', ('mprops', (346, 170, 12, 13)): 'Barrel',
    ('mprops', (371, 171, 10, 11)): 'BarrelSmall', ('mprops', (272, 168, 16, 16)): 'Crate',
    ('mprops', (67, 43, 11, 10)): 'Planter', ('mprops', (83, 44, 10, 9)): 'PlanterSmall',
    ('fprops', (83, 13, 20, 24)): 'Haystack', ('fprops', (11, 13, 33, 23)): 'HayPile', ('fprops', (16, 48, 32, 8)): 'Bales',
    ('fprops', (76, 48, 11, 15)): 'Scarecrow', ('tprops', (9, 86, 23, 17)): 'TankardBoard', ('tprops', (147, 94, 10, 9)): 'PostSign',
    ('ftiles', (400, 120, 8, 8)): 'Fence_TL', ('ftiles', (408, 120, 8, 8)): 'Fence_Top', ('ftiles', (432, 120, 8, 8)): 'Fence_TR',
    ('ftiles', (400, 128, 8, 8)): 'Fence_Left', ('ftiles', (432, 128, 8, 8)): 'Fence_Right', ('ftiles', (400, 152, 8, 8)): 'Fence_BL',
    ('ftiles', (408, 152, 8, 8)): 'Fence_Bottom', ('ftiles', (432, 152, 8, 8)): 'Fence_BR',
}
GROUP = {'ThatchedHall': 'Buildings', 'BlueRoofHall': 'Buildings', 'BrownCottage': 'Buildings', 'TowerExterior': 'Buildings',
         'PaintedWagon': 'Buildings', 'CartOpen': 'Market', 'Pedestal': 'Memorial', 'Figure': 'Memorial', 'Plaque': 'Memorial'}

GARDEN, PLOTS = (3, 2, 20, 11), [(42, 4, 48, 9), (36, 29, 42, 33), (50, 25, 56, 30)]
KEEP_BED_COLUMNS = (9, 13)
TUFTS = {(48, 8, 16, 12): (49, 14, 14, 9), (152, 8, 24, 12): (152, 13, 24, 10), (16, 8, 16, 12): (18, 12, 14, 11)}

placed = []
_put = kar.Map.put
def put(s, key, fx, fy, flip=False, occ=True, sortoff=0, spec=None):
    r = _put(s, key, fx, fy, flip=flip, occ=occ, sortoff=sortoff, spec=spec)
    sh, rect, piv, shs = spec or kar.S[key]
    if sh == 'fp' and key in ('treeL', 'treeM', 'treeS', 'treeXS'): sh = s.fol
    placed.append(dict(key=key, sheet=sh, rect=tuple(rect), piv=piv, fx=fx, fy=fy, flip=flip, sortoff=sortoff, index=len(s.sprites) - 1))
    return r
kar.Map.put = put

def art_xy(rect, piv, fx, fy):
    return int(round(fx * 8 - rect[2] / 2)), int(round(fy * 8 - rect[3] + piv))

_extent = {}
def shadow_extent(shs, rect, tile=False):
    """The shadow's own bounds: the components on the shadow sheet touching the art's rectangle (a tile's is its cell)."""
    if tile: return rect
    k = (shs, rect)
    if k in _extent: return _extent[k]
    a = np.asarray(kar.sheet(shs))[..., 3] > 0
    x, y, w, h = rect
    seen = np.zeros_like(a); xs = []; ys = []
    q = deque((cx, cy) for cy in range(y, y + h) for cx in range(x, x + w) if a[cy, cx])
    for cx, cy in list(q): seen[cy, cx] = True
    while q:
        cx, cy = q.popleft(); xs.append(cx); ys.append(cy)
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                nx, ny = cx + dx, cy + dy
                if 0 <= nx < a.shape[1] and 0 <= ny < a.shape[0] and a[ny, nx] and not seen[ny, nx] \
                        and abs(nx + .5 - (x + w / 2)) < w / 2 + 10 and abs(ny + .5 - (y + h / 2)) < h / 2 + 10:
                    seen[ny, nx] = True; q.append((nx, ny))
    r = (min(xs), min(ys), max(xs) + 1 - min(xs), max(ys) + 1 - min(ys)) if xs else None
    _extent[k] = r; return r

grass_picks = []
EXPECTED = {}
_autotile = kar.Map.autotile
def autotile(s, name, m):
    real_pad = kar.np.pad
    p = real_pad(m, 1, mode='edge')
    sh, ox, oy = kar.AT[name]
    for y in range(s.h):
        for x in range(s.w):
            if not m[y, x]: continue
            N = p[y, x + 1]; So = p[y + 2, x + 1]; W = p[y + 1, x]; E = p[y + 1, x + 2]
            NW = p[y, x]; NE = p[y, x + 2]; SW = p[y + 2, x]; SE = p[y + 2, x + 2]
            r = 0 if not N else (2 if not So else 1); c = 0 if not W else (2 if not E else 1)
            if (r, c) != (1, 1): cx, cy = ox + c, oy + r
            else:
                miss = [k for k, v in (('NW', NW), ('NE', NE), ('SW', SW), ('SE', SE)) if not v]
                cx, cy = {(): (ox + 1, oy + 1), ('NW',): (ox, oy + 3), ('NE',): (ox + 1, oy + 3), ('SW',): (ox, oy + 4), ('SE',): (ox + 1, oy + 4),
                          ('NW', 'SE'): (ox + 2, oy + 3), ('NE', 'SW'): (ox + 2, oy + 4)}.get(tuple(miss), (ox + 1, oy + 1))
            EXPECTED.setdefault(name, []).append([x, y, cx, cy])
    kar.np.pad = lambda a, w, *args, **kw: real_pad(a, w, mode='edge')
    try: return _autotile(s, name, m)
    finally: kar.np.pad = real_pad
kar.Map.autotile = autotile

_render = kar.Map.render
def render(s, out, scale=3):
    global MAP
    MAP = s
    drop = set()
    for p in placed:
        if p['sheet'] == 'crops': drop.add(p['index'])                                   # 1. crops are the game's
        if p['sheet'] == 'ftiles':                                                          # 2. own posts and runs
            for (x0, y0, x1, y1) in [GARDEN] + PLOTS:
                if p['rect'] == (400, 128, 8, 8) and abs(p['fx'] - (x1 + .5)) < 1e-6 and y0 < p['fy'] - 1 < y1: p['rect'] = (432, 128, 8, 8)
                if p['rect'] == (408, 120, 8, 8) and abs(p['fy'] - (y1 + 1)) < 1e-6 and x0 < p['fx'] - .5 < x1: p['rect'] = (408, 152, 8, 8)
        if p['sheet'] == 'fp' and p['key'] is None and p['rect'] in TUFTS: p['rect'] = TUFTS[p['rect']]   # 3. whole tufts
        if p['key'] == 'well':                                                              # 4. the well's whole frame, top kept
            p['rect'] = (0, 0, 24, 32); p['fy'] += 1.0
    sprites = []
    for i, (sk, px, py, im) in enumerate(s.sprites):
        if i in drop: continue
        p = next(q for q in placed if q['index'] == i)
        im = kar.cut(p['sheet'], *p['rect'])
        if p['flip']: im = im.transpose(Image.FLIP_LEFT_RIGHT)
        px, py = art_xy(p['rect'], p['piv'], p['fx'], p['fy'])
        p['left'], p['top'] = px, py
        sprites.append((p['fy'] * 8 + p['sortoff'], px, py, im))
    s.sprites = sprites
    for n, (name, mk) in enumerate(s.layers):                                               # 1. the four beds' soil
        if name == 'dirt' and n > 0 and mk.sum() == 72:
            keep = np.zeros_like(mk)
            for c in KEEP_BED_COLUMNS: keep[:, c:c + 3] = True
            s.layers[n] = ('soil', mk & keep)
            kar.AT['soil'] = kar.AT['dirt']
    s.shadow = Image.new('RGBA', s.shadow.size)                                             # every drawing's own shadow
    for p in placed:
        if p['index'] in drop: continue
        shs = SHADOW_OF.get(p['sheet'] if p['sheet'] != s.fol else 'fp')
        if shs is None or (p['sheet'], p['rect']) == ('mon', (8, 201, 15, 17)): continue   # the statue stands on its pedestal
        ext = shadow_extent(shs, p['rect'], tile=p['sheet'] == 'ftiles')
        if not ext: continue
        p['shadow'] = (shs, ext)
        ex, ey, ew, eh = ext
        im = kar.sheet(shs).crop((ex, ey, ex + ew, ey + eh))
        ox, oy = ex - p['rect'][0], ey - p['rect'][1]
        if p['flip']: im = im.transpose(Image.FLIP_LEFT_RIGHT); ox = (p['rect'][0] + p['rect'][2]) - (ex + ew)
        s._paste(s.shadow, im, p['left'] + ox, p['top'] + oy)
    # 5. the base grass the render will pick (replayed from a copy of the generator's state)
    import copy
    rng = copy.deepcopy(s.rng)
    for y in range(s.h):
        for x in range(s.w):
            r = rng.random()
            if r < .30: grass_picks.append(list(rng.choice(kar.SPARSE)))
            elif r < .34: grass_picks.append(list(rng.choice(kar.GRASS[:4])))
            else: grass_picks.append(None)
    return _render(s, out, scale)
kar.Map.render = render

sys.argv = ['layout2_crossroads.py', os.path.join(HERE, 'layout2_crossroads_built.png')]
runpy.run_path(os.path.join(HERE, 'layout2_crossroads.py'), run_name='__main__')
m = MAP

def extend(cells, w, h, by=2):
    """Roads that touch the border continue past it (off camera), so the border cell is drawn as road, not as an end."""
    out = set(map(tuple, cells))
    for x, y in cells:
        for i in range(1, by + 1):
            if x == 0: out.add((x - i, y))
            if x == w - 1: out.add((x + i, y))
            if y == 0: out.add((x, y - i))
            if y == h - 1: out.add((x, y + i))
    return sorted(out)

ground = {}
for name, mk in m.layers:
    kind = {'dirt': 'dirt', 'cobble2': 'cobble', 'water': 'water', 'soil': 'soil', 'gpatch0': 'patch0', 'gpatch1': 'patch1'}[name]
    cells = [[int(x), int(y)] for y, x in zip(*mk.nonzero())]
    if kind in ('dirt', 'cobble'): cells = [list(c) for c in extend(cells, m.w, m.h)]
    ground.setdefault(kind, {'cells': [], 'expected': []})
    ground[kind]['cells'] += cells
for name, rows in EXPECTED.items():
    kind = {'dirt': 'dirt', 'cobble2': 'cobble', 'water': 'water', 'soil': 'soil', 'gpatch0': 'patch0', 'gpatch1': 'patch1'}[name]
    if name == 'dirt' and len(rows) == 72: continue      # the script's eight beds (replaced by the soil below)
    ground[kind]['expected'] += rows
soil = ground['soil']
soil['expected'] = [e for e in EXPECTED['dirt'] if any(c <= e[0] < c + 3 for c in KEEP_BED_COLUMNS) and 4 <= e[1] <= 10] if 'soil' not in EXPECTED else soil['expected']
soil['expected'] = [e for e in soil['expected'] if [e[0], e[1]] in soil['cells']]

beds = []
for by in (4, 8):
    for bx in KEEP_BED_COLUMNS: beds.append([bx, by, 3, 3])

sprites = []
for p in placed:
    if p['sheet'] == 'crops': continue
    sheet = 'fp' if p['sheet'] == m.fol else p['sheet']
    if sheet == 'fp' and p['key'] is None: name = f"Flower_{p['rect'][0]}_{p['rect'][1]}"
    else: name = NAMES[(sheet, p['rect'])]
    group = GROUP.get(name, 'Fences' if name.startswith('Fence') else 'Flowers' if name.startswith('Flower')
                      else 'Trees' if name.startswith(('Tree', 'Bush', 'Shrub')) else 'Props')
    entry = dict(name=name, group=group, sheet=sheet, rect=list(p['rect']), left=p['left'], top=p['top'], flip=p['flip'])
    if 'shadow' in p: entry['shadow'] = dict(sheet=p['shadow'][0], rect=list(p['shadow'][1]))
    sprites.append(entry)

layout = dict(
    source='Tools/village/mockup/layout2_crossroads.py', exporter='Tools/village/mockup/export_crossroads.py',
    note='Kariaston, the Crossroads: tiles and art pixels, origin top-left, y down. Read once by KariastonBuilder.RelayoutBatch.',
    width=m.w, height=m.h,
    grass=grass_picks, ground=ground, beds=beds, sprites=sprites,
)
out = os.path.join(os.path.dirname(HERE), 'crossroads_layout.json')
with open(out, 'w') as f: json.dump(layout, f, separators=(',', ':'))
print(f'{out}: {len(sprites)} sprites, ground ' + ', '.join(f"{k} {len(v['cells'])}" for k, v in ground.items()))
