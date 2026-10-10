"""Kariaston layout mockup engine (2026-10-10). Draws a village from the raw Minifantasy packs:
autotiled ground layers, shadows, y-sorted sprites. Mockup tool only; never used by the game."""
import os, random, math
from PIL import Image
import numpy as np
# Root of the raw Minifantasy packs (outside the repo). Override with MINIFANTASY_ROOT.
M=os.environ.get('MINIFANTASY_ROOT', r'C:\Dev\Minifantasy').rstrip('/\\')+'/'
A=M+'All_Exclusives_20261002/Addons/'
FOL=M+'Minifantasy_Plants_&_Foliage_v1.0/Minifantasy_Plants_&_Foliage_Assets/Plains_And_Forests/'
P={
 'plains':M+'Minifantasy_ForgottenPlains_v3.6_Commercial_Version/Minifantasy_ForgottenPlains_Assets/Tileset/Minifantasy_ForgottenPlainsTiles.png',
 'mc':M+'Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Tileset/Tileset.png',
 'bld':M+'Minifantasy_Towns2_v1.5/Minifantasy_Towns2_Assets/Buildings/_Mix_And_Match_Samples/Minifantasy_TownsIIMoreBuildingSamples.png',
 'bldsh':M+'Minifantasy_Towns2_v1.5/Minifantasy_Towns2_Assets/Buildings/_Mix_And_Match_Samples/Minifantasy_TownsIIMoreBuildingSamplesShadows.png',
 'tower':A+'_Miscellany/Wizard_Tower/Exterior/Wizard_Tower_Exterior.png',
 'wagons':A+'Medieval_Carnival/Caravans_And_Wagons/Tileset/CaravansAndWagons.png',
 'mon':A+'Towns_I_II/Town_Monuments/Monuments.png',
 'well':A+'Towns_I_II/Animated_Well/WellStaticFrames.png',
 'fp':FOL+'Forgotten_Plains.png','gf':FOL+'Green_Forest.png','af':FOL+'Autumn_Forest.png','folsh':FOL+'_Shadow.png',
 'mprops':M+'Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Props/Props.png',
 'fprops':M+'Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Props/Minifantasy_FarmProps.png',
 'ftiles':M+'Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Tileset/Minifantasy_FarmTileset.png',
 'crops':M+'Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Crops/Minifantasy_FarmSeedsAndCrops.png',
 'tprops':M+'Minifantasy_Towns_v3.0/Minifantasy_Towns_Assets/Props/Minifantasy_TownsProps.png',
 'cart':M+'All_Exclusives_20261002/Creatures/Travelling_Merchant/Merchant_On_Cart/Idle_Shop_Open.png',
 'grassv':A+'Forgotten_Plains/More_Grass_Variations/MoreGrassVariations.png',
 'tall':A+'Forgotten_Plains/Tall_Grass_Tileset/TallGrass.png',
 'moretrees':A+'_Miscellany/Tree_Variations/MoreTrees.png',
 'fruit':A+'Farm/Frutal_Trees/Frutal_Trees.png',
 'bridge':M+'Minifantasy_Towns2_v1.5/Minifantasy_Towns2_Assets/Structures/Stone Bridge/Minifantasy_TownsIIStoneBridgeTileset.png',
 'flags':A+'_Miscellany/8x8_Flags/8x8Flags.png',
}
_cache={}
def sheet(k):
    if k not in _cache: _cache[k]=Image.open(P[k]).convert('RGBA')
    return _cache[k]
def cut(k,x,y,w,h): return sheet(k).crop((x,y,x+w,y+h))

# sprite catalogue: key -> (sheet, rect, pivot_from_bottom_px, shadow_sheet or None)
S={
 'tallyho':('bld',(9,264,118,105),18,'bldsh'),
 'bluehall':('bld',(13,28,110,93),17,'bldsh'),
 'browncottage':('bld',(319,160,58,64),0,'bldsh'),
 'thatchcottage':('bld',(313,264,70,88),0,'bldsh'),
 'purplecottage':('bld',(317,44,62,60),0,'bldsh'),
 'stuccohall':('bld',(15,144,106,97),0,'bldsh'),
 'tower':('tower',(0,0,64,136),0,None),
 'wagon':('wagons',(302,229,28,61),0,None),
 'cart':('cart',(0,0,64,64),7,None),
 'pedestal':('mon',(40,16,16,27),0,None),
 'figure':('mon',(8,201,15,17),0,None),
 'plaque':('mon',(296,244,16,12),0,None),
 'well':('well',(0,0,24,24),0,None),
 'treeL':('fp',(16,124,118,72),10,'folsh'),'treeM':('fp',(157,134,71,59),7,'folsh'),
 'treeS':('fp',(243,144,54,46),5,'folsh'),'treeXS':('fp',(306,152,34,34),3,'folsh'),
 'shrub':('fp',(11,50,18,12),0,'folsh'),'bush':('fp',(43,49,26,15),0,'folsh'),
 'shrubB':('fp',(115,49,18,13),0,'folsh'),'bushB':('fp',(147,48,26,16),0,'folsh'),
 'shrubP':('fp',(218,48,20,14),0,'folsh'),'bushP':('fp',(250,48,28,16),0,'folsh'),
 'shrubR':('fp',(323,49,18,13),0,'folsh'),'bushR':('fp',(355,48,26,16),0,'folsh'),
 'shrubY':('fp',(427,48,18,14),0,'folsh'),'bushY':('fp',(459,47,26,17),0,'folsh'),
 'bench':('mprops',(24,113,16,7),0,None),'benchL':('mprops',(24,148,16,9),0,None),
 'lamp':('mprops',(112,98,9,30),0,None),'barrel':('mprops',(346,170,12,13),0,None),
 'crate':('mprops',(272,168,16,16),0,None),'flowerbox':('mprops',(99,42,10,11),0,None),
 'flowerboxR':('mprops',(130,43,12,10),0,None),'planter1':('mprops',(67,43,11,10),0,None),
 'planter2':('mprops',(83,44,10,9),0,None),'barrel2':('mprops',(371,171,10,11),0,None),
 'haystack':('fprops',(83,13,20,24),0,None),'haypile':('fprops',(11,13,33,23),0,None),
 'bales':('fprops',(16,48,32,8),0,None),'scarecrow':('fprops',(76,48,11,15),0,None),
 'trough':('fprops',(120,40,16,8),0,None),'bucket':('fprops',(121,57,6,6),0,None),
 'tankard':('tprops',(9,86,23,17),0,None),'signpost':('tprops',(147,94,10,9),0,None),
 'menuboard':('tprops',(130,92,12,11),0,None),'redwell':('tprops',(11,48,19,32),0,None),
 'fruit1':('fruit',(5,4,21,25),2,None),'fruit2':('fruit',(2,96,27,29),2,None),
 'mtSmall':('moretrees',(13,38,21,25),2,None),'mtDark':('moretrees',(88,21,40,42),3,None),
 'mtAutumn':('moretrees',(137,68,30,35),2,None),'mtWillow':('moretrees',(96,74,25,29),2,None),
 'mtDark2':('moretrees',(48,129,30,38),2,None),
}
def crop_sprite(n,g):  # farm crop n (0..14), growth g (0..2) -> 8x16
    gx=(n%3)*80+32+8*g; gy=(n//3)*16
    return ('crops',(gx,gy,8,16),0,None)

# autotile specs: sheet, 3x3 origin cell, inner origin cell (defaults below 3x3)
AT={
 'dirt':('mc',14,138),'cobble':('mc',18,138),'cobble2':('mc',22,138),
 'pdirt':('plains',7,3),'stone':('plains',12,3),'water':('plains',25,3),'water2':('plains',29,3),
 'gpatch0':('grassv',1,3),'gpatch1':('grassv',5,3),'gpatch2':('grassv',9,3),'gpatch3':('grassv',13,3),'gpatch4':('grassv',17,3),
 'tall':('tall',1,1),'tall2':('tall',5,1),
}
SPARSE=[(2,3),(3,3),(4,3),(2,5),(4,5),(3,5)]
GRASS=[(1,1),(2,1),(3,1),(4,1),(2,4),(3,5),(4,6),(2,7)]

class Map:
    def __init__(s,w,h,seed,foliage='fp',grass_tint=None):
        s.w,s.h=w,h; s.rng=random.Random(seed); s.fol=foliage
        s.img=Image.new('RGBA',(w*8,h*8)); s.shadow=Image.new('RGBA',(w*8,h*8))
        s.layers=[]  # region layers in order: (name,mask)
        s.sprites=[]; s.occ=np.zeros((h,w),bool); s.grass_tint=grass_tint
    def mask(s): return np.zeros((s.h,s.w),bool)
    # ---- region helpers
    def rect(s,m,x0,y0,x1,y1): m[max(0,y0):min(s.h,y1),max(0,x0):min(s.w,x1)]=True; return m
    def path(s,m,pts,wid=2):
        for (ax,ay),(bx,by) in zip(pts,pts[1:]):
            n=int(max(abs(bx-ax),abs(by-ay))*2)+1
            for i in range(n+1):
                t=i/max(n,1); x=ax+(bx-ax)*t; y=ay+(by-ay)*t
                s.rect(m,int(round(x-wid/2)),int(round(y-wid/2)),int(round(x-wid/2))+wid,int(round(y-wid/2))+wid)
        return m
    def blob(s,m,cx,cy,rx,ry,jit=0.0):
        for y in range(s.h):
            for x in range(s.w):
                d=((x+.5-cx)/rx)**2+((y+.5-cy)/ry)**2
                if d<=1+jit*(s.rng.random()-.5): m[y,x]=True
        return m
    def clean(s,m):
        m=m.copy()
        for _ in range(8):
            p=np.pad(m,1)
            n=p[:-2,1:-1];so=p[2:,1:-1];w=p[1:-1,:-2];e=p[1:-1,2:]
            bad=m&(((~n)&(~so))|((~w)&(~e)))
            # also diagonal-only pinches: all 4 orth present but opposite diagonals both missing
            nw=p[:-2,:-2];ne=p[:-2,2:];sw=p[2:,:-2];se=p[2:,2:]
            if not bad.any(): break
            m=m&~bad
        return m
    def layer(s,name,m,occupy=True):
        m=s.clean(m); s.layers.append((name,m))
        if occupy: s.occ|=m
        return m
    # ---- autotile
    def autotile(s,name,m):
        sh,ox,oy=AT[name]; src=sheet(sh); p=np.pad(m,1)
        for y in range(s.h):
            for x in range(s.w):
                if not m[y,x]: continue
                N=p[y,x+1];So=p[y+2,x+1];W=p[y+1,x];E=p[y+1,x+2]
                NW=p[y,x];NE=p[y,x+2];SW=p[y+2,x];SE=p[y+2,x+2]
                r=0 if not N else (2 if not So else 1); c=0 if not W else (2 if not E else 1)
                if (r,c)!=(1,1): cx,cy=ox+c,oy+r
                else:
                    miss=[k for k,v in (('NW',NW),('NE',NE),('SW',SW),('SE',SE)) if not v]
                    if not miss: cx,cy=ox+1,oy+1
                    elif miss==['NW']: cx,cy=ox,oy+3
                    elif miss==['NE']: cx,cy=ox+1,oy+3
                    elif miss==['SW']: cx,cy=ox,oy+4
                    elif miss==['SE']: cx,cy=ox+1,oy+4
                    elif set(miss)=={'NW','SE'}: cx,cy=ox+2,oy+3
                    elif set(miss)=={'NE','SW'}: cx,cy=ox+2,oy+4
                    else: cx,cy=ox+1,oy+1
                t=src.crop((cx*8,cy*8,cx*8+8,cy*8+8)); s.img.alpha_composite(t,(x*8,y*8))
    # ---- sprites
    def put(s,key,fx,fy,flip=False,occ=True,sortoff=0,spec=None):
        """fx,fy: foot position in tiles (float). sprite bottom-centre sits at foot + pivot."""
        sh,(x,y,w,h),piv,shs=spec or S[key]
        if sh=='fp' and key in ('treeL','treeM','treeS','treeXS'): sh=s.fol
        im=cut(sh,x,y,w,h); shim=cut(shs,x,y,w,h) if shs else None
        if flip:
            im=im.transpose(Image.FLIP_LEFT_RIGHT); shim=shim.transpose(Image.FLIP_LEFT_RIGHT) if shim else None
        px=int(round(fx*8-w/2)); py=int(round(fy*8-h+piv))
        s.sprites.append((fy*8+sortoff,px,py,im))
        if shim: s.shadow.alpha_composite(shim,(px,py)) if 0<=px and 0<=py and px+w<=s.w*8 and py+h<=s.h*8 else s._paste(s.shadow,shim,px,py)
        if occ and h>=40 and key not in ('treeL','treeM','treeS','lamp'):
            s.occ[max(0,py//8):min(s.h,int(fy)+2),max(0,px//8-1):min(s.w,(px+w)//8+2)]=True
        elif occ:
            x0=int((px)//8); x1=int((px+w)//8)+1; y1=int(fy)+1; y0=max(0,y1-max(1,int(min(h,w)/16)+1))
            s.occ[max(0,y0):min(s.h,y1),max(0,x0):min(s.w,x1)]=True
        return px,py,w,h
    def _paste(s,dst,im,px,py):
        tmp=Image.new('RGBA',dst.size); tmp.paste(im,(px,py),im); dst.alpha_composite(tmp)
    def free(s,x,y,r=0):
        if x-r<0 or y-r<0 or x+r>=s.w or y+r>=s.h: return False
        return not s.occ[y-r:y+r+1,x-r:x+r+1].any()
    def scatter(s,keys,n,area=None,r=0,occ=False,tries=20):
        x0,y0,x1,y1=area or (0,0,s.w,s.h); placed=0
        for _ in range(n*tries):
            if placed>=n: break
            x=s.rng.randrange(x0,x1); y=s.rng.randrange(y0,y1)
            if s.free(x,y,r):
                k=s.rng.choice(keys); s.put(k,x+s.rng.random(),y+s.rng.random()*0.8+0.2,flip=s.rng.random()<.5,occ=occ)
                if not occ: s.occ[y,x]=True
                placed+=1
    def flowers(s,n,area=None):
        # small flower clusters / tufts from foliage sheet rows 72-112 & 8-24
        specs=[]
        for gx in (120,224,328,432):
            for dy in (72,88,104):
                specs.append((gx,dy,24,10)); specs.append((gx+32,dy,24,10))
        tufts=[(16,8,16,12),(48,8,16,12),(120,8,24,14),(152,8,24,12),(225,8,24,15),(330,8,24,15),(432,6,24,17)]
        x0,y0,x1,y1=area or (0,0,s.w,s.h)
        placed=0
        while placed<n:
            cx=s.rng.randrange(x0,x1); cy=s.rng.randrange(y0,y1); kind=s.rng.random()<.4
            col=s.rng.randrange(4); placed+=1
            for i in range(s.rng.randint(2,6)):
                x=cx+s.rng.randint(-2,2); y=cy+s.rng.randint(-1,1)
                if not s.free(x,y): continue
                r=s.rng.choice(tufts) if kind else specs[col*6+s.rng.randrange(6)]
                s.put(None,x+s.rng.random(),y+.9,occ=False,spec=('fp',r,0,None)); s.occ[y,x]=True; placed+=1
    def forest_edge(s,thick=3,gaps=()):
        """dense overlapping tree wall round the map edges"""
        trees=['treeL','treeM','treeM','treeS']
        def ok(x,y):
            for (gx0,gy0,gx1,gy1) in gaps:
                if gx0<=x<gx1 and gy0<=y<gy1: return False
            return True
        pts=[]
        for x in range(-2,s.w+3,5):
            pts.append((x+s.rng.random()*2,1.5+s.rng.random()*1.5)); pts.append((x+2.5+s.rng.random()*2,-0.5+s.rng.random()))
        for x in range(-2,s.w+3,6):
            pts.append((x+s.rng.random()*2,s.h+1.2+s.rng.random()))
        for y in range(2,s.h,5):
            pts.append((-1+s.rng.random()*2.5,y+s.rng.random()*2)); pts.append((s.w+1-s.rng.random()*2.5,y+s.rng.random()*2))
        for (x,y) in pts:
            if ok(x,y): s.put(s.rng.choice(trees),x,y,flip=s.rng.random()<.5)
        for y in range(s.h):
            for x in range(s.w):
                if (x<thick or x>=s.w-thick or y<thick+1 or y>=s.h-1) and ok(x,y): s.occ[y,x]=True
    # ---- final
    def render(s,out,scale=3):
        base=s.img; g=Image.new('RGBA',base.size)
        flat=sheet('plains').getpixel((2*8+1,3*8+1))
        g=Image.new('RGBA',base.size,flat)
        for y in range(s.h):
            for x in range(s.w):
                r=s.rng.random()
                if r<.30: cx,cy=s.rng.choice(SPARSE)
                elif r<.34: cx,cy=s.rng.choice(GRASS[:4])
                else: continue
                g.alpha_composite(sheet('plains').crop((cx*8,cy*8,cx*8+8,cy*8+8)),(x*8,y*8))
        g.alpha_composite(base); s.img=g
        for name,m in s.layers: s.autotile(name,m)
        s.img.alpha_composite(s.shadow)
        for (_,px,py,im) in sorted(s.sprites,key=lambda t:t[0]): s._paste(s.img,im,px,py)
        o=s.img.convert('RGB'); o=o.resize((o.width*scale,o.height*scale),Image.NEAREST); o.save(out); return out
