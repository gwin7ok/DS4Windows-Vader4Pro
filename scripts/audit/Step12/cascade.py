import re, json, os, collections
from audit import *
gp = os.path.normpath(os.path.join(ROOT,'DS4Windows/DS4Control/Global.cs'))
bp = os.path.normpath(os.path.join(ROOT,'DS4Windows/DS4Control/BackingStore.cs'))
gm = json.load(open('global_members.json')); bm = json.load(open('backing_members.json'))
gdead = set(json.load(open('global_dead.json')))
gl = SRC[gp].split('\n')
alive_lines = []
dead_lines=set()
for m in gm:
    if m['name'] in gdead:
        for ln in range(m['line']-1, m['end']): dead_lines.add(ln)
live_global_text = '\n'.join('' if i in dead_lines else l for i,l in enumerate(gl))
names = sorted(set(m['name'] for m in bm))
dre = re.compile(r'\.\s*(\w+)\b')
ext = {n: collections.defaultdict(int) for n in names}
srcs = {p:s for p,s in SRC.items() if p not in (bp,)}
srcs[gp] = live_global_text
for p,s in srcs.items():
    for m in dre.finditer(s):
        if m.group(1) in ext: ext[m.group(1)][rel(p)] += 1
init = {n: collections.defaultdict(int) for n in names}
ire = re.compile(r'[{,]\s*(\w+)\s*=(?!=)')
for p,s in srcs.items():
    for m in ire.finditer(s):
        if m.group(1) in init: init[m.group(1)][rel(p)] += 1
bc = {r['name']:r for r in json.load(open('backing_counts.json'))}
cand=set()
for n in names:
    prod = sum(c for f,c in ext[n].items() if not f.startswith(('DS4WindowsTests','StandaloneTests')))
    if prod==0 and sum(init[n].values())==0: cand.add(n)
ch=True
while ch:
    ch=False
    for n in list(cand):
        if any(x not in cand for x in bc[n]['internal']): cand.discard(n); ch=True
print('backing cascade-dead (after Global dead removed):', len(cand), 'of', len(names))
json.dump(sorted(cand), open('backing_dead_cascade.json','w'))
tests = {n: sum(c for f,c in ext[n].items() if f.startswith(('DS4WindowsTests','StandaloneTests'))) for n in cand}
print('with test refs:', {n:t for n,t in tests.items() if t})
