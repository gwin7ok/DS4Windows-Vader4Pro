# Phase6-Step10b 分割ツール（作業用）

- 目的: 複数のトップレベル型を持つ .cs を、型の宣言を一字一句そのまま移して 1 ファイル 1 型へ分割する。
- 実行環境: PowerShell 7（同梱の Roslyn を使う。dotnet のビルドは不要）。.NET 9 の参照パック（C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref）が必要。
- 手順
  1. `TypeSplitter.cs.txt` を `TypeSplitter.cs` として作業ディレクトリへ置き、`build-splitter.ps1` で `TypeSplitterAsm.dll` を作る（スクリプトと同じフォルダに出力）。
  2. `run-split.ps1 -ListFile list-W1.txt`（ドライラン）で計画を確認する。
  3. `run-split.ps1 -ListFile list-W1.txt -Apply` で実行。各ファイルの再読み込み検証（1 ファイル 1 型、宣言テキストの一致、構文エラー 0）が VERIFY-OK になること。
  4. `scan-types.ps1` の前後比較で、型の一覧（名前）が一致することを確認する。
- 制限: 名前空間が複数のファイル（`DI/AppHost.cs`）は ISSUE として報告される。個別に対応する。
- 一覧の `list-W*.txt` は 2026-10-15 時点（W1 は実行済み）。
