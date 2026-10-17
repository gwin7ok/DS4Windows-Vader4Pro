# フェーズ4-Step10-2-A-9 完了報告書: シム接続拡張（Mapping.cs専用）

作成日: 2026-09-02
対象ブランチ: `For-DI-migration-work`
関連計画書: `docs-forDIMG/MadeByAgent/Phase4-Step10-2-Plan.md`（Stage1 / Step10-2-A / サブタスク9）

## 1. 実施内容

Stage1（Step10-2-A: シム接続拡張）のサブタスク9「Mapping.cs専用」を実施した。対象はMapping.csおよび関連呼び出し元が利用する残存Globalメンバーである。

対象メンバー:

- `ProfileChangedNotification`
- `DebouncingMs`
- `DebouncingMsChanged`
- `outputKBMMapping`
- `getMainColor`

## 2. 変更内容

- `DS4Windows/DI/IProfileSettingsService.cs`: A-9対象設定、通知イベント、ランタイムKBMマッピングの契約を追加。
- `DS4Windows/DS4Control/Services/ProfileSettingsService.cs`:
  - A-1〜A-8で使用している`BackingStore _config`を継続利用。
  - `ProfileChangedNotification`と`DebouncingMs`を`BackingStore`へ委譲。
  - `DebouncingMsChanged`のサービスイベントと通知メソッドを追加。
  - `VirtualKBMMapping`をサービスのランタイムプロパティとして保持。
- `DS4Windows/DS4Control/ScpUtil.cs`:
  - 対象のGlobal公開メンバーを`ProfileSettingsServiceInstance`経由の後方互換シムへ変更。
  - 既存のGlobalイベント通知、KBMマッピング初期化、`ref`色取得APIを維持。
  - Mapping.csの既存呼び出し形状とプロファイル設定のXML読込・保存処理は維持。

## 3. 外部呼び出し元への影響

`Global`の既存API形状（設定プロパティ、配列アクセス、イベント、ランタイムマッピング、`ref`色取得）は維持しているため、既存呼び出し元との互換性を維持している。Stage1では呼び出し元のDI直接参照化は行わず、Stage2で別途実施する。

## 4. 検証状況

- デバッグビルド: 成功（警告0、エラー0）。
- テストビルド: 成功（Actions、Standalone）。
- テスト実行: 成功（Actions、Standalone、全件成功）。
- 変更コミット: 完了。
- リモートリポジトリへの反映: 完了。
- A-9単独の実機検証は実施していない。計画どおり、Stage1（Step10-2-A、9サブタスク）完了後にまとめて実施する。

## 5. Stage1完了

Step10-2-Aの全9サブタスクが完了した。実機検証は計画どおり、Stage1完了後にまとめて実施する。

| サブタスク | 内容 | 状態 |
|---|---|---|
| Step10-2-A-1 | スティック関連 | 完了 |
| Step10-2-A-2 | トリガー(L2/R2)関連 | 完了 |
| Step10-2-A-3 | タッチパッド関連 | 完了 |
| Step10-2-A-4 | ジャイロ関連 | 完了 |
| Step10-2-A-5 | ライトバー・ランブル関連 | 完了 |
| Step10-2-A-6 | ボタン/マウス出力関連 | 完了 |
| Step10-2-A-7 | SA(ステアリングホイール)・デッドゾーン関連 | 完了 |
| Step10-2-A-8 | 残余（デバイスオプション・雑多フラグ） | 完了 |
| Step10-2-A-9 | Mapping.cs専用 | **完了（本報告書）** |
