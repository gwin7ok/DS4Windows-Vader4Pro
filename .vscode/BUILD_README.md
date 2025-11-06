# VS Code Build Configuration for DS4Windows

このプロジェクトは、GitHub Actionsと同じ形式でビルドできるよう設定されています。

## ビルドタスク

### 利用可能なタスク

1. **build-release-like-github-x64** (デフォルト)
   - GitHub Actions形式でx64版をビルド
   - `dotnet publish` → `post-build.py` の順で実行
   - 出力: `./bin/x64/Release/DS4Windows/` + ZIP

2. **build-release-like-github-x86**
   - GitHub Actions形式でx86版をビルド
   - 出力: `./bin/x86/Release/DS4Windows/` + ZIP

3. **clean-output**
   - ビルド出力をクリーンアップ

### キーボードショートカット

- `Ctrl+Shift+B`: x64版ビルド (GitHub Actions形式)
- `Ctrl+Shift+Alt+B`: x86版ビルド (GitHub Actions形式)
- `Ctrl+Shift+Alt+C`: ビルド出力クリーンアップ

### ビルド出力構造

GitHub Actions形式のビルド後：

```text
bin/
├── x64/Release/
│   ├── DS4Windows/          # 実行可能ファイルとDLL
│   │   ├── DS4Windows.exe
│   │   ├── *.dll
│   │   ├── Lang/            # 言語ファイル（post-build.pyで移動）
│   │   └── BezierCurveEditor/
│   └── DS4Windows_3.11.2_x64.zip
└── x86/Release/
    └── ...
```

## デバッグ設定

### 利用可能な起動設定

1. **Debug DS4Windows (x64)**
   - 通常のデバッグビルドで起動

2. **Launch DS4Windows (GitHub Actions Build x64)**
   - GitHub Actions形式ビルドで起動

3. **Launch DS4Windows (GitHub Actions Build x86)**
   - GitHub Actions形式ビルド（x86版）で起動

## 使用方法

1. **VS Codeでビルド実行**:
   - `Ctrl+Shift+P` → "Tasks: Run Task" → "build-release-like-github-x64"
   - または `Ctrl+Shift+B` (デフォルトタスク)

2. **コマンドラインでビルド実行**:

   ```bash
   # publish
   dotnet publish ./DS4Windows/DS4WinWPF.csproj -c Release /p:platform=x64 -o ./bin/x64/Release/output
   
   # post-build processing
   python ./utils/post-build.py ./bin/x64/Release/output . 3.11.2
   ```

## 必要要件

- .NET 8.0 SDK
- Python 3.x (post-build.pyスクリプト用)

## 注意事項

- `post-build.py` スクリプトにより、言語ファイルが `Lang/` フォルダに整理されます
- `inject_deps_path.py` により、依存関係パスが調整されます
- 最終的な実行ファイル群は `DS4Windows/` フォルダ内に配置されます
- ZIPアーカイブが自動生成されます