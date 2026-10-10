import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
m=Map(64,42,55)
m.forest_edge(gaps=[(-3,21,3,27),(29,39,36,45)])
cx,cy=32,24.5
ring=m.blob(m.mask(),cx,cy,14,8.8); inner=m.blob(m.mask(),cx,cy,8.8,4.3); rd=ring&~inner
m.path(rd,[(0,24),(19,24)],4); m.path(rd,[(45,24),(51,24)],3); m.path(rd,[(32,32),(32,42)],4)
m.path(rd,[(32,14),(32,17)],3)            # Tally Ho! door
m.path(rd,[(9,20.6),(9,24)],3)            # Maximo
m.path(rd,[(50,20.6),(50,24)],3)          # Grim
m.path(rd,[(32,36.6),(55,36.6)],3)   # lane to the tower
m.layer('dirt',rd)
cob=m.blob(m.mask(),cx,cy,13,7.9)&~m.blob(m.mask(),cx,cy,9.9,5.2)
m.path(cob,[(1,24),(20,24)],2); m.path(cob,[(32,31),(32,42)],2)
m.layer('cobble',cob)
m.occ|=inner
w=m.mask(); m.blob(w,12,34,6,2.3,0.3); m.layer('water',w)
m.put('tallyho',32,13.8); m.put('tankard',38.5,15.6)
m.put('bluehall',9,20.6); m.put('browncottage',50,20.6); m.put('tower',55,35.4); m.put('wagon',23.5,35.5)
# the green
statue(m,32,24.2)
gb=m.mask(); m.rect(gb,29,22,35,27); 
for (x,y) in ((27.5,22),(36.5,22),(27.5,28.2),(36.5,28.2)): m.put('bench',x,y)
for (x,y) in ((30,21.2),(34,21.2),(30,28.6),(34,28.6)): m.put('bushR' if x<32 else 'bushY',x,y)
m.put('treeXS',26,25.6); m.put('treeXS',38,25.6,flip=True)
m.put('cart',32,34.4); m.put('well',44,33.4)
lamps(m,[(22.5,19),(41.5,19),(21.5,29.5),(20,24.9)])
m.put('barrel',37,34); m.put('crate',37.8,35.6)
# farm behind the tavern, west
m.layer('dirt',garden(m,5,4,4,2,[0,3,9,13],3,3,1))
fence(m,3,2,20,11,gates=[(20,7),(20,8)])
m.put('scarecrow',12.5,7.2); m.put('haystack',22.5,6); m.put('haypile',23,10.5); m.put('trough',4,13)
plot(m,42,3,48,8,[(45,8)]); plot(m,51,3,57,8,[(54,8)]);
plot(m,15,26,21,31,[(18,26)])
dress(m,smalltrees=5)
m.render(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'layout5_village_green.png'), 3)
