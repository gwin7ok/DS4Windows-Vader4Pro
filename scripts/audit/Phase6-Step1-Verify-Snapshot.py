"""Read-only textual verification; not a C# semantic/reference completeness audit.
Run with Python 3. Uses pinned Git objects, never the working-tree source/documents.
Outputs JSON to stdout; creates/changes no files, refs, or working trees.
"""
import hashlib
import json
import re
import subprocess
from collections import Counter

ROOT = r"G:\Cursor_Folder\DS4Windows-Vader4Pro"
SOURCE = "62e69387ebb99d32003897b261ca2ac9fb3bebd8"
ORIGINAL = "59d46db47cc9d20f212b0f6fa881515b767c91ed"
DOCUMENTS = "cbf1dd172f2197be4f88574099e40d2adc1c9b9e"
DOCROOT = "docs-forDIMG/MadeByAgent/"


def git(*args):
    return subprocess.check_output(["git", "-C", ROOT, *args])


def text(revision, path):
    return git("show", f"{revision}:{path}").decode("utf-8-sig")


def tree(revision):
    return git("rev-parse", f"{revision}:DS4Windows").decode().strip()


def main():
    entries = git("ls-tree", "-r", "-z", SOURCE, "--", "DS4Windows").split(b"\0")
    manifest = []
    sources = {}
    for entry in entries:
        if not entry:
            continue
        metadata, raw_path = entry.split(b"\t", 1)
        path = raw_path.decode("utf-8")
        if not path.endswith(".cs") or any(p in ("bin", "obj") for p in path.split("/")):
            continue
        blob = metadata.decode().split()[2]
        manifest.append({"path": path, "blob": blob})
        sources[path] = text(SOURCE, path).splitlines()
    manifest.sort(key=lambda x: x["path"])
    counts, imports = [], []
    for item in manifest:
        path = item["path"]
        lines = sources[path]
        hits = [len(re.findall(r"\bGlobal\s*\.", line)) for line in lines]
        if sum(hits):
            counts.append({"path": path, "lines": sum(n > 0 for n in hits), "occurrences": sum(hits)})
        for number, line in enumerate(lines, 1):
            if re.search(r"using\s+static\s+(?:global::)?DS4Windows\.Global\s*;", line):
                imports.append({"path": path, "line": number})

    checks, skipped = [], []
    core_files = {name: "DS4Windows/DS4Control/" + name for name in (
        "ControlService.cs", "Mapping.cs", "DS4LightBar.cs", "Mouse.cs", "MouseCursor.cs")}
    for kind, filename in (("Core", "Phase6-Step1-Core-Reference-Evidence.md"),
                           ("UI", "Phase6-Step1-UI-Reference-Evidence.md")):
        document = text(DOCUMENTS, DOCROOT + filename)
        active = None
        schema = None
        last_member = None
        default_mapping = False
        for row_number, row in enumerate(document.splitlines(), 1):
            if kind == "Core" and row.startswith("## "):
                active = next((p for n, p in core_files.items() if f"`{n}`" in row), None)
                schema = None
                last_member = None
            if kind == "UI" and row.startswith("### ["):
                match = re.search(r"/(DS4Windows/[^)]+\.cs)\)", row, re.I)
                active = match.group(1) if match else None
                schema = None
                last_member = None
            if kind == "UI" and row.startswith("## "):
                active = "DS4Windows/App.xaml.cs" if "App.xaml.cs" in row else None
                schema = None
            if row.startswith("### "):
                default_mapping = "`outputKBMMapping`" in row
                schema = None
            if not row.startswith("|"):
                continue
            cells = [c.strip() for c in row.strip().strip("|").split("|")]
            if any("行番号" in c for c in cells) and any("メンバ" in c or "下位" in c for c in cells):
                line_col = next(i for i, c in enumerate(cells) if "行番号" in c)
                member_col = next(i for i, c in enumerate(cells) if "メンバ" in c or "下位" in c)
                schema = (line_col, member_col)
                continue
            if not active or not schema or len(cells) <= max(schema):
                continue
            line_cell, member_cell = cells[schema[0]], cells[schema[1]]
            if not re.search(r"\d", line_cell):
                continue
            tokens = re.findall(r"`([A-Za-z_]\w*(?:\.\w+)*)`", member_cell)
            if default_mapping:
                member = "outputKBMMapping"
            elif tokens:
                member = tokens[0].split(".")[0]
            elif "同上" in member_cell and last_member:
                member = last_member
            else:
                skipped.append({"document": kind, "row": row_number, "reason": "member not explicit", "text": row})
                continue
            last_member = member
            # Do not silently expand ranges, prose dates, or unnamed positions.
            if re.search(r"\d\s*[-–〜]\s*\d", line_cell):
                skipped.append({"document": kind, "row": row_number, "reason": "range", "text": row})
                continue
            numbers = [int(n) for n in re.findall(r"\d+", line_cell)]
            for number in numbers:
                lines = sources.get(active, [])
                source_line = lines[number - 1] if 1 <= number <= len(lines) else ""
                found = bool(re.search(r"\b" + re.escape(member) + r"\b", source_line))
                checks.append({"document": kind, "document_row": row_number, "path": active,
                               "line": number, "member": member, "match": found,
                               "source": source_line.strip()})

    # Service document has ambiguous ranges; only its precise changed-file claim is checked here.
    hid = sources["DS4Windows/HidLibrary/HidDevices.cs"]
    service_check = [{"path": "DS4Windows/HidLibrary/HidDevices.cs", "line": n,
                      "match": "Global.DeviceOptions.VerboseLogMessages" in hid[n - 1],
                      "source": hid[n - 1].strip()} for n in (74, 87)]
    result = {
        "source_commit": SOURCE, "documents_commit": DOCUMENTS,
        "source_tree": tree(SOURCE), "original_source_tree": tree(ORIGINAL),
        "original_source_identical": tree(SOURCE) == tree(ORIGINAL),
        "changed_source_since_snapshot": git("diff", "--name-only", SOURCE, DOCUMENTS, "--", "DS4Windows").decode().splitlines(),
        "manifest_sha256": hashlib.sha256(json.dumps(manifest, ensure_ascii=False, sort_keys=True).encode()).hexdigest(),
        "files_scanned": len(manifest), "files_matched": len(counts),
        "matching_lines": sum(r["lines"] for r in counts),
        "text_occurrences": sum(r["occurrences"] for r in counts),
        "imports": imports, "file_counts": counts,
        "checks_by_document": dict(Counter(c["document"] for c in checks)),
        "matching_checks": sum(c["match"] for c in checks),
        "failed_checks": [c for c in checks if not c["match"]],
        "skipped_rows": skipped, "service_changed_file_check": service_check,
        "manifest": manifest,
        "scope_warning": "Checks only parseable Core/UI table rows, plus two Service positions. Word presence is not semantic binding or classification correctness; comments can match. Non-table prose, unparsed tables, ranges, unknown members, and completeness are not certified."
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
