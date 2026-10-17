import re, json, os, collections
from audit import *
gp = os.path.normpath(os.path.join(ROOT,'DS4Windows/DS4Control/Global.cs'))
bp = os.path.normpath(os.path.join(ROOT,'DS4Windows/DS4Control/BackingStore.cs'))
gm = json.load(open('global_members.json'))
bm = json.load(open('backing_members.json'))
BARE_FILES = [p for p in SRC if re.search(r'(?m)^s*using static DS4Windows.Global;', SRC[p])]
print('bare files', [rel(p) for p in BARE_FILES])

def lines_of(p): return SRC[p].split('\n')

# ---- Global ----
names = sorted(set(m['name'] for m in gm))
qual = {n: collections.defaultdict(int) for n in names}   # file -> count
bare = {n: collections.defaultdict(int) for n in names}
qre = re.compile(r'(?<![\w])(?:DS4Windows\s*\.\s*)?Global\s*\.\s*(\w+)')
for p, s in SRC.items():
    if p == gp: continue
    for m in qre.finditer(s):
        if m.group(1) in qual: qual[m.group(1)][rel(p)] += 1
for p in BARE_FILES:
    s = SRC[p]
    for m in re.finditer(r'(?<![\w.@])(\w+)\b', s):
        if m.group(1) in bare: bare[m.group(1)][rel(p)] += 1

# internal refs: inside Global.cs, bare name occurrences outside own span(s) -> which members refer
gl = SRC[gp].split('\n')
spans = {}  # idx -> member
owner = {}
for idx, m in enumerate(gm):
    for ln in range(m['line']-1, m['end']): owner[ln] = idx
# the first lines before first member belong to nothing
internal_from = {n: set() for n in names}   # n -> set of member idx that reference n
tok = re.compile(r'(?<![\w@])(\w+)\b')
# also qualified Global.X inside Global.cs
for ln, text in enumerate(gl):
    if ln not in owner: continue
    o = owner[ln]
    for m in tok.finditer(text):
        w = m.group(1)
        if w in internal_from and gm[o]['name'] != w:
            # exclude when preceded by '.' and not Global.
            pre = text[:m.start()].rstrip()
            if pre.endswith('.') and not re.search(r'Global\s*\.$', pre): continue
            internal_from[w].add(o)
# BackingStore-side: m_Config usage etc is irrelevant here.

res = []
for n in names:
    q = dict(qual[n]); b = dict(bare[n])
    ext_prod = {f:c for f,c in {**q}.items() if not f.startswith(('DS4WindowsTests','StandaloneTests'))}
    ext_test = {f:c for f,c in q.items() if f.startswith(('DS4WindowsTests','StandaloneTests'))}
    bprod = dict(b)
    res.append(dict(name=n, kinds=sorted(set(m['kind'] for m in gm if m['name']==n)),
        lines=[m['line'] for m in gm if m['name']==n],
        q_prod=sum(ext_prod.values()), q_prod_files=ext_prod, q_test=sum(ext_test.values()), q_test_files=ext_test,
        bare=sum(bprod.values()), bare_files=bprod,
        internal=sorted(set(gm[o]['name'] for o in internal_from[n]))))
json.dump(res, open('global_counts.json','w'), ensure_ascii=False, indent=1)

# transitive dead (prod-only) : member dead if no external prod/bare refs and all internal referrers dead
byname = {r['name']: r for r in res}
dead = set(n for n,r in byname.items() if r['q_prod']==0 and r['bare']==0)
changed = True
# internal referrers must be dead for a candidate to stay dead
while changed:
    changed = False
    for n in list(dead):
        for ref in byname[n]['internal']:
            if ref not in dead:
                dead.discard(n); changed = True; break
print('names', len(names), 'zero-ext-prod (incl bare):', sum(1 for r in res if r['q_prod']==0 and r['bare']==0), 'transitively dead:', len(dead))
json.dump(sorted(dead), open('global_dead.json','w'), ensure_ascii=False)
tot_q = sum(r['q_prod']+r['q_test'] for r in res)
print('total qualified refs (named members)', tot_q)
