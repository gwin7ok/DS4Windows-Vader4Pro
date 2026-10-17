# C5-2 実装記録: specActionLaunchProc の LaunchProcessAction 経由への置換

作成: Claude（外部レビュー依頼への対応として実装）
参照元: docs-forDIMG/MadeByAgent/LaunchProcessAction-Design.md, C5-LaunchProcessAction-Tests.md,
Phase1-Status.md, .github/copilot-instructions.md

## 実施日
2026-08-26

## 背景・実施理由

`Phase1-Status.md` で「C5-2 作業中（specActionLaunchProc ブロックが長大で、ピンポイント置換のための
正確な範囲特定に時間がかかっている）」と報告されていた作業を引き継いで実施した。

## 調査で判明した事実（着手前の重要な発見）

1. **`DefaultActionFactory.CreateFrom`（実際にDI登録されているファクトリ）に `Program` 型のマッピングが
   存在しなかった**。`switch (sa.typeID)` は `Key` のみを処理し、`Program` は `default: return null` で
   `TODO: add Macro, Profile, Program, Disconnect etc.` というコメントのまま放置されていた。
   → これが `LaunchProcessAction` が実質的に「作られたが使われていない」状態だった直接の原因。

2. **既存の `LaunchProcessAction.Execute()` は機能が大幅に不足していた**。`sa.details` のみを見て
   `Process.Start(details)` するだけであり、`sa.extra`（コマンドライン引数、`$hidden` 修飾子）を
   一切参照していなかった。元の `Mapping.cs` L5508-5569 の `Program` ブロックは実際には
   **3つの異なる起動経路**を持つ：
   - `$hidden` 修飾子あり: `.bat`/`.cmd` なら COMSPEC 経由でラップして起動（ウィンドウ非表示）
   - `$hidden` 修飾子なしで `extra` あり: `extra` をそのまま `Arguments` に設定して起動
   - `extra` が空: 引数なしで `details` のみを起動
   これを反映しないまま接続すると `§2.2 No Feature Drop`（機能の完全維持）に違反する。

3. **`DispatchOrSetBeingTriggered`（L5515で使用）は戻り値がない `void` メソッド**であり、
   `ActionManager` 経由での処理が成功したかどうか（`handled`）を呼び出し元が知る手段がなかった。
   このまま `Program` 型を `ActionFactory` に配線すると、新経路（`LaunchProcessAction`）と
   旧経路（直下の直接 `Process.Start` 呼び出し）の**両方が無条件に実行され、プロセスが二重起動する**
   重大なバグを生む状態だった。

## 実施した修正

### 1. `DS4Windows/Actions/IProcessLauncher.cs`
引数・ウィンドウ非表示オプションに対応したオーバーロード `Launch(string fileName, string arguments,
bool useShellExecute, bool hidden)` を追加。既存の `Launch(string filePath)`（単純起動、テスト仕様 T6 が
参照）は後方互換のためそのまま維持。

### 2. `DS4Windows/Actions/LaunchProcessAction.cs`
`Execute()` を全面的に書き換え、元の `specActionLaunchProc` ブロックが持つ3つの起動経路をすべて
再現するようにした。`sa.details` に加えて `sa.extra` を正しく参照し、`$hidden` 修飾子の検出・
`.bat`/`.cmd` 判定・COMSPEC ラップ・`WindowStyle`/`CreateNoWindow` の設定を完全に踏襲。
DI経由の `IProcessLauncher` を優先し、解決できない場合は元と等価な `Process.Start` 呼び出しに
フォールバックする（§2.1修正版準拠）。ログ出力（`AppLogger.LogTrace`/`LogDebug`）は維持。

### 3. `DS4Windows/Actions/LaunchProcessActionAdapter.cs`（新規作成）
`KeyActionAdapter` と同一パターンで、`IOutputAction`（`LaunchProcessAction`）を
`SpecialActionBase`（`OnTrigger`/`OnRelease` 契約）へ橋渡しするアダプタ。`ActionFactory.CreateFrom` の
戻り値として使えるようにするために必要だった（既存の `LaunchProcessAction` 単体では
`ActionEntry.ActionImpl` に代入できない型だったため）。

`OnRelease` は元コード同様ログのみ（no-op）。元の `Mapping.cs` にも `Program` 型に対する
release/untrigger 処理が存在しないことを確認済み（§2.2 準拠、新規機能を追加しない）。

### 4. `DS4Windows/DS4Control/DefaultActionFactory.cs` / `ActionFactory.cs`
`switch (sa.typeID)` に `case SpecialAction.ActionTypeId.Program: return new LaunchProcessActionAdapter(sa, index);`
を追加（両ファイルとも、DI経由・非DI経由フォールバックの一貫性のため）。

### 5. `DS4Windows/DS4Control/Mapping.cs`（L5508-5569、ピンポイント置換）
`DispatchOrSetBeingTriggered(action, device, true);` の呼び出しを、同等の内部処理
（`TriggerContext` 構築 → `DispatchInputEdge(ctx)` → 失敗時 `SetBeingTriggeredIf`）を
インラインで展開した形に置換し、`handled` の値を捕捉できるようにした。
既存の直接 `Process.Start` 呼び出しブロック（3経路すべて）は**削除せず**、
`if (!handled) { ... }` でラップしてフォールバックとして保持（§2.1修正版・§2.2準拠）。

## 制約遵守の確認

- [x] §2.1修正版: 古い方式（直接 `Process.Start`）を削除せず、`!handled` 時のフォールバックとして保持
- [x] §2.1修正版: 複数候補手段を同時実装しない（`LaunchProcessAction` 経由の単一路線、フォールバックのみ許容）
- [x] §2.2 No Feature Drop: 3つの起動経路すべて（`$hidden`+`.bat/.cmd`、引数あり、引数なし）を完全再現
- [x] §2.3 ログ維持: `AppLogger.LogTrace`/`LogDebug` を削除せず、`LaunchProcessAction` 内で維持
- [x] §3.1 DIの実装: コンストラクタインジェクション（`LaunchProcessAction(SpecialAction sa)`）、
      `IProcessLauncher` は現時点では `ServiceProviderHolder.Provider` 経由で解決（既存設計を踏襲）
- [x] §3.2 巨大ファイルの編集方針: `Mapping.cs` は該当ブロックのみをピンポイント置換（Python の
      文字列一致による厳密な置換で実施、他の8,800行超には一切触れていない）
- [x] §4.4 調査結果の文書化: 本ファイルに記録

## 未実施・今後の課題

1. **ビルド未検証**: 作業環境に `dotnet` SDK が無く（ネットワーク制限）、`dotnet build` による
   実際のコンパイル確認ができていない。中括弧バランスの機械的検証は実施済み（全ファイルで一致）だが、
   実機ビルドでの最終確認が必要。
2. **`IProcessLauncher` はまだDI未登録**（設計書の方針通り、正式登録はフェーズ5で実施予定）。
   現時点では `Launch` 呼び出し試行は常に失敗し、フォールバック（直接 `Process.Start`）が実質的に
   常時使われる。この状態は意図通り（既存設計書の記述と整合）。
3. **単体テスト（T1〜T6）は未追加**。`C5-LaunchProcessAction-Tests.md` のテスト項目を
   `DS4WindowsTests` に実装する作業が残っている。特に T2/T3（DI経由 vs フォールバックの分岐）は
   `IProcessLauncher` のモックを用意すれば検証可能。
4. **分類②（権限昇格）・⑥（多重起動チェック）は本作業のスコープ外**のまま
   （既存設計書通り、フェーズ3で扱う予定）。

## 次の推奨アクション

1. 実際の開発環境で `dotnet build` を実行し、コンパイルエラーがないことを確認する。
2. `DS4WindowsTests` に `MockProcessLauncher` を追加し、T1〜T6 の単体テストを実装する。
3. 実機で `$hidden` 修飾子あり/なし、`.bat`/`.cmd`/通常exe の3パターンについて、
   `specActionLaunchProc` を含むプロファイルで動作確認を行う（T10相当）。
4. `Phase1-Status.md` の C5 セクションを「完了」に更新する。
