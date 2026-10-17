import re, os, json, collections
from audit import *

ifaces = {}  # name -> (path, members)
for p in ALLFILES:
    r = rel(p)
    if not r.startswith('DS4Windows/'): continue
    s = SRC[p]
    for m in re.finditer(r'\binterface\s+(I\w+)\b', s):
        name = m.group(1)
        # body
        i = s.find('{', m.end())
        if i < 0: continue
        depth = 0; j = i
        while j < len(s):
            if s[j] == '{': depth += 1
            elif s[j] == '}':
                depth -= 1
                if depth == 0: break
            j += 1
        body = s[i+1:j]
        mem = []
        for line in body.split('\n'):
            l = line.strip()
            if not l or l.startswith('['): continue
            if l.endswith(';') or l.endswith('}'):
                mm = re.search(r'(\w+)\s*(<[^>]*>)?\s*\(', l)
                if mm and '=>' not in l.split('(')[0]:
                    mem.append(mm.group(1))
                else:
                    pm = re.search(r'(\w+)\s*\{\s*get', l)
                    if pm: mem.append(pm.group(1))
                    else:
                        em = re.search(r'event\s+[\w<>\.]+\s+(\w+)\s*;', l)
                        if em: mem.append(em.group(1))
        ifaces[name] = (r, sorted(set(mem)))

# implementers
impl = collections.defaultdict(set)
for p in ALLFILES:
    r = rel(p)
    s = SRC[p]
    for m in re.finditer(r'\bclass\s+\w+[^{;]*?:\s*([^{]+)\{', s):
        for b in re.findall(r'\b(I[A-Z]\w+)\b', m.group(1)):
            if b in ifaces: impl[b].add(r)

out = []
for iname, (ipath, mems) in sorted(ifaces.items()):
    excl = {ipath} | impl[iname]
    for mem in mems:
        prod = test = 0
        pat = re.compile(r'(?<![\w])' + re.escape(mem) + r'\b')
        for p in ALLFILES:
            r = rel(p)
            if r in excl: continue
            n = len(pat.findall(SRC[p]))
            if not n: continue
            if scope(p) == 'test': test += n
            else: prod += n
        out.append((iname, mem, prod, test, ipath))
zero = [o for o in out if o[2] == 0]
print('interfaces', len(ifaces), 'members', len(out), 'zero-prod', len(zero))
byi = collections.Counter(o[0] for o in zero)
for k, v in byi.most_common(): print(v, k)
print('--- zero-prod, with test refs')
for o in zero:
    print(o[0], o[1], 'test', o[3])
