# フェーズ6 - ステップ6 個別計画書: 出力デバイス切替時のマッピング一覧追従修正、および `IOutputSlotService` 負債APIの取り扱い方針確定

作成日: 2026-09-11
対象ブランチ: `For-DI-migration-work`
前ステップ: Phase6-Step5（`OutputSlot / 横断設定の是正`）
全体計画書: `docs-forDIMG/DI-App-Wide-Migration-Plan.md`（§4.5 カテゴリA〜F、§5.5 `Mapping.cs`完全instance化見送りの判断ロジック）
Phase6計画書: `docs-forDIMG/MadeByAgent/Phase6-Plan.md`（§1, §0.2, §2 Phase6-Step6, §3.4）
移管元ドキュメント:
- `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md`（§3, §5.4, §8.2）
- `docs-forDIMG/MadeByAgent/Phase5-Step14-Issue7-Fix-Plan.md`（タスク5・タスク6）
- `.github/copilot-instructions.md`（§3.1 Pure DI原則、§3.3-4 残置クラスへの経緯コメント明記、§6.11 実地確認の原則）

---

## 1. 位置づけ

Phase6-Plan.md §0.2・§1では、本ステップを「出力デバイス切替時の実行時スロット動的再接続（ホットスワップ）連動」として位置づけ、「旧Phase5-Step14 タスク5・6」の移管としている。該当する `Phase5-Step14-Issue7-Fix-Plan.md` 上のタスクは以下の2件である。

- **タスク5（任意）**: `ProfileEditor.xaml.cs` の `Reload()` 内、`profileSettingsVM.UpdateLateProperties();` の直後に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加し、同一エディタウィンドウ内でプロファイルを切り替えた際にマッピング一覧の機種表示を追従させる。
- **タスク6**: `IOutputSlotService.cs` の `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` に技術的負債コメントを付与する。

**重要**: Phase6-Plan.md §2の本文は、これを「実際にViGEmと連動する仮想デバイスの動的アンプラグ／再プラグインを実装するホットスワップ機能」として、タスク5・6よりも大幅に踏み込んだ記述（§2.1参照）をしている。本計画書は、実装着手前の実地確認により、この記述と実際のコードとの間に重要な乖離があることを確認したため、その事実を明記したうえで対応方針の選択をgwin7ok氏に仰ぐ形で構成する。

---

## 2. 実地確認結果（2026-09-11、`git clone --depth 1 --branch For-DI-migration-work` によるリポジトリ全文 `grep`）

### 2.1 タスク5相当のバグは実在する

```
DS4Windows/DS4Forms/ProfileEditor.xaml.cs:1094: public void Reload(int device, ProfileEntity profile = null)
...
1148:  profileSettingsVM.UpdateLateProperties();
      （UpdateMappingDevType呼び出しなし）

DS4Windows/DS4Forms/ProfileEditor.xaml.cs:1221: private void RefreshEditorBindings()
1225:  profileSettingsVM.UpdateLateProperties();
      （UpdateMappingDevType呼び出しなし）
```

一方、コンボボックスの手動選択時のみ、以下のように追従処理が存在する。

```
DS4Windows/DS4Forms/ProfileEditor.xaml.cs:1911:
private void OutConTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    int index = outConTypeCombo.SelectedIndex;
    if (index >= 0)
    {
        mappingListVM.UpdateMappingDevType(profileSettingsVM.TempConType);
    }
}
```

`Phase5-Step14-Issue7-Fix-Plan.md` §3の記載通り、**エディタを閉じずに別プロファイルへ切り替えた場合（`Reload()` 経由）にのみ、マッピング一覧のボタン名表示が旧プロファイルの機種のまま残る**ことを確認した。これはタスク5として是正が必要である。

### 2.2 【重要な新規発見】実行時ホットスワップ自体は既存レガシー経路で既に機能している

Phase6-Plan.md §2 Step6の記述は、「プロファイル設定変更イベントを検知し、現在接続中の仮想スロットのコントローラー種別を動的にアンプラグ＆再プラグインするホットスワップ連動を実装する」ことを求めている。この機能が現在**存在しない**という前提に立った記述だが、実地確認の結果、**同等の機能は既に `Global`／`ScpUtil.cs` 側のレガシー経路で実装済み**であることを確認した。

```
DS4Windows/DS4Control/ScpUtil.cs（PostLoadSnippet内、抜粋）:

else if (!dinputOnly[device] && oldContType != outputDevType[device])
{
    xinputPlug = true;
    xinputStatus = true;
}
...
private void PostLoadSnippet(int device, ControlService control, bool xinputStatus, bool xinputPlug)
{
    ...
    if (xinputPlug)
    {
        OutputDevice tempOutDev = control.outputDevices[device];
        if (tempOutDev != null)
        {
            tempOutDev = null;
            control.UnplugOutDev(device, tempDev);
        }
        OutContType tempContType = outputDevType[device];
        control.PluginOutDev(device, tempDev);
    }
    else
    {
        control.UnplugOutDev(device, tempDev);
    }
    ...
}
```

すなわち、プロファイルの `Apply`（`Global.ApplyProfile` → 内部の `LoadProfile`/`PostLoadSnippet`）が実行される際、**保存前の `oldContType` と保存後の `outputDevType[device]` を比較し、変化していれば `UnplugOutDev` → `PluginOutDev` によって実際のViGEm仮想デバイスを再接続する処理が既に存在する**。この経路は `IOutputSlotService` を一切経由せず、`ControlService.cs`／`Mapping.cs` が `Global.OutContType[device]` を直接参照する構造（`Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md` §5.4で確認済み）と一体になっている。

### 2.3 §2.2を踏まえた `IOutputSlotService` 負債APIの性質の再評価

`IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` は、`Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md` §5.2の実地確認（`grep`）により、アプリ本体コードから**呼出元0件**（自身の単体テストを除く）であることが確定している。§2.2の発見と合わせると、これは「本来あるべき機能が未接続のまま放置されている」のではなく、**「同等の機能が既に別経路（`Global`／`ScpUtil.cs`）で正常に稼働しており、`IOutputSlotService` 側の実装はそもそも一度も採用されなかった並行実装（未使用の代替設計）である」**と再評価するのが実態に即している。

この再評価に基づき、Phase6-Plan.md §2 Step6の「実行時ViGEm接続状態を正しく問い合わせ・制御する正式APIとして昇華・整理する」という当初方針は、実装のギャップ分析の結果、前提が成立しないため、そのままでは採用できない。

---

## 3. 対応方針の分岐案（要承認・実装着手前に必ず確認）

以下の2案を提示する。**いずれを採用するかは、実装着手前にgwin7ok氏の承認を得る。**

### 案1: 保守的対応（推奨）

- タスク5（`ProfileEditor.xaml.cs` の `Reload()`/`RefreshEditorBindings()` への `UpdateMappingDevType` 追加）のみを実装する。
- `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` の実装自体は変更しない。ただし、§2.3の再評価結果を反映した技術的負債コメントを付与する（`Phase5-Step14-Issue7-Fix-Plan.md` タスク6を、実態に即した内容に更新して実施）。
- 既存のホットスワップ機能（`Global`／`ScpUtil.cs` の `PostLoadSnippet` 経路）はStrangler Fig方針のもとレガシーシムとして温存し、Phase6のスコープでは一切変更しない。

**採用理由**:
1. 全体計画書§4.5「仮想コントローラー出力バックエンド（ViGEm）について」は、`ControlService.cs` のViGEm型への直接アクセスを「`Global`静的クラスの問題とは無関係の別種の技術的負債」として明示的にPhase6・Phase7の対象外としている。`PostLoadSnippet` のホットスワップ判定・実行ロジックも、この「ViGEm実体操作」に該当する。
2. 全体計画書§5.5は、`Mapping.cs`の完全instance化について「影響範囲が非常に大きいリファクタリングは、周辺の副作用切り出しが完了してから再評価する」という段階的判断ロジックを採用している。本件（実働しているホットスワップ機構を、呼出元0件のAPIに置き換える）も同種の「動いているものを、リスクを冒して置き換える」作業であり、同じ慎重さが妥当する。
3. Phase6全体の完了条件（Phase6-Plan.md §4）は「呼出元193件の解消」であり、`IOutputSlotService` の当該4メソッドは呼出元0件（デッドコード）である。呼出元が存在しないコードを「置き換える」ことはPhase6の目的（呼び出し元側のGlobal直参照解消）の範囲外である。

### 案2: 積極的対応（Phase6-Plan.md原案に忠実だが高リスク）

- `PostLoadSnippet` のホットスワップ判定・実行（`oldContType != outputDevType[device]` の比較、`UnplugOutDev`/`PluginOutDev` 呼び出し）を `IOutputSlotService` 経由の呼び出しに置き換える。
- これにより `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType` が初めて実際に使用される「正式API」になる。
- Phase6-Plan.md §3.4「スロット動的再接続時の ViGEm バス競合保護」ガードレールの新規設計・実装が必要になる。
- `ControlService.cs`（ホットパス）および `ScpUtil.cs`（既存の巨大シム）双方への変更を伴うため、Phase6-Step2（`ControlService.cs`のGlobal直参照解消）との作業競合・依存関係の整理が必要。
- 実機でのプロファイル切替・デバイス再接続シナリオの回帰テストが必須（クラッシュ・デバイス切断リスクを伴うため）。

**不採用推奨の理由**: 現在正常に動作している機能（ホットスワップ自体）に対する置き換えであり、副作用（No Feature Drop違反、ViGEmドライバクラッシュ等）のリスクに見合うだけの実利（呼出元0件のAPIを「正式化」する以外の便益）が乏しい。将来的にViGEmバックエンド自体を差し替える別イニシアチブ（全体計画書§4.5末尾の`IVirtualControllerBackend`構想）に着手する際に、あらためて設計し直すのが合理的である。

### 3.1 本計画書の既定方針

**本計画書は案1を推奨案として以降のマイクロタスクを記述する。** gwin7ok氏が案2を選択する場合は、実装着手前に別途詳細設計（ガードレール設計を含む）を追加で行う必要があるため、その旨を申し出ること。

---

## 4. スコープ確定（案1採用時）

### 4.1 スコープに含むもの

1. `ProfileEditor.xaml.cs` の `Reload()` および `RefreshEditorBindings()` への `mappingListVM.UpdateMappingDevType(...)` 追加。
2. `IOutputSlotService.cs` の `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` への、§2.3の再評価結果を反映した技術的負債コメントの付与（`copilot-instructions.md` §3.3-4準拠）。
3. 上記コメントの内容を、全体計画書またはPhase6-Status.mdの技術的負債一覧に反映する。

### 4.2 スコープに含まないもの

1. `PostLoadSnippet`／`Global.ApplyProfile` 経路のホットスワップロジック自体の変更（案2を選択しない限り対象外）。
2. ViGEmバックエンドの抽象化（全体計画書§4.5により恒久的に対象外）。
3. `Mapping.cs`の完全instance化（Phase7へ委譲済み）。

---

## 5. 修正対象ファイル棚卸し（案1ベース）

| # | ファイル | 変更内容 | 種別 |
|---|---|---|---|
| 1 | `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | `Reload()` 内、`profileSettingsVM.UpdateLateProperties();` の直後に `mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);` を追加 | 追加修正 |
| 2 | `DS4Windows/DS4Forms/ProfileEditor.xaml.cs` | `RefreshEditorBindings()` 内、同一パターンで `UpdateMappingDevType` 呼び出しを追加要否を確認し、必要なら追加 | 追加修正 |
| 3 | `DS4Windows/DI/IOutputSlotService.cs` | `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` の宣言部に技術的負債コメントを付与 | コメント追記のみ |
| 4 | `docs-forDIMG/DI-App-Wide-Migration-Plan.md` または `Phase6-Status.md` | `IOutputSlotService` 4メンバの技術的負債（呼出元0件、実機能は`Global`/`ScpUtil.cs`側に存在）を記録 | ドキュメント追記 |

> **No Feature Drop方針**: `ProfileEditor.xaml.cs` への追加はUI表示の追従処理のみであり、`BackingStore` への保存内容・保存タイミングには影響しない。

---

## 6. マイクロタスク breakdown（案1ベース）

### タスク1: `ProfileEditor.xaml.cs` の `Reload()` 修正

- [ ] `Reload()` 内、`profileSettingsVM.UpdateLateProperties();` の直後に以下を追加:
  ```csharp
  mappingListVM.UpdateMappingDevType(profileSettingsVM.ContType);
  ```
- [ ] `profileSettingsVM.ContType` は `Phase6-Step5` で是正済みの `profileSettings.OutContType[device]`（正しい永続化実体）を参照するgetterであることを前提とする。Phase6-Step5が本ステップより先に完了していることを確認する。

### タスク2: `RefreshEditorBindings()` への同一修正の要否確認と実装

- [ ] `RefreshEditorBindings()` が `Reload()` と同様に別プロファイルへの切替を伴うケースで呼ばれるかを呼び出し元（2421行目付近）から確認する。
- [ ] 該当する場合、`profileSettingsVM.UpdateLateProperties();` の直後に同一の `UpdateMappingDevType` 呼び出しを追加する。

### タスク3: `IOutputSlotService.cs` への技術的負債コメント付与

- [ ] `GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` の宣言部に、以下のようなコメントを付与する（内容は§2.3の再評価結果を反映）:
  ```csharp
  // TODO(技術的負債・Phase6-Step6で再評価): 呼出元0件（2026-09-11 grep確認、
  // Phase5-Step14-Issue7-RootCause-and-CrossSetting-Audit-Report.md §5.2）。
  // 実際の出力デバイス種別のホットスワップ（アンプラグ／再プラグイン）は、
  // Global.ApplyProfile → ScpUtil.cs の PostLoadSnippet（oldContType比較 →
  // ControlService.UnplugOutDev/PluginOutDev）という別経路のレガシーシムで
  // 既に実現されており、このAPIは一度も採用されなかった並行実装である。
  // 削除／実配線は、ViGEmバックエンド抽象化（全体計画書§4.5末尾、
  // IVirtualControllerBackend構想）を独立イニシアチブとして実施する際に
  // 再評価すること。プロファイル永続値の参照には
  // IProfileSettingsService.OutContType を使用すること。
  ```
- [ ] `docs-forDIMG/DI-App-Wide-Migration-Plan.md` または `Phase6-Status.md` の技術的負債一覧に、`IUdpServerService` 未接続の記載と並記する形で追記する。

### タスク4: 単体テスト・ビルド確認

- [ ] `dotnet build` によるクリーンビルド確認（警告0・エラー0）。
- [ ] `dotnet test` による全件PASS確認。
- [ ] 実機またはUIレベルの確認: プロファイル編集画面を開いたまま、`Emulated Controller` 設定が異なる2つのプロファイルを順に読み込み（`Reload()` 経由）、マッピング一覧のボタン名表示が都度正しい機種名に切り替わることを確認する。

---

## 7. リスクと回避策

| リスク | 該当タスク | 回避策 |
|---|---|---|
| `RefreshEditorBindings()` への追加が、`Reload()` とは異なる呼び出しコンテキスト（機種変更を伴わない軽微な再描画）で不要な処理を発生させる | タスク2 | 呼び出し元（2421行目付近）のコンテキストを確認し、機種変更が発生しうる経路かどうかを個別判定してから追加する |
| 案1採用にもかかわらず、将来のエージェント／開発者が技術的負債コメントを見落とし、`IOutputSlotService` の当該4メソッドを「正式API」と誤認して新規に呼び出してしまう | タスク3 | コメント内で「並行実装であり採用されていない」ことを明示し、正しい参照先（`IProfileSettingsService.OutContType`、`Global.ApplyProfile`系列）を明記する |
| Phase6-Plan.md本文（§2 Step6）の記述と、本計画書の方針（案1）が食い違ったまま放置される | 全体 | 本計画書の完了後、Phase6-Plan.md §2 Step6の記述を実装結果に基づいて改訂することを次のアクションに含める |

---

## 8. 完了条件（案1ベース）

1. `ProfileEditor.xaml.cs` 内でプロファイルを切り替えた場合（`Reload()` 経由）、マッピング一覧のボタン名表示が常に現在のプロファイルの `Emulated Controller` 設定と一致すること。
2. `IOutputSlotService.GetOutputDeviceType`/`SetOutputDeviceType`/`PluginSlot`/`UnplugSlot` に、実態（呼出元0件・並行実装）を正しく反映した技術的負債コメントが付与されていること。
3. 全自動テストがクリーンにPASSし、ビルド警告・エラーが増加していないこと。
4. Phase6-Plan.md §2 Step6の記述が、本ステップの実装結果（案1採用の経緯を含む）に基づいて改訂されていること。

（案2を採用する場合は、Phase6-Plan.md §3.4のガードレール設計を含めて完了条件を別途再定義する。）

---

## 9. 進行ルール

- **案1/案2の選択について、着手前に必ずgwin7ok氏の承認を得る。**
- 承認後、案1のマイクロタスク（§6）を1タスク＝1PRを基本として順次進める。
- 巨大ファイル編集時はピンポイント編集ルールを徹底する。
- 実装完了後、`Phase6-Status.md` のStep6欄を更新し、Phase6-Plan.md §2 Step6の記述を実態に合わせて改訂する。
- 成果物は `present_files` による直接ダウンロード形式で提供する（PowerShellスクリプト形式は使用しない）。

---

## 10. 次のアクション

1. gwin7ok氏に本計画書§3の案1／案2の選択を確認する。
2. 案1が選択された場合、Phase6-Step5の完了（`profileSettingsVM.ContType` が正しい実体を参照する状態）を前提条件として着手する。
3. タスク1〜4を実施し、`dotnet build`／`dotnet test` の結果と実機確認結果を添えて完了報告書を作成する。
4. Phase6-Plan.md §2 Step6の記述を、本計画書の実地確認結果（§2.2, §2.3）に基づいて改訂する。