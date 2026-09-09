# フェーズ6計画書: 残存 `Global` 実利用箇所の解体と4層構造DI化の完成

作成日: 2026-09-09
対象ブランチ: `For-DI-migration-work`
前フェーズ: `Phase5-Plan.md`（Step1〜15、ドメイン集約型）
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`
参照調査: `Phase5-Step13+14-Addendum-Findings-Report-Part1.md`／`-Part2.md`（`Global.*`残存件数の実地調査）

---

## 0. 目的とスコープ

### 0.1 目的

**完全にDI化された4層構造（入力監視層／信号変換層／信号出力層／UI層）のアプリへ移行し、その結果として新機能追加時の実装容易性を恒常的に向上させること。**

Phase1〜5により、`Global`静的クラスの主要機能はすでに10種以上のDIサービスへ再編され、各サービス内部のLegacy委譲（`Global`／`Program.rootHub`への再委譲）もPhase5で監査・是正済みである。しかし、**呼び出し元側**（`ControlService.cs`、`Mapping.cs`、UIビュー群等）には、DIサービスが存在するにもかかわらず旧来通り`Global.X`を直接呼び続けている箇所が実地調査の結果**193件**確認されている。Phase6は、この呼び出し元側の残存直接参照を解消し、既存の全レイヤーがDIサービス経由で一貫して動作する状態を完成させる。

### 0.2 スコープに含むもの

`git clone`による実地調査（2026-09-09）で判明した、`Global.`修飾で参照されている193件の実利用箇所のうち、以下を対象とする。

| 参照元ファイル | 対象件数 | 層 |
|---|---|---|
| `ControlService.cs` | 39件 | 信号変換層／入力監視層の境界（最優先） |
| `Mouse.cs` | 27件 | 信号出力層 |
| `SettingsViewModel.cs` | 23件 | UI層 |
| `App.xaml.cs` | 22件 | 起動シーケンス |
| `ProfileEditor.xaml.cs` | 18件 | UI層（Phase5-Step15の対象漏れ箇所） |
| `MouseCursor.cs` | 14件 | 信号出力層 |
| `Mapping.cs` | 12件 | 信号変換層（既存方針の範囲内で再点検） |
| `PresetOption.cs` | 10件 | UI補助 |
| `WelcomeDialog.xaml.cs` | 9件 | UI層 |
| `MainWindowsViewModel.cs` | 8件 | UI層 |
| `SaveWhere.xaml.cs` / `BindingWindow.xaml.cs` / `TrayIconViewModel.cs` | 各5件 | UI層 |
| `ProfileSettingsViewModel.cs` | 4件 | UI層 |

（`ProfileRepository.cs`等、DIサービス自身の内部実装からの参照16件は、Global＝実データ源であるStrangler Fig委譲として意図的なものであり対象外。詳細はStep1監査で最終確定する。）

### 0.3 スコープに含まないもの（明示的除外）

1. **仮想コントローラー出力バックエンド（ViGEm）の抽象化**: `ControlService.cs`内の`as Xbox360OutDevice`/`as DS4OutDevice`ダウンキャストによるフィードバック配線、および`Nefarius.ViGEm.Client`への直接依存は、`Global`とは無関係の別種の技術的負債であり、Phase6の対象外とする。将来必要になった場合は別イニシアチブ（`IVirtualControllerBackend`抽象化）として扱う。
2. **`Mapping.cs`の完全instance化**: 全体移行計画書§5.5の既存方針（「本プランでは見送る」）を継承する。Phase6では`Mapping.cs`の残存12件についても、既存の`IDeviceStateAccessor`等の委譲パターンの範囲内でのみ対応し、`Mapping`自体のDIコンテナ管理下への完全移行は行わない。
3. **テストファイル内の`Global.*`参照**（`ProfileSettingsServiceTests.cs`, `AppSettingsServiceTests.cs`, `ProfileRepositoryTests.cs`等、計28件相当）: これらは新旧比較による孤立プロパティバグの再発防止用の意図的な参照であり、移行対象としない。
4. **真の定数**（`Global.TEST_PROFILE_INDEX`, `Global.RESOURCES_PREFIX`等）: DI化不要（元プラン§4.1）。

---

## 1. 実施ステップ一覧（全10ステップ、ドメイン集約型）

```
【Phase6 全10ステップ構成】
Phase6-Step1: 詳細監査と対象確定【最優先】

── [ドメイン1: コア層（信号変換・入力監視境界）] ──
├─ Phase6-Step2: ControlService.cs のGlobal直参照解消（39件、最難関）
├─ Phase6-Step3: Mapping.cs 残存参照の再点検（12件、既存方針踏襲）

── [ドメイン2: 信号出力層] ──
├─ Phase6-Step4: Mouse.cs / MouseCursor.cs のGlobal直参照解消（41件）

── [ドメイン3: 起動・UI層] ──
├─ Phase6-Step5: App.xaml.cs 起動シーケンスの整理（22件）
├─ Phase6-Step6: ProfileEditor.xaml.cs のGlobal直参照解消（18件）
├─ Phase6-Step7: SettingsViewModel他 主要ViewModel群の解消（39件）
├─ Phase6-Step8: 残りの小型UIファイル群の解消（29件）

── [ドメイン4: 検証・仕上げ] ──
├─ Phase6-Step9: 自動テスト・実機検証
└─ Phase6-Step10: 呼出元0件シムの物理削除判断
```

---

## 2. 各ステップの詳細内容

### Phase6-Step1: 詳細監査と対象確定
- **内容**: 193件全件について、(a) 既存DIサービスに同名・同等メソッドが既にある「単純リダイレクト」、(b) 既存サービスへの軽微な拡張が必要な「中程度作業」、(c) 新規インターフェースが必要な「要設計」の3分類を確定する。Phase5-Step0/Step1と同様、`git clone`による全文grep突合をベースとし、「呼出元候補」の記述ではなく実地確認を必須とする（Phase5-Watchpoints報告書で判明した「grep未確認の記述はStep15計画の前提を誤らせる」という教訓を反映）。
- **成果物**: `Phase6-Step1-Global-Usage-Classification-Report.md`（193件の全件分類表）。

### 【ドメイン1: コア層】

#### Phase6-Step2: `ControlService.cs` のGlobal直参照解消（最優先・最難関）
- **対象**: 39件。多くは`IProfileSettingsService`（プロファイル設定値）、`IDeviceStateService`（デバイス状態）等、Phase4で既に用意されているサービスに同等メソッドが存在する見込みが高い（Step1監査で確定）。
- **【アーキテクチャ・ガードレール注記】**:
  - **[ホットパス性能維持]（新規ガードレール、§3.1参照）**: `ControlService.cs`内のGlobal参照の一部は、毎秒250〜1000回実行される入力ポーリングループから直接呼ばれる（例: `getLSDeadzone(index)`相当のデッドゾーン取得）。単純な配列アクセスをDIサービス経由の仮想メソッド呼び出しに置き換える際、キャッシュ無し・都度XML参照等のオーバーヘッドが混入しないよう、置換前後でのマイクロベンチマーク比較を必須とする。
  - **[Halt保証]（§5.2継承）**: 設定変更を伴う一部メンバ（`InitOutputKBMHandler`関連等）は、既存のHalt機構との整合を崩さないことを確認する。
- **完了判定基準**: `ControlService.cs`内の`Global.`直接参照が0件になり、かつ実機での連打・ホールド・同時押しの入力遅延が置換前と同等であることを確認する。

#### Phase6-Step3: `Mapping.cs` 残存参照の再点検
- **対象**: 12件。全体移行計画書§5.5の方針（`Mapping`完全instance化の見送り）を継続するため、既存の`IDeviceStateAccessor`等と同様の「メソッド引数として注入済みインターフェースを受け取る」形の部分的解消にとどめる。
- **完了判定基準**: 是正可能な範囲（フォールバックパターンでの引数渡し化等）を洗い出し、対応した件数と、意図的に残す件数（`Mapping`のstatic性質上不可避なもの）を明確に区分して記録する。

### 【ドメイン2: 信号出力層】

#### Phase6-Step4: `Mouse.cs` / `MouseCursor.cs` のGlobal直参照解消
- **対象**: 41件（27+14）。マウス出力設定（感度、加速度カーブ等）が中心と推測され、`IProfileSettingsService`または`IOutputHandlerSettingsService`相当への単純リダイレクトが主体になる見込み。
- **完了判定基準**: マウスエミュレーションの感度・加速度が実機操作で従来と体感差がないことを確認する。

### 【ドメイン3: 起動・UI層】

#### Phase6-Step5: `App.xaml.cs` 起動シーケンスの整理
- **対象**: 22件。アプリ起動時のパス解決・多重起動チェック等、Composition Root構築前後のタイミング依存が強い領域。
- **【アーキテクチャ・ガードレール注記】**:
  - **[起動順序逆転防止]（§5.4継承）**: Phase5-Step10の「On-Demandパス評価」原則を踏襲し、DIコンテナ構築前に評価が必要な値（パス確定等）はコンストラクタキャッシュを避ける。

#### Phase6-Step6: `ProfileEditor.xaml.cs` のGlobal直参照解消
- **対象**: 18件。Phase5-Step13完了時点で対象漏れになっていた箇所（`MainWindow.xaml.cs`のみが対象で本ファイルは含まれていなかった）。Phase5-Step13と同一の置換パターン（`AppHost.GetService<T>() ?? Global.XxxInstance`フォールバック）をそのまま踏襲できる見込み。

#### Phase6-Step7: `SettingsViewModel.cs`他 主要ViewModel群の解消
- **対象**: 39件（`SettingsViewModel.cs`23件、`MainWindowsViewModel.cs`8件、`TrayIconViewModel.cs`5件、`ProfileSettingsViewModel.cs`4件（このうち一部はコンストラクタで既にDIサービスを受け取っているため、内部の未使用箇所の置換のみ)）。

#### Phase6-Step8: 残りの小型UIファイル群の解消
- **対象**: 29件（`WelcomeDialog.xaml.cs`9件、`PresetOption.cs`10件、`SaveWhere.xaml.cs`5件、`BindingWindow.xaml.cs`5件）。件数が少なく個別ファイルの複雑度も低いため、1PRでまとめて対応可能な見込み。

### 【ドメイン4: 検証・仕上げ】

#### Phase6-Step9: 自動テスト・実機検証
- **内容**: 各Step完了ごとにビルド・既存自動テスト（Actions/Standalone、Phase5までに追加された新規テスト含む）を実行する。`ControlService.cs`/`Mouse.cs`が絡むStep2・Step4は、実機での連続入力・マウス操作の回帰確認を必須項目とする。

#### Phase6-Step10: 呼出元0件シムの物理削除判断
- **内容**: Phase6完了時点で`Global`内の各メンバについて、リポジトリ全体でのgrep突合により呼出元0件を再確認する。0件が確定したメンバに`[Obsolete]`を付与し、次回リリースサイクルでの物理削除候補としてリストアップする（即時削除は行わず、Phase5-Step15と同様に安全側に倒す）。

---

## 3. 実装における潜在的懸念点とアーキテクチャ・ガードレール

Phase5の6大ガードレールに加え、Phase6固有の懸念点を追加する。

### 3.1 [新規] ホットパス性能維持（Step2, Step4）
- **リスク**: `ControlService.cs`・`Mouse.cs`のGlobal参照の一部は、入力ポーリングループ（毎秒250〜1000回）から直接呼ばれる。静的フィールドアクセス（配列インデックスアクセス等、O(1)かつJIT最適化されやすい）をDIサービス経由の仮想メソッド呼び出しに置き換える際、抽象化のオーバーヘッドや誤ったキャッシュ設計（Phase5-Step10で撤廃した「起動時キャッシュ」の再発）により、体感可能な入力遅延が生じるリスクがある。
- **ガードレール**: 置換対象がホットパスに該当するかを事前分類し、該当する場合は置換前後で簡易ベンチマーク（同一操作の入力→出力遅延比較）を実施する。DIサービス側の実装は`IProfileSettingsService`等の既存パターン同様、内部キャッシュを持たずBackingStoreへの直接委譲とする。

### 3.2 [継承] 既存6大ガードレール（Phase5-Plan.md §5より）
- ファイルI/O排他、Halt保証、スレッド直列化、On-Demandパス評価、ViGEmドライバ破棄順序、切断時クリーンアップの各原則を、該当するStepで引き続き遵守する。

### 3.3 [新規] ドキュメント記述の実地確認原則
- Phase5-Watchpoints報告書で判明した教訓（grep未確認の具体例記述が計画の前提を誤らせた事例）を踏まえ、Phase6の各Step計画書・完了報告書に記載する`Global.*`メンバ名や参照箇所は、必ず`git clone`等による実地grep確認を経てから記載する。

---

## 4. 完了条件

- [ ] Step1で確定した対象（真の除外分を除く実質193件相当）の呼出元が、全てDIサービス経由の呼び出しに置換されていること。
- [ ] `ControlService.cs`, `Mapping.cs`, `Mouse.cs`, `MouseCursor.cs`のホットパスにおいて、置換前後で体感可能な性能劣化がないことを実機確認済みであること。
- [ ] 既存の全自動テスト（Actions／Standalone／Phase4・5で追加された単体テスト）が成功を維持していること。
- [ ] `ProfileEditor.xaml.cs`を含むUI層全体で、Phase5-Step13と同水準の静的参照撲滅が完了していること。
- [ ] Phase6完了時点で呼出元0件となった`Global`メンバに`[Obsolete]`が付与され、削除候補リストが更新されていること。
- [ ] ビルドエラーおよび警告の増加がないこと。

---

## 5. 進行ルール（Phase5-Plan.md §7を継承）

- **マイクロステップの原則**: 各Stepは個別計画書を作成し、承認を得てからマイクロタスク単位で実装する。
- **No Feature Drop**: リファクタリングによる機能削減・設定値欠落・通知抑制不全を発生させない。
- **ガードレール順守の原則**: 第3章の潜在リスクへの予防策を、該当するStepの個別計画書に明記した上で着手する。
- **実地確認の原則**（Phase6新設）: 計画書・報告書に記載する`Global`メンバ名・参照箇所数は、実装前に必ず`git`ベースの実地確認を経る。

---

## 6. スケジュール見積り

`Phase5-Step13+14-Addendum-Findings-Report`シリーズおよびPhase1〜5の実績（AIエージェントによる実装で、人間チーム想定見積りに対し平均5〜19倍の速度を記録）に基づく概算。

| 区分 | 人間チーム想定見積り | 実績ベース速度倍率 | Phase6見積り |
|---|---|---|---|
| ドメイン1（コア層、Step2-3） | 約1.5〜2週間 | 5〜10倍（ホットパス検証を含むため保守的） | 1.5〜3日 |
| ドメイン2（出力層、Step4） | 約3〜5日 | 8〜15倍 | 0.5〜1日 |
| ドメイン3（UI層、Step5-8） | 約1〜1.5週間 | 8〜15倍 | 1〜2日 |
| ドメイン4（検証・仕上げ、Step9-10） | 約2〜3日 | 実機検証は人間ペース律速 | 1〜2日 |
| **合計** | **約3〜4週間** | - | **実働 4〜8日程度** |

※ ドメイン1（`ControlService.cs`）は入力遅延の実機体感確認を伴うため、他ドメインより保守的な倍率を採用している。

---

## 7. 次のアクション

1. 本計画書をレビュー・承認する。
2. Phase6-Step1（詳細監査）に着手し、`Phase6-Step1-Global-Usage-Classification-Report.md`を作成する。
3. Step1完了後、ドメイン1（`ControlService.cs`）から順に個別計画書を1つずつ作成・承認を得てから実装する。
4. 各Step完了時にビルド・自動テストを実行し、結果を`Phase6-Status.md`（新設）に記録する。