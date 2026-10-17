import re, os, sys, json, collections
ROOT = r"G:\Cursor_Folder\DS4Windows-Vader4Pro"
OUT = os.path.dirname(os.path.abspath(__file__))

def strip_comments(src):
    """Remove // and /* */ comments, keep strings. Newlines preserved."""
    out = []; i = 0; n = len(src)
    while i < n:
        c = src[i]
        if c == '/' and i + 1 < n and src[i+1] == '/':
            while i < n and src[i] != '\n': i += 1
        elif c == '/' and i + 1 < n and src[i+1] == '*':
            j = src.find('*/', i + 2)
            j = n if j < 0 else j + 2
            out.append(''.join('\n' if ch == '\n' else ' ' for ch in src[i:j])); i = j
        elif c == '@' and i + 1 < n and src[i+1] == '"':
            j = i + 2
            while j < n:
                if src[j] == '"':
                    if j + 1 < n and src[j+1] == '"': j += 2; continue
                    break
                j += 1
            out.append(src[i:j+1]); i = j + 1
        elif c == '"':
            j = i + 1
            while j < n and src[j] != '"' and src[j] != '\n':
                if src[j] == '\\': j += 1
                j += 1
            out.append(src[i:j+1]); i = j + 1
        elif c == "'":
            j = i + 1
            while j < n and src[j] != "'" and src[j] != '\n':
                if src[j] == '\\': j += 1
                j += 1
            out.append(src[i:j+1]); i = j + 1
        else:
            out.append(c); i += 1
    return ''.join(out)

def files(sub, exts=('.cs',)):
    res = []
    for dp, dn, fn in os.walk(os.path.join(ROOT, sub)):
        dn[:] = [d for d in dn if d not in ('obj', 'bin', '.git', 'externals')]
        for f in fn:
            if f.endswith(exts): res.append(os.path.join(dp, f))
    return res

def rel(p): return os.path.relpath(p, ROOT).replace('\\', '/')

def read(p):
    with open(p, encoding='utf-8-sig', errors='replace') as f: return f.read()

def scope(p):
    r = rel(p)
    if r.startswith('DS4WindowsTests/') or r.startswith('StandaloneTests/'): return 'test'
    if r.startswith('DS4Windows/'): return 'prod'
    return 'other'

ALLFILES = files('DS4Windows') + files('DS4WindowsTests') + files('StandaloneTests')
SRC = {p: strip_comments(read(p)) for p in ALLFILES}
RAW = {p: read(p) for p in ALLFILES}

def parse_members(path, cls_open_hint=None):
    """Parse 8-space-indented member declarations of a single big class file."""
    lines = read(path).split('\n')
    stripped = strip_comments('\n'.join(lines)).split('\n')
    decl = []
    mod = re.compile(r'^        (public|internal|private|protected|static|const|protected internal)\b')
    for i, l in enumerate(stripped):
        if mod.match(l):
            decl.append(i)
    members = []
    for k, i in enumerate(decl):
        l = stripped[i].strip()
        if re.match(r'^(public |internal |private |protected )*(static )?(class|struct|enum)\b', l):
            m = re.search(r'(class|struct|enum)\s+(\w+)', l); name = m.group(2); kind = 'type'
        else:
            eq = l.find('='); par = l.find('(')
            kind = 'field/prop'
            if 'event ' in l:
                name = re.search(r'event\s+[\w<>\.\[\]]+\s+(\w+)', l).group(1); kind = 'event'
            elif par >= 0 and (eq < 0 or par < eq):
                name = re.search(r'(\w+)\s*(<[^>(]*>)?\s*\(', l).group(1); kind = 'method'
            else:
                head = l[:eq] if eq >= 0 else l
                head = head.split('{')[0].split(';')[0].replace('=>', '').strip()
                name = head.split()[-1]
                if 'const ' in l: kind = 'const'
        start = i
        end = (decl[k+1] if k + 1 < len(decl) else len(stripped))
        # end excludes trailing blank
        members.append(dict(name=name, kind=kind, line=i+1, end=end, text=l[:110]))
    return members, stripped

if __name__ == '__main__':
    gpath = os.path.join(ROOT, 'DS4Windows/DS4Control/Global.cs')
    mem, gl = parse_members(gpath)
    json.dump(mem, open(os.path.join(OUT, 'global_members.json'), 'w'), ensure_ascii=False, indent=1)
    print(len(mem), 'members;', len(set(m['name'] for m in mem)), 'names')
    bpath = os.path.join(ROOT, 'DS4Windows/DS4Control/BackingStore.cs')
    bmem, bl = parse_members(bpath)
    json.dump(bmem, open(os.path.join(OUT, 'backing_members.json'), 'w'), ensure_ascii=False, indent=1)
    print(len(bmem), 'backing members')
