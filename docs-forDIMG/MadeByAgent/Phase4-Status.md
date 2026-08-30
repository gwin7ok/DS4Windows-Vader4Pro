# フェーズ4 進捗状況（Phase4-Status.md）

作成日: 2026-08-31
最終更新: 2026-08-31（フェーズ4実施前）
対象ブランチ: For-DI-migration-work
参照: `docs-forDIMG/MadeByAgent/Phase4-Plan.md`, `docs-forDIMG/MadeByAgent/Phase3-Status.md`, `docs-forDIMG/DI-App-Wide-Migration-Plan.md`, `.github/copilot-instructions.md`

本書はフェーズ4の進捗管理用文書である。フェーズ4着手前の基準状態として、各Stepを未着手で登録する。以後、各Stepの実施前調査、変更内容、ビルド・自動テスト・実機確認結果、残課題、次Stepへの引継ぎを本書へ追記・更新する。詳細な実装手順と対象一覧は `Phase4-Plan.md` を正本とする。

## 1. ステップ別進捗

| ステップ | 内容 | 状況 | 完了日 | 備考 |
|---|---|---|---|---|
| **Phase4-Step4-0** | **現状棚卸し・基準テスト** | **未着手** | — | `Global`、ViewModel直接生成、DI起動経路、イベント購読、既存ログを調査し、移行前のビルド・テスト結果を基準値として記録する |
| **Phase4-Step4-1** | **`IProfileSettingsService` 実装化** | **未着手** | — | `ProfileSettingsServicePlaceholder`を実設定対応へ置換。既定値、保存・読込、変更通知、配列境界を維持する |
| **Phase4-Step4-2** | **`IProfileRepository` 分離** | **未着手** | — | プロファイル読込・保存・選択・切替を分離。Phase3で残った`ApplyProfileDirect`／`RestoreProfileDirect`の依存を整理する |
| **Phase4-Step4-3** | **`ISpecialActionRepository` 分離** | **未着手** | — | SpecialActionの取得・保存・正規化をデータアクセスとして分離し、ActionManagerの実行責務と分ける |
| **Phase4-Step4-4** | **入力・出力・デバイス状態サービス** | **未着手** | — | `IInputBehaviorSettingsService`、`IOutputHandlerSettingsService`、`IDeviceConnectionTracker`を導入する |
| **Phase4-Step4-5** | **環境・UI・通知サービス** | **未着手** | — | 環境情報、アプリパス、外観・UIレイアウト、通知・キャッシュの責務を必要最小限移行する |
| **Phase4-Step4-6** | **Composition Root 一本化** | **未着手** | — | `AppHost`／`ServiceRegistration`を正式な本番解決経路とし、`App.xaml.cs`の簡易DIと`ServiceProviderHolder`依存を整理する |
| **Phase4-Step4-7** | **ViewModel パターンA移行** | **未着手** | — | 引数なしViewModelをDI登録・解決へ移行する |
| **Phase4-Step4-8** | **ViewModel パターンB移行** | **未着手** | — | 共有依存をコンストラクタ注入し、DataContext、ライフサイクル、イベント解除を維持する |
| **Phase4-Step4-9** | **ViewModel パターンC Factory化** | **未着手** | — | 実行時引数付きViewModelを`IXxxViewModelFactory.Create(...)`経由へ移行する |
| **Phase4-Step4-10** | **Phase3引継ぎ再確認・シム整理** | **未着手** | — | 実機未対応項目とDI経路を再確認し、呼び出し元ゼロのシムだけを根拠付きで削除する |

全体進捗: 11ステップ中0ステップ完了。**フェーズ4は実施前であり、まだ完了扱いとしない。**

## 2. 着手前の基準状態

- Phase3の実装、自動テスト、実機確認結果の記録は完了している。
- Phase3の実機確認で`△`、`×`、未実施となった項目は、Phase4のDI経路確立後に再確認する未対応事項として引き継ぐ。
- `Global`は`DS4Windows/DS4Control/ScpUtil.cs`内にあり、プロファイル、入力・出力、デバイス、環境、UI、通知などの責務が混在している。
- `AppHost`／`ServiceRegistration`と、`App.xaml.cs`／`ServiceProviderHolder`の二重のDI起動・解決経路が残っている。
- `DS4Windows/DS4Forms`では、ViewModelの直接生成、共有依存、実行時引数付き生成が混在している。
- `IProfileSettingsService`はPlaceholder登録の状態であり、実設定を扱うサービスへの移行が必要である。

## 3. 4層モデルとの整合確認

Phase4は、全体計画書で定義した4層モデルのうち、第4層（UI層）と、UI層から実行時3層へ設定・状態を渡すサービス境界を主対象とする。

- 入力監視層、信号変換層、信号・アクション実行層の実行責務はPhase4のUI・設定サービスへ移さない。
- 信号変換層が作成する実行指示は、信号・アクション実行層で3-a（仮想コントローラー）、3-b（KBM）、3-c（アプリ内アクション）へ振り分ける構造を維持する。
- SpecialActionリポジトリは定義の取得・保存を担当し、信号変換層が実行指示を生成し、ActionManager／実行層が振り分け・実行を担当する。
- UI／設定サービスは、3-a／3-b／3-cを直接実行せず、設定・プロファイル・状態をサービス経由で実行時3層へ反映する。
- 混在マクロの順序・遅延・キャンセルを含む実行責務は実行層に残し、Phase4では依存関係のDI化によってこの境界を壊さない。

確認状況: **着手前（未確認）**。Step 4-0の実コード棚卸しで、UI／ViewModel／設定サービスから実行層への直接呼び出しがないか確認し、各Step完了時に再確認する。

## 4. Phase3からの引継ぎ事項

| 引継ぎ事項 | Phase4での扱い | 状況 |
|---|---|---|
| `Mapping.cs`の`ApplyProfileDirect`／`RestoreProfileDirect`に残る`Program.rootHub`依存 | `IProfileRepository`の責務境界を設計し、直接依存を解消または理由付きで整理する | 未着手 |
| `LaunchProgram`の外部プログラム起動・多重起動防止経路 | `IProcessInspector`等のDI経路を再確認し、実機結果と合わせて切り分ける | 未着手 |
| Bluetooth切断後の再接続 | Composition Root・デバイス状態サービス移行後に再確認する | 未着手 |
| 非管理者起動時のUAC、UAC承認／拒否 | 昇格サービスと呼び出し元の境界を維持して再確認する | 未着手 |
| ラムブル動作、`IDeviceStateAccessor`経路 | DI化後の状態伝達と実機動作を確認する | 未着手 |
| `IDs4DeviceRegistry.ReEnableDevice`と`IElevatedProcessLauncher`の境界 | Phase3で確定した責務を維持し、不要な再統合を行わない | 確認待ち |

これらはPhase3の完了を取り消すものではなく、Phase4のDI化・実機再確認後に解消または未対応理由を記録する項目である。

## 5. 進捗更新時の確認事項

- [ ] 対象Stepの実コード、依存関係、呼び出し元を調査した
- [ ] `Phase4-Plan.md`の対象範囲・完了基準と突合した
- [ ] 既存機能、条件分岐、初期化順序、配列インデックス、ログを維持した
- [ ] 新DI経路のビルドを確認した
- [ ] Phase2／Phase3を含む自動テストを実行し、結果を記録した
- [ ] 必要な主要画面起動・操作確認または実機確認を実施した
- [ ] 旧シムを削除した場合、削除根拠と呼び出し元ゼロを確認した
- [ ] 残課題と次Stepへの引継ぎを記録した
- [ ] UI／設定サービスが3-a／3-b／3-cの実行責務を直接持たないことを確認した
- [ ] SpecialAction／混在マクロの実行指示が実行層へ渡る境界を維持した

## 6. Phase4完了判定

現時点では完了条件を満たしていない。以下をすべて確認した時点で、Phase4を完了扱いとする。

- `Global`の主要責務がサービスへ分割され、`ServiceRegistration`から解決できる。
- `IProfileSettingsService`のPlaceholderと本番の二重Composition Rootが解消される。
- ViewModelがDIまたはFactory経由で生成され、画面ライフサイクル・DataContext・イベント解除が維持される。
- Phase3からの引継ぎ項目が解消済み、または再現条件・理由・次の対応先とともに文書化される。
- Phase2／Phase3を含む全自動テスト、ビルド、主要画面の起動・操作確認が成功する。
- 新経路の動作確認前に旧シムを削除しておらず、削除したシムには根拠がある。
- UI／設定サービスが実行時3層の実行責務を直接持たず、SpecialAction／混在マクロの実行指示が3-a／3-b／3-cへ渡る構造が維持されている。

## 7. 次に着手するStep

次は **Phase4-Step4-0（現状棚卸し・基準テスト）** に着手する。`Phase4-Plan.md` §3の調査項目に従い、`Global`メンバー、全呼び出し元、ViewModel直接生成箇所、DI起動経路、移行前のビルド・テスト結果を確定して本書へ記録する。
