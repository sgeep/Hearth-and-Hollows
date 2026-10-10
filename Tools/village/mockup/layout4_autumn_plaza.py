import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
m=Map(64,42,44,foliage='af')
m.forest_edge(gaps=[(-3,25,3,31),(29,39,36,45)])
road=m.mask()
m.path(road,[(0,28),(32,28),(32,42)],4)          # west road into plaza, south exit
m.path(road,[(42,14),(42,18)],3)                  # tavern door to plaza
m.path(road,[(32,28),(56,28)],3)                  # east lane
m.path(road,[(55.5,23.6),(55.5,28)],3)            # Grim's door
m.path(road,[(20,26.4),(20,28)],3)
m.path(road,[(7,26),(7,28)],3)                    # tower door
pz=m.mask(); m.blob(pz,40,23,9,5.5)
m.layer('dirt',road|pz)
st=m.mask(); m.blob(st,40,23,7.5,4.2); m.layer('stone',st)
w=m.mask(); m.blob(w,48,35,6.5,2.6,0.3); m.layer('water',w)
m.put('tallyho',42,13.6); m.put('tankard',48.5,15.4)
m.put('tower',7,26.2); m.put('bluehall',20,26.4); m.put('browncottage',55.5,23.6); m.put('wagon',38,35)
statue(m,40,23.2); m.put('well',35,26); m.put('cart',45.5,26.3)
for x,y in ((35,20.6),(45,20.6)): m.put('bench',x,y)
lamps(m,[(31.5,21),(48.5,21),(31.5,27),(48.5,27)])
m.put('flowerboxR',38,19.6); m.put('flowerbox',42,19.6); m.put('barrel',50,25); m.put('crate',50.6,26.6)
m.layer('dirt',garden(m,8,5,4,2,[2,5,8,14],3,3,1))
fence(m,6,3,23,12,gates=[(23,9),(23,10)])
for i,(x,y) in enumerate(((27,7),(31,6.5),(27.5,11),(31.5,10.6))): m.put('fruit1' if i%2 else 'fruit2',x,y)
m.put('scarecrow',15,4.6); m.put('haystack',4,15); m.put('haypile',9,15.5); m.put('bales',18,14.5)
plot(m,52,4,58,9,[(55,9)]); plot(m,9,32,15,37,[(12,32)]); plot(m,18,32,24,37,[(21,32)])
m.put('signpost',4,29.5)
for (x,y) in ((26,19),(52,15.5),(28,35),(60,33),(36,32)): m.put('mtAutumn',x,y,flip=x>30)
dress(m,bushkeys=['bushY','shrubY','bushR','shrubR','bush','shrub'],smalltrees=5)
m.render(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'layout4_autumn_plaza.png'), 3)
