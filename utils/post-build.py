import os
from pathlib import Path
import sys
import shutil

target_dir = Path(sys.argv[1])
project_dir = Path(sys.argv[2])
version = sys.argv[3]

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
arch = target_dir.parents[1].name
zip_name = f"DS4Windows_{version}_{arch}"
target_zip_path = target_dir.parent / f"{zip_name}.zip"
if target_zip_path.exists():
    os.remove(target_zip_path)

zip_dir = shutil.make_archive(zip_name, "zip", target_dir.parent)

# move the zip to the build directory
shutil.move(zip_dir, target_zip_path)