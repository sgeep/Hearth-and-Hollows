import sys; sys.path.insert(0,'.')
from kar import *
def garden(m,x0,y0,cols,rows,crops,bw=3,bh=3,gap=1,stage=None):
    beds=m.mask()
    for j in range(rows):
        for i in range(cols):
            bx=x0+i*(bw+gap); by=y0+j*(bh+gap)
            m.rect(beds,bx,by,bx+bw,by+bh)
            n=crops[(j*cols+i)%len(crops)]
            for yy in range(1,bh):
                for xx in range(bw):
                    g=stage if stage is not None else m.rng.choice((1,2,2))
                    m.put(None,bx+xx+.5,by+yy+.85,occ=False,spec=crop_sprite(n,g))
    return beds
def fence(m,x0,y0,x1,y1,gates=()):
    """thin farm fence round tile rect [x0,x1]x[y0,y1] (inclusive cells)"""
    T={'TL':(50,15),'H':(51,15),'TR':(54,15),'V':(50,16),'BL':(50,19),'BR':(54,19)}
    def tile(k,x,y):
        cx,cy=T[k]; m.put(None,x+.5,y+1,occ=True,spec=('ftiles',(cx*8,cy*8,8,8),0,None))
    for x in range(x0,x1+1):
        for y in (y0,y1):
            if (x,y) in gates: continue
            k='H'
            if x==x0: k='TL' if y==y0 else 'BL'
            elif x==x1: k='TR' if y==y0 else 'BR'
            tile(k,x,y)
    for y in range(y0+1,y1):
        for x in (x0,x1):
            if (x,y) not in gates: tile('V',x,y)
