# タスク指示書: C5 LaunchProcessAction 単体テスト実装 (T1〜T6)

## 1. 概要と目的
本タスクの目的は、`DS4WindowsTests`（`DS4Windows.Actions.Tests.csproj`）内に `LaunchProcessAction` の単体テストクラスを作成し、仕様書（`docs-forDIMG/MadeByAgent/C5-LaunchProcessAction-Tests.md`）に定義されたテストケース（T1〜T6）を実装して、すべてのテストを通過（Pass）させることです。

## 2. 前提条件と現在の状態
* **`MockProcessLauncher.cs` は実装・ビルド確認済み**:
  * `DS4WindowsTests/MockProcessLauncher.cs` に `IProcessLauncher` のモック（1引数版および4引数版 `void Launch(...)`）が実装済み。
  * `dotnet build ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj` のビルドが通る状態。
* **テスト対象クラス**:
  * `DS4Windows/Actions/LaunchProcessAction.cs`
  * `DS4Windows/Actions/IProcessLauncher.cs`

## 3. 参照ドキュメント・ファイル
1. **テスト仕様書**:
   * `docs-forDIMG/MadeByAgent/C5-LaunchProcessAction-Tests.md` (T1〜T6 の各シナリオ詳細)
2. **実装対象コード**:
   * `DS4Windows/Actions/LaunchProcessAction.cs`
   * `DS4WindowsTests/MockProcessLauncher.cs`
3. **プロジェクト構成**:
   * `DS4WindowsTests/DS4Windows.Actions.Tests.csproj`

## 4. 作業手順

### ステップ1: テストプロジェクトの仕様確認
1. `DS4WindowsTests/DS4Windows.Actions.Tests.csproj` を確認し、採用されているテストフレームワーク（xUnit, NUnit, MSTest など）を特定する。
2. `DS4Windows/Actions/LaunchProcessAction.cs` のコンストラクタ引数および実行メソッドのシグネチャを確認する。

### ステップ2: テストコードの作成
`DS4WindowsTests/` 配下にテストクラスファイル（例: `LaunchProcessActionTests.cs`）を作成し、以下のテストシナリオ T1〜T6 を実装する。

* **T1: 単純な実行可能ファイルの起動**
  * 引数なし・通常のパス（例: `notepad.exe`）が指定された場合、モックの `Launch` が正しく呼び出されること。
* **T2: 引数付き実行ファイルの起動**
  * パスと引数が分離され、4引数版 `Launch` に正しく渡されること。
* **T3: `$hidden` 指定時の非表示起動**
  * パスまたは引数に `$hidden` プレースホルダーが含まれている場合、`hidden = true` フラグが立ち、文字列からは `$hidden` が除去されて `Launch` に渡されること。
* **T4: バッチファイル（`.bat` / `.cmd`）の起動**
  * 拡張子が `.bat` や `.cmd` の場合、`useShellExecute = true`（または cmd.exe 経由等の所定のフラグ）が設定されて `Launch` に渡されること。
* **T5: 不正・無効なパス指定時のハンドリング**
  * 空文字や不正なパスが渡された場合、クラッシュせずに安全に処理（または無視）されること。
* **T6: 複数回実行・状態リセットの検証**
  * 連続でアクションがトリガーされた場合の呼び出し回数・履歴の検証。

### ステップ3: ビルドとテストの実行
以下のコマンドを実行し、コンパイルエラーがなく、すべてのテストケースがグリーン（成功）になることを確認する。

```bash
dotnet test ./DS4WindowsTests/DS4Windows.Actions.Tests.csproj