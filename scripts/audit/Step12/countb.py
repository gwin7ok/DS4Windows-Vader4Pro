import re, json, os, collections
from audit import *
bp = os.path.normpath(os.path.join(ROOT,'DS4Windows/DS4Control/BackingStore.cs'))
bm = json.load(open('backing_members.json'))
names = sorted(set(m['name'] for m in bm))
ext = {n: collections.defaultdict(int) for n in names}
dre = re.compile(r'\.\s*(\w+)\b')
for p, s in SRC.items():
    if p == bp: continue
    for m in dre.finditer(s):
        w = m.group(1)
        if w in ext: ext[w][rel(p)] += 1
# also object-initializer / bare use in other files of same name can't be known; treat '.NAME' only, plus 'NAME =' in initializer contexts
init = {n: collections.defaultdict(int) for n in names}
ire = re.compile(r'[{,]\s*(\w+)\s*=(?!=)')
for p, s in SRC.items():
    if p == bp: continue
    for m in ire.finditer(s):
        if m.group(1) in init: init[m.group(1)][rel(p)] += 1
bl = SRC[bp].split('\n')
owner = {}
for idx, m in enumerate(bm):
    for ln in range(m['line']-1, m['end']): owner[ln] = idx
internal = {n:set() for n in names}
tok = re.compile(r'(?<![\w@])(\w+)\b')
for ln, text in enumerate(bl):
    if ln not in owner: continue
    o = owner[ln]
    for m in tok.finditer(text):
        w = m.group(1)
        if w in internal and bm[o]['name'] != w:
            pre = text[:m.start()].rstrip()
            if pre.endswith('.') and not re.search(r'(this|store|config|Config)\s*\.$', pre) and not re.search(r'\]\s*\.$|\)\s*\.$', pre) : continue
            internal[w].add(o)
res=[]
for n in names:
    e = ext[n]; i = init[n]
    prod = sum(c for f,c in e.items() if not f.startswith(('DS4WindowsTests','StandaloneTests')))
    test = sum(c for f,c in e.items() if f.startswith(('DS4WindowsTests','StandaloneTests')))
    res.append(dict(name=n, kinds=sorted(set(m['kind'] for m in bm if m['name']==n)), lines=[m['line'] for m in bm if m['name']==n],
       prod=prod, test=test, init=sum(i.values()), files=dict(list(e.items())[:5]), internal=sorted(set(bm[o]['name'] for o in internal[n]))))
json.dump(res, open('backing_counts.json','w'), ensure_ascii=False, indent=1)
byn={r['name']:r for r in res}
dead=set(n for n,r in byn.items() if r['prod']==0 and r['init']==0)
ch=True
while ch:
    ch=False
    for n in list(dead):
        if any(x not in dead for x in byn[n]['internal']): dead.discard(n); ch=True
print(len(names),'names; zero-ext-prod', sum(1 for r in res if r['prod']==0 and r['init']==0),'; trans-dead', len(dead))
for n in sorted(dead):
    r=byn[n]; print(n, r['kinds'], 'L',r['lines'], 'test',r['test'], 'int', r['internal'][:4])
