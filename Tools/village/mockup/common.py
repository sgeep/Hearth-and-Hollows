from opt1 import *
def statue(m,x,y):
    m.put('pedestal',x,y); m.put(None,x,y-27/8+0.05,occ=False,sortoff=1,spec=S['figure'])
    m.put('plaque',x,y+1.2,occ=False)
def patches(m,n,keys=('gpatch0','gpatch1','gpatch3','tall','tall2')):
    for i in range(n):
        k=m.rng.choice(keys); pm=m.mask()
        cx=m.rng.uniform(4,m.w-4); cy=m.rng.uniform(4,m.h-3)
        m.blob(pm,cx,cy,m.rng.uniform(2,4.5),m.rng.uniform(1.6,3),0.5)
        pm=m.clean(pm&~m.occ)
        if pm.sum()>=4: m.layers.append((k,pm))
def lamps(m,pts):
    for x,y in pts: m.put('lamp',x,y)
def dress(m,flowers=70,bushes=26,patch=14,smalltrees=8,bushkeys=None):
    patches(m,patch)
    m.scatter(['treeS','treeXS','treeM'] if smalltrees else [],smalltrees,(4,5,m.w-4,m.h-4),r=4,occ=True)
    m.scatter(bushkeys or ['bush','shrub','bushB','bushP','bushR','bushY','shrubR','shrubY'],bushes,(3,4,m.w-3,m.h-2),r=1,occ=True)
    m.flowers(flowers,(2,4,m.w-2,m.h-1))
def plot(m,x0,y0,x1,y1,gate):
    fence(m,x0,y0,x1,y1,gates=gate); m.put('signpost',gate[0][0]+1.6,y1+1.1)
    m.occ[y0:y1+1,x0:x1+1]=True
