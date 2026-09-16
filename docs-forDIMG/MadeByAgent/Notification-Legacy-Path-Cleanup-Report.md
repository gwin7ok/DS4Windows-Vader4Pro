# 通知レガシー経路クリーンアップ 実施報告書

- **実施日時**: 2026-09-16
- **対象ブランチ**: `For-DI-migration-work`
- **作業概要**: `H.NotifyIcon` の旧通知システム（バルーン・レガシートーストフォールバック）を撤去し、モダン通知（`AppNotificationRegistration.ShowModernToast`）への一本化を実施。あわせて関連する未使用引数のクリーンアップを行いました。

---

## 変更内容詳細

### 1. DS4Windows/DS4Forms/MainWindow.xaml.cs

- `ShowSystemNotification(string message, bool isWarning)` 内の「2. フォールバック（従来の H.NotifyIcon レガシー通知）」ブロックを削除。
- `ShowModernToast` 失敗時は、フォールバック表示を試みず、失敗した旨をログに記録するのみとしました。
- 上記に伴い不要となった `using H.NotifyIcon.Core;` を削除。
- `notifyIcon` フィールド自体（トレイアイコン本体・コンテキストメニュー用途）は削除していません。

### 2. DS4Windows/Actions/IProfileSwitcher.cs

- `ApplyManualProfile` のシグネチャから未使用の `bool showNotification` 引数を削除。

### 3. DS4Windows/Actions/DefaultProfileSwitcher.cs

- `IProfileSwitcher` の変更に合わせて `ApplyManualProfile` の実装を更新（`showNotification` 引数削除）。
- メソッド本体のロジックに変更はありません（元々参照していなかったため）。

### 4. DS4WindowsTests/MockProfileSwitcher.cs

- `IProfileSwitcher` の変更に合わせてモック実装のシグネチャを追随。

---

## 影響範囲の確認（静的確認）

- リポジトリ全文検索により、`showNotification` / `notifyIcon.ShowNotification` / `H.NotifyIcon.Core` への参照が上記4ファイル以外に存在しないことを確認しました。
- `ApplyManualProfile`（`IProfileSwitcher` 経由）は変更前時点で本番コードからの呼び出し箇所が存在しなかったため（テストモックのみ）、シグネチャ変更による既存呼び出し元への影響はありません。

---

## 未実施・要確認事項

- 本変更はコード編集のみであり、`dotnet build` / `dotnet test` は実行していません。ローカル環境で以下を実施し、グリーンであることを確認してください。
  - `dotnet build`
  - `dotnet test`
- 実機でのトースト通知表示確認（プロファイル切替・コントローラー検出・アップデータ完了時の3経路）。
