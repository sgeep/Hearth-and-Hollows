import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from common import *
m=Map(64,40,11)
m.forest_edge(gaps=[(60,31,66,37)])
# ground
road=m.mask(); m.path(road,[(1,34),(63,34)],3)
m.path(road,[(32,14),(32,22)],3)       # tavern to square
m.path(road,[(13,31),(13,34)],3); m.path(road,[(47,31),(47,34)],3); m.path(road,[(57,31),(57,34)],3)
m.path(road,[(22,14),(30,14)],2)        # garden lane
sq=m.mask(); m.rect(sq,25,20,41,32); m.path(sq,[(32,32),(32,34)],3)
m.layer('dirt',road|sq)
cob=m.mask(); m.rect(cob,26,21,40,31); m.layer('cobble',cob)
w=m.mask(); m.blob(w,11.5,17.2,6,1.9,0.3); m.layer('water',w)
# buildings
m.put('tallyho',32,13.2); m.put('tankard',38,14.6)
m.put('bluehall',13,30.8); m.put('browncottage',47,31); m.put('tower',57,31); m.put('wagon',21.5,29)
# square
statue(m,33,25); m.put('well',28.5,29.5); m.put('cart',37.5,29.6)
for x,y in ((27,22.5),(39,22.5)): m.put('benchL',x,y)
lamps(m,[(25.5,21),(40.5,21),(25.5,31.5),(40.5,31.5)])
m.put('flowerboxR',30,22); m.put('flowerbox',36,22); m.put('barrel',41.5,28); m.put('crate',41.6,29.8)
# garden
m.layer("dirt",garden(m,6,5,4,2,[0,4,9,12],3,3,1))
fence(m,4,3,21,12,gates=[(21,10),(21,11)])
m.put('scarecrow',12.5,9.3); m.put('haystack',3,14); m.put('bales',8,14.5); m.put('trough',17,14.4)
# plots
plot(m,43,6,49,11,[(46,11)]); plot(m,51,6,57,11,[(54,11)])
m.layer('dirt',m.path(m.mask(),[(38,14),(58,14)],2))
m.put('signpost',61,32.5)
# pond dressing
m.scatter(['shrub','bush'],5,(4,14,21,20),r=0,occ=True)
dress(m)
m.render(sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'layout1_tidied_original.png'), 3)
