# プロファイル全設定項目 保存機能 実機テストチェックリスト

## 1. 概要
プロファイル編集画面（`ProfileEditor`）において各種設定を変更し保存を実行した際、設定値が `Profiles/[プロファイル名].xml` へ正確に書き込まれているかを網羅的に検証するための実機テスト仕様書です。

---

## 2. テスト共通プロトコル

### ■ 事前準備
1. テスト専用のプロファイル（例: `TestSaveCheck.xml`）を新規作成する。
2. 作成した直後の XML ファイルを退避または Git でクリーンな状態として記録する。
3. XML 差分確認ツール（VS Code の差分比較機能、WinMerge 等）を準備する。

### ■ テスト実行手順
1. プロファイル編集画面で対象の設定項目を変更する。
2. 画面下部の **「Save（保存）」** または **「Apply（適用）」** ボタンを押下する。
3. テキストエディタまたは差分ツールで `Profiles/TestSaveCheck.xml` を再読み込みする。
4. 対象の XML タグの値が、UI上で変更した値に正しく更新されているかを確認する。
   - 正しく反映されている ➔ **PASS**
   - タグ自体が出力されない、あるいは変更前の値のまま ➔ **FAIL**

---

## 3. テストチェックマトリクス

### カテゴリA: Special Actions（特殊アクション）タブ 【既知の不具合確認】
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| A-01 | **Special Action 有効/無効** | 任意の複数アクションのチェックボックスを ON/OFF | `<ProfileActions>` | **特大** | [ ] | スラッシュ区切り文字列が更新されるか |

---

### カテゴリB: Controls / Mapping（ボタンマッピング）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| B-01 | **Output Controller Type** | Xbox 360 ⇄ DualShock 4 を切り替え | `<PadOutDevType>` | 中 | [ ] | 出力仮想デバイス種類 |
| B-02 | **通常ボタン割り当て** | ×ボタンを「Key A」等に変更 | `<Control>` / `<Button>` | 低 | [ ] | 単一キーマッピング |
| B-03 | **マクロ割り当て** | 任意のボタンに複数キーのマクロを記録 | `<Macro>` | 高 | [ ] | キーストローク配列の保存 |
| B-04 | **Shift Modifier 割り当て** | Shift Trigger設定 ＋ Shift時キー割り当て | `<ShiftControl>` | **特大** | [ ] | 修飾キー押下時のマッピング |
| B-05 | **Vader 4 Pro 背面ボタン (M1〜M4)** | M1〜M4 に任意の入力を割り当て | `<Vader4ProControllerOpts>` / `<Control>` | **特大** | [ ] | Vader 4 Pro 拡張マッピング |
| B-06 | **Vader 4 Pro 追加ボタン (C / Z)** | C / Z に任意の入力を割り当て | `<Vader4ProControllerOpts>` / `<Control>` | **特大** | [ ] | 前面追加ボタンマッピング |

---

### カテゴリC: Left Stick / Right Stick（スティック詳細）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| C-01 | **DeadZone / MaxZone** | スライダー値をデフォルトから変更 | `<LSDeadZone>`, `<RSDeadZone>`, `<LSMaxZone>` | 低 | [ ] | 基本デッドゾーン設定 |
| C-02 | **AntiDeadZone / MaxOutput** | スライダー値を変更 | `<LSAntiDeadZone>`, `<LSMaxOutput>` | 低 | [ ] | 出力レンジ調整 |
| C-03 | **Output Curve（出力カーブ）** | Linear ➔ Enhanced Precision 等に変更 | `<LSCurve>`, `<RSCurve>` | 中 | [ ] | 応答カーブ列挙値 |
| C-04 | **Custom Curve（ベジェ曲線）** | カーブエディタでカスタム曲線を編集 | `<LSCustomCurve>`, `<RSCustomCurve>` | **特大** | [ ] | 外部エディタからの値反映 |
| C-05 | **DeadZone Type** | Radial ⇄ Axial を切り替え | `<LSDeadZoneType>` | 中 | [ ] | デッドゾーン形状 |
| C-06 | **軸別設定 (`AxialStickUserControl`)** | X/Y 個別のデッドゾーン・MaxZoneを変更 | `<LSAxialDeadOptions>`, `<RSAxialDeadOptions>` | **特大** | [ ] | 独立UserControlからの値吸い上げ |
| C-07 | **Snap to Axial** | チェックボックスを ON にする | `<LSSnap>`, `<RSSnap>` | 中 | [ ] | 軸スナップ設定 |
| C-08 | **Invert / Vertical Scale** | 軸反転ON、感度スケール値を変更 | `<LSInvert>`, `<LSSens>` | 低 | [ ] | 反転および感度スケール |
| C-09 | **Flick Stick モード** | Flick Stick を有効化し各パラメータ変更 | `<LSFlickStick>`, `<FlickThreshold>` | 高 | [ ] | フリックスティック詳細設定 |

---

### カテゴリD: L2 / R2 Trigger（トリガー）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| D-01 | **Trigger DeadZone / MaxZone** | スライダー値を変更 | `<L2DeadZone>`, `<R2DeadZone>` | 低 | [ ] | トリガー有効ストローク範囲 |
| D-02 | **Trigger AntiDeadZone / Sens** | スライダー値を変更 | `<L2AntiDeadZone>`, `<L2Sens>` | 低 | [ ] | トリガー立ち上がり感度 |
| D-03 | **Trigger Output Curve** | カーブ形状を変更 | `<L2Curve>`, `<R2Curve>` | 中 | [ ] | トリガー応答カーブ |
| D-04 | **Two Stage Trigger Mode** | Hair Trigger / Normal 等に変更 | `<L2TwoStageMode>`, `<R2TwoStageMode>` | **特大** | [ ] | 2段階トリガー動作モード |
| D-05 | **Trigger Effect / Force Feedback** | 抵抗値や振動モードを変更 | `<TriggerEffect>` / `<Vader4Pro...>` | **特大** | [ ] | フォースフィードバック設定 |

---

### カテゴリE: Touchpad（タッチパッド）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| E-01 | **Touchpad Output Mode** | Mouse ⇄ Controls ⇄ Passthru 変更 | `<TouchpadOutputMode>` | 中 | [ ] | 動作モード切り替え |
| E-02 | **Trackball Mode / Friction** | トラックボールON、摩擦係数変更 | `<TrackballMode>`, `<TrackballFriction>` | 中 | [ ] | 慣性シミュレーション設定 |
| E-03 | **Sensitivity / Invert** | 感度スライダー変更、反転チェックON | `<TouchSensitivity>`, `<TouchInvert>` | 低 | [ ] | カーソル移動感度・反転 |
| E-04 | **Touch Button (`TouchButtonUserControl`)** | タッチ領域のボタン割り当てを変更 | `<TouchButtonAllocations>` | **特大** | [ ] | 独立UserControlからの値吸い上げ |

---

### カテゴリF: Gyro / Sixaxis（ジャイロ）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| F-01 | **Gyro Output Mode** | Mouse ⇄ Controls ⇄ Directional Swipe 変更 | `<GyroOutputMode>` | 中 | [ ] | ジャイロ割り当て先 |
| F-02 | **Gyro Sensitivity / Invert** | X/Y感度・反転チェック変更 | `<GyroSens>`, `<GyroInvert>` | 低 | [ ] | ジャイロ追従感度 |
| F-03 | **Gyro DeadZone / MaxZone** | スライダー値を変更 | `<GyroDeadZone>`, `<GyroMaxZone>` | 低 | [ ] | 不感帯・最大有効角速度 |
| F-04 | **Gyro Smoothing (One Euro Filter)** | スムージング方式・MinCutoff/Beta変更 | `<GyroSmoothing>`, `<GyroMinCutoff>` | 高 | [ ] | フィルタリングパラメータ |
| F-05 | **Gyro Trigger (有効化ボタン)** | 「L2押下時のみ有効」等に設定 | `<GyroTrigger>` | 中 | [ ] | ジャイロ作動条件 |

---

### カテゴリG: Lightbar（ライトバー）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| G-01 | **Lightbar Mode** | Custom ⇄ Battery Status 変更 | `<LightbarMode>` | 低 | [ ] | LED点灯モード |
| G-02 | **Custom Color (RGB)** | カラーピッカーで色数値を変更 | `<LedColor>` / `<CustomLed>` | 低 | [ ] | 発光色データ（HEX/RGB） |
| G-03 | **Flash Type / Flash At** | 点滅パターン・点滅開始残量変更 | `<FlashType>`, `<FlashAt>` | 低 | [ ] | バッテリー低下時警告点滅 |
| G-04 | **Charging Rainbow** | 充電中レインボー点滅を ON にする | `<ChargingType>` | 低 | [ ] | 充電時イルミネーション |

---

### カテゴリH: Other / Miscellaneous（その他・デバイス固有）タブ
| ID | UI設定項目名 | 操作内容（変更例） | 期待されるXMLタグ | 重要度 | 結果 | 備考 |
| :--- | :--- | :--- | :--- | :---: | :---: | :--- |
| H-01 | **Idle Disconnect** | 自動切断時間を変更（例: 5分） | `<IdleDisconnect>` | 低 | [ ] | 無操作タイムアウト時間 |
| H-02 | **Bluetooth Poll Rate** | ポーリングレート（例: 1ms/1000Hz）変更 | `<BTPollRate>` | 低 | [ ] | 通信更新頻度 |
| H-03 | **Touchpad Jitter Compensation** | ジッター補正を ON/OFF | `<TouchJitterCompensation>` | 低 | [ ] | タッチ微小振動抑制 |
| H-04 | **Button Mouse Sensitivity** | ボタン割り当て時のマウス感度変更 | `<ButtonMouseSens>` | 低 | [ ] |  |
| H-05 | **Vader 4 Pro モーター振動強度** | グリップおよびトリガーモーター強度変更 | `<Vader4ProControllerOpts>` 配下 | **特大** | [ ] | Vader 4 Pro 独自DTO保持項目 |

---

## 4. 最重点確認項目（優先実施推奨）

実機テストを実施する際は、データ欠落の可能性が極めて高い以下の **4大リスク箇所** を優先して検証してください。

1. **A-01: `<ProfileActions>`**（既知の不具合確認）:
   - チェックボックス変更後に保存した際、XML内の文字列が即時反映されるか。
2. **B-05, B-06, H-05: Vader 4 Pro 独自設定（`Vader4ProControllerOpts`）**:
   - 本フォークで拡張された背面ボタン（M1〜M4）、前面追加ボタン（C, Z）、モーター振動パラメータが正しく保存されるか。
3. **C-06: 軸別スティック設定（`AxialStickUserControl`）**:
   - 親 ViewModel と別コントロールで編集された X/Y 軸の個別デッドゾーンが、親の保存処理に反映されるか。
4. **B-04: シフト修飾マッピング（`ShiftControl`）**:
   - 修飾キー押下時の特殊マッピングが、DTO変換時にクリアされずに保持されるか。