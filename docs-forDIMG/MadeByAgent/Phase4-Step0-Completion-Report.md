# Phase4-Step0 完了報告

## 実施結果

Phase4-Step0「現状棚卸し・基準テスト」を実施した。Step0はコード変更を行わず、調査結果と基準値の文書化のみを行った。

| タスク | 結果 | 成果物 |
|---|---|---|
| Step0-1 `Global`メンバー抽出・分類 | 完了 | `Phase4-Step0-Global-Member-Inventory.md` |
| Step0-2 `Global`呼び出し元一覧 | 完了 | `Phase4-Step0-Global-Member-Inventory.md` |
| Step0-3 ViewModel直接生成分類 | 完了 | `Phase4-Step0-ViewModel-Inventory.md` |
| Step0-4 DI起動順序整理 | 完了 | `Phase4-Step0-DI-Startup-Sequence.md` |
| Step0-5 基準ビルド・テスト | 完了 | `Phase4-Step0-Baseline-Test-Report.md` |
| Step0-6 進捗更新・完了報告 | 完了 | 本書、`Phase4-Status.md` |

## 確定した実測値

- `Global`: `ScpUtil.cs`内の`public class Global`範囲にある`public static`宣言は442件。
- 計画書の469件との差分は-27件。469件は`ScpUtil.cs`全体の宣言数であり、Globalクラス外の27件を含む値だった。
- `Global.`呼び出し元: `ScpUtil.cs`自身を除く`DS4Windows`内で80ファイル。暫定値75ファイルより5ファイル多い。
- ViewModel直接生成: 16ファイル、実行される生成29件、コメントアウト1件。
- ViewModel分類: A（引数なし）11件、B（共有依存）8件、C（実行時引数）10件。

## 基準結果

- `dotnet build DS4WindowsWPF.sln --nologo`: 成功、警告0、エラー0。
- `dotnet test DS4WindowsTests/DS4Windows.Actions.Tests.csproj --nologo --no-restore`: 31/31成功。
- `dotnet test StandaloneTests/StandaloneTests.csproj --nologo --no-restore`: 13/13成功。
- MainWindow、ProfileEditor、Controller関連タブ、UAC、実機操作: 本環境では未実施。確認手順と未実施理由をBaselineレポートへ記録した。

## DI上の主要な引継ぎ事項

1. `App.xaml.cs`はActions系の簡易`ServiceProviderHolder`を構築した後、別の`AppHost`を構築している。
2. `ControlService`は`AppHost`から解決せず、`CreateControlService`で手動生成して`App.rootHub`／`Program.rootHub`へ保持している。
3. `ServiceRegistration.AddAppServices`を正式登録先とし、Step6で二重Composition Rootを整理する。
4. `IDeviceStateAccessor`の`Program.rootHub`ファクトリ委譲は、null時の挙動を含めて維持・検証する。

## 残課題・次Step

- Global分類は宣言名と周辺責務に基づく一次分類であり、「その他フラグ・状態」はStep1以降のサービス境界設計時に個別確認する。
- 実機・GUI確認はローカル環境で実施し、Phase3引継ぎ項目と併せて記録する。
- 次は`Phase4-Step1`（`IProfileSettingsService`実装化）へ進む。
