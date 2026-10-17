# フェーズ4-Step10-2-B-1-9 完了報告書: ProfileSettingsViewModel Mapping 関連設定の DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ProfileSettingsViewModel` の Mapping 関連設定を、`Global` シム経由から注入済み `IProfileSettingsService` の直接参照へ移行した。

対象:

- デバウンス時間
- SA ジャイロマウスのトリガー一覧
- SA ジャイロスティックのトリガー一覧

既存のトリガー文字列形式、Always On 処理、メニュー表示更新、デバウンス変更通知を維持した。`Mapping.cs` の入力ループや出力処理は計画どおり B-1 の対象外とし、後続の B-4 で扱う。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- ユーザー側テストビルド: 成功（Actions、Standalone）
- ユーザー側テスト実行: 成功（Actions、Standalone、全件成功）
- ユーザー側コミット・リモート反映: 完了
- B-1-9 対象の直接 `Global` 参照: なし

## 3. Stage2-B-1 完了

B-1-1 から B-1-9 までの `ProfileSettingsViewModel` 呼び出し元 DI 直接参照化が完了した。次は Stage2-B-2 のプロファイル操作 DI 接続へ進む。
