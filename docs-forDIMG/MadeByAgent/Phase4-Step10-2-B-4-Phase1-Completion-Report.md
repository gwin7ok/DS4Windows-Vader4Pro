# フェーズ4-Step10-2-B-4 第1段階完了報告書: Mapping の DI 設定境界

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`Mapping.cs` の static 構造を維持したまま、`IProfileSettingsService` を初回静的初期化時に一度だけ解決する設定境界を追加した。

移行対象:

- デバウンス時間の入力処理参照
- プロファイル変更通知設定の参照

入力ループ内で `AppHost.GetService` を毎回呼び出さず、既存の `Global` フォールバックも維持した。Mapping 全体の instance 化、定数、純粋関数、出力ハンドラは変更していない。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- 対象 `Global.DebouncingMs`／`Global.ProfileChangedNotification` 参照: なし
- `IProfileSettingsService` の解決箇所: 1 箇所（静的キャッシュ）
- ユーザー側テストビルド・テスト実行: 未実施
- ユーザー側コミット・リモート反映: 未実施

## 3. 次の作業

ユーザー検証後、Mapping のスティック、トリガー、ジャイロ設定参照を同じキャッシュ境界へ段階的に移行する。
