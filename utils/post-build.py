import os
from pathlib import Path
import sys
import shutil

target_dir = Path(sys.argv[1])
project_dir = Path(sys.argv[2])
version = sys.argv[3]
# Optional 4th argument: explicit arch string (e.g. 'x64' or 'x86')
arch_override = None
if len(sys.argv) > 4:
    arch_override = sys.argv[4]

# ディレクトリ存在チェック
if not target_dir.exists():
    print(f"Error: target_dir not found: {target_dir}")
    sys.exit(1)




# run the script injecting new dependency paths to DS4Windows.deps.json
lang_script = project_dir.parent / "utils" / "inject_deps_path.py"
deps_json_path = target_dir / "DS4Windows.deps.json"
os.system(f"python {lang_script} {deps_json_path}")


# write the version to newest.txt
newest_txt = project_dir / "newest.txt"
with open(newest_txt, 'w') as file:
    file.write(version)


# rename target dir (net8.0-windows) to DS4Windows
output_dir = target_dir
lang_dir = output_dir / "Lang"
if not lang_dir.exists():
    lang_dir.mkdir()

langs = ["ar", "cs", "de", "el", "es", "fi", "fr", "he", "hu-HU", "idn", "it", "ja", "ms",
         "nl", "pl", "pt", "pt-BR", "ru", "se", "tr", "uk-UA", "vi", "zh-Hans", "zh-Hant", "zh-CN"]
search_dirs = [output_dir, output_dir.parent]
for search_dir in search_dirs:
    for lang in langs:
        # 出力ディレクトリ配下を再帰的に探索し、言語フォルダをLang配下に移動
        for found in search_dir.rglob(lang):
            if found.is_dir():
                target_lang_dir = lang_dir / lang
                if not target_lang_dir.exists():
                    target_lang_dir.mkdir()
                for file in found.iterdir():
                    if file.is_file():
                        shutil.move(file, target_lang_dir / file.name)
                try:
                    found.rmdir()
                except OSError:
                    pass

# create a zip
# Allow CI to pass an explicit arch (preferred). Otherwise fall back to
# the previous heuristic based on directory layout.
if arch_override:
    arch = arch_override
else:
    try:
        arch = target_dir.parents[1].name
    except Exception:
        arch = 'unknown'

zip_name = f"DS4Windows_{version}_{arch}"
target_zip_path = target_dir.parent / f"{zip_name}.zip"

# Ensure deterministic staging folder (clean previous runs)
staging_dir = target_dir.parent / 'DS4Windows'
if staging_dir.exists():
    shutil.rmtree(staging_dir)

# Determine actual source directory to copy from. Some publish outputs
# produce an inner 'DS4Windows' folder (e.g. Release\DS4Windows). If so,
# copy from that inner folder's contents to avoid creating DS4Windows/DS4Windows.
source_dir = target_dir
try:
    top_dirs = [p for p in target_dir.iterdir() if p.is_dir()]
    if len(top_dirs) == 1 and top_dirs[0].name.lower() == 'ds4windows':
        source_dir = top_dirs[0]
except Exception:
    source_dir = target_dir

staging_dir.mkdir(parents=True, exist_ok=True)
for item in source_dir.iterdir():
    dest = staging_dir / item.name
    if item.is_dir():
        shutil.copytree(item, dest)
    else:
        shutil.copy2(item, dest)

print(f"Post-build: source_dir={source_dir} staging_dir={staging_dir}")

# Remove any net8.0-windows subdirectory from staging to avoid duplication
net_framework_dir = staging_dir / "net8.0-windows"
if net_framework_dir.exists():
    shutil.rmtree(net_framework_dir)

# Remove any pre-existing zip in both candidate locations to avoid duplicates
if target_zip_path.exists():
    os.remove(target_zip_path)
candidate_in_staging = staging_dir / f"{zip_name}.zip"
if candidate_in_staging.exists():
    os.remove(candidate_in_staging)

# Create archive with an explicit full path so no temporary archive is left
# base_name for make_archive should be without the .zip suffix
archive_base = str(target_zip_path.with_suffix(''))
zip_path = shutil.make_archive(archive_base, "zip", root_dir=str(staging_dir.parent), base_dir=staging_dir.name)

# zip_path should equal target_zip_path; report both for diagnostics
print(f"Build output: {staging_dir}")
print(f"Archive created: {zip_path}")