# フェーズ4-Step10-2-B-3 完了報告書: ControlService の DI 直接参照化

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-B-Plan.md`

## 1. 実施内容

`ControlService` に `IProfileSettingsService` をコンストラクタ注入し、対象設定の `Global` 直接参照を DI サービス参照へ移行した。

対象:

- プロファイル変更通知設定
- L2/R2 トリガー出力の最大値計算
- L2/R2 トリガー設定の変更イベント登録・リセット

App の `ControlService` 生成時には `AppHost` 登録済みの設定サービスを渡し、既存のコンストラクタ利用互換性のためオプションフォールバックも維持した。入力処理、プロファイル適用、デバイス状態処理は変更していない。

## 2. 検証状況

- Debug x64 ビルド: 成功（警告 0、エラー 0）
- 対象 `ControlService` の直接 `Global` 参照: なし
- ユーザー側テストビルド・テスト実行: 未実施
- ユーザー側コミット・リモート反映: 未実施

## 3. 次の作業

B-3 のテストビルド・テスト実行後にコミット・pushを行い、次に Stage2-B-4 の `Mapping.cs` DI 境界整理へ進む。
